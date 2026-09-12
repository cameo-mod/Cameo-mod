#!/usr/bin/env python3
"""apply_carrier_slave_ammo.py — give every carrier slave a sized pool and a reload (R8).

GENERATED, never hand-typed: the numbers come from `carrier_slave_ammo.plan()`, whose
self-test reproduces both of the maintainer's worked examples. This script only decides WHERE
to write them.

THE BUG BEING FIXED. All 19 `CarrierSlave` actors break the rule, in two opposite ways:

  * NO POOL  -> `CarrierSlave.cs:59-65` returns early, commented "The unit may not have ammo
    but will have unlimited ammunitions." The slave never expends, never needs to dock, and
    the carrier's launch/expend/return cycle never runs at all.
  * POOL, NO RELOAD -> the slave empties once and can then never attack again. Nothing
    refills it: `CarrierMaster` has no `GiveAmmo`/`TakeAmmo` path (`RearmTicks` only gates
    relaunch timing), these actors carry no `Rearmable`, and `CarrierSlave.NeedToReload` is
    declared and never called anywhere in CA.

⚠ `AmmoPool.Armaments` DEFAULTS to ("primary", "secondary") and consumption is gated on it —
`AmmoPool.Attacking` calls `TakeAmmo` only when `Info.Armaments.Contains(a.Info.Name)`. An
armament with any other `Name` silently spends nothing, so the pool would never empty. The
list is written whenever the default does not cover every armament.

Usage:  python tools/balance/apply_carrier_slave_ammo.py [--apply]
        (dry run prints the plan and restores the tree; --apply leaves it dirty -- BOOT GATE)
"""
from __future__ import annotations

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

import carrier_slave_ammo as law  # noqa: E402
import miniyaml  # noqa: E402
import yaml_ops  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[2]
POOL_STEMS = ("AmmoPool", "AmmoPoolCA")
RELOAD_STEMS = ("ReloadAmmoPool", "ReloadAmmoPoolCA")


def kv(node):
    return {c.key: str(c.value).strip() for c in node.children}


def stem(key: str) -> str:
    return key.split("@")[0].lstrip("-")


def collect(rs):
    """[(actor_name, src_node, resolved, arms, plan)] for the in-scope slaves, plus skips."""
    out, skipped = [], []
    for name in sorted(rs.actors):
        if name.startswith(("^", "-")):
            continue
        res = rs.resolve(name)
        if res is None or not any(stem(c.key) == "CarrierSlave" for c in res.children):
            continue
        src = rs.actors[name]
        stems = {stem(c.key) for c in res.children}
        arms = []
        for a in [c for c in res.children if stem(c.key) == "Armament"]:
            d = kv(a)
            w = rs.resolve_weapon(d.get("Weapon", "") or "")
            wv = kv(w) if w is not None else {}
            try:
                burst = int(wv.get("Burst", "1").split(",")[0])
            except ValueError:
                burst = 1
            arms.append({"key": a.key, "name": d.get("Name", "primary"), "burst": burst,
                         "cond": d.get("RequiresCondition"), "targets": wv.get("ValidTargets"),
                         "weapon": d.get("Weapon"), "cur_usage": d.get("AmmoUsage"),
                         "local": src.child(a.key)})
        why = law.is_suicide(name, stems, [a["weapon"] for a in arms])
        if why:
            skipped.append((name, why))
            continue
        out.append((name, src, res, arms, law.plan(arms)))
    return out, skipped


