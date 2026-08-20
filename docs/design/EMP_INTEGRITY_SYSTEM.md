# EMP / Integrity Auto-Scale System + Warhead Falloff/Tesla-tier plan

**Handoff to Devin — 2026-08-10 (Claude).** Read this whole file before touching warheads.
Ownership rule still holds: **one editor on `weapons.yaml` / `gen_weapon_template.py` at a time** —
announce before you start. Boot-gate every commit (perf.log `MenuPostProcessEffect.PostWorldLoaded`,
no new `exception-*.log` past baseline 169). Scoped `git add <files>` only.

---

## 1. DONE + shipped this session (commit `0d2cd6e82`, boot-gated)

### 1a. Integrity/EMP now auto-scales with damage (C#)
New field **`AreaDamage.IntegrityScale`** (`OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs`,
method `ApplyIntegrityScale`, a direct mirror of `ApplyPhysicalState`). On hit it drains the
victim's `Integrity` pool by `damage × IntegrityScale / 100`, using the **real post-armor/falloff
damage** — so EMP self-adjusts to the weapon's output, never hand-set. Also added the same call to
`AreaDamagePercentageWarhead.InflictDamage`.

Generator config `FAMILY_INTEGRITY_SCALE` (Tesla-content law = `100 × Tesla-parents / total-parents`):

| Family | Parents | IntegrityScale |
|---|---|---|
| Tesla / TeslaCharged | pure Tesla | **100** |
| Storm | Tesla + Magic | **50** |
| Quantum | Railgun + Laser + Tesla | **33** |

Emitted on the **main AreaDamage warhead only** (the %-twin is `AreaDamagePercentage` which *can*
hold it, but a concrete weapon that restates `: HealthPercentageDamage` would then inherit an invalid
field → crash, so kept main-only for safety). Flat `Warhead@EMPUnit: AffectsIntegrity` is now
**upgrade-only** (a flat bonus stacked on top; or an upgraded weapon just carries a higher
IntegrityScale so its bonus EMP also scales).

### 1b. All %-twins unified on `AreaDamagePercentage`
Was 80 `HealthPercentageDamage` + 8 `AreaDamagePercentage`; now **all 85 families** use the Cameo
`AreaDamagePercentage`. Behaviour-preserving drop-in (fields ⊆ HealthPercentageDamage, no
`ValidRelationships: Ally` ⇒ no new friendly fire). `verify_generator_sync` drift = 0. 4 `HealthPercentageDamage` remain: Sniper template +
3 concrete Demolition weapons that restate the type — harmless.

### 1c. Storm collapses (Ixian faction-signature wiring, finished)
- RA2 `LightningStorm` cloud-weapon (`LightningBolt` in RedAlert2/Shared/weapons.yaml) →
  `^Warhead_Storm_Super`; dropped its old duplicate `Warhead@TeslaChargedExtraDamage` key.
- Ixian `D2K_ShockGun` family (Tesla+Railgun) → Storm, matching the `StormGun` family.
- Stripped the flat `Warhead@EMPUnit` off the whole `StormGun` family + `StormLasher` (they auto-scale
  via IntegrityScale 50 now).

---

## 2. How the integrity/EMP mechanic ACTUALLY works (answers the maintainer's question)

The `Integrity` trait is defined 3× in `mods/cameo/rules/defaults.yaml` (`^Infantry`/`^Vehicle`/
`^Building`, ~lines 695/1955/3975):

```
Integrity:
    MaxPercentageStrength: 100     # pool = 100% of the actor's MaxHP
    AffectedByDamageTypes: Tesla   # ONLY "Tesla"-typed damage drains it passively
    ActiveCondition: electronics   # granted while Strength > 0
    RegenAmount: 1000              # regens after DamageRegenDelay: 75 ticks
GrantCondition@electronics: { Condition: empdisable, RequiresCondition: !electronics }
# integrity depleted -> lose "electronics" -> gain "empdisable" -> black overlay + lockdown = DISABLED
```

**There are TWO drain paths, and both fire on a Tesla-typed hit of `D` damage:**
1. **Passive** (`INotifyDamage`, pre-existing): if the damage carries the `Tesla` damage type, integrity
   drops by `D` (1:1). Non-Tesla damage does nothing to integrity.
2. **Active** (new `IntegrityScale`): integrity drops by `D × Scale / 100` on top.

