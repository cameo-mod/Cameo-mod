# STATUS: Campaign acceptance integration

## Resume
- Branch: `codex/ab-campaign-policy-overlay-followup`; starting HEAD `de9c6e9b9f2a17384d25ed4f0ef5ac323bf34be0`.
- Selectively integrated the isolated map acceptance PID fallback + fail-fast changes from `codex/ab-acceptance-rootfallback-1011@f1d9255853ada56bc1da51920ed98a8075cfb8c8`, retaining the current policy constants.
- Acceptance admission now sources the same measured local headless peak (`pilot.HEADLESS_PEAK_BYTES`) as the campaign runner; measured A5 per-run peak is not substituted for the policy pin. Single-game floor is peak + 3 GiB; second small game 14 GiB; emergency stop 3 GiB; private cap 10.5 GiB.
- Revoked the stale execution manifest in place: execution_approved=false; previous file SHA-256 `05f96755d60edee0461e888b0b6bd99bd06094685a7569ece4f9909955057e81`; it carries old 6da7fce/6f4f82 pins. Candidate remains unapproved and must be refreshed/reviewed.
- Focused verification at integrated worktree: `py -3 -m unittest tools.tests.test_map_engine_acceptance tools.tests.test_campaign_runner tools.tests.test_ab_campaign_pilot tools.tests.test_ab_campaign` ? 66 passed.
- No game, boot, map acceptance, or campaign cell launched. PILOT HOLD remains. Do not use the dirty canonical `C:\cameo-wt\ab-campaign` worktree.
- Next: consolidator selectively ports/rebases this branch; refresh all candidate tool/source/schema/map/A5 hashes on exact consolidated SHA; independently review the final tree; complete fail-closed 8-map acceptance under a fresh reservation before considering any outcome cell.
