# W24 batch 03 — `MissileHE_Heavy` + `Concussion_Medium` (3 weapons) — ⛔ SPLIT BY ROLE

**Prepared 2026-08-30. Not applied — this container cannot boot-gate.**
Do batches **01** then **02** first. This one carries the widest profile gap in the surveyed set
**and** is the first cluster where the three members should probably **not** get the same answer.

## The three weapons

| weapon | file | MissileHE | Concussion | **total** |
|---|---|--:|--:|--:|
| `BigShieeTusk` | `ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml` | 40000 | 40000 | **80000** |
| `SandmarineTusk` | `ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml` | 40000 | 40000 | **80000** |
| `GradRockets` | `ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` | 8000 | 8000 | **16000** |

All three are the equal-split broadcast fingerprint. No percentage twins — verified.

## ⛔ THE COLLAPSE DIRECTION IS NOT OBVIOUS, AND IT INVERTS

Unlike batches 01 and 02, here the two candidate targets pull **opposite ways on both axes**:

| | None | Flak | Plate | Scout | Light | Super | Fighter | **Spread** |
|---|--:|--:|--:|--:|--:|--:|--:|--:|
| **MissileHE_Heavy** | **79** | 75 | 63 | 129 | 126 | **113** | **103** | **200** |
| **Concussion_Medium** | **151** | 107 | 92 | 144 | 126 | **82** | **72** | **350** |

* Collapse to **MissileHE** → blast **NARROWS** 350→200, much weaker vs `None` (79 vs 151), much
  stronger vs `Superheavy` and air.
* Collapse to **Concussion** → blast **WIDENS** 200→350, nearly **doubles** damage vs unarmoured
  infantry, loses the anti-heavy and anti-air edge.

`None 79↔151` is the widest gap in the whole surveyed set. Whichever way this goes, these weapons
feel different afterwards.

## The recommendation — and why it is a SPLIT

**`BigShieeTusk` and `SandmarineTusk` → `^Warhead_MissileHE_Heavy`.** They are "Tusk" mammoth-style
missile pods: single heavy targets, anti-armour and anti-air. The narrower 200 blast and the
`Superheavy 113` / `Fighter 103` rows are the behaviour they are supposed to have.

**`GradRockets` → `^Warhead_Concussion_Medium`.** A Grad is a Katyusha — **area saturation against
soft targets**, not a precision anti-armour missile. Collapsing it to MissileHE would narrow its
blast to 200 and halve its damage against unarmoured infantry (151→79), which is the opposite of
what the weapon is for.

⚠ **This is a recommendation from the weapon's ROLE, and role is a maintainer judgement — exactly
the thing `anchor_readiness` proved a classifier cannot do (17.6% against known labels).** If you
disagree, the table above is the evidence either way. Do not let the tooling pick.

## The edit

For the two Tusks, drop the Concussion inherit + node and set MissileHE Damage to **80000**.
For `GradRockets`, drop the **MissileHE** inherit + node and set Concussion Damage to **16000**.
Note the two Tusks share one file, so they are one edit site; `GradRockets` is a second.

## Verification and commit

Same order as batch 01. ⚠ `review_resolve_diff` will show a **large** blast/profile change on all
three — that is expected here and is the whole point of the sign-off.

```sh
git fetch origin master && git merge origin/master
git add mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml \
        mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml \
        docs/balance
```

In-game: fire the Grad at massed infantry — it must still shred them. Fire a Tusk at a heavy tank
and at aircraft — it must still hurt both. If either of those stops being true, the split went the
wrong way and the table above tells you which row to blame.
