# Class moves — completing the 2026-07-21 template split, one class at a time

_Working document. One section per class, in the order they are done. Each section is a PROPOSAL
until the maintainer approves it; approved sections still need `apply_balance --confirm` where they
move a number, and a **boot gate** in every case (they edit `mods/**`)._

Governed by [`BASELINE_ACTOR_REVIEW.md`](BASELINE_ACTOR_REVIEW.md). Enforced by
`tools/audit/audit_class_templates.py`.

---

## 0. ⭐ Why five templates are dead: a split that was designed and never applied

`^RocketTrooperInfantryTemplate` documents it in its own comment:

> *"Formula v2 ROCKET TROOPER class (design 2026-07-21): dedicated missile-based anti-vehicle /
> anti-air infantry. **Splits from the old AntiTankAntiAir template, which mixed bullet-based AA
> infantry into special forces.**"*

`^ArcherInfantryTemplate` carries the same 2026-07-21 stamp. So the five dead templates —
`^ArcherInfantryTemplate`, `^HeavySniperInfantryTemplate`, `^RocketTrooperInfantryTemplate`,
`^SupportInfantryTemplate`, `^SuperDefenseTemplate` — are not abandoned ideas. **They are the
unapplied half of a designed split.** The units they were meant to hold are still sitting in the
templates the split was supposed to empty.

⛔ **That is why `archer` and `heavy_sniper` are the only two classes whose member count went DOWN
against the ledger, and why both are SIGNED against a template with zero inheritors.**

---

## 1. `archer` — ⭐ PROPOSAL (revised: 8 members, not 4)

⛔ **My first roster was wrong and the maintainer corrected it twice.** I searched by NAME
(`archer|bow|arrow`) and found 4. The maintainer: *"The orc axe throwers and headhunter are the
equivalent to the elven archers so they must be in the same archer template."* Correct — and
enumerating the bucket structurally instead of by name found **8**, including
`wc2_humans_elvenranger`, which has neither "archer" nor "bow" nor "arrow" in its name.

⭐ **The reliable query is the template's own definition, not the unit's name.** All eight sit in
`^AntiTankAntiAirInfantryTemplate` firing `Missile`-projectile thrown/drawn weapons —
`wc2arrowFire`, `wc2axeFire`, `AsianMaidenBow`. That is exactly the population
`^ArcherInfantryTemplate` describes ("projectile-arc infantry using the MISSILE projectile … hits
air") and exactly what the 2026-07-21 split was meant to remove from AT/AA.

### The roster, and the repricing

Anchor = cheapest = **`asianalliance_veteranarcher` @ 450**.

| was | → new | ×cost0 | HP | actor | why it moves |
|--:|--:|--:|--:|---|---|
| 450 | **450** | 1.00 | 14,000 | `asianalliance_veteranarcher` | anchor |
| 500 | **500** | 1.11 | 20,000 | `japan_archermaiden` | lowest HP of the 500 group keeps the round number |
| 500 | **550** | 1.22 | 30,000 | `wc2_orcs_trollaxethrower` | collision |
| 500 | **600** | 1.33 | 30,000 | `wc2_orcs_trollberserker` | collision; the axethrower's upgrade form, so it prices above it |
| 600 | **650** | 1.44 | 25,000 | `wc2_humans_elvenarcher` | collision |
| 600 | **700** | 1.56 | 25,000 | `wc2_humans_elvenranger` | collision; the archer's upgrade form |
| 1,000 | **1,000** | 2.22 | 40,000 | `wc2_orcs_trollheadhunter` | unchanged |
| 1,100 | **1,100** | 2.44 | 35,000 | `wc2_humans_highelvenarcher` | unchanged |

| | measured | target |
|---|--:|--:|
| occupancy in the target band | **8 of 8 = 100%** | >= 80% |
| spread | **2.44x** | fits 2.50x |
| **sigma_log** | **0.296** | 0.3575 |
| unique prices, all multiples of 50 | ✅ | required |

⚠ **Four units need +50 or +100, and that is a balance number** — so it goes through
`apply_balance --confirm` on a maintainer order, not a hand edit (CLAUDE.md rule 3).

⚠ **The tie-break needs a ruling, because mine was luck.** `trollaxethrower`/`trollberserker` and
`elvenarcher`/`elvenranger` are stat-identical pairs (500/30,000 and 600/25,000). I broke the tie
alphabetically and it happened to put each upgrade form above its base. That is not a rule. The
principled tie-break is **the tech/upgrade relationship**, and it should be stated rather than
relied on by accident.

### Two units that look like archers and are not

* `ra1_allies_longbow` — an Apache gunship (`^HelicopterTemplate` ✅)
* `td_gdi_archerartillery` — the British Archer SPG (`^ArtilleryTankTemplate` ✅)

### ⛔ And one separate defect found on the way

`wc2_orcs_kodobeast` (1,000) throws the same `wc2axeFire` and sits in
`^AntiTankAntiAirInfantryTemplate` — but it inherits **`^WC2Vehicle`**. It is a **vehicle in an
infantry class template**, which is a different defect from the eight above and needs its own
ruling: a mounted axe-thrower is arguably a `scout_vehicle` or a `light_tank`, not an archer.
⚠ Its `Tooltip` also declares `Name:` twice (`Kodo Beast`, then `garrisoned`).

### ⛔ The bucket this comes out of needs a 3-way split, not a 1-way move

`^AntiTankAntiAirInfantryTemplate` holds **44** members and is the grab-bag the 2026-07-21 design
was written to break up. Measured by projectile:

| destination | n | examples |
|---|--:|---|
| **`^ArcherInfantryTemplate`** (this section) | 8 | thrown/drawn `Missile` weapons |
| **`^RocketTrooperInfantryTemplate`** — currently DEAD | ~20 | `td_gdi_rocketsoldier`, `ra1_allies_alliedrocketsoldier`, `ordos_rockettrooper`, `cabal_rocketcyborg` … all `Missile` rockets/bazookas |
| **stays, or goes elsewhere** | ~16 | `Bullet`/`InstantHit` AA — and several that are plainly misfiled: `terran_marine` (basic infantry), `zerg_hydralisk` (3,314), `protoss_hightemplar` (a PsiStorm caster), `ra1_soviets_dragunovantimaterialsniper` (a sniper) |

⭐ **That is the real shape of the job**: filling `^RocketTrooperInfantryTemplate` is the same task
as this one and roughly 20 units in size, and it empties most of the grab-bag at the same time.

### Open questions for this class

1. **Is `Armor: None` intended for archers?** `^ArcherInfantryTemplate` declares it; all eight ship
   as `Flak` today, so the template has never been tested against a real member.
2. **Do archers keep hitting air?** The template says *"Hits air"* — that is why they were in AT/AA.
3. **The tie-break rule** for stat-identical upgrade pairs (above).
4. **`wc2_orcs_kodobeast`** — which class does a mounted axe-thrower belong to?

