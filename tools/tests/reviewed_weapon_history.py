"""Exact, test-only reconciliation of independently reviewed upstream changes.

Never used by a converter. Assert modern values, reverse only the enumerated
delta in a copy, then let the ORIGINAL historical fingerprints check everything
else, including child order. No live data or stored fingerprints are rewritten.
"""
from miniyaml import Node
import json
import pathlib
import sys

ENDPOINT_COHORT = {
    'NaxiHetzerDestroyer', 'NaxiHetzerDestroyer_elite', 'NaxiHetzerDestroyerCorrosion',
    'NaxiAntiTankCannon', 'NaxiAntiTankCannon_elite', 'NaxiAntiTankCannonCorrosion',
    'RA2120xmm', 'RA2120xmm_elite',
}
EXTENDED_ENDPOINTS = {'AlliedTankDestroyerCannon', 'SkyHawkCannon', 'TSLaser90mm', 'TSLaser90mmDep', '2Inch'}
ORDOS_ENDPOINTS = {'120mm_cobra', '120mm_cobra_deploy', '120mm_python', '120mm_python_deploy'}
ALL_ENDPOINTS = ENDPOINT_COHORT | EXTENDED_ENDPOINTS | ORDOS_ENDPOINTS


OWNED_GUNBOAT = 'ra1_allies_gunboat_cannon'


def current_endpoint_name(rules, name):
    """Resolve the exact ownership rename without changing historical fixtures."""
    return OWNED_GUNBOAT if name == '2Inch' and name not in rules.weapons else name


def restore_endpoint_weapon(test, node):
    """Validate the exact h0 replacement, then restore the ordered historical view."""
    from dump_resolved import node_to_obj
    from miniyaml import load_text
    historical_name = '2Inch' if node.key == OWNED_GUNBOAT else node.key
    directory = pathlib.Path(__file__).parent / 'fixtures'
    filename = ('cannonap_extended_before_20260910.json' if historical_name in EXTENDED_ENDPOINTS else
                'cannonap_ordos_before_20260910.json' if node.key in ORDOS_ENDPOINTS else
                'cannonap_endpoint_before_20260910.json')
    fixture = json.loads((directory / filename).read_text(encoding='utf-8'))
    def rebuild(row):
        return Node(row[0], row[1], [rebuild(c) for c in row[2]])
    before = rebuild(fixture['weapons'][historical_name])
    generated = load_text((directory / 'cannonap_continuous_generated.yaml').read_text(encoding='utf-8'))[0]
    old_tag = ('Warhead@CannonAP_LightFlatCompatibility' if node.key in ORDOS_ENDPOINTS else
               'Warhead@CannonAP_Medium' if node.key.startswith('TSLaser90mm') else
               'Warhead@CannonAP_Light')
    main = before.child(old_tag).deep_copy()
    main.key = 'Warhead@CannonAP'
    profile = {'Versus', 'PercentageVersus', 'PercentageVersusLight', 'PercentageVersusHeavy',
               'Spread', 'Heaviness', 'HeavinessMode', 'PercentageScale'}
    main.children = [n for n in main.children if n.key not in profile]
    main.children += [n.deep_copy() for n in generated.child('Warhead@CannonAP').children if n.key in profile]
    main.child('Heaviness').value = '1000' if node.key.startswith('TSLaser90mm') else '0'
    if historical_name == '2Inch':
        main.child('Spread').value = '450'
    test.assertEqual(
        [n.key.replace(old_tag, 'Warhead@CannonAP') for n in before.children
         if n.key.startswith('Warhead@')],
        [n.key for n in node.children if n.key.startswith('Warhead@')], node.key)
    expected = node_to_obj(before)
    expected.pop(old_tag)
    expected['Warhead@CannonAP'] = node_to_obj(main)
    test.assertEqual(expected, node_to_obj(node), node.key)
    before.key = node.key
    return before


def restore_freedom_elite(test, node):
    """Assert the exact new contract before presenting the frozen old converter view."""
    from dump_resolved import node_to_obj
    from miniyaml import load_text
    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / 'balance'))
    from consolidate_rule_driven_energy_ordnance import companion_lines
    fixture = json.loads((pathlib.Path(__file__).parent / 'fixtures' /
                          'freedom_elite_before_20260910.json').read_text(encoding='utf-8'))
    def rebuild(row):
        return Node(row[0], row[1], [rebuild(c) for c in row[2]])
    before = rebuild(fixture['ordered_weapon'])
    expected = node_to_obj(before)
    expected.pop('Warhead@MissileAP_MediumFlatCompatibility')
    expected['Warhead@MissileAP_Medium']['Damage'] = '360000'
    expected['Warhead@MissileAP_Medium']['PercentageScale'] = '0'
    source = before.child('Warhead@MissileAP_Medium')
    text = 'Weapon:\n' + ''.join(companion_lines(source, 'FreedomElitePreservedPercentage',
                                               {'units': 6000, 'denominator': 10000}))
    companion = load_text(text)[0].children[0]
    companion.child('Falloff').value = '100, 50, 0'
    companion.child('Range').value = '0, 32, 33'
    expected[companion.key] = node_to_obj(companion)
    # Removing the compatibility parent leaves every surviving inherited event
    # in its original position; the explicit companion is appended by the elite.
    expected_order = [n.key for n in before.children if n.key.startswith('Warhead@')
                      and n.key != 'Warhead@MissileAP_MediumFlatCompatibility']
    expected_order.append(companion.key)
    test.assertEqual(expected_order,
                     [n.key for n in node.children if n.key.startswith('Warhead@')])
    test.assertEqual(expected, node_to_obj(node))
    return before

