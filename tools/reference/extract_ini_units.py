#!/usr/bin/env python3
"""extract_ini_units.py — read the Westwood/Ares INI reference mods into the peer schema.

Companion to `extract_peer_units.py`, which reads the OpenRA peers. Between them every
reference source lands in ONE schema so `assign_references.py` can treat them alike.

WHY A SECOND EXTRACTOR. The OpenRA peers are yaml with `Inherits:` chains and must be read
through `miniyaml.Ruleset`. These sources are Westwood INI: flat sections, no inheritance,
`Owner=` for faction, and armor expressed as `Verses=` (RA2/YR, 11 slots) or
`Modifier.<armor>=` (Tiberian Sun, named). Nothing about that fits the yaml resolver.

⛔ TRAPS THIS FILE EXISTS TO AVOID — each one already cost a wrong conclusion:
  * A mod's loose `rulesmd.ini` can be **vanilla Yuri's Revenge byte for byte** (Mental Omega's
    is; md5 cf7eb658327aff1fe7e6c4e7400eb87f). Harvesting it yields vanilla YR counted twice and
    zero mod data. `--verify` refuses that hash.
  * These files are NOT UTF-8. They are latin-1/cp1252 with CRLF. Decode as latin-1 so every
    byte round-trips; a UTF-8 read throws partway through several of them.
  * `Owner=` is the faction column and it is a COMMA LIST. A unit owned by six countries is six
    faction rows, not one.
  * ⛔ Rail/particle weapons deal damage through channels `Damage=` does not describe. The
    engine profile below is taken from the OpenTS source (weapon keys `AmbientDamage`,
    `IsRailgun`, `UseFireParticles`, `UseSparkParticles`, `AttachedParticleSystem`; warhead
    key `Particle`). DTA's X-O Power Suit (Primary=`XORail` railgun, Secondary=`XOMachineGun`)
    once had its railgun demoted to a bare name and the machine gun promoted in its place.
    The final contract: channels stay RAW, the fold is declared — `w_evidence` is
    `nominal_direct` for the plain Damage/ROF contract (never "complete") or `incomplete`
    with a reason — and `w_dps_usable` True vouches for the NOMINAL-DIRECT contract only.
    There is NO auto-promotion: the original primary and the raw secondary stay in their
    slots; the only promotion path is `EXPLICIT_DUMMY_WEAPONS`, an empty-by-default
    per-source profile the exact rules files must populate. Burst > 1, effect references and
    dangling/absent warheads are incomplete until a cycle model exists.

Usage:
    python tools/reference/extract_ini_units.py --list
    python tools/reference/extract_ini_units.py --source "Mental Omega"
    python tools/reference/extract_ini_units.py --json docs/reference/ini_corpus.json
    python tools/reference/extract_ini_units.py --rules path/Rules.ini \
        [--overlay path/Enhance.ini] --engine ts --label NAME --json out.jsonl
        (single-source mode: exact files only — Rules.ini + Enhance.ini from DTA_INI.zip;
         GlobalCode/Base are never auto-merged; --engine and a nonblank --label are
         required with --rules; --json must land outside every git repo and source
         directory and is never overwritten)
"""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import sys

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

REF = pathlib.Path.home() / "Documents" / "GitHub" / "Cameo-mod-reference" / "extraction"

# Vanilla Yuri's Revenge. Any source whose rules hash to this is not a mod at all.
VANILLA_YR_MD5 = "cf7eb658327aff1fe7e6c4e7400eb87f"

# ⛔ THE ONLY PROMOTION PATH, AND IT IS EMPTY. A missing/zero `Damage` cannot prove a dummy
# primary — the absence of known markers is not proof that all channels are supported — and a
# wrongly promoted secondary silently votes. So by default the ORIGINAL primary keeps `w_*`
# and the raw secondary stays whole in `w2_*`. Promotion happens ONLY when the source itself
# proves the placeholder: a per-source set of weapon names. The exact DTA INI archive has
# been received by the maintainer OUTSIDE this repo; ⚠ it does NOT verify any dummy — this
# profile stays empty until the parent's review proves placeholder weapons from that source's
# own text. When it fires, the historical compatibility shape is emitted — `w_from_secondary`,
# `w_dummy_primary` (the original public key, kept as a compatibility diagnostic where it
# actually applies), the demoted record whole under `wdummy_*` — and the raw `w2_*` slot is
# re-emitted. The legacy committed corpus keeps its old promoted rows regardless: it is NOT
# regenerated this session.
EXPLICIT_DUMMY_WEAPONS: dict[str, set] = {}

