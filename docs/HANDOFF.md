# Cameo — THE HANDOFF

**This is the single entry point for anyone picking up work on Cameo — human or agent.**
Written 2026-08-23, re-verified against master at `e60aab63`. It supersedes every previous handoff document;
those are archived under [`history/handoffs/`](history/handoffs/) and must not be resumed from.

| you want to… | go to |
|---|---|
| know what to do next | §3 below, then [`design/ROADMAP.md`](design/ROADMAP.md) |
| know the balance program's state and who owns what | [`design/BALANCE_PROGRAM_PLAN.md`](design/BALANCE_PROGRAM_PLAN.md) §0, §0a, §1, §2 |
| know a binding rule before editing yaml | [`DESIGN.md`](DESIGN.md) |
| avoid a trap someone already hit | [`LESSONS_LEARNED.md`](LESSONS_LEARNED.md) |
| know the current bug counts | [`audit/SUMMARY.md`](audit/SUMMARY.md) |
| find which document owns a topic | [`README.md`](README.md) |

---

## 0. The one rule that makes all the others work

**Don't trust — verify.** Before you assert that anything is done, pending, blocked or missing:
grep the data, `ls` the file, run the tool, boot-gate the tree. When a summary (a ROADMAP line,
a status table, an older handoff, this file) disagrees with the artifact, **the artifact wins —
and then you fix the stale summary in the same commit.**

This is not a slogan. The 2026-08-23 documentation pass found, by running the tools:

* five pinned numeric claims had drifted from the tree, and one gate (`audit_balance_drift`) was
  RED while the committed report said "clean" — the report was three commits stale;
* `docs/audit/latest/` held **two** copies of every report under different names, because the
  repo had two audit runners with different filename conventions;
* `DESIGN.md` used the section id **§12.0a twice**, for two different binding laws;
* the retired 2000-step damage grid was still taught as law in eight documents, one skill and
  one audit script, four days after `formula.DAMAGE_STEP` became 100;
* eight board statuses in `BALANCE_PROGRAM_PLAN.md` contradicted that same file's own per-item
  headings.

None of that was visible by reading. All of it was one command away.

### Verify a claim, not a hash

Cloud and CI checkouts of this repo are **shallow** — `git log` starts at 2026-08-10, so
`git show <older-hash>` fails on most hashes the docs cite. That is a property of the checkout,
not of the history: the commits exist upstream. Either run `git fetch --unshallow` first, or
(better) verify the claim against the artifact — which is what §0 asks for anyway.

### Two more things you cannot resolve from the repository

* **`memory <name>` citations.** 36 references across the design docs point at an external,
  per-agent memory store. Nobody else can open them. Treat every one as **provenance only,
  never as authority** — if a memory carried a binding rule, that rule needs to be promoted
  into `DESIGN.md` before it counts.
* **`engine/` is not in this repository.** It is `.gitignore`d, has no `.git`, and
  `git ls-files engine` returns zero. Editing `engine/**` produces work that cannot be
  committed here and is deleted by the next `make all`. See §5.

---

## 1. Where the project actually is (verified 2026-08-23)

**The mission.** Cameo is a crossover RTS spanning the classic RTS games. The architectural goal
is **dynamic faction loading** — load only the factions the lobby picked, instead of everything
at boot (historical peak: 12 GB RAM, unplayable on 8 GB machines). Every faction therefore
becomes a self-contained ContentPack. Runbook: [`MIGRATION.md`](MIGRATION.md).

**Health.** Green, with one easy red.

| | |
|---|---|
| crash-class content (B8) | **0** |
| empty warhead types (boot NRE class) | **0** of 2680 weapons |
| dangling weapon refs / dangling inherit targets | **0** / **0** |
| `tools/tests` | **227 tests, all green** |
| cross-document consistency audit | 73 passed, 0 failed |
| balance-ledger drift | **0** — master re-extracted in `31e649b8` |
| ⛔ **pinned doc claims** | **4 of 19 drifted** — see §3.0 |

**The active front is the weapon rebuild, and pricing is deliberately NOT running yet.**
`BALANCE_PROGRAM_PLAN.md` §0a is the binding order, and the reason is measurable: a price is a
function of `K`, `K` is built from a weapon's warhead set and their `Versus` profiles, and both
are still scheduled to change across most of the roster. Pricing now means pricing inputs that
are about to be replaced.

