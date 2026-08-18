# The armor-plating layer, shields and Integrity — measured mechanics and shipped design

**Status: ✅ MOSTLY SHIPPED (2026-08-16/17).** Started as research into three maintainer
questions and became the plating layer. What is LIVE, with the binding summary in
`DESIGN.md §12.0e/§12.0f`:

| shipped | where |
|---|---|
| 5 platings `HAZMAT` `COMPOSITE` `BLAST` `REFLECTOR` `ARMOR`, ALL CAPS, full columns in all 94 templates | §D-bis, §G, §H |
| LAYER SELECTION — a plating replaces the class armor | `AreaDamageWarhead.DamageVersus` |
| the column law: every plating averages **70** | §I |
| `effective_HP = HP + shield x 0.540`, measured live | §I |
| 4 upgrades retagged; the generic ones stay multipliers | §G |
| guards `audit_armor_upgrade_harm.py` + `audit_plating_exclusivity.py` | §F, run_all.sh |
| `Waveforce` IntegrityScale deleted (could never fire) | §B, §D-bis |

**STILL OPEN:** the Integrity options I1–I4 (§B) are analysis only — the pool is still 100%
of max HP, so the EMP disable still lands late; and the `IntegrityScaleMultiplier` design
(§D) is written but NOT built, so 15 sites still hard-code `IntegrityScale: 150`.

⚠ **Sections A–E are preserved as WRITTEN, including options that were later rejected**, so
the reasoning stays auditable. Where a later section supersedes an earlier one it says so.
The most important such correction: **§A1–A4 describe the AVERAGING world**, which still
governs class armors but NO LONGER governs platings — §F replaced that with selection.

Three maintainer questions of 2026-08-16, answered from the artifacts rather than from the
design docs:

1. scale `HAZMAT` **and** `REFLECTOR` by the weapon's thermal / chemical / explosive /
   energy composition — and add `REFLECTOR` to the mixed families that lack it;
2. integrity damage below the disable threshold is useless — what do we do;
3. the Tesla EMP upgrade is hard-coded inline; make it a MULTIPLIER on the base weapon.

Plus the general question: **what is the balance formula still not seeing?**

---

## A. The mechanics, as measured

Every claim here was read out of the code or counted in the resolved ruleset. Several
contradict what the design docs assume, so the numbers matter.

### A1 — Multiple armor types AVERAGE (they do not multiply)

`AreaDamageWarhead.DamageVersus` overrides the engine's product with
`MultiArmorCombination`, default **Average** (the W21 ruling). An actor carrying a base
armor plus an overlay takes `avg(Versus[base], Versus[overlay])`.

### A2 — ⚠ A MISSING row is EXCLUDED from the average, NOT treated as 100

Both the engine and the Cameo override filter on `Versus.ContainsKey(a.Info.Type)`. So for
a multi-armor actor:

```
weapon has no REFLECTOR row   ->  REFLECTOR is dropped; only the base armor counts
weapon has REFLECTOR: 100     ->  avg(base, 100)  — which is NOT the same thing
```

**Omitting the row is the only way to say "this weapon does not care about reflective
plating".** Writing 100 is a real change that pulls the result toward 100. This is the
single most important fact for the maintainer's question, because "just add REFLECTOR
everywhere" and "add it only where it means something" are genuinely different designs.

(For a SINGLE-armor actor the two coincide: an empty armor list returns 100. That is why
"a missing row resolves to 100" was true in the W23 retrofit and is false here.)

### A3 — An overlay armor can never cut damage by more than ~50%

With averaging, `effective = (base + overlay) / 2`. Even at the window floor
(`overlay = 10`) the result is `(base + 10) / 2` — just over half. **The whole HAZMAT /
REFLECTOR design space is bounded at a ~2x damage reduction**, and no choice of row value
can exceed it. Any stronger protection has to come from somewhere else (a
`DamageMultiplier`, a second armor, a shield).

### A4 — How much an overlay protects depends on the BASE armor

Because it is an average, a flat `HAZMAT: 50` gives wildly different protection:

| target | base Versus (chem weapon) | with HAZMAT 50 | reduction |
|---|--:|--:|--:|
| unarmoured infantry | 200 | 125 | **37%** |
| superheavy tank | 60 | 55 | **8%** |

Thematically that is defensible — suits are for infantry — but it is an accident of the
averaging rule, not a decision. It also means the row value alone does not express intent;
the intent is a *reduction*, and the row that produces it depends on the target.

### A5 — Integrity does NOT absorb damage

`Integrity` is an ELECTRONICS pool, not a shield (its own `[Desc]` says so). Two things
drain it, and **neither reduces the HP damage**:

* `INotifyDamage.Damaged` — subtracts the damage **1:1**, but only for damage whose types
  overlap `AffectedByDamageTypes` (Cameo uses `Tesla`);
* `AreaDamageWarhead.ApplyIntegrityScale` — subtracts a further `damage x IntegrityScale/100`.

So the drain rate is `(1 if Tesla-typed else 0) + IntegrityScale/100` per point of damage
dealt.

### A6 — The pool is 100% of max HP, and the disable fires at zero

`^UnitDisable` sets `MaxPercentageStrength: 100` (`^EpicAirUnitTemplate` 200). At
`Strength <= 0` the `electronics` condition is revoked, which grants `empdisable` — the
actor is disabled. Strength then banks down to `-MaxStrength`.

Regen: `DamageRegenDelay: 75` ticks (~3 s, **reset by every hit**), then `RegenAmount: 1000`
every tick. A 20 000-HP unit refills in 20 ticks (~0.8 s).

### A7 — Census (resolved, transitive through `Inherits`)