SOURCES = {
    "Rise of the East":  {"file": "rulesmd_RotE300c.ini",  "engine": "ra2"},
    "RA2 0XX":           {"file": "rulesmd_RA20XX108.ini", "engine": "ra2"},
    "Mental Omega":      {"file": "rulesmd_MO336.ini",     "engine": "ra2"},
    "CnC Reloaded":      {"file": "rulesmd_CnCR270.ini",   "engine": "ra2"},
    "Red Resurrection":  {"file": "rulesmd_RedRes2213.ini","engine": "ra2"},
    "RA2 Reborn":        {"file": "rulesmd_Reborn1031.ini","engine": "ra2"},
    "DTA Classic":       {"file": "rules_DTA_Classic.ini", "engine": "ts"},
    "DTA Enhanced":      {"file": "rules_DTA_Classic.ini", "engine": "ts",
                          "overlay": "rules_DTA_Enhance_overlay.ini"},
    # ⭐ Twisted Insurrection is the named fix for Cameo's `forgotten`: it is a Tiberian Sun mod
    # and it ships a MUTANT faction — its houses are GDI, Nod, GT, **Forsaken**, Phoenix, Sons.
    # ⚠ It declares them under `[Houses]`, not `[Countries]` like every other source here.
    "Twisted Insurrection": {"file": "rules_TwistedInsurrection.ini", "engine": "ts"},
}

# Which list section declares which unit type. `type` matches extract_peer_units' vocabulary.
TYPE_LISTS = {
    "InfantryTypes": "infantry",
    "VehicleTypes": "vehicle",
    "AircraftTypes": "aircraft",
    "BuildingTypes": "building",
}

# RA2/YR `Verses=` slot order. Fixed by the engine, not by the mod.
RA2_ARMOR = ["none", "flak", "plate", "light", "medium", "heavy",
             "wood", "steel", "concrete", "special_1", "special_2"]

SECTION = re.compile(r"^\s*\[([^\]]+)\]")
KV = re.compile(r"^\s*([$A-Za-z0-9_.]+)\s*=\s*([^;]*)")


def read_ini(path: pathlib.Path) -> dict[str, dict[str, str]]:
    """{section: {key: value}}. latin-1 so every byte round-trips — these are not UTF-8."""
    out: dict[str, dict[str, str]] = {}
    cur: dict[str, str] | None = None
    for line in path.read_bytes().decode("latin-1").splitlines():
        line = line.strip()
        if not line or line.startswith(";"):
            continue
        m = SECTION.match(line)
        if m:
            cur = out.setdefault(m.group(1).strip(), {})
            continue
        if cur is None:
            continue
        m = KV.match(line)
        if m:
            cur[m.group(1).strip()] = m.group(2).strip()
    return out


def resolve_inherits(ini: dict[str, dict[str, str]]) -> dict[str, dict[str, str]]:
    """Flatten Vinifera's ``$Inherits=`` section inheritance within one file."""
    resolved: dict[str, dict[str, str]] = {}

    def flatten(name: str, stack: tuple[str, ...]) -> dict[str, str]:
        if name in resolved:
            return dict(resolved[name])
        if name in stack:
            cycle = " -> ".join((*stack[stack.index(name):], name))
            raise ValueError(f"cyclic $Inherits chain: {cycle}")
        own = ini.get(name)
        if own is None:
            return {}
        stack = (*stack, name)
        parent = own.get("$Inherits", "").strip()
        if parent and parent in ini:
            merged = flatten(parent, stack)
            merged.update(own)
        else:
            merged = dict(own)
        merged.pop("$Inherits", None)
        resolved[name] = merged
        return dict(merged)

    return {name: flatten(name, ()) for name in ini}


def merge_overlay(base: dict, overlay: dict) -> dict:
    """DTA Enhanced = Classic + Enhance.ini, which is how the game loads it (DefaultIndex=1)."""
    out = {k: dict(v) for k, v in base.items()}
    for sec, kv in overlay.items():
        out.setdefault(sec, {}).update(kv)
    return out


def listed(ini: dict, section: str) -> list[str]:
    """A Westwood list section is `0=NAME`, `1=NAME`, ... — order is the registry order."""
    return [v.strip() for k, v in sorted(ini.get(section, {}).items(),
                                         key=lambda kv: int(kv[0]) if kv[0].isdigit() else 1 << 30)
            if v.strip()]


def num(v, default=None):
    if v is None:
        return default
    v = str(v).strip().rstrip("%")
    try:
        return float(v) if "." in v else int(v)
    except ValueError:
        return default


def versus_of(ini: dict, warhead: str, engine: str) -> tuple[dict, str]:
    """{armor: percent} for a warhead, plus which notation it came from."""
    wh = ini.get(warhead)
    if not wh:
        return {}, "missing"
    if "Verses" in wh:
        vals = [num(x, 0) for x in wh["Verses"].split(",")]
        # TS ships 5 slots, RA2/YR 11. Name them by the engine's own order; a TS file read as
        # RA2 would silently mislabel every row, which is why engine is threaded through.
        names = RA2_ARMOR if len(vals) >= 11 else ["none", "wood", "light", "concrete", "heavy"]
        return {n: v for n, v in zip(names, vals)}, "Verses"
    mods = {k.split(".", 1)[1].lower(): num(v, 0)
            for k, v in wh.items() if k.lower().startswith("modifier.")}
    if mods:
        return mods, "Modifier.*"
    return {}, "none"


