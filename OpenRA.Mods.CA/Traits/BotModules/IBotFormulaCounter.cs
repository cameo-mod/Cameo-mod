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

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ARMY-BRAIN-01 S1: a provider of the counter multiplier of the unit roll, computed from unit stats against the enemy the bot has seen
	/// and remembers (units and defences). While a provider is active it REPLACES the classic counter term (UnitRoll.CounterPct) of the roll;
	/// the roll floor is still applied after it by UnitPickArbiter. No provider, or none active, leaves the roll unchanged. Providers live in
	/// another assembly and must not be referenced by name here.
	/// </summary>
	public interface IBotFormulaCounter
	{
		/// <summary>False = the switch is off, the trait is disabled or the row ab_formula is ablated.</summary>
		bool IsActive { get; }

		/// <summary>
		/// Fills <paramref name="percents"/> (same index as <paramref name="pool"/>; at least pool.Count long) with the counter multiplier of
		/// each candidate, in percent (100 = neutral). With no enemy known every entry is 100.
		/// </summary>
		void FormulaCounterPercents(IReadOnlyList<ActorInfo> pool, int[] percents);
	}

	/// <summary>The roll side of the formula counter, apart from the roll so that "off = the classic counter term" is testable.</summary>
	public static class FormulaCounters
	{
		/// <summary>The first active provider, or null: no provider, the switch off, the trait disabled or the row ablated.</summary>
		public static IBotFormulaCounter Active(IReadOnlyList<IBotFormulaCounter> providers)
		{
			if (providers != null)
				foreach (var p in providers)
					if (p.IsActive)
						return p;

			return null;
		}

		/// <summary>
		/// With no active provider (<paramref name="formula"/> null) the percents are left exactly as the classic counter term wrote them.
		/// With one, they are replaced, indexed by the pool; a type the bot has never ordered (<paramref name="ordered"/> false) is never
		/// lowered below 100, so first copies and newly unlocked types keep their chance.
		/// </summary>
		public static void Apply(IBotFormulaCounter formula, IReadOnlyList<ActorInfo> pool, int[] percents, System.Func<string, bool> ordered)
		{
			if (formula == null)
				return;

			formula.FormulaCounterPercents(pool, percents);
			for (var i = 0; i < pool.Count; i++)
				if (!ordered(pool[i].Name))
					percents[i] = System.Math.Max(100, percents[i]);
		}
	}

	public static partial class AblationIds
	{
		// ARMY-BRAIN-01 S1 row. Owner: Fable. Off = the classic counter term (UnitRoll.CounterPct) drives the roll.
		public const string ArmyBrainFormula = "ab_formula";
	}
}
