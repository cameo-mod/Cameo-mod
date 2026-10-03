# TIER 4 — SPSA proposer for `tune_build_order` (DESIGN 19.13 tier 4, SPEC — approved with rulings, section 11)

**Status:** approved by the coordinator with rulings R1-R6 (section 11); implementation authorised for
`tools/ai/tune_build_order.py` + `tools/tests/test_tune_build_order.py` + this spec. Owner: Devin (fleet orders
2026-10-03, item F4). Route-2 coordinate descent stays the default; `--spsa` is an opt-in proposer mode beside it.

Tier 4 asks for SPSA / Bayesian optimisation over the ~8 BO-1 knobs per (personality, faction) against a
continuous score. We spec **SPSA** (Spall's simultaneous perturbation stochastic approximation): it needs
**2 arms per step** where coordinate descent needs `2 × |TUNABLE|` = 14, at roughly a third of the match budget
per gradient estimate — and it is the method §19.13 names first. Bayesian optimisation is the fallback if SPSA
proves too noisy at MIN_MATCHES = 20 (section 10, question 4).

## 1. What exists today (do not break)

`tools/ai/tune_build_order.py` (route 2) already provides everything except the multi-knob step:

- **Score** per 1v1 match = win + margin + speed (`ai_log_common.match_score`; speed weight 0.25, ref 54000
  ticks) — the "existing win/margin term" of the objective.
- **Paired statistics:** match cells = (enemy faction, map, spawn side), k-th replicate to k-th replicate;
  paired z on the per-pair score diffs, Welch z fallback; one-sided; **Holm-Bonferroni** over the arms sharing
  one base (alpha = 1 - Phi(1.96) = 0.025).
- **Floors:** MIN_MATCHES = 20 per arm; undecided / < 3000-tick matches carry no signal.
- **Bounds:** learned multipliers clamp to [LEARN_MIN, LEARN_MAX] = [800, 1250] milli.
- **One write per scope:** at most the single best significant arm per (personality, faction) per `--write`;
  games of a decided scope are recorded in `processed_knobs` and never re-scored; the opening posteriors
  (route 3) consume only control-arm matches, perturbed arms are recorded skipped.
- **Proposer:** `--propose OUT_DIR` writes `<arm>.yaml` learned-file copies plus `experiment_switches.yaml`
  (one group per arm, `UseLearnedBuildOrder` + `LearnedFile`) armed by `apply_increment_switches.py --spec`
  next to `AK_build_order_knobs`. Batch dirs are `bo__<personality>__<faction>__base` /
  `bo__<personality>__<faction>__<knob>__up|dn<delta>` (`ARM_RE`).

None of the above changes. `--spsa` adds a second arm grammar and a second write path inside the same tool.

## 2. The objective: match score plus engagement quality

DESIGN 19.13 gives every closed fight a `score.total_milli` in `engagement` records
(`Logs/cameo-ai-engagements.jsonl`; `total_milli` = (500 trade + 250 vs_prediction + 250 objective)/1000,
clamped to ±1000; mirror `ai_log_common.engagement_score`). The match score alone rewards surviving, not
fighting well; a knob vector that wins by hiding looks identical to one that wins by trading efficiently.

Composite per-match score (what the gates and the gradient see):

    composite = match_score + EL_WEIGHT x (mean_el_total_milli / 1000)

- `mean_el_total_milli` = mean of `score.total_milli` over the bot's **non-skirmish** `engagement` records in
  that match (`skirmish: true` records are below 300 value traded and carry no decision signal; their count is
  still reported). Join key `(game_uid, player)`, same as the situations join in `load_matches`.
- A match with zero non-skirmish engagements contributes `el_term = 0` — neutral, not missing.
- **EL_WEIGHT = 0.5** (approved, R3): half the win term. Measured normalisation: `total_milli` is hard-clamped
  to +-1000 per record, and across the available logs (elsmoke + support-1v1-armed engagements, n = 568
  non-skirmish records, n = 20 per-match means) records span the full [-1000, +1000] while **per-match means**
  span only [-495, +450] — so the normalised term is effectively +-0.5, contributing about +-0.25 against a
  0..1 win term and a -1..+1 margin term: fight quality is a real but never dominant signal. A knob change
  that wins slightly less but trades far better still moves the objective; a pure-margin sacrifice cannot
  outweigh a lost match.

## 3. The `--spsa` proposer: one perturbation, two arms

For a scope (personality, faction) at step **k** over the tunable knob vector theta (log-multiplier space):

1. Draw the perturbation **Delta in {-1, +1}^p** — Rademacher, one sign per tunable knob, **deterministically**:
       bits = sha256("spsa\0" + personality + "\0" + faction + "\0" + str(k))
       Delta_i = +1 if byte i of the digest is odd else -1   (i < p; p <= 32 needs one digest)
   sha256, never `hash()` (PYTHONHASHSEED varies), never wall-clock — the same (personality, faction, k)
  yields the same Delta on every machine, so a proposed step is reproducible and reviewable. The emitted
  arm files and the experiment spec carry the resolved per-knob Delta as a comment, so a reviewer sees the
  direction without recomputing it.
2. Perturbation size **c_k = c / (k + 1)^0.101** (Spall's standard exponent gamma = 0.101; see §10 ref).
   `c = 0.08` (R2) in log space: the first step perturbs every knob ~8% (vs the 10% coordinate DELTA),
   decaying slowly — at k = 50, c_k ~= 0.054.
3. Arms, in log space over `TUNABLE` only (all other knobs keep the base value):
       theta_plus_i  = log(old_i) + c_k x Delta_i
       theta_minus_i = log(old_i) - c_k x Delta_i
       arm_milli_i = clamp(round(1000 x exp(theta_arm_i)), LEARN_MIN, LEARN_MAX)
   Multiplicative-in-milli, matching today's `old x (1 +/- delta)` but symmetric in log space (up-step and
   down-step are exact mirrors, so their difference is a clean finite difference).
4. Arm names and batch dirs (new grammar beside the old one; `SPSA_ARM_RE` added, `ARM_RE` untouched):
       bo__<personality>__<faction>__spsa__k<step>__plus
       bo__<personality>__<faction>__spsa__k<step>__minus
   `<step>` is the scope's k at propose time. `is_perturbed_arm` treats both as perturbed (their matches never
   feed the opening posteriors — same rule as knob arms).
