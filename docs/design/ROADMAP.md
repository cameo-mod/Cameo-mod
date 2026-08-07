# Cameo Roadmap — detailed work queue (rebuilt 2026-07-13)

_The living work queue, resumable by any agent. Rule zero: crashes and
bugs ALWAYS jump the queue. Ordering within a section: **quickest wins
first, then by severity**. Effort: S < 1h, M = one session, L = multi-
session. Every completed item gets its commit hash; every new order
lands here first. Goal: **mod-synthesis balance overhaul** (see ★ MAJOR
PROGRAM below) — finish infantry classes, then vehicles/aircraft/defenses/
naval. CABAL faction work is largely complete; remaining CABAL items are
flagged inline. Faction reference: [FACTIONS.md](../FACTIONS.md)._

> **Multi-agent repo.** Three contributors touch this tree: the
> maintainer (AedisToru), **333ggg** (i333ggg@yandex.ru — works Starcraft
> vultures, TS GDI riot troopers, `cabal.xlsx` rows), and **Devin AI**
> (leaves a log at `DevinCameoProject/DEVELOPMENT_LOG.md` in the external scratch folder). ALWAYS `git add <files>` scoped, never `-A`.
> Verify others' commits before building on them. Devin's 2026-07-12
> sound pass (obelcor3/samshot1 fixes) was reviewed and TRUSTED
> 2026-07-13; keep it. 333ggg's mine commits are self-contained (SC +
> GDI), unrelated to CABAL.

---

## ▶ ACTIVE — VEHICLE BALANCE APPLY + BACKLOG (2026-07-31)

**Vehicle ladder DESIGN is being re-tuned** — latest table = `docs/balance/anchor_decisions_log.md`
"⚠ REVISION 2026-07-31" (PENDING maintainer "did it fix the problems?" confirm). STRUCTURAL work DONE +
committed: `^MissileVehicleTemplate` + 10 reassignments (missile-MLRS family + Nod bikes) + `EpicBuff`
removal (`43df39235`); 5 earlier templates + buff-strip (`090d3d997`).

**Queue (priority order):**
- **[P0 RESOLVED 2026-08-04] Empty-warhead-type NRE on load** — two typeless `Warhead@` nodes
  (`RA2MirageGun` `Warhead@Effect:` in `mods/cameo/weapons/redalert2.yaml`,
  `TSSAPCMissiles` `Warhead@GrenadeFriendlyFire:` in `mods/cameo/weapons/tiberiansun.yaml`)
  crashed boot (`NullReferenceException` in `WeaponInfo.LoadWarheads`, abstract `Warhead` base
  instantiated). Fixed by giving each node its concrete type (`CreateEffect` / `SpreadDamage`).
  New regression audit `tools/audit/audit_empty_warheads.py` sweeps the full resolved ruleset
  (4,202 weapons incl. `^templates`): 0 remaining. Boot-gate PASSED (menu `PostWorldLoaded`,
  no new exception log). `--check-yaml` does NOT catch this class — run the audit after bulk
  warhead edits. See `docs/audit/SUMMARY.md` § "Empty warhead type NRE (2026-08-04)".
- **2026-08-04:** `sweep_areadamage.py --apply` converted 134 main-warhead `SpreadDamage`
  overrides to bare inheritance (now `AreaDamage`) across 23 `weapons.yaml` files; stripped
  12 stale `ValidRelationships: Neutral, Enemy` blocks. `extract_stats.py` refreshed
  32 `docs/balance/*.json` ledgers. Boot-gated `MenuPostProcessEffect.PostWorldLoaded`,
  no new `exception-*.log`.
- **2026-08-04:** audit quick-fix bundle — added `MinimumExposure: 0.45` to `RAAtomic` and
  `CabalMagicNuke`; corrected `MinRange` for `RA2REVENANTAA`/`RA28Inch`; renamed `DropPodExplode`
  `Warhead@1Eff` to `Warhead@Effect`; fixed `TSDPOD` render image (`tsdpod` → `tsdroppod`) and
  `sietch_creep_disabled` image (`sietch_creep_disabled` → `sietch`). Boot-gated, no new exceptions.
0. **[DONE 2026-08-01, `59c77f444`] Armor normalization** — armor is now a per-CLASS property (single
   source in `^<Class>Template`). Fixed 3 templates (MBT→Heavy, HighTechTank→Superheavy,
   LineBreaker→Superheavy) + stripped 215 flat per-actor `Armor: Type:` overrides + dropped stale Medium
   from `^CombatTank`. Verified 273/274 class vehicles resolve to class armor; boot-gated. **OPEN items
   left for later:** (a) `wc2_humans_paladin` is tagged `line_breaker` but is a vehicle inheriting the
   *infantry* `wc2_humans_knight` — suspected mis-tag, resolves Medium not Superheavy; re-classify in a
   tagging pass. (b) 4 conditional-armor actors intentionally KEPT their `Armor: RequiresCondition:`
   deploy/shield swaps and were NOT normalized: `terran_siegetank` (Heavy), `terran_matador` (Medium),
   `td_gdi_defenserig` (Superheavy — already correct), `cabal_ravager` (Plate) — decide per-unit whether
   the base-state armor should match class.
