# PREDICTOR-PARITY P1 — C# effective-damage model (impl spec, devin/t3verify/predictor-parity)

Port of the Python balance pipeline's effective-damage model into `OpenRA.Mods.CA`,
feeding `BotCombatPredictor` behind the `BM_live_combat_model` switch (default OFF;
classic path must stay bit-identical).

**Contract:** `docs/balance/derived/*.json` (from `tools/balance/extract_stats.py`
`derived_metrics` + `weapon_efficiency.analyse` + `effective_damage.effective_damage`).
C# must compute the same values **from rules at load** — never read the JSON at runtime.

## 1. File layout (all new except where noted)

| File | Content |
|---|---|
| `OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/MiniYamlMirror.cs` | Port of `tools/audit/miniyaml.py`: `Node` (key/value/children/file/line, `child(key)` = first child exact key, `children_named`, `get(*path)`), `load_text`, `merge_children`/`_merge_into` (override = last non-empty value wins; `-Key` removes; new keys append), `load_manifest`+`resolve_ref` (mod.yaml + `Include:` + `pkg\|rel` refs, `PACKAGE_PREFIXES = {cameo:"mods/cameo", ContentPacks:"mods/cameo/ContentPacks", common:"engine/mods/common"}`), `Ruleset` (`_merge_files`, `resolve`/`resolve_weapon` = recursive `Inherits`/`Inherits@` resolution, cycle-guard taint). **Mirror PYTHON semantics, not engine `MiniYaml.Merge`** — the fixtures were produced by this code. |
| `.../BotEffectiveDamage.cs` | Port of `tools/balance/effective_damage.py` + `formula.py` helpers (`parse_int32`, `parse_wdist` incl `40c0` cell notation → `_unchecked_int32(1024*first+sub)`, `parse_bool`, `fnum` = first comma token as float, `eff_reload`/`burst_delay_sum`, `charge_attack_cycle`, `charge_share`), `effective_heaviness.py`, `percentage_damage.py` — all constants verbatim (see `_model.json` for the dump). |
| `.../BotTargetModel.cs` | Port of `target_model.py`: `ARMORS` (16, order matters), `ARMOR_MACRO`, `DENSITY`, `ENGAGEMENT`, `A_BLOB`/`A_SELF`/`BLOB_UPTIME`, `REFERENCE_HP=200000`, `_condition_is_gate` (regex `[A-Za-z_][A-Za-z0-9_.]*`), census (`_scan`), `armor_weights` (incl. `Shield` row via `shield_damage_share`), `pseudo_armor_mean`, `shield_hp_factor`, `weighted_versus`, `effective_density`, `footprint_targets`, `is_damage_inert`. |
| `.../BotWeaponModel.cs` | `analyse` (`weapon_efficiency.py`) + `derived_metrics` (`extract_stats.py`) + `targets_factor`/`range_factor`/`deadzone_factor`/`overkill_factor`/`median_weapon_range` + `model_limitations` + `charge_up`/`charge_scalar`. Output = one record per weapon. |
| `.../BotWeaponModelTable.cs` | `Get(Ruleset)` via `ConditionalWeakTable`; lazily builds: `MiniYaml.Load`-equivalent via the mirror's `Ruleset(repoRoot)` over manifest `Rules:` + `Weapons:` lists, census, median range, armor weights. Runtime obtains `repoRoot`/file lists via `Game.ModData.DefaultFileSystem` + `Game.ModData.Manifest.Weapons` (manifest-only — Python never merges map yaml; map-defined weapons are a documented non-goal). |
| `BotCombatPredictor.cs` (edit) | `BotWeaponProfile` + `EffectiveDamagePerTick`; `BotUnitProfile` + per-armament effective models; `DamagePerTickAgainst(target, effective=false)`; `Predict(..., effective=false)` threading. |
| consumer call sites (edit) | Pass the flag from each owning module's `UseEffectiveDamageModel` field. |
| `OpenRA.Mods.Cameo.Test/WeaponModelParityTest.cs` | Parity test (§6). |
| `tools/ai/increment_switches.yaml` | `BM_live_combat_model` entry. |

## 2. Resolved-yaml view

