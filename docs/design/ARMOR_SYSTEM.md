# ARMOR SYSTEM — the weapon-class Versus law (canonical, 2026-07-19)

_How every weapon template's Versus table is constructed. This is THE
reference for creating or renaming weapon templates (MEGAPLAN §3).
Confirmed against the live weapons/weapons.yaml, not guessed._

## The two orthogonal axes

Every damage weapon's Versus table is fully determined by two choices:

1. **LEVEL (power)** = the STEP SIZE by which effectiveness falls from
   100 down the armor ladder. Bigger step = steeper falloff =
   specialist; smaller step = flatter = good-against-everything.
2. **PROFILE (role)** = the ORDER of the 17 armor types — which armor
   sits at 100 (the best target) and the descending sequence after it.
   The step is constant; the ORDER is the weapon's identity.

## LEVEL — the step law (main SpreadDamage warhead)

| level | step | runs 100 → | Shield (special) |
|---|---|---|---|
| **Light** | **6** | **10** | **110** |
| **Medium** | **5** | **25** | **125** |
| **Heavy** | **4** | **40** | **140** |

- 16 non-Shield armor types, so 100 down in 15 steps to the floor:
  light 100,94,88,…,10 · medium 100,95,…,25 · heavy 100,96,…,40.
- **Shield = top + floor** — the one unifying rule for BOTH warheads.
  Main warhead: top 100 + floor (10/25/40) = **110 / 125 / 140**.
  Shield is the only main value above 100; heavier weapons hit shields
  hardest.
- Flatter = tougher target set survives less: a HEAVY weapon never
  drops below 40% vs anything (relatively universal); a LIGHT weapon
  drops to 10% vs its worst target (hard specialist).
- DESIGN §12 notes the ladder extends to steps 3/2/1 for
  superheavy/superweapon bands — the L/M/H (6/5/4) triple is the
  standard set; confirm the heavier bands with the maintainer before
  using them.

## The PERCENTAGE warhead (HealthPercentageDamage) — its own scale

Each weapon pairs its main warhead with a HealthPercentageDamage
warhead that ALSO ladders down by step 1, in a level-dependent window:

| level | % top → floor | Shield % (= top + floor) |
|---|---|---|
| Light | 16 → 1 | **17** |
| Medium | 20 → 5 | **25** |
| Heavy | 25 → 10 | **35** |

Same armor ORDER as the main warhead; step always 1. Shield obeys the
SAME `top + floor` law (16+1, 20+5, 25+10). Confirmed against every
heavy-class weapon in the file (HeavyMissile/HeavyBomb/Laser/Railgun/
Tesla/Heavy Flame+Chemical all show Shield% 35). This is the "extra %
of max-HP" chip damage that keeps high-HP targets killable.

## PROFILE — the standard armor orderings (which type is at 100)

The 17 armor types: Shield, None, Flak, Plate, Heroic, Scout, Fighter,
Wood, Light, Bomber, Steel, Medium, Helicopter, Concrete, Heavy,
Spaceship, Superheavy. The profile picks the descending order:

| profile | leads with (100) → … | good vs | example today |
|---|---|---|---|
| **anti-infantry** | None, Flak, Plate, Heroic… | infantry | ^SmallArms (light), ^Chaingun (medium) |
| **universal** | Scout, None, Wood, Light, Flak, Concrete, Medium… | everything (gentle slope) | ^ShrapnelWeapon (medium) |
| **HE / anti-vehicle** | Scout, Wood, Light, Concrete, Medium… | light-medium vehicles + structures | ^MediumCannon (medium), ^HeavyCannon (heavy) |
| **AP / anti-heavy** | Superheavy, Heavy, Medium, Light… (inverted) | heavy + superheavy | ^TankDestroyerCannon (light-step, AP order) |
| **anti-structure** | Wood, Concrete… | buildings | ^HeavyBomb |
| **AA** | Fighter, Bomber, Spaceship, Helicopter… | aircraft | ^FlakWeapon (medium) |

## Consequences for the new templates (MEGAPLAN §3)

Creating `CannonAP_Light/Medium/Heavy` = the AP armor ORDER with step
6/5/4. `MissileAA_Heavy` = the air ORDER with step 4. The "good vs
everything" explosion = the UNIVERSAL order at whichever step gives the
right flatness (medium step 5 = today's Shrapnel; heavy step 4 = even
flatter). So:

- The FAMILY name = the PROFILE (armor order).
- The Light/Medium/Heavy suffix = the STEP (6/5/4), i.e. power/flatness.
- A tool generates the whole 17-row Versus table from (order, step) —
  no hand-typing; every template stays law-conformant by construction.

## Special cases to confirm with the maintainer

- HAZMAT armor overrides (e.g. ^ShrapnelWeapon HAZMAT:50) — per-family
  exceptions, not part of the step ladder.
- Exact heavy-% Shield value and the step 3/2/1 superheavy bands.
- Which profiles need a friendly-fire warhead variant (halved values).
