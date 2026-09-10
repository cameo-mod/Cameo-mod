# Four-faction balance project — 11 September 2026 checkpoint

This is a preservation checkpoint, not a merge-ready or playtest-ready release.
The integration branch includes the earlier reference, identity and shared-heaviness
draft work, plus the subsequent Aedis-requested corrections. Existing PRs #339,
#340 and #341 overlap this snapshot; do not merge them and this aggregate blindly.

## Completed locally

- The reference map contains 163 actors, 282 identity links and 195 peer cycle
  profiles. Among 55 ordinary armed actors with at least three assigned references,
  54 have full-source range and nominal weapon-rate coverage. The remaining Spy
  row has accepted unarmed CA/DTA counterparts. Sparse hero normalization remains
  a separate limitation; identity links are not a completion percentage.
- Saved unit-local outgoing firepower factors were converted into explicit weapon
  damage for 30 pilot actors / 100 weapon contexts. Five isolated weapon variants
  preserve other users. Main damage now uses the approved 10-point grid; Monster
  Tank's 126262.5 rounds to 126260. Percentage fractions are retained through
  scalar/denominator metadata where needed. Conditional mechanics remain.
- Targeting and held missile-role corrections are applied. The pilot target fixture
  covers 36 parent/emitter/fragment routes, including isolated damage variants.
- Cargo checking uses actual mixed `InitialUnits` loads and passenger weights.
  Eleven full loads are valued: three prices match and eight differ. Armed cargo
  uses K=1.25 inside its combat formula, not as a purchase-price surcharge.
- Equal-source armor comparison arithmetic and vehicle endpoint interpolation are
  implemented as helpers. The source-backed Flame Tower example combines split
  channels first and demonstrates Cameo's additional max-HP-dependent damage.
  This is groundwork, not a completed all-unit armor model.

## Runtime impact and unresolved work

The YAML changes affect gameplay: shared-default unconditional multipliers remain
removed without compensation, pilot local firepower is baked into weapon damage,
and reviewed target masks/weapon families change eligible hits and armor response.
Shared defaults and some shared weapons affect actors outside the four-faction
test population. Earlier commits also contain Cameo C# shared-heaviness changes.
No engine pin change is part of this preservation commit.

1. **Incoming damage:** Bastion's saved 50% and Soviet/Nod SAM's 75% incoming
   factors remain inactive. Their removal means respectively 2x and 4/3x local
   incoming damage. Restoration versus a reviewed durability conversion needs
   disposition; HP-only conversion changes healing/shield/percentage interactions.
2. **Calibration:** the current unsigned/provisional-anchor diagnostic reports
   37 band flags across 23 classes. These are not 37 proven gameplay bugs and
   must not be fixed by blindly changing prices to match an incomplete model.
3. **Cargo:** eight price differences need review. GDI Landing Craft and Nod
   Transport Submarine have no initial load; Nod Chinook's eight passengers weigh
   11 against capacity 8. Tier suitability is separate from full-capacity checks.
4. **Armor synthesis:** finish applicable per-source channels, max-HP terms,
   target states, cadence and armor mapping before four equal source votes.
   DTA preprocessing and reference secondary-payload coverage remain incomplete.
   The arithmetic/geometric blend and shared-family proposals remain open.
5. **Shrapnel:** integrate reviewed isolated/group benchmarks; do not assume a
   universal random-hit probability. V2 Tesla SCUD's parent Air role remains open.
6. **Projectile speed:** new requirement to compare common-distance travel times,
   accounting for source units/ticks, acceleration, guidance and instant-hit
   behavior. This is not yet implemented in the reference map.
7. **Playtest:** runtime validation and actual gameplay review remain outstanding.
   A narrow four-faction bug-finding session is plausible for Sunday; complete
   roster recalibration and global Versus synthesis are not promised by then.
   Japan, upgrade pricing and selective faction loading remain later work.

## Validation and preservation

The last implementation batch passed 30 focused checks: 13 target-policy,
6 firepower conversion, 4 cargo, 4 band-membership and 3 armor-projection checks.
The formula grid self-test passed. Broad historical suites were not repeated;
these results do not establish a green full suite. The current band command
intentionally fails on the open diagnostics above. No game launch or new engine
build was performed for this publication.

All raw/derived ledgers were staged together after the damage conversion; only
changed outputs were copied back. Frozen ordinary and hero Cameo self-vote
baselines remain preserved. The [checkpoint artifacts](checkpoints/20260911/manifest.json)
include the latest sent HTML, migration receipts, cargo and band diagnostics,
armor examples and the source-channel atlas, with content hashes. Downloaded
reference repositories and local runtimes are not embedded in this checkpoint.

The 15-minute Discord automation was paused at the authorized 05:00 WIB cutoff.
Implementation status above is the cutoff state, not a claim about later DMs.
Continue from the open critical path, group meaningful validation, and keep
publication preservation separate from merge approval.