# ── Engine profile: where damage can hide, with the source that says so ─────────────────────
# OpenTS looks INI names up by their RAW BYTES: "Names are looked up by their raw bytes, so
# lookups are case sensitive" (opents code/ini.h:144; INIStringHash hashes the string_view
# unchanged). Every key below is copied EXACTLY from the engine's own ini.Get_* calls, and
# every read here is therefore EXACT-CASE. A near-miss spelling is EXPOSED as ambiguity
# (`channel_ambiguities`) — never silently folded, never counted as evidence.
#   AmbientDamage            weapon   Get_Int     opents code/weapon.cpp:169
#   IsRailgun                weapon   Get_Bool    opents code/weapon.cpp:200
#   UseFireParticles         weapon   Get_Bool    opents code/weapon.cpp:198
#   UseSparkParticles        weapon   Get_Bool    opents code/weapon.cpp:199
#   AttachedParticleSystem   weapon   Get_String  opents code/weapon.cpp:214 — the system
#                                         techno.cpp:4088-4097 attaches for rail/fire/spark
#   Particle                 warhead  Get_String  opents code/warhead.cpp:162
# Bool VALUES are the one case-folded thing, and that is the engine's own rule: Get_Bool
# (opents code/ini.cpp:1390) decides on toupper(Value[0]) ∈ {Y,T,1} vs {N,F,0}.
# ⚠ This profile is OPENTS (Tiberian Sun). The RA2/YR sources share the weapon keys but no
# engine source for them is on disk here — the ambiguity probe covers that gap instead of a
# guess. `versus_of`'s pre-existing `k.lower().startswith("modifier.")` fold is NOT touched:
# it predates this repair and the committed armor corpora were built on it (see report).
WEAPON_CHANNEL_KEYS = ("AmbientDamage", "IsRailgun", "UseFireParticles",
                       "UseSparkParticles", "AttachedParticleSystem")
WARHEAD_CHANNEL_KEYS = ("Particle",)
# Stamped onto every single-source provenance row; bump when the profile's key set or
# verdict rules change, so an exported corpus states WHICH rules produced it.
ENGINE_PROFILE_ID = "opents_weapon_channels"
ENGINE_PROFILE_VERSION = 1


def bool_value(v):
    """The engine's own bool parsing (opents code/ini.cpp:1390): decide on the FIRST
    character of the value, case-insensitively — Y/T/1 true, N/F/0 false, anything else
    (or nothing) falls back to the caller's default, which reads as None here."""
    c = (v or "").strip()[:1].upper()
    if c in ("Y", "T", "1"):
        return True
    if c in ("N", "F", "0"):
        return False
    return None


def damage_channels(ini: dict, w: dict, warhead: str) -> dict:
    """Raw, DECLARED damage channels that the simple Damage/ROF fold cannot express.

    ⛔ Rail/particle weapons carry damage outside the plain `Damage` literal: the railgun
    beam deals `AmbientDamage` along its path (techno.cpp `Railgun_Beam_Damage`), and the
    fire/spark particle systems damage through their own behaviour. The values are kept RAW
    on purpose — no fold, no total, no DPS is computed from them here; the verdict is carried
    separately by `w_evidence`/`w_evidence_reason`/`w_dps_usable` in `weapon_of`.

    Marker vs reference, per the engine profile above:
      * `IsRailgun` true, a NON-ZERO `AmbientDamage`, `UseFireParticles`/`UseSparkParticles`
        true are MARKERS — damage the fold cannot see;
      * `AttachedParticleSystem` (weapon) and warhead `Particle` are retained REFERENCES —
        the engine attaches/draws them for many weapons regardless of damage behaviour, so
        they never drive the verdict on their own;
      * an explicit `AmbientDamage=0` is a cancellation the fold CAN include (it adds
        nothing), so it is retained but is not a marker;
      * an unparsable `AmbientDamage` reads the engine default 0 — retained raw for review,
        not a marker.
    """
    out: dict = {}
    ambient = w.get("AmbientDamage")
    if ambient is not None and str(ambient).strip():
        parsed = num(ambient)
        # Unparsable ambient text round-trips as its raw string rather than being dropped.
        out["w_ambient_damage"] = parsed if parsed is not None else str(ambient).strip()
    if bool_value(w.get("IsRailgun")):
        out["w_railgun"] = True
    if bool_value(w.get("UseFireParticles")):
        out["w_fire_particles"] = True
    if bool_value(w.get("UseSparkParticles")):
        out["w_spark_particles"] = True
    attached = w.get("AttachedParticleSystem")
    if attached and str(attached).strip():
        out["w_attached_particle_system"] = str(attached).strip()
    wh = ini.get(warhead) if warhead else None
    particle = wh.get("Particle") if wh else None
    if particle and str(particle).strip():
        out["w_particle_system"] = str(particle).strip()
    return out


