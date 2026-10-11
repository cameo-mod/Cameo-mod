# STATUS: Campaign complete overlay + RAM thresholds

## Resume
- Branch: `codex/ab-campaign-policy-overlay-followup`, based on `origin/codex/ab-campaign@169388f` (campaign branch still needs consolidation/rebase onto current master e49).
- Source fixes: build the campaign mod root by hard-linking the complete pinned `mods/cameo` tree except `ai/`, preserving generated dual-arm AI; fallback to copy across volumes. Separate first-game admission (measured headless peak + 3 GiB), second small-game admission (14 GiB), 10.5 GiB process cap, 3 GiB hard host stop; big teams exclusive, max two small teams.
- Focused verification: `py_compile` and `python -m unittest tools.tests.test_ab_campaign_pilot tools.tests.test_campaign_runner` (24 passed), `git diff --check`.
- Open gates: refreshed manifest/tool hashes, consolidated-tree integration/review, runtime overlay acceptance. No game, boot, or acceptance was launched by this change.