1. **[BLOCKED on maintainer confirm + `--confirm`] Apply VEHICLE stats** — once the REVISION table is
   confirmed: baselines → `apply_balance --confirm` (fit_class scales members 0.5–4.0×, verifier 2.5×) →
   self-heal Step → epic 4×HP + MonsterTank DPS→10000 → re-extract → audits + BOOT →
   commit yaml+ledger. THEN **infantry** (build the same big class table first, then apply). NOTE: the
   HP/Speed/Cost/DPS restat of the 13 baselines + per-member synthesis is still pending here; DPS/range
   are blocked on the weapon/cannon rebuild (#4).
2. **[L] Regression sweep** — review all commits since ~2026-07-24; hunt fluent/description-reference
   breakages like the RA1-Soviet upgrade regression (broke in `53fb10725`, fixed `f68a01833`). Pattern:
   renames that update Fluent keys but leave live `Buildable.Description` refs pointing at the old key.
3. **[L] Repo cleanup** — audit duplicate/overlapping python scripts (multiple balance + rename scripts)
   and docs; propose merge/generalize/delete plan. NO deletes without maintainer sign-off.
4. **[M] New weapon templates** (AFTER vehicles) — kill warhead-mixing, **HARD LIMIT 2 inherits/weapon**
   (special >2 only if justified, bar TBD); then weapon-class pipeline + unit↔weapon binding. Maintainer
   names them + I propose. See [[cameo-weapon-structure-rules]] + [[cameo-weapon-ordering-law]].
   DESIGNED + SIGNED OFF 2026-08-01/02 (survives /compact via docs+memory): two-level ordering law
   (ARMOR_SYSTEM "PROFILE construction" + `cameo-weapon-ordering-law`); 4-dimensional differentiation
   model + flat/% orthogonal axis + Super tier + AoE-FF rule + CORRECTED %-warhead
   (WEAPON_TYPE_SYSTEM §13 + `cameo-weapon-differentiation`). `gen_weapon_template.py` rebuilt —
   **55 templates**, unified `^<Family>_<Level>` naming, modes sloped/FLAT(Sonic)/PCT(Magic):
   Bullet/CannonAP/CannonHE/MissileAP/HE/AA/Flak/Laser/Prism/Flame/Chemical/Melee/Arrow/Demolition/
   Concussion/Sonic (L/M/H) + Railgun/Tesla (Heavy) + TeslaCharged/Nuclear (Super, WC1.5) +
   Magic (%-equalizer, ground-only). ✅ **SPLICED + BOOT-GATED 2026-08-02**: the 55 templates now live
   ABOVE the `DO NOT INHERIT` divider in `weapons.yaml` (replacing the 6 stale provisional
   `^*Demolition`/`^*Concussion`, which carried the old `Wood>Concrete>Steel` building bug); the 55
   `^<Family>_<Level>` WeaponClass scalars are recorded in `docs/balance/weapon_classes.yaml`. Verified:
   key-set diff = only 6 provisional removed / 55 added, rest byte-identical; all weapon audits green;
   game reached main menu (`PostWorldLoaded`, no new exception log). Old bespoke templates
   (`^Grenade`/`^ShrapnelWeapon`/`^HeavyBomb`/`^SmallArms`/etc.) intentionally KEPT until repoint.
   **GENERATOR RECONCILED 2026-08-04** (A1 of BALANCE_MEGAPLAN) — `gen_weapon_template.py` now emits
   `^Warhead_<Family>_<Level>` naming + `AreaDamage` main + universal baked FF (`Ally, Neutral, Enemy` +
   `FriendlyFireDamage/Spread 50`) + `_Percentage` suffix, matching the swept/converted templates in
   `weapons.yaml`. Regenerating is now a no-op diff against the file. Fixed `AOE_FAMILIES` `NameError`
   (leftover from removed `aoe` param). Spot-verified Bullet + Tesla templates match byte-for-byte.
   **REPOINT REFRAMED AS THE FULL 3-WAY SPLIT (#4b), maintainer 2026-08-02.** A bare reparent onto the
   warhead-only families is UNSAFE: survey found 392/437 single-inherit weapons override a warhead by the
   OLD key (`Warhead@SmallArms:` → orphaned/double-fire) + 253 rely on the old template's bundled FX (go
   silent). Root cause: old templates are FULL-STACK; the 55 new families are warhead-only BY DESIGN. So
   the repoint = build the projectile + effect layers first, then retrofit weapons to the 4-inherit model.
   Progress (docs: `WEAPON_3WAY_SPLIT.md`):
   - ✅ **Layer 2 (PROJECTILE) + Layer 3 (EFFECT) libraries BUILT + SPLICED + BOOT-GATED 2026-08-02** —
     `gen_projectiles.py` (24 `^Projectile<Family>_<Level>`) + `gen_effects.py` (27 `^Effect<Family>_<Level>`),
     extracted verbatim from the 30 old full-stack templates, additive/0-usage above the divider. Boot OK.
   - ✅ **Warhead FF twins BUILT + BOOT-GATED 2026-08-02** (`956cf1ecb`) — 19 FriendlyFire twins for the
     7 AoE families (Demolition/Concussion/Flame/Chemical/Nuclear/Sonic/Melee). ExtraDamage twin (energy)
     stays per-weapon (bespoke +vs-shield). All 3 layers now exist (55 wh + 24 proj + 27 fx).
   - **RETROFIT Phase A (SmallArms/Chaingun pilot) — IN PROGRESS 2026-08-02.** Repoint weapons to
     `Inherits@wh + @proj + @fx`, renaming `Warhead@<Old>` keys → new key while **PRESERVING each
     weapon's existing on-grid `Damage` verbatim** (damage law = 2000-grid, all mains identical, fine-tune
     ONLY via one unconditional actor `FirepowerMultiplier` — DESIGN.md §nice-number). Handle INTERMEDIATE
     templates (`^RA2Chaingun`→`^Chaingun`). Pilot = **SmallArms→Bullet_Light + Chaingun→Bullet_Medium**,
     boot-gate, then roll out; energy families in a small ExtraDamage-aware pass; **609 MIXED = Phase B**
     kill-mixing (≤2 warheads, honor the exception allow-list — Dune 3-cannon, Siege Tank/Engine).
     **Bullet_Heavy → the Pulverizer mecha** (Asian Alliance, currently mixed → Phase B). Then delete the
     30 orphaned old templates + their `weapon_classes.yaml` rows. This unblocks the vehicle DPS restat (#1).
   - **RETROFIT mechanical clusters 2026-08-05/07** — `HeavyBomb+ShrapnelWeapon`
     (`Demolition_Heavy+Concussion_Medium`), `LightMissile+MediumMissile` (`MissileHE_Light+MissileHE_Medium`),
     `Grenade+HeavyMissile` (`Concussion_Light+MissileHE_Heavy`), `ShrapnelWeapon+HeavyCannon`
     (`Concussion_Medium+CannonHE_Heavy`), `MediumCannon+HeavyCannon`
     (`CannonHE_Medium+CannonHE_Heavy`), `Grenade+HeavyBomb`
     (`Demolition_Light+Demolition_Heavy`), `Grenade+ShrapnelWeapon`
     (`Demolition_Light+Concussion_Medium`) converted and boot-gated. Total dual-inherit live weapons
     reduced by ~48.
   - **Effect-heavy clusters (flame/chemical/sonic/energy) are BLOCKED** until a `PhysicalState`/`GroundFire`/
     `EMP`/ExtraDamage-aware converter is built — see `docs/LESSONS_LEARNED.md` § "Effect-warhead merge safety".
     **Phase A progress (2026-08-02):** `tools/archive/retrofit_v3.py` repointed ~130 single-inherit
     weapons from `^SmallArms`→`^Bullet_Light`/`^ProjectileBullet_Light`/`^EffectBullet_Light` and
     `^Chaingun`→`^Bullet_Medium`/`^ProjectileBullet_Medium`/`^EffectBullet_Medium`, including intermediate
     templates (`^RA2SmallArms`, `^RA2Chaingun`, `^RA2MG`, `^TSMG`, `^SteelChaingun`). Warhead override
     keys renamed (`Warhead@SmallArms`→`Warhead@Bullet_Light`, `Warhead@Chaingun`→`Warhead@Bullet_Medium`,
     etc.). Dual-inherit weapons skipped (Phase B). `Report: gun8.aud` added to `^Bullet_Light` and
     `^Bullet_Medium` to preserve default sound from old templates. `check-yaml` verified: no new
     retrofit-related errors. **REMAINING:** boot-gate, then roll out to remaining weapon families.
     - **2026-08-04:** `tools/balance/retrofit_weapon_family.py --old LaserWeapon` repointed 34
       single-inherit weapons across 14 files to `^Warhead_Laser_Heavy`/`^Projectile_Laser_Heavy`/
       `^Effect_Laser_Heavy`; boot-gated with no new exception log.
     - **2026-08-04:** `--old TeslaWeapon,TeslaChargedWeapon,RailgunWeapon` repointed 85
       single-inherit weapons across 27 files to `^Warhead_Tesla_Heavy`, `^Warhead_TeslaCharged_Super`,
       and `^Warhead_Railgun_Heavy` (plus matching projectile/effect layers); boot-gated clean.
   - **[FUTURE, reason later] SPREAD REBALANCE** (maintainer 2026-08-02) — spreads must be UNIQUE per weapon
     but balanced so **`Damage × Spread ≈ constant`** (inverse trade); a small spread MUST carry a unique
     extra effect (energy's +vs-shield chip is the model). Folded into the restat; do NOT hand-tune yet.
4b. **[L, FUTURE] 3-WAY weapon-template split** (maintainer 2026-08-02) — decompose every weapon into
   THREE independent composable templates: (1) WARHEAD/weapon-class (Versus+damage — the §12 families
   already ARE this layer, projectile/effect-agnostic by design), (2) PROJECTILE (speed/homing — so a
   fast projectile can carry a heavy warhead), (3) EFFECTS (impact/muzzle/trail/sound). MASSIVE:
   retrofitting thousands of inline weapons + the 2-inherit rule must widen to 3 (warhead+projectile+
   effects). Best folded INTO the repoint pass (#4) rather than a separate sweep, since that already
   touches every weapon. Not quick — deferred.
5. **Weapons-hygiene batch** — folds into #4 (also fix the duplicate `227mm` weapon def in
   `weapons/tiberiandawn.yaml` vs `weapons/missiles.yaml`).
6. **[L] Actor-to-actor inheritance audit (DEFERRED, maintainer 2026-07-31)** — prefer `^Templates`
   over `Inherits: <actor>` for ContentPack self-containment. **199 existing instances reviewed &
   deemed fine/grandfathered** (116 = RA2 civ-terrain `ra2ct*`; ~83 variant/husk: `*mkii`←base,
   `ifv_*`←ifv, `E1`←minigunner, badger family, WC2 towers). NOT a must-fix — do it as its own pass
   later; resolution = inline (cross-pack/one-off) or hoist to `^Template` (same-pack). Memory:
   [[cameo-no-actor-inheritance]]. Audit cmd in the memory. Don't stop pipeline work for it.

**ENGINE workflow (Blackrobe 2026-07-31):** `cameo-mod/engine` is a git **submodule (a working clone of `origin Cameo-mod/OpenRA`)**, whose **main branch is `cameo-engine`** — i.e. the "cameo-engine dev clone" referenced in `.windsurf/rules/start-protocol.md`. Engine updates: branch off `cameo-engine`;
**MIRROR changes both ways** (`cameo-engine` ↔ `cameo-mod/engine`); rebuild before the boot gate. Memory:
`cameo-engine-submodule`.

---

## 🔴 BUG — campaign maps vanish from editor + mission selector

- [x] **FIXED** (`42ba6f34c`, 2026-07-27): Root cause was `LockFaction: Random`
  (string) instead of `LockFaction: True` (boolean) in 6 map.yaml files — a
  regression from commit `6ccb9a749`. OpenRA silently dropped maps with invalid
  `LockFaction` values. Also fixed invalid fluent key `bot-campaign-ai.name` →
  `CampaignAI` in delivery/deliverycoop rules.yaml and added missing
  `bot_ai.campaign` fluent key to en.ftl.

## ★ MAJOR PROGRAM (2026-07-25): mod-synthesis balance overhaul — see [`BALANCE_SYNTHESIS.md`](BALANCE_SYNTHESIS.md)

Big multi-session effort to fix Cameo's extreme-value balance by synthesizing extracted mods into
class anchors. Full plan + the new binding laws (spread-width, baseline-only, class↔weapon binding,
AA-gating, rock-paper-scissors) are captured in `BALANCE_SYNTHESIS.md` + `ORIGINAL_UNIT_STATS.md`
(reference map + extracted data) + memory. Work items, in order:
1. **Extract remaining sources** — CnC Reloaded (`Tools/Map Editor/rulesmd.ini`), Romanov's Vengeance
   (`mods/rv/rules`+`weapons`), Dune games, Outpost 2. **Extend tooling to weapons/warheads/versus**
   (currently HP/Cost/Speed only) — the full spreadsheet stat set.
2. **Normalized full reference tables** per mod per faction (÷ each mod's basic rifleman).
3. **Synthesize per-class/faction targets** → **re-derive class anchors** (tightened spread band).
4. **Weapon/warhead rework** — class↔weapon binding matrix, grow the warhead library, remove wild
   mixes (audit `weapons.yaml` vs mod versus-values + `ARMOR_SYSTEM.md`).
5. **AA class-gating** (§9) + **bake out per-class multipliers** into baselines (§7).
6. **Promote the §6–§10 laws into `DESIGN.md`** (binding). Then rerun the formula per class → apply.

## Active documentation maintenance

- [x] **Documentation architecture quick wins** — owner: Cascade. Added `docs/README.md`; reduced `PROJECT_CONTEXT.md` to orientation and canonical links; kept the complete startup, evidence, incident, and commit-gate protocol in `AGENT_WORKSPACE.md`. Validation: checked links in the entry documents and ran `git diff --check`.
- [x] **Documentation architecture continuation** — owner: Cascade. De-mixed `MEGAPLAN.md` into a short rebalance index and moved the Dynamic Campaign vision into non-binding `VISION.md`; Formula V2, balance-pipeline, and ARMOR_SYSTEM remain canonical linked sources. Excludes the ROADMAP history split and Formula V2 roster-log migration. Validation: internal-link check and `git diff --check`.

## Balance — universal class-formula program (2026-07-22, ACTIVE)

**Goal:** ONE balance formula for every class; a class is re-weighted only
by dropping in a **baseline actor** + **verifier actor** (the two calibrate
the weights). `UnitClass` scalar is deprecated → set to 1.0 once all anchors
are picked, then delete. Order: infantry → tanks/vehicles → aircraft →
defenses → naval. All DPS/cost below are PROVISIONAL (maintainer tunes
in-game); actors + stats + structure are LOCKED. Full anchor store:
`docs/balance/class_anchors.json` (14 classes as of 2026-07-22).

**Laws locked this session (bake into pre-flight + audits so they can't be skipped):**
- **SUM law** — effective damage = Σ offensive SpreadDamage warheads (excl.
  `*ExtraDamage`/`*Percentage`/`*FriendlyFire`), never MAX. Canonical reducer
  `formula.spread_damage_sum` (done: propose_class_rebalance/fit_class/update_ranges route through it).
- **Two-stage DPS tuning** — coarse: warhead `Damage` on the 2000 grid;
  fine: `FirepowerMultiplier@<unit>` in 1% steps (1 = ×0.01). Dispatcher must emit both.
- **Baseline @ band middle**; **verifier ≡ baseline on range+speed, exactly
  2×HP / 2×DPS / 2.5×cost**; same tech tier as baseline so it cancels.
- **WC/StarCraft unit costs = multiples of 20** (power = Cost/20).
- **RevealsShroud per class = baseline range, floored to 5000** for
  scout/closecombat/melee (helps snipers scout). Apply to each `^…Template`.
- **Melee range IS priced** (FORMULA_V2 §6b corrected).

**To do (in order):**
- [x] **BUILDABILITY LAW** (maintainer 2026-07-22): a unit is balance-relevant
  ONLY if buildable — has a `Buildable` trait with a non-empty `Queue` and NO
  disabling prereq (`~disabled`/`~wip`/…). Non-buildable units (legacy tokens
  E1/E3 = no Queue; spawn/veterancy `_sp`/`_r4` = no Buildable; ~disabled units;
  cost-10 XP-bag civilians) are EXCLUDED from balancing AND every audit — their
  cost is just an XP-on-kill value. DONE: `extract_stats._is_balance_buildable`
  writes `u["buildable"]`; `propose_class_rebalance` skips non-buildable (keeps
  anchor/verifier). 23/280 infantry excluded. STILL TODO: apply the same filter
  to the standalone audits (uniqueness, outliers, stat_formulas, etc.).
- [x] **Infantry membership auto-classified** (2026-07-22, "auto-classify + review"):
  membership = the `^…InfantryTemplate` each unit inherits (design.subtype),
  mapped by `subtype_to_anchor` (now all 14 classes), + explicit
  `design.class_anchor` overrides for pollutants. 257 buildable infantry classed:
  melee 41, heavy 39, rocket 35, support 34, scout 24, commando 24, SF 16,
  pure_sniper 16, grenadier 10, flying 7, closecombat 4, heavy_sniper 2. See
  `docs/balance/membership_review.md`. Reclassified: engineers/medics/spies/
  casters→support; dogs→melee; dragunov+virus→heavy_sniper; futuretech droids
  (shotgun→closecombat, cannon→heavy, missile→rocket, scout→scout, repair→support);
  zerg_ultralisk/wc2 knight+ogre→melee (were on the tank template); marauder→heavy.
  OPEN CALLS: (a) terran_marine/zerg_hydralisk/terran_madcap fell to rocket_trooper
  via their AntiTankAntiAir subtype — confirm or redirect; (b) terran_ghost/specter
  still SF (subtype SniperInfantry) — SF or sniper?; (c) grenadier VERIFIER
  ra1_soviets_molotovconscript is ~disabled (non-buildable) — pick a buildable
  verifier or confirm it's upgrade-reachable; (d) 5 buildable vehicles sit in the
  infantry section (leech/bmwbike/antitankcannon/noidharvester/engineeringarmor) —
  handle in the vehicle pass.
- [ ] **Populate design.special (K) + design.tech_tier** across the roster:
  the formula APPLIES them when set (verified: madcap K=1.25, ghost tier=0.75 are
  used) but most units are untagged → default 1.0, so specials/high-tech are
  under-counted. The huge SF-pollutant deltas (ghost SUM 130000, specter 260000)
  are dominated by MAX-era hot damage, not missing modifiers, but the modifiers
  still need populating (ties into the catch-all-specials audit).
- [ ] Build `tools/balance/rebalance_classes.py` dispatcher: SUM price →
  2000-grid warheads → 1%-step FP-mult → range-solve to band (mult-of-10) →
  uniqueness within broad TYPE → Δ (goal ≤1). Consolidates the scout/
  closecombat/SF one-offs (LESSONS §172-176).
- [x] **Fix uniqueness in code** (done 2026-07-22, commit pending):
  `propose_class_rebalance.resolve_dps_uniqueness` now keys on effective
  damage-per-shot (Σwarheads×FP); the report checks the 5 raw stats — HP, Speed,
  Range, RAW ReloadDelay, effective-damage-per-shot — with damage-per-shot and
  reload as SEPARATE dimensions (reload dupes flagged, never auto-nudged). STILL
  TODO: apply the same 5-stat metric to the standalone uniqueness AUDIT.
- [x] **Speed-step in code** (done 2026-07-22, refined): step is PER-UNIT, not
  per-class — 1 for foot infantry (turn instantly), 5 for vehicle-turn-rate units
  (turn = speed/5, snapped to a multiple of 5). Detected by a defined
  `Mobile.TurnSpeed` (`row["vehicle_turnrate"]`): catches actual vehicles AND the
  Cabal cyborgs / FutureTech droids, while foot units (incl. zerglings, chem
  locomotor but no TurnSpeed) stay step-1. Foot infantry also get a Speed±1
  fine-tune as a Δ lever (maintainer 2026-07-22). `VEHICLE_TYPE_CLASSES` still
  forces the class default where every member is a vehicle (mbt).
- [x] Apply **closecombat ReloadDelay 75→70** (anchor DPS 250 / verifier 500) —
  done as part of the 4-anchor restat below.
- [x] Fix the 4 anchor units to grid (shotgunner/fanatic 4000→2000×2, reload
  75→70; japan 12000→4000×3; lunar 24000→8000×3) via ledger→apply_balance→boot
  gate. Verified Δ0: anchors price to cost0, verifiers to 2.5×cost0. (2026-07-22)
- [ ] **Tech-tier is applied ABSOLUTE, must be RELATIVE to the anchor's tier**
  (found 2026-07-22 during the 4-anchor restat): `class_baseline_price` multiplies
  by `design.tech_tier` (default 1.0). Closecombat is documented T3 (0.75) but no
  unit is tagged, so all price at tier 1.0 and the anchor lands on cost0 — correct
  BY ACCIDENT. The moment any closecombat unit is tagged T4, it would get absolute
  0.5 instead of 0.5/0.75 (relative). FIX: effective tier = unit_tier / anchor_tier
  so the anchor always cancels to 1.0 (matches the "verifier shares tier so it
  cancels" law). Until fixed, do NOT tag class members with a tech_tier ≠ the
  anchor's.
- [~] Reconvert the ~20 MAX-era-hot closecombat+SF members (each warhead was
  set = intended total → 2–3× hot under SUM). BLOCKED on membership cleanup
  first — the current subtype rosters pull in snipers/casters/spies/core-combat
  units (scout: spies+zerg_defiler; SF: dragunov sniper, terran_*, zerg_hydralisk).
  PROGRESS 2026-07-22: **closecombat 3/4 at Δ≤1** — shotgunner/fanatic anchors
  (Δ0), naxis_sssoldier (range 4500, FP 95%, Δ−0.8). `alien.nax` DEFERRED (Δ+67):
  its weapon `NaxiAlienPistol` is defined in shared `mods/cameo/weapons/redalert2mod.yaml`
  and inherited cross-pack (Naxis + SchwarzerMond) — editing it would leak.
- [ ] **Shared-weapon ownership pass** (systemic, found 2026-07-22): many members
  share a weapon via cross-pack `Inherits:` (e.g. NaxiAlienPistol → Naxis+SchwarzerMond).
  Per-unit balance edits leak. Before converting such a unit, FORK it a per-unit
  weapon (+ its `…E`/elite + garrison variants) in its own pack, repoint the
  actor, then balance the fork. Aligns with the self-contained-pack mission goal.
  Detect them: `apply_balance` writing to a weapon whose block lives in a shared
  file / is inherited elsewhere.
- [ ] **PIPELINE LAW — never hand-calc DPS; use the tools** (learned 2026-07-22):
  effective DPS depends on ReloadDelay, Burst AND **BurstDelays** (+ FirepowerMultiplier).
  A hand calc that skipped BurstDelays mis-set naxis_sssoldier (prescribed FP 88
  instead of ~95). Always derive base DPS via `propose_class_rebalance.unit_dps`
  (or armament_dps), which reads every knob; then solve FP for Δ0. Validate the
  APPLIED state by pricing the ledger stats directly — the proposal RE-SOLVES
  range/FP and is a generator, not a validator.
- [ ] Restat + reconvert each infantry class to its anchor (class_anchors.json).
- [ ] NEW BUILDING: RA1 Soviets Tier-4 dummy (forward-command-center sprite,
  placeholder `ra1_soviets_experimentaltechcenter`) unlocking the heavy-infantry
  shocktrooper; ladder T3=tech center, T4=experimental. (Needs a real name.)
- [x] Rocket troopers: raise td_nod + td_gdi to 300 (weak at 200). DONE
  2026-07-30: changed ^E3 template Cost from 200 to 300 in
  ContentPacks/TiberianDawn/Shared/yaml/templates.yaml. RA1 rocket
  soldiers already at 300 via ^RA1AlliesAlliedRocketSoldier.
- [ ] Heavy-sniper verifier warhead recipe (yuri_virus/ts_nod_toxintrooper):
  sniper+chaingun+railgun templates, equal warheads; virus upgrade 1 = +light
  chemical, upgrade 2 = +medium chemical; spawned gas = special K+0.25 (1.25×).
- [ ] **Catch-all-specials audit** (maintainer flagged): detect EVERY special
  reliably — granted-condition effects, FireShrapnel-spawned warheads/gas,
  charge-delay/frontal-facing negatives — so K is never under/over-counted.
- [ ] Then vehicle anchor proposal (MBT live; light tank / heavy / tank
  destroyer / artillery / AA / scout / battlefortress / APC).
- [ ] DEFERRED to elite-weapon audit: elite weapon range = base + 1000
  (naxis elite is 6500, should be 6000).
- [ ] **Class descriptions rework** (maintainer 2026-07-22): every unit CLASS
  needs its own fluent `.description` (only a few exist so far —
  scout/antitank/mbt/commando + the 4 added today: heavy_sniper/rocket_trooper/
  archer/support). Descriptions live in `mods/cameo/fluent/rules/en.ftl`
  (Buildable.Description is a `[FluentReference]` key, NEVER inline text; use
  real line breaks, never `\n`). ALSO: the "Strong vs / Weak vs" matchup lines
  belong at the END of the description, after the flavour text — needs a design
  pass on wording/order. Support-type units get NO Strong/Weak line.
- [x] **Naming: dots → underscores** (maintainer 2026-07-22 — actor/template
  names must ALWAYS use `_`, NEVER `.`): renamed `^upgrade.template` → `^upgrade_template`,
  `^researched_upgrade.template` → `^researched_upgrade_template`, `^promotion_upgrade.template`
  → `^promotion_upgrade_template`, `^default.angry_mob` → `^default_angry_mob`,
  `^default.alien_mob` → `^default_alien_mob` mod-wide (76 files, 1183 replacements,
  commit `7f704c981`). `unit_upgrade` already fixed 2026-07-22. No dotted husk templates
  remain (all ground husks removed in prior commit). Boot-gate clean.
- [x] **Engine 910e50de → 2cfb751694 → ba153be0c6 → 1f71ccde9 migration** — engine pin
  updated to `1f71ccde90c1194fe908702f2e915807b2f0f3fd` (2026-07-31, fixes
  `InvalidOperationException` crash in `ClassicProductionQueueProperties` when
  an actor with no production queue is produced via Lua). Previous pin
  `ba153be0c6` (2026-07-30, fixes cargo pips showing 0). The stricter parser
  issues from the earlier `910e50de` bump were fixed 2026-07-22 (4 template
  Description indents → fluent keys, `unit_upgrade_template` rename). Current
  engine is clean; master boots. If a future engine bump surfaces new parser
  rejections, fix as found (master must always boot).

## P0 — Crashes (always first)

- [x] **P0 CRASH: InvalidOperationException in ClassicProductionQueueProperties**
  (2026-07-31, fixed): `System.InvalidOperationException: Sequence contains no
  elements` at `ProductionProperties.cs:line 226` —
  `GlobalProductionHandler` calls `.First()` on `BuildableInfo.Queue`,
  crashing when an actor with no production queue is produced (e.g. via
  Lua `Actor.Create` on survival maps). Engine fix in
  `cameo-engine` commit `1f71ccde90`: replaced `.First()` with
  `.FirstOrDefault()` + null guard in `GlobalProductionHandler`,
  `Build()`, and `IsProducing()`. `mod.config` updated to
  `1f71ccde90c1194fe908702f2e915807b2f0f3fd`. Boot-gate passed (menu
  reached, 0 new exceptions).
- [x] **P0 CRASH: InvalidOperationException in InfectCA.OnEnterComplete**
  (2026-08-02, map Terra Cotta): `Attempted to get trait from destroyed
  object (ra2dron 521 (not in world))` at `TraitDictionary.CheckDestroyed`
  called from `World.Remove` → `InfectCA.OnEnterComplete` frame-end task.
  The infector actor (`self`) was already disposed by the time the
  frame-end task ran, so `w.Remove(self)` crashed when iterating
  `INotifyRemovedFromWorld` traits. Fix: added `self.IsDead` guard before
  `w.Remove(self)` in `OpenRA.Mods.Cameo/Activities/InfectCA.cs` — if
  dead, revoke `BeingInfectedCondition` on target and return early.

- [x] Voice-set rename crashes (`1616a26d2`); pink menu (`e956d2280`);
  boot crashes crab-junk/shadowteam/stale-DLL (`28ae47612`). LAW:
  launch-game.cmd to menu before EVERY commit (CLAUDE.md gate).
- [x] **ts_nod_ticktank voxel sequence crash** (`4bfd1bcaf`): `ts_nod_ticktank`
  and `ts_nod_attackcycle` had no `idle:` sequence filename — the voxel files
  are `tsttnk.vxl` and `tsbike.vxl` (old TS names), but the sequence entries
  only had `idle:` with no filename. Fixed by adding `idle: tsttnk` and
  `idle: tsbike` respectively in `voxels.yaml`.
- [x] **magicnuke sequence crash** (`4bfd1bcaf`): CABAL neutron weapons
  (`CabalCommandoPlasmaNeutron`, `CabalCommandoPlasmaMk2Neutron`,
  `CabalRavagerPlasmaNeutron`) had `Image: magicnuke` in their
  `CreateEffect` warheads. The `magicnuke` image has sequences `magicnuke`,
  `magicnuke_med`, `magicnuke_small`, `magicnuke_micro` — but `Image:
  magicnuke` makes the engine look for a sequence named `magicnuke_med`
  inside image `magicnuke`, which doesn't exist (the sequences are defined
  under the `magicnuke` image key with those names). Removing `Image:
  magicnuke` lets the engine use the `Explosions:` field directly against
  the sequence set. The `CabalMagicNuke` weapon (line ~1847) already
  worked correctly because it only had `Explosions: magicnuke` without
  `Image:`.
- [x] **ra2_cgtbnkbb.shp not found crash** (`4bfd1bcaf`): Asset was renamed
  to `ra2_cgtbnkbib.shp` (bb→bib convention) but YAML references in
  `redalert2.yaml` were not updated. Fixed all 3 references.
- [x] **ra2_ctoutpbb.shp not found** (`4bfd1bcaf`): Renamed to
  `ra2_ctoutp_bib.shp`, updated 4 YAML references in `redalert2.yaml`.
- [x] **tamrefbb.shp reference** (`4bfd1bcaf`): Renamed to `tamref_bib.shp`,
  updated reference in Forgotten `sequences.yaml`.
- [x] **mk→make asset renames** (`4bfd1bcaf`): 8 construction animation
  files renamed from `_mk.shp` to `_make.shp` (ra2_cgoildmk, ra2_ntyardmk,
  tambarmk, tampowrmk, tamradrmk, tamrefmk, tamtechmk, tsnttmplmk) with
  all YAML references updated.
- [x] **Weapon rename task backlogged** (`4bfd1bcaf`): Full research and
  tooling documented in `docs/history/backlog_weapon_rename.md` for future
  continuation.
- [x] **CABAL Orb Drone carrier-slave crash** (`ec63784bd`):
  `cabal_orb_drone` had `CarrierSlave`+`HasParent` traits while also being
  buildable from the cyborg factory. When built independently, no master is
  linked, causing `NullReferenceException` in `CarrierSlave.EnterSpawner`.
  Split into `cabal_orb_drone` (standalone, no slave traits) and
  `cabal_orb_drone_slave` (non-buildable, inherits base + CarrierSlave).
  Updated `CarrierMaster` on `cabal_hunter_drone_carrier` to spawn the slave.
  Pattern follows RA1 Japan `zerofighter`/`japancarrier`.
- [x] **RA2 corpse death_d crash** (`ac3ba04b7`): `RA2CorpseSpawner` and
  `RA2FlyingBody` CreateEffect warheads lost `Image: ra2corpse` during CE2
  cleanup, causing engine to look for `death_a`-`death_f` sequences in the
  default `explosion` image where they don't exist. Restored `Image: ra2corpse`
  per corpse-spawner exception in DESIGN.md §8.

### P0 — Completed (2026-07-26 session)

- [x] **RA1 Soviet atomic bomb lost its directional flash**: bulk YAML lint
  commit `d42ad53a1` deleted the `Warhead@NuclearFlash` header from active
  `RAAtomic`, leaving its tuning fields under a removal node. Split the shared
  weapon into `^AtomicCore` and an `Atomic` wrapper so `RAAtomic` can define the
  approved 40-tick effect without a regex-fragile negative removal. Added an
  active-ruleset contract audit covering RA1 `RAAtomic`, Ixian `PulseMissile`,
  and CABAL `CabalMagicNuke`.

### P0 — Completed (2026-07-24 session)

- [x] **RA2 weapons migration to ContentPack** (`fix/ra2-weapons-migration`):
  The ContentPack `RedAlert2/Shared/yaml/weapons.yaml` only had templates
  (`^RA2*` prefixed), missing 134 weapon definitions (RA2CarrierTarget,
  RA2BrutePunch, MigMissiles, V3Launch, etc.) that were only in
  `mods/cameo/weapons/redalert2.yaml` (commented out in mod.yaml). Replaced
  ContentPack weapons.yaml with full copy of redalert2.yaml and applied all
  lint fixes from commit d42ad53a1 (NegativeRemovals, invalid fields, etc.).
  This resolves the `RA2CarrierTarget not found` error.
- [x] **Yuri weapons missing headers** (`fix/ra2-weapons-migration`): The lint
  commit d42ad53a1 accidentally removed 6 weapon/warhead headers in
  `RedAlert2/Yuri/yaml/weapons.yaml` while doing NegativeRemoval cleanup.
  Restored: `RA2DiskSteal:`, `Warhead@Cloud: SpawnSmokeParticle` (RA2Chemspray),
  `Warhead@MediumChemicalWeaponPercentage: HealthPercentageDamage` (RA2Chemspray),
  `Warhead@LaserWeapon: SpreadDamage` (RA2Magnet),
  `Warhead@FlakWeapon: SpreadDamage` (RA2Virusgun2),
  `Warhead@Smudge: LeaveSmudge` (RA2CosmonautLaser).
  The missing `Warhead@Cloud: SpawnSmokeParticle` header caused the
  `Sequences` field error (orphaned SpawnSmokeParticle child nodes under a
  removal line).
- [x] **Naxis Kübelwagen weapon encoding fix** (`fix/ra2-weapons-migration`):
  Weapon name `NaxiWW2KÃ¼belwagenMachinegun` in Naxis weapons.yaml had
  double-encoded UTF-8 (mojibake), causing weapon-not-found crash for
  `naxis_kbelwagen` actor. Fixed to `NaxiWW2KübelwagenMachinegun`.
- [x] **Missing postprocess_nuclearflash.frag shader** (prior session):
  `NuclearFlashRenderer.cs` expects `postprocess_nuclearflash.frag` in
  `engine/glsl/` but the file was never created. Created shader with proper
  uniforms (LightPosition, LightRadius, LightColor, Brightness, Darkness,
  SourceTexture). NOTE: file lives in engine/ which is .gitignored; must be
  recreated after `make all` fetches engine. See history/AI_AGENT_HANDOFF.md.

### P0 — Completed (2026-07-14 session)

- [x] **CABAL Backup Systems upgrade coverage (avatar, widow)**
  (`d4be72f8f`): Added `SpawnActorOnDeath@backup` to `cabal_avatar` and
  `cabal_widow`; added `Inherits@BACKUP` to `cabal_avatar`; created
  `cabal_avatar_backup` and `cabal_widow_backup` actors in
  `rules/tiberiansun.yaml`; added `Repairable` trait to
  `cabal_artilleryspider_backup`.
  **NOTE (2026-07-16):** The original session plan referenced `cabal_legion`
  and `cabal_legion_backup`, but no `cabal_legion` actor exists in the
  current tree (it was likely renamed or removed during the N9 rebalance).
  `cabal_widow_backup` was created instead. If a `cabal_legion` actor is
  re-added later, it will need its own backup actor.
- [x] **Backup husk repair/reanimate** (`d4be72f8f`): `Repairable` trait
  added to `cabal_artilleryspider_backup` (was missing — present on
  manticore and tarantula backups already).
- [x] **CABAL infantry death palette break** (`a2b4de333`): All 8 CABAL
  infantry actors and the `^TSInfantry` template had `WithDeathAnimation`
  with `PlayerPalette: playerra2` but no `DeathSequencePalette`. The
  `DeathSequencePalette` field controls which palette the death sequence
  frames render with; without it, the engine defaults to a non-player
  palette, causing visible color breakage on death. Fixed by adding
  `DeathSequencePalette: ra2player` to `^TSInfantry` template and all 8
  CABAL infantry overrides (cyborginfantry, rocketcyborg, devout,
  ascended, hackercyborg, cyborgcommando, cyborgcommandov2,
  eliminator800).
- [x] **TS GDI building death palette break** (`b417c6f96`): The
  `^BaseBuilding` template in `defaults.yaml` had `WithDeathAnimation`
  with `DeathSequence: dead` but no `DeathSequencePalette` — same root
  cause as the infantry palette bug. Fixed by adding
  `DeathSequencePalette: ra2player` to `^BaseBuilding` and to the
  `WithDeathAnimation@BIB` overrides on GDI and CABAL service depots.
- [x] **TD building death palette fix** (`d72194748`): `^BaseBuilding`
  template sets `DeathSequencePalette: ra2player` globally, but TD
  buildings use `PlayerPalette: player_rgba` — mismatch causes wrong
  colors on death. Fixed by overriding `DeathSequencePalette: player_rgba`
  in `^TDBuilding` and `^TDDefense` templates. Also fixed 3 CABAL infantry
  (rocketcyborg, hackercyborg, eliminator800) that had `ra2player` instead
  of `playerra2` as their death palette (mismatch with their
  `PlayerPalette: playerra2`).
- [x] **TS-only death palette audit** (`54816b1f3`, 2026-07-27): Wrote
  `tools/audit/audit_ts_death_palette.py` — checks all 56 YAML files in
  TiberianSun ContentPacks for DeathSequencePalette vs PlayerPalette
  mismatches. Found and fixed 2 issues: `cabal_cyborgreaper` and
  `cabal_heavyreaper` were missing `DeathSequencePalette: playerra2`.
  Audit now passes with 0 issues. Did NOT touch TD, D2k, RA1, RA2, TKM.

- [x] **Railgun NullReferenceException crash** (2026-07-27): Weapons
  inheriting both `^LaserWeapon` (which sets `HitAnim: laserfire` on its
  `Projectile: LaserZap` node) and `^RailgunWeapon`/`^RA2RailgunWeapon`
  (`Projectile: Railgun`) caused a `NullReferenceException` in
  `Railgun.Render` → `Animation.Render` because OpenRA's deep YAML merge
  carried `HitAnim: laserfire` into the Railgun projectile node. The
  Railgun constructor creates an `Animation` but `Render()` can be called
  before `Tick()` initializes `CurrentSequence`. Affected weapons:
  `SteelQuantumCannon`, `SteelStalkerRailgun`, `SteelFighterRailgun`,
  `RA2Robotmm`, `DalekCannon`, and their elite/EMP variants. Fixed by
  adding `HitAnim:` (empty value) to `^RailgunWeapon`'s `Projectile:
  Railgun` block, which overrides the inherited value. The engine checks
  `!string.IsNullOrEmpty(info.HitAnim)` so empty string prevents the
  Animation from being created. Also removed the redundant empty
  `HitAnim:` from the unused `^TSRailgun` template. Boot verified.

- [x] **Shellmap boot crash: "No valid shellmaps available"** (`6a74333d5`):
  the fix-oramap.ps1 rename pass used CASE-INSENSITIVE replaces on map.yaml
  inside the .oramap zips, corrupting shellmap_v2's PlayerReference
  `Allies:` field keys into `ra1_allies:` (invalid field → map excluded
  from the shellmap pool), and renamed display player names without
  updating the lua inside the zips (`Player.GetPlayer("Allies")` → nil →
  lua fatal). desert-shellmap-2 also kept nonexistent factions `soviet`
  (singular, missing from the tool's rename list) and `modjapan`. Fixed
  both maps + hardened the tool (`-creplace`, added soviet/modjapan
  entries). LESSON: .oramap rewrites must be case-sensitive and must
  update embedded lua player/actor strings in the same pass; a mod-wide
  GetPlayer↔player-name scan now shows 0 mismatches.
- [x] **12 more maps broken by the renames** (`2df758574`): mod-wide sweep
  of all 364 maps (invalid Faction values, unknown actor types, orphaned
  Owners, stale lua ids). Fixed: 5 mission maps (ch1-e1, ch1-e1c,
  delivery, deliverycoop, iris-ally-hb) with 25 stale `Faction:` values +
  1 lua id; 5 .oramaps with singular `ra1/ra2_soviet_*` actor types
  (Border conflict, _ra_ore-gardens, _ra_temperal, thelake6people,
  chernobyl); survival.oramap lua (21 ids pluralized);
  desert-shellmap-2-playable orphaned GDI/Nod owners → Neutral.
- [ ] **Pre-existing broken maps found by the sweep (design decisions
  needed, NOT rename-caused)**: (a) ~70 imported maps carry
  `Faction: england`/`ukraine` — factions that never existed in Cameo
  (decide: bulk-rewrite to `Random` or leave — engine may fall back);
  (b) `troublerebels.oramap` references `heavy_inf` (only
  `heavy_inf.ixian` exists — ambiguous); (c) `tiberium-split.oramap`
  references never-defined `split0a/0b/0c/4/8/9` terrain actors (split2/3
  exist); (d) `_d2k_Centerbase` + `_d2k_tournament_spice` reference
  base-D2K generics (`refinery`, `harvester`, `artillery_platform`,
  `combat_siege_tank`, `medium_gun_turret`, `combat_tank_ixian`) that
  Cameo never defined; (e) survival.oramap still has 13 ancient
  `aa_*`/`steel_*` compressed ids — proposed mappings:
  `aa_phoenix→asianalliance_phoenix`,
  `steel_quantumtank→steelconsortium_quantumtank`,
  `steel_katy→steelconsortium_katytank`,
  `steel_mega→steelconsortium_megalodon`,
  `steel_defender→steelconsortium_defenderbot`,
  `aa_samurai→asianalliance_japanesesamurai`,
  `aa_lynx→asianalliance_lynxtank`, `aa_mecha→asianalliance_pulverizermecha`,
  `aa_flam→asianalliance_asiansentryflamer`; unresolved: `aa_archer`,
  `aa_ftnk`, `steel_fedinf`, `steel_qinf`. Effort: S–M once decided.

### New orders 2026-07-17 (second batch)

- [x] **Umlaut transliteration** (2026-07-17): `schwarzermond_bermensch`
  → `schwarzermond_ubermensch` (Ü was dropped instead of transliterated),
  `ÜbermenschLaser(E)` → `UbermenschLaser(E)`, assets git-mv'd. RULE in
  DESIGN §1: Ü→u, Ö→o, Ä→a, ß→ss in ids; display names keep umlauts.

- [x] **BUG: cameo tileset palettes** — FIXED 2026-07-30: root cause was
  that CAMEO's `terrain` and `staticterrain` palettes used `temperat.pal`
  while all CAMEO tile templates use `Palette: ra_temperat` and all
  bib/smudge sprites (`.tem` files) were designed for `ra_temperat.pal`.
  The `temperat.pal` and `ra_temperat.pal` are different palette files,
  causing color mismatches on smudges, craters, and building bibs.
  Fix: changed both `PaletteFromFile@terrain-cameo` and
  `PaletteFromFile@staticterrain-cameo` in `rules/palettes.yaml` from
  `temperat.pal` to `ra_temperat.pal`. Reported by maintainer; was lost
  from an earlier queue.
- [x] **SM promotion grid (maintainer's design, image 2026-07-17)** —
  3 columns x 4 ranks implemented in `SchwarzerMond/yaml/promotions.yaml`.
  [Übermensch/Laser Tank(rpl Beetle)/Crystal Tank/Parzival] |
  [Noid MG/Lunar Tiger(rpl Panzer)/Korruptes Biest/Dalek] |
  [Piercer/Haunebu 3(rpl H2)/MARS(rpl Jagerline)/Die Glocke]. Unit
  prerequisites wired to require the matching promotion; replaced units
  disabled when the replacement promotion is bought. Promotion-unit
  `^PromotionUnitBuff` inheritance verified on all grid units. Boot
  test passed (2026-07-17). FINAL layout re-chained 2026-07-17 after
  the maintainer's decision — see P2 (RESOLVED) for the binding grid;
  do not rearrange promotions.yaml except through a new design order.
- [x] **cabal_plasmaturret not buildable** — root cause: no sequence/
  icon defined for `cabal_plasmaturret`. Added sequence in `ContentPacks/
  TiberianSun/CABAL/yaml/sequences.yaml` and voxel turret mapping in
  `sequences/voxels.yaml`, using TS Nod laser turret assets as placeholder
  (2026-07-17). Boot test passed.
- [x] **cabal_mobilestealthgenerator removed** — CABAL should not have
  it (design 2026-07-17); actor + AI references deleted.
- [x] **RA1 LEGACY-ID RENAME** — DONE 2026-07-17 (`fdd466494`): all 52
  legacy ids renamed via tools/rename/apply_ra1_legacy.py +
  rename_map_ra1_legacy.yaml; zerofighter collision resolved as
  japan_zerofighter_slave; registry 3910/2365, zero old ids, boot green.
  Follow-up fix same day: explicit `actor_<oldid>.description/.name`
  refs in yaml (13) broke when ftl keys renamed — applicator now has a
  fluent-stem pass; warcraft2_en.ftl + tkm_en.ftl were never registered
  in mod.yaml FluentMessages (added). audit_fluent: 0 unresolved.
- [x] **Stale copy cleanup** — DONE 2026-07-17 (`fdd466494`):
  rules/weapons/sequences redalert.yaml + dead RedAlert wrapper
  content.yaml/ai.yaml deleted.

### P0/P1 — User-reported issues (2026-07-15/17)

> Golden reference (pre-rename, everything working):
> the last Cameo-IFV release install —
> diff against it when a rename regression is suspected. Tester reports
> (NFWRambo) need verification before fixing.

- [x] **SHARED-ASSET RENAME CLASS sweep** (2026-07-17) — audit_asset_files
  re-run on the full tree: A1 rename-broken refs = 0, A2 missing voxels
  = 0 (the brik/chainlink fixes cleared the class in the loaded tree).
  56 A3 informational refs remain in UNLOADED legacy rules (actiblizz,
  darkreign, iok, starwars) + a few possibly-in-mix refs — no action
  while unloaded. Rule added to DESIGN §1: rename only after crossref
  proves ONE user; shared assets keep their names.
- [x] **RA1 Allies reinforcement pad** (2026-07-17) — chain VERIFIED
  intact: pad needs conyard + techcenter + the promotion + derricklimit;
  the promotion itself needs the Rapier Jumpjet promotion + rank1.
  Tester most likely hadn't completed the two-step promotion chain or
  hit the lobby derrick limit. Not a code bug; maintainer to confirm
  in-game.
- [x] **RA1 Allies description listed SOVIET doctrines** (2026-07-17)
  — CONFIRMED + FIXED: `faction_ra_allies.description` in
  fluent/rules/en.ftl carried the 6 Soviet doctrines and doctrine
  feature bullets; replaced with the real Allied research tree
  (Advanced Radar Systems ... GPS Satellite Support).
- [x] **TD GDI APC described as amphibious** (2026-07-17) — CONFIRMED
  + FIXED in FACTIONS.md: locomotor is `tracked` (not amphibious); the
  AA capability is real (APCGunAA).
- [x] **Schwarzer Mond promotions missing** (tester, second report) —
  FIXED 2026-07-17: implemented the 3-column SM promotion grid from the
  maintainer's image in `SchwarzerMond/yaml/promotions.yaml`, wired all
  unit prerequisites, and verified `^PromotionUnitBuff` on promotion units.
  Boot test passed.
- [x] **Warhead wall-capitalization** (2026-07-17) — evidence reversed
  the call: lowercase `wall` IS the standard (all 3 TargetTypes
  definitions + 345 weapon refs lowercase; only 2 refs used `Wall`).
  Normalized the 2 outliers (starcraft, starwars) to lowercase instead
  of churning 348 lines. Convention documented in DESIGN §1.
- [x] **P0 CRASH: missing `futuretech_concretebarrier_brik.shp` during menu load**
  (2026-07-17) — FIXED: corrected `brik:` sequence in `sequences/tiberiandawn.yaml`
  to use `brik.shp` / `brikicon.png` matching release. Boot verified.
- [x] **P0 CRASH: `japan_chainlinkfence_icon.tem` not found in `cycl` sequence**
  (2026-07-17) — FIXED: replaced with `cyclicon.png` matching release in
  `sequences/tiberiandawn.yaml`. Boot verified.
- [x] **P0 BUG: TD GDI vehicle palette issues** — RESOLVED by user confirmation
  (2026-07-17): palettes are correct in current build; tester was likely on an
  older commit without fixes.
- [x] **P0 BUG: All renamed factions missing voice/notification variants** (2026-07-17)
  — ROOT CAUSE: faction rename migration changed `InternalName` values (e.g.
  `gdi`→`td_gdi`, `nod`→`td_nod`, `allies`→`ra1_allies`/`ra2_allies`,
  `soviets`→`ra1_soviets`/`ra2_soviets`, `tsgdi`→`ts_gdi`, `tsnod`→`ts_nod`),
  but audio variant/prefix keys in `voices.yaml`, `notifications.yaml`, and
  `redalert2.yaml` still used the old names. Without a matching key, the engine
  falls back to `DefaultVariant`/`DefaultPrefix` with no faction suffix,
  producing filenames like `vehic1.aud` instead of `vehic1v00.aud` — which
  don't exist, so voices/notifications are silently skipped.
  FIX: added variant entries for all renamed factions to:
  - `voices.yaml`: `GenericVoice`, `VehicleVoice` (td_gdi, td_nod);
    `RAGenericVoice`, `RAVehicleVoice` (ra1_allies, ra2_allies);
    `RussianVehicleVoice` (ra1_soviets, ra2_soviets)
  - `notifications.yaml`: Prefixes section (td_gdi, td_nod, ra1_allies,
    ra1_soviets, ra2_allies, ra2_soviets, ts_gdi, ts_nod)
  - `redalert2.yaml`: `RA2EngineerVoice`, `RA2MCVVoice`, `RA2LanderVoice`
    Prefixes (ra2_allies, ra2_soviets, ra1_allies, ra1_soviets, td_gdi, td_nod)
  Units with explicit `Voiced` traits using non-variant voice sets (e.g.
  `TSVehicle`, `CommandoVoice`, `BattleFortressVoice`) were unaffected.
  Boot verified, no new exceptions.
- [x] **CRASH: ixian_koda_tank missing icon sequence** — VERIFIED 2026-07-16:
  the `icon` sequence already exists in `Ixian/yaml/sequences.yaml` (line 1372,
  `Filename: DATA.R16, Start: 4028`). `audit_sequences.py` reports 0 S2 missing
  sequences. Crash may have been fixed in a prior session.
- [x] **BUG: Repair drone not repairing** — root cause: `AutoTarget:
  EnableTargeting: false` prevented auto-acquisition of repair targets.
  Fixed by removing the override and restoring `InitialStance: Defend,
  ScanRadius: 12` from `^HelicopterTemplate`. Also set
  `PersistentTargeting: true` on `AttackAircraft` to maintain repair
  targeting. Matches working Ixian repair drone pattern.
- [x] **BUG: Tarantula firing offset** (2026-07-17) — FIXED: restored
  release values. `Turreted: Offset` from `-500,0,0` to `-500,1,1`;
  `LocalOffset` from `500,0,250` to `800,300,700` on both armaments.
  The offset had been changed during the CABAL rebalance and broke
  projectile origin alignment.
- [x] **BUG: Artillery spider firing offset** (2026-07-17) — FIXED:
  restored release values. `LocalOffset` from `300,0,800` to
  `-125,1,250,-125,1,250` (dual barrels) on both armaments.
- [x] **BUG: Tarantula upgraded weapon missing correct magicnuke explosion**
  (2026-07-17) — FIXED: `TS120mm_bluenuke` was using `magicnuke_small`
  (Scale 0.25) instead of `magicnuke_med` (Scale 0.5). Per the scaling
  system: `magicnuke` (1.0) = superweapon, `magicnuke_med` (0.5) =
  second biggest (artillery/heavy units), `magicnuke_small` (0.25) =
  third, `magicnuke_micro` (0.2) = fourth. The Tarantula deals the
  most damage among units, so it gets `magicnuke_med`. The Artillery
  Spider's `CabalArtilleryWalkerShellUpgraded` already correctly used
  `magicnuke_med`.
- [x] **RENAME: interceptor.nax → naxis_interceptor** — renamed
  `nax_interceptor.shp` to `naxis_interceptor.shp` in `bits/ra2/mod/`,
  updated all references in Naxis `sequences.yaml`.
- [x] **RENAME/MOVE: drone.nax → schwarzermond_drone** — renamed
  `nax_drone.shp` to `schwarzermond_drone.shp` and `nax_drone_icon.png`
  to `schwarzermond_drone_icon.png`. Updated SchwarzerMond `sequences.yaml`
  and Naxis `sequences.yaml` (interceptor icon reference).
- [x] **BUG: CABAL Obelisk range/detection** — weapon range set to 12288,
  `WithRangeCircle: Range: 12c0` added, `RevealsShroud: Range: 7c0` matches
  Nod obelisk. All three items already present in working copy.
- [x] **BUG: Starcraft alien ranks applied to all SC factions** (2026-07-17)
  — FIXED: verified that separate decorations already exist in code:
  `^ZergRankDecoration` (alienrank), `^TerranRankDecoration` (terranrank),
  `^ProtossRankDecoration` (protossrank). All three sequence definitions
  exist in `sequences/misc.yaml` using `alienranks.png` as placeholder.
  Found and fixed 7 actors missing their faction's decoration:
  `protoss_corsair`, `protoss_positron`, `terran_madcap`,
  `terran_jimraynor`, `terran_goliathmk2`, `zerg_guardian`,
  `zerg_gorekraken`, and `SCINTERCEPTOR`. Updated
  `audit_rank_decoration.py` to recognize the new decoration names and
  correct `StarCraft` path casing. Audit now reports 0 StarCraft issues.
- [x] **RULE: ActorStatValues upgrade list limit** — documented in DESIGN.md §6
  (design 2026-07-17): `ActorStatValues.Upgrades` maximum expanded from 5 to 10.
  Every unit must list all faction upgrades that affect it; team upgrades from
  other factions must never appear. Applied to `ra1_soviets_monstertank`.
- [x] **RULE: Promotion-unit prerequisite formula** — documented in DESIGN.md §15:
  `Buildable.Prerequisites: ~productionbuilding, techbuilding, ~promotion`.
  The `~promotion` token hides the unit until the promotion is bought; tech
  buildings disable but do not hide. Applied the `~promotion` change to ~144
  promotion units across all factions; reverted accidental `~promotion` changes
  in promotion-actor prerequisite chains.
- [x] **RA1 Soviet Monster Tank upgrade coverage** — added all tank/vehicle doctrine
  and upgrade inherits: `^InfernoDoctrineRA1`, `^TeslaExperimentalTechDoctrineRA1`,
  `^TeslaRocketsUpgradeRA1`, `^NuclearRocketsUpgradeRA1`, `^NuclearShellsTeamUpgradeRA1`,
  plus modest `FirepowerMultiplier` traits for the rocket conditions. Added the
  full `ActorStatValues` upgrade list (10 entries). Note: combined firepower
  stack may exceed the 2.0× power-budget rule for an epic unit; monitor in
  playtesting.
- [x] **All-faction promotion construction-yard gates restored** — corrected an
  earlier mistake: promotion actors MUST keep their `~constructionyard`
  prerequisite. Re-added `~constructionyard` to all promotion actors across all
  factions and updated `tools/audit/audit_promotion_gating.py` and DESIGN.md §15
  to enforce this rule. Promotion-units themselves still use
  `~productionbuilding, techbuilding, ~promotion`.
- [x] **Yuri Mastermind turret attack** — added missing `AttackTurreted:` trait to
  `yuri_mastermind`. The actor already had `Turreted:` and `Armament@PRIMARY`,
  but no turret attack activity, so it defaulted to frontal behavior.
- [ ] **BALANCE: Eliminator 800 overpowered** — 7 Eliminator 800s
  destroyed AI base with only 1 loss. Needs rebalancing (part of full
  CABAL rebalance). Effort: M. **Do NOT auto-apply — requires user
  approval per balance policy.**
- [ ] **BALANCE: Warcraft anti-air damage** — Warcraft anti-air damage is
  reportedly too low/unsatisfying. Needs investigation and balance pass
  (warhead values, weapon targeting, or unit stats). Effort: M. **Do NOT
  auto-apply — requires user approval per balance policy.**

### P2 — SM promotion grid tier ladder (RESOLVED 2026-07-17 — maintainer picked the reshuffle)

**FINAL LAYOUT (implemented; column convention: left = infantry, middle =
vehicles/tanks, right = aircraft/artillery/support):**

| Rank | Infantry | Vehicles | Air/Artillery/Support |
|---|---|---|---|
| 1 | Noid MG (T2) | Lunar Tiger (rpl Panzer, T2) | Laser Tank (rpl Beetle, T1) |
| 2 | Übermensch (T3) | MARS (rpl Jagerline, T2) | Haunebu III (rpl H2, T3) |
| 3 | Korruptes Biest (T3) | Crystal Tank (T3) | Piercer (T3) |
| 4 | Parzival | Dalek | Die Glocke |

Implemented 2026-07-17: promotions.yaml re-chained + BuildPaletteOrder
row-major; promotion actor `..._bermensch` renamed `..._ubermensch`
(umlaut law); `^PromotionUnitBuff` corrected to the FutureTech
convention — EXACTLY the 12 grid units inherit it (was also on 10 base
units incl. all 4 replaced ones: Lunar Soldier/Rocket, Laser Beetle,
Lunar Panzer, Jagerline, Haunebu II, Neo Jagdpanzer, Lunar Grille,
Gravity Core Tank, Black Bomb — an unintended faction-wide buff).
Follow-up: fluent-ify the 12 promotion tooltips (raw strings), part of
the SM rebalance pass below.

<details><summary>Decision record (superseded analysis)</summary>

#### Original analysis — kept for the record

**Maintainer input (2026-07-17):** MARS is an AA artillery unit (ground +
air, long range); Laser Beetle and Laser Tank are AA too, so MARS replacing
Jagerline is fine — the earlier "loses mobile AA" concern is WITHDRAWN.
Binding principle: promotion rank should ladder with tech tier. Open
dilemma: Übermensch is T3 yet sits at rank 1; making it a base unit leaves
an empty cell; SM also feels thin on early-game units.

**Implemented state:** the grid exists in `SchwarzerMond/yaml/promotions.yaml`
with CABAL-pattern gating already in place (the promotion gates the option;
units keep their tech prereqs — e.g. Dalek needs warfactory + techcenter +
promotion; replaced units carry `!promotion_x`). NOTE: the implemented
chains DEVIATE from the maintainer's image: col1 Übermensch→Crystal
Tank→Korruptes Biest→Parzival, col2 Laser Tank→Lunar Tiger→Noid MG→Dalek,
col3 MARS→Haunebu III→Piercer→Die Glocke (the image had Noid MG + Piercer
at rank 1 and MARS at rank 3). Needs maintainer sign-off either way.

**The structural fact:** 8 of the 12 grid units are T3 payoffs. Only Laser
Tank (T1 replace), Noid MG, Lunar Tiger and MARS (T2) are early/mid-game.
A strict tier-per-rank ladder is impossible with these 12 units — but a
clean monotone ladder IS possible, because rank 2+ arrives around the T3
tech era anyway. Only rank 1 must be early-tier.

**Base roster reference (after promotion extraction):**
T1 base: Lunar Soldier, Lunar Rocket, Engineering Armor, Laser Beetle,
Lunar Panzer, Sturm Cannon, Laser Tower. T2 base: Jagerline, Lunar
Grille, Space Zeppelin, Noid Harvester. T3 base: Neo Jagdpanzer,
Gravity Core Tank, Black Bomb. (Everything else is promotion-gated.)

**Option 1 — RESHUFFLE (recommended; zero balance impact — only
promotions.yaml chains + BuildPaletteOrder change, unit files untouched):**

| Rank | Vehicles | Infantry & walkers | Armor & air |
|---|---|---|---|
| 1 | Laser Tank (rpl Beetle, T1) | Noid MG (T2) | Lunar Tiger (rpl Panzer, T2) |
| 2 | MARS (rpl Jagerline, T2) | Übermensch (T3) | Haunebu III (rpl H2, T3) |
| 3 | Crystal Tank (T3) | Korruptes Biest (T3) | Piercer (T3) |
| 4 | Dalek (capstone) | Parzival (capstone) | Die Glocke (capstone) |

Ladder: rank 1 = T1/T2, rank 2 = T2/T3, rank 3 = T3, rank 4 = buildlimit-1
capstones. Replacements (straight upgrades) all land in ranks 1–2, pure
unlocks in ranks 2–4. Übermensch keeps its promotion prestige at rank 2,
matching its techcenter timing.

**Option 2 — Übermensch to base roster, swap in an existing T3 base unit**
(if the flagship must always be visible): Übermensch becomes a regular T3
buildable; its cell is filled by promoting an existing base T3 unit into
the grid instead (candidates: Gravity Core Tank, Neo Jagdpanzer, Black
Bomb) — no new art needed, roster depth unchanged. Then apply Option 1's
ladder to the resulting 12.

**Option 3 — early-game retier pass** (independent lever for "not enough
early units"; sheet-first + maintainer approval since stats/prices move):
candidates Noid MG T2→T1 (classic MG-infantry tier) and/or Lunar Grille
earlier. Combines freely with Option 1; does NOT block it.

**Rejected:** keeping the image order with pure visibility-gating (rank 1
unlocking T3 options) — contradicts the maintainer's tier principle. New
early-game units (old Option E) stays a separate long-term roster item.

(Maintainer picked Option 1 with two switches: infantry column left,
vehicles middle; Laser Tank ↔ Lunar Tiger swapped — Lunar Tiger is a
line tank, Laser Tank plays as support. See the FINAL LAYOUT above.)

</details>

### P0 — TKM CONTRIBUTOR PORT (ordered 2026-07-18, jumps the queue)

A community contributor updated TKM (new upgrades and/or rebalance) but
can't merge anymore after our renames. He sent his ENTIRE repo as a zip,
extracted from a contributor's zip (base version UNKNOWN). Plan: (1) inventory his tree; (2) find his base by matching his
files against our git history / the golden reference release; (3) his
real changes = diff(his tree, base); (4) port onto master through the
rename maps (old ids → tkm_*) into ContentPacks/TKM/TKM/yaml; (5)
audits + boot + commit. Balance numbers he changed are the
contributor's design — port faithfully, flag anything that contradicts
DESIGN formulas instead of silently "fixing".

### New orders 2026-07-19 (template-conformance + classic rifles)

- [x] **RULE + AUDIT: conyard power** — VERIFIED 2026-07-30:
  `audit_template_conformance.py` T1 reports 0 findings. All conyards
  already use the template's 100 power. No overrides found.
- [x] **RULE + AUDIT: icon offsets** — VERIFIED 2026-07-30:
  `audit_template_conformance.py` T2 reports 0 blocking findings.
  6 informational T2b items (D2k legacy + TS 0,0,25 Z-offset patterns)
  flagged for maintainer visual pass — not violations.
- [ ] **LAW: range bands** — every unit stays within ±10% of its class
  baseline range (scouts: 4500–5500 around 5000); lower edge = cheapest
  units, upper edge = most expensive. Applies to ALL templates.
- [x] **Classic rifles get unique characters** — DONE 2026-07-19 (Formula
  v2 scout conversion): TD GDI/Nod minigunners burst 4, RA1 Allies/Soviets
  rifle infantry burst 3, FP multiplier compensation, cost 100 from
  templates. Each has unique HP/speed/range/burst-delays/FP-mult:
  GDI (31k HP, 63 spd, 5499 rng, BD 3, FP 24), Nod (30k HP, 66 spd,
  4609 rng, BD 2, FP 29), Allies (27k HP, 55 spd, 5500 rng, BD 4, FP 47),
  Soviets (34k HP, 54 spd, 4668 rng, BD 5, FP 42). Verified 2026-07-30.

### ~~P0 — ENGINE PIN vs LOCAL ENGINE MISMATCH~~ RESOLVED 2026-07-19

Commit a4b2eb8a7 (#210) bumped mod.config ENGINE_VERSION to `b89ae60`
but the local engine/ is still `7ba39d9` and NO engine fetch/build ran
— `launch-game.cmd` refuses to start ("Required engine files not
found") for EVERYONE on a fresh pull until the engine is updated
(make all / fetch b89ae60 + dotnet rebuild) or the pin is reverted.
Owner: whoever landed #210 (their session likely has the context).
My boot gates ran against the proven 7ba39d9 via a temporary LOCAL
pin revert (never committed). **RESOLVED: `make.cmd all` fetched b89ae60 and rebuilt engine + all mod assemblies (0 errors); boot to menu verified on the new engine. TEAMMATES: run `make.cmd all` once after pulling if your local engine is still 7ba39d9.**

### New orders 2026-07-18 (third batch — crash + SM polish)

- [x] **P0 CRASH (TheCommando315): `KeyNotFoundException 'badr'` in
  ProductionParadropCA.Produce** — VERIFIED 2026-07-27: already fixed.
  C# default is `ra1_badger` (not `badr`), and both YAML usages in
  `ContentPacks/RedAlert/Allies/yaml/buildings.yaml` have explicit
  `ActorType: ra1_badger`. No remaining references to `badr` exist.
- [x] **BUG (Blackrobe follow-up): replaced SM units stay VISIBLE
  (greyed) after their replacement promotion** — VERIFIED 2026-07-27:
  already fixed. All four affected units use `~!` prefix correctly:
  Laser Beetle (`~!schwarzermond_promotion_lasertank`), Lunar Panzer
  (`~!schwarzermond_promotion_lunartiger`), Jagerline
  (`~!schwarzermond_promotion_mars`), Haunebu II
  (`~!schwarzermond_promotion_haunebuiii`).
- [ ] **RENAME ORDER (maintainer): "Jagerline" is fake German** — the
  unit is a ROCKET anti-air vehicle (maintainer 2026-07-18), so the
  gun-flakpanzer names (Kugelblitz/Wirbelwind/Ostwind) do NOT fit.
  Historically correct German AA-ROCKET names to pick from:
  **Wasserfall** (guided AA missile — recommended), **Taifun**
  (salvo-fired unguided AA rocket — fits a line vehicle),
  **Rheintochter** (AA missile, most distinctive sound). Awaiting the
  maintainer's pick; then one pass: id
  (schwarzermond_m200bjagerline -> schwarzermond_<name>), display name
  (drop the American-sounding "M200B" or Germanize it), ftl, MARS
  replacement description, sheet row.

### New orders 2026-07-18 (second batch — Blackrobe report + maintainer)

- [ ] **BUG (Blackrobe): SM passive income building missing** — being fixed in the maintainer's OTHER session (uncommitted WIP adds ra2oilderrick/ra2ywall provisions to the SM conyard); moondairyfarm itself verified wired (techcenter+derricklimit). Do not double-fix. on latest
  dev commit — find what removed/hid it and restore.
- [x] VERIFIED 2026-07-18 **"laser car" + M200B report**: wiring is
  correct both ways (before purchase Beetle/Jagerline buildable; after,
  retired and Laser Tank/MARS appear). If Blackrobe means the
  REPLACEMENTS never appear even after buying the promotions, the rank1
  prerequisite may not be granted by the lobby points option — needs an
  in-game check by the team.
- [x] DONE 2026-07-18 **TKM moved into ContentPacks/RedAlert2Mod** (Blackrobe: do
  the move, postpone the theme-folder rename decision — CnCUniverse /
  CnCExtended / RA2Expanded still open, "not wise to rush").
- [x] DONE 2026-07-18 (superlinear ramp RampFactor 0.08, min-count dip fix, wave veterancy floor(idx/4)) **Survival difficulty (maintainer order):** steepen the ramp so
  late waves outscale early ones, fix the tier-3/4 dip (min unit
  count), and make waves elite over time (veterancy/upgrades — "apply
  upgrades over time or all available from the start").
- [x] DONE 2026-07-18 (MonsterTankTuskTesla/Thermobaric weapons, armament swaps, flat multipliers removed) **Monster tank rockets (maintainer order): apply the MAMMOTH TANK
  logic** — real weapon swaps for Tesla Rockets, (Thermo)Nuclear
  Rockets etc., not the current flat +10% firepower multiplier.

### New orders 2026-07-18 (mid-turn batch)

- [ ] **Theme-folder rename + TKM move (DECISION PENDING — maintainer
  picks the name first).** TKM belongs inside the RA2-mod theme folder
  (it presents in-game as an RA2 modded faction), but
  `ContentPacks/RedAlert2Mod/` shall be renamed first: the folder holds
  RA2-mod factions AND Cameo originals AND other-mod imports; maintainer
  floated "CnCExpandedUniverse", wants alternatives + effort estimate.
  No split into two folders. Move TKM only AFTER the rename so paths
  churn once.
- [x] **BUG (tester, maintainer-confirmed "add to the list"): Tesla
  Rockets upgrade has no visible effect on the monster tank.** VERIFIED
  2026-07-27: wiring is correct. `ra1_soviets_monstertank` inherits
  `^TeslaRocketsUpgradeRA1` which grants the condition; armament
  conditions properly switch between `MonsterTankTusk` (base) and
  `MonsterTankTuskTesla` (upgraded). The Tesla weapon has different
  damage (26750 vs 20000), Tesla damage type, EMP, arc shrapnel, and
  `ra2_tesla_impact` visual. Issue is subtle visual feedback, not
  wiring. Also tester: doctrine upgrades "don't have very descriptive
  descriptions" — separate issue, needs description text improvements.
- [ ] **Survival map (unpacked at maps/survival/ by maintainer/tester —
  do NOT clobber; NFWRambo makes his own `survival 2` copy):
  (a) BUG: game does not end when all waves are cleared — FIXED
  2026-07-29: Implemented CA-style `InitObjectives` (speech notifications +
  objective feedback), centralized `ResolveMission` function, `GameLost`
  guards on ALL perpetual systems (verified zero unguarded), `PendingSpawns`
  counter to prevent premature victory from async reinforcements, and coop
  player elimination handling. Research documented in
  `docs/design/mission_win_lose_research.md`.
  (b) difficulty dip waves 12–15 — ADDRESSED 2026-07-27: randomized
  wave system now pads waves with cheapest unit to meet minUnits floor.
  (c) maintainer idea: waves spawn with all upgrades ("elite force") —
  IMPLEMENTED: veteran levels scale with wave index (1 per 4 waves).
  (d) pacing — UPDATED 2026-07-29: max game time capped at 60 min.
  Prep 2.5-3.5 min (150-210s), wave gaps 35-135s (10% short/10% long/80%
  normal). Worst case: 210 + 25*135 = 3585s < 3600s. Budget variance
  reduced from -50%/+80% to -50%/+50% (0.5x-1.5x) to compensate for
  higher max difficulty multiplier. Difficulty reworked from 5 tiers to
  7: TRIVIAL 0.5x, EASY 0.75x, MEDIUM 1.0x, HARD 1.25x, BRUTAL 1.5x,
  UNBEATABLE 1.75x, NIGHTMARE 2.0x. Min/max thresholds unchanged, 2 new
  intermediate tiers (BRUTAL, UNBEATABLE) spread evenly between HARD and
  NIGHTMARE. All taunt lines, hysteresis, and event commentary updated.
  (e) RANDOMIZED WAVES — IMPLEMENTED 2026-07-27: each wave now randomly
  picks a faction from a tier-appropriate pool (22 factions across T1-T4),
  fills an increasing budget with random units from that faction, and
  spawns faction-specific power plants + airfields every wave (old
  buildings are destroyed and replaced). Every playthrough is now
  different.**
  (f) COST & ACTOR VERIFICATION — COMPLETED 2026-07-27: all 22 factions'
  unit, aircraft, and epic costs verified against YAML definitions.
  Fixed 4 cost mismatches (ra1_allies_machinegunner 400→557,
  zerg_hydralisk 500→3314, ra2_soviets_flaktrooper 300→416,
  yuri_gatlingtrooper 300→431). Fixed 1 wrong building reference
  (japan_corepowerplant→japan_waveforcereactor: the former is a
  deployable vehicle, not a ^PowerPlant building). All powerplant
  buildings verified to have ^PowerPlant trait; all airfield buildings
  verified to have ^IsAircraftFactory + Reservable traits.**
  (g) GENERAL TAUNT SYSTEM — IMPLEMENTED 2026-07-27: each wave now picks
  a random general from the faction's roster (3 generals per faction,
  66 total). Each general has a doctrine (infantry/tank/aircraft) that
  biases unit selection 60% toward their specialty, and 6+ unique
  taunt lines in the style of Generals Zero Hour Challenge mode.
  Taunts play at wave start, mid-wave (15-25s later), and final wave
  gets a third taunt. Lines reference faction lore, unit costs, memes,
  and internet culture. Database in maps/survival/generals.lua.**
  (h) STARTING DEFENSES — IMPLEMENTED 2026-07-31: human players now
  receive faction-specific power sources (~500 power) and defensive
  turrets in 4-fold symmetric rings around their base. Turrets are
  strictly own-faction and exclude garrisonable bunkers; the placement
  budget targets ~10k cost per player, using the most expensive turrets
  first and falling back to cheaper ones until the target is met.
  `FactionTurrets`, `DefenseCosts`, and `FactionPowerPlantData` are
  defined in `mods/cameo/maps/survival_work/script.lua`. Heavy support
  starting army is wired but left disabled (`HeavySupport = false`) until
  a map option is added.

### P1a — FORMULA V2 CLASS 1: SCOUT INFANTRY (maintainer 2026-07-18)

Maintainer picked the scout class first; proposed anchor 20000 HP /
50 Speed / 5.0 Range / 4000 Damage / 50 Reload / Cost 100 with the
2x-health bake replacing the ScoutInfantryBuff damage reduction.
Assessment + simulation: docs/balance/formula_v2_scout.md — anchor
structure confirmed, speed 60 recommended over 50, bake endorsed;
BLOCKED ON: (1) garrisoned/pricing armament flag in the extractor,
(2) WeaponClass seeding for the class weapons, then bake -> anchor ->
sign-off. Awaiting maintainer GO on the refined spec.

### P1 — BALANCE PIPELINE (ordered 2026-07-18 — "very important long term goal")

Full plan: **docs/design/BALANCE_PIPELINE.md**. PHASE 1 DONE
2026-07-18: `tools/balance/extract_stats.py` + committed baseline
ledger (32 faction files, 2025 actors, raw stats + provenance,
deterministic, `--check` drift mode). PHASE 2 DONE 2026-07-18:
`formula.py` (Tiger identity exact, symbolic equivalence vs the
legacy cell formulas exact, closed-form Range solver) +
`build_workbook.py` -> cameo_balance_v2.xlsx workbench (gitignored;
32 faction tabs, weapon sub-rows, live formulas, locked non-input
cells, delta traffic lights). PHASES 3+4 DONE 2026-07-18 — WORKING
PROTOTYPE: seed_design.py (437 units seeded from the legacy sheet,
discrepancies.md: 22 cost mismatches, 581 never-priced combat units,
180 unmatched legacy rows for name_map.yaml), import_workbook.py
(xlsx -> ledger, input cells only, proportional warhead scaling),
apply_balance.py (ledger -> yaml via provenance, resolved-value
diffing, SHADOWED-definition detection, --confirm gate). Loop PROVEN:
fixed point exact (0 changes on untouched ledger), live demo
1000->1050->1000 through ledger+push with yaml byte-identical after.
Bonus: the fixed-point test exposed and fixed a resolver cache
poisoning bug affecting ALL audits. Next: Phase 5 Formula v2 +
Phase 6 enforcement (balance check into run_all). yaml → per-faction JSON
ledger (committed) → generated cameo_balance_v2.xlsx (CABAL-tab format,
formulas live in the sheet, locked cells) → legacy-sheet comparator +
discrepancy triage → gated write-back (apply_balance.py, maintainer
order only) → drift audit in run_all so hand-edited balance numbers
become red findings mechanically. Phases 1-3 first (extractor,
workbook builder, comparator); the SM rebalance below is the
pipeline's first customer.

- Jagerline rename: new candidate from maintainer "Alter Peter" (the
  Munich bell tower) — parked with Wasserfall / Taifun / Rheintochter;
  maintainer explicitly wants to think more before deciding.

### P1b — FULL SCHWARZER MOND REBALANCE (ordered 2026-07-17 — now the balance pipeline's first customer)

Maintainer order: "we also need a full rebalance on the schwarzer mond
faction." Rules of engagement:
- **Sheet first** (absolute law): every price/tier lands in
  `docs/design/cameo_armor_system.xlsx` (M in its cell, O/P/Q
  recompute) BEFORE yaml; both edits in the same pass. If the `~$` lock
  file exists the workbook is open — queue the sheet edit and say so.
- **Sequencing**: the rebalance prices the POST-buff-strip stats (the
  10 base units just lost the unintended ^PromotionUnitBuff — their
  effective firepower/durability changed ~10%, so old prices are stale).
- **Workplan**: extract all schwarzermond_* rows from
  `docs/audit/latest/stat_formulas.md` (formula deviations) +
  `power_budget.md`; propose per-unit price/tier corrections; maintainer
  approves the numbers; then sheet + yaml dual-write, §15 superiority
  check on the 4 replacement pairs (Beetle→Laser Tank, Panzer→Lunar
  Tiger, Jagerline→MARS, H2→H3), boot + audits.
- Include: fluent-ification of the 12 promotion tooltips/descriptions
  (raw strings today) and the SM upgrades/defenses columns.

### P2b — CABAL promotion grid tier-mismatch (DESIGN DECISION NEEDED)

**Same problem as SM.** CABAL's 3×4 grid also has tier mismatches:

| Row | Col 1 (Infantry) | Col 2 (Vehicles) | Col 3 (Aircraft) |
|---|---|---|---|
| 1 | Devout **T2** | Spider CNC4 **T1** | Cyborg Assassin **T2** |
| 2 | Ascended **T2** | Heavy Reaper **T2** | Super Hunter Killer **T1** |
| 3 | Beholder **T3** | Widow **T2** | Overkill Gunship **T1** |
| 4 | Cyborg Commando V2 **T3** | Core Defender **T2** | Mothership **T1** |

Col 3 (Aircraft) is inverted: Row 1 is T2, but Rows 2-4 are all T1 (helipad
only). Capstones (Row 4) include T1 and T2 units alongside T3.

**How FutureTech solved it:** All 12 FutureTech promotion-units are T3.
Every unit requires high-tier buildings (`battlelab`, `hypercore`,
`robotcontrolcenter`, `transmissioncenter`). The promotion grid only
determines *which* T3 units you can see — the tech buildings gate the
actual power. No tier mismatch because all units are the same tier.

This works for FutureTech because it's a high-tech faction where everything
is advanced. CABAL is more diverse — it has T1 helipad units, T2 cyborg
factory units, and T3 techcenter units.

**Solution options for CABAL:**

**Option FT — Make all promotion-units T3 (FutureTech pattern)**
- Add `cabal_techcenter` (or `cabal_core`) as a prerequisite to every
  promotion-unit that doesn't already have it.
- The promotion grid then just gates visibility — all units are T3 power.
- **Pro:** Cleanest, proven pattern (FutureTech works). Zero grid
  restructuring. Matches the CABAL pattern already in use.
- **Con:** T1/T2 units (Overkill Gunship, Hunter Killer, Devout, etc.)
  become T3 — delayed availability, possible balance shift. Mothership
  and Overkill Gunship are currently early-game options; making them T3
  changes CABAL's early game feel.
- **Effort:** S (add prereq to ~6 units, boot test).

**Option SR — Sort grid rows by tier (restructure)**
- Row 1 → T1 units: Spider CNC4, (new T1 vehicle), Hunter Killer
- Row 2 → T2 units: Devout, Heavy Reaper, Cyborg Assassin
- Row 3 → T3 units: Beholder, Widow, Overkill Gunship (rebalance to T3)
- Row 4 → Capstones: Cyborg Commando V2, Core Defender, Mothership
- **Pro:** Clean tier ladder.
- **Con:** Requires rebalancing several units (Overkill Gunship T1→T3,
  Mothership T1→T3). Need a T1 vehicle for Col 2 Row 1 (or move an
  existing unit down). Significant balance pass.
- **Effort:** L (rebalance + grid restructure + test).

**Option HY — Hybrid: tier-sort columns + CABAL gating**
- Sort each column so tiers go T1→T2→T3→capstone within the column.
- Keep the CABAL pattern: promotion gates visibility, tech gates power.
- Col 1: Devout (T2) → Ascended (T2) → Beholder (T3) → Cyborg Commando V2 (T3)
- Col 2: Spider CNC4 (T1) → Heavy Reaper (T2) → Widow (T2) → Core Defender (T2)
- Col 3: Hunter Killer (T1) → Overkill Gunship (T1) → Cyborg Assassin (T2) → Mothership (T1)
- **Pro:** Best tier progression within columns. Minimal unit changes.
- **Con:** Col 3 is still mostly T1 — aircraft are inherently low-tier
  for CABAL. Would need to rebalance aircraft to higher tiers for a
  clean ladder, or accept that CABAL aircraft are early-game.
- **Effort:** M (rearrange grid + minor rebalance + test).

**Option KP — Keep as-is, promotion gates option (accept the mismatch)**
- CABAL already uses the "promotion gates visibility, tech gates power"
  pattern. The tier mismatch is acceptable because:
  - Row 1 unlocks options you can build once you have the right building.
  - Row 4 capstones are powerful regardless of tier (Mothership is T1
    but requires `cabal_core` which is a late-game building).
- **Pro:** Zero changes needed. Already works.
- **Con:** Tier progression doesn't feel intuitive. A new player might
  expect Row 4 to be "bigger" than Row 1 but it's not always.
- **Effort:** S (zero).

**Recommendation:** Option FT (FutureTech pattern) is the cleanest if
we're willing to make all CABAL promotion-units require `cabal_techcenter`
or `cabal_core`. This is the proven solution. However, it changes CABAL's
early game by delaying T1/T2 promotion-units to T3. Option KP (keep
as-is) is the lowest-risk if the maintainer is comfortable with the
existing CABAL pattern where `cabal_core` already gates the capstones.

**Note:** `cabal_core` (CABAL Core building) is already a high-tier
prerequisite for Core Defender, Widow, and Mothership. It's effectively
CABAL's T3.5 gate. If we standardize all promotion-units to require
either `cabal_techcenter` or `cabal_core`, we get the FutureTech pattern
without changing the feel of individual units — just their availability
window.

**Awaiting maintainer decision before implementation.**

---

## CABAL — recently completed (this push)

- [x] Confident quick fixes: missile arc, HK mk1 blue laser, Core
  Defender offset, Mantis sound (`87a716b41`).
- [x] Crab → **Ravager** infantry plasma line-breaker + plasma bullet
  effect (`e4ac0ce40`, `b31113a6d`). Crab id retired.
- [x] CABAL weapons get their own firing sounds (`1281a71f5`).
- [x] Rocket-launcher offsets/counts + Manticore dual laser (`c4691e758`).
- [x] Mantis + Laser Spider → AttackFrontal fire support (`cc6a290db`).
- [x] Dissolver: cloak → corrosion (`corroded` cond) + TankDestroyer +
  LightChemical combo + new `cabal_dissolveimpact` effect (`de25b469d`);
  effect re-rendered to fit its frame (`45b8f0caa`).
- [x] Eliminator 800: real `^GatlingSpeedUpUnitBehavior` spin-up (drop
  the AmmoPool hack), single ground + Air-only twin, dune autogun muzzle
  @3671 (`33c13a553`).
- [x] All CABAL infantry: vehicle-style turn rate 2×Speed/5 (`f98bf8155`).
- [x] Devin sound pass (uncommitted, verified, keep): DarkObeliskLaser /
  CabalCommandoPlasma / Mk2 → obelcor3.aud; Reaper/TwinBazooka/rocket
  weapons → samshot1.aud; Core Defender offset raise; magicnuke Tick tune.
- [x] Effect-naming: CABAL authored weapons already clean. `TS90mm_bluenuke`
  `@3Eff` is NOT a violation — it overrides `^TSCannonEffect`'s own
  `@3Eff`. Mod-wide sweep still pending (CE).

---

## CABAL — new orders 2026-07-13 (the big batch)

### N1. Green-plasma / neutron-shell gating (`7a0d0025d`)
- [x] New art: `cabal_greenplasma.png` (weak green plasma projectile) +
  `cabal_greenplasmaimpact.png` (green impact burst), both border-safe
  RGBA PngSheets.
- [x] **Neutron-shell gates every magicnuke weapon.** Non-upgraded
  (`!cabal_upgrade_neutronnuclearcatalyst`) = green plasma projectile +
  green impact; upgraded = the blue magicnuke. Pattern already on
  Artillery Spider + Tarantula (basic armament `!cond`, `Armament@Upgraded`
  `cond`); extend the same split to Cyborg Commando, Commando Mk2, and
  the Ravager. Consider updating the upgrade description (it now empowers
  the whole plasma line, not just Artillery+Tarantula).
- [x] **Magicnuke sizes scaled to power, all 4 used** (`magicnuke_micro`
  0.2 < `_small` 0.25 < `_med` 0.5 < `magicnuke` 1.0):
  - micro → TS90mm_bluenuke (~12k)
  - small → TS120mm_bluenuke (Tarantula, ~24k), CabalRavagerPlasma (~32k)
  - med   → Commando plasma (~50k), TS155mm_bluenuke (Artillery, ~60k)
  - **magicnuke (biggest) → the new CABAL superweapon ONLY** (below).
- [x] **Artillery Spider projectile rework** (`901a9018f`): Archer/Specter-style
  ballistic shell with visible blue contrail; upgraded version uses CABAL
  purple → dark-blue thicker contrail and adds Tesla/Magic/Railgun/Chemical
  warheads. Spreadsheet synced.

### N2. CABAL superweapon (biggest magicnuke) (`1f8b58820`)
- [x] New nuke support power, **same values as the Ixian EMP Nuke**
  (`supercomputer.ixian` `NukePowerCA` firing `PulseMissile`:
  ChargeInterval 10500, MissileWeapons PulseMissile, MissileDelay 25,
  CameraRange/CircleRanges 10000, etc.) but with the **biggest magicnuke**
  as the missile/impact animation (+ a new sound, see S-rules).
- [x] **Fired from the CABAL Core**, using **TD Nod Temple of Nod logic**,
  **plus an add-on that adds the missile silo**. (Find the Temple-of-Nod
  NukePower pattern; the "add-on = missile silo" is a prerequisite
  building/attachment that unlocks or houses the silo.)

### N3. CABAL Core = money structure (`7a0d0025d`)
- [x] Turn the CABAL Core into a **special money-generator structure like
  the Asian Military Academy**: **double the income of the Oil Derrick**,
  and it **also counts as an Oil Derrick** (provides that prerequisite /
  captured-tech behavior). It also launches the N2 superweapon.

### N4. Commando plasma weapons + CABAL Obelisk plasma-laser (high-impact + warhead combos)
- [x] DarkObeliskLaser, CabalCommandoPlasma, CabalCommandoPlasmaMk2: keep
  **obelcor3.aud** (do NOT change the sound). All three already use **long
  ReloadDelay + heavy Damage**.
- [x] The **two Commando plasma weapons** already carry the large-AoE triad:
  base = **Cannon + Flame + Chemical**; on the **neutron-shell upgrade**
  they add **Tesla + Magic + Railgun** warheads.
- [x] **CABAL Heavy Obelisk** (`TSCABALObeliskLaserFire`) made unique from
  TS Nod Obelisk: converted to **plasma-laser** = **Laser + Flame + Chemical**
  with matching percentage twins; removed inherited TS Nod upgrade armament;
  paired `cabal_laserimpact_l` effect + `obelmod1.aud`/`drtelectro.wav` sound.
- [x] Warhead audit pass: fixed `CabalMagicNuke`/`TS90mm_bluenuke` effect
  warhead naming, duplicate `Warhead@1Dam` in `TSCyCannon`, and incorrect
  `HealthPercentageDamage` twin on `TSHunterKillerLasers`.

### N5. Laser beam visual rework (DESIGN law — see below) (`6f43f5639`)
- [x] Every CABAL laser: **two beam colors** (inner + outer), a **mix of
  purple + dark blue**, **not too thin**. Beam **width scales with
  damage** (Mantis + all others currently too thin; Core Defender a touch
  too thick but must still scale). **Color also scales with damage**
  (scale BOTH colors so bigger damage looks more dangerous).
- [x] **Laser Spider → obelmod1.aud** (TS Obelisk sound) — FIX from the
  obelray1.aud I set. Smaller lasers → **laser turret sounds** (lastur1.aud).
- [x] **Manticore double laser**: too thin → **spread the two beams out
  more**; rebalance with **more range + more armor** (range/armor deferred to
  balance sheet per DESIGN §3).
- [x] **3 levels of laser ground-impact effect** (purple/blue, scaled by
  damage), applied to ALL laser weapons; each needs a new sound.

### N6. New CABAL effects + sounds
- [x] Audio audit: all CABAL weapons have Report + ImpactSounds (via
  inheritance or direct). Only CabalOverkillDroneLauncher was missing
  a Report — fixed (`5437d4f63`).
- [x] Effect-warhead naming: CABAL had 1 violation (CabalBerserkerBlades
  @3Eff -> @Effect) — fixed (`63c859fde`).
- [ ] New explosion effect for ALL CABAL missiles (+ new sound) — needs
  custom art/audio from maintainer.
- [ ] Plasma-weapon sounds: prefer NEW/unique; cross-check Shattered
  Paradise references. (Cannot synthesize quality .wav here — assign
  unique existing mod sounds and flag any that truly need new custom
  audio for the maintainer to source.)

### N7. Weapon-mount offsets (`7a0d0025d`)
- [x] **Ascended + Devout**: increase the **second (Y) value** of each
  triple offset ~**2×** so their weapons sit further left/right.

### N8. Armor combo (was CC; DONE)
- [x] Cyborg Commando + V2: Heroic/Superheavy dual-armor applied.
- [x] Eliminator 800: Flak/Heavy dual-armor applied.
- [x] Berserker: Heroic/Superheavy via `^HeroInfantryTemplate` + `^TSCyborgDualArmorHeavy`.
- [x] All 11 CABAL infantry verified: every unit has Armor@Secondary +
  DamageMultiplier@Secondary: 200 (some via `^TSCyborgDualArmor*` templates).

### N9. Role + tier + promotion rebalance (L, sheet-first) — MOSTLY DONE
- [x] **3×4 promotion grid fully populated**: Devout, Ascended, Beholder,
  CCV2 (infantry); Spider CNC4, Heavy Reaper, Widow, Core Defender
  (vehicles); Wasp Striker, Super Hunter Killer, Overkill Fortress,
  Mothership (aircraft).
- [x] **T1000 removed**; Beholder moved from Consortium to CABAL.
- [x] **All Omega variants removed** (HK2 Omega, Mothership Omega).
- [x] **Berserker refactored** to hero infantry (`^HeroInfantryTemplate`),
  T4, HP 800k, DPS 7500, cost 10000, from Cyborg Factory, requires Core.
- [x] **Overkill Fortress rebuilt** as Farasha-style carrier with drones.
- [x] **HK1 + Super Hunter Killer**: dual rockets + dual lasers.
- [x] **Carryall renamed**, unarmed transport.
- [x] **Spreadsheet synced**: 35 rows, all TechTier/UnitClass/Special
  values legal per DESIGN.md (1.0/0.75/0.5, epic=1.0/0.3), obsolete rows
  deleted, missing units added, names updated.
- [x] **Husk names fixed** (Carryall, Hunter Killer, Overkill Fortress,
  Overkill Drone).
- [x] **Design doc updated** (CABAL_FACTION_DESIGN.md reflects all changes).
- [x] **Template role audit**: fixed Engineer→^MechanicTemplate,
  Eliminator 800→^HeavyInfantryTemplate, Carryall→^UnarmedTransportHelicopterTemplate,
  Scarab APC→^SupportVehicleTemplate + ^CargoVehicle (`81bad88d2`).
- [x] **Balance formula audit**: all 30 CABAL units [OK] — 0 ABSURD, 0 HIGH,
  0 formula-broken. Fixed 7 problem units (Legion, Mothership, RocketCyborg,
  Wasp, WaspStriker, Ascended, Beholder) + dissolver crash (missing crippled
  sequences + wrong icon palette) (`50f3db5e4`); fixed 3 formula-broken
  workbook rows 27-29 (`160a6491a`).
- [x] **Repair Drone** added as buildable support aircraft (`94a58b2a7`);
  spreadsheet row added, icon uses carrier icon placeholder.
- [x] **Open question**: Overkill Fortress vs Overkill Carrier final name.

### N10. Upgrades audit
- [x] Reviewed every CABAL upgrade for meaningful consumption. Removed the
  meaningless `cabal_upgrade_clusterwarhead` (no actor, building, or template
  consumed it; also removed its Fluent description and AI entry).
  All other upgrades are wired: conditions granted by templates are
  inherited and used by at least one actor or support power. Kept the
  neutron-shell twins untouched.

### N11. Descriptions + AI
- [x] All CABAL units have Fluent descriptions (converted 8 inline \n
  descriptions to Fluent keys per DESIGN.md §7, `1f580f6e0`; plus 2 more
  fixed: cabal_refinery + cabal_mobileconstructionvehicle).
- [x] AI wiring: all CABAL units in UnitsToBuild list with weights.
  cabal_engineer added to CapturingActorTypes; stale tscyc2.cabal removed.
- [x] CABAL added to global Random + RandomTournament faction pools;
  "(WIP)" suffix removed from faction name.
- [x] Fluent key naming fixed: actor-cabal_core/actor-cabal_techcenter
  → underscores (actor_cabal_core/actor_cabal_techcenter).
- [x] Building name capitalization fixed: "Cabal Tech Center" → "CABAL
  Tech Center", "Heavy Cabal Obelisk" → "Heavy CABAL Obelisk".
- [x] Manticore description updated: removed trap net references (trap
  weapon removed from unit).

### CE (carried). Effect-warhead naming sweep, mod-wide
- [x] CABAL: 1 violation fixed (CabalBerserkerBlades @3Eff -> @Effect,
  `63c859fde`). CABAL is fully compliant.
- [x] Mod-wide: 202 renames across 40 files via scripted sweep
  (`2ad0f35e1`). Audit: `tools/audit/audit_effect_warhead_names.py`
  (0 violations). Template override names preserved; suffixed variants
  (@Effect2, @EffectAir2, etc.) recognized as canonical.

### CE2. CreateEffect Image field audit + explosion sequence consolidation
- [x] **CABAL CreateEffect Image: removal** (2026-07-15): Removed explicit
  `Image:` fields from all CABAL `CreateEffect` warheads in
  `CABAL/yaml/weapons.yaml`. All impact animations now use the default
  `explosion` image (engine default when `Image:` is omitted).
- [x] **CABAL impact animations moved to misc.yaml** (2026-07-15):
  `cabal_greenplasmaimpact`, `cabal_missileexplosion`,
  `cabal_laserimpact_s`, `cabal_laserimpact_m`, `cabal_laserimpact_l`,
  `cabal_dissolveimpact` moved from `CABAL/yaml/sequences.yaml` to
  `sequences/misc.yaml` under the `explosion:` key. Removed the old
  top-level definitions from the CABAL sequences file.
- [x] **Mod-wide CE-only Image: fixes** (2026-07-15): Moved CE-only
  image `wc2_building_collapse` under `explosion:` in misc.yaml; removed
  `Image:` from 7 CE warheads in `weapons/warcraft2.yaml`. Removed
  redundant `Image: explosion` from `weapons/halloween.yaml`. Shared
  images (used by both CE and other traits) keep their `Image:` field
  per the shared-image exception in DESIGN.md §8. `ra2corpse` reverted —
  corpse spawner needs `Image:` for random-pick from its own
  sub-sequences (corpse-spawner exception, DESIGN.md §8).
- [x] **DESIGN.md updated** (2026-07-15): Added rules to §8 documenting
  that `CreateEffect` must never carry `Image:` (CE-only), the
  shared-image exception, and that all impact animations must live in
  `misc.yaml` under `explosion:`.
- [x] **Audit tooling** (2026-07-15): `tools/audit_createeffect_image.py`
  flags all CE `Image:` fields; `tools/audit_ce_image_usage.py`
  classifies CE-only vs shared.
- [ ] **Future**: If a shared image's non-CE references are ever removed,
  it becomes CE-only and should be moved under `explosion:` at that time.

### CE3. Map actor renaming (delivery + deliverycoop)
- [x] **Actor rename in new maps** (2026-07-15): Commit
  `e6ad4ded5fa08c6b41fde63a256f2f5c15917241` added new maps
  (`delivery/map.yaml`, `deliverycoop/map.yaml`) with old compressed
  actor names. All 2257 actor references in both map.yaml files and 90
  string references in lua scripts renamed to new §1-compliant ids using
  `tools/rename_map_actors.py` with the `tools/rename/rename_map_*.yaml`
  mapping files. Terrain decorations (t01, v01, boxes01, brik, etc.) left
  as-is since they still exist with those names.
- [x] **DESIGN.md updated** (2026-07-15): Added §14 documenting map actor
  naming rules and the rename procedure.

---

## Dune factions (D2K) — split + naming + upgrades (P2)

- [x] **Split dune Light Infantry + Rocket Trooper per faction** (neutral
  base template → per-faction Ixian/Ordos actors) so upgrades apply
  separately (`b180aef36`).
- [x] **Ordos Light Infantry gets Laser Cartridges** once it's its own actor
  (`b180aef36`).
- [x] **Rename Ordos "Armor-Piercing Rounds" → "Rapid Fire Armor-Piercing
  Belts"** (actor id, template, condition, sequence, icon — full rename)
  (`b180aef36`).
- [x] No-hyphen naming scheme across all dune factions.
  Verified 2026-07-14: no hyphenated actor IDs, weapon IDs, or asset
  references in any D2k ContentPack yaml. All hyphens found are
  engine-defined conditions/sequence names (build-incomplete, damaged-idle,
  etc.) which are engine-owned and stay as-is per DESIGN.md §1.
- Note: 7 Ordos armor-rework files are the maintainer's live WIP — leave.

---

## Content-pack completion — TOP PRIORITY (ordered 2026-07-16)

_User order (verbatim intent): "Move everything to the new content packs
and verify that everything has been converted correctly! Try your best
reasoning to make sure every actor you move is in the right content
pack. It happened before that some ended up in the wrong section. Also
start moving all the necessary game files into the content packs as
well."_

- [ ] **PACK-RA1: Split RA1 (Allies / Soviets / Japan) out of
  rules/redalert.yaml** into ContentPacks/RedAlert/{Shared,Allies,
  Soviets,Japan} using tools/packs/split_faction.py. Shared concrete
  actors (`RAE1`, `RARE1`, shared `^RA*` templates) go to
  RedAlert/Shared. Verify: registry identity + resolved-closure diff
  empty + boot. NOTE: `RAE1` IS the Allied basic rifleman (user
  correction 2026-07-16) — legacy short ids like RAE1/RARE1 get their
  §1-compliant names during this split's rename step.
- [ ] **PACK-RA2: Split RA2 (Allies / Soviets / Yuri)** from
  rules/redalert2.yaml the same way.
- [x] **PACK-SC** (`4fe295183`): Terran/Zerg/Protoss split, registry-identical, boot-verified.
- [x] **PACK-WC2** (2026-07-17): Humans/Orcs split, registry-identical, boot-verified.
- [x] **PACK-TKM** (2026-07-17): split (ContentPacks/TKM/TKM), registry-identical, boot-verified.
- [ ] **PACK-OP2**: split the Outpost2 monolith (eden/plymouth, WIP factions) — last loaded monolith.
- [ ] **PACK-AUDIT (wrong-section detector)**: new
  `tools/audit/audit_packs.py` that verifies per pack: (a) every actor
  id carries the pack's faction prefix (catches actors landing in the
  wrong pack); (b) actors sit in the correct per-type file (trait
  heuristic: Building→buildings/defenses, Aircraft→aircraft, naval
  Locomotor→naval, husk→husks, upgrade/promotion markers→their files);
  (c) content.yaml lists exactly the yaml files on disk (no drift, no
  nonstandard filenames); (d) pack references resolve inside
  pack+Shared+core only. Run after every split.
- [ ] **PACK-ASSETS: per-faction asset migration** — repeat the CABAL
  pilot for every split pack (identify faction-unique files, move to
  files/{sprites,icons,voxels,sounds}, reference via package prefix,
  boot). Order: follow the pack splits; the four cross-game blockers
  (gunfire2, electro, dragon, DATA.R16) stay tracked above.
- [ ] **PACK-GEN (automatic maintenance)**: `tools/packs/gen_content.py`
  regenerates every pack's content.yaml deterministically from the
  files on disk (sorted, grouped Rules/Weapons/Sequences/FluentMessages);
  audit mode fails on drift. content.yaml becomes machine-maintained.

## Content-pack folder restructure (P2/P3, L)

- [x] Every content pack: `content.yaml` at root + one **`yaml`** folder
  (rules+weapons+sequences merged) + an empty **`files`** folder. Shared
  assets → per-GAME `Shared/files/`. DONE 2026-07-14: all packs
  restructured, boot-tested, committed.
- [x] **`mod.yaml` package hierarchy** (2026-07-15): per-faction
  `files/` packages are mounted first, then per-game `Shared/files/`,
  then top-level `ContentPacks/Shared/files/`, then legacy `bits/`. This
  lets new content shadow old content without breaking old cameo fallback.
- [x] **CABAL asset migration** (2026-07-15, `68cdd5ebb`/`472209150`): 128
  CABAL-unique assets moved into
  `ContentPacks/TiberianSun/CABAL/files/{icons,sprites,voxels}` and
  referenced with package prefixes.
- [x] **Cross-game shared asset migration** (2026-07-15, `e1b153d9c`/`472209150`):
  38 single-file cross-game shared assets moved into
  `ContentPacks/Shared/files/sprites/` and referenced with
  `shared_sprites|<name>` across all affected ContentPacks.
- [x] **TiberianSun intra-game shared asset migration** (2026-07-15,
  `6835a04`): 21 TS-only shared assets moved into
  `ContentPacks/TiberianSun/Shared/files/{icons,sprites,voxels}` and
  referenced with `ts_shared_*|<name>` prefixes.
- [ ] **Remaining critical cross-game shared assets**: `gunfire2`
  (generic/RA/TD variants), `electro` (7 tileset variants), `dragon`
  (RA sprite vs WC2 sound name collision), and `d2k/DATA.R16` (resource
  package). These must be resolved before `bits/` can be deprecated.
  → active work: cross-game sharing is a release blocker for dynamic
  faction loading, so this jumps the queue within the content-pack section.
- [ ] **AI module split**: per-faction `ai.yaml` is currently blocked by
  OpenRA's YAML merge behavior (trait instances with the same `@name`
  are replaced, not deep-merged). Needs custom trait or engine change.
  → backlog until architecture is designed.
- [ ] **Unused-file audit**: once all referenced assets are out of `bits/`,
  run an audit to identify and delete the ~25,000 unreferenced legacy
  files left in `bits/`.

## Cross-faction shared-effect independence (LONG-TERM, L)

- [x] Top-level `ContentPacks/Shared/files/` created as a temporary
  holding area for cross-game assets (2026-07-15).
- [ ] Duplicate or replace every cross-game shared asset so each game
  owns its own copy, then remove the top-level `Shared/files/` entries.
- [ ] Give each faction its own effects, or share only PER GAME. Prereq
  for true dynamic per-faction loading. DESIGN + MIGRATION.

---

## D2k wall+turret system expansion (LONG-TERM, L)

**Current state:** The D2k wall (`ContentPacks/D2k/Shared/yaml/buildings.yaml`)
already has `Replaceable: Types: Tower` and all D2k turrets (Ordos + Ixian)
have `Replacement: ReplaceableTypes: Tower`. This means turrets can be
built on top of wall segments, replacing them — the core D2k mechanic
works. The wall's `LineBuild` includes `turret` in `NodeTypes`, so walls
connect to turrets visually.

**Goal:** Expand this mechanic to all factions and add new turret-on-wall
types, creating a unified wall+turret defense system across the mod.

### Plan

1. **Audit existing wall+turret pairs** — identify which factions have
   walls with `Replaceable` and which turrets have `Replacement`. Currently
   only D2k (Ordos + Ixian) has this. TS Nod has laser fences but no
   turret replacement. TD/RA factions have plain walls with no replacement.

2. **Design faction-specific wall+turret pairs** — each faction that gets
   a concrete wall should also get a turret that can mount on wall
   segments. Examples:
   - TD GDI: Guard Tower → mountable on BRIK walls
   - TD Nod: Turret → mountable on BRIK walls
   - RA1 Allies: Gun Turret → mountable on BRIK walls
   - RA1 Soviets: Tesla Coil → mountable on BRIK walls (or a smaller turret)
   - TS GDI/Nod: Component Tower style → mountable on concrete walls
   - CABAL: unique turret type → mountable on concrete walls

3. **Add `Replaceable` to all wall actors** — add `Replaceable: Types: Tower`
   (or faction-specific type) to BRIK, SBAG, CYCL, FENC, BARB, and the
   RA2/D2k walls. Use multiple `Replaceable@` traits if a wall should
   accept multiple turret types.

4. **Add `Replacement` to turret actors** — add
   `Replacement: ReplaceableTypes: Tower` (or matching type) to each
   faction's base defense turret.

5. **Add `turret` to wall `LineBuild.NodeTypes`** — so walls visually
   connect to turrets. Currently only the D2k wall has this.

6. **Balance pass** — wall-mounted turrets should cost less than
   free-standing turrets but require the wall to exist first. This
   matches D2k's design where walls are cheap and turrets are expensive.

7. **Art pass** — ensure turret sprites align visually when placed on
   wall segments. May need wall+turret composite sprites for some factions.

8. **Future: D2k faction wall variants** — when Atreides and Harkonnen
   are added, give them faction-specific wall sprites (concrete wall
   variants per house color/style).

### Dependencies
- Must be done after barrier type assignment is finalized per faction.
- Requires balance workbook updates (new turret costs, wall costs).
- May need new C# traits if the existing `Replaceable`/`Replacement`
  system doesn't support all desired behaviors (e.g. conditional
  replacement based on tech level).

---

## Barrier & Wall Assignment Refactor (2026-07-17, COMPLETED)

**Goal:** Thematic and balanced barrier assignment across all factions.
Only classic TD and RA1 factions have dual wall types (light + heavy).
All other factions have a single, thematically appropriate wall type.

### Final Barrier Assignment Map

| Faction | Wall Type | Prerequisite Token |
|---------|-----------|-------------------|
| TD GDI | SBAG (sandbag) + BRIK (concrete) | `sandbagbarrier` + `concretebarrier` (^FACT) |
| TD Nod | CYCL (chainlink) + BRIK (concrete) | `chainlinkfence` + `concretebarrier` (^FACT) |
| RA1 Allies | SBAG (sandbag) + BRIK (concrete) | `sandbagbarrier` + `concretebarrier` (^RAFACT) |
| RA1 Soviets | FENC (wire) + BRIK (concrete) | `wirefence` + `concretebarrier` (^RAFACT) |
| RA1 Japan | CYCL (chainlink) + BRIK (concrete) | `chainlinkfence` + `concretebarrier` (^RAFACT) |
| RA2 Allies | ra2_awall | `ra2awall` |
| RA2 Soviets | ra2_swall | `ra2swall` |
| RA2 Yuri | ra2_ywall | `ra2ywall` |
| FutureTech | ra2_awall | `ra2awall` |
| Consortium | ra2_awall | `ra2awall` |
| Syndicate | ra2_swall | `ra2swall` |
| Naxis | ra2_swall | `ra2swall` |
| Schwarzer Mond | ra2_ywall | `ra2ywall` |
| Asian Alliance | asianalliance_concretebarrier | `asianalliancebarrier` |
| TS GDI | asianalliance_concretebarrier | `asianalliancebarrier` |
| TS Nod | ts_nod_laserfence | `tslaserfence` |
| CABAL | ts_nod_laserfence | `tslaserfence` |
| Forgotten | FENC (wire) | `wirefence` |
| TKM | FENC (wire) | `wirefence` |
| D2k Ordos | D2k wall | `d2k_construction_yard` |
| D2k Ixian | D2k wall | `d2k_construction_yard` |

### Changes Made
- Restored missing RA2 faction wall actors (`ra2_awall`, `ra2_swall`, `ra2_ywall`)
  with sprites and sequences from golden reference.
- Changed TD Nod from `wirefence` to `chainlinkfence` per maintainer request.
- Added shared `tslaserfence` prerequisite token so both TS Nod and CABAL
  can build the laser fence.
- Added shared `asianalliancebarrier` prerequisite token so both Asian
  Alliance and TS GDI can build the Asian Alliance concrete barrier.
- Removed `ra2fact` prerequisite from FutureTech (was enabling secondary
  `ra2brik` wall).
- Negated inherited `ra2awall` on RA2 Soviets and Yuri so they only get
  their own faction-specific wall.
- Removed `concretebarrier` and `sandbagbarrier` from all non-classic
  faction construction yards.

---

## Phase B — CABAL effects & art polish
- SP-recipe projectiles/contrails (art our own); dark-blue/purple identity;
  promotion icons for placeholders; SP-like reports from TS material.

## Phase C — Balance & consistency (other factions)
- Infantry offset sweep beyond TS; TS rocket launch-angle sweep beyond
  CABAL; clean workbook (port CABAL rows); 165 sheet↔game mismatches;
  [x] FutureTech .futu→futuretech_ rename — 32 asset files renamed, 8
  YAML/FTL files updated (voxels, sequences, ContentPack rules, Fluent).
  Soviet Gorynych/Stalin Fist.

## Phase D — SP-ification of the other TS factions (after CABAL)
- TS GDI, Nod, Forgotten, then Scrin — SP-recipe weapons/effects, workbook stats.

## Phase E — Platform & engine (background, L)
- [x] **Port `AttackGarrisonedSP`** (one fire port per passenger) + convert all
  `AttackGarrisoned`/`AttackOpenTopped` units to per-passenger independent
  targeting. New `AttackGarrisonedSP` trait in `OpenRA.Mods.CA/Traits/Attack/`
  inherits `AttackFollow`, supports both `Cargo`/`Passengers` and
  `Garrisonable`/`Garrisoners`, and adds per-passenger opportunity fire via
  each passenger's `AutoTarget` trait. All 26 YAML usages across rules +
  ContentPacks converted from `AttackGarrisoned`/`AttackOpenTopped` to
  `AttackGarrisonedSP`. `PortYaws`/`PortCones` made optional (default 360°).
  **REVERTED** (`cfa117c78`): AttackGarrisonedSP caused a major regression —
  garrisoned passengers could no longer independently auto-target because
  passenger AutoTarget traits don't function while inside cargo. All 56 YAML
  trait renames reverted to vanilla `AttackGarrisoned`/`AttackOpenTopped`.
  The C# source file is kept for future reference but unreferenced.
- SP engine-trait ports; TS Shared pack move; Formula v2; dynamic faction
  loading end-game (per-pack ai.yaml, assets into packs, unused-file audit).

---

## Standing rules recorded (see DESIGN.md / memory)

- **CreateEffect Image: field** (DESIGN §8, 2026-07-15): a weapon
  `CreateEffect` must NEVER carry an `Image:` field — omit it and the
  engine defaults to the `explosion` image in `misc.yaml`. All impact
  animations live as sub-sequences under `explosion:` in
  `sequences/misc.yaml`, never in faction sequence files.
- **Map actor naming** (DESIGN §14, 2026-07-15): maps must use renamed
  actor ids, not old compressed names. Rename maps in
  `tools/rename/rename_map_*.yaml` are the source of truth. Lua scripts
  must also be updated. Tool: `tools/rename_map_actors.py`.
- **No weapon inheritance between units** (DESIGN §15, reinforced
  2026-07-15): unit-unique weapons must never `Inherits:` from another
  unit's weapon. Copy stats or use a shared `^`-prefixed template. This
  was the root cause of the CreateEffect crash class.
- **CABAL Avatar = 50% Core Defender** (DESIGN §15, 2026-07-15): the
  avatar is a 50%-scaled copy of the Core Defender, not a spider.
- **CABAL husk recovery** (DESIGN §15, 2026-07-15): backup husks are
  immobile, high-HP, repairable, auto-reanimate via
  GrantPeriodicCondition + TransformOnCondition.
- **Effect + sound are always defined together** (DESIGN §8): every new
  impact/projectile effect gets BOTH a new effect sprite AND a new Report/
  ImpactSound — never fall back to the template's default for either.
  Unique-per-faction is the goal.
- **Effect frame-fit**: every rendered effect must sit INSIDE its frame
  (2px border alpha 0) or it clips to a square. Verify with a bordered
  preview. (memory: cameo-custom-effects-pngsheet)
- **Laser beams (DESIGN §3)**: two colors (inner+outer), width AND color
  scale with damage; CABAL = purple + dark blue, never too thin.
- **Obelisk/laser sound map (DESIGN §3)**: obelmod1.aud = TS Obelisk of
  Light / Obelisk of Darkness / CABAL Obelisk; obelcor3.aud = Core
  Defender + DarkObeliskLaser + Commando plasma; obelray1.aud = Tiberian
  DAWN obelisk — NOT allowed on TS units unless specified (SP `^LaserWeapon`
  inherit = the TD version); smaller lasers = lastur1.aud turret sounds.
- **Effect-warhead naming**: one `CreateEffect` per impact surface.
- **Per-frame randomness** on new animated effects.
- **Content-pack structure**: yaml folder + files folder + content.yaml.

### Backlog — Rank decorations & elite weapons (DESIGN §16, 2026-07-15)

- [x] **Fix TS Nod rank decoration** — 13 TS Nod actors were using
  `^GDIRankDecoration` instead of `^NodRankDecoration`. FIXED in this
  session. Also fixed 4 TS Forgotten actors in `defenses.yaml` and 2
  core `tiberiansun.yaml` Nod units (`ts_nod_attackcycle`,
  `ts_nod_ticktank`).
- [x] **Wire D2k factions to `^DuneRankDecoration`** (`5ff288c5c`) — Added
  `Inherits@decoration: ^DuneRankDecoration` to 64 D2k actors across Ixian,
  Ordos, Harkonnen, and Shared yaml files. Audit tool:
  `tools/audit/audit_dune_rank_decoration.py` (0 remaining).
- [x] **Create `^AlienRankDecoration` template** (`b95f5e7f3`) — Created
  template in `rules/starcraft.yaml` using existing `alienrank` sequence
  from `misc.yaml`. Wired to 79 StarCraft actors (Terran, Protoss, Zerg)
  that use `^GainsExperienceTD`. Warcraft2 actors still need a custom
  `wc2rank` image (no sequence exists yet — out of scope).
  **NOTE (2026-07-16):** This commit incorrectly applied `^AlienRankDecoration`
  to ALL Starcraft factions. It should only apply to Zerg. Terran and
  Protoss need separate decorations. See SC-RANKS below for the fix plan.
- [ ] **Create per-faction rank decorations for RA2Mod factions** —
  currently all RA2Mod factions share `ra2rank` via
  `^GainsExperienceRA2`. Eventually each could have a unique rank image
  for faction identity (low priority — shared `ra2rank` is functional).
- [x] **Write `audit_rank_decoration.py`** (`10220c0ee`) — verifies every
  `^GainsExperienceTD` actor has the correct `^*RankDecoration` for its
  faction, verifies `^GainsExperienceRA2` actors do NOT have a separate
  decoration, and checks that rank image sequences exist in `misc.yaml`.
  Current state: 135 issues (mostly SC/WC2/RA2Mod factions that share
  `ra2rank` or lack faction-specific decorations — low priority).
- [ ] **E1: Add missing elite weapons** — Audit (`tools/audit/audit_missing_elite.py`,
  `4d0e8ec85`) found **1256** buildable actors with `GainsExperience` but no
  `Armament@*ELITE*` block. Top factions: rules/redalert (100), rules/starcraft
  (79), rules/wh40k (75), rules/darkreign (68), rules/shockwave (67),
  rules/generals (55), rules/advancewars (52), rules/starwars (45),
  rules/redalert2 (41), TS/Forgotten (37), rules/tkm (36), TS/CABAL (34).
  This is a large multi-session design effort — each elite weapon needs unique
  stats, not a mechanical rename. Needs user direction on scope/priority.
  **NOTE (2026-07-16):** The audit script was updated to only flag
  `^GainsExperienceRA2` actors (per DESIGN.md §16.3 "RA2 system only").
  The count of 1256 was from the old scope — re-run the audit for the
  current RA2-only count. TD/D2k/SC/WC2 actors no longer flagged.
- [x] **E2: Fix missing `rank-elite` conditions** (`ac3ba04b7`) — Only 2
  genuine bugs found (out of 18 flagged; rest use Generals `scrap_create_bonus`
  rank system or upgrade-switch naming). Fixed:
  `asianalliance_plasmatrooper` GARRISONEDELITE and
  `asianalliance_heavyrailguntank` ELITE. Added audit tool
  `tools/audit/audit_elite_gating.py`.
- [x] **E3: Normalize elite weapon naming** (`ab870ddb3`) — Renamed 10
  non-standard elite weapons to `<base>E` convention (38 references across
  12 files): `NaxPlanegun`→`NaxPlanegunE`, `NaxPlaneRockets`→`NaxPlaneRocketsE`,
  `NaxiWW2MachinegunnerElite`→`NaxiWW2MachinegunnerE`, `NaxiBeetleLaser`→`NaxiBeetleLaserE`,
  `NaxiBeetleLaserAA`→`NaxiBeetleLaserAAE`, `NaxCorrosionRocketTrooper`→`NaxCorrosionRocketTrooperE`,
  `TSBikeMissileNashwaElite`→`TSBikeMissileNashwaE`, `V3LaunchElite`→`V3LaunchE`,
  `RA2KirovBomb_nuclear_Elite`→`RA2KirovBomb_nuclear_E`, `CuteKirovBombElite`→`CuteKirovBombE`.
  Remaining 44 are doctrine variants (`_rad`/`_fire`/`_tesla`), upgrade combos,
  or gatling spin-ups — intentionally non-standard. Audit tool:
  `tools/audit/audit_weapon_suffixes.py` (X1 section).
  **NOTE (2026-07-16):** The `E` suffix convention has been superseded —
  ALL elite weapons must now use `_elite` per DESIGN.md §16.3. The renames
  done here will need to be re-done as `<base>_elite` in WEAPON-SUFFIX-ELITE.
- [x] **E4: Verify base weapon gating** (`ac3ba04b7`) — Fixed the 2 actors
  from E2: added `RequiresCondition: !rank-elite` to
  `asianalliance_heavyrailguntank` PRIMARY and
  `asianalliance_plasmatrooper` GARRISONED so elite replaces, not stacks.

## D2K Sprite Conversion Pipeline

- [x] **D2K-CONV: Conversion script** — `tools/d2k_to_openra.py` written
  and documented in DESIGN.md §17. Combines BMP frames → PNG spritesheet,
  pink→transparent, hue-shift green player color to target hue, embeds
  FrameAmount/FrameSize PNG metadata for OpenRA.
- [x] **D2K-KODA: Koda Tank** — replaced `combat_tank.ixian` with
  `ixian_koda_tank` using new PNG spritesheets (chassis + turret).
  Updated all references in Ixian/Ordos faction.yaml, upgrades.yaml,
  ai.yaml. Muzzle flash still uses DATA.R16. Pending in-game visual
  confirmation.
- [ ] **D2K-CONV-FUTURE: Convert more D2K units** — other D2K units that
  could benefit from custom PNG sprites instead of DATA.R16 remapping.
  Use the same script with appropriate `--hue` per faction.

## Schwarzer Mond Faction Design & Upgrades

- [x] **SM-RESEARCH: Finalize promotion intent** — promotions will upgrade
  existing units via `^PromotionUnitBuff` rather than unlocking new actor
  variants. The `Bradley` unit in the promotion image is resolved as the MARS
  hover artillery (`schwarzer_mond_mars`). Added the buff to all combat
  infantry, vehicles, and aircraft. Updated DESIGN.md §18.7 / §18.11.
- [x] **SM-UPGRADE-1: Add upgrade templates** — create `^NaxiCryptofascism`,
  `^NaxiLunarAlloys`, `^NaxiMoonPropaganda` in the appropriate Shared or
  Schwarzer Mond templates file. Update DESIGN.md §18.6 if the template set
  changes.
- [x] **SM-UPGRADE-2: Split laser upgrade** — turn Crystal Lens into a +1-burst
  radar-tier upgrade for all yellow laser weapons; add Amplified Lens as the
  tech-tier +1-burst upgrade for all yellow laser weapons. Update all weapon
  variants and actor armament conditions per DESIGN.md §18.4.
- [x] **SM-UPGRADE-3: Move cannon upgrade to tech tier / rename to Vril Powered
  Weapons** — change `schwarzer_mond_upgrade_vrilpoweredweapons` prerequisite
  from radar to `~schwarzer_mond_techcenter`, keep it in the `Research` queue,
  and rename the display name/template/icon from Green Plasma Shells to Vril
  Powered Weapons.
- [x] **SM-UPGRADE-4: Add Cryptofascism upgrade** — create
  `schwarzer_mond_upgrade_cryptofascism` (tech tier, Research queue) with
  `CashTrickler` 1 credit per 25 ticks per unit. Add icon sequence for
  `nax2_cryptofascismicon.png` in `mods/cameo/bits/ra2/mod/`. Inherit on
  every Schwarzer Mond actor.
- [x] **SM-UPGRADE-5: Wire upgrades to every unit** — ensure every Schwarzer
  Mond actor has at least two relevant upgrade hooks (Cryptofascism + either
  Lunar Alloys, Crystal Lens, Vril Powered Weapons, Moon Propaganda, or
  Helium-3). Do not change unit stats without a spreadsheet pass.
- [x] **SM-DESC: Normalize faction and unit descriptions** — rewrite the
  Schwarzer Mond `faction_ra2_lnaxis` description in the point-based format
  (Difficulty, Early/Mid/Late Game, Playstyle, etc.) and add/update unit
  descriptions for new upgrades. Normalize other RA2Mod factions when touched.
- [x] **SM-LORE: Add Iron Sky / Nazi Moon lore** — document Vril, Helium-3,
  MoonCoin/Reichsmark 2.0 parody in DESIGN.md §18.12 and update upgrade names
  and descriptions to match.
- [x] **SM-HELIUM3: Add Helium-3 Enrichment upgrade** — create
  `schwarzer_mond_upgrade_helium3` (radar tier, Upgrades queue) that increases
  Hydrogen Plant power output by 50% and vehicle/aircraft speed by 25%. Add
  template, icon, and sequence; wire to all vehicles and aircraft.
- [x] **SM-VRILINFUSION: Add Vril Infusion upgrade** — create
  `schwarzer_mond_upgrade_vrilinfusion` (tech tier, Research queue) that gives
  all Schwarzer Mond infantry +25% firepower, +25% speed/turn rate, and 15%
  damage reduction. Add template, icon, sequence, and wire to every infantry
  actor. Update descriptions and intent.
- [x] **SM-1BURST: Re-enable laser upgrades on 1-burst weapons** — add Lunar
  Soldier and Laser Tower to the Crystal Lens / Amplified Lens switch and
  recreate the 1-burst yellow/amplified weapon variants.
- [x] **SM-AUDIT: Run audit suite and rebuild** — audit suite run
  2026-07-15. Schwarzer Mond upgrades: cryptofascism 26/27, lunaralloys
  26/27, moonpropaganda 5/5, vrilinfusion 5/5 (only uncovered: tsprobe
  shared unit). No orphaned SM actors/weapons. No faction leaks. Game
  boots to menu clean.
- [ ] **SM-BALANCE: Spreadsheet pass** — if any base stats change (e.g.
  raising base burst of Lunar Soldier or Laser Tower), update
  `docs/design/cameo_armor_system.xlsx` and the yaml in the same pass.
  Queue if the Excel lock file is present.
- [x] **SM-ARTWORK: Replace copy-pasted icons** — create unique placeholder
  icons for `schwarzer_mond_mars`, `schwarzer_mond_m200bjagerline`,
  `schwarzer_mond_gravitycoretank`, and `schwarzer_mond_blackbomb`. See
  `docs/design/schwarzer_mond_artwork_status.md` for the full status. Final
  production-quality cameo art can replace the placeholders later.

## Sequence Filename Standardization

- [ ] **SEQ-RESEARCH: Cross-reference audit** — build a complete map of
  which sequence filenames are used by which actors across all sequence
  YAML files. Identify:
  (a) files used by only one actor (safe to rename),
  (b) files shared across multiple actors (MUST NOT be renamed),
  (c) files in shared namespaces (`shared_sprites|`, `ts_shared_sprites|`,
      `td_shared_sprites|` — never renamed),
  (d) template default filenames in inherited `^` templates (never renamed),
  (e) death/muzzle/parachute files defined in templates (never renamed).
  Output: `tools/audit/sequence_file_crossref.json`.
  Effort: M.
- [ ] **SEQ-MIGRATE: Rename sequence files to match actor + sequence name**
  — per faction, rename actor-owned files so that:
  (a) the idle/body sprite is `<actor_id>.<ext>` and moved to `Defaults:`,
  (b) non-idle sequences use `<actor_id>_<sequence_name>.<ext>` (e.g.,
      `_bib`, `_make`, `_turret`, `_icon`, `_muzzle`, `_active`, `_dead`,
      `_damaged`, `_deploy`, etc.),
  (c) shared files are left untouched,
  (d) Combine sub-images unique to one actor are renamed to
      `<actor_id>_<descriptive_suffix>.<ext>`,
  (e) inherited template defaults are left untouched.
  Use `tools/rename/rename_map_<faction>.yaml` + `tools/rename/safe_rename.py`.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Update `.oramap` files with `tools/fix-oramap.ps1` if needed.
  Effort: L (multi-session, ~18,500 asset files across all factions).
  **Risk assessment**: HIGH — missing a reference causes a crash. Must
  be done one faction at a time with boot tests between each. Shared
  file detection is the critical safety gate. See DESIGN.md §1
  "Sequence filenames must match their actor and sequence name".

- [ ] **WPN-MIGRATE: Rename weapons to include full actor id prefix**
  — per faction, rename actor-specific weapons from PascalCase to
  `<actor_id>_<weapon_descriptive_name>` (e.g., `CabalTarantulaCannon` →
  `cabal_tarantula_cannon`, `RA2KirovBomb` → `ra2_soviets_kirov_bomb`).
  Weapon class templates (`^SmallArms`, `^MediumCannon`, etc.) and
  faction-level templates (`^CabalMissile`, `^RA2RadShell`) keep their
  PascalCase `^` names. Elite variants append `_elite`, EMP variants
  append `_EMP`, AA variants append `_AA`, upgraded variants append
  `_upgraded`. Weapons shared across factions (in Shared/ packs) stay as-is.
  Use `tools/rename/rename_map_<faction>.yaml` + `tools/rename/safe_rename.py`.
  Verify with `tools/audit/dump_resolved.py` before/after diffs (empty).
  Effort: L (multi-session). See DESIGN.md §1 "Weapon names must include
  the full actor id as a prefix".

## Faction Internal Name & Inherits Consistency

- [x] **FACTION-RENAME: Rename faction internal names for consistency**
  — DONE 2026-07-16. Renamed 11 faction InternalNames to match actor
  prefixes, plus WC2 actor prefix rename. All YAML, Python, MD, AI files
  and asset files updated:
  - `gdi` → `td_gdi`, `nod` → `td_nod` (TD factions)
  - `allies` → `ra1_allies`, `soviets` → `ra1_soviets` (RA1 factions)
  - `ra2allies` → `ra2_allies`, `ra2soviets` → `ra2_soviets` (RA2 factions)
  - `tsgdi` → `ts_gdi`, `tsnod` → `ts_nod` (TS factions)
  - `consortium` → `steelconsortium`, `syndicate` → `latinsyndicate` (RA2 mod)
  - `warcraft_humans` → `wc2_humans`, `warcraft_orcs` → `wc2_orcs` (WC2 factions + actors)
  - `asian_alliance` → `asianalliance` (fixed underscore-in-faction-name violation)
  - Already consistent: `schwarzermond`, `naxis`, `futuretech`, `japan`, `yuri`,
    `forgotten`, `cabal`, `terran`, `zerg`, `protoss`, `tkm`, etc.
  - Verified by `audit_consistency_report.py` checks C6-C11 (73 checks, 0 failures).
  - Remaining: `.oramap` map files may need `tools/fix-oramap.ps1` update.
  - Remaining: WC1 factions (`human` → `wc1human`, `orc` → `wc1orc`) not yet done.

- [x] **INHERITS-PASCAL: Convert camelCase/snake_case inherits to PascalCase**
  — all inherits templates converted to PascalCase per DESIGN.md §1.
  Commit `3f5c53915` (WC2/WC1, 301 replacements/20 files): WC2 Humans
  (`^wc2_h_*`/`^wc2_humans_*` → `^WC2Humans*`), WC2 Orcs
  (`^wc2_o_*`/`^wc2_orcs_*` → `^WC2Orcs*`), WC2 shared (`^wc2_*` → `^WC2*`),
  WC1 Humans (`^wc_h_*` → `^WCHumans*`). Full faction names used (no
  single-letter abbreviations).
  Commit `cf0e4485d` (all remaining, ~2158 replacements/126 files): RA2
  Soviets camelCase, CABAL snake_case, Outpost 2/SOW, TKM, USA, RA1 Allies,
  D2K, generic templates (`^wall`→`^Wall`, `^refinery`→`^Refinery`, etc.),
  and Sidebar faction name capitalization. D2K-specific `^Refinery` renamed
  to `^D2KRefinery` to avoid collision with base `^Refinery`. Boot-gate
  clean (zero "Parent type not found" errors).

## Starcraft Rank Decoration Fix

- [x] **SC-RANKS: Split alien rank decoration per Starcraft faction**
  — FIXED: commit `c3e3490f7` reverted the blanket `^AlienRankDecoration`,
  commit `031c54d6b` created 3 separate decorations (`^ZergRankDecoration`
  with `alienrank`, `^TerranRankDecoration` with `terranrank`,
  `^ProtossRankDecoration` with `protossrank`). All use `alienranks.png`
  as placeholder. 7 actors missing faction decorations fixed and
  `audit_rank_decoration.py` reports 0 StarCraft issues.

## Weapon Suffix Standardization

- [x] **WEAPON-SUFFIX-ELITE: Migrate legacy E suffix to _elite**
  — DONE 2026-07-30: Renamed 117 elite-gated weapons from `<base>E` to
  `<base>_elite` across 44 files (339 lines changed) via
  `tools/rename_elite_weapons.py`. Handles compound suffixes: `AAE`→`AA_elite`,
  `EMPE`→`EMP_elite`, `EResonance`→`_eliteResonance` + bounce variants.
  Skipped `MigMissiles_AA_ELITE` (already contains ELITE) and 45 doctrine
  variants (`_rad`/`_fire`/`_tesla`), upgrade combos, and gatling spin-ups
  that are intentionally non-standard. Boot-gated: menu reached, 0 new
  exception logs. Audit: X1 count dropped from 112+ to 45 (intentional
  non-standard remnants).
  **Follow-up** (2026-07-31): Renamed 17 remaining deprecated `E`-suffix
  weapons missed by the first pass (33 replacements across 15 files) via
  `tools/archive/rename_elite_E_suffix.py` (one-time, archived). X4 dropped 19→2 (only `HE` =
  High Explosive false positives remain). Boot-gated, O2=0, V3=0.

- [x] **WEAPON-SUFFIX-EMP: Standardize EMP weapon names to _EMP suffix**
  — DONE 2026-07-31: Renamed 62 EMP weapons across 44 files (179 lines
  changed) via `tools/rename_emp_weapons.py`. Handles: EMP suffix ->
  _EMP, mid-name EMP -> _EMP_, compound EMPAA -> _EMP_AA, EMPulse ->
  _EMP_Pulse, ArcTeslaFragment sub-variants. Case-insensitive global
  replacement catches Weapon:, Weapons: list entries, and Inherits:
  references. Skipped DREMPDeviceSound (audio VoiceSet) and EMPGrenade
  (EMP is prefix). Boot-gated: menu reached, 0 new exception logs.

- [x] **WEAPON-SUFFIX-AA: Standardize anti-air weapon names to _AA suffix**
  (2026-07-31) — per corrected DESIGN.md §1 rule: `_AA` marks the
  air-only sibling of a **dual-weapon actor** (an actor/template that
  equips two separate weapons via different `Armament` traits — one
  ground-capable, one air-only — e.g. an Anti-Air Tank). Standalone
  AA-only weapons with no ground-capable sibling on the same actor (SAM
  Sites, etc.) are intentionally excluded, as are single weapons whose
  own `ValidTargets` already covers both Ground and Air.
  **Renamed 111 weapons** via `tools/rename_aa_weapons.py`
  (structurally-scoped exact-token replacement — top-level def key,
  `*Weapon:`/`*Weapons:` fields, indexed superweapon lists,
  block-context-gated `Inherits:` — never a blind substring match).
  Verified: `audit_weapon_suffixes.py` X3 = 0, `audit_orphans.py`
  dangling weapon refs = 0, `audit_inherits.py` V3 dangling = 0, rename
  script is idempotent (second run finds nothing to do).
  **Bugs found/fixed during this task** (see `docs/LESSONS_LEARNED.md`):
  an earlier draft of the rename script did a blind file-wide
  word-boundary substitution of bare weapon names, corrupting unrelated
  Tooltip/Name/RequiresCondition/Prerequisite text and comments across
  ~30 files (reverted via `git reset --hard` before merge, never
  pushed); `AA_LEGACY_KEYWORDS` including bare `"aa"` silently excluded
  every weapon that already contained "AA" without an underscore;
  identical names reused across actor/weapon/sequence namespaces (e.g.
  `sow_mech_avenger`, `d2k_aircraft_eater`) required block-type gating
  before renaming `Inherits:` or top-level definition keys; many
  weapons declare `ValidTargets` only on a `^Template` ancestor,
  requiring Inherits-chain resolution.
  Effort: M. See DESIGN.md §1.

## Long-term goals

- [ ] **ZERO YAML ERRORS & WARNINGS** — achieve zero errors and zero warnings
  from `utility.cmd cameo --check-yaml`. Latest report: 2026-07-24
  (check_yaml_v8.txt, ~89,392 errors, ~69,325 warnings).
  Full phased plan in `docs/design/MEGAPLAN_YAML_CLEANUP.md`.
  Analysis tool: `tools/audit/analyze_check_yaml.py`. Effort: L (multi-session).

  **Fixes applied this session (2026-07-24):**
  - [x] LaunchAngle (363→0): Converted LaunchAngle↔Min/MaxLaunchAngle per
    projectile type; removed LaunchAngle from WarheadTrailProjectileCA; added
    missing MaximumLaunchAngle where Min>Max.
  - [x] UndefinedCursor chrono-target (195→0): Added `chrono-target` cursor
    sequence alias in cursors.yaml (hyphen variant of `chrono_target`).
  - [x] NegativeRemoval (64→0): Stripped values from `-Trait: value` removal
    lines across 15 weapon YAML files.
  - [x] InvalidWeaponField (55→0): Removed `WeaponClass` (40 lines, deprecated);
    fixed `Burstdelays`→`BurstDelays` (9); `BurstDelay`→`BurstDelays` (4);
    `Angle`→`LaunchAngle` on Bullet (1); removed weapon-level `ValidStances` (4);
    `ChangeOwnerValidStances`→`ValidStances` (2).
  - [x] DuplicateInteractable (234→0): Added `-Selectable:` to all bridge actors
    to remove inherited Selectable (which includes InteractableInfo), keeping
    only the explicit `Interactable:` with custom Bounds.
  - [x] MissingTooltip (39→0): Added `Tooltip` trait to `camera.gpssat`.
  - [x] OverrideActor on Tooltip (2→0): Removed invalid `OverrideActor` field
    from Tooltip traits in TD GDI vehicles and TD Shared aircraft.
  - [x] ProductionCost/TimeMultiplier RequiresCondition (10→0): Converted
    `RequiresCondition`→`Prerequisites` on ProductionCostMultiplier and
    ProductionTimeMultiplier in ^ScaledProducer template and 9 other instances.
    These traits use `Prerequisites:` not `RequiresCondition:`.
  - [x] ValidStances on AutoTargetPriority (3→0): Removed invalid `ValidStances`
    fields from AutoTargetPriority traits in outpost2.yaml.
  - [x] BadIndent (39): Investigated chrome/lobby_music.yaml — no actual
    indentation issues found. Likely false positive from engine miniyaml parser.

  **Error breakdown (2026-07-24, post-fixes):**
  - 72,813 UngrantedConditions — actors consume conditions not granted (biggest)
  - ~700 InvalidField — trait fields that don't exist on their trait (reduced
    from 761 after OverrideActor, ValidStances, RequiresCondition fixes)
  - 209 MissingSequences — images with no sequence definitions
  - 39 UndefinedNotification — missing notification references
  - 12 CannotParse — Cannot parse `Random` into LockFaction.Boolean
  - 11 UndefinedActor — husk actors not defined by any rule
  - 9 InvalidOwner — map actors with wrong owner
  - 4 InvalidChildNodes — traits with invalid child nodes
  - 2 MissingPrereq — buildable actors with unprovided prerequisites
  - 2 UnknownTrait — unknown traits in player.yaml
  - 1 MissingFluentVariable — missing fluent variable

  **Warning breakdown (2026-07-24):**
  - 62,640 UnconsumedConditions — actors grant conditions not consumed (biggest)
  - 375 UnusedFluentAttribute — unused fluent attributes in en.ftl files
  - 1 UnusedFluentVariable — unused fluent variable

  Phases: (1) palette fixes, (2) Interactable/Selectable conflicts, (3) missing
  FTL keys, (4) missing actor definitions [biggest], (5) unresolved prerequisites,
  (6) unused granted conditions [biggest warnings], (7) VisibilityType.Footprint,
  (8) invalid map factions, (9) MuzzleSequence/LaunchAngle/misc, (10) sequence
  warnings, (11) unused field/trait.

  **NOTE:** `utility.cmd cameo --check-yaml` takes 10+ minutes. Only run it
  after completing ALL connected fixes and expecting 0 errors/warnings. Do NOT
  run it repeatedly. Keep findings above updated in this section.

---

## Superweapon Documentation Audit (2026-07-25, COMPLETED)

Full cross-reference of all superweapon and support power YAML traits vs
`FACTIONS.md`. Raw data: `docs/audit/latest/superweapon_audit.yaml`.
Summary: `docs/audit/SUMMARY.md` § "Superweapon documentation audit".

**14 findings** — all FACTIONS.md discrepancies FIXED:
- SW-001 (HIGH): Harkonnen Palace has `^PrimarySuperweapon` but no power trait (parked faction, not a regression)
- SW-002 (MED): Forgotten superweapon corrected from "Tiberian Wildlife Rampage" to "Nuclear Missile"
- SW-003 (MED): CABAL corrected — added Nuclear Missile, removed unimplemented "Satellite Hack"
- SW-004–011 (LOW): Added missing support powers (Cluster Missile, Chrono Reinforcements, Force Shield, EMP Disable, Traitors, Slow, Invisibility, Bloodlust, Haste) + fixed name mismatches (Meteor Blitzkrieg, Chaos Storm)
- SW-012–014 (INFO): Added Drop Pods, Federation Support Teleport to reference table; noted Protoss reuses SteelIonCannon

**WIP factions discovered** (not in FACTIONS.md): Warzone 2100, Worms, Win98, Warcraft 1, WH40K all have superweapon traits in rules/ YAML. Document when factions become active.
