# RESEARCH 2026-10-05 — How RTS AIs learn from opponents

Task: LEARN-RESEARCH (brief: `BRIEF_2026-10-05_bot_meta_learning.md`). Author: Devin-Integrator.
Scope: survey of working opponent-learning systems and the theory under them, with URLs, ending in a ranked "what Cameo should steal".

Bottom line up front: **every shipped RTS bot that learns does the same four things** — (1) writes a compact per-opponent game record, (2) recognizes the opponent's opening into a *coarse* label, (3) picks openings with a bandit that forgets on a schedule, (4) keeps everything in human-editable data files. Nobody ships an end-to-end learned policy for this. The academic line (Weber & Mateas → Synnaeve & Bessière → Dereszynski → AlphaStar) supplies better labels, priors, and signatures — not a different architecture.

---

## 1. StarCraft competition bots — the practical state of the art

Brood War bot tournaments (AIIDE, CIG, SSCAIT) are the only place this has been tested at scale for a decade, because bots keep persistent write-access data between ladder games. Jay Scott's *Starcraft AI blog* (satirist.org) is the primary source and reviews every bot's learning files each year.

### 1.1 Steamhammer — the reference implementation

- **Game records.** One append-only file per opponent (`om_<name>.txt`), each record a compressed game summary: map, own opening, recognized enemy plan, *timing table* (first combat unit, first flyer, first detection, …), and periodic unit-mix snapshots. Bounded retention (100 records/opponent; Locutus raised to 200). This is exactly the brief's "observe + confirm" record.
  https://satirist.org/ai/starcraft/blog/archives/474-the-opponent-model-in-Steamhammer-1.4.html
- **Plan recognizer.** Rule-based, <150 lines, maps raw observations to **coarse categories** ("fast rush", "worker rush", "proxy", "factory tech", "not fast rush" as catch-all) rather than exact builds — categories compress more usable signal per game than precise fingerprints.
  https://satirist.org/ai/starcraft/blog/archives/427-recognizing-the-enemys-opening-plan.html
- **Plan predictor.** At game start, histogram the opponent's recognized plans from game records **with a recency discount factor** — "the past is gradually forgotten so it reacts quickly when the enemy changes play." Wins/losses deliberately ignored for prediction (it predicts *what* they'll do, not whether it worked).
- **Counter-strategies.** Config section `CounterStrategies`: opening named `Counter [plan] v[race]` is chosen when that plan is predicted. "Steamhammer doesn't need to lose to learn" — one observed game is enough to trigger a stored counter.
  https://satirist.org/ai/starcraft/blog/archives/466-early-experience-with-Steamhammers-new-opening-selection.html
- **UCB1 for a binary tactic.** Gas steal is a two-arm bandit per opponent, seeded with **fictitious losses** so an untried action needs evidence, not naivety. (Later generalized: post-1.4 opening selection uses UCB "with a twist to cope with the large number of openings.")
  https://satirist.org/ai/starcraft/blog/archives/451-Steamhammer-is-getting-auto-gas-stealing.html
  http://satirist.org/ai/starcraft/steamhammer/version-history.html
- **Nearest-neighbour mid-game prediction.** First opponent model found the best-matching past game (summed unit-mix differences over time) and used *its* future as the enemy forecast.
  http://satirist.org/ai/starcraft/blog/archives/362-Steamhammers-opponent-model.html
- **Position evaluator.** Cheap learned win-prob estimator (binary reservoir computing), one data file per matchup (`eval_ZvT.txt` etc.), inputs are fog-honest scouted counts — used for in-match decisions, and retrainable between games.
  http://satirist.org/ai/starcraft/steamhammer/machine-learning.html
- **Strategy mix config.** `StrategyMix` weights per opening, with per-map-size weight overrides (`Weight2`, `Weight4`) and `EnemySpecificStrategy` name-keyed overrides — the seam where learned selection plugs into hand-authored content.
  https://satirist.org/ai/starcraft/steamhammer/2.4/configuration.html

