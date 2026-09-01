# `docs/patches/` — yaml changes prepared where they could not be boot-gated

⛔ **Why patches instead of commits.** CLAUDE.md rule 1 and the commit gate are absolute: engine
content (anything the game parses at boot — all of `mods/**`) may not be committed without a boot
proof, and `tools/hooks/bash_guard.py` enforces it on `git commit`. A cloud container has no
`engine/` build and no `%APPDATA%/OpenRA/Logs`, so the gate is **unsatisfiable there, by design**.

The wrong answers are to disable the hook, or to leave the work in an ephemeral container's
working tree where it dies with the container. The right answer is this directory: the change is
authored, verified as far as a boot-less environment can verify it, and committed as a patch that
applies in one command on a machine that *can* boot.

**A patch here is not a decision.** It is a prepared change waiting on the boot gate and, where
noted, on a maintainer ruling. Apply, verify, boot, then commit the yaml — and delete the patch in
that same commit, so this directory never accumulates changes that already landed.

---

## Applying one

```bash
git apply --check docs/patches/<name>.patch     # dry run first, always
git apply         docs/patches/<name>.patch
# ... run the verification named in the patch's section below ...
# ... BOOT GATE: launch-game.cmd -> main menu, no new exception-*.log ...
git add <the yaml files>  docs/patches/<name>.patch  <any doc the section says to update>
git rm  docs/patches/<name>.patch
git commit
```

⚠ Never `git add -A` (CLAUDE.md rule 2) — other contributors have live WIP in this tree.

---

## `bot_insurance_01_fix_medium_and_human_parity.patch`

**Status:** ready. Fixes a live bug. Needs the boot gate; needs no design ruling.

### What it changes

`mods/cameo/ai/ai.yaml` — adds one condition grant to `^AIDifficulties`:

```
	GrantConditionOnBotOwner@campaign:
		Condition: campaignbot
		Bots: campaign
```

`mods/cameo/rules/defaults.yaml` — rewrites the eight `RequiresCondition` lines of the four
lowest rungs of the insurance ladder in `^AIConyardCash`, doing two things at once:

1. **`normalbot` → `mediumbot`.** `^AIDifficulties` grants `mediumbot` and never `normalbot`; the
   mod's only `normalbot` grant is on the Dark Reign building `drpplant1.freedomguard`
   (`darkreign.yaml:3348`) and conditions are per-ACTOR, so the ladder's host never saw it.
   `mediumbot` appeared in **none** of the ten rung expressions, so a `medium` bot — the default
   difficulty — received **zero** insurance income while `easy` got 3 rungs and `hard` got 5.
2. **`(!genericbot && !campaignbot)` added to the four lowest rungs**, opening them to human
   players. `genericbot` covers the ten selectable difficulties but deliberately **not** the
   eleventh bot type, `campaign`; several modules rely on that, so `campaignbot` is added above
   rather than widening `genericbot`, and campaign AI is explicitly excluded so scripted missions
   are not handed free income.

### Why human parity, and why exactly four rungs

Maintainer ruling, 2026-09-01: *"the players should also get the same insurance that the medium
bot gets."* The medium bot's rungs are `medium`, `easy`, `veryeasy`, `easiest` — so a human gets
those four, ramping 1 → 4 credits/tick as cash falls from 4000 to below 1000. This also sets the
target for cheat removal: humans already had a comeback floor of their own
(`player.yaml:243-262`), so **parity is one ladder, not zero** — the ladder must never simply be
deleted when the difficulty cheats come off.

### Verification (all of this was run before the patch was committed)

```bash
python tools/audit/audit_bot_insurance.py     # FAILS before, PASSES after
python tools/audit/audit_doc_claims.py        # bot_insurance_unreachable_difficulties: 1 -> 0
```

Measured rung counts, evaluated by resolving the real `RequiresCondition` expressions through
`miniyaml.Ruleset`:

