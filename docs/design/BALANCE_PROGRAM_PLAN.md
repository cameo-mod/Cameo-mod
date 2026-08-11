# BALANCE PROGRAM — the execution plan (rev. 2026-08-11)

**This file is the SINGLE SOURCE OF TRUTH for what is done, what is next, and who owns
what.** It survives compaction, agent handover and session death. Every other document
(ROADMAP, EFFECTIVE_DAMAGE, PHYSICAL_STATE_SYSTEM, the AI handoffs) links *here* for
status rather than keeping its own copy.

---

## 0. HOW TO USE THIS FILE (read first, every session, any agent)

1. **Don't trust the status column — verify it.** Every work item carries a `VERIFY`
   command that answers "is this actually done?" in one line. Run it. If it disagrees
   with the status, **the command wins** — fix the status in the same commit.
2. **Take the topmost item whose `needs:` are all ✅ and whose `owner:` is free.**
3. **Do not start an item whose files another agent holds.** Ownership is per FILE SET
   (§2), not per person — check `git log --oneline -3 <file>` and the file mtime first.
4. **Every item is finished the same way**: its `DONE WHEN` list, then the universal
   gate in §3, then update this file's status line **in the same commit** as the work.
5. **Never renumber the items.** W-ids are permanent references used by commits,
   memory files and letters. Add W13, W14 … ; mark dead ones `✖ DROPPED` with a reason.

**Status vocabulary:** `✅ DONE` · `🔵 IN PROGRESS (agent, date)` · `⬜ READY` (deps met)
· `⛔ BLOCKED (on Wx / on maintainer)` · `✖ DROPPED`.

---

## 1. THE BOARD

