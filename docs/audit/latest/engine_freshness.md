# audit_engine_freshness — Cameo's engine vs the upstreams it tracks

| | |
|---|---|
| `mod.config` pins | `6da7fce14da541180c6baddd6925118fbef65b94` |
| `engine/VERSION` (what is built) | `git-6da7fce14da541180c6baddd6925118fbef65b94` |

Clone: `C:\Users\AedisToru\Documents\GitHub\cameo-engine`

⚠ The clone's `cameo-engine` is `d5d8b2a685` but `mod.config` pins `6da7fce14d` — the numbers below describe the CLONE, which is ahead of or behind what this repo actually builds.

| upstream | ref last seen | commits it has that we lack | what it is |
|---|---|--:|---|
| OpenRA bleed | `upstream/bleed` @ 2026-10-09 | 18 | the engine everything descends from |
| MustaphaTR rv-engine | `mtr/rv-engine` @ 2026-07-25 | 8 | our direct parent branch; Generals Alpha pins its tip |

⚠ **The ref dates above are that commit's own date, NOT when the clone last fetched.** A number here is only as fresh as the last `git -C <clone> fetch upstream mtr --no-tags`.

### Behind OpenRA bleed by 18 commits

```
e7e9e4ad27 2026-06-16 aigles1 | MapPreviewWidget: show team number instead of spawn letter on map preview when spawn and team are both selected
0cbd48f501 2026-10-04 snowyukitty | Fix SpawnActorPower shroud validation to use the owner
153fd9b406 2026-08-31 aigles1 | Show spawn letter next to team number in spawn selector tooltip on the map preview. This goes together with #22511 to show the team number on the spawn label.
b6fc03fcfa 2026-10-03 GhostCoder6969 | Fix interpolation typo in combined.frag
7d57605bca 2026-02-23 JovialFeline | Adjust allies-06b difficulty
4df197ec44 2026-09-27 Matthias MailÃ¤nder | Add runtime identifiers.
ce08ede1a7 2026-08-29 Paul Chote | Package aarch64 appimages.
cd4848f640 2026-08-29 Paul Chote | Support linux packaging on aarch64.
696fb2ec7c 2026-08-07 Gustas | Fix MockUpdateRule
2cc3abdb05 2026-09-27 Matthias MailÃ¤nder | Package AppStream metadata file.
5688cdfa22 2026-09-15 Mustafa Alperen Seki | Add Trigger.OnSupportPowerActivated.
8156783506 2026-09-15 Acts1631 | Reject lobby order frames before game start
ce92468b37 2026-09-26 Acts1631 | Validate lobby colors before changing state
eb23bff59a 2026-09-15 Acts1631 | Contain malformed lobby command errors
44da8ac643 2026-09-26 Acts1631 | Validate serialized terrain position counts
```
_…and 3 more._

### Behind MustaphaTR rv-engine by 8 commits

```
40065d3e58 2026-07-25 Mustafa Alperen Seki | Try to fix ProductionQueue.Build chosing the wrong queue.
57d48afbbe 2026-07-23 Mustafa Alperen Seki | Style warning fixes.
6cbd098ed1 2026-07-23 Mustafa Alperen Seki | Update OpenRA.slnx
4cd5422b75 2026-07-12 Mustafa Alperen Seki | Check for the correct place for FluentReferences in SupportPowerInfo>Names.
793dd6c7ef 2026-07-12 Mustafa Alperen Seki | Remove unnecessary ISyncs in AS dll.
bc375399b2 2026-07-12 Mustafa Alperen Seki | This exists in downstream now.
ab443403df 2026-07-12 Mustafa Alperen Seki | Didn't wait for everything to copy.
edbd6b0e05 2026-07-12 Mustafa Alperen Seki | Post rebase mess.
```

_Informational: catching up is the `cameo-engine` pipeline in docs/LESSONS_LEARNED.md, and a maintainer decision._
