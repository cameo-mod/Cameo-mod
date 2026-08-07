# AI Handoff — 2026-08-05: Weapon 3-way split + AreaDamage + Balance pipeline

> **Scope:** This is a continuation handoff for the next AI agent. Read it top-to-bottom before touching weapons, balance, or warhead templates. Companion docs: `AREADAMAGE_HANDOFF.md`, `WEAPON_3WAY_SPLIT.md`, `ROADMAP.md`, `BALANCE_PIPELINE.md`, `AGENT_WORKSPACE.md`.

---

## 1. TL;DR — state at a glance

- **No P0 crashes.** Empty-warhead audit reports `0`; game reaches `MenuPostProcessEffect.PostWorldLoaded` with no new `exception-*.log`.
- **AreaDamage universal conversion is done in the engine and templates.** The C# `AreaDamageWarhead` and `AreaDamagePercentageWarhead` are built and deployed (`mods/cameo/OpenRA.Mods.Cameo.dll` + `engine/bin`). All 54 non-Nuclear shared `^Warhead_*` templates are `AreaDamage` with baked `FriendlyFireDamage: 50` / `FriendlyFireSpread: 50`.
- **New template libraries exist and are boot-gated:** 55 warhead, 24 projectile, 27 effect families in `mods/cameo/weapons/weapons.yaml`.
- **Weapon 3-way split is ~80 % mechanical; the remainder is bespoke.** Single-inherit families and several clean dual-inherit clusters are retrofitted. About **609 mixed weapons** still need maintainer-directed collapse to ≤2 warheads (or documented exception).
- **Balance pipeline is structurally ready but blocked on two things:** (1) maintainer confirmation of the vehicle anchor table, and (2) completion of the weapon 3-way split / A1 generator reconcile so DPS/range numbers are stable.
- **This session made mistakes and fixed them.** I temporarily reverted `SpreadDamage` into new-family nodes because I missed the `AREADAMAGE_HANDOFF.md` at startup; that was corrected in `6364775bb`.

---

## 2. What the previous AI (Claude/Opus) completed

| Commit (examples) | What was done |
|---|---|
| `851537a03`, `1b638bf28` | C# `AreaDamageWarhead` + `AreaDamagePercentageWarhead` built and deployed. |
| `3dac92ee8` | `sweep_areadamage.py` — 559 weapon main overrides stripped from `SpreadDamage` to bare, 156 `_FriendlyFire` twins deleted, 11 stale `ValidRelationships` removed. |
| `b2fbc372f` | 54 shared `^Warhead_*` templates flipped to `AreaDamage` with baked universal FF. |
| `48245737e` | `tools/balance/gen_weapon_template.py` reconciled to emit `AreaDamage`, baked FF, `^Warhead_<Family>_<Level>` naming, `Warhead@<tag>_Percentage`. `verify_generator_sync.py` added. |
| `956cf1ecb`, `fa0947ae5` | 55 warhead templates, 24 projectile, 27 effect templates spliced + boot-gated. |
| `697595cdc` | `sweep_areadamage.py` applied to main warhead overrides. |
| `65b14006f` | `extract_stats.py` now tolerates >2 warhead mixes for `illegal_mix` audit. |

**Key files to treat as authoritative:**
- `docs/design/AREADAMAGE_HANDOFF.md`
- `docs/design/WEAPON_3WAY_SPLIT.md`
- `tools/balance/sweep_areadamage.py`
- `tools/balance/verify_generator_sync.py`
- `tools/balance/gen_weapon_template.py`

---

## 3. What this Devin session accomplished (and corrected)

Commits in order:

1. `e6a4024ca` — `docs/LESSONS_LEARNED.md` expanded; `extract_stats.py` `_MIX_ALLOWLIST` fixed for CABAL 4-warhead combos; `SmallArms+Chaingun` dual-tier demo converted (24 weapons). **Mistake here:** `Warhead@Bullet_*: SpreadDamage` was restated on converted weapons; new templates are `AreaDamage`, so bare would have been correct, but the old key `SpreadDamage` was carried forward.
2. `d0acad576` — `HeavyBomb+ShrapnelWeapon` (5 weapons) retrofitted to `Concussion_Medium` + `Demolition_Heavy`; `RashinanGun` renamed to `RashidanGun`; attempted to correct the bullet `SpreadDamage` issue by stripping and re-adding types. **Mistake:** I re-added `SpreadDamage` to 52 nodes that did not inherit `^Warhead_Bullet_*` instead of `AreaDamage`.
3. `d77dff2db` — `LightMissile+MediumMissile` (5 weapons) retrofitted to `MissileHE_Light` + `MissileHE_Medium`. Correctly used bare/AreaDamage.
4. `6364775bb` — Corrected the `SpreadDamage` regressions: 191 new-family main warheads changed to `AreaDamage` (or bare if the matching `^Warhead_*` is inherited).