So combined drain per hit = `D × (passive + Scale/100)`, where `passive = 1` if the weapon is
Tesla-typed else `0`. Pool = 100% MaxHP, so the unit is **disabled** once cumulative drain = MaxHP,
i.e. at **HP% = 100 / (passive + Scale/100)**:

| Weapon | Tesla-typed? | drain multiple | **disabled at HP%** |
|---|---|---|---|
| Tesla / TeslaCharged | yes (DamageTypes has `Tesla`) | 1 + 1.00 = **2.0×** | **50% HP** |
| Storm | yes (`Tesla`) | 1 + 0.50 = **1.5×** | **~67% HP** |
| Quantum | **NO** (`ExplosionDeath` only) | 0 + 0.33 = **0.33×** | **~300% (never)** ⚠ |
| any non-electric weapon | no | 0 | never (correct) |

**Direct answers to the maintainer:**
- *"At 100% does integrity deplete same %-wise as HP → never disabled before death?"* — That's true for
  the **passive drain alone** (1:1 → disable == death). But `IntegrityScale 100` adds a **second** 1:1
  drain, so Tesla weapons drain integrity at **2× the HP rate → disabled at 50% HP**, well before death.
  The whole point of IntegrityScale is to push the drain *past* the 1:1 passive rate.
- *"Upgrade 100%→200% → disable at 50% HP?"* — Close, but it's **33% HP**, because the passive 1× is
  still there: `1 + 2.00 = 3× → 100/3 ≈ 33%`. (Your math assumed IntegrityScale was the only source.)
- **⚠ Quantum bug this exposes:** Quantum is NOT Tesla-typed, so it has no passive drain and only
  `0.33×` from IntegrityScale → it effectively **never disables** (needs 300% HP). Fix options: (a) add
  `Tesla` to Quantum's `DamageTypes` (thematic — it has a Tesla component → 1.33× → disable at ~75% HP),
  or (b) bump Quantum's IntegrityScale. **Recommend (a).** MAINTAINER DECISION NEEDED.
- Tuning knobs if 50%/67% feels wrong: the pool size (`MaxPercentageStrength`), the IntegrityScale
  values, or whether the passive `AffectedByDamageTypes: Tesla` drain counts at all.

---

## 3. PENDING — proposed, NEED maintainer confirm before regenerating

### 3a. Falloff restructure (maintainer wants all profiles to end in 0, 6 values)
Current default `falloffs` tuple in `gen_weapon_template.py` `family()` (indexed by level
Light/Medium/Heavy/Super) is 5-value and doesn't end in 0. Proposed new default set (per maintainer):

| Level | NEW falloff |
|---|---|
| Light  | `100, 50, 33, 25, 20, 0` |
| Medium | `100, 60, 30, 15, 5, 0` |
| Heavy  | `100, 50, 25, 10, 5, 0` |
| Super  | `100, 40, 20, 10, 5, 0` |

Plus special per-family overrides (add a `FAMILY_FALLOFFS` dict, like `FAMILY_SPREADS`):
- **Even / linear** (opt-in for families that want a flat line): `100, 80, 60, 40, 20, 0`
- **Bullet**: `100, 0` (pure max-radius linear)
- **Nuclear**: `100, 90, 80, 70, 60, 50, 40, 30, 20, 10, 0` (11 values)

⚠ **OPEN QUESTION for maintainer:** *which* families should use the "even" profile vs the default
per-level set? (Bullet and Nuclear are specified; the rest unclear.) Get the list before regenerating.
The C# already handles arbitrary Falloff length (`effectiveRange = MakeArray(Falloff.Length,...)`), so
6/11/2-value falloffs all work.

### 3b. Tesla → full 4-tier family (add Super; TeslaCharged = the reference)
Today: `^Warhead_Tesla_{Light,Medium,Heavy}` + separate `^Warhead_TeslaCharged_Super`. Maintainer wants
a unified 4-tier Tesla (Light/Medium/Heavy/Super) with the **ExtraDamage chip scaled from the
TeslaCharged (Super) anchor down**. The current Tesla chip == the Medium tier and TeslaCharged == Super,
which happen to differ by a flat +100, so a clean **+50 / tier** step falls out. Proposed chip Versus:

