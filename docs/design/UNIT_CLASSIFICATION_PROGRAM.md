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
Regenerated from `Inherits@Template:`, with an **explicit exclusion list** for actors the pipeline
must not price. Measured candidates for exclusion: `EDEN_*` / `PLYMOUTH_*` imports, `*_backup`
variants, `ra2_c_*` campaign actors — the 21 the `mbt` ledger already omits. **The omissions get
recorded rather than staying implicit**, which is what makes one source of truth safe.

⚠ Drift being repaired: `heavy_infantry` +48, `support` +47, `rocket_trooper` +43, `melee` +43,
`scout` +34, `commando` +33, `scout_vehicle` +27, `pure_sniper` +26, `mbt` +21.

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

## Sequencing

1. **A3 + A4** — classify the 67 and the epic 24, grouped, for approval. *(in progress)*
2. **A1 re-audit to zero**, then **A5** — regenerate the ledger tags from the templates.
3. **B** — the `KeepsDistance` rework, in `OpenRA.Mods.CA/`. Needs a C# build and a boot gate.
4. **C2** — dummy-weapon audit, once B proves which are redundant.
5. **C1 / C3** — weapon splits, alongside the per-class passes in
   [`CLASS_MOVES.md`](CLASS_MOVES.md).

⚠ Every yaml or C# change here is engine content: boot gate before commit (CLAUDE.md rule 1), and
any number that moves goes through `apply_balance --confirm` on a maintainer order (rule 3).
