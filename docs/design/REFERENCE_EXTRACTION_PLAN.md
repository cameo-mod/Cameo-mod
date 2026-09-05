# Reference extraction & faction mapping — the plan

**Owner: Claude-Local.** Lane: `tools/reference/**`,
`tools/balance/{assign_references,faction_routes,faction_extrapolate}.py`, `docs/balance/review/**`,
`docs/reference/**`. No other agent should edit these while this runs.

This is the build order for turning the reference corpus into per-class targets. It supersedes
nothing — it *implements* `REFERENCE_METHOD.md` and the maintainer rulings of 2026-09-05 recorded
in §0 below.

---

## §0 — Maintainer rulings, 2026-09-05. Do not re-litigate these.

| # | ruling |
|---|---|
| R1 | **Class anchors become VIRTUAL.** No class is anchored to a real actor any more. An anchor is a pure number at the **100% point of the price band** (the band is `FORMULA_V2.md` §3's 50–250% envelope of `C₀`). |
| R2 | **The verifier goes virtual too**, and is replaced by a stronger check: a new audit resolves **every** unit in a class through `miniyaml` and asserts the formula reproduces its price. This is strictly stronger than the single 2.5×`C₀` tripwire unit it replaces. |
| R3 | **Faction mapping is a unique SET, members may repeat.** No two Cameo factions share the same combination, but one reference faction may appear in several combinations. The existing law that a reference *unit* is spent once is unchanged. |
| R4 | **Equal thirds, and current Cameo yaml is always one of the voices** — `Cameo TD GDI = DTA GDI × Combined Arms GDI × current Cameo TD GDI`, each 1/3, synthesised by geometric mean. |
| R5 | **Faction profiles are computed per TYPE and overall** — infantry, vehicle, aircraft, naval, defense, and everything-combined — each compared against **the same group in the faction's own source game**, and each reported as an **independent separate value**. Geometric mean. |
| R6 | **Collect every stat we can get.** Some will turn out to be junk; that is a later triage, not a reason to narrow the extraction now. |
| R7 | **Variance is reported three ways**: coefficient of variation, min/max spread, and percentile position of the faction's mean within its own game. |
| R8 | **Armor extrapolation: interpolate between the peer's OWN declared rungs.** Every value a peer actually declares is a fixed anchor on its ladder; rungs between two anchors are interpolated; beyond the outermost anchor the value is held flat. A peer that declares only ONE rung on a ladder votes flat and is **downgraded to low confidence**. ⛔ Never distribute using Cameo's own §12.0i curve — that would make the "independent" reference partly a measurement of ourselves. |
| R9 | **Derived corpus is committed under `docs/reference/`; raw game data stays outside the repo** in `Cameo-mod-reference/`. |

---

## §1 — What we are extracting from

**Two families of source, and they need different extractors.**

**A. INI mods** (Ares/Westwood engines) — already extracted, in
`Cameo-mod-reference/extraction/`, 8183 unit rows:

| source | units | costed | armor rows |
|---|--:|--:|--:|
| Rise of the East 3.0.0c | 2445 | 1666 | — |
| RA2 0XX 1.0.8 | 2104 | 486 | 684 |
| Mental Omega 3.3.6 | 1706 | 786 | — |
| CnC Reloaded 2.7.0 | 1306 | 816 | 355 |
| Red Resurrection 2213 | 1048 | 491 | 480 |
| DTA (Classic + Enhanced overlay) | 869 + 112 | 417 + 85 | `Modifier.*` |
| RA2 Reborn 1.0.31 | 697 | 366 | 176 |

**B. OpenRA peer mods** — 16 declared in `tools/reference/extract_peer_units.py:PEERS`:
`ca` (Combined Arms), `cn` (Crystallized Nexus), `sp` (Shattered Paradise), `rv` (Romanov's
Vengeance), `hv` (Hard Vacuum / OpenHV), `e2140` (OpenE2140), `gen` (Generals Alpha), `fnw`,
`ra2vsh` (Valiant Shades), plus the upstream `ra`, `ra2`, `yr`, `ts`, `cnc`, `d2`, `d2k`.

⛔ **`extract_peer_units.py` does not run today** — see task A1.

---

## §2 — The build order

### Phase A — extraction

| # | task | notes |
|---|---|---|
| **A1** | **Port `mod_id` into `tools/audit/miniyaml.py`** | ⛔ BLOCKER. `Ruleset.__init__` takes one arg; `extract_peer_units.py` calls it with two. The change exists on `origin/claude/docs-audit-reorganize-xgzwhr` (27 insertions, `mod_id: str = "cameo"` default, so every existing caller is unaffected). Devin landed the tooling without it. |
| **A2** | Resolve all 16 peers on disk; report which are missing | `--dry-run`. A missing root is skipped near-silently — read every line. |
| **A3** | INI extractor → the same schema as the OpenRA extractor | RA2/YR 11-slot `Verses=`, TS 5-slot, DTA `Modifier.*`. |
| **A4** | Every stat, both families (R6) | core (HP/cost/speed/range/DPS/armor) + build/economy + combat detail + vision/utility. |
| **A5** | Every `Versus` / `Verses` / `Modifier.*` row | ⚠ In OpenRA yaml `Versus` is a **node with an EMPTY value whose children are the rows**. `node.get("Versus")` returns empty and yields the false result "0 peers expose Versus". Use `weapon_efficiency.versus_of`. |
| **A6** | Armor normalisation per R8 | onto the four ladders in `docs/reference/peer_armor_map.yaml`; confidence gates voting; only `high`/`medium` vote. |
| **A7** | Convert the two UTF-16 reference files to UTF-8 | `PEER_ARMOR_VOCABULARIES.md`, `peer_armor_map.yaml` — PowerShell `>` wrote them; every grep silently under-reads them. |

### Phase B — statistics (R5, R7)

| # | task |
|---|---|
| **B1** | Per-source, per-faction rosters, split by type: infantry / vehicle / aircraft / naval / defense |
| **B2** | Faction "average actor": geometric mean per type **and** overall |
| **B3** | The same aggregate for the **whole source game**, per type — the comparison group |
| **B4** | Faction profile = B2 ÷ B3, per type and overall, each an independent value |
| **B5** | Variance: CV, min/max spread, percentile position — faction vs its own game |

### Phase C — the mapping matrix

| # | task |
|---|---|
| **C1** | For each of the 29 real Cameo factions, assign its reference combination (R3, R4) |
| **C2** | Record it as reviewable data next to `faction_routes.py`, with the reason per pairing |
| **C3** | Verify every Cameo faction reaches the ≥2-reference floor, or is explicitly `UNROUTED` (formula-only) |

### Phase D — virtual anchors (R1, R2)

| # | task |
|---|---|
| **D1** | Migrate `docs/balance/class_anchors.json`: drop `anchor_actor` / `verifier_actor`, keep `spec` as the definition at 100% of the band |
| **D2** | Write the resolver-check audit: resolve **every** unit in a class, assert the formula reproduces its price |
| **D3** | Update `FORMULA_V2.md` §2 — it currently rules that every class has a *living baseline unit in game*, which R1 replaces |

---

## §3 — Traps already paid for. Do not rediscover these.

1. **`git grep` skips several of our weapons yaml as binary** (non-UTF-8 bytes), and **`miniyaml`
   silently under-parses the same files.** For any presence check use
   `git show <rev>:<file> | grep -a`. This nearly deleted 30 live weapon nodes.
2. **A mod's loose `rulesmd.ini` can be vanilla Yuri's Revenge byte-for-byte** — Mental Omega's is
   (md5 `cf7eb658327aff1fe7e6c4e7400eb87f`). Check every extraction against that hash.
3. **`extract_mix_ini.py` sniffs only the first 4096 bytes**, so a rules file with a comment banner
   reports "0 INI blobs found". Judge blobs by full content.
4. **`Versus` is an empty-valued node** — see A5.
5. **A claim about the corpus is not a claim about the world.** "MO and CnC Reloaded are not
   recoverable" was recorded as fact in `REFERENCE_PIPELINE_HANDOFF.md` §1.3; both were on disk.
