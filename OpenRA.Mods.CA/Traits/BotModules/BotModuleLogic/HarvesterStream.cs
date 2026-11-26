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
namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P9 (a), DEV only: the equivalence hash of the harvester-count decisions (HarvesterPlan). An FNV-1a 64 running hash over (site, live, queued, refineries, need,
	/// verdict); logged at most once per 750 ticks as HV-PLAN-STREAM. Nothing reads it back. Compiled out of a release.
	/// </summary>
	sealed class HarvesterStream
	{
		const ulong Prime = 1099511628211UL;
		ulong hash = 14695981039346656037UL;
		int count;
		int atLog;
		int loggedTick = -750;

		ulong Mix(ulong h, int v)
		{
			unchecked
			{
				h ^= (uint)v;
				return h * Prime;
			}
		}

		public void Observe(int tick, int client, int site, int live, int queued, int refineries, int need, int verdict)
		{
			hash = Mix(Mix(Mix(Mix(Mix(Mix(hash, site), live), queued), refineries), need), verdict);
			count++;
			if (tick - loggedTick < 750 || count == atLog)
				return;

			loggedTick = tick;
			atLog = count;
			Log.Write("debug", $"AI ({client}): HV-PLAN-STREAM tick {tick} n {count} hash {hash:x16} site {site} live {live} queued {queued} refineries {refineries} need {need} verdict {verdict}");
		}
	}
}
#endif
