# Classic-four transport chassis batch — 2026-09-18

This batch applies the chassis-only HP and Speed projections from the
20260916 reference map to the four classic-four air transports. Carrier Cost
is deliberately unchanged: all four authored `Cargo.InitialUnits` loads
resolve to a passenger sum equal to the current carrier cost, and the cargo
pricing law owns that field.

| Actor | HP | Speed | Aircraft TurnSpeed | Self-heal Step | HpPerStep | Cost |
|---|---:|---:|---:|---:|---:|---:|
| `td_gdi_chinooktransport` | 100,000 -> **82,000** | 150 -> **124** | 30 -> **25** | 40 -> **33** | 5,000 -> **4,100** | **4,120 held** |
| `td_nod_chinooktransport` | 100,000 -> **82,000** | 150 -> **124** | 30 -> **25** | 40 -> **33** | 5,000 -> **4,100** | **3,853 held** |
| `ra1_allies_alliedchinooktransport` | 125,000 -> **88,000** | 125 -> **120** | 25 -> **24** | 50 -> **35** | 6,250 -> **4,400** | **4,300 held** |
| `ra1_soviets_hiptransport` | 150,000 -> **104,000** | 100 -> **118** | 20 -> **24** | 60 -> **42** | 7,500 -> **5,200** | **2,760 held** |

HP uses the 1,000 grid. Speed and aircraft TurnSpeed follow the helicopter
F19 law (`round(Speed / 5)`); self-heal uses the nearest integer to HP/2500,
and repair uses HP/20. Every actor already owns its Aircraft, Health,
Repairable and SelfHealing blocks, so no shared transport template is edited.

Cargo was inspected directly through `cargo_pricing.authored_load`: all four
loads are valid and filled to capacity, including the Nod Chinook's eight
authored passengers whose heavier passenger weights total 11. No Cargo,
InitialUnits, MaxWeight or passenger price changes are included.

## Playtest focus

- Check whether the lower transport HP makes infantry drops vulnerable without
  making transport production uneconomical; cost remains the passenger sum.
- Check the faster/slower reach changes at factory state and during unload.
- Treat any future transport cost change as a separate cargo-law decision with
  cross-carrier passenger-price impact.
