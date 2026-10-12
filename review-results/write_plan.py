from pathlib import Path
p=Path('C:/Users/AedisToru/Documents/GitHub/Cameo-mod-fleet/PLAN_2026-10-07_multi_turret_targeting.md')
p.write_text('''# Independent turret and weapon targeting

Maintainer direct assignment, 2026-10-07. Owner: gpt-6.1-sol, task 01a117fe-4fa9-7f23-8416-9805f5fca55a.
Mod: codex/multi-turret-targeting, C:/cameo-wt/codex-multi-turret, base inc@6bb8818e864dbb2598c683d0a40d72a32076fb06.
Engine: codex/multi-turret-engine, C:/cameo-wt/codex-multi-turret-engine, base pinned0e42ed433d237c7844005f2b145e877953053bf4.
Claim expires 2026-10-08T20:00Z; no ownership transfers on expiry. No merge authority inferred.

## Required behavior
- AttackMultiTurreted owns separate target/scan/aim state per physical turret. Conditional upgrade armaments sharing one turret stay in that turret group. Multiple barrels of one armament remain one weapon, rather than pretending barrels are independently articulated turrets.
- Each group selects only visible, hostile, valid weapon targets in its own effective min/max range. Rank by weapon Versus percentage descending, distance ascending, stable ActorID ascending for an exact tie. Face and fire each group independently in the same simulation tick; one reloading, turning, empty, paused, or invalid group cannot suppress the others.
- Same-turret compatible armaments share aim, separate turret groups may choose different targets. Independent selection does not imply forced target spreading: identical weapons may correctly choose the same best target.
- Replace class preference weights with weapon effectiveness ranking, retaining relationship, targetability, cloak/fog, stance, ammo, minimum range and fire-cone eligibility filters. No damage, price, Versus YAML, range or rate-of-fire number changes.
- Honor explicit attack/force-fire targets for every compatible group; incompatible groups may engage their own legal opportunities. Move/stop/hold-fire/return-fire, capture and disable semantics need explicit regressions.
- Passengers continue using exclusive stable fire-port stations. Their weapons use the same dynamic scoring through the existing occupant scanner; multi-turret passengers require separate aim/fire groups within their station without violating the host port cone or letting an unassigned occupant fire.
- Preserve ranged infantry attacks before close-range demolition approach: automatic selection searches currently shootable sniper targets first; C4 approach may start only when that ranged engagement is absent. Explicit attack orders remain explicit. Do not globally delete noncombat/heal/repair acquisition filters.

## Implementation and checks
1. Resolve mounted MiniYaml actor inheritance and enumerate every actor with multiple physical turrets, every attack alias, upgrades and shared turret armaments. Save inventory and use it as migration coverage; include O-I and Yamato explicitly.
2. Add canonical engine AttackMultiTurreted + group state, factored weapon Versus scorer and deterministic target comparator. Share the scanner between ordinary AutoTarget, multi-turret groups, and garrison stations. Establish clear multi-damage-warhead and multi-armor scoring semantics matching resolved damage modifiers; non-damage weapons retain legal targeting fallback.
3. Engine changes: new trait/helper/tests, AutoTarget and the minimum AttackFollow/Garrisoned extension needed for order/force-fire state and passenger grouping. Confirm no overlap with Architect BOT-DETERMINISM before editing shared files. Keep assembly order unchanged.
4. Mod changes: migrate resolved multi-turret actors and inherited cancellations, enable dynamic selection on all applicable AutoTarget templates, remove obsolete class-weight influences, configure ranged/demolition guard for commandos. Handle aliases/dormant/map-local actors with the existing MiniYaml parser, preserve BOM/EOL and inheritance semantics. No engine pin update until a verified upstream engine commit and maintainer/lead pin ruling.
5. Runtime fixtures: cannon vs tank and MG vs infantry; Yamato ground guns + flak simultaneously; O-I all four groups; equal Versus distance tie and equal distance ActorID tie; zero/negative immunity, disabled armor/warheads, conditional upgrades, reload/turn independence, minimum range, manual force-fire, stance/stop/capture, hidden/cloaked targets, passengers/port cone/capacity, sniper-before-C4. Tests must execute firing and actual scanner integration, not only a ranking helper.
6. Gates: independent engine/mod builds and bounded suites, MiniYaml migration coverage and map-actor audit, conflict-marker and garrison capacity audit, deterministic selection tests, fog/mutation audits where applicable, BOOT_GATE=PASS, and independent review. Respect exclusive heavy-run ownership and >=4GiB memory cutoff. Human playtest remains separate.

## Inspection evidence
AttackTurreted.CanAttack turns all listed turrets toward its one target. AttackBase.DoAttack sends that same target to all armaments. AttackFollow also shares RequestedTarget/OpportunityTarget and movement activity. Replacing only AutoTargetPriority numbers cannot deliver independent firing.
Yamato has primary/secondary/flak turret bindings; O-I has main/primary/secondary/back and conditional weapon upgrades. Existing passenger ScanForTarget accepts an explicit armament list and host-port predicate; this is the sharing seam.

Status: isolated trees prepared and inventory running; no gameplay code modified or tests claimed yet. Shared Attack/AutoTarget write-scope coordination sent to Architect and Integrator.
''',encoding='utf-8')
print(p)
