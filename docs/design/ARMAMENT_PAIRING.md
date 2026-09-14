# Per-armament reference pairing

_Maintainer order, 2026-09-13:_

> "we need to be careful because those fire bombs and the sidewinder anti air missiles have
> separate ranges … Those two weapons are so different they should not be mixed. actually this
> should be true for **any dual or multi weapon units** so the correct weapon is mapped … you need
> to separate those two weapons, **use DTA for the missile as reference only while using the
> cannon reference from all 3!** Adjust all the existing units with that logic!"

This document is the ruling, the mechanism and the measurements. The code is
[`tools/balance/armament_roles.py`](../../tools/balance/armament_roles.py),
[`tools/balance/build_armament_pairing_report.py`](../../tools/balance/build_armament_pairing_report.py)
and [`tools/reference/extract_ini_projectile_roles.py`](../../tools/reference/extract_ini_projectile_roles.py);
the contract is [`tools/tests/test_armament_roles.py`](../../tools/tests/test_armament_roles.py).

---

## 1. The defect, measured

`reference_distribution.armament_profile` folds every baseline armament of an actor into ONE
`w_range` / `w_damage` / `w_dps`, and takes `max(ranges)` for the range. For a single-weapon unit
that is exactly right. For a dual-weapon unit it is a number about no weapon at all:

| actor | armament | role | range |
|---|---|---|---|
| `td_gdi_firehawk` | `…_firehawkmissiles_AA` | air | **12,500** |
| `td_gdi_firehawk` | `…_firehawkbomb` | ground | **1,250** |
| `td_gdi_mammothtank` | `…_120mmdual` | ground | 6,141 |
| `td_gdi_mammothtank` | `…_mammothmissiles` | both | 6,141 |

A ten-fold range gap inside one row. Measured over the whole tree (2026-09-13):

| | |
|---|---|
| priced actors | **940** |
| **actors whose unconditional armaments span more than one role** | **106** |
| …of those, still multi-role after `baseline_armaments` has run | 47 |
| air-role armaments the existing `_AA` NAME test cannot see | **50** |
| assigned actors whose reference row is contaminated by the fold | **39** |

⚠ **106 and 47 are different facts and an earlier draft of this document quoted the 47 as the
scope, understating it by more than half.** `reference_distribution.baseline_armaments` already
drops **59** of the 106 before anything downstream sees them — but it drops them by matching
`@AA` / `_AA` in the slot or weapon NAME, which is the guard §3b shows cannot see 50 real air
weapons. So 106 is the population the ruling covers, 47 is merely what survives a name test that
is itself unreliable. Registered as `armament_multi_role_actors` in `docs/audit/doc_claims.yaml`.

⛔ **THIS FIGURE WAS FIRST PUBLISHED AS 108 AND THAT WAS WRONG.** `audit_doc_claims` measured 106
against the same tree from the day the claim was registered; the 108 came from a hand count taken
while the module was still being written, and it was never re-derived from the artifact. The two
actors are not missing from anything — they were never in the population. Corrected 2026-09-13,
value and prose together, which is the whole reason the registry requires a `measure:` block.

## 2. Why the references can already fix it

Nothing needs re-extracting. All three corpora have carried per-armament data all along; only the
consumer folded it.

| side | where the armaments live | role evidence |
|---|---|---|
| Cameo | ledger `armaments[]` — slot, weapon, range, burst, burstdelays, reloaddelay, `requires` | the weapon's own `ValidTargets` / `InvalidTargets`, resolved |
| OpenRA peers | `weapon_evidence[]` — slot, weapon, range, reload_delay, burst, burst_delays, warheads, `requires_condition` | the armament's `valid_targets` |
| INI peers (DTA …) | `w_*` = `Primary=`, `w2_*` = `Secondary=` | the **projectile's** `AA=` / `AG=` flags |

410 peer rows carry `weapon_evidence`; **217 of them hold more than one armament.**

And the counterparts line up without a single name match:

