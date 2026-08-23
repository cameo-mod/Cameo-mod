# Baseline Audit — Summary

## AI personality audit

`audit_ai_personalities.py` verifies that the five personality-gated
`SquadManagerBotModuleCA` instances retain byte-identical shared fields and
that their consumed conditions exactly match the `GrantRandomCondition`
selector. Personality-specific differences are restricted to an explicit
tuning allow-list.

The implementation removes the stale `RushInterval` and
`RushAttackScanRadius` keys; neither exists in the vendored CA or pinned engine
SquadManager implementation. Steamroller is intentionally documented as
having at most one harasser because the engine always creates the first
guerrilla squad and YAML cannot express zero guerrilla units.

There is no current in-game personality announcement. A condition-triggered
notification/observer integration is a follow-up.

_One page. Details: [FINDINGS.md](FINDINGS.md) · raw tables: [baseline/](baseline/) ·
faction map: [../factions/MATRIX.md](../factions/MATRIX.md)._

Recurring code-health audits and their cadence are tracked in
[`PERIODIC.md`](PERIODIC.md) and [`periodic.json`](periodic.json).

## Counts by bug class

> **Note:** Counts below were generated from the baseline audit run. Significant work since then (weapon template splicing, renames, armor normalization, ContentPack migration) may have changed some counts. Re-run `tools/audit/run_all.sh` for current numbers.

| class | what | count (live tree) | severity profile |
|---|---|---|---|
| B8 | crash-class content | **0** distinct (was 3+ — fixed 2026-07-14: ts_nod_ticktank voxel, magicnuke sequence, ra2_cgtbnkbb/ctoutpbb missing assets; 2026-07-15: CABAL CreateEffect Image: fields removed, impact animations consolidated in misc.yaml, map actors renamed; 2026-07-24: RA2 weapons migrated to ContentPack, Yuri weapons headers restored, Naxis Kübelwagen encoding fixed, nuclearflash shader created) | crash |
| B1 | cross-faction leaks | 10 L1 + 13 L3 (+1,106 shared needing owners) | balance |
| B2 | illegal inherits | **328** concrete→concrete, 24 cross-faction, 0 dangling | balance-risk |
| B5 | AI wiring | **200** ids defined nowhere, 620 unloaded refs, 26 factions with unwired units | balance |
| B3 | upgrade direction | 12 anti-buff combos (2 suspicious, 1 verify, rest intended drawbacks), 4 dead upgrades, 5 dead-wiring families on 300–1,042 actors each | balance |
| B4 | upgrade coverage | 15 tracked upgrades, ~40 real uncovered combat slots (CABAL backup systems: avatar+widow done; `cabal_legion` does not exist — was renamed/removed) | balance |
| B6 | art/sequence refs | 11 missing images, 11 missing sequences, 542 orphan images | cosmetic→crash-risk |
| B7 | metadata rot | 24 duplicate-tooltip groups, 0 missing tooltips | cosmetic |
| B9 | numeric drift | bounds screen **clean** (TB23 fix held); 163 outlier leads | balance-minor |
| B10 | dead content | 345 orphan weapons, 542 orphan images, 16 dead conditions | hygiene |
| B11 | asset norms | 3,632 / 8,776 WAVs off-norm (mono/16-bit/22050 Hz); 131 PNGs over budget | hygiene |
| B12 | localization | 0 unresolved Fluent refs, 233 orphaned messages, ≤10% Fluent coverage | cosmetic |
| R2 | stacked multipliers | **757** units over the 2.0× budget; worst 36× (RA2 Allies) | balance |
| W | weapon uniqueness (DESIGN §10) | 36 same-faction + 42 cross-faction shared weapons; 95 carrier-only (IFV borrow, informational) | design/identity |
| G | garrison weapons (DESIGN §11) | **clean** (G1/G2/G3 = 0 after 2026-07-10 fixes; 30 design exceptions) | crash-free/balance |

## Top 20 findings

