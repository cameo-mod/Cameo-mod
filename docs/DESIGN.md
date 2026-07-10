# OpenRA Cameo — Design Document

_The distilled, binding design contract for this mod. Every AI agent session
and every contributor reads this FIRST. The long-form analysis and roadmap
live in [MASTER_REPORT.md](MASTER_REPORT.md); the machine-checkable state
lives in [audit/](audit/) — this document is the rules themselves._

_When code and this document disagree, the document wins unless an audit
baseline explicitly defers the fix (e.g. "DEFERRED" findings). When this
document is silent, check MASTER_REPORT §13, then ask design._

---

## 1. Naming (the RA1-Soviet baseline)

```
unit/building id :=  [game_]faction_nameinonegroup[_variant]
tech item id     :=  [game_]faction_(upgrade|promotion|doctrine)_nameinonegroup
```

- The **name is ONE lowercase group without separators**: `ra_heatraytank`,
  `forgotten_ghoststalker`, `forgotten_experimentalmammothtank`.
- **The only separator is the underscore — hyphens are banned in ALL
  naming we own** (design 2026-07-10): actor ids, asset file names
  (`cabal_dissolver_weapon.shp`, never `cabal_dissolver-weapon.shp`),
  fluent keys (`actor_forgotten_scoopertank`, never `actor-…`), and every
  yaml reference to them. Hyphens double as token boundaries in tooling
  and caused the rename crash class. Exception: identifiers the ENGINE
  defines or derives (built-in condition names like `build-incomplete`,
  engine chrome/fluent keys) stay as the engine spells them.
  **C#-derived names count as engine-owned**: fluent keys composed in
  code (`actor-stats-label-prefix.*`, `label-armor-class.*`,
  `checkbox-*` graphics options, `support-power-timer`, ...) and chrome
  collections composed in code (`sidebar-<faction>`) keep their hyphens
  even when defined in our files — scan ALL assemblies (engine mods AND
  OpenRA.Mods.CA/Cameo in-repo) for string literals before renaming any
  key family. A no-exception global rename was considered and rejected
  (2026-07-11): it would require C# changes, which are out of bounds.
- **Game prefix only on actual collisions** (`td_gdi` vs `ts_gdi`,
  `ra1_soviet` vs `ra2_soviet`). Unique factions (cabal, forgotten, yuri,
  ordos, terran…) take no game prefix. Prefixes are added the day a
  collision appears, never preemptively.
- **Tech markers are full words**: `upgrade` (cash research), `promotion`
  (rank-gated), `doctrine` (mutually exclusive picks). Team proxies end
  `_proxy_actor`. Promotions never carry "unlock" in the id.
- **Variants** are structural suffixes: `_husk _sp _r4 _wild _mk2 _elite
  _ai _water`.
- **Tooltip ↔ id consistency**: the id's name group derives from the
  Tooltip Name and both stay in sync. No two actors of a faction may share
  a Tooltip Name (audit_metadata M1). New display names are a **design
  decision — propose options and let design choose** (the blue Tiberian
  Fiend became "Vinifera Fiend").
- **Asset files follow their actor id**: body sprite `<id>.<ext>`, icon
  `<id>_icon.<ext>`; multi-file bodies keep only their distinguishing
  suffix (`forgotten_warfactory_door.png`). Shared sprite archives
  (DATA.R16-style) and files shared between images are never renamed after
  one unit.
- **Cross-actor namespaces are sacred**: voice sets, notifications, shared
  art are NEVER renamed with a unit. `tools/rename/apply.py` protects
  audio files and `VoiceSet:` lines structurally.

Migration is per faction via `tools/rename/rename_map_<faction>.yaml`
(curated, reviewed) + `tools/rename/apply.py`, proven behavior-preserving
with `tools/audit/dump_resolved.py` before/after diffs (must be empty).

## 2. Content pack layout

```
mods/cameo/ContentPacks/<Theme>/<Faction>/
  content.yaml            # the pack's include list
  rules/                  # split per actor type:
    faction.yaml buildings.yaml defenses.yaml infantry.yaml vehicles.yaml
    aircraft.yaml naval.yaml upgrades.yaml promotions.yaml husks.yaml
    templates.yaml        # faction-local ^templates only
  weapons/weapons.yaml    # ONE file per faction
  sequences/sequences.yaml# ONE file per faction
```

- **naval.yaml holds ALL naval content: every ship AND the naval yards**,
  even though naval yards normally count as buildings. The lobby option
  that unlocks the naval prerequisite can then also dynamically load (or
  skip) the whole naval asset set based on the lobby setting.
