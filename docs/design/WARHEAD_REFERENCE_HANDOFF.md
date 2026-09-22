# Reference warhead pipeline — handoff

**Status: the measurement is finished and verified; the family assignment is 1 source of 17 done.**

This is a LANE handoff, not a dated one. It describes the live state of the WARHEAD and ARMOUR
side of the reference programme and is meant to be edited in place as the lane moves.

⚠ **This is not a second entry point.** `docs/HANDOFF.md` remains THE handoff and outranks this
file everywhere they touch; `AGENT_WORKSPACE.md` forbids a second roadmap or handoff, and this is
not one.

⚠ **Do not confuse it with [`REFERENCE_PIPELINE_HANDOFF.md`](REFERENCE_PIPELINE_HANDOFF.md).**
That file is the FACTION-ROUTING lane — which reference mod's units a Cameo faction draws on,
rulings R1–R15. This file is the WARHEAD/ARMOUR lane — what the Versus tables should be, rulings
R16–R38. Both draw on `REFERENCE_EXTRACTION_PLAN.md`, which is the single home for every ruling in
both; where a handoff and the plan disagree, the plan wins.

The binding rulings for this lane are **R1–R38 in `docs/design/REFERENCE_EXTRACTION_PLAN.md`**.
Read those before changing any tool here — several of them exist because a plausible-looking
shortcut produced confidently wrong numbers, and the numbers looked fine until a human queried
them.

## Contents