| trait / armor | concrete actors | of 3107 |
|---|--:|--:|
| `Shielded` (the real shield layer) | 1592 | 51% |
| `Integrity` (the EMP pool) | 1233 | 40% |
| `HAZMAT` armor | 329 | 11% |
| `REFLECTOR` armor | 16 | 0.5% |

None of these is dead code. `HAZMAT` is the third-commonest armor type in the mod.

---

## B. Question 2 answered: WHY integrity damage is useless — and it is not the scale

Drain rate `x` damage until the pool (= 1x MaxHP) empties:

| family | `IntegrityScale` | carries `Tesla` type? | drain / damage | damage to disable | target HP left |
|---|--:|---|--:|--:|--:|
| Tesla | 100 | ✅ | 2.00x | 0.50 x MaxHP | **50%** |
| Storm | 50 | ✅ | 1.50x | 0.67 x MaxHP | **33%** |
| Quantum | 33 | ✅ | 1.33x | 0.75 x MaxHP | **25%** |
| **Waveforce** | 20 | ❌ | **0.20x** | **5.00 x MaxHP** | **NEVER — dies 5x over** |

**The finding is sharper than the maintainer's framing.** Tesla is fine; the disable lands
at half health, which is a real effect. What kills the axis is the **damage TYPE**, not the
scale value: `Waveforce` never received `Tesla` in its `DamageTypes`, so it loses the 1:1
passive drain and keeps only its 20% scale — a factor of six. Its `IntegrityScale: 20` is
decorative. (Its `_Percentage` twin *does* carry `DamageTypes: Tesla`, but the twin deals
~1% of max HP a hit, so emptying the pool through it alone takes ~80 seconds of sustained
fire.)

Two further structural problems:

* **Overdrain buys almost nothing.** Strength banks to `-MaxStrength`, but regen is
  1000/tick, so a full overdrain holds the disable ~0.8 s longer — against a 3 s
  `DamageRegenDelay` that any hit resets anyway. There is no reward for exceeding the
  threshold, so the axis is a cliff with nothing on either side of it.
* **The pool scales with max HP.** A tank's electronics are not ten times tougher because
  its armour is. Tying the pool to HP makes big units disproportionately EMP-proof, which
  is backwards for what EMP is *for*.

### Options

| # | change | effect | cost |
|---|---|---|---|
| **I1** | `MaxPercentageStrength` 100 -> **25** | disable lands at 87% HP (Tesla), 81% (Quantum) — the axis starts mattering | one number |
| **I2** | give every integrity family the `Tesla` damage type | fixes Waveforce outright (0.20x -> 1.20x) | generator table |
| **I3** | pool becomes a FLAT `MaxStrength` per class, not %HP | EMP stops scaling with armour; a scout and a mammoth are equally disablable, which is what electronics means | needs per-class values |
| **I4** | slow regen (`RegenAmount` 1000 -> ~2% of max per tick) | overdrain finally buys disable TIME, so the threshold stops being a cliff | one number, but changes every EMP duel |
| **I5** | make the pool absorb damage | rejected — that makes it a second shield, which its own docs forbid, and W21's "two intercepting layers must never share a hit" applies | — |

**Recommendation: I2 first** (it is a correctness fix — a family that declares an EMP role
and cannot deliver it is a bug), then **I1** as the cheap lever that makes the axis live,
with **I3 + I4** as the principled version once units are finished. I3 in particular should
wait: it interacts with the class anchors.

---

## C. Question 1 answered: scaling HAZMAT and REFLECTOR

### The problem, stated precisely

`HAZMAT: 50` is a **constant** on every family (`family(hazmat=50)`), suppressed only for
Sonic and Magic. A flamethrower and a nerve-gas shell are blunted identically by a hazmat
suit, which is exactly what the maintainer objected to. `REFLECTOR` is worse: it exists on
**one** table — Tesla's `ExtraDamage` chip — so 30 energy and blended-energy families have
no opinion about reflective plating at all, and by A2 that means the plating does nothing
against them.

### The composition model

Every family already declares its composition; it just is not read as one:

* `FAMILY_PHYSICAL_STATE` says which meter it drives (`Flame`->Temperature 100,
  `Chemical`->Corrosion 100, `Cryo`->Temperature -100);
* `BLEND_FAMILIES` says what a blend is made of (`Plasma = Flame + Chemical`,
  `Waveforce` = all five primitives);
* `PHYSICS_RANK` already ranks field-coupling for the Shield axis.

So a family's `chem_share` (what a suit stops) and `energy_share` (what a mirror stops) are
derivable rather than invented.

### Options

**Option C1 — share-scaled depth (simplest).**
```
HAZMAT    = round(100 - HAZ_DEPTH   x chem_share)     # omit the row when chem_share == 0
REFLECTOR = round(100 - REFL_DEPTH  x energy_share)   # omit when energy_share == 0
```
With `HAZ_DEPTH = 90`: Chemical/Toxic (1.0) -> 10, Plasma (0.5) -> 55, Flame (0.6) -> 46,
Bullet (0) -> **omitted**. One constant per axis, per-family shares, and A2 respected.

**Option C2 — derive both shares from the physical-state system.** `chem_share` =
`PhysicalStateScale/100` for Temperature/Corrosion families, averaged through
`BLEND_FAMILIES` for blends. Most principled and self-maintaining — a new blend gets a
correct HAZMAT for free — but it leaves the energy axis undefined, because there is no
"photonic" meter. Needs C3 for the other half.

**Option C3 — an explicit `ENERGY_RANK` table for REFLECTOR.** Deliberately **not**
`PHYSICS_RANK`: that ranks coupling to a force FIELD, and a mirror is not a field. A mirror
stops *photons* — so Laser and Prism should be at the top, while Tesla (which
`PHYSICS_RANK` puts at 1.00) should be near the bottom, because a mirror does nothing about
a lightning bolt. Reusing `PHYSICS_RANK` here would be a category error that happens to
look tidy.

