# The unit-classification programme — twelve rulings, three workstreams

_Maintainer rulings, 2026-09-02, collected over four question rounds. Binding. Where a ruling
touches an existing law it says whether it CONFIRMS or OVERRIDES it._

⭐ **Priority order is fixed by the maintainer:** *"First thing must be to apply all actors to the
right class by giving them the correct unit template inherit."* Workstream A blocks B and C.

---

## A. Class templates — the classification itself

### A1. One class template per buildable unit ✅ ruled, audited
A buildable unit with **no** class template is a defect; one with **more than one** is a defect.
`^EpicVehicleTemplate` and `^EpicAirUnitTemplate` are **add-ons** that layer on top of a full class.
Enforced by `tools/audit/audit_class_templates.py`. **This is the classification the balance
pipeline reads**, replacing the ledger's `design.class_anchor` tag.

### A2. A sub-template is not a second class ✅ ruled
`^UnarmedTransportHelicopterTemplate` declares `Inherits@Template: ^HelicopterTemplate`;
`^DogTemplate` declares `Inherits: ^MeleeInfantryTemplate`. **Only the most specific template
counts.** Current state: **881 of 978 units (90%) already comply.**

### A3. The 67 untemplated units → grouped by proposed class
Classified from role and weapon, presented **one group per class** ("these 9 are rocket troopers"),
maintainer approves each group. Not 67 separate decisions.

### A4. The 24 epic-only units → base class from role, epic band kept separate
Each gets a real base class (a mammoth-pattern epic becomes `^HighTechTankTemplate` + the epic
add-on) **and stays band-exempt**, so a 10,000-credit epic does not distort the class it joins.
⚠ `BuildLimit:1` epics are already band-exempt in `check_band.py`; this keeps that.

### A5. `design.class_anchor` becomes DERIVED output ⛔ overrides current practice
### A5a. ⭐ MOSTLY ALREADY BUILT — and the two sources CONVERGE

⛔ **I was about to write a tool that exists.** `extract_stats.py` already derives the class from the
template chain: `actor_subtype()` (line 193) walks every `Inherits*` and returns the nearest
`^<Name>Template`, writing it as **`design.subtype`**. Only `class_anchor` is hand-maintained — it is
literally initialised to `None` (line 945) and filled in by hand afterwards.

| field | source | coverage |
|---|---|--:|
| `design.subtype` | **derived from templates** | **2,099 rows — the whole ledger** |
| `design.class_anchor` | hand-maintained | 346 rows (16%) |

⭐ **So A5 is a rename plus a name map, not a new derivation.**

**And the two sources agree far more than they disagree:**

| | pairs | actors |
|---|--:|--:|
| exact name match | 13 | **233** |
| disagree | 27 | 113 |
| — **not actually defects** | 2 | 46 |
| **real disagreements** | | **67** |

The two non-defects matter because they define the mapping rules:
* `MainBattleTank -> mbt` (42) — an **abbreviation**. A5 needs an explicit CamelCase→snake name map;
  `mbt` cannot be derived mechanically from `MainBattleTank`.
* `Dog -> melee` (4) — **correct by the A2 sub-template rule.** `subtype` returns the *nearest*
  template (`Dog`), the class is its parent (`melee`). The map must therefore resolve a sub-template
  to its class, exactly as `audit_class_templates.py` already does.

### ⭐ The 67 disagreements ARE the classification to-do list — and it matches the template analysis

This was derived from the *ledger*, entirely independently of the template scan, and it lands on the
same defects:

| disagreement | n | already known as |
|---|--:|---|
| `Infantry -> support` | 12 | **§2.1** — engineers with NO class template at all |
| `ScoutInfantry -> support` | 6 | spies/clones mis-tagged |
| `HeavyInfantry -> special_forces` | 5 | |
| `SniperInfantry -> support` | 4 | **§2.1** — the four engineers stuck in `^SniperInfantryTemplate` |
| `AntiTankAntiAirInfantry -> special_forces` | 4 | the AT/AA grab-bag |
| **`AntiTankAntiAirInfantry -> archer`** | **3** | **class 1** — the elven/Asian archers |
| **`SniperInfantry -> archer`** | **1** | **class 1** — `japan_archermaiden` |
| `futuretech_*droid` family | 5 | one faction tagged by ROLE while templated as vehicles |

