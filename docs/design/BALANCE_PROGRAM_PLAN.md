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
| **W2** | `^LightFlameWeapon` → 3-way split + new `^Warhead_Inferno_*` family | ⚠ **ABANDONED by Devin (30 live weapons left); LOCK RELEASED by the maintainer 2026-08-15 — set B is FREE** | Claude | — |
| **W3** | Ledger split: raw stays, derived moves to `docs/balance/derived/` | ✅ DONE | Claude | W1 |
| **W4** | Retire weapon-class K; charge-up becomes an ACTOR property | ✅ DONE | Claude | W1 |
| **W5** | Missing metrics: overkill/TTK, range advantage, ValidTargets, MinRange, AttackDelay | ✅ DONE | Claude | W1 |
| **W6** | C# `ModifiesCombatProportionalToPhysicalState` (+ pitch/glow hooks) | ✅ DONE `fc45a9632` | Claude | — |
| **W7** | Sonic → `Resonance` meter (no new C# needed) | ⬜ READY | either | — |
| **W8** | Gatling ladder → `SpinUp` meter | ✅ DONE `c0d6abf70` — all 43 actors, `GattlingSpeed` = 0 | Claude | W6 ✅ |
| **W9** | `^Poisonable` → `Poison` meter (gas-cloud dose-response) | ⬜ READY | either | — |
| **W10** | `^Blindable` → `Blind` meter | ⬜ READY (unblocked by W6) | either | W6 ✅ |
| **W11** | Wire K into `fit_class.py` behind a flag; fit one class both ways and compare | ✅ BUILT, sign-off owed (+2 pipeline bugs fixed: 43% of the roster priced at zero DPS) | Claude | W3 ✅, W4 ✅, W5 ✅ |
| **W12** | Superweapon balancing as a SEPARATE track (not unit-priced) | ⬜ READY | maintainer-led | — |
| **W13** | Warhead system rebuild from the 3150-profile reference corpus | 🔵 steps 1-4a DONE — **the measured profiles are LIVE** on all 10 sourced families (+ 8 blends); 4b = the 10 INVENTED families | Claude | W1, W5 |
| **W14** | ~~Renormalise `avg_versus`~~ — ✖ DROPPED, the multi-role premium is intended; folded into W13 rule 8b | ✖ DROPPED | — | — |
| **W15** | `%`-twin fix + `reference_hp` → 200 000 — **PREREQUISITE for W17** | ✅ DONE | Claude | — |
| **W16** | Charge-up discount PROPORTIONAL to real charge share (supersedes W4's flat 0.75×) | ✅ DONE | Claude | W4 ✅ |
| **W17** | ~~Remove the 2000-damage grid~~ (done as a 100 grid in W15); retire FirepowerMultiplier as a fine-tuning knob | 🔵 TOOLING DONE `451e10a63`; **content half NOW UNBLOCKED** | Claude | W15 ✅ |
| **W18** | Roll the 0.1% percentage unit out into yaml (`PercentageDenominator: 1000`, ×10 the values) | ⬜ READY (set B free) | Claude | W15 ✅ |
| **W19** | Collapse the 195 `SpreadDamage` ExtraDamage chips into the main warhead (KEEP the 34 sniper `OpenToppedDamage`) | ⬜ READY (set B free) | Claude | W13 |
| **W20** | Multi-armor combination rule (engine MULTIPLIES → squares the profile); mechanism + switch | ⬜ MECHANISM DONE, rule = maintainer | Claude | — |
| **W21** | Layered health Shield → Integrity → Armor → Health, layer-aware armor (solves W20 structurally) | ✅ BUILT + LIVE `ab467fe52` | Claude | — |

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

⚠ **SET B'S LOCK IS RELEASED (maintainer, 2026-08-15): "you can release his lock since
Devin will not come back anytime soon".** Devin's W2 stopped on 2026-08-13 with 30 live
weapons still inheriting `^LightFlameWeapon`. Claude owns set B from now on, which
unblocks **W13 step 4, W17's content half, W18, W19 and W7** in one stroke — those were
the only things waiting on it. Finish W2's remaining 30 weapons as part of W13 step 4
rather than as a separate item; they need the same regeneration anyway.

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

### W2 — `^LightFlameWeapon` → 3-way split + `^Warhead_Inferno_*` 🔵 IN PROGRESS (Devin, 2026-08-11) · owner **Devin**

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
- [x] `Inferno` in the generator; `splice_templates.py inferno`; drift back to 1.
- [x] Every weapon in the explicit mapping table above inherits exactly ONE `^Warhead_*`,
      ONE `^Projectile_*`, ONE `^Effect_*`.
- [ ] `^LightFlameWeapon` has zero remaining inheritors, then is deleted
      (38 matches remain, almost all multi-family mixed weapons or human-live/ASK files).
- [x] `tools/audit/review_resolve_diff.py` run before/after on a sample of ≥10 of the
      77 — the ONLY expected change is that flame damage now lands (verified on 10).
- [x] `find_empty_warhead.py` = 0.
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

⚠ **CORRECTED 2026-08-15 — this item's own end-points were wrong, and building from
them would have inverted a stat on all 47 actors.** The line below used to read
"`1.02¹⁰ = 1.219` range/**speed** … max → 60% reload / 122% range / **122% speed**".
Verified against `defaults.yaml`: **all 30 `SpeedMultiplier` entries in
`^GatlingSpeedUpUnitBehavior` are `95`, not `102`.** A spinning-up gatling unit gets
**SLOWER**, not faster — which is the better design anyway (you root yourself to gain
fire rate), and it is what the mod has always shipped. The spec had silently copied the
range direction onto speed.

Current ladder resolves to `0.95¹⁰ = 0.599` reload (fire rate ×1.67),
`1.02¹⁰ = 1.219` range, and `0.95¹⁰ = 0.599` speed — those are the **end-points** the
meter must reproduce (0 → 100%, max → **60% reload · 122% range · 60% speed**). The
turret template has no speed term at all; only the unit one does. Elite variant fills faster
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

### W11 — Wire K into `fit_class.py` ✅ BUILT · ⬜ awaiting maintainer sign-off

W3/W4/W5 were all ✅ long before anyone re-read this line — the ⛔ was stale, which is
why this sat "blocked" while its dependencies were done.

**Built:** `--use-k` prices on the K-adjusted `effective_dps` from the derived sidecar
(accuracy, spread, falloff, range, dead zone, reachable targets) instead of raw
damage/reload; `--compare-k` prices the class BOTH ways and writes the evidence report.
K is read from the sidecar, never recomputed, so there is one definition of it. The
anchor is re-fitted in whichever mode is running — pricing members on K against an
anchor fitted on raw DPS would compare two scales and make every delta meaningless.
`--compare-k` deliberately writes **no candidate anchor**: it is a report, not a fit.

**⚠ TWO PIPELINE BUGS FOUND BY ACTUALLY RUNNING IT** — both pre-existing, both far more
consequential than the flag:

1. **43% of the roster was invisible to pricing.** `unit_inputs` skipped every armament
   carrying any `requires` at all. But `!rank-elite` is the BASE weapon, not an
   upgrade gate — as is `!forgotten_upgrade_chemicalweapons`, and so on. **371 of 863
   actors with priced armaments came out at zero DPS** and dropped out of class fits
   entirely, `tiger.nax` — the recorded `mbt` anchor — among them, which is why fitting
   `mbt` failed outright. Replaced with `formula.condition_holds_by_default()`: evaluate
   the condition with every named condition FALSE, i.e. *the weapon the unit fires as
   built*. Coverage **57% → 96%**; the 37 still at zero genuinely have no as-built weapon
   (transport- and deploy-gated). 18 unit tests, and it fails CLOSED on an expression it
   cannot parse — a wrong price looks authoritative, a missing one does not.
2. **The class-member scan never ran.** The anchor was unioned into `actors_filter`, and
   a non-empty filter switches off the `design.class_anchor == cls` branch — so every
   run collected exactly ONE unit (the anchor) and wrote a one-row validation table for
   the whole class. The anchor now passes through `always=` instead.

**First result — `docs/balance/derived/k_comparison_mbt.md`, 40 units:**
median price shift **+1.2%** (range −50% … +43%), but it moves prices AWAY from current
cost for **30/40** units. Individual movements are large and plausible in direction
(`protoss_dragoon` −51%, `tkm_trenchtank` +43%). Sanity check passed: the raw anchor
reproduces the documented Tiger identity exactly, O0 = P0 = Q0 = cost0 = 800.

**⚠ That result does NOT justify flipping the pipeline yet**, and the honest reading is
that it cannot on its own: current costs are themselves unbalanced — that is why this
program exists — so "moves away from current cost" is not automatically evidence
against K. What would settle it is running `--compare-k` on a class whose costs the
maintainer already considers CORRECT, and checking whether K pulls those towards or
away from them.

**VERIFY:** `python tools/balance/fit_class.py --class mbt --anchor tiger.nax --compare-k`
→ report in `docs/balance/derived/`, `class_anchors.json` untouched. Sign-off still owed
in `anchor_decisions_log.md` before `--use-k` becomes the default.

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
| field median profile span | **85** raw · **100** counting damage warheads only (Cameo: Light 90 · Medium 75 · Heavy 60 · Super 45) |
| field distribution | ⚠ **CORRECTED 2026-08-15 — see the box below.** Raw: 65% sharp · 7% moderate · 27% flat. **Damage warheads only: 84% sharp · 9% moderate · 7% flat** |
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
3. ⚠ **REVISED 2026-08-15 — the field is far sharper than this rule assumed.**
   The original rule read *"most warheads SHARP; **~20% intentionally FLAT**, the field's
   own ratio"*. That 20% was an artifact of counting warheads that carry **no damage at
   all**: 182 corpus rows are ALL-ZERO and 186 more peak at ≤5 — death animations
   (`AvatarDeathWH`), dummies (`BioDummyWH`), repair guns, de-evolution and EMP-only
   effects. A zero profile has span 0, so every one of them was filed as a "flat
   all-rounder". They are plumbing, not design.
   Excluding them (`cluster_versus.py`, `DAMAGE_FLOOR`), the real field ratio is
   **84% sharp · 9% moderate · 7% flat** — flat is roughly **a third** as common as the
   rule assumed. So: keep flat as a deliberate, RARE identity (Sonic, Magic, Tesla) at
   under 10% of families, and make everything else genuinely sharp. MO still proves both
   extremes can coexist without a mushy middle.
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
9. ✅ **Prerequisite — the `%`-twin. SATISFIED by W15.** The twin used to be
   `per // DAMAGE_STEP` (integer division), so every percentage warhead silently became 0
   below one grid step — hard immunity by rounding. `formula.percentage_twin` now rounds
   half-up and never falls below 1. The grid moved only after that landed.
10. ✖ **VOID — `FirepowerMultiplier` does NOT survive.** This rule was written before the
    maintainer's 2026-08-11 ruling that **no weapon is shared**, which removed its entire
    premise ("one weapon serves many actors"). The knob is retired; see **W17**.

**PROGRESS (2026-08-15) — step 1 of W13 is DONE: the corpus is clustered.**
`tools/reference/cluster_versus.py` → `docs/reference/versus_archetypes.md`. It places
**1876 damage profiles into 85 archetypes** (Cameo occupies 14) keyed on
`macro order x sharp/flat x HE/AP`, and reports the **median profile WITHIN each cluster**,
never a global average (rule 4). The biggest occupied archetypes, with the number of
independent mods backing each:

| archetype | n | sources | median span | INF | VEH | BLD |
|---|--:|--:|--:|--:|--:|--:|
| `INF>VEH>BLD sharp HE` | 345 | 14 | 100 | 100 | 47 | 23 |
| `INF>BLD>VEH sharp HE` | 250 | 14 | 100 | 100 | 47 | 63 |
| `VEH>BLD>INF sharp AP` | 114 | 13 | 90 | 25 | 92 | 63 |
| `VEH>INF>BLD sharp HE` | 82 | 10 | 110 | 60 | 115 | 24 |
| `BLD>INF>VEH sharp HE` | 71 | 10 | 150 | 110 | 77 | 197 |

⚠ **The AIR axis is NOT measurable from this corpus and must not be faked.** Only **37 of
1876** profiles define any aircraft armor at all: the source engines share one armor type
between aircraft and ground vehicles. That is precisely why Cameo's four dedicated aircraft
armors are an improvement (rule 8) — and it means each archetype's air POSITION is a
maintainer design decision, with the corpus contributing nothing. The tool says so in its
own output rather than emitting an invented number.

**STEP 4a — SHIPPED. The measured profiles are live in `weapons.yaml`.**

The even ramp is gone from every family the corpus can speak for. `table()` in
`gen_weapon_template.py` survives only as the fallback for the families Cameo invented.

| piece | where |
|---|---|
| frozen data | `docs/reference/family_profiles.json` — 10 families x 3 levels, `blend` aggregation, provenance (`n`, `mods`, `origin`) per cell |
| exporter | `propose_family_profiles.py --json` |
| consumer | `gen_weapon_template.reference_main()` — order still from `build_order()` |
| impact report | `tools/balance/report_versus_change.py <rev>` |

**Why the data is FROZEN into a committed JSON rather than derived at generation time:**
`survey_platforms.py` traces the source mods' INI files out of `~/Downloads`. Nobody else
has those, so a generator that imported the derivation would only run on one machine.

**Measured result:** 51 warhead tables changed. Profile SPAN (the counter-play) went from a
uniform 60/75/90 to **72–268**. Mean lethality moved **1.25x** on average (0.79x–2.04x), and
across 2436 live armaments K moved **median 1.07x, mean 1.16x** (0.88x–1.98x). 36% of
armaments did not move at all — those are the ~878 legacy nodes still declaring inline
`Versus` on `SpreadDamage` (item A5), which the templates do not reach.

⚠ **That K shift is not yet paid for.** `Damage` still has its old values, so a family whose
mean rose 1.4x currently deals 1.4x. The correction is `apply_balance --confirm`, which needs
a maintainer order (CLAUDE.md rule 3). Until then the tree is deliberately mid-pipeline.

**Two rules were CORRECTED by running this** (both now in DESIGN.md):
- **§12.0b Heroic/Airborne divide by the profile's PEAK, not by 100.** The two stopped being
  the same thing when normalisation moved to the median. Dividing by 100 with a parent at 137
  AMPLIFIES: `Bullet_Light` gave `Plate 137 · Scout 106 · Heroic 145` — heroes softer than
  either half, the exact inversion §12.0b exists to prevent. **36 of 60** derived cells.
- **§12.0 rule 1 said "peak is 100"** and the tooling had already moved to the median. Doc
  fixed to match the artifact.

**Two things deliberately NOT done here:**
- **`Airborne` is computed but NOT emitted.** Its column would make 17 armors share the
  %-twin's 16-wide window, where "no two identical" can only ever be the even ramp. Opening
  that window is **W18**, and W18 must land as ONE change (denominator + x5 values) or every
  %-twin deals a fifth or five times. `Airborne` ships with W18. ⚠ Also: `Jumpjet` is already
  a **TerrainType** (`mods/cameo/bits/d2k/arrakis.yaml`) — a reason to keep `Airborne`.
- **`--spread-flat-blocks` left OFF.** 24 of 30 family-levels have a macro block the corpus
  left flatter than 20 points (worst: `CannonAP` vehicles spanning 7–8 across five rungs).
  Widening them is DESIGN, not measurement, and it can push a block past its macro neighbour
  and break the ordering law — so it stays a per-family maintainer call.

**NEXT (step 4b):** the 10 families with NO reference coverage — `Flak`, `Chemical`, `Melee`,
`Arrow`, `Magic`, `Demolition`, `Concussion`, `Sonic`, `Railgun`, `Nuclear`. `Magic` (PCT) and
`Sonic` (FLAT) are already mode-designed and `Nuclear` is `HAND_TUNED`, so **7 sloped ladders**
need inventing with reasoning + the spreadsheet the maintainer asked for. They still carry the
even ramp and the OLD floors (10/25/40), which contradict §12.0's 10–25 band — resolve there.

**VERIFY:** `python tools/reference/extract_versus.py --summary` → 16 sources, 3150 rows;
`python tools/balance/verify_generator_sync.py` → drift = 1 (`^Warhead_Sniper_Light`);
`python tools/balance/report_versus_change.py <rev>` → the profile diff.

---

### W14 — ✖ DROPPED as specified; folded into W13 rule 8b

**Original claim (mine, wrong):** ground-only weapons are double-charged because the
aircraft armors are averaged into `avg_versus` AND discounted again by `targets_factor`,
so `avg_versus` should be renormalised over reachable armors only.

**Maintainer's objection (2026-08-11, correct):** that is the INTENDED mechanism. Low air
multipliers *should* pull `avg_versus` down and make a ground-only unit cheaper than a
multi-role one. Renormalising would delete the multi-role premium — which is exactly the
thing we want the pricing to express.

**Measured, which settles it:**

| | n | mean K | mean air-Versus | targets_factor |
|---|---|---|---|---|
| AA-capable | 868 | 0.790 | 51.9 | 1.000 |
| ground-only | 1104 | 0.955 | 43.4 | 0.890 |

The Versus route contributes **~1%** (8.5 points of air-Versus at air's 10% engagement
weight); `targets_factor` contributes **-11%**. The overlap is a rounding error, so there
is no double-count worth fixing — and renormalising would have removed the good mechanism
to chase a negligible one.

**The real finding, now W13 rule 8b:** ground-only weapons average **43.4%** against
aircraft, i.e. the ordering law is NOT yet pushing air to the bottom for them. The
maintainer's natural-pricing mechanism cannot bite until W13 sets air values properly per
archetype — last in the order, genuinely low, never zero. Re-check the interaction with
`targets_factor` AFTER that, not before.

(Note also that K is dominated by splash footprint, not Versus: the ground-only
population scores HIGHER K than the AA-capable one because it is full of artillery.)

---

### W15 — `%`-twin fix + `reference_hp` 200 000 ⬜ READY · **blocks W17**

Two maintainer rulings, both about how percentage damage is valued.

**1. The `%`-twin cannot survive an off-grid Damage value.**
`formula.distribute_damage` computes it as `per // DAMAGE_STEP` — integer division:

| Damage | twin | effect |
|---|---|---|
| 2000 | 1 | fine |
| 1999 | **0** | the %-warhead silently does NOTHING — hard immunity by rounding |
| 3500 | 1 | same as 2000 — the twin stops tracking damage |

So freeing the grid (W17) before fixing this silently zeroes every percentage warhead
under 2000 damage. Fix the derivation first (float, or a scale that is continuous in
Damage), then remove the grid.

**2. `reference_hp` is a DESIGN constant of 200 000, not a measured median.**
Maintainer 2026-08-11: percentage damage must be priced as if fired at an average
BASELINE actor, and 200 000 HP is the right middle — high-tech tanks, dreadnoughts and
epics all sit well above it, everything else below.

`target_model.reference_hp()` currently MEASURES 74 000 (engagement-weighted median of
the live roster). Overriding it to 200 000 makes every %-twin worth ~2.7x more in K, so
this is a real model change: expect the family table to move and say so in the commit.
Keep the measured value available as a diagnostic — the gap between "what the roster
actually is" (74 000) and "what we price against" (200 000) is itself information.

**DONE WHEN** the twin is continuous in Damage; `reference_hp` is the design constant
with the measured one still reportable; the family-table shift is recorded in §5.

**✅ DONE** — `formula.percentage_twin()` replaces `per // DAMAGE_STEP`: same 1-per-2000
design ratio, rounded half-up, floored at 1 for any live warhead, monotone in Damage.
Rounding is explicit rather than `round()`, whose banker's rounding sends 5000 → 2 but
7000 → 4. The engine's Damage field for a percentage warhead is an INTEGER percent of
max HP, so 1 point remains the finest step available — the derivation is continuous, the
engine's resolution is not, and going finer would need a scale field on
`AreaDamagePercentageWarhead` (not done; flag it if a design ever needs sub-1% twins).

`target_model.REFERENCE_HP = 200_000` is now a plain constant; the measured figure moved
to `measured_reference_hp()` and is still printed by the family table, the
`target_model` report and the derived ledger (`reference_hp_measured`). A test asserts
the measured value stays BELOW the constant — if the roster ever catches up, the constant
has stopped being the middle it was chosen to be and wants a re-ruling.

**3. ✅ THE BASIS-POINT REGRID (maintainer order 2026-08-11/12).**
*"Flat damage steps of 100 and percentage is always 0.01% for each 100 flat damage since
that seems very easy to remember. Now we can increase all the versus values for the
percentage warhead to 5x … 20 to 100 … steps of 5."*

**The law, in one sentence: 100 flat damage == 0.01% of max health.** So the twin is
literally `Damage / 100` and one step of either grid is one step of the other — it cannot
drift from the weapon it belongs to.

| | before | after |
|---|---|---|
| flat damage grid | 2000 | **100** (20x finer) |
| percentage twin unit | whole percent (1%) | **basis point (0.01%)** |
| base ratio | 1% per 2000 damage | **1% per 10000 damage** (5x weaker) |
| percentage-warhead Versus | 1..17, steps of 1 | **multiples of 5 in [5, 100]** (5x larger) |

The 5x weaker base and the 5x larger Versus **cancel exactly**, so total percentage damage
is unchanged: `16000 damage → 160bp (1.60%) × Versus 85` is the same as
`16000 → 8% × Versus 17`. What is bought is resolution *in both dimensions at once* — the
twin now separates every flat step, and Versus moves in clean 5s away from the cramped
1..17 band where a single integer step was a 100% jump at the bottom.

⚠ **THE TWO HALVES ARE ONE CHANGE.** `DAMAGE_PER_PERCENT` (2000 → 10000) without the
Versus x5 makes every percentage twin deal **a fifth** of its damage; the Versus x5 without
the ratio makes it deal **five times**. Never land one alone — see W18.

- C#: `AreaDamagePercentageWarhead.PercentageDenominator` — a DENOMINATOR, not a
  multiplier (it sits beside `IntegrityScale`/`PhysicalStateScale`, which scale UP;
  the `[Desc]` says so explicitly). `100` = whole percent = the engine convention and
  the **default, so no existing weapon changes behaviour**; `10000` = basis points.
  Validated at load through a new `AreaDamageWarhead.ValidateFields()` hook —
  implementing `IRulesetLoaded<WeaponInfo>` in the subclass instead would REPLACE the
  base's explicit implementation, leaving `effectiveRange` unbuilt and every ring empty.
- Tools: `formula.DAMAGE_STEP = 100`, `DAMAGE_PER_PERCENT = 10000`,
  `BASIS_POINT_DENOMINATOR = 10000`, `PERCENTAGE_VERSUS_STEP = 5`;
  `percentage_twin(per, denominator)` takes the unit from the node, `twin_denominator()`
  reads it from the ledger record, and `extract_stats` records
  `percentage_denominator` **only when the node states it**, so ledgers of weapons still
  on the default diff empty.

⚠ **The unit is threaded, never assumed** — writing whole percent into a basis-point node
(or the reverse) is a silent 100x error in a number nobody re-reads.

**Which 17-step Versus window?** The maintainer picked **20..100**. Recorded, with one
caveat for W13 to settle: 20..100 has a best/worst ratio of **5:1**, where the exact x5
rebase (5..85) keeps today's **17:1**. A 5:1 profile is a GENERALIST — the direction W13
is explicitly moving away from (field median span 87; "each warhead more specialized").
**Recommendation: make the STEP the law (multiples of 5) and the WINDOW a per-family
choice** — 5..85 for the sharp families, 20..100 for the intentional generalists (Magic,
Sonic, Tesla, which the maintainer has already named as such). Both windows are equally
clean to remember; only the sharpness differs.

⚠ **The yaml rollout is NOT in this commit — see W18.** The mechanism is live and inert:
nothing writes `PercentageDenominator: 1000` yet, so every weapon still behaves exactly
as before.

---

### W16 — Charge-up proportional to real charge share ⬜ READY · supersedes W4's flat rate

W4 applied a flat **0.75x** to every charging actor. Measured, that is too blunt:

| actor | trait | charge | reload | share of cycle |
|---|---|---|---|---|
| Obelisk of Light (TD/TS) | `AttackCharges` | ChargeLevel **50** | — | the heavy case the ruling was written for |
| RA1 Tesla Coil | `AttackTesla` | InitialChargeDelay **25** | 100 | **20%** |
| AsianAlliance railtower | `AttackTesla` | **12** | 120 | **9%** |
| **RA2 Tesla Coil** | `AttackTesla` | **22 (engine default)** | 75 | **23%** |

Maintainer ruling 2026-08-11: *"AttackTesla doesn't have the long charge time of the
Obelisk … it's very fast, so this needs to be taken into account."* ⚠ CORRECTION 2026-08-11: an earlier draft read the RA2 Tesla Coil as having NO charge
delay. Wrong — `InitialChargeDelay` is simply not written on the actor, so it takes the
ENGINE DEFAULT of 22 (`AttackTesla.cs:31`). An absent key means default, never zero.
Re-measured, the RA2 Tesla Coil has the HIGHEST charge share of the Tesla group (23%),
not the lowest. The ruling stands and is now better supported: Tesla charges are real
but SHORT relative to the Obelisk's 50, so they earn a smaller discount, not none.

⭐ **THE ANCHOR IS A LAW, NOT A BUILDING** (maintainer 2026-08-15, "some nice ratio …
might be more consistent"):

> **A unit whose charge is 50% of its reload earns the full 0.75× discount.**
> Reload 100, charge 50. As a share of the whole cycle that is `0.5 / 1.5` = **1/3**.

`formula.CHARGE_ANCHOR_SHARE = 1/3`. The Obelisk sits at 50/(50+96) = 34.2%, just above
the line, so it still anchors at 0.75 and **nothing moved**: measured across the 11
chargers with a real share, spread 0.198 against the old accidental anchor's 0.199.

⚠ **A 25%-of-reload anchor (share 20%) was measured and REJECTED** — it puts **7 of 11**
chargers on the 0.75 floor instead of 5, erasing most of the differentiation this item
exists to create. Clean is good; clean and flat is not.

Optional tidy-up, NOT done (it is a weapon balance number and belongs in the pipeline):
`td_nod_obeliskoflight`'s weapon reload 96 → 100 would make the anchor unit sit exactly
ON the law at 33.3% instead of clamping from just above it.

**Model:** `charge_share = charge / (charge + reload)`, discount scaled so the anchor
share earns the documented 0.75x and a zero-charge actor gets exactly 1.0, clamped to
[0.75, 1.0]. This also RESOLVES the open Tesla question: `AttackTesla` can now join
`CHARGE_UP_TRAITS` safely, because the model gives each actor the discount its real
charge burden earns instead of a binary in/out. Retire `CHARGE_UP_EXCLUDED_TRAITS`.

**VERIFY:** Obelisk == 0.75 (anchor); railtower (9%) closest to 1.0; RA2 Tesla (23%) and
RA1 Tesla (20%) in between. Read charge values from the RESOLVED actor INCLUDING engine
defaults — `InitialChargeDelay` defaults to 22.

**✅ DONE. Measured across all 14 charging actors in the tree:**

| actor | trait | ticks | cycle | share | multiplier |
|---|---|---|---|---|---|
| `td_nod_obeliskoflight` | AttackCharges | 50 | 96 | 34.2% | **0.750** (anchor) |
| `ra2_soviets_teslacoil` | AttackTesla | **20** | 75 | 21.1% | 0.846 |
| `ra1_soviets_teslacoil` | AttackTesla | 25 | **106** | 19.1% | 0.861 |
| `wc2_*_siegeengine` | AttackFrontalCharged | 20 | 100 | 16.7% | 0.878 |
| `asianalliance_railtower` | AttackTesla | 12 | **160** | 7.0% | **0.949** |

`ts_nod_obeliskoflight` (45.5%) clamps to 0.75, proving the clamp.

⭐ **`AttackTesla` OVERRIDES THE WEAPON'S RELOAD** (maintainer 2026-08-15): *"if you have
the AttackTesla trait, ReloadDelay is taken from that instead of from the weapon, and the
reload delay from the weapon counts as the burst delay in the formula."* The coil winds up
once, fires `MaxCharges` zaps, and the WEAPON's reload is the gap between them — the burst
law verbatim, `eff_reload = trait ReloadDelay + weapon reload × (MaxCharges − 1)`:
RA1 = 100 + 3×2 = **106**, railtower = 120 + **10**×4 = **160**, RA2 = **75** (one charge).

⚠ **`ChargeDelay` is NOT the gap.** An earlier draft used it and was right twice by
coincidence — it defaults to 3, and both Tesla Coils happen to carry weapons that also
reload in 3. The AA railtower's weapon reloads in **10**, and only the railtower exposed
the error (132 against the correct 160). Two agreeing data points proved nothing.

⭐⭐ **THE REAL PRIZE: an 11.8× DPS OVERSTATEMENT.** Because a Tesla Coil's weapon reloads
every 3 ticks, `unit_inputs` was pricing the coil as firing 20 times a second when it
fires 3 zaps per 106 ticks. DPS drives the price, so every `AttackTesla` actor was priced
off a number ~12× too large. `formula.charge_attack_cycle` now returns the cycle and
shots-per-cycle for any trait that overrides the weapon, and `fit_class` prices on that.

⭐ **This FLIPS the Tesla ordering, and the flip is the point.** RA1 charges LONGER (25 vs
20) yet ends up with the SMALLER share (19.1% vs 21.1%), because its three zaps stretch
the cycle while the single-charge RA2 coil stays at 75. **Charge share is a ratio, not a
duration** — a fact no flat rate and no charge-time-only reading could ever express.

Charge times are now a DECISION rather than a leftover: RA1 stays 25 and the RA2 coil
writes `InitialChargeDelay: 20` explicitly instead of inheriting the engine's 22. `CHARGE_UP_EXCLUDED_TRAITS` is retired to an empty set and `AttackTesla`
joins `CHARGE_UP_TRAITS`, as the item asked.

⚠ **The cycle for `AttackTesla` is its OWN `ReloadDelay`, never the weapon's.** A Tesla
Coil's armaments reload every 3 ticks (`ChargeDelay`), so using the weapon would read as a
~90% charge share and hand it the full discount for nothing. The `ChargeLevel` family has
no reload of its own and falls back to the LONGEST base-weapon reload — longest, because a
charge gates the heavy shot, and the Terran siege tank's fast 37-tick secondary next to its
sieged 148 would otherwise fake a huge share.

⚠ **An actor whose charge cannot be measured keeps the flat 0.75, not 1.0** (2 of the 14:
`ra1_allies_mobileradarjammer`, `terran_siegetank` — both have only condition-gated weapons,
so there is no base reload to measure against). It charges; we just cannot see by how much,
and pricing it as if it did not charge is the larger error — a price cut is a BUFF in value
terms, so over-paying is not the safe default.

⚠ **SEPARATE DEFECT FOUND AND GUARDED: a `--faction` extract silently staled 30 derived
files.** `extract_stats --faction X` rewrites the GLOBAL `derived/_model.json` (its armor
census and weights are measured across the whole roster) but regenerates only X's sidecar —
so every other faction's `avg_versus`, `k` and `effective_dps` keep being computed against
the old model. Nothing caught it: `audit_balance_drift` compares raw yaml to the RAW ledger
and never looks at derived. Fixed here by a full re-extract (verified idempotent: a second
run changes nothing), and `extract_stats` now prints a loud warning after any filtered run.

---

### W17 — Retire FirepowerMultiplier 🔵 TOOLING DONE (2026-08-15) · content half ⛔ set B

⚠ **Partly superseded by W15's regrid.** The maintainer chose a **grid "for sanity"** (100,
`formula.DAMAGE_STEP`), not free-valued Damage, so "remove the grid" is now "the grid is 100
and the %-twin tracks it exactly". What remains of W17 is the SECOND half: retiring
`FirepowerMultiplier` as a fine-tuning knob, which the finer grid makes possible.

⚠ My earlier objection — "keep FP because one weapon serves many actors" — is **VOID**.
Maintainer 2026-08-11: **no weapon is shared; every vehicle has its own unique weapon
defined.** So FP has no remaining pricing role at all. (This also voids **W13 rule 10**,
written before that ruling.)

**MEASURED before changing anything** (`plan_firepower_retirement.py`, the whole roster):
1322 main warheads across **152 actors** carry an unconditional FP. Folding the multiplier
into `Damage` and snapping back to the grid leaves **1144 exact**, **1214 within 1%**, and
**108 needing a damage decision**. The residual is not the argument for retirement on its
own — the argument is that the 1% band is the step the retired knob itself moved in.

⚠ **The 108 are not trims.** They cluster on actors whose FP is a SCALE, not a fine-tune:
`futuretech_cryocopter` 0.12, `protoss_voidray` 0.09, `ra1_soviets_ak47conscript` 0.14,
`ra2_soviets_conscript` 0.19. A multiplier that far from 1.0 means the actor is firing
another unit's weapon at a fraction of its written damage; the grid cannot express the
result, so those need a real damage decision rather than a fold.

**TOOLING HALF — DONE (set A):**
- [x] `propose_class_rebalance.decompose_dps` solves on `formula.DAMAGE_STEP` and returns a
      multiplier of **1.0**, always. It also stopped using the stale hard-coded 2000 grid.
- [x] The two `over_priced` dead-ends no longer emit `2000, 0.05`. The floor is
      deliberately identical: one step at fp=1 is the same 100 effective damage.
- [x] `unique_dmg_per_shot` nudges **Damage in grid steps** instead of walking FP in 1% steps.
- [x] `apply_balance` cannot WRITE the knob: `firepower_multiplier` moved from
      `UNIT_FIELDS` to `RETIRED_UNIT_FIELDS`, a ledger/yaml disagreement is REPORTED, and
      the `set_field` branch that could MINT a missing `FirepowerMultiplier:` block is gone.
- [x] The report flags `fp-debt` and orders **"DELETE the unconditional
      FirepowerMultiplier"** — prescribed Damage is solved at fp=1, so a surviving trait
      would scale it a second time. (The old code overwrote the trait, so this instruction
      is new and load-bearing.)
- [x] `tools/tests/test_firepower_retired.py` — 12 tests pinning both halves.
- [x] `extract_stats` still READS FP and `fit_class` still prices with it. It must: 152
      actors still carry one, and un-pricing them would misprice the roster.

**CONTENT HALF — blocked on set B** (`mods/cameo/weapons/**`, Devin's while W2 runs).
Worklist: `docs/balance/firepower_retirement.md`. Per actor: write `Damage x FP` snapped to
the grid on every main warhead, then DELETE the trait; boot-gate per batch. Conditional
(upgrade) FP traits are design and are NOT touched.

Versus values keep integer steps of 1 and the ordering law, but the floor may sit
anywhere without tier restriction (W13 rule 5).

**VERIFY:** `python tools/balance/plan_firepower_retirement.py` → 0 actors, once done.

---

### W18 — Roll the basis-point unit out into yaml ⛔ BLOCKED on set B (Devin, W2)

W15 shipped the MECHANISM; this ships the CONTENT. Blocked purely by file ownership:
every file involved is set B (`mods/cameo/weapons/**`, `ContentPacks/**/weapons.yaml`),
which Devin holds while W2 runs. **Do not start this until W2 lands** — §2 is not advisory.

Measured scope (2026-08-11, `Warhead@*Percentage` nodes carrying an explicit `Damage`):

| warhead type | explicit Damage | inherits Damage | can go per-mille? |
|---|---|---|---|
| `HealthPercentageDamage` (stock) | **2611** | 135 | ✗ — no such field; must migrate type first |
| `AreaDamagePercentage` (Cameo) | **182** | 1 | ✓ |

**Order of operations** (each step boot-gated; the whole thing is behaviour-preserving):
1. `gen_weapon_template.py` emits `PercentageDenominator: 10000` on every `_Percentage`
   twin, `pct_damage = damage // 100` (2000 damage = `20` = 0.20%), **and the x5 Versus
   band in multiples of 5** — all three together, never separately.
2. Regenerate the shared templates; `verify_generator_sync.py` drift back to its
   expected value. ⚠ This rewrites `mods/cameo/weapons/weapons.yaml` — **set B**.
3. Restate every explicit twin `Damage` on a node that just gained the finer unit
   (old whole-percent `N` → `N × 20` basis points, since the base ratio also fell 5x).
   A unit change, NOT a balance change: assert the resolved percentage damage is
   identical before/after with `tools/audit/review_resolve_diff.py`.
4. Migrate the 2611 stock `HealthPercentageDamage` nodes to `AreaDamagePercentage`
   (already documented as a behaviour-preserving drop-in) and restate them too.

⚠ **A node on the stock warhead CANNOT hold the new ratio** — whole percent rounds 1.60%
to 2%, a 25% error. Until step 4 lands, those 2611 nodes keep the old ratio and the old
Versus; the two systems must not be mixed inside one template.

⚠ **Deleting or retyping a `Warhead@` on a template orphans child BARE overrides → an
abstract warhead → NRE at `CreateBasic` with no weapon name in the stack.** Run
`python tools/audit/find_empty_warhead.py` (expect 0) after EVERY batch, not at the end.

**VERIFY:** `grep -rc "PercentageDenominator" mods/cameo` > 0 and
`python tools/balance/extract_stats.py --check` = 0 drifted.

---

### W19 — Collapse the `ExtraDamage` chips into the main warhead ⬜ READY (design), ⛔ content BLOCKED on set B

Maintainer 2026-08-11: *"extra damage warheads are no longer needed — after our new
balance formula that can take into account everything from the projectile like spread and
speed, we can collapse it into the main damage warhead (and later change it based on the
data-mining synthesis)."*

The reasoning holds and is reinforced by the corpus: the chip is a SECOND warhead, and
2+ warhead weapons measure a median span of 58 against 75 for single-warhead ones — chips
flatten exactly the rock-paper-scissors W13 is being built to sharpen. K now measures
footprint, reliability and profile directly, so the chip no longer pays for anything the
model cannot see.

Measured scope (229 nodes, 33 files) — and it does **not** collapse uniformly:

| chip type | nodes | families | verdict |
|---|---|---|---|
| `SpreadDamage` | **195** | Tesla 184 · Laser 5 · Railgun 1 · Magic 1 | ✓ COLLAPSE — a damage bonus with a bespoke Versus |
| `OpenToppedDamage` | **34** | Sniper only (`Sniper_Light_ExtraDamage` 26, `SniperWeaponExtraDamage` 8) | ✗ **KEEP** |

⚠ **The 34 sniper chips are not damage chips at all.** `OpenToppedDamage` is the MECHANIC
by which a sniper hits passengers inside an open-topped transport. Folding it into the main
warhead does not "merge damage", it deletes the ability — the sniper stops being able to
shoot a garrison. Collapse the 195 `SpreadDamage` chips; leave the sniper's alone.

The 195 carry bespoke per-family Versus (`CHIPS` / `CHIP_FLOOR` in
`gen_weapon_template.py`: Tesla = anti-armored-infantry + anti-shield, floors Laser 9 /
Railgun 10 / Tesla 10). Collapsing therefore means the MAIN warhead's profile must absorb
that role — which is W13's job, not a mechanical merge. **Sequence W19 after W13** so the
chip's identity is folded into a profile that was designed with it in mind, rather than
dropped and re-invented.

Damage bookkeeping: the chip is 50% of main and EXCLUDED from the damage total
(`spread_damage_sum`), so a naive delete is a real nerf and a naive merge (`main += chip`)
is a real buff. The collapse is behaviour-preserving only against the RESOLVED effective
damage — verify with `tools/audit/review_resolve_diff.py`, as in the 3-way split.

**DONE WHEN** the 195 `SpreadDamage` chips are gone, the sniper's 34 `OpenToppedDamage`
warheads remain, `find_empty_warhead.py` = 0, and the generator no longer emits `CHIPS`.

---

### W20 — Multi-armor combination rule ✅ DONE (`Average` is live)

Maintainer 2026-08-12: dual-armor units (FutureTech droids, Schwarzer Mond noids, CABAL
cyborgs) *"can feel unfair — certain weapons seem to do nothing against it while other
weapons seem too powerful."*

**The cause is multiplication, and it is ENGINE behaviour, not a Cameo choice.**
`DamageWarhead.DamageVersus` (engine `DamageWarhead.cs:88`) ends in
`Util.ApplyPercentageModifiers(100, armor)` over EVERY enabled `Armor` trait — a product.
So a second armor does not average the weapon's profile, it **squares** it: a weapon with a
17:1 spread becomes ~289:1 against a dual-armor unit. 40% × 30% = 12%, while 90% × 80% =
72% — a 6:1 gap between "bad" and "good" weapons where a single-armor unit shows ~2-3:1.
A flat 200% multiplier cannot fix this: it shifts the whole curve, and the problem is the
curve's SHAPE.

**Measured (2026-08-12): 36 actors declare more than one `Armor`, and they are three
different things wearing one mechanic —**

| group | actors | pattern | compensation |
|---|---|---|---|
| FutureTech droids | 4 | `Plate+Heavy`, `Plate+Medium`, `Flak+Light`, `None+Scout` | **`Modifier: 200`** |
| **CABAL cyborgs** | **12** | `Plate+Medium`, `Flak+Light`, `Heroic+Superheavy`, … | **NONE** |
| shields / stealth suits / upgrades | ~20 | a CONDITIONAL `Armor@Shield` layered on the body | various (50–150) |

⚠ **The compensation is applied inconsistently.** The FutureTech droids carry the 200%;
the CABAL cyborgs — the same design, named in the same breath by the maintainer — carry
**nothing**, so they are silently far tougher than their FutureTech counterparts. That
inconsistency is a likely part of what "feels unfair", independent of the combination rule.

⚠ **The ~20 shield/upgrade actors are NOT the same problem.** A conditional `Armor@Shield`
layered over the body is the layered system (W21) done crudely, and any global change to
the combination rule hits Protoss plasma shields, D2K/Ixian personal shields, Yuri stealth
suits and Steel Consortium at the same time. **Do not treat "36 dual-armor actors" as one
population.**

**Mechanism:** `AreaDamageWarhead.MultiArmorCombination` — `Average` (**the default since
2026-08-15**) · `Multiply` (the engine's rule) · `Lowest` · `Highest`. Single-armor actors
are unaffected by construction: any rule over one value returns that value, which is also
why a SHIELDED unit is untouched — its body armor is gated off while the shield holds.

**Maintainer order 2026-08-15, closing R5:** *"armored means armor plating + health armor
types are averaged"* — so `Average` is now the DEFAULT rather than an opt-in field, and no
weapon yaml has to declare it. `Average` keeps the weapon's designed profile intact (35%
rather than 12% for a 40/30 weapon), so no weapon is ever useless or oppressive.

**Landed together with the flip** (they are one change and cannot be split):
- the 7 `DamageMultiplier … Modifier: 200` squaring compensations are DELETED — 4 FutureTech
  droids, 2 Yuri slave miners, `^FlyingInfantryTemplate`. Averaged armor plus a 2x damage
  multiplier would have made those units paper.
- the 12 CABAL cyborgs needed no edit: they never had the compensation, so averaging simply
  removes the over-toughness they had been carrying silently.

⚠ **Only warheads routing through `AreaDamage` obey this.** 878 legacy warhead nodes still
declare inline `Versus` on `SpreadDamage` and keep MULTIPLYING until they are retired onto
`^Warhead_*` templates (item A5). Until then a dual-armor unit is tougher against legacy
weapons than against templated ones — a bounded inconsistency that A5 closes, and the
reason the universal alternative (moving the combination into the engine's `DamageWarhead`
base, submodule + mirror workflow) stays on the table.

**VERIFY:** `grep -n "MultiArmorCombination" OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs`
shows `= ArmorCombination.Average`, and
`grep -rn "DamageMultiplier@\(Concrete\|Scout\|Heavy\|Medium\|Light\|FlyingInfantry\):" mods/cameo`
is empty.

---

### W21 — Layered health: Shield → Integrity → Armor → Health ✅ BUILT + LIVE (2026-08-15)

⚠ **The "needs C#" status below is STALE — the C# exists and is in the game.**
`OpenRA.Mods.Cameo/Traits/` holds `Integrity.cs`, `ArmorPlating.cs` and `GrantsShield.cs`;
the stack is wired in yaml (`Shielded` 22 files, `Integrity` 6, `ChangesShield` 6,
`ArmorPlating` 2) and boot-gated across `0556f8fc9` → `4cdf8b2a8` → `ab467fe52`. The
rulings (R1–R14), the ONE-POOL/ONE-BAR law and the two-intercepting-layers hazard live in
`docs/design/ARMOR_LAYERS.md` + memory `cameo-armor-layers-and-granularity`.

⚠ **The bug class this shipped with, because boot gates cannot catch it:** two layers that
both intercept a hit each return damage modifier 1 and then each charge their own pool, so
the modifiers MULTIPLY — 1% x 1% made a shielded+plated unit effectively immortal in play,
with a clean boot. Guard: only the TOP surviving layer may absorb (`ShieldHolds`).

The original design notes follow.

**Design reference (as written 2026-08-12, before the build):**

Maintainer 2026-08-12: three bars, *"only the highest layer active determines the armor"* —
shield weak to Tesla/Storm/EMP/Quantum/Laser, armor weak to AP (CannonAP/MissileAP/Railgun),
health weak to flame/explosive. Reference: **Crystallized Nexus**
(`~/Downloads/crystallized-nexus-main`, GPLv3 — same licence as Cameo, so a port is fine
**with attribution**).

**What CN actually has** (`.modsdk/OpenRA.Mods.CN/Traits/Player/SecondaryHealth.cs`, 232
lines; `CNHealth.cs`, 293 lines):

- ✅ **Already N-layer, not 2** — `CNHealth` collects `TraitsImplementing<SecondaryHealth>()`
  into an array and walks it, so Shield → Armor → Health works structurally today.
- ✅ Per layer: `MaxHP`/`InitialHP`, `RegenerateRate` (**0 = ablative armor, >0 =
  regenerating shield** — exactly the Armor/Shield distinction), `RegenerateDelay`/
  `Interval`, `BypassDamageTypes`, `PierceDamageTypes` + `PiercePercentage`,
  `RepairDamageTypes`, `FullCondition`/`EmptyCondition`, depleted/recharged sounds,
  `BarColor`, and its own `ISelectionBarAboveHealth`.
- ❌ **`SecondaryHealth.ArmorType` is a DEAD FIELD.** Nothing outside `SecondaryHealth.cs`
  reads it (verified by grep across the whole CN assembly). CN gives layered HP POOLS, but
  Versus is still resolved against the actor's single `Armor` trait before the layer ever
  sees the damage. **The one feature we want is the one CN does not implement.**

**…and we do not need their C# for it.** `Armor` is a `ConditionalTrait` and `DamageVersus`
filters on `!a.IsTraitDisabled`. So **three `Armor` traits gated on the layers'
`FullCondition`/`EmptyCondition` give layer-aware armor with ZERO new C#** — and because
exactly one is enabled at a time, **W20's multiplication problem disappears structurally**.
That is the whole design, and the maintainer's instinct that the layers solve the dual-armor
problem is correct.

**The real cost** is the damage routing: intercepting damage before `Health` requires
replacing or subclassing the stock `Health` trait — CN wrote a 293-line `CNHealth` for
exactly this, and that is the invasive part, not the layers.

**Also worth lifting from CN** (relevant to the physical-state program's art phase):
`DamageSmoke`, `CharredPalette`, `BloomGlowEffect`, `VoxelDynamics` (spring-based impact
tilt, firing recoil, roll on turns), `PeriodicSpriteEffect`.

**DONE WHEN** a unit can carry Shield/Armor/Health with per-layer bars, the active layer
alone decides the Versus lookup, and a dual-armor cyborg needs no `DamageMultiplier` crutch.

#### W21 — verified ground truth (2026-08-12), correcting three assumptions

⚠ **`Integrity` is NOT the shield.** It is Cameo's own **electronics** pool
(`AffectedByDamageTypes: Tesla`, `ActiveCondition: electronics`, sits beside the EMP bar,
drained by a warhead's `IntegrityScale`). The shield is **`Shielded`**, from
`engine/OpenRA.Mods.AS/Traits/Shielded.cs` — 23 files use it vs 9 for Integrity. Every
`[Desc]` in `Integrity.cs` had been copied verbatim from `Shielded.cs` and called it a
shield; corrected 2026-08-12. ⚠ `Shielded` lives in the **engine submodule**, so extending
it needs the mirror workflow — prefer a Cameo-side layer trait.

**The stack forks below the shield** (maintainer 2026-08-12): *"Integrity should only be
protected by shields but not by armor, so once there are no shields left the unit starts
taking integrity damage."*

```
        Shield  (Shielded — absorbs EVERYTHING, incl. electrical)
           |
    +------+------+
    |             |
  Armor        Integrity        (parallel, selected by damage type:
 (physical)   (electrical)       armor never protects electronics)
    |             |
    +------+------+
           |
        Health
```

**Measured, and each contradicts a stated assumption:**

1. **The regen rule is real but has DRIFTED.** `defaults.yaml` carries only a flat
   `Step: 10` fallback; the real rule is hand-set per actor. Of 846 actors with a Step:
   **508 = HP/2500, 232 = HP/1000, 106 (12.5%) OFF-RULE** — including an undocumented
   third divisor `HP/1250` (chronotank, japan_chihaheavytank, apparition.ixian) and
   `HP/10000` on the carryalls. This is the case for moving regen INTO the trait.
   Note the defaults already slow infantry down via `Delay: 2` / `DamageCooldown: 20`
   against vehicles' `1` / `10`.
2. **"Versus vs shields is always >100%" is true for mains, false for twins.**
   Main warheads: n=185, median 110, **129 (70%) above 100**, range 9–400.
   `%`-twins: n=89, median **25**, only **4 (4%)** above 100.
   ⚠ The W15 Versus x5 rebase silently FLIPS this — a twin at 25 becomes 125, turning
   every percentage warhead from shield-resistant to shield-punishing. Decide it
   deliberately.
3. **The 150% multiplier is the REVERSE of what was remembered.**
   `DamageMultiplier@shieldpermanent: Modifier: 150` is gated on `shieldpermanent`,
   granted by `ixian_upgrade_personalshield` / `japan_upgrade_stealthsuitintegration` /
   `ordos_upgrade_shields` — the unit's OWN permanent shield. So **permanently**-shielded
   units take 150% damage and externally-shielded ones take normal, not the other way
   round. The plan (drop the multiplier, halve externally-granted capacity so 1 shield HP
   always means one thing) still stands — it just corrects the opposite asymmetry.

**⚠ The 50% armor cap does NOT contain the problem it was chosen for.** Effective HP from a
layer is `pool × (1 / versus)`. Armor at 50% of HP using a VEHICLE armor type, hit by an
anti-infantry weapon at 20% vs Medium, absorbs `50k / 0.20 = 250k` — **2.5x the unit's
whole health bar, from a "50%" layer** — and ~8x at a 17:1 profile. Pool size is additive,
the armor multiplier is multiplicative, so no flat percentage can cap it. **The cap must
scale with the spread** (e.g. `pool = HP × k / spread`), or the armor layer's Versus band
must be narrowed (e.g. 60–140) while body armor keeps the full 20–100.

**A property worth keeping deliberately:** shield 200% pool at 2x rate and armor 50% pool
at 0.5x rate both refill in EXACTLY the same time as health (2500 ticks in the worked
example) — pool and rate cancel. So "shields regenerate twice as fast" changes nothing in
relative terms; only the ramp-up delays (25 / 125 / 250) differentiate the layers. In
sustained fire the ABSOLUTE rate is what matters, and the shield soaks 4x the armor's
per-tick — likely more attrition dominance than intended.

**Suggested single ramp formula** for all three layers (one implementation, no per-unit
tuning): `rate = base × min(1, ticks_since_damage / ramp)`, ramp = 25 / 125 / 250.

#### W21 — MAINTAINER RULINGS 2026-08-12 (the full decision set)

⚠ **Layer order CORRECTED.** An earlier note in this file drew Integrity as a parallel
branch. The ruling is **sequential**:

```
Shield  →  Integrity  →  Armor  →  Health
```
- **Shield** absorbs EVERYTHING — physical damage, physical-state meters, DoT, and
  electrical. Nothing gets past an intact shield.
- **Integrity** (electronics) sits BETWEEN shield and armor: once the shield is gone,
  electrical damage starts eating it. Type-filtered, so non-electrical damage skips it.
- **Armor** protects the HEALTH POOL ONLY — it stops nothing else.
- **Health** decides life and death; every actor has one.

**R1 — 1 HP is 1 HP, always.** THE unifying law. The same armor type must always take the
same damage from the same hit, so **`DamageMultiplier` is abolished**:
- damage-reduction upgrades convert to **flat % of HP granted as additional ARMOR,
  additive** (15% reduction, i.e. `Modifier: 85`, becomes +15% of HP as armor);
- **no class-level `DamageMultiplier` on unit templates**;
- veterancy stops granting damage multipliers and **grants HP instead** — currently
  veterancy gives NO HP at all, only invisible multipliers. HP is visible in the unit stat
  widget; a multiplier is not. ⚠ This removes an invisible stat from the whole game and is
  a large re-pricing job — route it through the pipeline.
- **The ONE possible surviving use** (undecided): Superheavy + armor plating, which has no
  higher rung to promote into (see R5).

**R2 — Shields are 200% of HP** *because* the W15 Versus x5 rebase flips `%`-twins from
shield-resistant (median 25) to shield-punishing (~125). The bigger pool is the deliberate
compensation, not a coincidence. Shields regenerate fastest; armor slowest.

**R3 — Damage cascades.** Excess damage always flows into the next layer in the same shot,
exactly as `Shielded` behaves today. (So `BlockExcessDamage` stays `false`.)

**R4 — A `%`-warhead computes against the ACTIVE layer**, not max health — it is damaging
whatever the outer layer currently is.

**R5 — The armor layer's armor TYPE.** ✅ **LIVE since 2026-08-15** (W20 default = `Average`).
The three states, in the maintainer's words: *"shielded means only shield armor is active,
armored means armor plating + health armor types are averaged and health means only health
armor is active."* So the plating armor is gated on the plating's `FullCondition` and the
BODY armor stays enabled underneath it; only the SHIELD gates the body armor off.
- **Infantry: AVERAGE the body armor and the plating armor** (this is W20's `Average` mode,
  and it is what stops an anti-infantry weapon being useless against a plated cyborg —
  *"infantry with armor platings will still feel distinct from actual tanks"*).
- **Vehicles: the plating promotes one rung** — Scout→Light, Light→Medium, Medium→Heavy,
  Heavy→Superheavy. Superheavy has no rung above it (open).
- Per-class Health+Armor type COMBOS to be designed: `None+Scout`, `Flak+Light/Medium`,
  `Plate+Heavy/Superheavy`, etc.
- ✅ **SETTLED 2026-08-15 — average both, everywhere.** The maintainer's rule is stated for
  the ARMORED state as such, not for infantry only, and the mechanism is a warhead-wide
  default rather than a per-actor switch, so vehicles average too. This costs nothing: the
  promoted type is an ADJACENT rung, so averaging a tank barely moves it, while the same
  rule matters a lot for infantry, aircraft, ships and defences.

**R6 — Pool sizes.** Armor = 50% of HP **for units that start with an armor bar or get a
full bar from an upgrade**. Other upgrades granting armor stack ADDITIVELY on top.

**R7 — One ramp formula for all layers** (adopted):
`rate = base × min(1, ticks_since_damage / ramp)`, ramp = **25 health / 125 armor /
250 shield** (health doubles to 50 for infantry). Regen moves INTO the Health/Armor/Shield
traits — no more per-actor `Step`. (See the drift evidence above: 12.5% of 846 actors are
already off-rule.)

**R8 — Armor regenerates in combat, slowly** — no repair facility required, because not
every faction has one. Armor at **half** the earlier proposal, shield at **twice** it.
⚠ Exact numbers still to pin: the earlier worked example (100k HP → 40 HP/tick, 200k shield
→ 80/tick, 50k armor → 20/tick) made all three refill in the SAME time, which erases the
distinction. With R8's re-scaling they no longer do — confirm the final triple.

**R9 — Shield-break stun: ADOPTED, 25 ticks (1 second).** Accepted *because* shields now
stop physical-state meters and DoT as well, which is enormous. ⚠ Maintainer's own caveat,
recorded on purpose: a big AoE breaking every shield at once and stunning a whole army is
potentially miserable to play against — treat the 25 ticks as a starting value and be
willing to cut it.

**R10 — Repair vs heal split.** Repair restores **armor plates** (and vehicle health);
medics restore **infantry health only**. Neither restores shields — shields self-regenerate.

**R11 — Splash hits the top layer only** (current behaviour, kept). **Future idea, not
decided:** layer-PENETRATING weapons — railgun punches through armor straight to health,
sonic ignores shields. Note the data already leans this way: mean Versus vs Shield is
Sonic **55** and Railgun **75**, i.e. both are already poor against shields, so "ignore the
shield instead" is a thematic upgrade rather than a new axis.

**R12 — Who gets armor.** Cyborgs / droids / noids START with a bar; **any** unit can gain
one from an external effect or upgrade.

**R13 — UI.** Three bars: health green/yellow/red, shield purple, armor yellow-orange.
Gradients on shield/armor are OPTIONAL and off by default (colour overload risk). Build a
**combined segmented bar as a separate trait**, switchable from the game's visual settings
(3 bars ↔ 1 segmented bar). **All three bars are always visible to everyone**, and
**"Show Status Bars on Damage" must default to always-on** in the display settings.

**R14 — Tesla is the shield-killer** (verified: mean Versus vs Shield 228.8, the highest of
any family, next is Nuclear 155 and Storm 147.5). So the "shields hard-counter electrical"
worry is answered by design: you break the shield with the same weapon family you then use
on the electronics.

#### ⚠ The Heroic armor conflict is STRUCTURAL, not a data bug

Maintainer: *"Heroic is designed as the heaviest infantry armor, but this causes it to take
more damage from armor-piercing weapons meant to be anti-tank — suddenly they are really
good at fighting a commando. Heroic should always be the BEST armor."*

**Measured: of 186 main warheads carrying a full infantry ladder, 52 (28%) give Heroic a
HIGHER multiplier than some lighter infantry armor** — `^TeslaWeapon` None 125 / Flak 150 /
Plate 175 / **Heroic 200**, `^RailgunWeapon` 68/72/76/**80**, `^LaserWeapon` 44/56/72/**88**.
(A few of the 52 are `^HealingWeapon` / `^RepairWeapon`, where a higher number is a bigger
heal and therefore correct.)

**This is the ordering law working exactly as written** ([[cameo-weapon-ordering-law]]:
AP → heavy). Heroic is being asked to be two incompatible things at once: the heaviest rung
of the LIGHT→HEAVY infantry ladder, and "the best armour in the game". Under any law where
AP scales up with weight, those contradict. Three ways out:

- **(a) Take Heroic out of the ladder** — make it a QUALITY tier that sits at or near the
  best multiplier for every family. Clean semantics, but it is an exception to the ordering
  law, and the law is the thing keeping 2494 profiles coherent.
- **(b) Keep it in the ladder** and accept that a heavily armoured commando is precisely
  what an AP round is for. Costs nothing, and is defensible thematically.
- **(c) ★ Give commandos an ARMOR LAYER instead of a special armor type.** Their toughness
  comes from the extra bar (W21), not from bending the ladder — the ordering law stays
  intact and Heroic can retire to being just "heavy infantry". **This is the recommended
  option: W21 dissolves the problem instead of trading one exception for another.**

#### R1 addendum — "HP multiplier, not armor multiplier" (maintainer 2026-08-12)

Clarification: veterancy and upgrades should **raise the unit's maximum health dynamically**
rather than reduce incoming damage.

⚠ **VERIFIED BLOCKER: max health is IMMUTABLE in this engine.**
`engine/OpenRA.Mods.Common/Traits/Health.cs:81` declares `public int MaxHP { get; }` — a
get-only property assigned once in the constructor (`:69`). **No trait in
`OpenRA.Mods.Common`, `OpenRA.Mods.AS` or `OpenRA.Mods.Cameo` modifies it**; the only other
file that mentions max health, `AS/ActorStatValues.cs`, merely READS it for the stat widget.

So this is not a yaml swap. It needs `Health.cs` — a **core engine trait in the submodule**
(mirror workflow required) — made mutable, plus a ruling on what happens to CURRENT HP when
the maximum changes mid-life (scale proportionally, or keep absolute and heal the gap?).
`MaxHP` also feeds damage states, selection bars, husks, repair and AI evaluation, so
making it dynamic is invasive well beyond veterancy.

**✅ DECIDED 2026-08-12: veterancy and upgrades grant an ARMOR POOL, not max HP.** Max HP
stays immutable; no `Health.cs` change; the engine submodule is not touched.

**★ THE ALTERNATIVE, now the decision — grant an ARMOR POOL instead of raising max HP.** It is the
rule R1 already mandates for upgrades ("damage reduction becomes flat % of HP as additive
armor"), simply applied to veterancy as well:
- **zero engine change** — the layer trait is Cameo-side by design;
- **visible**, which was the whole point of dropping invisible multipliers — it shows as a
  bar, and `ActorStatValues` can total the layers for the stat widget;
- **additive and stackable**, so veterancy, upgrades and external effects compose without
  a special case;
- **one mechanism** for every "this unit is tougher now" effect in the game.

⚠ Either way, note the consequence under R4 (a `%`-warhead hits the ACTIVE layer): a bigger
pool means a percentage warhead removes proportionally more absolute HP, so **percentage
weapons give veterans NO protection at all** — they scale straight through. That makes
`%`-damage the natural anti-veteran counter. Decide whether that is a feature (it is a
clean rock-paper-scissors answer to deathballs of veterans) or needs a cap.

#### How a layer intercepts damage — the pattern, and the bug NOT to copy

`Shielded` never touches `Health.cs`. It absorbs damage with a two-step trick
(`engine/OpenRA.Mods.AS/Traits/Shielded.cs:138,197`):

1. `IDamageModifier.GetDamageModifier` returns **1** while the shield is up, so the engine
   scales the incoming hit to 1%. It returns 1 rather than 0 because a hit reduced to
   nothing would fire no damage event, and step 2 would never run.
2. `INotifyDamage.Damaged` then reconstructs the original (`e.Damage.Value / 0.01`),
   subtracts it from the shield pool, **heals back** the 1% that leaked to health
   (`InflictDamage` with negative damage), and cascades any excess to health — which is
   exactly R3's behaviour, already implemented.

**This is the pattern the armor layer should follow** (it needs no engine change and
composes with the shield automatically), **but not the arithmetic.**

⚠ **The 1%-round-trip loses damage, always downward.** `Util.ApplyPercentageModifiers` is
integer maths, so a hit of 5032 becomes `5032 × 1 / 100 = 50`, and 50 / 0.01 = **5000** —
the shield is charged 5000 for a 5032 hit. The residue is silently forgiven, up to 99 per
hit, which is a small systematic buff to every shield in the game.
⚠ **Below 100 damage it is total**: `99 × 1 / 100 = 0`, so a sub-100 hit costs the shield
nothing at all. Cameo's main damage sits in the thousands and lands on the 100 grid, so
mains are near-exact — but **Versus and Falloff scale damage before this point**, and DoT
ticks, physical-state chip damage and `%`-twin damage are all small. Those are precisely
the effects R9 just made shields responsible for absorbing.

**For the armor layer: carry the full-precision value yourself** instead of round-tripping
through a percentage — e.g. modifier 1 for the event, but subtract the pre-scaled damage
captured from `e.Damage`, or track the residue and carry it into the next hit. Worth fixing
in `Shielded` too, but that file is in the ENGINE SUBMODULE (mirror workflow), so the clean
path is a Cameo-side layer trait that both the armor bar and a future shield replacement
can share.

#### Still open

- The exact regen triple after R8's rescaling (R8).
- **The ledger has no concept of a layer.** `extract_stats` records one `#Armor.Type` per
  actor, so a plated walker is booked as its BODY armor (`Plate`) and the model prices it as
  plain infantry — the plating bar and the averaged type are invisible to pricing. Wiring
  the first three walkers moved the global armor census by one actor (`Plate` 89→90,
  `Superheavy` 94→93) and rippled every K in that faction by <0.01%, which is harmless now
  and will not be once plating is widespread. Decide before the rollout whether the ledger
  books the bare type, the plated type, or the average.
- Superheavy + plating: the one place a multiplier might survive (R1/R5). ⚠ Note that
  averaging (R5, now live) makes this LESS urgent, not more: a Superheavy body averaged
  with a Superheavy plating is still Superheavy, so the unit simply gains the bar without
  gaining a type — which may be answer enough.
- Which layer-penetrating weapons exist, if any (R11).
- Whether an EXTERNALLY granted shield protects electronics, or only a unit's own.

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

**W15 is the first item that MOVED it, on purpose** (`reference HP 74,000 → 200,000`):

`Storm 2.94 · Flame 2.21 · Plasma 2.16 · Concussion 2.15 · Thermobaric 2.14 ·
Chemical 2.13 · Demolition 2.11 · Quantum 1.99 · Flak 1.96 · CannonHE 1.92 ·
CannonAP 1.89 · Sonic 1.70 · MissileHE 1.69 · MissileAP 1.64 · Magic 1.36 ·
Prism 0.96 · Bullet 0.92 · Tesla 0.89 · Railgun 0.83 · Laser 0.63`

`k_context`: `Storm 2.69 · Flame 2.02 · Plasma 1.98 · Concussion 1.96 · Thermobaric 1.96
· Chemical 1.95 · Demolition 1.93 · Flak 1.89 · Quantum 1.82 · CannonHE 1.76 ·
CannonAP 1.73 · MissileHE 1.63 · MissileAP 1.58 · Sonic 1.56 · Magic 1.24 ·
Bullet 0.89 · Prism 0.87 · Tesla 0.81 · Railgun 0.76 · Laser 0.61`

Every family rose, because every family carries a %-twin and each twin is now priced
against 2.7x more HP. What matters is that they rose UNEQUALLY, in proportion to how much
of the family's output is percentage damage:

| family | K before | K after | change | why |
|---|---|---|---|---|
| Magic | 0.99 | **1.36** | **+37%** | the %-equalizer family — mostly percentage damage by design |
| Storm | 2.43 | 2.94 | +21% | biggest footprint, so its %-twin catches the most targets |
| Prism · Bullet · Tesla · Laser | 0.81–0.84 | 0.89–0.96 | +13% | single-target: the twin is all they gain |
| Sonic | 1.63 | 1.70 | **+4%** | flat anti-low-HP by design — least %-exposed |

**Bare-K order is unchanged; `k_context` order changed once — MissileAP overtakes Sonic**
(1.58 vs 1.56). Both are the intended direction: Sonic is the deliberately flat family, so
raising the value of percentage damage should move it DOWN a generalist ladder, and the AA
capability that `targets_factor` rewards now decides a tie the %-shift created.

⚠ Magic's +37% is the number to watch when W13 restates the families: Magic was already the
%-based counter to high-HP targets, and it just got substantially better at its own job.

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
