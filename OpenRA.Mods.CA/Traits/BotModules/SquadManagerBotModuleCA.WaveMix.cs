#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// ARMY-MIX-01 (USER P0, 2026-11-23): attack waves mix all available unit types. The manager owns the gate (a ready wave waits, bounded,
	// for a missing role); the unit builder owns the pick (WHAT to build for a missing role, never whether) and reports which roles it can build.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateArmyMix;
		WaveMixClock waveMixClock = WaveMixClock.Idle;

		/// <summary>ARMY-MIX-01 is on for this bot: the Info switch and the DEV ablation row (row off = the old launch and build path).</summary>
		internal bool WaveMixActive => Info.WaveMixEnabled && !ablateArmyMix.Off(Player, AblationIds.ArmyMix);

		/// <summary>The distinct wave roles among the idle pool's units (primary role each). Empty without a roles provider.</summary>
		internal HashSet<string> WaveMixPoolRoles()
		{
			var roles = UnitRoles;
			if (roles == null)
				return [];

			return WaveMixCA.DistinctRoles(unitsHangingAroundTheBase.Select(u => roles.PrimaryRoleOf(u.Actor.Info.Name)));
		}

		/// <summary>
		/// True while a ready wave should wait for a missing role. Needs the unit builder's report of the roles it can build now; without one
		/// (no provider, no pick yet) there is nothing to wait for. Bounded by WaveMixWaitTicks: then the wave is released and stays released until the role arrives or the wave launches.
		/// </summary>
		bool WaveMixHolds()
		{
			if (!WaveMixActive || UnitRoles == null)
				return false;

			var builder = Player.PlayerActor.TraitsImplementing<UnitBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			if (builder == null || !builder.TryAvailableWaveRoles(World.WorldTick, out var available))
				return false;

			var needed = WaveMixCA.Needed(Info.WaveMixMinRoles, available.Count);
			var satisfied = WaveMixPoolRoles().Count >= needed;
			var (hold, next) = WaveMixCA.Step(World.WorldTick, waveMixClock, Info.WaveMixWaitTicks, satisfied);
			waveMixClock = next;
			return hold;
		}

		/// <summary>The wave left the base: the next wave starts a fresh wait (a released wait stays released until here).</summary>
		void WaveMixLaunched() => waveMixClock = WaveMixClock.Idle;
	}
}
