"""Alias zero-use lint for trait unification (SPEC_2026-10-05_trait_unification §6/§7).

`trait_aliases.json` is the migration registry. When an entry flips to ``active`` (the
MigrateUnifiedTraits update rule has rewritten the corpus) or ``retired`` (the old name is gone),
NO use of the old YAML name may survive anywhere: mounted rules/weapons files, dormant (unmounted)
yaml, or map ``Rules:``/``Weapons:`` overrides — loose map dirs and packed ``.oramap`` archives
alike. This audit scans every yaml under mods/cameo and fails on a survivor:

  * ``OldName:`` / ``OldName@i:`` / ``-OldName@i:`` trait nodes at any depth >= 1
    (top-level keys are actor/weapon IDs, never traits — see spec §6 "do not globally replace"),
  * ``Projectile: OldName`` and ``Warhead@x: OldName`` VALUES (key untouched),
  * pending entries are counted for ``--report`` but never fail.

  python tools/audit/audit_trait_aliases.py            # gate
  python tools/audit/audit_trait_aliases.py --report   # per-alias use counts, all states
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import sys
import zipfile

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(REPO / "tools" / "audit"))
import miniyaml  # noqa: E402

ALIASES = REPO / "tools" / "audit" / "trait_aliases.json"
MANIFEST = REPO / "tools" / "audit" / "merged_bot_modules.json"
MOD_ROOT = REPO / "mods" / "cameo"

VALID_STATES = {"pending", "active", "retired"}
VALID_KINDS = {"trait", "projectile", "warhead"}


def key_base(key: str) -> str:
    """Node key minus any '-' removal prefix and '@instance' suffix."""
    k = key.lstrip("-").strip()
    return k.split("@", 1)[0]


def scan_nodes(nodes, old_names: set[str], out: list):
    """Walk parsed MiniYAML; record (file, line, old, form) for every old-name use."""
    def walk(node_list, depth):
        for n in node_list:
            base = key_base(n.key)
            if base in old_names:
                if depth >= 1:
                    out.append((n.file, n.line, base, "trait-key"))
            elif depth >= 1 and base == "Projectile" and (n.value or "").strip() in old_names:
                out.append((n.file, n.line, (n.value or "").strip(), "projectile-value"))
            elif depth >= 1 and base == "Warhead" and (n.value or "").strip() in old_names:
                out.append((n.file, n.line, (n.value or "").strip(), "warhead-value"))
            walk(n.children, depth + 1)
    walk(nodes, 0)


def mounted_paths() -> set[pathlib.Path]:
    """Files the mod actually loads (rules/weapons/sequences/include sources)."""
    man = miniyaml.load_manifest(REPO)
    return {p.resolve() for p in (*man.rules, *man.weapons, *man.sequences, *man.sources)}


def iter_corpus():
    """Yield (source_label, nodes_or_None, error) for every yaml the game or a map can read.

    source_label = "<mounted|dormant>:<path>" for loose files, "map:<pack>!<entry>" for archives.
    """
    mounted = mounted_paths()
    for path in sorted(MOD_ROOT.rglob("*.yaml")) + sorted(MOD_ROOT.rglob("*.yml")):
        if any(part in ("bin", "obj") for part in path.parts):
            continue
        label = "mounted" if path.resolve() in mounted else "dormant"
        try:
            yield f"{label}:{path.relative_to(REPO).as_posix()}", miniyaml.load(path), None
        except Exception as e:
            yield f"{label}:{path.relative_to(REPO).as_posix()}", None, str(e)
    for pack in sorted(MOD_ROOT.rglob("*.oramap")):
        try:
            zf = zipfile.ZipFile(pack)
        except zipfile.BadZipFile as e:
            yield f"map:{pack.relative_to(REPO).as_posix()}", None, f"unreadable package: {e}"
            continue
        with zf:
            for entry in sorted(zf.namelist()):
                if not entry.lower().endswith((".yaml", ".yml")):
                    continue
                label = f"map:{pack.relative_to(REPO).as_posix()}!{entry}"
                try:
                    text = zf.read(entry).decode("utf-8-sig", errors="replace")
                    yield label, miniyaml.load_text(text, label), None
                except Exception as e:
                    yield label, None, str(e)


def validate_schema(doc: dict, families: dict) -> list[str]:
    failures = []
    if doc.get("version") != 1:
        failures.append("trait_aliases.json: unsupported version")
    seen = set()
    for a in doc.get("aliases", []):
        old = a.get("old")
        for req in ("old", "kind", "state"):
            if req not in a:
                failures.append(f"{old}: missing {req}")
        if old in seen:
            failures.append(f"{old}: duplicate alias entry")
        seen.add(old)
        if a.get("state") not in VALID_STATES:
            failures.append(f"{old}: unknown state {a.get('state')!r}")
        if a.get("kind") not in VALID_KINDS:
            failures.append(f"{old}: unknown kind {a.get('kind')!r}")
        if a.get("state") in ("active", "retired") and not a.get("new"):
            failures.append(f"{old}: state={a.get('state')} requires a non-null `new`")
        fam = a.get("family")
        if fam is not None and fam not in families:
            failures.append(f"{old}: family {fam!r} is not in merged_bot_modules.json trait_families")
    return failures


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--report", action="store_true", help="print per-alias use counts for every state")
    ap.add_argument("--write", action="store_true", help=argparse.SUPPRESS)  # accepted, no-op
    ap.add_argument("--force-latest", action="store_true", help=argparse.SUPPRESS)  # run_all.sh plumbing
    args, _unknown = ap.parse_known_args()

    doc = json.loads(ALIASES.read_text(encoding="utf-8"))
    families = json.loads(MANIFEST.read_text(encoding="utf-8")).get("trait_families") or {}
    aliases = doc.get("aliases", [])
    old_names = {a["old"] for a in aliases}

    failures = validate_schema(doc, families)
    state_of = {a["old"]: a.get("state") for a in aliases}

    # hits: (source_label, line, old, form); miniyaml already stamps file/line on each node
    hits = []
    scanned = 0
    for label, nodes, err in iter_corpus():
        scanned += 1
        if nodes is None:
            failures.append(f"{label}: unparsable yaml ({err})")
            continue
        base = len(hits)
        scan_nodes(nodes, old_names, hits)
        for i in range(base, len(hits)):
            _f, line, old, form = hits[i]
            hits[i] = (label, line, old, form)

    blocked = [h for h in hits if state_of.get(h[2]) in ("active", "retired")]
    pending_hits = [h for h in hits if state_of.get(h[2]) == "pending"]

    if args.report:
        print("alias\tstate\tkind\tuses\ttarget")
        counts = collections.Counter(h[2] for h in hits)
        for a in sorted(aliases, key=lambda x: (x["state"], x["old"])):
            print(f"{a['old']}\t{a['state']}\t{a['kind']}\t{counts.get(a['old'], 0)}\t{a.get('new') or a['family']}")
        print()

    for label, line, old, form in hits:
        if state_of.get(old) in ("active", "retired"):
            print(f"FAIL: {old} ({form}) at {label}:{line}")

    for f in failures:
        print(f"FAIL: {f}")

    if blocked or failures:
        print(f"{len(blocked)} surviving use(s) of active/retired alias(es), {len(failures)} schema failure(s)")
        return 1
    n_act = sum(1 for a in aliases if a['state'] in ('active', 'retired'))
    print(f"PASS: {n_act} active/retired aliases have zero surviving uses "
          f"({len(pending_hits)} pending-state use(s) tracked across {scanned} yaml source(s), "
          f"{len(old_names)} aliases registered)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
