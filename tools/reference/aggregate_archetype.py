#!/usr/bin/env python3
"""W13 step 2 — aggregate ONE weapon concept across every reference mod.

The maintainer's method (2026-08-15): *"research all the obelisk of light lasers
from all the mods and then see how that big laser versus values behaves against
all armor types … then try to extrapolate the values to all the cameo armor
types."* One concept at a time, two tables each:

  **Table A** — what each mod actually ships, in its OWN armor vocabulary.
  **Table B** — the same profiles mapped onto Cameo's 16 armor types.

    python tools/reference/aggregate_archetype.py obelisk
    python tools/reference/aggregate_archetype.py --list
    python tools/reference/aggregate_archetype.py --all --write

**How a warhead is identified matters more than the arithmetic.** For the INI
mods this TRACES the real chain — `[OBELISK] Primary=` -> `[weapon] Warhead=` ->
`[warhead] Verses=` — so what lands in the table is the weapon that building
actually fires, not a warhead whose name happened to contain "laser". A name
match would silently average an Obelisk with an infantry laser rifle, which is
precisely the flattening W13 exists to undo. OpenRA sources have no such flat
index here, so they use a curated warhead list, and every row says which method
produced it.

⚠ **Table B is EXTRAPOLATION, not measurement**, and it is labelled as such per
cell. Five of Cameo's 16 armors have no source equivalent — Heroic, Scout,
Superheavy and the four aircraft classes — because the Westwood engines carry
11 armors and share ONE of them between aircraft and ground vehicles.
Maintainer ruling 2026-08-15: *"some mods use light or heavy armor for aircraft
and you can translate that to our fighter, bomber, helicopter, spaceship"*, so
the air classes are read off the vehicle ladder by weight. The ladder ends
(Heroic / Scout / Superheavy) continue the ladder's own local slope.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "reference"))
import extract_versus as ev  # noqa: E402

OUT = ROOT / "docs" / "reference" / "archetype_tables.md"

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# --------------------------------------------------------------------------- #
# Cameo's 16 armor types, in the ladder order gen_weapon_template.py uses.
# --------------------------------------------------------------------------- #
CAMEO_LADDERS = {
    "INF": ["None", "Flak", "Plate", "Heroic"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}
CAMEO16 = [a for lad in CAMEO_LADDERS.values() for a in lad]

# Source armor -> Cameo armor, where a real equivalent exists.
DIRECT = {
    "none": "None", "flak": "Flak", "plate": "Plate",
    "light": "Light", "medium": "Medium", "heavy": "Heavy",
    "wood": "Wood", "steel": "Steel", "concrete": "Concrete",
    # `drone` is RA2's terror-drone/light-walker armor — the closest thing the
    # source engines have to Cameo's Scout class, so it seeds Scout when present.
    "drone": "Scout",
}
# Aircraft read off the VEHICLE ladder by weight (maintainer ruling above).
AIR_FROM = {"Fighter": "Light", "Helicopter": "Light",
            "Bomber": "Medium", "Spaceship": "Heavy"}

# ⚠ THE NORMALISATION LAW (maintainer, 2026-08-15):
#   *"everything needs to be normalized to 100% for the maximum versus value for
#    each game and each warhead from each game"*
#
# Every profile is rescaled so its OWN maximum is 100. This is per-WARHEAD, not
# per-source, and it is what makes cross-mod comparison mean anything:
#
#   * it removes engine scale differences outright — DTA runs on the Tiberian Sun
#     engine whose multipliers are x10 (measured median Versus 690 against 55-100
#     elsewhere), and a hand-maintained per-source divisor only ever fixed the
#     cases somebody had already noticed;
#   * it removes each mod's POWER LEVEL, which is not what we are borrowing. We
#     want the SHAPE — "twice as good against infantry as against tanks" — and a
#     mod that writes 200/100 and one that writes 100/50 are the same design;
#   * it makes the aggregate meaningful: averaging raw values weights whichever
#     mod happens to use the biggest numbers.
#
# What it deliberately discards is absolute lethality. That belongs to Damage and
# the level slope, not to the armor profile.
NORMALISE_MAX = 100.0

# Geometric mean cannot see a zero (any zero makes the whole product zero), and a
# hard 0 in a source means "immune". Clamping to 1 keeps the data point at
# "essentially immune" instead of deleting the row from the statistic.
GMEAN_FLOOR = 1.0

# --------------------------------------------------------------------------- #
# The concepts. `ini` entries are ACTOR names traced through the rules; `openra`
# entries are warhead names, because those trees have no flat actor index here.
# --------------------------------------------------------------------------- #
ARCHETYPES = {
    # Actor ids below are VERIFIED present in the rules, not guessed: `OBLI` (DTA),
    # `TSOBEL`/`TSLASR`/`ROBOTOBELISK` (CnC Reloaded's TS content), and — the one that
    # would defeat any name search — the RA2 **Prism Tower is `[ATESLA]`**, "Allied
    # Tesla". Tracing the actor is what finds it; grepping for "prism" finds the
    # Mayan temple prop instead.
    "obelisk": {
        "what": "Obelisk of Light — the big defensive laser",
        "ini_actors": ["OBLI", "TSOBEL", "TSLASR", "ROBOTOBELISK", "NAOBEL"],
        "openra_warheads": ["Laser", "LaserTur", "IonBeamMini"],
        "cameo_family": "^Warhead_Laser_Heavy",
    },
    "tesla_coil": {
        "what": "Tesla Coil — the big defensive electric weapon",
        "ini_actors": ["TESLA", "NATCOIL", "RATSLA"],
        "openra_warheads": ["TeslaZap", "PortaTesla", "CoilBolt", "PostBolt",
                            "^TeslaWeapon"],
        "cameo_family": "^Warhead_Tesla_Heavy",
    },
    "prism_tower": {
        "what": "Prism Tower — the RA2-lineage counterpart of the Obelisk",
        "ini_actors": ["ATESLA"],
        "openra_warheads": ["PrismShot", "PrismWarhead"],
        "cameo_family": "^Warhead_Prism_Heavy",
    },
    "he": {
        "what": "Generic HE — the canonical anti-light/anti-infantry profile",
        "ini_warheads": ["HE"],
        "openra_warheads": ["^Cannon", "ArtilleryShell"],
        "cameo_family": "^Warhead_CannonHE_Medium",
    },
    "ap": {
        "what": "Generic AP — the canonical anti-heavy/armour-piercing profile",
        "ini_warheads": ["AP"],
        "openra_warheads": ["^ArmorPierceDamage", "sabot"],
        "cameo_family": "^Warhead_CannonAP_Medium",
    },
}

INI_SECTION = re.compile(r"^\s*\[([^\]]+)\]")
INI_KEY = re.compile(r"^\s*([A-Za-z][A-Za-z0-9_.]*)\s*=\s*(.+?)\s*(?:;.*)?$")


def read_ini(path: pathlib.Path) -> dict[str, dict[str, str]]:
    """Flat {section: {key: value}}; comment lines dropped like extract_versus."""
    out: dict[str, dict[str, str]] = {}
    section = None
    for line in path.read_text(encoding="utf-8", errors="replace").splitlines():
        if line.lstrip().startswith(";"):
            continue
        head = INI_SECTION.match(line)
        if head:
            section = head.group(1)
            out.setdefault(section, {})
            continue
        kv = INI_KEY.match(line)
        if kv and section is not None:
            out[section][kv.group(1).lower()] = kv.group(2)
    return out


def trace_ini(ini: dict, actor: str) -> list[tuple[str, str]]:
    """[(weapon, warhead)] the actor actually fires — Primary and Secondary."""
    node = ini.get(actor)
    if node is None:
        return []
    found = []
    for slot in ("primary", "secondary", "elitesecondary", "eliteprimary"):
        weapon = node.get(slot)
        if not weapon:
            continue
        wnode = ini.get(weapon.strip())
        if not wnode:
            continue
        warhead = (wnode.get("warhead") or "").strip()
        if warhead:
            found.append((weapon.strip(), warhead))
    return found


def collect(concept: str) -> list[dict]:
    """One row per (source, warhead) that this concept resolves to."""
    spec = ARCHETYPES[concept]
    corpus = json.loads((ROOT / "docs/reference/versus_raw.json")
                        .read_text(encoding="utf-8"))
    rows = []
    for sid, lineage, kind, path, engine in ev.SOURCES:
        entry = corpus["sources"].get(sid)
        if not entry:
            continue
        by_name = {str(r["warhead"]): r for r in entry["rows"] if "versus" in r}

        wanted: list[tuple[str, str]] = []          # (warhead, how)
        if kind == "ini" and path.exists():
            ini = read_ini(path)
            for actor in spec.get("ini_actors", []):
                for weapon, warhead in trace_ini(ini, actor):
                    wanted.append((warhead, f"traced [{actor}]->{weapon}"))
            for name in spec.get("ini_warheads", []):
                if name in by_name:
                    wanted.append((name, "named warhead"))
        else:
            for name in spec.get("openra_warheads", []):
                if name in by_name:
                    wanted.append((name, "curated name"))

        seen = set()
        for warhead, how in wanted:
            if warhead in seen or warhead not in by_name:
                continue
            seen.add(warhead)
            raw = {k: float(v) for k, v in by_name[warhead]["versus"].items()}
            rows.append({"source": sid, "lineage": lineage, "warhead": warhead,
                         "how": how, "raw": raw, "versus": normalise(raw)})
    return rows


def normalise(versus: dict[str, float]) -> dict[str, float]:
    """Rescale a profile so its own maximum is `NORMALISE_MAX` (the law above)."""
    peak = max(versus.values(), default=0.0)
    if peak <= 0:
        return dict(versus)
    factor = NORMALISE_MAX / peak
    return {k: v * factor for k, v in versus.items()}


# --------------------------------------------------------------------------- #
# Extrapolation to Cameo's 16
# --------------------------------------------------------------------------- #
def _extend(known: list[float], beyond_top: bool) -> float | None:
    """Continue a ladder by its own last step. Clamped at 0 — a negative
    multiplier is meaningless, and W13 rule 8 forbids a hard zero anyway.

    ⚠ **Needs TWO known values.** An earlier version returned the lone value
    unchanged when only one rung was defined, which quietly manufactured data:
    a source defining only `none` produced a `Heroic` equal to it, and a source
    defining only `heavy` produced a `Scout` equal to it. Those invented cells
    then entered the medians — Tesla's `Heroic` came out at 100 against a real
    infantry ladder of 100/55/50, i.e. the extrapolation contradicted the very
    rows it was built from. One rung is not a slope; return None and let the
    column report a smaller n.
    """
    vals = [v for v in known if v is not None]
    if len(vals) < 2:
        return None
    step = vals[-1] - vals[-2] if beyond_top else vals[0] - vals[1]
    base = vals[-1] if beyond_top else vals[0]
    return max(0.0, base + step)


def to_cameo(versus: dict) -> tuple[dict[str, float], dict[str, str]]:
    """(values per Cameo armor, provenance per Cameo armor)."""
    src = {k.lower(): float(v) for k, v in versus.items()}
    out: dict[str, float] = {}
    how: dict[str, str] = {}

    for skey, ckey in DIRECT.items():
        if skey in src:
            out[ckey] = src[skey]
            how[ckey] = "direct"

    # Ladder ends: continue the ladder's own slope.
    inf = [out.get(a) for a in ("None", "Flak", "Plate")]
    if any(v is not None for v in inf):
        v = _extend([x for x in inf if x is not None], beyond_top=True)
        if v is not None and "Heroic" not in out:
            out["Heroic"], how["Heroic"] = v, "extrapolated"

    veh_up = [out.get(a) for a in ("Light", "Medium", "Heavy")]
    if any(v is not None for v in veh_up):
        v = _extend([x for x in veh_up if x is not None], beyond_top=True)
        if v is not None and "Superheavy" not in out:
            out["Superheavy"], how["Superheavy"] = v, "extrapolated"
    if "Scout" not in out and any(v is not None for v in veh_up):
        v = _extend([x for x in veh_up if x is not None], beyond_top=False)
        if v is not None:
            out["Scout"], how["Scout"] = v, "extrapolated"

    for air, from_armor in AIR_FROM.items():
        if from_armor in out:
            out[air] = out[from_armor]
            how[air] = f"from {from_armor}"
    return out, how


def flag_scale_outliers(rows: list[dict]) -> None:
    """Mark rows that look like a different scale from their peers.

    `SOURCE_SCALE` fixes the two whole-source cases (DTA). What is left is
    per-ROW: a handful of OpenRA `^template` blocks carry values ten times their
    neighbours'. They are NOT dropped — a row discarded on suspicion is data
    lost silently, which is worse than a row shown with a warning — and the
    median is robust enough to survive a few. But they must be visible, because
    a reader comparing "1000" to "100" down a column will otherwise conclude the
    weapon is ten times stronger in that mod.
    """
    maxima = [max(r["versus"].values()) for r in rows if r["versus"]]
    if len(maxima) < 4:
        return
    typical = statistics.median(maxima)
    for r in rows:
        if r["versus"] and max(r["versus"].values()) > 4 * typical:
            r["how"] += " ⚠scale?"


def buckets_of(rows: list[dict]) -> dict[str, list[float]]:
    out: dict[str, list[float]] = {a: [] for a in CAMEO16}
    for r in rows:
        vals, _ = to_cameo(r["versus"])
        for armor, value in vals.items():
            out[armor].append(value)
    return {a: v for a, v in out.items() if v}


def aggregate(rows: list[dict], how: str) -> dict[str, float]:
    """One profile from many, three ways.

    * `median` — W13 rule 4's choice. Robust: one mod with an extreme profile
      cannot move it, so it answers "what does a typical mod ship?".
    * `mean` — the arithmetic average. Every source pulls proportionally to how
      far out it sits, so a single outlier drags the whole cell.
    * `gmean` — the geometric mean. The natural average for MULTIPLIERS, which
      is exactly what a Versus value is: averaging x2 and x0.5 arithmetically
      gives 1.25 (a net buff out of nowhere) where the geometric mean gives 1.0.
      It also sits below the arithmetic mean whenever the spread is wide, so the
      gap between the two columns is itself a readout of disagreement.
    """
    picked: dict[str, float] = {}
    for armor, values in buckets_of(rows).items():
        if how == "median":
            picked[armor] = statistics.median(values)
        elif how == "mean":
            picked[armor] = statistics.fmean(values)
        elif how == "gmean":
            picked[armor] = statistics.geometric_mean(
                [max(GMEAN_FLOOR, v) for v in values])
        else:
            raise ValueError(how)
    return {a: round(v, 1) for a, v in picked.items()}


# Keys that are not armor types — Cameo carries a few pseudo-armors on the same
# node (the shield layer, HAZMAT gating, Tesla's REFLECTOR). They must not enter
# a profile comparison or they distort the normalisation peak.
NON_ARMOR = {"shield", "hazmat", "reflector"}


def cameo_profile(family: str) -> dict[str, float] | None:
    """Cameo's CURRENT profile for a `^Warhead_*` family, normalised the same way.

    Read from the resolved ruleset rather than the file, so inheritance is
    applied — the number compared is the one the game uses.
    """
    try:
        sys.path.insert(0, str(ROOT / "tools" / "audit"))
        from cameo_model import Model
    except ImportError:
        return None
    node = Model().rs.weapons.get(family)
    if node is None:
        return None
    for child in node.children:
        # the MAIN warhead only — not the _Percentage or _ExtraDamage twins
        if not child.key.startswith("Warhead@") or child.key.endswith(
                ("_Percentage", "_ExtraDamage", "_FriendlyFire")):
            continue
        for grand in child.children:
            if grand.key != "Versus":
                continue
            vals = {}
            for leaf in grand.children:
                if leaf.key.lower() in NON_ARMOR:
                    continue
                try:
                    vals[leaf.key] = float(leaf.value)
                except (TypeError, ValueError):
                    continue
            if vals:
                return {k: round(v, 1) for k, v in normalise(vals).items()}
    return None


# --------------------------------------------------------------------------- #
# Rendering
# --------------------------------------------------------------------------- #
def render(concept: str, rows: list[dict]) -> str:
    spec = ARCHETYPES[concept]
    lines = [f"## `{concept}` — {spec['what']}", ""]
    if not rows:
        return "\n".join(lines + ["_No source resolves this concept._", ""])
    flag_scale_outliers(rows)

    lines += [f"Cameo family: `{spec['cameo_family']}` · sources: "
              f"**{len({r['source'] for r in rows})}** · profiles: **{len(rows)}**", ""]

    # --- Table A: original armor vocabulary --------------------------------- #
    armors: list[str] = []
    for r in rows:
        for a in r["versus"]:
            if a.lower() not in armors:
                armors.append(a.lower())
    lines += ["### Table A — as each mod ships it (original armor types)", "",
              "| source | warhead | how identified | " + " | ".join(armors) + " |",
              "|---|---|---|" + "--:|" * len(armors)]
    for r in sorted(rows, key=lambda r: (r["lineage"], r["source"])):
        cells = []
        for a in armors:
            v = {k.lower(): v for k, v in r["versus"].items()}.get(a)
            cells.append("—" if v is None else f"{v:g}")
        lines.append(f"| `{r['source']}` | `{r['warhead']}` | {r['how']} | "
                     + " | ".join(cells) + " |")

    # --- Table B: mapped onto Cameo's 16 ------------------------------------ #
    lines += ["", "### Table B — extrapolated onto Cameo's 16 armor types", "",
              "⚠ Direct where the source has the armor; **extrapolated** for the ladder",
              "ends (Heroic / Scout / Superheavy) by continuing the ladder's own step; the",
              "four aircraft classes are read off the vehicle ladder by weight",
              "(Fighter/Helicopter←Light, Bomber←Medium, Spaceship←Heavy).", "",
              "| source | " + " | ".join(CAMEO16) + " |",
              "|---|" + "--:|" * len(CAMEO16)]
    for r in sorted(rows, key=lambda r: (r["lineage"], r["source"])):
        vals, _ = to_cameo(r["versus"])
        cells = ["—" if vals.get(a) is None else f"{vals[a]:g}" for a in CAMEO16]
        lines.append(f"| `{r['source']}` | " + " | ".join(cells) + " |")

    counts = {a: len(v) for a, v in buckets_of(rows).items()}
    lines.append("| _n sources_ | "
                 + " | ".join(str(counts.get(a, 0)) for a in CAMEO16) + " |")

    # --- Table C: the three aggregations, against Cameo as it stands now ----- #
    aggs = {label: aggregate(rows, how) for label, how in
            (("median", "median"), ("arithmetic mean", "mean"),
             ("geometric mean", "gmean"))}
    cameo = cameo_profile(spec["cameo_family"])
    if cameo:
        aggs["CAMEO today"] = cameo

    lines += ["", "### Table C — three ways to aggregate, vs Cameo today", "",
              "All four rows are normalised to max = 100, so they compare SHAPE, not",
              "power level. `median` is robust to one weird mod; `arithmetic mean` lets",
              "every source pull proportionally; `geometric mean` is the correct average",
              "for MULTIPLIERS (averaging x2 and x0.5 arithmetically invents a net buff",
              "of 1.25 — geometrically it is 1.0). A wide mean-vs-gmean gap means the",
              "sources disagree about that armor.", "",
              "| aggregation | " + " | ".join(CAMEO16) + " | span |",
              "|---|" + "--:|" * (len(CAMEO16) + 1)]
    for label, prof in aggs.items():
        cells = ["—" if prof.get(a) is None else f"{prof[a]:g}" for a in CAMEO16]
        vals = list(prof.values())
        span = f"{max(vals) - min(vals):.0f}" if vals else "—"
        mark = "**" if label == "CAMEO today" else ""
        lines.append(f"| {mark}{label}{mark} | " + " | ".join(cells) + f" | {span} |")

    med = aggs["median"]
    vals = list(med.values())
    if vals:
        lines += ["", f"**Reference span {max(vals) - min(vals):.0f}** "
                  f"(min {min(vals):g} · max {max(vals):g})."]
        if cameo:
            cvals = list(cameo.values())
            lines.append(f"**Cameo span {max(cvals) - min(cvals):.0f}** — "
                         + ("SHARPER than" if max(cvals) - min(cvals) >
                            max(vals) - min(vals) else "FLATTER than")
                         + " the field for this concept.")
        lines.append("")
    return "\n".join(lines)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("concept", nargs="?", help="which concept to aggregate")
    ap.add_argument("--list", action="store_true", help="list known concepts")
    ap.add_argument("--all", action="store_true", help="every concept")
    ap.add_argument("--write", action="store_true", help=f"write {OUT.relative_to(ROOT)}")
    args = ap.parse_args()

    if args.list:
        for name, spec in ARCHETYPES.items():
            print(f"  {name:14} {spec['what']}")
        return 0

    names = list(ARCHETYPES) if args.all else ([args.concept] if args.concept else [])
    if not names:
        ap.error("give a concept, --all, or --list")
    for name in names:
        if name not in ARCHETYPES:
            print(f"unknown concept: {name}; known: {', '.join(ARCHETYPES)}")
            return 1

    chunks = [render(n, collect(n)) for n in names]
    body = "\n\n".join(chunks)
    print(body)
    if args.write:
        header = [
            "# W13 — weapon-concept reference tables",
            "",
            "Generated by `tools/reference/aggregate_archetype.py` from",
            "`docs/reference/versus_raw.json` (16 mods, 3150 profiles).",
            "One concept at a time, per the maintainer's method: see what every mod ships",
            "for the SAME weapon, then extrapolate onto Cameo's armor set.",
            "",
            "For the INI mods the warhead is found by TRACING `[actor] Primary=` ->",
            "`[weapon] Warhead=`, so each row is the weapon that building actually fires",
            "rather than a warhead that merely has a matching name.",
            "",
        ]
        OUT.write_text("\n".join(header) + body + "\n", encoding="utf-8")
        print(f"\nwrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
