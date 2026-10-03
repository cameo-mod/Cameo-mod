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

		public BotUnitProfile(string name, int cost, int hp, string armor, int speed, bool isAircraft, bool isBuilding,
			BitSet<TargetableType> targetTypes, BotWeaponProfile[] weapons)
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
		}

		public WDist MaxRange => Weapons.Length == 0 ? WDist.Zero : Weapons.Max(w => w.Range);

		/// <summary>Damage per tick this unit deals to one `target`: every weapon that may hit it, scaled by Versus.</summary>
		public double DamagePerTickAgainst(BotUnitProfile target)
		{
			var total = 0.0;
			foreach (var w in Weapons)
				if (w.CanTarget(target.TargetTypes))
					total += w.DamagePerTick * (target.Armor == null ? 100 : w.Versus.GetValueOrDefault(target.Armor, 100)) / 100.0;

			return total;
		}
	}

	public readonly struct BotWeaponProfile
	{
		public readonly double DamagePerTick;
		public readonly WDist Range;
		public readonly BitSet<TargetableType> Valid;
		public readonly BitSet<TargetableType> Invalid;
		public readonly IReadOnlyDictionary<string, int> Versus;

		/// <summary>Delivery key of the main warhead: its yaml <c>Warhead@&lt;tag&gt;</c> suffix when the weapon yaml
		/// resolves (the balance-pipeline delivery taxonomy the tier-1 fitter fits), else the warhead class name
		/// lowercased minus the "Warhead" suffix (AI_ARCHITECTURE 12.31).</summary>
		public readonly string Delivery;

		public BotWeaponProfile(double damagePerTick, WDist range, BitSet<TargetableType> valid, BitSet<TargetableType> invalid,
			IReadOnlyDictionary<string, int> versus, string delivery = null)
		{
			DamagePerTick = damagePerTick;
			Range = range;
			Valid = valid;
			Invalid = invalid;
			Versus = versus ?? new Dictionary<string, int>();
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

		public static BotUnitProfile Get(Ruleset rules, ActorInfo actor)
		{
			var byName = Cache.GetOrCreateValue(rules);
			lock (byName)
			{
				if (!byName.TryGetValue(actor.Name, out var profile))
					byName[actor.Name] = profile = Build(rules, actor);

				return profile;
			}
		}

		static BotUnitProfile Build(Ruleset rules, ActorInfo actor)
		{
			var weapons = new List<BotWeaponProfile>();
			foreach (var armament in actor.TraitInfos<ArmamentInfo>().Where(a => a.EnabledByDefault))
			{
				if (armament.Weapon == null || !rules.Weapons.TryGetValue(armament.Weapon.ToLowerInvariant(), out var weapon))
					continue;

				// The largest positive damage warhead is the weapon's main one (W24), as the counter picker reads it.
				var main = weapon.Warheads.OfType<DamageWarhead>().Where(d => d.Damage > 0).OrderByDescending(d => d.Damage).FirstOrDefault();
				if (main == null)
					continue;

				var cycle = BotWeaponProfile.CycleTicks(weapon.ReloadDelay, weapon.Burst, weapon.BurstDelays);
				weapons.Add(new BotWeaponProfile((double)main.Damage * Math.Max(1, weapon.Burst) / cycle, weapon.Range,
					weapon.ValidTargets, weapon.InvalidTargets, main.Versus, DeliveryKey(armament.Weapon, weapon, main)));
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

		/// <summary>Two forces given as (profile, count); damage is spread over the other side by HP share.</summary>
		public static Prediction Predict(IReadOnlyList<(BotUnitProfile Unit, int Count)> own, IReadOnlyList<(BotUnitProfile Unit, int Count)> enemy)
		{
			var ownHp = own.Sum(u => (double)u.Unit.Hp * u.Count);
			var enemyHp = enemy.Sum(u => (double)u.Unit.Hp * u.Count);
			return Predict(DamagePerTick(own, enemy, enemyHp), ownHp, DamagePerTick(enemy, own, ownHp), enemyHp);
		}

		static double DamagePerTick(IReadOnlyList<(BotUnitProfile Unit, int Count)> attackers,
			IReadOnlyList<(BotUnitProfile Unit, int Count)> targets, double targetHp)
		{
			if (targetHp <= 0)
				return 0;

			var total = 0.0;
			foreach (var (attacker, count) in attackers)
				foreach (var (target, targetCount) in targets)
					total += count * attacker.DamagePerTickAgainst(target) * target.Hp * targetCount / targetHp;

			return total;
		}
	}
}