**Mistakes learned:**
- I did not load `AREADAMAGE_HANDOFF.md` before starting; the universal `AreaDamage` state was not in my head.
- I wrote a `restore_bullet_types.py` that set `SpreadDamage` on nodes that had no `^Warhead_Bullet_*` parent, causing the user to have to clean up manually. The correct temporary fix is `AreaDamage`; the correct structural fix is to inherit the right `^Warhead_*` template.
- `find_empty_warhead.py` passed because `SpreadDamage` is a valid type; the issue was wrong design state, not an NRE.

---

## 4. Current repository state

- **Branch:** `master`
- **Last commit:** `6364775bb Fix: new-family main warheads must be AreaDamage, not SpreadDamage (AREADAMAGE_HANDOFF)`
- **Working tree:** clean except two untracked helper scripts:
  - `tools/audit/audit_damage_grid.py`
  - `tools/balance/_requantize_ledgers.py`
  These are from prior sessions; do not commit unless you know their purpose.
- **Boot-gate status:** passes.
- **Empty-warhead audit:** `0`.
- **Ledger count:** 32 ledgers, 2087 actors.
- **Estimated weapons still on old templates:** `~609` mixed (from `WEAPON_3WAY_SPLIT.md` and `PROJECT_CONTEXT.md`). My own scan found `637` blocks with at least one old-template reference before this session; after the sessions' conversions the count is now closer to the 609 figure.

---

## 5. Remaining work: Weapon 3-way split

### 5.1 Phase 3 — mixed-weapon collapse (~609 weapons)

The 2-inherit cap is on **warheads**, not total inherits. Most mixed weapons combine two old full-stack templates (e.g. `^SmallArms + ^Chaingun`, `^Grenade + ^HeavyMissile`). They must be collapsed to:

```yaml
MyWeapon:
    Inherits@wh:  ^Warhead_<A>_<tier>
    Inherits@wh2: ^Warhead_<B>_<tier>   # only if a second warhead is legitimate
    Inherits@proj: ^Projectile_<family>_<tier>
    Inherits@fx:   ^Effect_<family>_<tier>
    ...
    Warhead@<A>_<tier>:                 # bare or AreaDamage
        Damage: 2000                     # preserved verbatim
```

**What to do mechanically:**
- Continue the cluster-by-cluster retrofit. The safest pattern is:
  1. Run `count_mixed.py` (or equivalent) to find the next-largest exact-2-inherit signature.
  2. Inspect 2–3 examples and determine the correct new-family mapping.
  3. Write a one-off `phase3_<cluster>.py` script that:
     - Replaces the 2 old `Inherits:` with `Inherits@wh/@wh2/@proj/@fx`.
     - Renames `Warhead@<Old1>` / `Warhead@<Old2>` (and `*Percentage`) to new key names.
     - Drops stale `*_FriendlyFire` twins when the 50% ratio matches the new baked FF.
     - Keeps `Damage` verbatim.
     - Uses **bare** main warhead keys when the block inherits the matching `^Warhead_*`; otherwise uses `AreaDamage` as a stopgap.
  4. Run `find_empty_warhead.py`.
  5. Run `resolve_weapon()` for a sample.
  6. Run `extract_stats.py`.
  7. Boot-gate.
  8. Commit.

**Exception list (do NOT reduce to 2 without maintainer sign-off):**
- Dune combat tanks — 3 cannon warheads (Cannon Light + Medium + Heavy).
- Terran Siege Tank (`SiegeTankSiegeCannon`) + Warcraft Siege Engine (`SiegeEngineCannon`).
- Asian Pulverizer gatling/mecha (multiple: CannonAP + Bullet + Chem + Missile).
- CABAL missile family (Reaper/HeavyReaper/Manticore/RocketCyborg — up to 4 warheads allowed).

### 5.2 Phase 4 — delete 30 orphaned old templates

