#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// Hotspot #3 extraction (2026-10-04): the pure decision atoms of
	/// SquadManagerBotModuleCA.CreateAttackForce — the launch gate, the Defend-hold
	/// window, the raid-steering overcommit cap, and the fire-support escort quota.
	/// Behaviour-preserving; everything world-facing stays in the method.
	/// </summary>
	public static class AttackForceEvalCA
	{
		/// <summary>The two launch arms: hitting MaxIdleUnits launches regardless of the
		/// value/size gates (overflow); otherwise the pool's army value must reach
		/// requiredValue AND the count gate (waived entirely by ValueOnlyAttackLaunch
		/// when a squad value is configured — CA F2p2 A5-2).</summary>
		public static bool ShouldLaunch(int poolCount, int maxIdleUnits, int idleUnitsValue,
			int requiredValue, int requiredSize, bool valueOnlyLaunch, int squadValue)
		{
			var countGateMet = (valueOnlyLaunch && squadValue > 0) || poolCount >= requiredSize;
			return poolCount >= maxIdleUnits || (idleUnitsValue >= requiredValue && countGateMet);
		}

		/// <summary>A Defend mission holds the pool while its hold window is unexpired;
		/// the clock resets when a different region re-publishes (checked by the caller).</summary>
		public static bool DefendHoldActive(int heldTicks, int holdTicks)
		{
			return heldTicks <= System.Math.Max(0, holdTicks);
		}

		/// <summary>TC-2f (§12.27): the steering cap is the larger of the overcommit ratio
		/// and the flat floor — long math, a deep pool x the percent passes int.</summary>
		public static int RaidSteerCap(int idleUnitsValue, int overcommitPercent, int minValue)
		{
			return (int)System.Math.Min(int.MaxValue, System.Math.Max(
				(long)idleUnitsValue * overcommitPercent / 100, (long)minValue));
		}

		/// <summary>12.4a: fire-support escorts per artillery piece, capped at a third of
		/// the assault — the screen must win the flanker fight without stripping the raid.</summary>
		public static int EscortsNeeded(int artilleryCount, int assaultCount, int perArtillery)
		{
			return System.Math.Min(artilleryCount * perArtillery, assaultCount / 3);
		}
	}
}