`ResolvedWeapon`/`ResolvedActor` wrap the mirror's `Node` (post-`resolve*`). Every
Python `resolved.get(k)` → `node.Get(k)`; `resolved.child("Projectile")` → `node.Child("Projectile")`;
`resolved.get("Projectile","Speed")` → `node.Get("Projectile","Speed")`; `c.children` → `node.Children`;
`c.key`/`c.value` → `node.Key`/`node.Value`; `_field_present` → `node.Child(k) != null`.

Keys carry their `@TAG` verbatim (`Warhead@FooFriendlyFire` → `Key = "Warhead@FooFriendlyFire"`).

## 3. Per-term port notes (the gotchas)

* **`flat_damage_warheads`**: children where `key.StartsWith("Warhead@")` AND tag
  excludes `"FriendlyFire"` AND `value in {AreaDamage,SpreadDamage,TargetDamage}` AND
  `Damage` parses to Int32 and `> 0`. Type match is the yaml STRING, not a subclass
  test (`OpenToppedDamage`, `AffectsIntegrity`, `Dummy`, `WarpDamage` are excluded even
  though they are TargetDamage/DamageWarhead subclasses).
* **`percentage_applications`**: `key.StartsWith("Warhead")` — LOOSER (includes bare
  `Warhead:` nodes; a `Warhead:` key has no `@` so tag = whole key).
* **`effective_damage`** → `(effective, base_total, foot_total, avg_rel, sigma)`;
  reliability field = `rel_weighted/base_total`. AreaBeam applies `projectile_impact_multiplier`
  to eff + footprint but NOT `base_total`.
* **sigma**: `weapon_reliability_ctx` — `UNMODELED` → `(false, 0)`; `INSTANT` set →
  early return `(true, inaccuracy_at_range(inacc))` with the sub-cases:
  `CENTER_TARGET_ACTOR` + `direct_actor_impact` → inacc=0; `TRACKED_ZAP` + TrackTarget
  (default TRUE) → 0; else if not `INSTANT_SCATTER` → 0. `AreaBeam` TrackTarget
  (default FALSE!) → `(false,0)`. `Missile`: LockOnProbability≥99 (default 100) and
  LockOnInaccuracy≥0 (default −1 sentinel when ABSENT) → inacc=lock_inacc.
  `MOVING` dict: Bullet/ScaledBullet=17, Missile=384, AreaBeam=128, SpriteAthenaLaser=90,
  LinearPulse=6144. Bullet speed `"a,b"` → `(a+b−1)/2` when a≠b (nondecreasing enforced).
  ScaledBullet: `ProjectileSpeedPercentage`>0 AND speed==17 sentinel → `speed=rng*pct/100`
  (csharp_div); `InaccuracyPercentage`>0 AND inacc==0 → `inacc=rng*pct/100`.
  `inaccuracy_at_range`: PerCellIncrement → `csharp_div(inacc*rng,1024)`; Maximum/
  Absolute → inacc. drift = `LEAD*TARGET_SPEED*rng/min(speed,10000)` (min speed 1).
  Non-MOVING type with no authored `Speed` → `(true, inacc_at_range)` (hitscan-ish).
* **`Inaccuracy` is only read** when ptype ∈ INACCURACY_PROJECTILES.
* **`direct_actor_impact`**: AreaBeam→true; Railgun→DamageActorsInLine (false default);
  {SpriteRailgun,SmokeParticleRailgun}→LineWidth>0; LinearPulse→ImpactType
  (default "StandardImpact") lowercase ∈ {rectangle,cone,trapezoid};
  {InstantHit,InstantHitWithFakeBullets}→weapon-level `TargetActorCenter` truthy.
  Direct path reliability = `reliability([100,0],[0,POINT_TARGET_RADIUS=100],sigma)`,
  footprint/secondary = 0.
* **`falloff_and_radii`**: `Falloff` absent → `(100,37,14,5,0)`; present-but-empty →
  error. `Spread` absent/blank → 43. `Range` present → radii = parsed list (must be
  len 1 or == len(fo), nondecreasing); absent → `i*spread`. AreaDamage/
  AreaDamagePercentage: heaviness ≥ 0 scales spread + radii by `(h+2)/3` int-trunc
  (`scale_length`, Int32 overflow → error; positive collapsed duplicate front →
  HeavinessError). live = `len(fo)>=2 && len(radii)>=2`.