When `grep -c 'Inherits.*\^<Old>'` returns `0` for each of:
- `^SmallArms`, `^Chaingun`, `^Grenade`, `^ShrapnelWeapon`, `^HeavyBomb`, `^TankDestroyerCannon`, `^MediumCannon`, `^HeavyCannon`, `^LightMissile`, `^MediumMissile`, `^HeavyMissile`, `^FlakWeapon`, `^HeavyAAWeapon`, `^LightFlameWeapon`, `^MediumFlameWeapon`, `^HeavyFlameWeapon`, `^LightChemicalWeapon`, `^MediumChemicalWeapon`, `^HeavyChemicalWeapon`, `^SwordWeapon`, `^ArrowWeapon`, `^MagicWeapon`, `^LaserWeapon`, `^RailgunWeapon`, `^TeslaWeapon`, `^TeslaChargedWeapon`, `^NuclearWarhead`, `^SniperWeapon`, `^ToxicWeapon`, `^HealingWeapon`, `^RepairWeapon`

Remove them and their `weapon_classes.yaml` entries. Boot-gate.

### 5.3 W2 — per-game / faction art templates (4–8 sessions)

`^Projectile_*` and `^Effect_*` currently use classic CnC assets. RA2, TS, and other game-specific weapon art is still inline or missing. This is mostly additive and low-risk.

Examples:
- `^Projectile_Missile_RA2` (vertical VLS for Patriot/Hover).
- `^Projectile_BallisticMissile_RA2` (V3/Dreadnought/Boomer).
- `^Effect_*_RA2` for Flak, Missile, Cannon.
- CABAL blue trail, Steel Consortium blue piffs, etc.

Build them, inherit them from the shared `^Effect_*` as game-specific overrides, boot-gate.

### 5.4 W3 — bundle dissolution (2–3 sessions)

Intermediates like `^RA2SmallArms`, `^RA2Chaingun`, `^TSMG`, `^SteelChaingun` still exist. Convert them to the new 3-inherit model. Their children will inherit the new structure.

### 5.5 W4 — retire `Warhead@1Dam` (4–7 sessions)

`297` live weapons still use the deprecated `Warhead@1Dam` pattern (`DESIGN.md` §870). Each needs a per-unit tier/profile judgment to reassign to a `^Warhead_*` template. Maintainer input is required.

---

## 6. Remaining work: Balance pipeline

### 6.1 Phase A — weapon/warhead foundation (structural)

| Sub-task | Status | Blocker | Notes |
|---|---|---|---|
| A1. Generator reconcile | Pending | Must be done before any `gen_weapon_template.py` regen | `gen_weapon_template.py` must emit `AreaDamage`, baked FF, `^Warhead_<Family>_<Level>` names, `Warhead@<tag>_Percentage`. `verify_generator_sync.py` must report `drift = 0`. |
| A2. Cannon/weapon rebuild | Pending | A1 | Split cannons into `CannonAP_` (anti-heavy) and `CannonHE_` (anti-vehicle). `TankDestroyerCannon` → `CannonAP_Light`. |
| A3. Projectile + effect libraries | Done | — | 24 + 27 templates. |
| A4. Weapon tuning laws | Pending | A1 | Energy `ExtraDamage` chips, MissileAA spread reduction, overall spread reduction, spread-pricing term. |
| A5. Retire `Warhead@1Dam` | Pending | Phase 3/W4 | 297 weapons. |

### 6.2 Phase F — synthesize + apply (currently blocked)

The sanctioned loop (from `AGENT_WORKSPACE.md` and `BALANCE_PIPELINE.md`):

```
python tools/balance/extract_stats.py              # refresh ledger
# edit the ledger or use build_workbook.py / import_workbook.py
python tools/balance/apply_balance.py --faction X --confirm
python tools/balance/extract_stats.py              # refresh again
tools/audit/run_all.sh
.\launch-game.cmd                                   # boot gate
```

**Vehicle stats apply** is the first concrete apply. It is blocked on:
- Maintainer confirm of the REVISION table in `docs/balance/anchor_decisions_log.md` (2026-07-31).
- A2/A4 to stabilize DPS/range.

### 6.3 Other pending balance items

- Infantry class anchors (C2): 4 new templates, `^AntiTankAntiAir` split, scout tier fix.
- Defense + aircraft anchors (C3).
- Formula v2 (D1/D2): spread pricing, AA pricing, AoE pricing, bake out per-actor multipliers.
- Phase G discrepancy triage and YAML cleanup.

---

## 7. Other pending backlog

- **Regression sweep (L):** review commits since ~2026-07-24 for Fluent/description-reference breakages.
- **Repo cleanup (L):** audit duplicate Python scripts; no deletes without maintainer sign-off.
- **Pre-existing content issues:** `mammothbunker.husk` missing `ArmamentInfo`; `ShortGameEnabled` field drift; voice-set gaps; `DeliversCash`/`Valued` unresolved.

---

