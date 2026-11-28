#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>ARMY-BRAIN-01 S1: the formation slot of a unit type.</summary>
	public enum ArmySlot { Line, Front, Back, Fast, Air, Support }

	/// <summary>The stat features a slot is decided from (ARMY_BRAIN_DESIGN_02.md, 3.1 / 3.2). Integer only.</summary>
	public readonly record struct ArmySlotFacts(int HpPerCostPermille, int Range, int Speed, bool IsAircraft, bool HasWeapon)
	{
		public static ArmySlotFacts Of(BotUnitProfile unit)
		{
			var range = 0;
			foreach (var w in unit.Weapons)
				range = Math.Max(range, w.Range.Length);

			var hpPerCost = unit.Cost > 0 ? (int)Math.Min(int.MaxValue, (long)unit.Hp * 1000 / unit.Cost) : 0;
			return new ArmySlotFacts(hpPerCost, range, unit.Speed, unit.IsAircraft, unit.Weapons.Length > 0);
		}
	}

	/// <summary>
	/// ARMY-BRAIN-01 S1: formation slots from quantile cut points of the stat features (a one-dimensional, deterministic clustering; Opus
	/// ruling on Q5). The cut points are computed ONCE over all unit types of all factions of the ruleset and every type, own, enemy or
	/// captured, is classified with the same cut points, so a slot never changes during a game. S1 only reports the slots (DEV); the later
	/// slices learn on them.
	/// </summary>
	public sealed class ArmySlots
	{
		public readonly int FrontHpPerCost;
		public readonly int BackRange;
		public readonly int FastSpeed;

		public ArmySlots(int frontHpPerCost, int backRange, int fastSpeed)
		{
			FrontHpPerCost = frontHpPerCost;
			BackRange = backRange;
			FastSpeed = fastSpeed;
		}

		/// <summary>The cut points over a population: the given percentiles (0..100) of HP per cost, of range (armed types) and of speed.</summary>
		public static ArmySlots From(IEnumerable<ArmySlotFacts> population, int frontPercentile, int backPercentile, int fastPercentile)
		{
			var all = population.ToArray();
			var ground = all.Where(f => !f.IsAircraft).ToArray();
			return new ArmySlots(
				Percentile(ground.Where(f => f.HasWeapon).Select(f => f.HpPerCostPermille), frontPercentile),
				Percentile(ground.Where(f => f.HasWeapon).Select(f => f.Range), backPercentile),
				Percentile(ground.Select(f => f.Speed), fastPercentile));
		}

		/// <summary>The value at the percentile (nearest rank below, integer); int.MaxValue for an empty set, so nothing passes the cut.</summary>
		public static int Percentile(IEnumerable<int> values, int percentile)
		{
			var sorted = values.OrderBy(v => v).ToArray();
			if (sorted.Length == 0)
				return int.MaxValue;

			return sorted[(sorted.Length - 1) * Math.Clamp(percentile, 0, 100) / 100];
		}

		public ArmySlot Classify(ArmySlotFacts facts)
		{
			if (facts.IsAircraft)
				return ArmySlot.Air;

			if (!facts.HasWeapon)
				return ArmySlot.Support;

			var durable = facts.HpPerCostPermille >= FrontHpPerCost;
			var longRange = facts.Range >= BackRange;
			if (durable && !longRange)
				return ArmySlot.Front;

			if (longRange && !durable)
				return ArmySlot.Back;

			return facts.Speed >= FastSpeed ? ArmySlot.Fast : ArmySlot.Line;
		}
	}
}