⭐ **The hand-written tag usually recorded the INTENDED role correctly, and the TEMPLATE is what
drifted** — the archers were tagged `archer` in the ledger years before `^ArcherInfantryTemplate`
had a single inheritor. So fixing the templates makes the derived value right, and `class_anchor`
then follows for free. **The 67 are the work list, and finishing workstream A empties it.**

⚠ **Do not "fix" this by copying `class_anchor` onto the templates.** The ledger tag covers only
16% of the roster, so it cannot be the source. The template is the authority per A1; the tag is the
cross-check that says which templates to fix first.

⛔ **CORRECTED — my example of a "tag error" was not one.** I cited `futuretech_cannondroid`
(tagged `heavy_infantry`, templated `MainBattleTank`) as evidence the tag has its own errors.
**Maintainer:** *"The futuretech droids were actually already planned for changing into the infantry
templates, so yeah the heavy infantry was already correct."* The tag recorded the **planned**
classification; the template is the stale side. All five droids are in that state.

### A5b. Working the conflicts one by one — 67 → 56, before any ruling

⛔ **11 of the 67 are a NAMING CONVENTION, not a conflict.** My comparison stripped punctuation but
not the type suffix, so a template named for its type never matched its class:

| template | class | n | normalises to |
|---|---|--:|---|
| `CloseCombatInfantry` | `closecombat` | 3 | `closecombat` ✅ |
| `SpecialForcesInfantry` | `special_forces` | 3 | `specialforces` ✅ |
| `ScoutInfantry` | `scout` | 5 | `scout` ✅ |

**A5's name map must strip a trailing `Infantry` / `Vehicle` / `Tank`** before comparing, on top of
the `mbt` abbreviation and the sub-template resolution. With that, **the real conflict count is 56.**

### A5c. ⭐ `^MedicTemplate` and `^MechanicTemplate` should be SUB-TEMPLATES of support

Five more conflicts (`Medic -> support` 3, `Mechanic -> support` 2) share one cause: both templates
stand alone, inheriting only `^GainsExperienceInfantry`, `^ExternalConditions`,
`^GenericGroundDetector` and `^InfantryBuffs` — **not `^SupportInfantryTemplate`**.

⭐ **Make them inherit it, exactly as `^DogTemplate` inherits `^MeleeInfantryTemplate`** (A2). Then
`subtype` keeps returning the specific `Medic` / `Mechanic`, the class resolves to `support`, and all
five conflicts disappear without retagging a single actor.

⚠ And it composes with C2: both templates are the only two carrying `dummytargeting`, and both are
the units B5's friendly-seeking is designed for. The same five actors sit at the centre of three
workstreams.

Regenerated from `Inherits@Template:`, with an **explicit exclusion list** for actors the pipeline
must not price. Measured candidates for exclusion: `EDEN_*` / `PLYMOUTH_*` imports, `*_backup`
variants, `ra2_c_*` campaign actors — the 21 the `mbt` ledger already omits. **The omissions get
recorded rather than staying implicit**, which is what makes one source of truth safe.

⚠ Drift being repaired: `heavy_infantry` +48, `support` +47, `rocket_trooper` +43, `melee` +43,
`scout` +34, `commando` +33, `scout_vehicle` +27, `pure_sniper` +26, `mbt` +21.

### A6. Unbuildable spawned units get their OWN class, not an exclusion ⛔ corrects my proposal
**Maintainer:** *"there should be still something for unbuildable units like those like the carrier
drones so that these also get their own unique classification … it should be their own carrier
drone template that belongs to their own balancing."*

⛔ I had proposed putting spawned units on the A5 exclusion list. **Wrong** — they are balanced
content and need a class; only genuinely non-unit actors (husks, cameras, concrete markers) are
excluded.

**Measured:** 29 spawner carriers exist, via `DroneSpawnerMaster`, `DroneSpawnerMasterCA`,
`MissileSpawnerMasterCA`, `MobSpawnerMaster` and `SlaveMinerSpawnerMaster`.

