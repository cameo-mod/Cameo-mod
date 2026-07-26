# Class-anchor decisions log (maintainer-confirmed, one class at a time)

_Running record of the collaborative baseline+verifier definition (started 2026-07-25). Each class
is LOCKED here once the maintainer confirms; then `fit_class` + sign-off + `defaults.yaml` template
work follow. Fixed MBT anchor pivot: **Tiger `tiger.nax` = 100000 HP / 100 spd / **5500 rng** / 10000
dmg @ 50 reload / cost0 800** (DPS 200). (Range bumped 5000→5500 on 2026-07-26 to complete the range
ladder — see the RANGE LADDER section.)_

## ▶ STATUS / RESUME HERE (2026-07-26)

**Collaborative class-by-class anchor definition IN PROGRESS.** The maintainer names a class → I
propose baseline+verifier → they give exact numbers → LOCK here → later: create template (boot-gated)
+ `fit_class` + sign-off. **Every class needs a baseline AND a verifier** (verifier = 2×HP + 2×DPS +
2.5×cost, same speed/range → clean identity). Support is EXEMPT; cargo = Σ(passengers).

**LOCKED (cost0):** ScoutVehicle 300 · LightTank 400 · AntiAir 600 · TankDestroyer 600 · MBT 800
(Tiger pivot, signed) · FireSupport 1000 · ArtilleryTank 1200 (baseline; verifier pending) · LineBreaker
1200 · HighTechTank 2000 · Dreadnought 3000.
**➡ NEXT DEFENSE CLASSES:** Basic + AntiAir + Advanced(baseline) + Super(baseline) **LOCKED**. Remaining:
**AdvancedDefense cost (idea B: 1000 + DPS trim)** + **Bunker** → then aircraft (rearmable formula first)
/ naval. Defense formula = **3-input**; **verifier convention = 2.5×HP + 2.5×DPS → 4.0×** (Basic 2000,
AntiAir 2400; Super = epic tier, no 4×); armor Concrete/Steel; flat-10 regen. **Nuclear Warhead Versus
fix CONFIRMED (boot-gated, step 3).** Verifiers PENDING: ArtilleryTank (lunar grille / Juggernaut Mk II);
Artillery (V2).
**Open flags:** LineBreaker armor (TBD); `japan_armoredcar` Scout-vs-AntiAir; umlaut renames
(`naxis_brummbr`→brummbar, `naxis_kbelwagen`→kubelwagen — boot-gated); `asianalliance_pulverizer`→AntiAir.
**Future audits:** projectile speed = range÷2 (V3-type missiles exempt); weapon-type↔unit-type binding.
Infantry anchors (14) already exist, need sign-off (commando needs a verifier). **After all anchors:**
create templates in defaults.yaml (boot-gated), run `fit_class`, wire `check_band` into `run_all.sh`.
**Upgrades LAST.** **Weapons.yaml below-divider cleanup = ON HOLD** (`weapons_cleanup_plan.md`, no
deletions). WeaponClass restored to `weapon_classes.yaml`.

## ★ RANGE LADDER (maintainer 2026-07-26 — verified consistent, steps of 500)

| Class | baseline range | band (±500) |
|---|--:|--:|
| LightTank | 5000 | 4500–5500 |
| **MBT** | **5500** | 5000–6000 |
| HighTechTank | 6000 | 5500–6500 |
| Dreadnought | 6500 | 6000–7000 |
| TankDestroyer | 7000 | 6500–7500 |
| **FireSupport** | **10000** | **9000–11000** |
| **ArtilleryTank** | **12000** | **10000–14000** |

The **direct-fire gun ladder now ENDS at TankDestroyer 7000** (500-step, close brawler → long-range).
The **indirect long-range classes** sit above it with wider ±bands: **FireSupport 10000 (band
9000–11000)** — slow + very fragile, must outrange tanks; **ArtilleryTank 12000 (band 10000–14000)** —
tanky TURRETED artillery (Medium armor); **pure Artillery 15000 (band 13000–17000)** — FRONTAL-facing
(no turret), Light armor, fragile. Scout / AntiAir / LineBreaker
have their own ranges (Scout 4500 band 4000–5000; **AntiAir GND 5000 band 4500–5500**, AA weapon
+50% = 7500; LineBreaker short ~2500). **These ranges override the per-class range values below.**

## ★ ARMOR LADDER (maintainer 2026-07-26 — one armor type per class)

Each class carries a fixed armor type (lightest → heaviest: **Scout < Light < Medium < Heavy <
Superheavy**):

| Class | Armor |
|---|---|
| ScoutVehicle | **Scout** |
| LightTank | **Medium** *(revised from Light, 2026-07-26)* |
| AntiAir Vehicle | **Medium** |
| MBT | **Heavy** |
| TankDestroyer | **Heavy** |
| HighTechTank | **Superheavy** |
| Dreadnought | **Superheavy** |
| FireSupport | **Light** |
| Artillery | **Light** |
| ArtilleryTank | **Medium** (tanky turreted artillery) |
| LineBreaker | **TBD** — "very durable" → likely Heavy/Superheavy (confirm) |

*(Aircraft / naval / defenses get their own armor scheme later.)*

## ★ TWO NEW PRICING RULES (maintainer 2026-07-26)