```
W24  one damage warhead per weapon          927 fired weapons still carry 2+
 └─> W23  retrofit the legacy templates      1162 direct inheritors left; 1231 fired
 │        (its old "33-collision" blocker    weapons already reach a ^Warhead_* family
 │         is DISSOLVED — W24 removes it)
 └─> A5   retire the remaining inline-Versus weapons onto templates
      └─> class anchors → fit_class per class → W11 maintainer sign-off
           → targets written into the ledger → apply_balance --confirm → boot gate
```

⚠ **`apply_balance --confirm` is a NO-OP until targets are written into the ledger, and that
needs W11's sign-off.** Signed-off class anchors today: **0**. So no price in the tree is final,
and "run `--confirm`" is never the next step on its own.

Independent of that chain (different file sets, safe in parallel): the physical-state meter
items **W7, W9, W10**, and the superweapon track **W12**.

---

## 2. Before you touch anything

Read, in this order. This is the canonical order; [`README.md`](README.md) is its definition and
wins over any copy of it.

1. [`CLAUDE.md`](../CLAUDE.md) — the hard rules, loaded every session.
2. [`LESSONS_LEARNED.md`](LESSONS_LEARNED.md) — the traps, each one paid for.
3. [`AGENT_WORKSPACE.md`](AGENT_WORKSPACE.md) — workflow, evidence rules, commit gate.
4. **this file** — current state and the queue.
5. [`DESIGN.md`](DESIGN.md) — the binding contract. Read the sections your change touches.
6. [`design/ROADMAP.md`](design/ROADMAP.md) — the granular queue.
7. [`audit/SUMMARY.md`](audit/SUMMARY.md) — current counts by bug class.

Then the topic doc for your task, from the table in [`README.md`](README.md).

### The ten hard rules, in one place

Rules 1–2 are enforced by hooks in `.claude/settings.json`.

1. **Boot-gate every commit of engine content.** `launch-game.cmd` must reach the main menu:
   `perf.log` ends with `MenuPostProcessEffect.PostWorldLoaded`, and no NEW `exception-*.log`
   in `%APPDATA%/OpenRA/Logs`. Snapshot the log list **before** launching. Menu proof is
   grepping `perf.log`, not eyeballing its last line.
2. **Scoped `git add <files>` only — never `-A`, `.` or `--all`.** Other contributors have live
   uncommitted work in this tree.
3. **Never hand-edit a balance number.** Use the pipeline: `extract_stats` → ledger →
   `apply_balance --confirm`. `--confirm` requires a maintainer order.
4. **`Versus` lives ONLY in `^Warhead_*` templates.** Never change a warhead, `Burst` or
   `BurstDelays` without explicit permission.
5. **Weapon 3-way split:** preserve resolved behaviour (`Damage` verbatim, projectile fields),
   `find_empty_warhead.py` = 0, boot-gate per batch. Verify with
   `tools/audit/review_resolve_diff.py` (resolve before and after).
6. **One owner per file-set.** Check a file's mtime and `git log -3 <file>` for a live agent
   before editing. Re-verify others' commits before building on them. Never
   `git checkout -- .` or wide-add someone else's work.
7. **Rebuild C# before booting** if `OpenRA.Mods.Cameo/` or `engine/` changed. Stale DLLs crash
   the boot with `Cannot locate type: …Info`. See §5 for the engine pipeline.
8. **Audit reports regenerate via `bash tools/audit/run_all.sh` only** — a PowerShell `>`
   redirect writes UTF-16 and corrupts them.
9. **Underscore-only naming** — no hyphens in ids, files or fluent keys. (The single
   deliberate exception is the `cameo-content` installer mod, which must match the engine's
   `*-content` convention.)
10. **Sign the commit trailer with your OWN identity and your REAL model name** —
    `Co-Authored-By: Claude <model> <noreply@anthropic.com>`. The git author is a shared repo
    identity, so the trailer is the only provenance signal. **Never copy a trailer from a
    previous commit or from CLAUDE.md** — those are templates, and copying one makes a newer
    model misreport itself as an older one. A non-Claude agent signs as itself
    (`Co-Authored-By: Devin AI <devin@cognition.ai>`) and never appends the Claude trailer.

### The gate before every commit

