#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	// BASE-FIX-02 (AUDIT_BASE_01). Pure decisions, no state.
	public static class BaseFix02CA
	{
		/// <summary>BASE-06 / BASE-16: a hold that began at <paramref name="since"/> has lasted <paramref name="limit"/> ticks (off = never).</summary>
		public static bool HoldExpired(bool armed, int now, int since, int limit) => armed && since >= 0 && now - since >= limit;

		/// <summary>The age of a hold: a hold not seen for <paramref name="staleGap"/> ticks starts over. Returns the new (since, last) pair.</summary>
		public static (int Since, int Last) TouchHold(bool known, int since, int last, int now, int staleGap) =>
			known && now - last <= staleGap ? (since, now) : (now, now);

		/// <summary>BASE-07: the type is banned while the ban runs and no new base provider has appeared since it was set (off = never).</summary>
		public static bool BanActive(bool armed, int now, int until, int providersNow, int providersAtBan) =>
			armed && now < until && providersNow <= providersAtBan;

		/// <summary>BASE-12: the index of the first safe claim, or -1 when there is none.</summary>
		public static int FirstSafe(IReadOnlyList<bool> claimSafe)
		{
			for (var i = 0; i < claimSafe.Count; i++)
				if (claimSafe[i])
					return i;

			return -1;
		}

		/// <summary>BASE-12: the refinery want is blocked only when claims exist and none of them is safe.</summary>
		public static bool SafetyBlocksAll(IReadOnlyList<bool> claimSafe) => claimSafe.Count > 0 && FirstSafe(claimSafe) < 0;
	}
}
