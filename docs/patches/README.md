# Patches waiting for a boot machine

A cloud container has no `engine/` build and no `%APPDATA%/OpenRA/Logs`, so the boot gate
(CLAUDE.md rule 1, enforced on `git commit` by `bash_guard.py`) is **unsatisfiable there by
construction**. Engine content authored in such a session lands here as a `git apply`-able patch
plus a section saying what was verified, what was NOT, and the exact apply-verify-boot-commit
sequence. **Delete the patch in the same commit that lands it**, so this directory never holds a
change that is already in the tree.

---

## `01_bulletchem_hydraspit.patch` — the `BulletChem` family + the Hydralisk collapse (2026-09-02)

**Maintainer order, 2026-09-02:** *"For now it's more important to reduce everything down to a
single warhead and we can then make new warheads. For the hydralisk I'm thinking about a new
BulletChem that is like Bullet x Chemical so it's more similar to what it was before but more
damage against infantry and aircraft with a little bit damage against tanks from the chemical
side."* Follow-up ruling: accept the air rows as the blend produces them, and repoint `HydraSpit`
in this same patch.

Adds `^Warhead_BulletChem_{Light,Medium,Heavy}` as the **bullet-delivery member of the Chem set**
alongside `CannonChem` and `MissileChem`, and collapses `HydraSpit` from four damage mains onto it.
Measured before/after: [`../design/W24_COLLAPSE_REVIEW.md`](../design/W24_COLLAPSE_REVIEW.md) §8.

### Six files, and they must land TOGETHER

| file | change |
|---|---|
| `tools/balance/gen_weapon_template.py` | +16: one `BLEND_FAMILIES` entry, one `FAMILY_DAMAGE_TYPES` entry |
| `mods/cameo/weapons/weapons.yaml` | the `--all` regenerate: 3 new templates, plus ±1 on the DERIVED rows (`REFLECTOR`, `COMPOSITE`, `HAZMAT`) of the other families |
| `mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml` | `HydraSpit`: four legacy inherits → the 3-way split, one main at 72,000 |
| `tools/audit/intentional_composites.py` | −26: removes `HydraSpit`'s two quarantine entries (see below) |
| `docs/audit/intentional_weapon_composites.json` | regenerated: 226 → 225 entries |
| `tools/audit/audit_three_way_split.py` | raw ratchet 340 → **339**, the one weapon this patch consolidates |

⚠ **Splitting them breaks a gate.** The generator alone makes `verify_generator_sync.py` report
drift 3; the yaml alone is a hand-edited generated file; the weapon alone leaves the composite
manifest refusing to regenerate. The ±1 on the derived rows is CLAUDE.md rule 8d working as
designed — *"adding a family re-ranks the shield-coupling ladder and a partial splice leaves
drift"* — which is why the regenerate was `--all`.

### ⛔ This patch reverses an earlier maintainer decision, deliberately

`tools/audit/intentional_composites.py` held `HydraSpit` under *"maintainer-approved role blend"*
with this rationale, already in the tree before Blackrobe raised it:

> *"Restore the pre-PR-287 Hydralisk profile after the Chemical-Light fold raised real ground
> damage by roughly 1.6x to 2.38x and quadrupled the flat corrosion feed. Preserve the exact
> four-part behavior."* — review reference: *"Maintainer regression report: Hydralisk was not
> previously this strong"*

So the fold was tried once, reverted, and quarantined. **This patch removes that quarantine on the
2026-09-02 ruling** — the collapse now goes to `BulletChem` rather than to `Chemical`, which is the
difference the ruling turns on. If the Hydralisk is again *"not previously this strong"* in play,
that is the decision to revisit, and this note is where the history is.

### Apply, verify, boot, commit

```sh
git apply docs/patches/01_bulletchem_hydraspit.patch
python tools/balance/verify_generator_sync.py       # 142 templates, drift = 0
python tools/audit/audit_family_uniqueness.py       # OK
python tools/audit/audit_heaviness_bell.py          # 0 inversions, 0 mean drift
python tools/audit/find_empty_warhead.py            # 0
python tools/audit/intentional_composites.py        # PASS 225
python tools/audit/audit_three_way_split.py         # raw 339/339
python tools/audit/audit_physical_state_warheads.py # PASS
launch-game.cmd                                     # main menu; no new exception-*.log
git add tools/balance/gen_weapon_template.py mods/cameo/weapons/weapons.yaml \
        mods/cameo/ContentPacks/StarCraft/Zerg/yaml/weapons.yaml \
        tools/audit/intentional_composites.py tools/audit/audit_three_way_split.py \
        docs/audit/intentional_weapon_composites.json
git rm docs/patches/01_bulletchem_hydraspit.patch
```

### Verified here, without a boot

