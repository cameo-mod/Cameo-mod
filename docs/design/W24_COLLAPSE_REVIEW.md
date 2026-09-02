# W24 collapse review — Blackrobe's Hydralisk finding, verified

**Auditor:** Claude Opus 5, 2026-09-02, from `claude/bot_insurance_dynamic_trait` @ `0bc327a4b`.
**Method:** every number below was re-measured in this session through `miniyaml.Ruleset` and
`percentage_damage.versus_table` (CLAUDE.md rule 8e — no hand-parsed yaml), or read out of the C#
source. Nothing here rests on the relayed summary.

> ⛔ **THIS IS A REVIEW, NOT AN AUTHORITY.** `DESIGN.md` §11b is the law, `BALANCE_PROGRAM_PLAN.md`
> §1b is the plan, `CLAUDE.md` outranks both. If this file disagrees with them, they win and this
> file gets fixed. Nothing here is applied — every fix it proposes needs a maintainer order and a
> boot gate.

**The finding under review** (Blackrobe, relayed by the maintainer): the W24 "one damage warhead"
plan, implemented as *"preserve raw total and select one canonical family"*, is unsafe; the
Hydralisk's 72k Chemical collapse preserved 4 × 18k yet substantially increased most ground
matchups and changed AA, splash and Corrosion.

---

## §0 — Verdict

**Confirmed, with a correction in Blackrobe's favour and one important piece of context they did
not have.**

| claim | verdict |
|---|---|
| Preserving summed `Damage` does not preserve effective damage | ✅ **CONFIRMED** — 1.48× mean, 0.62–2.38× per armor |
| It changed AA, splash and Corrosion | ✅ **CONFIRMED** — AA −30…−41%, splash geometry unified to the widest ring, Corrosion feed **4.00×** |
| Runtime applies `PhysicalStateName` **and** `PhysicalStates`; the audit treats them as alternatives | ✅ **CONFIRMED in the C#**, and independently counted: **156 fired weapons** — their exact number |
| The Marine comparison omitted its 31% firepower multiplier | ✅ **CONFIRMED, and understated** — the unconditional stack is **0.1876×**, not 0.31× |
| Hydralisk's existing cost is not disproven | ⚠ **CONSISTENT, NOT PROVEN** — `check_band` flags neither unit; a naive dps/cost says the opposite. See §5. |

**The context Blackrobe did not have:** sum-preservation is not an oversight. It is a *documented,
deliberate staging decision* — `BALANCE_PROGRAM_PLAN.md` §1b, "RECOMMENDATION — preserve the SUM
anyway, and let the pricing pass fix the magnitude", chosen so W24 stays a behaviour-neutral
refactor that a boot gate can actually verify.

**But that decision has an unstated precondition, and that is the real defect.** §1b justifies
sum-preservation from the **broadcast** finding: 576 of 934 multi-main weapons have every main at
the *identical damage*, a copy-paste artifact. Where the piled-up mains also share a *profile*,
summing is genuinely neutral. **HydraSpit is the other kind** — four mains at an identical 18,000
but with four *different* `Versus` ladders. For that shape, "preserve the sum" preserves the raw
number and moves the resolved one. §1b never separates the two shapes, so the rule is applied to
both.

---

## §1 — The measurement (HydraSpit)

`mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml:55`. Four damage mains, four percentage
twins:

| warhead | type | Damage | Spread | mean Versus |
|---|---|--:|--:|--:|
| `LightChemicalWeapon` | AreaDamage | 18,000 | 350 (Falloff 99/66/33) | 0.640 |
| `LightMissile` | SpreadDamage | 18,000 | 200 | — |
| `SmallArms` | SpreadDamage | 18,000 | 100 | — |
| `ArrowWeapon` | SpreadDamage | 18,000 | 70 (7-step falloff) | — |

Comparison excludes `HAZMAT` and `Shield`, for the reasons `measure_retrofit_gap.py` already
records: HAZMAT is a flat-50 immunity flag in every family and Shield is the W21 health layer, and
either one in the mean makes the rescale over-pay.

```
raw summed Damage                     72,000
mean EFFECTIVE damage now             46,080     (over the 20 real armor rows)
mean Versus of ^Warhead_Chemical_Light 0.950

NAIVE collapse, keep the raw 72,000:  68,400 mean effective  =  1.48x
CORRECT collapse Damage:              48,500 (preserves mean effective exactly)
```