- Weapons move into a pack only when used **exclusively** by that faction
  (computed through warhead sub-weapon and Inherits closure); shared
  weapons stay in the theme/shared files.
- Splits are done with `tools/packs/split_faction.py`, byte-preserving,
  and verified: merged actor/weapon/sequence registries must be identical
  before and after, and the faction's resolved closure diff must be empty.
- mod.yaml `Include:` order defines the lobby faction order.

**End goal — dynamic faction loading.** Content packs exist so the game can
load ONLY the factions picked in the lobby (and only what the active
shellmap needs) instead of every actor at boot: Cameo is the ultimate
crossover RTS and will keep growing — at peak it consumed 12 GB of RAM,
locking out 8 GB and 4 GB players before the main menu. Therefore packs
must become COMPLETELY self-contained, loadable without cross-dependencies:
- a separate ai.yaml per faction inside its pack;
- all game files the faction needs — sprites, voxels, icons, sounds —
  inside the pack, cleanly separated into per-type subfolders;
- shared content lives only in theme Shared/ packs or the core;
- a future audit walks every pack, verifies which files are actually used,
  and deletes the unused ones.

## 3. House stat formulas (audited as F1–F18, `audit_stat_formulas.py`)

Reference-clean units: **TD GDI Archer** (`gdiarcher`), **Ordos Raider**
(`raider.ordos`).

| rule | formula |
|---|---|
| Repair | `Repairable.HpPerStep = HP / 20` (non-infantry) |
| Self-heal | `ChangesHealth@SelfHealing.Step = HP / 2500`; infantry `HP / 1000`; infantry never has Repairable |
| Upgrade shields | `Shielded.RegenAmount = 2 × SelfHealing Step` (Ixian model) |
| Defense vision | `RevealsShroud.Range = weapon range` |
| AA / advanced defense detection | `DetectCloaked.Range = weapon range / 2` |
| Defense power | `Power.Amount = -(Cost / 20)` |
| Vehicle turning | `Mobile.TurnSpeed = Speed / 5`; `Turreted.TurnSpeed` equals it |
| Turretless (AttackFrontal) vehicles | `TurnSpeed = 2 × Speed / 5` — the former artillery exception was dropped 2026-07-10 (data check: turretless artillery split 24 at 2×, 18 at 1× — no real pattern) |
| Turreted artillery / fire support | Archer firing-slow: `GrantConditionOnAttack(firing)`, 50% Speed/Turn/TurretTurn multipliers, `RevokeDelay = weapon ReloadDelay / 2` |
| Fighters & bombers (by template) | `Aircraft.TurnSpeed = Speed / 15` (frontal-weapon craft 2×) |
| Helicopters & spaceships (by template) | `Aircraft.TurnSpeed = Speed / 5`, like vehicles (design 2026-07-10; 45 of ~55 helicopters already comply) |
| AA support vehicles | anti-air weapon range = **1.5 × anti-ground range** (forgotten_m113adats is reference-clean: 5606 / 8409) |
| AA weapons | a weapon whose ValidTargets include Air must have ≥1 damage warhead that hits Air (inheritance-resolved) |

Unit classification is authoritative from the **class templates in
defaults.yaml** (`^HighTechTankTemplate` ⇒ vehicle, whatever the render
traits say). Power plant vision currently: 4c0 small / 5c0 advanced —
a project-wide size/cost scaling rule is TODO.

## 4. Tech tier rules (F12/F13)

Building tiers are data-driven from prerequisite chains (conyard 0,
barracks/refinery 1, radar 2, tech 3, post-tech 4+; faction-relative,
cheapest provider wins).

- Every faction has an **anti-air tower on its radar tier**. Additional AA
  at tech tier or above is legal "advanced AA".
- An AdvancedDefense with an AA weapon may HOLD the radar tier when the
  faction has no dedicated radar-tier AA (jballistat, Ixian Rocket Turret).
- **Advanced defenses gate ABOVE the radar tier** — tech building or later
  (Tier 4/5 gates are fine).
- **Every faction always keeps a Tier-1 defense**; violations whose fix
  would remove the last pre-radar defense are DEFERRED until a Tier-1
  defense is added.
- Exemptions: single-armed-defense factions (Protoss photon cannon),
  promotion-gated defenses (transitional), Terran/Zerg (non-tiered).

## 5. Starting units (F14–F16)

- Every referenced actor must exist (crash class).
- **Light Support**: Tier-1 only (nothing gated above its producer
  building), total ≈ **2000**, diverse, ≈ **5 infantry : 1 vehicle**,
  pricier units never outnumber cheaper ones.
