# Cameo — one-page orientation

A short orientation for a first-time reader. **Everything it summarises is authoritative over
it**; if this page and a primary document disagree, the primary document wins.

**Working on the project? Start at [`HANDOFF.md`](HANDOFF.md), not here.**

## What Cameo is

An OpenRA mod: a crossover RTS drawing content from Tiberian Dawn, Tiberian Sun, Red Alert 1
and 2, Dune 2000, StarCraft, WarCraft 2 and more, plus original factions. It keeps growing, and
that growth is the source of its central engineering problem.

## The architectural goal

**Dynamic faction loading.** Loading every faction at boot peaked at **12 GB RAM**, which locked
out 8 GB players before the main menu. The end state is that the game loads only the factions the
lobby picked (and only what the shellmap needs). That requires every faction to be a fully
self-contained **ContentPack**: rules, weapons, sequences, its own `ai.yaml`, and all assets in
per-type subfolders — zero cross-pack dependencies, shared content only in theme `Shared/` packs,
unused files audited and deleted.

Progress and the runbook: [`MIGRATION.md`](MIGRATION.md).

## The three programs running in parallel

| program | what it is | where it lives |
|---|---|---|
| **ContentPack migration** | make each faction self-contained so factions can be loaded on demand | [`MIGRATION.md`](MIGRATION.md) |
| **Weapon rebuild** | split every weapon into warhead / projectile / effect layers, and collapse each to ONE damage warhead | [`design/WEAPON_3WAY_SPLIT.md`](design/WEAPON_3WAY_SPLIT.md), board items W23/W24 |
| **Balance overhaul** | price every unit through a mechanical pipeline instead of by hand, so no agent can silently drift the numbers | [`design/BALANCE_PIPELINE.md`](design/BALANCE_PIPELINE.md), [`design/BALANCE_PROGRAM_PLAN.md`](design/BALANCE_PROGRAM_PLAN.md) |

They are sequenced, not independent: **the weapon rebuild comes before pricing**, because a
price is a function of the weapon's warhead set and `Versus` profile, and both are still
changing. `BALANCE_PROGRAM_PLAN.md` §0a is the binding order.

## How work is verified here

Cameo has an unusually large safety net because it has been bitten by bug classes that no
ordinary review catches — a valid yaml file that boots cleanly and plays wrong.

* **The boot gate** is absolute: the game must reach the main menu with no new exception log
  before any commit of engine content.
* **The audit suite** (`bash tools/audit/run_all.sh`, 54 audits) writes its evidence into
  `docs/audit/latest/`, and `docs/audit/SUMMARY.md` is the human summary of it.
* **`docs/audit/doc_claims.yaml`** pins every number a decision rests on, with the command that
  re-measures it, so a claim in prose goes red instead of quietly rotting.
* **The balance ledger** (`docs/balance/*.json`) mirrors the live rules; `audit_balance_drift`
  goes red the moment yaml and ledger disagree. Balance numbers are never hand-edited.

## Safety focus

Do not change palettes, templates, actor names or tooltip data because a migration *looks*
suspicious. Require an observed mismatch, current audit output, a release comparison, or an
engine exception. The last known-good release used for regression comparison is the local
Cameo-IFV release install.

## Who works here

The maintainer (AedisToru), co-maintainer (Blackrobe), other human contributors, and AI agents.
The repository is the shared source of truth — **do not create a second roadmap, audit tree or
design contract outside it.** An external historical scratch folder (DevinCameoProject) is
retained for provenance only; nothing in it is current.
