#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P4: the queue manager's side of PowerArbiter. Every power question of the building queues goes through here, so the DEV stream sees each one.
	partial class BaseBuilderQueueManagerCA
	{
		int BuildOrderPowerMilli()
		{
			var knobs = baseBuilder.BuildOrderKnobs;
			return knobs == null ? BuildOrderKnob.Neutral : knobs.KnobMilli(BuildOrderKnob.PowerMargin);
		}

		// The raw emergency test (essential holds, mode bias, recovery): the surplus right now against the required margin.
		bool PowerShortRaw(int site)
		{
			var excess = playerPower?.ExcessPower ?? 0;
			var result = PowerArbiter.Short(playerPower != null, excess, minimumExcessPower);
			PowerObserved(site, excess, minimumExcessPower, 0, result);
			return result;
		}

		// The surplus the emergency picks (S1, the legacy override) read: pw_inflight_emergency adds the plants in production.
		internal int EmergencyExcess() =>
			PowerArbiter.Effective(playerPower?.ExcessPower ?? 0, 0, false); // the in-flight forecast arrives with the P4 rows (commit b)

		bool PowerShortEmergency(int site)
		{
			var excess = EmergencyExcess();
			var result = PowerArbiter.Short(playerPower != null, excess, minimumExcessPower);
			PowerObserved(site, excess, minimumExcessPower, 0, result);
			return result;
		}

		// DEV only: a running FNV-1a hash of every power decision (site, surplus, margin, candidate power, verdict), logged each 750 ticks when it moved. The same seed with the P4 rows
		// off gives the same hash sequence as the base: the equivalence proof of CONSOLIDATE-03 P4 (a).
#if CAMEO_DEV_LOGGING
		ulong powerStream = 14695981039346656037UL;
		int powerStreamCount;
		int powerStreamAtLog;
		int powerStreamLogged = -750;

		static ulong PowerMix(ulong h, long v)
		{
			for (var i = 0; i < 8; i++)
			{
				h ^= (byte)(v >> (i * 8));
				h *= 1099511628211UL;
			}

			return h;
		}
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PowerObserved(int site, int excess, int required, int amount, bool verdict)
		{
#if CAMEO_DEV_LOGGING
			var h = powerStream;
			h = PowerMix(h, site);
			h = PowerMix(h, excess);
			h = PowerMix(h, required);
			h = PowerMix(h, amount);
			h = PowerMix(h, verdict ? 1 : 0);
			powerStream = h;
			powerStreamCount++;

			var now = world.WorldTick;
			if (now - powerStreamLogged < 750 || powerStreamCount == powerStreamAtLog)
				return;

			powerStreamLogged = now;
			powerStreamAtLog = powerStreamCount;
			Log.Write("debug", $"AI ({player.ClientIndex}): BB-POWER-STREAM queue {Category} tick {now} n {powerStreamCount} hash {powerStream:x16} site {site} excess {excess} required {required} amount {amount} short {verdict}");
#endif
		}
	}
}
