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
9. **Always separate top-level elements with a single blank line** —
   every actor, weapon, template, and sequence block is followed by an
   empty line before the next one, so it is easy to see where one ends
   and the next begins. A comment block stays attached (no blank line)
   to the element it documents. Scripted edits must preserve the blank
   line (a common bug: a block-replace that drops the trailing blank).
10. **All icons carry the `_icon` suffix** (§1/§8), including upgrade
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
  Light+Medium = 0.875, Medium+Heavy = 1.125.
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
- **Prices: 25-credit steps** — never 387-style numbers; if the formula
  lands off-grid, adjust unit stats until the price fits.
- **Damage: 2000-steps.** The HealthPercentageDamage twin is always
  **1 per 2000** main damage (16000 -> Percentage 8); FriendlyFire twins
  are always **50% damage and 50% spread**; all class warheads carry the
  identical (even-spread) value.
- **HP: 2500-steps** for vehicles/aircraft/ships (self-heal HP/2500,
  repair HP/20); **1000-steps for infantry** (self-heal HP/1000);
  defenses may use either (their self-heal is a flat 10).
- **Speed: steps of 5** (TurnSpeed = Speed/5 stays integral).
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

---

## 14. CABAL faction design rules

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

**Unique stat rule.** Every CABAL actor must carry at least one stat or
ability that no other actor shares (cost, speed, range, weapon class,
special K, or role). Two units may not feel identical.

**Balance workflow.** All CABAL rebalances start in
`docs/design/cameo_armor_system.xlsx` (or the CABAL concept sheet) and
land in YAML in the same pass. The workbook wins on mismatch. Promotions
add `^PromotionUnitBuff` on top of the sheet stats.