### 1.2 UAlbertaBot — bandit opening learning, and why they turned it off

- UCB strategy selection over named openings, per-opponent win/loss counts in flat files ("keeps counts of wins and losses per strategy, not full history files").
  https://github.com/davechurchill/ualbertabot/wiki/Artificial-Intelligence
- **Disabled by default since 2013** — in a tournament you meet ~20 opponents a handful of times each; UCB's exploration tax is paid against opponents you'll never see again. Replacement pattern: one **primary strategy per race/matchup, switch only when the primary proves bad** — a degenerate bandit that front-loads exploitation.
- Post-mortem data shows what learned selection buys: it discovered `4RaxMarines` beat specific opponents (Stardust) and `DTRush` beat Dragon — counters the author never coded.
  http://satirist.org/ai/starcraft/blog/archives/1163-AIIDE-2021-what-UAlbertaBot-learned.html
  https://satirist.org/ai/starcraft/blog/archives/1232-AIIDE-2023-what-UAlbertaBot-learned.html
- Related: BOSS build-order search — a learned goal feeds a heuristic planner, so "learn the *what*, plan the *how*".
  https://github.com/davechurchill/ualbertabot/wiki/UAlbertaBot-Build-Orders
- Research fork: UAlbertaBot + Synnaeve's probabilistic opening detector beat random selection in a simulated tournament — strategy adaptation measurably pays.
  https://exa.ai/library/publication/llpxjk6wn7q

### 1.3 PurpleWave — fingerprints and format-aware forgetting

- "Fingerprinting": recognizes *specific* openings (4-pool vs 9-pool vs 12-hatch), finer than Steamhammer's categories; the author argues the fine distinction is actionable (Nexus-first is safe vs 12-hatch, dead vs 9-hatch). **Lesson: keep both granularities — fine labels for response rules, coarse labels for statistics.**
  http://satirist.org/ai/starcraft/blog/archives/427-recognizing-the-enemys-opening-plan.html
- Learning files store own strategy choices + fingerprinted enemy strategies; **different learning parameters per tournament format** (round-robin config tuned to avoid catastrophic forgetting, elimination config tuned to exploit fast). Format-aware memory management — steal this directly.
  http://satirist.org/ai/starcraft/blog/archives/985-AIIDE-2020-what-bots-wrote-data.html
  http://satirist.org/ai/starcraft/blog/archives/1047-SSCAIT-Steamhammer-PurpleWave-games.html
- Architecture note (HTN-ish plan tree, resource mutexes): decisions structured as goals that lock resources — a learned *playbook entry* fits as a plan node.
  https://github.com/dgant/PurpleWave/tree/master/src

### 1.4 Locutus (and the Locutusoids) — pre-learned priors and their pitfall

- Steamhammer fork; same record machinery, 200 records/opponent. Ships **pre-learned data** as priors — but in a 100-round tournament, half the retained records stayed pre-learned, so stale priors dominated live data. **Lesson: priors must decay too, or the bot fights last year's meta.**
  http://satirist.org/ai/starcraft/blog/archives/857-AIIDE-2019-what-Locutus-learned.html
  http://satirist.org/ai/starcraft/blog/archives/703-AIIDE-2018-what-Locutus-learned.html
- Enemy-specific strategies exist but are config-authored, not learned — the hand-authored and learned selection paths coexist cleanly.
  http://satirist.org/ai/starcraft/blog/archives/518-new-bot-Locutus.html
- ISAMind fork: swapped the rule-based plan recognizer for a small feedforward NN (frame + early unit counts + recognizer features → 10 plan classes). Proof the recognizer layer is swappable; gains were modest — **labels matter more than the classifier.**
  http://satirist.org/ai/starcraft/blog/archives/685-looking-at-ISAMind.html