# e1ab9bb26 removed duplicate singular bindings; the map already applies100.
CORROSION_CLEANUP = {
    "AsianChemical": ("LightChemicalWeaponPercentage", "MediumChemicalWeaponPercentage", "HeavyChemicalWeaponPercentage"),
    "AsianHarbingerPlasma": ("LightChemicalWeaponPercentage", "MediumChemicalWeaponPercentage"),
    "FutureMechPlasma": ("MediumChemicalWeaponPercentage",),
    "SpecterArtilleryShellUpgrade": ("MediumChemicalWeaponPercentage",),
    "SteelQuantumTurretRail": ("HeavyChemicalWeaponPercentage",),
    "WyvernRockets": ("MediumChemicalWeaponPercentage",),
    "PhobosLaser": ("HeavyChemicalWeaponPercentage",),
    "FutureMechPlasma_elite": ("MediumChemicalWeaponPercentage",),
    "AsianChemical_elite": ("LightChemicalWeaponPercentage", "MediumChemicalWeaponPercentage", "HeavyChemicalWeaponPercentage"),
    "SteelQuantumTurretRail_EMP": ("HeavyChemicalWeaponPercentage",),
}

# path, historical value, authored current value. b905d7679 regenerated coupling
# columns; a92ae850f removed LatinSmoker's trailing medium-cannon inheritance.
FIELD_CHANGES = {
    "AAGunBoatFlak": (
        (("Warhead@Flak_Medium", "Versus", "BLAST"), "58", "57"),
        (("Warhead@Flak_Medium", "Versus", "Shield"), "171", "172"),
    ),
    "AAGunBoatFlak_elite": (
        (("Warhead@Flak_Medium", "Versus", "BLAST"), "58", "57"),
        (("Warhead@Flak_Medium", "Versus", "Shield"), "171", "172"),
    ),
    "RA2FlakTrackAAGun": (
        (("Warhead@Flak_Medium", "Versus", "BLAST"), "58", "57"),
        (("Warhead@Flak_Medium", "Versus", "Shield"), "171", "172"),
    ),
    "RA2FlakTrackAAGun_elite": (
        (("Warhead@Flak_Medium", "Versus", "BLAST"), "58", "57"),
        (("Warhead@Flak_Medium", "Versus", "Shield"), "171", "172"),
    ),
    "RA2FlakTrackGun": (
        (("Warhead@Flak_Medium", "Versus", "BLAST"), "58", "57"),
        (("Warhead@Flak_Medium", "Versus", "Shield"), "171", "172"),
    ),
    "TeslaArmorDischargeArc": (
        (("Warhead@MissileAP_Light", "Versus", "COMPOSITE"), "44", "45"),
    ),
    "TeslaArmorDischargeFragment1": (
        (("Warhead@MissileAP_Light", "Versus", "COMPOSITE"), "44", "45"),
    ),
    "TeslaArmorDischargeFragment2": (
        (("Warhead@MissileAP_Light", "Versus", "COMPOSITE"), "44", "45"),
    ),
    "GrenadeRA": (
        (("Warhead@Demolition_Light", "Versus", "COMPOSITE"), "101", "102"),
    ),
    "ASDFKamikazeExplosion": (
        (("Warhead@Demolition_Heavy", "Versus", "COMPOSITE"), "101", "102"),
        (("Warhead@Demolition_Heavy", "Versus", "Shield"), "177", "178"),
    ),
    "AsianHowitzerCannon": (
        (("Warhead@CannonHE_Heavy", "Versus", "BLAST"), "40", "39"),
        (("Warhead@CannonHE_Heavy", "Versus", "COMPOSITE"), "99", "100"),
        (("Warhead@CannonHE_Heavy", "Versus", "Shield"), "168", "169"),
    ),
    "AsianHowitzerCannon_elite": (
        (("Warhead@CannonHE_Heavy", "Versus", "BLAST"), "40", "39"),
        (("Warhead@CannonHE_Heavy", "Versus", "COMPOSITE"), "99", "100"),
        (("Warhead@CannonHE_Heavy", "Versus", "Shield"), "168", "169"),
    ),
    "ConscriptMolotov": (
        (("Warhead@Flame_Light", "Versus", "COMPOSITE"), "76", "77"),
        (("Warhead@Flame_Light", "Versus", "Shield"), "205", "208"),
    ),
    "TSBusMortar": (
        (("Warhead@Concussion_Medium", "Versus", "COMPOSITE"), "106", "107"),
    ),
    "tkm_trooper_gp25": (
        (("Warhead@Demolition_Light", "Versus", "COMPOSITE"), "101", "102"),
    ),
    "RA2FreedomRocket": (
        (("Warhead@MissileAP_Medium", "Versus", "COMPOSITE"), "44", "45"),
    ),
    "RA2FreedomRocket_elite": (
        (("Warhead@MissileAP_Medium", "Versus", "COMPOSITE"), "44", "45"),
    ),
    "PositronBounce1": (
        (("Warhead@CannonHE_Medium", "Versus", "BLAST"), "40", "39"),
        (("Warhead@CannonHE_Medium", "Versus", "COMPOSITE"), "99", "100"),
    ),
    "PositronBounce2": (
        (("Warhead@CannonHE_Medium", "Versus", "BLAST"), "40", "39"),
        (("Warhead@CannonHE_Medium", "Versus", "COMPOSITE"), "99", "100"),
    ),
    "TS155mm_bluenuke": (
        (("Warhead@Concussion_Medium", "Versus", "COMPOSITE"), "106", "107"),
        (("Warhead@Demolition_Heavy", "Versus", "COMPOSITE"), "101", "102"),
        (("Warhead@Demolition_Heavy", "Versus", "Shield"), "177", "178"),
    ),
    "RA2KirovHowitzerSplash": (
        (("Warhead@Concussion_Medium", "Versus", "COMPOSITE"), "106", "107"),
    ),
    "LatinSmokerCannon": (
        (("Warhead@Concrete", "Damage"), "150", "200"),
        (("Warhead@Effect", "Explosions"), "ra2_medium_explosion", "ra2_large_grey_explosion"),
        (("Warhead@Effect", "ImpactSounds"), "gexp14a.wav", "kaboom15.aud"),
        (("Warhead@EffectAir", "Explosions"), "med_explosion_air", "big_explosion_air"),
        (("Warhead@Glow", "FadeFrames"), "10", "15"),
        (("Warhead@Glow", "Scale"), "0.55", "0.8"),
        (("Warhead@ShieldHit", "Duration"), "10", "12"),
    ),
}


