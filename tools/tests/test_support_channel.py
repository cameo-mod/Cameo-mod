"""THE TWO CHANNELS — offensive damage vs support throughput.

⛔ THE BUG THIS PINS. A negative `Damage` HEALS — that is the engine's
convention — and `formula.spread_damage_sum` used to add it straight into the
offensive total. Eight actors therefore priced as if they SHOT BACKWARDS:

    cabal_engineer -650   tkm_battlebus -600   futuretech_repairdroid -508
    tkm_engineer   -397   ra1_allies_mechanic -357   terran_medic -183
    ra1_allies_medic -40  ts_gdi_medic -40

`support` and `line_breaker` could not be priced at all, and they were two of
the only three classes `band_granularity.py` flagged as non-bell-shaped.

⛔ AND THE OBVIOUS FIX IS THE WRONG ONE. A tag whitelist (`HealingWeapon`,
`RepairWeapon`, …) looks natural and fails for exactly the reason `formula.py`
already documents about `smallarms`: a literal is something a migration renames
out from under you. Measured on this tree, **7 of 160 negative warheads carry a
generic tag** — `1Dam` on the five WC2 paladin/priest heals, `Percentage` on two
Tesla charges — so a name filter would have priced five healers as combat units.
The SIGN cannot be renamed, and these tests assert the sign is what decides.
"""

from __future__ import annotations

import pathlib
import sys

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import formula  # noqa: E402

HEAL = [{"damage": "-2000", "tag": "HealingWeapon", "type": "SpreadDamage"},
        {"damage": "-1000", "tag": "ExtraHealing", "type": "SpreadDamage"}]
# ⭐ The generically-tagged heals a name filter misses. Real actors, real tags.
ODD_HEAL = [{"damage": "-4025", "tag": "1Dam", "type": "SpreadDamage"}]
COMBAT = [{"damage": "5000", "tag": "Bullet_Light", "type": "SpreadDamage"}]


# --------------------------------------------------------------------------------------
# 1. Neither channel can ever go negative. This is the whole bug.
# --------------------------------------------------------------------------------------

@pytest.mark.parametrize("whs", [HEAL, ODD_HEAL, COMBAT, [], None])
def test_neither_channel_is_ever_negative(whs):
    assert formula.spread_damage_sum(whs) >= 0
    assert formula.support_throughput_sum(whs) >= 0


def test_a_pure_healer_has_zero_OFFENSIVE_damage_and_real_support_throughput():
    assert formula.spread_damage_sum(HEAL) == 0
    assert formula.support_throughput_sum(HEAL) == 3000


def test_a_combat_weapon_has_zero_SUPPORT_throughput():
    assert formula.spread_damage_sum(COMBAT) == 5000
    assert formula.support_throughput_sum(COMBAT) == 0


# --------------------------------------------------------------------------------------
# 2. The properties a fix could satisfy accidentally and still be wrong.
# --------------------------------------------------------------------------------------

def test_adding_a_heal_weapon_CANNOT_REDUCE_offensive_dps():
    """⛔ THE CONTRACT, stated as the thing that used to fail. Bolting a medic
    beam onto a tank must not make the tank read as a weaker shooter."""
    before = formula.spread_damage_sum(COMBAT)
    after = formula.spread_damage_sum(COMBAT + HEAL)
    assert after >= before
    assert after == before          # and in fact it must not move at all


def test_adding_a_combat_weapon_CANNOT_REDUCE_support_throughput():
    before = formula.support_throughput_sum(HEAL)
    assert formula.support_throughput_sum(HEAL + COMBAT) == before


def test_the_two_channels_PARTITION_the_main_warheads():
    """Together they must account for every main warhead exactly once — no
    warhead counted twice, none silently dropped. A `max(0, ...)` patch would
    pass the sign tests above and fail this one, because the healing would
    simply vanish instead of moving to the other channel."""
    mixed = COMBAT + HEAL
    off = formula.spread_damage_sum(mixed)
    sup = formula.support_throughput_sum(mixed)
    magnitudes = sum(abs(float(w["damage"]))
                     for w in formula.main_spread_warheads(mixed))
    assert off + sup == pytest.approx(magnitudes)


# --------------------------------------------------------------------------------------
# 3. Classification is by SIGN, at the ARMAMENT grain.
# --------------------------------------------------------------------------------------

def test_channel_is_decided_by_the_SIGN_not_the_tag_name():
    """⭐ `1Dam` is a real tag on a real heal (`wc2paladinhealing`, −4025). A tag
    whitelist would classify it as combat. Anyone replacing the sign test with a
    name test fails here."""
    assert formula.armament_channel(ODD_HEAL) == "support"
    assert formula.spread_damage_sum(ODD_HEAL) == 0
    assert formula.support_throughput_sum(ODD_HEAL) == 4025


@pytest.mark.parametrize("whs,want", [(HEAL, "support"), (ODD_HEAL, "support"),
                                      (COMBAT, "offensive"), ([], "empty"),
                                      (None, "empty")])
def test_armament_channel(whs, want):
    assert formula.armament_channel(whs) == want


# --------------------------------------------------------------------------------------
# 4. The write-back guard — a data-loss bug one careless apply away.
# --------------------------------------------------------------------------------------

def test_distribute_damage_REFUSES_a_support_armament():
    """⛔ `spread_damage_sum` reads 0 for a healer, so a caller that round-trips
    "read the total, redistribute it" would overwrite `Damage: -2000` with 0 and
    DELETE the heal — invisible in a diff of numbers. It must refuse loudly."""
    with pytest.raises(ValueError, match="SUPPORT armament"):
        formula.distribute_damage(0, HEAL)
    with pytest.raises(ValueError, match="SUPPORT armament"):
        formula.distribute_damage(1000, HEAL)


def test_distribute_damage_still_works_on_a_combat_armament():
    """The guard must not have narrowed the normal path."""
    out = formula.distribute_damage(5000, COMBAT)
    assert out and all(v > 0 for v in out.values())


# --------------------------------------------------------------------------------------
# 5. The premise, re-checked against the real ledger.
# --------------------------------------------------------------------------------------

def test_no_priced_actor_in_the_tree_has_negative_dps():
    """⭐ The end-to-end assertion. Before the fix this found 8 actors."""
    sys.path.insert(0, str(ROOT / "tools" / "balance"))
    import check_band as cb
    bad = []
    for _fn, actor, u, du in cb.collect({}):
        inp = cb.unit_inputs(u, du)
        if inp is not None and inp[3] < 0:
            bad.append((actor, inp[3]))
    assert bad == [], f"actors priced with negative dps: {bad}"


def test_no_armament_in_the_tree_MIXES_the_two_channels():
    """⛔ THE PREMISE THAT LICENSES ARMAMENT-GRAIN CLASSIFICATION. Measured
    2026-08-31: 0 of 2,561 armaments carry both a positive and a negative main
    warhead. If that ever stops being true, `armament_channel` is answering a
    question the data no longer supports and the design needs a ruling — so this
    test failing is a DESIGN signal, not a flake to silence."""
    sys.path.insert(0, str(ROOT / "tools" / "balance"))
    import check_band as cb
    mixed = []
    for _fn, actor, u, _du in cb.collect({}):
        for arm in (u.get("armaments") or []):
            whs = arm.get("damage_warheads") or []
            vals = []
            for w in whs:
                try:
                    vals.append(float(w.get("damage")))
                except (TypeError, ValueError):
                    continue
            if any(v > 0 for v in vals) and any(v < 0 for v in vals):
                mixed.append((actor, arm.get("weapon")))
    assert mixed == [], f"armaments mixing damage and healing: {mixed}"