def channel_ambiguities(ini: dict, w: dict, warhead: str) -> list:
    """Near-miss spellings of the profile keys — keys that differ only by case.

    The engine ignores them (raw-byte lookup, opents code/ini.h:144), so they are NOT
    evidence and MUST NOT close a gate or count as a channel. They are exposed so a mod's
    spelling drift is visible instead of silently folded away.
    """
    found = set()
    for section, keys in ((w, WEAPON_CHANNEL_KEYS),
                          (ini.get(warhead) if warhead else None, WARHEAD_CHANNEL_KEYS)):
        if not section:
            continue
        for key in keys:
            for k in section:
                if k != key and k.lower() == key.lower():
                    found.add(f"{key}<>{k}")
    return sorted(found)


def relabel_weapon(rec: dict, prefix: str) -> dict:
    """Copy a weapon record under another slot prefix (`w2_*`, `wdummy_*`), keeping only the
    fields that carry a value — the same shape the `w2_` merge has always used."""
    return {f"{prefix}{k[2:]}" if k.startswith("w_") else f"{prefix}weapon": v
            for k, v in rec.items() if v is not None}


def weapon_of(ini: dict, wname: str, engine: str) -> dict:
    """Scale-free numbers for one weapon, plus its warhead's armor profile."""
    w = ini.get(wname)
    if not w:
        return {"weapon": wname or None}
    dmg, rof = num(w.get("Damage")), num(w.get("ROF"))
    warhead = (w.get("Warhead") or "").strip()
    vs, notation = versus_of(ini, warhead, engine)
    channels = damage_channels(ini, w, warhead)
    ambiguity = channel_ambiguities(ini, w, warhead)
    burst = num(w.get("Burst"))
    # A dangling or absent warhead is a missing DEPENDENCY: the versus profile cannot be
    # resolved and the weapon's damage application cannot be assessed. (The projectile is
    # deliberately NOT dependency-checked: TS mods declare projectiles in art.ini as often as
    # in rules, which this extractor does not read — checking it here would fabricate
    # missing-dependency verdicts.)
    wh_resolved = bool(warhead) and warhead in ini
    # Verdict, kept SEPARATE from the raw channels. There is no "complete" verdict:
    #   * declared markers (railgun identity — even beside a positive `Damage` literal — a
    #     non-zero `AmbientDamage`, fire/spark particles) → incomplete, `exotic_channels`;
    #   * dangling/absent warhead → incomplete, `missing_dependency`;
    #   * `Burst` > 1 → incomplete, `burst_unfolded` — damage/ROF is one shot per cycle and
    #     cannot describe the declared multi-shot cadence until a cycle model exists;
    #   * an effect-system reference (AttachedParticleSystem / warhead `Particle`) →
    #     incomplete, `effect_reference` — an effect the fold cannot model;
    #   * missing/zero `Damage` → incomplete, `direct_undeclared` — the absence of known
    #     markers is NOT proof that all channels are supported;
    #   * otherwise → `nominal_direct`: the DECLARED contract that `w_dps` is the plain
    #     direct Damage/ROF estimate. That is a NOMINAL DIRECT value, never a proven total
    #     DPS or a full cycle — and `w_dps_usable` True vouches for exactly that contract.
    if channels:
        markers = (channels.get("w_railgun")
                   or channels.get("w_fire_particles")
                   or channels.get("w_spark_particles")
                   or (num(channels.get("w_ambient_damage")) or 0) != 0)
    else:
        markers = False
    if markers:
        evidence, reason = "incomplete", "exotic_channels"
    elif not wh_resolved:
        evidence, reason = "incomplete", "missing_dependency"
    elif burst is not None and burst > 1:
        evidence, reason = "incomplete", "burst_unfolded"
    elif channels.get("w_particle_system") or channels.get("w_attached_particle_system"):
        evidence, reason = "incomplete", "effect_reference"
    elif not dmg:
        evidence, reason = "incomplete", "direct_undeclared"
    else:
        evidence, reason = "nominal_direct", None
    dps = (dmg / rof) if (dmg and rof) else None
    return {
        "weapon": wname or None,
        "w_damage": dmg,
        "w_reload": rof,
        "w_range": num(w.get("Range")),
        "w_min_range": num(w.get("MinimumRange")),
        "w_burst": burst,
        # ROF is frames between shots; damage/ROF is the engine's own scale-free rate.
        # A NOMINAL DIRECT estimate under the declared contract: when `w_dps_usable` is not
        # True the number MUST NOT be consumed at all (not even as a diagnostic total).
        "w_dps": dps,
        "w_projectile": w.get("Projectile"),
        "w_warhead": warhead or None,
        "w_versus": vs or None,
        "w_versus_notation": notation,
        **channels,
        "w_evidence": evidence,
        "w_evidence_reason": reason,
        "w_dps_usable": True if (reason is None and dps is not None) else None,
        # Exposed, never folded: near-miss spellings the raw-byte engine will not read.
        "w_channel_ambiguity": ambiguity or None,
    }


