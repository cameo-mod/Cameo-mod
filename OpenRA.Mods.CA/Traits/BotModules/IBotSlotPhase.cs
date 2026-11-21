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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// QW-B (STAGGER_DESIGN_06 section 15, PERF_SPIKE_01_02 tier 1): shared-phase timers. Several Cameo bots running the
	// same periodic work on the same absolute tick all spike in one frame. A bot whose type sets SlotPhasedTimers gives
	// each of its periodic modules a phase offset from its seat rank, so the bots' bursts land on different ticks.
	// Implemented by the bot (ModularBot, in OpenRA.Mods.Cameo); the modules that need it live in this assembly too,
	// so they reach it through this seam. A bot that does not implement it, or whose flag is off, gives no phase.
	public interface IBotSlotPhase
	{
		/// <summary>True when this bot is active, phase-offsets its timers and has its roster (from the first bot tick on), the
		/// roster holds more than one seat, and this bot is in it. <paramref name="phase"/> is then in [0, period). False means
		/// every site keeps its legacy schedule (N = 1 identity).</summary>
		bool TryPhaseOf(int period, out int phase);
	}

	public static class BotSlotPhases
	{
		/// <summary>The phase of the bot that owns <paramref name="player"/> for a timer of the given period. False (the site
		/// keeps its existing phase) when no active bot of the player phase-offsets its timers. Enumerates a trait list:
		/// call it once per module (when seeding), not every tick.</summary>
		public static bool TryResolve(Player player, int period, out int phase)
		{
			phase = 0;
			if (period <= 1)
				return false;

			foreach (var bot in player.PlayerActor.TraitsImplementing<IBotSlotPhase>())
				if (bot.TryPhaseOf(period, out phase))
					return true;

			phase = 0;
			return false;
		}

		/// <summary>The same through the bot a module is ticking under (no trait enumeration).</summary>
		public static bool TryResolve(IBot bot, int period, out int phase) => TryResolve(bot as IBotSlotPhase, period, out phase);

		public static bool TryResolve(IBotSlotPhase phased, int period, out int phase)
		{
			phase = 0;
			if (period <= 1 || phased == null)
				return false;

			return phased.TryPhaseOf(period, out phase);
		}

		/// <summary>The phase for a site whose existing phase is <paramref name="today"/>.</summary>
		public static int Resolve(IBot bot, int period, int today) => TryResolve(bot, period, out var phase) ? phase : today;

		public static int Resolve(IBotSlotPhase phased, int period, int today) => TryResolve(phased, period, out var phase) ? phase : today;

		/// <summary>The phase for a site whose existing phase is <paramref name="today"/>.</summary>
		public static int Resolve(Player player, int period, int today) => TryResolve(player, period, out var phase) ? phase : today;

		/// <summary>True on the ticks of a phase-offset period: tick == phase (mod period). With phase 0 this is `tick % period == 0`.</summary>
		public static bool IsDue(int tick, int period, int phase)
		{
			if (period <= 1)
				return true;

			return ((tick - phase) % period + period) % period == 0;
		}

		/// <summary>The first tick at or after <paramref name="now"/> that is on the phase-offset period.</summary>
		public static int NextDue(int now, int period, int phase)
		{
			if (period <= 1)
				return now;

			return now + (phase - now % period + period) % period;
		}
	}
}