def build_plan(rs):
    plan = yaml_ops.new_plan()
    rows, skipped = collect(rs)
    notes = []

    for name, src, res, arms, p in rows:
        if p["ammo"] <= 0:
            notes.append((name, "no scoring armament - skipped"))
            continue
        after = yaml_ops.insert_after_inherits(src)
        add: list[str] = []

        # ---- the pool ---------------------------------------------------- #
        pools = [c for c in res.children if stem(c.key) in POOL_STEMS]
        want_arms = law.pool_armaments(arms)
        if pools:
            local = src.child(pools[0].key)
            cur = kv(pools[0])
            if local is not None and local.child("Ammo") is not None:
                node = local.child("Ammo")
                plan[node.file][node.line].append(
                    ("sub", f"Ammo: {cur['Ammo']}", f"Ammo: {p['ammo']}"))
            else:
                # inherited pool: override just the field, keyed identically so it merges
                add.append(f"\t{pools[0].key}:")
                add.append(f"\t\tAmmo: {p['ammo']}")
            if want_arms and cur.get("Armaments") is None:
                add.append(f"\t{pools[0].key}:" if not add else "")
                add.append(f"\t\tArmaments: {', '.join(want_arms)}")
        else:
            add.append("\tAmmoPool:")
            add.append(f"\t\tAmmo: {p['ammo']}")
            if want_arms:
                add.append(f"\t\tArmaments: {', '.join(want_arms)}")

        # ---- the reload -------------------------------------------------- #
        reloads = [c for c in res.children if stem(c.key) in RELOAD_STEMS]
        if reloads:
            notes.append((name, f"already has {reloads[0].key} - left alone"))
        else:
            add.append("\tReloadAmmoPool:")
            add.append(f"\t\tDelay: {p['delay']}")
            add.append(f"\t\tCount: {p['count']}")

        # ---- per-armament AmmoUsage -------------------------------------- #
        for a in arms:
            want = p["usage"].get(a["key"])
            if want is None or str(want) == str(a["cur_usage"]):
                continue
            local = a["local"]
            if local is not None:
                existing = local.child("AmmoUsage")
                if existing is not None:
                    plan[existing.file][existing.line].append(
                        ("sub", f"AmmoUsage: {str(existing.value).strip()}",
                         f"AmmoUsage: {want}"))
                else:
                    plan[local.file][local.line].append(("ins", [f"\t\tAmmoUsage: {want}"]))
            else:
                # inherited armament: a bare local override merges onto it by key
                add.append(f"\t{a['key']}:")
                add.append(f"\t\tAmmoUsage: {want}")

        add = [ln for ln in add if ln]
        if add:
            plan[src.file][after].append(("ins", add))

    return plan, rows, skipped, notes


def main() -> int:
    apply_it = "--apply" in sys.argv
    rs = miniyaml.Ruleset(str(ROOT))
    paths = sorted({str(p.relative_to(ROOT)).replace("\\", "/") for p in rs.manifest.rules})

    plan, rows, skipped, notes = build_plan(rs)
    n = yaml_ops.apply_plan(plan, ROOT)

    print(f"SKIPPED {len(skipped)} suicide slaves (a reload is dead weight on a unit that "
          f"dies when it attacks):")
    for name, why in skipped:
        print(f"   {name:24s} {why}")

    print(f"\nIN SCOPE {len(rows)} — edits {n}\n")
    print(f"{'actor':26s} {'Ammo':>5s} {'N':>2s} {'share':>5s} {'Count':>5s} {'Delay':>5s}  "
          f"refill  AmmoUsage")
    for name, _src, _res, arms, p in rows:
        if p["ammo"] <= 0:
            continue
        refill = p["ammo"] / p["count"] * p["delay"]
        usage = " ".join(
            f"{a['key'].replace('Armament', 'A')}={p['usage'].get(a['key'])}"
            f"{'' if p['roles'][a['key']] == 'score' else '(' + p['roles'][a['key']][0] + ')'}"
            for a in arms)
        print(f"{name:26s} {p['ammo']:5d} {p['n']:2d} {p['share']:5d} {p['count']:5d} "
              f"{p['delay']:5d}  {refill:5.0f}t  {usage}")

    # The invariant the ruling states, asserted per actor rather than trusted.
    bad = []
    for name, _src, _res, arms, p in rows:
        if p["ammo"] <= 0:
            continue
        if p["ammo"] / p["count"] * p["delay"] != law.RELOAD_TICKS:
            bad.append((name, "refill is not 100 ticks"))
        groups = {}
        for a in arms:
            if p["roles"][a["key"]] != "score":
                continue
            for t in law.target_set(a.get("targets")):
                groups.setdefault(t, 0)
                groups[t] += a["burst"] * p["usage"][a["key"]]
        if groups and max(groups.values()) != p["ammo"]:
            bad.append((name, f"a full attack spends {max(groups.values())}, pool {p['ammo']}"))
    print("\nINVARIANTS (refill == 100 ticks; one full attack empties the pool exactly)")
    if bad:
        for name, why in bad:
            print(f"   FAIL {name:24s} {why}")
    else:
        print("   all pass")
    for name, note in notes:
        print(f"   note {name:24s} {note}")

    if not apply_it or bad:
        yaml_ops.git_restore(ROOT, paths)
        print("\ntree restored." if bad else
              "\ndry run - tree restored. Re-run with --apply, then BOOT GATE.")
        return 1 if bad else 0
    print("\nWRITTEN. Boot-gate before committing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
