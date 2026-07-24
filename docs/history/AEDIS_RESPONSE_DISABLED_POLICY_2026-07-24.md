# Aedis Response Addendum — `~disabled` Policy

Date: 2026-07-24

Status: Accepted clarification; standalone revision to the initial audit

Scope: This document records Aedis's response concerning the use of
`~disabled`. It does not modify the first-wave audit documents already given
to Aedis's Claude session.

## Response from Aedis

Aedis clarified that `~disabled` is intentionally the common prerequisite
marker for actors that should not currently be available:

- the linter is expected not to warn about these actors;
- all disabled actors should be discoverable with one simple search;
- the marker can include unfinished actors that may later be implemented;
- a future review can either remove an actor or restore and complete it.

Under this policy, `~disabled` does not mean only "permanently abandoned."
It means that the actor is deliberately excluded from normal availability at
the present time.

## Revision to the initial audit

The initial audit described blanket conversion to `~disabled` as dangerous
because older tokens such as `~wip-content`, `~wip`, and `~unbuildable`
encoded more specific reasons.

That conclusion was too strong. Aedis has confirmed that consolidating those
states is intentional project policy. The linter exemption and the ability to
find all unavailable actors through a single search are desired properties,
not evidence that the linter is dictating game design.

Accordingly:

1. Do not restore the old prerequisite names merely to preserve their former
   categories.
2. Do not treat an actor as incorrectly converted solely because an older WIP
   or unbuildable token became `~disabled`.
3. Do not require a separate WIP reason or status field unless the project
   later decides that such classification is useful.
4. Retain `~disabled` as the canonical prerequisite marker for actors
   deliberately excluded from current availability.

## Remaining valid review question

The clarification does not prove that every individual occurrence is
intentional. An actor may still have been disabled accidentally—for example,
if `~disabled` was added to silence a warning that actually revealed a broken
prerequisite provider or technology chain.

Any further review must therefore ask:

> Is this actor deliberately unavailable under the project's unified
> `~disabled` policy, or was it disabled accidentally instead of repairing an
> intended prerequisite chain?

That determination requires actor-specific design evidence. The mere presence
of `~disabled`, or its replacement of an older marker, is not itself a defect.

## Effect on the prerequisite triage

The actors listed as "needs individual review" in
`PREREQUISITE_TRIAGE_2026-07-24.md` should not be reviewed for restoration of
the old category names. They should be reviewed only where there is concrete
reason to suspect accidental disabling or a concealed prerequisite defect.

No YAML or code change follows directly from Aedis's clarification.