## 8. Time estimates (PERT-style)

Assumptions:
- A "session" = 4–6 hours of focused work.
- `S` < 1 h, `M` = 1 session, `L` = 2+ sessions (from `ROADMAP.md`).
- PERT expected duration `E = (O + 4M + P) / 6`.
- These are **engineer-estimate ranges**, not commitments. Contingency not included.

| Work item | Optimistic (h) | Most likely (h) | Pessimistic (h) | PERT E (h) | Sessions |
|---|---|---|---|---|---|
| A1. Generator reconcile | 2 | 4 | 8 | 4.3 | 1 |
| A2. Cannon AP/HE rebuild | 4 | 8 | 16 | 8.7 | 1–2 |
| A4. Weapon tuning laws (ExtraDamage, spread, etc.) | 6 | 12 | 24 | 13.0 | 2–3 |
| Phase 3 mixed-weapon collapse (609 weapons) | 20 | 40 | 80 | 43.3 | 7–14 |
| W2 art templates | 12 | 24 | 48 | 26.0 | 4–8 |
| W3 bundle dissolution | 8 | 16 | 32 | 17.3 | 3–5 |
| W4 `Warhead@1Dam` retirement (297) | 16 | 32 | 64 | 34.7 | 6–12 |
| Phase 4 old template deletion | 2 | 4 | 8 | 4.3 | 1 |
| Vehicle stats apply (after unblocks) | 4 | 8 | 16 | 8.7 | 1–2 |
| Infantry anchors (C2) | 8 | 16 | 32 | 17.3 | 3–5 |
| Defense + aircraft anchors (C3) | 12 | 24 | 48 | 26.0 | 4–8 |
| Formula v2 (D1/D2) | 16 | 32 | 64 | 34.7 | 6–12 |
| Per-class/faction apply (F) | 20 | 40 | 80 | 43.3 | 7–14 |
| Regression sweep (L) | 8 | 16 | 32 | 17.3 | 3–5 |
| Repo cleanup (L) | 8 | 16 | 32 | 17.3 | 3–5 |
| Discrepancy triage + cleanup (G) | 12 | 24 | 48 | 26.0 | 4–8 |

**Total PERT expected:** ~360 hours / ~60–90 sessions.

**Realistic filtered path (weapon split → balance apply):**
- Weapon split: A1 (4) + Phase 3 (43) + W2/W3 (43) + W4 (35) + Phase 4 (4) ≈ **129 hours / ~22 sessions**.
- Balance: A2/A4 (22) + Vehicle apply (9) + Infantry anchors (17) + Formula (35) + Per-class apply (43) ≈ **126 hours / ~22 sessions**.
- Parallel/optional: W2 art + regression sweep + cleanup ≈ 60–90 hours.

**Bottom line:** The critical path (3-way split + first balance apply) is roughly **25–45 sessions**, depending on how many mixed weapons are mechanical vs bespoke. Full closure of all listed work is **60–90 sessions**.

---

## 9. Recommended execution order for the next agent

1. **Stabilize the 3-way split before touching balance numbers.**
2. **Start with A1 generator reconcile** so templates can be safely regenerated.
3. **Continue Phase 3 mixed-weapon clusters** in order of signature frequency (2-inherit families first, then 3+ inherits, then bespoke).
4. **Only after the weapon layer is clean** proceed to A2/A4 and the first `apply_balance --confirm` for vehicles.
5. **Document exceptions** as you go in `WEAPON_3WAY_SPLIT.md` and `ROADMAP.md`.

**Immediate next concrete step:** Continue the next highest-count dual-inherit cluster. Before this session, the top remaining exact-2-inherit clusters were:
- `Chaingun + LaserWeapon` / `Chaingun + FlakWeapon` (6 each)
- `HeavyChemical + LightChemical + MediumChemical` (6)
- `TeslaCharged + Tesla` (6)
- `Grenade + HeavyMissile` (now converted: `TorpTube`, `NodTorpTube`)

---

## 10. Critical rules and pitfalls

