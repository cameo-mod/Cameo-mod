# Default-off turtle fallback expiry

Maintainer-approved increment4, after the turtle regression audit. Switch
`TF4_turtle_fallback_expiry` enables `MasterAiBotModule.UseTurtleFallbackExpiry`;
the Info default is false and no baseline YAML is armed. This is a restraint group,
not campaign adoption. Candidate ordering and the explicit legacy emergency override
remain unchanged.

The production selector now records why a candidate exists. Only the terminal
pressure fallback to turtle can retain an available rush, steamroller or guerrilla
incumbent, and only with a positive own combat army and an alive/reached own target
profile. Explicit steamroller, guerrilla, tech, rush, expansion and legacy emergency
candidates keep their precedence. Bandit pins and ordinary synced personality-order
handling still apply.

The hold lasts twice the enabled tier's existing personality reaction delay (positive
durations only, long addition and int saturation). The first eligible fallback fixes
expiry; later pressure, incumbent changes and delay changes cannot extend it. At
expiry the selector returns turtle to the ordinary sustained-candidate switching
rule. No forced rush, team counter, RNG call or shared personality state is added.
Each MasterAi instance owns its policy. A genuinely different selection reason or
loss of incumbent eligibility ends that hold episode.

Visible enemy combat actors within the existing PressureRadius of an own live
construction-yard base bypass retention. Visibility is checked before enemy location
access. A missing base also refuses retention. Remote army losses, remembered pressure
and ally requests do not establish local base danger. Bypassing retention changes the
candidate immediately; the existing reaction/hold and explicit emergency transition
rules still govern when a personality order is issued.

Only changed decisions emit `TURTLE_FALLBACK`, with exact seat, tick, reason, original
candidate, selected personality and absolute expiry. Off has no policy call or new
diagnostic. Existing personality save nodes are unchanged: armed restoration invalidates
the policy, which conservatively returns baseline candidates for the rest of that load
rather than manufacturing a new hold. A proper hold-provenance restore adapter remains
a separate gate.

Validation: focused policy/production selector/MasterAi tests139 passed; full Release
C#1694 passed (two existing explicit max-census fixtures not selected); focused Python
registry17 passed. The base registry had an existing BP_squad_desire group without an
effect class, making the fail-closed applier unusable. This delta also classifies that
existing stance hold as restraint and updates the catalogue count, without arming it.
No engine/pin change, boot or game ran. Loaded-world adapter, off replay parity, matched
turtle outcomes, runtime cost and adoption remain open. Own exact-SHA review required.