⛔ **CORRECTED 2026-09-02 — three of the four I called defects are not defects.** A spawner is not a
carrier, and I lumped four unrelated mechanics together:

| spawned actor | what it actually is | verdict |
|---|---|---|
| `ra2dmisl`, `yrbsubmisl`, `miniv2.nax` | **MISSILES** — weapon projectiles that explode on impact | ✅ not drones. *"They should be balanced as if they were fired by the unit directly. However they can be intercepted and destroyed, that's why it's harder to balance those and they need their own formula."* |
| `ra2shk.bot` | the RA2 **tesla charger** — spawned only to charge a tesla coil, **with no attack at all** | ✅ a support unit, not a drone |
| `fremen_creep` | "Fremen Warrior", 500cr / 25,000 HP, foot, HMG + RPG, from a `MobSpawnerMaster` | ⚠ spawned *infantry* from a sietch, not a drone |
| **`apparition.ixian`** | 2,000cr / 37,500 HP hover craft, `d2k_basq` + `d2k_basq_AA`, filed as **`^MeleeInfantryTemplate`** | ⛔ **the only real defect** |

⭐ **So three spawn mechanics need three treatments, not one class:**
* **Missiles** — priced into the firing unit's damage, with their own formula because they are
  interceptable. Not classified as units.
* **Support spawns** (tesla charger) — a support unit; no drone class, no damage contribution.
* **Carrier drones** (apparition) — a real class with an ammo pool (D4).

**Ruled:** a new `^CarrierDroneTemplate` (and the audit's population widens from "buildable" to
"buildable **or spawned by a carrier**"). ⚠ `audit_class_templates.py` currently scopes on
`Buildable`, so it does not see these at all — that scope has to widen with this ruling.

### A6a. The apparition fix, and ⛔ why it is not yet a yaml-only change
**Maintainer:** *"the apparition should be transformed into a carrier drone and the Ixian projector
should work more like a land based aircraft carrier."*

**The structural half is straightforward** and can go in the next class pass:
* `apparition.ixian` → `^CarrierDroneTemplate` (replacing `^MeleeInfantryTemplate`)
* `ixian_ixprojector` (5,000cr, epic add-on, no base class) → a base class + the epic add-on

⛔ **The behavioural half is blocked on the engine, and I could not make it work in yaml.** A
land-based carrier means *launch → attack → return → rearm from a finite pool*. What the projector
has today is **respawn**, not rearm:

```
DroneSpawnerMaster@steel_scalpel:
    Actors: apparition.ixian x5
    ArmamentNames: secondary
    RespawnTicks: 250          <-- a destroyed drone is REPLACED after 250 ticks
    FollowAfterAttackDelay: 25
```

`DroneSpawnerMasterCA` (readable in `OpenRA.Mods.Cameo/`) is respawn-based — `spawnReplaceTicks =
Info.RespawnTicks` — and exposes no ammo pool, no rearm and no return-to-carrier. The projector's
own `DroneSpawnerMaster` (no `CA` suffix) is in an assembly this repository does not contain, so its
capabilities are **unverified**.

⚠ **Therefore: the class move is proposable now; carrier SEMANTICS need either a trait that supports
rearming or new C#.** Recorded, not attempted — and it is the concrete reason D stays deferred
rather than a scheduling preference.

### A7. Husks and non-units are SEPARATED, never classified
**Maintainer:** *"Husks and other things must be also separated so they don't appear as regular
units in the balance formula."*

⚠ **This is the counterpart to A6, not a contradiction of it.** A6 says a spawned *combat* unit is
balanced content and gets a class. A7 says a wreck, a camera or a blast marker is not a unit at all
and must never enter a class distribution — a husk priced as an `mbt` would drag that class's
anchor and its sigma.

**Measured — 951 non-buildable actors, in four groups:**

| group | n | examples |
|---|--:|---|
| **husks** | **305** | `A10.Husk`, `ARCO.Husk`, `BADR.Husk` — all with no template |
| unbuildable buildings | 439 | `AMMOBOX1`, `ARCO` |
| unbuildable mobile | 123 | `C1`, `C10`, `C17` (civilians) — ⚠ needs sorting: carrier drones live here too |
| other | 84 | `BLASTRADIUS.atomic`, `CAMERA` |

