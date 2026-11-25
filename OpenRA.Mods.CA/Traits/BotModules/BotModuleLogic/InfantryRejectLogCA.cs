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
using System.Collections.Generic;
using System.Text;

namespace OpenRA.Mods.CA.Traits
{
	// INFANTRY-REJECT-LOG-01 (DEV only): why the infantry queues end a pass idle. Pure bookkeeping. The caller only feeds it when the DEV sink is
	// enabled, and nothing it holds is ever read by a production decision. One line per queue type per period.
	public sealed class InfantryRejectLogCA
	{
		public const int PeriodTicks = 750;

		// The fixed column order of the line: every reason is printed, zero or not.
		public static readonly string[] Reasons =
		{
			"delay", "interval", "limit", "capturer", "cappedout", "not_in_shares",
			"headroom", "aircraft", "afford_cash", "share_held", "no_candidate",
		};

		sealed class Tally
		{
			public int Passes;
			public int IdlePasses;
			public int FloorRan;
			public int FloorFound;
			public int LastFlush = -1;
			public readonly Dictionary<string, int> Rejects = new(StringComparer.Ordinal);
		}

		readonly Dictionary<string, Tally> tallies = new(StringComparer.Ordinal);

		Tally For(string queueType)
		{
			if (!tallies.TryGetValue(queueType, out var tally))
				tallies[queueType] = tally = new Tally();

			return tally;
		}

		/// <summary>One candidate of this queue was refused by <paramref name="reason"/> (one of <see cref="Reasons"/>).</summary>
		public void Reject(string queueType, string reason)
		{
			var tally = For(queueType);
			tally.Rejects[reason] = tally.Rejects.GetValueOrDefault(reason) + 1;
		}

		/// <summary>The idle-queue floor was asked for a pick on this queue; <paramref name="found"/> = it returned one.</summary>
		public void Floor(string queueType, bool found)
		{
			var tally = For(queueType);
			tally.FloorRan++;
			if (found)
				tally.FloorFound++;
		}

		/// <summary>The queue's pass ended; <paramref name="idle"/> = nothing is queued or ordered in it.</summary>
		public void Pass(string queueType, bool idle)
		{
			var tally = For(queueType);
			tally.Passes++;
			if (idle)
				tally.IdlePasses++;
		}

		/// <summary>When a period has passed since the last line of this queue type, returns one line and clears the counts.</summary>
		public bool TryFlush(string queueType, int now, out string line)
		{
			line = null;
			var tally = For(queueType);
			if (tally.Passes == 0 || (tally.LastFlush >= 0 && now - tally.LastFlush < PeriodTicks))
				return false;

			var text = new StringBuilder();
			text.Append("t=").Append(now).Append(" q=").Append(queueType)
				.Append(" passes=").Append(tally.Passes).Append(" idle=").Append(tally.IdlePasses)
				.Append(" floor_ran=").Append(tally.FloorRan).Append(" floor_found=").Append(tally.FloorFound);
			foreach (var reason in Reasons)
				text.Append(' ').Append(reason).Append('=').Append(tally.Rejects.GetValueOrDefault(reason));

			line = text.ToString();
			tally.LastFlush = now;
			tally.Passes = 0;
			tally.IdlePasses = 0;
			tally.FloorRan = 0;
			tally.FloorFound = 0;
			tally.Rejects.Clear();
			return true;
		}
	}
}
