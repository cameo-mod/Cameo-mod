#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// LEARN-P6 (SPEC 2026-10-05 §11/A4): the six postures a squad can want. The provider picks
	/// one per squad per eval. The ground squad controller executes six distinct routes;
	/// an independent integer combat guard blocks unsafe attacks immediately.
	/// </summary>
	public enum SquadDesireStance { Attack, Defend, Retreat, Regroup, Harass, Reinforce }

	/// <summary>
	/// LEARN-P6: the fog-honest facts the squad manager packages for the desire provider each eval.
	/// Squad internals are internal to this assembly, so the consumer ships facts, never objects the
	/// provider could read past — the provider alone computes stance desirability from them (§19.3).
	/// All strengths are value units (ValuedInfo.Cost sums); ratios/distances are fixed-point
	/// thousandths; nothing here ever saw an unobserved enemy.
	/// </summary>
	public readonly struct SquadDesireSignals
	{
		/// <summary>The squad's own cost value (SquadValueOf).</summary>
		public readonly int OwnValue;

		/// <summary>Observed enemy value near the squad's target (the units the state already collected).</summary>
		public readonly int SeenEnemyValue;

		/// <summary>Observed enemy value near the own base centre (MaxBaseRadius).</summary>
		public readonly int BaseEnemyValue;

		/// <summary>The integer rules-derived combat predictor's own/enemy ratio x1000, clamped to [0, 4000].</summary>
		public readonly int PredictedRatioMilli;

		/// <summary>Mean health of the orderable members, 0..1000.</summary>
		public readonly int HealthMilli;

		/// <summary>Mean member distance from the squad centre vs a 16-cell reference, clamped to 0..1000.</summary>
		public readonly int ScatterMilli;

		/// <summary>The squad's committed target is still valid.</summary>
		public readonly bool HasTarget;

		/// <summary>Guerrilla/Harass squad types — the stances a raiding chassis can express.</summary>
		public readonly bool HarassCapable;

		/// <summary>WorldTick at the eval — the provider's dwell/prune clock.</summary>
		public readonly int WorldTick;

		public SquadDesireSignals(int ownValue, int seenEnemyValue, int baseEnemyValue, int predictedRatioMilli,
			int healthMilli, int scatterMilli, bool hasTarget, bool harassCapable, int worldTick)
		{
			OwnValue = ownValue;
			SeenEnemyValue = seenEnemyValue;
			BaseEnemyValue = baseEnemyValue;
			PredictedRatioMilli = predictedRatioMilli;
			HealthMilli = healthMilli;
			ScatterMilli = scatterMilli;
			HasTarget = hasTarget;
			HarassCapable = harassCapable;
			WorldTick = worldTick;
		}
	}

	/// <summary>
	/// LEARN-P6 (SPEC §11, lead ruling §15.4): the squad desire provider — per squad, per stance, a
	/// leaky integrator over that stance's urgency plus a capped personality bias; a stance switch
	/// needs best &gt; current + margin AND MinDwellTicks elapsed. It issues NO orders: the per-personality
	/// SquadManagerBotModuleCA instances consume <see cref="StanceFor"/> in place of the binary
	/// attack/retreat branch, and §19.3 holds because only the provider computes desirability.
	/// No enabled provider (classic, switch off) = the consumers keep the unchanged binary call.
	/// </summary>
	public interface IBotSquadDesire
	{
		/// <summary>The squad's stance this eval. Advances the integrators; call once per squad eval.</summary>
		SquadDesireStance StanceFor(SquadCA squad, in SquadDesireSignals signals);
	}
}
