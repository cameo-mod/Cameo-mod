# Cameo Project Context — Agent Summary

Use this as a short orientation document. It summarizes the repository documentation; the referenced primary documents remain authoritative.

## Project

Cameo is an OpenRA crossover RTS mod. The repository is undergoing a migration toward self-contained faction ContentPacks, consistent actor/asset naming, auditable balance rules, and safer rule changes. The last known-good release used for regression comparisons is the local Cameo-IFV release install (use as golden reference for regression diffs).

## Required reading

**The canonical reading order is defined in `docs/README.md`.** The list below
is provided for convenience; if it disagrees with README.md, README.md wins.

Read these documents in order before any implementation work. Load all of them into context at the start of every session.

1. **`CLAUDE.md`** (repo root) — project instructions, loaded every session.
2. **[LESSONS_LEARNED.md](LESSONS_LEARNED.md)** — accumulated pitfalls, safe defaults, and the required reading order.
3. **[AGENT_WORKSPACE.md](AGENT_WORKSPACE.md)** — mandatory workflow, evidence rules, incident protocol, and commit gate.
4. **[DESIGN.md](DESIGN.md)** — binding rules and conventions (especially before modifying YAML, assets, naming, weapons, balance, or descriptions).
5. **[design/ROADMAP.md](design/ROADMAP.md)** — active work queue; record new bugs and ownership before implementation.
6. **[audit/SUMMARY.md](audit/SUMMARY.md)** — current known-issue state by bug class.

Crashes and player-visible regressions always take priority over queued work.

## Current focus

- **Active program:** mod-synthesis balance overhaul (see `design/BALANCE_SYNTHESIS.md` and `design/ROADMAP.md` ★ MAJOR PROGRAM). Goal: fix extreme-value balance by synthesizing extracted mods into class anchors, then re-derive all stats via the universal class formula.
- **B8 crash-class content: 0** — all previously known crash-class issues are resolved. Historical incidents (TD GDI palette revert, `brik:` sequence fix) are documented in `audit/INCIDENT_TD_GDI_RELEASE_REGRESSION.md` (crash resolved, boot-verified; remaining: TS-only death palette audit pending).
- Do not change palettes, templates, actor names, or tooltip data merely because a migration looks suspicious. Require an observed mismatch, current audit output, release comparison, or engine exception.

## Multi-agent rule

The repository docs are shared truth. An external historical/scratch folder (DevinCameoProject) is retained only for provenance. Its roadmap and instructions point back to this repository; do not create or maintain a second active roadmap or audit-output tree there.
