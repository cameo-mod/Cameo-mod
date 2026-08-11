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
| **W2** | `^LightFlameWeapon` → 3-way split + new `^Warhead_Inferno_*` family | 🔵 IN PROGRESS (Devin, 2026-08-11) | **Devin** | — |
| **W3** | Ledger split: raw stays, derived moves to `docs/balance/derived/` | ✅ DONE | Claude | W1 |
| **W4** | Retire weapon-class K; charge-up becomes an ACTOR property | ✅ DONE | Claude | W1 |
| **W5** | Missing metrics: overkill/TTK, range advantage, ValidTargets, MinRange, AttackDelay | ✅ DONE | Claude | W1 |
| **W6** | C# `ModifiesCombatProportionalToPhysicalState` (+ pitch/glow hooks) | ⬜ READY | either | — |
| **W7** | Sonic → `Resonance` meter (no new C# needed) | ⬜ READY | either | — |
| **W8** | Gatling ladder → `SpinUp` meter | ⛔ BLOCKED | either | W6 |
| **W9** | `^Poisonable` → `Poison` meter (gas-cloud dose-response) | ⬜ READY | either | — |
| **W10** | `^Blindable` → `Blind` meter | ⛔ BLOCKED | either | W6 |
| **W11** | Wire K into `fit_class.py` behind a flag; fit one class both ways and compare | ⛔ BLOCKED | Claude | W3, W4, W5 |
| **W12** | Superweapon balancing as a SEPARATE track (not unit-priced) | ⬜ READY | maintainer-led | — |
| **W13** | Warhead system rebuild from the 2494-profile reference corpus | ⬜ READY | Claude | W1, W5 |
| **W14** | Renormalise `avg_versus` over REACHABLE armors (fixes a W5 double-count) | ⬜ READY | Claude | W5 |

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

### W3 — Ledger split ✅ DONE · owner Claude · needs W1

`BALANCE_PIPELINE.md` §2 says the ledger is RAW STATS ONLY. Five derived fields sat in
it, and correcting the scatter model rewrote **4136 ledger lines with `mods/`
untouched** — model noise inside the artifact whose job is proving yaml ↔ ledger
equality.

**Shipped:** one `extract_stats.py` run writes two trees off the same resolve, so they
cannot desync.

| tree | a diff means |
|---|---|
| `docs/balance/<faction>.json` — raw only | **the game changed** |
| `docs/balance/derived/<faction>.json` — `k`, `avg_versus`, `effective_per_shot`, `eff_reload`, `effective_dps`, `effective_damage`, `damage_total`, `footprint`, `reliability`, `sigma` | **the model changed** |
| `docs/balance/derived/_model.json` — every constant they depend on | the model was **retuned** |

- [x] the five `effective_*` fields are gone from `docs/balance/*.json` — the split
      commit is **12 130 deletions, 0 additions**, every removed line one of the five
      names, so provably not one raw stat moved;
- [x] `docs/balance/derived/*.json`, 32 sidecars + `_model.json`; rows carry only
      `slot` + `weapon` as join keys, never a duplicated raw stat;
- [x] `audit_balance_drift` reads the raw tree **by construction** —
      `build_ledgers()` returns raw and `build_both()` is the two-tree entry point, so
      it cannot start diffing model output by accident;
- [x] `extract_stats.py --check` verifies both trees and labels findings
      `DRIFT (raw)` vs `DRIFT (model)`;
- [x] `BALANCE_PIPELINE.md` §2's ⚠ block replaced with the settled rule;
- [x] `tools/tests/test_ledger_split.py` (9 tests) pins it — the guard trips on 310
      rows of the pre-split ledgers, so it fails when it should.

⚠ **Correction to this item's original DONE list:** it required "the workbook builder
reads derived from the new path". That premise was wrong — `build_workbook.py` and
`import_workbook.py` never read the five fields even while they sat in the ledger
(`grep -n effective tools/balance/build_workbook.py` → one comment). Nothing consumes
the derived tree today; giving it its first consumer is **W11**, not W3. No consumer was
invented just to satisfy a checkbox.

Also folded in (both measured, neither changes a number): `target_model` now resolves
the roster **once** instead of twice and reuses the caller's `Ruleset` via
`use_ruleset()` — cold census 15.3s → 6.8s, full extraction of *both* trees 18s. The
armor census is byte-identical afterwards (Wood 563 … Fighter 20, reference HP 74 000).

**VERIFY:** `grep -l effective_damage docs/balance/*.json | wc -l` → 0
and `ls docs/balance/derived/*.json | wc -l` → 33

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
- [x] `formula.dps()` no longer takes `weapon_class`; **all six** call sites updated —
      `fit_class`, `check_band`, `propose_rebalance`, `propose_class_rebalance`,
      `update_ranges`, and the workbook's **Excel DPS cell** (`build_workbook` dropped
      `*WeapClass`). The sheet is a second implementation of the same math and
      `formula.py`'s docstring promises the two agree — leaving the factor in Excel
      would have made the workbook and the module disagree silently;