5. `--propose --spsa --pair <personality>:<faction>` writes the two learned-file copies and adds the two
   groups to `experiment_switches.yaml` — **unchanged harness interface**: same `UseLearnedBuildOrder` /
   `LearnedFile` fields, same `<arm>.yaml` copy step, same batch-dir convention (harness appends `_N`).
   A `--pair` may combine `--spsa` and coordinate proposals for different scopes in one OUT_DIR.

## 4. The gradient estimate and the write

When both arms are measured (each >= MIN_MATCHES scored matches):

1. **Paired difference** d = composite(plus) - composite(minus), paired by cell exactly as today
   (enemy faction x map x spawn, k-th replicate to k-th replicate). The cell structure already cancels map /
   spawn / matchup effects; the same pairing powers the plus-vs-minus contrast. Welch fallback when pairing
   fails, as today.
2. **Effective perturbation** per knob: `eff_i = (log plus_i - log minus_i) / 2` computed from the **clamped**
   arm values. At a bound, one arm may equal the base (eff_i = c_k or ~0): the estimate divides by the step
   actually taken, not by c_k — otherwise bound-adjacent knobs get inflated gradients. Knobs with
   `|eff_i| < 0.005` contribute nothing this step (masked; reported).
3. **Gradient estimate** (standard two-sided SPSA):
       g_i = d_bar / (2 x eff_i)        per unmasked knob
   One paired contrast identifies the whole direction — that is the point of simultaneous perturbation.
