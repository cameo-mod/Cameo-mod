#!/usr/bin/env python3
"""LANE-5: Gather stats for the 222 units with no class, grouped by subtype.

For each subtype, report the member count and the observed HP / speed /
damage-per-shot / range / cost spread of its members.
"""
import json, glob, os, statistics
from collections import defaultdict

TARGET_SUBTYPES = {
    "Helicopter", "Bomber", "Harvester", "Fighter", "ScoutShip",
    "Spaceship", "ArtilleryShip", "BattleShip", "UnarmedTransportHelicopter",
}

def _num(d):
    if isinstance(d, dict):
        v = d.get('v')
        try:
            return float(v) if v is not None else None
        except (TypeError, ValueError):
            return None
    if isinstance(d, (int, float)):
        return float(d)
    return None

def _hex_to_num(s):
    """Parse hex string like '5c0' to int."""
    if s is None:
        return None
    if isinstance(s, (int, float)):
        return float(s)
    try:
        return float(int(str(s), 16))
    except (ValueError, TypeError):
        try:
            return float(s)
        except (ValueError, TypeError):
            return None

def _max_range(armaments):
    ranges = []
    for arm in armaments:
        if not isinstance(arm, dict):
            continue
        r = arm.get('range')
        if r is not None:
            try:
                ranges.append(float(r))
            except (TypeError, ValueError):
                pass
    return max(ranges) if ranges else None

def _max_damage(armaments):
    """Maximum single-shot damage across all armaments and warheads."""
    max_dmg = 0
    found = False
    for arm in armaments:
        if not isinstance(arm, dict):
            continue
        for wh in arm.get('damage_warheads', []):
            if not isinstance(wh, dict):
                continue
            d = wh.get('damage')
            if d is not None:
                try:
                    dmg = float(d)
                    if dmg > max_dmg:
                        max_dmg = dmg
                    found = True
                except (TypeError, ValueError):
                    pass
    return max_dmg if found else None

def _sum_damage(armaments):
    """Sum of all warhead damages (total damage per volley)."""
    total = 0
    found = False
    for arm in armaments:
        if not isinstance(arm, dict):
            continue
        for wh in arm.get('damage_warheads', []):
            if not isinstance(wh, dict):
                continue
            d = wh.get('damage')
            if d is not None:
                try:
                    total += float(d)
                    found = True
                except (TypeError, ValueError):
                    pass
    return total if found else None

def collect_units():
    units_by_subtype = defaultdict(list)
    ledgers = glob.glob('docs/balance/*.json')
    for lf in sorted(ledgers):
        if 'derived' in lf or 'class_anchors' in lf:
            continue
        with open(lf, encoding='utf-8') as f:
            data = json.load(f)
        ledger_name = data.get('ledger', os.path.basename(lf).replace('.json', ''))
        for sec_name, sec in data.get('sections', {}).items():
            for actor, info in sec.items():
                if not isinstance(info, dict):
                    continue
                design = info.get('design', {})
                sub = design.get('subtype', '')
                if sub in TARGET_SUBTYPES:
                    # Speed: try speed_air, then speed
                    speed = _num(info.get('speed_air', {}))
                    if speed is None:
                        speed = _num(info.get('speed', {}))
                    # Sight: parse hex
                    sight = _hex_to_num(info.get('sight', {}).get('v'))
                    unit = {
                        'actor': actor,
                        'ledger': ledger_name,
                        'subtype': sub,
                        'buildable': info.get('buildable', False),
                        'hp': _num(info.get('hp', {})),
                        'cost': _num(info.get('cost', {})),
                        'speed': speed,
                        'sight': sight,
                        'range': _max_range(info.get('armaments', [])),
                        'max_dmg': _max_damage(info.get('armaments', [])),
                        'volley': _sum_damage(info.get('armaments', [])),
                        'armor': info.get('armor', {}).get('v', ''),
                        'class_anchor': design.get('class_anchor'),
                        'unit_class': design.get('unit_class'),
                        'armaments': len(info.get('armaments', [])),
                    }
                    units_by_subtype[sub].append(unit)
    return units_by_subtype

def fmt_stats(values):
    nums = [v for v in values if v is not None and v > 0]
    if not nums:
        return "n/a"
    return f"{min(nums):.0f} / {statistics.median(nums):.0f} / {max(nums):.0f}"

def main():
    units_by_subtype = collect_units()

    print("# LANE-5: Air/Naval/Economy Class Proposal — Data\n")
    total = sum(len(v) for v in units_by_subtype.values())
    print(f"## Units by subtype ({total} total with no class)\n")

    for sub in ["Helicopter", "UnarmedTransportHelicopter", "Bomber", "Fighter",
                "Spaceship", "ScoutShip", "ArtilleryShip", "BattleShip", "Harvester"]:
        units = units_by_subtype.get(sub, [])
        if not units:
            print(f"### {sub}: 0 units\n")
            continue

        buildable = [u for u in units if u['buildable']]
        print(f"### {sub} — {len(units)} total ({len(buildable)} buildable)\n")

        hps = [u['hp'] for u in units]
        costs = [u['cost'] for u in units]
        speeds = [u['speed'] for u in units]
        sights = [u['sight'] for u in units]
        ranges = [u['range'] for u in units]
        dmgs = [u['max_dmg'] for u in units]
        volleys = [u['volley'] for u in units]

        print(f"| stat | min / median / max |")
        print(f"|---|---|")
        print(f"| HP | {fmt_stats(hps)} |")
        print(f"| Cost | {fmt_stats(costs)} |")
        print(f"| Speed | {fmt_stats(speeds)} |")
        print(f"| Sight (cell) | {fmt_stats([s/1024 if s else None for s in sights])} |")
        print(f"| Max range | {fmt_stats(ranges)} |")
        print(f"| Max dmg/shot | {fmt_stats(dmgs)} |")
        print(f"| Volley dmg | {fmt_stats(volleys)} |")
        print()

        # List members
        print(f"| actor | ledger | build | HP | cost | speed | range | dmg/shot |")
        print(f"|---|---|---|---|---|---|---|---|")
        for u in sorted(units, key=lambda x: (x['ledger'], x['actor'])):
            b = "Y" if u['buildable'] else "-"
            hp = f"{u['hp']:.0f}" if u['hp'] else "?"
            cost = f"{u['cost']:.0f}" if u['cost'] else "?"
            speed = f"{u['speed']:.0f}" if u['speed'] else "?"
            rng = f"{u['range']:.0f}" if u['range'] else "?"
            dmg = f"{u['max_dmg']:.0f}" if u['max_dmg'] else "?"
            print(f"| {u['actor']} | {u['ledger']} | {b} | {hp} | {cost} | {speed} | {rng} | {dmg} |")
        print()

if __name__ == '__main__':
    main()
