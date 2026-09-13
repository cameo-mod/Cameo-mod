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
| **actors whose unconditional armaments span more than one role** | **108** |
| …of those, still multi-role after `baseline_armaments` has run | 47 |
| air-role armaments the existing `_AA` NAME test cannot see | **50** |
| assigned actors whose reference row is contaminated by the fold | **40** |

⚠ **108 and 47 are different facts and an earlier draft of this document quoted the 47 as the
scope, understating it by more than half.** `reference_distribution.baseline_armaments` already
drops **61** of the 108 before anything downstream sees them — but it drops them by matching
`@AA` / `_AA` in the slot or weapon NAME, which is the guard §3b shows cannot see 50 real air
weapons. So 108 is the population the ruling covers, 47 is merely what survives a name test that
is itself unreliable. Registered as `armament_multi_role_actors` in `docs/audit/doc_claims.yaml`.

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

### 2d. A source that cannot state a role still votes on the main gun

⛔ **THIS WAS A REGRESSION IN THE FIRST DRAFT AND IT WAS CAUGHT BY READING THE OUTPUT.** Seven of
the nine INI sources pin no `source_sha256`, so §4 refuses to state their projectiles' domains and
every one of their armaments arrives with `role = None`. The first `pair_by_role` simply skipped
those views — and `ra2_soviets_apocalypsetank` (5 sources), `ra2_allies_ifv` (5),
`yuri_gatlingtank` (5) and `ra2_soviets_flaktrack` (4) came back with **zero votes on any weapon**.
That is precisely the shape of PR #369: a guard that looks green by having nothing left to guard.

The rule is satisfied either way. We cannot prove such a source has the anti-air weapon, so it does
not vote on it; we can see it has a main gun, so it votes there and **nowhere else**. Recovered
**317** votes, and the pair is flagged so a reader can always discount it.

⛔ **And it pairs against the GROUND weapon, not the strongest one** — a second defect in the same
helper, found the same way. On the Apocalypse the AA missile out-damages the cannon (32,000 vs
24,000), so taking the hardest hitter matched **five peer cannons against an anti-air missile** —
the one pairing this ruling forbids. `UNPROVEN_PREFERENCE` orders ground → both → air → special,
which is not a new rule either: DESIGN's `anti_air_vehicle` anchor already says to price on the
ground weapon.

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
* **An unclassified token is neutral AND reported.** It can never flip a domain silently, and
  `armament_pairing.json` lists any it sees. Today that list is **empty**: the three measured token
  sets cover both corpora completely.

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
| actors firing in more than one role | 46 |
| **reference rows contaminated by the fold** | **93** |
| armament pairs formed | **607** |
| …proven exact role matches | 213 |
| …a dual-role `both` weapon stood in | 94 |
| …source could not state a role, votes on the main gun only | 300 |
| …the reference carries that weapon only at ELITE rank | 5 |
| pairs by role | ground 400 · both 177 · air 27 · special 3 |
| actors with a weapon no structured source covers | **28** (air 20 · both 8) |
| actors with no structured reference at all | 21 |
| unknown target tokens | **0** |

⚠ **TWO REASONS A WEAPON ENDS UP UNREFERENCED, AND COUNTING THEM TOGETHER MAKES THE NUMBER LIE.**
An early draft of this table reported "157 actors with an uncovered role, 105 of them ground",
which reads as a broken matcher. It was not: the great majority paired nothing at all because every
source assigned to them reaches the map as a Doc 5 markdown TABLE with one folded weapon column and
no armaments to pair — the known Doc 5 coverage gap, not a role finding. The report now separates
them (`actors_with_no_structured_reference_at_all` vs `uncovered_despite_a_structured_reference_*`)
and only the second is a finding about roles.

⭐ **Every remaining uncovered weapon is an air or dual-role one — `..._ground` is now zero.** That
is the expected shape: peer mods give a unit one main gun and, much more rarely, a second anti-air
mount, so a Cameo unit's cannon almost always finds a counterpart and its AA missile often does
not. `td_gdi_firehawk` is the maintainer's own example and its answer is correct — the A-10 Warthog
has no anti-air weapon in Combined Arms or in DTA, so the Sidewinders genuinely have no reference
in either assigned source. **Reporting that is the point.** The previous behaviour gave them one
anyway, by averaging them with a napalm bomb.

## 6. What this deliberately does NOT do

* It does not re-score or re-assign anything. Clause 2 of the matching law (one reference unit per
  source, per Cameo unit) is untouched — this splits what a pair MEANS, not which pair it is.
* It writes **no balance number**. Rules 3 and 4 stand: the ledger is the only route, and
  `apply_balance --confirm` needs a maintainer order.
* It does not change `armament_profile`, `baseline_armaments` or `is_anti_air_armament`. Retiring
  the fold in favour of per-role targets is the next step and it is a **model change**, so it needs
  the maintainer's word before it lands.
* ⛔ **Superweapons are excluded everywhere upstream and nothing here reaches them.**