```
CA   4TNK  130mm      ground 4864  |  MammothTusk      Air,AirSmall,Infantry   both 6656
CnC  HTNK  120mmDual  ground 4864  |  MammothMissiles  Ground,Water,Air        both 4864
RA   4TNK  120mm      ground 4864  |  MammothTusk      AirborneActor,Infantry  both 6656
DTA  HTNK  120mm      ground 5.7c  |  MammothTusk      AGHeatSeeker2           both 5.7c
```

## 2a. ONE WEAPON, ONE VOTE — the maintainer's model

Maintainer, 2026-09-13, when asked how far to wire this in:

> "the reference map needs to show each weapon per actor and try to find the weapon for each
> reference. **If the reference unit does not have the weapon it should not vote on it.** But yeah.
> Try to always find a reference weapon for each unit. … all 3 references vote on the cannon since
> all 3 references have that but only the DTA reference votes on the missile"

So the electorate is **per weapon, not per unit**. Three patterns fall out, and the counts are the
whole reviewable population (46 multi-weapon units with at least one structured reference):

| | | |
|---|--:|---|
| **A** | every source votes the first weapon, fewer vote the second | Hum-vee Mk II, `td_gdi_apc`, **`td_gdi_battletank`** |
| **B** | every source carries both weapons | the mammoths, `ra1_allies_destroyer` |
| **C** | the second weapon has no voter at all → **abstains** | `td_gdi_firehawk` |

⛔ **I GOT THE BATTLE TANK WRONG FIRST AND THE MAINTAINER CORRECTED IT.** Reading only
`Primary=`/`Secondary=` on DTA's `[MTNK]` shows a dummy and a 90 mm cannon, so I reported that DTA
has no missile and the Battle Tank therefore falls in pattern C. That was reading two of the unit's
**three** weapon declarations — `Elite=70mmMsl1` is a missile launcher, and it replaces the dummy.
§2b is the whole correction. `td_gdi_battletank` is pattern **A** and reads exactly as described:

```
ground  120mm         =  CA 120mm · DTA 90mm · TD 120mm    3 of 3 sources vote
both    m1a1missiles  ~  DTA 70mmMsl1 (elite)              1 of 3 sources vote
```

Maintainer ruling on the uncovered weapons (same exchange): **abstain and say so.** An armament no
source can reference gets no target and is shown as unreferenced, rather than being handed an
averaged number.

### 2b. THREE weapon slots, not two — `Elite=`

⛔ **THE MAINTAINER HAD TO SAY THIS TWICE.** A TS unit declares `Primary=`, `Secondary=` **and**
`Elite=`; `ini_corpus.json` carries only the first two, as `w_*` and `w2_*`. **171 DTA sections
declare `Elite=` and not one of those weapons reached the reference map.**

```
[MTNK]                                   [70mmMsl1]
Name=GDI Medium Tank                     Damage=30      Range=6.14
Primary=90mmDummy   ;ROF ... both        Projectile=AGHeatSeeker
Secondary=90mm                           Warhead=BazAP
Elite=70mmMsl1
```

The primary is a zero-damage **dummy** that exists only to set the rate of fire, so the GDI Medium
Tank's real loadout at elite is the 90 mm cannon **plus a 70 mm missile launcher** — *"on elite it
has one cannon and one missile launcher"*, exactly as stated. Reading two slots made it look like a
single-cannon tank, and that is why my first pass reported the Battle Tank's missile as having no
reference anywhere.

⭐ **`Trainable=` is the gate and the Enhance overlay flips it.** Measured 2026-09-13:

| | sections with `Elite=` | trainable | reachable in the corpus | an ADDITIONAL armament |
|---|--:|--:|--:|--:|
| DTA Classic | 171 | **0** | 0 | 0 |
| DTA Enhanced | 171 | 139 | **131** | **7** |

`[MTNK]` is `Trainable=no` in `Rules.ini` and `Trainable=yes` in `Enhance.ini`. The elite missile
exists in both rulesets and can only ever be FIRED in Enhanced. An extractor ignoring `Trainable`
would hand DTA Classic 139 weapons no unit in that ruleset can reach.

