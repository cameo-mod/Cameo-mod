#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// Armed squad desire must not convert the legacy floating-point predictor. Read the
	// integer weapon rules independently; the legacy cache and its callers remain untouched.
	public static class IntegerCombatPredictor
	{
		public readonly record struct Weapon(int Damage, int Burst, int CycleTicks,
			BitSet<TargetableType> Valid, BitSet<TargetableType> Invalid, IReadOnlyDictionary<string, int> Versus);
		public sealed record Unit(int Hp, string Armor, BitSet<TargetableType> Targets, Weapon[] Weapons);
		static readonly ConditionalWeakTable<Ruleset, Dictionary<ActorInfo, Unit>> Cache = new();

		public static Unit Profile(Ruleset rules, ActorInfo actor)
		{
			var cache = Cache.GetOrCreateValue(rules);
			lock (cache)
			{
				if (cache.TryGetValue(actor, out var unit))
					return unit;

				var weapons = new List<Weapon>();
				foreach (var armament in actor.TraitInfos<ArmamentInfo>().Where(a => a.EnabledByDefault))
				{
					if (armament.Weapon == null || !rules.Weapons.TryGetValue(armament.Weapon.ToLowerInvariant(), out var weapon))
						continue;
					var main = weapon.Warheads.OfType<DamageWarhead>().Where(w => w.Damage > 0)
						.OrderByDescending(w => w.Damage).FirstOrDefault();
					if (main != null)
						weapons.Add(new Weapon(main.Damage, Math.Max(1, weapon.Burst),
							BotWeaponProfile.CycleTicks(weapon.ReloadDelay, weapon.Burst, weapon.BurstDelays),
							weapon.ValidTargets, weapon.InvalidTargets, main.Versus));
				}

				unit = new Unit(actor.TraitInfoOrDefault<IHealthInfo>()?.MaxHP ?? 0,
					actor.TraitInfos<ArmorInfo>().FirstOrDefault(a => a.EnabledByDefault)?.Type,
					actor.GetAllTargetTypes(), weapons.ToArray());
				cache.Add(actor, unit);
				return unit;
			}
		}

		// Damage is millionths per tick. BigInteger aggregates avoid overflow and make
		// addition independent of enumeration order, even for modded large HP/bursts.
		static BigInteger WeightedDamage(IReadOnlyList<(Unit Unit, int Count)> attackers,
			IReadOnlyList<(Unit Unit, int Count)> targets)
		{
			BigInteger total = 0;
			foreach (var (attacker, count) in attackers)
				foreach (var (target, targetCount) in targets)
					foreach (var weapon in attacker.Weapons)
					{
						if (!weapon.Valid.Overlaps(target.Targets) || weapon.Invalid.Overlaps(target.Targets))
							continue;
						var versus = target.Armor == null ? 100 : weapon.Versus?.GetValueOrDefault(target.Armor, 100) ?? 100;
						var damage = (BigInteger)Math.Max(0, weapon.Damage) * Math.Max(1, weapon.Burst)
							* Math.Max(0, versus) * 10000 / Math.Max(1, weapon.CycleTicks);
						total += damage * Math.Max(0, count) * Math.Max(0, target.Hp) * Math.Max(0, targetCount);
					}
			return total;
		}

		public static int RatioMilli(IReadOnlyList<(Unit Unit, int Count)> own,
			IReadOnlyList<(Unit Unit, int Count)> enemy, int cap = 100000)
		{
			BigInteger ownHp = 0, enemyHp = 0;
			foreach (var (unit, count) in own)
				ownHp += (BigInteger)Math.Max(0, unit.Hp) * Math.Max(0, count);
			foreach (var (unit, count) in enemy)
				enemyHp += (BigInteger)Math.Max(0, unit.Hp) * Math.Max(0, count);
			var ownDamage = WeightedDamage(own, enemy);
			var enemyDamage = WeightedDamage(enemy, own);
			if (ownDamage <= 0 && enemyDamage <= 0)
				return 1000;
			if (enemyDamage <= 0)
				return Math.Max(1000, cap);
			if (ownDamage <= 0 || ownHp <= 0)
				return 0;

			// DPS_own = ownDamage/enemyHp, DPS_enemy = enemyDamage/ownHp.
			// Their square-law strength ratio cancels to this integer expression.
			var ratio = ownDamage * ownHp * ownHp * 1000 / (enemyDamage * enemyHp * enemyHp);
			return (int)BigInteger.Min(Math.Max(1000, cap), ratio);
		}
	}
}
