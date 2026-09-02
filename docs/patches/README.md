# Patches waiting for a boot machine

A cloud container has no `engine/` build and no `%APPDATA%/OpenRA/Logs`, so the boot gate
(CLAUDE.md rule 1, enforced on `git commit` by `bash_guard.py`) is **unsatisfiable there by
construction**. Engine content authored in such a session lands here as a `git apply`-able patch
plus a section saying what was verified, what was NOT, and the exact apply-verify-boot-commit
sequence. **Delete the patch in the same commit that lands it**, so this directory never holds a
change that is already in the tree.

---

## `01_bulletchem_family.patch` — the `BulletChem` warhead family (2026-09-02)

**Maintainer order, 2026-09-02:** *"For the hydralisk I'm thinking about a new BulletChem that is
like Bullet x Chemical so it's more similar to what it was before but more damage against infantry
and aircraft with a little bit damage against tanks from the chemical side."*

Adds `^Warhead_BulletChem_{Light,Medium,Heavy}` as the **bullet-delivery member of the Chem set**,
alongside the existing `CannonChem` and `MissileChem`. It exists because `HydraSpit` stacks
`^SmallArms` (Bullet) + `^LightChemicalWeapon` + `^LightMissile` + `^ArrowWeapon`, and no existing
family reproduces a corrosive small-arms round — collapsing it to plain `Chemical` moved every
ground matchup ([`../design/W24_COLLAPSE_REVIEW.md`](../design/W24_COLLAPSE_REVIEW.md)).

**Two files, and they must land TOGETHER:**

| file | change |
|---|---|
| `tools/balance/gen_weapon_template.py` | +16 lines: one `BLEND_FAMILIES` entry, one `FAMILY_DAMAGE_TYPES` entry |
| `mods/cameo/weapons/weapons.yaml` | the `--all` regenerate: the 3 new templates, plus ±1 rounding on the DERIVED rows (`REFLECTOR`, `COMPOSITE`, `HAZMAT`) of the other families |

⚠ **Splitting them breaks a gate.** The generator alone makes `verify_generator_sync.py` report
drift 3; the yaml alone is a hand-edited generated file. The ±1 rounding on the derived rows is
CLAUDE.md rule 8d working as designed — *"adding a family re-ranks the shield-coupling ladder and a
partial splice leaves drift"* — which is why the regenerate was `--all` and not a subset.

### Apply, verify, boot, commit

```sh
git apply docs/patches/01_bulletchem_family.patch
python tools/balance/verify_generator_sync.py     # expect: 142 templates, drift = 0
python tools/audit/audit_family_uniqueness.py     # expect: OK, no shared radius+curve
python tools/audit/audit_heaviness_bell.py        # expect: 0 inversions, 0 mean drift
python tools/audit/find_empty_warhead.py          # expect: 0
launch-game.cmd                                   # to the main menu; no new exception-*.log
git add tools/balance/gen_weapon_template.py mods/cameo/weapons/weapons.yaml
git rm docs/patches/01_bulletchem_family.patch
```

### Verified here, without a boot

| check | result |
|---|---|
| `verify_generator_sync.py` | **142 templates, drift 0** — no-op regenerate (139 before) |
| `audit_family_uniqueness.py` | **OK** — 47 distinct Light shapes, 46 Medium, 46 Heavy; no two families share both a radius and a curve |
| `audit_heaviness_bell.py` | 49 families, **0 ladder inversions, 0 mean drift**; flat families still 2 (`Magic`, `Sonic`) at the ratchet |
| `find_empty_warhead.py` | **0** of 2,870 nodes (the boot-NRE class) |
| `git apply --check` | clean against the restored tree |
| shape uniqueness by hand | radius = geometric mean of Bullet 100 and Chemical 1100 = **332**; falloff `100, 82, 61, 38, 0`. Nearest neighbour is `BulletFire` (346, `100, 83, 64, 43, 0`) — different on both axes |

### NOT verified, and it needs the boot machine

* **The engine has never parsed these three templates.** Nothing inherits them yet, so the
  blast radius is small, but the Python resolver does not catch junk trait nodes and only the
  engine does.
* **No weapon points at the family yet.** This patch adds the family and changes no unit. The
  `HydraSpit` collapse is a separate, later change.

### The measured profile, for review before anything inherits it

`^Warhead_BulletChem_Light`, against its two parents on the rows the order names:

| armor | Bullet | Chemical | → BulletChem |
|---|--:|--:|--:|
| None | 200 | 115 | **191** |
| Flak | 159 | 134 | **155** |
| Scout | 151 | 119 | **146** |
| Light | 110 | 125 | **126** |
| Fighter | 126 | 48 | **94** |
| Bomber | 80 | 50 | **63** |
| Helicopter | 66 | 54 | **53** |
| Spaceship | 53 | 60 | **45** |
| Heavy | 59 | 138 | **91** |
| Superheavy | 48 | 140 | **80** |

⭐ **The order's intent holds against the Chemical collapse it replaces**: vs infantry `None`
2.67× (Chemical 1.61×) and `Scout` 2.73× (2.22×); vs air `Fighter` 1.71× (0.87×) and `Bomber`
1.18× (0.93×); vs tanks `Heavy` 1.57× where pure Chemical gave 2.38× and pure Bullet would give
far less — *"a little bit damage against tanks from the chemical side"*, exactly.

⚠ **Two air rows go the other way and need a ruling before the Hydralisk is repointed:**
`Helicopter` 1.02× (Chemical 1.04× — flat) and `Spaceship` 0.92× (Chemical 1.22× — down). Both
parents are weak against those two rows, so the blend cannot lift them; only a third air-tilted
parent (`MissileAA`, as `PhotonCannon` does) could. Recorded, not decided.
