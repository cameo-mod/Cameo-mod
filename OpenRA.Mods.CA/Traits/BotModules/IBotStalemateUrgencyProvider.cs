#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules
{
	/// <summary>
	/// BOTPASSIVE (design _06 §3-§5, _07, _08): the master module's ON-only urgency facts, read by the stalemate
	/// breaker in the squad manager. Every value is from the latest master snapshot (150 ticks) or emergency check (25 ticks).
	/// Enabled is false (and every other member is a neutral default) whenever the switch is off.
	/// </summary>
	public interface IBotStalemateUrgencyProvider
	{
		bool StalemateUrgencyEnabled { get; }

		/// <summary>Urgency′ as 0 = Normal, 1 = Pressured, 2 = Emergency.</summary>
		int StalemateUrgency { get; }

		/// <summary>The legacy (switch-OFF) urgency at the same snapshot, telemetry only (Opus N7).</summary>
		int StalemateUrgencyLegacy { get; }

		/// <summary>Emergency′: the breaker's homeEmergency (one home predicate for the breaker and the global urgency).</summary>
		bool StalemateHomeEmergency { get; }

		bool StalemateHomeThreat { get; }

		bool StalemateHomeLatch { get; }

		/// <summary>Observed (or remembered this snapshot) enemy combat value within its home-threat radius of the own anchors, from the same snapshot as StalemateHomeThreat. 0 = none.</summary>
		long StalemateHomeThreatValue { get; }

		/// <summary>Value of the own armed static buildings (walls excluded) within the home radius of the own anchors at the same snapshot.</summary>
		long StalemateHomeDefenceValue { get; }

		/// <summary>Own live combat value (ownArmy) at the latest snapshot.</summary>
		long StalemateOwnArmy { get; }

		/// <summary>Live combat value of mutual allies at the latest snapshot (design _07 L2).</summary>
		long StalemateAlliedArmy { get; }

		/// <summary>Sum of the enemy profiles' remembered ArmyValue (the existing enemyArmy, denT).</summary>
		long StalemateEnemyArmy { get; }
	}
}
