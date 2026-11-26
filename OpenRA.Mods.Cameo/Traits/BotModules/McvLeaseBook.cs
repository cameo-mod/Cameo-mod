#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>How an MCV's field lease ended (the DEV stream records it).</summary>
	public enum McvLeaseEnd
	{
		/// <summary>The MCV is gone: deployed into a yard, dead or no longer ours (the planner's holder-gone sweep).</summary>
		HolderGone = 1,

		/// <summary>McvSafe aborted the trip (zone or threat) and parked the field.</summary>
		Abort = 2,

		/// <summary>Row mcv_lease_stall_release: the MCV has not changed cell for McvLeaseStallTicks.</summary>
		Stall = 3,
	}

	/// <summary>
	/// CONSOLIDATE-03 P8 (a) (CONSOLIDATE_03_P8_DESIGN_01.md): the ONE record of "this MCV is heading for this field". It replaces the planner's bare inflightMcvSites map; every grant,
	/// release and read goes through it, so the lifecycle (granted, moving, released by holder gone / abort / stall) is in one place. The book orders nothing: the engine's
	/// McvExpansionManager stays the only owner of MCV move and deploy orders. Pure, no world.
	/// </summary>
	public sealed class McvLeaseBook
	{
		public readonly record struct Lease(int Field, CPos Center, int GrantTick, CPos LastCell, int LastMoveTick);

		readonly Dictionary<uint, Lease> leases = new();

		public int Count => leases.Count;

		/// <summary>Grants (or re-grants: a later hand-out replaces the lease and restarts its movement clock) the lease of <paramref name="id"/> on a field.</summary>
		public void Grant(uint id, int field, CPos center, CPos mcvCell, int tick) => leases[id] = new Lease(field, center, tick, mcvCell, tick);

		/// <summary>Ends the lease of <paramref name="id"/>. False = it held none.</summary>
		public bool Release(uint id) => leases.Remove(id);

		public bool Holds(uint id) => leases.ContainsKey(id);

		/// <summary>The leased field index of <paramref name="id"/>, -1 = none.</summary>
		public int FieldOf(uint id) => leases.TryGetValue(id, out var l) ? l.Field : -1;

		/// <summary>A snapshot of the ids that hold a lease (the sweep releases while it walks).</summary>
		public uint[] Ids() => leases.Keys.ToArray();

		/// <summary>The centres of every leased field (the claim filter reads these).</summary>
		public IEnumerable<CPos> Centers()
		{
			foreach (var l in leases.Values)
				yield return l.Center;
		}

		/// <summary>Adds the centres of every lease except <paramref name="exceptId"/>'s (the separation avoid list of that MCV's next field).</summary>
		public void AddCentersExcept(uint exceptId, List<CPos> into)
		{
			foreach (var kv in leases)
				if (kv.Key != exceptId)
					into.Add(kv.Value.Center);
		}

		/// <summary>The MCV stands on <paramref name="cell"/> at <paramref name="tick"/>: a changed cell restarts the movement clock of its lease.</summary>
		public void Note(uint id, CPos cell, int tick)
		{
			if (leases.TryGetValue(id, out var l) && l.LastCell != cell)
				leases[id] = l with { LastCell = cell, LastMoveTick = tick };
		}

		/// <summary>The lease of <paramref name="id"/> exists and its MCV has not changed cell for at least <paramref name="stallTicks"/> (0 or less = never).</summary>
		public bool Stalled(uint id, int tick, int stallTicks) => leases.TryGetValue(id, out var l) && IsStalled(l.LastMoveTick, tick, stallTicks);

		/// <summary>No cell change for <paramref name="stallTicks"/> ticks or more. Pure.</summary>
		public static bool IsStalled(int lastMoveTick, int tick, int stallTicks) => stallTicks > 0 && tick - lastMoveTick >= stallTicks;
	}
}
