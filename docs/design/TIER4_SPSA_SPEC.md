# TIER 4 — SPSA proposer for `tune_build_order` (DESIGN 19.13 tier 4, SPEC — pending coordinator approval)

**Status:** spec only. No code is written until the coordinator rules on section 10. Owner: Devin (fleet orders
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
- **EL_WEIGHT = 0.5** (proposed): half the win term. Rationale: the existing score spans about -1..2.5, the
  normalised EL term spans -1..1, so 0.5 keeps fight quality a real but never dominant signal — a knob change
  that wins slightly less but trades far better still moves the objective, and a pure-margin sacrifice cannot
  outweigh a lost match. Section 10, question 1.

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
   Proposed `c = 0.08` in log space: the first step perturbs every knob ~8% (vs the 10% coordinate DELTA),
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
   Proposed `a = 0.10`, `A = 10`: a_0 ~= 0.025 in log space, ~2.5% per-knob move on a clean gradient;
   at k = 50, a_k ~= 0.012. (An earlier draft said a = 0.05, A = 25; the simulation below showed that
   converges visibly too slowly at our noise level — ~30% more residual distance at k = 60.)
5. **Gradient smoothing** — an EMA over successive estimates (heavy-ball flavour, cheap variance
   reduction against the random-sign cross terms of one-step SPSA):
       g_bar_k = G_EMA x g_k + (1 - G_EMA) x g_bar_(k-1),   g_bar_0 = 0
   Proposed `G_EMA = 0.4`. Simulation: at paired-diff noise sigma = 0.5 this cut the damped
   trajectory's final distance to target ~20% (0.425 -> 0.343); at sigma = 0.16 it is neutral-to-better.
   The EMA state lives beside `k` in `SpsaSteps` (per knob milli-of-log, i.e. plain floats).
6. **Update (in log space, then clamp):**
       new_i = clamp(round(1000 x exp(log(old_i) + STEP_SCALE x a_k x g_bar_i)), LEARN_MIN, LEARN_MAX)
   `STEP_SCALE` is the significance damping of section 5. All knobs update together — one write moves the
   whole vector, not the single best knob. That is the deliberate behavioural difference from coordinate
   descent: correlated improvements (tempo up + production down) are found directly.
7. **Expected accuracy (simulation-backed).** On a 7-knob quadratic target inside [800, 1250] with
   paired-diff noise sigma_d in [0.16, 0.5] (realistic for MIN_MATCHES = 20: per-pair sd ~0.3-0.6 over
   >= 20 pairs), 60 steps land the vector ~0.28-0.34 log-units from target damped — i.e. each knob
   settles within roughly +-10-20% of optimal. That is the honest noise floor of 20-match arms; more
   matches per arm lower it, and the "settled-scope" re-sweep of section 10.6 polishes individual knobs.
6. **k increments exactly once** when the pair is fully measured and `--write` runs — regardless of verdict.
   A rejected step still consumed its matches; re-proposing the same k would resend identical arms.

### Step counter state

`k` lives **in the learned file**, per scope — the file is the single committed state (same precedent as
`processed_knobs` / posterior counts; a sidecar would drift out of sync on checkout). The gradient EMA
(`g_bar`, one float per tunable knob) is stored beside it so a later step continues the smoothed direction:

    SpsaSteps:
        Step@<personality>__<faction>: <k>
        GBar@<personality>__<faction>: <g1>,<g2>,...   # floats, same order as TUNABLE

Absent = 0 / zero vector. The parse/write round-trip ignores unknown nodes, so older tool versions
tolerate the fields.

## 5. Significance: how the gate and Holm apply to one-direction steps

Coordinate descent tests m arms against one base (m hypotheses). One SPSA step tests **one hypothesis**: "the
plus direction beats the minus direction" — a single paired contrast, not a per-arm comparison. Therefore:

- **m = 1**: the SPSA pair contributes one test to the Holm family, with the plain `Z_CRIT` critical value —
  the machinery is unchanged, the family just usually has size 1.
- **Mixed batches** (a scope whose dirs hold SPSA arms AND stale coordinate arms): the family is every
  measured hypothesis sharing that base — each coordinate arm counts one, the SPSA pair counts one — and Holm
  critical z values widen as today. The SPSA verdict uses its own contrast z, never either arm's z vs base.
- **Step damping instead of accept/reject** (proposed ruling, §10 q2). Classic SPSA steps every iteration and
  lets noise average out; our governance wants an evidence gate. Both, via `STEP_SCALE`:

  | paired z(plus - minus) | meaning | STEP_SCALE |
  |---|---|---|
  | `z >= Z_CRIT` | direction confirmed | 1.0 — full a_k step |
  | `0 <= z < Z_CRIT` | weak evidence | 0.25 — damped explore step |
  | `z < 0` | measured direction wrong | 0 — no update (k still increments) |

  A significantly-negative z does not flip the update (the estimator is unbiased for the chosen direction;
  inverting would double-count the noise). It simply skips the write for that scope.

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
   every knob is within 200 milli of theta* (the simulation's damped noise floor is ~100-160) and inside
   [800, 1250]; total distance < 0.35 log-units. Re-running gives the identical trajectory (determinism
   end-to-end). A zero-noise run must reach the target region much tighter (< 50 milli) — proves the update
   direction is right, not just damped.
2. **Deterministic perturbation:** same (personality, faction, k) -> same Delta across two runs; k and k+1
   differ in at least one component; Delta entries are all +-1.
3. **Bounds:** theta* beyond LEARN_MAX -> the converged vector sits at 1250, never above; clamped-arm
   effective-delta masking keeps eff_i honest at the bound.
4. **Floors:** < MIN_MATCHES matches on either arm -> no write, k unchanged, scope re-proposed.
5. **Damping:** forced z in each band -> STEP_SCALE 1.0 / 0.25 / 0 respectively; k increments in all decided
   bands.
6. **Holm family:** one SPSA pair + two coordinate arms sharing a base -> family size 3, SPSA hypothesis uses
   its contrast z only.
7. **Coordinate mode unchanged:** without `--spsa`, report/propose/write output is byte-identical to today on
   the same fixture dirs (regression fixture).
8. **EL term:** fixture engagements -> composite = match_score + 0.5 x mean(total_milli)/1000; zero non-
   skirmish records -> el_term 0; skirmish records excluded and counted.
9. **State round-trip:** `SpsaSteps` written by --write is parsed back; absent node = k 0; unknown-node
   tolerance preserved.

## 10. Open questions for the coordinator

1. **EL_WEIGHT = 0.5** of the normalised per-fight score — enough that fight quality matters, never enough to
   outweigh the match result. Alternative: 0.25 (pure win/margin dominance) or 1.0 (parity with win).
2. **STEP_SCALE damping policy** (§5): the 1.0 / 0.25 / 0 bands are my proposal. The purist alternative is
   unconditional updates (classic SPSA); the conservative one is full-step-or-nothing.
3. **Switch letter:** none needed (offline tooling, existing AK seam). Assign AQ_tier4_spsa only if the
   increment ledger wants the tier lettered anyway.
4. **If SPSA proves too noisy** at 20-match arms: fall back to a small GP/expected-improvement proposer over
   the same arm grammar — the spec's pairing, bounds, file format and switch seam carry over unchanged.
   Decide after the first real batch.
5. **Gains (simulation-calibrated):** a = 0.10, A = 10, c = 0.08, G_EMA = 0.4; exponents 0.602/0.101 per
   Spall (refs below). My scratch sim on a 7-knob quadratic inside the bounds: a = 0.05 stalls (~30% more
   residual at k = 60), a = 0.10 + EMA is the sweet spot among {0.05, 0.10} x {raw, EMA} at sigma_d 0.16-0.5.
   The sim is a scratch script, not committed; happy to rerun under other priors.
6. **Settled-scope rule (optional):** after 3 consecutive writes moving no knob > 25 milli, mark the scope
   settled and stop proposing (re-checkable with one coordinate sweep). Saves match budget; adds a state flag.
   Default spec: not implemented.

References: Spall, J.C., "Multivariate stochastic approximation using a simultaneous perturbation gradient
approximation," IEEE Trans. Automatic Control 37(3):332-341, 1992; Spall, J.C., "Implementation of the
simultaneous perturbation algorithm for stochastic optimization," IEEE Trans. Aerospace and Electronic
Systems 34(3):817-823, 1998 (source of the 0.602/0.101 exponents and the stability-constant guidance).

## 11. Rulings (coordinator — pending)

_(empty — to be filled by the coordinator's review, as in TIER1_FITTER_SPEC §10)_
