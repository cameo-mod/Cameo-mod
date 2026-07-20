# MEGAPLAN — the consolidated rebalance program (master index, 2026-07-19)

_One entry point that ties every plan together. Read this first, then
the detailed sub-docs it points to. Update this when a phase completes
or a new program is added._

## 0. The goal

Rebalance EVERY unit in the game onto the per-class Formula v2 system,
mechanically enforced by the balance pipeline so no agent can drift it.
Two intertwined programs run together: the **class rebalance** (units)
and the **weapon-template refactor** (damage profiles). Neither is done
by hand — both flow through the ledger → workbook → gated apply loop.

## 1. The three pillars (existing docs — do not duplicate, extend)

| doc | what it is |
|---|---|
| **BALANCE_PIPELINE.md** | the yaml⇄JSON-ledger⇄workbook machinery + gated write-back + drift audit. The HOW. |
| **FORMULA_V2.md** | the per-class formula law book: O=P=Q=cost construction, King-Tiger 2.5× identity, stat bands, the infantry class ladder, all standing laws. The RULES. |
| **docs/balance/formula_v2_<class>.md** | per-class conversion logs (binding lessons; scout + closecombat live). The RECORD. |

The generated workbook now carries a **WeaponTypes** column (the
resolved ^-class templates per weapon — armor profile + effects) so a
unit's weapon behaviour is visible at a glance.

Supporting: ROADMAP.md (work queue), DESIGN.md (§12 formula origin),
docs/balance/class_anchors.json (the anchor registry).

## 2. Class rebalance — sequence (units)

Contiguous range bands DEFINE class membership (FORMULA_V2 §6b). Order:

1. **scout** [4500,5500] — LIVE. Baseline japan_imperialscoutsman (100),
   verifier forgotten_mutantsoldier (250). 6 units converted; ~13
   queued (formula_v2_scout.md proposal v2 awaits row verdicts).
2. **closecombat** [2500,4500) — LIVE. Baseline td_gdi_shotgunner (200),
   verifier asianalliance_fanatic (500). 3 units done; member list in
   formula_v2_closecombat.md.
3. **melee** [1250,2500) — NEXT. Range is size-derived (ratio fixed at 1;
   priced on HP/speed/DPS). Needs a baseline + verifier pick. Survey done
   (FORMULA_V2 §6b): zealot/zergling/footman/knight/dogs.
4. then: sniper (long band; zerg_defiler transforms in), heavy (own
   survey — many flame/chem units), hero/commando (attach/C4),
   support/special (ability-priced: spies, Yuri mind-control, CABAL
   hackers — needs an ability-value table).
5. then vehicles (mbt anchor = Tiger, LIVE) → aircraft (fighter/bomber)
   → defenses. Each: baseline + verifier + one-at-a-time conversions.

## 3. Weapon-template refactor — the profile/level system (NEW, design 2026-07-19)

**The construction law lives in ARMOR_SYSTEM.md** (step 6/5/4 = light/
medium/heavy; profile = armor order). Templates are GENERATED from
(order, step), never hand-typed.