def historical_copy(test, node):
    if node.key in ALL_ENDPOINTS or node.key == OWNED_GUNBOAT:
        node = restore_endpoint_weapon(test, node)
    if node.key == 'RA2FreedomRocket_elite':
        node = restore_freedom_elite(test, node)
    copy = node.deep_copy()
    if node.key == "TSPulseCannon_EMP":
        # 9bfee2b85 removed an unused field from AffectsIntegrity, not damage.
        warhead = copy.child("Warhead@2Con")
        test.assertEqual("AffectsIntegrity", warhead.value)
        test.assertIsNone(warhead.child("Falloff"))
        test.assertEqual("ValidTargets", warhead.children[0].key)
        test.assertEqual("Damage", warhead.children[1].key)
        warhead.children.insert(1, Node("Falloff", "100, 75, 50, 25"))
    for tag in CORROSION_CLEANUP.get(node.key, ()):
        warhead = copy.child("Warhead@" + tag)
        test.assertIsNotNone(warhead, (node.key, tag))
        test.assertEqual(8, len(warhead.children), (node.key, tag))
        test.assertEqual("PhysicalStates", warhead.children[7].key, (node.key, tag))
        test.assertEqual("100", warhead.child("PhysicalStates").get("Corrosion"))
        test.assertIsNone(warhead.child("PhysicalStateName"))
        test.assertIsNone(warhead.child("PhysicalStateScale"))
        warhead.children.extend([Node("PhysicalStateName", "Corrosion"), Node("PhysicalStateScale", "100")])
    for path, before, after in FIELD_CHANGES.get(node.key, ()):
        field = copy
        for key in path:
            field = field.child(key)
            test.assertIsNotNone(field, (node.key, path))
        test.assertEqual(after, field.value, (node.key, path))
        field.value = before
    return copy


class HistoricalView:
    def __init__(self, test, rules):
        self.test, self.rules = test, rules

    def __getattr__(self, key):
        return getattr(self.rules, key)

    def resolve_weapon(self, name):
        return historical_copy(self.test, self.rules.resolve_weapon(name))