⭐ **The 1.48× is not a new discovery — the repo already measures it.**
`tools/balance/measure_retrofit_gap.py` reports `^LightChemicalWeapon → ^Warhead_Chemical_Light`
at ratio **1.484**, and its docstring already states the rule: *"`Damage` must be divided by it to
preserve resolved behaviour."* My independent calculation arrives at the same 1.484. The retrofit machinery
knows; the collapse rule does not use it.

### Per-armor, both ways

| armor | now | naive 72k | ratio | corrected 48.5k | ratio |
|---|--:|--:|--:|--:|--:|
| ARMOR | 72,000 | 50,400 | 0.70 | 33,950 | 0.47 |
| BLAST | 72,000 | 51,840 | 0.72 | 34,920 | 0.48 |
| Bomber | 38,520 | 36,000 | 0.93 | 24,250 | **0.63** |
| COMPOSITE | 72,000 | 44,640 | 0.62 | 30,070 | 0.42 |
| Concrete | 25,560 | 57,600 | 2.25 | 38,800 | 1.52 |
| Fighter | 39,600 | 34,560 | 0.87 | 23,280 | **0.59** |
| Flak | 51,480 | 96,480 | 1.87 | 64,990 | 1.26 |
| Heavy | 41,760 | 99,360 | **2.38** | 66,930 | 1.60 |
| Helicopter | 37,440 | 38,880 | 1.04 | 26,190 | **0.70** |
| Heroic | 38,520 | 84,960 | 2.21 | 57,230 | 1.49 |
| Light | 39,600 | 90,000 | 2.27 | 60,625 | 1.53 |
| Medium | 40,680 | 93,600 | 2.30 | 63,050 | 1.55 |
| None | 51,480 | 82,800 | 1.61 | 55,775 | 1.08 |
| Plate | 51,480 | 101,520 | 1.97 | 68,385 | 1.33 |
| REFLECTOR | 72,000 | 69,120 | 0.96 | 46,560 | 0.65 |
| Scout | 38,520 | 85,680 | 2.22 | 57,715 | 1.50 |
| Spaceship | 35,280 | 43,200 | 1.22 | 29,100 | **0.82** |
| Steel | 34,200 | 54,720 | 1.60 | 36,860 | 1.08 |
| Superheavy | 42,840 | 100,800 | 2.35 | 67,900 | 1.58 |
| Wood | 26,640 | 51,840 | 1.95 | 34,920 | 1.31 |
| | | **min 0.62 · max 2.38 · median 1.74** | | **min 0.42 · max 1.60 · median 1.17** | |

⛔ **Correcting the magnitude does not rescue the matchups.** Even at the right total, the spread
stays 0.42×–1.60×. That is structural, not a tuning error: **a four-warhead stack's profile is the
SUM of four ladders, which is flatter than any one of them.** No single warhead can reproduce
"tilted where all four templates had an opinion, flat 100 where none of them did."

⚠ And the flat-100 rows are the tell. `ARMOR`, `BLAST`, `COMPOSITE` and `REFLECTOR` read exactly
72,000 today because **no** legacy template declares them, so each of the four mains defaults to
100 and the weapon deals 4× its nominal damage there. That is an accident of the pileup, not a
design — and it is an argument *for* the retrofit, since the `^Warhead_*` families do declare all
20 rows. (`BLAST` is on zero actors; `ARMOR` 38, `REFLECTOR` 32, `COMPOSITE` 16.)

### The three side effects Blackrobe named, quantified

* **AA.** All four mains are `ValidTargets: Ground, Water, Air`, and the Hydralisk is the Zerg
  anti-air infantry (`^AntiTankAntiAirInfantryTemplate`). At the *corrected* magnitude it still
  loses **37% vs Bomber, 41% vs Fighter, 30% vs Helicopter, 18% vs Spaceship**. A collapse that
  is neutral on average is a large nerf to the unit's actual job.
* **Splash.** Today the four mains carry `Spread` 350 / 200 / 100 / 70 with different falloff
  curves, so only a quarter of the damage lands at the widest radius. One Chemical warhead gives
  **all** of it the 350 ring — a straight AoE upgrade the sum-preservation rule cannot see.