| player kind | before | after |
|---|--:|--:|
| human | 0 | **4** |
| campaign | 0 | 0 |
| easiest | 1 | 1 |
| veryeasy | 2 | 2 |
| easy | 3 | 3 |
| **medium** | **0** ⛔ | **4** |
| hard | 5 | 5 |
| veryhard | 6 | 6 |
| brutal | 7 | 7 |
| challenger | 8 | 8 |
| unbeatable | 9 | 9 |
| cameogod | 10 | 10 |

⭐ **Every existing bot difficulty is unchanged.** Only the broken rung and the human column move.

### On commit, also

* set `value: 0` for `bot_insurance_unreachable_difficulties` in `docs/audit/doc_claims.yaml`
  (the registry's own rule: the number and the docs move in the same commit);
* strike the "medium gets zero" wording in `docs/HANDOFF.md`,
  `docs/design/AI_RESEARCH_RECONCILIATION.md` §1, `docs/audit/SUMMARY.md` and
  `docs/Cameo_Knowledge_Base_Manual.md`, and close **OD-G**.

---

## `bot_insurance_02_relocate_to_player_actor.patch`

**Status:** ⚠ prepared, **needs a maintainer ruling AND one in-game check** on top of the boot
gate. Applies on top of patch 01.

### What it changes

Moves all ten rungs — `BotInsurance` + `CashTrickler` + `ResourcePurifier` — out of
`^AIConyardCash` (`defaults.yaml`) and onto `Player:` (`player.yaml`). `^AIConyardCash` keeps its
two `Inherits@` lines and a note; the template name is kept because 47 actors inherit it and
renaming it is a 47-file change for no behavioural gain.

### Why the Player actor is the right host

* **`BotInsurance.cs` was written for it.** `Created` opens with
  `var playerActor = self.Info.Name == "player" ? self : self.Owner.PlayerActor;` — an explicit
  Player-actor special case, with the conyard as the fallback path.
* **The conyard placement multiplies the ladder by conyard count.** `BotLimits` lets `cameogod`
  build 7 (`ai.yaml:134`), and each conyard carries an independent ladder.
* ⭐ **And it switches the ladder off exactly when it is needed most.** A bot that loses its last
  construction yard loses the whole ladder and drops to `player.yaml`'s single `nobase` rung with
  a 60-second delay — which is precisely the *"stuck on no income and cannot rebuild"* case the
  feature exists to prevent. This is the strongest argument for the move: it is a bug fix, not a
  refactor.

### ⛔ The two things that must be settled first

1. **`ResourcePurifier` on the Player actor is UNVERIFIED.** The vendored `ResourcePurifierCA`
   (`OpenRA.Mods.CA/Traits/ResourcePurifierCA.cs`) carries the same `"player"` special case and
   even guards its floating text with `HasTraitInfo<IOccupySpaceInfo>()`, so it clearly expects to
   run there — **but the yaml says `ResourcePurifier:`, not `ResourcePurifierCA:`**, and with the
   assembly order `AS, CA, Cameo, Cnc, D2k, Common` that name resolves past CA to a type neither
   vendored in this repository nor present in a cloud container. **Confirm in a running game that
   purifier income is still credited after the move**, or split the purifiers back onto the
   conyard. Everything else is safe: `CashTrickler` on the Player actor is already proven in this
   very mod (`player.yaml:250`, `:258`).
2. **It is a balance change at the top difficulties.** For the common case — one construction
   yard — nothing changes. It removes a late-game multiplier that only bites when a bot holds
   several conyards *and* is broke. Whether that multiplier was intended is the maintainer's call.

### Verification already done

Resolved through `miniyaml.Ruleset` on a shadow tree with both patches applied:

* `Player` gains the 10 rungs and keeps its existing `secondaryinsurance` + `comeback` mechanics;
* `^Conyard` — the template all 47 conyard actors reach — drops to **0** rungs;
* `Player` already grants every `*bot` condition the rungs gate on (it inherits `^AIDifficulties`
  at `ai.yaml:144-145`), so no condition is left dangling;
* `audit_bot_insurance.py` PASSES, with rung counts **identical** to patch 01 alone — the move is
  behaviour-preserving in rung terms, which is exactly what a relocation should be.
