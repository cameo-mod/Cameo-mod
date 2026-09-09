# OpenRA peer weapon evidence — repair plan and source review

The findings below began as a read-only plan. Bounded P0 implementation now retains
resolved armament evidence and withholds unsupported numeric summaries; ordinary and
hero Doc5 consumers now honor emitted evidence columns. Legacy corpus rows remain
unassessed and unchanged. Full factory-ready/max-upgrade state evaluation and structured
corpus migration remain pending. Source checkouts were read only; no source game ran.

## Current CA source validation

At `ab9e477c3db818e91946d4cfdc86e71012966141`, active manifest extraction returns
377 rows: 288 armed and 89 unarmed. The former Doc5 headline of 382 differs by exactly
AFAC, FACT, SFAC, TRUK and TRUK.DROP: all still exist but resolve `~disabled` prerequisites
and are excluded by the current availability filter. No actor was deleted by this repair.

All 288 armed rows initially remain incomplete under the conservative evidence gate.
This is not 288 broken weapons or zero damage. Overlapping reasons include 190 multi-slot
actors, 147 conditional armaments, 42 bound ammo pools, 36 unknown bindings and 15 unresolved
burst cadences. A review also identified false warning reasons from incoming DamageMultiplier
and ordinary AttackMove; those are being corrected separately from genuine state ambiguity.

HMMV.TOW now retains M60mgTD plus TOW and its secondary-bound Ammo=1 / reload Delay=200
evidence. XO retains XOCoilGun and XOLaser. E1 retains its AI commandeer helper, M1Carbine
and M1CarbineBATF. The old E1 summary's range 1024 / reload 5 describes the first AI helper,
not its combat rifle. HP/cost/speed spot checks for HMMV, HMMV.TOW, XO and E1 match Doc5.
No declaration-order replacement is automatically certified as the factory-ready weapon.

## 1. Verified findings

**F1 — `weapon_stats` keeps only the first Armament.** `tools/reference/extract_peer_units.py:97`
collects every `Armament@*` weapon but resolves and reports `weapons[0]` only — declaration
order, with no priority, condition, ammo or upgrade awareness. Every multi-armament Doc5 row
carries one weapon's numbers presented as the unit's damage summary.