4. **Step size** a_k = a / (k + 1 + A)^0.602 (Spall's alpha = 0.602; A is the stability constant).
   Constants (approved, R2): `a = 0.05`, `A = 25` — so a_0 = 0.05 / (0 + 1 + 25)^0.602 = 0.05 / 7.11 ~=
   **0.0070**, and a_50 = 0.05 / 76^0.602 ~= 0.0037. Worked example: a clean step d_bar = 0.2 over
   eff_i = 0.08 gives g_i = 0.2 / (2 x 0.08) = 1.25, and the first full step moves each knob by
   STEP_SCALE x a_0 x g_i = 1.0 x 0.007 x 1.25 ~= 0.0088, i.e. **~0.9%** in multiplier space.
5. **Update (in log space, then clamp):**
       new_i = clamp(round(1000 x exp(log(old_i) + STEP_SCALE x a_k x g_i)), LEARN_MIN, LEARN_MAX)
   `STEP_SCALE` is the significance damping of section 5. All knobs update together — one write moves the
   whole vector, not the single best knob. That is the deliberate behavioural difference from coordinate
   descent: correlated improvements (tempo up + production down) are found directly.
6. **Expected accuracy (simulation-backed).** On a 7-knob quadratic target inside [800, 1250] with
   paired-diff noise sigma_d in [0.16, 0.5] (realistic for MIN_MATCHES = 20), the R1-gated walk lands
   ~0.26-0.35 log-units from target at k = 60 — each knob within roughly +-10-20% of optimal. The noise
   floor (~0.21 at sigma_d = 0.16) is reached by k ~= 120 and does not improve further at constant
   MIN_MATCHES; even at zero noise the small a keeps convergence gradual (dist 0.22 at k = 60, 0.10 at
   k = 250). SPSA moves the whole vector into the right neighbourhood cheaply; per-knob polish is what
   the coordinate follow-up sweep is for (section 7).
7. **k increments exactly once** when the pair is fully measured and `--write` runs — regardless of step
   scale. A damped step still consumed its matches; re-proposing the same k would resend identical arms.

### Step counter state

`k` lives **in the learned file**, per scope — the file is the single committed state (same precedent as
`processed_knobs` / posterior counts; a sidecar would drift out of sync on checkout):

    SpsaSteps:
        Step@<personality>__<faction>: <k>

Absent = 0. The parse/write round-trip ignores unknown nodes, so older tool versions tolerate the field —
and the C# loader provably ignores it too: `BuildOrderLearned.Parse` only reads `Knobs@*` / `Openings@*`
children of the `BotBuildOrderKnobs` root (no else clause; `BuildOrderKnobsEval.cs:361-403`), so a
`SpsaSteps` node is invisible to the game (R6, verified).

## 5. Significance: how the gate and Holm apply to one-direction steps

Coordinate descent tests m arms against one base (m hypotheses). One SPSA step tests **one hypothesis**: "the
plus direction beats the minus direction" — a single paired contrast, not a per-arm comparison. Therefore:

- **m = 1**: the SPSA pair contributes one test to the Holm family, with the plain `Z_CRIT` critical value —
  the machinery is unchanged, the family just usually has size 1.
- **Mixed batches** (a scope whose dirs hold SPSA arms AND stale coordinate arms): the family is every
  measured hypothesis sharing that base — each coordinate arm counts one, the SPSA pair counts one — and Holm
  critical z values widen as today. The SPSA verdict uses its own contrast z, never either arm's z vs base.
- **Step damping instead of accept/reject** (R1, two-sided gate). Classic SPSA steps every iteration and
  lets noise average out; our governance wants an evidence gate. The gate is on **|z|**, never on signed z:
  the sign of d_bar IS the gradient direction (when the minus arm wins, g_i = d_bar/(2 x eff_i) already
  steps toward minus — that is the correct update). Skipping z < 0 would discard half of every step's
  information and make the update depend on which arm happened to be labelled "plus".

  | paired z(plus - minus), two-sided | meaning | STEP_SCALE |
  |---|---|---|
  | `abs(z) >= Z_CRIT` | direction measured | 1.0 — full a_k step |
  | `abs(z) < Z_CRIT` | weak evidence | 0.25 — damped explore step |

  Two-sided test at alpha = 0.05 gives critical |z| = 1.96 — the same Z_CRIT. A measured pair always
  updates; there is no zero band for decided scopes.

- **Floors unchanged**: both arms >= MIN_MATCHES scored matches AND >= MIN_MATCHES paired cells, else
  under-sampled: no write, k does not increment, the pair is re-proposed or waits for more batches.

## 6. Determinism and review surface

- Perturbation: sha256-seeded (§3.1) — byte-identical arms for a given (scope, k) on any machine.
- The whole write path is deterministic given the same batch dirs (no clocks anywhere in the tool today; none
  added). Two `--write` runs on identical inputs produce byte-identical files.
- Every arm's learned-file copy carries a generated comment: step k, c_k, the resolved Delta vector, eff_i
  after clamping — the reviewer sees exactly what was tested without recomputing.
- `processed_knobs` gains the spsa arm games on a decided scope (same once-only rule); the opening posterior
  path is untouched (spsa arms are perturbed arms).

## 7. What SPSA deliberately does not change

- Coordinate descent remains the default mode and is used for sanity checks: if a settled SPSA scope later
  shows a single dominant knob, one coordinate sweep confirms it cheaply (the modes share score, pairing,
  floors, bounds and the learned file).
- `expansion` stays out of `TUNABLE` (no consumer reads it yet; measured noise only). The mechanism is
  knob-list-agnostic — when the planner consumes it, adding it to TUNABLE extends p automatically.
- No new increment switch: `--spsa` is offline tooling. Arms arm through the existing `AK_build_order_knobs`
  provider + per-arm `LearnedFile` seam. (If a tier-4 letter is wanted for the increment ledger, that is a
  coordinator call — §10 q3.)
- Nothing in-match: frozen at match start (DESIGN 19.2), learned file is committed and reviewed, bounds and
  difficulty-on-top unchanged.

## 8. Files (when approved)

- `tools/ai/tune_build_order.py` — `--spsa` flag; `SPSA_ARM_RE`; arm grammar `__spsa__k<step>__plus|minus`;
  `spsa_delta` (sha256 Rademacher), `gain_a`/`gain_c`; EL-term join in `load_matches` + `composite` score;
  `spsa_experiment` (proposer), `spsa_update` (write path with STEP_SCALE damping); `SpsaSteps` in
  parse/format; report lines for the composite terms.
- `tools/tests/test_tune_build_order.py` — new cases (section 9).
- `mods/cameo/ai/learned/build_order_knobs.yaml` — gains `SpsaSteps` (and eventually the moved multipliers;
  written only by `--write`).
- `docs/design/AI_ARCHITECTURE.md` — new §12.35 "T4 — SPSA knob tuning" (12.31 veto, 12.32 NOVA adaptation,
  12.33 DAWN tier-3, 12.34 tier-1 are taken); a three-line back-reference added to §12.25's tuner bullet.
- `docs/design/TIER4_SPSA_SPEC.md` — this spec.
- No changes: `apply_increment_switches.py`, `increment_switches.yaml`, `ai_log_common.py` (score mirrors
  already exist), any C# (the arms are just learned files through the existing seam), `AI_MATCH_LOG.md`
  (fields already documented).

## 9. Acceptance tests (pytest, `test_tune_build_order.py`)

1. **Synthetic quadratic convergence:** objective `J(theta) = -sum((theta_i - theta*_i)^2)` + seeded noise
   (sigma_d = 0.16, the optimistic-realistic end); theta* interior. After <= 60 SPSA steps at test floors,
   every knob is within 250 milli of theta* and inside [800, 1250]; total distance < 0.40 log-units (the
   simulation's damped floor is ~0.26-0.35). Re-running gives the identical trajectory (determinism
   end-to-end). Zero-noise must show monotone approach: dist < 0.25 at k = 60 and < 0.10 at k = 250 —
   proves the update direction is right, not just damped.
2. **Deterministic perturbation:** same (personality, faction, k) -> same Delta across two runs; k and k+1
   differ in at least one component; Delta entries are all +-1.
3. **Bounds:** theta* beyond LEARN_MAX -> the converged vector sits at 1250, never above; clamped-arm
   effective-delta masking keeps eff_i honest at the bound.
4. **Floors:** < MIN_MATCHES matches on either arm -> no write, k unchanged, scope re-proposed.
5. **Damping:** forced |z| in each band -> STEP_SCALE 1.0 / 0.25 respectively; k increments in both.
   A significantly NEGATIVE z still updates — toward the minus arm (R1): fixture where the minus arm
   wins -> knobs move toward the minus vector, not zero.
6. **Holm family:** one SPSA pair + two coordinate arms sharing a base -> family size 3, SPSA hypothesis uses
   its contrast z only.
7. **Coordinate mode unchanged:** without `--spsa`, report/propose/write output is byte-identical to today on
   the same fixture dirs (regression fixture).
8. **EL term:** fixture engagements -> composite = match_score + 0.5 x mean(total_milli)/1000; zero non-
   skirmish records -> el_term 0; skirmish records excluded and counted.
9. **State round-trip:** `SpsaSteps` written by --write is parsed back; absent node = k 0; unknown-node
   tolerance preserved.

## 10. Open questions for the coordinator — resolved by section 11 rulings

1. **EL_WEIGHT = 0.5** of the normalised per-fight score — approved (R3); normalisation stated from measured
   logs (§2).
2. **STEP_SCALE damping** — ruled (R1): |z| gate, 1.0 / 0.25, never 0 for a measured pair.
3. **Switch letter:** none — ruled (R4); offline tool through the existing AK seam.
4. **GP fallback:** deferred until after the first real SPSA batch (R5).
5. **Gains:** a = 0.05, A = 25, c = 0.08 (R2); exponents 0.602/0.101 per Spall (refs below). A post-review
   gradient-EMA variant from my calibration sim was dropped — it was not in the approved spec text.
6. **Settled-scope rule:** not now (R5).

References: Spall, J.C., "Multivariate stochastic approximation using a simultaneous perturbation gradient
approximation," IEEE Trans. Automatic Control 37(3):332-341, 1992; Spall, J.C., "Implementation of the
simultaneous perturbation algorithm for stochastic optimization," IEEE Trans. Aerospace and Electronic
Systems 34(3):817-823, 1998 (source of the 0.602/0.101 exponents and the stability-constant guidance).

## 11. Rulings (coordinator, 2026-10-03 — spec @e3d4e2c04 approved with these)

- **R1 (BLOCKER, §5).** The gate is on **|z|**, not z: in SPSA the sign of d_bar IS the gradient direction —
  when the minus arm wins (z < 0), `g_i = d_bar/(2 x eff_i)` already steps toward minus, and that is the
  correct update. Skipping z < 0 would discard half of every step's information and make the update depend
  on which arm got the "plus" label. STEP_SCALE: `|z| >= Z_CRIT` -> 1.0; `|z| < Z_CRIT` -> 0.25; never 0 for
  a measured pair. Two-sided at alpha = 0.05 -> critical |z| = 1.96 (same Z_CRIT).
- **R2 (§4 arithmetic).** a_0 = 0.05/(1+25)^0.602 ~= 0.0070; a_50 ~= 0.0037 — the earlier "~1.4%" was wrong.
  Constants: a = 0.05, c = 0.08, A = 25. Worked example in §4.
- **R3.** EL_WEIGHT = 0.5 approved; the normalisation range is stated from measured EL logs (§2), not assumed.
- **R4.** No switch letter — offline tool, existing `AK_build_order_knobs` + `LearnedFile` seam.
- **R5.** GP fallback deferred until after the first real SPSA batch; settled-scope rule: not now.
- **R6.** `k` in the learned file (`SpsaSteps`/`Step@p__f`) approved. Verified the C# loader ignores the node:
  `BuildOrderLearned.Parse` reads only `Knobs@*`/`Openings@*` children of the `BotBuildOrderKnobs` root
  (`BuildOrderKnobsEval.cs:361-403`, no else clause) — `SpsaSteps` is invisible to the game.
