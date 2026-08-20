# Incident: TD GDI Release-Regression Investigation

**Status:** crash resolved (boot-verified 2026-07-17, ROADMAP §P0 CRASH) — the death palette commit (9579827e9) was reverted per user instructions. The `brik:` sequence crash was fixed by correcting references to existing TD filenames and boot-verified. The **TS-only death palette audit** was completed (`54816b1f3`, 2026-07-27): `tools/audit/audit_ts_death_palette.py` checked all 56 TS ContentPack YAML files and found/fixed 2 mismatches (`cabal_cyborgreaper`, `cabal_artilleryspider`).

## Evidence source

- Last known-good release: the local Cameo-IFV release install
- Current checkout: this repository
- Engine failure observed 2026-07-17:

```text
System.IO.FileNotFoundException: cameo|sequences/tiberiandawn.yaml:1161: futuretech_concretebarrier_brik.shp not found
```

## Confirmed crash cause

The current `brik:` sequence had two references to nonexistent `futuretech_concretebarrier_brik.shp`. The current asset tree contains the original TD files `bits/td/brik.shp` and `bits/td/brikicon.png`; the known-good release uses those names. Both current sequence references were corrected locally to those existing original filenames.

**Boot verification:** PASSED 2026-07-17 (ROADMAP §P0 CRASH: "Boot verified"). No new exception logs generated. The crash component of this incident is closed.

## TD GDI vehicle migration comparison

| Release actor | Current actor | Current image resolution | Finding |
|---|---|---|---|
| `MCV.GDI` | `td_gdi_mobileconstructionvehicle` | explicit `Image: mcv` | The global `mcv:` sequence now points to `td_nod_mobileconstructionvehicle_mcv.shp`. The release `mcv:` used `mcv.shp`. Both current factions still use `mcv`; the current and release sprite files are equal in size. This is a naming/ownership discrepancy, not yet proven to cause a visual regression. |
| `HARV.GDI` | `td_gdi_tiberiumharvester` | inherited `Image: harv` from `^TDHARV` | The global `harv:` sequence now points to `td_nod_tiberiumharvester.shp`; release used `harv.shp`. The current renamed asset equals the release file in size. Naming/ownership discrepancy, not yet proven as a visual regression. |
| `APC` | `td_gdi_apc` | `td_gdi_apc` | Matching faction sequence exists. |
| `JEEP` | `td_gdi_humvee` | implicit actor key | Matching faction sequence exists. |
| `MTNK` | `td_gdi_battletank` | implicit actor key | Matching faction sequence exists. |
| `HTNK` | `td_gdi_mammothtank` | implicit actor key | Matching faction sequence exists. |
| `MLRS` | `td_gdi_mlrs` | explicit `msam` | `msam` sequence exists in GDI sequences. |
| `gdiarcher` | `td_gdi_archerartillery` | `td_gdi_archerartillery` | Matching faction sequence exists. |
| `gdiassaultapc` | `td_gdi_assaultapc` | `td_gdi_assaultapc` | Matching faction sequence exists. |
| `gdihumvee` | `td_gdi_humveemkii` | explicit `newhumvee` | `newhumvee` sequence exists in GDI sequences. |
| `gdipredator` | `td_gdi_predatortank` | `td_gdi_predatortank` | Matching faction sequence exists. |
| `gdimammoth3` | `td_gdi_mammothtankmkiii` | `td_gdi_mammothtankmkiii` | Matching faction sequence exists. |

## Palette comparison

The GDI vehicle `PlayerPalette` overrides match release behavior:

- default `player`: MCV, APC, Humvees, tanks, MLRS, Assault APC, Predator, Mammoth Mk III;
- `player_rgba`: Boxer, Archer Artillery, Defense Rig.

The release and current `player` palette definitions, vehicle/aircraft body traits, and inspected sprite file sizes do not show a proven palette-data regression. The reported visual issue remains unconfirmed pending in-game reproduction.

## Firehawk comparison

- Release actor `gdifirehawk` and current `td_gdi_firehawk` both rely on the default `player` palette.
- The renamed Firehawk sprite has the same size as the release asset.
- The Firehawk husk uses the corresponding renamed sequence in the current tree.
- No data-only cause of palette flicker was established. Reproduce in-game before modifying palette, animation, or trait data.

## Remaining next steps

1. ~~**TS-only death palette audit**~~ — DONE (`54816b1f3`, 2026-07-27). `audit_ts_death_palette.py` found and fixed 2 mismatches in TS ContentPacks.
2. Run `audit_sequences.py` and record the current output in `docs/audit/latest/`.
3. Compare GDI and Nod sequence key ownership and actor image resolution using the release build, with explicit checks for MCV, harvester, Firehawk, Construction Yard transform, and all GDI vehicle tooltips.
4. Create a targeted reproducible map or in-game checklist before any palette change.
5. Update this incident and `docs/design/ROADMAP.md` only with verified findings.