* **`runtime_falloff`**: exact segment walk, `csharp_div` interpolation, `int(distance)`
  floor, distance clamp ≥0 — returns 0 past last radius, extrapolates inward before
  radii[0] via first segment.
* **`footprint_cells2(fo,radii,cutoff)`**: sorted boundaries {0, limit, radii in (0,limit)},
  per segment midpoint → `_falloff_line` intercept+slope (slope /100, intercept /100−slope*inner),
  analytic ∫F(r)r dr; `2π·total/1024²`. cutoff = min(radii[-1], cutoff).
* **`area_geometry_samples`**: `area_tick_modifiers` — `Ticks` default 1; `TickDamage`
  present → must == Ticks count, weights `csharp_div(100*w,total)` when sum>0 else
  `csharp_div(100,ticks)` each. Per tick: `outer = radii[-1]`; if AUTHORED
  `MaxRadius>0 && ticks>1`: `outer = min_radius + csharp_div((max_radius-min_radius)*(tick+1),ticks)`
  (min/max scaled by heaviness for Area* types; authored_max_radius — the AUTHORED
  value — gates the branch). `scaled_outer = csharp_div(outer*radius_scale,100)`;
  `cutoff=min(outer,scaled_outer)`; sample weight = modifier/100.
* **`reliability(fo,radii,sigma,cutoff)`**: sigma≤0 → `runtime_falloff(0)/100`
  (or 0 when cutoff<0). Else 400-bin midpoint integration over the scatter PDF
  `t∈(0,√2)`, `distance = t*sigma`, `cutoff` gate; `acc/weight`.
* **`uniform_reliability`/`uniform_footprint_cells2`**: `π·r²/1024²`; 400-bin catch
  prob `t*sigma ≤ radius`.
* **Scatter PDF**: Python builds a 256-bin histogram from `random.Random(20260811)`,
  400k samples of `(uniform(-1,1)+uniform(-1,1))/2` per axis, `hypot`, bin `int(r/√2*256)`,
  normalised to density (`h/total/width`, width=√2/256). **Port faithfully: implement
  MT19937 (`init_genrand(20260811)`, `genrand_int32`) and CPython `random()` =
  `((a>>5)*67108864.0 + (b>>6)) / 9007199254740992.0` with a=genrand(), b=genrand();
  `uniform(-1,1) = -1 + 2*random()`.** Then identical loop → identical table.
  Verify against Python output (test asserts a few bins + total ≈ 1·width).