| Armor | Light | Medium (=old Tesla) | Heavy | Super (=old TeslaCharged) |
|---|---|---|---|---|
| Shield | 250 | 300 | 350 | 400 |
| Heroic | 150 | 200 | 250 | 300 |
| Plate  | 125 | 175 | 225 | 275 |
| Flak   | 100 | 150 | 200 | 250 |
| None   | 75  | 125 | 175 | 225 |
| Superheavy | 50 | 100 | 150 | 200 |
| Heavy  | 25  | 75  | 125 | 175 |
| Medium | floor | 50 | 100 | 150 |
| Light  | floor | floor | 75 | 125 |

Chip `Damage = main_damage / 2` and `Spread` still auto-scale with the weapon. The main (non-chip)
Versus keeps its sloped ladder, extended to the Super step. IntegrityScale stays 100 on all 4 tiers.
Open: does TeslaCharged stay as an alias for Tesla_Super, or get retired? MAINTAINER DECISION.

---

## 3c. DONE (Devin, 2026-08-10) — upgraded-weapon IntegrityScale bump + missing chip DamageTypes

Two bugs found while investigating why RA1 Tesla Doctrine / RA2 Tesla Overload upgrades still
drained integrity at the same ~150% ratio as the un-upgraded weapon instead of ~200%:

1. **Missing `DamageTypes: Tesla` on several standalone `TeslaExtraDamage`/`TeslaChargedExtraDamage`
   chips** (`SpreadDamage`, no `IntegrityScale` field — passive drain is their ONLY integrity
   mechanism per §2). Added it to the affected chips in RA1 Soviets/Japan, RA2 Soviets/Shared, D2K
   Ixian weapons, and to the `^Warhead_Tesla_*` / `^Warhead_Quantum_*` / `^Warhead_Storm_*`
   template chips (main + `_Percentage` twins already carried `IntegrityScale`; now the chip carries
   the passive `Tesla` type too).
2. **Upgraded Tesla weapons didn't scale IntegrityScale** — per §1a "an upgraded weapon just
   carries a higher IntegrityScale so its bonus EMP also scales", added `IntegrityScale: 150`
   (maintainer-picked value, up from the template default 100) to the main `AreaDamage` warhead of
   every genuine upgrade-gated Tesla variant and its arc fragments: RA1 `PortaTesla_EMP`,
   `TTankZap_EMP`, `TTankZap2_EMP`, `TeslaZap_EMP` (fragments inherit it); RA2 `RA2CoilBolt2`,
   `RA2OPCoilBolt2`, `RA2TankBolt2`, `RA2PortaTesla2` + `TeslaFragment`/`TeslaFragment2`/
   `TeslaFragmentLarge`/`TeslaTankFragment`/`TeslaTankFragment2`. Left ALWAYS-ON EMP weapons
   (`ZapperPortaTesla`, `MammothTuskTesla`, `RA2OPCoilBolt1`, `TeslaFragmentWeak`) at the template
   default 100 — they are not gated behind the Doctrine/Overload upgrade condition, so their ratio is
   correct as-is.

Generator (`gen_weapon_template.py`) updated to keep future-generated families in sync:
`emit_chip` now tags integrity-affecting families' `_ExtraDamage` chip with `DamageTypes: Tesla`,
and the `_Percentage` twin (`AreaDamagePercentage`) now also receives `IntegrityScale` alongside the
main warhead.

Boot-gated (`MenuPostProcessEffect.PostWorldLoaded`, no new `exception-*.log`). Commit `87512a045`
(amended for the doc update and commit-trailer fix in this pass).

**Not done** (still queued, see §4 below): the flat-EMP double-count cleanup on always-on Tesla
weapons and `^Effect_Tesla_*` templates — out of scope for this pass, which only targeted the
upgrade-ratio discrepancy.

---

## 4. Follow-up sweep (queued, not started) — the flat-EMP cleanup
Now that Tesla/Storm/Quantum mains auto-scale integrity, the OLD flat EMP sources **double-count**:
1. `^Effect_Tesla_Heavy` (EMP 1000) + `^Effect_Tesla_Super` (EMP 2000) still carry a flat
   `Warhead@EMPUnit`. Remove them — BUT first audit the ~100 `^Effect_Tesla_*` inheritors to confirm
   each also inherits a Tesla/Storm/Quantum **warhead** (else it silently loses EMP). Script the check.