1. **Flame units → special K 1.25.** Every flame weapon burns / deals damage-over-time, so ALL flame
   units carry a **1.25× special modifier** (new rule; wasn't applied before).
2. **WeaponClass is part of the DPS calc** (confirmed): `DPS = Damage × Burst / eff-reload ×
   WeaponClass`. e.g. **Medium Flame = 1.0, Heavy Flame = 1.25** — the heavier weapon class raises DPS
   directly, so the verifier reaches 2× DPS with a *higher weapon class + lower damage-per-shot*.

---

## ✅ LightTank (NEW) — LOCKED 2026-07-25

**Baseline actor:** `ra1_allies_alliedlighttank`, **restatted** to:
| HP | Speed | Range | Damage | Reload | **cost0** | DPS |
|--:|--:|--:|--:|--:|--:|--:|
| **40000** | **120** | **5000** | **4000** | **40** | **400** | 100 |

**Verifier:** **Nod Light Tank Mk II** (`td_nod_lighttankmkii`, the promotion unit) at **exactly 2.5×
cost = 1000¢**, restatted to **2× HP / 2× DPS** (80000 HP / 8000 dmg @ 40, same 120 spd / 5000 rng).
- **Move its point-defense laser to the Black-Market upgrade** (same pattern as the bikes) so it is
  no longer a base **special** modifier → keeps the verifier identity clean (K = 1.0).
- The **regular Nod Light Tank** (`td_nod_lighttank`) keeps its current price but is rebalanced to fit
  this band.

**Class rules:** all members → **Medium armor** (revised from Light on 2026-07-26 — see ARMOR LADDER);
rebalanced into the baseline→verifier band.

**★ REBALANCE METHOD (maintainer 2026-07-25, applies to ALL classes):** each member **keeps its
current Speed, Range, Cost, ReloadDelay, Burst, BurstDelays** where possible. Rebalance is done by
**(1) adjusting the main Damage first, then (2) fine-tuning with the unit's `FirepowerMultiplier`**
(+ HP to fit the band). Cost stays nostalgic (§20); the formula prices the kept cost from the tuned
stats.

**Members (maintainer-confirmed):** latinsyndicate_rushertank, yuri_lashertank, ra1_allies_sheridan
assaultta, schwarzermond lunar panzer, japan_shrineminitank, panzer.nax (Naxis Panzer III — HP comes
DOWN into band), futuretech_robottank. **Tick Tank DROPPED** (slow/deploys — doesn't fit).

**Members (my additions — CONFIRMED, templates already exist in yaml per maintainer):**
`terran_vulture`, `asianalliance_viper`/`asianalliance_quasar`, `steelconsortium_manta`,
`ixian_shockraider`, `cabal_ravager`, `naxis_kbelwagen`, `japan_armoredcar`.
- **`latinsyndicate_diablo` → NOT LightTank.** It's Latin's main **anti-air** vehicle → **move from
  Support to the AntiAirTank template.** (flagged for the AntiAirTank class.)
- **`ordos_ordoscombattank` (Ordos Combat Tank) → ADD to LightTank** — the lightest of the three Dune
  house tanks (§17.4: Ordos 3.2× < Atreides 3.7× < Harkonnen 4.8×). Nudge its speed up toward the
  class if it feels too slow; otherwise keep.
- Scouts (ordos_raider, ts_gdi_pitbull, forgotten_raidercar, IFVs, futuretech_salamanderifv) =
  **already ScoutVehicle in the yaml** (confirmed) — not here.

**PREREQ note:** the class↔weapon binding rules (which unit class may pick which weapon class/type)
need the **new weapon types (§13 warhead library) implemented first**, and the restored
**`WeaponClass`** sidecar (`docs/balance/weapon_classes.yaml`) wired into the pipeline.

---

## ✅ HighTechTank — LOCKED 2026-07-25

**Baseline = RA1 Soviet Mammoth Tank** (`ra1_soviets_mammothtank`) — EXACT round numbers
(maintainer 2026-07-25):
| HP | Speed | Range | Dmg/shot | Burst | BurstDelay | **eff-reload** | **cost0** | DPS |
|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| **400000** | **50** | **6000** | **20000** | **2** | 8 (keep) | **80** | **2000** | **500** |
- **eff-reload 80** for BOTH weapons, but they keep their different burst delays: **cannon bd 8 ⇒
  ReloadDelay 72** (72+8); **AA tusk bd 12 ⇒ ReloadDelay 68** (68+12). Total per burst = 40000.
  **Range 6000, band 5500–6500** (2026-07-26). (Verifier likewise: eff-reload 80 both, cannon RD 72 / AA RD 68.)

**Verifier = Siege Mammoth Tank** (`ra1_soviets_siegemammothtank`) — 2×HP / 2×DPS / 2.5×cost, SAME
speed+range:
| HP | Speed | Range | Dmg/shot | Burst | eff-reload | **cost0** | DPS |
|--:|--:|--:|--:|--:|--:|--:|--:|
| **800000** | **50** | **6000** | **40000** | **2** | **80** | **5000** | **1000** |
- ✓ identity verified: 2×HP + 2×DPS (same spd/rng) = **exactly 2.5× cost** under the class-baseline
  formula (o 1.5 / p 2 / q 4 → mean 2.5).

**⚠ Pricing-flag cleanup (both units):** all 10/8 armaments are `pricing=True` (base gun + Targeting-
Computer/Thermobaric/Tesla upgrade variants + the AA MammothTusk). Anchor DPS = **500** from the base
main gun. Upgrade variants are upgrade-gated (excluded). **★ AG/AA PAIR LAW (maintainer 2026-07-25):
a unit's anti-ground and anti-air base weapons must have the SAME EFFECTIVE reload delay** (so
identical DPS) **+ same Damage, Burst, WeaponClass — but their BurstDelays MAY differ** (ReloadDelay
compensates to keep eff-reload equal). **Counted as ONE for pricing — never summed** (can't fire on
the same target). Mammoth: cannon bd 8 → ReloadDelay 72; AA tusk bd 12 → ReloadDelay 68; **both
eff-reload 80** → both DPS 500, priced once (not 500+500).
**Other Soviet mammoth variants:** only these two exist in the RA1 Soviet roster; the per-armament
upgrade variants inherit the base damage (20000 baseline / 40000 verifier) + their upgrade modifiers.
- Ladder so far: LightTank 400 · MBT 800 · **HighTechTank 2000**. (Maintainer called 2000 "twice the
  MBT baseline" — vs the Tiger's cost0 800 that's 2.5×; confirm MBT stays 800 or bumps to 1000.)
- **Apocalypse** sits as a heavy *member*, not the baseline.
- **Turreted tanks are ALWAYS Light / MBT / HighTech** (maintainer rule).

## ✅ TankDestroyer — LOCKED 2026-07-25

**Role:** frontal-facing (no turret), long range, anti-tank. **Baseline = the cheapest/budget TD
(Hetzer); verifier = RA2 Tank Destroyer.**

| | Unit | HP | Speed | Range | Dmg | Burst | Reload | **cost0** | DPS |
|---|---|--:|--:|--:|--:|--:|--:|--:|--:|
| **Baseline** | `naxis_hetzer` | **75000** | 60 | 7000 | 20000 | 1 | 80 | **600** | 250 |
| **Verifier** | `ra2_allies_tankdestroyer` | **150000** | 60 | 7000 | 40000 | 1 | 80 | **1500** | 500 |

- Range band **6500–7500**. ✓ identity: 2×HP + 2×DPS, same spd/rng → 2.5× cost (600 → 1500).
- **Ladder now:** LightTank 400 · **TankDestroyer 600** · MBT 800 · HighTechTank 2000 (Hetzer is a
  budget unit, cheaper than the MBT — intended).
- **Other TDs in between** (600–1500): `ra1_allies_alliedtankdestroyer`, `naxis_jagdpanzer`.
- **Ordos Tank Destroyer = MORE expensive** — it's **cloaked** → carries a special modifier (K>1),
  so it prices above the plain band.
- **Neo Jagdpanzer → Dreadnought** (450k HP Superheavy — too tanky for a TD).

## Taxonomy clarifications (maintainer 2026-07-25)

- **LineBreaker** = **very short range + very durable** (flame tanks). The template's heavy
  damage-reduction + extra-firepower suits CLOSE range only.
- **FireSupport** = **weaker armor + longer range**; countered easily *because* fragile. **REMOVE
  anti-air from FireSupport** (e.g. GDI MLRS loses AA) for consistency.
- **ArtilleryTank** = between tank and artillery (e.g. **Ixian Combat Siege**; maybe Sturm Tiger —
  research later; so far Ixian Combat Siege is the only clear fit).

## ✅ NEW CLASS — **Dreadnought** (`^DreadnoughtTemplate`) — heavy, long-range, FRONTAL, TANKY

**Named by maintainer 2026-07-25.** Frontal-facing + long range + **tanky** — the tanky counterpart
to the fragile FireSupport. Currently mis-assigned to LineBreaker, whose damage-reduction + firepower
buff only works at *close* range; Dreadnought needs its OWN damage-reduction tuned for long range
(NOT the LineBreaker buff). Members (move off LineBreaker):
- `asianalliance_pulverizermecha` — 285000 HP, Superheavy, cost 3000
- `terran_warhound` — 300000 HP, Heavy, cost 4500
- `ixian_neocymek` — 300000 HP, Heavy, cost 4500

**TODO:** create `^DreadnoughtTemplate` in defaults.yaml (boot-gated); baseline/verifier pick later.

## HOLD

**Weapons.yaml below-divider cleanup = ON HOLD** (maintainer: "don't delete anything yet"). Plan
stays in `weapons_cleanup_plan.md`; no deletions/moves until greenlit.

---

## ✅ Dreadnought — LOCKED 2026-07-26 (Warhound baseline, no cloak)

**Baseline = Warhound** (`terran_warhound`) — the previous baseline stats, but KEEP the Warhound's own
weapons (adjust only their DAMAGE to hit the target DPS):
| HP | Speed | Range | cost0 |
|--:|--:|--:|--:|
| **300000** | **60** | **6500** (band 6000–7000) | **3000** |
- Weapons kept: **SCTyr** dual AG cannon (burst 2, bd 0, reload 44) + **SCTyrAA** burst-4 anti-
  everything rockets (burst 4, bd 2, reload 84). **Both hit ground → multi-weapon ground-sum**: anchor
  DPS = cannon DPS + rocket DPS (set via weapon damage). Keep bursts/burst-delays/reloads as-is.

**Verifier = Neo Cymek** (`ixian_neocymek`) — a Warhound carbon-copy (dual **railgun** StormGun +
burst-4 rockets; only the cannon→railgun differs), so "changing the weapon damage is the easiest
thing":
| HP | Speed | Range | cost0 |
|--:|--:|--:|--:|
| **600000** | **60** | **6500** | **7500** |
- 2× HP, **2× DPS** (adjust the weapon damage; keep its bursts/burst-delays/reloads), 2.5× cost, same
  speed/range → clean identity.

**Cloak/K = RESOLVED (2026-07-26): NO cloak on either.** Both Warhound baseline and Neo Cymek verifier
run at **K 1.0** (the cloak's only purpose — cancelling the Pulverizer's gatling K — is gone now that
the Warhound is the baseline). Clean 2×HP + 2×DPS + K 1.0 + same spd/rng → 2.5× cost = 7500 identity.

**Pulverizer Mecha** → scaled DOWN to a **member** at **cost 2500**, **range 6000** (CONFIRMED — the
minimum of the dreadnought band 6000–7000). Keeps its gatling (its own K 1.25 as a member, not the
anchor). **Other members:** Neo Jagdpanzer.

**Ladder:** LightTank 400 · TankDestroyer 600 · MBT 800 · HighTech 2000 · Pulverizer(member) 2500 ·
**Dreadnought baseline 3000.**

**★ MULTI-WEAPON GROUND-SUM RULE (maintainer 2026-07-25):** when a unit has multiple weapons that
can ALL hit the GROUND, **SUM their DPS** (they fire together on a ground target) — even if only one
of them also hits air. This is DISTINCT from the **AG/AA PAIR LAW** (an AG-only + an AA-only weapon =
alternatives, counted ONCE). Rule of thumb: **the ground is the reference — sum every weapon that
reaches a ground target.**

---

## ✅ LineBreaker — LOCKED 2026-07-26 (short range, very durable — flame + melee)

**Baseline = Nod Flame Tank** (`td_nod_flametank`):
| HP | Speed | Range | Dmg/shot | Burst | eff-reload | WeaponClass | special K | cost0 | DPS |
|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| **200000** | **80** | **2500** | **20000** | **2** | **60** | **1.0** (Medium Flame) | **1.25** (flame) | **1200** | **666.7** |
- DPS = 20000 × 2 / 60 × **1.0** = 666.7. Flame → **special K 1.25** (burn/DoT, new rule).

**Verifier = Flame Tank Mk II** (`td_nod_flametankmkii`, the upgrade) — **Heavy Flame → WeaponClass
1.25**, which raises DPS directly, so the 2× DPS is reached with a *lower* damage-per-shot:
| HP | Speed | Range | Dmg/shot | Burst | eff-reload | WeaponClass | special K | cost0 | DPS |
|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| **400000** | **80** | **2500** | **32000** | **2** | **60** | **1.25** (Heavy Flame) | **1.25** (flame) | **3000** | **1333.3** |
- DPS = 32000 × 2 / 60 × **1.25** = 1333.3 = 2× baseline. ✓ identity: 2×HP + 2×DPS + same K(1.25) +
  same spd/rng → 2.5× cost (1200 → 3000). (Damage-per-shot is 32000, NOT 40000, because the 1.25
  weapon-class already carries part of the DPS — as you flagged.)

**Members:** flame tanks (`td_nod_flametankmkii`, `forgotten_flametank`, `asianalliance_asianflametank`,
`japan_hovercraftflametank`) **+ melee / very-short-range durable**: WC2 **Ogre-Mage**, WC **Knight**,
**Zerg Ultralisk**, **Consortium Megalodon**. (Berserker / MAD Tank read as epic/suicide — flag if not.)
**Ladder (cost):** … LineBreaker baseline **1200** (short-range brawler class; its own range ~2500,
outside the gun range-ladder).

---

## ✅ ScoutVehicle — LOCKED 2026-07-26 (fastest, most fragile, cheapest; INFANTRY HP granularity)

**Baseline = Nod Buggy** (`td_nod_buggy`) — anchored on its REAL stats (not an invented DPS):
| HP | Speed | Range | DPS | cost0 | Armor |
|--:|--:|--:|--:|--:|--:|
| **20000** | **200** | **4500** | **450** | **300** | Scout |
- **DPS 450 = the buggy's ACTUAL main-gun DPS** (MachineGun: 4000 dmg × burst 3 / eff-reload 20 ×
  wc 0.75 = 450). The class self-anchors on the real baseline unit → the buggy keeps its weapon as-is,
  no nerf. *(The earlier "75" was a bogus cross-class ¾-of-LightTank guess — corrected 2026-07-26 per
  maintainer.)* HP 20000 = ½ the LightTank → fragile. Speed 200 = fastest class. Range **4500** =
  scout's own (**band 4000–5000**). cost0 **300** = nostalgic. NB the weapon is anti-infantry (SmallArms warhead) so this
  raw DPS is NOT cross-comparable with the tanks' anti-armor DPS — each class self-anchors.

**Verifier = Terran Vulture** (`terran_vulture`), restatted to the 2.5× identity point:
| HP | Speed | Range | DPS | cost0 |
|--:|--:|--:|--:|--:|
| **40000** | **200** | **4500** | **900** | **750** |
- 2×HP + 2×DPS + same spd/rng → exactly 2.5× cost (300 → 750). ✓ clean identity (o1.5 / p2 / q4 → 2.5).
- Restat: HP 75000→40000, cost 900→750, speed 125→**200**, range 4800→4500, weapon damage → **DPS 900**
  (2× the buggy). Ground-only (lays mines) → no AA question.

**★ INFANTRY HP GRANULARITY (maintainer 2026-07-26) — the scout class's special rule:**
Scouts use the **infantry HP granularity (steps of 1000)**, NOT the vehicle granularity (steps of
2500), so the 20000–30000 band holds **11 levels** (20k,21k,…,30k) instead of 5. Enforced by the
self-heal convention:
- **Engine mechanic:** self-heal Step is tied to max-HP. Vehicles use `ChangesHealth@SelfHealing.Step
  = HP/2500` (→ HP must be a multiple of 2500); infantry use **`Step = HP/1000`** (→ HP a multiple of
  1000). **Scouts switch to the infantry rule: `Step = HP/1000`** (verified: buggy 20000 → Step 20;
  Ixian/Ordos infantry actors already set Step = HP/1000).
- **Template change (to implement, boot-gated):** `^ScoutVehicleTemplate` currently inherits the
  VEHICLE self-heal from `^VehicleBuffs` (Step 10 / **Delay 1** / **DamageCooldown 10**). Override it to
  the INFANTRY timing from `^InfantryBuffs` (**Delay 2** / **DamageCooldown 20** / StartIfBelow 100),
  and set each scout actor's `ChangesHealth@SelfHealing.Step = HP/1000` (applied in the member
  rebalance, since HP must first be re-rounded to a multiple of 1000). **HARD RULE — do not forget.**