**F2 — the concrete casualty is CA `HMMV.TOW`.** External [CA checkout](https://github.com/Inq8/CAmod)
(HEAD `ab9e477c3db818e91946d4cfdc86e71012966141`, 2026-07-30, clean tree; matches
`UPSTREAM_MODS.md` "ca-engine/1.09", `Inq8/OpenRA`), `mods/ca/rules/vehicles.yaml:3224-3345`:

* `HMMV` builds with `Prerequisites: ~vehicles.hmmv, ~!tow.upgrade` — **factory-ready base**,
  one unconditional `Armament:` (M60mgTD), cost 400, speed 144; `ReplacedInQueue: hmmv.tow`.
* `HMMV.TOW` (`Inherits: HMMV`) requires `~tow.upgrade`, cost 575, speed 126, adds
  `Armament@SECONDARY` (Weapon: TOW, `PauseOnCondition: !ammo`), `AmmoPool` (Armaments:
  secondary, Ammo: 1, `AmmoCondition: ammo`), `ReloadAmmoPoolCA` (Delay: 200), and cancels
  `-ReplacedInQueue` / `-Upgradeable@TOW` — an **upgrade-gated queue-replacement variant**,
  mutually exclusive with the base state, not an additive upgrade.
* Doc5 (`docs/design/ORIGINAL_UNITS_PEER_OPENRA.md:303-304`) shows BOTH rows with identical
  M60 numbers — F1 hiding the TOW slot and the ammo-gated cadence entirely.
  `BUGGY_HUMVEE_REFERENCE_REVIEW_20260909.md` recommendation 5 already flagged this.

**F3 — Doc5's MD tables are a lossy transport.** `reference_distribution.py` parses
the markdown into ordinary and hero rows. Other paths consume, maintain or fingerprint
the document; `propose_anchor_spec.py` and `build_japan_pilot.py` fingerprint it rather
than independently parsing all its weapon columns. History: cost and faction were each
extracted and then silently dropped on read; `vsINF` case mismatch zeroed armor columns;
`peer_rows` drops any row whose cell count differs from the header (line 609). Any metadata
that does not fit today's columns vanishes exactly this way.

**F4 — Doc5 is hash-pinned.** Anchor dossiers (`docs/balance/anchors/*.md`) pin its SHA-256
(`d37db59f…`) and `build_japan_pilot.py` fingerprints it; `BLACKROBE_ASTRA_ORDERS_2026-09-07.md`
line 1563 forbids full-file regeneration — CA-section changes go through
`splice_peer_section.py`. Changed inputs invalidate earlier evidence: regenerate and
review affected diagnostics, rather than mechanically updating a signed evidence hash.

**F5 — explicit source routing is needed.** `PEERS["ca"]["root"]` candidates
(`extract_peer_units.py:219-220`) do not include this independently acquired checkout, and the
stored corpus records no checkout revision at all.

## 2. The smallest fail-closed repair (prioritized)

**P0 — evidence-honest weapon extraction.** Extend `weapon_stats` (or emit alongside it) so
that per actor it emits ONE row PER RESOLVED ARMAMENT SLOT, using the INI extractor's
vocabulary (`extract_ini_units.py` `weapon_of`/`relabel_weapon`): `w_evidence`,
`w_evidence_reason`, `w_dps_usable`, plus slot twins `w2_*` / `wdummy_*`. The base numeric
summary (`w_dps`) is emitted ONLY when proven: exactly one resolved Armament, no
`PauseOnCondition`/`RequiresCondition` on it, no AmmoPool binding, weapon resolves through
`miniyaml.Ruleset.resolve_weapon`, all main warheads carry positive conventional `Damage`.
That state is labelled `nominal_direct` (the declared Damage/ROF contract, never "complete").
Everything else is `incomplete` with a reason; its raw direct estimate moves to `w_dps_raw`
and votes nowhere. Existing Doc5 numbers are never re-labelled as certified: absent evidence
stays `legacy-unassessed` (consumer `apply_weapon_evidence`, `reference_distribution.py:428`).

**P1 — retain cadence and gating evidence RAW, never folded silently.** Per slot, carry:
slot name, weapon id, `PauseOnCondition`, `RequiresCondition`, AmmoPool binding
(`Armaments`, `Ammo`, `AmmoCondition`), `ReloadAmmoPoolCA` delay, raw `ReloadDelay` /
`Burst` / `BurstDelays` / `Range` / `MinRange`, per-warhead `Damage`, and target-side
evidence (AutoTarget stance, warhead `Versus` presence). Ammo-pooled cadence (TOW:
ammo 1 + pool reload 200) must never be expressed as a plain ReloadDelay cycle.

**P2 — base and maximum-upgrade as SEPARATE views, never summed.**
* Base view = factory-ready, established from source-specific activation evidence.
  A token spelling such as `~<x>.upgrade` is not a general proof of upgrade state across
  mods. Preserve prerequisites and modifiers raw; unknown activation must remain unknown.
  The verified HMMV/TOW pair illustrates a source-specific case, not a universal parser rule.
  Distinguish the prerequisites needed to purchase a build variant from optional upgrades
  applied after it is produced: the TOW missile is intrinsic to HMMV.TOW's own definition.
  Its upgrade-gated availability does not, by itself, prove that it must be excluded as a
  counterpart to a separately buildable Cameo Mk II unit. That is a role/variant decision.
* Max-upgrade view = per base actor, LIST its mutually exclusive achieved states (the
  queue-replacement pair: `ReplacedInQueue` on the base, cancelled on the variant), each
  state its own row with its own evidence and gating provenance. No cross-state addition,
  no blended "upgraded DPS" — the views are separate columns of evidence, not a simulator.

**P3 — transport that cannot drop metadata.** Follow the INI family's precedent
(`docs/reference/ini_corpus.json`): the extractor writes a JSONL peer weapon corpus under
`docs/reference/` as the machine source of truth, and Doc5 becomes display-only output
regenerated per-section via `splice_peer_section.py`. Consumers migrate to the JSONL reader
(`peer_rows` shape already matches); until each migrates, keep the MD columns the consumer
reads strictly in sync and treat any header/column mismatch as an error, not a skip.
This is the reader-path fix for F3; no MD column gymnastics.

**P4 — provenance routing.** Add an explicit, recorded source identity for peer extraction,
mirroring `extract_ra3_units.py --expect-commit`: explicit `--root` (or a candidate entry)
for the CA checkout plus HEAD `ab9e477…`, dirty state and input hashes recorded in the
corpus. Keep `ENGINE_VERSION="ca-engine/1.09"` (mod.config) as part of the identity. Raw
sources stay outside the repo (R9); the checkout is never executed. Any Doc5 CA-section
rewrite invalidates earlier source-pinned diagnostics and needs explicit evidence review
(F4). A `TASK_INDEX.md` row for this task is added when implementation lands.

## 3. Non-goals

No general upgrade simulator or new armor taxonomy in this bounded extractor repair;
no rebalance, assignment changes or retroactive certification of the committed corpus.
Heaviness C# work is a separate worktree/task, not part of this extractor change.

## 4. Open items for the implementing session

* Whether HMMV.TOW belongs in the base population at all once labelled (it is a distinct
  Buildable actor; routing rules R13/R15 and the population rule decide, not this note).
* `weapon_stats`' damage fold sums all warheads of the picked weapon — keep per-warhead
  rows in the corpus so that fold stays inspectable.
* Label `Combined Arms` (no parentheses — `main()` guard) must not change, or every
  de-dup/route that names the source breaks.