### 1.5 MegaBot / "Rock, Paper, StarCraft" — selection as a game, not a bandit

- Model the bot-vs-bot payoff table as a normal-form game; select the **Nash-equilibrium mixture** over your portfolio instead of a bandit's argmax. Correct answer when the opponent is also adaptive — pure counters get countered.
  https://cdn.aaai.org/ojs/12857/12857-52-16373-1-2-20201228.pdf (Tavares, Azpúrua, Santos, Chaimowicz, AIIDE 2016)
- MegaBot extends this to adversarial **algorithm selection** with minimax-Q; robust under non-stationarity, beat 60% of AIIDE 2016 entries with an outdated portfolio.
  https://exa.ai/library/publication/r1ycbywxp1z

---

## 2. Predicting builds and plans (the academic line)

- **Weber & Mateas, "A Data Mining Approach to Strategy Prediction" (CIG 2009).** The canonical encoding: feature vector = **time each unit/building type was first produced**; supervised classifiers (logistic regression, kNN, Naive Bayes, decision trees) predict the strategy label and the timing of strategic actions; robust under simulated fog. Directly the "signature vector" the brief needs — cheap, domain-independent.
  https://eis.ucsc.edu/papers/cig_2009.pdf
  https://researchr.org/publication/WeberM09-1
  (Companion: Weber & Mateas, "Case-Based Reasoning for Build Order in RTS Games", AIIDE 2009 — CBR over the same encoding.)
- **Synnaeve & Bessière (2011), two papers.**
  - Opening prediction (CIG): Bayesian model over labeled openings, **EM semi-supervised labeling** of replays, posteriors update as observations arrive. https://hal.science/hal-00607277/document
  - Build-tree prediction (AIIDE): generative model `P(T|BuildTree)` — **bell-shaped timing distributions per tech step**, learned from replays; predicts the tech tree under partial observation. https://ojs.aaai.org/index.php/AIIDE/article/view/12429 — slides: https://emotion.inrialpes.fr/people/synnaeve/index_files/aiide11.pdf
  - **Caveat that Cameo must heed:** parameters learned on *human* replays transfer badly to *bot* opponents; they had to impose a large minimum variance. Train on the population you will actually face — for Cameo, its own match logs, not human replays.