- `Repairable.HpPerStep = HP/20` stays (a multiple of 1000 is always a multiple of 20 → clean).

**Rebalance method (as always):** each member keeps its Speed/Range/Cost/Reload/Burst; tune main Damage
→ FirepowerMultiplier (+HP to band, now in 1000-steps).

**Membership (2026-07-26):** all currently-`^ScoutVehicleTemplate` actors STAY scouts (maintainer:
"currently scout ⇒ still scout unless I give another order") **EXCEPT the moves below.**
- **KEEP (rebalance into band):** `td_nod_buggy` (baseline), `ra1_allies_ranger`, `td_gdi_humvee`,
  `forgotten_raidercar`, `ts_nod_attackbuggy`, `futuretech_scoutdroid` (**bump speed** ~70→~180 — too
  slow for the fastest class), `japan_armoredcar`, `japan_scoutcar`, `tkm_technical`, `tkm_as42`,
  `ordos_leech`, `forgotten_bowler`, `forgotten_ruiner`, `protoss_positron`,
  `steelconsortium_whiterabbit`, `terran_vulture` (verifier).
- **`ordos_raider` = PREMIUM HEAVY SCOUT** (maintainer): keeps its 1200¢ / 60000 HP / K 1.25 — an
  intentional high outlier priced with the special modifier. Stays scout.
