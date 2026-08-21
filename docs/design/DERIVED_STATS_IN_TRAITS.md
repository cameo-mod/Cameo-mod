# Derived stats — moving the audit's formulas INTO the traits

_Feasibility analysis, 2026-08-19. Maintainer: "I want the turn rate to be automatically set as
speed/5 for both the mobile trait and the turreted trait … Also the self healing, shield recharge and
the repairable traits should be scaled with the health … I want to see if everything we have already
discussed is already implemented."_

Same principle as [`UNIFIED_AREADAMAGE_WARHEAD.md`](UNIFIED_AREADAMAGE_WARHEAD.md): **the template is
the big brain, the inline entry is one brain cell.** That document does it for weapons; this one does
it for actors.

---

## 1. Status — what is actually implemented today

| rule | documented | implemented? |
|---|---|---|
| `Mobile.TurnSpeed = Speed / 5` (turreted) | DESIGN §307 | ⬜ hand-set in yaml, checked by `audit_stat_formulas` |
| `Turreted.TurnSpeed` equals it | DESIGN §307 | ⬜ hand-set |
| turretless / frontal = `2 × Speed / 5` (= Speed ÷ 2.5) | DESIGN §308 | ⬜ hand-set |
| fighters & bombers = `Speed / 15` (frontal 2×) | DESIGN §310 | ⬜ hand-set |
| helicopters & spaceships = `Speed / 5` | DESIGN §311 | ⬜ hand-set |
| self-heal `Step = HP / 2500`, infantry `HP / 1000` | DESIGN §302 | ⬜ hand-set as a FLAT `Step:` |
| shield regen = `2 × self-heal` | DESIGN §303 | 🟡 **partly — already scales with max HP** |
| **ramp-up over time** | discussed | 🟡 **BUILT, but only for ArmorPlating** |
| **per-tick resolution** instead of per-25-ticks | requested | ⛔ not implemented |
| **never heal 0 on a tick** | requested | ⛔ not implemented |
| `Repairable` = `HP / 20` | DESIGN §1318 | ⬜ hand-set |

Two pleasant surprises and one gap worth naming:

- **`GrantsShield` already scales with health.** It is a **Cameo** trait and carries
  `PercentageRegenAmount = 2` — *"Percentage OF MAX HEALTH refilled each interval"*. So "shield
  recharge should scale with health" is **already true**; what is missing is the resolution and the ramp.
- **The ramp is already built and proven** — `ArmorPlating.cs` (W21 R7):
  `rate = base * min(1, ticks_since_damage / RampTicks)`, `RampTicks = 125`, and its own `[Desc]`
  already says *"50 for infantry; shields use 250"*. The pattern exists; it simply was never applied
  to self-heal or to the shield.
- **`ChangesHealth` has `PercentageStep`** — so percentage self-heal needs no new code *in principle*.
  But it is **integer percent**, and `HP / 2500` is **0.04%**, which integer percent cannot express.
  That single fact is why self-heal needs a Cameo trait and the shield does not.

---

## 2. Turn rate — hooks exist, but they are the wrong shape

Both hooks are real:

```csharp
Mobile.cs:332    TurnSpeedModifiers = self.TraitsImplementing<ITurnSpeedModifier>()…
Turreted.cs:189  turnSpeedModifiers  = self.TraitsImplementing<ITurretTurnSpeedModifier>()…
```

⚠ **But they are INTEGER PERCENTAGE modifiers, not setters**, and that loses the value. To express
`TurnSpeed = Speed / 5` for a Speed-100 unit you want 20; from a base of 512 that is
`20 × 100 / 512 = 3%`, and 3% of 512 is **15, not 20** — a 25% error, worse at low speeds. Percentage
hooks are right for *"the Archer turns at 50% while firing"* and wrong for *"derive the absolute
value"*.

**Three routes, and the boring one is best:**

| route | exact? | risk |
|---|---|---|
| **(a) generate the values into yaml** | ✅ exact | none — no C#, reviewable in the diff, already audited |
| (b) Cameo trait reflectively setting `MobileInfo.TurnSpeed` at `IRulesetLoaded` | ✅ exact | writes a `readonly` field; load-order fragile; invisible in yaml |
| (c) shadow `Mobile`/`Turreted` in `OpenRA.Mods.Cameo` | ✅ exact | ~900-line trait forked from upstream forever |

