# Selected structured peer evidence

The index explicitly replaces each selected source's legacy Doc5 slice. Currently only
Combined Arms is enabled. Do not append these records to the old CA table population.

The CA payload was extracted read-only from clean commit
`ab9e477c3db818e91946d4cfdc86e71012966141`, with 48 unchanged source inputs. It contains
one schema-1 metadata record and 377 actor records, including all nested armament evidence.
Hashes identify the captured inputs and detect corruption; they do not establish source
authenticity, engine compatibility or gameplay correctness. All 288 armed rows still
carry incomplete evidence. Factory-ready and maximum-upgrade certification remain absent.

Selected production evidence now retains ordered raw Buildable, initial-level and experience
traits, recognized condition-grant declarations and queue/upgrade routes. 57 CA records declare
routes. HMMV records two routes to HMMV.TOW; that resolved variant cancels both.
Target existence does not prove activation or reachability. Routes must not be summed
as simultaneous actors or weapons. This is an input inventory, not an upgrade simulator.
Custom or differently named state providers require separate source-specific review.

Regenerate with `tools/reference/extract_peer_units.py --mod ca --root <clean-checkout>
--json <new-output.jsonl> --expect-commit <full-commit>`, review the source/population and
evidence changes, then explicitly update the payload and index SHA-256 together. Do not
edit a generated payload manually. Git preserves its exact bytes. Invalid selected
evidence fails closed; deleting the index intentionally returns to legacy readers and
must therefore be reviewed as a semantic change, not a recovery workaround.
