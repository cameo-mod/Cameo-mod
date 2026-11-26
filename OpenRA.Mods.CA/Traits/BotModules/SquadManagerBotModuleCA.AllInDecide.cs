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
	// CONSOLIDATE-03 P10 phase (a) (DESIGN_02 bb5d77bb; Sol 49BE2103 and BB5D77BB; Opus and Leader rulings). The all-in decision is RECORDED, never changed: AllInReturn hands
	// the decision back exactly as TeamLeadAllInWhy computed it. Control flow, lazy scans, the latch and defeat writes and the live stalemate read stay where they are.
	// The DEV hash (row all_in_decide, every AllInDecideRowTicks) is diagnostic only. The phase gate is the full replay order stream of the match with producer and queue
	// identity plus world sync hashes; that instrument is not part of this change (no single mod-side choke point: the orders are issued from many call sites).
	public partial class SquadManagerBotModuleCA
	{
		const int AllInDecideRowTicks = 5000;
		ulong allInDecideHash = 14695981039346656037UL;
		int allInDecideCount;
		int allInDecideRowNext;

		// Hands a decision back unchanged (release and DEV alike). A null decision means no all-in is asked this tick.
		string AllInReturn(int now, string why)
		{
			AllInRecord(now, why);
			return why;
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void AllInRecord(int now, string why)
		{
			var h = allInDecideHash;
			h = AllInMix(h, now);
			h = AllInMix(h, why ?? "-");
			allInDecideHash = h;
			allInDecideCount++;
			if (now < allInDecideRowNext)
				return;

			allInDecideRowNext = now + AllInDecideRowTicks;
			DiagnosticTeamEvent("all_in_decide", "hash=" + h.ToString("x16") + " count=" + allInDecideCount + " tick=" + now);
		}

		static ulong AllInMix(ulong h, string s)
		{
			foreach (var c in s)
			{
				h ^= c;
				h *= 1099511628211UL;
			}

			return h;
		}

		static ulong AllInMix(ulong h, int v)
		{
			for (var i = 0; i < 4; i++)
			{
				h ^= (byte)(v >> (i * 8));
				h *= 1099511628211UL;
			}

			return h;
		}
	}
}
