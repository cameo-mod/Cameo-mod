# Playtest triage — B1/B2/B6/B7 diagnosis (Devin-Integrator)

Base: frozen master `3d99405bd` (playtest push, engine pin `0a3f77dbe1a`).
Scope: diagnosis + proposed fixes only; review before merge. No master/shared-ref pushes.
Evidence: `PLAYTEST_TRIAGE_2026-10-08.md`, release diff vs `playtest-20260709`, engine source read at the pinned tree.

## B1 — Exorcist orbs never deal damage — CONFIRMED regression, root cause found

`japan_exorcistoitank` fires `OIHakureiring` / `OIHakureiring2` (`Armament@MAIN`/`@AntiEpic`,
`Turret: main`; turret wiring, armament names, projectile/trail sequences all verified intact —
the missile launches but detonates mid-air).

Commit `b7f6d436865` ("Implement missile range and proximity feel-test defaults") removed
`RangeLimit: 33333` / `22222` and `CloseEnough: 333` from the three inline `Projectile: Missile`
definitions (`OIHakureiring`, `OIHakureiring2`, infantry `Hakureiring`). These weapons do **not**
inherit `^Projectile_Missile_*` (which received `RangeLimitPercent: 150` +
`CloseEnoughFromSpeed: true`), so they now resolve `RangeLimitPercent: 0` →
`ProjectileInfoUtils.EffectiveRangeLimit` = weapon range (5394) measured as **path length**
(`Missile` accumulates `distanceCovered += speed` per tick). The launch is a fixed 111° arc —
path length far exceeds slant range — so `distanceCovered > rangeLimit` triggers freefall and
`ExplodeWhenEmpty` (default true) detonates the orb in the air short of the target.

`tools/balance/rangelimit_removed_2026-10-05.tsv` shows ~250 removals; the inline custom missiles
never got the new template defaults. Largest lost fuel budgets: `SkyHawkArrows` ~8.2×,
`OIHakureiring` ~6.18×, `Hakureiring` ~5×, `OIHakureiring2` ~4.12×.

**Proposed fix:** restore authored `RangeLimit: 33333` / `22222` / `CloseEnough: 333` verbatim on
the three Hakureiring-family weapons (or `RangeLimitPercent` equivalents ~618/412/500 if the
maintainer prefers the new convention). Recommend a sweep of `rangelimit_removed_2026-10-05.tsv`
for other steep-arc/inline missiles (e.g. `SkyHawkArrows`) before merge — same defect class.

## B7 — Japanese superweapon doesn't damage aircraft — CONFIRMED regression

Chain (`mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml`):
`MagicOrbHailstormActivate` → `FireFragment` → `MagicOrbHailstormSWDamage` →
`SpawnSmokeParticle` → `MagicOrbHailstormSpawner`/`2` → `FireShrapnel` → `MagicOrbSpreaderProjectile`/`2`
→ `AthenaProjectile` → `FireFragment` → `MagicOrb`/`MagicOrb2`. Every filter warhead in the spawn
chain still carries `Ground, Water, Air`.

The payload warheads lost Air in the 3-way split:
- `Warhead@Tesla_Heavy: AreaDamage` — explicit `ValidTargets: Ground, Water` (lines ~962/1121)
- `Warhead@Tesla_Heavy_ExtraDamage: SpreadDamage` — `ValidTargets: Ground, Water`
- `Warhead@EMPUnit: AffectsIntegrity` — no `ValidTargets` → default `Ground, Water`; also lost
  `InvalidTargets: Shielded`

Release `playtest-20260709` resolved these via `^TeslaWeapon`/`^MagicWeapon`
(warhead bodies `ValidTargets: Ground, Water, Air`) and `^EMPDamage`
(`ValidTargets: Vehicle, Ship, Air, Cyborg, Defense, Building, Epic`, `InvalidTargets: Shielded`).
`AreaDamageWarhead.DoImpact` filters victims by `IsValidAgainst` → aircraft now take 0 damage.

Blast radius: ALL `^Warhead_Magic_*` (13933–14285) and `^Warhead_Tesla_*` (15575–16157, plus
`Tesla_Heavy_Flat`/`CannonTesla_*`) templates in `mods/cameo/weapons/weapons.yaml` resolve
`Ground, Water`. `MissileTesla_*`/`BulletTesla_*` correctly kept Air. Some weapons carry explicit
opt-outs (`OIHakureiring` `InvalidTargets: Air`) or opt-ins (`OIHakureiring2`
`ValidTargets: Air, Ground, Water`), so the conversion was partially deliberate — a blanket
template restore could overshoot.