- **MOVED OUT → AntiAir Vehicle** (maintainer new order): `td_nod_reconbike`,
  `td_nod_chemicalattackbike` (TD bikes), `ts_nod_attackcycle` (TS bike). *(`naxis_bmwbike` = WW2 Naxis
  bike — FLAG: move too, or keep scout?)*
- **`ra2_soviets_terrordrone` = SPECIAL EXCEPTION** (maintainer): melee suicide/sabotage → EXEMPT.
- **`ra2_c_hum` = CIVILIAN** (`ra2_c_` prefix, only in garrison/spawn lists, 80000 HP is a civilian
  stat) → out of scope, not a buildable faction scout.

**★ Cross-note (infantry):** TD **rocket infantry → 300¢** (align with the other rocket-infantry
anchors; was 200) — matters for cargo/transport pricing (Σ passengers). Feeds the rocket-trooper
infantry anchor.

---

## ✅ FireSupport — LOCKED 2026-07-26 (fragile, LONGEST range 10000, NO anti-air)

**Role:** weak armor + the longest direct range — slow + fragile → it must OUTRANGE the tanks to
survive. **Range = 10000** (revised up from 7500; leaves the direct-fire gun ladder). **NO anti-air**
(strip it — e.g. GDI MLRS loses its AA).

| | Unit | HP | Speed | Range | DPS | cost0 |
|---|---|--:|--:|--:|--:|--:|
| **Baseline** | `td_gdi_mlrs` (GDI MLRS) | **25000** | **80** | **10000** | **400** | **1000** |
| **Verifier** | `latinsyndicate_missiletruck` | **50000** | 80 | 10000 | **800** | **2500** |

- ✓ identity: 2×HP + 2×DPS + same spd/rng → exactly 2.5× cost (1000 → 2500). (o1.5 / p2 / q4 → 2.5.)
- **Baseline (GDI MLRS):** iconic fragile rocket support. **AA stripped**; weapon buffed to **DPS 400**
  (from ~188); range 9920→10000; keeps HP 25000 / speed 80 / cost 1000.
- **Verifier (Latin Syndicate missile truck):** fits Latin's "best artillery / rocket-artillery"
  faction identity. Restat: HP 30000→50000, cost 1000→**2500** (premium tier), speed 75→80, range
  7777→10000, DPS →800. *(NB the 1000→2500 cost jump makes it a premium unit — flag if you want it
  kept cheaper and a different verifier named.)*
- **Ladder (cost):** … MBT 800 · **FireSupport 1000** · LineBreaker 1200 · HighTech 2000 · Dreadnought
  3000. Fragile long-range members to rebalance into band: prism tank, hover-MLRS, tank-killer,
  missile trucks, SSM launcher, Type-89 MLRS, etc.
- **Consequence:** FireSupport at 10000 now sits where Artillery / ArtilleryTank were "beyond 7500" —
  resolve the range overlap when we lock those (they must extend past 10000).

---

## ✅ AntiAir Vehicle (`^AntiAirVehicleTemplate`) — LOCKED 2026-07-26 (great vs air, HORRIBLE vs ground)

**Concept:** dedicated mobile anti-air — short range + Medium armor vs ground (bad on purpose), massive
range + firepower vs air (excellent). All members = **Medium armor**. Absorbs the planned AntiAirTank.

