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

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-03 P6 (a): DEV-only order-stream hash of the pending-start registry. Every upgrade start, unit emission and pass refusal is folded into one FNV-1a 64 hash;
	// a pst_hash row goes to unit_roll.csv every 5000 ticks. Equivalence proof: the same seed and the same switches give the same hash sequence before and after the registry refactor.
	// Compiled out of a release build (the Conditional attribute drops the calls and their string arguments).
	public partial class UnitBuilderBotModuleCA
	{
#if CAMEO_DEV_LOGGING
		ulong pendingStream = 14695981039346656037UL;
		int pendingStreamCount;
		int pendingStreamLogged = -5000;

		static ulong PendingMix(ulong h, string s)
		{
			foreach (var c in s)
			{
				h ^= c;
				h *= 1099511628211UL;
			}

			return h;
		}

		static ulong PendingMix(ulong h, long v)
		{
			for (var i = 0; i < 8; i++)
			{
				h ^= (byte)(v >> (i * 8));
				h *= 1099511628211UL;
			}

			return h;
		}
#endif

		/// <summary>kind: upg_start, unit_emit, refuse_capturer or refuse_aircraft; name: the upgrade or actor name; amount: the cost (0 for a refusal).</summary>
		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void PendingStreamNote(string kind, string name, int tick, int amount)
		{
#if CAMEO_DEV_LOGGING
			var h = pendingStream;
			h = PendingMix(h, kind);
			h = PendingMix(h, name);
			h = PendingMix(h, tick);
			h = PendingMix(h, amount);
			pendingStream = h;
			pendingStreamCount++;
			if (world.WorldTick - pendingStreamLogged < 5000)
				return;

			pendingStreamLogged = world.WorldTick;
			RollLog($"{world.WorldTick},{Array.IndexOf(world.Players, player)},pst_hash,{pendingStream:x16},,{pendingStreamCount}");
#endif
		}
	}
}