* **`area_tick_modifiers`** also called in `validate_damage_warheads` path — keep
  the same exceptions (they're load-time errors anyway).
* **`target_damage_radius`**: Spread absent/blank → 0 → live=false (uniform path
  returns footprint 0/rel 0 only when live — check Python: `target_damage_radius`
  returns `(spread, spread>0)`; `if not live: return (versus,0,0,0)`).
* **Impact multipliers**: SpriteAthenaLaser → 0 if nominal==0 else 1; TRACKED_ZAP →
  `active_ticks = max(min(DamageDuration, max(Duration,1)),0)`;
  `interval≤0 → active_ticks` else `ceil(active/interval)` — i.e.
  `(active+interval−1)//interval` INT division; LightningZap → `max(min(DamageDuration,Duration),0)`;
  AreaBeam → `Duration/DamageInterval` (float, both must be >0 else throw).
* **`nominal_impact_count`** (SpriteAthena only): `flight = max(rng//max(speed,1),1)`;
  `final = max(flight+pierce+stay+1,1)`; `final//interval` (int).
* **versus_table** = the warhead's `Versus:` block leaves (int each); default {}.
* **analyse**: `ref_hp=200000`. `flat_total = Σ base`. `share_total = flat_total or 1`.
  `damage_total` arg = `ed[1]` (flat_total; 0 when no flat warheads). Parts:
  flat parts share = `base/share_total`; pct apps share = `application_hp/share_total`
  (folded → `continuous_hp`, standalone → `runtime_hp`); `rounding_share` =
  `rounding_hp/share_total` (folded only matters). `contrib = share*versus*(rel+secondary)`.
  `k_flat = Σ contrib(scalable: flat,chip,pct_folded) * impact_mult`.
  `pct_absolute = Σ contrib(pct_standalone) * share_total * impact_mult`.
  `folded_rounding = Σ rounding_share*versus*(rel+secondary) (pct_folded) * share_total * mult`.
  `k = k_flat + (pct_absolute+folded_rounding)/damage_total` (null if ≤0).
  `ctx = targets*range*deadzone` (see below). `effective = damage_total*k_flat*ctx
  + pct_absolute*ctx + folded*ctx`. `overkill = ref_hp/(ceil(ref_hp/eff_unctx)*eff_unctx)`
  where `eff_unctx = damage_total*k_flat + pct_abs + folded` (1.0 if ≤0).
  `avg_versus` = share-weighted versus over flat+chip parts only.
* **factors**: `targets` — `ValidTargets` absent → 1.0; else token set;
  ground = ∩{ground,water,ship,trees,wall}, air = "air"; neither → 1.0;
  `0.5 + 0.5*share` where share = ΣENGAGEMENT[INF,VEH,BLD](+AIR).
  `range` — absent/≤0 → 1.0; else `clamp(1+0.25*(rng/median−1), 0.75, 1.5)`.
  `deadzone` — `1 − (MinRange/Range)²` when both present and 0<Min<Range else 1.0.
* **`warhead_terms`** (flat parts): AreaDamage → `vs` via heaviness transform
  (`mode==SharedVersus → shared_versus_profile else versus_profile`); versus =
  `weighted_versus(vs)`; density = `effective_density(vs)`; then per-wtype geometry.
  `SpreadDamage`/`TargetDamage`/`HealthPercentageDamage` do NOT get heaviness —
  `heaviness_of` reads the field only for Area* (Python reads node field regardless
  but engine drops it there — Python's `falloff_and_radii` gates the scale on
  `value in ("AreaDamage","AreaDamagePercentage")` — mirror EXACTLY).
* **`percentage_terms`**: `AreaDamagePercentage` or PCT_FOLDED → `falloff_and_radii`
  + `area_geometry_samples` with `radius_scale = percentage_spread` (folded) or 100
  (standalone). `HealthPercentageDamage` → uniform path via `target_damage_radius`.
* **`percentage_applications`**: folded only when `AreaDamage` Damage>0 AND
  PercentageScale>0 → `folded_units(damage,scale)` = `cont = d*s/200000`,
  `runtime = trunc((d*s+100000)/200000)` (Int32-bound checked); SHARED mode →
  `shared_folded_units(d,s,h)` = growth `(h≤1000 ? (4000+h)/5000 : (3000+h)/4000)`,
  `cont = d*s*gnum/(200000*gden)`, rounded = `(num + den//2) trunc-div den`;
  `effective_pct` = shared → `shared_versus_profile(Versus)` else
  `percentage_profile(Versus,PercentageVersus,Light,Heavy,h)`.
  `continuous_hp = ref_hp*cont/denominator`; `runtime_hp = runtime_percentage_hp`
  (`hp*units//100` trunc, `*100//denominator` trunc, Int32-checked).
  denominator: AreaDamage → PercentageDenominator default 10000 (>0 enforced);
  AreaDamagePercentage → default 100, PercentageScale>0 REJECTED;
  HealthPercentageDamage → always 100. Skip apps where both units ≤0 (folded) or
  damage ≤0 (standalone).
* **`heaviness_profile_config`/`validate_anchors`/`validate_shared_numeric`/`mode_of`**:
  port with the same raise-on-violation semantics (they're load-time errors; the C#
  mirror throws → the model treats as uncomputable → parity test reports weapon
  as FAILED rather than wrong).
* **`bell_transform`**: 13-slot `BELL_AXIS_ORDER`, `BELL_LO=2/3`, `BELL_SIGMA=0.75`,
  `com` over tiltable (in BELL_AXIS, not DERIVED_ARMORS, value>0), `mu=(h+com)/2`,
  bell + geometric-mean renormalise + per-LADDER rank-restore (sort rungs by
  (-original,index) then assign ranked belled values), then `result = round-half-up?`
  — ⚠ Python `round()` = banker's rounding → use `Math.Round(v, MidpointRounding.ToEven)`
  — then main_table Heroic re-derive (`Plate*Scout/200`, or flat value when all-equal),
  then GEO_DERIVED rows `product^(1/n)` rounded the same way. NON_ARMOR_ROWS =
  {Shield,HAZMAT,COMPOSITE,BLAST,REFLECTOR,ARMOR} excluded from live/body sets.
* **`median_weapon_range`**: median `parse_wdist(Range)` >0 over ALL resolved
  weapons (incl `^` templates). `_model.json` pins 5830 — hard-check in test.
* **`armor_census`**: iterate resolved actors; skip name `startswith ^,$,-` or
  contains `.`; FIRST child `Armor`/`Armor@*` whose `Type` ∈ ARMORS → armor;
  `Health.HP` int → macro bucket (median per macro — `hp_by_macro`,
  `reference_hp_measured` diag = Σ ENGAGEMENT·median — _model pins 65350).
* **`armor_weights`**: per macro `share*count/total` with `+1` floor per armor;
  `Shield` row = `shield_damage_share` (always-on shields only — `Shielded` child,
  `MaxStrength + MaxPercentageStrength*hp/100` pool when `init>0` and not gated;
  `v_class` = pseudo_armor_mean(armor) when armor ∈ ARMORS else 100; share =
  `Σ 100*pool/v_shield / (Σ100*pool/v_shield + Σ100*hp/v_class)`), then class
  weights × `(1−s)`, `weights["Shield"]=s`. _model pins `shield_versus_mean=184.71`,
  `shield_hp_factor=0.5414`.
* **`pseudo_armor_mean(row)`**: over ALL resolved weapons incl `^`, skip
  `is_damage_inert` nodes (every damage-typed `Warhead@` has `Damage: 0` — a
  damage-typed warhead missing Damage → NOT inert), skip warheads whose TYPE
  contains "Percentage" or KEY contains "ExtraDamage"/"FriendlyFire", skip
  zero/unparseable Damage, then collect `Versus[row]` floats → `fmean`, default 100.
* **`charge_up(resolved, local)`**: first child `key.split("@")[0]` ∈
  {AttackCharged,AttackTurretedCharged,AttackFrontalCharged,AttackCharges,AttackTesla}
  → `{v, ticks, cycle_reload?, burst?, charge_delay?}` per `CHARGE_FIELDS`
  (defaults: Tesla (InitialChargeDelay,22),(ReloadDelay,120),(MaxCharges,1),
  (ChargeDelay,3); others (ChargeLevel,25),(ChargeRate,1)); `charge_scalar` =
  comma-list → (min+max)/2 floats, unparseable → field default; `ticks = charge/rate`.
* **`charge_attack_cycle(charge, weapon_reload)`** (formula.py:384): returns
  `(cycle, shots)` ONLY when `charge.cycle_reload` is set (AttackTesla-like override);
  else `None` — the ChargeLevel family keeps the weapon reload and its `ticks` merely
  delay the cycle. Tesla mode: `wind_up = ticks`; `gap = weapon_reload > charge_delay
  ? weapon_reload + wind_up : charge_delay`; `cycle = (burst−1)*gap + cycle_reload +
  wind_up`; `shots = burst` (MaxCharges). Fallback when charge_delay absent:
  `eff_reload(cycle_reload, burst, weapon_reload) + wind_up`.
  **Bot-facing `DamagePerTick(charge, weaponBurst)`** on `BotWeaponModel`:
  `charge == null` → `EffectivePerShot * weaponBurst / EffReload`;
  `charge.CycleReload == null` → `EffectivePerShot * weaponBurst / (EffReload + ticks)`;
  else `EffectivePerShot * shots / cycle`.
* **`condition_holds_by_default`** (`formula.py`): textual expr eval with every
  token → 0 — port via simple recursive descent or token-substitution eval
  (only `&&`,`||`,`!`,`!=`,`>=`,`<=`,`==`,`(`,`)`,numbers,identifiers). Mirror
  "unparseable → false".
* **`fnum`**: `"15, 15"` → 15.0; dict→`v`; first comma piece → float else None.
* **`eff_reload`** = `reload + burst_delay_sum(burst,burstDelays)`;
  `burst_delay_sum`: gaps=burst−1; absent/empty → 5·gaps; len==1 → v·gaps;
  else `Σ values[:gaps]`.
* **`derived_metrics` field emission rules** (for parity diff): `effective_damage`
  (2dp), `damage_total` (raw), `footprint` (4dp), `reliability` (4dp), `sigma` (2dp);
  `k`/`k_context`/`k_flat`/`k_flat_context`/`avg_versus`/`factor_*`/`overkill` (4dp);
  `pct_absolute`/`pct_absolute_context`/`folded_rounding*` (2dp, folded only when
  ≥0.005); `effective_per_shot` (2dp); `eff_reload`/`effective_dps`/`dps_floor` (2dp);
  `projectile_impact_multiplier` only when ≠1; `model_limitations`+`model_status`
  when non-empty. Compare by NAME within tolerance; treat missing-vs-computed as
  divergence except the documented conditional fields.

## 4. `BotWeaponModel` record shape

`{EffectiveDamage, DamageTotal, Footprint, Reliability, Sigma, K, KContext, KFlat,
KFlatContext, PctAbsolute, PctAbsoluteContext, FoldedRounding, FoldedRoundingContext,
AvgVersus, FactorTargets, FactorRange, FactorDeadzone, Overkill, EffectivePerShot,
EffReload, EffectiveDps, DpsFloor, ProjectileImpactMultiplier, NominalImpacts,
ModelLimitations[], IsModelled}` — plus `Weapon`/`Slot` join keys.

## 5. Bot-side integration (switch-gated)

* `BotWeaponProfile` gains `double EffectiveDamagePerTick` (0 = classic-only) plus the
  live-path fields `Model`, `Charge`, `Burst`, `PowerScale`, `CycleScale`,
  `MainDamage`, `DamageTypes`.
* `BotUnitProfile` gains `Source` — the live actor the profile was built from (null
  for type-table profiles; set only under the fog contract below).
* `BotUnitProfiles.Build(rules, actor, effective)`:
  `TraitInfos<ArmamentInfo>()`: `a.EnabledByDefault` → Weapons; others →
  `ConditionalWeapons` (new array, carries `Condition = a.RequiresCondition?.Expression`).
  Effective = `table.Get(rules)` → per-weapon `BotWeaponModel`:
  `dpt = effective_per_shot * shots / cycleTicks` where
  `cycleTicks = chargeAttackCycle(charge, effReload)` → `(cycle,shots)` when the
  trait overrides (Tesla), else `effReload + chargeTicks` (shot count = weapon Burst);
  uncharged → `shots=Burst, cycle=effReload` → equals fixture `effective_dps`.
* **`BotUnitProfiles.Get(Actor, Player viewer, effective)` — the live path**
  (lead ruling 2026-10-05, supersedes the "per-target Versus is P2" note):
  - `effective == false` → cached type profile, bit-identical to classic.
  - Fog contract: own actors are read in full; an enemy actor only while
    `actor.CanBeViewedByPlayer(viewer)`. Anything else → type profile. Fogged enemies
    never expose upgrades, weapon swaps, current HP or hidden armor.
  - Live read: every `Armament` trait that is enabled RIGHT NOW (upgrade/condition
    grants appear as extra armaments; simultaneous slots both count), the armament's
    resolved `WeaponInfo` + `Info.Weapon` model key, aggregated
    `IFirepowerModifier.GetFirepowerModifier(armamentName)` → `PowerScale`,
    `IReloadModifier.GetReloadModifier(armamentName)` → `CycleScale`, current
    `IHealth.HP`, enabled `Armor.Info.Type` (fallback: first `EnabledByDefault`
    `ArmorInfo`), `GetEnabledTargetTypes()`, and `Source = actor`.
  - `DamagePerTickAgainst` under the flag: `w.Model.DamagePerTick(w.Charge, w.Burst,
    target.Armor)` — the model's per-armour `KByArmor`/`PctByArmor`/`FoldedByArmor`
    maps evaluate the Versus term at the OBSERVED armour instead of the census
    average; census value stands when armour is unknown. Then `PowerScale` /
    `CycleScale` multiply, and when BOTH profiles carry a live `Source` the
    defender's `IDamageModifier.GetDamageModifier(attacker, new Damage(MainDamage,
    DamageTypes))` product applies (veterancy/upgrades/status). Unmodelled weapons
    fall back to the classic `damage × versus` term.
* `Predict(own, enemy, effective)` + inner `DamagePerTick(...)` thread the flag.
* Call sites passing the flag (owning module field `UseEffectiveDamageModel`):
  SquadManagerBotModuleCA (fight-size Predict), SiegeEvaluatorBotModule,
  GroundStatesCA (focus check + kite standoff), SquadMicroEvalCA (via caller),
  BotSituation (own side; remembered enemies stay type-level),
  CombatVetoBotModule→CombatVetoEval, EngagementLogBotModule,
  UnitBuilderBotModuleCA→DerivedUnitWeights.Strength (production planning stays
  type-level per the ruling). MaxRange/CanTarget-only reads are mode-invariant
  and keep the type profile in both modes.
* `increment_switches.yaml`:
  `BM_live_combat_model: <comment>` → every module above
  `UseEffectiveDamageModel: true` under ONE switch id.

## 6. Parity test (`WeaponModelParityTest.cs`)

* Locate repo root: walk up from `AppDomain.CurrentDomain.BaseDirectory` to the dir
  containing `mods/cameo/mod.yaml`.
* Build the mirror Ruleset: `load_manifest` port → `Rules:` + `Weapons:` file lists
  → `MiniYaml.FromFile`+mirror-merge (or `MiniYaml.Load` equivalent — manifest-only).
* Hard invariants first (fast, catches pipeline-wide drift):
  `armor_census` == `_model.json` target_model.armor_census exactly (ints);
  `shield_versus_mean` 184.71, `shield_hp_factor` 0.5414,
  `median_weapon_range` 5830, `reference_hp_measured` 65350 (tol 0.001).
* Per `(actor, armament)` row in every `docs/balance/derived/*.json`
  (`System.Text.Json`): compute C# model for `weapon`; compare every numeric field
  both present: `effective_per_shot` (rel 1e-3 or abs 0.05), `reliability`,
  `footprint`, `sigma`, `effective_damage`, `k`, `k_context`, `k_flat`,
  `k_flat_context`, `avg_versus`, `factor_*`, `overkill` (abs 5e-4 or rel 1e-3),
  `pct_absolute*`, `folded_rounding*`, `eff_reload`, `effective_dps`, `dps_floor`,
  `projectile_impact_multiplier`. Missing-in-fixture vs computed-nonzero = fail
  (respect the conditional-emission rules). A weapon that raises in either
  implementation = FAIL (count + report).
* Also compare raw `charge_up` records from `docs/balance/<faction>.json` armament
  entries (fields v/ticks/cycle_reload/burst/charge_delay) against the C# charge
  read — secondary surface.
* Scatter-PDF table self-check: bin count 256, Σdens·width ≈ 1, plus one recorded
  Python bin value pinned as a constant (generate once, embed).

## 7. Determinism / fog / architecture notes

* All pure statics + `ConditionalWeakTable` caches — no synced state, no RNG reads
  (the MT19937 table is a fixed-seed constant — NOT `world.SharedRandom`).
* Bot-side: `Ruleset`-keyed, built lazily on first `Get(rules, actor, effective:true)`
  — zero cost when the switch is off (classic path untouched, bit-identical).
* `Game.ModData` may be null in exotic hosts. Implemented decision: manifest/repo-root
  failure → `BotWeaponModelTable.Unavailable` (rs null) → `Compute` returns null →
  `EffectiveDamagePerTick` 0 → `DamagePerTickAgainst` takes the classic term for every
  weapon. The flag then degrades to bit-identical classic numbers rather than zeros;
  the failure is logged once (`debug` channel).
* P2 (NOVA) owns situational target terms; P3 shields/meters; P4 closing time.
  Per-armour Versus moved INTO P1 by the lead ruling (`KByArmor`/`PctByArmor`/
  `FoldedByArmor` + `EffectivePerShotAgainst(armor)`); the `Source`-carrying live
  profile is the seam for every later phase.

## 8. Verification plan

1. `dotnet build -c Release` clean.
2. `dotnet test OpenRA.Mods.Cameo.Test` — new parity test + existing suite.
3. Boot gate per workflow (skill `boot-gate`), exception-log diff.
4. Devlog + HANDOFF + claim RESULT; scoped `git add` only.
