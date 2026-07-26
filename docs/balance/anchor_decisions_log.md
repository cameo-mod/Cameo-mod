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

**LOCKED (cost0):** ScoutVehicle 300 · LightTank 400 · TankDestroyer 600 · MBT 800 (Tiger pivot,
signed) · FireSupport 1000 · LineBreaker 1200 · HighTechTank 2000 · Dreadnought 3000.
**➡ NEXT: AntiAir Vehicle** (NEW `^AntiAirVehicleTemplate` — draft rules recorded below: **priced on
the GROUND weapon**, the AA weapon is a fixed **+50% range / +100% damage** bonus; absorbs the TD/TS
bikes + AA troop-transports + Diablo + flak-tracks; confirm the transport-K1.25 rule + baseline/verifier).
**Then:** ArtilleryTank (Ixian Combat Siege), Artillery → then the 5 DEFENSE classes → aircraft/naval.
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
| ~~FireSupport 7500~~ → **10000** | **10000** | 9500–10500 |

The **direct-fire gun ladder now ENDS at TankDestroyer 7000** (500-step, close brawler → long-range).
**FireSupport revised to 10000 (2026-07-26):** slow + very fragile → it must OUTRANGE the tanks to
survive, so it leaves the direct-fire ladder and jumps to 10000. Artillery / ArtilleryTank extend
*beyond* 10000 (indirect long-range — resolve the overlap when we lock them). Scout / AntiAir /
LineBreaker have their own ranges (LineBreaker short ~2500; Scout 4500). **These ranges override the
per-class range values below.**

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

**Class rules:** all members → **Light armor**; rebalanced into the baseline→verifier band.

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
  scout's own. cost0 **300** = nostalgic. NB the weapon is anti-infantry (SmallArms warhead) so this
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

## ◧ AntiAir Vehicle (`^AntiAirVehicleTemplate`) — DRAFT rules (maintainer 2026-07-26; finalize after FireSupport)

**Concept:** dedicated mobile anti-air. Absorbs the planned AntiAirTank plus new members. Rules named
by the maintainer 2026-07-26 — **NOT yet locked** (baseline + verifier TBD when we reach this class).

**★ NEW PRICING RULE (the class's identity):**
1. **Priced ONLY on the GROUND weapon** — balanced as if it were a ground unit.
2. **The AA weapon is a fixed function of the ground weapon: +50% RANGE and +100% DAMAGE** (AA range =
   ground range × 1.5, AA damage = ground damage × 2). The strong AA weapon comes "free" with the class
   → makes them *excellent* dedicated AA. **This SUPERSEDES the old AG/AA pair law for this class.**
3. **Transport-with-a-weapon → special K 1.25** *(maintainer tentative — "I think maybe")*: any troop
   TRANSPORT that also has a weapon carries a 1.25× special modifier; if that weapon is AA it lives in
   this template. **CONFIRM before locking.**

**Membership (drafted):**
- **All troop transports that have anti-air** (new rule; needs a Cargo+AA scan across the roster).
- **TD/TS bikes** moved from Scout: `td_nod_reconbike`, `td_nod_chemicalattackbike`,
  `ts_nod_attackcycle` (+ `naxis_bmwbike`? flag).
- **`latinsyndicate_diablo`** (Latin's main AA vehicle — moved from Support earlier).
- **Flak-tracks** (`ra2_soviets_flaktrack`) and similar.
- **AA-primary scout candidates to REVIEW** (have AA but weren't explicitly ordered moved — decide at
  lock time): `ts_gdi_pitbull` (missiles), `japan_armoredcar` (flak).

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

### ★ Reconciled CARGO × weapon pricing (proposed — confirm)
The cargo rule (cost = Σ passengers) and rule 3 (transport+weapon → K 1.25) combine cleanly:
**armed transport cost = Σ(passenger costs at capacity) × 1.25**; unarmed transport = Σ × 1.0. So the
Flak Track is priced Σ(passengers)×1.25, and its AntiAir-template membership governs its armor + the
derived AA weapon, NOT its price. *(Confirm this interpretation.)*

**OPEN before locking:** baseline + verifier pick (Diablo the cleanest non-transport anchor?); confirm
rule 3 + the reconciled cargo×1.25; does "ground-weapon-priced, AA free at 2× dmg" under-price dedicated
AA (need a small class K)?; the AA-primary bikes/Pitbull ruling (a vs b above); Armored Car placement.
