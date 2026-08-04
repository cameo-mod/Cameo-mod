# AreaDamage warhead + the cross-weapon rebalance (design capture, 2026-08-03)

Maintainer design session, 2026-08-03. This doc is the **canonical capture** of the
AreaDamage warhead decision and the wider weapon/warhead/projectile rebalance that
surrounds it. It is not yet threaded into `DESIGN.md` / `ROADMAP.md` (the maintainer had
live uncommitted edits there); fold the binding parts in on the next clean commit pass.

Related: `ARMOR_SYSTEM.md` (Versus/step law), `WEAPON_TYPE_SYSTEM.md`,
`WEAPON_3WAY_SPLIT.md`, `FORMULA_V2.md` (SUM law), memory `cameo-expanding-damage-trait`
(→ now the AreaDamage decision), `cameo-versus-only-in-templates`.

---

## 1. The `AreaDamage` warhead (C# — MOD code, not the engine submodule)

**File:** `OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs` (repo root — the Cameo assembly
lives at the root, NOT in `engine/`; so this is a normal `dotnet build`, NO engine mirror).
Extends `DamageWarhead` (from `OpenRA.Mods.Common.Warheads`), mirrors `SpreadDamageWarhead`'s
spatial pass, and adds the ring/DoT + baked friendly-fire.

**At defaults (`Ticks: 1`, `MaxRadius: 0`) it is byte-behaviour-identical to `SpreadDamage`**
plus baked FF — so it can safely become the universal main-warhead type.

Fields:
- Spatial (same as SpreadDamage): `Spread`, `Falloff[]`, `Range[]`, `DamageCalculationType`.
- `Ticks` (default 1) — number of damage applications. >1 = damage over time.
- `TickDelay` (default 0) — engine ticks between applications.
- `MinRadius` / `MaxRadius` (default 0) — when `MaxRadius>0`, the damaged radius GROWS from
  MinRadius to MaxRadius across the ticks (expanding shockwave); when 0, every tick covers the
  full Falloff range (a **static DoT cloud**).
- `TickDamage[]` (optional, length == Ticks) — relative per-tick damage weights, **normalised so
  the ticks always sum to the authored `Damage`**. A DECREASING profile (`5,4,3,2,1`) + expanding
  radius = the **nuclear shockwave** the maintainer wants: *small area / high damage first → each
  tick larger area / weaker damage*. INCREASING builds up instead. Omit for an even split.
- **Baked friendly fire** (replaces the `_FriendlyFire` twin): `FriendlyFireDamage` (default 50 =
  Cameo law; 0 disables FF), `FriendlyFireSpread` (default 50 = allies only hit within half radius).

**Balance invariant:** the authored `Damage` is always the TOTAL dealt across all ticks, so the
balance pipeline reads ONE number (2000-grid law intact). Rounding on `100/Ticks` (or the weight
split) is sub-1% — acceptable; if exactness ever matters, hand the remainder to tick 0.

## 2. Conversion scope — ALL 55 template main warheads → AreaDamage