- **Heavy Support**: same ratio/frequency rules at ≈ **10000**, mixing all
  tiers (at least one above-Tier-1 unit).

## 6. Upgrades, promotions, power curve

- One source of truth per layer (class templates / faction upgrades /
  promotions); actors never restate a layer's values.
- Every upgrade has an intent line in `docs/design/upgrades_intent.yaml`
  (direction, coverage, phase, intended drawbacks) — feeds B3/B4 audits.
- Multiplier semantics: reload/build <100 = faster; damage taken <100 =
  tankier; firepower/range/speed >100 = stronger.
- **Promotions grant options (units/abilities); research grants stats.**
- Roster-wide upgrades must cover the full roster (audit_upgrade_coverage).
- Worst-case stack budget ≤ 2.0× fresh-self effective power
  (audit_power_budget; veterancy ladders are exclusive, count best rank).
- AA gating, defense tiers, and tech trees per §4.

## 7. Descriptions & localization (rollout in progress)

- `\n` in yaml descriptions does not work anymore. **All names and
  descriptions live in Fluent files**; yaml references
  `actor-<id>.name` / `actor-<id>.description`.
- Scheme = RA1 Soviet style: one-line role summary; ability lines as
  separate indented lines; `Strong vs …` / `Weak vs …` at the end.
- Upgrade descriptions open with the tier tag ("Tech Upgrade (Only affects
  units of own faction)" / "Team Upgrade (…)" / "Promotion Upgrade (…)"),
  then one effect per line with exact stats and affected units (grouped
  when many). Promotion descriptions give a one-line summary of the
  unlocked unit.
- Every description is **verified against resolved traits, weapons, and
  upgrades** — it says what the unit does and the unit does what it says.
  Trait PRESENCE is not ability: a trait gated on RequiresCondition
  (crate cloaks, stealth-gen fields, spell states) is not an innate
  capability and must not be described as one. Weapon claims come from the
  weapon definition (inherits, warheads, reports), never from source-game
  lore — the Ghost Stalker's "railgun" was a lore error the code refuted.
- Rollout is incremental: one actor → review → one faction → review → all.
  Renames come FIRST so fluent keys are minted against final ids.

## 8. Assets & audio

- WAV norm: **mono / 16-bit / 22050 Hz** (audit_assets prints ffmpeg fixes).
- Sprites named per §1; icons end `_icon`.
- Voice sets are shared resources named for the VOICE, not a unit.

## 9. Operating rules for agents

1. Read this document, then `docs/audit/SUMMARY.md` for current state.
2. Run the relevant audit before and after your change
   (`tools/audit/run_all.sh`, or a single `audit_*.py`).
3. Classify bugs as B1–B12 (MASTER_REPORT §4); if the class detector
   missed your bug, improve the detector in the same change.
4. Small typo-class fixes (wrong condition name, weapon-as-voice, swapped
   tooltips): **fix immediately and report it** so design knows.
5. Display-name changes and balance-affecting decisions: propose options,
   let design choose first.
6. Renames/refactors are proven behavior-preserving with resolved-diff
   snapshots; balance changes are never mixed into them.
7. Clean commits, one concern each; commit when design says so.
8. Every new unit ships with: naming-compliant id + `_icon`, Fluent keys,
   ai.yaml wiring, roster-wide upgrade hooks, class template, sequences
   that resolve, and a changelog line (Definition of Done,
   MASTER_REPORT Appendix D).

## 10. Actor & faction uniqueness (design north star)

- **No two units or defenses use identical weapons.** Every armed actor
  owns its own weapon entries (`audit_weapon_uniqueness.py`). Sharing is
  legal only inside one actor's own variant family (`_sp`/`_elite`/husk/
  paradrop twins, garrisoned armaments of the same actor) and for
  systemic utility weapons (C4, DefuseKit, capture/heal/repair tools).
- Beyond weapons, every actor should be **unique in its own stats and its
  weapons' stats** — each actor has its own character and feeling. No two
  actors of a faction may feel the same, and especially no two actors of
  DIFFERENT factions may feel the same. Factions express identity through
  themed actors; uniqueness is a faction-identity feature, not polish.
- Weapon-dedup findings are **balance/design work**, never mechanical
  auto-fixes: propose per-actor stat divergence options, let design
  choose, then implement.

## 11. Garrison weapons (audit_garrison_weapons.py)

- **Every garrison-capable infantry with a damaging weapon carries a
  garrisoned armament** (`Name: garrisoned`) — commandos included (the
  Ordos Face Dancer is a commando in Cameo, so he fires from garrisons).