1. **Read `AREADAMAGE_HANDOFF.md` first** every session. The universal `AreaDamage` state is non-negotiable.
2. **Bare `Warhead@X:` is only safe when a same-key ancestor provides the type.** If the weapon does not inherit `^Warhead_X`, give it an explicit `AreaDamage` (main) or `HealthPercentageDamage` (percentage).
3. **Do not restate a different concrete type** on a warhead key (e.g. `Warhead@Bullet_Light: HealthPercentageDamage` when the template is `AreaDamage`). This causes FieldLoader crashes.
4. **`find_empty_warhead.py` must print `0` after any warhead edit.** It is the only reliable guard for the NRE class; `--check-yaml` does not catch it.
5. **Preserve `Damage` verbatim.** The retrofit is structural, not a rebalance. `Damage` is a multiple of 2000 per the `nice-number` law.
6. **Drop `_FriendlyFire` twins when the ratio is 50%** (new baked FF makes them redundant). Keep them only for bespoke ratios.
7. **Projectile `Report:` lives in `^Projectile_*` templates**; every weapon should eventually have its own explicit `Report:`. Watch for `-Report:` removal markers becoming orphaned.
8. **Boot-gate every commit.** `launch-game.cmd` → menu → kill → check for new `exception-*.log`.
9. **Never `git add -A`.** Use scoped adds. The maintainer has live uncommitted WIP.
10. **The CABAL 4-warhead exception and Dune 3-cannon exception are real.** Update `_MIX_ALLOWLIST` in `extract_stats.py` and `WEAPON_3WAY_SPLIT.md` if more exceptions are confirmed.

---

## 11. Essential commands

```bash
# Empty-warhead guard — run after any warhead/template edit
python tools/audit/find_empty_warhead.py

# Generator sync — must be 0 before regenerating templates
python tools/balance/verify_generator_sync.py

# Resolver spot-check
python -c "from miniyaml import Ruleset; print(Ruleset(pathlib.Path('.')).resolve_weapon('MyWeapon'))"

# Ledger refresh
python tools/balance/extract_stats.py

# Full audit suite (slow)
tools/audit/run_all.sh

# Boot gate
.\launch-game.cmd
```

---

## 12. Key files to load at the start of every session

In order:
1. `CLAUDE.md`
2. `docs/LESSONS_LEARNED.md`
3. `docs/AGENT_WORKSPACE.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/DESIGN.md` (relevant sections)
6. `docs/design/ROADMAP.md`
7. `docs/design/AREADAMAGE_HANDOFF.md`
8. `docs/design/WEAPON_3WAY_SPLIT.md`
9. `docs/audit/SUMMARY.md`

---

## 13. Where to pick up

If you are continuing the weapon split: open `mods/cameo/weapons/weapons.yaml`, confirm the new `^Warhead_*` / `^Projectile_*` / `^Effect_*` blocks are still present, then run the empty-warhead audit and inspect the next mixed-weapon cluster.

If you are continuing balance: open `docs/balance/anchor_decisions_log.md` and confirm whether the maintainer has approved the REVISION table; if yes, proceed with `apply_balance.py --confirm` for vehicles after A1/A2/A4 land.

---

## 14. Session log — 2026-08-07 (continuing the 3-way split)

### 14.1 Failed attempt: `HeavyFlame + MediumFlame` dual-flame retrofit

- Attempted a mechanical conversion of all weapons with `^HeavyFlameWeapon` + `^MediumFlameWeapon` (17 weapons in 7 files).
- Ran into `MiniYaml.Merge` duplicate `PhysicalStateName` boot crashes because flame weapons carry `ApplyPhysicalState` warheads (`PhysicalState*FlameWeapon` / `PhysicalState*FlameWeaponFriendlyFire`) and the new `^Effect_Flame_*` templates already provide `PhysicalStateName`, `ValidRelationships`, and `Range` for the same key.
- Lesson: local `PhysicalState...` overrides must not duplicate fields that the `^Effect_*` template already provides. Effect-heavy families (flame/chemical/sonic) need a `PhysicalState`-aware converter that strips/merges those fields carefully, or they must keep the old effect template inheritance for the non-last tier.
- Full working-tree revert executed; `master` remains boot-green.

### 14.2 Successful attempt: `ShrapnelWeapon + HeavyCannon` → `Concussion_Medium + CannonHE_Heavy`

- Converted 3 weapons: `RATurretGun` (RedAlert/Allies), `tkmtrenchcannon` (RedAlert2Mod/TKM), `TSRPGTower` (TiberianSun/GDI).
- New shape:
  ```yaml
  Inherits@wh: ^Warhead_Concussion_Medium
  Inherits@wh2: ^Warhead_CannonHE_Heavy
  Inherits@proj: ^Projectile_Shell_Heavy
  Inherits@fx: ^Effect_CannonHE_Heavy
  Warhead@Concussion_Medium:
  Warhead@Concussion_Medium_Percentage:
  Warhead@CannonHE_Heavy:
  Warhead@CannonHE_Heavy_Percentage:
  ```