```sh
python -m unittest discover -s tools/tests -t tools/tests   # all green (227 as of 2026-08-23)
python tools/audit/find_empty_warhead.py                    # 0
python tools/balance/verify_generator_sync.py               # ⛔ drift = 10 today; only
                                                            # ^Warhead_Sniper_Light is accepted
bash tools/audit/run_all.sh                                 # bash ONLY
python tools/balance/extract_stats.py --check               # 0 drifted
```

…then the boot gate (rule 1). If Windows Smart App Control blocks the launch, use one of the
four documented options in `LESSONS_LEARNED.md` § Smart App Control and **record the SAC state
in the commit message**. Never silently skip the gate, and never claim it passed when it did not.

`utility.cmd cameo --check-yaml` is a **separate lint tool**, not a boot-gate substitute. It
takes 10+ minutes; run it once you have finished a batch and expect 0 errors and 0 warnings —
not repeatedly.

---

## 3. The queue, in priority order

Crashes and player-visible regressions jump everything below.

### 3.0 — DO THIS FIRST (four pinned numbers have drifted)

`audit_doc_claims` is RED on **4 of 19** claims. Each is a number a DECISION rests on, and each
is quoted in prose somewhere:

| claim | documented | measured | why it moved |
|---|--:|--:|---|
| `shield_versus_mean` | 186.791 | 189.088 | the new Cryo/blend families changed the Shield column |
| `shield_hp_factor` | 0.535357 | 0.528855 | derived from the above |
| `live_damage_multipliers` | 366 | 353 | W26 has been deleting them — progress, not a bug |
| `plating_families` | 41 | 45 | four new families shipped; the plating matrix has no rows for them |

**The fix is not just editing `value:`.** Every claim in the registry carries a `docs:` key
naming each file that repeats the number — update `value` **and every one of those files in the
same commit**. That co-update is the entire point of the registry.

`plating_families` is the one needing judgement rather than find-and-replace: four families
exist that the plating matrix in `design/ARMOR_LAYERS.md` does not describe, so somebody has to
write their rows.

```sh
python tools/audit/audit_doc_claims.py     # documented vs measured, all 19
```

⚠ **The previous #1 item here — 9 drifted balance ledgers — is DONE.** Master fixed it in
`31e649b8`. `ledgers_drifted` is now pinned in the registry, so a recurrence goes red
immediately instead of being found by accident weeks later.

### 3.1 — The weapon rebuild (the main line)

⛔ **Set B (`mods/cameo/weapons/**`, `mods/cameo/ContentPacks/**/weapons.yaml`) is NOT free.**
Devin is working W2 in it — `IN PROGRESS (Devin, 2026-08-21)`, HeatRayBeam1-4 split, 28
`^LightFlameWeapon` matches left. Check `git log -3 <file>` and the file mtime before touching
anything in that set, and coordinate rather than assuming the 2026-08-15 lock release still
holds.

| step | what | how you know it moved |
|---|---|---|
| **W24** | collapse each fired weapon to ONE damage warhead (DESIGN §11b) | `multi_main_fired_weapons` in `doc_claims.yaml` goes DOWN from 927 |
| **W23** | retrofit the legacy templates onto `^Warhead_*` families | `unconverted_template_inheritors` goes DOWN from 1162; `warhead_family_reach` goes UP from 1231 |
| **A5** | retire the remaining inline-`Versus` weapons onto templates | rule 4 — `Versus` only in `^Warhead_*` |

Method for one W24 cluster, in order (this is the procedure that has worked for seven clusters
and is written out in full in `BALANCE_PROGRAM_PLAN.md` §1b):

1. **Resolve and INLINE first**, remove inherits second, clean up third. Never reorder an
   `Inherits` block "cosmetically" — position is semantic (see the trap list below).
2. Collapse the mains into one warhead at the SUMMED damage; keep the percentage twin
   consistent (`formula.percentage_twin`, **not** `damage // 2000`).
3. Preserve every effect the weapon had: physical state, trail, ground/air/water effects,
   smudges, `Report:`.
4. `tools/audit/review_resolve_diff.py` — before/after resolve must show only the intended
   change.
5. `find_empty_warhead.py` = 0 · `audit_warhead_split` at or below baseline ·
   `audit_physical_state_warheads` PASS · `audit_balance_drift` clean.
6. Boot-gate. Then commit yaml **and** ledgers, and lower the baseline in
   `audit_warhead_split.py` if it moved.

### 3.2 — Independent of the main line (safe in parallel)