def extract_rows_from_ini(ini: dict, label: str, engine: str) -> list[dict]:
    """The shared extraction core for BOTH modes: every unit-list section, weapon evidence,
    ownership and the naval/defense reclassification. It reads NOTHING but the `ini` dict it
    is handed — named sources (`extract`) and the single-source CLI
    (`extract_single_source`) differ only in how the dict is built; the single-source mode
    stamps per-row provenance on top of these rows."""
    # ⚠ `[Countries]` is the RA2/YR spelling; Tiberian Sun mods use `[Houses]`. Reading only the
    # first left `countries` empty for a TS source, which silently disabled the filter below
    # rather than failing — the owners were kept unvalidated. Read both.
    countries = set(listed(ini, "Countries")) | set(listed(ini, "Houses"))
    rows: list[dict] = []
    for list_sec, utype in TYPE_LISTS.items():
        for actor in listed(ini, list_sec):
            a = ini.get(actor)
            if not a:
                continue
            prim = (a.get("Primary") or "").strip()
            sec = (a.get("Secondary") or "").strip()
            wep = weapon_of(ini, prim, engine) if prim else {"weapon": None}
            sw = weapon_of(ini, sec, engine) if sec else None
            # ⛔ A DUMMY PRIMARY HIDES THE REAL GUN IN THE SECONDARY SLOT — and the wrong
            # selection silently votes. Westwood engines pick Primary/Secondary by TARGET, so
            # a unit whose anti-air or elite-only slot is a zero-damage placeholder carries its
            # actual cannon as `Secondary`. Reading only `Primary` recorded those units as
            # unarmed: DTA's GDI Medium Tank extracted as `90mmDummy`, damage 0 — and clause 5
            # of the matching law ("a zero-damage row never matches a combat unit") then
            # removed the one exact-name reference for Cameo's GDI Battle Tank, which fell
            # through to a MOBILE SENSOR ARRAY. 161 rows across 8 sources were reading damage
            # off the wrong weapon.
            # ⛔ NO AUTO-PROMOTION. An unsupported primary cannot prove a dummy: a missing or
            # zero `Damage` is `w_evidence: incomplete` (`direct_undeclared`) even with no
            # known marker, because the profile above is what the engine source declares, not
            # what every mod writes — and promoting the wrong slot would make the secondary
            # vote for the unit. So the ORIGINAL primary keeps `w_*` and the raw secondary
            # stays whole in `w2_*`, always. The ONLY promotion path is the explicit
            # source-profile proof in `EXPLICIT_DUMMY_WEAPONS` (empty until the exact DTA
            # files arrive); when it fires it emits the historical compatibility shape —
            # `w_from_secondary`, `w_dummy_primary` (the original public key, kept as a
            # compatibility diagnostic where it actually applies), the demoted record whole
            # under `wdummy_*` — and `sw` is KEPT so the raw `w2_*` slot is re-emitted.
            # ⛔ DPS IS NEVER SUMMED ACROSS THE SLOTS. The OTHER weapon is kept whole so an
            # effective-damage fold (burst + simultaneous weapons) can be built later without a
            # re-extract. It does NOT vote yet: Westwood Primary/Secondary is usually
            # target-SELECTED, not simultaneous, so summing the two would overstate every
            # dual-purpose unit.
            if sw and sw.get("w_damage") and not wep.get("w_damage"):
                if wep.get("weapon") in EXPLICIT_DUMMY_WEAPONS.get(label, ()):
                    demoted = wep
                    wep = {**sw, "w_from_secondary": True,
                           "w_dummy_primary": demoted.get("weapon")}
                    wep.update(relabel_weapon(demoted, "wdummy_"))
            if sw:
                wep = {**wep, **relabel_weapon(sw, "w2_")}
            owners = [o.strip() for o in (a.get("Owner") or "").split(",") if o.strip()]
            rows.append({
                "source": label,
                "engine": engine,
                "id": actor,
                "name": a.get("Name") or a.get("UIName") or actor,
                "type": utype,
                # ⭐ the faction column — a comma list, and every owner is its own vote
                "faction": "/".join(o for o in owners if not countries or o in countries),
                "owners": owners,
                "hp": num(a.get("Strength")),
                "cost": num(a.get("Cost")),
                "speed": num(a.get("Speed")),
                "armor": (a.get("Armor") or "").strip() or None,
                "sight": num(a.get("Sight")),
                # ⭐ `ROT` is the Westwood rate of turn and it is the CHASSIS stat every INI
                # source was abstaining on — `reference_distribution` scores `turn_speed` and
                # `turn_ratio`, and with no ROT column all eight sources contributed nothing to
                # either. Higher is faster in both Westwood and OpenRA, and every coordinate is
                # built inside ONE source, so the two scales never have to meet.
                "turn_speed": num(a.get("ROT")),
                "turreted": (a.get("Turret") or "").strip().lower() in ("yes", "true"),
                "tech_level": num(a.get("TechLevel")),
                "prerequisite": a.get("Prerequisite"),
                "build_limit": num(a.get("BuildLimit")),
                "power": num(a.get("Power")),
                "build_time": num(a.get("BuildTimeMultiplier")),
                "secondary": (a.get("Secondary") or "").strip() or None,
                "naval": (a.get("Naval") or "").strip().lower() in ("yes", "true"),
                # ⛔ `cost > 0` IS NOT A BUILDABILITY TEST IN THESE MODS. They price internal
                # dummies at 1 credit, and the result poisons exactly the tail a distribution is
                # most sensitive to: CnC Reloaded's `TSCARRYALL_DUMMY` ("Call Carryall from the
                # sky", Selectable=no, Armor=unkillable_armor) is a costed 10,000,000 HP row
                # against a real ceiling of 6,000 — a 1,667x outlier sitting in the arithmetic
                # mean of a 443-unit population. Two of the engine's OWN flags settle it:
                #   TechLevel = -1        Westwood for "the player can never build this". It also
                #                         removes the elite/upgraded DUPLICATES these mods ship as
                #                         separate actors (RotE's `RANGER_E`, `MINDUP_E`), which
                #                         would otherwise double-count their own base unit.
                #   Selectable = no  /  IsSelectableCombatant = no
                #                         not something a player commands.
                # Measured max HP before -> after: CnCR 10,000,000 -> 6,000, RotE 15,000 -> 2,000,
                # RA2 0XX 9,999 -> 3,000, MO 6,000 -> 2,500. DTA's `civilian` roster goes to zero,
                # which is the right answer. The rows are KEPT and FLAGGED rather than dropped —
                # R6 says collect everything; the population rule belongs to the consumer.
                "buildable": not (
                    (num(a.get("TechLevel")) is not None and num(a.get("TechLevel")) < 0)
                    or (a.get("Selectable") or "").strip().lower() == "no"
                    or (a.get("IsSelectableCombatant") or "").strip().lower() == "no"),
                **wep,
            })
    for r in rows:
        # RA2 INI has no naval or defence list — ships live in VehicleTypes and turrets in
        # BuildingTypes. The maintainer's profile groups need them separated, so derive:
        #   naval   = a vehicle flagged Naval=yes
        #   defense = a building that actually carries a weapon
        if r["type"] == "vehicle" and r.get("naval"):
            r["type"] = "naval"
        elif r["type"] == "building":
            r["type"] = "defense" if r.get("weapon") else "building"
    return rows


