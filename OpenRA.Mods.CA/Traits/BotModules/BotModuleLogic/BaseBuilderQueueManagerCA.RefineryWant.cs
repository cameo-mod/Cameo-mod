#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P3: the recording side of RefineryWant. One gate per ChooseWantedRefinery call; DEV only: a running FNV-1a hash of (queue kind, gate, picked) of every call, the equivalence
	// proof (same seed and old switches give the same hash as the base probed at the same return points).
	partial class BaseBuilderQueueManagerCA
	{
		RefineryGate lastRefineryGate;

		ActorInfo RefineryHeld(RefineryGate gate)
		{
			lastRefineryGate = gate;
			RefineryWantObserved(gate, false);
			return null;
		}

#if CAMEO_DEV_LOGGING
		ulong refineryWantStream = 14695981039346656037UL;
		int refineryWantCount;
		int refineryWantAtLog;
		int refineryWantLogged = -750;
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void RefineryWantObserved(RefineryGate gate, bool picked)
		{
#if CAMEO_DEV_LOGGING
			var h = refineryWantStream;
			foreach (var v in new long[] { (int)ThisQueueKind, (int)gate, picked ? 1 : 0 })
			{
				for (var i = 0; i < 8; i++)
				{
					h ^= (byte)(v >> (i * 8));
					h *= 1099511628211UL;
				}
			}

			refineryWantStream = h;
			refineryWantCount++;

			var now = world.WorldTick;
			if (now - refineryWantLogged < 750 || refineryWantCount == refineryWantAtLog)
				return;

			refineryWantLogged = now;
			refineryWantAtLog = refineryWantCount;
			Log.Write("debug", $"AI ({player.ClientIndex}): BB-REFWANT-STREAM queue {Category} tick {now} n {refineryWantCount} hash {refineryWantStream:x16} gate {gate} picked {picked}");
#endif
		}
	}
}