| | Unit | HP | Speed | Range(GND) | GND Dmg | Reload | DPS | cost0 |
|---|---|--:|--:|--:|--:|--:|--:|--:|
| **Baseline** | `latinsyndicate_diablo` | **50000** | **125** | **5000** | **10000** | **15** | **667** | **600** |
| **Verifier** | `steelconsortium_barracuda` | **100000** | 125 | 5000 | 20000 | 15 | **1333** | **1500** |

- DPS = 10000 / 15 = 666.7 (burst 1, wc 1.0); verifier 2× via dmg 20000 / 15. ✓ identity 2×HP + 2×DPS +
  same spd/rng → exactly 2.5× cost (600 → 1500). **GND range band 4500–5500** (baseline 5000 ±500).
- **★ AA weapon = the ground weapon +50% RANGE / +100% DAMAGE** → Diablo AA: **range 7500, damage
  20000**. Priced ONLY on the GROUND weapon; the strong AA weapon is **FREE** (the class's whole appeal).
  **NO class K** — they are MEANT to be horrible vs ground (short 5000 range, Medium armor) and to shine
  vs air (7500 range, 2× damage). **Supersedes the old AG/AA pair law for this class.**
- **★ AA-primary members (bikes / Pitbull) — SPLIT rule:** their single dual-purpose missile is split
  into a **separate ground weapon** (priced, range in the 4500–5500 band) **and a separate AA weapon**
  (+50% range / +100% damage off it), applied as two armaments.

**Membership (drafted):**
- **All troop transports that have anti-air** (new rule; needs a Cargo+AA scan across the roster).
- **TD/TS bikes** moved from Scout: `td_nod_reconbike`, `td_nod_chemicalattackbike`,
  `ts_nod_attackcycle` (SPLIT their missile per the rule above). **`naxis_bmwbike` STAYS Scout** (its
  two weapons are ground-only WW2 MGs — no AA).
- **`latinsyndicate_diablo`** (baseline — Latin's main AA vehicle).
- **`ts_gdi_pitbull`** (missiles) → AntiAir (AA-primary, SPLIT rule).
- **Armed AA transports** (e.g. `ra2_soviets_flaktrack`, Cargo 5) → AntiAir template for armor + the
  derived AA weapon, but **priced by the cargo rule** (below), not the ground-weapon formula.
- **`japan_armoredcar`** (MG + AA MG, non-transport) — FLAG: stays Scout (maintainer "currently scout ⇒
  stays scout") or move to AntiAir? (has AA.)

### Findings (2026-07-26 roster scan)
- **`ra2_soviets_flaktrack` = TRANSPORT** (Cargo MaxWeight 5) + AA. Its AA range is already GND×1.5
  (5528→8292) → the +50%-range rule is native here. **Priced by the cargo rule** (Σ passengers), see
  the reconciliation below.
- **`latinsyndicate_diablo` = NON-transport**, clean GND cannon (dps 193, rng 7300) + AA cannon (rng
  10450 ≈ +43%). Latin's dedicated AA → strong **baseline candidate** (K 1.0). cost 1200.
- **`td_nod_reconbike` / `td_nod_chemicalattackbike` / `ts_nod_attackcycle` / `ts_gdi_pitbull` =
  AA-PRIMARY** — their only real weapon is AA missiles (+ a dmg-1 point-defense laser); **no distinct
  ground weapon.** ⇒ the "AA = +50%rng/+100%dmg OFF the ground weapon" rule has nothing to derive from.
  **RULING NEEDED:** (a) give them a ground weapon and derive AA from it, or (b) treat their
  anti-vehicle missile as the "ground" weapon and add a +50%/+100% AA variant?
- **`naxis_bmwbike` = GROUND-ONLY** (two WW2 MGs, no AA) → **STAYS Scout** (resolved, not AA).
- **`japan_armoredcar` = scout-with-AA** (MG + AA MG, non-transport). Maintainer rule "currently scout
  ⇒ stays scout" → keep Scout, but flag as an AA candidate.

### ★ CARGO × weapon pricing (CORRECTED by maintainer 2026-07-26)
For an armed transport the **price is FIXED = Σ(passenger costs at capacity)** — **NOT** ×1.25. The
**1.25× is the special modifier K applied INSIDE the balance formula**, which at that fixed price budgets
the unit **weaker combat stats**: `formula(stats) = Σ / 1.25`. So the transport pays full Σ for its
cargo utility and receives combat stats as if it cost Σ/1.25 (weaker at the same price). Unarmed
transport = Σ, no weapon (K n/a). Flak Track: price = Σ(5 passengers), stats solved with K 1.25; its
AntiAir membership only governs armor + the derived AA weapon.

**RESOLVED (all locked):** baseline Diablo @600 / verifier Barracuda @1500; AA = +50%rng/+100%dmg free,
NO class K (horrible-vs-ground / great-vs-air is the point); AA-primary units SPLIT their dual weapon;
cargo K as corrected above. **Only open flag:** `japan_armoredcar` placement (Scout vs AntiAir).

**★ Added member (maintainer 2026-07-26):** `asianalliance_pulverizer` (85000 HP, range 5517 → fits the
GND band 4500–5500) → **move to AntiAir Vehicle**, and **remove its "disabled when Pulverizer Mecha is
unlocked" prerequisite** (they're different units). *(NOT the `asianalliance_pulverizermecha`, which is
the Dreadnought member.)* Implement in the boot-gated pass.

---

## ✅ ArtilleryTank — LOCKED baseline 2026-07-26 (TANKY, TURRETED artillery — verifier pending)

**Definition (maintainer 2026-07-26):** the **tankiest artillery** — a unit with the **Artillery role +
a TURRET** (`AttackTurreted`), Medium armor, more durable than the frontal-facing (Light-armor) pure
Artillery. Range **12000 (band 10000–14000)**.

| | Unit | HP | Speed | Range | DPS | cost0 | Armor |
|---|---|--:|--:|--:|--:|--:|--:|
| **Baseline** | `ixian_ixcombatsiege` | **80000** | **80** | **12000** | **80** | **1200** | Medium |
| **Verifier** | *(pending — same-tier, see below)* | 160000 | 80 | 12000 | 160 | **3000** | Medium |

- Baseline = Ixian Combat Siege (Tier 2). Verifier target = 2×HP / 2×DPS / 2.5×cost, same spd/rng
  (160000 / DPS 160 / 3000). ✓ identity holds.
- **Verifier pick OPEN — tech-tier matched (maintainer wants baseline+verifier same tier):** Combat
  Siege is Tier 2; the only other Tier-2 turreted artillery is **`schwarzermond_lunargrille`** (the
  "lunar grille"). Also a natural premium option: **`ts_gdi_juggernautmkii`** (the Juggernaut Mk II
  upgrade — tier to confirm). **Name the verifier.**
- **`naxis_sturmtiger` = slow HEAVY member** of ArtilleryTank (250000 HP / range 14000 fits the band;
  keeps its speed 30 via the rebalance method — members keep their own speed). NOT the verifier (too
  slow to share the baseline's 80) and NOT Dreadnought (its 14000 range far exceeds Dreadnought's 6500).

**Membership = Artillery-template + `AttackTurreted` (roster scan 2026-07-26):** `ordos_cobratank`,
`ordos_pythontank`, `japan_waveforceartillery`, `ra1_soviets_grad`, `asianalliance_howitzer`,
`schwarzermond_lunargrille`, `schwarzermond_mars`, `td_gdi_archerartillery`, `forgotten_missilevan`,
`forgotten_mlrs`, `ts_gdi_juggernaut` (+`juggernautmkii`), `ts_nod_artillery` + the Combat Siege
baseline + Sturm Tiger. *(`ra2_tractor_driveby` = flag, likely a special/civilian driveby.)*
**Non-turreted artillery stays pure Artillery** (frontal, Light armor, range 15000).

---

## ✅ Artillery (pure) — LOCKED baseline 2026-07-26 (FRONTAL, fragile, Light armor, longest range)

**Definition:** frontal-facing (**NO turret**), **Light armor**, fragile, **range 15000 (band
13000–17000)** — the longest-range ground class. Distinct from the turreted, Medium-armor ArtilleryTank.

| | Unit | HP | Speed | Range | DPS | cost0 | Armor |
|---|---|--:|--:|--:|--:|--:|--:|
| **Baseline** | `ra1_allies_alliedartillery` | **20000** | **60** | **15000** | **300** | **600** | Light |
| **Verifier** | *(pending — V2 candidate)* | 40000 | 60 | 15000 | 600 | **1500** | Light |

- ✓ identity: 2×HP + 2×DPS + same spd/rng → exactly 2.5× cost (600 → 1500). DPS 300 (round, high
  per-shot / slow reload).
- **Verifier candidate = `ra1_soviets_v2rocketlauncher`** — restat to 40000 / DPS 600 / 1500, same
  60 spd / 15000 rng. **Confirm** (mind the tech-tier match — Allied Artillery is early T1).
- **Members = Artillery-template WITHOUT `AttackTurreted`** (frontal): `td_nod_artillery`,
  `naxis_grille`, **`naxis_brummbr`** (Brummbär — frontal casemate gun ⇒ pure Artillery, NOT
  ArtilleryTank; needs the umlaut rename), `naxis_donnerschlag`, `ra1_soviets_v1rockettruck` /
  `v2rocketlauncher` / `nuclearv2launcher`, `ra2_soviets_v3rocketlauncher`, `td_nod_specterartillery` /
  `chemicalssmlauncher`, `japan_ballista`, WC2 ballista/catapult/siege-engine, `ixian_ixmissiletank` /
  `ixsiegetank`, `steelconsortium_dagger` / `hammerheadartillerytank`, `ordos_deviatorartillery`,
  `latinsyndicate_burrito`, `futuretech_athenacannon`, `cabal_artilleryspider`, `asianalliance_viper`,
  `tkm_dronepodtruck` / `tornadoglauncher`.
- **Deploy siege tanks** (`terran_siegetank` 150k HP, `siege_tank`, `missile_tank`) = tanky DEPLOY
  specials → FLAG (their own handling, not fragile Light artillery).

---

## ★ FUTURE-AUDIT RULES (maintainer 2026-07-26 — record now, wire into `run_all.sh` later)

1. **Projectile speed = range ÷ 2** for every weapon (e.g. range 15000 → projectile speed 7500).
   **EXCEPTION:** interceptable / shoot-down-able missiles (e.g. the **V3**) keep their own (slower)
   speed so they *can* be shot down. → a **projectile-speed audit** will enforce this.
2. **Weapon type ↔ unit type binding:** each unit CLASS is bound to a weapon TYPE, and a unit's weapon
   must inherit the weapon type bound to its class — unit type and weapon type are always linked. → a
   **weapon-type-binding audit** will enforce this (depends on the WeaponClass sidecar + the §13
   warhead library being wired in).

---

## ★ DEFENSE PRICING FORMULA — 3-input (maintainer 2026-07-26) — implemented in `formula.py`

Static defenses have no speed → the 4-input v2 formula's speed term is meaningless (like "speed 100" as
a placeholder). **Defenses use a new 3-input formula** (HP, Range, DPS) built with the SAME
degree-1 / degree-2 / degree-3 logic, symmetric across all three inputs, so O = P = Q = cost0 at the
baseline:

    h = hp/hp0 ; r = (range/range0)*special ; d = dps/dps0
    O = (h + r + d)/3        * cost0   (degree 1 — mean of the singles)
    P = (h*r + h*d + r*d)/3  * cost0   (degree 2 — mean of the pairs)
    Q = (h * r * d)          * cost0   (degree 3 — the triple product)
    Cost = (O + P + Q)/3

`formula.py`: `class_baseline_estimators_3` / `class_baseline_price_3` /
`solve_class_baseline_range_3` (price stays LINEAR in range → closed-form solve). Verified numerically:
- baseline → O=P=Q=cost0, price=cost0 ✓
- **fully symmetric:** 2× any ONE input → **1.667×** cost (all three interchangeable — the "same logic").
- 2×HP + 2×DPS + same range → **2.778×** (vs the mobile 2.5×); 2× all three → **4.667×**.

**★ ONLY for defenses** (mobile classes keep the 4-input formula + 2.5× verifier). **OPEN — defense
verifier convention:** pick (a) 2×HP + 2×DPS + same range = 2.778×, (b) a 2×-all = 4.667× tripwire, or
(c) another.

## ★ REARMABLE AIRCRAFT — needs its OWN formula (maintainer 2026-07-26; when we reach aircraft)

Bombers / rearmable aircraft return to base to reload, so the weapon `ReloadDelay` (currently a
placeholder ~250 multiplier) does NOT reflect their real damage cadence — the same nonsense as
"speed 100" for defenses. **Effective DPS must be driven by the SORTIE cycle** (fly out → attack → fly
back → rearm), not the weapon reload. **TODO: derive an aircraft-specific formula when we lock the
aircraft classes** (applies to all return-to-base aircraft; loitering gunships/fighters may keep the
normal form — decide per subclass).

## ★ DEFENSE CLASS RULES (maintainer 2026-07-26)

**Formula:** the 3-input defense formula (HP, Range, DPS — above). **★ Verifier convention = 2.5×HP +
2.5×DPS + same range → exactly 4.0× cost** (maintainer 2026-07-26 — the ONLY "both-round" point of the
3-input formula: price = (2·2.5+1)²/9 = 36/9 = 4). Replaces the earlier 2.778×. More extreme band, but
clean. **Caveat:** 4× only makes sense where a class spans a real range (Basic/AntiAir/Advanced);
**SuperDefense is a narrow epic tier** (4000–5000), so 4× of 4000 = 16000 has no real unit → Super uses
NO 4× verifier (see its section).

**★ Defense HP granularity + regen (maintainer 2026-07-26):** defenses regenerate at a **FLAT 10 HP per
step** (NOT HP-scaled like units), so their self-heal is constant regardless of HP ⇒ **defense HP may be
in EITHER 1000 or 2500 steps** (both fine). No per-actor `Step = HP/n` for defenses.

**★ Defense ARMOR scheme (already in the templates, `defaults.yaml` 2005–2071):** BasicDefense =
**Concrete**, AntiAirDefense = **Concrete**, AdvancedDefense = **Steel**, SuperDefense = **Steel**,
Bunker = **Steel**. Build-time modifiers: Basic **100** (longest), Advanced/Bunker **75**, AntiAir/Super
**50** (quickest). *(Advanced/Super build-times may need to lengthen to match "plan-ahead" intent +
the new power/cost ratios — review in the implementation pass.)*

**★ Power-to-Cost per defense TYPE** (was a uniform `cost/20`):
| Type | Power draw | Build-time modifier |
|---|---|---|
| BasicDefense | **cost / 20** | LONGEST (early-game — fine as is) |
| AntiAirDefense | **cost / 20** | QUICKEST (specialized, need it fast) |
| AdvancedDefense | **cost / 10** | long (the thing you plan in advance) |
| SuperDefense | **cost / 5** | long (scale accordingly) |
Build-time modifiers scale with the power/cost ratio.

**★ SuperDefense membership rule:** any defense with a **footprint bigger than 1×1 cell** (Building
`Dimensions` > `1,1`) — **EXCEPT anti-air turrets** — is a SuperDefense (verified: Grand Cannon =
`2,2`). PLUS **1×1 defenses that are extremely powerful** (large HP AND large cost) also qualify —
cross-reference the roster. Named members: `ra2_allies_grandcannon` + Latin **SML turret**, Asian
Alliance **plasma cannon**, Ixian **storm lasher**, … (full footprint scan when we lock SuperDefense).

**★ Bunker rules:**
- **Pure bunker** (holds cargo, no own weapon) → price scales with **HP + number of cargo slots** (the
  garrison capacity is the value).
- **Combined defense + bunker** (e.g. **RA1 Bastion** — own weapon AND holds cargo) → **special K =
  1.25** (same as armed vehicle transports — weaker stats at the same price).
- **Pillbox with a FIXED garrison** (an infantry unit permanently inside, whose weapon IS the pillbox's
  weapon) → priced as a **BasicDefense** on that garrisoned weapon's DPS (the garrison = the weapon).

---

## ✅ BasicDefense — LOCKED baseline 2026-07-26 (range band 6500–7500)

| | Unit | HP | Range | DPS | cost0 |
|---|---|--:|--:|--:|--:|
| **Baseline** | `td_gdi_guardtower` | **100000** | **7000** | **400** | **500** |
| **Verifier** | `japan_japanesemgnest` | **250000** | 7000 | **1000** | **2000** (4×) |

- All BasicDefenses **range 6500–7500** (7000 middle). Power draw = **cost / 20** (guardtower 500 →
  power 25), longest build-time modifier.
- Verifier = Japanese MG Nest restatted to **2.5×HP + 2.5×DPS + same range = 4× → 2000** (250000 HP /
  DPS 1000 / 7000). Current MG Nest = 110000 HP / dual MG (dmg 4000 ×5 / rd 25 bd 5) / cost 1000.
- Baseline guardtower restat: HP 60000→**100000**, range 6720→**7000**, weapon → **DPS 400**, keep
  cost 500.

---

## ✅ AntiAirDefense — LOCKED 2026-07-26 (pure AA, long range, Concrete armor, quickest build)

| | Unit | HP | Range | Dmg | Reload | DPS | cost0 |
|---|---|--:|--:|--:|--:|--:|--:|
| **Baseline** | `ra2_soviets_flakcannon` | **150000** | **12500** | **12000** | **12** | **1000** | **600** |
| **Verifier** | `latinsyndicate_latinaadefender` | **375000** | 12500 | 30000 | 12 | **2500** | **2400** |

- DPS = 12000 / 12 = **1000** (burst 1). **Range band 10000–15000** (12500 middle). Armor **Concrete**,
  power = cost/20, quickest build time. **Pure-AA priced directly on the AA weapon** (no ground-weapon
  split — confirmed).
- Verifier = Latin **Air Defender** (dual flak, naturally tankier) → **2.5×HP + 2.5×DPS + same range =
  4× → 2400** (375000 HP / DPS 2500 / 12500). ✓ 4× convention.
- HP 150000 / 375000 clean (flat-10 regen → any 1000/2500 step is fine).

---

## ⚠ WEAPON-CLASS EXTRACTION BUG (found 2026-07-26) — extractor ≠ authoritative

`extract_stats.py`'s `design_weapon_class` is WRONG — it averages a weapon's **versus-armor templates**
via a hard-coded `_WEAPON_CLASS_OVERRIDES` table, instead of reading the authoritative sidecar
`weapon_classes.yaml` for the weapon's **own class**. Two symptoms found:
- **Guard tower → 1.0833** = mean of its 6 recognized versus templates `[Grenade 1.0, Shrapnel 1.0,
  Flak 1.0, HeavyAA 1.25, HeavyMissile 1.25, MediumMissile 1.0]` = 6.5/6 = 13/12. (Should be its OWN
  class ≈ 1.0.) `^TankDestroyerCannon` → None (unrecognized).
- **Obelisk → 1.0** because the override table has **`LaserWeapon: 1.0`** — WRONG; sidecar + legacy
  Excel say **1.25**. Also **`ShrapnelWeapon` should be 1.25** (Heavy), table has 1.0.

**★ THE RULE IS AUTOMATIC (ARMOR_SYSTEM.md — canonical Versus law):** a weapon's class = read from its
main SpreadDamage warhead's **`Versus: Shield`** value — Light/Medium/Heavy = step 6/5/4 = Shield
**110 / 125 / 140 → 0.75 / 1.0 / 1.25** (+15 shield = +0.25; superheavy bands 155/170/185 → 1.5/1.75/2.0).
Multi-warhead weapons = arithmetic mean. So `^Grenade` (Light Demolition, Shield 110) = **0.75**,
`^ShrapnelWeapon` (Medium Concussion, Shield 125) = **1.0** — automatic, no hand list.

**✅ FIXED 2026-07-26:** `extract_stats.py` now derives `design_weapon_class` **automatically from the
Versus Shield** (`weapon_class_from_versus`, `source="versus_shield"`) — self-correcting from the yaml,
so a stale value can't recur. The `weapon_classes.yaml` sidecar is the **fallback** for weapons with no
Versus of their own; the stale hard-coded override table was deleted (it had LaserWeapon 1.0, Grenade
1.0 — both wrong; Shield-derived gives 1.25 / 0.75 correctly). **`^ShrapnelWeapon` reverted to 1.0**
(maintainer: shrapnel was right, GRENADE was the wrong one — now 0.75 via Shield). New gate
**`--check-weapon-classes`** fails on any template lacking BOTH a Versus Shield and a sidecar entry;
after the automatic law it's down from 48 → **10** (targeting-only `^AADeployTargeting`/`^DeployTargeting`
→ IGNORE set; abstract parents `^MissileWeapon`/`^MG`/`^LightMG`/`^AntiGroundMissile`; `^SnipeWeapon` →
0.75; `^D2K_Cannon`/`^DRPlasmaWeapon`/`^TSDefaultMissile`). Finish those 10, then wire into `run_all.sh`.
Ledgers pick up the corrected classes on the next sanctioned `extract_stats` run.