- The `ShrapnelWeaponFriendlyFire` 50% twin was dropped (new `AreaDamage` baked FF replaces it).
- `find_empty_warhead.py` = 0. Boot-gate passed (reached `MenuPostProcessEffect.PostWorldLoaded`, no new `exception-*.log`).
- Ledgers refreshed via `extract_stats.py`.

### 14.3 Current safest next targets

Effect-free / low-effect dual-warhead clusters that should convert cleanly with the same pattern:
- `HeavyBomb + ShrapnelWeapon` — already done.
- `Grenade + HeavyBomb` — `Demolition_Light + Demolition_Heavy`.
- `Grenade + ShrapnelWeapon` — `Demolition_Light + Concussion_Medium`.
- `HeavyCannon + ShrapnelWeapon` — now done.
- `MediumCannon + TankDestroyerCannon` — `CannonHE_Medium + CannonAP_Light`.
- `HeavyCannon + MediumCannon` — `CannonHE_Heavy + CannonHE_Medium`.

Avoid flame/chemical/sonic/energy dual-warhead clusters until a `PhysicalState`/`GroundFire`/`EMP` effect-aware converter is built.

### 14.4 Completed in the same session: `MediumCannon + HeavyCannon` → `CannonHE_Medium + CannonHE_Heavy`

- Converted 4 weapons: `Type97Cannon` (RA Japan), `TigerCannon` (RA Shared), `HammerTankCannon` and `KotinCannon` (RA Soviets).
- New shape:
  ```yaml
  Inherits@wh: ^Warhead_CannonHE_Medium
  Inherits@wh2: ^Warhead_CannonHE_Heavy
  Inherits@proj: ^Projectile_Shell_Heavy
  Inherits@fx: ^Effect_CannonHE_Heavy
  Warhead@CannonHE_Medium:
  Warhead@CannonHE_Medium_Percentage:
  Warhead@CannonHE_Heavy:
  Warhead@CannonHE_Heavy_Percentage:
  ```
- `find_empty_warhead.py` = 0. Boot-gate passed. Ledgers refreshed.

### 14.5 Updated safest next targets

- `Grenade + HeavyBomb` — `Demolition_Light + Demolition_Heavy` (no `PhysicalState`, but `Grenade` provides the projectile, `HeavyBomb` the heavy effect). **Done 2026-08-07:** `8Inch` (RA Shared) converted. Removal markers (`-Warhead@Effect2:`, `-		-LaunchAngle:` etc.) must be deleted if the new `^Effect_*`/`^Projectile_*` template no longer contains that key, otherwise boot fails with "no elements to remove".
- `Grenade + ShrapnelWeapon` — `Demolition_Light + Concussion_Medium`. **Done 2026-08-07:** `ArtilleryShell` (`weapons/tiberiandawn.yaml`) and `SpecterArtilleryShell` (TD Nod) converted. The converter now auto-strips `-Warhead@*` removal markers to avoid the same crash.
- `MediumCannon + TankDestroyerCannon` — `CannonHE_Medium + CannonAP_Light`. **Done 2026-08-07:** `AlliedTankDestroyerCannon`, `SheridanCannon` (RA Allies) and `tkmturretcannon` (RA2Mod/TKM) converted.
- `HeavyCannon + MediumCannon` — now done.

### 14.6 Additional clusters completed 2026-08-07

- `HeavyMissile + ShrapnelWeapon` → `MissileHE_Heavy + Concussion_Medium`:
  `Hellfire`, `Aphid_AA` (RA Allies), `GradRockets` (RA Soviets),
  `SandmarineTusk`, `BigShieeTusk` (RA2Mod/TKM). The missile family
  always supplies `^Projectile_Missile_Heavy` because Shrapnel/Concussion
  has no projectile template; `^Effect_*` follows the last listed old
  inherit.
- `Chaingun + FlakWeapon` → `Bullet_Medium + Flak_Medium`:
  `APCGunAllies` (RA Shared), `FLAK-23-AG` (RA Soviets), `APCGun`
  (TiberianDawn GDI), `TSMutApcCannon` (TS Forgotten), `TSAAPCCannon`
  (TS GDI), plus one additional in `mods/cameo/weapons/tiberiansun.yaml`.
  `Inherits@proj`/`Inherits@fx` follow the last listed old inherit.
- `SmallArms + FlakWeapon` → `Bullet_Light + Flak_Medium`:
  3 weapons (RA Japan, RA2Mod/TKM). `Inherits@proj`/`Inherits@fx`
  follow the last listed old inherit.
