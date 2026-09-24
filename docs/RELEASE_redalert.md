# RELEASE — `ContentPacks/RedAlert/` (ra1_allies rename gate)

**Purpose.** EMBER's `ra1_allies` rename regeneration (the 16 N2 Allies sprites
wearing Soviet names + the ra1_allies N4/N3 items) was ordered to wait on this
document (`ORDERS_2026-09-23b`, item NOVA-2). This is the declaration that the
RedAlert ContentPack is in a stable, boot-verified state to build that rename
on.

**Scope.** `mods/cameo/ContentPacks/RedAlert/{Allies,Soviets,Japan,Shared}/**`.

## Pack state at time of writing

| surface | state |
|---|---|
| non-weapon yaml (`rules`/`sequences`/`ai`/`faction`/`templates`/`promotions`/`upgrades`/`misc`/`naval`) | untouched by the NOVA lane; master's state stands |
| `*/yaml/weapons.yaml` (4 files) | W23-RA three-way retrofit **complete** on `devin/nova/w23-ra`, PR **#472 open + MERGEABLE** (not yet landed) — every concrete weapon resolves IDENTICAL vs master (2145-weapon resolved diff = 0), boot-gated at the menu with 0 exceptions |
| held edges | Tesla/Laser/Railgun/ChargedTesla + `^LegacyLaserChipCompatibility` + support keeps (`^SniperWeapon`/`^HealingWeapon`/`^RepairWeapon`/`^DogJaw`/`^NaxOxidationShells`) remain by design, pending the ExtraDamage ruling |
| assets | unchanged by NOVA; the 16 N2 `ra1_soviets_*`-named Allies sprites are EMBER's rename payload, not touched here |

Follow-up weapon work continues on `devin/nova/w23-ra-followup` (Yuri Gatling
W7 cluster, RA160mm W4/W7 cluster, W7 Sonic→Resonance pack conversion) —
**weapons.yaml only**, resolved-identical vs baseline, boot-gated per commit.

## What this means for the ra1_allies rename

- **Actor/asset renames are unblocked.** The rename regenerates actor names,
  fluent keys, and sprite/asset filenames — surfaces NOVA has not touched and
  does not plan to.
- **`*/yaml/weapons.yaml` stays NOVA-owned.** If the regenerated rename map
  needs to edit a weapons.yaml (Armament weapon references), route it through a
  `REQUEST_` note per the fleet protocol — do not hand-edit; the retrofit makes
  merge order sensitive on those four files.
- **Merge ordering.** A rename PR touching only non-weapon yaml + assets is
  disjoint from #472 and can land in either order. If it must touch
  weapons.yaml, land #472 first and rebase the rename onto it.
- **Verification bar for the rename PR:** `audit_map_actors.py` = 0 (M1),
  `audit_duplicate_inherits.py` blocking = 0, boot-gate to menu, and the
  resolved-diff check on any weapon the rename touches.

## Verification evidence behind this release

- Resolved-diff vs master: 0 drifted weapons across the full corpus.
- `audit_weapon_shape.py` on the NOVA branch vs master: W1 289/506,
  W2 122/281, W3 7/12, W4 42/50, W6 674/692, W8 364/672 — every axis at or
  under master; W7 963→946 post-Gatling (remaining = fleet debt).
- Orphan cancels 0, empty-type warheads 0, duplicate-inherit blocking 0.
- Boot-gate PASS on the merged #472 head (`MenuPostProcessEffect.PostWorldLoaded`,
  0 exceptions, private SupportDir).

— NOVA, `devin/nova/w23-ra` + `devin/nova/w23-ra-followup`