**★ Robustness upgrade (to build): classify by the Versus FLOOR/STEP, not just Shield** — the Shield can
be non-standard (Charged Tesla Shield = 200, an EMP bonus). The floor is the reliable signal:
`class = 0.75 + (floor − 10)/60` → floor 10/25/40/55 = 0.75/1.0/1.25/1.5. Cross-check floor vs Shield vs
the sidecar and **auto-update the sidecar** on mismatch (single always-accurate source).
**Off-ladder EXCEPTIONS (hard-set in the sidecar):** `^ToxicWeapon` = **0.5**; `^TeslaChargedWeapon` +
`^NuclearWarhead` = **1.5** (Superheavy, step 3). Sidecar: `^NuclearWarhead` 1.25 → **1.5**.

**★ NUCLEAR WARHEAD Versus fix — CONFIRMED 2026-07-26 (BOOT-GATED weapons.yaml edit, apply in the pass):**
`^NuclearWarhead` is currently a **Heavy** profile (step 4, 100→40, Shield 140 = class 1.25). Change to
**Superheavy (step 3)** to match its 1.5 class — KEEP the armor order, change the step 4→3:
- **Main warhead:** Superheavy 100, then −3 per row (Plate 97, Heavy 94, Flak 91, Medium 88, None 85,
  Light 82, Heroic 79, Scout 76, Steel 73, Concrete 70, Wood 67, Spaceship 64, Helicopter 61, Bomber 58,
  **Fighter 55** floor), **Shield 155** (standard top+floor; NOT the Tesla's 200 — nukes get no shield
  bonus). **HAZMAT 50 kept.**
