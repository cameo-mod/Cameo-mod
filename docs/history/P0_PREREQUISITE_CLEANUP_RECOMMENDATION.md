# P0 Recommendation: Prevent Behavior-Changing YAML Cleanup

> **OVERRULED by [AEDIS_RESPONSE_DISABLED_POLICY_2026-07-24.md](AEDIS_RESPONSE_DISABLED_POLICY_2026-07-24.md).**
> Aedis confirmed that consolidating WIP/unbuildable/disable markers to `~disabled`
> is intentional project policy. The concerns below are preserved as historical
> context, but the policy decision supersedes the recommendation against blanket
> replacement. The remaining valid question (accidental vs. intentional disabling)
> still applies on a per-actor basis.
>
> **Implementation:** See `docs/design/MEGAPLAN_YAML_CLEANUP.md` Phase 5 for the
> completed `~disabled` consolidation. The binding rule is codified in `DESIGN.md`
> §9 and enforced by `tools/audit/audit_yaml_lint_rules.py`.

## Summary

The goal of reducing `OpenRA.Utility --check-yaml` warnings is useful, but
warning-count reduction must not be treated as proof that a change is correct.

In particular, prerequisite markers such as:

```text
~wip-content
~wip
~unbuildable
~disable
```

must not all be replaced automatically with:

```text
~disabled
```

The linter exempts prerequisites beginning with `~disabled`. This explains why
the warning disappears, but it does not prove that all the original markers
have the same gameplay meaning.

## Why blanket replacement is dangerous

The original markers may represent different states:

- `~wip-content`: temporarily unavailable because the content is unfinished;
- `~unbuildable`: not available through normal production, but potentially
  used by maps, scripts, transformations, spawning, veterancy, or other systems;
- a missing real prerequisite: possibly a typo, renamed actor, or missing
  prerequisite provider;
- `~disabled`: deliberately and indefinitely unavailable.

Converting all of these to `~disabled` can:

- hide a broken production or technology chain;
- make temporarily unfinished content appear permanently abandoned;
- erase why an actor was unavailable;
- conceal a typo instead of repairing it;
- alter actor availability while appearing to be harmless lint cleanup;
- make future restoration harder because the original intent has been lost.

The number of warnings may decrease even when the project becomes less
understandable or the gameplay configuration becomes less correct.

## Required classification before changing a prerequisite

Every unresolved-prerequisite warning should be classified into one of the
following categories.

### 1. Real defect

The prerequisite is misspelled, was renamed, or should be provided by an
existing actor.

Action:

- repair the prerequisite reference or its provider;
- do not replace it with `~disabled`.

Example:

```text
~construction_yard.atreides
```

If this is supposed to be provided by an Atreides construction yard, replacing
it with `~disabled` would hide a broken Atreides technology tree.

### 2. Permanently disabled content

The actor is deliberately unavailable and is not planned to return to normal
play.

Action:

- use the engine-recognized `~disabled` gate;
- record why the actor is permanently disabled.

This is the category for which a direct `~disabled` conversion is clearly
appropriate.

### 3. Work-in-progress content

The actor or faction is temporarily unavailable while its rules, artwork,
production chain, AI, balance, or other dependencies are unfinished.

Action:

- keep it unavailable for now;
- preserve a machine-readable WIP status and reason;
- do not silently turn “unfinished” into “permanently disabled.”

### 4. Spawn-only or script-only actor

The actor is intentionally absent from normal production but is created by a
map, script, support power, transformation, death behavior, veterancy system,
or another actor.

Action:

- verify the mechanic that creates or references it;
- do not treat its lack of a normal production prerequisite as a normal unit
  defect;
- preserve its non-production purpose.

### 5. Inactive legacy content

The actor belongs to a dormant file, retired faction, or content that is not in
the active `mods/cameo/mod.yaml` include graph.

Action:

- confirm that the file is inactive;
- exclude inactive content from live-status conclusions or record it as legacy;
- do not rewrite dormant files merely to improve the active build’s warning
  count.

### 6. Tool false positive or unsupported pattern

The prerequisite is provided indirectly or conditionally, but the linter
cannot understand that mechanism.

Action:

- improve the detector or add a narrow, documented exception;
- do not change valid gameplay data to accommodate an incomplete detector.

## Preserve the reason when `~disabled` is technically required

If `~disabled` is the only prerequisite token that the engine and linter safely
recognize, it may still be used. However, it must not be the only stored
information.

The original intent could be preserved through:

- a nearby YAML comment;
- a dedicated content-status manifest;
- an audit exception/status file;
- faction metadata;
- a generated actor-status ledger;
- a roadmap item for temporary WIP gates.

Illustrative YAML:

```yaml
Buildable:
    Prerequisites: ~disabled
    # DisabledReason: wip-faction
    # Restore after the Atreides construction yard, production queues,
    # AI, assets, and prerequisites pass readiness review.
```

A structured form would be easier to audit:

```yaml
atreides_factory:
    Status: wip
    EngineGate: disabled
    Reason: production-chain-incomplete
```

The maintainers should decide the final storage format. The important
requirement is that `~disabled` must not erase whether an actor is WIP,
spawn-only, legacy, or permanently removed.

## Required review record

For every changed unresolved prerequisite, record:

- actor ID;
- source file;
- whether that file is actively loaded;
- original prerequisite;
- classification;
- evidence for that classification;
- chosen action;
- expected availability after the change;
- validation performed.

This makes the cleanup reviewable and reversible.

## Validate behavior, not warning-count reduction

After a prerequisite change, answer:

- Is the actor supposed to be buildable?
- Which faction and building should unlock it?
- At what technology tier should it appear?
- Which production queue should contain it?
- If it is not buildable, what live mechanic creates or references it?
- Do AI rules, starting units, maps, scripts, support powers,
  transformations, or Random-faction membership depend on it?
- Did its resolved availability change?

For a behavior-preserving cleanup, compare the resolved actor state before and
after the change.

If actor availability changes, the edit is a gameplay change requiring an
explicit design decision. It must not be presented as lint cleanup.

## Applying this to the Eden warning

Example warning:

```text
Buildable actor eden_factory_structure has prereq ~wip-content not provided by anything.
```

This warning alone does not justify changing the prerequisite to `~disabled`.

The correct investigation is:

1. Confirm whether the Outpost 2 content containing
   `eden_factory_structure` is actively loaded.
2. Determine whether the actor is intended to be WIP, permanently disabled,
   spawn-only, or normally buildable.
3. Find every actor, map, script, transformation, AI rule, production queue,
   and support power that references it.
4. If it should be buildable, identify and restore its real prerequisite
   provider.
5. If it should remain unavailable, use the correct engine gate while
   preserving the reason.
6. Verify that its resulting availability and dependent mechanics match the
   design decision.

Only if the intended decision is “this actor is permanently unavailable” is a
bare `~disabled` conversion clearly correct.

## Proposed project policy

> Linter output is a diagnostic input, not a design authority. No prerequisite
> may be replaced with `~disabled` solely to reduce the warning count. Every
> unresolved prerequisite must first be classified as a real defect,
> permanently disabled content, WIP content, spawn/script-only content,
> inactive legacy content, or a tool limitation. Any change that alters actor
> availability is a gameplay change and requires an explicit design decision
> plus behavior-level validation.

