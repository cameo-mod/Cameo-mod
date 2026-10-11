# M13 restraint-budget tooling checkpoint

This implements only the switch-catalogue budget from the approved fleet design
`DESIGN_2026-10-10_m13_liveness.md` (SHA256
`4e9ab817d6d4bd874aaeae03f0486603a78553621094a5848a4790fd52720dcc`).
It changes no gameplay defaults, squad decisions, engine pin, or runtime logger.
The escort/liveness helpers remain unwired; the M13 implementation is incomplete.

`increment_switches.yaml` has exactly 59 effect classes: 26 restraints, 24
capabilities and 9 neutral groups. The applier validates complete, unique metadata
before editing YAML. A combined group is a restraint whenever any part imposes a
hold. Generated build-order experiment arms conservatively count as restraints.
An ordinary increment may introduce at most one restraint. Dependencies still
have to be explicitly selected; there is no implicit dependency activation.
Thus Q+O from an empty baseline refuses, whereas AA+D is capability-only.

Without a manifest the baseline is empty. To retain an already armed baseline,
pass `--increment-manifest PATH --manifest-sha256 REVIEWED_SHA256`. Version 1 JSON
requires these fields:

```json
{
  "version": 1,
  "approval_receipt": "external-lead-review-receipt.md",
  "purpose": "ordinary_increment",
  "baseline_classes": {},
  "baseline_patch_sha256": {},
  "selected_groups": ["D_zone_topology"],
  "reclassification_note": ""
}
```

Baseline classes must name existing groups, and their patch pins must have exactly
the same keys. `group_patch_sha256(groups[name])` hashes canonical sorted compact
JSON for the trait/field/value patch. Every baseline patch must already be armed
in the target YAML, excluding skipped classic traits. Missing traits, changed
baseline patch pins, or unarmed fields refuse. For an altered hold duration or
threshold, remove the group from the baseline so its current policy counts as new.
Class changes need an explicit reviewed note and capability-to-restraint counts
as new. Arbitrary source-policy changes cannot be detected from YAML patch hashes;
review must identify those changes and likewise remove the group from baseline.
Independent new holds must not be hidden under one group label.

`--groups all` ordinarily refuses. A separately lead-reviewed, digest-pinned
manifest may allow a combination test only with `purpose: combination_test` and
JSON `campaign_allowed: false`. A SHA pin binds externally approved input; this
tool neither authenticates a reviewer nor grants launch or campaign authorization.
Ordinary acceptance prints `campaign_allowed: null`, not permission to run.
The JSON receipt on stdout records baseline/added/selected groups, patch digests,
reclassifications, new restraint count and manifest digest; archive it with the
increment evidence. No receipt overwrites an existing evidence file.

Legacy custom specs without effect metadata now refuse. The build-order proposal
generator emits the required metadata. Existing runner defaults that request all
groups will fail at the applier; this checkpoint does not add runner exception
forwarding or authorize a batch. Coordinator integration remains separate.

Verification: 17 focused unittest cases cover metadata, baseline patch/state,
reclassification, dependencies, refusal before mutation, dry run, manifest digest,
duplicate keys and bounded reads. Existing A/B helper and build-order tests are
run separately. These tests launch no game and establish no runtime liveness.