**Option C4 — express the intent as a REDUCTION, not a row.** Because of A3/A4 the row
value does not mean what it looks like. Author `hazmat_reduction = 0.30` and let the
generator solve the row against a reference base armor. Most honest, most machinery.

**Recommendation: C2 for HAZMAT + C3 for REFLECTOR, emitted through C1's formula**, with
the row OMITTED whenever the share rounds to zero. C4 is the right long-term shape but
should wait until the reduction targets are themselves decided.

### Sanity bounds to respect

* Never below the window floor (10) — a pseudo-armor is still a Versus row.
* Never above 100 — an overlay that makes a weapon *better* is a different mechanic.
* By A3, the strongest possible suit is ~2x, so `HAZ_DEPTH` above ~90 buys nothing.

---

## D. Question 3 answered: the EMP upgrade should be a MULTIPLIER

### What it looks like today

```yaml
PortaTesla_EMP:
    Inherits: PortaTesla
    Warhead@Tesla_Heavy:
        IntegrityScale: 150          # hard-coded, x15 across the tree
    Warhead@EMPUnit: AffectsIntegrity
        Damage: 10000                # a SECOND hard-coded number
```

**15 sites** carry `IntegrityScale: 150` inline. The maintainer is right that this is
wrong, and there is a second reason beyond ugliness: **150 is not a multiplier**. On Tesla
(base 100) it is 1.5x; on Quantum (base 33) the same literal would be **4.5x**. The number
means something different on every family, so it cannot be shared — which is precisely why
it got copied fifteen times.

### The design

Add one field to `AreaDamageWarhead`:

```csharp
[Desc("Percentage MULTIPLIER applied to IntegrityScale, so an upgrade template can say",
      "'twice the EMP' once instead of restating an absolute value per family.")]
public readonly int IntegrityScaleMultiplier = 100;
```
with `ApplyIntegrityScale` using `IntegrityScale * IntegrityScaleMultiplier / 100`.

Then the generator emits one upgrade template per family+level, carrying nothing else:

```yaml
^Upgrade_EMP_Tesla_Heavy:
    Warhead@Tesla_Heavy:
        IntegrityScaleMultiplier: 200
```

and every upgrade weapon becomes exactly the one inherit the maintainer asked for:

```yaml
PortaTesla_EMP:
    Inherits: PortaTesla
    Inherits@emp: ^Upgrade_EMP_Tesla_Heavy
```

200% then means 200 on Tesla and 66 on Quantum automatically — the maintainer's own
example, and the reason the multiplier is the right primitive.

⚠ **`Inherits` POSITION is semantic** (LESSONS_LEARNED, 2026-08-16): the upgrade inherit
must come AFTER the base so the multiplier is not overridden back. The upgrade template
carrying *only* that one field keeps the blast radius of that rule small.

⚠ **The separate `Warhead@EMPUnit: AffectsIntegrity` chips are a different question.** They
are a flat, damage-independent drain and they duplicate what `IntegrityScale` now does
proportionally. They should probably be retired in the same sweep — but that changes
behaviour, so it is a maintainer call, not a refactor.

---

## D-bis. ✅ SHIPPED 2026-08-16 — the maintainer's rulings

### 1. Waveforce loses `IntegrityScale` entirely

> *"waveforce should remove the integrity damage entirely because it can never actually
> reach a full integrity damage, so that it is not calculated in the balance formula without
> any effect."*