⛔ **The 123 "unbuildable mobile" is the group that needs care**, because A6's carrier drones and
A7's civilians are both in it. Spawned-by-a-carrier is the discriminator, and it is structural: an
actor named in some carrier's `Spawner*.Actors` list.

---

## B. `KeepsDistance` — make the trait do what its own `[Desc]` claims

⛔ **The trait today handles ONLY an explicit right-click order.** Its description says *"Will keep
distance from enemies that the unit can't attack"*; `OpenRA.Mods.CA/Traits/KeepsDistance.cs`
implements a `KeepDistance` order targeter and a `MoveWithinRange` resolver, and nothing else.
Nothing hooks attack-move or autotarget. ⭐ `OpenRA.Mods.CA/` **is tracked in this repository**, so
this is editable here — unlike `engine/`.

### B1. Trigger: attack-move **and** autotarget
A plain move order still goes exactly where you clicked — you can always walk a medic in
deliberately.

### B2. Predicate: structural, symmetric, no yaml marker
* **"I cannot attack E"** — no enabled armament of mine has a weapon whose target filters accept E.
  A sniper with an anti-infantry weapon reads a tank as un-attackable; a medic reads *everything*
  as un-attackable.
* **"E is a threat to me"** — E has an enabled armament whose weapon can target *my* type.
  ⭐ **Both halves must hold before the unit keeps distance.** Spy planes, weaponless walls, ore
  trucks and disabled defences are harmless, so a sniper walks straight past them.
* **No yaml marker.** Fully structural, so it self-corrects when a unit's weapons change.

### B3. Behaviour: route around; if no route exists, STOP and hold
Path around the threat at >= `Distance`. ⚠ When no such path exists — a defended chokepoint — the
unit **halts at stand-off and keeps the attack-move queued**, resuming when the blocker dies or
moves. It does not push through.

