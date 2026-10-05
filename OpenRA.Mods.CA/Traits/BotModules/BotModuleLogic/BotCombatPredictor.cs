#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// What one unit TYPE brings to a fight, read once per ruleset from its rules (AI_DEEP_RESEARCH.md §2.3,
	/// AI_ARCHITECTURE.md §12.3–12.4): cost, hit points, armour, speed, and per weapon the damage per tick, the
	/// Versus row of its main warhead, its range and what it may target. Bot-side, unsynced, read-only.
	/// </summary>
	public sealed class BotUnitProfile
	{
		public readonly string Name;
		public readonly int Cost;
		public readonly int Hp;
		public readonly string Armor;
		public readonly int Speed;
		public readonly bool IsAircraft;
		public readonly bool IsBuilding;
		public readonly BitSet<TargetableType> TargetTypes;
		public readonly BotWeaponProfile[] Weapons;

		/// <summary>The live actor this profile was built from, null for type-table profiles.
		/// Set only when the bot owns the actor or can currently see it — the fog contract.</summary>
		public readonly Actor Source;

		public BotUnitProfile(string name, int cost, int hp, string armor, int speed, bool isAircraft, bool isBuilding,
			BitSet<TargetableType> targetTypes, BotWeaponProfile[] weapons, Actor source = null)
		{
			Name = name;
			Cost = cost;
			Hp = hp;
			Armor = armor;
			Speed = speed;
			IsAircraft = isAircraft;
			IsBuilding = isBuilding;
			TargetTypes = targetTypes;
			Weapons = weapons;
			Source = source;
		}

		public WDist MaxRange => Weapons.Length == 0 ? WDist.Zero : Weapons.Max(w => w.Range);

		/// <summary>Damage per tick this unit deals to one `target`: every weapon that may hit it, scaled by Versus.
		/// When <paramref name="useEffective"/> is set (BM_live_combat_model), a weapon's pipeline-modelled
		/// damage per tick is used — evaluated against the target's own armour when that armour is known; the
		/// balance model already folds reliability, falloff, and target density in. Unmodelled weapons (0) fall
		/// back to the classic term. Live profiles additionally fold in the shooter's firepower/reload
		/// modifiers and the target's damage modifiers (veterancy, upgrades, status).</summary>
		public double DamagePerTickAgainst(BotUnitProfile target, bool useEffective = false)
		{
			var total = 0.0;
			foreach (var w in Weapons)
			{
				if (!w.CanTarget(target.TargetTypes))
					continue;

				var dpt = useEffective && w.Model != null
					? w.Model.DamagePerTick(w.Charge, w.Burst, target.Armor)
					: 0;
				if (dpt <= 0)
				{
					if (useEffective && w.EffectiveDamagePerTick > 0)
						dpt = w.EffectiveDamagePerTick;
					else if (useEffective && w.Terms != null && w.Terms.Count > 1)
					{
						// Legacy multi-damage-warheads sum until W24 picks a main warhead —
						// each warhead keeps its own Versus row.
						foreach (var t in w.Terms)
							dpt += t.DamagePerTick * (target.Armor == null ? 100 : t.Versus.GetValueOrDefault(target.Armor, 100)) / 100.0;
					}
					else
						dpt = w.DamagePerTick * (target.Armor == null ? 100 : w.Versus.GetValueOrDefault(target.Armor, 100)) / 100.0;
				}

				if (w.PowerScale != 1.0)
					dpt *= w.PowerScale;
				if (w.CycleScale != 1.0)
					dpt /= w.CycleScale;
				if (Source != null && target.Source != null)
					dpt *= LiveDamageTaken(Source, target.Source, w);

				total += dpt;
			}

			return total;
		}

		/// <summary>The defender's live damage modifiers (veterancy, upgrades, status effects)
		/// against one attacker weapon: every damage warhead gets its own modifier eval and the
		/// weapon's multiplier is their damage-weighted mean. Only evaluated when both actors
		/// are in sight — a fogged profile carries no traits to read.</summary>
		static double LiveDamageTaken(Actor attacker, Actor defender, BotWeaponProfile weapon)
		{
			var terms = weapon.Terms;
			if (terms == null || terms.Count == 0)
			{
				var scale = 1.0;
				var damage = new Damage(weapon.MainDamage, weapon.DamageTypes);
				foreach (var m in defender.TraitsImplementing<IDamageModifier>())
					scale *= Math.Max(0, m.GetDamageModifier(attacker, damage)) / 100.0;
				return scale;
			}

			var weighted = 0.0;
			var totalDamage = 0.0;
			foreach (var t in terms)
			{
				var scale = 1.0;
				var damage = new Damage(t.Damage, t.DamageTypes);
				foreach (var m in defender.TraitsImplementing<IDamageModifier>())
					scale *= Math.Max(0, m.GetDamageModifier(attacker, damage)) / 100.0;
				var weight = Math.Max(1, t.Damage);
				weighted += scale * weight;
				totalDamage += weight;
			}

			return totalDamage > 0 ? weighted / totalDamage : 1.0;
		}
	}

	/// <summary>One positive-damage warhead's contribution inside a weapon — the "legacy
	/// multi-damage-warheads" term the ruling wants summed until W24 picks a main warhead.
	/// The classic path keeps using <see cref="BotWeaponProfile.DamagePerTick"/> (largest
	/// warhead only); the effective fallback and the defender-modifier eval iterate all terms.</summary>
	public readonly struct BotWarheadTerm
	{
		public readonly double DamagePerTick;
		public readonly IReadOnlyDictionary<string, int> Versus;
		public readonly int Damage;
		public readonly BitSet<DamageType> DamageTypes;

		public BotWarheadTerm(double damagePerTick, IReadOnlyDictionary<string, int> versus, int damage, BitSet<DamageType> damageTypes)
		{
			DamagePerTick = damagePerTick;
			Versus = versus;
			Damage = damage;
			DamageTypes = damageTypes;
		}
	}

	public readonly struct BotWeaponProfile
	{
		public readonly double DamagePerTick;

		/// <summary>BM_live_combat_model: the balance pipeline's damage per tick for this weapon
		/// (effective_per_shot over its charge-aware cycle). 0 = not modelled — callers fall back to
		/// <see cref="DamagePerTick"/> rather than pricing the weapon as harmless.</summary>
		public readonly double EffectiveDamagePerTick;

		public readonly WDist Range;
		public readonly BitSet<TargetableType> Valid;
		public readonly BitSet<TargetableType> Invalid;
		public readonly IReadOnlyDictionary<string, int> Versus;

		/// <summary>The modelled weapon record when the effective table is in play — carries the
		/// per-armour decomposition so a live target's observed armour picks its own Versus row.</summary>
		public readonly BotWeaponModel Model;

		/// <summary>The firing actor's charge-up record (null when the type has none).</summary>
		public readonly BotChargeUp Charge;

		/// <summary>Live actor stats, 1.0 on type profiles: the shooter's aggregated firepower
		/// multiplier (veterancy/handicap) and reload multiplier per armament.</summary>
		public readonly double PowerScale;
		public readonly double CycleScale;
		public readonly int Burst;

		/// <summary>Main warhead's nominal damage and damage types — the arguments a target's
		/// IDamageModifier traits are evaluated with on the live path.</summary>
		public readonly int MainDamage;
		public readonly BitSet<DamageType> DamageTypes;

		/// <summary>Every positive-damage warhead on the weapon, largest first. Under the
		/// effective flag an unmodelled weapon falls back to the SUM of these terms (each with
		/// its own Versus) rather than the largest alone, and the live defender-modifier eval
		/// weights each term's modifier by its nominal damage.</summary>
		public readonly IReadOnlyList<BotWarheadTerm> Terms;

		/// <summary>Delivery key of the main warhead: its yaml <c>Warhead@&lt;tag&gt;</c> suffix when the weapon yaml
		/// resolves (the balance-pipeline delivery taxonomy the tier-1 fitter fits), else the warhead class name
		/// lowercased minus the "Warhead" suffix (AI_ARCHITECTURE 12.31).</summary>
		public readonly string Delivery;

		public BotWeaponProfile(double damagePerTick, WDist range, BitSet<TargetableType> valid, BitSet<TargetableType> invalid,
			IReadOnlyDictionary<string, int> versus, double effectiveDamagePerTick = 0,
			BotWeaponModel model = null, BotChargeUp charge = null, int burst = 1,
			double powerScale = 1.0, double cycleScale = 1.0, int mainDamage = 0, BitSet<DamageType> damageTypes = default,
			IReadOnlyList<BotWarheadTerm> terms = null, string delivery = null)
		{
			DamagePerTick = damagePerTick;
			EffectiveDamagePerTick = effectiveDamagePerTick;
			Range = range;
			Valid = valid;
			Invalid = invalid;
			Versus = versus ?? new Dictionary<string, int>();
			Model = model;
			Charge = charge;
			Burst = burst;
			PowerScale = powerScale;
			CycleScale = cycleScale;
			MainDamage = mainDamage;
			DamageTypes = damageTypes;
			Terms = terms;
			Delivery = delivery;
		}

		public bool CanTarget(BitSet<TargetableType> targetTypes) => Valid.Overlaps(targetTypes) && !Invalid.Overlaps(targetTypes);

		/// <summary>Ticks per full cycle: the burst's gaps, then the reload (the charge-cycle law's first two terms).</summary>
		public static int CycleTicks(int reloadDelay, int burst, IReadOnlyList<int> burstDelays)
		{
			var cycle = Math.Max(1, reloadDelay);
			for (var shot = 0; shot < burst - 1; shot++)
				cycle += burstDelays == null || burstDelays.Count == 0 ? 0
					: burstDelays.Count == 1 ? burstDelays[0] : burstDelays[Math.Min(shot, burstDelays.Count - 1)];

			return cycle;
		}
	}

	public static class BotUnitProfiles
	{
		static readonly ConditionalWeakTable<Ruleset, Dictionary<string, BotUnitProfile>> Cache = new();

		public static BotUnitProfile Get(Ruleset rules, ActorInfo actor, bool useEffective = false)
		{
			var byName = Cache.GetOrCreateValue(rules);
			lock (byName)
			{
				// The effective variant reads a different model table — cache it under a
				// separate key so a mixed-switch roster never sees the wrong numbers.
				var key = useEffective ? actor.Name + "\u0001" : actor.Name;
				if (!byName.TryGetValue(key, out var profile))
					byName[key] = profile = Build(rules, actor, useEffective);

				return profile;
			}
		}

		static BotUnitProfile Build(Ruleset rules, ActorInfo actor, bool useEffective)
		{
			var weapons = new List<BotWeaponProfile>();
			var models = useEffective ? BotWeaponModelTable.Get(rules) : null;
			foreach (var armament in actor.TraitInfos<ArmamentInfo>().Where(a => a.EnabledByDefault))
			{
				if (armament.Weapon == null || !rules.Weapons.TryGetValue(armament.Weapon.ToLowerInvariant(), out var weapon))
					continue;

				var weaponProfile = WeaponProfile(models, weapon, armament.Weapon, actor.Name);
				if (weaponProfile.HasValue)
					weapons.Add(weaponProfile.Value);
			}

			var speed = actor.TraitInfoOrDefault<MobileInfo>()?.Speed ?? actor.TraitInfoOrDefault<AircraftInfo>()?.Speed ?? 0;
			return new BotUnitProfile(actor.Name,
				actor.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0,
				actor.TraitInfoOrDefault<IHealthInfo>()?.MaxHP ?? 0,
				actor.TraitInfos<ArmorInfo>().FirstOrDefault(a => a.EnabledByDefault)?.Type,
				speed,
				actor.HasTraitInfo<AircraftInfo>(),
				actor.HasTraitInfo<BuildingInfo>(),
				actor.GetAllTargetTypes(),
				weapons.ToArray());
		}

		// The balance pipeline keys deliveries on the warhead's yaml Warhead@<tag> suffix, which resolved
		// WeaponInfo objects do not retain. Re-derive it once from the resolved weapon yaml: MiniYaml.Load
		// merges inheritance, so each weapon's child list is exactly what WeaponInfo.LoadWarheads iterated,
		// in order — the resolved index maps back to the child and its tag. Validated against the warhead
		// class name (the node value); unresolvable weapons fall back to the lowercased class name, a
		// coarser but still consistent axis (the fitter's coarse Factor@ floor, if any, still reaches it).
		// The static cache never invalidates — valid within one process lifetime: mod yaml is fixed at
		// load, and every match resolves the same warhead set.
		// Process-lifetime cache, deliberately never invalidated: weapon yaml is immutable within a
		// match and the map only ever loads once per process (bot matches are one world per launch).
		static Dictionary<string, (string Tag, string Class, IReadOnlyDictionary<string, int> Versus)[]> warheadYaml;

		static Dictionary<string, (string Tag, string Class, IReadOnlyDictionary<string, int> Versus)[]> WarheadYamlMap()
		{
			if (warheadYaml != null)
				return warheadYaml;

			var map = new Dictionary<string, (string, string, IReadOnlyDictionary<string, int>)[]>();
			var modData = Game.ModData;
			if (modData != null)
			{
				try
				{
					foreach (var node in MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Weapons, null))
					{
						map[node.Key.ToLowerInvariant()] = (node.Value?.Nodes ?? [])
							.Where(n => n.Key.StartsWith("Warhead", StringComparison.Ordinal))
							.Select(n => (
								n.Key.StartsWith("Warhead@", StringComparison.Ordinal) ? n.Key.Substring(8) : null,
								n.Value.Value,
								(IReadOnlyDictionary<string, int>)VersusOf(n)))
							.ToArray();
					}
				}
				catch
				{
					// No file system (unit tests): Delivery falls back to the class name below.
				}
			}

			return warheadYaml = map;
		}

		static Dictionary<string, int> VersusOf(MiniYamlNode warheadNode)
		{
			var versus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var node = warheadNode.Value?.Nodes.FirstOrDefault(n => n.Key == "Versus");
			if (node == null)
				return versus;

			foreach (var armor in node.Value.Nodes)
				if (int.TryParse(armor.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pct))
					versus[armor.Key] = pct;

			return versus;
		}

		/// <summary>The resolved Versus table the tier-1 fitter fitted this delivery tag on (AI_ARCHITECTURE
		/// 12.31, F1-b) — `^Warhead_&lt;tag&gt;`'s `Warhead@&lt;tag&gt;` child, else the one-level family
		/// fallback `^Warhead_&lt;tag minus the last _segment&gt;`: the fitter's exact resolution
		/// (fit_engagement_priors.versus_priors). Template tables are canonical (Versus lives only in
		/// `^Warhead_*`); weapon children sharing the tag may carry inline overrides the fitter never
		/// reads, so they are not consulted. Null when the tag resolves nowhere — fitter-excluded tags
		/// emit no cells, so an unfitted lookup can only come from an unverifiable (stale-safe) cell.</summary>
		/// <summary>True once the resolved warhead template map actually loaded — false under unit
		/// tests or headless tools, where an unresolved tag means "unverifiable" rather than "the
		/// delivery's Versus row is gone".</summary>
		public static bool VersusTableLoaded => WarheadYamlMap().Count > 0;

		public static IReadOnlyDictionary<string, int> ResolvedTagVersus(string tag)
		{
			var map = WarheadYamlMap();
			var versus = TemplateVersus(map, tag);
			if (versus != null)
				return versus;

			var cut = tag.LastIndexOf('_');
			return cut > 0 ? TemplateVersus(map, tag[..cut]) : null;
		}

		static IReadOnlyDictionary<string, int> TemplateVersus(
			Dictionary<string, (string Tag, string Class, IReadOnlyDictionary<string, int> Versus)[]> map, string templateTag)
		{
			if (!map.TryGetValue("^warhead_" + templateTag.ToLowerInvariant(), out var entries))
				return null;

			foreach (var (t, _, versus) in entries)
				if (string.Equals(t, templateTag, StringComparison.Ordinal))
					return versus;

			return null;
		}

		static string DeliveryKey(string weaponName, WeaponInfo weapon, DamageWarhead main)
		{
			var cls = main.GetType().Name;
			if (cls.EndsWith("Warhead", StringComparison.Ordinal))
				cls = cls.Substring(0, cls.Length - "Warhead".Length);

			if (WarheadYamlMap().TryGetValue(weaponName.ToLowerInvariant(), out var tags))
			{
				var idx = weapon.Warheads.IndexOf(main);
				if (idx >= 0 && idx < tags.Length && tags[idx].Tag != null
					&& string.Equals(tags[idx].Class, cls, StringComparison.OrdinalIgnoreCase))
					return tags[idx].Tag;

				// A null CreateObject earlier in the list shifts indices — fall back to matching by class.
				foreach (var (tag, @class, _) in tags)
					if (tag != null && string.Equals(@class, cls, StringComparison.OrdinalIgnoreCase))
						return tag;
			}

			return cls.ToLowerInvariant();
		}

		/// <summary>One armament's weapon profile: the classic main-warhead term plus, when the
		/// effective table is live, the pipeline-modelled record with charge-up cadence. Returns
		/// null when the armament has no resolvable positive-damage warhead.</summary>
		static BotWeaponProfile? WeaponProfile(BotWeaponModelTable models, WeaponInfo weapon, string weaponName, string actorName)
		{
			// The largest positive damage warhead is the weapon's main one (W24), as the counter picker reads it.
			var warheads = weapon.Warheads.OfType<DamageWarhead>().Where(d => d.Damage > 0).OrderByDescending(d => d.Damage).ToList();
			if (warheads.Count == 0)
				return null;

			var main = warheads[0];
			var burst = Math.Max(1, weapon.Burst);
			var cycle = BotWeaponProfile.CycleTicks(weapon.ReloadDelay, weapon.Burst, weapon.BurstDelays);
			var model = models?.Compute(weaponName);
			var charge = models?.ChargeUpFor(actorName);
			var effectiveDpt = model != null && model.IsModelled ? model.DamagePerTick(charge, weapon.Burst) : 0;
			var terms = warheads
				.Select(w => new BotWarheadTerm((double)w.Damage * burst / cycle, w.Versus, w.Damage, w.DamageTypes))
				.ToArray();
			return new BotWeaponProfile((double)main.Damage * burst / cycle, weapon.Range,
				weapon.ValidTargets, weapon.InvalidTargets, main.Versus, effectiveDpt,
				model, charge, burst, 1.0, 1.0, main.Damage, main.DamageTypes, terms,
				DeliveryKey(weaponName, weapon, main));
		}

		/// <summary>The live variant of <see cref="Get(Ruleset, ActorInfo, bool)"/> — the actor's
		/// armaments as enabled RIGHT NOW (upgrade/condition grants, weapon swaps, simultaneous
		/// slots), its current hit points and armour, and its firepower/reload modifiers folded
		/// into each weapon. Fog contract: the bot's own actors are fully read; an enemy only
		/// while <see cref="Actor.CanBeViewedByPlayer"/> — a fogged actor returns the type-table
		/// profile, so callers never branch on visibility themselves.</summary>
		public static BotUnitProfile Get(Actor actor, Player viewer, bool useEffective = false)
		{
			var rules = actor.World.Map.Rules;
			if (!useEffective || viewer == null || (actor.Owner != viewer && !actor.CanBeViewedByPlayer(viewer)))
				return Get(rules, actor.Info, useEffective);

			var models = useEffective ? BotWeaponModelTable.Get(rules) : null;
			var weapons = new List<BotWeaponProfile>();
			foreach (var armament in actor.TraitsImplementing<Armament>())
			{
				if (armament.IsTraitDisabled || armament.Weapon == null)
					continue;

				var name = armament.Info.Name;
				var power = 1.0;
				var reload = 1.0;
				foreach (var m in actor.TraitsImplementing<IFirepowerModifier>())
					power *= m.GetFirepowerModifier(name) / 100.0;
				foreach (var m in actor.TraitsImplementing<IReloadModifier>())
					reload *= m.GetReloadModifier(name) / 100.0;

				var weaponProfile = WeaponProfile(models, armament.Weapon, armament.Info.Weapon, actor.Info.Name);
				if (!weaponProfile.HasValue)
					continue;

				var w = weaponProfile.Value;
				weapons.Add(new BotWeaponProfile(w.DamagePerTick, w.Range, w.Valid, w.Invalid, w.Versus,
					w.EffectiveDamagePerTick, w.Model, w.Charge, w.Burst, power, reload, w.MainDamage, w.DamageTypes, w.Terms,
					w.Delivery));
			}

			var info = actor.Info;
			var health = actor.TraitOrDefault<IHealth>();
			var armor = actor.TraitsImplementing<Armor>().FirstOrDefault(a => !a.IsTraitDisabled)?.Info.Type
				?? info.TraitInfos<ArmorInfo>().FirstOrDefault(a => a.EnabledByDefault)?.Type;
			var speed = info.TraitInfoOrDefault<MobileInfo>()?.Speed ?? info.TraitInfoOrDefault<AircraftInfo>()?.Speed ?? 0;
			return new BotUnitProfile(info.Name,
				info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0,
				health?.HP ?? info.TraitInfoOrDefault<IHealthInfo>()?.MaxHP ?? 0,
				armor,
				speed,
				info.HasTraitInfo<AircraftInfo>(),
				info.HasTraitInfo<BuildingInfo>(),
				actor.GetEnabledTargetTypes(),
				weapons.ToArray(),
				actor);
		}
	}

	/// <summary>
	/// Lanchester square-law prediction of a fight between two forces (AI_DEEP_RESEARCH.md §2: Stanescu et al.,
	/// AIIDE 2013/2015). With x, y the surviving HP fractions, dx/dt = -b·y and dy/dt = -a·x, where a is side A's
	/// damage per tick over side B's total HP and b the reverse; a·x² − b·y² is invariant, so A wins when
	/// DPS_A·HP_A > DPS_B·HP_B and keeps √(1 − b/a) of its HP. Each unit's damage is spread over the enemy by
	/// HP share (no focus fire, no range or terrain advantage — deliberately simple; learned per-type factors
	/// correct it later, CA-1b).
	/// </summary>
	public static class BotCombatPredictor
	{
		public readonly record struct Prediction(double Ratio, double OwnSurvivingFraction, double EnemySurvivingFraction)
		{
			/// <summary>`Ratio` > 1: the own side wins. Capped so an undefended enemy does not print infinity.</summary>
			public bool OwnWins => Ratio > 1;
		}

		public const double MaxRatio = 100;

		/// <summary>The pure square law on aggregates: damage per tick against the other side, and total HP.</summary>
		public static Prediction Predict(double ownDamagePerTick, double ownHp, double enemyDamagePerTick, double enemyHp)
		{
			var own = ownDamagePerTick * ownHp;
			var enemy = enemyDamagePerTick * enemyHp;
			if (own <= 0 && enemy <= 0)
				return new Prediction(1, ownHp > 0 ? 1 : 0, enemyHp > 0 ? 1 : 0);
			if (enemy <= 0)
				return new Prediction(MaxRatio, 1, 0);
			if (own <= 0)
				return new Prediction(0, 0, 1);

			var ratio = Math.Min(MaxRatio, own / enemy);
			return ratio >= 1
				? new Prediction(ratio, Math.Sqrt(1 - 1 / ratio), 0)
				: new Prediction(ratio, 0, Math.Sqrt(1 - ratio));
		}

		/// <summary>Two forces given as (profile, count); damage is spread over the other side by HP share.
		/// <paramref name="useEffective"/> (BM_live_combat_model) swaps the per-weapon term for the
		/// balance pipeline's modelled value; profiles must have been fetched with the same flag.</summary>
		public static Prediction Predict(IReadOnlyList<(BotUnitProfile Unit, int Count)> own, IReadOnlyList<(BotUnitProfile Unit, int Count)> enemy,
			bool useEffective = false)
		{
			var ownHp = own.Sum(u => (double)u.Unit.Hp * u.Count);
			var enemyHp = enemy.Sum(u => (double)u.Unit.Hp * u.Count);
			return Predict(DamagePerTick(own, enemy, enemyHp, useEffective), ownHp, DamagePerTick(enemy, own, ownHp, useEffective), enemyHp);
		}

		static double DamagePerTick(IReadOnlyList<(BotUnitProfile Unit, int Count)> attackers,
			IReadOnlyList<(BotUnitProfile Unit, int Count)> targets, double targetHp, bool useEffective)
		{
			if (targetHp <= 0)
				return 0;

			var total = 0.0;
			foreach (var (attacker, count) in attackers)
				foreach (var (target, targetCount) in targets)
					total += count * attacker.DamagePerTickAgainst(target, useEffective) * target.Hp * targetCount / targetHp;

			return total;
		}
	}
}
