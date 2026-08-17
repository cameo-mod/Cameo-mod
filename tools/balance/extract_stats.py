#!/usr/bin/env python3
"""extract_stats.py — Balance Pipeline Phase 1 (BALANCE_PIPELINE.md §2).

yaml -> per-faction RAW-STAT JSON ledger in docs/balance/.

Laws implemented here:
- RAW stats only: every value exactly as the resolved rules state it
  (wdist stays wdist, no DPS, no combined damage — warheads are listed
  raw, one entry per damage warhead).
- Provenance on every value: "<repo-relative-file>#Trait.Field" when the
  value is written in the actor's own block, "inherited" when it comes
  from a template (write-back then knows to add-or-edit).
- Deterministic serialization (sorted keys, fixed indent) so ledger
  diffs are minimal and mergeable.
- `--check`: re-extract in memory and diff against the committed ledger;
  exit 1 on drift (run_all wiring comes in Phase 6).

Usage:
    python tools/balance/extract_stats.py            # write the ledger
    python tools/balance/extract_stats.py --check    # drift detection
    python tools/balance/extract_stats.py --faction tkm   # subset
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
sys.path.insert(0, str(ROOT / "tools/balance"))
from cameo_model import Model  # noqa: E402
import effective_damage as effmod  # noqa: E402
import formula  # noqa: E402
import target_model as tm  # noqa: E402
import weapon_efficiency as we  # noqa: E402

OUT = ROOT / "docs/balance"
DERIVED_OUT = OUT / "derived"
PACKS = ROOT / "mods/cameo/ContentPacks"
DEFAULTS_YAML = ROOT / "mods/cameo/rules/defaults.yaml"

# pack rules files that define balance-relevant actors (closed set,
# DESIGN §2); weapons/sequences/ai/templates/husks are not rosters.
SECTION_FILES = ("faction", "buildings", "defenses", "infantry", "vehicles",
                 "aircraft", "naval", "upgrades", "promotions", "misc")
SHARED_LEAVES = {"Shared", "Core"}

SECTION_DEFAULT_SUBTYPE = {
    "infantry": "Infantry",
    "vehicles": "Vehicle",
    "aircraft": "Aircraft",
    "naval": "Ship",
    "defenses": "Defense",
    "buildings": "Building",
    "upgrades": "Upgrade",
    "promotions": "Promotion",
    "misc": "Misc",
    "faction": "Faction",
}

def rel(p) -> str:
    return str(pathlib.Path(p).resolve().relative_to(ROOT)).replace("\\", "/")


def top_keys(path: pathlib.Path) -> list[str]:
    out = []
    for line in path.read_text(encoding="utf-8-sig", errors="replace").split("\n"):
        if line and (line[0].isalnum() or line[0] in "^_") and ":" in line:
            out.append(line.split(":")[0].strip())
    return out


def child(node, key):
    for c in node.children:
        if c.key == key:
            return c
    return None


def stat(resolved, local, trait: str, field: str):
    """Raw value + provenance for one Trait.Field."""
    t = child(resolved, trait)
    if t is None:
        return None
    v = t.get(field)
    if v is None:
        return None
    lt = child(local, trait) if local is not None else None
    if lt is not None and lt.get(field) is not None:
        src = f"{rel(lt.file)}#{trait}.{field}"
    else:
        src = "inherited"
    return {"v": v, "src": src}


def charge_up(resolved, local):
    """The actor's charge-up attack trait + provenance, or None.

    Charge-up is an ACTOR property, not a weapon one (maintainer ruling
    2026-08-11): the delay inflates the effective reload AND leaves the unit
    helpless while it winds up, neither of which a weapon's own stats show.
    `formula.charge_price_multiplier` turns this into the 0.75x price discount.

    Recorded for EVERY charge trait — the data should show what the tree actually
    has, and the pricing decision belongs in one place, not two.

    W16 also records the MEASURED wind-up so the discount can scale with it:
      ticks — how long the actor spends charging
      cycle — the reload the trait itself governs, when it has one

    ⚠ Both read the ENGINE DEFAULT when the key is absent from the yaml. An absent
    key means default, never zero: the RA2 Tesla Coil writes no
    `InitialChargeDelay`, and reading that as "no charge" made it look like the
    LOWEST-charge Tesla when the engine's default 22 makes it the highest.
    """
    known = formula.CHARGE_UP_TRAITS | formula.CHARGE_UP_EXCLUDED_TRAITS
    for c in resolved.children:
        base = c.key.split("@", 1)[0]
        if base not in known:
            continue
        lt = child(local, c.key) if local is not None else None
        src = f"{rel(lt.file)}#{c.key}" if lt is not None else "inherited"
        rec = {"v": base, "src": src}

        spec = formula.CHARGE_FIELDS.get(base)
        if spec:
            def num(pair, fallback=None):
                if not pair:
                    return fallback
                key, default = pair
                n = child(c, key)
                if n is None or n.value in (None, ""):
                    return default
                try:                       # ChargeLevel may be a LIST (CA's frontal
                    return float(str(n.value).split(",")[0].strip())   # variant)
                except (TypeError, ValueError):
                    return default

            ticks = num(spec.get("charge"))
            rate = num(spec.get("rate"), 1) or 1
            rec["ticks"] = round(ticks / rate, 2)

            # RAW trait fields only. The cycle is NOT precomputed here because it
            # needs the weapon's reload as its burst delay, and combining the two is
            # the formula's job — the ledger stores what the yaml says, one law in
            # one place (formula.charge_attack_cycle).
            cycle_reload = num(spec.get("cycle_reload"))
            if cycle_reload:
                rec["cycle_reload"] = round(cycle_reload, 2)
                rec["burst"] = int(num(spec.get("burst"), 1) or 1)
        return rec
    return None


def defaults_role_templates() -> dict[str, str]:
    """Map full role template names to subtype names from defaults.yaml.

    Only unit-type templates matching ^<Name>Template: are considered, so
    trait/behaviour templates like ^AutoTargetGroundAssaultMove are not
    picked as subtypes.
    """
    cache = getattr(defaults_role_templates, "_cache", None)
    if cache is not None:
        return cache
    out: dict[str, str] = {}
    if DEFAULTS_YAML.exists():
        for line in DEFAULTS_YAML.read_text(encoding="utf-8-sig", errors="replace").splitlines():
            m = re.match(r"^\^([A-Za-z0-9_]+)Template:", line)
            if m:
                out[f"^{m.group(1)}Template"] = m.group(1)
    defaults_role_templates._cache = out
    return out


def _parent_inherits(rs, name: str):
    """Yield the direct Inherits values of a node (template or actor)."""
    node = rs.actor(name)
    if node is None:
        return
    for c in node.children:
        if c.key == "Inherits" or c.key.startswith("Inherits@"):
            v = (c.value or "").strip()
            if v:
                yield v


def actor_subtype(rs, local, section: str) -> str:
    """Derive the unit subtype from the defaults.yaml role template chain.

    Walks the actor's inheritance chain and returns the nearest
    ^<Name>Template it inherits from defaults.yaml.  Units that do not
    inherit a role template get a generic section label rather than
    "Unclassified".
    """
    roles = defaults_role_templates()
    # Start from the actor's own Inherits and walk upward breadth-first so
    # the nearest (most specific) role template wins.
    queue = list(_parent_inherits(rs, local.key)) if local is not None else []
    seen: set[str] = set()
    while queue:
        name = queue.pop(0)
        if name in seen:
            continue
        seen.add(name)
        if name in roles:
            return roles[name]
        queue.extend(_parent_inherits(rs, name))
    return SECTION_DEFAULT_SUBTYPE.get(section, "Unclassified")


WEAPON_CLASS_SIDECAR_PATH = ROOT / "docs/balance/weapon_classes.yaml"


def _load_weapon_class_sidecar() -> dict[str, float]:
    """The SINGLE authoritative source for per-template WeaponClass:
    ``docs/balance/weapon_classes.yaml`` (the linter-proof sidecar).

    The extractor must NEVER hard-code its own weapon-class values — bug
    2026-07-26: a stale in-code table had ``LaserWeapon = 1.0`` while the
    sidecar + legacy Excel said 1.25 (and TeslaWeapon, Grenade, ShrapnelWeapon
    all disagreed too). Keys are stored WITHOUT the leading ``^``.
    """
    out: dict[str, float] = {}
    if not WEAPON_CLASS_SIDECAR_PATH.exists():
        return out
    for line in WEAPON_CLASS_SIDECAR_PATH.read_text(encoding="utf-8").splitlines():
        line = line.split("#", 1)[0].strip()
        if not line or ":" not in line:
            continue
        key, _, val = line.partition(":")
        try:
            out[key.strip().lstrip("^")] = float(val.strip())
        except ValueError:
            continue
    return out


_WEAPON_CLASS_SIDECAR = _load_weapon_class_sidecar()

_WEAPON_CLASS_IGNORE = {
    "ImpactGlow", "AMTProjectile", "RemovesIvanBombs", "RemovesTerrorDrone",
    "DefuseKit", "GenericC4", "TanyaAttach",
}

# Populated during extraction: class templates a weapon references that are
# NOT in the authoritative sidecar (fell through to keyword-guessing). The
# ``--check-weapon-classes`` gate fails when this is non-empty so a missing
# sidecar entry can never silently mis-price a unit again.
_UNMAPPED_WEAPON_TEMPLATES: set[str] = set()


_MIX_ALLOWLIST = {
    "CombatTank", "SiegeTankSiegeCannon", "SiegeEngineCannon",
    # CABAL missile family (2026-08-05): deliberate two-theme x two-tier combos
    # (Missile Light+Medium/Medium+Heavy combined with Demolition_Light +
    # Concussion_Medium), up to 4 warhead inherits, per maintainer's
    # "up to 4 warheads for two-theme combos" rule. See docs/LESSONS_LEARNED.md
    # "Weapon 3-way split — effect/projectile pitfalls" (2026-08-05).
    "CabalReaperMissiles", "CabalHeavyReaperMissiles", "CabalManticoreMissiles",
    "CabalRocketCyborgRockets",
}


def weapon_class_from_types(types: list[str]) -> float | None:
    """Arithmetic mean of a weapon's resolved ^-class templates (DESIGN.md
    §WeaponClass), read from the authoritative sidecar.

    Returns None when no class template is recognised, so weapons with only
    utility templates stay blank instead of defaulting to 1. Any template not
    in the sidecar (and not ignored) is recorded in _UNMAPPED_WEAPON_TEMPLATES
    and given a keyword-based provisional value the check will flag.

    Also returns None when more than two class templates are mixed, because
    the 2-warhead cap (WEAPON_3WAY_SPLIT.md) forbids that unless the weapon
    is on the maintainer allow-list (Dune combat tanks, Siege units).
    """
    vals = []
    for t in types:
        name = t[1:] if t.startswith("^") else t
        if name in _WEAPON_CLASS_IGNORE:
            continue
        # New split-warhead families: only the ^Warhead_* template carries the
        # class; ^Projectile_* / ^Effect_* are visual/projectile components.
        if name.startswith("Projectile_") or name.startswith("Effect_"):
            continue
        if name in _WEAPON_CLASS_SIDECAR:
            vals.append(_WEAPON_CLASS_SIDECAR[name])
            continue
        # NOT in the authoritative sidecar -> record + provisional keyword value
        _UNMAPPED_WEAPON_TEMPLATES.add(name)
        if "Superweapon" in name or "Nuclear" in name:
            vals.append(5.0)
        elif "Superheavy" in name:
            vals.append(1.5)
        elif "Heavy" in name or "Railgun" in name or "Bomb" in name:
            vals.append(1.25)
        elif "Medium" in name or "Chaingun" in name or "Grenade" in name or "Flak" in name:
            vals.append(1.0)
        elif "SmallArms" in name or "Sniper" in name or "Light" in name:
            vals.append(0.75)
        elif "Repair" in name or "Engineer" in name:
            vals.append(1.5)
    if not vals:
        return None
    if len(vals) > 2:
        return None
    return sum(vals) / len(vals)


def firepower_multiplier(resolved, local):
    """Extract a single unconditional, locally-defined FirepowerMultiplier.

    Inherited template traits like FirepowerMultiplier@GlobalBuffs are NOT
    captured, because they are not the per-actor fine-tuning knob.  Only
    values written directly on the actor block are balance-relevant.
    Conditional FirepowerMultiplier traits (RequiresCondition) are also
    ignored for pricing because they are situational buffs/debuffs.
    The actor-specific FirepowerMultiplier@<actor> or unqualified
    FirepowerMultiplier is preferred over template-override entries so the
    fine-tuning knob is captured.
    """
    if local is None:
        return None
    actor = local.key
    candidates = []
    for c in local.children:
        if c.key == "FirepowerMultiplier" or c.key.startswith("FirepowerMultiplier@"):
            if c.get("RequiresCondition"):
                continue
            mod = c.get("Modifier")
            if mod is None:
                continue
            candidates.append(c)
    if not candidates:
        return None
    preferred = None
    for c in candidates:
        if c.key == "FirepowerMultiplier":
            preferred = c
            break
        suffix = c.key.split("@", 1)[1] if "@" in c.key else ""
        if suffix.lower() == actor.lower():
            preferred = c
            break
    if preferred is None:
        preferred = candidates[0]
    mod = preferred.get("Modifier")
    src = f"{rel(preferred.file)}#{preferred.key}.Modifier"
    # Modifier is an integer percentage in YAML (e.g. 89 = 89%).
    s = str(mod).strip()
    if "." in s or "e" in s.lower():
        # Legacy decimal form; treat the literal as the intended fraction.
        v = float(s)
    else:
        v = float(s) / 100.0
    return {"v": v, "src": src, "trait": preferred.key}


def warheads(rs, wname: str, _seen=None) -> list[str]:
    """Resolved ^-prefixed warhead templates (^Warhead_*) in the weapon's
    inheritance chain (deduped, document order). Other ^-parents are
    recursed but not emitted, so the list records only the actual
    new-split warhead carriers."""
    _seen = _seen if _seen is not None else set()
    if wname.lower() in _seen:
        return []
    _seen.add(wname.lower())
    node = rs.weapon(wname)
    if node is None:
        return []
    out: list[str] = []
    for c in node.children:
        if c.key == "Inherits" or c.key.startswith("Inherits@"):
            parent = c.value
            if parent.startswith("^"):
                if parent.startswith("^Warhead_") and parent not in out:
                    out.append(parent)
                # recurse into every ^-parent to find nested ^Warhead_* carriers
                for t in warheads(rs, parent, _seen):
                    if t not in out:
                        out.append(t)
            else:  # a parent weapon — recurse to pull its ^-templates
                for t in warheads(rs, parent, _seen):
                    if t not in out:
                        out.append(t)
    return out


# ---------------------------------------------------------------------------
# DERIVED METRICS — computed here, but SPLIT OUT of the raw ledger (W3).
#
# BALANCE_PIPELINE.md §2: the ledger is RAW STATS ONLY. Model output living in it
# made the artifact ambiguous — correcting the scatter model once rewrote 4136
# ledger lines with `mods/` untouched, so a ledger diff no longer answered a single
# question. The split gives each tree exactly one meaning:
#
#   docs/balance/<faction>.json          diff => THE GAME changed (yaml was edited)
#   docs/balance/derived/<faction>.json  diff => THE MODEL changed (a tool was edited)
#
# They are produced by the same run off the same resolve, so they cannot fall out of
# step with each other; `DERIVED_KEY` is the temporary carrier that `split_derived`
# lifts out before the raw ledger is serialised.
# ---------------------------------------------------------------------------
DERIVED_KEY = "_derived"


def model_constants() -> dict:
    """The knobs every derived number depends on, written once to derived/_model.json.

    Committed so that a model change shows up as a small, readable diff at the top of
    the derived tree instead of only as thousands of shifted decimals underneath it.
    """
    return {
        "effective_damage": {"SWARM_W": effmod.SWARM_W, "LEAD": effmod.LEAD,
                             "TARGET_SPEED": effmod.TARGET_SPEED,
                             "MIN_SPREAD": effmod.MIN_SPREAD},
        "target_model": {"A_BLOB": tm.A_BLOB, "A_SELF": tm.A_SELF,
                         "BLOB_UPTIME": tm.BLOB_UPTIME,
                         "DENSITY": dict(tm.DENSITY),
                         "ENGAGEMENT": dict(tm.ENGAGEMENT),
                         "reference_hp": tm.reference_hp(),
                         "reference_hp_measured": tm.measured_reference_hp(),
                         "armor_census": tm.armor_census()},
    }


def derived_metrics(resolved, raw: dict) -> dict | None:
    """Model output for one weapon. Never written to the raw ledger.

    Carries TWO different metrics on purpose — EFFECTIVE_DAMAGE.md §1 warns they are
    not interchangeable, so they keep separate names rather than being averaged:

    * `effective_damage` / `footprint` / `reliability` / `sigma` — the area-integrated
      per-shot metric.
    * `k` / `effective_per_shot` / `effective_dps` — the W1 pricing coefficient. K is
      independent of the Damage magnitude, which is what makes pricing invertible:
      `Damage_required = target_dps x eff_reload / (burst x FP x K)`.

    `effective_dps` is the WEAPON's number. `FirepowerMultiplier` is an actor
    property, so it is deliberately NOT baked in here — the caller applies it.
    """
    out: dict = {}
    ed = effmod.effective_damage(resolved)
    if ed is not None:
        out["effective_damage"] = round(ed[0], 2)
        out["damage_total"] = ed[1]
        out["footprint"] = round(ed[2], 4)
        out["reliability"] = round(ed[3], 4)
        out["sigma"] = round(ed[4], 2)

    damage_total = ed[1] if ed is not None else 0.0
    res = we.analyse(resolved, damage_total or 1.0)
    if res is not None:
        # Weighted over the FLAT warheads only, whose shares sum to 1.0 — the same
        # quantity the family table's avgVersus column reports. Including the %-twins
        # would drag it toward their armor profile and make the two disagree.
        flat = [p for p in res["parts"] if p["kind"] != "pct"]
        shares = sum(p["share"] for p in flat) or 1.0
        out["k"] = round(res["k"], 4)
        out["k_context"] = round(res["k_context"], 4)
        out["avg_versus"] = round(
            sum(p["share"] * p["versus"] for p in flat) / shares, 4)
        # W5 factors, each its own column so a price move can be traced to ONE of
        # them rather than to an opaque blend.
        for name, value in res["factors"].items():
            out[f"factor_{name}"] = round(value, 4)
        out["overkill"] = round(res["overkill"], 4)
        out["effective_per_shot"] = round(res["k_context"] * damage_total, 2)

        burst = int(fnum(raw.get("burst")) or 1)
        reload_delay = fnum(raw.get("reloaddelay"))
        if reload_delay:
            eff = formula.eff_reload(reload_delay, burst, fnum(raw.get("burstdelays")))
            out["eff_reload"] = round(eff, 2)
            out["effective_dps"] = round(
                res["k_context"] * damage_total * burst / eff, 2)
    return out or None


def fnum(v):
    """First number in a raw ledger value ("15, 15" -> 15.0), else None."""
    if v is None:
        return None
    try:
        return float(str(v).split(",")[0].strip())
    except (TypeError, ValueError):
        return None


def split_derived(doc: dict) -> tuple[dict, dict]:
    """One built doc -> (raw ledger, derived sidecar), mirroring sections/armaments.

    The derived side repeats only the join keys (`slot`, `weapon`) so a row can be
    matched back to its raw counterpart without duplicating any raw stat.
    """
    sections: dict = {}
    for section, actors in doc["sections"].items():
        sec: dict = {}
        for actor, unit in actors.items():
            arms = []
            for arm in unit.get("armaments", []):
                metrics = arm.pop(DERIVED_KEY, None)
                if metrics:
                    arms.append({"slot": arm.get("slot"),
                                 "weapon": arm.get("weapon"), **metrics})
            if arms:
                sec[actor] = {"armaments": arms}
        if sec:
            sections[section] = sec
    derived = {"schema": doc["schema"], "ledger": doc["ledger"],
               "pack": doc["pack"], "sections": sections}
    return doc, derived


def weapon_entry(rs, wname: str) -> dict | None:
    resolved = rs.resolve_weapon(wname)
    if resolved is None:
        return None
    local = rs.weapon(wname)
    out = {"weapon": wname, "defined_in": rel(local.file) if local is not None else None}
    for field in ("ReloadDelay", "Burst", "BurstDelays", "Range", "MinRange"):
        v = resolved.get(field)
        if v is not None:
            out[field.lower()] = v
    damage_warheads = []
    for c in resolved.children:
        if c.key.startswith("Warhead@") and c.value in ("SpreadDamage", "HealthPercentageDamage", "AreaDamage", "AreaDamagePercentage", "TargetDamage"):
            d = c.get("Damage")
            if d is not None:
                record = {
                    "tag": c.key.split("@", 1)[1],
                    "type": c.value,
                    "damage": d,
                    "spread": c.get("Spread"),
                    "falloff": c.get("Falloff"),
                }
                # The UNIT of a percentage twin's Damage (100 = whole percent,
                # 1000 = per-mille). Recorded only when the node states it, so the
                # ledger diff stays empty for every weapon still on the default —
                # without it `distribute_damage` would write whole percent into a
                # per-mille node and silently deal a tenth of the damage.
                denominator = c.get("PercentageDenominator")
                if denominator is not None:
                    record["percentage_denominator"] = denominator
                damage_warheads.append(record)
    out["damage_warheads"] = damage_warheads
    if not damage_warheads:
        out["extraction_note"] = "no_damage_warheads"
    out[DERIVED_KEY] = derived_metrics(resolved, out)
    if local is not None:
        out["versus_templates"] = [c.value for c in local.children
                                   if c.key == "Inherits" or c.key.startswith("Inherits@")]
    out["warheads"] = warheads(rs, wname)
    # Priority: explicit WeaponClass field (rare - the lint strips it) -
    # sidecar-template lookup (weapon_classes.yaml is the source of truth).
    wc = resolved.get("WeaponClass")
    parsed = None
    if wc is not None:
        try:
            parsed = float(wc)
        except (ValueError, TypeError):
            parsed = None
    if parsed is not None:
        out["design_weapon_class"] = parsed
        out["weapon_class_source"] = "WeaponClass"
    else:
        vclass = weapon_class_from_types(out["warheads"])
        out["design_weapon_class"] = vclass
        if vclass is None:
            if not out["warheads"]:
                out["weapon_class_source"] = "none"
            else:
                out["weapon_class_source"] = (
                    "allowlist_mix" if any(a in wname for a in _MIX_ALLOWLIST) else "illegal_mix"
                )
        else:
            out["weapon_class_source"] = "template"
    return out


# Prerequisite tokens that no building ever provides → gate the unit off.
_DISABLING_PREREQS = {"disabled", "wip", "disable", "unavailable", "notbuildable"}


def _is_balance_buildable(buildable) -> bool:
    """True iff the actor can actually be built (maintainer law 2026-07-22):
    has a Buildable trait with a non-empty Queue and no disabling prerequisite
    (~disabled / ~wip / …). Legacy tokens (E1/E3 — Buildable but no Queue),
    spawn/veterancy variants (no Buildable), and ~disabled units all fail."""
    if buildable is None:
        return False
    if not buildable.get("Queue"):
        return False
    prereq = buildable.get("Prerequisites") or ""
    toks = {t.strip().lstrip("~").strip().lower() for t in prereq.split(",")}
    return not (toks & _DISABLING_PREREQS)


def extract_actor(rs, key: str, section: str) -> dict | None:
    resolved = rs.resolve(key)
    if resolved is None:
        return None
    buildable = child(resolved, "Buildable")
    valued = child(resolved, "Valued")
    if buildable is None and valued is None:
        return None  # not balance-relevant (husk fragments, decorations...)
    local = rs.actor(key)
    u: dict = {}
    tooltip = child(resolved, "Tooltip")
    if tooltip is not None and tooltip.get("Name"):
        u["name"] = tooltip.get("Name")
    for out_key, trait, field in (
            ("cost", "Valued", "Cost"),
            ("hp", "Health", "HP"),
            ("armor", "Armor", "Type"),
            ("speed", "Mobile", "Speed"),
            ("speed_air", "Aircraft", "Speed"),
            ("turn_speed", "Mobile", "TurnSpeed"),
            ("sight", "RevealsShroud", "Range"),
            ("build_limit", "Buildable", "BuildLimit"),
            ("build_duration", "Buildable", "BuildDuration"),
            ("self_heal_step", "ChangesHealth", "Step"),
            # --- THE SURVIVABILITY LAYERS (E1, 2026-08-16) ---------------------------- #
            # Maintainer: *"shielded units and armored units need to have a price! it is
            # like extra survivability ... Extra shields and extra armor platings just make
            # the units a lot more durable so they need to be included in the balance
            # formula."*  Read as RAW fields here; `derived_actor_metrics` turns them into
            # effective HP. Nothing read them before, so 1592 shielded and 1233
            # EMP-pooled actors carried their whole extra layer for free.
            ("shield_flat", "Shielded", "MaxStrength"),
            ("shield_pct", "Shielded", "MaxPercentageStrength"),
            ("integrity_flat", "Integrity", "MaxStrength"),
            ("integrity_pct", "Integrity", "MaxPercentageStrength"),
    ):
        s = stat(resolved, local, trait, field)
        if s is not None:
            u[out_key] = s
    if buildable is not None:
        prereq = buildable.get("Prerequisites")
        if prereq:
            u["prerequisites"] = [p.strip() for p in prereq.split(",") if p.strip()]
        queue = buildable.get("Queue")
        if queue:
            u["queue"] = [q.strip() for q in queue.split(",") if q.strip()]
    # Buildability (maintainer law 2026-07-22): a unit is balance-relevant ONLY
    # if it can be built in some way. NON-buildable (no Buildable trait, no Queue,
    # or a disabling prereq like ~disabled/~wip) → excluded from balancing AND all
    # audits. Its cost is only an XP-on-kill value; its stats don't matter.
    u["buildable"] = _is_balance_buildable(buildable)
    arms = []
    for c in resolved.children:
        if c.key == "Armament" or c.key.startswith("Armament@"):
            wname = c.get("Weapon")
            if not wname:
                continue
            entry = {"slot": c.key}
            w = weapon_entry(rs, wname)
            if w is None:
                entry["weapon"] = wname
                entry["unresolved"] = True
            else:
                entry.update(w)
            req = c.get("RequiresCondition")
            if req:
                entry["requires"] = req
            if c.get("Name"):
                entry["armament_name"] = c.get("Name")
            arm_name = c.get("Name") or ""
            entry["pricing"] = not ("garrison" in arm_name.lower()) and not (
                entry.get("extraction_note") == "no_damage_warheads")
            arms.append(entry)
    if arms:
        u["armaments"] = arms
    fp = firepower_multiplier(resolved, local)
    if fp:
        u["firepower_multiplier"] = fp
    chg = charge_up(resolved, local)
    if chg:
        u["charge_up"] = chg
    sub = actor_subtype(rs, local, section)
    # design judgment inputs — seeded by Phase 3 from the legacy sheet;
    # null until then (they never exist in yaml).  Subtype is auto-derived
    # from the nearest ^...Template the actor inherits from defaults.yaml.
    u["design"] = {"unit_class": None, "special": None, "tech_tier": None,
                   "class_anchor": None, "subtype": sub}
    # Special forces always use the class weapon-class multiplier of 1.0,
    # regardless of whether the member uses SA+CG+Laser/Railgun or a
    # re-themed light+medium+heavy triad.
    if sub == "SpecialForcesInfantry":
        for arm in arms:
            arm["design_weapon_class"] = 1.0
    return u


def pack_rosters() -> dict[str, dict]:
    """{ledger-name: {"pack": relpath, "sections": {section: [actor,...]}}}"""
    rosters: dict[str, dict] = {}
    for pack_dir in sorted(PACKS.glob("*/*/")):
        theme, leaf = pack_dir.parts[-2], pack_dir.parts[-1]
        ydir = pack_dir / "yaml"
        if not ydir.is_dir():
            continue
        if leaf in SHARED_LEAVES:
            ledger = f"shared_{theme.lower()}" if leaf == "Shared" else None
            if ledger is None:
                continue  # Core: meta factions only, no balance rosters
        else:
            ledger = f"{theme.lower()}_{leaf.lower()}"
        entry = rosters.setdefault(
            ledger, {"pack": rel(pack_dir).rstrip("/"), "sections": {}})
        for section in SECTION_FILES:
            f = ydir / f"{section}.yaml"
            if not f.is_file():
                continue
            keys = [k for k in top_keys(f) if not k.startswith("^")]
            if keys:
                entry["sections"].setdefault(section, []).extend(keys)
    return rosters


def load_existing_design(name: str) -> tuple[dict[str, dict], dict[str, dict]]:
    """(unit design, per-armament weapon-class judgments) from the
    committed ledger — design.* fields are judgment data (seeded from
    the legacy sheet / maintainer), NOT yaml facts, so re-extraction
    must never wipe them.  A stale "Unclassified" subtype is not kept,
    because extract_actor now derives a real subtype from the yaml."""
    p = OUT / f"{name}.json"
    if not p.exists():
        return {}, {}
    out, wc = {}, {}
    try:
        doc = json.loads(p.read_text(encoding="utf-8"))
        for sec in doc.get("sections", {}).values():
            for actor, u in sec.items():
                d = u.get("design")
                if d:
                    # Subtypes are always re-derived from yaml; keep only
                    # judgment fields that yaml can never provide.
                    kept = {k: v for k, v in d.items()
                            if v is not None and k != "subtype"}
                    if kept:
                        out[actor] = kept
                slots = {a["slot"]: a["design_weapon_class"]
                         for a in u.get("armaments", [])
                         if a.get("design_weapon_class") is not None}
                if slots:
                    wc[actor] = slots
    except (json.JSONDecodeError, OSError):
        pass
    return out, wc


def build_ledgers(model: Model, only: str | None = None) -> dict[str, dict]:
    """RAW ledgers only — what `audit_balance_drift` diffs against `docs/balance/`.

    Kept as the narrow entry point so the drift audit cannot accidentally start
    comparing model output: a raw diff must always mean "the game changed".
    """
    return build_both(model, only)[0]


def build_both(model: Model, only: str | None = None) -> tuple[dict[str, dict],
                                                               dict[str, dict]]:
    """(raw ledgers, derived sidecars) from a single pass — they cannot desync."""
    rs = model.rs
    tm.use_ruleset(rs)          # reuse the built tree for the armor census (~8s saved)
    we.use_ruleset(rs)          # ... and for the median-weapon-range yardstick
    ledgers: dict[str, dict] = {}
    sidecars: dict[str, dict] = {}
    for ledger, info in sorted(pack_rosters().items()):
        if only and only not in ledger:
            continue
        keep_design, keep_wc = load_existing_design(ledger)
        sections: dict = {}
        for section, actors in sorted(info["sections"].items()):
            sec: dict = {}
            for a in sorted(set(actors)):
                u = extract_actor(rs, a, section)
                if u is not None:
                    if a in keep_design:
                        u["design"].update(keep_design[a])
                    # Special-forces weapon-class is always derived from the
                    # class rule (1.0); do not let stale ledger judgments
                    # overwrite it.
                    if u["design"].get("subtype") != "SpecialForcesInfantry":
                        for arm in u.get("armaments", []):
                            # Fresh auto-derivation (WeaponClass field -> sidecar) is
                            # self-correcting and AUTHORITATIVE. Only fall back to a
                            # PRESERVED ledger value when nothing could be derived, so a stale WC
                            # can never clobber the correct current one (2026-08-01 fix).
                            # A source of "illegal_mix" or "allowlist_mix" means the weapon violates
                            # the 2-warhead cap (or is a deliberate exception); do NOT restore a stale
                            # value.  "none" means the weapon has no class templates (dummy/utility).
                            if (arm.get("design_weapon_class") is None and
                                    arm.get("weapon_class_source") not in ("illegal_mix", "allowlist_mix", "none")):
                                v = keep_wc.get(a, {}).get(arm["slot"])
                                if v is not None:
                                    arm["design_weapon_class"] = v
                    sec[a] = u
            if sec:
                sections[section] = sec
        if sections:
            raw, derived = split_derived({"schema": 2, "ledger": ledger,
                                          "pack": info["pack"], "sections": sections})
            ledgers[ledger] = raw
            sidecars[ledger] = derived
    return ledgers, sidecars


def serialize(doc: dict) -> str:
    return json.dumps(doc, sort_keys=True, indent=1, ensure_ascii=False) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true",
                    help="diff against the committed ledger; exit 1 on drift")
    ap.add_argument("--faction", help="ledger-name substring filter")
    ap.add_argument("--check-weapon-classes", action="store_true",
                    help="fail if any weapon references a class template missing "
                         "from docs/balance/weapon_classes.yaml (the sidecar)")
    args = ap.parse_args()

    ledgers, sidecars = build_both(Model(), args.faction)

    if args.check_weapon_classes:
        # Extraction has populated _UNMAPPED_WEAPON_TEMPLATES with every class
        # template that was NOT found in the authoritative sidecar. A non-empty
        # set means a weapon would be priced from a guessed value -> hard fail,
        # so the LaserWeapon=1.0 class of bug can never recur silently.
        missing = sorted(_UNMAPPED_WEAPON_TEMPLATES)
        if missing:
            print("WEAPON-CLASS CHECK FAILED -- templates missing from "
                  f"{rel(WEAPON_CLASS_SIDECAR_PATH)}:")
            for name in missing:
                print(f"  ^{name}")
            print(f"add each to the sidecar with its Light/Medium/Heavy class "
                  f"(0.75/1.0/1.25). {len(missing)} unmapped.")
            return 1
        print(f"weapon-class check OK: every class template is in "
              f"{rel(WEAPON_CLASS_SIDECAR_PATH)} ({len(_WEAPON_CLASS_SIDECAR)} entries)")
        return 0

    # Both trees are checked, but they are reported apart because they answer
    # different questions: raw drift = the GAME changed (someone hand-edited yaml),
    # model drift = a TOOL changed (re-run the extractor and commit the sidecar).
    targets = [("raw", OUT, ledgers), ("model", DERIVED_OUT, sidecars)]

    if args.check:
        drift = 0
        for label, root, docs in targets:
            for name, doc in docs.items():
                p = root / f"{name}.json"
                want = serialize(doc)
                have = p.read_text(encoding="utf-8") if p.exists() else ""
                if want != have:
                    print(f"DRIFT ({label}): {name} "
                          f"({'missing' if not have else 'stale'})")
                    drift += 1
        want = serialize(model_constants())
        mp = DERIVED_OUT / "_model.json"
        if want != (mp.read_text(encoding="utf-8") if mp.exists() else ""):
            print("DRIFT (model): _model.json — the model constants changed")
            drift += 1
        print(f"balance check: {len(ledgers)} ledgers, {drift} drifted")
        return 1 if drift else 0

    total = 0
    for label, root, docs in targets:
        root.mkdir(parents=True, exist_ok=True)
        for name, doc in docs.items():
            (root / f"{name}.json").write_text(serialize(doc),
                                               encoding="utf-8", newline="\n")
            if label == "raw":
                n = sum(len(s) for s in doc["sections"].values())
                total += n
                print(f"  {name}.json: {n} actors")
    (DERIVED_OUT / "_model.json").write_text(serialize(model_constants()),
                                             encoding="utf-8", newline="\n")
    print(f"wrote {len(ledgers)} ledgers, {total} actors -> {rel(OUT)}")
    print(f"wrote {len(sidecars)} derived sidecars -> {rel(DERIVED_OUT)}")

    # `_model.json` above is GLOBAL — its armor census and weights are measured
    # across the whole roster — but a filtered run only rewrites the sidecars it
    # was asked for. So a --faction run can move the model for everyone while
    # leaving 31 factions' derived numbers computed against the old one, and
    # nothing catches it: audit_balance_drift compares raw yaml to the RAW ledger
    # and never looks at derived. That is exactly how avg_versus/k/effective_dps
    # went quietly stale across 30 files in 2026-08-15.
    if args.faction:
        print("\n⚠ FILTERED RUN — `_model.json` is global and has been rewritten, but only "
              f"the sidecar(s) matching `{args.faction}` were regenerated.\n"
              "  Every OTHER faction's derived numbers (avg_versus, k, effective_dps) may "
              "now be stale.\n"
              "  Re-run without --faction before trusting or committing derived data.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
