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

- Weapons move into a pack only when used **exclusively** by that faction
  (computed through warhead sub-weapon and Inherits closure); shared
  weapons stay in the theme/shared files.
- Splits are done with `tools/packs/split_faction.py`, byte-preserving,
  and verified: merged actor/weapon/sequence registries must be identical
  before and after, and the faction's resolved closure diff must be empty.
- mod.yaml `Include:` order defines the lobby faction order.

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
| Turretless (AttackFrontal) vehicles | `TurnSpeed = 2 × Speed / 5` — artillery-template units keep `Speed / 5` |
| Turreted artillery / fire support | Archer firing-slow: `GrantConditionOnAttack(firing)`, 50% Speed/Turn/TurretTurn multipliers, `RevokeDelay = weapon ReloadDelay / 2` |
| Fighters & bombers (by template) | `Aircraft.TurnSpeed = Speed / 15` (frontal-weapon craft 2×) |
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