- Design exceptions live in `docs/design/garrison_exceptions.yaml`:
  melee, suicide/bomb attackers, and casters/mind-control do not fire
  from garrisons. Engineers without combat weapons and units garrisons
  cannot accept (non-Infantry CargoType) are auto-exempt.
- **G2 miswire class**: an `Armament@GARRISONED`-style block WITHOUT
  `Name: garrisoned` silently becomes a second primary — double-fire in
  the open, silence in bunkers (live cases: TS engineer pistol, RA1
  Imperial Scoutsman, M113 Adats-style condition typos).
- **G3: garrisoned armaments never carry a FireDelay.**

## 12. Balance formula — the Cameo Armor System workbook

_Source: `C:\Users\AedisToru\OneDrive\Dokumente\Cameo Armor System.xlsx`
(sheets: Armor Types, Weapon Types, Infantry, Tanks, Vehicles, Aircraft,
Defenses; Tabelle2/3 are scratch). 333ggg's CABAL concept
(`Downloads\cabal.xlsx`) uses the same sheet layout. Tooling: openpyxl
reads AND writes these — formula changes can be re-applied to every
unit programmatically. Research 2026-07-11; open questions marked ❓._

**The cost identity.** Every unit sheet computes three cost estimates
from the stats and averages them; the design workflow INVERTS this:

```
DPS  = Damage / ReloadDelay × WeaponClass                 (column J)
O    = (HP/100000 + Speed/100 + Range·K/5 + DPS/200) × 200 × L × M
P    = (HP·Speed/25000 + Range·K·DPS/2.5) × L × M
Q    = HP·Speed·Range·K·DPS·L·M / 12 500 000
Cost = (O + P + Q) / 3                                    (column R = S)
```

O is linear in each stat, P pairwise (survivability × mobility and
reach × damage), Q the full product — so cost grows superlinearly when
everything is high at once. **Workflow: the price S is set FIRST** (last
column); Range is then solved from the identity (column F formula), so
tuning HP/Speed/Damage/Reload auto-rebalances Range to hold the price.
Range and DPS cells are never hand-edited.

**Column semantics** (values observed; meanings to confirm):
- `WeaponClass` H ∈ {0.75, 0.875, 1, 1.05, 1.125, 1.25, 1.5} — a weapon
  quality multiplier on DPS. ❓ exact mapping to the Weapon Types sheet.
- `Special` K ∈ {0.75, 1, 1.25, 1.5, 1.75, 2} — ability premium.
  ❓ which abilities cost which factor.
- `UnitClass` L — per-section class factor (infantry sections 0.4–1,
  vehicles 0.25–1.25, defenses 0.225/0.325/0.35). ❓ table of sections.
- `TechTier` M ∈ {1, 0.75, 0.5} — appears to DISCOUNT stats for
  higher-tech units. ❓ which tier maps to which value.
- ❓ Defenses have no movement — what goes in their Speed column?

**Armor & versus system (hypothesis to verify in yaml).** 20 armor
classes in 4 categories (Infantry: None/Flak/Plate/Hero; Vehicles:
Scout/Light/Medium/Heavy/Superheavy; Aircraft: Fighter/Bomber/
Helicopter/Spaceship; Buildings: Wood/Concrete/Steel) with a base
ladder in ~4% steps (None 100 … Wood 56 … Spaceship 4?) and two armor
ORDERINGS. Weapon Types carry: effectiveness ranks 1–4 vs
Infantry/Vehicle/Aircraft/Building, a weapon band (light/medium/heavy),
and SCALING tables — six bands (SmallArms/Light/Medium/Heavy/
Superheavy/Superweapon) with per-rank percentage columns starting at
100 and stepping by 6/5/4/3/2/1 per rank, plus a low table starting at
15–40. ❓ hypothesis: a weapon's `Versus` per armor = scaling table
value at that armor's rank in the ordering matching the weapon band —
needs one worked example to confirm before generating yaml.

**Definition of Done for a formula unit:** stats from the sheet map to
yaml as HP→`Health.HP`, Speed→`Mobile.Speed`, Range (cells)→weapon
`Range` (×1024 wdist), Damage→warhead `Damage`, ReloadDelay→weapon
`ReloadDelay` (ticks); versus table per the armor system; every new
unit gets its own unique weapon (§10).

## 13. Map props (Obstacle target type)

- Trees, rocks, utility poles and other decorations carry
  `TargetTypes: Ground, Obstacle` (templates `^Tree ^TreeHusk ^Rock ^Box`).
- `Obstacle` exists for AI logic (minelayer bot ignores it), **never for
  weapons**: no weapon lists Obstacle in Valid/InvalidTargets — props are
  hit as plain Ground.