- [x] `fit_class.py` reads the charge trait off the ACTOR and applies 0.75×, via the new
      `price_unit()` (extracted so the rule is testable rather than inline in `main`);
- [x] `docs/design/FORMULA_V2.md` + `ARMOR_SYSTEM` updated to state the retirement;
- [x] `tools/tests/test_formula_charge.py` (10 tests) pins the fixture
      "charged actor prices 0.75× an identical uncharged one", plus the Tesla exclusion
      and the positional-argument shift.

⚠ **Two findings that changed the shape of this item:**

1. **The ruling's own example was not covered by the ruling's own trait list.** It names
   `AttackCharged` / `AttackTurretedCharged` / `AttackFrontalCharged` and cites the
   Obelisk of Light as the model case — but the Obelisk uses **`AttackCharges`**, a
   different trait, so the three named traits would have left the cited precedent at
   full price. `AttackCharges` is therefore in `formula.CHARGE_UP_TRAITS` (4 Obelisks).
   Live counts: `AttackFrontalCharged` 5 · `AttackCharges` 4 · `AttackTurretedCharged` 2.
2. **`AttackTesla` (3 actors) is recorded but NOT discounted.** The Tesla Coil is already
   priced as a special case (ReloadDelay 100 + InitialChargeDelay 25, MaxCharges 3, its
   own K), so the generic 0.75× on top would compensate the same nerf twice — leaving a
   charging unit over-paid and cost-efficient rather than balanced. It sits in
   `CHARGE_UP_EXCLUDED_TRAITS` and **needs a maintainer ruling** before it joins.

Also resolved: `FORMULA_V2` §3b planned to compensate this same nerf as a **−0.25
negative special**. Both firing would pay for one weakness twice — and since a price cut
is a BUFF in value terms (cheaper = better per credit), the result would be a charging
unit that is over-compensated and cost-efficient, not balanced. §3b now records that the
charge half is implemented as the actor price multiplier and the special-K route must
not also fire. (The frontal-facing
−0.25 half is untouched and still future scope.)

**VERIFY:** `python -c "import sys;sys.path.insert(0,'tools/balance');import inspect,formula;print('weapon_class' in inspect.signature(formula.dps).parameters)"` → `False`
(the old `grep … | wc -l → 0` cannot pass: the docstrings that EXPLAIN the retirement
must name the retired thing. Test the signature, not the prose.)

---

### W5 — The five missing metrics ✅ DONE · owner Claude · needs W1

All approved by the maintainer 2026-08-11. Each is a named, individually-inspectable
factor in `weapon_efficiency.py` — never one blended fudge, so a price that moved can be
traced to the ONE factor that moved it. Spec + shapes: `EFFECTIVE_DAMAGE.md` §3.0.

| # | factor | shape | it bites |
|---|---|---|---|
| 1 | **overkill / TTK** | `HP / (ceil(HP/dmg) × dmg)` — waste is only the LAST shot | 200k on 50k → **0.25** |
| 2 | **range advantage** | `1 + 0.25 × (range/median − 1)`, bounded `[0.75, 1.50]` | long artillery **1.33** |
| 3 | **`ValidTargets`** | `0.5 + 0.5 × engagement share` | ground-only **0.95**, AA-only **0.55** |
| 4 | **`MinRange`** | `1 − (MinRange/Range)²` — the annulus, so area not radius | 2800/11000 → **0.96** |
| 5 | **`AttackDelay`** | ✖ **does not exist** — see below | — |

**The split that makes this safe:** factors 2–4 do NOT depend on `Damage`, so they fold
into the new **`k_context`** and the pricing inversion stays closed-form. **Overkill does**
depend on Damage, so it is reported BESIDE K and never inside it — folding it in would turn
`Damage_required = target_dps × eff_reload / (burst × FP × K)` into a fixed-point iteration.
`test_weapon_context.py` pins that distinction explicitly.

