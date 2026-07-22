# Cameo Project Context — Agent Summary

Use this as a short orientation document. It summarizes the repository documentation; the referenced primary documents remain authoritative.

## Project

Cameo is an OpenRA crossover RTS mod. The repository is undergoing a migration toward self-contained faction ContentPacks, consistent actor/asset naming, auditable balance rules, and safer rule changes. The last known-good release used for regression comparisons is:

`C:\Users\AedisToru\AppData\Local\Cameo-IFV\instances\cameo\main`

## Required reading

Read these documents in order before any implementation work. Load all of them into context at the start of every session.

1. **[LESSONS_LEARNED.md](LESSONS_LEARNED.md)** — accumulated pitfalls, safe defaults, and the required reading order.
2. **[AGENT_WORKSPACE.md](AGENT_WORKSPACE.md)** — mandatory workflow, evidence rules, incident protocol, and commit gate.
3. **[DESIGN.md](DESIGN.md)** — binding rules and conventions (especially before modifying YAML, assets, naming, weapons, balance, or descriptions).
4. **[README.md](README.md)** — canonical owners and generated-artifact policy.
5. **[design/ROADMAP.md](design/ROADMAP.md)** — active work queue; record new bugs and ownership before implementation.

Crashes and player-visible regressions always take priority over queued work.

## Current safety focus

- The currently reported TD GDI palette/animation issue is open. Its evidence record is `audit/INCIDENT_TD_GDI_RELEASE_REGRESSION.md`.
- A menu-load crash was observed from two `brik:` sequence entries referencing nonexistent `futuretech_concretebarrier_brik.shp`. Local references were returned to the existing release-compatible TD filenames; a clean boot remains required before resolution.
- Do not change palettes, templates, actor names, or tooltip data merely because a migration looks suspicious. Require an observed mismatch, current audit output, release comparison, or engine exception.

## Multi-agent rule

The repository docs are shared truth. `C:\Users\AedisToru\Documents\DevinCameoProject` is retained as an external historical/scratch folder only. Its roadmap and instructions point back to this repository; do not create or maintain a second active roadmap or audit-output tree there.