def extract(label: str, spec: dict) -> tuple[list[dict], list[str]]:
    """NAMED-source mode — unchanged public behavior. Resolves the file under `REF`,
    refuses a vanilla-YR byte-identical file, applies the overlay fail-closed, then runs the
    shared core."""
    notes: list[str] = []
    path = REF / spec["file"]
    if not path.exists():
        return [], [f"{label}: MISSING {path}"]
    digest = hashlib.md5(path.read_bytes()).hexdigest()
    if digest == VANILLA_YR_MD5:
        return [], [f"{label}: REFUSED — this file is vanilla Yuri's Revenge (md5 {digest})"]

    def load(p: pathlib.Path) -> dict[str, dict[str, str]]:
        raw = read_ini(p)
        inherited = sum(1 for values in raw.values() if "$Inherits" in values)
        if inherited:
            notes.append(f"{label}: flattened {inherited} $Inherits sections in {p.name}")
            return resolve_inherits(raw)
        return raw

    ini = load(path)
    if spec.get("overlay"):
        # ⛔ FAIL CLOSED. A missing overlay must not silently label the base file's rows with
        # the overlay's identity (DTA Classic would be extracted AS "DTA Enhanced"). Refuse
        # the whole source: no rows, one clear note.
        ov = REF / spec["overlay"]
        if not ov.exists():
            return [], [f"{label}: REFUSED — overlay {spec['overlay']} missing; base "
                        f"{spec['file']} would be mislabelled as {label}"]
        ini = merge_overlay(ini, load(ov))
        notes.append(f"{label}: applied overlay {spec['overlay']}")
    return extract_rows_from_ini(ini, label, spec["engine"]), notes


class SingleSourceRefusal(RuntimeError):
    """The single-source mode refuses loudly (CLI exits 1) instead of guessing."""


