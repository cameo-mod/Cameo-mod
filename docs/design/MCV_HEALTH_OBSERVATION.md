# Separate MCV health observation checkpoint

Opt-in world trait McvHealthCapture is unmounted: no YAML, decision, order, engine
or BaseBuilder edits. Lead ownership ACK2026-10-10 permits a separate read-only
world provider. Canonical path Logs/cameo-ai-mcv-health.jsonl, schema
cameo-mcv-health version1; proposed consumer now tools/ai/mcv_health.py.
Consumer source SHA25639a29d5a376359d6b7c8e4b88f4a957d68b6785240faaa612417b496c619a82f
at this checkpoint before documentation/commit; adoption requires reviewed exact
commit/blob and executed-byte verification, independently of economy checkers.

The provider reads only participating own actors after owner selection. Loaded
McvExpansionManagerBotModuleInfo.McvTypes plus exactly one TransformsInfo and
loaded target BuildingInfo/RefineryInfo/configured ConstructionYardTypes resolve
roles. Construction mapping inconsistent with the target is unknown. No actor-name
substring classification. Up to128 configured types,64 actors/player and64 players.
Overflow, unresolved mapping, late/gapped observation, dropped output or absent
file is incomplete/UNKNOWN. It neither asks deploy search nor changes engine pins.

Each world tick tracks live/in-world state, cell, idle flag and current top-level
activity. Top-level Move is mobile, Transform deploying, null idle, other activity
busy; nested activity is conservatively busy. Cell change is independent progress,
not proof of a Move order. Idle age resets on cell progress/activity. Removed or
owner-transferred actors are pruned; disappearance never establishes deployment.
Bounded role/type maps resolve once. LeaseOf is a read-only query on the existing
registry: lease ownership is observed, never inferred to be refusal or policy hold.

Start at tick0, transition-only rows and50tick pulses use per-player contiguous
seq and covered end event. Row actors are deterministically actor-ID ordered.
Shared bounded CreateNew writer:128MiB/file,64KiB/line,200k records; existing
captures are never appended or overwritten. Runner must archive the separate MCV
file before later cells. No shared economy completion watermark is implied.

Order request/acceptance, site intent, hold reasons and transform outcome lack an
approved factual hook in this scope. They remain null/unknown, and explicit
intent/order/hold/transform completeness flags remain false. Therefore ACTUAL
EMITTER CAPTURES RETURN UNKNOWN: an idle finding is a diagnostic observation,
never causal blame or a campaign stop. No schema channel claims absent coverage.
Supported zero-actor census remains explicit; it does not imply a yard was founded.

Consumer CLI: python tools/ai/mcv_health.py <support> --game-uid <uid> --player <key>
Optional --idle-ticks is an explicitly declared diagnostic experiment threshold;
there is NO default copied from building-queue thresholds. Diagnostic exits0,
UNKNOWN exits21; never BLOCK20. PERSISTENT_IDLE_OBSERVED retains actor/interval,
known policy hold and causal_conclusion=null, including UNKNOWN captures. Single
idle/busy/dead/inactive snapshots are not a persistent-idle defect. The consumer
rejects unsupported schemas/codes, late census, gaps, invalid sequence, duplicate
actor identity, bad bounds and missing/incomplete terminal evidence. It reads
bounded bytes, makes no output-file mutation and preserves input SHA/path evidence.

Validation: focused C#9/9, Python8/8 including actual C# canonical-path CLI21 and
immutability, full Release1412/1412 PASS; diff-check clean. First compile attempt
required qualifying OpenRA.Player because a local Player namespace shadows it;
corrected before passing checks. All code remains inert/unmounted.

Open gates: independent exact-SHA review, accepted future order/hold/transform
hooks, complete loaded-runtime coverage, per-tick world enumeration/allocation/
disk cost and mounted/unmounted order/sync parity. No game, boot, reservation or
campaign adoption is authorized or performed by this checkpoint.