- **% warhead:** superheavy window **30 → 15** (Superheavy 30 … Fighter 15, step 1), **Shield 45** (was
  the Heavy window 25→10 / Shield 35). Matches the Charged Tesla's % ladder (30→15).

## ✅ AdvancedDefense — LOCKED baseline 2026-07-26 (1×1, Steel armor, plan-ahead build)

| | Unit | HP | Range | DPS | cost0 |
|---|---|--:|--:|--:|--:|
| **Baseline** | `td_gdi_advancedguardtower` | 200000 | **9000** | 1250? | **UNDER REVIEW (1000?)** |
| **Verifier** | `ixian_rocketturret` | 2.5×HP | 9000 | 2.5×DPS | **4× baseline** |

- Baseline range band **8000–10000**. **1×1**, Steel armor, power = cost/10. Heavy-missile-only (wc 1.25).
- **⚠ COST + POWER UNDER REVIEW (maintainer 2026-07-26):** with the 4× convention, cost0 1200 → verifier
  4800, overshooting SuperDefense (4000). Also the guard tower as specced (200k HP / DPS 1250 / range
  9000) is **stronger than its old 1600 price** — underpriced at 1000. **The footprint rule (Advanced
  1×1 vs Super 2×2) already keeps the classes distinct, so cost overlap is harmless** — the real issue
  is value-for-cost. **Ideas (pick one):** (A) cost 1000, keep stats (strong-but-cheap anchor; verifier
  4000 = meets Super floor); (B) cost 1000 + **trim DPS 1250 → ~800** (a clean 2× step over Basic's 400,
  fair; verifier 4000); (C) cost 1200, verifier 4800 (footprint separates it from Super anyway).
  **Recommend (B).**
