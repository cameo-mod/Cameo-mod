# Dune identical weapon-definition cleanup

Removed only the redundant `OrniBombC` and `OrniGunC` blocks from the active Ordos
weapon file. Their identical definitions remain in Atreides beside their parents.
The manifest loads Ordos before Atreides. No weapon ID, actor reference, projectile,
damage, firing timing, target eligibility, effect or asset was removed or changed.

All 2,897 resolved weapons compare exactly with the pre-edit snapshot from
`0dba5542ff8068179066321ab1aa633a1a4cf82b`. Three new tests cover sole active
definition location, full resolved payload hashes, and explicit range/target/damage
fields. The retained OrniBombC range is 2500; OrniGunC range 6000, minimum 1200, damage 800.

Independent review supports this scoped cleanup. Split-definition S2 drops 4→2;
S1 stays 22. The existing thresholds 56/2 are not raised. Flamethrower and ZClaw3 are
not identical duplicates and remain visible, untouched. This clears the S2 overrun,
not all duplicate-definition debt or W24 migration.

Final combined validation with PR339/340: 1,895 tests, 13 failures, 8 errors and
64 skips. Exact failure/error signatures match the preceding integration run; no
new signatures. Peak sampled system memory 80.97%, no guard stop. All 2,900 combined
resolved weapons also remain identical across this cleanup. This is combined-suite
evidence, not a claim that the separate PR341 branch has a green full suite.

Canonical PR341 audits completed with no empty reports and eight remaining failing
categories: inherits, upgrades, basebuilder_crates, buildable_order, packs,
nuclear_flash_bindings, doc_claims and doc_health. Split-definitions now passes.

The removed text is recoverable in Git; the runtime weapons themselves still exist.