⚠ **And most elite weapons are not a second weapon at all.** `Elite=` REPLACES the primary, so the
124 `E`-suffix entries (`RaiderCannonE`, `MinigunE`, `HellfireE`, `120mmE`) are the same gun
improved. Only where the primary is a zero-damage dummy does the elite weapon occupy a slot that
was otherwise empty — **7 units** — and only those are genuinely additional. Recorded per row as
`replaces_dummy_primary`. Elite weapons enter the bench as **rank-gated**: they never displace an
ordinary armament, they only fill a place nothing else can.

### 2c. The bench — a reference's SECOND weapon in a compatible role

Keeping only the strongest peer weapon per role lost the Battle Tank even after the elite slot
arrived. DTA's medium tank at elite has **two ground-role weapons** (the TS engine puts both the
90 mm cannon and the 70 mm missile launcher on the ground domain), while Cameo's battle tank has a
`ground` cannon and a `both` missile. The cannon claimed DTA's only ground candidate, and the
missile was then told no source covered its role.

⚠ **A DELIVERY AXIS WOULD NOT HAVE FIXED THIS HONESTLY.** The obvious idea — classify guided vs
unguided, match missile to missile — has no reliable source signal on the INI side. `ROT` (rate of
turn) reads like the homing field, but `TracerM`, an ordinary gun tracer, declares `ROT=1`, and
**426 of 433** sections that declare `ROT` are above zero. `Image=DRAGON` and `Warhead=BazAP` are
names, which is the trap this whole module exists to avoid.

So `candidates_by_role` keeps the whole bench, ranked by the per-cycle coordinate, and a Cameo
armament falling back to a compatible role takes the next unclaimed weapon there. No semantics
required. The result is the maintainer's own sentence:

```
td_gdi_battletank
  ground  120mm            =  CA 120mm  ·  DTA 90mm  ·  TD 120mm      3 of 3 vote
  both    m1a1missiles     ~  DTA 70mmMsl1 (elite)                    1 of 3 vote
```

⛔ **Clause 5 still binds: a zero-damage dummy is never anybody's reference.** The first bench
handed `90mmDummy` — a real armament with a real range whose only job is to set the next slot's
rate of fire — to the Battle Tank's missile.

### 2d. ⛔ STRUCK — a source that cannot state a role now ABSTAINS

**This section used to say the opposite, and the reversal is the ruling, not a bug fix.**

The original text: seven of the nine INI sources pin no `source_sha256`, so §4 refuses to state
their projectiles' domains and every one of their armaments arrives with `role = None`. Dropping
those views left `ra2_soviets_apocalypsetank` (5 sources), `ra2_allies_ifv` (5) and
`yuri_gatlingtank` (5) with zero votes, so a fallback let such a source vote on the main gun only,
ordered ground → both → air. That recovered 317 votes.