- Verifier = **Ixian rocket turret** (1×1, clean K=1.0) → 2.5×HP + 2.5×DPS + same range = 4× baseline.
  **Resolve its Tier-2 → Tier-3 move + the Ixian AA-gap** (let their guard-tower after-upgrade AA count,
  or give them a dedicated AA turret).

## ✅ SuperDefense — LOCKED baseline 2026-07-26 (epic, 2×2 footprint, Steel armor, power = cost/5)

**Membership:** footprint > 1×1 (except AA turrets) + extremely-powerful 1×1. **Baseline =
`asianalliance_plasmacannon`** @ **4000** (300000 HP, range 14000, **2×2**, PlasmaWeapon 1.25, **no
charge** — the clean epic candidate). Members (MOVE out of AdvancedDefense): `ra2_allies_grandcannon`,
`ixian_stormlasher`, `latinsyndicate_smlturret` (all 5000, 2×2). **NO 4× verifier** — a 4× of 4000 =
16000 has no real unit; SuperDefense is a **narrow epic tier** (baseline 4000, members clustered
4000–5000), so it's baseline-anchored only (the epic ceiling), not a full baseline→4× band. **cost0
4000 + range band TBD.**
`cabal_heavycabalobelisk` (2400, charge) is NOT Super (charge → Advanced/own handling).

**★ Tesla Coil EXEMPT from charge-up rule** (maintainer 2026-07-26): both RA1+RA2 tesla coils have a
QUICK charge (25) and are already strong → NO 0.75 discount, NO reload=2×charge; **K = 1.25 (EMP)**;
effective reload still 125 (RA1: ReloadDelay 100 + InitialChargeDelay 25). See [[cameo-charge-up-rule]].

---

## 🔤 NAMING FIX — dropped umlauts (maintainer 2026-07-26) — BOOT-GATED, via rename tool

Rule: umlauts transliterate to the base letter (ü→u, ö→o, ä→a, ß→ss). A roster scan (display-name
umlaut vs actor id) found **only two ids that DROPPED the umlaut instead of transliterating:**
- `naxis_brummbr` → **`naxis_brummbar`** (Brummbär)
- `naxis_kbelwagen` → **`naxis_kubelwagen`** (Kübelwagen)

`schwarzermond_ubermensch` (Übermensch) is already correct (Ü→u); `frank.nax` is a codename (not
name-derived). **To fix via `tools/rename/apply.py` + a `rename_map` (touches rules/sequences/weapons/
cameos/AI/fluent), then BOOT-GATE.** (Also the `frank.nax` display name "Übermutant" shows a mojibake
`�` — check the source file encoding separately.)
