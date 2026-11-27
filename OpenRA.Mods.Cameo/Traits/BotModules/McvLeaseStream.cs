#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

#if CAMEO_DEV_LOGGING
namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// CONSOLIDATE-03 P8 (a), DEV only: the equivalence hash of the MCV lease events (grant, release by holder gone / abort / stall). FNV-1a 64 over (kind, mcv id, field, centre,
	/// tick); logged at most once per 750 ticks as EX-LEASE-STREAM. Nothing reads it back. Compiled out of a release.
	/// </summary>
	sealed class McvLeaseStream
	{
		const ulong Prime = 1099511628211UL;
		ulong hash = 14695981039346656037UL;
		int count;
		int atLog;
		int loggedTick = -750;

		static ulong Mix(ulong h, int v)
		{
			unchecked
			{
				h ^= (uint)v;
				return h * Prime;
			}
		}

		public void Observe(int tick, int client, int kind, uint id, int field, CPos center)
		{
			hash = Mix(Mix(Mix(Mix(Mix(Mix(hash, kind), (int)id), field), center.X), center.Y), tick);
			count++;
			if (tick - loggedTick < 750 || count == atLog)
				return;

			loggedTick = tick;
			atLog = count;
			Log.Write("debug", $"AI ({client}): EX-LEASE-STREAM tick {tick} n {count} hash {hash:x16} kind {kind} mcv {id} field {field} at {center}");
		}
	}
}
#endif