### B4. Config: the same trait, the same `Distance` ✅
No new trait and no second distance field. The five templates that already declare
`KeepsDistance: Distance: 10` (Medic, Mechanic, Sniper, HeavySniper, Archer's neighbours) get the
behaviour for free, and the trait finally matches its own description.

### B5. Falling back means REJOINING THE ARMY, not just halting
**Maintainer:** *"if it has to fall back or route around enemies it should instead move to where
the most friendly units are by value."*

* **Value = build cost weighted by damage taken.** A support unit drifts toward the expensive
  *and* the hurt, not merely the expensive.
* **Preference is a weighting, never exclusive.** Friendlies the unit can service count roughly 3x;
  everything else still counts, so a medic in an all-vehicle army shelters with the tanks instead of
  standing alone. A sniper services nobody, so it weights everything equally and simply goes where
  the army is.
* **The attack-move order stays queued, and the unit KEEPS TRACKING the friendly centre of value
  while it waits** — so a medic trails an advancing army rather than sitting where the army used to
  be, and resumes the push when the blocker clears.
* **Search radius = the trait's own `Distance`.** No second number. With no friendlies inside it,
  B3 applies unchanged: halt at stand-off and hold.

⭐ **"Units I can service" is the SAME predicate as B2, pointed at allies.** Verified on
`ra1_allies_medic`: `Armament: Weapon: Heal, TargetRelationships: Ally`. So one mechanism answers
all three questions — *can I attack E*, *can E attack me*, *can I help F* — with no new concept and
no yaml marker.

⚠ **Assumed, flag if wrong:** the brief said *"medics should stay with vehicles they can repair"*;
read as **mechanics** repair vehicles, medics heal infantry, matching the separate `^MedicTemplate`
and `^MechanicTemplate` that both already declare `KeepsDistance`.

### B6. ⛔ The predicate must IGNORE zero-damage armaments — or it fails on the exact units it is for
Measured on `ra1_allies_medic`:

```
Armament@Dummy:  Name: dummy,  Weapon: dummytargeting     <- Range 10000, ValidTargets: Ground, Water, Air
Armament:        Weapon: Heal, TargetRelationships: Ally
```

`dummytargeting` is `Range: 10000`, targets **Ground, Water and Air**, and carries a single
`Warhead@Dummy: Dummy` — zero damage. A naive *"does any armament of mine target E"* therefore
answers **yes** for a medic against everything, and the medic never keeps distance. **The predicate
counts only armaments whose weapon can actually deal damage.**

⭐ This is also direct evidence for C2: `dummytargeting` is declared on exactly two templates —
`^MedicTemplate` (`defaults.yaml:1133`) and `^MechanicTemplate` (`:1162`) — and does nothing but
give an unarmed unit something to point at. It is the prime removal candidate once B lands.
⚠ But it feeds `AutoTarget` (`InitialStance: AttackAnything` +
`AutoTargetPriority: ValidTargets: Infantry, plagued, ivanattached, lockdowned`), which is how a
medic finds patients — so removing it must not break auto-healing. Verify, do not assume.

⛔ **No dummy weapons.** *"I don't want to use some crappy dummy weapons for that since that would
also block their main healing or repair weapon from firing, and it would stop the snipers from
attacking infantry."* The stand-off must be independent of any armament.

---

## C. Weapons

### C1. One weapon per actor, zero-damage support exempt ✅ CONFIRMS `DESIGN.md`
`DESIGN.md` already rules cross-faction weapons *"are split, one weapon per actor"* with one
exception: **zero-damage support weapons (21 measured) may stay shared** under a name that is
*specific*, e.g. `shared_targeting_air_long` rather than two different things both called
`shared_targeting`. **That exemption stands.** 283 weapons are currently shared; the damage-dealing
ones are the backlog.

### C2. The 7 dummy weapons: audit each, remove only the stand-off ones
### C2a. ⭐ MEASURED: what `dummytargeting` is actually for, and why B5 replaces it

**Maintainer:** *"exactly, that's why we need to remove the dummy weapon from those! But only after
the keep distance rework is fine and done."* ✅ Sequencing confirmed — C2 runs **after** B is
verified, never alongside it.

Measured, so the removal is a specified swap rather than a hopeful deletion:

| | `Heal` | `dummytargeting` |
|---|---|---|
| `Range` | **5,000** | **10,000** |
| `ValidTargets` | `Heal, lockdowned, plagued, ivanattached` | `Ground, Water, Air` |
| `ReloadDelay` | 75 | — |

⭐ **The dummy is exactly 2x the heal range, and it is the only armament that matches the
autotarget priority.** `AutoTargetPriority@DEFAULT` lists `Infantry, plagued, ivanattached,
lockdowned` — and `Infantry` is a target type the `Heal` weapon **cannot** hit while the dummy can.
So the dummy does two jobs: it widens the autotarget SCAN to 10,000 so a medic notices a patient it
must walk to, and it satisfies the `Infantry` priority row.

⛔ **Deleting it without a replacement would halve a medic's effective seek range and break that
priority row.** That is the concrete risk behind "must not break auto-healing".

⭐ **And B5 is the replacement, at the same distance.** `KeepsDistance: Distance: 10` cells is
~10,240 — the dummy's 10,000 to within 2%. B5 already makes the unit move toward friendlies it can
service, using the B2 predicate pointed at allies, inside exactly that radius. So once B lands the
medic seeks patients *because it is a support unit*, not because it carries a fake gun.

**The C2 test for these two templates is therefore specific:** with the dummy removed and B active,
a medic must still walk to and heal a wounded infantryman first sighted at ~10,000 — not merely one
already inside 5,000.


`dummytargeting`, `FakeHealtAPC`, `RemovableDebuffDummy`, `ScarabLaunchDummy`,
`TeslaArmorDischargeDummy`, `bfg10kCannonDummy`, `superbfg10kCannonDummy`. Any that exists **only**
to fake a range or a stand-off becomes removable once B lands; the ones carrying real mechanics
(debuff carriers, launch triggers, armor discharge) stay. ⚠ Nothing is deleted on the assumption
that "dummy" means useless.

### C3. `wc2_orcs_kodobeast` → `^SupportVehicleTemplate`, with its own weapon
It inherits `^WC2Vehicle`, so it is a **vehicle in an infantry class** today — a defect independent
of the archer work. It gets `^SupportVehicleTemplate` and its own `wc2kodo*` weapon cloned from
`wc2axeFire` and then tuned, which also stops `wc2axeFire` being shared three ways.
⚠ Its `Tooltip` declares `Name:` twice (`Kodo Beast`, then `garrisoned`) — a separate defect.

---

---

## D. Carriers and their drones — recorded for later, not yet scheduled

**Maintainer, verbatim:** *"carrier drone damage must be added to the damage of the carrier itself.
Currently there is only one carrier where it was done correctly (if you count the fire power
multiplier which will be replaced in the future) and it's the Steel Consortium cloud breaker. If you
calculate it the carrier drones summed up will deal exactly 50% of the total damage while the other
50% is done by that carriers main weapon."*

### D1. A carrier's damage INCLUDES its drones' damage
For pricing, a carrier's DPS is `own weapon + sum of drone DPS`. Today only
`steelconsortium_cloudbreaker` is built this way — 50% carrier weapon, 50% drones.
⚠ *"if you count the firepower multiplier, which will be replaced in the future"* — so the 50/50
holds under today's `FirepowerMultiplier`, which W17 is retiring. **The split has to be re-derived
once that knob goes**, or the one correct example stops being correct.

### D2. Drone cost = 20% of the carrier's price, divided across the drones
```
drone_cost = (carrier_cost / drone_count) * 0.2
```
so **all drones together cost 20% of the carrier**. Worked example from the brief: a 4,000-credit
carrier with 8 drones gives `4000 / 8 * 0.2 = 100` per drone, and `8 x 100 = 800 = 20% of 4000`. ✅

### D3. Drone health uses the same function
`drone_hp = (carrier_hp / drone_count) * 0.2`, so all drones together carry 20% of the carrier's HP.

### D4. Carrier drones carry a LIMITED AMMO POOL, and it sizes the damage
**Maintainer:** *"Carrier drones should also have a limited ammo pool like the bomber template
(that's exactly why they used that template before, but the bomber template is now only for
buildable bomber units while carrier drones should be separated). The carrier drones ammo pool
should be used to calculate the entire damage output which is then used to calculate the damage
output of the carrier including all the drones."*

⭐ **This explains the bomber template.** The drones were never mis-filed at random — they were put
in `^BomberTemplate` *for its ammo pool*. `^CarrierDroneTemplate` (A6) therefore has to carry an
ammo pool of its own, and `^BomberTemplate` narrows to buildable bombers only.

**So a carrier drone's damage is a FINITE total, not a rate:**

```
drone_damage_total   = ammo_pool x damage_per_shot
carrier_damage_total = own weapon + SUM(drone_damage_total)      # D1
```

⚠ **This changes what D1 means.** A drone with 4 rounds contributes four shots' worth and then must
rearm — so a carrier's "50% from drones" is a *sortie* figure, not a sustained DPS. Whether the
pricing formula should use burst or sustained damage for carriers is an open question, and it must
be settled before D1 can be applied.

⚠ **Not yet verified, and it must be before this is applied:** the cloudbreaker is
`^SpaceshipTemplate`, `Cost: 5000`, `HP: 250000`, and its spawner was not in the 29 the scan found —
so its drone count and the 50/50 measurement still need confirming against the resolved rules.
Recorded as a maintainer statement, not yet as a measured fact.

---

## Sequencing

1. **A3 + A4** — classify the 67 and the epic 24, grouped, for approval. *(in progress)*
2. **A1 re-audit to zero**, then **A5** — regenerate the ledger tags from the templates.
3. **B** — the `KeepsDistance` rework, in `OpenRA.Mods.CA/`. Needs a C# build and a boot gate.
4. **C2** — dummy-weapon audit, once B proves which are redundant.
5. **C1 / C3** — weapon splits, alongside the per-class passes in
   [`CLASS_MOVES.md`](CLASS_MOVES.md).
6. **D** — carriers. ⛔ Deliberately last: D1's 50/50 baseline is stated in terms of
   `FirepowerMultiplier`, which W17 is retiring, so measuring it now would pin a number to a knob
   that is being removed. Verify the cloudbreaker first, then derive.

⚠ Every yaml or C# change here is engine content: boot gate before commit (CLAUDE.md rule 1), and
any number that moves goes through `apply_balance --confirm` on a maintainer order (rule 3).