1. **`tatacitus` NukePower fires nonexistent `TSChemTacticalMissile`** — FIXED: changed to existing `TSTacticalChemMissile` with valid `tsnodmmsil` image (tiberiaalliances.yaml).
2. **RA2 Allies hero-infantry stack measures 36×** fresh-self power (Assault Squad + Vanguard + Infiltrators + Chromium/Prismatic lines) — worst in game; Yuri 30× behind it.
3. **ai.yaml: 200 references defined nowhere** — incl. `ra2naclon`, `nax2_chrono` (CABAL refs `tsgtcnstcabalb`/`tsntpulscabal` already removed).
4. **Stale "BuildingFractions Dune Universe" block** uses pre-ContentPacks names — entire section steers nothing.
5. **`raider.ordos` not in any AI build list** — FIXED: added to Dune Universe `UnitsToBuild` with weight 7 (also `runner.steel`, `orion.futu`, `yrrobo.futu`, 5 Naxis units still pending).
6. **`ra_doctrine_teslatech` doubles reload (Modifier 200) on 2 actors** — suspected Dark-Armament-class inversion; verify.
7. **`up_energizedarrows` has ReloadDelayMultiplier 125** on one actor — suspected inversion; verify.
8. **328 concrete→concrete inherits** — the Slave-Miner bug factory; Phase-1 queue, full grouped list in FINDINGS.
9. **13 L3 leaks**: CABAL/Forgotten/TS-Nod buildings inherit GDI/Nod concrete actors (tscabaltech→tsgttech etc.).
10. **Modern Fire Control Systems covers 15/33 of TS GDI** — all aircraft + half the infantry lack the roster-wide hook.
11. **WC2 tower upgrade names** (guard↔cannon swap) — FIXED; remaining 24 duplicate-tooltip groups still under review.
12. **Dead-wiring families on 1,042 actors each** (`usabombardament`, `usaholdtheline`, `usasearchndestroy`, `upsubliminal(2)`) + `upra2deso` on 302 — Generals-era hooks granted by nothing.
13. **3 player-visible raw Fluent keys** — STALE/RESOLVED: current `audit_fluent.py` F1 shows 0 unresolved refs.
14. **`wc2_orc_eye_of_kilrogg` TurnSpeed 2048** — FIXED: reduced to 28 (bounds screen still clean; high vision range remains by design as scout).
15. **345 orphan weapons + 542 orphan sequence images** — RAM/load-time dead weight.
16. **CABAL absent from Random AND Tournament pools, still titled "(WIP)"** — FIXED: CABAL added to both pools, WIP label removed.
17. **CABAL post-TB23 full stack = 3.9×** — over the 2.0 budget but sane; trim one Research multiplier or cap rank scaling.
18. **_old.yaml deprecated files removed** — tiberiansunold + warcraft2old rules/sequences/weapons deleted (20,919 lines).

## Recommended fix order (per MASTER_REPORT §4)

1. **B5 AI wiring** (items 3–5) — restore bot competence for pool factions; delete/fence the 620 unloaded refs.
2. **B3/B4 verify+fix** (items 6, 7, 10) and transcribe the remaining 526 `upgrades_intent.yaml` entries.
3. **B2+B1 structurally** via §12 Phase-1 per-faction migration (items 8–9), `dump_resolved.py`-verified; turn `audit_inherits` blocking in CI as factions land.
4. **B7/B9/B12 quick wins** (items 11, 13, 14) — ideal AI-agent batch work.
5. **B10/B11 hygiene** (items 15, 18) — orphan purge + per-directory WAV normalization; deprecated *_old.yaml files already removed.
6. **R2 rebalance** (items 2, 17) with tournament telemetry before touching Consortium-family numbers.

## Superweapon documentation audit (2026-07-25)

Full cross-reference of YAML superweapon/support power traits vs `FACTIONS.md` completed.
Raw data: [`latest/superweapon_audit.yaml`](latest/superweapon_audit.yaml).