- **Schadd, Bakkes & Spronck, "Opponent Modeling in RTS Games" (GAMEON 2007).** **Hierarchical** opponent models in Spring (TA-derived engine — Cameo's closest ancestor in this list): top level classifies play style (rush/boom/turtle), bottom level classifies the specific strategy. Splitting hard classification into staged easy ones.
  https://sander.landofsand.com/publications/Schadd,_Bakkes_and_Spronck_-_Opponent_Modeling_in_Real-Time_Strategy_Games.pdf
- **Dereszynski et al., "Learning Probabilistic Behavior Models in RTS" (AIIDE 2011).** HMMs over game logs learn recurring *strategic states* with no predefined labels; supports prediction, simulation, and — key for the brief's novelty step — **flagging uncharacteristic strategies** by low likelihood under the learned model.
  https://web.engr.oregonstate.edu/~tgd/publications/aiide2011-starcraft-model.pdf
- Scouting-aware prediction: GIST lab bot collects only what scouts actually saw, then classifies — the fog-honest constraint applied to the Weber encoding.
  https://cilab.gist.ac.kr/hp/wp-content/uploads/publications/international_conference/2012/prediction_of_early_stage_opponents_strategy_for_starcraft_ai_using_scouting_and_machine_learning.pdf

## 3. Case-based imitation — Darmok and relatives

- **Aha, Molineaux & Ponsen, "Learning to Win" (ICCBR 2005).** Wargus; a library of counter-strategies (each evolved against a specific opponent), **case-based plan selection** picks per building-state which tactic to run. The template for Cameo's "per-context playbook library".
  https://faculty.cc.gatech.edu/~isbell/reading/papers/l2win.pdf
- **Darmok / D2 (Ontañón, Ram, et al.).** Case-based *planning* for Wargus: learns plans from human demonstrations as **plan-dependency graphs** (which actions enable which), retrieves the best-matching past plan, adapts it online interleaved with execution. Cases = the brief's playbook entries; dependency graph = executable form of a build order.
  https://dl.acm.org/doi/10.1007/978-3-540-74141-1_12 — CBP for RTS overview
  https://ashwinram.org/2009/07/12/learning-from-human-demonstrations-for-real-time-case-based-planning/ — LfD into D2
  https://ashwinram.org/2009/01/20/on-line-case-based-planning/ — online CBP architecture
- **ROLCBP (AAMAS 2015).** Extends OLCBP with robust strategy selection — augments retrieved plans concurrently; evaluated by win rate in Wargus *and* StarCraft, strategies mined from human replays (teamliquid). Learning the library offline from replays, selecting online.
  https://www.ifaamas.org/Proceedings/aamas2015/aamas/p155.pdf

## 4. Cheap ideas from AlphaStar (don't take the neural net, take the conditioning)

- **The `z` statistic**: condition the policy on a compact build-order descriptor — **the first 20 constructed buildings/units** of a sampled human game, plus cumulative unit stats. In RL, a pseudo-reward measures **edit distance** between the z build order and the executed one — a "follow this opening" signal with zero extra machinery.
  https://www.nature.com/articles/s41586-019-1724-z — unformatted PDF: https://storage.googleapis.com/deepmind-media/research/alphastar/AlphaStar_unformatted.pdf
- **League training**: main agents + *main exploiters* (trained to beat the main agent) + *league exploiters* (beat the whole league), agents frozen into the league as checkpoints — a self-maintaining counter-strategy generator.
- **PFSP (prioritized fictitious self-play)**: pick training opponents proportional to how often you lose to them — directly translatable as "weight counter-learning effort by observed threat."
  NeurIPS 2023 analysis/confirms league structure + z mechanism: https://proceedings.neurips.cc/paper_files/paper/2023/file/94796017d01c5a171bdac520c199d9ed-Paper-Conference.pdf
- **Warning for Cameo**: AlphaStar's main agent was still fragile to uncommon counters — breadth of the counter-library, not depth of any response, is what makes adaptivity real.

## 5. Non-StarCraft RTS AIs — mostly *no* cross-game learning; the useful ideas are structural

- **Age of Empires II** (`.per` rule scripts + strategic numbers): top community AIs (Promi, Barbarian, Bright Spark) do extensive *in-match* opponent tracking via goals/facts (enemy sighted, army size, attacker identity, threat detection, enemy location estimation) but **nothing persists between games** — no file persistence in the engine. Confirms the pattern: in-match adaptation = rule-based; learning = between matches only.
  https://airef.github.io/ — http://userpatch.aiscripters.net/CPSB.pdf — https://forums.aiscripters.com/viewtopic.php?t=4207
- **0 A.D. Petra** (open source): `defenseManager` reacts to attacks with defender/attacker ratios (`defenseRatio` per attitude), `startingStrategy.js`, attack plans — parametric adaptivity, no learning. Active community forks (Arch AI changes personality defensive↔aggressive on population; the "Strategos" proposal adds a small-model strategy layer answering typed questions — "rush, boom or turtle?" — exactly Cameo's doctrinal seam).
  https://github.com/0ad/0ad/tree/master/binaries/data/mods/public/simulation/ai/petra
  https://wildfiregames.com/forum/topic/169871-strategos-a-strategy-layer-for-petra-driven-by-a-small-open-source-model-that-runs-locally-proposal-fork-to-test/
  https://github.com/eserlxl/Arch-AI
- **Beyond All Reason / Spring (Recoil engine — Cameo's own lineage):** competitive AI **BARb** (Shard framework) is *config-driven*: `behaviour.json` (squad quotas, per-unit `thr_mod` threat multipliers, retreat thresholds), `factory.json` (**production weights by income tier × map type**), AngelScript glue. Nobody learns — but the design lesson is large: **every knob a learner could tune already lives in data files**. Make playbook entries the same shape.
  https://deepwiki.com/beyond-all-reason/Beyond-All-Reason/8-ai-system
  https://deepwiki.com/beyond-all-reason/Beyond-All-Reason/8.2-shard-ai-framework
- **FAF (Supreme Commander)**: AI-Uveso ships **named sub-AIs as a manual strategy portfolio** (Adaptive, Overwhelm, Rush, Experimental, Easy); RNGAI emulates ladder meta; M28AI has an "AI memory system" for large maps — all in-match. The portfolio/sub-AI boundary = playbook boundary.
  https://faforever.com/ai — https://wiki.faforever.com/en/Development/AI/Custom-AIs

## 6. The math layer — bandits that forget, drift detectors

The brief's selection problem (pick a playbook against a changing opponent/meta) is a **non-stationary contextual bandit**:

- **UCB1** — the baseline everyone ships; deterministic bonus `sqrt(2 ln n / n_i)`. Fine for stationary.
- **Discounted UCB (D-UCB)** — weight rewards by γ^(t−s); effective sample count `N_t(γ,i)` is discounted too. **SW-UCB** — hard sliding window τ. Garivier & Moulines prove both near-optimal under abrupt changes, and D-UCB ≈ SW-UCB empirically. D-UCB is the natural fit for "forget what a rebalance made stale": γ per game *or* keyed on balance-hash change.
  https://ar5iv.labs.arxiv.org/html/0805.3415 — ACM: https://dl.acm.org/doi/10.5555/2050345.2050365
- **EXP3 family** — adversarial bandit, no stationarity assumption at all; the safe pick when the "opponent" is the maintainer's next balance patch. Same Garivier–Moulines paper benchmarks EXP3.S.
- **Contextual bandits (LinUCB / Thompson sampling)** — arms conditioned on context vector (opp faction × doctrine × archetype × map), exactly the brief's per-context stats. Agrawal & Goyal, "Thompson Sampling for Contextual Bandits with Linear Payoffs" (ICML 2013), arXiv:1209.3352. TS usually beats UCB empirically and gives exploration "for free" via posterior sampling — and its `sample from posterior` step *is* the brief's "switch randomly or as a learned counter".
- **ADWIN (Bifet & Gavaldà, SDM 2007)** — adaptive-size window with *provable* false-positive/false-negative bounds; grows when stationary, shrinks at detected drift. Use as the drift detector that triggers archive/reset, or inside the estimator itself.
  https://epubs.siam.org/doi/10.1137/1.9781611972771.42 — PDF: https://www.cs.upc.edu/~gavalda/papers/adwin06.pdf
- **Practical recipe proven by the bots**: recency-discounted counting (Steamhammer plan predictor) ≈ cheap D-UCB; fictitious pessimistic priors on untried arms; per-format forgetting parameters (PurpleWave); bounded per-opponent files.

## 7. Counter matrices and strategy ratings

- **Payoff/counter matrix `P(win | my strat, their strat)`** — fill from records, smooth (Laplace/Beta prior), then:
  - Opponent modelled → exploit: argmax row against predicted distribution (Steamhammer counter path).
  - Opponent adaptive → Nash mixture over rows (Rock Paper StarCraft); minimax-Q if their learning matters (MegaBot).
  - Both can coexist: Nash mixture as the prior, bandit posterior sharpened per-opponent.
- **Ratings under drift**: Elo/Glicko assume stationarity; **Whole-History Rating (Coulom)** models time-varying strength — rate each playbook, not just each player, to detect a playbook going stale before the bandit notices.
  https://www.remi-coulom.fr/WHR/WHR.pdf
- **Credit beyond win/loss**: Steamhammer's per-matchup reservoir evaluator shows even a crude learned win-prob function enables in-match credit assignment — "we were ahead until the artillery timing" — much denser signal than W/L bits.
- **Validation gotcha from the field**: UAlbertaBot's learned `4RaxMarines` win was real *against that pool* — counter-matrix cells are only valid conditioned on the opponent distribution they were measured against. Store context with every cell.

## 8. What Cameo should steal — ranked

1. **The record format.** Per-(opponent|opponent-class) append-only game records: signature vector (Weber: first-production times + AlphaStar-z: first-20 builds), recognized plan label, timings table, unit-mix snapshots, outcome + simple credit features. Bounded length, human-readable — this is the substrate every other item needs. (§1.1, §2)
2. **Two-level plan recognizer.** Coarse categories for statistics (Steamhammer `OpponentPlan`), fine fingerprints for response rules (PurpleWave) — hierarchical like Schadd: play-style first, specific strategy second. Rule-based first; classifiers are swappable later (ISAMind). (§1.1, §1.3, §2)
3. **Discounted plan predictor at game start.** Recency-weighted histogram of the opponent's recognized plans → predicted distribution over openings. ~20 lines, proven. (§1.1)
4. **D-UCB / Thompson-sampling selection keyed on context.** Arms = playbooks; context = opp faction × predicted plan × map class; discount γ, fictitious pessimistic priors on new arms, minimum-sample gate. This *is* the brief's §5, with citations that it works. (§6)
5. **Counter matrix + Nash fallback.** Maintain `P(win|mine,theirs)` per context; exploit when the opponent is predictable, Nash-mix when they adapt. Store the opponent-population context with each cell. (§7)
6. **Post-game relabeling loop.** Write both the in-game recognized label and the omniscient-log true label; disagreement rate is the recognizer's quality metric and the correction channel the brief's "confirm" step wants. (§2, Synnaeve timing distributions as the truth model)
7. **Novelty = low likelihood, promotion = N wins.** Cluster signatures by edit distance on the z-sequence + composition-vector distance; an unseen cluster is a candidate playbook promoted only after confirmed successes — Dereszynski's anomaly channel made concrete. (§4, §2)
8. **Balance-hash-triggered forgetting.** Keep the archive; on balance-hash change, apply a steep discount (γ drop or ADWIN-style window collapse) rather than deletion; rate playbooks over time (WHR) to detect slow staleness. (§6, §7)
9. **Learning params live in data, shaped like the tuning configs they replace.** BARb's JSON knobs and Steamhammer's `StrategyMix`/`CounterStrategies`/`EnemySpecificStrategy` show the seam: playbook library entries must be hand-authorable so seeded playbooks, learned entries, and maintainer overrides are the same type. (§5, §1.1)
10. **Format-aware memory + exploiter practice.** Different forgetting settings for "one-off vs repeated opponent"; between matches, cheaply rehearse counters against recorded opponent signatures (PFSP-flavored: practice most against what beats you). Skip this only if the sim budget can't afford it — everything above ranks higher. (§1.3, §4)

### Failure modes to design around (all observed, not hypothetical)

- UCB exploring wastefully when each opponent is seen once or twice → UAlbertaBot's fix: strong primary + switch-on-failure. (§1.2)
- Stale priors dominating live data → Locutus pre-learned records never decayed; cap prior mass. (§1.4)
- Catastrophic forgetting under pressure → PurpleWave keeps per-format learning configs. (§1.3)
- Training on the wrong population → Synnaeve: human-replay params failed on bots; learn from Cameo's own matches. (§2)
- Predictable exploiters get countered → keep a mixed strategy when the opponent also adapts (RPS-Nash). (§7)
- Winners that aren't real → `4RaxMarines` worked *for that pool*; tag every counter stat with the population it was measured on. (§1.2, §7)