⚠ **Item 5 was based on a field that isn't there.** `AttackDelay` appears **0 times** in
the tree. Charge-up is an ACTOR trait (`AttackCharged`, `AttackCharges`, `AttackTesla`, …)
and W4 already implemented it there as the 0.75× price multiplier — which is the right
layer, since one weapon serves many actors. Nothing to add at the weapon level; no
placeholder was invented to fill the row.

**DONE WHEN**
- [x] each factor is a separate column in the derived output — `factor_targets`,
      `factor_range`, `factor_deadzone`, `overkill`, plus `k_context`;
- [x] each has a test — `tools/tests/test_weapon_context.py`, 21 tests;
- [x] `EFFECTIVE_DAMAGE.md` §3.0 documents them, moved out of "deliberately not included".

**VERIFY:** `python tools/balance/weapon_efficiency.py --families` shows
`targets · range · deadzone · overkill · K ctx`.

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

### W13 — Warhead system rebuild from the reference corpus ⬜ READY · owner Claude

Reference data: `docs/reference/versus_raw.json` — **2494 warhead profiles, 14 sources**,
built by `tools/reference/extract_versus.py` (+ `extract_mix_ini.py` for Mental Omega).

**Measured findings that drive the rules below** (all reproducible from that file):

| finding | number |
|---|---|
| field median profile span | **87** (Cameo: Light 90 · Medium 75 · Heavy 60 · Super 45) |
| field distribution | **56% sharp · 23% moderate · 21% flat** — the moderate middle is the LEAST used band, and 3 of Cameo's 4 levels sit in it |
| Mental Omega alone | median span **95**, 34% flat — a BARBELL: many hard counters AND many all-rounders, few in between |
| archetypes occupied | Cameo **14** · field **28** |
| Cameo's most common archetype | `BLD>INF>VEH FLAT HE` at **17.8%**, vs **0.4%** in the field |
| multi-warhead flattening | 1 warhead → median span **75**; 2+ warheads → **58**. Worst cases lose ~250 span (`VonSniperLockdown` 7 warheads: 290 → 30) |
| live weapons with 2+ warheads | **1335 of 1972 (68%)** — the size of the migration |

**THE RULES (maintainer, 2026-08-11):**

1. **Exactly ONE damage warhead per weapon.** This is the balance mechanism, not
   housekeeping: mixing warheads averages their profiles and destroys the counter. The
   flattest weapons in the game today are the mixed ones, not the designed ones.
2. **Archetype = macro order × sharp/flat × HE/AP direction × air position.** Aim to
   occupy the field's ~28 rather than today's 14.
3. **Most warheads SHARP; ~20% intentionally FLAT** (Sonic, Magic, Tesla) — the field's
   own ratio, and MO proves you can have both extremes without a mushy middle.
4. **CLUSTER the reference values, never average them.** Averaging all 2057 three-class
   profiles yields span **24** against a field median of 87 — it collapses exactly the
   rock-paper-scissors the corpus was gathered to produce. Take the median WITHIN each
   archetype cluster.
5. **Wild values allowed** — no fixed step law. Sources run to span 295 (`LaserTur`:
   infantry 320, heavy 25). Cameo's step law caps at 90.
6. **The ordering law still governs** (best→worst by macro priority + sub-ladder). It is
   what keeps "wild" coherent rather than random.
7. **Thematic fit per family**: flame = `INF>BLD>VEH · SHARP · HE`, missiles = the
   air-capable counterpart of cannons, etc.
8. ⚠ **EVERY warhead damages EVERY armor type — never zero** (maintainer ruling). A
   landed helicopter is a legitimate target for a flame tank. "Cannot fight air" is
   expressed by putting the aircraft armors at the END of the ordering (low, non-zero),
   NOT by omitting them.
   **Cameo's four dedicated aircraft armors are a deliberate improvement over the source
   mods, not a divergence to fix.** Those engines share one armor type between aircraft
   and tanks, so they simply cannot express "devastating vs aircraft, mediocre vs tanks,
   still good vs infantry" — the flak-cannon profile. An earlier draft of this item
   listed Cameo's 100%-air-coverage as a gap; that was wrong.