Deleted from `FAMILY_INTEGRITY_SCALE`. The second half of that sentence is the important
half: **a knob that does nothing in play but is still read by the pricing model is worse
than no knob**, because the weapon is charged for an effect it cannot deliver. (E3 says
`IntegrityScale` is not priced *today* — but it is on the fix list, and the moment it is,
Waveforce would have started paying for a disable that needs 5x the target's max HP.)

The alternative — granting Waveforce the `Tesla` damage type so the passive drain fires —
was deliberately NOT taken. That would give a 3/5-kinetic blend the same EMP status as a
Tesla coil, which is a design claim nobody made. If Waveforce should have an EMP role it
needs both halves, chosen on purpose.

### 2. HAZMAT and REFLECTOR are now derived from composition

> *"before when armors were multiplied a HAZMAT and REFLECTOR versus value of 50 means it
> would also half the incoming damage from those weapon types right? now that it is averaged
> it should be at what?"*

**The answer is ~10, and the reason is that averaging silently halved every overlay.**
Under multiplication a row of 50 meant exactly "half the damage", target-independent. Under
averaging the same 50 gives `(base + 50)/2` — at base 100 that is 75, a **25%** cut, not
50%. Restoring the old feel needs a row of 0, which is immunity; the window floor of 10 is
as close as the mechanic allows (a 45% cut).

So the row is now SOLVED from the reduction it should produce:

```
effective = (100 + x) / 2 = 100 - 100 R        ->        x = 100 - 200 R
```

The reference base of 100 is not an assumption — **W25 S1 pinned every family's Versus mean
to exactly 100**, so 100 *is* the average armor row a weapon writes. That is a second thing
S1 bought that was not planned for: overlay armors became solvable.

With `OVERLAY_DEPTH = 0.45`, `x = 100 - 90 x share`, and two composition tables supply the
shares — `CHEM_SHARE` (what a sealed suit stops) and `ENERGY_SHARE` (what plating stops).
Blends average their parents, so `Waveforce` (Flame + Chemical + Railgun + Laser + Tesla)
gets **both** rows automatically at 68/68, which is exactly the gap the maintainer spotted.

⚠ `ENERGY_SHARE` is deliberately **not** `PHYSICS_RANK`. That table ranks coupling to a
force FIELD, where Tesla is top at 1.00. Plating is a MIRROR: coherent light (Laser, Prism)
is the canonical case, while electrical discharge arcs and conducts rather than reflecting,
so Tesla sits at 0.60. Merging the two would have been a category error that happened to
look tidy.

| family | chem | energy | HAZMAT | REFLECTOR | suit / plating buys |
|---|--:|--:|--:|--:|---|
| Toxic | 1.00 | — | **10** | — | 45% less |
| Chemical | 0.95 | — | 14 | — | 43% less |
| Flame · Inferno | 0.70 | —·0.70 | 37 | —·37 | 32% less |
| Plasma | 0.82 | 0.45 | 26 | 60 | 37% / 20% |
| **Waveforce** | 0.36 | 0.35 | **68** | **68** | 16% / 16% |
| Quantum | 0.05 | 0.58 | — | 48 | — / 26% |
| Tesla | — | 0.60 | — | 46 | — / 27% |
| Laser · Prism | 0.15·0.10 | 1.00 | 86·— | **10** | — / 45% |
| Bullet, Cannon\*, Missile\*, Flak, Melee, Arrow, Concussion | — | — | — | — | **rows omitted** |

Rows below a 5% effect are omitted rather than written (`OVERLAY_MIN_EFFECT`): by A2 a
present row joins the average and moves the result, so a 4% row is a real-but-invisible
change that still costs a line and a reader's attention.

⚠ **This is a real balance shift for the 329 HAZMAT actors.** They used to carry a flat
`HAZMAT: 50` against *every* family, i.e. a general 25% damage reduction from all sources.
Ten kinetic families now omit the row entirely, so hazmat infantry are markedly squishier
against bullets and shells — which is the correct semantics (a sealed suit does not stop a
bullet) and is what the ruling asked for, but it is a nerf and should be watched in play.

The `_ExtraDamage` chip now reads its overlays from the same function instead of the
hand-set `REFLECTOR: 50` it carried: the chip belongs to the same family, and a hazmat suit
cannot care which warhead of a weapon hit it. A second source for one cell is the exact trap
that let Tesla's Shield be contested for months.

## F. ⚠ CONFIRMED BUG — an armor plating can make a unit take MORE damage

> *"now that I think about it would that mean that averaging can also make the unit take MORE
> damage? this is a serious concern and something you need to take a deep look into!"*

**Yes. Measured across the live matrix: 98 of 1152 cells, 8.5%, up to 1.84x MORE damage.**

The arithmetic is unavoidable: `effective = (base + plating) / 2` is an INCREASE whenever
`plating > base`. Any unit whose class armor already resists a weapon better than the plating
does is *punished for wearing the plating*. It hits heavy units hardest, because they are the
ones with low (resistant) class rows — which is precisely backwards.

Narrowing HAZMAT to the maintainer's definition (fire / chemical / radiation only — Laser,
Prism, Demolition and Concussion dropped to zero) removed **the four worst cells outright**
and cut the total to **57 (5.4%), worst case 1.43x**. That is a real improvement and it
carries its own lesson — *a share too small to matter is not harmless; averaged against a
resistant armor it INVERTS* — but 57 inversions is still a broken mechanic. **The taxonomy
fix is not sufficient. The combination rule has to change.**

### The fix already exists in the tree — for shields

```yaml
    Armor:
        RequiresCondition: !shielded        # base armor DISABLED while shielded
    Armor@shielded:
        Type: Shield
        RequiresCondition: shielded         # shield armor INSTEAD OF, not as well as
```
`mods/cameo/rules/defaults.yaml:7290`. The shield layer already does exactly what the
maintainer proposed — layer SELECTION, not combination — in pure yaml, no C# at all. The
plating armors simply never got the same treatment: they add `Armor@HAZMAT` without ever
disabling the base, so they average.

### ⚠ But the shield precedent does NOT transfer unchanged, and this is the trap

`Shield` has a row in **every one of the 94 templates**. A plating is SPARSE by design — it
only carries rows for the weapon classes it counters. Under pure layer selection, a plating
with no row for bullets leaves the armor list EMPTY, and both the engine and Cameo's override
`return 100` for an empty list. **A superheavy tank with reactive armor would take 100%
from bullets instead of ~20%.** Layer selection plus sparse rows is a catastrophic failure
mode, not a conservative one.

### Options for the combination rule

| # | rule | never increases damage? | cost |
|---|---|---|---|
| **R1** | keep averaging | ❌ 57 cells invert | none (status quo, broken) |
| **R2** | layer selection, FULL plating profiles (the shield model, literally) | ✅ | 6 platings x 94 templates = **564 hand-designed rows**, and one missing row silently returns 100 |
| **R3** | layer selection, **falling back to the base when the plating has no row** | ❌ still inverts where `plating > base` | small: one branch in Cameo's `DamageVersus` |
| **R4** | `effective = min(base, plating)` | ✅ **provably** | one new `MultiArmorCombination` mode in Cameo's own warhead — no engine edit |
| **R5** | R3 **plus a generator invariant** that a plating row may never exceed that template's lowest class-armor row | ✅ by construction | R3 + a guard, and it pushes plating values DOWN |

**Recommendation: R5**, which is the maintainer's own model made safe. It keeps the intended
semantics — *the plating is what gets hit, when it has an opinion* — while the invariant
forces R3 and R4 to agree, so the mechanic cannot invert even in principle. The invariant is
also good design on its own terms: **a plating that counters a weapon class should be better
against it than any class armor is**, which is exactly what "reactive armor is the answer to
shaped charges" means. R4 alone is the cheapest correct answer if the maintainer wants this
closed today rather than designed.

### ✅ The invariant is now enforced (2026-08-16), independently of the ruling

