# Class-anchor decisions log (maintainer-confirmed, one class at a time)

_Running record of the collaborative baseline+verifier definition (started 2026-07-25). Each class
is LOCKED here once the maintainer confirms; then `fit_class` + sign-off + `defaults.yaml` template
work follow. Fixed MBT anchor pivot: **Tiger `tiger.nax` = 100000 HP / 100 spd / 5000 rng / 10000
dmg @ 50 reload / cost0 800** (DPS 200)._

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

**Baseline:** **RA1 Soviet Mammoth Tank** (`ra1_soviets_mammothtank`), **cost0 2000** (its nostalgic
price). Keep speed 50 + range ~6400; HP/Damage set by `fit_class` to price at 2000 in-band (current
375000 HP likely comes down — HP was scaling faster than cost).
**Verifier:** **Siege Mammoth Tank** (`ra1_soviets_siegemammothtank`, the upgraded version) at
**exactly 2.5× = 5000¢**, restatted to 2×HP / 2×DPS (same speed/range).
- Ladder so far: LightTank 400 · MBT 800 · **HighTechTank 2000**. (Maintainer called 2000 "twice the
  MBT baseline" — vs the Tiger's cost0 800 that's 2.5×; confirm MBT stays 800 or bumps to 1000.)
- **Apocalypse** sits as a heavy *member*, not the baseline.
- **Turreted tanks are ALWAYS Light / MBT / HighTech** (maintainer rule).

## ✅ TankDestroyer — DEFINITION LOCKED (baseline TBD)

**Role:** **frontal-facing (no turret), long range, anti-tank.** All the same kind.
**Members:** `ra1_allies_alliedtankdestroyer`, `ra2_allies_tankdestroyer`, `naxis_hetzer`,
`naxis_jagdpanzer`, Ordos Tank Destroyer. (Baseline/verifier pick next.)

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
