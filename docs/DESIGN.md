# OpenRA Cameo — Design Document

_The distilled, binding design contract for this mod. Every AI agent session
and every contributor reads this FIRST. The long-form analysis and roadmap
live in [MASTER_REPORT.md](MASTER_REPORT.md); the machine-checkable state
lives in [audit/](audit/) — this document is the rules themselves.
Faction lore, gameplay profiles, and roster details live in
[FACTIONS.md](FACTIONS.md)._

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
- **Underscores separate SECTIONS, never within names** (design 2026-07-16):
  The actor id has exactly three sections — `[game_]faction_actorname[_variant]`.
  Underscores are ONLY used to separate these sections. Faction names and actor
  names are each a single unbroken lowercase group with NO internal underscores.
  - **Correct**: `td_gdi_lighttank`, `ra1_soviets_heavytank`, `wc2_humans_footman`,
    `asianalliance_quasar`, `steelconsortium_manta`, `latinsyndicate_freedomfighter`,
    `schwarzermond_lunarsoldier`.
  - **Wrong**: `asian_alliance_quasar` (underscore inside faction name),
    `steel_consortium_manta`, `latin_syndicate_freedomfighter`.
  - Faction InternalName must match the faction section of the actor prefix
    exactly: actors `td_gdi_*` → faction `td_gdi`; actors `asianalliance_*` →
    faction `asianalliance`.
- **Umlauts and other non-ASCII letters TRANSLITERATE, never drop**
  (design 2026-07-17): ids derive from display names by mapping
  Ü→u, ü→u, Ö→o, ö→o, Ä→a, ä→a, ß→ss — e.g. `Übermensch` →
  `schwarzermond_ubermensch` (the earlier `_bermensch`, with the Ü
  silently dropped, was a bug). Weapon and sequence ids follow the
  same rule (`ÜbermenschLaser` → `UbermenschLaser`). DISPLAY names
  (Tooltip `Name:`, fluent text) keep their proper umlauts.
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
  `ra1_soviets` vs `ra2_soviets`). Unique factions (cabal, forgotten, yuri,
  ordos, terran…) take no game prefix. Prefixes are added the day a
  collision appears, never preemptively.
- **Tech markers are full words**: `upgrade` (cash research), `promotion`
  (rank-gated), `doctrine` (mutually exclusive picks). Team proxies end
  `_proxy_actor`. Promotions never carry "unlock" in the id.
- **Variants** are structural suffixes: `_husk _sp _r4 _wild _mk2 _elite
  _ai _water _EMP _AA _upgraded` plus dotted variants (`.husk`) and
  paradrop twins (`para`).
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
- **Asset suffix words are ALWAYS full words, never abbreviations**
  (design 2026-07-14): construction animations use `_make` (never `_mk`,
  never compressed old-name + `mk` like `ntcnstmk`); building bib overlays
  use `_bib` (never `_bb`); damaged/idle/active/dead sequences use their
  full names. When migrating old assets, strip any compressed old-name
  prefix from the suffix and replace short forms with full words:
  `ra2_soviets_constructionyard_ntcnstmk.shp` →
  `ra2_soviets_constructionyard_make.shp`. The only exception is `_mk2`/
  `_mk3` as unit variant markers (Mark II/III), which are part of the
  actor name, not a sequence suffix.
- **Sequence filenames must match their actor and sequence name**
  (design 2026-07-16):
  - **Idle/body sprite**: the primary body sprite filename MUST be
    `<actor_id>.<ext>` and placed in the `Defaults:` section so all
    sequences that use the same body sprite inherit it automatically.
    Example: `latinsyndicate_topolsilo.shp` goes into `Defaults:`,
    not `latinsyndicate_topolsilo_cg12hit_f.shp` in each idle/damaged-idle/
    critical-idle entry.
  - **Non-idle sequences**: every sequence that is NOT the idle/body
    sprite MUST include the sequence name as a suffix in the filename:
    `<actor_id>_<sequence_name>.<ext>`. Examples:
    `latinsyndicate_topolsilo_bib.shp` (bib sequence),
    `latinsyndicate_topolsilo_make.shp` (make sequence),
    `ra2_soviets_constructionyard_icon.shp` (icon sequence),
    `cabal_tarantula_turret.shp` (turret sequence).
  - **Sequence name suffixes use full words**: `_bib`, `_make`, `_turret`,
    `_muzzle`, `_icon`, `_active`, `_dead`, `_damaged`, `_critical`,
    `_shadow`, `_deploy`, `_up`, `_down`, `_harvest`, etc. These match
    the sequence key names exactly (hyphens in sequence keys like
    `damaged-idle` map to underscores in filenames: `_damaged_idle` or
    are collapsed to the distinguishing word only: `_damaged`).
  - **Shared files are NEVER renamed** (critical safety rule):
    - Files in `shared_sprites|`, `ts_shared_sprites|`, `td_shared_sprites|`
      and other shared namespaces (`invisibleitem.shp`, `gunfire2.shp`,
      `electro.shp`, `frag3.shp`, `parach_shadow.shp`, `ra2_oregath.shp`,
      etc.) are used by many actors and MUST stay with their shared name.
    - Death sequence files defined in inherited templates like
      `^RA2InfantryDeaths`, `^RA2BasicInfantry`, `^RA2ProneInfantry`
      are shared across all inheriting actors and MUST NOT be renamed
      to match any single actor.
    - Template default filenames (e.g., `ra2gi.shp` in `^RA2BasicInfantry`)
      are placeholder defaults that get overridden per-actor; the template
      file itself stays as-is.
    - Muzzle flash files (`gunfire2.shp`, `ra2_gunfire.shp`) shared across
      many actors stay as-is.
    - DATA.R16-style shared sprite archives stay as-is.
    - Voice sets, notifications, and shared art are already protected by
      the cross-actor namespace rule above.
  - **Combine sequences**: sub-images referenced in `Combine:` blocks
    that are unique to one actor should be renamed to
    `<actor_id>_<descriptive_suffix>.<ext>`. Sub-images shared across
    multiple actors stay as-is.
  - **Inherited template defaults**: when an actor inherits from a
    template (e.g., `^RA2ArmedInfantry`) and overrides `Defaults:
    Filename:`, the override filename MUST be `<actor_id>.<ext>`. The
    template's own default filename (e.g., `ra2gi.shp`) is a shared
    baseline and is NOT renamed.
  - **Migration approach**: per faction, curated via
    `tools/rename/rename_map_<faction>.yaml` + `tools/rename/apply.py`,
    verified with `tools/audit/dump_resolved.py` before/after diffs
    (must be empty). A pre-flight audit script must build a complete
    cross-reference of which filenames are used by which actors to
    identify shared files that must NOT be renamed.
- **Shared asset files are NEVER renamed after one user (LAW,
  2026-07-17).** The naming migration renamed `brik.shp` (a TD concrete
  barrier used by several factions) into a nonexistent
  `futuretech_concretebarrier_*` name and broke the menu; the TD GDI
  voice variant keys broke the same way. An asset file may be renamed
  ONLY after a full cross-reference proves exactly one actor uses it.
  Shared assets keep their original names and move to the owning
  Shared pack instead. After every rename batch run
  `audit_asset_files.py` (A1 must be 0). Golden reference for
  pre-rename values: `C:/Users/AedisToru/AppData/Local/Cameo-IFV/
  instances/cameo/main` (the last release before the renames).
- **The wall target type is lowercase `wall`** (evidence 2026-07-17:
  all TargetTypes definitions + 345 weapon refs are lowercase; treat
  it as engine-adjacent vocabulary, never capitalize).
- **Cross-actor namespaces are sacred**: voice sets, notifications, shared
  art are NEVER renamed with a unit. `tools/rename/apply.py` protects
  audio files and `VoiceSet:` lines structurally.
- **Weapon names must include the full actor id as a prefix**
  (design 2026-07-16): weapon ids follow the pattern
  `<actor_id>_<weapon_descriptive_name>`. The actor id prefix makes it
  immediately clear which actor owns the weapon and prevents name
  collisions across factions. Examples:
  `td_gdi_commando_sniper`, `cabal_tarantula_cannon`,
  `ra2_soviets_conscript_ak47`, `latinsyndicate_topolsilo_nuclear`.
  - **Weapon class templates keep their PascalCase `^` prefix**:
    `^SmallArms`, `^MediumCannon`, `^HeavyMissile`, `^RA2FlakWeapon`,
    `^LaserWeapon`, etc. These are shared class templates, not actor
    weapons, and are NOT prefixed with an actor id.
  - **Faction-level weapon templates** (shared across multiple actors of
    one faction) use PascalCase with the faction prefix:
    `^CabalMissile`, `^RA2RadShell`, `^RA2EliteEffects`. These are also
    NOT prefixed with a full actor id since they serve multiple actors.
  - **Elite weapon variants** append `_elite` — this is the ONLY accepted
    suffix for elite weapons, regardless of naming style. Legacy PascalCase
    weapons that still use the `E` suffix (e.g. `BorisAKME`) must be
    migrated to `_elite` when touched. See §16.3 for the full convention.
    Example: `ra2_soviets_conscript_ak47_elite`, `BorisAKM_elite`.
  - **EMP weapon variants** append `_EMP`: `steel_consortory_emp_cannon`,
    `ra1_tesla_tank_zap_EMP`. This suffix identifies weapons whose
    primary function is to disable vehicles via EMP effect. It is
    primarily used by Steel Consortium but may appear on other factions.
  - **AA weapon variants** append `_AA`: `ra2_allies_ifv_missile_AA`,
    `SWAWingGun_AA`. This suffix identifies anti-air weapons — weapons
    whose `ValidTargets` include `Air`.
  - **Upgraded weapon variants** append `_upgraded` or the upgrade name:
    `cabal_artilleryspider_shell_upgraded`.
  - **Combined suffixes** follow this order: `<weapon_name>_AA_EMP_elite`
    (e.g. an elite EMP anti-air weapon). The base descriptive name comes
    first, then capability tags (`_AA`, `_EMP`), then the rank tier
    (`_elite`) last.
  - **Migration**: per faction via `tools/rename/rename_map_<faction>.yaml`
    + `tools/rename/apply.py`, verified with
    `tools/audit/dump_resolved.py` before/after diffs (must be empty).
    Weapons shared across multiple factions (in theme Shared/ packs) stay
    as-is and are NOT renamed after any single actor.

Migration is per faction via `tools/rename/rename_map_<faction>.yaml`
(curated, reviewed) + `tools/rename/apply.py`, proven behavior-preserving
with `tools/audit/dump_resolved.py` before/after diffs (must be empty).

## 2. Content pack layout

```
mods/cameo/ContentPacks/<Theme>/<Faction>/
  content.yaml            # the pack's manifest (include list) — root level
  yaml/                   # ALL MiniYaml, split per concern:
    faction.yaml buildings.yaml defenses.yaml infantry.yaml vehicles.yaml
    aircraft.yaml naval.yaml upgrades.yaml promotions.yaml husks.yaml
    templates.yaml        # faction-local ^templates only
    weapons.yaml          # ONE weapons file per faction
    sequences.yaml        # ONE sequences file per faction
    ai.yaml               # per-faction bot data (once the AI split lands)
  files/                  # ALL assets in per-type subfolders:
    icons/ sprites/ voxels/ sounds/
  translations/en.ftl     # Fluent (not MiniYaml, stays its own folder)
```