### 3.1 The problem
The ~13 damage templates (^SmallArms … ^HeavyBomb, each used by 90-170
weapons) each bake a Versus armor-profile AND an implied power level
into one name, with inconsistent coverage and step sizes ("different
scaling"). There is no clean way to say "the same anti-heavy profile,
but stronger."

### 3.2 The fix: TWO orthogonal axes
- **PROFILE** (which armor it beats) = a family with ONE Versus table.
- **LEVEL** (raw power) = Light / Medium / Heavy, identical profile,
  scaled damage + spread by a fixed per-family ratio.
A weapon designer picks `{profile}{level}` (e.g. CannonAP_Heavy). One
scaling rule per family kills the "different scaling" dilemma; the
profile is the design axis across families.

### 3.3 The families (maintainer's taxonomy, mapped to today's templates)

| family | 3 levels | profile (good vs) | replaces |
|---|---|---|---|
| **CannonHE** | L/M/H | Light+Medium armor + structures (blast) | ^MediumCannon, ^HeavyCannon |
| **CannonAP** | L/M/H | Heavy+Superheavy (penetration) | ^TankDestroyerCannon |
| **MissileHE** | L/M/H | Light+Medium + structures | (from ^MediumMissile split) |
| **MissileAP** | L/M/H | Heavy+Superheavy | ^HeavyMissile |
| **MissileAA** | L/M/H | Fighter+Bomber+Spaceship (air) | (^FlakWeapon missile analogue) |
| explosion **Shrapnel** | L/M/H | infantry/soft (steep falloff) | ^Grenade (anti-inf role) |
| explosion **⟨broad⟩** | L/M/H | EVERYTHING (shallow falloff) | ^ShrapnelWeapon |
| explosion **Demolition** | L/M/H | Wood+Concrete structures | ^HeavyBomb |
| (kept) SmallArms / Chaingun | — | infantry only | unchanged (the WC .75/.875 base) |
| (kept) FlakWeapon | — | air (gun) | unchanged |

### 3.4 The naming decision the maintainer must make (the "good vs everything" explosion)

Today's ^ShrapnelWeapon hits Scout:100 → Plate:65 — a gentle slope
across ALL armor. That's the "universal blast." Names considered:

- **Concussion** (my recommendation) — a concussive overpressure blast
  genuinely damages everything; unambiguous, evocative, not overloaded.
- **Grenade** (maintainer's instinct) — familiar, but semantically a
  grenade is anti-personnel, which muddies it against the Shrapnel
  (anti-inf) family right next to it.
- **Thermobaric** — most physically accurate for "devastates
  everything," but exotic/long.
- **HE** — REJECTED: HE is a cannon/missile profile (blast vs
  light/medium); reusing it for the universal explosion collides.

Recommended full explosion taxonomy: **Shrapnel** (anti-inf, replaces
today's ^Grenade), **Concussion** (universal, replaces ^ShrapnelWeapon),
**Demolition** (anti-structure, replaces ^HeavyBomb) — each L/M/H. This
also fixes today's backwards naming (today "Shrapnel" is the broad one,
"Grenade" the narrow one). **Maintainer picks the universal name.**

### 3.5 How it plugs into the pipeline
Each family+level gets a **WeaponClass scalar** (the formula's damage
weight, like SmallArms .75 / SA+CG .875 today) in a design table the
maintainer fills — the profile does NOT change price, the level +
WeaponClass do. The refactor is a scripted template rename (pair-rename
law: base + every variant together) verified by the resolver + boot
gate; ~1300 weapon references move, so it runs as its own batch AFTER
the current class work, through the naming tooling.

## 4. Standing laws added this session (also in FORMULA_V2)

- **Descriptions carry NO `\n`**: all unit/weapon descriptions move to
  the fluent files (`fluent/**/en.ftl`) using REAL line breaks, never
  the `\n` escape. (Template placeholder descriptions are migrated with
  the fluent pass; new ones go straight to fluent.)
- SoundVolume = 1/burst for BurstDelays-0 multishot weapons.
- Tech-tier factor in pricing: T1=1.0, T3=0.75 (deepest prereq).
- Verifier-by-2×-bursts (== 2× damage) for burst weapons.
- `pricing:false` armament flag (teammate) — garrisoned/duplicate
  armaments are excluded from DPS pricing.

## 5. Multi-agent sync (2026-07-19)

Concurrent teammate work merged cleanly and is COMPLEMENTARY: the
`pricing` armament flag + workbook Class column (balance tooling),
Forgotten naval units, Obelisk charging flares (#211), the engine pin
bump to 2e0783c (stricter weapon validation — surfaced the ZClaw P0),
survival timing 80/40. No collisions with the class work. Rule stays:
commit early, scoped adds, verify others' work.

## 6. Dynamic Campaign Mode — "The Singularity Crisis" (design pitch 2026-07-19)

A highly replayable, dynamic, turn-based campaign that blends the strategic
overworld of *Empire at War*, the pre-battle army building of *Call to Arms:
Gates of Hell*, and the boss-gauntlet thrill of *C&C Generals: Zero Hour
Challenges*.

### 6.1 The Narrative Hook: The Convergence

The timelines are collapsing. An event known as the "Convergence" is drawing
every universe (Tiberian, Red Alert, Dune, StarCraft, Warcraft, Outpost, and
custom timelines) into a single, unstable singularity. If nothing is done, all
factions will be annihilated.

The player's chosen faction has theorized a "Chronal-Lock" (or equivalent
faction-specific device, e.g., an Omega Shield for the Consortium, a massive
Psychic Anchor for Yuri, a Temporal Core for the Allies). To build it, they
need pinnacle assets from across the multiverse.

The Commander and a vanguard army must step through a dimensional tear, select
a target dimension, and execute a 3-mission invasion to steal what they need
before moving to the next universe.

*Example:* A GDI Commander invades the Schwarzer Mond dimension to steal their
*Gravity Core* technology, then invades the Zerg dimension to harvest
*biological regeneration sequences*.

### 6.2 The Strategic Layer: The Multiverse Map

The campaign begins on a strategic UI layer representing the Multiverse.

- **Nodes representing Dimensions:** Instead of planets or territories, the map
  features dimensional nodes. Each node represents one of Cameo's 25+ factions.
- **Turn Sequence:**
  1. **Select Target:** The player chooses which faction dimension to invade.
  2. **The 3-Mission Arc:** Once a dimension is selected, the player is locked
     into a 3-mission mini-campaign against that faction.
  3. **Tension/Difficulty Scaling:** After defeating a faction, the overall
     "Convergence Threat" rises, making subsequent invasions harder (AI starts
     with more tech, better units, or more resources).

### 6.3 Pre-Battle Setup & Meta-Progression ("Gates of Hell" Style)

Between missions and dimensional jumps, players return to their Command
Ship/Temporal Hub to manage their army.

**Starting MCV Loadout:** Instead of starting every game with just an MCV and
two riflemen, players use "Meta-Credits" (earned by completing missions and
objectives) to construct their initial drop force. As the campaign progresses,
your initial landing force becomes a massive, customized vanguard.

**The Research System (Meta vs. Tactical):**

1. **Universal Meta-Tech (Between Battles):** Permanent, passive buffs applied
   universally. *Example:* Defeat the Steel Consortium → unlock "Nanite
   Infusion" giving all vehicles a slow passive heal. Defeat the Forgotten →
   infantry get Tiberium resistance.
2. **Tactical Tech (In-Battle):** If a technology is a standard in-game
   research (like Asian Alliance's *Celestial Power*), you unlock the *right*
   to research it in battle. You still spend credits and time inside the RTS,
   preserving pacing.

### 6.4 The Tactical Layer: The 3-Mission Arc

When a player invades a dimension, they play a 3-mission gauntlet on
handcrafted maps tailored to the defending faction's mechanics and lore.

- **Mission 1: Beachhead (Low/Medium Difficulty).** Establish a foothold. AI
  uses T1/T2 units. Objective: Secure a forward operating base or capture a key
  local resource.
- **Mission 2: Asymmetric Objective (Medium/High Difficulty).** A mission
  tailored to the enemy's playstyle.
  - *Example vs. Yuri:* Rescue mind-controlled scientists from a heavily
    defended Grinder facility.
  - *Example vs. Nod TS:* Survive a subterranean and stealth ambush while
    waiting for an evac transport.
- **Mission 3: The Artifact/Commander Assault (Boss Difficulty).** A massive,
  heavily fortified base assault. Enemy AI fully unlocked (T4/T5, superweapons
  active). Destroy the enemy HQ, steal the unique artifact, or assassinate the
  enemy Commander.

### 6.5 Co-Op Integration

The campaign fully supports 2-player Co-Op:

- AI difficulty multiplier is automatically increased.
- AI receives passive resource income modifiers to compensate for two brains.
- Both players pool Meta-Credits to build a joint MCV drop force.

### 6.6 Implementation Strategy & AI Assistance

Because of the massive scale (25+ factions = 75+ unique maps and scripts):

1. **Map Generation & Aesthetics:** Handcraft terrain; use AI to generate Lua
   scripts for AI behavior, attack waves, and objective triggers.
2. **Procedural Mission Templates:** Standard Lua templates for Mission 1/2/3.
   Feed the AI the target faction's profile → it populates attack waves with
   correct, lore-accurate units.
3. **Phased Rollout:** Launch "Campaign V1" with 6 dimensions (e.g., GDI,
   Yuri, Consortium, Schwarzer Mond, Ordos, Zerg), add new nodes in updates as
   maps and scripts are finalized.
