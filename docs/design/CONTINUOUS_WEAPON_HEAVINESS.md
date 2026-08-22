# Continuous weapon heaviness — replace the level ladder with one scalar

**Status:** design proposal, measured but not implemented. No yaml or C# changed yet.
**Date:** 2026-08-22
**Supersedes the plan to generate intermediate level templates** (`LightMedium`, `MediumHeavy`, …).

---

## 1. The problem this solves

Two standing laws collide:

- **The 3-way split** (`WEAPON_3WAY_SPLIT.md`): a weapon is ONE warhead + ONE projectile + ONE effect.
- **The Tier↔WeaponClass law** (`weapon_classes.yaml` header, memory `cameo-tier-weaponclass-law`):
  a unit sitting *between* tech tiers carries **two adjacent-level warheads** —
  `Bullet_Light + Bullet_Medium`.

A between-tier unit cannot satisfy both. 102 weapons are in that state today.

⚠ **The mix is not merely inelegant — it does not deliver what it promises.** Two warheads'
damage is **added**, not interpolated. Measured against the median single-warhead damage of the
same family:

| | weapons |
|---|--:|
| mix lands **between** its two levels (as intended) | 55 |
| mix **exceeds the higher level outright** | 33 |
| below the lower level | 3 |
| no comparable baseline | 11 |

Totals range **0.27× to 6.00×** the higher level. A third of "between-tier" units are simply
*stronger than the tier above them* — the opposite of the intent.

### 1.1 Why generating intermediate templates is the wrong fix

The obvious repair — generate `^Warhead_Bullet_LightMedium` — multiplies badly:

| | today | + intermediate rungs | + planned crossover families |
|---|--:|--:|--:|
| families | 40 | 40 | ~100 |
| rungs each | 3–4 | 6 | 6 |
| **templates** | **126** | **240** | **~600** |

Every new crossover family (`MissileTesla`, `MissileHE`, `BulletThermobaric`, …) multiplies by
the rung count. ~600 near-identical hand-maintainable Versus tables is not a system.

---

## 2. The key measurement: a "level" is already a pure transform

The level does **not** encode an independent armor profile. It applies a **uniform additive
offset** to the Versus table, and a **fixed ratio** to Spread. Measured across all 40
`^Warhead_<Family>_<Level>` templates in `mods/cameo/weapons/weapons.yaml`.

### 2.1 Versus — additive offset

```
Bullet          Light  Medium  Heavy      step L->M   step M->H
  Superheavy        1       5     10          +4          +5
  Heavy             3       7     12          +4          +5
  Concrete          4       8     13          +4          +5
  None             16      20     25          +4          +5
  Shield           17      25     35          +8         +10     (2x)
  ARMOR            70      70     70           0           0     (plating: never shifts)
  BLAST            69      69     69           0           0
  COMPOSITE        43      43     43           0           0
```

Generalised:

```
Versus(armor, h) = base(armor) + offset(h)

  offset:   0 at Light,  +4 at Medium,  +9 at Heavy   (cumulative)
  plating (ARMOR/BLAST/COMPOSITE/HAZMAT/REFLECTOR):  always 0
  Shield:   2x the offset
```

**Conformance: 39 of 40 families.** `CannonChem` and `MissileChem` follow the pattern exactly
plus ONE extra family-specific entry (`+13`/`+17`). Only **`Storm`** is genuinely irregular —
every entry carries its own delta — and it is already a hand-tuned special case in the generator.

### 2.2 Spread — fixed ratio

Across the 35 families with three measurable rungs, the ratio
`(Spread_Medium / Spread_Light, Spread_Heavy / Spread_Light)` is:

| ratio | families |
|---|--:|
| (1.50, 2.00) | 22 |
| (1.51, 2.00) | 4 |
| (1.49, 1.98) / (1.51, 2.02) / (1.50, 2.01) / … | 9 (rounding) |

So `Spread(h) = Spread_base × (1 + 0.5·h)` with `h = 0, 1, 2` for Light/Medium/Heavy. Clean and
continuous.

### 2.3 Damage — currently carries no level signal at all

Every one of the 40 templates declares the same `Damage: 2000`. That is a **convention**, not a
bug: the template holds the SHAPE, the weapon holds the MAGNITUDE via the WeaponClass scalar.
See §5 — the effective ladder is broken and must be fixed before any of this lands.

---

## 3. The proposal

**One warhead template per family. One continuous `Heaviness` scalar per weapon.**

```
templates:  40 today, ~100 once the crossover families exist — and it NEVER multiplies
```

`h` is continuous, so any blend is expressible — 1/3 Light + 2/3 Medium, or anything else — which
is what the level ladder was reaching for and could never do.

### 3.1 Where h comes from — it is already computed

`tools/balance/tier_chain.py` already exists and is validated:

- `TierChain.chain_cost(actor)` → `C`, total cost of the unique building prerequisite chain.
- `tier_multiplier(C) = 1 / (1 + (C - B) / S)`, `B = 9500` (T1 median chain), `S = 8250`
  (T4 median − B), clamped `[0, 1]`.
- `extract_stats.py` **already stores `tier_chain_cost` and `tier_multiplier` for every buildable
  actor** in `docs/balance/derived/*.json`, and `effective_tier()` preserves manual
  `design.tech_tier` overrides.

Verified examples from `tier_chain_validation.md`: `td_nod_lasertrooper` → C = $27,000,
f = 0.3204; `wc2_orcs_deathknight` → C = $15,000.

⚠ **Direction:** `f(C)` *falls* as tech deepens (it is a cost discount). Heaviness must *rise*.
So `h` is derived from `1 − f(C)`, not `f(C)`.

