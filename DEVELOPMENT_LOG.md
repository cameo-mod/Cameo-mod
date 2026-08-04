# Development Log

## 2026-07-18 — BALANCE PIPELINE LIVE (all agents read this)

**NEW LAW: never hand-edit balance numbers in yaml.** The pipeline is
implemented and enforced (`docs/design/BALANCE_PIPELINE.md`, CLAUDE.md
"Balance changes" section, DESIGN §12):
extract_stats.py → docs/balance/*.json (raw-stat ledger, committed) →
build_workbook.py → cameo_balance_v2.xlsx (gitignored workbench) →
import_workbook.py → apply_balance.py --confirm (maintainer order) →
re-extract, audits, boot, commit yaml+ledger together.
`audit_balance_drift` in run_all fails RED whenever yaml and ledger
disagree — hand edits cannot land silently anymore.
Loop PROVEN: exact fixed point + live 1000→1050→1000 round trip.
Phase 5 (per-class anchors via fit_class.py + class_anchors.json)
awaits maintainer anchor picks; the fixed-point test also exposed and
fixed an order-dependent resolver-cache-poisoning bug in
tools/audit/miniyaml.py that affected ALL resolved-value audits.

## 2026-07-18 — Claude session (TKM port + Blackrobe batch)

- TKM CONTRIBUTOR PORT (`3bb6a34b3`): full-repo zip from a community
  contributor analyzed (base = cea431010 with pre-rename-id payload),
  translated through the applied rename_map_tkm, per-actor 3-way
  merged into the pack. Arsenal-tree redesign, GP-25 replaces M203,
  Berezka speed/cloak, engineer field kits, new weapons + warhead .cs
  (DLLs rebuilt). Deviations flagged in the commit (kept warfactory
  ProvidesPrerequisite — his removal would orphan every
  ~tkm_warfactory prereq).
- TKM MOVED into ContentPacks/RedAlert2Mod (`d981d65fe` renames +
  `915714fe8` manifest/mod.yaml — the renames rode the earlier commit
  via the staged index; completion committed immediately). Theme
  folder rename POSTPONED (Blackrobe) — candidates logged in ROADMAP.
- Monster tank Tesla/Thermonuclear rockets (`d981d65fe`): real weapon
  swaps (mammoth logic) replace the imperceptible +10% multipliers;
  duplicate ActorStatValues fixed earlier in `71765570b`.
- Survival (`e8af695eb`): superlinear ramp, wave-size floor (dip fix),
  veteran waves; win-objective fix earlier in `71765570b`. `survival 2`
  copy was deleted by the team (`32669f345`) — main copy carries all.
- SM passive income (Blackrobe): moondairyfarm verified correctly
  wired; the missing piece (ra2oilderrick/ra2ywall conyard provisions)
  is the MAINTAINER'S OTHER SESSION's uncommitted WIP — do not
  double-fix. Laser Beetle/M200B report: wiring verified WAD
  (replacement promotions retire them); if the REPLACEMENTS don't
  appear despite bought promotions, check rank1 granting in-game.
- NEXT: FULL SM REBALANCE (ROADMAP P1, sheet-first, workbook free).

## 2026-07-17 — Claude session SID-20260717-cl4b7e (RA1 legacy rename + two-session repair pass)

**Landed (commits `fdd466494`, `4cf7e6909` + this session's repair commit):**
- RA1 LEGACY-ID RENAME complete: all 52 old-style ids (RAE1, PT/DD/CA,
  SS/MSUB, POWR/APWR/RASILO, BADR family, naval yards, civilians, husks,
  8 upgrade proxies) → grammar-compliant ids; only `japan` unprefixed.
  Applied by tools/rename/apply_ra1_legacy.py (context-scoped successor
  to apply.py). zerofighter collision → japan_zerofighter_slave.
- Umlaut transliteration (schwarzermond_ubermensch), CABAL plasmaturret
  buildable + mobilestealthgenerator removed, stale RA1 monoliths deleted.
- REPAIR PASS after two-session collision (this entry's second half):
  1. 13 explicit `actor_<oldid>.description/.name` yaml refs broke when
     ftl keys renamed (whole-identifier pass can't see through the
     `actor_` prefix) — added a fluent-stem pass to the applicator
     (combined-alternation regexes; 52 sequential re.subs was too slow)
     and fixed all 13. audit_fluent: 17 → 0 unresolved.
  2. warcraft2_en.ftl + tkm_en.ftl were NEVER registered in mod.yaml
     FluentMessages — WC2/TKM faction descriptions showed raw keys.
     Registered both.
  3. 19 audit reports in docs/audit/latest/ were UTF-16-corrupted by a
     concurrent session's PowerShell `>` redirect (10 committed
     corrupted). Regenerated the whole suite via bash run_all.sh (UTF-8).
     Lesson saved to agent memory.
- Verification: full audit suite green (fluent 0 unresolved, consistency
  73/0, packs P2 = known D2k suffix-style backlog only), resolver spot
  checks green (3913 actors / 2365 weapons, zero old ids), FACTIONS.md
  clean of old ids, boot gate to main menu.
- SM promotion grid: implemented by the concurrent session in
  SchwarzerMond/yaml/promotions.yaml with CABAL-pattern gating BUT the
  chains deviate from the maintainer's image; row order under redesign —
  see ROADMAP P2 (sharpened 2026-07-17 with maintainer's MARS/tier
  clarifications + reshuffle proposal). DO NOT touch the grid before the
  maintainer picks an option.
- NOTE for all agents: SCUD/SCUDNUKE (RedAlert/Soviets weapons.yaml) are
  legacy-uppercase WEAPON ids shared with generals/darkreign — WPN-MIGRATE
  scope, intentionally untouched by the actor rename.
- SM PROMOTION GRID FINALIZED (maintainer decision): columns
  infantry | vehicles | air/artillery/support, tier-laddered rows —
  see ROADMAP P2 (RESOLVED) for the binding table. promotions.yaml
  re-chained, `..._promotion_bermensch` → `..._promotion_ubermensch`,
  ^PromotionUnitBuff stripped from 10 non-promotion SM units
  (FutureTech convention: grid units only). Boot green.
- NEW ORDER: FULL SM REBALANCE (sheet-first; post-buff-strip stats;
  38 stat_formulas findings as the seed) — queued as ROADMAP P1.

## 2026-07-16

**Task:** Diagnose ACP connection issue with Claude.
**Done:**
- Confirmed ACP refers to Agent Client Protocol; Claude integration is typically via `claude-agent-acp` / `claude-code-acp` or inside Devin Desktop/Windsurf/Zed/JetBrains.
- Checked Cameo-mod repo: no ACP/Claude config present.
- Checked local environment: `node`, `npm`, `devin`, `claude`, and `claude-agent-acp` are not on PATH for this shell; no Windsurf ACP registry (`~/.windsurf/acp/registry.json`) or Windsurf logs found.
**Diagnosis (after user logs):** Devin Desktop/Windsurf is trying to spawn `npx -y @agentclientprotocol/claude-agent-acp@0.59.0`, but `npx` is not found in the IDE's PATH (`spawn npx ENOENT`). The ACP client needs Node.js installed (>=20.19 for this package) and available to the IDE process.
**Fix applied:**
- Downloaded and extracted Node.js v24.18.0 LTS to `%LOCALAPPDATA%\Programs\nodejs\node-v24.18.0-win-x64`.
- Added the Node `bin` directory to the user `PATH`.
- Set PowerShell execution policy to `RemoteSigned` for the current user so `*.ps1` scripts (including `npx.ps1`) can run.
- Installed `@agentclientprotocol/claude-agent-acp@0.59.0` globally via `npm`.
- Verified `node -v`, `npx -v`, `claude-agent-acp --version`, and `npx -y @agentclientprotocol/claude-agent-acp@0.59.0 --version` all work.
**Next:** Restart Devin Desktop/Windsurf so the IDE process picks up the updated `PATH`, then enable the Claude agent again.

## 2026-08-04 — Balance ledger re-extract

- Refreshed 32 per-faction JSON ledgers from the current resolved ruleset (`python tools/balance/extract_stats.py`).
- Drift check: 0 drifted.
- Multiplier audit: 0 non-integer `Modifier` values (run with `PYTHONIOENCODING=utf-8`).
- Boot-gate: reached main menu (`PostWorldLoaded`), no new `exception-*.log` files.
- Committed updated ledgers + current uncommitted YAML rule sync (Yuri Slave Miner cost/build duration, `^SwarmlingGrinderTemplate` Valued default).