**Structure decisions (design consultation 2026-07-16):**
- **The `yaml/` folder name STAYS.** It was decided 2026-07-12 and rolled
  out across all 27 packs on 2026-07-14; renaming again is churn without
  behavioral gain. (If it were greenfield, `data/` would be marginally
  more descriptive — not worth a second migration.)
- **The per-concern file split STAYS — do NOT merge rules files.** With
  multiple concurrent contributors (maintainer + 333ggg + two AI agents),
  small per-type files minimize merge conflicts, keep audits targeted,
  and make wrong-section placement detectable. One merged rules.yaml
  would undo all three.
- **The standard file set above is CLOSED.** A pack may omit a file it
  doesn't need, but must not invent new names — `audit_packs.py`
  enforces the set, so tooling can always find everything.
- **content.yaml becomes machine-generated** (`tools/packs/gen_content.py`):
  regenerated from the files on disk, deterministic ordering; the audit
  fails on drift. Nobody hand-edits include lists.
- **Wrong-section rules** (the "actor in the wrong pack/file" bug class):
  every actor id in a pack MUST carry the pack's faction prefix (Shared
  packs excepted); actors live in the file matching their class template
  (naval.yaml holds ships AND naval yards — existing rule; husk variants
  in husks.yaml; upgrade/promotion actors in their marker files).
  `audit_packs.py` checks both.

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

**Building stat templates (design 2026-07-13).** All non-defense
buildings must derive their core stats from the appropriate template in
`mods/cameo/rules/defaults.yaml` (`^BaseBuilding`, `^RadarBuilding`,
`^IsTechnoBuilding`, `^IsAircraftFactory`, `^RepairFacility`, etc.). A
child actor must **not** duplicate a value that its template already
provides (e.g., `Armor`, `Power`, `Health`, `RevealsShroud`,
`DetectCloaked`). Overrides are allowed **only when justified by a
special faction mechanic** (for example, the TD Nod Temple of Nod or the
GDI Advanced Communications Center count as a tech building but gain an
add-on that turns them into a superweapon, so they use the lower
`^IsTechnoBuilding` health and then apply a 50% damage reduction to end
up equivalent to the `^Superweapon` template). Defenses are exempt because
each faction's defenses are individually balanced against their
weapons and roles, but they still obey the **defense power** rule above.

**The dual-weapon AA law (design 2026-07-11).** A SEPARATE anti-air
weapon is allowed ONLY on support vehicles (`^SupportVehicleTemplate`
family / AA-support class) — sole exception: the Ordos Anti Air
Trooper, a dedicated AA special. Whenever a unit carries both a ground
weapon and an AA weapon, the two must be **mutually exclusive in
ValidTargets** (ground weapon: `Ground, Water` only — never Air;
AA weapon: `Air` only), otherwise the missile templates (whose
ValidTargets already include Air) double-dip and the unit deals twice
the intended anti-air damage. Reference-clean pattern: the RA1 Soviet
Flak Truck. Rocket INFANTRY never gets a second AA weapon — the
missile class templates already target Ground+Air with one weapon
(TS rocket infantry pattern). Support vehicles get the 1.5× AA range
(rule above); a non-support dual-role unit (line breaker, e.g. CABAL
Manticore) uses the SAME range, weapon class, reload, and DPS for
both weapons.

**AA weapon construction — always inherit the ground twin (house
rule, codified 2026-07-12; 76 existing AA weapons follow it).** An AA
weapon is NEVER redefined from scratch. It `Inherits: <the ground
weapon>` and then only overrides what differs: `ValidTargets: Air`,
the AA `Range`, and each damage warhead flipped to `ValidTargets:
Air` (the DamagesConcrete / CreateEffect / smudge warheads are left
alone). This keeps the AA's damage, class mix, reload, projectile and
trail identical to the ground weapon by construction — never
hand-copied. Reference: `ArmoredCarMGAA: Inherits: ArmoredCarMG`.
Corollary: shared projectile behaviour (launch angle, turn rate,
trail — §3 rocket rules) belongs in ONE faction/family missile
template that the ground weapons inherit last, so the AA twin picks it
up automatically through the ground weapon (e.g. `^CabalMissile`).
Never copy a projectile block across weapons.

**Weapon grouping order in YAML (design 2026-07-13).** Within a
faction's `weapons.yaml`, every weapon family must be kept in a strict
order so upgrades and twins are easy to audit and impossible to miss:

1. **Basic weapon** (the unupgraded ground version).
2. **Elite / veteran twin** if any.
3. **Anti-air twin** (inherits the basic weapon, only changes
   `ValidTargets` and AA range).
4. **Upgrade variants** in the order they are unlocked — if a unit has
   multiple upgrades, list the combined versions after the single
   upgrades. Reference pattern: the Consortium Quantum Missile Trooper
   weapons show the full progression of basic → elite → anti-air →
   upgrade → combined upgrade.

The same rule applies inside an actor's `Armament` blocks: the basic
`Armament@PRIMARY` must come first, then the upgraded armament directly
below it (e.g., `Armament@UPGRADE` right after `Armament@PRIMARY`), then
garrison variants, then anti-air variants. Never scatter related weapons
across the file.

