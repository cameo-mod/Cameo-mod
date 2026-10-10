# M13 response claim transport checkpoint

`SquadManagerBotModuleCA.PrepositionDefenceTick` previously synthesized allied
defence/assist protection requests without the owner-admitted claim episode.
The provider checkpoint already preserves these identities in broadcasts and
coalition assignments. This follow-up carries them through all three existing
request synthesis sites using `M13ResponseClaimCA.Synthesize`.

The factory preserves the exact existing rally, value and expiry payload.
It carries a known episode only when it belongs to the broadcast participant;
the elected paths additionally require the election and broadcast to name the
same admitted episode. A stale election, missing publisher or unsupported
expansion-assist identity produces an UNKNOWN/default episode. It does not
withdraw the legacy request or change drafting, holds, orders or precedence.

`TryKey` produces a bounded-kind key from the provider's owner identity and
admission sequence. Rally movement, expiry refresh and observation pulses cannot
renew the key. Different participants, provider instances, sequences and response
channels cannot share the key. Unsupported identity and non-response channels
are refused. The key helper does not itself admit a response or allocate units.

Tests exercise the production factory/key contract: exact legacy payload,
published/elected matching, old and foreign elections, unsupported publisher,
100 pulse/rally/expiry refreshes, readmission, owner/channel separation and
unsupported assist/non-response channels. These are pure contract tests, not
loaded-world response or switch-off parity evidence.

Remaining M13 response implementation must wire the player-wide budget across
personality managers, calculate spare eligible assault members above the launch
bar, enforce the nonrenewing tier lease/cooldown, reclaim actual membership on
withdrawal/staleness/reserve loss/emergency/expiry, and serialize the resulting
state. UNKNOWN episode admission must fail closed only behind the default-off
M13 switch; legacy behavior while off remains unchanged.

This record-only checkpoint neither wires that budget nor certifies M13's
first-wave, deadline, semantic capture, runtime-cost or paired 3v3 requirements.
No engine/pin/BaseBuilder/YAML/default changes or game launches.