| check | result |
|---|---|
| `verify_generator_sync.py` | **142 templates, drift 0** (139 before) |
| `audit_family_uniqueness.py` | **OK** — no two families share both a radius and a curve |
| `audit_heaviness_bell.py` | 49 families, **0 inversions, 0 mean drift** |
| `find_empty_warhead.py` | **0** of 2,870 nodes |
| `audit_physical_state_warheads.py` | **PASS** |
| `intentional_composites.py` | **PASS 225** — and the regenerate was checked entry-by-entry: **0 added, 1 removed (`HydraSpit`), 0 changed beyond their digests**. Nothing was silently re-blessed |
| `audit_three_way_split.py` | raw **339/339** — the ratchet was lowered by exactly the one weapon consolidated, never raised |
| resolved `HydraSpit` | ONE damage main (`AreaDamage`, 72,000) + the family effect layer; `Range` 5979, `ReloadDelay` 15, `Report`, `ValidTargets`, `TargetActorCenter` all preserved; projectile keeps `scmspore`, `Speed 2500`, `Width 25`, contrails, `TrailImage` |
| `git apply --check` | clean against the restored tree |

### NOT verified, and it needs the boot machine

* **The engine has never parsed the three new templates or the rewritten weapon.**
* **`Concrete` damage is new on this weapon** — `^Effect_Chem_Light` carries `DamagesConcrete: 100`
  and the old stack had none. Family standard, but it is a behaviour addition.
* **Impact audio is new** — `^Effect_Chem_Light` adds `firebl3.aud`; the explosion sprite stays
  `sczhsplash` via a local override.
* No in-game feel test. The numbers below are arithmetic, not playtesting.

### What actually changed, measured

Per-armor resolved damage, before (4 × 18,000) vs after (1 × 72,000 on `BulletChem_Light`):

| armor | before | after | × | | armor | before | after | × |
|---|--:|--:|--:|---|---|--:|--:|--:|
| None | 51,480 | 137,520 | **2.67** | | Heavy | 41,760 | 65,520 | 1.57 |
| Wood | 26,640 | 74,160 | 2.78 | | Superheavy | 42,840 | 57,600 | 1.34 |
| Scout | 38,520 | 105,120 | **2.73** | | Fighter | 39,600 | 67,680 | **1.71** |
| Light | 39,600 | 90,720 | 2.29 | | Bomber | 38,520 | 45,360 | **1.18** |
| Flak | 51,480 | 111,600 | 2.17 | | Helicopter | 37,440 | 38,160 | 1.02 |
| Medium | 40,680 | 73,440 | 1.81 | | Spaceship | 35,280 | 32,400 | 0.92 |
| Plate | 51,480 | 89,280 | 1.73 | | ARMOR | 72,000 | 50,400 | 0.70 |
| Heroic | 38,520 | 68,400 | 1.78 | | BLAST | 72,000 | 50,400 | 0.70 |
| Concrete | 25,560 | 41,760 | 1.63 | | REFLECTOR | 72,000 | 59,040 | 0.82 |
| Steel | 34,200 | 53,280 | 1.56 | | COMPOSITE | 72,000 | 37,440 | 0.52 |

**mean ×1.46 · per-armor min 0.52, max 2.78, median 1.60.** Accepted by the ruling; the manual
per-faction pass is what stands behind it.

⭐ **Two things came out BETTER than the review predicted**, both because `BulletChem` is a
bullet-sized blend rather than a chemical one:

* **Splash SHRINKS.** The review's worry was that a Chemical collapse would put all 72,000 on the
  350-Spread ring. `BulletChem_Light` is `Spread 55`, `Falloff 100, 82, 61, 38, 0` — **radius 220**,
  against the old stack's 700 (Chemical) and 420 (Arrow). Tighter than every old warhead but one.
* **Corrosion is nearly preserved.** `11,520 → 13,493` per shot, **×1.17**, not the ×4.00 the
  Chemical collapse would have caused — because the Chem set's Light rung scales Corrosion at
  **20%**, not 100%.

⚠ **The percentage half rises hard: ×9.6.** The four old standalone `AreaDamagePercentage` twins
were `Damage: 1` against tiny Versus rows (11 / 2 / 16 / 16), so they delivered **0.45%** of max HP
vs `None`. The family fold at 72,000 gives 3,600 basis points × `PercentageVersus` = **4.32%**.
Both express the same "1% per 2,000 damage" convention; the old twins were hand-typed and never
tracked the weapon's real damage, which is precisely what `PercentageScale` exists to fix
(`AreaDamageWarhead.cs:99-110`). Flagged because it is the largest single move in this patch and it
scales with target max HP — say the word and a local `PercentageScale: 1042` reproduces the old
floor exactly.

⚠ **Air, as ruled: accepted as the blend produces it.** `Fighter` ×1.71 and `Bomber` ×1.18 are up;
`Helicopter` ×1.02 is flat and `Spaceship` ×0.92 is down. Neither parent has anti-air (Bullet
126/80/66/53, Chemical 48/50/54/60), so no weighting of the two lifts those rows — only a third
air-tilted parent could. Recorded, not fixed.
