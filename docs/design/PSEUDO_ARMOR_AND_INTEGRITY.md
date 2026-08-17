# HAZMAT / REFLECTOR / Integrity — measured mechanics, and what to do about them

**Status: RESEARCH + OPTIONS. No mechanic changed yet.** Three maintainer questions of
2026-08-16, answered from the artifacts rather than from the design docs:

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

⚠ Whatever is chosen, note the invariant that should be written into DESIGN.md regardless:
**an armor upgrade must never increase incoming damage.** Nothing currently enforces it, and
nothing would have caught these 98 cells — no audit, no test, and the boot gate cannot see a
number that is merely wrong.

## G. The plating taxonomy — 4 given, 2 proposed

> *"Hazmat against fire, chemical and radiation, BlastProtection against all the HE weapons
> like demolition, concussion etc, reflector against energy, composite against AP weapons and
> bullets ... try to find another 1 or 2 that fit the real world armors"*

| plating | counters | real-world basis |
|---|---|---|
| **HAZMAT** | Flame, Inferno, Chemical, Toxic, Cryo, Nuclear, + fire/chem blends | NBC suit, sealed overpressure hull |
| **BlastProtection** | Demolition, Concussion, Thermobaric, CannonHE, MissileHE, Flak, Sonic | spall liner, blast-attenuating V-hull |
| **REFLECTOR** | Laser, Prism, Plasma | ablative / mirrored coating |
| **Composite** | Bullet, Sniper, CannonAP, Railgun, Arrow | Chobham, ceramic matrix — the anti-KINETIC answer |
| **➕ Reactive** | MissileAP, and the shaped-charge half of the AT families | **ERA / slat armour.** The KE-vs-HEAT split is the actual axis real tank armour is designed around, and it is the one distinction `Composite` alone cannot express: ceramics beat penetrators, ERA beats shaped charges, and neither does the other's job. |
| **➕ Insulated** | Tesla, Storm, and the electrical share of Quantum / Waveforce | **Faraday cage / grounding mesh.** This also repairs a compromise in §D-bis: I put Tesla on REFLECTOR at 0.60 because the maintainer's ruling said "energy", while noting a mirror does not stop lightning. With `Insulated` in the set, REFLECTOR goes back to being honestly PHOTONIC (Laser/Prism 1.0, Tesla 0) and electricity gets its own real counter. |

Both additions do the same kind of work: they split a category that was hiding two different
physics behind one name. That is the test a seventh type would have to pass too — `Damping`
for Sonic and `Warding` for Magic were considered and rejected, because Sonic is already
served by `BlastProtection` (both are pressure) and Magic ignores armor by design.

### Consequences to decide before building

1. **Six platings x 32 families is a real matrix**, but it is generated, not hand-typed — the
   composition shares already exist and each plating reads one axis.
2. **One plating at a time, or several?** If several can stack, every combination rule above
   needs re-checking; R4 (`min`) is the only one that stays safe under stacking.
3. **The 329 HAZMAT actors and 16 REFLECTOR actors need re-tagging** to whichever plating
   their upgrade actually represents — RA2 reactive armor is arguably `Reactive`, not HAZMAT.
4. **E1 grows again**: six plating types priced at zero instead of two.

## E. What the balance formula still does not see

Measured against `formula.py`, `weapon_efficiency.py` and `target_model.py`.

| # | gap | why it matters | severity |
|---|---|---|---|
| **E1** | **`Shield`, `HAZMAT` and `REFLECTOR` are priced at ZERO.** `target_model.ARMORS` is the 16 canonical types only, so `K` never sees them. | Tesla's `Shield: 400` against **51% of the roster** is free, and the S1/Shield rebuild just made that row far more meaningful. HAZMAT covers 11% of actors. This is the biggest hole, and it grew today. | **high** |
| **E2** | `PhysicalState` (heat / cold / corrosion) is priced at zero — `extract_stats` reads no such field. | ~89 live bindings deliver a real effect for free. Design work exists (`cameo-physical-state-pricing`), the extractor does not. | **high** |
| **E3** | `IntegrityScale` is priced at zero. | 1233 actors carry the pool; a disable at 50% HP is worth real money. | medium |
| **E4** | **`K` mixes two units.** `avg_versus` deliberately excludes the `_Percentage` twins, but `k` sums over ALL parts including them — and until W18 the twin's `Versus` IS its magnitude, on a 16-wide window. | The twin's contribution to `K` is on a different scale from the main's. W18 fixes the unit; until then this is a systematic distortion of every %-carrying weapon's price. | **high** |
| **E5** | Upgrades are priced at zero — there is no ΔP report, so a weapon swap is free. | The maintainer has already flagged this; it is the whole upgrade-rebalance prerequisite. | high (deferred by design) |
| **E6** | Inaccuracy and projectile speed are not priced. | A weapon that misses is worth less than one that does not; `reliability` covers spatial falloff, not aim. | medium |
| **E7** | `MinRange` is not priced. | A real artillery drawback that costs nothing. | low |
| **E8** | A5's asymmetry is undocumented in the balance docs: an omitted Versus row and a row of 100 differ for multi-armor actors. | Any future sweep that "fills in missing rows for completeness" would silently rebalance every shielded and hazmat unit. | **trap** |
| **E9** | 23 macro ladders are non-monotone — blend families average their parents and `finish_blend` never re-imposes the ordering law. | The ordering law is "the most important part"; blends quietly opt out of it. Pre-existing, unrelated to W25. | medium |
| **E10** | `target_model.ARMORS`' comment says "the 16 canonical armor types + Shield" — the tuple has no Shield. | Stale comment that would mislead exactly the fix E1 needs. | trivial |

**The two I would fix first are E1 and E4**, because both distort prices *systematically*
rather than for one weapon, and both are now larger than they were a week ago — E1 because
the Shield ladder went from a near-constant 110..140 to a designed 100..400, and E4 because
every family now has a normalised main warhead sitting next to an un-normalised twin.