**14 findings** (1 HIGH, 2 MEDIUM, 8 LOW, 3 INFO):
- **SW-001 (HIGH)**: Harkonnen Palace has `^PrimarySuperweapon` + `SupportPowerChargeBar` but **no power trait** — Death Hand Missile unimplemented (parked faction).
- **SW-002 (MED)**: Forgotten superweapon was "Tiberian Wildlife Rampage" in docs but YAML implements `NukePowerCA` (nuclear missile). FIXED in FACTIONS.md.
- **SW-003 (MED)**: CABAL listed "Data Worm, Satellite Hack" but YAML has `NukePowerCA` (CabalMagicNuke) + `FireArmamentPower` (Data Worm). Satellite Hack not found. FIXED.
- **SW-004–011 (LOW)**: Missing support powers in FACTIONS.md — Nod TS Cluster Missile, RA1 Allies Chrono Reinforcements, RA2 Allies Force Shield, Latin Syndicate EMP/Traitors, Schwarzer Mond name mismatch, Ordos name mismatch, WC2 Humans Slow/Invisibility, WC2 Orcs Bloodlust/Haste. ALL FIXED.
- **SW-012–014 (INFO)**: Protoss reuses SteelIonCannon weapon, Consortium missing Federation Support Teleport in ref table, TS GDI missing Drop Pods in ref table. ALL FIXED.

**Outpost 2 verified**: Supernova Missile IS implemented in `rules/outpost2.yaml` (`NukePower`, charge 9000). FACTIONS.md was correct.

**WIP faction superweapons discovered** (not in FACTIONS.md): Warzone 2100 (IonCannonPower + AirstrikePower + NukePower), Worms (Sheep Strike + Concrete Donkey), Win98 (Demo Disk Strike + Red Ring of Death), Warcraft 1 (Rain of Fire + Poison Cloud), WH40K (8 Deep Strike variants + Marauder Bomber + Inquisition).

## War Economy SpeedMultiplier bug (2026-08-03)

**FIXED**: `SpeedMultiplier@ra1_soviets_upgrade_wareconomy` in `ContentPacks/RedAlert/Shared/yaml/upgrades.yaml:134` used `Prerequisites` instead of `RequiresCondition`. Since `SpeedMultiplierInfo` has no `Prerequisites` field, the engine ignored it, making the trait **always active** at 110% — even without the War Economy upgrade researched.

**Impact**: Every unit inheriting `^WarEconomyTeamUpgradeRA1` (all harvesters via `^HarvesterTemplate`, all refineries via `^Refinery`) got a permanent +10% speed boost. For the Noid Harvester (base speed 50), this produced a displayed speed of 55 instead of 50. When combined with the Gap Generator shroud effect (80%), the result was 50 × 80% × 110% = 44.

**Fix**: Changed `Prerequisites` → `RequiresCondition` on line 135. The trait is now properly conditional on the `ra1_soviets_upgrade_wareconomy` condition granted by `GrantConditionOnPrerequisite`.

**Audit**: Swept all YAML files for the same pattern (`SpeedMultiplier` with `Prerequisites` but no `RequiresCondition`) — no other SpeedMultiplier instances found. **Superseded**: the bug class later proved to cover ALL conditional multipliers — see next section.

## Empty warhead type NRE (2026-08-04)

**FIXED**: Boot crashed with `NullReferenceException` in `WeaponInfo.LoadWarheads` (`ObjectCreator.CreateBasic` on the abstract `Warhead` base class) because two weapons had `Warhead@` nodes with **no type value**:

- `RA2MirageGun` — `Warhead@Effect:` → set to `CreateEffect` (`mods/cameo/weapons/redalert2.yaml`)
- `TSSAPCMissiles` — `Warhead@GrenadeFriendlyFire:` → set to `SpreadDamage` (`mods/cameo/weapons/tiberiansun.yaml`)
- `HighV` — `Warhead@Bullet_Medium_Percentage:` → set to `HealthPercentageDamage` (`mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`)

**Mechanism**: An empty `Key:` line parses to a null value. The merge's null-fallback (`overrideNodes.Value ?? existingNodes.Value`) only rescues the node when a same-key ancestor has a value; these two nodes had none. The engine constructs `WeaponInfo` for **every** top-level weapon node — including unused `^templates` — so a typeless warhead anywhere in the resolved ruleset is a boot crash, and `LoadWarheads` then calls `Game.CreateObject<IWarhead>(null + "Warhead")`, which resolves to the abstract `Warhead` class and NREs.

