#!/usr/bin/env python3
"""propose_sonic_mapping.py — heuristic ^Warhead_Sonic_* targets for adoption.

Does NOT modify YAML. Outputs a JSON/MD report in docs/audit/latest/.

Candidate list from docs/design/ROADMAP.md and PHYSICAL_STATE_SYSTEM.md §5.
For each weapon we resolve its current warhead templates and pick a Sonic
level heuristically; the maintainer must confirm the proposal before any
conversion is applied.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
from cameo_model import Model  # noqa: E402

OUT_DIR = ROOT / "docs" / "audit" / "latest"

# Candidate weapons from ROADMAP.md / PHYSICAL_STATE_SYSTEM.md §5
CANDIDATES = [
    # TS GDI base sonic weapons
    "TSSonicZapWeapon",
    "TSSonicZapWeaponSonic",
    # TS GDI sonic UPGRADE variants
    "TSVulcanGunSonic",
    "TSAssaultCannonSonic",
    "TSAssaultCannonTalSonic",
    "TSHellfireSonic",
    "TSZoneHellfireSonic",
    "TSBombSonic",
    "TSGrenadeSonic",
    "KodiakCannonSonic",
    # RA2
    "SonicZap",
    # hand-tuned existing grants (renamed, not folded; baseline non-Sonic)
    "TSDisruptor",
    "JapanWaveforce",
    "RA2IonPulseDischarge",
]

# existing warhead family -> suggested Sonic level
# Laser/Railgun/Tesla/Magic/Flame/Chemical are high-energy -> Heavy
# Bullet/Chaingun/Flak small arms -> Light or Medium by base tier
# CannonHE/CannonAP -> by level
WH_TO_SONIC = {
    "^Warhead_Bullet_Light": "Sonic_Light",
    "^Warhead_Bullet_Medium": "Sonic_Medium",
    "^Warhead_Bullet_Heavy": "Sonic_Heavy",
    "^Warhead_CannonAP_Light": "Sonic_Light",
    "^Warhead_CannonAP_Medium": "Sonic_Medium",
    "^Warhead_CannonAP_Heavy": "Sonic_Heavy",
    "^Warhead_CannonHE_Light": "Sonic_Light",
    "^Warhead_CannonHE_Medium": "Sonic_Medium",
    "^Warhead_CannonHE_Heavy": "Sonic_Heavy",
    "^Warhead_Flak_Light": "Sonic_Light",
    "^Warhead_Flak_Medium": "Sonic_Medium",
    "^Warhead_Flak_Heavy": "Sonic_Heavy",
    "^Warhead_MissileAP_Light": "Sonic_Light",
    "^Warhead_MissileAP_Medium": "Sonic_Medium",
    "^Warhead_MissileAP_Heavy": "Sonic_Heavy",
    "^Warhead_MissileHE_Light": "Sonic_Light",
    "^Warhead_MissileHE_Medium": "Sonic_Medium",
    "^Warhead_MissileHE_Heavy": "Sonic_Heavy",
    "^Warhead_Demolition_Light": "Sonic_Light",
    "^Warhead_Demolition_Medium": "Sonic_Medium",
    "^Warhead_Demolition_Heavy": "Sonic_Heavy",
    "^Warhead_Concussion_Light": "Sonic_Light",
    "^Warhead_Concussion_Medium": "Sonic_Medium",
    "^Warhead_Concussion_Heavy": "Sonic_Heavy",
    "^Warhead_Flame_Light": "Sonic_Light",
    "^Warhead_Flame_Medium": "Sonic_Medium",
    "^Warhead_Flame_Heavy": "Sonic_Heavy",
    "^Warhead_Chemical_Light": "Sonic_Light",
    "^Warhead_Chemical_Medium": "Sonic_Medium",
    "^Warhead_Chemical_Heavy": "Sonic_Heavy",
    "^Warhead_Prism_Light": "Sonic_Light",
    "^Warhead_Prism_Medium": "Sonic_Medium",
    "^Warhead_Prism_Heavy": "Sonic_Heavy",
    "^Warhead_Tesla_Light": "Sonic_Light",
    "^Warhead_Tesla_Medium": "Sonic_Medium",
    "^Warhead_Tesla_Heavy": "Sonic_Heavy",
    "^Warhead_Tesla_Super": "Sonic_Heavy",
    "^Warhead_Laser_Light": "Sonic_Light",
    "^Warhead_Laser_Medium": "Sonic_Medium",
    "^Warhead_Laser_Heavy": "Sonic_Heavy",
    "^Warhead_Railgun_Light": "Sonic_Light",
    "^Warhead_Railgun_Medium": "Sonic_Medium",
    "^Warhead_Railgun_Heavy": "Sonic_Heavy",
    "^Warhead_Sniper_Light": "Sonic_Light",
    "^Warhead_Sniper_Medium": "Sonic_Medium",
    "^Warhead_Sniper_Heavy": "Sonic_Heavy",
    # Magic and other high-energy families default to Heavy
    "^Warhead_Magic_Light": "Sonic_Light",
    "^Warhead_Magic_Medium": "Sonic_Medium",
    "^Warhead_Magic_Heavy": "Sonic_Heavy",
}


# Old full-stack family templates (same list as phase_b_survey.py)
OLD_FAMILIES = {
    "^SmallArms", "^Chaingun", "^TankDestroyerCannon", "^MediumCannon",
    "^HeavyCannon", "^LightMissile", "^MediumMissile", "^HeavyMissile",
    "^FlakWeapon", "^HeavyAAWeapon", "^Grenade", "^ShrapnelWeapon",
    "^HeavyBomb", "^LaserWeapon", "^RailgunWeapon", "^TeslaWeapon",
    "^TeslaChargedWeapon", "^SwordWeapon", "^ArrowWeapon", "^MagicWeapon",
    "^LightFlameWeapon", "^MediumFlameWeapon", "^HeavyFlameWeapon",
    "^LightChemicalWeapon", "^MediumChemicalWeapon", "^HeavyChemicalWeapon",
    "^NuclearWarhead", "^SniperWeapon", "^LightArms",
}


def old_families_of(rs, wname):
    """Return the old full-stack family templates the weapon directly inherits."""
    local = rs.weapon(wname)
    if local is None:
        return []
    out = []
    for c in local.children:
        if c.key == "Inherits" or c.key.startswith("Inherits@"):
            val = (c.value or "").strip()
            if val in OLD_FAMILIES:
                out.append(val)
    return out


# old full-stack family -> suggested Sonic level
OLD_TO_SONIC = {
    "^SmallArms": "Sonic_Light",
    "^Chaingun": "Sonic_Medium",
    "^LightArms": "Sonic_Light",
    "^Grenade": "Sonic_Light",
    "^ShrapnelWeapon": "Sonic_Medium",
    "^HeavyBomb": "Sonic_Heavy",
    "^MediumCannon": "Sonic_Medium",
    "^HeavyCannon": "Sonic_Heavy",
    "^TankDestroyerCannon": "Sonic_Light",
    "^LightMissile": "Sonic_Light",
    "^MediumMissile": "Sonic_Medium",
    "^HeavyMissile": "Sonic_Heavy",
    "^FlakWeapon": "Sonic_Medium",
    "^HeavyAAWeapon": "Sonic_Heavy",
    "^LightFlameWeapon": "Sonic_Light",
    "^MediumFlameWeapon": "Sonic_Medium",
    "^HeavyFlameWeapon": "Sonic_Heavy",
    "^LightChemicalWeapon": "Sonic_Light",
    "^MediumChemicalWeapon": "Sonic_Medium",
    "^HeavyChemicalWeapon": "Sonic_Heavy",
    "^TeslaWeapon": "Sonic_Heavy",
    "^TeslaChargedWeapon": "Sonic_Heavy",
    "^LaserWeapon": "Sonic_Heavy",
    "^RailgunWeapon": "Sonic_Heavy",
    "^MagicWeapon": "Sonic_Heavy",
    "^SniperWeapon": "Sonic_Light",
    "^ArrowWeapon": "Sonic_Light",
    "^SwordWeapon": None,
    "^NuclearWarhead": None,
}


def main():
    model = Model()
    rs = model.rs
    rows = []
    for wname in CANDIDATES:
        local = rs.weapon(wname)
        if local is None:
            rows.append({
                "weapon": wname,
                "file": None,
                "found": False,
                "old_families": [],
                "proposed_sonic": None,
                "reason": "weapon not found in resolved ruleset",
                "needs_confirm": True,
            })
            continue
        olds = old_families_of(rs, wname)
        # if the weapon already directly inherits a Sonic warhead template, skip
        has_sonic = any(((c.value or "").strip()).startswith("^Warhead_Sonic")
                        for c in local.children
                        if (c.key == "Inherits" or c.key.startswith("Inherits@")))
        proposed = None
        for old in olds:
            if old in OLD_TO_SONIC:
                proposed = OLD_TO_SONIC[old]
                break
        needs_confirm = has_sonic or proposed is None
        if has_sonic:
            reason = "already inherits a Sonic-related template"
        elif not olds:
            reason = "no old full-stack family Inherits found"
        elif proposed is None:
            reason = f"old family {olds[0]} has no Sonic mapping"
        else:
            reason = f"based on old family {olds[0]}"
        rows.append({
            "weapon": wname,
            "file": str(Path(local.file).relative_to(ROOT)) if local is not None else None,
            "found": True,
            "old_families": olds,
            "proposed_sonic": proposed,
            "proposed_inherits": f"Inherits@sonic: ^Warhead_Sonic_{proposed.split('_')[-1]}" if proposed else None,
            "reason": reason,
            "needs_confirm": needs_confirm,
        })

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    jpath = OUT_DIR / "propose_sonic_mapping.json"
    jpath.write_text(json.dumps(rows, indent=2), encoding="utf-8")

    mpath = OUT_DIR / "propose_sonic_mapping.md"
    lines = ["# Proposed Sonic-family adoption mapping", ""]
    for r in rows:
        check = "✅" if not r["needs_confirm"] else "⚠️ needs confirm"
        lines.append(f"## `{r['weapon']}` {check}")
        if r["file"]:
            lines.append(f"- File: `{r['file']}`")
        if r["old_families"]:
            lines.append(f"- Old families: {', '.join(r['old_families'])}")
        if r["proposed_sonic"]:
            lines.append(f"- Proposed: add `{r['proposed_inherits']}`")
        lines.append(f"- Reason: {r['reason']}")
        lines.append("")
    mpath.write_text("\n".join(lines), encoding="utf-8")

    print(f"Wrote {jpath} and {mpath}")
    print(f"Total candidates: {len(rows)}")
    print(f"  ready to apply (needs_confirm=False): {sum(1 for r in rows if not r['needs_confirm'])}")
    print(f"  needs confirm: {sum(1 for r in rows if r['needs_confirm'])}")


if __name__ == "__main__":
    main()
