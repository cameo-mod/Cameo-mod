# W24 batch 02 — collapse `Demolition_Heavy` + `Concussion_Medium` (4 weapons)

**Prepared 2026-08-30. Not applied — this container cannot boot-gate.**
Do **batch 01** (`w24_batch_01_cannonhe.md`) first: it is the low-risk one. This batch moves the
profile much further and needs a real design call.

## The four weapons

| weapon | file | Concussion | Demolition | **total** | split |
|---|---|--:|--:|--:|---|
| `TS155mm` | `weapons/tiberiansun.yaml` | 30000 | 30000 | **60000** | equal — broadcast fingerprint |
| `TSBomb` | `ContentPacks/TiberianSun/GDI/yaml/weapons.yaml` | 10000 | 10000 | **20000** | equal — broadcast fingerprint |
| `TSBusMortar` | `ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml` | 32000 | 32000 | **64000** | equal — broadcast fingerprint |
| `TSBoatcannon` | `ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml` | 2000 | 16000 | **18000** | **89% Demolition already** |

No percentage twins on any of the four — verified.

⭐ **`TSBoatcannon` is the SAFEST member, not the odd one out.** Its damage is already 89%
Demolition, so collapsing to Demolition barely changes what it does. Its Concussion 2000 is **not
declared locally** — it comes from the `^Warhead_Concussion_Medium` template default — and it
carries a local `Spread: 400` override on the Demolition half. **Do this one first** as the
in-batch canary.

## The edit

Drop `Inherits@wh: ^Warhead_Concussion_Medium`, delete the `Warhead@Concussion_Medium` node,
set the Demolition Damage to the total. `TSBoatcannon` has no local Concussion node to delete —
only the inherit.

```diff
 TS155mm:
-	Inherits@wh: ^Warhead_Concussion_Medium
 	Inherits@wh2: ^Warhead_Demolition_Heavy
 	Inherits@fx: ^Effect_Demolition_Heavy
 	…
-	Warhead@Concussion_Medium:
-		Damage: 30000
 	Warhead@Demolition_Heavy:
-		Damage: 30000
+		Damage: 60000
```

## ⛔ WHY THIS NEEDS A DESIGN CALL, NOT JUST A BOOT

**The profile shift is large** — far larger than batch 01's:

| | None | Flak | Plate | Scout | Light | Medium | Heavy | Super | Wood | Steel | Concrete |
|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| **Demolition_Heavy** (kept) | 154 | 142 | 124 | **81** | **76** | 75 | 74 | 72 | 195 | **164** | 136 |
| **Concussion_Medium** (dropped) | 151 | 107 | 92 | **144** | **126** | 109 | 106 | 82 | 148 | **105** | 90 |

`Scout 81↔144` and `Light 76↔126` mean these weapons get **markedly worse against light targets**
and **much better against Steel structures** (164 vs 105). For `TSBusMortar` and `TS155mm` — both
long-range indirect fire — that is a real role change, not a rounding difference.

**And the blast widens:** Concussion is `Spread 350`, Demolition `466`. The half that landed at
350 now lands at 466. (`TSBoatcannon` overrides to 400, so its shift is 350→400 — smaller again.)

**If you would rather keep the anti-light character**, the alternative is collapsing to
`^Warhead_Concussion_Medium` instead, which inverts every number above. Both are defensible;
neither is mine to pick.

## Verification and commit

Same order as batch 01 — `review_resolve_diff` per weapon → `find_empty_warhead` = 0 →
`audit_warhead_split` ratchet DOWN → `audit_duplicate_inherits` → `extract_stats` →
`audit_balance_drift` clean → **boot to the main menu, no new `exception-*.log`**.

```sh
git fetch origin master && git merge origin/master
git add mods/cameo/weapons/tiberiansun.yaml \
        mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml \
        mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml \
        docs/balance
```

In-game: TS 155mm artillery and the Bus Mortar against infantry (should feel WEAKER) and against
a Steel building (should feel STRONGER). That contrast is the whole design question.