**AN ARMOR UPGRADE MUST NEVER INCREASE INCOMING DAMAGE.** Two changes, both compatible with
all five rules above, so neither can conflict with a later decision:

1. **`tools/audit/audit_armor_upgrade_harm.py`** (in `run_all.sh`) checks every
   (template, plating, class-armor) combination and fails on any that inverts. It encodes
   the INVARIANT rather than today's arithmetic, so if the combination rule moves to R4 or
   R5 it goes green on its own and stays useful as the thing that proves it.
2. **`overlay_rows` now lays the plating into `[VERSUS_FLOOR, min(class rows)]`** instead of
   onto a fixed scale, so the invariant holds BY CONSTRUCTION — a plating is at worst equal
   to the best thing the weapon is already resisted by, and better everywhere else. Under R4
   the clamp is simply redundant.

That took the guard from 57 violations to **0**, and it made the platings meaningfully
stronger rather than weaker, because the clamp only ever lowers a row:

| | before | after | reduction |
|---|--:|--:|--:|
| `Waveforce` HAZMAT / REFLECTOR | 68 / 68 | **38 / 37** | 16% → 31% |
| `Tesla` REFLECTOR | 46 | **24** | 27% → 38% |
| `Laser` REFLECTOR | 10 | **10** | 45% (already at the floor) |
| `Plasma` HAZMAT / REFLECTOR | 26 / 60 | **16 / 29** | 37%/20% → 42%/36% |

⚠ **This is a patch, not the fix.** It removes the defect from the values we ship; it does
not remove it from the MECHANIC. Any hand-written plating row, any new armor type added
without going through the generator, and any future actor carrying two platings at once can
reintroduce it — which is why the audit exists alongside the clamp, and why §F's R4/R5 is
still the answer.

**Why this class of bug deserves the guard.** It was invisible to everything we run: the
yaml is well-formed, every value is inside the window, the resolver is happy,
`find_empty_warhead` is 0, and the game boots — a boot gate cannot see a number that is
merely WRONG. It was invisible by inspection too, because the defect is in neither value but
in their INTERACTION: `HAZMAT: 86` is unremarkable, `Heroic: 32` is unremarkable, and their
average is a 1.84x self-inflicted damage increase.

## G. The plating taxonomy — 4 given, 2 proposed

> *"Hazmat against fire, chemical and radiation, BLAST against all the HE weapons
> like demolition, concussion etc, reflector against energy, composite against AP weapons and
> bullets ... try to find another 1 or 2 that fit the real world armors"*

| plating | counters | real-world basis |
|---|---|---|
| **HAZMAT** | Flame, Inferno, Chemical, Toxic, Cryo, Nuclear, + fire/chem blends | NBC suit, sealed overpressure hull |
| **BLAST** | Demolition, Concussion, Thermobaric, CannonHE, MissileHE, Flak, Sonic | spall liner, blast-attenuating V-hull |
| **REFLECTOR** | Laser, Prism, Plasma | ablative / mirrored coating |
| **COMPOSITE** | Bullet, Sniper, CannonAP, Railgun, Arrow | Chobham, ceramic matrix — the anti-KINETIC answer |
| **➕ Reactive** | MissileAP, and the shaped-charge half of the AT families | **ERA / slat armour.** The KE-vs-HEAT split is the actual axis real tank armour is designed around, and it is the one distinction `COMPOSITE` alone cannot express: ceramics beat penetrators, ERA beats shaped charges, and neither does the other's job. |
| **➕ Insulated** | Tesla, Storm, and the electrical share of Quantum / Waveforce | **Faraday cage / grounding mesh.** This also repairs a compromise in §D-bis: I put Tesla on REFLECTOR at 0.60 because the maintainer's ruling said "energy", while noting a mirror does not stop lightning. With `Insulated` in the set, REFLECTOR goes back to being honestly PHOTONIC (Laser/Prism 1.0, Tesla 0) and electricity gets its own real counter. |

Both additions do the same kind of work: they split a category that was hiding two different
physics behind one name. That is the test a seventh type would have to pass too — `Damping`
for Sonic and `Warding` for Magic were considered and rejected, because Sonic is already
served by `BLAST` (both are pressure) and Magic ignores armor by design.

### Consequences to decide before building

1. **Six platings x 32 families is a real matrix**, but it is generated, not hand-typed — the
   composition shares already exist and each plating reads one axis.
2. **One plating at a time, or several?** If several can stack, every combination rule above
   needs re-checking; R4 (`min`) is the only one that stays safe under stacking.
3. **The 329 HAZMAT actors and 16 REFLECTOR actors need re-tagging** to whichever plating
   their upgrade actually represents — RA2 reactive armor is arguably `Reactive`, not HAZMAT.
4. **E1 grows again**: six plating types priced at zero instead of two.

## H. The plating cycle — real-world reasoning, and what the roster can actually support

### H1 — every counter and every weakness, with its physical basis

A plating defeats a damage mechanism by a specific physical means; it is weak where that
means does nothing. Both halves are mechanisms, not flavour.

