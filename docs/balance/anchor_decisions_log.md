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

**Class rules:** all members → **Light armor**; rebalanced into the baseline→verifier band; **keep
each unit's current speed** (don't make them feel different) *unless* one is clearly out of place.

**Members (maintainer-listed):** latinsyndicate_rushertank (Latin Rusher), yuri_lashertank (Lasher),
ts_nod_ticktank (Tick — ⚠ slow/deploys, may feel out of place), ra1_allies_sheridanassaultta
(Sheridan), schwarzermond lunar panzer, japan_shrineminitank (Shrine), panzer.nax (Naxis Panzer III
— ⚠ currently 100k HP, comes DOWN into band), futuretech_robottank (Robot Tank).

**Members (my additions — fast/light tanks, tracked, tank-gun, NOT scouts):**
- `terran_vulture` (StarCraft — fast hover raider, 125 spd)
- `latinsyndicate_diablo` (Latin — light tank)
- `asianalliance_viper` / `asianalliance_quasar` (Asian Alliance light tanks)
- `steelconsortium_manta` (Consortium hover light tank)
- `ixian_shockraider` (Dune Ixian fast light)
- `cabal_ravager` (TS Cabal light)
- `naxis_kbelwagen` (Naxis light recon tank)
- `japan_armoredcar` (Japan light)
- **Ambiguous — probably SCOUT-vehicle, not LightTank (confirm):** ordos_raider (180 spd, Scout
  armor), ts_gdi_pitbull, forgotten_raidercar, ra2_allies_ifv (transport), futuretech_salamanderifv.