| id | work item | status | owner | needs |
|---|---|---|---|---|
| **W1** | K coefficient + target model (measured Versus weights, capped density) | ✅ DONE `f8421d345` | Claude | — |
| **W2** | `^LightFlameWeapon` → 3-way split + new `^Warhead_Inferno_*` family | ⬜ READY | **Devin** | — |
| **W3** | Ledger split: raw stays, derived moves to `docs/balance/derived/` | ⬜ READY | Claude | W1 |
| **W4** | Retire weapon-class K; charge-up becomes an ACTOR property | ⬜ READY | Claude | W1 |
| **W5** | Missing metrics: overkill/TTK, range advantage, ValidTargets, MinRange, AttackDelay | ⬜ READY | Claude | W1 |
| **W6** | C# `ModifiesCombatProportionalToPhysicalState` (+ pitch/glow hooks) | ⬜ READY | either | — |
| **W7** | Sonic → `Resonance` meter (no new C# needed) | ⬜ READY | either | — |
| **W8** | Gatling ladder → `SpinUp` meter | ⛔ BLOCKED | either | W6 |
| **W9** | `^Poisonable` → `Poison` meter (gas-cloud dose-response) | ⬜ READY | either | — |
| **W10** | `^Blindable` → `Blind` meter | ⛔ BLOCKED | either | W6 |
| **W11** | Wire K into `fit_class.py` behind a flag; fit one class both ways and compare | ⛔ BLOCKED | Claude | W3, W4, W5 |
| **W12** | Superweapon balancing as a SEPARATE track (not unit-priced) | ⬜ READY | maintainer-led | — |

**Recommended order:** W2 ∥ W3 → W4 → W5 → W6 → (W7, W9) → W8 → W10 → W11 → W12.
`∥` = safe to run in parallel (disjoint file sets).

---

## 2. FILE OWNERSHIP — how two agents work at once without collisions

One owner per FILE SET at a time. These sets are disjoint by construction:

| set | files | items |
|---|---|---|
| **A — pipeline tools** | `tools/balance/*.py`, `docs/balance/**` | W3, W4, W5, W11 |
| **B — weapon content** | `mods/cameo/weapons/**`, `mods/cameo/ContentPacks/**/weapons.yaml` | W2 |
| **C — engine C#** | `OpenRA.Mods.Cameo/**`, `engine/**` | W6 |
| **D — actor defaults** | `mods/cameo/rules/defaults.yaml` | W7, W8, W9, W10 |

⚠ **Set D is a single file — serialise W7/W8/W9/W10, never run two at once.**
⚠ W2 (set B) touches `mods/cameo/weapons/weapons.yaml`, which W7 also touches for the
`^Warhead_Sonic_*` templates. **If W2 and W7 overlap in time, W7 waits.**

---

## 3. THE UNIVERSAL GATE (every item, no exceptions)

```sh
python -m unittest discover -s tools/tests -t tools/tests   # all green
python tools/audit/find_empty_warhead.py                    # 0
python tools/balance/verify_generator_sync.py               # drift = 1 (^Warhead_Sniper_Light)
bash tools/audit/run_all.sh                                 # bash ONLY — PowerShell writes UTF-16
python tools/balance/extract_stats.py --check               # 0 drifted
```
then the **boot gate** (CLAUDE.md rule 1 — absolute):
1. snapshot `%APPDATA%/OpenRA/Logs/exception-*.log` count **before** launching (baseline 169);
2. rebuild first if `OpenRA.Mods.Cameo/` or `engine/` changed:
   `DOTNET_ROLL_FORWARD=LatestMajor dotnet build -c Release --nologo -p:TargetPlatform=win-x64`;
3. `launch-game.cmd`, then **grep** `perf.log` for `MenuPostProcessEffect.PostWorldLoaded`
   (never read the last line — map loading trails after it) and confirm the file's mtime
   is **after** the cutoff, so it is this run and not a stale marker;
4. 0 new `exception-*.log`; `Stop-Process -Name OpenRA -Force`;
5. **scoped `git add <files>` only** — never `-A` / `.` / `--all`;
6. update this file's status row **in the same commit**.

Commit trailer = the ACTUAL agent (CLAUDE.md rule 10). Never sign as another agent.

---

## 4. THE WORK ITEMS

### W1 — K coefficient + target model ✅ DONE (`f8421d345`)

`effective_dps = Damage_total × (burst / eff_reload) × FirepowerMultiplier × K`, with
`K = Σ_warheads share_w × versus_w × (reliability_w + secondary_w)`.

K is independent of the Damage magnitude, so pricing inverts exactly:
`Damage_required = target_dps × eff_reload / (burst × FP × K)`, quantised to the 2000
grid with `FirepowerMultiplier` absorbing the remainder. Spec: `EFFECTIVE_DAMAGE.md`.

**VERIFY:** `python tools/balance/weapon_efficiency.py --families` prints 20 rows.

---

### W2 — `^LightFlameWeapon` → 3-way split + `^Warhead_Inferno_*` ⬜ READY · owner **Devin**

**Why:** `^LightFlameWeapon` sets `Spread: 500` **and** `Range: 500`. A single-value
`Range` makes `effectiveRange` length 1, so `GetDamageFalloff`'s loop never runs and it
returns 0 — **77 live weapons deal zero flame damage** and always have. The fix is not
deleting the line; it is finishing the 3-way split (one warhead + one projectile + one
effect inherit), which removes the dead template entirely.

**Maintainer's warhead order (2026-08-11) — granted, this mapping:**

| weapons | → warhead |
|---|---|
| `HonestJohn`, `FireRockets*` | `^Warhead_FireMissile_Heavy` |
| `SiegeMortar*`, **V2 rocket** | `^Warhead_Thermobaric_Heavy` |
| `VenomLaser`, `NodTurretLaser` | `^Warhead_Laser_Medium` |
| `LaserBuggy2`, laser rifle infantry | `^Warhead_Laser_Light` |
| `HeatRayBeam1/2` | `^Warhead_Inferno_Heavy` (new) |
| `25mmWaveforce`, `TankBusterBeamCannonCharged` | **ASK the maintainer** |

**New family** — three lines in `tools/balance/gen_weapon_template.py`:
```python
INHERIT_FAMILIES = {
    "Cryo":    ("Prism", "Temperature", -100, L3),   # existing
    "Inferno": ("Prism", "Temperature", +100, L3),   # NEW — prism chassis that burns
}
```
Named `Inferno`, not `HeatRay`: the family is the ELEMENT, not the delivery, so
non-beam flame weapons can use it later (same reason `Cryo` keeps its name).

**DONE WHEN**
- [ ] `Inferno` in the generator; `splice_templates.py inferno`; drift back to 1.
- [ ] Every weapon in the table above inherits exactly ONE `^Warhead_*`, ONE
      `^Projectile_*`, ONE `^Effect_*`.
- [ ] `^LightFlameWeapon` has zero remaining inheritors, then is deleted.
- [ ] `tools/audit/review_resolve_diff.py` run before/after on a sample of ≥10 of the
      77 — the ONLY expected change is that flame damage now lands.
- [ ] `find_empty_warhead.py` = 0.
- [ ] Balance pass queued (77 weapons gaining real damage is a live balance change).

**VERIFY:** `grep -rc "\^LightFlameWeapon" mods/cameo --include=*.yaml` → 0

---

### W3 — Ledger split ⬜ READY · owner Claude · needs W1

`BALANCE_PIPELINE.md` §2 says the ledger is RAW STATS ONLY. Five derived fields
currently sit in it, and correcting the scatter model rewrote **4136 ledger lines with
`mods/` untouched** — model noise inside the artifact whose job is proving yaml ↔
ledger equality.

**Design:** same `extract_stats.py` run writes two trees.
`docs/balance/<faction>.json` — raw only (a diff means *the game changed*).
`docs/balance/derived/<faction>.json` — `k`, `effective_per_shot`, `eff_reload`,
`effective_dps`, `avg_versus`, `footprint`, `reliability`, `sigma` (a diff means
*the model changed*). Keeps clutter out of the raw file and gives each diff one question.

**DONE WHEN**
- [ ] the five `effective_*` fields are gone from `docs/balance/*.json`;
- [ ] `docs/balance/derived/*.json` exists, one per faction, same section shape;
- [ ] `audit_balance_drift` still passes and now reads only the raw tree;
- [ ] the workbook builder reads derived from the new path;
- [ ] `BALANCE_PIPELINE.md` §2's ⚠ block is replaced with the settled rule.

**VERIFY:** `grep -l effective_damage docs/balance/*.json | wc -l` → 0

---

### W4 — Retire weapon-class K; charge-up moves to the ACTOR ⬜ READY · owner Claude · needs W1

**Maintainer ruling 2026-08-11:** chips now count in the metric, so their structural
"payment" must come off or they are double-charged. Concretely:
- `WeaponClass` / K as a per-weapon-type multiplier is **retired** — the metric measures
  what the weapon does, so the tier weight is no longer needed to stand in for it.
- **Charge-up is an ACTOR property, not a weapon one.** The actors carrying
  `AttackCharged` / `AttackTurretedCharged` / `AttackFrontalCharged` take a **0.75×**
  multiplier (a charge delay is a large real nerf), handled exactly like the documented
  Obelisk of Light case: the delay inflates the effective reload AND lowers the price.

**DONE WHEN**
- [ ] `formula.dps()` no longer takes `weapon_class`; call sites updated;
- [ ] `fit_class.py` reads the charge trait off the ACTOR and applies 0.75×;
- [ ] `docs/design/FORMULA_V2.md` + `ARMOR_SYSTEM` updated to state the retirement;
- [ ] a fixture test pins "charged actor prices 0.75× an identical uncharged one".

**VERIFY:** `grep -rn "weapon_class" tools/balance/formula.py | wc -l` → 0

---

### W5 — The five missing metrics ⬜ READY · owner Claude · needs W1

All approved by the maintainer 2026-08-11. Add each to `weapon_efficiency.py` as a
named, individually-inspectable factor — never one blended fudge:

1. **Overkill / TTK** — DPS ignores waste; a 200k burst on a 50k target throws away 75%.
   Model per-shot damage against the class-anchor HP and discount the excess.
2. **Range advantage** — outranging is worth more than DPS in a straight fight. Score
   `range / class_median_range` as a bounded bonus.
3. **`ValidTargets`** — ground-only and all-target weapons currently score identically.
   A hard multiplier (a weapon that cannot hit air is worth measurably less).
4. **`MinRange`** / blockability / turret traverse — a dead zone is a real cost.
5. **`AttackDelay`** — the charge-up law lives in a memory, not in the metric (pairs
   with W4).

**DONE WHEN** each factor is a separate column in the derived output, each has a test,
and `EFFECTIVE_DAMAGE.md` §3 documents it (moved out of "deliberately not included").

**VERIFY:** `python tools/balance/weapon_efficiency.py --families` shows the new columns.

---

### W6 — C# `ModifiesCombatProportionalToPhysicalState` ⬜ READY · owner either

The framework's missing half: every existing proportional trait only makes things
*worse* (`SlowsProportionalToPhysicalState`, `DamageMultiplierProportionalToPhysicalState`).
A spin-**up** needs a signed one.

**Shape** — mirror `SlowsProportionalToPhysicalState`:
```
PhysicalStateName: SpinUp
ReloadDelayFrom/To: 100 / 60      # any subset of the four
RangeFrom/To:       100 / 122
SpeedFrom/To:       100 / 122
FirepowerFrom/To:   100 / 100
```
Maintainer picked **option C**: fold the readability hooks INTO this trait rather than
bolting on separate ones — an audio **pitch** scale (`PitchFrom/To`) driven by the same
meter, and a glow/overlay hook reusing the existing weapon-glow effects.

**DONE WHEN** built, `dotnet build -c Release -p:TargetPlatform=win-x64` clean, deployed
to `engine/bin`, and a CONCRETE actor instantiates it (an abstract-only template proves
nothing — see memory `cameo-dll-deploy-engine-bin`).

**VERIFY:** boot with a gatling actor present, no `Cannot locate type` in the log.

---

### W7 — Sonic → `Resonance` meter ⬜ READY · owner either

Needs **no new C#** — `DamageMultiplierProportionalToPhysicalState` and
`SlowsProportionalToPhysicalState` already exist.

```yaml
^Warhead_Sonic_<Level>:
    Warhead@Sonic_<Level>: AreaDamage
        PhysicalStates:
            Resonance: 100        # replaces the whole _Debuff GrantExternalCondition
```

**The design rule that keeps it distinct from Corrosion** (maintainer-approved):

| | Corrosion | **Resonance** |
|---|---|---|
| role | attrition — kills on its own | **force multiplier — kills nothing** |
| damage | DoT | **none, ever** |
| decay | slow, lingers | **fast, dies with the beam** |
| identity | poison you flee | a spotlight your team shoots into |

Sonic becomes the only debuff that deals no damage of its own: worthless solo, doubles
the army's output in a group. Emit via the generator's `FAMILY_PHYSICAL_STATE`, not by
hand. Retire `^SonicDebuff` + the `_Debuff` warheads once the meter is live.

**DONE WHEN** meter defined on defaults; generator emits it; `_Debuff` warheads and
`^SonicDebuff` removed; the predator/waveforce/IonPulse hand-grants re-pointed or
removed. Expect a balance pass: one hit no longer gives the full debuff.

**VERIFY:** `grep -rc "SonicDebuff" mods/cameo --include=*.yaml` → 0

---

### W8 — Gatling ladder → `SpinUp` meter ⛔ needs W6

**47 actors** × 20–30 multiplier traits ≈ **1340 trait objects**, roughly **40% of all
3197 multiplier instances in the mod**, in ten visible 5% steps. A meter replaces them
with 3–4, continuously.

Current ladder resolves to `0.95¹⁰ = 0.599` reload (fire rate ×1.67) and
`1.02¹⁰ = 1.219` range/speed — those are the **end-points** the meter must reproduce
(0 → 100%, max → 60% reload / 122% range / 122% speed). Elite variant fills faster
(`RequiredShotsPerInstance: 1,1,1…` vs `1,2,3…`, `RevokeDelay` 15 vs 30) → same meter,
higher fill rate.

**DONE WHEN** `^GatlingSpeedUpTurretBehavior` / `…UnitBehavior` / `…SpecialUnitBehavior`
are meter-based, all 47 actors verified in `review_resolve_diff`, end-points match.

**VERIFY:** `grep -c "GattlingSpeed" mods/cameo/rules/defaults.yaml` → 0

---

### W9 — `^Poisonable` → `Poison` meter ⬜ READY · owner either

A Corrosion clone with a different victim class: **corrosion eats vehicles, poison hurts
infantry, flame does both** — a clean three-way split of the DoT space.

Maintainer's design: the Yuri Virus (and friends) spawn a **gas cloud**; the cloud does
very little direct damage but **fills the Poison meter for as long as a unit stands in
it**, and the DoT scales off the meter. Dose-response — one dart ≠ a lingering cloud.
`ChangesHealthProportionalToPhysicalState` already exists, so no new C#.

**DONE WHEN** meter on defaults, cloud weapons feed it via `PhysicalStates`, the old
binary `poisoned` condition is retired, infantry-only gating verified.

**VERIFY:** `grep -rc "Condition: poisoned" mods/cameo --include=*.yaml` → 0

---

### W10 — `^Blindable` → `Blind` meter ⛔ needs W6

Today: binary, range/vision/detection → 20%. A cliff. Maintainer's spec:
- scale range **100% → 20%** proportionally with the meter (20% at full blind);
- **at FULL blind only**: disable the weapon entirely, show the `blinded_icon`
  decoration, and apply the `blinded` **Targetable** type so blinding units retarget
  instead of wasting shots on an already-blind target.

Needs W6 for the proportional range scaling; the full-blind cliff stays a
`GrantConditionOnPhysicalState` at max.

**VERIFY:** `grep -c "RequiresCondition: blinded" mods/cameo/rules/defaults.yaml` → only
the max-meter uses remain.

---

### W11 — Wire K into `fit_class.py` ⛔ needs W3, W4, W5

Behind a flag. Fit ONE class both ways, diff the resulting prices, show the maintainer,
and only then switch the pipeline. **Never** flip pricing and content in one commit.

**VERIFY:** the comparison report exists in `docs/balance/derived/` and the maintainer
has signed off in `anchor_decisions_log.md`.

---

### W12 — Superweapons as a separate track ⬜ READY · maintainer-led

Maintainer 2026-08-11: superweapons are **not tied to a unit** and are not priced by the
unit formula — the blob cap in W1 exists partly because a superweapon footprint would
otherwise claim 50 kills. They need their own process (charge time, one-per-base,
counterplay), tracked separately from class anchors.

---

## 5. WHAT THE MODEL SAYS TODAY (the W1 baseline, for regression comparison)

All families at Heavy, 20 000 damage, abstract templates (so `reliability` = 1.00 —
the accuracy axis only differentiates on a concrete weapon with a projectile):

`Storm 2.43 · Flame 2.07 · Plasma 2.03 · Concussion 2.01 · Thermobaric 2.00 ·
Chemical 1.99 · Demolition 1.97 · Quantum 1.86 · Flak 1.84 · CannonHE 1.79 ·
CannonAP 1.76 · Sonic 1.63 · MissileHE 1.57 · MissileAP 1.52 · Magic 0.99 ·
Prism 0.84 · Bullet 0.81 · Tesla 0.81 · Railgun 0.75 · Laser 0.56`

Constants: `reference HP 74,000` (measured median) · `A_BLOB 9 cell²` · `A_SELF 1 cell²`
· `BLOB_UPTIME 0.30` · density INF 2.0 / VEH 0.33 / BLD 0.25 / AIR 0.20 · engagement
INF 35% / VEH 40% / BLD 15% / AIR 10%.

**If a change moves these numbers, that is the signal to explain in the commit message.**

---

## 6. LINKS

`EFFECTIVE_DAMAGE.md` (the metric) · `BALANCE_PIPELINE.md` (the loop) ·
`FORMULA_V2.md` (the laws) · `PHYSICAL_STATE_SYSTEM.md` (meters) ·
`SPREAD_FALLOFF_PLAN.md` (falloff shapes) · `WEAPON_3WAY_SPLIT.md` (the split) ·
`ROADMAP.md` (everything else) · `AI_HANDOFF_2026-08-05.md` (agent letters)
