# W24 batch 01 — collapse `CannonHE_Heavy` + `CannonHE_Medium` (4 weapons)

**Prepared 2026-08-30 in a container that CANNOT boot-gate.** Everything below is measured from
the resolved ruleset; nothing here has been applied. This exists so the boot-gated session is
**paste → run → verify**, not analysis.

## Why this cluster first

Of the **472** weapons in W24's scope (`survey_weapon_structure`
→ `stacked_main_direct_actor_armament`), this is the cleanest starting batch:

* both mains are **already `^Warhead_*` families** — a pure W24 *collapse*, not a W23 conversion;
* the two families are **adjacent levels of the SAME family**, so the profile shift is the
  smallest available (see the ladder below);
* **no percentage twins** on any of the four — verified, so there is no twin to keep in step;
* all four sit on the identical inherit signature, so it is one edit repeated four times.

⚠ Three files, so one boot covers the batch but the `git add` must name all three.

## The four weapons

| weapon | file | Heavy | Medium | **per-shot total (must not change)** |
|---|---|--:|--:|--:|
| `TigerCannon` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml` | 8000 | 8000 | **16000** |
| `HammerTankCannon` | `ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` | 6000 | 6000 | **12000** |
| `KotinCannon` | `ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` | 6000 | 6000 | **12000** |
| `Type97Cannon` | `ContentPacks/RedAlert/Japan/yaml/weapons.yaml` | 6000 | 6000 | **12000** |

All four currently match `audit_warhead_split`'s **FAIL-1 broadcast fingerprint** (≥2 mains, every
main carrying the identical non-zero Damage), so collapsing them lowers that ratchet too.

## The edit, per weapon

Three lines out, one number changed:

```diff
 TigerCannon:
 	Inherits@wh: ^Warhead_CannonHE_Heavy
-	Inherits@wh2: ^Warhead_CannonHE_Medium
 	Inherits@proj: ^Projectile_Shell_Heavy
 	Inherits@fx: ^Effect_CannonHE_Heavy
 	…
 	Warhead@CannonHE_Heavy:
-		Damage: 8000
-	Warhead@CannonHE_Medium:
-		Damage: 8000
+		Damage: 16000
```

Same shape for the other three, with **12000** as the new Heavy Damage.

## ⛔ THE DECISION THAT IS YOURS — and it is not cosmetic

Collapsing to `CannonHE_Heavy` changes two things the maintainer must accept:

**1. The `Versus` profile.** The two families differ, most at the extremes:

| | None | Flak | Plate | Scout | Light | Medium | Heavy | Super | Wood | Steel | Concrete | Fighter | Bomber | Heli | Space |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| **Heavy** (kept) | 121 | 102 | 85 | 130 | 120 | 114 | 107 | 106 | 136 | 116 | 105 | 76 | 69 | 67 | 65 |
| **Medium** (dropped) | 127 | 87 | 79 | 144 | 129 | 128 | 111 | 90 | 150 | 107 | 92 | 98 | 75 | 64 | 41 |

Biggest gaps: **Spaceship 65↔41**, **Fighter 76↔98**, **Superheavy 106↔90**, **Flak 102↔87**.
These are the closest two profiles available in the whole W24 scope — the Demolition/Concussion
and MissileHE/Concussion clusters diverge far more (`Scout 81↔144`, `None 79↔151`), which is why
they are **not** the first batch.

**2. The blast radius.** Heavy carries `Spread: 400`, Medium `Spread: 300`. Today the shot lands
half at each; afterwards all of it lands at 400. Same total damage, **wider spread**.
`review_resolve_diff` will show this — it is expected, not a mistake, but it IS a gameplay change
and it is why this batch needs your sign-off rather than being mechanical.

## Verification, in order

```sh
python tools/audit/review_resolve_diff.py TigerCannon          # repeat per weapon; expect ONLY
python tools/audit/review_resolve_diff.py HammerTankCannon     # the dropped Medium warhead and
python tools/audit/review_resolve_diff.py KotinCannon          # the Heavy Damage change
python tools/audit/review_resolve_diff.py Type97Cannon

python tools/audit/find_empty_warhead.py                       # MUST print 0
python tools/audit/audit_warhead_split.py                      # ratchet must go DOWN, never up
python tools/audit/audit_duplicate_inherits.py                 # the boot-crash class grep can't find
python tools/balance/extract_stats.py                          # re-extract BEFORE committing
python tools/audit/audit_balance_drift.py                      # must read clean
```

Then the gate that only your machine can run:

```
launch-game.cmd  →  main menu
  perf.log ends with MenuPostProcessEffect.PostWorldLoaded
  NO new exception-*.log in %APPDATA%/OpenRA/Logs   (snapshot the list BEFORE launching)
```

In-game spot check: build the Tiger (RA1 Shared), Hammer and Kotin (RA1 Soviets), Type 97 (RA1
Japan); fire each at a building and at infantry. Damage per shot should feel unchanged; the
splash should read slightly wider.

## Commit

```sh
git fetch origin master && git merge origin/master     # AGENT_WORKSPACE git rule 1
git add mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml \
        mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml \
        mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml \
        docs/balance
```

⚠ Scoped `add` only — other contributors have live WIP (CLAUDE.md rule 2, hook-enforced).
Re-extract and commit the ledgers **with** the yaml, never after.

## The next two batches, already surveyed

| cluster | n | files | note |
|---|--:|--:|---|
| `Demolition_Heavy` + `Concussion_Medium` | 4 | 3 | ⚠ profiles diverge hard (`Scout 81↔144`, `Steel 164↔105`); `TSBoatcannon` is the only member NOT on the broadcast fingerprint (2000 + 16000) |
| `MissileHE_Heavy` + `Concussion_Medium` | 3 | 2 | ⚠ `None 79↔151` — the widest gap of the three |
