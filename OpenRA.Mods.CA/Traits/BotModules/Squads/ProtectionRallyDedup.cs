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

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// AR-S2 (BM_protection_rally_dedup): the squad manager's defence preposition tick
	/// re-pushed AttackMove(rally) to every protection member on every ProtectInterval —
	/// a same-cell resend that cancels the in-flight activity and restarts the path, and
	/// the last large untagged order-churn emitter left after BJ/BK/BL (attrib3: 27-28
	/// flagged units per match, all dominated by that push). The lattice records which
	/// rally cell the members actually carry and who received it:
	///
	/// - rally moved past ProtectionRallyHysteresisCells -> everyone re-orders (real redirect);
	/// - rally inside the band -> only members that joined since the last push get an order
	///   (existing members keep marching, no restart);
	/// - nothing changed -> no order at all.
	///
	/// The state machine still owns per-tick combat/lure orders — this dedup only gates
	/// the manager's periodic group push. Reset when the protection squad releases.
	/// Generic over the member identity so the rule is unit-testable without a World.
	/// </summary>
	public sealed class ProtectionRallyDedup<T> where T : class
	{
		CPos? ordered;
		readonly HashSet<T> issued = new();

		public void Reset()
		{
			ordered = null;
			issued.Clear();
		}

		/// <summary>
		/// The emit set for the group rally push, or null when nothing changed.
		/// <paramref name="members"/> is the squad's current member actors (post-draft).
		/// </summary>
		public T[] EmitSet(IReadOnlyList<T> members, CPos rally, int hysteresisCells)
		{
			// Members that left the squad since the last push stop counting as issued.
			issued.RemoveWhere(a => !members.Contains(a));

			var bandSquared = (long)hysteresisCells * hysteresisCells;
			if (!ordered.HasValue || (rally - ordered.Value).LengthSquared >= bandSquared)
			{
				ordered = rally;
				issued.Clear();
				foreach (var m in members)
					issued.Add(m);

				return members.ToArray();
			}

			var joiners = members.Where(m => !issued.Contains(m)).ToArray();
			foreach (var m in joiners)
				issued.Add(m);

			return joiners.Length > 0 ? joiners : null;
		}
	}
}