| plating | what it physically IS | COUNTERS because | WEAK TO because |
|---|---|---|---|
| **HAZMAT** | sealed, filtered, overpressured envelope + insulation | thermochemical harm arrives as an AGENT or a heat flux, and a sealed boundary keeps gas and liquid off skin/optics while insulation slows conduction | **kinetic** — a seal has no mass and no hardness; a bullet passes through a rubber suit as if it were not there |
| **COMPOSITE** | hard ceramic tiles in a ductile matrix | a kinetic penetrator is defeated by SHATTERING or eroding it before it reaches the backing plate — ceramic is harder than the rod and destroys it on contact | **shaped charge** — the jet is already liquid metal at 8 km/s; there is nothing to shatter, which is historically why ERA had to be invented on top of composite |
| **Reactive** | explosive sandwich / standoff cage that fires outward | a shaped-charge JET is disrupted by moving plate ACROSS its path, breaking the jet's continuity before it can penetrate | **blast** — ERA bricks are surface-mounted and an HE burst strips or pre-detonates them, leaving the base armour bare |
| **BLAST** | spall liner, V-hull, standoff, energy-absorbing structure | blast is an IMPULSE through the structure; you survive it by spreading it over time and area and by catching the spall the shock throws off the inner wall | **energy** — a liner absorbs mechanical impulse, and a beam delivers none; it deposits heat at a point, which a liner does nothing about |
| **REFLECTOR** | polished / ablative optical coating | radiated energy is defeated by TURNING IT AWAY before absorption — reflectivity is the whole mechanism, and ablation carries away what does couple | **thermochemical** — sustained flame and corrosives foul, soot and etch the surface, and a mirror that is no longer mirror-bright is just thin plate |

The cycle closes: `thermo → kinetic → shaped → blast → energy → thermo`. Every link is a
real defeat mechanism rather than a balance convenience, and an odd cycle cannot collapse
into two mirrored pairs.

### H2 — ⚠ MEASURED: the roster cannot support five EVEN categories

> *"try to make it balanced for amount of weapon types so each category covers about the same
> number of weapons"*

Total composition share per axis, across all 33 families:

| axis | share of the roster | families it counters |
|---|--:|--:|
| thermochemical | **27.4%** | 8 |
| kinetic | **23.4%** | 6 |
| blast | **22.7%** | 8 |
| energy | **20.1%** | 6 |
| **shaped charge** | **6.4%** | **3** |

**Four of the axes are beautifully even at 20–27%. The fifth is not, and it cannot be made
even without lying about what the weapons are** — only six families carry ANY shaped share,
and only `MissileAP` is shaped-led. Shaped charge fails the maintainer's own niche test, the
one that retired `Insulated`, `Damping` and `Warding`: *"only a few factions have tesla but
everyone has something like energy, AP, HE, fire / chemical"*.

So the honest answer to "find another 1 or 2, but keep it balanced" is: **you can have
balanced, or you can have five, not both.** Three ways out, in preference order:

1. **Ship FOUR** — HAZMAT / COMPOSITE / BLAST / REFLECTOR, at 20–27% each. The
   maintainer's original set, and the measurement says it was right. `COMPOSITE` then
   counters kinetic AND shaped (ceramic and ERA are both anti-armour), which is how a real
   modern tank is built anyway — it carries both at once.
2. **Keep five and accept `Reactive` as a SPECIALIST** — narrow but deep. It should then be
   cheaper or stronger than the other four, because it answers 6% of the roster.
3. **Five by splitting kinetic instead** — `Ballistic` (small arms, fragments, blades) vs
   `COMPOSITE` (penetrators, slugs, jets). Both real, but each lands near 12%, which trades
   one uneven category for two undersized ones.

**Recommendation: option 1.** The four-way split is what the roster actually is, and it is
the maintainer's own first instinct — *"I think those 4 seem to be good for now"*.

### H3 — corrections made to the composition table

Two families were credited to the wrong counter in the first draft:

* **`Flak` and `MissileAA` were blast-led.** Fragments are METAL MOVING FAST, not
  overpressure — which is exactly why "flak jacket" is a real garment rated in ballistic
  terms. Both are now kinetic-led (0.60 / 0.55), which moves them to `COMPOSITE`.
* **`Sonic` 0.60 → 0.70 blast.** A pressure wave IS overpressure; the energy share was
  overstated by treating "it is a wave" as "it is radiation".

## I. Priced survivability — shields and platings as effective HP (VERIFIED)

> *"since everything deals more damage to shields you can count the 200% shield strength like
> an extra 100% HP ... but right now you also made the average versus value to armor platings
> to 100 right? so it evens out"*

**Both halves verified against the shipped matrix.**

| layer | column mean | 1 point is worth | maintainer's estimate |
|---|--:|--:|---|
| `Shield` | **210.2** | **0.476 HP** | "200% shield ≈ 100% extra HP" — i.e. 0.5. **Confirmed to 5%.** |
| all five platings | **100.0** | **1.000 HP** | "it evens out" — **confirmed exactly**, by construction |

So the pricing rule is:

```
effective_HP = HP + shield_strength x (100 / mean_versus_shield)      # x0.476 today
```

and a plating contributes **nothing** to effective HP on average — it redistributes only.
That is the column law doing exactly the job it was designed for.

### ⚠ But the plating column mean of 100 is the WRONG target, and the matrix says so

A plating REPLACES the class armor, so what matters is how its column compares to the column
it displaces. Measured per class armor:

| class armor | column mean | a plating at 100 is... |
|---|--:|---|
| `Heroic` | 74.3 | **35% WORSE** |
| `Spaceship` · `Helicopter` · `Bomber` · `Fighter` | 76–80 | **25–31% WORSE** |
| `Concrete` | 97.8 | ~neutral |
| `Steel` … `None` | 102–129 | 2–22% better |

**Six of the sixteen class armors are already better than any plating**, so an aircraft or a
hero that takes a plating gets *worse* — and `td_gdi_upgrade_heavyaircraftarmorplating` is a
live aircraft plating. This is the same failure the averaging bug had, arriving by a
different route: it is not that the plating stacks badly, it is that it displaces something
better.