**Audit**: New `tools/audit/audit_empty_warheads.py` resolves the full manifest weapon set via the shared `miniyaml.Ruleset` and flags any resolved node whose key starts with `Warhead` but has no type (plus empty `Projectile:` as a suspect). 4,202 weapons checked, 0 remaining findings. **`utility --check-yaml` does NOT catch this class** — run the audit after bulk warhead/weapon edits. Boot-gate passed after the fix.

## Conditional-multiplier `Prerequisites:` sweep (2026-08-04, follow-up to War Economy bug)

**FIXED**: The War Economy bug class was wider than `SpeedMultiplier`. Every `ConditionalTrait`-based multiplier (`FirepowerMultiplier`, `DamageMultiplier`, `SpeedMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, `InaccuracyMultiplier`, `RevealsShroudMultiplier`, `DetectCloakedMultiplier`, ...) has **no `Prerequisites` field** — a `Prerequisites:` line inside such a block is silently ignored by the loader, making the multiplier **always active**. (`ProductionCostMultiplier` / `ProductionTimeMultiplier` legitimately support `Prerequisites` and are unaffected.)

Three more always-active `FirepowerMultiplier` instances were found and fixed (`Prerequisites:` → `RequiresCondition:`):

- `FirepowerMultiplier@ra1_soviets_doctrine_conscription` (Modifier 110) — `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/templates.yaml`
- `FirepowerMultiplier@global_conscription_buff` (Modifier 110) — `mods/cameo/rules/defaults.yaml`
- `FirepowerMultiplier@selectcolin` (Modifier 80) — `mods/cameo/rules/advancewars.yaml`

**Impact before fix**: Conscription gave +10% firepower permanently (not just while the doctrine was active); the same permanent-always-on behaviour applied to `global_conscription_buff` and Advance Wars CO Colin's firepower debuff.

**Sweep method**: scanned every `*.yaml` under `mods/cameo` for a `Prerequisites:` line whose parent block is any `*Multiplier@` trait other than `Production*` — 0 remaining instances after the fixes. The UK-economy / France-siege / RA2 commando-doctrine templates from the earlier audit notes are not present on this branch; nothing else was actionable here.

## W24 cluster 8 — 227mm / GDIRigMissilePod / MammothTusk (2026-08-20)

**Converted** three legacy mixed-stack missile weapons to the 3-way split (one warhead, one projectile, one effect):

- `227mm` (`mods/cameo/weapons/tiberiandawn.yaml`) → `^Warhead_MissileHE_Medium` + `^Projectile_Missile_Medium` + `^Effect_MissileHE_Medium`.
- `GDIRigMissilePod` (`mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`) → `^Warhead_MissileHE_Heavy` + `^Projectile_Missile_Medium` + `^Effect_MissileHE_Medium`.
- `MammothTusk` (`mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml`) → `^Warhead_MissileHE_Heavy` + `^Projectile_Missile_Heavy` + `^Effect_MissileHE_Heavy`.

**Preserved:** resolved per-shot damage totals (8000/32000/24000), percentage twins, `Range`, `ReloadDelay`, `Burst`, `ValidTargets`, local `Projectile` overrides (`Speed`, `Inaccuracy`, launch angles), impact sounds, target filters, and `ImpactActors`. Added local `Warhead@EffectWater` because the new `^Effect_MissileHE_*` templates do not provide one.

**Resolver diff:** `tools/audit/review_resolve_diff.py` clean. The flak bullet contrail colors (`ContrailStartColor: FF884400`, `ContrailEndColor: 000000FF`) were restored to the three non-AMT parent projectiles as a resolved-behaviour preserve.

**Audits:** `find_empty_warhead.py` 0; `audit_warhead_split` broadcast count 958, baseline lowered 965→958; `audit_physical_state_warheads` PASS; `audit_balance_drift` clean; `audit_duplicate_inherits` no new findings for the cluster; `utility --check-yaml` pre-existing errors/warnings unchanged.

**Boot-gate:** `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log` after the run.
