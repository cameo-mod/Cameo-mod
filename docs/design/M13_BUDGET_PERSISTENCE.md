# M13 response-budget persistence checkpoint

This is an unwired helper contract, based on the reviewed response-claim checkpoint 864966c59. It does not mount the player coordinator, admit or reclaim units, serialize MiniYaml, or change the default-off switch. Production manager integration must use one budget instance per player and separately restore actual membership and stable manager ownership.

## Contract

Capture exports version 1, caller-supplied stable player identity, last observed tick, the exact active claim/manager/start/expiry and ordinal-sorted cooldown entries. It exports copies: changes to the transport cannot change the source or an already-restored budget. Player identities and manager IDs are at most 128 UTF-16 code units; claim keys at most 512; history remains at most 128 entries, with one slot reserved for releasing an active claim.

Restore is allowed once on a fresh instance, before any tick/admission operations. It verifies player identity, version, clock and duration bounds, active/idle consistency, history bounds and unique keys before copying state. Expired cooldowns prune at restore time; unexpired cooldowns and the original active expiry survive. An expired active claim still occupies the slot until the owning manager explicitly reclaims actual membership and releases it. Restore never extends a lease or resets cooldown relative to load time.

Missing, malformed, foreign or unsupported state disables new admission on that fresh instance; a second empty restore cannot rearm it. Attempts to restore an already-used instance are rejected without mutating it. Capture of unsupported state fails explicitly, so a future save adapter must propagate unsupported coverage rather than write an empty budget as complete evidence. This is not the engine save adapter.

## Validation and remaining work

Focused regressions exercise original lease ownership across managers, exact expiry/cooldown boundaries, a full history, canonical copies, malformed saves, one-shot restore, clock rollback and long deadline arithmetic. No full directory Python test is needed.

Remaining before M13 implementation completion: player-shared coordinator lifecycle, bounded actual drafted-member ownership, reserve/admission and factual uncontested evidence, reclaim without dissolving unrelated defenders, game save/load adapter, armed/off manager integration, semantic combat stream and paired 3v3/runtime gates. Helper round trips prove none of these gameplay gates.