**Proposed fix (release-faithful, scoped):** on `MagicOrb` + `MagicOrb2` set
`ValidTargets: Air, Ground, Water` on `Warhead@Tesla_Heavy` and `Warhead@Tesla_Heavy_ExtraDamage`,
and on `Warhead@EMPUnit` set `ValidTargets: Vehicle, Ship, Air, Cyborg, Defense, Building, Epic` +
`InvalidTargets: Shielded`. Then run `review_resolve_diff.py`/extract_stats to enumerate sibling
weapons that lost air unintentionally; decide template-level vs per-weapon restoration with the
maintainer.

## B2 — "multiple superweapons built" — proximate change identified; mode-dependent

Commit `ac4c8683f` ("Multiple superweapons extend to secondaries", Aug 29) made two changes to
`^Superweapon`/`^PrimarySuperweapon` (`mods/cameo/rules/defaults.yaml`):
- **removed `Buildable.BuildLimit: 1`** — the release's unconditional hard cap on every superweapon
- moved `ProvidesPrerequisite@swlimit` (`RequiresPrerequisites: global-swlimit`) from
  `^PrimarySuperweapon` to `^Superweapon` (extends locks to secondaries)

Locks now engage only via `!<name>_swlimit` buildable prereqs, which are provided only when the
player tech tree has `global-swlimit`. `global-swlimit` is granted by lobby Tech Level
`no-superweapons` / `superweapons` ("Limited Superweapons"); the **default tech level
`unrestricted` ("Unlimited Superweapons") provides `techlevel.superweapons` without
`global-swlimit`** → all `*_swlimit` locks inert → multiples allowed.

Full audit: every superweapon building (Japan shrine, RA1/RA2 silos, weather control, chrono/iron
curtains, psychic dominator, genetic mutator, RA2M/SC/TS superweapons, TD/TS plug hosts) has an
intact `!x` + `ProvidesPrerequisite@swlimit` pair — no missing locks found.

**Verdict:** if the playtest lobby ran the default `unrestricted` tech level, B2 is the
deliberate new behavior of `ac4c8683f` (release capped at 1 unconditionally, so it *is* a
behavior change vs release — but intentional per the commit). If the playtest ran "Limited
Superweapons", no static break exists — request the replay's `techlevel` lobby option to
disambiguate before proposing a yaml change. Residual hole regardless of mode: `PlacePlug` never
re-checks `Buildable` prereqs at placement (pre-existing engine behavior), so plugs produced
before an attach can be placed on additional hosts.

## B6 — ion cannon plug-in buildable after owned — same mechanism as B2; likely WAD under default

Chain verified end-to-end (`mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/buildings.yaml`):
- uplink `td_gdi_ioncannonuplink` (`^BuildingPlug`): `Prerequisites: ~td_gdi_constructionyard,
  td_gdi_advancedcommunicationscenter, !ionc, ~techlevel.superweapons`
- ACC (`^PrimarySuperweapon` + `^BuildingPlugProducer`): `Pluggable` maps plug type
  `td_gdi_advancedcommunicationscenter` → condition `ionc` (`Requirements: !build-incomplete && !ionc`)
- on attach, `Pluggable.EnablePlug` grants `ionc` on the ACC (engine `Pluggable.cs`)
- `ProvidesPrerequisite@swlimit` (`Prerequisite: ionc`, `RequiresCondition: ionc`, inherited
  `RequiresPrerequisites: global-swlimit`) then provides the player-level `ionc` prereq →
  `!ionc` blocks further uplinks — in Limited mode only.

The release carried the identical gating (`eye.ionc`/`EYE` blocks, same `global-swlimit` gate on
the template). Additionally the ACC was already BuildLimit-exempt (`-BuildLimit:`) in release, so
B6 is **not a regression**: in `unrestricted` mode the uplink stayed re-buildable there too.
In Limited mode the lock is statically correct; remaining hole is the same pre-existing
`PlacePlug` race (two pre-produced uplinks can be attached to two ACCs — per-host `!ionc` is
independent).

## Requested confirmations / next steps

1. Replay `techlevel` option from the Bots Suck replays → settles B2/B6 classification
   (expected `unrestricted` → both are intended behavior; document the default change for the
   playtest notes).
2. Approval to prepare fix branch(es): B1 (RangeLimit/CloseEnough restore, 3 weapons + TSV sweep)
   and B7 (scoped ValidTargets restore on `MagicOrb`/`MagicOrb2` + resolved-diff audit for
   siblings). Both are small yaml diffs; boot-gate + audit suite per workflow before "ready".
3. B2/B6: no yaml change proposed unless the maintainer wants a cap in `unrestricted` mode or the
   playtest ran `superweapons`. If a cap is desired in unlimited mode, the correct fix is a
   policy decision (restore `BuildLimit: 1` on `^Superweapon` would kill the new feature).