The fix is not to abandon the column law — the maintainer's stated purpose for it was that
platings be equally good *as each other* (*"so all weapon types combined deal the same
damage"*), which any common mean satisfies. **Lower the common mean to ~70**, just under
`Heroic`'s 74.3, and a plating becomes a genuine upgrade for every armor it can replace while
staying exactly equal across the five. The ~30% durability gain then has to be PRICED, which
is E1's job and is the correct place for it.

## E. What the balance formula still does not see

Measured against `formula.py`, `weapon_efficiency.py` and `target_model.py`.

| # | gap | why it matters | severity |
|---|---|---|---|
| **E1** | ✅ **FIXED 2026-08-17 (both halves).** Weapon side: `armor_weights()` now carries a 17th `Shield` row at its measured damage share, and `weighted_versus` iterates the weights instead of `ARMORS`. Unit side: `extract_stats.survivability()` publishes `effective_hp` for actors that SPAWN with a pool. | ⚠ **The "51% of the roster" figure was wrong** — it counted the 1592 actors carrying `Shielded`, but 1318 of those hold an EMPTY capacity behind `shieldgen >= 1`. Only **58** spawn with a pool, so baseline Shield exposure is **1.432%**, and the weapon-side correction is +0.65% (Bullet) to +3.47% (Tesla), not a repricing. The real hole is the unit side: those 58 carry **+57.8% effective HP at zero cost**. Report: `audit_survivability_pricing.py`. | ~~high~~ **done** |
| **E2** | `PhysicalState` (heat / cold / corrosion) is priced at zero — `extract_stats` contains **0** references to it. | ⚠ **"~89 live bindings" was wrong by 8×. Measured 2026-08-18: 722 bindings on 453 weapons, of which 367 are actually FIRED, carried by 578 armaments** — roughly a quarter of the damaging roster delivers a status meter for free. It is also TWO mechanisms, not one (see below), and the earlier count saw only part of one. Design work exists (`cameo-physical-state-pricing`), the extractor does not. | **high** |
| **E3** | `IntegrityScale` is priced at zero. | 1233 actors carry the pool; a disable at 50% HP is worth real money. | medium |
| **E4** | ✅ **FIXED 2026-08-17 — `K` was not damage-independent.** The `%`-twin's damage is a share of the TARGET's max HP, so it does not scale with the weapon's flat `Damage` — yet `k` folded it in as `share = ref_hp × pct_damage / 100 / flat_total`, putting `flat_total` in a DENOMINATOR. | Not a mis-price of anything shipped: `effective_per_shot = damage_total × k_context` is exact at the current Damage, and `propose_class_rebalance` never routes through K (it sums flat warheads, twins excluded). It was a **documented wrong recipe** — the inversion `Damage_required = target_dps × eff_reload / (burst × FP × K)` was stated in 6 places and is only correct at λ=1. Fix: the affine split `k_flat_context` + `pct_absolute_context`, `required_damage()`, and `dps_floor` in the ledger. Guard: `audit_k_linearity.py`. | ~~high~~ **done** |
| **E5** | Upgrades are priced at zero — there is no ΔP report, so a weapon swap is free. | The maintainer has already flagged this; it is the whole upgrade-rebalance prerequisite. | high (deferred by design) |
| **E6** | Inaccuracy and projectile speed are not priced. | A weapon that misses is worth less than one that does not; `reliability` covers spatial falloff, not aim. | medium |
| **E7** | `MinRange` is not priced. | A real artillery drawback that costs nothing. | low |
| **E8** | A5's asymmetry is undocumented in the balance docs: an omitted Versus row and a row of 100 differ for multi-armor actors. | Any future sweep that "fills in missing rows for completeness" would silently rebalance every shielded and hazmat unit. | **trap** |
| **E9** | 23 macro ladders are non-monotone — blend families average their parents and `finish_blend` never re-imposes the ordering law. | The ordering law is "the most important part"; blends quietly opt out of it. Pre-existing, unrelated to W25. | medium |
| **E10** | ✅ **FIXED 2026-08-17** alongside E1 — the comment now says what the tuple is (16 CLASS armors) and why `Shield` and the platings are layers with their own weight rather than rungs on the ladder. | It would have misled exactly the fix E1 needed, which is how it got found. | ~~trivial~~ **done** |

**E1 and E4 were fixed first** (2026-08-17), because both distorted prices *systematically*
rather than for one weapon. Both turned out to be a different size than this table first
claimed, in opposite directions, and the corrections are recorded below rather than quietly
edited away — a severity estimate that moves by 30x is itself a finding.

### E2 measured — and it is TWO mechanisms, which is why the old count was small

```
Temperature, damage-SCALED   396 bindings   PhysicalStateName + PhysicalStateScale on an AreaDamage warhead
Temperature, discrete APPLY  242 bindings   Warhead@X: ApplyPhysicalState + Amount
Corrosion,   damage-SCALED    84 bindings
                             ---
                             722 bindings on 453 weapons — 367 of them FIRED, on 578 armaments
```

⚠ **Counting only one mechanism is how "~89" happened, and I repeated the mistake mid-measurement**
— a first pass that looked only for `ApplyPhysicalState` reported 242, which is also wrong. The
damage-scaled meters (`PhysicalStateScale`, the larger half) ride on the *damage* warhead and
carry no `ApplyPhysicalState` marker at all.

Two consequences for the fix:

* **The scale units are NOT comparable across mechanisms.** `Amount` is an absolute meter delta
  per hit (`800`, `1200`, `-30000` for cryo — the sign is the direction, heat vs cold), while
  `PhysicalStateScale` is a PERCENTAGE of the damage dealt (`100`, `75`). A pricing term has to
  normalise them before it can add them up.
* **Only two states are live: Temperature and Corrosion.** Everything else in
  `PHYSICAL_STATE_SYSTEM.md` (Sonic, ArmorBreach, Hex, Knockback) is design, not shipped content,
  so E2's scope is exactly these two.

⛔ **What is still owed before the extractor can price it: what a meter is WORTH.** Recording the
bindings is mechanical and needs no ruling; converting them into a `state_w` term does — the
existing note (`cameo-physical-state-pricing`) has cryo at 0.75× as an empirical measurement, and
nothing equivalent exists for heat or corrosion. Claim: `physical_state_fired_weapons`.

### E1, as measured and fixed (2026-08-17)

**The premise was wrong, and checking it was the whole job.** "Tesla's `Shield: 400` is free
against 51% of the roster" came from counting the 1592 actors whose resolved ruleset contains a
`Shielded` trait. But `^ShieldedShieldable` (`defaults.yaml:7230`) sets
`MaxPercentageStrength: 100` together with `InitialStrength: 0`, and the regen sits behind
`RequiresCondition: shieldgen >= 1`. That is a **capacity**, not a shield: it starts empty and
stays empty until something fills it.

| bucket | actors | is it durability? | who prices it |
|---|--:|---|---|
| spawns with a pool, ungated | **58** | yes | **E1** |
| empty capacity, needs `shieldgen` | 1318 | no | nobody — correctly |
| pool behind an upgrade | ~216 | yes, but not baseline | **E5** |

The maintainer's own qualifier decides the split — *"that's only if the unit already has armor
or shield included in them"*. A shield the unit spawns with is baseline durability; one an
upgrade grants is not, and charging the base cost for it would overprice every un-upgraded
unit.

⚠ **`!disabled` is not a gate.** It is the standard not-EMP'd/not-captured guard and is TRUE on
a healthy unit. An early version of the classifier treated any `RequiresCondition` as a gate,
which put every Protoss unit (`InitialPercentageStrength: 100`, `RequiresCondition: !disabled`)
in the "needs an upgrade" bucket and reported that the roster had **no** always-on shields at
all — contradicting the maintainer, who was right. Only a POSITIVE token gates.

**The two halves, and their true sizes:**

* **Weapon side — small.** Baseline Shield exposure is **1.432%** of all roster raw damage, so
  adding the row moves a family's `versus` by **+0.65%** (Bullet, `Shield: 165`) to **+3.47%**
  (Tesla, `Shield: 369`). Correctly ordered — energy families pay most, kinetic least — but a
  correction, not a repricing. `armor_weights()` takes the share OUT of the class rows so the
  weights still sum to 1.0 and `versus` stays comparable.
* **Unit side — the real hole.** Those 58 actors hold **12 872 500 HP that is really 20 316 495
  effective HP, +57.8%, priced at zero**. Implied price change: median **×1.378**, up to
  **×1.752**. At a 200% pool the multiplier is **×2.080**, so the maintainer's "count the 200%
  shield strength like an extra 100% HP" was right to **8%**.

⚠ **The Protoss carry a 150% damage multiplier to compensate for their shields.** Pricing the
shield is the prerequisite for retiring that multiplier — they have to land in ONE pass, or the
faction is charged twice for the same thing.

**Why the weapon side stayed on the baseline world.** Counting upgrade-granted shields would
raise the Shield weight from 1.4% to roughly 30% and reprice every energy weapon. That is a
design ruling about whether K prices the baseline or the post-upgrade game, so it is left as a
one-predicate change in `shield_damage_share()` for the maintainer, not decided here.

**Platings need no weight of their own.** Every plating is upgrade-granted, so baseline
exposure is zero and they belong to E5 with the conditional shields. Their columns are also
pinned to a common mean by construction (§F), so once E5 does price them, a plating changes
*where* damage lands, not how much on average.

### E4, as measured and fixed (2026-08-17)

Two things had to be separated that the single `k` conflated, and getting the severity right
mattered as much as the fix:

* **`k` as a MEASUREMENT is sound.** `effective_per_shot = damage_total × k_context`
  reproduces the truth exactly at the weapon's current Damage, and the identity
  `k == k_flat + pct_absolute / flat_total` (checked on all 2016 concrete weapons, L2) shows
  the new split is a decomposition of the published number, not a second opinion. **No
  shipped price was wrong.** The `_Percentage`-excluding `spread_damage_sum` also keeps the
  live `propose_class_rebalance` inversion clear of K entirely.
* **`k` as a SHAPE COEFFICIENT was false**, and that is what six documents told the reader
  to invert. Measured on the worst case, `AnthraxCloudLarge` (twin = 75% of output):

  | want | old prescription | old actually delivers | error | new |
  |--:|--:|--:|--:|--:|
  | 2.0× | 354 | 1225 vs 1961 asked | **−37.5%** | 887 → exact |
  | 1.5× | 266 | 1103 vs 1471 asked | −25.0% | 532 → exact |
  | 1.0× | 177 | 981 vs 981 asked | 0.0% | 177 → exact |
  | 0.6× | 106 | 883 vs 588 asked | **+50.0%** | UNREACHABLE |
  | 0.4× | 71 | 834 vs 392 asked | **+112.6%** | UNREACHABLE |

  The old form is exact **only at λ=1**, its own fixed point — which is exactly why nothing
  caught it: every check that re-derived a weapon's current Damage passed.

**The `%`-twin is a DPS FLOOR.** This fell out of the fix and is a design fact, not a bug: a
weapon delivers `pct_absolute_context` at `Damage: 0`, so lowering flat Damage can never
price it below that. 52 weapons have a floor ≥25% of output. `required_damage()` returns
`None` there instead of a positive number, and `dps_floor` is now published per weapon so the
balance pass sees the bound before it prescribes an impossible target. To price one of these
lower, the **twin** must shrink.

**What this leaves for W18.** W18 rebases the twin's `Versus` to basis points (`×5`,
`PercentageDenominator: 10000`). That changes the twin's magnitude and therefore every
`pct_absolute` — but not the SHAPE of the model, because the affine split already puts the
twin on the correct side of the equation. Re-run `extract_stats` after W18 and the floors
move; nothing needs re-deriving.
