#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// DEF-PLACE-RETRY-01 (DEF-LANES P3a): the memory of defence cells the base builder authoritatively refused. Pure, no world.
	/// The only rejection is <see cref="Reject"/>, called when the reservation mask refused the chosen cell at the emission point.
	/// A queue that asks again for the same type is not a rejection and is not tracked here (its order may still be in flight).
	/// A rejected cell is skipped for a time that doubles with every further rejection of that cell (base x 2^(strikes - 1), capped) and is
	/// forgotten when a building of the type appears on it or when it has stayed quiet for the cap after its time ran out.
	/// </summary>
	public sealed class RejectedCells
	{
		readonly Dictionary<(string Type, CPos Cell), (int Until, int Strikes)> rejected = [];

		public int RejectedCount => rejected.Count;

		/// <summary>An authoritative rejection of the cell for this type; returns how long it is skipped.</summary>
		public int Reject(string type, CPos cell, int tick, int baseTicks, int maxTicks)
		{
			Prune(tick, maxTicks);
			var key = (type, cell);
			var strikes = rejected.TryGetValue(key, out var old) ? old.Strikes + 1 : 1;
			var duration = Duration(strikes, baseTicks, maxTicks);
			rejected[key] = (tick + duration, strikes);
			return duration;
		}

		/// <summary>base x 2^(strikes - 1), at least 1 and at most the cap.</summary>
		public static int Duration(int strikes, int baseTicks, int maxTicks)
		{
			var duration = (long)Math.Max(1, baseTicks) << Math.Clamp(strikes - 1, 0, 20);
			return (int)Math.Clamp(duration, 1, Math.Max(1, maxTicks));
		}

		/// <summary>
		/// A building of this type stands with its location on the cell: its strikes are forgotten. Only ever shortens a skip, so a location
		/// that is not the ordered cell (it would simply not match) cannot create a rejection.
		/// </summary>
		public void Confirm(string type, CPos cell)
		{
			rejected.Remove((type, cell));
		}

		public bool IsRejected(string type, CPos cell, int tick)
		{
			return rejected.Count > 0 && rejected.TryGetValue((type, cell), out var entry) && tick < entry.Until;
		}

		// Strikes decay: an entry that has been free again for the cap is dropped, so an old failure does not lengthen a new one.
		void Prune(int tick, int maxTicks)
		{
			if (rejected.Count < 16)
				return;

			List<(string, CPos)> old = null;
			foreach (var (key, value) in rejected)
				if (tick - value.Until > maxTicks)
					(old ??= []).Add(key);

			if (old != null)
				foreach (var key in old)
					rejected.Remove(key);
		}
	}
}
