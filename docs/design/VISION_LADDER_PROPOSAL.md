# Vision ladder — 27 unique sight values, for approval

> ⛔ **A PROPOSAL. NOTHING IS APPLIED.** `class_anchors.json` is untouched; `reveals_shroud` is a
> balance number and only the maintainer writes one.

**Maintainer ruling 2026-09-03:** *"Artillery needs spotters but they should all have a unique
vision range."*

## §1 — The gap this closes

| | |
|---|--:|
| classes | 27 |
| **distinct sight values today** | **9** |
| classes sharing a value with another | **24 of 27** |

`8000` is used by seven classes (`commando`, `dreadnought`, `epic_vehicle`, `fire_support`,
`heavy_sniper`, `line_breaker`, `scout_vehicle`); `5000` by six. Vision is currently almost
undifferentiated.

## §2 — ⛔ Why a formula cannot do this

The obvious approach — derive sight from weapon range — **cannot produce 27 unique values, because
classes share weapon ranges.** Two attempts, measured:

| approach | distinct values |
|---|--:|
| proportional (`range × 0.60` for spotters, `range + 1500` otherwise) | **15 of 27** |
| fixed gap (`range − 3000` for spotters, `range + 1500` otherwise) | **14 of 27** |

`mortar` and `pure_sniper` both sit at range 10,000; `flying_infantry`, `heavy_infantry`,
`light_tank` and `scout` all at 5,000. Any function of range collapses them. **Uniqueness has to be
a designed ladder, and that is what this is.**

## §3 — The ladder

Ordered by how far a class should **see**, which is deliberately not the order of how far it
**shoots**: scouting classes see far beyond their reach, brawlers barely past it, and the five
artillery families fall short of their own range so they still need a spotter.

| class | range | sight now | **proposed** | Δ vs range | move | |
|---|--:|--:|--:|--:|--:|---|
| `scout_vehicle` | 4,500 | 8,000 | **12,500** | +8,000 | +4,500 | |
| `scout` | 5,000 | 5,000 | **12,000** | +7,000 | **+7,000** | |
| `flying_infantry` | 5,000 | 5,000 | **11,500** | +6,500 | **+6,500** | |
| `pure_sniper` | 10,000 | 10,000 | **11,000** | +1,000 | +1,000 | |
| `commando` | 8,000 | 8,000 | **10,750** | +2,750 | +2,750 | |
| `mortar` | 10,000 | 10,000 | **10,500** | +500 | +500 | |
| `artillery` | 15,000 | 9,000 | **10,000** | **−5,000** | +1,000 | spotter |
| `heavy_sniper` | 8,000 | 8,000 | **9,750** | +1,750 | +1,750 | |
| `artillery_tank` | 12,000 | 9,000 | **9,500** | **−2,500** | +500 | spotter |
| `special_forces` | 6,000 | 6,000 | **9,250** | +3,250 | +3,250 | |
| `archer` | 7,000 | 7,000 | **9,000** | +2,000 | +2,000 | |
| `rocket_trooper` | 6,500 | 6,500 | **8,750** | +2,250 | +2,250 | |
| `tank_destroyer` | 7,500 | 7,500 | **8,500** | +1,000 | +1,000 | |
| `dreadnought` | 7,000 | 8,000 | **8,250** | +1,250 | +250 | |
| `anti_air_vehicle` | 6,000 | 7,000 | **8,000** | +2,000 | +1,000 | |
| `fire_support` | 10,000 | 8,000 | **7,750** | **−2,250** | −250 | spotter |
| `high_tech_tank` | 6,500 | 7,000 | **7,500** | +1,000 | +500 | |
| `missile_vehicle` | 8,000 | 7,000 | **7,250** | **−750** | +250 | spotter |
| `light_tank` | 5,000 | 6,000 | **7,000** | +2,000 | +1,000 | |
| `mbt` | 5,500 | 6,000 | **6,750** | +1,250 | +750 | |
| `epic_vehicle` | 8,500 | 8,000 | **6,500** | **−2,000** | −1,500 | spotter |
| `grenadier` | 5,500 | 5,500 | **6,250** | +750 | +750 | |
| `heavy_infantry` | 5,000 | 5,000 | **6,000** | +1,000 | +1,000 | |
| `line_breaker` | 2,500 | 8,000 | **5,750** | +3,250 | −2,250 | |
| `closecombat` | 3,500 | 5,000 | **5,500** | +2,000 | +500 | |
| `melee` | 1,500 | 5,000 | **5,250** | +3,750 | +250 | |
| `support` | 0 | 5,000 | **5,000** | — | 0 | no weapon |

**27 distinct values. Zero constraint violations** — every non-spotter sees at least as far as it
shoots; every spotter falls deliberately short.

## §4 — ⚠ Read this before approving

**Five moves are large enough to change how the game plays:**

| class | now → proposed | |
|---|---|---|
| `scout` | 5,000 → **12,000** | **+7,000** — a scout that cannot out-see a rifleman is not a scout, but this is a big buff |
| `flying_infantry` | 5,000 → **11,500** | +6,500 |
| `scout_vehicle` | 8,000 → **12,500** | +4,500 |
| `special_forces` | 6,000 → **9,250** | +3,250 |
| `line_breaker` | 8,000 → **5,750** | **−2,250** — the only large *cut*; a 2,500-range brawler seeing 8,000 was the outlier |

⚠ **Two judgement calls I made and should be checked:**

1. **`pure_sniper` and `mortar` are NOT treated as spotters.** Both currently have sight exactly
   equal to range (10,000), and a classic sniper or mortar arguably *should* out-range its own
   vision. I gave them a small surplus instead, so the "sees what it shoots" rule holds for
   everything outside the five named artillery families. **Moving either into the spotter set is a
   one-line change.**
2. **The ladder's ORDER is a design claim.** The numbers are mechanical once the order is fixed
   (250-unit steps), but the order says, for example, that a `commando` should see further than a
   `heavy_sniper`, and that `epic_vehicle` sees less than an `mbt`. Those are judgements.

## §5 — Apply

Nothing is applied. On approval, `reveals_shroud` for all 27 classes changes in
`class_anchors.json`, and the values should be pinned in `doc_claims.yaml` as a set — the artillery
deficits are deliberate and will otherwise read as defects, exactly like the dreadnought range
(`CLASS_STATUS_BOARD.md` §10).