- `FlakWeapon + MediumMissile` → `Flak_Medium + MissileHE_Medium`:
  `TS30mm` (TiberianSun GDI), `TKMAATurretCannon` and `FlakbusAA`
  (RA2Mod/TKM). `Inherits@proj`/`Inherits@fx` follow the last listed
  old inherit.

### 14.7 Final effect-free dual-inherit sweep (2026-08-07)

A generic converter processed the remaining 9 effect-free dual-inherit
pairs in one pass, converting 13 weapons across 8 files:

- `HeavyMissile + RailgunWeapon` → `MissileHE_Heavy + Railgun_Heavy` (2)
- `HeavyBomb + HeavyMissile` → `Demolition_Heavy + MissileHE_Heavy` (2)
- `Grenade + MediumMissile` → `Demolition_Light + MissileHE_Medium` (2)
- `MediumCannon + RailgunWeapon` → `CannonHE_Medium + Railgun_Heavy` (2)
- `FlakWeapon + LightMissile` → `Flak_Medium + MissileHE_Light` (1)
- `HeavyBomb + HeavyCannon` → `Demolition_Heavy + CannonHE_Heavy` (1)
- `HeavyCannon + HeavyMissile` → `CannonHE_Heavy + MissileHE_Heavy` (1)
- `ShrapnelWeapon + TankDestroyerCannon` → `Concussion_Medium + CannonAP_Light` (1)
- `MediumCannon + MediumMissile` → `CannonHE_Medium + MissileHE_Medium` (1)

All 13 were boot-gated with `find_empty_warhead.py = 0` and `extract_stats.py`.
After the generic sweep, an additional 3 leftover effect-free pairs were
converted (`Chaingun+Grenade`, `HeavyBomb+RailgunWeapon`, `ArrowWeapon+MediumMissile`).
`JHindChainGun` required a manual fix: `-		-LaunchAngle:` nested under
`Projectile: Bullet` had to be removed because the new `^Projectile_Bullet_Medium`
template does not include `LaunchAngle`.
A final effect-free dual `Grenade+RailgunWeapon` (`GlaveCanon`, StarCraft Protoss)
was converted to `Demolition_Light+Railgun_Heavy`.
- **First effect-heavy cluster test** (`Grenade+LightFlameWeapon` →
  `Demolition_Light+Flame_Light`) converted 4 parent molotovs
  (`ConscriptMolotov`, `GrenadeRA`, `tkmm203`, `tkm_trooper_gp25`) and 3
  child weapons. The `PhysicalStateLightFlameWeapon` local overrides were
  preserved (only `Amount` differs from `^Effect_Flame_Light`); `FriendlyFire`
  warhead nodes were stripped because the `^Warhead_*` templates already
  supply them. First attempt duplicated `Damage` under the new warhead key
  because `FriendlyFire` children were not skipped; fixed and boot-gated.
- **HeavyBomb + HeavyFlameWeapon** (`Demolition_Heavy + Flame_Heavy`) converted
  3 weapons: `Napalm` and `NapalmA10Carrier` (TiberianDawn/GDI) and
  `ParaBomb` (RedAlert/Shared). `Inherits@glow: ^ImpactGlow` on `ParaBomb`
  was preserved. No `Inherits@proj` was added because all three use
  `Projectile: GravityBomb` rather than the `Bullet` in `^Projectile_Flame_Heavy`.
- **FlameWeapon + Missile** (light/medium/heavy) converted 7 weapons. Mapping:
  `Light/MediumFlameWeapon -> Flame_Light/Medium`,
  `Light/MediumMissile -> MissileAP_Light/Medium`,
  `HeavyMissile -> MissileHE_Heavy`. `Projectile` inherited from the missile
  side for true `Projectile: Missile` weapons; `SCUD` kept its `Bullet: V2`
  and did not inherit `^Projectile_Missile_Heavy`. Local `Warhead@Effect`,
  `EffectAir`, and `Smudge` overrides preserved.
- **LaserWeapon mixed pairs** (`RA2LasherLaser` `MediumCannon+LaserWeapon` and
  `SteelQuantumCannon` `Railgun+LaserWeapon`) converted to
  `CannonHE_Medium+Laser_Heavy` and `Railgun_Heavy+Laser_Heavy`. Both use
  `^Projectile_Laser_Heavy` and `^Effect_Laser_Heavy`/`^Effect_Railgun_Heavy`;
  local `LaserZap`/`Railgun` projectile overrides and custom `Warhead@Effect`
  were preserved.
