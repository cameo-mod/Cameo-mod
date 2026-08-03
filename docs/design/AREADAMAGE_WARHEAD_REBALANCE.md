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

**FF vs ExtraDamage — the small-vs-big-spread axis (maintainer intent):**
- **Big-spread AoE weapons** (Flame, Chemical, Demolition, Concussion, Sonic, Melee/Sword, Magic,
  Nuclear, and the HE splashers) get **friendly fire** (safer to use near your own units) →
  `FriendlyFireDamage: 50`.
- **Small-spread / precise / energy weapons** (Bullet, CannonAP, MissileAP, Laser, Prism, Railgun,
  Tesla, Arrow) get **no FF** (`FriendlyFireDamage: 0`) and instead carry an **ExtraDamage** bonus
  to compensate for their smaller area — BUT see §3: today's ExtraDamage does NOT actually deliver
  general compensation, so this intent needs the formula, not (only) the chip.

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
  - **Railgun** → anti-BUILDING + superheavy (siege): `Wood 200, Steel 175, Concrete 150, Superheavy 125,
    Heavy 100, Medium 75, Light 50, Scout 25, infantry 10, air 10`. NOTE: Shield UNLISTED by maintainer
    → set to 10 (kinetic ≠ anti-shield; the railgun MAIN still does Shield 140). Confirm if oversight.
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