| item | set | note |
|---|---|---|
| **W7** Sonic → `Resonance` meter | D (`rules/defaults.yaml`) | ⚠ set D is ONE file — serialise W7/W9/W10, never two at once |
| **W9** `^Poisonable` → `Poison` meter | D | same |
| **W10** `^Blindable` → `Blind` meter | D | unblocked, W6 shipped |
| **W12** superweapons as a separate track | — | maintainer-led; superweapons are not unit-priced |
| **Adopt the Sonic family** | B | `^Warhead_Sonic_*` bakes the mark but **nothing inherits it**, so it is inert. Needs a maintainer warhead order (rule 4). Law: an effect upgrade ADDS `^Warhead_Sonic_*`, it never replaces the base damage TYPE. |

### 3.3 — Bounded bug work (good for a short session)

From [`audit/SUMMARY.md`](audit/SUMMARY.md), smallest first:

1. **2 missing sequence images** (`audit/latest/sequences.md`) — player-visible, tiny.
2. **6 G1 garrison weapons** — armed garrison-capable infantry with no garrison weapon.
3. **1 unresolved fluent ref** — shows a raw key in-game.
4. **1 basebuilder faction without a crate** (28 of 29 covered).
5. **89 D1 duplicate-`Inherits` keys** — each one silently DROPS a template. This is the same
   family as the `Parent type X was already inherited` boot crash; triage before it bites.
6. **47 prerequisite-order violations** across 841 buildable combat actors.

### 3.4 — Documentation and tooling debt this pass left behind

* **`tools/audit/audit_damage_grid.py` is quarantined.** It still enforces the retired 2000-step
  grid and the retired `main // 2000` percentage twin, so it reports ~300 false findings and is
  deliberately excluded from `run_all.sh`. Re-derive it from `formula.DAMAGE_STEP` and
  `formula.percentage_twin`, then wire it in. It is the last of the three audits
  `audit_recent_changes` R2 flagged as unregistered (the other two are now in the suite).
* **`gen_sync` drift is 10, not 1** — and this one is real work, not bookkeeping. The accepted
  entry is `^Warhead_Sniper_Light` (a template the generator does not emit). The other **nine**
  are live disagreements introduced by the 2026-08-20 W24 chemical split, which edited the
  chemical warhead templates in `weapons.yaml` without updating the generator:
  `^Warhead_ChemCannon_{Light,Medium,Heavy}` and `^Warhead_ChemMissile_{Light,Medium,Heavy}`
  differ on `DamageTypes` (`TiberiumDeath` in the file vs `ExplosionDeath` from the generator)
  and on `Corrosion` (20/33 vs 50); `^Warhead_Chemical_{Light,Medium,Heavy}` differ on shape
  (`PhysicalStates:` map in the file vs `PhysicalStateName`/`PhysicalStateScale` from the
  generator). Decide which side is right per template, make the generator emit it, and then
  restate the expected drift in `BALANCE_PROGRAM_PLAN.md` §3 — the gate there still says
  "drift = 1", so it currently reads as passing when it is not.