- **LightFlameWeapon + MediumMissile** (`LightTank2Missiles` in
  TiberianDawn/Nod) converted to `Flame_Light + MissileAP_Medium`. The weapon
  keeps its local `Warhead@Effect` (`small_frag`) and inherits
  `^Projectile_Missile_Medium` because the missile side was the final `Inherits`.
- **SmallArms + Chaingun** (`d2k_air_drone_guns` in D2k/Ixian) converted to
  `Bullet_Light + Bullet_Medium` while preserving the `^D2KMissile` addon.
  The addon is kept first so its `Warhead@MissileAP_Heavy` still applies;
  `^Projectile_Bullet_Medium` and `^Effect_Bullet_Medium` override the
  missile projectile/effect because they are inherited last.
- **TeslaWeapon + MediumMissile** (`JHindPlasmaCannon` in RedAlert/Japan)
  converted to `Tesla_Heavy + MissileAP_Medium` while keeping the `JHindCannon`
  custom inherit. `Warhead@ShrapnelWeapon` and `Warhead@TeslaExtraDamage` kept
  as standalone `SpreadDamage` nodes; the `JHindCannon` addon supplies the
  `Projectile` and `Effect`.
- **Tesla + Railgun + HeavyCannon** (`OIBigPlasmaCannon` and
  `Type97PlasmaCannon` in RedAlert/Japan) converted to
  `Tesla_Heavy + Railgun_Heavy + CannonHE_Heavy` with `^Effect_CannonHE_Heavy`
  and `^Projectile_Shell_Heavy` (corrected from a first-attempt typo
  `^Projectile_CannonHE_Heavy`, which does not exist).
- **TeslaWeapon + RailgunWeapon** (`WaveArtilleryImpact` in RedAlert/Japan)
  converted to `Tesla_Heavy + Railgun_Heavy` with `^Effect_Railgun_Heavy` and
  `Projectile: InstantHit` preserved. `Warhead@TeslaExtraDamage` kept as a
  separate `SpreadDamage` node.
- **Triple-FlameWeapon** (`FireballLauncherBuggy2`, `MatadorFlamer`,
  `MammothTuskThermobaric`) converted Light+Medium+Heavy flame stacks to
  three `^Warhead_Flame_*` inherits with `^Projectile_Flame_*` and
  `^Effect_Flame_*` from the last old family. Any local `ApplyPhysicalState`
  warhead missing `PhysicalStateName` had it injected (`Temperature`) because
  the old families used to supply it and the new `^Effect` only covers one
  tier.
- **TeslaChargedWeapon + TeslaWeapon** converted 6 pure dual-inherit EMP/ion
  weapons to `Tesla_Heavy + TeslaCharged_Super` with `^Projectile_Lightning_Super`
  and `^Effect_Tesla_Super`. `PulseMissile` (D2k), `IonCannon`,
  `Support_EMP_Bomb`, `SteelInspectorIonCannonDamage`, and others. Weapons with a
  local `Projectile:` got only `^Effect_Tesla_Super` so their custom projectiles
  stayed intact.
- **TeslaWeapon + MagicWeapon** converted 9 weapons to
  `Tesla_Heavy + Magic_Heavy` with `^Effect_Magic_Heavy`. `RA2DiskDrain` was
  skipped because it uses `^TeslaWeapon` only for `DamageTypes`, not for a
  `Warhead@TeslaWeapon` node. `D2k` storm weapons and `TiberianSun` sonic-zap
  weapons kept their local `Warhead@TeslaExtraDamage` / `EMPUnit` and
  custom `Projectile: Bullet` fields.
- **LightMissile + TeslaWeapon** converted 5 weapons to
  `MissileAP_Light + Tesla_Heavy` with `^Projectile_Lightning_Heavy` and
  `^Effect_Tesla_Heavy`. Nested `Projectile: LightningZap` removal markers
  (`-Image:`, `-TrailImage:`) were stripped to avoid NREs.
- **HeavyBomb + MediumFlameWeapon** (`Demolition_Heavy + Flame_Medium`) converted
  5 weapons across 3 files (`sandmarinemortar`, `bigshieemortar` and three
  others). `Inherits@proj` was set to `^Projectile_Flame_Medium` and the
  weapons override `Speed`/`Inaccuracy`/`LaunchAngle` locally.

### 14.8 Single-inherit effect-free sweep (2026-08-07)

A conservative single-inherit converter repointed 26 pure single-inherit
weapons (only one `Inherits:` tag, no other `Inherits@X` addons, and not
starting with `^`) across 15 files. An initial overly-broad attempt that
included multi-addon weapons produced 46 empty-type warheads and was
reverted before boot. The stricter filter left zero empty warheads and
passed the boot-gate.
