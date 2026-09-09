# Selected structured peer evidence

The index explicitly replaces each selected source's legacy Doc5 slice. Currently only
Combined Arms is enabled. Do not append these records to the old CA table population.

The CA payload was extracted read-only from clean commit
`ab9e477c3db818e91946d4cfdc86e71012966141`, with 48 unchanged source inputs. It contains
one schema-1 metadata record and 377 actor records, including all nested armament evidence.
Hashes identify the captured inputs and detect corruption; they do not establish source
authenticity, engine compatibility or gameplay correctness. All 288 armed rows still
carry incomplete evidence. Factory-ready and maximum-upgrade certification remain absent.

Regenerate with `tools/reference/extract_peer_units.py --mod ca --root <clean-checkout>
--json <new-output.jsonl> --expect-commit <full-commit>`, review the source/population and
evidence changes, then explicitly update the payload and index SHA-256 together. Do not
edit a generated payload manually. Git preserves its exact bytes. Invalid selected
evidence fails closed; deleting the index intentionally returns to legacy readers and
must therefore be reviewed as a semantic change, not a recovery workaround.