Fitting the three known rungs (`f ≈ 1.00 / 0.80 / 0.60` → `offset 0 / 4 / 9`):

```
offset(C) ≈ 22 × (1 − f(C))        -> 0.0, 4.4, 8.8      (needs proper calibration, §6)
```

This makes weapon heaviness and tier pricing read off the *same* number, so they cannot drift.

### 3.2 Where h is applied — C#, not generated yaml

Generating the resolved table per weapon would write 2325 × 24 Versus entries into yaml and
defeat the entire purpose. Instead `AreaDamageWarhead` gains a `Heaviness` int applied at Versus
lookup and Spread computation.

**Precedent:** the AreaDamage fold already added `PercentageScale` as exactly this kind of
per-weapon integer interpreted by Cameo-owned C#. No engine change is needed — `AreaDamageWarhead`
lives in `OpenRA.Mods.Cameo`.

### 3.3 What this buys

- **One warhead per weapon** → the 3-way split holds with no permanent audit carve-out.
- The tier law's intent is preserved and, for the first time, actually delivered.
- The **33 overshooting weapons are fixed by construction** — interpolation cannot exceed its
  endpoints, whereas addition always could.
- Rock-paper-scissors across the tech tree becomes continuous rather than snapping between three
  buckets: a T1 weapon's small offset meets T1 armor low on the same ladder.
- ~600 future templates collapse to ~100.

---

## 4. What must NOT be lost

- `Storm` is genuinely irregular and needs an explicit exception or a hand-authored table.
- `CannonChem` / `MissileChem` need their one extra entry preserved.
- The **plating** entries (ARMOR, BLAST, COMPOSITE, HAZMAT, REFLECTOR) must keep offset 0 — they
  are layer-selected, not level-scaled (memory `cameo-armor-layers-and-granularity`).
- `Shield` keeps its 2× offset.
- Per-weapon `Versus` overrides remain **forbidden** outside `^Warhead_*` templates (standing rule).

---

## 5. ⛔ BLOCKER — the ladder must be fixed first

`audit_level_ladder.py` (ratchet 9, in `run_all.sh`) measures the EFFECTIVE ladder — median damage
of the real weapons on each rung:

| verdict | families |
|---|---|
| rise correctly | CannonHE, Flame, Laser, MissileChem, MissileHE |
| **FLAT** | Bullet, Demolition, MissileFire |
| **INVERTED** | Chemical, Flak, Inferno, MissileAP, Tesla, Thermobaric |

```
MissileAP   Light 20000 (n=23) -> Medium 12000 (n=26) -> Heavy 11000 (n=32)   falls throughout
Tesla       Heavy 12000 (n=47) -> Super  6500 (n=20)                          Super is half
Flak        Light 32000 (n=2)  -> Medium  8000 (n=15)                         quarter
```

Interpolating between two **equal** endpoints yields nothing — every intermediate rung would be
identical to its neighbours. Interpolating between two **inverted** endpoints yields nonsense — a
T1.5 unit would land *above* the T2 unit, faithfully and invisibly encoding the bug.

⚠ Restating a rung is a **balance change**: it goes through the pipeline (`extract_stats` →
ledger → `apply_balance --confirm`) and `--confirm` needs a maintainer order. Never hand-edit.

---

## 6. Open questions for the maintainer

1. **Super is inconsistent.** `Tesla` Heavy→Super is `+5` (the same step as Medium→Heavy) but
   `Magic` Heavy→Super is `+15`. Which is right? The answer sets the top of the `h` scale.
2. **Calibration of `offset(h)`.** `22 × (1 − f(C))` fits the three known rungs to ±0.4, but it is
   a back-of-envelope fit, not a derivation.
3. **Should Spread scale from the same `h`?** The measured `1 : 1.5 : 2` ramp says yes, but that
   couples blast radius to tech tier — a deliberate design choice, not an obvious one.
4. **Is `HeavySuper` needed?** `Tesla_Heavy + Tesla_Super` exists as a mix today. Under the
   continuous model the question dissolves, but the affected weapon still needs a value.
5. **Does `h` come from the unit, or stay per-weapon?** A unit with two weapons might want
   different heaviness on each (a T3 tank's coaxial MG is not a T3 weapon).

---

## 7. Build order

1. **Fix the 9 broken ladders** — balance pipeline, maintainer `--confirm`. Blocks everything.
2. Settle §6.1 and §6.2 (Super, calibration).
3. **Add `Heaviness` to `AreaDamageWarhead`**, inert by default (`0` = today's behaviour), rebuild,
   boot-gate. Ship the mechanism before using it — same as the AreaDamage fold.
4. Verify the transform reproduces all 126 existing templates exactly, family by family.
5. Collapse the level templates to one per family; set per-weapon `Heaviness` from
   `tier_multiplier`.
6. Re-point the 102 mix weapons; lower the `three_way_split` and `tier_weapon_class` ratchets.

---

## 8. Provenance

Every number here was measured on the resolved ruleset via `tools/audit/miniyaml.Ruleset`, not
read from a summary. Guards added while investigating:

- `tools/audit/audit_tier_weapon_class.py` — TYPES × LEVELS budget, ratchet 218.
- `tools/audit/audit_level_ladder.py` — effective ladder monotonicity, ratchet 9.

⚠ Three earlier versions of these audits measured the WRONG SURFACE — source instead of resolved,
override instead of addition, template placeholder instead of effective value — and each produced
a confident, wrong number (393 violations; 40 broken ladders; 79 weapons queued for a "repair"
that would have erased their tier identity). Assert against the resolved node, always.
