# Shared Agent Workspace

This document is the canonical coordination design for Cameo's developers and their agents.
`AGENTS.md` is its entry point. Technical design and evidence remain versioned in the repository;
GitHub tracks who owns each active task.

**Status: preparation for team review, 2026-09-11.** The private
[Cameo Development Coordination Project](https://github.com/orgs/cameo-mod/projects/1) covers
`cameo-mod/Cameo-mod` and `cameo-mod/OpenRA`. Creating it does not activate this pilot or release
any existing assignments. Aedis and Kmoney are asked to review this design with their agents before
the governance PR is merged. After agreement, record the activation decision here and in the
Project README. Repository Issues are currently disabled; Project draft items support the initial
review, while enabling Issues requires a repository administrator.

The OpenRA repository also needs a reviewed adapter for its own `AGENTS.md` before the pilot is
activated for engine tasks. Its existing instructions name a different base branch. This Project
does not change that repository's rules; confirm the approved `cameo-engine` base in each task.

## Design and responsibilities

Blackrobe uses Codex with GPT-5.6 Sol at High effort. Aedis and Kmoney keep their preferred agent
tools, including Claude, Devin and AionUI where available. Each human remains responsible for
their agents' scope and outputs. Blackrobe has designated Aedis's coordinator agent as the primary
approver for task claims, overlap resolution, work reviews and technical integration readiness.
Blackrobe takes over that approval role when Aedis's agent exhausts its usage quota.

### Approver identity and quota handover

Aedis identifies the coordinator by human plus provider/tool (for example, Aedis's Claude Code
coordinator). The session ID is provenance, not the role identity, because a new session may have
a new ID after restart. An arbitrary agent nickname or another Aedis worker does not inherit the
role. Until the coordinator identity is confirmed, existing explicit human assignments continue.
Each task records its current approver and links the approval receipt. This is delegated approval
within human-authorized project scope;
task-specific HOLDs and decisions explicitly reserved for humans remain binding.

On a quota-limit report from Aedis, his designated agent, or Blackrobe, Blackrobe records the
takeover in the Project and any affected task: previous approver, new approver, time, reason and
last approval/remaining review. Silence alone is not evidence of quota exhaustion. Only the
recorded current approver issues new approvals. Quota recovery does not transfer control back
automatically: Blackrobe records handback after Aedis's agent reads the intervening decisions.
Existing claims and accepted evidence survive both transfers. GitHub does not enforce this
handover or monitor provider quotas; it is an explicit coordination procedure.

```text
Human owner + preferred agents
          |
          v
GitHub task record -> approved scope -> isolated writer checkout
          |                                  |
          +------------- evidence / draft PR-+
                                             |
                                   designated approver review
```

Discord carries scoped discussions, handoffs and review requests. When Codex communicates using
Blackrobe's account, messages start with `[Codex]`. No Discord bot or shared server execution is
part of this preparation. ACP and MCP can connect clients and tools, but provide no file locks
or task ownership by themselves.

## Source-of-truth map

| Need | Canonical location | Rule |
|---|---|---|
| **Live task ownership and claim state** | Organization GitHub Project plus linked issue or draft item | Sole live assignment ledger for migrated pilot tasks after activation. The designated approver records approvals and overlap checks; no atomic lock is implemented. |
| Lessons learned / start protocol | `docs/LESSONS_LEARNED.md` | Read before every new task: accumulated pitfalls and safe defaults. (It carries a convenience copy of the reading order; `docs/README.md` is the canonical one.) |
| **Project state and priority rationale** | `docs/HANDOFF.md` | The single project handoff. It supersedes dated handoffs but does not grant an agent write claim. |
| Short project orientation | `docs/README.md` | One page for a first-time reader; every document above is authoritative over it. |
| Program backlog | `docs/design/ROADMAP.md` | Crashes and player-visible bugs are P0 and always jump the queue. It records program priority, while the linked GitHub task record carries live ownership. Closed July items live in `docs/history/ROADMAP_ARCHIVE_2026-07.md`. |
| Balance program and acceptance criteria | `docs/design/BALANCE_PROGRAM_PLAN.md` | W1–W26 and the technical order of operations (§0a). Dated ownership tables are context; confirm reservations before changing them. |
| Numeric claims that must not rot | `docs/audit/doc_claims.yaml` | Every number a DECISION rests on, with its re-measure command. Update `value` AND every doc under `docs:` in the same commit. |
| Binding rules and conventions | `docs/DESIGN.md` | Read before modifying YAML, assets, naming, weapons, balance, or descriptions. |
| Engine and custom-trait reference | `docs/Cameo_Knowledge_Base_Manual.md` | Consult before changing unfamiliar traits or C#-backed behavior. |
| Audit overview | `docs/audit/SUMMARY.md` | Read first for known issue classes and current audit status. |
| Detailed audit findings | `docs/history/audits/BASELINE_FINDINGS.md`, `docs/audit/CONSISTENCY_REPORT.md` | Update only from evidence produced by a current audit or engine boot. |
| Audit scripts | `tools/audit/` | Scripts are reusable tooling; do not duplicate them into personal-agent folders. |
| Generated audit output | `docs/audit/latest/` | Regenerate via `tools/audit/run_all.sh`; it is the current evidence set. |
| Baseline audit evidence | `docs/audit/baseline/` | Historical comparison only. |
| Faction reference | `docs/FACTIONS.md`, `docs/factions/MATRIX.md` | Use for display-name, faction-role, roster, and documentation checks. |
| Migration process | `docs/MIGRATION.md` | Use for naming, actor splits, asset movement, and Fluent migrations. |
| External-agent historical evidence | `docs/history/LEGACY_DEVIN_CABAL.md` | Historical register only. No external output is current until rerun in this repository. |
| Archived handoffs | `docs/history/handoffs/` | Dated session records. Provenance only — **never** resume work from one. |

## Cross-provider coordination

During preparation, work continues under explicit human assignments. After activation, use the
following procedure for tasks the team has moved into the Project. Move existing work only after
its human owner confirms the current branch, scope and status; an old roster entry cannot be
silently treated as free space.

1. Create one task record for one independently mergeable outcome. Record the human owner,
   current approver, provider/session ID, base commit, writable and excluded paths, dependencies,
   acceptance evidence, runtime impact, and claim review time. Include the branch, a portable
   worktree label and the exact reading route from `TASK_INDEX.md`. Use `repository#issue` as the
   ID, or the permanent Project draft-item URL while Issues are unavailable.
2. After approval, the writer creates the provider-prefixed branch, an empty claim commit, and
   pushes that ref to the agreed remote. The remote branch ref is the verifiable claim receipt;
   a local branch name alone is not a claim. If pushing is not authorized or fails, remain
   read-only and record the blocker. The current approver (Aedis's designated agent, or Blackrobe
   during quota fallback) then checks
   active claims, existing reservations and PRs for overlapping paths and shared technical
   dependencies, then approves `Claimed` with a linked receipt. Workers cannot self-approve.
   Work implemented by the approving agent needs a separate reviewer before integration. For the pilot,
   explicitly enumerate paths and directory prefixes; review globs conservatively. Different
   files can still conflict through shared templates, schemas or generators.
3. Start each writer in an isolated worktree or clone from the pushed branch/base with its own
   branch and approved base.
   Read-only reviewers may overlap. Start the pilot with at most one write task per human; expand
   only after the handoff process works. A local supervisor may use workers, but must isolate each
   independent writer and partition the parent's approved scope.
4. Record outcome, changed paths, base and head commit, evidence, remaining risks and next action.
   Code changes use a linked draft PR; research tasks may finish with an accepted review artifact.
   Source YAML and derived ledgers belong together where required by the balance pipeline.
5. The current approver decides technical readiness. Agents may execute a merge only under explicit human
   authorization; green checks or a `Review` status do not grant it. Mark code tasks `Done` only
   when the accepted change has a merge receipt. Keep an explicit HOLD until its human owner
   releases it.

### States, recovery and claim expiry

Use `Intake`, `Ready`, `Claimed`, `In progress`, `Review`, `Blocked`, `Done`, and `Abandoned`.
`Ready` means scoped but unclaimed. `Claimed` and `In progress` require recorded approver approval.
`Review` and `Blocked` retain the reservation until the current approver releases it. `Abandoned` means
the owner stopped the task and left recoverable evidence; it never means delete the branch.

Keep Project fields small: Status, Priority, Claim expires and Work class; use built-in Assignees
and Repository fields where available. `Claim expires` is a DATE reminder. Put the precise
timestamp with timezone in the task body. It schedules approver reassessment, not an expiring lock.
On expiry, stop further writes until renewed; preserve the files and reservation. A replacement
cannot take over until the current approver has confirmed the previous writer stopped, inspected its
branch/diff, and explicitly released or reassigned the scope.

After a crash or restart, recover from the task ID, provider session ID, base/head commit and
worktree label. Inspect the actual checkout and current task record before resuming. A friendly
agent name is not an identity. If the board is unavailable or state is uncertain, preserve work,
continue read-only investigation and contact the current approver instead of self-assigning.

### What is enforced and what is a convention

| Mechanism | Actual guarantee |
|---|---|
| Git worktree or separate clone | Separate working files when writers stay inside their checkout; not a security sandbox |
| Issue form and PR template | Prompts for scope and evidence; cannot prevent unauthorized edits or enforce correctness |
| Project states, timestamps and designated approver review | Explicit coordination; no atomic claims, lease service, automatic expiry, quota monitor or filesystem lock |
| Local validation and human review | Record actual checks on the relevant commit and their limits; CI is disabled and is not an activation prerequisite |
| Repository protection | Only the rules configured by an administrator; this preparation does not add review or required-check enforcement |

The initial pilot must exercise two simultaneous requests for one scope, a restart, and an
expired claim with unfinished work, plus quota takeover and handback. In each case the approver must preserve one writer and a
recoverable handoff. These are acceptance scenarios to run before expanding the pilot, not tests
already performed by this documentation PR.

With two agents, review can be mutual rather than independent: each side may review the other's
work while the current approver also integrates it. Record that limitation on consequential tasks
and escalate to a third reviewer when the pilot's humans decide it is needed.

## Required operating sequence

Read `AGENTS.md`, use `TASK_INDEX.md` to locate existing tools and technical decisions, then read
the relevant current source and evidence. `docs/README.md` maps the technical documentation.
Record a task before implementation during the active pilot; prioritize reproduced crashes and
player-visible regressions. Current evidence wins over dated state claims, but a measurement does
not authorize a gameplay-policy change.

Inspect `mods/cameo/mod.yaml` before auditing YAML/data and follow active include lists. Use the
existing MiniYAML resolver for inheritance. Refactors require an appropriate before/after resolved
comparison. Run focused checks for affected behavior, then broaden only when the risk or result
requires it. Report baseline failures, new regressions and environment limits separately.

Documentation and template-only changes need syntax, links and diff validation. C# or engine
shader changes need the current build procedure. YAML/assets need a restart, not a C# rebuild.
Before `make all`, run `tools/preflight-build.ps1` from the configured Cameo checkout and stop if
it fails; do not overwrite merged local engine work or alter a pin to bypass it. Preserve engine
source in the separate OpenRA clone and mirror approved engine edits into the intended Cameo
engine copy for validation. A pin update requires a verified merged upstream engine commit and
explicit human authorization. Do not run `--check-yaml` or `make test`, which invokes it.

`Continuous Integration` (`.github/workflows/ci.yml`) is disabled in GitHub, verified on 2026-09-11.
Keep packaging, deployment and other workflows unchanged. Use relevant local checks and human
review; do not enable CI, add replacement CI, or make CI a pilot activation condition without a
new explicit human request. PR #343 added a branch filter only; it did not enable that workflow.

For engine-content changes, a merge requires the applicable boot evidence. If launch authorization
or the runtime environment is unavailable, hold the engine change for a human rather than merging
it without proof. For other runtime-affecting changes, keep build, menu-load and in-game proof distinct. A menu-load test
must identify the tested commit/build and new logs; it cannot establish gameplay correctness.
Launch the game only when authorized. If runtime review is pending, say so in the PR instead of
claiming complete validation. Never change OS security settings or bypass a blocked launch as
part of validation. Human gameplay and visual sign-off remains a separate acceptance decision.

## Git workflow and commit rules

Inspect remotes, base and dirty state before making changes. Fetching does not authorize merging
into a shared checkout. Keep agent writes in isolated checkouts, stage named files, and preserve
others' work. Publish to the human owner's fork or another explicitly authorized remote; use PRs
for upstream integration. Never force-push, delete or reassign others' branches without authority.

Use clear commit titles describing behavior. Record actual agent/provider provenance in the task
and PR; do not infer ownership from shared Git authors or copy another agent's attribution.
Update the canonical document affected by the change, not every status file. An agent may execute
explicitly authorized publication/merge steps. Humans set the authority and scope; Aedis's
designated agent reviews within that delegation, with Blackrobe as the quota fallback.
Pending reviews, explicit holds and failing checks must be reported before any integration decision.

## Documentation rules

- **Never use absolute local file paths in any repository document.** Always use relative paths from the repository root (e.g. `mods/cameo/rules/defaults.yaml`, not `C:\Users\...\mods\cameo\rules\defaults.yaml`). Other contributors and AI agents have different local paths. Absolute paths leak personal filesystem information and break on other machines.
- External personal folders are referenced by name only (e.g. "the external DevinCameoProject folder"), never by absolute path.

## Audit organization

- `tools/audit/`: executable detectors and shared parsing/model infrastructure.
- `docs/audit/latest/`: generated current reports; overwrite only by running the suite.
- `docs/audit/baseline/`: immutable-ish baseline snapshots.
- `docs/audit/`: human-maintained summaries, decisions, consistency records, and imported historical registers.
- External personal folders: scratch space only. They must link to this file and must not be treated as authoritative.

## Current incident protocol

For an engine crash or player-visible visual regression:

1. Preserve the exact exception and failing asset/actor/sequence.
2. Compare the affected actor, sequence key, asset filename, and tooltip/display references against the last known-good release.
3. Add an evidence-backed P0 task to the active intake, linked to the program backlog when relevant.
4. Do not modify unrelated palette, template, or naming data while root cause is unproven.
5. Verify the affected behavior at the appropriate runtime boundary before marking the incident resolved.

## Pilot activation and deferred work

Before activation, record Aedis's designated coordinator identity and the team's acceptance of
the status meanings, quota handover, recovery procedure and task access. The primary/fallback
approval roles are decided; the governance design still awaits team review. A repository administrator decides whether to enable
Issues in both repositories. Review/status-check branch protections are a separate team decision,
not an activation prerequisite. Report any required check that blocks merging before changing
protections. No Project field or template changes repository permissions.

Start with three small independent tasks and one handoff/restart exercise. Review accepted
outcomes, duplicated effort, abandoned work and time spent coordinating before increasing writers.
Keep old worktrees and branches intact; inventory only work the owners identify as active.

This preparation does not deploy a Discord bot, host an agent service on the game server, add an
automatic lease/lock service, configure AionUI for teammates, or choose CODEOWNERS. Any future
automation should justify its cost against the manual pilot and be reviewed separately.