2. Concrete Tesla/Quantum weapons across factions with explicit `Warhead@EMPUnit` overrides → strip
   them (they auto-scale now), keep flat EMP only on genuine **upgrade** weapons.
3. Wire the Steel Consortium advanced-Quantum upgrade EMP (higher IntegrityScale and/or flat on top).
4. Add an audit (like `audit_warhead_split`): flat `AffectsIntegrity` only on upgrades; Tesla/Storm/
   Quantum mains carry the right IntegrityScale. Then re-run `bash tools/audit/run_all.sh`.

## 5. How to continue (Devin)
- The generator is the source of truth. Workflow: edit `gen_weapon_template.py` →
  `python tools/balance/splice_templates.py <families…>` → `python tools/balance/verify_generator_sync.py`
  (expect drift=1 Sniper) → `python tools/audit/find_empty_warhead.py` (expect 0) → boot-gate → commit.
- **Get the two maintainer decisions first** (§3a which families use "even"; §3b TeslaCharged fate;
  §2 Quantum Tesla-type). Don't regenerate before that — a full re-splice touches every family.
- Sign your commits `Co-Authored-By: Devin AI <devin@cognition.ai>` — never the Claude trailer.
- Full context: this file + `docs/design/PHYSICAL_STATE_SYSTEM.md` §5 + memory
  `cameo-physical-state-program`.

## 6. Letter to Claude — 2026-08-10

Hi Claude,

We just finished the second half of the Tesla double-fire / integrity-drain fix. Quick handoff so you can pick up from here.

### What we did
- **Renamed 136 `Warhead@TeslaExtraDamage` / 41 `Warhead@TeslaChargedExtraDamage` local keys** to the template-matching `Warhead@Tesla_Heavy_ExtraDamage` / `Warhead@Tesla_Super_ExtraDamage` forms across the faction `weapons.yaml` files plus `mods/cameo/weapons/*.yaml`. This eliminates the "double-fire" where a child weapon inherited a new template chip (`Tesla_Heavy_ExtraDamage`) while still defining an old-key chip (`TeslaExtraDamage`), so both fired.
- **Found and fixed a related regression**: several renamed standalone chips had `DamageTypes: Tesla` stripped because it looked redundant. It is not redundant — these chips do not inherit `^Warhead_Tesla_*_ExtraDamage` templates (none exist yet), so the `DamageTypes` line is the only way the extra-damage warhead triggers passive integrity drain. I restored `DamageTypes: Tesla` to **84** `Tesla_Heavy_ExtraDamage` / `Tesla_Super_ExtraDamage` blocks where it was missing.
- **Left the inherited old-template `Warhead@TeslaExtraDamage`/`TeslaChargedExtraDamage` alone** for now: 89 / 20 weapons still carry them from templates like `^TeslaWeapon`, but they are not double-firing because no matching new chip is present. They are queued for the §4 flat-EMP cleanup pass, not this one.

### Verification (all green)
- `tools/audit/find_empty_warhead.py`: 0
- `tools/audit/find_orphan_old_keys.py`: 0 real Bug B orphans
- `check_teslaextradamage.py`: 0 double-fires
- `check_tesla_charged_extra.py`: 0 double-fires
- `launch-game.cmd` boot-gate: reached `MenuPostProcessEffect.PostWorldLoaded`, no new `exception-*.log`

### Working-tree warning for the next agent
The repo currently contains a lot of **unrelated work-in-progress** outside the Tesla scope: regenerated `docs/audit/latest/*` reports, `docs/factions/MATRIX.md` changes, the untracked `tools/audit/audit_damage_grid.py` / `tools/balance/_requantize_ledgers.py`, and many non-Tesla weapon tweaks in the same `weapons.yaml` files. **Do not use `git add -A` or `git add .` for the Tesla commit.** Scoped `git add` of the Tesla-affected `weapons.yaml` files only is required.

### Next step
The feature branch `fix/tesla-integrity-upgrade-drain` is clean enough to commit (with a scoped add). After that, the §4 "flat-EMP cleanup" can begin: strip legacy `Warhead@EMPUnit` from non-upgrade Tesla/Storm/Quantum weapons and remove the inherited old-key `TeslaExtraDamage`/`TeslaChargedExtraDamage` from templates when safe.

— Devin