* **Corrosion.** The meter is fed by damage dealt × `PhysicalStateScale`, and only the Chemical
  warhead carries it. Feed goes from `18,000 × 0.640 = 11,520` to `48,500 × 0.950 = 46,075` per
  shot — **4.00×**.
* **Percentage floor** (Blackrobe's "percentage runtime", made concrete). The weapon carries
  **four** standalone `AreaDamagePercentage` twins at `Damage: 1`, i.e. four armor-scaled
  percent-of-max-HP hits, together ~11% of the weapon's modelled output
  (`weapon_efficiency.py HydraSpit`: four `pct_standalone` rows at share 0.028). Collapsing 4 → 1
  removes three of them. That loss is **proportional to target max HP**, so it falls hardest on
  exactly the big targets an anti-armour unit exists to shoot.

---

## §2 — Why the guard did not catch it

`tools/audit/review_resolve_diff.py` is the instrument the board names for verifying a retrofit.
Its own docstring (lines 8–13) says it compares *"the multiset of offensive-warhead **Damage**
values (the 'Damage verbatim' law)"* and that *"inherit repoints, **new-template Versus tables**
… are NOT flagged."*

**So the tool that certifies a collapse as behaviour-neutral is, by design, blind to the only
thing that changed.** Blackrobe's "it needs resolved per-armor damage as a first-class comparison"
is not a nice-to-have; it is the missing half of the existing gate.

---

## §3 — The physical-state double-binding

`OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs:504-515`:

```csharp
if (!string.IsNullOrEmpty(PhysicalStateName) && PhysicalStateScale != 0)
    ApplyOneState(victim, firedBy, PhysicalStateName, ScaleDamage(damage, PhysicalStateScale));

foreach (var kv in PhysicalStates)
    if (kv.Value != 0)
        ApplyOneState(victim, firedBy, kv.Key, ScaleDamage(damage, kv.Value));
```

The field's own `[Desc]` (line 142) says the map is *"applied **IN ADDITION** to the single
PhysicalStateName/Scale above."* Both run. Naming the same state in both forms applies it twice.

`tools/audit/audit_physical_state_warheads.py` models them as alternatives:
`scaled_states()` returns a **set** (union — a state in both forms counts once) and
`state_scale()` returns the singular value and short-circuits, never adding the map's.

**Independently measured over the resolved ruleset:**

| | |
|---|--:|
| warheads naming the same state in both forms, both scales non-zero | **216** |
| distinct weapons | 172 |
| **of which fired by an actor's armament** | **156** ← Blackrobe's exact number |
| duplicate bindings counted on fired weapons only | 201 (they said 198 — 1.5% apart, likely a different "fired" definition) |
| by state | Corrosion 212, Temperature 4 |

Examples: `120mm_td`, `25mm`, `25mmWaveforce`, `ArtilleryShellUpgrade`, `AsianChemical` — the
pattern sits on the `…ChemicalWeaponPercentage` warheads.

⚠ This is a **live gameplay defect independent of W24**: ~200 warheads push their Corrosion meter
at double the intended rate, and the audit says PASS.

---

## §4 — The Marine firepower omission

Unconditional `FirepowerMultiplier` traits — no `RequiresCondition`, so always on:

| unit | unconditional stack | components |
|---|--:|---|
| `terran_marine` | **0.1876×** | GlobalBuffs 50 · InfantryBuff 110 · AntiTankAntiAirInfantryBuff 110 · **TripleShot 31** |
| `zerg_hydralisk` | **0.5990×** | GlobalBuffs 50 · InfantryBuff 110 · AntiTankAntiAirInfantryBuff 110 · (bare) 99 |
| `zerg_spithid` | 0.6655× | GlobalBuffs 50 · InfantryBuff 110 · ScoutInfantryBuff 110 · PromotionUnit 110 |
| `zerg_zergling` | 1.0312× | GlobalBuffs 50 · InfantryBuff 110 · MeleeBuff 125 · zergling 150 |

The Marine's `TripleShot: 31` pairs with `Burst: 3` — it is the burst compensator. Blackrobe is
right that omitting it invalidates the comparison, and the real gap is larger than "31%": the two
units differ by **3.19×** in unconditional firepower scaling before a single damage number is read.

⚠ Note for the pricing pass: W17 retired `FirepowerMultiplier` as a pricing knob (`apply_balance`
cannot write it; `decompose_dps` always solves at `fp = 1.0`). These unconditional stacks are
therefore *invisible to the price* while being fully live in play. That is a bigger problem than
one bad comparison, and it belongs on the board.

---

## §5 — The cost claim, honestly

Naive effective-DPS per credit, including the unconditional stack above:

| unit | cost | HP | mean effective dmg/shot | eff DPS | DPS/cost | HP/cost |
|---|--:|--:|--:|--:|--:|--:|
| `terran_marine` | 689 | 41,000 | 34,315 | 18,564 | **26.9** | 59.5 |
| `zerg_hydralisk` | 3,314 | 80,000 | 43,100 | 43,025 | **13.0** | 24.1 |

That looks like the Hydralisk is priced ~2× *worse* per point of DPS — which contradicts
"independently matches Marine". **But the naive ratio is not the project's instrument** and I am
not treating it as a refutation: it ignores range, footprint, target coverage, overkill and the
percentage floor, all of which `weapon_efficiency.py`'s K and W5 context factors carry.

The instrument that exists says Blackrobe is fine: `tools/balance/check_band.py` flags neither
`zerg_hydralisk` nor `terran_marine` as a band outlier. ⚠ With the caveat the tool prints itself —
**0 class anchors are signed off**, so no price in the tree is final and `check_band` is measuring
against unapproved anchors.

**Recorded as unresolved.** Neither "matches" nor "does not match" is established here.

---

## §6 — What should change (proposals, none applied)

1. **Split the collapse rule by SHAPE, not by damage equality.** §1b's broadcast finding counts
   weapons whose mains share a *Damage*. The safe case is weapons whose mains share a *profile*.
   Classify every multi-main weapon into:
   * **true broadcast** — all mains identical in Damage *and* Versus ⇒ sum-preservation is
     genuinely neutral, keep §1b's rule;
   * **profile pileup** — equal Damage, different Versus (HydraSpit) ⇒ sum-preservation moves
     resolved damage; rescale through `measure_retrofit_gap`'s mean ratio in the same commit;
   * **real multi-warhead design** ⇒ needs a maintainer call per weapon.
2. **Extend `review_resolve_diff.py` with a resolved per-armor comparison.** Same weapon, before
   and after, `Damage × Versus` for all 20 rows, plus the AA rows, the widest `Spread`, the
   physical-state feed and the standalone percentage count — reported as a table with a
   configurable tolerance, not silently dropped as "intended change". This is the gate the current
   one cannot be.
3. **Fix `audit_physical_state_warheads.py` to model the two forms as ADDITIVE** and fail on the
   ~200 duplicate bindings. Independent of W24 and cheap.
4. **Put the unconditional `FirepowerMultiplier` stacks on the board.** W17 removed them from
   pricing while they remain live in play; `audit_power_budget` already reports 790 units over the
   2.0× budget, which is the same debt from the other end.
5. **Leave HydraSpit alone until 1–2 land.** Blackrobe's "the existing weapon is the better result
   for now" is the right call: a collapse verified only on raw Damage cannot be shown to be safe
   for this weapon, and the pricing pass that §1b defers to has not run.

---

## §7 — Reproduce every number here

```sh
python tools/balance/weapon_efficiency.py HydraSpit      # the four mains + four pct twins, K = 0.551
python tools/balance/measure_retrofit_gap.py             # ^LightChemicalWeapon -> Chemical_Light = 1.484
python tools/balance/check_band.py                       # neither hydralisk nor marine is flagged
python tools/audit/audit_physical_state_warheads.py      # PASS today — the point of §3
```

The per-armor tables and the duplicate-binding census were produced by short scripts over
`miniyaml.Ruleset` + `percentage_damage.versus_table`; §6.2 and §6.3 turn both into permanent
tools so they never have to be re-derived by hand.

---

## §8 — ⛔ MAINTAINER RULING, 2026-09-02 — this review's §6 is SUPERSEDED in part

> *"Don't worry about it, I will review all factions manually one by one actor before we release
> anything. For now it's more important to reduce everything down to a single warhead and we can
> then make new warheads. For the hydralisk I'm thinking about a new BulletChem that is like
> Bullet x Chemical so it's more similar to what it was before but more damage against infantry
> and aircraft with a little bit damage against tanks from the chemical side."*

**What this settles.** The balance drift this review measured is **accepted**, because a manual
per-actor pass stands behind it. §1b's "preserve the SUM and let the pricing pass fix the
magnitude" therefore stands as written — it is now backed by a human review, not only by the
pricing pass. **Structure first, magnitudes later, new families after.**

| this review's proposal | status after the ruling |
|---|---|
| §6.1 split the collapse rule by shape | ⛔ **NOT NEEDED for magnitude.** Sum-preservation is ruled fine. The shape classifier may still be worth having as a REPORT, so the manual review knows which weapons changed most — but it is no longer a gate. |
| §6.2 resolved per-armor comparison in `review_resolve_diff` | ✅ **STILL WANTED** — it is what makes the manual review cheap. It stops being a blocker and becomes a worksheet. |
| §6.3 fix `audit_physical_state_warheads` to model the two forms as ADDITIVE | ✅ **UNAFFECTED** — a live defect on ~200 warheads, nothing to do with W24. |
| §6.4 unconditional `FirepowerMultiplier` stacks on the board | ✅ **UNAFFECTED**. |
| §6.5 leave `HydraSpit` alone | ⛔ **SUPERSEDED** — it collapses onto the new `BulletChem` family. |

### The new family

`^Warhead_BulletChem_{Light,Medium,Heavy}` — the **bullet-delivery member of the Chem set**, next
to `CannonChem` and `MissileChem`. Generated, not hand-written: one `BLEND_FAMILIES` entry
(`["Bullet", "Chemical"]`) plus `--all` regenerate. Waiting on a boot machine as
[`../patches/01_bulletchem_hydraspit.patch`](../patches/README.md), with its full verification
table there.

Shape falls out of the machinery and is unique: radius = geometric mean of Bullet 100 and Chemical
1100 = **332**, falloff `100, 82, 61, 38, 0`. Nearest neighbour `BulletFire` (346,
`100, 83, 64, 43, 0`) differs on both axes, so `audit_family_uniqueness` stays OK.

Corrosion follows the **Chem** convention (`{Light 20, Medium 33, Heavy 50}`), not the flat
`_m(0.50)` the other Bullet blends use. The two agree at Heavy — Chemical's full 100 over 2
parents — so the ramp is the same per-parent-average rule with a level curve, and being the third
member of the Chem set outranks being the fifth member of the Bullet set. ⚠ Flagged rather than
assumed; say the word and it becomes flat 50.

### `HydraSpit` collapsed onto it — measured

Against the 4-warhead stack that ships today, and against the pure-`Chemical` collapse this
review was written about:

| armor | now | BulletChem @ raw 72,000 | ×now | Chemical @ 72,000 | ×now |
|---|--:|--:|--:|--:|--:|
| None | 51,480 | 137,520 | **2.67** | 82,800 | 1.61 |
| Scout | 38,520 | 105,120 | **2.73** | 85,680 | 2.22 |
| Flak | 51,480 | 111,600 | 2.17 | 96,480 | 1.87 |
| Light | 39,600 | 90,720 | 2.29 | 90,000 | 2.27 |
| Fighter | 39,600 | 67,680 | **1.71** | 34,560 | 0.87 |
| Bomber | 38,520 | 45,360 | **1.18** | 36,000 | 0.93 |
| Helicopter | 37,440 | 38,160 | 1.02 | 38,880 | 1.04 |
| Spaceship | 35,280 | 32,400 | 0.92 | 43,200 | 1.22 |
| Heavy | 41,760 | 65,520 | 1.57 | 99,360 | 2.38 |
| Superheavy | 42,840 | 57,600 | 1.34 | 100,800 | 2.35 |
| | | **min 0.52 · max 2.78 · median 1.60** | | min 0.62 · max 2.38 · median 1.74 | |

⭐ **The order's intent is confirmed on every row it names.** Against the Chemical collapse:
infantry up hard (`None` 2.67 vs 1.61, `Scout` 2.73 vs 2.22), aircraft up (`Fighter` 1.71 vs 0.87,
`Bomber` 1.18 vs 0.93), tanks kept to *"a little bit"* (`Heavy` 1.57 vs 2.38, `Superheavy` 1.34 vs
2.35) while staying well above what pure Bullet would give (Bullet's own Heavy row is 59).

⚠ **Two air rows move the wrong way and need a ruling before the repoint.** `Helicopter` is flat
(1.02 vs 1.04) and `Spaceship` drops (0.92 vs 1.22). Both parents are weak there — Bullet 66/53,
Chemical 54/60 — so no weighting of the two can lift them. Only a third, air-tilted parent could
(`MissileAA` is 177/191 on those rows; `PhotonCannon` already uses it as a third parent).

⚠ At the raw 72,000 the collapse is a **1.46× mean buff**, per the ruling. A mean-preserving
`Damage` would be **49,200** if it is ever wanted.

### §8.1 — RULED and APPLIED (2026-09-02): air accepted as-is, HydraSpit repointed

Maintainer: **accept the air rows as the blend produces them**, and **repoint `HydraSpit` in the
same patch**. Done — [`../patches/README.md`](../patches/README.md)
`01_bulletchem_hydraspit.patch`. `HydraSpit` now resolves to ONE damage main
(`AreaDamage`, 72,000) on `^Warhead_BulletChem_Light`, plus `^Projectile_Chem_Light` and
`^Effect_Chem_Light`.

⚠ **Projectile layer is `^Projectile_Chem_Light`, not `^Projectile_Bullet_Light`.** The Bullet
projectile family is HITSCAN (`InstantHitWithFakeBullets`) — taking it would have deleted the
visible travelling spore. The Chem projectile is `Projectile: Bullet`, and the weapon keeps
`scmspore`, `Speed 2500`, `Width 25`, its contrails and `TrailImage` as local overrides.

⭐ **Two of this review's own predictions came out BETTER than forecast**, because `BulletChem` is
a bullet-sized blend rather than a chemical one:

| §1 predicted, for a Chemical collapse | what BulletChem actually does |
|---|---|
| splash: all damage onto the 350 ring | **radius 220** (`Spread 55`, 5-step falloff) — TIGHTER than the old stack's 700 (Chemical) and 420 (Arrow) |
| Corrosion feed ×4.00 | **×1.17** (11,520 → 13,493) — the Chem set's Light rung scales Corrosion at 20%, not 100% |

⚠ **And one moved harder than forecast: the percentage half, ×9.6.** §1 counted four standalone
`AreaDamagePercentage` twins as ~11% of modelled output. Resolved, they are `Damage: 1` against
Versus rows of 11 / 2 / 16 / 16 — **0.45%** of max HP vs `None`. The family fold at Damage 72,000
gives 3,600 basis points × `PercentageVersus` = **4.32%**. Both are the same "1% per 2,000 damage"
convention; the hand-typed twins simply never tracked the weapon's real damage, which is the exact
drift `PercentageScale` was introduced to end (`AreaDamageWarhead.cs:99-110`). A local
`PercentageScale: 1042` would reproduce the old floor if that is wanted.

### §8.2 — ⛔ THE FOLD WAS TRIED ONCE BEFORE, AND REVERTED

Found while regenerating the composite manifest, not by searching for it.
`tools/audit/intentional_composites.py` held `HydraSpit` under *"maintainer-approved role blend"*
with this rationale, already in the tree before Blackrobe raised anything:

> *"Restore the pre-PR-287 Hydralisk profile after the Chemical-Light fold raised real ground
> damage by roughly 1.6x to 2.38x and quadrupled the flat corrosion feed. Preserve the exact
> four-part behavior."* — review reference: *"Maintainer regression report: Hydralisk was not
> previously this strong"*

⭐ **That is this review's §1 measured independently, months earlier, by someone else.** My numbers
were 1.57×–2.38× on ground and ×4.00 on Corrosion; the quarantine says *"roughly 1.6x to 2.38x"*
and *"quadrupled"*. Two separate measurements of the same fold agreeing to two significant figures
is the strongest evidence in this document — and it means the W24 collapse rule has now produced
the same regression twice on the same weapon.

The 2026-09-02 ruling **supersedes that quarantine**, and the patch removes it: the collapse now
targets `BulletChem`, not `Chemical`, which is the difference the ruling turns on. If the Hydralisk
reads as too strong again in play, this is the decision to revisit and this is where the history
is.