**Astra falsified the premise (PR #375 review, blocker 3).** The fallback picked the hardest-hitting
role-less weapon, and a *secondary* AA gun can out-damage its own chassis' main gun —
`FlakTrackAAGun` and `RA1RedEyeAA` were both being reported as **ground main-gun evidence**. That is
the one pairing this whole document exists to forbid, arrived at from the other direction: not by
mixing a cannon with a missile on the Cameo side, but by taking an anti-air gun as the reference.

Maintainer's ruling, 2026-09-13: *"ambiguous role or identity must abstain."*

So `_unproven_pair` and `UNPROVEN_PREFERENCE` are **deleted**. An unproven peer weapon never votes.
The cost is real, deliberate and counted rather than hidden:

| | before | after |
|---|--:|--:|
| pairs | 607 | **308** |
| exact pairs | 213 | **215** |
| `both` stand-ins | 94 | 93 |
| unproven pairs | 300 | **0** |

`ra2_soviets_apocalypsetank` is back to zero votes and that is now the **correct** answer for it,
not a regression: no source can prove which domain its weapons serve. The ordering rule the
fallback encoded is not lost — it still lives where it came from, DESIGN's `anti_air_vehicle`
anchor and `reference_distribution.baseline_armaments`.

### 2e. WHICH weapon a reference offers depends on the Cameo actor's TIER

Maintainer, 2026-09-13:

> *"base version only for original units (with the exception for that dummy weapon of the MTNK)
> and elite versions or upgraded weapons for promotion units to get some power creep for late game
> or upgraded promotion units"*

and, stated as a constraint: *"A replacement must not also count beside its base weapon."*

| tier | who | which peer weapons are on the bench |
|---|---|---|
| `original` | an original-shipping mod matched it BY NAME | the **base** weapon only |
| `expanded` | promotion units, Cameo additions, CA/DTA inventions | the **elite/upgraded replacement**, *in place of* the weapon it replaces |

Measured: **140 original, 174 expanded** of the 314 priced actors with an assignment.

⛔ **`Elite=` REPLACES the slot it is declared against, so benching it beside its base weapon lets
one gun vote twice.** Astra found exactly that on FRIGATE, BEHEMOTH, YAK and HTNKARTY (blocker 1).
130 of DTA Enhanced's 139 reachable elite weapons are the `E`-suffix upgrade of a gun the unit
already fires — `MinigunE`, `120mmE`, `RaiderCannonE` — the same weapon, improved.

⭐ **MTNK's dummy is the exception and it is not a special case in the code.** It falls out of
`replaces_dummy_primary`: `[MTNK] Primary=90mmDummy` is a zero-damage rate-of-fire stub, so
`Elite=70mmMsl1` fills a slot that was otherwise empty and IS a second weapon. **Nine** DTA units
are in that state, and for them the elite weapon joins **both** tiers because it displaces nothing.
That is why `td_gdi_battletank` — an original — still references DTA's elite missile.

### 2f. Every Cameo armament pairs, not one per role

⛔ **Reducing Cameo to the strongest weapon per role was a second fold wearing the first one's
clothes** (Astra, blocker 2). `japan_oitank` carries OIFlamer, OIBigCannon and OISmallCannon — three
GROUND weapons — and reporting only the flamer is the same defect this lane exists to remove, one
level down. `ra1_allies_destroyer` has four. `cameo_armaments()` returns every referenceable
armament, hardest-hitting first, deduplicated on **(weapon, role)** rather than on the slot, because
`Armament@PRIMARY` and `Armament@GARRISONED` are one gun fired from two places.

The same gate was wrong in the report: the actor-level cell is withheld for carrying more than one
ARMAMENT, not more than one ROLE, so 17 withheld actors were blanked above and had no per-armament
block below either.

### 2g. Unknown target tokens FAIL CLOSED

⛔ Astra, blocker 5: `Ground, UnknownFlyingTarget` came back a **proven exact ground** vote. The
classifier used to return a confident role alongside the unrecognised tokens, treating them as
neutral on the theory that a new marker must never silently flip a domain. That theory is a guess,
and guessing from tokens is the exact failure this module was built to avoid — DTA's
`AGHeatSeeker2` says "AG" in its name and declares `AA=yes`.

`role_of_targets` now returns `None` whenever any token is unrecognised, and an unproven weapon
does not vote (§2d). The tokens are still reported, because `--audit` proves the vocabulary covers
the corpus: it is **0 today**, which is exactly when this guard is cheap to install.

### 2h. Evidence fingerprints are enforced where the evidence is CONSUMED

⛔ Astra, blocker 4: *"Evidence hashes are recorded but not enforced when consumed."* Both
extractors refuse to run unless the INI they read hashes to the pin the corpus carries — and then
wrote a JSON that any later process could consume months after the corpus moved underneath it.
**A recorded hash that nobody re-checks is a comment.**

Two layers now check, and both work without the 9.9 GB reference folder, using only files the
repository holds:

* `extract_ini_projectile_roles.load()` and `extract_ini_elite_weapons.load()` re-verify each
  source's `rules_sha256`/`overlay_sha256` against `ini_corpus.json` and drop — **per source** — any
  whose pin has moved. Dropped sources are returned on `load.dropped` so a caller can report the
  gap instead of discovering it.
* `armament_pairing.json` records an `inputs` block for the complete reproducibility closure: the
  INI corpus and extracted evidence, selected OpenRA peer corpora, Cameo balance ledgers, active
  manifest/rules/weapons, and direct tool dependencies. `build_reference_report.pairing_document()`
  rebuilds that exact set and **raises** on a missing, extra, or changed input rather than rendering
  stale evidence as current. Missing, malformed, empty, or unsupported artifacts are refused for
  the same reason — they cannot prove they are fresh. Repository text is hashed after canonicalising
  LF/CRLF so the same commit verifies across checkout policies; the peer-corpus and INI extractors
  still enforce their separate raw-byte source pins.

Stale evidence is worse than missing evidence, because it looks exactly like the real thing.

### 2i. The attack cycle includes the CHARGE — ruled, and NOT yet applied

The law, as the maintainer states it (2026-09-14):

```
DPS = damage x burst / attack cycle
attack cycle = reload delay + sum of ALL burst delays + charge delay
```

⚠ **"DPS" is a name, not a unit — it is damage per TICK.** Every rate in this lane, in the ledger
and on the map is per tick.

One thing DOES reach the cycle from outside the weapon today: **the trait can override the reload.**
`AttackTesla`'s own `ReloadDelay` is the cycle, `MaxCharges` is the burst, and the WEAPON's reload
is the gap between zaps (`formula.charge_attack_cycle`, shipped in #385). Reading the weapon alone
reported a Tesla Coil, whose weapon reloads every 3 ticks, as firing twenty times a second.

⛔ **THE CHARGE TERM ITSELF IS WITHHELD, AND THAT IS A DECISION, NOT AN OVERSIGHT.** I implemented
it and Astra's engine trace held it (PR #386), correctly. Three shapes break a naive `cycle + ticks`:

* **multi-shot `ChargeLevel`** — the burning Obelisk is Burst 10 / BurstDelays 1, and its
  `AttackCharges` notifier resets `ChargeLevel` on every projectile, so later shots must recharge.
  Its period is not `105 + 50`.
* **interleaved `AttackTesla`** — ⭐ **resolved by a maintainer ruling, 2026-09-14**: the Rail
  Tower *"needs to charge for every shot unlike the tesla coil ... at 5 shots the initial charge
  delay is used 5 times"*. So its cycle is `160 + 5 x 12 = **220**` — #385's 160 never paid the
  charge and my 172 paid it once; both were wrong. A charged weapon is one of two machines,
  **charge-once** or **charge-per-shot**, differing by a factor of `MaxCharges`. Astra's trace
  gives the mechanism (the Rail Tower's post-shot `ChargeFire` wait of 3 expires inside a 10-tick
  weapon reload, so the trait exits and reacquires). What is still missing is the DETECTION:
  `ChargeDelay` against the weapon reload, a field `extract_stats` does not record.
* **random `ChargeLevel`** — `steelconsortium_dagger` declares `ChargeLevel: 25, 50` and
  `extract_stats` keeps only the lower bound. ⭐ **Maintainer ruled 2026-09-14: use the MEAN of
  min and max**, added to the reload — so the Dagger contributes 37.5 — and `wc2_humans_dwarvenrifleman`'s `0, 4` becomes **2**, not the
  zero the ledger currently records. Four actors carry a ranged charge; the extractor change to
  average them is pending and is one of the three unblocks.

The unblock is three extractor fields — `ChargeDelay`, `ShotsPerCharge`, and whether a value was a
RANGE — plus a formula that models recharge overlap. Withholding is the same answer this lane gives
everywhere else: an unprovable quantity abstains.

⛔ **THE POPULATION IS SMALLER THAN IT LOOKS, AND I OVERSTATED IT.** 14 actors carry a `charge_up`
record, but that is not 14 actors whose reported cadence would move. **Three are `AttackTesla`; the
other eleven are `ChargeLevel`-family records** — a different trait with a different schedule, not
units inheriting an engine default, which is how I first explained it and was wrong.
`ra1_allies_mobileradarjammer` has **no priced armament view at all**,
`wc2_humans_dwarvenrifleman` records **zero** ticks today (it declares `0, 4`, so under the
averaging ruling it becomes 2), and `terran_siegetank` keeps 37 under the ownership guard. I published a table claiming the siege
tank at 62 and the burning Obelisk at 155; **both were wrong** — measured before I adopted #385's
guard, and never re-measured after. Counting records is not counting effects.

⛔ **AND `charge_attack_cycle` RETURNING `None` DOES NOT MEAN "NO CHARGE TIME".** It says only that
the trait does not override the weapon's RELOAD. Both #385 and I read it the other way first.

## 3. The pairing key is the targeting envelope

Not the weapon's name. The vocabulary is **the maintainer's own missile ruling of 2026-09-07**,
reused rather than re-derived:

| ValidTargets | role | missile family (DESIGN, §missile role law) |
|---|---|---|
| ground only | `ground` | `^Warhead_MissileHE_*` |
| air only | `air` | `^Warhead_MissileAA_*` |
| both | `both` | `^Warhead_MissileAP_*` |
| neither (an interceptor, a healer, a snare) | `special` | — |

Three rules make it safe:

* **Absent `ValidTargets` is `Ground, Water`,** the engine default (`WeaponInfo.cs:116`) — 451 of
  the ~900 peer armaments leave it unwritten, so reading absence as "unknown" would abstain on half
  the corpus.
* **`InvalidTargets` subtracts** before the domains are read.
* **An unclassified token fails closed AND is reported.** The weapon abstains rather than deriving
  a role from the recognised subset, and `armament_pairing.json` lists every unknown token. Today
  that list is **empty**: the three measured token sets cover both corpora completely.

`air` and `ground` are the one forbidden pair. `both` is adjacent to everything, so an exact role
claims its partner first and only then may a `both` stand in — otherwise a cannon claims the peer's
dual-role missile before the missile gets a turn.

### 3a. Why this does not replace `audit_missile_role_family.weapon_role`

That function fails closed to `custom` on any token outside `{Ground, Water, Air}`, which is right
for a check driving four LOWER-ONLY ratchets and useless for pairing — `Ground, Water, Trees` is 31
CA armaments and `AirborneActor, Infantry` is the entire Red Alert mammoth missile. The pairing
classifier reads a wider, explicitly enumerated vocabulary and still fails closed on the unknown.
`test_agrees_with_the_missile_role_audit` pins that the two never disagree on a weapon the audit is
willing to judge: **the wider set may only add answers.**

### 3b. Why it does not replace `is_anti_air_armament` either

That helper tests the slot/weapon NAME (`@AA`, `_AA`) and is load-bearing for the existing baseline
selection, so it is untouched. But it cannot see 50 air-role armaments spelled otherwise —
`RA2Patriot`, `RA2TURRETFLAKAA`, `AsianPhotonCannon`, `PortableFlak`, `BallistaSingleShotAir` — and
a name blocklist is the mistake this codebase has paid for repeatedly. **The envelope is the
source; the spelling is a convention.**

## 4. The INI half: `AA=` / `AG=`, never the name

The TS/RA2 engine has no `ValidTargets`. A weapon's domain lives on its **projectile**, as two
booleans, and the corpus rows carry only the projectile's name. `extract_ini_projectile_roles.py`
reads the real flags out of `Rules.ini` with `$Inherits=` flattened, and writes
`docs/reference/ini_projectile_role_evidence.json`.

⛔ **The name is a trap, and it is the exact case the maintainer asked about.** DTA's mammoth Tusk
flies `[AGHeatSeeker2]` — "AG" right there in the section name — which declares
`$Inherits=AGHeatSeeker` and then `AA=yes`. It is a **dual-role** missile, which is what makes it
the correct partner for Cameo's `MissileAP_Heavy` and CA's `MammothTusk`. A name classifier calls
it anti-ground and pairs an air missile against a cannon.

Coverage: **DTA Classic 57/57 and DTA Enhanced 60/60** cited projectiles resolved, nothing
undeclared. A projectile declaring neither flag takes the engine default (`AA=no`, `AG=yes`), which
is recorded in the file as an explicit assumption with its basis — and each verdict says which half
was declared and which was defaulted.

⛔ **DTA only, and that is a provenance ruling.** Of the nine INI sources, DTA Classic and DTA
Enhanced are the only two whose corpus rows carry a `source_sha256`; the other seven pin nothing. A
projectile role is a claim about the same bytes the damage numbers came from, and for seven sources
that claim cannot be made. They abstain. Re-pin them and the tool covers them with no change. (The
two DTA hashes were verified against the reference install before anything was written:
`786f0ae5…` for `Rules.ini`, `d836d5a9…` for `Enhance.ini`, both exact.)

## 5. What the pairing currently reports

`python tools/balance/build_armament_pairing_report.py --write`

| | |
|---|---|
| actors with a priced armament in the assignment | 314 |
| …referencing a peer's BASE weapon (original) | 140 |
| …referencing the elite/upgraded replacement (expanded) | 174 |
| actors firing in more than one role | 46 |
| **reference rows contaminated by the fold** | **39** |
| armament pairs formed | **308** |
| …proven exact role matches | 215 |
| …a dual-role `both` weapon stood in | 93 |
| …source could not state a role | **0 — they abstain** (§2d) |
| …the reference carries that weapon only at ELITE rank | 10 |
| pairs by role | ground 181 · both 108 · air 19 |
| Cameo armaments with no reference | 423 |
| actors with at least one uncovered armament | **142** |
| uncovered armaments despite structured references | ground 104 · both 47 · air 25 · special 2 |
| actors with no structured reference at all | 17 |
| unknown target tokens | **0** |

⚠ **THE PAIR COUNT FELL FROM 607 AND THAT IS THE RULING LANDING, NOT A REGRESSION.** 300 of those
pairs were the unproven fallback §2d strikes; the maintainer ruled such a source must abstain.
Exact matches went UP (213 → 215) because §2e stopped a replacement voting beside its base weapon
and §2f gave every same-role armament its own turn. `cameo_armaments_without_a_reference` is large
(423) and newly honest for the same reason: it now counts every armament rather than one per role,
so a four-gun destroyer reports four gaps where it used to report one.

⚠ **TWO REASONS A WEAPON ENDS UP UNREFERENCED, AND COUNTING THEM TOGETHER MAKES THE NUMBER LIE.**
The report separates actors whose assigned peers expose no structured armament rows at all from
weapons left uncovered despite at least one structured peer row. Structure exists independently of
whether a particular weapon found a compatible match: a structured zero-match row is evidence of a
gap, not missing structure.

Coverage is also tracked by **weapon identity**, not merely by role. The earlier role-level count
said only seven actors were uncovered because one successful ground match could hide another ground
weapon on the same actor. The corrected report exposes 178 uncovered armaments on 142 actors. That
is an honest inventory for later reference work, not a licence to invent votes. For example,
`td_gdi_firehawk` remains correctly uncovered for its Sidewinders when its A-10 references carry no
anti-air weapon; the source abstains instead of averaging those missiles with a napalm bomb.

## 6. What this deliberately does NOT do

* It does not re-score or re-assign anything. Clause 2 of the matching law (one reference unit per
  source, per Cameo unit) is untouched — this splits what a pair MEANS, not which pair it is.
* It writes **no balance number**. Rules 3 and 4 stand: the ledger is the only route, and
  `apply_balance --confirm` needs a maintainer order.
* It does not change `armament_profile`, `baseline_armaments` or `is_anti_air_armament`. Retiring
  the fold in favour of per-role targets is the next step and it is a **model change**, so it needs
  the maintainer's word before it lands.
* ⛔ **Superweapons are excluded everywhere upstream and nothing here reaches them.**