- [What this pipeline is for](#what-this-pipeline-is-for)
- [The five stages, and where each one stands](#the-five-stages-and-where-each-one-stands)
- [Run it](#run-it)
- [Where the data lives](#where-the-data-lives)
- [The next task, concretely](#the-next-task-concretely)
- [Open questions the maintainer has not ruled on](#open-questions-the-maintainer-has-not-ruled-on)
- [Traps that have already cost a session](#traps-that-have-already-cost-a-session)
- [Verification](#verification)

## What this pipeline is for

Cameo's warhead vocabulary should be **an average of the C&C-family mods it draws from**, so that
unit-versus-unit time-to-kill feels like those mods while the warhead list stays small and tidy.
The maintainer's own framing: *"in the end unit behaviour must be approximately identical — like
how many shots of a minigunner it takes to destroy a medium tank and vice versa… But we also try
to cleanly organize it with only a few warheads."*

Two numbers come out of it:

* an **armour matrix** — what our 16 armour rows should be worth, averaged over 20 sources;
* a **per-warhead Versus profile** for each Cameo warhead family.

Cameo currently ships **50 families**; Combined Arms alone needs **32**. Closing that gap is the
consolidation target, and the maintainer ruled it waits until every source has voted.

## The five stages, and where each one stands

| # | stage | tool | state |
|---|---|---|---|
| 1 | Read 20 sources into one normalised matrix | `warhead_matrix.py` | **done**, 0 degenerate |
| 2 | Map each source's armours onto our 16 rows, and average | `armor_interpolate.py` | **done**, all 20 mapped |
| 3 | Compress each source's weapons into review groups | `compress_warheads.py` | **done**, 17 of 20 |
| 4 | Assign each group to a Cameo warhead family | `warhead_family_assignment.yaml` | **1 of 17 sources** |
| 5 | Collapse to one row per Cameo warhead | `family_matrix.py` | done for Combined Arms |

Stage 4 is the bottleneck and it is the only stage that needs human judgement.

### Stage 1 — the measurement

20 sources across three dialects (OpenRA MiniYAML, Westwood INI, one transcribed table). Every
source normalises to a geometric mean of 100 inside a 20:1 window, so the matrices can be
multiplied together. `--check` compares each source's window centre against its own positive
median and reports **0 degenerate sources**.

Four corrections live in here that were each found by the maintainer reading output, not by a
test:

* **R23 — a weapon's profile is the SUM of its warheads, not its biggest one.** OpenRA fires every
  warhead on every hit and Combined Arms' extras are complementary, not twins: `FireballLauncher`
  states `Light: 0` on one warhead and `Light: 50` on another. Picking one reported false
  immunities. Fold by effective damage instead.
* **R24 — a percentage warhead is priced through the typical unit it hits.**
  `HealthPercentageDamage` resolves as `HP x Damage/100 x Versus/100`, so `Damage: 300` is a
  3x-overkill one-shot. Priced against the geometric-mean HP of each armour class.
* **R25 — reachability and coverage are per ARMOUR CLASS, not per GND/AIR macro.** `380mm`'s
  second warhead deals 70,000 with `ValidTargets: Infantry`; summing that into Heavy and Wood
  turned a siege gun into a tank killer.
* **R27 — an AA armament is not a separate weapon.** 39 of `MissileAA`'s 43 members were the air
  half of a dual-purpose unit. Folding the pair shrank the dedicated-AA families to what they
  should mean and gave every ground warhead a real Aircraft figure.

### Stage 2 — the armour matrix

Each source armour is anchored to a POSITION on one of our four ladders — exactly, fractionally
(`combined_arms.Light` sits at VEH 0.5), flat across a ladder, or excluded. Unpinned rungs fill by
log-linear interpolation, clamped to the §12.0 window. The averaged target, one mod one vote:

| ladder | | | | | |
|---|--:|--:|--:|--:|--:|
| **INF** | None 112.8 | Flak 108.7 | Plate 99.3 | | |
| **VEH** | Scout 132.7 | Light 116.7 | Medium 100.9 | Heavy 88.4 | Superheavy 77.4 |
| **BLD** | Wood 105.8 | Steel 85.9 | Concrete 66.5 | | |
| **AIR** | Fighter 118.1 | Bomber 114.5 | Helicopter 110.5 | Spaceship 106.3 | |

All four are monotonic, and that was **not** imposed — it emerged from 18 independent sources
after two averaging defects were fixed (R34, R35). `Heroic` stays derived per §12.0b.

### Stage 3 — compression

**1,721 groups across 20 sources, at `tau = 0.20` (R41), with the element vocabulary corrected (R45).** The threshold was 0.50 until
2026-09-22, chosen as "well below the 25th percentile" of the pairwise distances — a heuristic
with nothing scoring it. `validate_families.py` scores a grouping against Cameo's own 904
labelled weapons, and at 0.50 a group held a MEDIAN OF 3 distinct Cameo families: 61% purity,
which is the CEILING on any single-family label. 0.20 buys 75%. See R39/R41. The Westwood INI dialects needed their own signal extraction
(R38): they have no `Projectile:` or `DamageTypes:`, and their projectile NAMES are useless
(`InvisibleWork`, `CannonInviso` are rendering variants). Delivery comes from weapon flags plus
the `[Projectile]` section's behaviour; element from warhead flags with `InfDeath` only as a
guarded fallback.

Three sources do not compress, all by construction: `cameo` needs no compression (a
`^Warhead_<Family>_<Level>` template IS its family, R37), `cameo_resolved` reaches 173 of 174, and
`d2k_mod` is a transcribed table with no actors so it votes on the armour ladder only.

### Stage 4 — family assignment

**Combined Arms is complete and maintainer-reviewed**, and it SURVIVED the tau change intact:
118 groups became **182** at tau 0.20 and all 182 inherited a reviewed decision — 74 carried, 72
split, 36 all-overridden, **0 straddle, 0 unreviewed**. The acceptance test is not the counts but
that **all 360 CA weapons resolve to the same family before and after (0 changed)**. The 72
`split` rows are re-opened as `proposed`: the old review saw those weapons mixed in with others,
so their label is inherited rather than re-confirmed.

That worked only because the review is recorded per WEAPON, not per group — group names carry a
`_2`/`_3` suffix assigned by clustering order and shift whenever the compressor is re-run. Use
`retau_assignment.py` for any future threshold change; never re-apply this file by group name.

The other sources carry 1,509 groups. `propagate_families.py` inherits a family wherever the
Combined Arms review fixed one for the same delivery x element x band triple:

| | groups |
|---|--:|
| inherited cleanly | 298 |
| inherited, but CA split that triple | 249 |
| **no usable precedent — needs a ruling** | **512** |

## Run it

```
python tools/reference/warhead_matrix.py --check          # 0 degenerate sources
python tools/reference/warhead_matrix.py --write          # docs/reference/warhead_matrix.json
python tools/reference/warhead_workbook.py                # the 25-sheet xlsx
python tools/reference/armor_interpolate.py               # armour mapping + averaged target
python tools/reference/compress_warheads.py --all --write # 1,059 groups
python tools/reference/propagate_families.py --write      # family proposals for 16 sources
python tools/reference/family_matrix.py --write           # one row per Cameo warhead
python tools/reference/build_family_page.py               # the reviewable page
```

The full chain takes a few minutes; `--all` compression is the slow part.

⚠ **Order matters.** `family_matrix` reads the groups file AND re-reads the matrix, so re-run
`compress_warheads --all --write` after any change to `warhead_matrix.py`, then `family_matrix`,
then `build_family_page`.

## Where the data lives

| path | what | hand-edited? |
|---|---|---|
| `docs/reference/warhead_family_assignment.yaml` | the Combined Arms review — groups + per-weapon overrides | **yes, no generator** |
| `docs/reference/warhead_groups.json` | compression output, all sources | generated |
| `docs/reference/warhead_families.json` | one row per Cameo warhead | generated |
| `docs/reference/warhead_family_proposals.json` | propagated proposals for the other 16 | generated |
| `docs/reference/warhead_matrix.xlsx` | the per-source workbook | generated |
| `docs/reference/warhead_map.html` | the reviewable page | generated |

`warhead_family_assignment.yaml` deliberately has **no generator**, the same as
`peer_armor_map.yaml`: it is a series of judgements and every one has to be arguable. A generator
would invite someone to rebuild the decisions mechanically, which R21 forbids.

The review page is published at **https://claude.ai/artifact/2b4mMZydFnKiox9XKGB8s3** — update
that URL, never publish a new one.

## The next task, concretely

⭐ **REFRESH THE 10 BASE PROFILES FROM THE 20-SOURCE CORPUS.** This is the family-outward half of
the maintainer's 2026-09-22 ruling, and R42 measured why it is the whole job:

Every shaped family in the mod rests on ten measured ones — `Bullet`, `CannonAP`, `CannonHE`,
`Flame`, `Laser`, `MissileAA`, `MissileAP`, `MissileHE`, `Prism`, `Tesla` — because the other 27
live families are `BLEND_FAMILIES` and average their PARENTS' real profiles. Refreshing the ten
therefore propagates to all 47 at once.

And they are stale in a specific, checkable way. `docs/reference/family_profiles.json` was
generated on **2026-08-15** by `propose_family_profiles.py` over `survey_platforms.py` — the
OLD single-machine extractor that traces INI files out of `~/Downloads`, which nobody else has.
Its 31 entries carry **1 to 9 mods each**, gated at `min_rows: 8, min_mods: 3`. The pipeline in
this document carries **20 sources and 1,721 groups** and is hermetic. The numbers that ship were
never exposed to most of the corpus.

So: point the profile proposal at `warhead_groups.json` + `warhead_family_assignment.yaml`
instead of `survey_platforms`, re-derive the ten, and diff against what ships before writing
anything. ⚠ `gen_weapon_template` consumes `family_profiles.json` directly, so a re-derivation
moves live Versus tables — it is a `Versus` change and needs explicit permission (rule 4) and a
boot gate.

⚠ **THERE IS NO RAMP GAP TO FILL — do not go looking for one.** R42 measured the shipped
templates: 47 shaped, 2 flat BY DESIGN (`Magic`, `Sonic`, which ignore armour and are
special-cased in `class_tilt` AND the heaviness bell), 2 hand-tuned (`Nuclear_Super`,
`Sniper_Light`). Three separate estimates of a "33 ramp" gap were wrong because they inferred a
family's provenance from which JSON it appears in. Measure the template that ships.

### The other half — assignment for the remaining sources

⛔ **DO NOT GO LOOKING FOR A MATCHER. FOUR HAVE BEEN MEASURED AND THEY ALL SCORE ~20%.** R46 has
the numbers, scored against Cameo's 904 self-labelled weapons and the 337 maintainer-reviewed
Combined Arms ones:

| method | top-1 | shortlist |
|---|--:|--:|
| propagation by delivery x element x band | 17% | 34% |
| direct categorical construction | 20% | 38% |
| name evidence | 17% | — (40% precision on 42% coverage) |
| nearest Versus shape | 3–11% | 19% |

against a **75% median-purity ceiling**. The one that *should* have worked is the direct
construction — Cameo's vocabulary IS delivery x element, so `(Missile, Fire)` ought to be
`MissileFire`. It fails because the map is not injective: `Bullet`, `CannonHE`, `CannonAP`,
`Demolition`, `Concussion`, `Flak` and `Arrow` all measure as roughly `(Bullet, HE)`, and the
thing that separates them is the profile shape, which is the 3–11% method.

**So the 1,509 unassigned groups are a judgement task.** The maintainer authorised it being done
here for later review — *"fit everything from all mods into that reference table yourself and I
should try to review everything"* — but it cannot be automated and declared finished.

How to actually work a source:

1. `python tools/reference/propagate_families.py` and read that source's rows. Treat every
   proposal as a SHORTLIST entry (34–38% containment), never as an answer.
2. For each group, weigh the measured signals together — delivery, element, band, profile shape,
   `uses`, and the example weapon names. No single one decides it; that is what R46 measured.
3. Record the decision per WEAPON in `overrides:`, never per group name. Group names are an
   artefact of one compression run.
4. Re-run `compress_warheads --all --write` and confirm nothing was lost.

⚠ A per-weapon override BEATS its group, and an overridden weapon does not vote for its group in
any later migration (R41). That is what makes per-weapon records safe across re-clustering.

⚠ `warhead_family_assignment.yaml` holds Combined Arms only. A second source needs either a second
file or a `source:` key per block — the `source:` field already exists, so the decided layout is a
glob over `warhead_family_assignment*.yaml` keyed by it.

Usage order by group count: `mental_omega` 169, `red_resurrection` 147, `rise_of_the_east` 140,
`romanovs_vengeance` 100, `shattered_paradise` 90, `ra20xx` 86, `ra2_reborn` 82,
`twisted_insurrection` 75, `cnc_reloaded` 96, `dta_enhanced` 57, `dta_classic` 42,
`openra_ra` 33, `crystallized_nexus` 28, `openra_ts` 22, `openra_td` 20, `openra_d2k` 15.

## Open questions the maintainer has not ruled on

* **The 50 → 32 consolidation.** Ruled to wait until every source has voted. Do not cut families
  on one source's evidence.
* **Uniqueness.** 14 family pairs sit within 0.20 RMS log distance (`Laser`/`Tesla` 0.084,
  `Flame`/`Plasma` 0.088, `Bullet`/`Arrow` 0.105). The maintainer's rule: averaging sets position,
  separation applies only on collision, along the axis where the sources disagree most — and only
  after all 20 have voted.
* **`AA_GROUND_RATIO = 0.5`** in `family_matrix.py`. Where an entire family is air-only its ground
  rows are DERIVED at half the measured air value (R31, the landed-aircraft rule). Most reference
  mods give their SAM sites real ground rows, so these cells should be replaced by measurement as
  sources are assigned. Anything still carrying `derived_ground` at the end is something no source
  had an opinion about.
* **The naval ladder.** `DepthCharge`, `DoubleDepthCharge`, `SubMissile`, `TorpTube` are parked as
  `(park: naval)`. Their measured profiles are unusable anyway — all four read flat 100 because
  `ValidTargets: Submarine` makes every other armour unreachable.
* **Spread and Falloff** were asked for per source and are not extracted yet.

## Traps that have already cost a session

* **A name is not a measurement.** `BazAP` classified as Tesla because `Ba`**`zAP`** contains
  "zap"; `ChronoBeam` matched "Laser" and has no damage profile at all. Delivery comes from the
  projectile, element from damage types, platform from the firing actors. R21.
* **`*Death` tokens and `InfDeath` codes are DEATH ANIMATIONS**, not elements. CA's `HonestJohn`
  rocket artillery carries `FireDeath` and is not a fire weapon.
* **Group names shift on every re-run.** Record decisions per weapon. This saved the Combined Arms
  review four times.
* **An invariant that holds by construction cannot fail.** "gmean = 100, range [10,200]" is true
  because the normaliser makes it true; it proves nothing. `--check` compares against the positive
  median instead.
* **`children_named`, never `child`.** An actor can carry several `Armor` traits and the engine
  honours all of them. `child("Armor")` once made every plating look dead.
* **Read the exit code of PYTHON, not of `tail`.** `python … | tail -4; echo $?` reports `tail`'s
  status. `audit_doc_claims` was reported clean for a whole session that way while it was failing.

## Verification

```
python tools/reference/warhead_matrix.py --check     # expect: 0 degenerate source(s)
python tools/audit/audit_doc_health.py               # expect: PASS
python -m py_compile tools/reference/*.py
```

⚠ `audit_doc_claims` currently **FAILS on 6 claims** and none of them belong to this lane: they
are the `ra1_soviets` / `ra1_allies` rename whose ledger was never re-extracted
(`multi_main_fired_weapons` 192 vs 856, `ledgers_drifted` 0 vs 7, and four more). That failure
predates this work and is for the rename lane to clear.
