# Class-anchor decisions log (maintainer-confirmed, one class at a time)

_Running record of the collaborative baseline+verifier definition (started 2026-07-25). Each class
is LOCKED here once the maintainer confirms; then `fit_class` + sign-off + `defaults.yaml` template
work follow. Fixed MBT anchor pivot: **Tiger `tiger.nax` = 100000 HP / 100 spd / **5500 rng** / 10000
dmg @ 50 reload / cost0 800** (DPS 200). (Range bumped 5000→5500 on 2026-07-26 to complete the range
ladder — see the RANGE LADDER section.)_

## ★ RANGE LADDER (maintainer 2026-07-26 — verified consistent, steps of 500)

| Class | baseline range | band (±500) |
|---|--:|--:|
| LightTank | 5000 | 4500–5500 |
| **MBT** | **5500** | 5000–6000 |
| HighTechTank | 6000 | 5500–6500 |
| Dreadnought | 6500 | 6000–7000 |
| TankDestroyer | 7000 | 6500–7500 |
| FireSupport | 7500 | 7000–8000 |

Clean 500-step ladder; role order sensible (close brawler → long-range support). Artillery /
ArtilleryTank extend *beyond* 7500 (indirect long-range). Scout / AntiAirTank / LineBreaker have their
own ranges (LineBreaker is short, ~2500). **These ranges override the per-class range values below.**

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

## ✅ Dreadnought — UPDATED 2026-07-26 (Warhound baseline)

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

**⚠ CLOAK / special-K decision needed:** the cloak was introduced only to cancel the *Pulverizer's
gatling* K 1.25. With the **Warhound (no gatling, K 1.0)** as the new baseline, the Neo Cymek's cloak
(K 1.25) would **break** the 2.5× identity. Options: **(a) DROP the Neo Cymek cloak** → both K 1.0,
clean tripwire (recommended); (b) keep the cloak AND cloak the Warhound too (both K 1.25); (c) keep
the cloak as a deliberate verifier bonus (not a clean tripwire). **Awaiting your call.**

**Pulverizer Mecha** → scaled DOWN to a **member** at **cost 2500**, **range 6000** (the minimum of
the dreadnought band 6000–7000 — you wrote "(6500)", but 6500 is the band *center*; using 6000 as the
min — correct me if you meant 6500). Keeps its gatling. **Other members:** Neo Jagdpanzer.

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