* **`docs/design/invented_family_profiles.json` is stale, and regenerating it MOVES DATA.**
  Running `tools/balance/design_invented_profiles.py --write` today rewrites one family's
  `sharpness_intended`/`sharpness_shipped` (3.322 → 3.492) and its whole Versus row, because
  the inputs it derives from have moved since the JSON was committed. That is a balance change,
  not a documentation change — it needs the set-A owner and a boot gate, so this pass
  deliberately left it alone. (The count in the sheet is now derived from `len(DESIGNS)`
  instead of a hard-coded word, so it can no longer go stale on its own. To be clear: the
  sheet's "seven" is CORRECT — `Toxic` is the eighth family in the JSON but is **measured**
  from Cameo's own 28 gas weapons, not designed, so it is deliberately outside the table.)
* **`noid_resolved.json`** sits at the repo root as tracked UTF-16 with 79 209 null bytes — a
  PowerShell-redirect artifact. It is maintainer WIP, so it was left alone; it should be
  regenerated as UTF-8 or removed.
* **Comment-only mojibake** (`â€"` for an em dash) exists in a handful of `mods/cameo/**` yaml
  files. Cosmetic, comments only, another file-set's ownership — listed here so the next
  encoding sweep knows where to look.
* **36 `memory <name>` citations** across the design docs cannot be resolved by anyone but the
  agent that wrote them. Promote anything binding into `DESIGN.md`.

---

### 3.5 — Keeping the documentation from rotting again

Two audits now guard the docs themselves, and both run in `run_all.sh`:

| audit | catches |
|---|---|
| `audit_doc_claims.py` | a NUMBER in prose that no longer matches the tree. 19 claims registered in [`audit/doc_claims.yaml`](audit/doc_claims.yaml), each with the command that re-measures it. **When a claim legitimately changes, update `value` AND every file listed under its `docs:` key in the same commit.** |
| `audit_doc_health.py` | the documents being structurally broken: control characters, mojibake, a link to a missing file, an in-page `#anchor` link with no matching heading, a reference to a document that moved, two DESIGN sections sharing one id |

Neither existed before 2026-08-23, and every defect they check for was found by hand that
day. Add a claim to the registry the moment a decision starts resting on a number.

What they still cannot check is **prose contradicting prose** — a ruling written into one
document while the older statement stands in another. The only defence is the discipline:
**grep for the old claim before you write the new one, and strike it everywhere it appears.**

---

## 4. The traps that keep costing people time

Each of these is written up in full in [`LESSONS_LEARNED.md`](LESSONS_LEARNED.md). This is the
index — read the entry before working in that area.

| trap | one-line form |
|---|---|
| `Inherits` POSITION is semantic | the LAST node wins, and `Inherits` is a node. Appended at the BOTTOM, the parent silently overrides the definition's own values. Tools that add an inherit must insert at the TOP. |
| `Parent type X was already inherited` | reaching the same parent twice along ONE chain is a boot crash. The `@suffix` does **not** make it legal — the guard is keyed on the parent TYPE. Order-dependent. Grep cannot find it; `audit_duplicate_inherits.py` reports all instances in one pass. |
| Empty warhead type | `Warhead@X:` with no type = boot NRE, and `--check-yaml` does not catch it. `find_empty_warhead.py` does. |
| Removal markers | `-Key:` crashes if the key no longer exists in the resolved chain. Strip stale removals — nested ones too — before boot-gating a conversion. |
| Child weapons after a parent conversion | children that override the OLD warhead key create an orphaned second warhead → **double damage**. Sweep children after converting any parent. |
| Dead yaml files | `mods/cameo/**/*.yaml` includes files `mod.yaml` does NOT load. Audits must read `Ruleset(ROOT).manifest.rules`, never a glob. A dead file is not evidence about what ships. |
| A missing `Versus` row | is not "no opinion" — an empty match returns 100, so a plated unit LOSES its armor. Every plating gets a row in EVERY template. |
| An armor upgrade must never increase incoming damage | DESIGN §12.0e law 4. Guard: `audit_armor_upgrade_harm.py`. |
| Bulk renames | never do a bare-identifier substitution: the same literal is a weapon, an actor, a condition and a sprite in this tree. Match the exact YAML field with a full-token comparison. |
| Loose `*_extracted/` map folders | `.oramap` is a zip; the packaged file is what ships and silently shadows loose edits. Repack in the same session, then validate with `--check-yaml`. |
| UTF-16 audit reports | a PowerShell `>` redirect corrupts them. `run_all.sh` only. |

---

## 5. Changing the engine

**First check whether a mod-side SHADOW avoids the whole procedure.**
`ObjectCreator.FindType` takes the first assembly in `mod.yaml`'s `Assemblies` list that holds
the name, and the order is **AS, CA, Cameo, Cnc, D2k, Common** — so an `OpenRA.Mods.Cameo` type
of the same name wins with zero yaml changes. Precedents: `ColorPickerColorShift`,
`PlayerColorShift`, `SelectionDecorations`. **Prove a shadow works** by giving the Cameo Info a
field the engine type lacks and booting with that field set — `--docs` lists both types and
proves nothing.

If you really need an engine change:

1. Edit C# only in the **separate `cameo-engine` clone** of `github.com/cameo-mod/OpenRA`
   (branch `cameo-engine`). Never in `engine/` here.
2. Commit and push to `origin/cameo-engine`; check `git status` for stray nested-clone entries.
3. `git rev-parse cameo-engine` for the **full 40-character** hash. Never hand-type or truncate.
4. Set `ENGINE_VERSION="<hash>"` in **`mod.config`** (not `mod.yaml`).
5. `make.cmd all` — the version mismatch makes the SDK delete `engine/`, refetch and rebuild.
6. Verify `engine/VERSION` matches and the build has 0 errors. **Recreate any `engine/glsl/`
   shaders** — the fetch wipes them (e.g. `postprocess_nuclearflash.frag`).
7. Boot-gate, then commit `mod.config` together with the doc updates.

---

## 5b. The shape of the documentation set

**43 live documents.** Everything else under `docs/` is generated (regenerate it) or archived in
`history/` (what happened, never what is true now). [`README.md`](README.md) lists the whole live
set in one table — if a document is not in that table, it is not live.

The set was 83 documents on 2026-08-23. It came down by **merging overlapping documents**, not by
deleting content: every merged file's body was carried across verbatim under its own heading with
its original path recorded. The clusters that collapsed:

| now | was |
|---|---|
| `design/ARMOR_LAYERS.md` | 5 files — pseudo-armor, shield normalisation, 2 plating docs, superweapon layering |
| `design/PROJECTILE_AND_EFFECT_LAYER.md` | 3 — projectile templates, per-game sourcing, game-specific bases |
| `design/RESEARCH_NOTES.md` | 5 — SP research, mission win/lose, CABAL rebuild, SM artwork, tier-chain |
| `design/DECISIONS.md` | 3 — hex shields, vehicle queue split, derived stats in traits |
| `design/WEAPON_HEAVINESS.md` | 2 — the research and the continuous scale |
| `design/AREADAMAGE_WARHEAD.md` | 2 — the rebalance and the unified node |
| `reference/WARHEAD_REFERENCE.md` | 3 — family profiles, versus archetypes, archetype tables |
| `balance/formula_v2_classes.md` | 4 per-class logs + the delta audit |
| `design/BALANCE_PROGRAM_PLAN.md` §7 | `BALANCE_MEGAPLAN.md`, which had spent two weeks disagreeing with §0a about order |

13 stale generated per-class proposals were deleted rather than merged — they regenerate with
`propose_class_rebalance.py --class <name>`, and the committed copies no longer matched the tree.
Ten finished or dormant working notes moved to `history/`.

**If you are about to add a document, don't.** Add a section to the one that already owns the
topic — the table in `README.md` says which. A new file is justified only when no existing
document owns the subject, and then it goes in that table in the same commit.

---

## 6. What this handoff replaces

Every document below is archived, banner-stamped, and **must not be resumed from**. They are
kept for provenance and for the technique notes inside them.

| archived | was |
|---|---|
| [`history/handoffs/AI_AGENT_HANDOFF_2026-07-25.md`](history/handoffs/AI_AGENT_HANDOFF_2026-07-25.md) | session log for the 2026-07-24 yaml-lint incident |
| [`history/handoffs/SESSION_CHECKPOINT_2026-08-03.md`](history/handoffs/SESSION_CHECKPOINT_2026-08-03.md) | compaction anchor on a long-merged branch |
| [`history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md`](history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md) | the AreaDamage conversion (complete) |
| [`history/handoffs/AI_HANDOFF_2026-08-05.md`](history/handoffs/AI_HANDOFF_2026-08-05.md) | the weapon-work must-read CLAUDE.md used to point at |
| [`history/handoffs/CLAUDE_HANDOFF_2026-08-11.md`](history/handoffs/CLAUDE_HANDOFF_2026-08-11.md) | agent letter; became W15–W19 on the board |
| [`history/handoffs/DEVIN_HANDOFF_SP_RESEARCH_2026-08-11.md`](history/handoffs/DEVIN_HANDOFF_SP_RESEARCH_2026-08-11.md) | Shattered Paradise parity research |
| [`history/handoffs/DEVIN_REPLY_2026-08-11.md`](history/handoffs/DEVIN_REPLY_2026-08-11.md) | agent letter; its pipeline fixes shipped |
| [`history/MEGAPLAN_2026-08-08.md`](history/MEGAPLAN_2026-08-08.md) | thin program index, superseded twice over |
| [`history/ROADMAP_ARCHIVE_2026-07.md`](history/ROADMAP_ARCHIVE_2026-07.md) | 14 fully-closed ROADMAP sections |
| [`history/audits/`](history/audits/) | two one-off dated infantry audits |

**The rule that keeps this file from becoming one of them:** a handoff records STATE, and state
rots. When you finish a session, update **this** file — do not write a new dated one. If a
statement here disagrees with the tree, the tree is right; fix the sentence.
