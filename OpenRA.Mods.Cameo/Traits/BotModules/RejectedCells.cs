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
	/// DEF-PLACE-RETRY-01 (DEF-LANES P3a): the memory of defence cells that were chosen and then not built on. Pure, no world.
	/// A cell is rejected only on an explicit signal tied to one attempt of one production queue (producer) and type:
	/// - the reservation mask refused it at the emission point (<see cref="Reject"/>), or
	/// - its place order went out (<see cref="Issue"/>), no building appeared (<see cref="Confirm"/>), and the same queue asks for the same
	///   type again at least a grace time later (<see cref="Begin"/>). Asking again inside the grace time is a pending order, not a rejection.
	/// A rejected cell is skipped for a time that doubles with every further rejection of that cell (base x 2^(strikes - 1), capped) and is
	/// forgotten when a building is confirmed on it or when it has stayed quiet for the cap after its time ran out.
	/// </summary>
	public sealed class RejectedCells
	{
		readonly Dictionary<(uint Producer, string Type), (CPos Cell, int Tick)> pending = [];
		readonly Dictionary<(string Type, CPos Cell), (int Until, int Strikes)> rejected = [];

		public int PendingCount => pending.Count;
		public int RejectedCount => rejected.Count;

		/// <summary>The attempt of this queue and type starts. Returns the cell that is rejected by it, if its last order produced nothing.</summary>
		public CPos? Begin(uint producer, string type, int tick, int graceTicks, int baseTicks, int maxTicks)
		{
			var key = (producer, type);
			if (!pending.TryGetValue(key, out var last) || tick - last.Tick < graceTicks)
				return null;

			pending.Remove(key);
			Reject(type, last.Cell, tick, baseTicks, maxTicks);
			return last.Cell;
		}

		/// <summary>The place order for the cell went out.</summary>
		public void Issue(uint producer, string type, CPos cell, int tick)
		{
			pending[(producer, type)] = (cell, tick);
		}

		/// <summary>An explicit rejection of the cell for this type; returns how long it is skipped.</summary>
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

		/// <summary>A building of this type stands on the cell: every pending order for it is done and its strikes are forgotten.</summary>
		public void Confirm(string type, CPos cell)
		{
			rejected.Remove((type, cell));
			if (pending.Count == 0)
				return;

			List<(uint, string)> done = null;
			foreach (var (key, value) in pending)
				if (key.Type == type && value.Cell == cell)
					(done ??= []).Add(key);

			if (done != null)
				foreach (var key in done)
					pending.Remove(key);
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
