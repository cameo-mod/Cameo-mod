# CAMEO — The Ultimate Crossover RTS Design Framework

**Version:** 8.0 — rebuilt 2026-10-05 from v7 plus a targeted research pass.
**Project:** [cameo-mod/Cameo-mod](https://github.com/cameo-mod/Cameo-mod)
**Status:** Non-binding design proposal. Binding rules live in `docs/DESIGN.md`; where this document and
`DESIGN.md` disagree, `DESIGN.md` wins and this document gets fixed.

> v7 was a catch-up plan against Combined Arms. v8 reframes the goal the maintainer actually set: **make
> Cameo the most interesting RTS.** Interesting means a match is a story — scout, read, deny, counter,
> commit — built from real decisions under fog, against opponents worth reading. Every recommendation
> below is mapped to a system Cameo already has or a law `DESIGN.md` already binds.

---

## Table of Contents

1. [The Question](#1-the-question)
2. [What Twenty-Five Years of RTS Prove](#2-what-twenty-five-years-of-rts-prove)
3. [The Interesting-Match Machine](#3-the-interesting-match-machine)
4. [Failure Modes and Their Antidotes](#4-failure-modes-and-their-antidotes)
5. [Opponents Worth Reading: AI and Personalities](#5-opponents-worth-reading-ai-and-personalities)
6. [Factions, Doctrines, Crossover](#6-factions-doctrines-crossover)
7. [The Physics: Armor, Warheads, the Pipeline](#7-the-physics-armor-warheads-the-pipeline)
8. [Game Modes](#8-game-modes)
9. [Maps, Fog, and Intel](#9-maps-fog-and-intel)
10. [UX and Readability](#10-ux-and-readability)
11. [Competitive Landscape](#11-competitive-landscape)
12. [Anti-Patterns](#12-anti-patterns)
13. [Prioritized Roadmap](#13-prioritized-roadmap)
14. [Sources](#14-sources)

---

## 1. The Question

v7 asked "how does Cameo catch Combined Arms?" — the wrong question. CA's polish is a precondition, not
a differentiator. The right question is the one its own players eventually ask of every RTS:

> **Why is this match interesting?**

The genre's answer, tested over twenty-five years, is consistent: interesting matches are made of
*decisions that could have gone otherwise* — scouting that pays off, commitments with real costs,
counter-moves the opponent can see coming and answer. Brood War's three factions each played a
different game on one map, and the matchup — not the faction — was balanced
([Aside House](https://asidehouse.com/respawn/starcraft-brood-war-the-rts-as-a-national-sport/);
[TeamLiquid asymmetric-balance analysis](https://tl.net/blogs/297358-a-theory-on-asymmetric-balance-in-brood-war)).
That is the bar: **asymmetric enough that every faction pairing is its own metagame, legible enough
that a player always knows why they lost.**

Cameo's structural advantage is that it can express more distinct strategies than any shipped RTS:
28+ factions across 7 universes on one ruleset. The risk is the same number: 28 factions is a
design surface nobody can hold in their head. Everything below is about keeping the surface *legible*
while the depth stays real — the same move DESIGN.md's laws already make on the data side (§11b's
one warhead, the role language, the uniqueness law).

### The five pillars (kept from v7, sharpened)

| Pillar | The test | Where it lives |
|--------|----------|----------------|
| **Identity** | Can I name this faction's plan blindfolded? | §6 doctrine branching; `FACTION_IDENTITY.md` |
| **Counterplay** | Did the opponent have an answer they could have found? | §7 Versus matrix; DESIGN §10 hard counters |
| **Agency** | Was the loss a decision or an inevitability? | §4 anti-snowball; §9 fog honesty |
| **Emergence** | Could this have happened in the designer's office? | §5 learning architecture; §8 modes |
| **Readability** | Do I know what just happened and why? | §10 unit cards, telegraphs, codex |

**The binding rule (unchanged):** *Complexity must create choices, not homework.*

---

## 2. What Twenty-Five Years of RTS Prove

This section is the research digest: what each long-lived or instructive title actually proved, and
the recommendation Cameo takes from it. Skip the nostalgia; take the mechanism.

### 2.1 StarCraft / Brood War — asymmetry is the metagame engine

- **Three factions, three different games.** Terran is siege-and-fortify, Protoss is expensive
  precision, Zerg is swarm tempo — economies, movement, and win conditions diverge, not just unit
  rosters. Every pairing developed its own named strategies
  ([Aside House](https://asidehouse.com/respawn/starcraft-brood-war-the-rts-as-a-national-sport/)).
- **Balance is cyclical, not static.** Brood War was never patched flat; the metagame moved around
  the imbalances (sAviOr's ZvT, Bisu's PvZ). "P > T > Z > P" was the community's shorthand for a
  rotating advantage that kept play alive for a decade
  ([TeamLiquid](https://tl.net/blogs/297358-a-theory-on-asymmetric-balance-in-brood-war);
  [Esports Heaven](https://www.esportsheaven.com/features/brood-war-was-never-balanced/)).
- **Skill expression kept the counters honest.** Mutalisk stacking, mine tricks, pixel dodges —
  player-found interactions the designers never authored. Emergent tech is a feature budgeted in
  QA time, not a bug.

**For Cameo:** the role system (§5.2) and faction bias table already encode asymmetry; the missing
piece is *matchup-level* metagame identity. Recommendation: the bot meta-learning spec
(`SPEC_2026-10-05_bot_meta_learning.md`) treats each `faction × doctrine × archetype` as its own
measurable cell — the instrumentation for "every pairing is its own metagame" already exists in the
engagement log (DESIGN §19.13).

### 2.2 Age of Empires II — scope discipline and the readable counter

- The Ensemble postmortem is a warning: the sequel's wish-list nearly killed the game until they cut
  back to the core loop ([GameDeveloper postmortem](https://www.gamedeveloper.com/design/postmortem-ensemble-studio-s-age-of-empires-ii-age-of-kings)).
- AoE2's counter matrix is *legible* — spears beat cavalry, skirmishers beat archers — and survived
  25 years of civ additions because each new civ speaks the same counter language
  ([AoE2 counter reference](https://aoe2db.com/counters)).

**For Cameo:** identical to the Versus-row law — every weapon reads its effectiveness off armor
class, and every armor class is reachable (DESIGN §11c, the cross-warhead law; the
"every warhead damages every armour" contract in the balance pipeline). Recommendation: publish the
armor-ladder picture in the War Codex (§10) — the counter language is already data, make it visible.

### 2.3 Warcraft III — heroes as commitments, creeps as incentives

- WC3's heroes made small armies carry big decisions: leveling, items, revival timing
  ([Ars Technica](https://arstechnica.com/features/2020/01/how-warcraft-iii-birthed-a-genre-changed-a-franchise-and-earned-a-reforge-ing/)).
- Creep camps gave non-mirror reasons to fight for map middle ground before the armies met — a
  mid-map *incentive gradient*, which is the generalizable lesson.

**For Cameo:** the commander tree and promotion system already provide per-unit stakes (DESIGN §19.x,
v7 §12.4); anomalies and tech sites (v7 §14.4) are the mid-map incentive gradient. Recommendation:
treat every contested map site as a *designed* incentive (defensible + pays out over time), not set
dressing.

### 2.4 Company of Heroes — the anti-turtle engine

- Territory that only pays when connected to home + victory points that force a fight for map
  control ended the "dig in forever" endgame ([GameDeveloper on CoH](https://www.gamedeveloper.com/design/on-company-of-heroes);
  [Matchsticks victory-condition analysis](https://www.matchstickeyes.com/2010/12/21/how-to-design-victory-conditions/)).
- A deformable battlefield (cover from craters, destroyed garrisons, demolished bridges) made
  position a decision with a half-life.

**For Cameo:** OpenRA's resource model is point-harvest, not territory — the direct port is
*map-design* (expansion spacing, contested tech sites) plus one cheap mechanic: **depleting
resources push the army outward** (already true in C&C). Recommendation: front/back base placement
(DESIGN §19.15) is the bot-side equivalent — production faces the front, valuables hide; the
*player-facing* equivalent is expansion pressure. A VP-control game mode is a cheap experiment
(§8) — the score-decay rule makes stalemates impossible by construction.

### 2.5 Supreme Commander / BAR — the economy IS the game; deathball is the risk

- SupCom's two-phase rhythm (expand/turtle → clash/tipping point) shows what happens when armies
  grow without positional pressure: one mega-ball decides everything
  ([Wayward Strategy on RTS phases](https://waywardstrategy.com/2015/06/07/time-as-a-resource-part-2-multiplayer-map-design/)).
- BAR's lesson for Cameo is tooling: every AI behavior knob lives in editable data files
  (`behaviour.json`, `factory.json` — [BAR AI deep-dive](https://deepwiki.com/beyond-all-reason/Beyond-All-Reason/8-ai-system)).
  A learner can only tune what is already a knob.

**For Cameo:** the knob language already exists — `build_order_knobs.yaml` (8 bounded multipliers,
preset × learned × jitter, DESIGN §19.2) and `plan_bandits.yaml` arms. Recommendation: every new
playbook entry must be a knob-vector, not code — the LEARN-SPEC design adopts this verbatim.

### 2.6 Red Alert 2 / Generals — faction fantasy as content

- RA2's factions were *fantasies* first (Soviet brute force, Allied tech, Yuri mind games); subfaction
  mods proved how far the fantasy axis stretches — Mental Omega shipped 12 subfactions (4 sides × 3)
  and a 133-mission campaign on the RA2 engine ([mentalomega.com](https://mentalomega.com/index.php?page=mod);
  [PC Gamer](https://www.pcgamer.com/let-red-alert-2-mod-mental-omega-expand-your-mind/)).
- Generals' three asymmetric economy models (Chinook logistics, GLA scrap, USA tech) showed economy
  *shape* is a faction-defining axis, not just unit stats.

**For Cameo:** this is the existing doctrine-branching thesis (v7 §3.4, kept in §6): in-match
mutually-exclusive doctrines beat lobby subfactions because the choice responds to the game.
Mental Omega's lesson *against* us: it succeeded partly by shipping a massive campaign — content
volume is a moat Cameo cannot match soon; the counter is modes + AI variety (§5, §8).

### 2.7 The recent wave — Stormgate, ZeroSpace, Tempest Rising

- **Stormgate**: postmortem verdict is scope — "tried to do too much" with VC expectations
  ([postmortem interview](https://www.youtube.com/watch?v=wsTNt7oy0gM)); launched mixed despite
  Blizzard pedigree ([PCGamesN](https://www.pcgamesn.com/stormgate/launch-feedback-response)).
  Lesson for Cameo: **sequencing** — the roadmap (§13) keeps each phase small enough to land.
- **ZeroSpace**: shipped the interesting experiment — one shared galactic-war map where solo, co-op,
  PvE and PvP matches all move a front line ([playzerospace.com](https://playzerospace.com/);
  [PC Gamer](https://www.pcgamer.com/games/strategy/zerospace-is-an-impressively-ambitious-rts-rpg-featuring-a-helldivers-2-inspired-galactic-war-but-the-streamer-pandering-is-a-real-bummer/)).
  Lesson: **persistence between matches makes modes cohere** — for Cameo this is the learned-files
  channel and (eventually) a multiverse-map campaign layer, not an MMO.
- **Tempest Rising**: built feeling-first on 90s-C&C nostalgia; the designer's own framing is "the
  feeling comes first" ([Wayward Strategy interview](https://waywardstrategy.com/2023/07/07/re-sharing-my-tempest-rising-interview/)).
  Lesson: nostalgia sells the first launch; systems keep the second hundred hours.

### 2.8 C&C mods — what the lineage already merged

Cameo's AI is literally assembled from these mods' bot projects (v7 §15.3): Romanov's Vengeance and
Combined Arms fully merged; Crystallized Nexus and Fransbot partially running. The design assets this
inherits are not theoretical:

- **Mental Omega** proved asymmetric subfactions + a huge campaign sustain a mod for 15+ years; its
  balance-mod origin mirrors Cameo's pipeline-first culture ([CNCNZ interview](https://cncnz.com/features/other-specials/mental-omega-mod-interview/)).
- **Combined Arms** proved a polished OpenRA crossover earns press ("the best new Command & Conquer
  game in over a decade" — [Polygon](https://www.polygon.com/pc/503559/command-and-conquer-combined-arms-mod/)).
- **Romanov's Vengeance** contributes RA-flavored content merged into the same trait stack.

**For Cameo:** the crossover is already deeper than any of these — the framework's job is making the
*crossing* readable, not adding more roster.

---

## 3. The Interesting-Match Machine

The thesis compressed into one loop. Every system in §5–§10 exists to keep this loop alive:

```
        ┌─────────────────────────────────────────────┐
        │                                             │
        ▼                                             │
   SCOUT ──► READ ──► COMMIT ──► CONTEST ──► ADAPT ───┘
   (fog-honest)  (signature)  (doctrine/   (counter)   (mid-game
                              build)                    switch)
```

- **Scout** — information must be purchasable and stealable. Fog-honest everywhere (DESIGN §19.5).
- **Read** — a build must be a *signature* a human or bot can name (LEARN-SPEC style space).
- **Commit** — doctrines, expansions, tech paths: expensive, legible, answerable.
- **Contest** — fights resolve through the Versus ladder and terrain, not blob-vs-blob (§7).
- **Adapt** — both players and bots switch plans when the read changes (§5 desire system, §19.11
  emergencies never rewrite posture).

A mode, map, unit, or AI feature earns its place by feeding this loop. That is the review question
for every future proposal.

---

## 4. Failure Modes and Their Antidotes

The genre's five recurring diseases, with the mechanism that treats each — and Cameo's coverage today.

### 4.1 Snowballing

Early wins compound: army advantage → map control → economy advantage → bigger army. The cure is
structural, not rubber-banding: **diminishing returns on the winner's advantages plus expanding
attack surface** ([Wayward Strategy, Anti-Snowball Design](https://waywardstrategy.com/2020/07/06/anti-snowball-design/);
[GameDeveloper balance-of-power](https://www.gamedeveloper.com/design/the-balance-of-power-progression-and-equilibrium-in-real-time-strategy-games);
[Sirlin on slippery slope](https://www.sirlin.net/articles/slippery-slope-and-perpetual-comeback)).

| Mechanic | How it checks the snowball | Cameo status |
|----------|---------------------------|--------------|
| Resource depletion | Winner must take more map → more surface to defend | Native (C&C harvest model) |
| Expansion vulnerability | Far bases are harrassable | Native; bot raid missions (BG switch) teach it to bots |
| Defender's edge | Chokes, high ground, garrisons | Terrain table (v7 §14.2); garrison cover |
| Production rebuild time | Losers can re-mass if they survive the hit | Native |
| Population/queue limits | Caps raw army-side compounding | Engine supply where factions opt in |

**Recommendation:** measure comeback rate in `stats_timeline` (already logged every 750 ticks —
earned/spent/army/assets/kills/deaths). A healthy match distribution shows win-probability crossings
after mid-game; if the A/B harness shows zero comebacks on a map, that map is the bug, not the
balance. No rubber-band mechanics — ever.

### 4.2 The deathball

One maximally-dense army that must be met with everything or nothing (SC2's documented failure —
[TeamLiquid deathball analysis](https://tl.net/blogs/369167-core-rts-design-and-the-deathball)).
Antidotes: positional damage that punishes density (splash, artillery arcs), multi-front pressure
that punishes concentration, and terrain that fragments approach.

**Cameo coverage:** the falloff/splash model is already in the balance pipeline (and now in the bot
predictor — `BM_live_combat_model`). Bot-side, the squad system's split/lane logic and the
formation-hysteresis work keep armies from clumping pathologically. Design-side: artillery and AoE
must stay priced as *anti-density* answers — the every-warhead-damages-every-armour law keeps splash
relevant against all armor classes rather than binary.

### 4.3 Turtling

Static defense that converts safety into inevitability. CoH's answer is canonical: make holding
ground cost map control, and score map control ([Matchsticks](https://www.matchstickeyes.com/2010/12/21/how-to-design-victory-conditions/)).

**Cameo coverage:** §19.15's front/back law puts production at the front (forcing the bot to defend
its own expansion line rather than cocooning); the siege evaluator and `CombatVetoBotModule` make
bots break defenses when ahead instead of sieging forever. Player-facing antidote: resource
depletion + a VP-control mode option (§8). Radar placement behind defended fronts (the BP rule)
already encodes "defenses guard information, not just ground."

### 4.4 Superweapon frustration

A nuke you never saw coming feels like dice, not defeat. The fairness contract from v7 survives
verbatim: **cost + telegraph + counter + risk + opportunity cost**. The bot adds one more: the
situation log already records superweapon sightings so the learned playbooks (§5) can price "they
go for the nuke" as an archetype feature.

### 4.5 The APM wall

High ceiling that keeps everyone below it out. Genre answers: control groups, attack-move, production
queues — and increasingly, *delegation* (Stormgate's buddy-bot ambition, ZeroSpace's effortless macro
note in [PC Gamer's review](https://www.pcgamer.com/games/strategy/zerospace-is-an-impressively-ambitious-rts-rpg-featuring-a-helldivers-2-inspired-galactic-war-but-the-streamer-pandering-is-a-real-bummer/)).

**Cameo coverage:** `HumanPaceBotModule` already paces bot action budgets to human-plausible rates
(difficulty table §19.1). The same delegation layer can serve humans: a co-op/sandbox assist mode
where a bot personality handles a subsystem (economy or harass) is a cheap mode with huge
accessibility upside.

---

## 5. Opponents Worth Reading: AI and Personalities

v7 listed six parent bots; v8 describes what makes an AI opponent *fun* — the actual design target.

### 5.1 What the research says a fun bot needs

1. **Readable intent.** Players enjoy opponents whose behavior they can interpret — the RTS teammate-
   bot study found communication of intent was universally rated the most important feature
   ([Senth et al., Communicating/Controllable Teammate Bot](https://portfolio.senth.org/A%20Communicating%20And%20Controllable%20Teammate%20Bot%20for%20RTS%20Games.pdf)).
   Intent-legibility generalizes to enemies: a rush that scouts read as a rush is *design*.
2. **Personality as bias, not script.** Utility-scored points-of-interest with personality-shaped
   weights produce varied-yet-coherent play ([ISART decision system](https://www.isart.fr/wp-content/uploads/2024/10/A-New-Decision-Making-System-in-Real-Time-Strategy-Games_CNRS_RTS_ISART.pdf));
   authored-but-jittered knobs beat identical clones.
3. **Fairness is non-negotiable.** No illegal vision, no hidden multipliers — the bot should be a
   credible player, not a cheat with a name tag
   ([phatryda's RTS-AI design notes](https://phatryda.com/ai-strategies-for-real-time-strategy-games/)).
   This is already Cameo law: DESIGN §19.5 fog honesty, §19.8 orders-only actuation, §19.1 difficulty
   by competence not income.

### 5.2 The Cameo realization — temperament, posture, playbook

The LEARN-SPEC spec (fleet `SPEC_2026-10-05_bot_meta_learning.md`, lead's addendum §17) splits
"personality" into three layers — this document adopts the split as design language:

| Layer | What it is | Lifetime | System anchor |
|-------|-----------|----------|---------------|
| **Temperament** | Bias vector in style space — the bot's identity | Fixed per match (bounded drift under sustained failure) | personality preset rows in `build_order_knobs.yaml` |
| **Posture** | Current stance — today's "personality" re-pick | Re-picked ~1500 ticks; hysteresis; emergencies pressure it | `BotSituation`; the §13 desire integrator |
| **Playbook** | Strategy: opening, compositions, timings | Chosen at start (bandit); switched mid-game on a recognized archetype | `PlanBanditBotModule`; `playbooks.yaml` (proposed) |

Selection rule (addendum §11): `score = learned value + temperament affinity + exploration` — all
personalities share the same evidence tables; affinity is capped so temperament can never hold a
losing plan. This is the shipped-bot lesson: UAlbertaBot's bandit exploring into losses was
correctable only because its fallback primary stayed strong
([Steamhammer/UAlbertaBot writeups](https://satirist.org/ai/starcraft/blog/archives/1163-AIIDE-2021-what-UAlbertaBot-learned.html)).

### 5.3 The desire system — gradient, not binary

Squad posture becomes a leaky integrator per stance (attack/defend/retreat/regroup/harass/reinforce):
`d += rate·(urgency − d)` each tick; switch only on margin + dwell (addendum §13, `BN_squad_desire`).
This is the same maths as an EMA — smooth where the old binary attack/defend thrashed. The AR-S
order-rate trace is the benchmark: evaluate, don't assume indecision caused the stutter.

### 5.4 Director pacing for co-op and survival

Left 4 Dead's AI Director is the canonical dynamic-pacing design: estimate player intensity, build to
peaks, then force relax periods — "peaks and valleys," never flat fatigue
([Mike Booth, AIIDE 2009](https://valvearchive.com/Presentations/AIIDE%202009/ai_systems_of_l4d_mike_booth.pdf);
[GDC 2009 co-op talk](https://cdn.cloudflare.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf)).
`BotDirector` already exists in the codebase — the design commitment is that co-op/survival waves use
director pacing (intensity estimate → build/sustain/fade) rather than timer-uniform waves. This is
the single highest-leverage design import for the co-op mode in §8.

### 5.5 Learning: where the tiers stand

DESIGN §19.13's five tiers, mapped into the framework's language:

| §19.13 tier | Framework role | Status 2026-10-05 |
|-------------|----------------|-------------------|
| 1. Measured priors | The "counterplay is real" evidence layer | `EngagementPriorsBotModule` + PRIORS-CARRY decay |
| 2. Combat veto | "Variety proposes, the veto disposes" — stops suicide commits | `CombatVetoBotModule` (AN switch) |
| 3. Bandits | Playbook/posture selection | `PlanBanditBotModule` (personality + plan arms, pooled) |
| 4. Tuned knobs | Macro shape (tempo/greed/expansion…) | `BuildOrderKnobsBotModule` |
| 5. Engagement network | Tactical choice (assault/flank/siege/harass) | deferred until ~50k logged engagements |

Meta-learning (observe → confirm → novelty → library → counter → forget) sits on top of tiers 1–4:
spec written, awaiting approval; `BN_meta_learning` default-off. Multiplayer safety is structural:
learning writes only post-match on the single log-owner process — nothing learned mid-match crosses
clients.

---

## 6. Factions, Doctrines, Crossover

Condensed from v7 §§6–9 — the conclusions survive, the repetition does not.

### 6.1 Doctrine branching beats lobby subfactions

The core argument (kept): a mutually-exclusive in-match doctrine is a *strategic decision with
timing*, where a lobby subfaction is a pre-match coin-flip. RA1 Soviets' reference tree (Tesla /
Mass-Production / Nuclear / Incendiary / Defensive → L2 sub-paths) demonstrates ~9 paths from one
faction — the Mental Omega subfaction payoff without the lobby surface area.

**The binding rule:** *Depth over width — one faction with branching in-game doctrines creates more
strategic variety than three lobby subfactions.* Extends to every core faction; Steel Consortium's
nine doctrines are the proof-of-concept already shipped.

### 6.2 The crossover loadout

Core faction → License → Relic → Detachment (v7 §9.1) stays, with the complementarity rule doing the
work: a crossover adds a *role the faction lacks*, never a better version of what it has (GDI+Protoss
adds teleport, not a better heavy tank). Three levels — Competitive (Core+License), Convergence
(full), Campaign (story) — give balance a bounded surface and chaos a sanctioned room.

### 6.3 Roster tiering

Core Ladder (strict, AI-covered) / Showcase (soft, swingy) / Museum-WIP (none) — unchanged; the
tier labels are how the framework stops the roster's size from becoming homework (the pillar test).

---

## 7. The Physics: Armor, Warheads, the Pipeline

v7 §§6, 10, 11 collapse into one section — they are one system seen at three altitudes.

### 7.1 The contract

- **LEVEL × PROFILE**: step-law down a 17-armor ladder; the order of armor types is the role language.
- **§11b one warhead per weapon**: readability made law — one weapon means one readable interaction;
  the predictor, the fitter, and the codex all inherit the same simplification.
- **Every warhead damages every armour**: no hard immunities on the ladder — counters are *gradients*
  (the soft-counter table: 3:1 strong … 1:3+ hard), which is why deathball insurance (§4.2) and
  faction-flexible doctrines (§6.1) work.
- **§11c cross-warheads**: faction upgrades swap basic deliveries for faction-tech deliveries —
  doctrine depth expressed in the warhead layer, so an upgrade is a *change in counter shape*, not
  +10%.

### 7.2 The balance pipeline as design infrastructure

`extract_stats → ledger → apply_balance` plus the effective-damage model (reliability, splash
footprint, charge-up, dead-zone — the same terms the bot predictor now reads under
`BM_live_combat_model`). The pipeline's job in framework terms: **keep the counter gradient true**,
not make everything equal — asymmetry survives because pricing is on the ladder, not the roster.

### 7.3 Measurement stack

`cameo-ai-matches.jsonl` (per-bot: personality/composition timelines, episode boundaries with
kills/deaths cost, arsenal ledger of created/lost/killed_value by victim, order-gate telemetry) +
`cameo-ai-engagements.jsonl` (per closed fight, `seen`/`truth` split per §19.13) — this is the
evidence layer every recommendation in this document is measured against. The gap the learning spec
closes: opponent-side signatures (first-production ticks, waves, expansions) are not yet logged for
all players.

---

## 8. Game Modes

| Mode | The interesting-match question it answers | Notes |
|------|-------------------------------------------|-------|
| **1v1 ranked** | Is my plan better than yours? | Core factions only; the ladder needs the comeback-rate metric live (§4.1) |
| **Co-op vs AI** | Can we read and answer together? | Director-paced waves (§5.4); teammate communication pattern from the Senth study |
| **Survival** | How long can the line hold? | L4D intensity curve is the wave law; escalating archetype mix |
| **Objective/scenario** | Can you solve a situation, not a build order? | Asymmetric objectives (defend-the-convoy, hold-N-sites); campaign missions ride this |
| **VP control (experimental)** | Can you hold the map, not just the base? | CoH's anti-stalemate rule; cheap to prototype on existing caps |
| **Chaos FFA** | Can you ride entropy? | Full roster; anomalies sanctioned here |
| **Spectator/replay** | Can I *see* the story? | Situation/engagement logs are the replay-commentary substrate; readable intent (§5.1) is what makes Cameo castable |

Mode rule: **one mode's balance budget never raids another's** (v7 anti-pattern kept) — Chaos does
not pay for ladder tuning, and ladder does not ban the fun.

---

## 9. Maps, Fog, and Intel

- **Map as incentive gradient**: central conflict zone + flanks + contested tech/resource sites
  (v7 §14.1 kept). Every site needs a payout *and* a defensive cost — the WC3 creep-camp lesson.
- **Fog discipline**: observed → remembered → inferred → predicted → decision (v7 §14.3) is now
  enforced structure: `audit_fog_honesty.py` is a zero-tolerance gate; the live predictor reads only
  what a human could see; internally-resolved random factions are never treated as publicly known.
- **Terrain table** (v7 §14.2): movement/cover/visibility per type stays; the design ask is that
  *every* contested area offers a cover/flank asymmetry so fights aren't decided in open field.

---

## 10. UX and Readability

- **Semantic unit cards** (v7 §17.1 kept): role, armor class, "strong vs / weak vs" in the ladder's
  own language — the card is generated from the same Versus data, never hand-written.
- **The War Codex**: the armor ladder, delivery families, doctrine trees — readable *because* §11b
  and the role law keep the space small enough to print.
- **Telegraphs**: every extreme mechanic carries cost + tell + counter + risk (fairness contract,
  §4.4); readable intent applies to units and bot postures alike.
- **Silhouette/effect grammar** (v7 §17.4): shield ≠ armor ≠ bio; faction color + universal role
  icons. Necessary because 28 factions × crossovers is unreadable otherwise.

---

## 11. Competitive Landscape

Condensed: v7's gap analysis was correct in content, wrong in framing.

| Combined Arms has | Cameo has | The reframe |
|-------------------|-----------|-------------|
| Polish, campaign, ladder, press | Roster breadth, pipeline, AI depth | CA's assets are *finishable* gaps; Cameo's are *structural* advantages |
| 5 factions × 4 subfactions | 28 factions × in-match doctrines | Doctrines answer the same need with less lobby surface |
| Familiar C&C feel | Multiverse fantasy | "Dream matches" are the pitch — Zerg vs Soviets is un-copiable |

**Stability stays P0** — v7's honest starting point stands: the README's "extremely buggy" line is
the project's own admission; nothing in this document outranks crashes, boot gates, or desyncs.
That is a hygiene bar, not a strategy.

---

## 12. Anti-Patterns

The v7 list plus three research-added rows:

| Anti-pattern | Why it kills the dream |
|--------------|----------------------|
| Infinite faction merge without tiers | Nobody learns the game |
| Balancing only HP/DPS | Loses experiential design; counter gradient goes flat |
| Copy-paste "tank line" across universes | Asymmetry dies |
| Superweapons without tells | Feels random |
| AI strength = cheaper tanks + map hack | Players learn wrong lessons; §19.5 law broken |
| Lore excuses for broken interactions | Crossover unreadable |
| One balance target for all modes | Competitive and Chaos both suffer |
| Mirror factions | Dull — uniqueness law |
| Features before stability | Crashes kill the community |
| **Rubber-band comeback mechanics** | Punishes winning play; snowball is fixed by structure, not pity |
| **Per-player learning** | Hard rule (maintainer §15): never key stats on who the opponent was — signatures only |
| **Scope that outruns the team** | Stormgate's postmortem lesson; every phase must be landable |

---

## 13. Prioritized Roadmap

Ordered by (evidence it unblocks × leverage) — not by excitement.

| # | Item | Why now | Maps to |
|---|------|---------|---------|
| 1 | **Stability + boot gates stay P0** | Everything else is built on not-crashing | §11 |
| 2 | **Finish §19.13 tiers 1–4 live-verified** | The evidence layer every later decision needs | §5.5 |
| 3 | **Predictor parity lands (BM switch)** | Same damage language for bot and pipeline = counter decisions that mean something | §7.2 |
| 4 | **Opponent-signature fields in match log (schema 3)** | Unlocks meta-learning's confirm channel | §5.5, §3 |
| 5 | **Meta-learning spec → impl (BN_meta_learning)** | The differentiator: bots that learn META from opponents | §5.2 |
| 6 | **Director-paced co-op/survival mode** | Cheapest big-content win; L4D pacing is proven | §5.4, §8 |
| 7 | **Doctrine trees for the remaining core factions** | Turns roster breadth into per-match decisions | §6.1 |
| 8 | **VP-control mode prototype** | Anti-stalemate insurance for ranked | §4.3, §8 |
| 9 | **War Codex + semantic cards wired to resolved data** | Readability pays for crossover scale | §10 |
| 10 | **Assist-mode delegation (bot runs a subsystem)** | APM wall; doubles as co-op onboarding | §4.5 |
| 11 | **Squad desire system (BN_squad_desire) + request board** | Posture as gradient; squad-driven reinforcement | §5.3 |
| 12 | **Campaign: three-mission Singularity arc** | The crossover fantasy needs a story vehicle — after modes prove the loop | §8 |

Deferred consciously: full campaign scale (Mental Omega economics don't apply to a small team),
MMO-style galactic persistence (ZeroSpace's bet, not ours — match logs give persistence cheaper),
any whole-game neural policy (§19.13's verdict stands), per-player learning (hard rule).

---

## 14. Sources

### Design theory & postmortems
- Wayward Strategy — Anti-Snowball Design: https://waywardstrategy.com/2020/07/06/anti-snowball-design/
- Wayward Strategy — Time as a Resource (RTS phases/map design): https://waywardstrategy.com/2015/06/07/time-as-a-resource-part-2-multiplayer-map-design/
- Wayward Strategy — Tempest Rising interview: https://waywardstrategy.com/2023/07/07/re-sharing-my-tempest-rising-interview/
- GameDeveloper — On Company of Heroes: https://www.gamedeveloper.com/design/on-company-of-heroes
- GameDeveloper — AoE2 postmortem: https://www.gamedeveloper.com/design/postmortem-ensemble-studio-s-age-of-empires-ii-age-of-kings
- GameDeveloper — Balance of Power (progression/equilibrium): https://www.gamedeveloper.com/design/the-balance-of-power-progression-and-equilibrium-in-real-time-strategy-games
- Matchsticks for my Eyes — victory conditions (CoH/RoN/Sins): https://www.matchstickeyes.com/2010/12/21/how-to-design-victory-conditions/
- Sirlin — Slippery Slope and Perpetual Comeback: https://www.sirlin.net/articles/slippery-slope-and-perpetual-comeback
- Gameflow/comebacks analysis: https://www.gamedeveloper.com/design/gameflow-some-pseudomathematics-about-comebacks-marginal-advantages-and-increasing-entropy-in-competitive-games

### StarCraft / Brood War
- Aside House — BW as national sport (asymmetry): https://asidehouse.com/respawn/starcraft-brood-war-the-rts-as-a-national-sport/
- TeamLiquid — asymmetric balance theory: https://tl.net/blogs/297358-a-theory-on-asymmetric-balance-in-brood-war
- Esports Heaven — "Brood War was never balanced": https://www.esportsheaven.com/features/brood-war-was-never-balanced/
- TeamLiquid — the deathball: https://tl.net/blogs/369167-core-rts-design-and-the-deathball
- SC2 asymmetry analysis (Halliday): https://simonhalliday.com/2019/09/04/starcraft-ii-a-study-in-asymmetrical-design/

### C&C mods
- Polygon — Combined Arms: https://www.polygon.com/pc/503559/command-and-conquer-combined-arms-mod/
- PC Gamer — CA five-faction mashup: https://www.pcgamer.com/this-mod-mashes-up-five-factions-from-command-and-conquers-past-into-one-giant-brawl/
- Mental Omega — official site/mod page: https://mentalomega.com/index.php?page=mod
- CNCNZ — Mental Omega interview: https://cncnz.com/features/other-specials/mental-omega-mod-interview/
- PC Gamer — Mental Omega campaign: https://www.pcgamer.com/let-red-alert-2-mod-mental-omega-expand-your-mind/

### Recent RTS
- Stormgate postmortem (Ask Alex): https://www.youtube.com/watch?v=wsTNt7oy0gM
- PCGamesN — Stormgate launch response: https://www.pcgamesn.com/stormgate/launch-feedback-response
- ZeroSpace — official: https://playzerospace.com/
- PC Gamer — ZeroSpace Galactic War review: https://www.pcgamer.com/games/strategy/zerospace-is-an-impressively-ambitious-rts-rpg-featuring-a-helldivers-2-inspired-galactic-war-but-the-streamer-pandering-is-a-real-bummer/

### AI opponents & pacing
- Mike Booth — AI Systems of Left 4 Dead (AIIDE 2009): https://valvearchive.com/Presentations/AIIDE%202009/ai_systems_of_l4d_mike_booth.pdf
- Valve GDC 2009 — Replayable Cooperative Design (L4D): https://cdn.cloudflare.steamstatic.com/apps/valve/2009/GDC2009_ReplayableCooperativeGameDesign_Left4Dead.pdf
- Senth et al. — Communicating/Controllable RTS teammate bot: https://portfolio.senth.org/A%20Communicating%20And%20Controllable%20Teammate%20Bot%20for%20RTS%20Games.pdf
- ISART — emotional/utility RTS decision system: https://www.isart.fr/wp-content/uploads/2024/10/A-New-Decision-Making-System-in-Real-Time-Strategy-Games_CNRS_RTS_ISART.pdf
- phatryda — RTS AI strategy notes (fairness/readability): https://phatryda.com/ai-strategies-for-real-time-strategy-games/
- BAR AI system (config-driven knobs): https://deepwiki.com/beyond-all-reason/Beyond-All-Reason/8-ai-system
- Steamhammer/UAlbertaBot learning writeups: https://satirist.org/ai/starcraft/blog/archives/1163-AIIDE-2021-what-UAlbertaBot-learned.html

### Cameo project (binding unless noted)
- `docs/DESIGN.md` — §11b one-warhead, §11c cross-warhead, §19 personalities/learning laws
- `docs/design/AI_ARCHITECTURE.md`, `docs/design/AI_MASTER_PLAN.md`, `docs/design/ROADMAP.md`
- Fleet: `BRIEF_2026-10-05_bot_meta_learning.md`, `RESEARCH_2026-10-05_rts_ai_learning.md`,
  `SPEC_2026-10-05_bot_meta_learning.md` (proposal, pending approval)
- `FACTION_IDENTITY.md`, `ARMOR_SYSTEM.md`, `VISION.md`, `FACTIONS.md`

---

*v8 cut scope from v7: the three redundant catch-up plans folded into §11+§13; the data-architecture
sketch dropped (the tree exists); marketing phases replaced by the mode/roadmap priorities that serve
the interesting-match loop. The doctrine-branching, crossover-loadout, roster-tiering, fairness-contract,
and codex conclusions all survive — they're the parts that were already working.*
