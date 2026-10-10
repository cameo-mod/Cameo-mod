# AI log privacy and phase-0 signatures

Structured AI logs use slot-based seat_N references (the World.Players array,
including neutral slots). Record IDs have a GUID or anonymized legacy game
prefix and that same seat. No runtime player InternalName or client index is a
persisted descriptor. Public map metadata and canonical faction/type codes
remain available. Mission targets are translated as structured tokens; capture
actor-type/actor-ID and Frans numeric IDs are separate public grammars.

AiMatchLogWriter writes one record per combat seat, including humans, only after
IGameOver. The record-only observer owns a separate BotFogMemory per seat; its
CanBeViewedByPlayer and FrozenActorLayer paths gate seen facts even when another
bot module opts into an omniscient profile. It checks sighting changes every 25
ticks and records changed feature vectors plus a 750-tick minimum cadence floor.
Raw actor facts are retained only in memory for post-game recomputation of the
true numeric signature and resolved faction. None of these buffers feed bot
planning or orders, and no learning file is loaded by this phase.

The single new world.Actors enumeration in AiMatchLogWriter is deliberately in
fog_honesty_manifest.json for independent review. It groups alive spatial actors
by owner once per observation pass. Seen classification is gated through the
independent fog memory; raw own-seat actor facts are retained for post-game
truth recomputation. The audit ratchet documents the site rather than proving
these semantics. Reviewers should check that no decision-side consumer exists.

fit_opponent_signatures emits deterministic clusters plus recognized-to-actual
relabel counts; the unknown public faction has the canonical `unknown` token.
The privacy gate pins nested field shapes/types in anonymous_log_shapes.json and
anonymous_log_types.json, rejects unknown keys recursively, checks reference
formats, and restricts faction/scope/actor/feature tokens to canonical vocabulary.
It checks all files recursively under ai/learned, rejecting unsupported formats
and unknown bodies. Legacy archives are projected into that same grammar, with
validated outputs; backup originals still contain names and must be treated as
rollback copies. Invalid partially migrated records are repaired instead of
being certified solely by their schema number.

Regression map: AiLogPrivacyRegressionTest covers findings 1/4/5/6 and actual
emitter fixtures; test_learn_p0_privacy_regressions.py covers findings 2/3/5/7/8,
including backup-before-replacement, idempotence, hidden factions, two-seat joins,
and physical homes that differ from anonymous array indices. CAMEO_PRIVACY_TEST_OUTPUT
exports the rebuilt emitter fixtures for the Python grammar/fit pipeline.