**Recommendation: (a).** Turn rate is a *static* value with no runtime input — there is nothing to
compute per tick, so computing it at runtime buys nothing and costs precision. Generating it puts the
formula in exactly one place (the generator), makes every value exact, keeps it visible in review,
and `audit_stat_formulas` already fails when yaml drifts from the rule. That IS the "big brain in the
template" model — the same one `gen_weapon_template.py` already uses for warheads.

⚠ Routes (b) and (c) also hide the number from `extract_stats`, which reads yaml. Pricing would stop
seeing turn rate entirely.

---

## 3. Self-heal — needs one new Cameo trait, and the per-tick maths has a trap

`ChangesHealth` is `sealed` and internal in `OpenRA.Mods.Common`, so it cannot be subclassed; the
answer is a small Cameo trait (`ScaledSelfHeal`) and a yaml key swap. Fully mod-side — no `engine/`.

**What it needs:**

1. **Basis-point percentage** (`PercentageStepBasisPoints`), because `HP / 2500` = 0.04% and integer
   percent cannot hold it. 2500-HP steps make `HP / 2500` land exactly on integers, so the rate is
   clean once the unit is fine enough.
2. **Per-tick application** — `Delay: 1`, with the rate divided by 25 to keep the *same* total rate.
   The maintainer's ask is a **resolution** change, not a rate change: `1% / 25` per tick over 25
   ticks is still 1% per second, just smooth instead of a step.
3. **A remainder accumulator** — and this is the trap.

⛔ **"Always heal at least the minimum per tick" and "keep the rate exact" CONFLICT for small units.**
At 0.04% per tick a 100000-HP tank heals 40/tick — fine. A 1000-HP infantryman heals **0.4/tick**,
which integer maths floors to **0** (heals never) or, with a hard minimum of 1, to **1 — two and a
half times the intended rate**. Neither is acceptable, and infantry HP steps of 1000 mean this is the
common case, not an edge case.

**The fix is an accumulator, not a floor:** carry the fractional remainder between ticks and heal
whenever it crosses 1. The 1000-HP infantryman then heals 1 every 2.5 ticks — *exactly* the specified
rate, still visibly smooth, and never 0 forever. Recommend this over a hard minimum; a hard minimum
should exist only as a safety net for a rate that would otherwise round to literally never.

---

## 4. Shield — the smallest job of the three

`GrantsShield` is already Cameo and already percentage-of-max-HP. It needs exactly what self-heal
needs, minus the scaling that is already there:

- `RegenInterval: 25` → per tick, rate ÷ 25, same accumulator;
- basis points, for the same 0.04% reason;
- **the ramp**, copied from `ArmorPlating.RampTicks` — and `ArmorPlating`'s own `[Desc]` already
  prescribes **250 for shields**, so the number is chosen, just not wired;
- keep `DamageCooldown = 250` (a hard "no regen for N ticks after damage"); the ramp then governs how
  fast it returns to full rate afterwards. Cooldown and ramp are complementary, not duplicates.

Design rule §303 (`shield regen = 2 × self-heal`) then becomes derivable rather than typed.

---

## 5. Repairable

`Repairable`/repair rate is documented as `HP / 20` (DESIGN §1318) and is an engine trait. Same
verdict as turn rate: **generate it into yaml.** Static value, no runtime input, and pricing reads it.

---

## 6. Recommended order

1. **`ScaledSelfHeal`** (new Cameo trait): basis points + per-tick + accumulator + ramp. Nothing else
   depends on it and it proves the accumulator.
2. **`GrantsShield`**: same three changes on a trait we already own.
3. **Generator pass** for the static values — turn rates (all four cases) and repair rate — plus a
   `doc_claims` entry so drift is caught.
4. **Then the weapon half**, `UNIFIED_AREADAMAGE_WARHEAD.md` — same principle, much larger blast
   radius (3243 yaml nodes), and it shares the basis-point unit with this work. Do it after the
   accumulator pattern is proven here on something small.

⚠ Every one of these changes what units actually do in play. Each needs `extract_stats` re-run, the
ledger committed WITH the yaml, and a boot gate — and the self-heal/shield changes are **balance
changes**, so they need a maintainer order, not just this document.