**Weapon mount offsets (design 2026-07-11 — always apply).** Every
firing armament needs a `LocalOffset` so the muzzle sits at the barrel,
not the actor's ground-center. For INFANTRY the default is
**`LocalOffset: 128,0,256`** (128 forward, 0 sideways, 256 up ≈ chest
height) — the mod-wide most common value. Apply it to every non-garrison
infantry armament that has no offset of its own; garrison armaments
(`Name: garrisoned`) keep none (they fire from the building's port).
A missing offset is not cosmetic for arcing/launched projectiles: the
round spawns at ground level and can explode under the unit's feet —
this is why the CABAL Rocket Cyborg could not shoot. When creating or
editing ANY infantry, set the offset. Vehicles/aircraft use
weapon-specific `LocalOffset`s at their barrels (not this default).

**Alternating (twin-muzzle) offsets.** A `LocalOffset` with TWO triplets
(six values), e.g. **`LocalOffset: 128,-64,256, 128,64,256`**, gives two
fire points that the weapon alternates between — the left barrel
(`-64` sideways) then the right (`+64`). Use it for any dual-weapon /
twin-barrel unit (the CABAL Devout's twin chainguns, gatling arms, etc.)
so bursts visibly alternate left/right instead of stacking on one point.
The single-triplet default `128,0,256` is for one centred muzzle.
**Match a unit to its analogue.** When two units share a weapon family,
copy the reference unit's offset AND muzzle setup wholesale (the CABAL
T800 gatling was aligned to the Yuri Gatling Trooper's `544,100,256`).
Note the muzzle style can differ: the Gatling Trooper shows a visible
tracer projectile and no `WithMuzzleOverlay`, whereas the T800 carries
`WithMuzzleOverlay` + `MuzzleSequence: muzzle` (a sprite muzzle flash) —
pick one style per look and keep the pair consistent.

**Preserve a unit's unique projectile when reforging its weapon.** A new
weapon that only `Inherits:` a class template (e.g. `^LightChemical
Weapon`) takes that template's plain projectile and DROPS any custom
`Projectile:` the old weapon had (Image/Speed/Palette/Inaccuracy). The
CABAL Dissolver lost its `tsdissolvereffect` spray this way in the Batch
2a rebuild. Always carry the old `Projectile:` block over (or restore it
from git) when replacing a unit's signature weapon.

**Laser beams — two colours, scaled by damage (design 2026-07-13).**
Every `LaserZap`/beam weapon uses **two colours**: a `Color` (outer beam)
and a `SecondaryBeam` (`SecondaryBeamColor`, inner core). Never a single
thin line. **Both the beam width AND the colours scale with the weapon's
damage** — a bigger-damage beam is thicker and reads as more dangerous
(brighter/hotter core, deeper outer). Rough CABAL ladder: turret-class
lasers `Width ~18–24` / secondary `~8–12`; heavy single-shot lasers
(Core Defender) thicker still, but even the heaviest keeps scaling rather
than maxing out. **CABAL beams are a mix of purple + dark blue** (e.g.
outer `~6622CCCC`, inner `~9977FFEE`) — never too thin. Dual-beam units
(Manticore) must **spread the two beam offsets apart** so the pair reads
as two beams, not one. Pair every beam with a colour-matched impact
effect (3 damage-scaled ground-impact levels for CABAL lasers) + a sound.

**Obelisk / laser sound map (design 2026-07-13).** The three obelisk
reports are NOT interchangeable:
- **`obelmod1.aud`** = Tiberian **Sun** obelisk — the Obelisk of Light,
  the Obelisk of Darkness, and the CABAL Obelisk building weapon, and the
  Laser Spider.
- **`obelcor3.aud`** = the Core Defender's weapon; also DarkObeliskLaser
  and both Commando plasma weapons (do not change these).
- **`obelray1.aud`** = Tiberian **Dawn** obelisk — **NOT allowed on any
  TS unit** unless explicitly specified. A weapon that only
  `Inherits: ^LaserWeapon` is using this TD version; override it for TS.
- Smaller/turret lasers use the laser-turret report **`lastur1.aud`**.

**TS rocket projectiles — launch straight up (design 2026-07-11,
replicating Shattered Paradise).** Tiberian-Sun-style rockets fire
near-vertically then home down onto the target. On the `Missile`
projectile set **`MinimumLaunchAngle: 255` and `MaximumLaunchAngle:
255`** (255 ≈ 90° in our WAngle scale; SP uses the same 255 = "90
degrees"). A steep launch overshoots close targets unless the missile
can turn fast enough, so ALWAYS pair it with a high turn rate —
**`HorizontalRateOfTurn: 128` / `VerticalRateOfTurn: 128`** (SP's
value; our `^LightMissile` default of 40 is too slow and causes the
overshoot). The Guardian GI's rocket was the reference bug: launch 200
with the default turn 40 always overshot at short range; fixed by
raising its turn to 128. Set both angle and turn per-weapon (never edit
the shared `^*Missile` templates, which serve all factions).

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
- **Stat-modifier philosophy** (design 2026-07-11, modeled by
  ^PromotionUnitBuff's +5%/+10% steps): speed and range are only ever
  increased MODERATELY (never big jumps); damage-taken multipliers may
  stack, but the COMBINED product must never drop below **50%** — below
  that units feel undamageable. Hard floor; audit extension TODO
  (worst-case stacked DamageMultiplier per unit < 0.5 = finding).
- **ActorStatValues upgrade list** (design 2026-07-17): the `Upgrades:`
  field on `ActorStatValues` was expanded from a maximum of 5 entries to
  **10**. Every unit must list all faction upgrades (and only faction
  upgrades, never team upgrades) that affect it so the actor-stat widget
  can display the full set of upgrade icons. Gaps are bugs; additions or
  removals of upgrade inherits must be mirrored in the ActorStatValues list.
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
- **Cameo/build/upgrade icons are always 64×48 px** (RGBA or RGB PNG;
  the mod-wide dominant size — design 2026-07-11). Any new icon,
  including upgrade research icons, is authored at 64×48. An upgrade
  actor's icon comes from a `sequences` entry named for the actor with
  an `icon:` sub-node (`Filename: <name>.png`), and the actor's
  `RenderSprites.Image` points at that same name — do not leave an
  upgrade borrowing a unit cameo.
- Voice sets are shared resources named for the VOICE, not a unit.
- **Custom animated effects (explosions, muzzle flashes, projectile
  trails) are authored as RGBA PngSheets** (design 2026-07-12; the
  proven pattern used by the neutron-shell `magicnuke.png` and the
  CABAL rocket trail `cabal_rockettrail.png`). A single horizontal PNG
  strip of equal-size frames carries two PNG `tEXt` chunks —
  `FrameSize: W,H` and `FrameAmount: N` — and a sequence references it
  with `Filename:`, `Length: N`, optional `Scale`, `Tick`. **An RGBA
  sheet renders in true colour with NO palette** — omit
  `TrailPalette`/`ExplosionPalette` entirely (this is why magicnuke
  looks right and why the smoke below broke when a palette was forced
  onto it). Keep effect frame counts small for trails (≤10, fading to
  transparent so each puff dies on its own).
- **Palette pitfall for indexed `x_smokey` trails**: they use the
  **`effect`** palette. `effect75alpha` is a D2K/Dune alpha palette
  (`PaletteFromPaletteWithAlpha`) — forcing it onto a non-Dune sprite
  like `blue_smokey` re-tints it (blue → dark green). Use `effect` for
  indexed trails, or an RGBA PngSheet (no palette) for full colour.
- **Per-frame randomness in animated effects (design 2026-07-12).** A
  new effect must NOT be identical geometry every frame — the first
  CABAL rocket trail came out as near-perfect spheres and read as
  low-quality. Give each frame small random offsets, distortion, and
  lobe variation (seeded, so it's reproducible) while still expanding
  and fading over time. Build soft fields with numpy gaussians, not PIL
  ImageDraw (which replaces pixels), and VERIFY the rendered preview
  before committing — a blank or bland sprite loads without error.
- **Effect-warhead naming LAW (design 2026-07-12).** The principle is
  **ONE `CreateEffect` warhead per impact surface**, named by that
  surface — because OpenRA overwrites same-named warhead nodes across
  inheritance, a canonical per-surface name guarantees exactly one
  effect survives and a weapon can never stack two overlapping impacts
  on the same surface. The legal names are:
  - **`Warhead@Effect`** — the ground/default impact (always this name,
    never `@2Eff`, `@3Eff`, `@DissolveEffect`, …).
  - **`Warhead@EffectAir`** — only when the weapon can hit air.
  - **`Warhead@EffectWater`** — only when the weapon can hit water.
  - **`Warhead@ShieldHitEffect`** — the `ValidTargets: Shielded` hit
    effect (shield-impact sound), present in the class templates.
  - The HeavyBomb template's two effects are the last sanctioned case.
  Rename anything outside this set to the matching surface name.
- **Effect + sound are always defined TOGETHER (design 2026-07-13).**
  A weapon with a bespoke impact/projectile effect must ALSO define its
  own `Report`/`ImpactSounds` — never leave either the effect or the
  sound to fall back to the class template's default. Falling back makes
  two different weapons look/sound identical and erodes each faction's
  identity; the goal is for every weapon (and especially every NEW impact
  animation) to be as unique as possible. When authoring a new effect,
  author (or assign a unique existing) sound in the same pass. Prefer new
  custom audio; cross-check Shattered Paradise for a fitting reference;
  only reuse a generic template sound as a last resort, and flag it.
- **Effect frame-fit check (design 2026-07-13).** An expanding effect
  drawn larger than its frame is clipped to a hard square in game
  (`cabal_dissolveimpact` v1 did this). Size radii/sigmas to fit, clamp
  each gaussian centre so `2.5*sigma` stays within a margin of the edge,
  and ASSERT the 2px border alpha is 0 on every frame. Render the preview
  with the frame border drawn (a red box) and Read it before committing.
- **CreateEffect `Image:` field — ALWAYS OMIT for the default explosion
  image (design 2026-07-15).** The `Warhead@Effect: CreateEffect` trait
  looks up its `Explosions:` sequence inside the image named by `Image:`.
  When `Image:` is omitted, the engine defaults to the `explosion` image
  defined in `sequences/misc.yaml`. All weapon impact animations
  (`magicnuke`, `magicnuke_med`, `magicnuke_small`, `magicnuke_micro`,
  `cabal_greenplasmaimpact`, `cabal_missileexplosion`,
  `cabal_laserimpact_s`, `cabal_laserimpact_m`, `cabal_laserimpact_l`,
  `cabal_dissolveimpact`, `poof`, `drplasmaex2`, …) are defined as
  sub-sequences under the `explosion:` key in `sequences/misc.yaml`.
  Setting `Image: explosion` explicitly is redundant but harmless; setting
  `Image: <custom>` for a sequence that lives under `explosion:` causes
  the engine to look for it inside the wrong image and **crashes with a
  missing-sequence exception**. The rule: **a weapon `CreateEffect` must
  never carry an `Image:` field** — leave it out and let the engine
  default to `explosion`. If a truly custom impact image is needed (one
  that does NOT live under `explosion:` in misc.yaml), define a new
  sub-sequence under `explosion:` instead and reference it by name in
  `Explosions:`.
- **Impact animations live in `misc.yaml` under `explosion:`, never in
  faction sequence files (design 2026-07-15).** All weapon impact/explosion
  animations — regardless of faction — are defined as sub-sequences under
  the `explosion:` image key in `sequences/misc.yaml`. This centralizes
  explosion rendering (full brightness, `IgnoreWorldTint: true`,
  `ZOffset: 2047` via the `Defaults:` block) and ensures the default
  `Image:` lookup works. Faction-specific sequence files (e.g.
  `CABAL/yaml/sequences.yaml`) must NOT define top-level image keys for
  impact animations. When a new impact animation is created, add it under
  `explosion:` in `misc.yaml` and reference it by name in the weapon's
  `CreateEffect` `Explosions:` field with no `Image:`.
- **Shared-image exception (design 2026-07-15).** When an image is used
  BOTH by a `CreateEffect` warhead AND by other traits (building
  `Effect:`, projectile `Image:`/`TrailImage:`,
  `SubterraneanTransitionImage:`, `HelixAnimSequence:`, `RingImage:`,
  etc.), the top-level image definition MUST stay in its sequence file
  for the non-CE traits. In this case the `CreateEffect` warhead KEEPS
  its `Image:` field — do NOT duplicate the sequence under `explosion:`.
  Only images used EXCLUSIVELY by `CreateEffect` warheads are moved
  under `explosion:` and have their `Image:` field removed. The audit
  tool `tools/audit_createeffect_image.py` flags all CE `Image:` fields;
  `tools/audit_ce_image_usage.py` determines which are CE-only vs shared.
  Known CE-only images already moved: `wc2_building_collapse`.
  Known shared images that keep `Image:`:
  `tsdig`, `tsioncannon`, `ionsfx`, `tspodring`, `tsmcnealmechdrop`,
  `tsdroppod`, `hakurei_giphy`, `hakurei_dream`, `wc2_effect_sparkle`,
  `wc2_effect_sparkle_circle`, `wc2_effect_heal`, `wc2_exorcism`,
  `wc2_catapult_impact`, `wc2_lightng`, `wc2_effect_blizzard`,
  `wc2_catapult_stone_projectile_medium`, `wc2_effect_death_and_decay`,
  `wc2_effect_daemon_attack`, `wc2_cannon_impact`, `wh40kcapsule`.
- **Corpse-spawner exception (design 2026-07-15).** `ra2corpse` is NOT
  an explosion — it is a corpse spawner that uses `CreateEffect` with
  multiple `Explosions:` entries (`death_a`–`death_f`) to pick a RANDOM
  corpse animation each time. The `Image:` field MUST be kept because
  the engine needs to resolve the random sequence from the `ra2corpse`
  image's own sub-sequences, not from the `explosion:` image. Moving
  these under `explosion:` would break the random-pick behaviour.
  `ra2corpse` stays as a top-level image in `sequences/redalert2.yaml`.

## 9. Operating rules for agents

1. **Read the ENTIRE DESIGN.md into your context before touching ANY yaml
   file.** Not a grep, not a search, not a summary — the full file, every
   session, every time. This is non-negotiable. Skipping this step causes
   violations of stat rules (Speed steps of 5, HP steps, TurnSpeed formulas,
   price quantization) that are all documented here. If you find yourself
   about to edit a yaml file and you have not read this document in full
   this session, STOP and read it first.
2. Read this document, then `docs/audit/SUMMARY.md` for current state.
3. **Changing ANY unit stat (HP, Speed, Damage, ReloadDelay, Range, Cost)
   requires a FULL REBALANCE of that unit using the balance formula (§12).**
   You cannot change one stat in isolation — the formula ties all stats
   together, so changing Speed changes the unit's power, which changes the
   correct price or requires adjusting other stats to hold the price. The
   rebalance MUST land in BOTH the spreadsheet
   (`docs/design/cameo_armor_system.xlsx`) AND the yaml in the same pass.
   Never change a stat without updating the spreadsheet and verifying the
   formula still holds. If the range is beautiful (6.000, 7.500), adjust
   HP/Damage instead of Range. If the new Range would violate promotion
   superiority, adjust HP or Price instead.
4. Run the relevant audit before and after your change
   (`tools/audit/run_all.sh`, or a single `audit_*.py`).
5. Classify bugs as B1–B12 (MASTER_REPORT §4); if the class detector
   missed your bug, improve the detector in the same change.
6. Small typo-class fixes (wrong condition name, weapon-as-voice, swapped
   tooltips): **fix immediately and report it** so design knows.
7. Display-name changes and balance-affecting decisions: propose options,
   let design choose first.
8. Renames/refactors are proven behavior-preserving with resolved-diff
   snapshots; balance changes are never mixed into them.
9. Clean commits, one concern each; commit when design says so.
10. Every new unit ships with: naming-compliant id + `_icon`, Fluent keys,
   ai.yaml wiring, roster-wide upgrade hooks, class template, sequences
   that resolve, and a changelog line (Definition of Done,
   MASTER_REPORT Appendix D).
11. **Always separate top-level elements with a single blank line** —
   every actor, weapon, template, and sequence block is followed by an
   empty line before the next one, so it is easy to see where one ends
   and the next begins. A comment block stays attached (no blank line)
   to the element it documents. Scripted edits must preserve the blank
   line (a common bug: a block-replace that drops the trailing blank).
12. **All icons carry the `_icon` suffix** (§1/§8), including upgrade
   research icons: `ordos_upgrade_hoverdrive_icon.png`, never
   `ordos_upgrade_hoverdrive.png`.

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

_Source of truth: **`docs/design/cameo_armor_system.xlsx`** (the repo
working copy; design's private master is synced into it; sheets:
Armor Types, Weapon Types, Infantry, Tanks, Vehicles, Aircraft,
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

O is linear in each stat, P pairwise — by design intent the CHASSIS
(HP·Speed) plus the TURRET (Range·DPS) — Q the full product. Pure
linear made one-weak-stat units undercosted, pure product made the
same units overcosted, so the average of all three was chosen. This is
the FIRST iteration: it does not yet price anti-air capability,
projectile speed, or area of effect — those must be added in a future
revision (how is an open design question). **Workflow: the price S is
set FIRST** (last column); Range is then solved from the identity
(column F formula), so tuning HP/Speed/Damage/Reload auto-rebalances
Range to hold the price. Range and DPS cells are never hand-edited.

**Sheet-and-yaml dual write (LAW, design 2026-07-11).** Every balance
change lands in BOTH the workbook and the yaml in the same pass —
never in only one of them. Prices are outputs, never inputs: to
re-price a unit (e.g. after a tier move), edit the tier multiplier M
in the unit's row FIRST, let O/P/Q recompute, compare every stat
column against the yaml, then make the yaml match the sheet. Scaling
an existing yaml cost by a relative factor (old × new_M/old_M) is
FORBIDDEN — it bakes in whatever error the old value had (this
produced the Cannon Attack Robot 400-vs-350 miss). If the Excel lock
file `~$cameo_armor_system.xlsx` exists the workbook is open on
design's machine: queue the sheet edit, do not write the file.
Standing long-term goal: a fresh, clean workbook whose row names are
the exact in-game tooltips and whose stat cells are exactly the values
the formula consumes.

**Column semantics** (confirmed by design 2026-07-11):
- `WeaponClass` H — from the weapons yaml warhead classes. Every weapon
  family exists as **Light / Medium / Heavy** warheads: Light = 0.75,
  Medium = 1.0, Heavy = 1.25. Combining warheads AVERAGES the classes:
  Light+Medium = 0.875, Medium+Heavy = 1.125. **Prefer balanced class
  combinations**: 3×Light, or 2×Light + 2×Medium, or 1×Light + 1×Medium +
  1×Heavy. Avoid lopsided mixes such as 1×Heavy + 2×Medium. The final
  weapon class is the arithmetic mean of the individual class values.
- `Special` K — **+0.25 per special ability** (C4, EMP, stealth, point
  defense laser, …). Example: Nod laser commando = C4 + Stealth + PDL =
  1.75. Some over-strong kits are simply set to 2. FUTURE: replace the
  flat +0.25 with per-ability values that represent each ability's real
  power.
- `TechTier` M — cost discount for late tech: **Tier 3 = 0.75, Tier 4/5
  = 0.5** (rewards the tech-center investment). The rule was introduced
  late, so many rows are missing it — correcting those gaps is standing
  work; always cross-reference sheet vs in-game stats and ASK on any
  mismatch.
- **Burst rule**: sheet Damage = single-burst damage × bursts; sheet
  ReloadDelay = weapon ReloadDelay + (bursts − 1) × BurstDelay (the
  weapon only reloads after the full burst).
- `UnitClass` L — per-section class factor (infantry sections 0.4–1,
  vehicles 0.25–1.25, defenses 0.225/0.325/0.35). ❓ table of sections.
- **Epic units (design 2026-07-11): UnitClass L = 0.3, TechTier M = 1.0,
  regardless of actual tech tier.** Historically epic rows carried
  L = 0.4 × M = 0.75 (same product 0.3); the new convention folds the
  whole epic effect into L so that M stays a pure function of the tech
  tier everywhere (no "0.75 at T5" rows that look like mistakes).
  Relabeling legacy epic rows (Core, GDI Rig, Future Tank, …) is
  price-neutral — only the two cells change. Epics are never
  re-discounted when tiers move.
- **Defenses use Speed = 100 always** — immobility is priced through
  their LOW UnitClass factors instead, keeping the formula uniform.

**The nice-number law (design 2026-07-11).** Every stat moves in fixed
steps so the house formulas stay integral:
- **Prices: 10-credit steps** (was 25) — never 387-style numbers. If the
  target cost is not already occupied by another unit of the same class
  (same UnitClass/template role), prefer coarser **100-credit steps**. Use
  10-credit granularity only when the 100-grid slot is already taken by a
  sibling unit. If many units of the same class converge on the same price,
  equally spread them across the available 100-grid slots first, then fall
  back to 10-grid only when necessary. Prices are outputs, never inputs.
- **Damage: 2000-steps.** The HealthPercentageDamage twin is always
  **1 per 2000** main damage (16000 -> Percentage 8); FriendlyFire twins
  are always **50% damage and 50% spread**; all class warheads carry the
  identical (even-spread) value.
- **HP: 2500-steps** for vehicles/aircraft/ships (self-heal HP/2500,
  repair HP/20); **1000-steps for infantry** (self-heal HP/1000);
  defenses may use either (their self-heal is a flat 10).
- **Speed: steps of 5**.
- **TurnSpeed (vehicles & fixed-weapon units):** units without a turret or
  with a forward-facing fixed weapon turn at **`TurnSpeed = 2 × Speed / 5`**;
  turreted units turn at **`TurnSpeed = Speed / 5`**. Infantry normally turns
  instantly, but CABAL cyborg infantry use the vehicle fixed-weapon rule
  because they carry forward-facing weapons.
- **TurnSpeed (aircraft):** helicopters and spaceships both use
  **`Speed / 5`**.
- ReloadDelay: any integer.
- **Beautiful ranges are kept**: if Range is exactly 6.000 or 7.500,
  adjust the other stats, not the range.
- **Preserve the unit's feel**: never double damage and reload together
  just to make the math easy.

**UnitClass L is bound to the defaults.yaml class template** — one value
per class, identical for every unit of that class:

| class (sheet section) | L |
|---|---|
| Scout Infantry 0.5 · Grenadier 0.4 · Mortar 0.6 · Anti-Tank/Anti-Air 0.5 | infantry |
| Heavy Infantry 0.8 · Melee 0.75 · Sniper 0.75 · Hero 1.0 | infantry |
| Main Battle Tank 1.0 · High Tech Tank 1.0 · Epic 0.4 | tanks |
| Scout Vehicle 0.333 · Advanced Scout 0.5 · Transport/Support 1.25 | vehicles |
| AA Support 1.0 · Fire Support 1.0 · Artillery 0.5 | vehicles |
| Helicopter 1.0 · Fighter 1.0 · Spaceship 1.0 | aircraft |
| Basic Defense 0.35 · AA Defense 0.225 · Advanced Defense 0.325 | defenses |

**Tier counting for the M discount**: the tier is set ONLY by TECH
building requirements — production buildings (barracks, war factory,
helipad, naval yard) and refineries NEVER count. No tech requirement =
Tier 1 (war-factory units without a radar requirement are still T1);
radar tech (or equivalent) = Tier 2; tech center = Tier 3; beyond =
Tier 4/5. Tech gates count TRANSITIVELY through the production chain:
the helipad/airfield itself requires radar, so **ALL aircraft are at
least Tier 2**. M: T1/T2 = 1.0, T3 = 0.75, T4/5 = 0.5. Auto-correcting the
missing discounts across the sheets is approved — under the nice-number
law above.

**Early-vs-late philosophy**: upgrades boost cheap early-game units
proportionally MORE than late units; late units are compensated through
the tech-tier discount instead. Tier 2 is the stopgap tier — units too
strong for T1 but not strong enough for T3.

**Tier placement themes** (initial; deep research ongoing): artillery
is ALWAYS at least Tier 2 (some Tier 3, e.g. the GDI Archer); fire
supports and line breakers usually Tier 2; flamethrower infantry
Tier 2 (exception: Japan); heavy infantry and snipers usually Tier 2.

**Repair/engineering units (design 2026-07-11):** the repair beam is
its own weapon class — **H = 1.5** — and engineering kits (capture +
repair + defuse) are the maxed special **K = 2**. Reference trio in the
workbook: IFV (Engineer), Engineering Armor, Engineering Truck (all
20000 @ 50, H 1.5, K 2, L 0.5); the CABAL repair engineer added its own
unique row (16000 @ eff. 40, HP 20000, Cost 800). H above 1.5 exists
once by design: the **TOPOL-M (H = 5) is a mobile superweapon**. The
"Consortium Artillery Tank" workbook row is the HAMMERHEAD (stale name)
— one of the sacred meme units.

**Promotion units carry a hidden yaml-side buff** the spreadsheet does
NOT model: every promotion-gated unit inherits `^PromotionUnitBuff`
(defaults.yaml) — +10% firepower, −10% damage taken, −10% reload, +5%
speed/range/vision/cloak detection, −10% inaccuracy. The sheet always
holds the unbuffed base stats; the buff is the promotions' flat bonus
on top, applied only through the yaml inner workings.

**Special ability catalog (K = 1 + 0.25 per special; overpowered kits
set straight to 2 until per-ability values exist):**
- COUNTS: cloak; auras (propaganda effect); vampire heal-on-attack
  (the Dissolver's `ChangesHealth` on `GrantConditionOnAttack`).
- DOES NOT COUNT: cloak detection (near-ubiquitous); deploy/transform;
  anti-air capability — AA belongs in a future formula term, not in K.
- **Charge-delay drawback −0.25 — DEFENSES ONLY** (design 2026-07-11):
  a defense that must charge before firing (others shoot instantly) may
  carry K below 1 (TD Obelisk of Light K=0.75, CABAL Obelisk Prime
  K=0.75). Mobile units with charge delays do NOT get the discount.
  The discount applies only to LONG charge-ups (Obelisk-class); brief
  charges (Tesla Coil, Prism, Waveforce) are not significant enough.
  The Tesla Coil's K=1.25 is its inherent EMP effect counting as the
  special (+0.25), undiscounted by its short charge.

**The baseline unit (design 2026-07-11): the Naxis Tiger Tank** —
100 000 HP, 100 Speed, 10 000 damage, range 5.0 (= 5000 wdist,
written literally as `Range: 5000` in the weapons yaml — Cameo uses
plain wdist integers, never OpenRA's `4c904` c-notation; the sheet's
Range unit is wdist/1000, NOT cells since a cell is 1024),
50 reload, all modifiers 1 → DPS 200 and **O = P = Q = Cost = 800
exactly**. Every stat trade in the system is anchored on these round
numbers.

**Known limitation — the low end breaks (second iteration planned).**
The formula has no intercept: cost → 0 forces every stat toward 0
simultaneously, so very cheap units (Minigunner at 100, Naxi Rifle
Recruit at 50) come out unusably weak — a unit's fixed "cost of
existing" (pathing, pop slot, minimum viable rifle) is not priced.
Stopgap in game: strong damage-reduction multipliers on the scout
infantry template. Design direction instead: **one baseline unit per
unit class** (scout/basic/heavy/hero infantry, each with Tiger-style
round numbers), and price by normalized deviation from the class
anchor: `Cost = Cost₀ × (O/O₀ + P/P₀ + Q/Q₀) / 3`. At each anchor the
identity is exact (like the Tiger's 800); below it, stats degrade far
more gently than the global formula, which fixes the low end. The
UnitClass column is then absorbed into the class baselines.

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
15–40. The scaling tables are the DESIGN REFERENCE that generated the
in-yaml Versus tables; in the game they are realized once inside the
weapon class templates and never re-derived per weapon.

**Weapon construction law (design 2026-07-11).** The Versus tables live
ONLY in the ~30 class templates of the central `weapons/weapons.yaml`
(`^SmallArms ^Chaingun ^FlakWeapon ^MediumCannon ^HeavyMissile
^LaserWeapon ^LightChemicalWeapon …`) and are **never modified without
an explicit design order**. A new weapon:

```
MyWeapon:
	Inherits: ^MediumCannon          # contributes its class warhead (versus)
	Inherits@2: ^HeavyCannon         # LAST inherit WINS for the shared fields:
	                                 # projectile, sounds, effects, defaults
	                                 # all come from ^HeavyCannon here; the
	                                 # warheads of BOTH accumulate
	ReloadDelay: 50                  # own overrides beat every template
	Range: 5000
	Warhead@MediumCannon: SpreadDamage
		Damage: 8000
	Warhead@HeavyCannon: SpreadDamage
		Damage: 8000                  # EVEN SPREAD — always identical values
```

Order the inherits so the template whose projectile/sound/feel you want
comes LAST; the earlier inherits only contribute their warheads.

- **Mixed class warheads always carry the SAME Damage** (even spread;
  1,023 weapons comply, 49 violations flagged — mostly the imported
  chem-upgrade weapons like TSChemBazooka 6000/24000; fix on order).
- Template auxiliaries (`LaserExtraDamage` 600, `RailgunExtraDamage`,
  `ShrapnelWeapon`, Tesla charged twins) ride along at fixed values and
  are NOT part of the even-spread accounting or the sheet Damage.
- ❓ FriendlyFire twins: some templates default them EQUAL to the main
  damage (^MediumChemicalWeapon 1000/1000), some HALF (^LightFlameWeapon
  2000/1000); the override convention needs a design ruling.
- **MEME UNITS ARE SACRED (design 2026-07-11).** `NanoArtilleryAG` /
  `NanoSmokeAG` (everything 7s and 3s) and `HammerheadArtillery`
  (everything 1s) are deliberate joke stat lines and are NEVER touched
  by any formula, rule sweep or rebalance — no matter how the balance
  formula changes. Audits list them as exempt, never as findings.
- **Multi-weapon units**: the sheet Damage is the SUM over the baseline
  loadout — every `primary` armament not gated behind an upgrade (GDI
  Battle Tank: cannon 8000 + missiles 8000 = sheet 16000).

**Plasma weapons (design 2026-07-13).** A CABAL "plasma" weapon is a
signature triad: a base weapon class plus a **Fire** warhead and a
**Chemical** warhead. The base class determines the projectile look and
report; the flame and chemical warheads add the plasma burn/corrosion
signature. Examples:
- **Plasma cannon** = Cannon + Fire + Chemical.
- **Plasma rocket** = Missile + Fire + Chemical.
- **Plasma laser** = Laser + Fire + Chemical.
All class warheads follow the even-spread law (same damage) and carry
matching percentage twins. The impact effect and sound are authored or
assigned together and kept unique to the weapon.

**Definition of Done for a formula unit:** stats from the sheet map to
yaml as HP→`Health.HP`, Speed→`Mobile.Speed`, Range (wdist/1000)→weapon
`Range` (×1000, written as a plain integer like `5000`), Damage→class
warheads per the even-spread law, ReloadDelay→weapon `ReloadDelay`
(ticks, burst rule applied); every new unit gets its own unique weapon
(§10) inheriting the sealed class templates. **On any sheet↔game
mismatch the balance sheet wins**; audit_balance_sheet.py is the
detector and fixes land as ordered batches, never silently.

## 13. Map props (Obstacle target type)

- Trees, rocks, utility poles and other decorations carry
  `TargetTypes: Ground, Obstacle` (templates `^Tree ^TreeHusk ^Rock ^Box`).
- `Obstacle` exists for AI logic (minelayer bot ignores it), **never for
  weapons**: no weapon lists Obstacle in Valid/InvalidTargets — props are
  hit as plain Ground.

## 14. Map actor naming (compatibility with renamed actors)

- **Maps must use the current renamed actor ids, not the original
  compressed names** (design 2026-07-15). When a map is added or updated,
  every `ActorNN: <type>` line in `map.yaml` and every actor-type string
  in lua scripts must use the new §1-compliant actor id (e.g.
  `td_nod_minigunner`, not `e1.nod`). The old compressed names no longer
  exist as actor definitions and will crash the game on map load.
- **Rename maps are the source of truth.** The mapping from old → new
  actor ids lives in `tools/rename/rename_map_<faction>.yaml` files.
  When renaming actors in a map, look up each old name in these files to
  find the new id. Actors that already exist with their old name (terrain
  decorations like `t01`, `v01`, `boxes01`, `brik`, `fenc`, `gmine`,
  `tanktrap1`, `split2`, `silo`, `nuk2`, `nuke`, `sbag`, `fcom`, etc.)
  are NOT renamed and stay as-is.
- **Lua scripts must also be updated.** Actor-type strings in lua
  (reinforcement lists, `Actor.Create` calls, `GetActorsByType` checks,
  `Reinforcements.ReinforceWithTransport` unit lists) must use the new
  ids. A map's `campaign.lua` may reference actor types in utility
  functions (e.g. `GetAirstrikeTarget` checking for `"sam"`) — these must
  also be renamed.
- **Tooling.** `tools/rename_map_actors.py` applies the rename maps to
  map.yaml and lua files in bulk. It matches `ActorNN: <type>` lines in
  yaml and quoted `"oldname"` strings in lua, replacing them with the
  new ids from the rename maps.

---

## 15. CABAL faction design rules

The full CABAL faction design lives in the local document
`C:\Users\AedisToru\Documents\DevinCameoProject\CABAL_FACTION_DESIGN.md`.
This section binds the rules that affect YAML-level auditing.

**Faction identity.** CABAL is a self-contained cybernetic collective: every
unit is a machine, cyborg, or drone. No cross-faction inheritance. The
faction owns two ground production queues — the Cyborg Factory builds
infantry and light walkers, the Mech Factory builds heavier vehicles.

**No dead tiers.** Infantry, vehicles, and aircraft must each have a
meaningful, non-limited, buildable unit at every tier in the regular tech
tree. Promotions are *better* versions of those base units, not the only
option at a tier. A promotion tier maps directly to a tech tier (rank 1–4
= Tier 1–4) and chains from the previous promotion in its column.

**3×4 promotion grid.** CABAL uses three promotion columns: Infantry,
Vehicles, Aircraft. Each column has four tiers. The aircraft column is
filled by adding a Tier-1 flying drone (`cabal_wasp`) produced from the
Cyborg Factory and a Tier-4 command ship (`cabal_mothership`), because the
Helipad is gated behind Radar and cannot provide a T1 aircraft by itself.
The **CABAL Core** is the Tier 4 technology unlock; all T4 units (base
and promotion) and the Core Defender require it.

**Class-template mapping.** Every CABAL actor must map to a single
`defaults.yaml` class template. Known mismatches to fix are bugs, not
style: `cabal_dissolver` must be `^HeavyInfantryTemplate`, `cabal_artilleryspider`
must be `^ArtilleryTemplate`, `cabal_cyborgreaper` and `cabal_manticore` must be
`^SupportVehicleTemplate`, `cabal_coredefender` must be `^EpicVehicleTemplate`.

**Cyborg dual-armor rule.** All CABAL cyborg infantry use the Future Tech
droid recipe: the base `Armor` from the infantry class template stays active,
a secondary vehicle `Armor@<role>` is added, and a `DamageMultiplier@<role>:
Modifier: 200` is applied to the secondary armor. This makes cyborgs count as
both infantry and vehicles for weapon Versus tables while keeping them brittle
against dedicated anti-vehicle fire. True vehicles and walkers do not use this
pattern.

**Cybernetic Plating shield rule.** CABAL cyborg infantry do NOT have an
innate `Shielded` trait. Instead, the `cabal_upgrade_cyberneticplating`
research upgrade (Tier 3, Tech Center) grants every cyborg infantry unit a
yellow shield bar (`SelectionBarColor: FFFF88FF`). The shield is gated by the
upgrade condition, uses `ShieldsUpCondition: armored`, and recharges slowly
with a low flat `RegenAmount` equal to twice the unit's self-heal step. The
base `Armor` type never changes: the upgrade adds plating as a shield pool,
not as an armor-type swap. This mirrors the Tiberian Dawn Nod cybernetics
upgrade behavior and must not be reverted to unconditional shields.

**Upgrade tiers.** Tier 2 (Radar, `Upgrades` queue): Overcharged Servos,
Dark Armament, Radar Hack. Tier 3 (Tech Center, `Research`):
Mobility Matrix, Neutron Nuclear Catalyst, Cybernetic Plating, Reinforced Chassis, Neural Uplink,
Reclamation Protocols, Networked Combat Protocols. Tier 4 (Tech Center + Core,
`Research`): Backup Systems, Hand of CABAL, Data Worm, Firewall Protocol, and
Full Assimilation as the team upgrade. The strongest and team-wide upgrades
sit at Tier 4.

**Weapon identity.** CABAL plasma weapons are the signature triad from
§12: Cannon/Missile/Laser + Fire + Chemical warheads with even spread.
CABAL lasers are purple/dark-blue outer beams with a near-white cyan core,
scaled by damage. Weapon sounds follow the obelisk/laser sound map in §3.

**Promotion visibility rule.** Promotions must be globally visible in the
Promotions tab as soon as the player has the faction's construction yard
and `rank1`. A promotion's `Buildable.Prerequisites` must always start with
`~constructionyard` so the promotion grid is only available while the
player still has a base; it also contains `rank1`, the prerequisite that
unlocks the promotion (`!self`), and the previous promotion in its column.
Production buildings and tech structures (other than the construction
yard) must NEVER gate whether a promotion appears in the Promotions tab —
those buildings belong on the *unit* that the promotion unlocks.

**Promotion-unit prerequisite formula.** A unit unlocked by a promotion uses
`Buildable.Prerequisites: ~productionbuilding, techbuilding, ~promotion`.
`~productionbuilding` hides the unit when the producer is missing; the
`~promotion` token hides the unit entirely when the promotion has not been
bought; tech buildings are positive prerequisites that disable the unit but
still show it in the build list. Replaced base units additionally carry
`!promotion` so they are disabled once the promotion is bought. Every
promotion-gated unit must include its `~promotion` token, otherwise it will
appear before the promotion is unlocked.

**Weapon minimum-range rule.** Any weapon with a `MinRange` must keep
`MinRange = round(Range / 5)` rounded to the nearest multiple of 5
(`expected = round(Range / 25.0) * 5`). This applies to artillery,
aircraft, and any other actor that uses a minimum firing distance.
Violations are caught by `tools/audit/audit_min_range.py`.

**Unique stat rule.** Every CABAL actor must carry at least one stat or
ability that no other actor shares (cost, speed, range, weapon class,
special K, or role). Two units may not feel identical.

**Armament naming and count.** Every unit uses only three canonical
armament slots: `Armament` (or `Armament@PRIMARY`), `Armament@SECONDARY`,
and `Armament@GARRISONED` for garrison logic. Never create `Armament@AA`,
`Armament@AntiAir`, or role-specific names; anti-air capability is handled
by `ValidTargets` on the same primary/secondary weapon, not by a separate
armament node. A unit that has a primary weapon should not receive a
separate garrison-only pistol; the garrison armament reuses the same weapon
as the primary.

**No weapon inheritance between units (reinforced 2026-07-15).** A
weapon definition must never `Inherits:` from another unit-unique weapon
(e.g. `CabalAvatarLaser` inheriting `CabalCoreDefenderLaser`,
`CabalReaperMissilesAA` inheriting `CabalReaperMissiles`,
`CabalHeavyReaperMissilesAA` inheriting `CabalHeavyReaperMissiles`,
`CabalHeavyReaperTrap` inheriting `CabalReaperTrap`,
`CabalRocketCyborgRocketsUpgraded` inheriting `CabalRocketCyborgRockets`,
`CabalWaspLaserStriker` inheriting `CabalWaspLaser`,
`CabalWidowPlasma` inheriting `CabalRavagerPlasma`,
`CabalCommandoPlasmaNeutron` inheriting `CabalCommandoPlasma`,
`CabalCommandoPlasmaMk2Neutron` inheriting `CabalCommandoPlasmaMk2`,
`CabalRavagerPlasmaNeutron` inheriting `CabalRavagerPlasma`). If two
units need similar firepower, create a shared `^`-prefixed base weapon
template in the faction weapons file and inherit from that template, or
copy the stats explicitly. Unit-unique weapons are not stable extension
points. **This was the root cause of the CreateEffect crash class**:
inheriting a weapon also inherits its `CreateEffect` warhead, and if the
parent's explosion sequence was moved or changed, the child silently
breaks. Copy stats, do not inherit between units.

**Effect inheritance order.** Visual effect templates such as
`^TSLaserEffect`, `^TSSparkEffect`, or faction laser-trail templates
must be inherited **last** in the `Inherits@` chain, after all weapon and
armor templates. Otherwise their trait definitions are overwritten by
later inherits and the intended effect is lost.

**Reusable engineer rule.** Engineers that can capture structures must not
be consumed on capture. Use an engineer armor template that grants the
`CaptureManager` / `Captures` behavior without `Consumed:` true, matching
the Schwarzer Mond "Engineering armor" pattern. CABAL's `cabal_engineer`
uses this reusable pattern.

**Promotion unit stat superiority.** A promotion-unlocked unit must be
strictly better than the base unit it replaces in every meaningful stat.
Allowed exceptions: (a) Speed may be lower only if HP increases
significantly to convey a heavier chassis; (b) ReloadDelay may be longer
only if total damage per salvo increases significantly. **Range must never
be lower** on a promotion unit. Promotion weapons must use stronger
warhead classes than the base unit (e.g. base medium → promotion heavy).
Audit this with `tools/audit/audit_promotion_superiority.py`.

**Linebreaker / mutual-weapon design (CABAL Manticore).** A unit whose
primary and anti-air weapons are mutually exclusive (e.g. a turret that
switches modes or a unit that fires only one weapon at a time) does NOT
add their DPS for balance purposes. Both weapons must be tuned to the
same final values (damage, reload, range) so the spreadsheet sees two
equivalent options, not a doubled output. For the Manticore this means a
ground laser and an air missile with identical range and comparable
throughput, using laser+railgun warheads on the ground weapon and heavy
missile+heavy AA warheads on the air weapon.

**Reaper net weapon (CABAL).** The Cyborg Reaper's net weapon copies the
Zerg Corruptor snare effect: a missile projectile that applies a slow/
snare condition to all valid snareable targets. The net shares the ground
weapon's range; the dedicated anti-air weapon receives the standard +50%
range increase over the ground weapon.

**Hunter Killer weapon identity.** Hunter Killer aircraft are powerful
attackers, not light infantry. Their weapon must use laser / missile
warheads appropriate to their role, never `^SmallArms`.

**Balance workflow.** All CABAL rebalances start in
`docs/design/cameo_armor_system.xlsx` (or the CABAL concept sheet) and
land in YAML in the same pass. The workbook wins on mismatch. Promotions
add `^PromotionUnitBuff` on top of the sheet stats.

**CABAL Avatar — 50% scaled Core Defender (design 2026-07-15).** The
`cabal_avatar` is a mass-produced variant of the Core Defender, NOT a
spider. It is the Core Defender at 50% scale: all visual offsets, weapon
damage, HP, and stats are halved from the `cabal_coredefender` reference.
The avatar uses its own weapon (`CabalAvatarLaser`) which is a copy of
`CabalCoreDefenderLaser` with damage and offsets scaled to 50%, NOT an
inherits-from relationship (§15 no weapon inheritance between units).
The avatar's sequence scale, muzzle offsets, and visual effects are all
50% of the Core Defender's values. This makes the avatar a smaller,
faster-to-produce walker that retains the Core Defender's identity.

**CABAL husk recovery (design 2026-07-15).** CABAL vehicles with the
Backup Systems upgrade leave a `_backup` husk on death. The husk is
immobile (Speed/TurnSpeed = 0), has 2–5× the base unit's HP, is
Repairable, and auto-reanimates after a delay via
`GrantPeriodicCondition@rebuild` + `TransformOnCondition@buildingrebirth`
back into the original unit. The recovery time is currently a fixed
delay (not scalable with remaining/max health in YAML-only — a C# trait
would be needed for health-scaled recovery). The husk carries
`WithColoredOverlay@backup` for a visual indicator. Each CABAL vehicle
needs three things for backup systems:
1. `Inherits@BACKUP: ^cabal_upgrade_backupsystems` (in vehicles.yaml)
   — grants the condition.
2. `SpawnActorOnDeath@backup` trait with
   `RequiresCondition: cabal_upgrade_backupsystems` and
   `Actor: <unit>_backup`.
3. A `<unit>_backup` actor definition in the faction pack's
   `yaml/husks.yaml` (moved from rules/tiberiansun.yaml 2026-07-17) that
   inherits the base unit, sets Speed/TurnSpeed=0, high HP, Repairable,
   removes `SpawnActorOnDeath@backup`, adds
   `GrantPeriodicCondition@rebuild` + `TransformOnCondition@buildingrebirth`
   for auto-reanimation, and `WithColoredOverlay@backup` for the visual.

## 16. Rank decorations, experience systems & elite weapons

Cameo uses **two distinct experience systems** with different rank counts,
stat curves, and decoration images. Every faction must use exactly one
system consistently across all its actors.

### 16.1 The two experience systems

**TD/TS system** (`^GainsExperienceTD` in
`ContentPacks/TiberianDawn/Shared/yaml/templates.yaml`):
- 4 veteran ranks + elite (5 pips total).
- XP thresholds: 200, 400, 600, 800. Elite at `rank-veteran >= 4`.
- `^GainsExperienceTD` does NOT include `WithDecoration` — rank icons
  come from a separate `^*RankDecoration` template that the actor must
  also inherit via `Inherits@decoration`.
- No elite weapons by design — stat multipliers scale progression.
- Building variant: `^GainsExperienceTDBuilding` (250/500/750/1000 XP).

**RA2 system** (`^GainsExperienceRA2` in `rules/redalert2.yaml`):
- 5 veteran ranks + elite (6 pips total).
- XP thresholds: 100, 250, 450, 700, 1000. Elite at `rank-veteran >= 5`.
- `^GainsExperienceRA2` includes `WithDecoration` with `Image: ra2rank`
  and a `rank-veteran-4` sequence — rank icons are built-in, no separate
  decoration template needed.
- **Elite weapons**: every RA2-styled actor with a primary armament must
  also have an `Armament@ELITE` block gated on `RequiresCondition:
  rank-elite` (see §16.3).
- Stronger multipliers than TD/TS: damage reduction to 50%, firepower to
  200%, speed to 125%. Reload/Range/Detect multipliers stay at 100 (RA2
  favors raw damage/speed over utility scaling).
- Building variant: `^GainsExperienceBuildings` (inherits
  `^GainsExperience`, adds firepower multipliers).

The default `^GainsExperience` in `defaults.yaml` is a third variant used
by RA1 and other non-TD/TS/RA2 factions. It has 4 veteran ranks (like
TD/TS) but includes `WithDecoration` with `Image: rank` (the generic
rank sprite) and crate-powerup decorations. Factions using this system
do not need a separate `^*RankDecoration` inherit.

### 16.2 Rank decoration images

Every faction should eventually have its own rank decoration image and
`^*RankDecoration` template. The sequence images are defined in
`sequences/misc.yaml` under their respective keys.

| Image key | Template | Used by |
|---|---|---|
| `rank` | (built into `^GainsExperience`) | RA1, default factions |
| `gdirank` | `^GDIRankDecoration` | TD GDI, TS GDI (shared) |
| `nodrank` | `^NodRankDecoration` | TD Nod, TS Nod (shared) |
| `cabalrank` | `^CABALRankDecoration` | TS CABAL |
| `forgotrank` | `^ForgottenRankDecoration` | TS Forgotten |
| `dunerank` | `^DuneRankDecoration` | D2k factions (Atreides, Harkonnen, Ixian, Ordos) |
| `alienrank` | `^AlienRankDecoration` | StarCraft Zerg (TODO — split per-faction: Zerg keep `alienrank`, Protoss and Terran need separate decorations) |
| `ra2rank` | (built into `^GainsExperienceRA2`) | RA2, RA2Mod factions, TKM |

**Rules:**
- TD and TS share rank images per side: both TD GDI and TS GDI use
  `gdirank`, both TD Nod and TS Nod use `nodrank`.
- RA2 factions all share `ra2rank` (baked into `^GainsExperienceRA2`).
  RA2Mod factions (AsianAlliance, Consortium, FutureTech, Naxis,
  SchwarzerMond, Syndicate) also use `ra2rank` via `^GainsExperienceRA2`.
- `^*RankDecoration` templates use `Palette: greyscale` (not `effect`)
  for TD/TS/CABAL/Forgotten/Dune factions. The RA2 system uses
  `Palette: effect`.
- Actors using `^GainsExperienceTD` MUST also inherit the appropriate
  `^*RankDecoration` via `Inherits@decoration` — otherwise they show no
  rank pips at all.
- Actors using `^GainsExperienceRA2` or `^GainsExperience` do NOT need
  a separate `^*RankDecoration` — their rank icons are built-in.

**Audit rule** (`audit_rank_decoration.py`, TODO): every actor with
`Inherits@EXPERIENCE: ^GainsExperienceTD` must also have
`Inherits@decoration: ^*RankDecoration` matching its faction. Actors
with `^GainsExperienceRA2` must NOT have a separate `^*RankDecoration`
(it would conflict with the built-in decorations).

### 16.3 Elite weapons (RA2 system only)

**Every RA2-styled actor with a primary armament must have an elite
weapon.** When a unit reaches elite rank, its primary weapon is
replaced by an upgraded version via `Armament@ELITE`.

**Pattern:**
```yaml
Armament@PRIMARY:
    Weapon: <baseWeapon>
    RequiresCondition: !rank-elite
Armament@ELITE:
    Weapon: <baseWeapon>_elite
    RequiresCondition: rank-elite
```

**Naming convention:** ALL elite weapons append `_elite` — this is the
only accepted suffix, regardless of the weapon's naming style.
- **Legacy PascalCase weapons** (not yet migrated to actor-prefixed
  names): the elite weapon name is the base weapon name with `_elite`
  appended (e.g. `BorisAKM` → `BorisAKM_elite`). The legacy `E` suffix
  (e.g. `BorisAKME`) is deprecated and must be migrated to `_elite` when
  the weapon is touched. Non-standard names (e.g. `AsianRailTank2`,
  `NaxPlanegun`) are bugs and must be renamed to follow the `_elite`
  suffix convention.
- **New actor-prefixed weapons** (lowercase, per §1 weapon naming rule):
  the elite weapon name appends `_elite` (e.g.
  `ra2_soviets_conscript_ak47` → `ra2_soviets_conscript_ak47_elite`).
  This is consistent with the lowercase underscore convention.

**Critical distinction — EMP weapons are NOT elite weapons:**
Weapons whose primary function is EMP disable (e.g. `SteelEmpBomb`,
`TSEMPZapWeapon`, `CorsairEMP`) must NOT be confused with elite
variants. EMP weapons use the `_EMP` suffix (see §1), not `_elite`.
The previous bulk rename (reverted) incorrectly treated EMP weapons as
elite weapons — this must never happen again. The audit script
(`audit_elite_naming.py`) only checks weapons gated by
`RequiresCondition: rank-elite`, so EMP weapons are never flagged.

**Audit rules** (`audit_elite_weapons.py`, TODO):
1. **E1 — Missing elite weapon**: every actor using
   `^GainsExperienceRA2` with at least one `Armament@PRIMARY` (or
   `Armament@PRIMARY`-equivalent without `RequiresCondition: rank-elite`)
   must also have an `Armament@ELITE` block. 217 actors currently fail
   this check.
2. **E2 — Missing rank-elite condition**: every `Armament@ELITE` block
   must have `RequiresCondition: rank-elite` (or a condition that
   includes `rank-elite`). 77 of 143 elite armaments currently fail
   this check — they fire at all ranks, not just elite.
3. **E3 — Non-standard naming**: elite weapon names must end with
   `_elite`. The legacy `E` suffix is deprecated. Weapons ending with
   `E` that are NOT elite variants (e.g. EMP weapons, `PrismChargeE`)
   must not be flagged — only weapons gated by `rank-elite` are checked.
4. **E4 — Base weapon must not fire at elite**: the primary armament
   must have `RequiresCondition: !rank-elite` (or equivalent) so the
   elite weapon replaces it, not stacks with it.

## §17 — Dune 2000 to OpenRA Sprite Conversion

When converting individual Dune 2000 BMP frames to OpenRA PNG spritesheets,
use the `tools/d2k_to_openra.py` script. This is the standard pipeline for
all D2K asset conversions (e.g. Koda Tank).

### Conversion Steps

1. **Source frames**: Individual BMP files extracted from Dune 2000 `.R16`
   archives, numbered `Prefix_0.bmp` through `Prefix_N.bmp` (e.g.
   `KodaBody_0.bmp` ... `KodaBody_31.bmp`).

2. **Run the script**:
   ```
   python tools/d2k_to_openra.py <input_dir> <output.png> [--prefix Prefix] [--hue HUE] [--no-hue-shift]
   ```
   - `--prefix`: filter frames by filename prefix (e.g. `KodaBody`)
   - `--hue`: target hue for player-color remap (default 300 = magenta)
   - `--no-hue-shift`: skip hue shifting (for chassis frames with no player color)
   - `--remap-hue-min` / `--remap-hue-max`: customize the source hue range
     (default 140–190°, covering the D2K green ramp)

3. **What the script does**:
   - Combines all BMP frames into a single horizontal PNG strip
   - Normalizes frame sizes to the largest frame (smaller frames centered)
   - Converts pink background (RGB 255,0,255) to transparent alpha
   - Hue-shifts the green player-color ramp (~163°) to the target hue
   - Embeds PNG metadata (`FrameAmount`, `FrameSize`) so OpenRA can split
     the strip into individual frames at load time

4. **Output placement**: PNG spritesheets go in `mods/cameo/bits/d2k/`
   (not in ContentPack `files/` directories — the engine's file system
   resolves sequence assets from the global `bits/` folder).

5. **Sequence wiring**: Use `Facings: -32` (for 32-frame D2K sprites) with
   `Filename: <name>.png` and `Start: 0`. No `Remap` or `Scale` fields
   needed — the PNG already has transparency and player color baked in.
   Muzzle flashes can still reference `DATA.R16` with `Remap: 54F94B`.

### PNG Metadata Format

OpenRA recognizes PNG spritesheets by two text chunks:
- `FrameAmount`: number of frames in the strip (e.g. `32`)
- `FrameSize`: frame dimensions in pixels as `W,H` (e.g. `39,30`)

The strip width must equal `FrameAmount * FrameSize_W`.
The strip height must equal `FrameSize_H`.

### Hue Shifting Rules

- **Chassis/body frames**: Use `--no-hue-shift` (no player color in body)
- **Turret frames**: Use `--hue 300` (shift green ramp to magenta for Ixian)
- Other factions may use different target hues (e.g. Atreides blue, Harkonnen red)
- The script preserves saturation and value; only hue changes
- Pixels outside the remap hue range are left untouched

## §18 — Schwarzer Mond Faction Design

### 18.1 Faction identity

Schwarzer Mond is the **space-faring, occult-science branch** of the Naxis.
Where the base Naxis faction is a clanking WWII pastiche, Schwarzer Mond is
its lunarpunk extension: gravity manipulation, crystal leech fields, yellow
lasers, green plasma shells, and flying saucers. Its intended power curve is
**early-game fragile, mid-game timing attack, late-game tank/artillery/space
superiority**, with a known weakness to aircraft and early rushes.

### 18.2 Roster (current)

**Infantry**
- `schwarzer_mond_lunarsoldier` — basic scout rifle (T1, laser, burst 1)
- `schwarzer_mond_lunarrocket` — rocket trooper (T1, anti-ground/anti-air)
- `schwarzer_mond_bermensch` — heavy infantry (T3, laser, burst 2)
- `schwarzer_mond_parzival` — hero black-hole caster (T3, buildlimit 1)
- `schwarzer_mond_noidmgarmor` — walker with MP40 laser (T2, burst 5)
- `schwarzer_mond_noidharvester` — harvester walker (T1, laser)
- `schwarzer_mond_engineeringarmor` — engineer/capture walker (T1)

**Vehicles**
- `schwarzer_mond_laserbeetle` — light laser support tank (T1, burst 2)
- `schwarzer_mond_lunarpanzer` — hover MBT (T1, cannon)
- `schwarzer_mond_lunartiger` — heavy hover MBT (T2, cannon)
- `schwarzer_mond_neojagdpanzer` — line-breaker TD (T3, cannon)
- `schwarzer_mond_lunargrille` — hover artillery (T2, cannon)
- `schwarzer_mond_korruptesbiest` — fire-support walker (T3, corrosion)
- `schwarzer_mond_crystaltank` — fire-support leech tank (T3)
- `schwarzer_mond_mars` — missile artillery (T2)
- `schwarzer_mond_m200bjagerline` — AA/artillery hybrid (T2)
- `schwarzer_mond_lasertank` — medium laser tank (T1, burst 4)
- `schwarzer_mond_gravitycoretank` — gravity debuff tank (T3)

**Aircraft**
- `schwarzer_mond_spacezeppelin` — heavy transport/gunship (T2, laser)
- `schwarzer_mond_blackbomb` — kamikaze plane (T3)
- `schwarzer_mond_haunebuii` — spaceship (T2, cannon/flak)
- `schwarzer_mond_haunebuiii` — heavy spaceship (T3, cannon/cow drop)
- `schwarzer_mond_corruptorpiercer` — rocket fighter (T3)
- `schwarzer_mond_dieglocke` — epic superweapon saucer (T3, buildlimit 1)

**Defenses**
- `schwarzer_mond_lasertower` — anti-infantry laser tower (T1, burst 1)
- `schwarzer_mond_sturmcannon` — artillery cannon defense (T2)
- `schwarzer_mond_gravitycore` — gravity superweapon building (T3)
- `schwarzer_mond_meteortractionray` — meteor superweapon (T4)

**Economy**
- `schwarzer_mond_moondairyfarm` — passive income building (T3)

### 18.3 Current upgrade audit

Two upgrades exist today:
- `schwarzer_mond_upgrade_crystallens` — **radar-tier** (wrong: should stay radar,
  but it currently doubles laser burst)
- `schwarzer_mond_upgrade_greenplasmashells` — **radar-tier** (wrong: should be
  **tech-tier** because it is a +25% firepower cannon upgrade for the whole tank
  line)

**Coverage gaps** (every unit must eventually be affected by at least two
upgrades, see §18.6):
- Crystal Lens only affects laser units; many non-laser units receive nothing.
- Green Plasma Shells only affects cannon units; the rest of the roster receives
  nothing.
- No team-wide economy, survivability, or utility upgrade exists yet.

### 18.4 Laser upgrade split (the +1-burst rule)

The current Crystal Lens upgrade **doubles** the burst of every laser weapon.
This is a ~2× damage spike and breaks the power-budget rule (§6: worst-case
stack ≤ 2× fresh-self). The upgrade must be split into two **+1-burst** steps:

- **Tier 1 — Crystal Lens** (`schwarzer_mond_upgrade_crystallens`, radar tier):
  +1 burst for **all** yellow laser weapons.
- **Tier 2 — Amplified Lens** (`schwarzer_mond_upgrade_amplifiedlens`, tech tier,
  requires Crystal Lens): another +1 burst for all yellow laser weapons.

Weapons with base burst 1 (`NaxiRifleLaser` on the Lunar Soldier, `NaxLaserT`
on the Laser Tower) are intentionally weak and therefore **do** benefit from the
+1-burst steps. The progression for a 1-burst weapon is 1 → 2 → 3, which is still
bounded by the +2 total cap and keeps the weakest lasers relevant.

Resulting burst progression:
| weapon | base | +Crystal Lens | +Amplified Lens |
|---|---|---|---|
| NaxiRifleLaser / NaxiRifleLaserE | 1 | 2 | 3 |
| NaxLaserT | 1 | 2 | 3 |
| NaxiBeetleLaser / AA | 2 | 3 | 4 |
| ÜbermenschLaser | 2 | 3 | 4 |
| NaxiTank2Laser / AA | 4 | 5 | 6 |
| NaxiMP40Laser | 5 | 6 | 7 |
| NaxiMP40LaserE | 10 | 11 | 12 |
| ÜbermenschLaserE | 4 | 5 | 6 |

The two upgrades **must not stack to double** any weapon. The total effect is
+2 bursts instead of ×2, which is much closer to the power-budget target.

### 18.5 Cannon upgrade tier move

`schwarzer_mond_upgrade_vrilpoweredweapons` (formerly *Green Plasma Shells*) must
move from `~schwarzer_mond_radar` to `~schwarzer_mond_techcenter`. The Vril energy
core gives cannon vehicles +25% firepower and converts their damage to a
Tesla-type discharge. This is a tech-tier effect (compare Naxis
`naxis_upgrade_wunderwaffe` at tech tier). The `Queue` stays `Research`.

### 18.6 Proposed upgrade framework

Every Schwarzer Mond unit must inherit at least two upgrade templates. The
following templates are the canonical set:

- `^NaxiCryptofascism` — economy trickler (1 credit per 25 ticks per unit).
  Tech tier, `Research` queue. Every Schwarzer Mond unit inherits this.
- `^NaxiCrystalLens` — radar-tier laser burst +1 for ALL yellow laser
  weapons (including 1-burst weapons). Laser units inherit this.
- `^NaxiAmplifiedLens` — tech-tier laser burst +1 for ALL yellow laser
  weapons (including 1-burst weapons). Laser units inherit this
  (requires Crystal Lens).
- `^NaxiVrilPoweredWeapons` — tech-tier +25% firepower and Tesla-type damage
  for cannon vehicles. Named after the Vril energy core from the Black Sun
  occult-science program.
- `^NaxiLunarAlloys` — radar-tier +10% damage reduction for all Schwarzer Mond
  units. Fills the survivability gap for non-laser, non-cannon units.
- `^NaxiMoonPropaganda` — tech-tier +10% firepower for all Schwarzer Mond
  infantry. The morale campaign is funded by **MoonCoin**, the official
  cryptocurrency of the Reichsmark 2.0 blockchain.
- `^NaxiHelium3` — radar-tier +50% power output for Hydrogen Plants and +25%
  speed/turn rate for all vehicles and aircraft. The Moon's regolith is rich in
  Helium-3, the isotope that fuels the Fourth Reich's fusion reactors and the
  Götterdämmerung-class warships; enriched Helium-3 is also used as a high-
  specific-impulse propellant for lunar vehicles and saucers.
- `^NaxiVrilInfusion` — tech-tier +25% firepower, +25% speed/turn rate and +15%
  damage reduction (Modifier 85) for all Schwarzer Mond infantry. Vril energy
  from the Black Sun program is spliced into the troopers, creating true
  Übermenschen on the battlefield.

Upgrade queue layout:
- **Radar tier (`Upgrades`)**: Crystal Lens, Lunar Alloys, Helium-3 Enrichment.
- **Tech tier (`Research`)**: Amplified Lens, Vril Powered Weapons, Vril Infusion,
  Cryptofascism, Moon Propaganda.

### 18.7 Promotion grid proposal

Schwarzer Mond uses the RA2 experience system (`^GainsExperienceRA2`) and can
support a 3-column promotion grid. The image proposal groups units into three
rough columns; this is refined into a **unit-improvement** promotion tree
because the listed units already exist in the regular tech tree.

| column | rank 1 | rank 2 | rank 3 | rank 4 |
|---|---|---|---|---|
| **Lunar Infantry** | Heavy Lunar Soldier | Übermensch Mk2 | Parzival (already hero) | — |
| **Lunar Armor** | Laser Beetle Mk2 | Lunar Tiger Mk2 | Neo Jagdpanzer Mk2 | Dalek (already epic) |
| **Lunar Flight** | Haunebu II (rebalanced) | Corruptor Piercer Mk2 | Haunebu III Mk2 | Die Glocke (already epic) |

Notes on the image proposal:
- The image lists **12 units** but the faction has **~25 buildable units**.
  The grid must cover the full roster or be supplemented by non-promotion
  upgrades.
- `Bradley` does not exist in the Schwarzer Mond roster; the closest unit is
  `MARS` (missile artillery) or `M-200B Jagerline` (AA/artillery). Either the
  image uses an old name or it refers to a different faction.
- `Noid MG`, `Neo Jagdpanzer`, `Korruptes Biest`, `Dalek` are a coherent heavy
  column.
- `Übermensch`, `Laser Tank`, `Crystal Tank`, `Parzival` are a coherent elite
  column.
- `Piercer`, `Haunebu 3`, `Die Glocke` are a coherent aircraft column, but
  `Bradley` is out of place.

If promotions are meant to **unlock** units, the base versions must be placed at
the appropriate tech tiers and the promotion versions must be strictly stronger
(§15 promotion superiority rule). If promotions are meant to **upgrade**
existing units, use the `^PromotionUnitBuff` template and add new upgrade
variants.

### 18.8 Cryptofascism upgrade

- ID: `schwarzer_mond_upgrade_cryptofascism`
- Name: Cryptofascism
- Tier: tech center, `Research` queue
- Cost: placeholder (rebalance with spreadsheet)
- Effect: grants every Schwarzer Mond unit a `CashTrickler` of 1 credit per
  25 ticks while the unit is alive.
- Icon: `nax2_cryptofascismicon.png` (64×48 px, placed in the faction's
  `files/` folder and wired via a sequence icon entry).
- Template: `^NaxiCryptofascism` added to every Schwarzer Mond actor's
  `Inherits` chain.

Because the effect scales with army size, it is a late-game snowball upgrade.
Cost and placement must be tuned so it pays back only after a large army exists.

### 18.9 Faction description normalization

The Schwarzer Mond description in `ContentPacks/RedAlert2Mod/SchwarzerMond/
translations/en.ftl` should follow the same point-based format used by Ixians,
Ordos, and Naxis:

```
Difficulty: ©©©
Early Game: ©©
Mid Game: ©©©©©
Late Game: ©©©©
Playstyle: Timing Attack
Strength: Mid to Lategame Tanks and Artillery
Weakness: Aircraft
Countered by: Early Game Rush, Aircraft
Special Units: Parzival, Dalek, Die Glocke
Special Buildings: Moon Dairy Farm, Gravity Core
Team Upgrades: Cryptofascism, Lunar Alloys
Support powers: Gravity Core, Meteor Traction Beam
```

Other RA2Mod factions (Consortium, Asian Alliance, Syndicate, FutureTech,
Naxis) should be normalized to the same format when touched; Schwarzer Mond is
the first to receive the full template because it is the focus faction.

### 18.10 Implementation order

1. [DONE] Add the new templates to `ContentPacks/RedAlert2Mod/Shared/yaml/
   templates.yaml` (Crystal Lens, Amplified Lens, Vril Powered Weapons, Lunar
   Alloys, Moon Propaganda, Cryptofascism, Helium-3, Vril Infusion).
2. [DONE] Add the four new upgrade actors to `ContentPacks/RedAlert2Mod/SchwarzerMond/
   yaml/upgrades.yaml`.
3. [DONE] Move Green Plasma Shells to tech center; split Crystal Lens; add
   Amplified Lens.
4. [DONE] Add the icon sequence for Cryptofascism and place the PNG in
   `mods/cameo/bits/ra2/mod/` (the same location used by the existing upgrade
   icons).
5. [DONE] Wire every Schwarzer Mond actor to the appropriate upgrade templates.
6. [DONE] Update weapon variants for the new +1-burst laser tiers.
7. [DONE] Update the faction description and individual unit descriptions.
8. [IN PROGRESS] Run the full audit suite (`tools/audit/run_all.sh`) and rebuild.
9. [TODO] Update the balance spreadsheet for any stat changes that affect cost or
   tier placement.
10. [DONE] Rename Green Plasma Shells to Vril Powered Weapons and add Helium-3
    Enrichment upgrade.
11. [DONE] Re-enable Crystal Lens / Amplified Lens on 1-burst laser weapons.
12. [DONE] Replace copy-pasted unit icons with unique placeholders per
    `docs/design/schwarzer_mond_artwork_status.md`.
13. [DONE] Finalize promotion intent: use existing `^PromotionUnitBuff` on all
    combat units instead of unlocking new actor variants.
14. [TODO] Boot-test the mod and verify the overhaul in-game.

### 18.11 Open questions for design

- Promotions will **upgrade existing units** via `^PromotionUnitBuff` rather than
  unlocking new actor variants. The existing RA2 experience system already
  provides veteran/elite ranks; the promotion grid proposal is flavor-only and
  does not require new Mk2 actors. All combat units now inherit the buff.
- The unit formerly referred to as `Bradley` in the promotion image is the
  hover artillery unit now named **MARS** (`schwarzer_mond_mars`). The name is
  an acronym (MARS = MRLS / mobile artillery rocket system) and follows the
  convention: actor id `schwarzer_mond_mars` (lowercase, underscore), display
  name `MARS` (uppercase acronym). The unit uses the `NaxisBradleyTarget`
  weapon, which is a legacy internal name that does not need to change unless
  we want to fully purge the old label.
- **MARS is AA-capable** (ground + air, long range). Laser Beetle and Laser
  Tank are also AA. SM does NOT lose all mobile AA when MARS replaces Jagerline
  — MARS is a direct upgrade with AA capability. The "no mobile AA" concern is
  invalid.
- Moon Dairy Farm passive income and Cryptofascism are independent: the dairy
  farm is a building income source, Cryptofascism only generates cash from living
  units. They can stack without special capping.
- **Promotion grid tier-mismatch (DESIGN DECISION NEEDED):** The current 3×4
  grid has row 1 unlocking T3 units (Übermensch, Piercer) while row 2 unlocks
  a T1 unit (Laser Tank). Five solution options are documented in
  `docs/design/ROADMAP.md` under "P2 — SM promotion grid tier-mismatch".
  Recommended: Option C (CABAL pattern — promotion gates visibility, tech
  gates power) or Option D (hybrid — soft tier sorting + CABAL gating).
  Awaiting maintainer decision.

### 18.12 Lore research — Iron Sky, Nazi Moon, and conspiracy-parody sources

Schwarzer Mond is built on the *Moon Nazi* conspiracy/parody trope, most famously
presented in the 2012 film *Iron Sky* and its 2019 sequel *Iron Sky: The Coming
Race*. Key motifs that can be mined for upgrades, unit names, and faction flavor:

- **Nazi Moon base**: In 1945 the Third Reich evacuates to the far side of the
  Moon, builds a swastika-shaped fortress, and waits decades to launch an
  invasion fleet of flying saucers and zeppelins. This is the core origin of
  Schwarzer Mond.
- **Helium-3**: The Moon's regolith is rich in Helium-3, a fusion fuel. In *Iron
  Sky* the Nazis mine it to power their ships, reactors, and the giant
  warship *Götterdämmerung*. This justifies a Helium-3 power/economy upgrade.
- **Vril and the Black Sun**: The sequel ties Nazi UFO technology to the Vril,
  a reptilian subterranean race that allegedly gave the Nazis advanced energy
  technology. "Vril Powered Weapons" replaces the generic "Green Plasma Shells"
  name and grounds the cannon upgrade in occult-science lore.
- **Die Glocke / Haunebu / Reichsflugscheibe**: Classic Nazi UFO conspiracy
  craft names (the Bell, the flying disc) that already appear in the roster as
  units (Haunebu II/III, Die Glocke).
- **MoonCoin / Reichsmark 2.0**: A crypto-currency parody fits the satirical
  tone. The Moon Propaganda upgrade is described as funded by MoonCoin on the
  Reichsmark 2.0 blockchain.
- **Jobsism and modern cults**: *Iron Sky 2* satirizes tech cults. This is
  optional flavor for future support powers or upgrades.

These sources are used only as parody/satire references; the faction remains a
fictional sci-fi faction, not an endorsement of any real-world ideology.