Maintainer escalated the scope (2026-08-03): **every central-template main warhead becomes
`AreaDamage`**, not just the AoE families. What stays behind:
- **`_Percentage` twin** stays `HealthPercentageDamage` — it is a different damage channel (% of
  max HP) with its OWN per-template Versus ladder (Bullet_Light %None:16, Bullet_Medium %None:20,
  vs the main's None:100). Cannot be baked in. Present on all 55 templates.
- **`_ExtraDamage` warhead** stays `SpreadDamage` (bespoke per-weapon Versus — see §3). Per-weapon
  on energy/special weapons, NOT in the templates.
- **`_FriendlyFire` twin is DELETED** everywhere — folded into AreaDamage's FF fields.

**Friendly fire — UNIVERSAL (maintainer 2026-08-03, supersedes the earlier small/big-spread split):**
EVERY template's main warhead is AreaDamage with baked FF `FriendlyFireDamage: 50` + `FriendlyFireSpread: 50`
(50% damage within 50% radius). This INCLUDES precise / energy / AA weapons — at 50%/50% the ally
damage is minor for thin-spread weapons, and the maintainer chose it over a pile of per-weapon
overrides. Even **aircraft AA missiles keep FF** (fighter self-splash is acceptable at 50%/50%; NO
fighter-AA override). `ValidRelationships` opens to `Ally, Neutral, Enemy` so the trait can apply FF.
The energy **ExtraDamage** chips (§3) are a SEPARATE mechanic (thematic anti-class bonus), independent
of FF; "compensate small spread" belongs in the formula (§4), not the chip.

Scope confirmed for the AoE/FF families: **Flame, Chemical, Demolition, Concussion, Sonic,
Melee(=Sword), Nuclear, +Magic** (maintainer added Magic; Magic is the %-equalizer, ground-only —
it gets AreaDamage + FF too). Sonic + Sword confirmed IN.

## 3. ExtraDamage investigation (maintainer asked: meaningful or redundant?)

Grep of the live tree: ExtraDamage appears on ~250 weapon nodes across 36 files. **Three
genuinely different profiles** (so "they all work differently per warhead" — confirmed):

1. **Meaningful RPS profile — KEEP.** `TeslaExtraDamage`: Shield 300, Heroic 200, Plate 175,
   Flak 150, None 125 (infantry HIGH), vehicles 75/50/25, buildings/aircraft 10. A real
   **anti-infantry + anti-shield** bonus that defines Tesla's identity. NOT redundant.
2. **Near-inert anti-shield chip — DECIDE.** `LaserExtraDamage` / `RailgunExtraDamage` /
   `MagicExtraDamage`: `Shield 100, everything else 1` (Damage 1 → floors to ~0 vs non-shielded).
   Contributes **nothing against ~90% of targets** — only vs Shield/Reflector. It is pure
   rock-paper-scissors "energy beats shields," NOT the "compensate small spread" the maintainer
   intended (it does not raise damage vs normal armor at all).
3. **Different mechanic — KEEP.** `SniperWeaponExtraDamage: OpenToppedDamage` — hits garrisoned /
   transport passengers (`Infantry, Monster, Garrisoned`). Unrelated to shields.

**Decision (maintainer 2026-08-03):**
- **OpenTopped garrison-hit CONFIRMED KEEP.** Verified in engine: `OpenToppedDamageWarhead`
  (`OpenRA.Mods.AS`) calls `DamagePassengers()` on every `INotifyPassengersDamage` trait —
  implemented by `Cargo` (transports), `Garrisonable` (garrisoned buildings) and `SharedCargo`
  (bunkers). It damages the passengers INSIDE, bypassing the transport's armor (Versus targets
  `Infantry, Monster, Garrisoned`). A real special; keep on the sniper (and any anti-garrison unit).