9. **Prerequisite — the `%`-twin.** `formula.distribute_damage` computes the twin as
   `per // DAMAGE_STEP` (integer division). Drop the 2000 grid before fixing that and
   every percentage warhead silently becomes 0 below 2000 damage — hard immunity by
   rounding. Fix first, then free the grid.
10. **FirepowerMultiplier survives the grid removal**, but only as the per-ACTOR knob
    (one weapon serves many actors). It is no longer needed to absorb rounding, because
    free-valued Damage solves exactly.

**VERIFY:** `python tools/reference/extract_versus.py --summary` → 14 sources, 2494 rows.

---

### W14 — Renormalise `avg_versus` over REACHABLE armors ⬜ READY · owner Claude · needs W5

A W5 double-count found by the maintainer 2026-08-11. A ground-only weapon is penalised
twice: `avg_versus` averages over all 16 armors including the four aircraft ones (10% of
the engagement weight) which it can rarely apply, **and** `targets_factor` then multiplies
by 0.95 for not reaching air.

**Fix:** renormalise the armor weights over the armors the weapon can actually engage, so
`avg_versus` measures the damage it can really deal; `targets_factor` then carries only
the opportunity cost of having fewer target types. Keeps the low-but-non-zero air values
(rule 8 above — landed aircraft) without letting them distort the price.

**VERIFY:** a ground-only and an all-target weapon with identical ground profiles differ
by `targets_factor` alone, not twice.

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
W3, W4 and W5 all left this list **byte-identical** — verified after each.

**W5 added a second baseline, `k_context`** (K × targets × range × deadzone):

`Storm 2.23 · Flame 1.89 · Plasma 1.86 · Concussion 1.84 · Thermobaric 1.83 ·
Chemical 1.82 · Demolition 1.81 · Flak 1.77 · Quantum 1.70 · CannonHE 1.64 ·
CannonAP 1.61 · MissileHE 1.51 · Sonic 1.49 · MissileAP 1.46 · Magic 0.91 ·
Bullet 0.78 · Prism 0.76 · Tesla 0.74 · Railgun 0.69 · Laser 0.54`

The ORDER changes against bare K, which is the point: **Flak overtakes Quantum** and
**MissileHE overtakes Sonic** because they can hit air and the others cannot. Under bare
K an AA-capable and a ground-only weapon were indistinguishable.

Constants added by W5: `TARGETS_FLOOR 0.5` · `RANGE_WEIGHT 0.25` ·
`RANGE_BOUNDS (0.75, 1.50)` · `DEADZONE_WEIGHT 1.0` · median weapon range **6000**
(measured over 2364 weapons that declare a Range).

---

## 5b. TOOLING AVAILABLE TO AGENTS (2026-08-11)

- **`gh` CLI 2.97.0 is installed** at `C:\Program Files\GitHub CLI\gh.exe`. It is NOT on
  the default PATH for a fresh shell — prepend it:
  `export PATH="$PATH:/c/Program Files/GitHub CLI"`. Use it for PR review comments, CI
  status and opening PRs instead of hand-rolling `curl` against the REST API.
  ✅ **Authenticated 2026-08-11** as `AedisToru`, scopes `gist, read:org, repo, workflow`;
  the repo resolves to `Zeruel87/Cameo-mod @ master`. `gh run list`, `gh pr view
  --comments` and `gh api` all work without further setup.
- **`openpyxl` 3.1.5** is present on the maintainer's Windows box, so `audit_balance_sheet`
  produces a real report here. It was MISSING on the Linux box that ran PR #251, which is
  why that PR committed `balance_sheet.md` as the 46-byte string "openpyxl not installed".
  **Any agent running the suite on Linux must `pip install openpyxl` first** or it will
  silently commit a degraded report.
- **`.github/dependabot.yml`** keeps the SHA-pinned actions fresh (weekly, grouped into
  one PR). Version updates only activate once it is on the default branch of the hosted
  repo — it does nothing while unpushed.

## 6. LINKS

`EFFECTIVE_DAMAGE.md` (the metric) · `BALANCE_PIPELINE.md` (the loop) ·
`FORMULA_V2.md` (the laws) · `PHYSICAL_STATE_SYSTEM.md` (meters) ·
`SPREAD_FALLOFF_PLAN.md` (falloff shapes) · `WEAPON_3WAY_SPLIT.md` (the split) ·
`ROADMAP.md` (everything else) · `AI_HANDOFF_2026-08-05.md` (agent letters)