def sha256_file(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def jsonl_lines(rows: list[dict]) -> list[str]:
    # One row per line: a changed unit is a one-line diff, and it greps. An indented
    # 10k-row array is neither reviewable nor small.
    # ⛔ allow_nan=False: a NaN/Infinity would be written as bare `NaN` — invalid JSON a
    # downstream parser rejects halfway through. Non-finite data fails HERE, before any
    # output file exists.
    return [
        # `buildable` is named explicitly because it is the one field whose FALSE is the
        # signal — a row that survives the filter is worth nothing without it.
        json.dumps({k: v for k, v in r.items()
                    if k == "buildable" or v not in (None, "", [])},
                   sort_keys=True, separators=(",", ":"), allow_nan=False)
        for r in rows
    ]


def extract_single_source(rules, overlay, label: str, engine: str) -> tuple[list[dict], list[str], dict]:
    """Single-source mode: extract ONE exact rules file (+ optional overlay) with full
    provenance, for when the exact source files live outside the repo (DTA's generated
    `Rules.ini` / `Enhance.ini`).

    ⛔ READS ONLY THE TWO GIVEN FILES. DTA's INI archive also ships GlobalCode / Base /
    artwork INI sets — none are merged, scanned or inferred here: no directory walk, no
    sibling-file magic. Overlay precedence is the game's own (Enhance wins over Rules, the
    same `merge_overlay` the named mode uses).

    Reproducibility contract:
      * paths are JUNCTION-RESOLVED before anything is decided;
      * SHA256 of every input is taken BEFORE the read and AGAIN after extraction — any
        change in between fails the whole run;
      * the returned provenance dict (stamped onto EVERY row) carries the exact source
        SHA256, the overlay's SHA256 and precedence, the label, the SELECTED engine (its
        own field, `engine` — ts/ra2 as passed), the OpenTS channel-profile version WITH an
        explicit applicability status, the SOURCE game runtime version status (unverified —
        we do not know which engine build produced these files) and the extractor's own
        Python version, which is the only runtime recorded as fact;
      * the raw source text is NEVER embedded — rows carry extracted scalars and hashes only.
    """
    notes: list[str] = []
    rules = pathlib.Path(rules).resolve()
    if not rules.exists() or rules.is_dir():
        raise SingleSourceRefusal(f"REFUSED — rules file missing or not a file: {rules}")
    if overlay:
        overlay = pathlib.Path(overlay).resolve()
        if not overlay.exists() or overlay.is_dir():
            raise SingleSourceRefusal(
                f"REFUSED — overlay {overlay.name} missing; base {rules.name} would be "
                f"mislabelled as {label}")
    digest = hashlib.md5(rules.read_bytes()).hexdigest()
    if digest == VANILLA_YR_MD5:
        raise SingleSourceRefusal(
            f"REFUSED — this file is vanilla Yuri's Revenge (md5 {digest})")

    sha_rules_before = sha256_file(rules)
    sha_overlay_before = sha256_file(overlay) if overlay else None

    # Resolve each file in isolation before applying overlay precedence. Merging first can
    # create cycles when Enhance.ini deliberately reverses a Classic inheritance relationship.
    ini = resolve_inherits(read_ini(rules))
    if overlay:
        ini = merge_overlay(ini, resolve_inherits(read_ini(overlay)))
        notes.append(f"{label}: applied overlay {overlay.name}")
    rows = extract_rows_from_ini(ini, label, engine)

    # ⛔ the extraction is only honest for the bytes it actually read
    if sha256_file(rules) != sha_rules_before or (overlay and sha256_file(overlay) != sha_overlay_before):
        raise SingleSourceRefusal(
            f"REFUSED — input changed during extraction ({rules.name}"
            + (f" / {overlay.name}" if overlay else "") + "); nothing is exported")

    provenance = {
        "source_label": label,
        "source_sha256": sha_rules_before,
        "overlay_sha256": sha_overlay_before,
        "overlay_precedence": "overlay_over_rules" if overlay else "none",
        # The SELECTED engine stays its own field (`engine`, stamped by the core). The
        # channel profile is OPENTS — the Tiberian Sun engine source. ⛔ ITS APPLICABILITY
        # IS ALWAYS UNVERIFIED, for BOTH engines: a user-declared `--engine ts` is not proof
        # that the mod's exact engine build implements OpenTS (DTA ships its own TS fork),
        # and RA2 has no engine source on disk at all. No executable fidelity is claimed —
        # the only thing recorded is that the profile was USER-SELECTED.
        "engine_profile": f"{ENGINE_PROFILE_ID}_v{ENGINE_PROFILE_VERSION}",
        "engine_profile_applicability": "unverified",
        "profile_selection": "user_declared",
        # The SOURCE game's runtime version is unknown here — status only, never a guess.
        "source_runtime_version_status": "unverified",
        "extractor_python_version": sys.version.split()[0],
    }
    for r in rows:
        r.update(provenance)
    return rows, notes, provenance


def ensure_external_output(out_path, source_paths) -> pathlib.Path:
    """--json in single-source mode must land OUTSIDE every git repo and outside every
    source directory, must not overwrite, and every decision runs on JUNCTION-RESOLVED
    paths (`Path.resolve` also normalizes `..`). Refusals are loud, not silent renames."""
    out = pathlib.Path(out_path).resolve()
    parent = out.parent
    for anc in (parent, *parent.parents):
        if (anc / ".git").exists():          # covers .git dirs AND worktree .git files
            raise SingleSourceRefusal(
                f"REFUSED — {out} sits inside the git repo at {anc}; external output only")
    for src in source_paths:
        src_dir = pathlib.Path(src).resolve().parent
        if parent == src_dir or src_dir in parent.parents:
            raise SingleSourceRefusal(
                f"REFUSED — {out} sits inside the source directory {src_dir}")
    if out.exists():
        raise SingleSourceRefusal(
            f"REFUSED — {out} already exists; single-source output never overwrites")
    return out


def existing_sources(path: pathlib.Path) -> set[str]:
    """Read an existing JSONL corpus strictly before a named export can replace it."""
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as e:
        raise ValueError(f"cannot inspect existing output: {e}") from e
    sources: set[str] = set()
    for lineno, line in enumerate(lines, 1):
        if not line.strip():
            continue
        try:
            row = json.loads(line)
        except (TypeError, ValueError) as e:
            raise ValueError(f"existing output line {lineno} is not valid JSON") from e
        if not isinstance(row, dict):
            raise ValueError(f"existing output line {lineno} is not a JSON object")
        source = row.get("source")
        if not isinstance(source, str) or not source.strip():
            raise ValueError(f"existing output line {lineno} has no source label")
        sources.add(source)
    return sources


def main(argv=None) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--source", action="append", help="limit to these sources (repeatable)")
    ap.add_argument("--rules", help="single-source mode: extract this exact rules file")
    ap.add_argument("--overlay", help="single-source mode: optional overlay (e.g. Enhance.ini)")
    ap.add_argument("--label", help="single-source mode: source label (default: rules stem)")
    ap.add_argument("--engine", choices=("ts", "ra2"),
                    help="single-source mode: engine profile — REQUIRED with --rules")
    ap.add_argument("--json", help="write the corpus to this path")
    ap.add_argument("--force-partial", action="store_true",
                    help="allow named --source output to replace rows for other sources")
    ap.add_argument("--list", action="store_true", help="list sources and exit")
    args = ap.parse_args(argv)

    if (args.overlay or args.label or args.engine) and not args.rules:
        print("  refused: --overlay/--label/--engine require --rules", file=sys.stderr)
        return 1
    if args.source and args.list:
        print("  refused: --source and --list are conflicting named-mode flags", file=sys.stderr)
        return 1

    if args.rules:
        if args.source:
            print("  refused: --rules cannot be combined with --source", file=sys.stderr)
            return 1
        if args.list:
            print("  refused: --rules cannot be combined with --list", file=sys.stderr)
            return 1
        if not args.engine:
            print("  refused: --engine ts|ra2 is required with --rules", file=sys.stderr)
            return 1
        if not args.label or not args.label.strip():
            # two source variants must never silently share one label
            print("  refused: --rules needs an explicit nonblank --label", file=sys.stderr)
            return 1
        try:
            label = args.label
            sources = [pathlib.Path(args.rules).resolve()]
            if args.overlay:
                sources.append(pathlib.Path(args.overlay).resolve())
            rows, notes, _ = extract_single_source(args.rules, args.overlay, label,
                                                   args.engine)
            for n in notes:
                print(f"  ! {n}")
            print(f"  {label}: {len(rows)} rows (single-source, engine {args.engine})")
            if args.json:
                # ⛔ strict serialization FIRST — a malformed (non-finite) row must never
                # leave a partial artifact behind
                lines = jsonl_lines(rows)
                out = ensure_external_output(args.json, sources)
                # exclusive create — the file must not exist, and it is written only after
                # the extraction passed its hash and output-safety gates
                with open(out, "x", encoding="utf-8", newline="\n") as fh:
                    fh.write("\n".join(lines) + "\n")
                print(f"  wrote {out}")
            return 0
        except SingleSourceRefusal as e:
            print(f"  {e}", file=sys.stderr)
            return 1
        except ValueError as e:
            print(f"  REFUSED — output serialization failed: {e}", file=sys.stderr)
            return 1

    if args.list:
        for k, v in SOURCES.items():
            mark = "OK " if (REF / v["file"]).exists() else "-- "
            print(f"  {mark}{k:<20} {v['engine']:<4} {v['file']}")
        return 0

    wanted = args.source or list(SOURCES)
    all_rows, all_notes = [], []
    for label in wanted:
        spec = SOURCES.get(label)
        if not spec:
            print(f"  unknown source {label!r}", file=sys.stderr)
            continue
        try:
            rows, notes = extract(label, spec)
        except ValueError as e:
            print(f"  refused: {label}: {e}", file=sys.stderr)
            return 1
        all_rows += rows
        all_notes += notes
        armed = sum(1 for r in rows if r.get("w_versus"))
        print(f"  {label:<20} {len(rows):>5} actors   "
              f"{sum(1 for r in rows if r['cost']):>5} costed   "
              f"{armed:>5} with an armor profile   "
              f"{len({f for r in rows for f in r['owners']}):>3} owners")
    for n in all_notes:
        print(f"  ! {n}")
    print(f"\n  TOTAL {len(all_rows)} rows from {len(wanted)} source(s)")

    if args.json:
        out = pathlib.Path(args.json)
        if args.source and out.exists() and not args.force_partial:
            try:
                existing = existing_sources(out)
            except ValueError as e:
                print(f"  refused: {e}; use --force-partial for an explicit partial export",
                      file=sys.stderr)
                return 1
            selected = {row.get("source") for row in all_rows if row.get("source")}
            lost = sorted(existing - selected)
            if lost:
                print("  refused: --source would replace existing sources "
                      + ", ".join(lost) + "; use --force-partial for an explicit partial export",
                      file=sys.stderr)
                return 1
        out.parent.mkdir(parents=True, exist_ok=True)
        out.write_text("\n".join(jsonl_lines(all_rows)) + "\n", encoding="utf-8")
        print(f"  wrote {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