- **Anti-shield chips (Laser/Railgun/Magic) → REPURPOSE into thematic GAP-FILL bonuses** (the Tesla
  model). CORRECTION (2026-08-03, maintainer caught it): the earlier "railgun is weak vs heavy, the
  chip fixes it" was WRONG — reasoned from the Bullet (anti-light) ladder. The ACTUAL energy mains
  already carry identity + shields (verified from templates):
  - Railgun main: `Superheavy 100 › Heavy 96 › Medium 92 … infantry 68–80 … air 40–52`, `Shield 140`
    → anti-heavy VEHICLE, ground-only.
  - Tesla main: `Superheavy 100 › Heroic 96 › Heavy 92 › Plate 88 …` → anti-heavy INF+VEH, ground-only.
  - Laser main: `Superheavy 100 › Heroic 94 › Spaceship 88 › Heavy 76 …` → anti-heavy, HITS AIR.
  - Prism main: `Scout 100 › None 94 › Wood 88 › Light 82 … Superheavy 34` → anti-LIGHT infantry (inverted).
  So the chip is REDUNDANT as anti-shield (the main already does `Shield 110/140`). Its ONLY real job
  is **thin-spread compensation** (`50% of main, EXCLUDED from price`), best spent as a THEMATIC
  gap-fill (cover the class the main is weakest vs). PLUS: **thin the energy main Spread 800 → ~150**
  (near single-target — that IS "almost only hits one target"), tighter falloff.
  **FINAL Versus ladders (maintainer 2026-08-03) — Damage = 50% of main, thin spread, excluded from price:**
  - **Laser** → anti-INFANTRY (light-focused): `None 200, Flak 175, Plate 150, Heroic 125, Shield 100,
    Scout 75, Light 50, Medium 25, Heavy 10, Superheavy 10, buildings 10, air 10`. (Laser main still
    hits air; the chip is the ground anti-infantry burn — best vs LIGHT infantry `None`.)
  - **Railgun** → anti-BUILDING + superheavy (siege): `Concrete 200, Steel 175, Wood 150, Superheavy 125,
    Heavy 100, Medium 75, Light 50, Scout 25, infantry 10, air 10, Shield 10`. Building order is
    toughest-first (Concrete > Steel > Wood). Shield 10 CONFIRMED (kinetic ≠ anti-shield; the railgun
    MAIN still does Shield 140).
  - **Tesla** → anti-INFANTRY (armor-focused) + shield (KEEP): `Shield 300, Heroic 200, Plate 175,
    Flak 150, None 125, Superheavy 100, Heavy 75, Medium 50, Light 25, Scout 10, buildings 10, air 10`.
  - Laser vs Tesla are INVERTED on the infantry ladder (Laser best vs None/light, Tesla best vs
    Heroic/armored) + Tesla is the big anti-shield (300 vs Laser's 100) — deliberately distinct.
  - **Prism** → NO chip (dedicated anti-light beam). **Magic** → DROP the chip (%-HP equalizer is its
    special). CONFIRMED.
  Net: each energy weapon = a thin-spread single-target specialist, main = what it kills, chip = a
  distinct thematic secondary; none overlap. Chip stays `SpreadDamage`, bespoke Versus, excluded from
  price. Fallback if minimal is preferred: delete all chips, let the main Shield Versus carry shields.
- **Sniper OpenTopped is ANTI-GARRISON, not anti-transport.** Verified: `Warhead@SniperWeapon`
  `ValidTargets: Infantry, Monster, Garrisoned` cannot target vehicles, so it never fires at an APC —
  but it CAN target a garrisoned building (`Garrisoned` target type) and `Garrisonable` takes the
  passenger damage. Keep it as the anti-garrison tool. Anti-transport passenger damage would have to
  ride on a weapon that can target vehicles (not the sniper).
- **"Compensate small spread with extra damage" belongs in the FORMULA, not a chip** — a warhead
  that is inert vs most targets compensates nothing in general combat (§4). Separate the two ideas:
  ExtraDamage = deliberate anti-class RPS; spread compensation = pricing.

## 4. Spread pricing + spread reduction (open formula work)

- **How spread enters the balance formula (OPEN):** bigger spread = higher *potential* damage when
  fighting many small units, but it does **not** scale linearly — against a single target, spread
  is irrelevant. So spread must be priced with **diminishing returns** (an expected-targets-hit
  model), never a flat multiplier. Needs a proper term in FORMULA_V2. Take the total-damage-over-
  the-spread-area into account, capped by the single-target case.
- **Reduce spreads overall** — many weapons have too much spread. Old pre-split model tied
  projectile **Inaccuracy = Spread**; the SpreadDamage falloff is ~3–4 concentric circles where the
  Spread value is the radius step (e.g. 100 @100%, 200 @34%, 300 @17%, 400 @5%). Even small weapons
  ended up with a big footprint. Normalise smaller. Lever: **raise projectile speed → lower
  inaccuracy → lower spread** (see §5). Reason each family individually; don't bulk-shrink blindly.

## 5. Projectile speed / tank-shell rules (write down; apply later)

- **Regular tank:** projectile speed = `maxRange / 10`; warhead **CannonHE**; **2× the spread** of
  CannonAP (HE = area, less targeted — bigger spread is correct).
- **Tank destroyer + any cannon TURRET (gun turret):** projectile speed = `maxRange / 5` (double);
  warhead **CannonAP only** (targeted); **smaller spread**. Faster shell → hits reliably without a
  big spread.
- **CannonHE spread > CannonAP spread** — deliberate (HE splashes, AP is a targeted penetrator).
- **Hybrid tanks (in-between: Light Tank, High Tech Tank, and case-by-case units):** **50% CannonAP
  + 50% CannonHE** (damage split evenly between the two warheads), projectile speed =
  `maxRange / 10 * 1.5` (between a normal tank and a TD). Apply the hybrid scaling only to units
  that are genuinely between a TD and a normal tank; keep it a per-unit choice, but the **logic is
  now recorded** so hybrids can be built consistently.
- Bullets are the fastest (small spread), cannons slower (bigger spread), **missiles slowest but
  can hit air** — slower projectiles may warrant a small compensating bonus for travel delay
  (relates to the ExtraDamage/formula question in §3–§4).

## 6. Epic vehicle template rework (mirror the epic AIR template)

Make `^EpicVehicleTemplate` work like the epic **aircraft** template: it only **advances** a unit
(build-limit, faster build time, commando/epic decoration) and **no longer defines the unit type
OR the armor**. An epic vehicle then inherits **both** its underlying class template **and** the
epic template — e.g. the **Chrono Tank = `^FireSupportVehicleTemplate` + `^EpicVehicleTemplate`**
(both required, since epic no longer supplies type/armor). Mirrors how epic aircraft can be a
spaceship or a bomber underneath. Balancing epic units is hard (they are very different from each
other) — attempt per-unit; no clean class anchor yet. This changes the current epic model where
`L=0.3, M=1.0` folds the whole epic effect into UnitClass (DESIGN §12) — keep that pricing, just
split the *template* responsibilities.

## 7. Build / rollout plan (the order to execute)

1. **Build the trait** — `AreaDamageWarhead.cs` is written (+ per-tick `TickDamage` shaping).
   `dotnet build -c Release --nologo -p:TargetPlatform=win-x64`; fix compile errors; boot-gate.
2. **Pipeline updates** (Python): `formula.spread_damage_sum` + `audit_warhead_split` must treat
   `AreaDamage` exactly like `SpreadDamage` (sum its `Damage`; still skip `_Percentage`,
   `_ExtraDamage`; the `_FriendlyFire` twin is gone). One-line type-name additions.
3. **Convert the 55 templates** — `Warhead@X: SpreadDamage` → `Warhead@X: AreaDamage`; delete the
   19 `_FriendlyFire` twins; set `FriendlyFireDamage` per §2 (50 for AoE, 0 for precise/energy);
   `_Percentage` / `_ExtraDamage` untouched. Scripted over the template block, boot-gate.
4. **Nuclear + cluster** — migrate the ~10-stacked-warhead bandaid onto one AreaDamage with
   `Ticks`/`TickDelay`/`MinRadius`/`MaxRadius`/`TickDamage` (decreasing). Retire the stacked nodes.
5. **Spread reduction + projectile-speed pass** (§4–§5) — after the type conversion, as balance.
6. **Epic vehicle template split** (§6) — independent; can run in parallel.

Everything after step 1 is boot-gated per commit; scoped `git add`; the C# needs a rebuild before
the boot gate (`cameo-launch-before-commit`).

## 8. The universal conversion CASCADES to retrofitted weapons (discovered + reverted 2026-08-03)

Splicing all 55 templates to AreaDamage **crashes the boot** — because ~hundreds of weapons across
~50 files were ALREADY retrofitted onto these `^Warhead_*` templates (Phase-2) and override the
inherited warheads in ways that clash with the new type. Attempted, boot-gated (crashed), REVERTED to
the known-good Nuclear-pilot state. The three clash classes:

1. **`-Warhead@X_FriendlyFire:` REMOVAL nodes** (e.g. `RedAlert2/Yuri/weapons.yaml:853` removing
   `Warhead@Chemical_Medium_FriendlyFire`) → `no elements with key ... to remove` at ResolveInherits.
   The template no longer provides the twin (baked in), so the removal is invalid. **This is the FIRST
   crash hit** (fails during inherit resolution, before field-loading).
2. **`Warhead@X: SpreadDamage` main RESTATEMENTS** (e.g. `CHFlame` → `Warhead@Flame_Medium: SpreadDamage`).
   MiniYaml keeps the child's `SpreadDamage` type but inherits the template's AreaDamage-only fields
   (`FriendlyFireDamage`/`FriendlyFireSpread`) → FieldLoader "unknown field on SpreadDamageWarhead"
   crash. (Not reached before #1, but would follow.)
3. **`Warhead@X_FriendlyFire: SpreadDamage` REDEFINITIONS** (~150) → become STANDALONE double-FF
   warheads (the main's baked FF + this leftover twin). Not a crash, but wrong (double ally damage).

**Resolution-awareness is mandatory.** Some `Warhead@X` keys are defined by LOCAL templates, NOT the
`^Warhead_*` family — e.g. `Warhead@Demolition_Light` on `^DamagingExplosionHE` (which inherits
`^Explosion`, defines its OWN twin). Weapons like `RockExplode` inherit those and their removals stay
VALID → must NOT be touched. The sweep only acts on a `Warhead@X` node when the weapon's resolved
`@wh`/inherit chain provides that key from a `^Warhead_*` template (not a closer local template).

**Staged sweep plan (the correct next operation — one boot-gated unit):**
1. Resolution-aware script (PROVIDES graph, like the earlier retrofit repairs): for every weapon whose
   `Warhead@X` resolves to a `^Warhead_*` template — (a) delete `-Warhead@X_FriendlyFire:` removal
   lines; (b) delete `Warhead@X_FriendlyFire: …` twin blocks; (c) strip ` SpreadDamage` from the
   `Warhead@X:` main override → bare (inherits the AreaDamage type); (d) if that main override also
   restates `ValidRelationships: Neutral, Enemy`, strip it too (so the template's `Ally, Neutral, Enemy`
   + baked FF stands). Dry-run counts first.

   **No separate per-weapon AA FF override exists to remove** (maintainer asked 2026-08-03): AA damage
   weapons just inherit the template's enemy-only default, so the universal AreaDamage+FF conversion
   (plus step (d)) gives them FF automatically. The `ValidRelationships: Enemy` overrides that DO exist
   in the tree are on **condition warheads** (snare / lock-on / corroded `GrantExternalCondition`) —
   correctly enemy-only, MUST NOT be touched.

   **Reduce the MissileAA template spread** (maintainer 2026-08-03): so the now-universal FF on anti-air
   is well-contained (fighters can dodge the 50%/50% ally splash). Add a per-family spread override for
   `MissileAA` in the generator, tighter than the default `400/600/800` (e.g. ~`250/350/450` — tune).
2. Re-apply the generator changes (main → `AreaDamage`; `ValidRelationships: Ally, Neutral, Enemy`;
   add `FriendlyFireDamage: 50` + `FriendlyFireSpread: 50`; remove the FF twin; naming `^Warhead_{tag}`
   + `Warhead@{tag}_Percentage`; `AREA_RINGS = {Nuclear: ticks5/delay6/max4000/5,4,3,2,1}`;
   `AA_ENEMY_ONLY = set()` — universal FF). These 4-ish edits are documented here + were verified to
   regenerate the Nuclear pilot byte-for-byte.
3. Regenerate + splice the 55 templates (anchor-based region replace, spot-check one template diff).
4. **Boot-gate.** 5. Pipeline: `spread_damage_sum` / `audit_warhead_split` recognize `AreaDamage`.
6. Commit generator + weapons + the weapon sweep TOGETHER.

Until then: the Nuclear pilot (committed `851537a03`) is the ONLY live `AreaDamage`; the 55 templates
stay `SpreadDamage`; the generator is reverted to `SpreadDamage` so it stays consistent with the file.

## 9. AreaDamagePercentage + AtomicCore = the first real in-game proof (2026-08-04)

The Nuclear pilot (`^Warhead_Nuclear_Super`) is an ABSTRACT `^`-template → **never
instantiated** → the AreaDamage trait was compiled but NEVER actually constructed at
runtime (the "pilot boot" was a false pass). AtomicCore is the first concrete instantiation.

**`AreaDamagePercentage` warhead (BUILT + BOOTS, uncommitted).** `OpenRA.Mods.Cameo/Warheads/
AreaDamagePercentageWarhead.cs` — a one-method subclass of `AreaDamageWarhead` overriding
`InflictDamage` to deal a **percentage of the victim's max HP** (mirrors `HealthPercentageDamage`).
It REUSES the entire ring/tick/baked-FF spatial pass; its `Falloff` collapses a whole STACK of
concentric `HealthPercentageDamage` rings into ONE smooth % gradient. Degrades to a single-hit
%-with-falloff at `Ticks:1`/`MaxRadius:0`.

**AtomicCore / Atomic / RA2Atomic converted (uncommitted, boot-gated to menu).**
- AtomicCore now `Inherits@nuke: ^Warhead_Nuclear_Super` and uses that AreaDamage nuke **as-is**
  (maintainer: "no override — superweapons ignore ReloadDelay"). Its 6 stacked `SpreadDamage`
  rings deleted; its 10 `NuclearMissilePercentage` rings → **one** `AreaDamagePercentage`
  (`Spread 1000, Damage 100, Falloff 100..0`, radiation/fire types, `UpdatesUnitStatistics: false`).
  Tesla-shield layers + effects/smudge/shake/flash kept.
- `RA2Atomic` (inherits Atomic) re-skinned the 6 rings with `RadiationDeath`; consolidated onto a
  single `Warhead@Nuclear_Super:` override carrying the radiation DamageTypes.

**⚠ Cascade lesson — the empty-type warhead boot crash.** Deleting `Warhead@X` from a `^template`
orphans any CHILD weapon with a **bare** `Warhead@X:` override (it relied on inheriting the type).
The bare override then resolves to an **empty type** → the engine builds the abstract base `Warhead`
→ `GetConstructor([])` null → **NullReferenceException in `ObjectCreator.CreateBasic`** during
`LoadDefaults` — and the stack **names no weapon**. Here `RA2Atomic`'s bare `Warhead@1Dam_impact:`
crashed every boot until fixed. Guard: `scratchpad/find_empty_warhead.py` resolves all 37 live
weapon files and names the offending weapon (0 = safe); `check-yaml` reproduces it fast without a
full boot. **Run the empty-type scan after ANY template warhead deletion/rename.** (Memory:
`cameo-empty-warhead-crash`.)

**Build/deploy fact (memory `cameo-dll-deploy-engine-bin`):** `dotnet build` → `engine/bin`
(gitignored, what the running `engine/bin/OpenRA.exe` loads). `mods/cameo/OpenRA.Mods.Cameo.dll`
is a git-TRACKED copy that does NOT auto-update (was a month stale) — refresh it from engine/bin
only for release/commit.
