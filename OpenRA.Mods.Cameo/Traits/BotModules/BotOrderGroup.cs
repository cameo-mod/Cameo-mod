#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING. In addition, please see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// AR-1: a grouped order (a null Subject with a GroupedActors array) resolves per member — the engine
	/// fans it out through Order.FromGroupedOrder — so the §19.6 order gate must judge every member on its
	/// own lease. A member the gate refuses is STRIPPED, not used to kill the whole order: refusing the
	/// group would drop a twenty-unit AttackMove over one conflicted member (a stall worse than the fight
	/// the gate exists to prevent), while stripping still keeps the usurped unit under its lease holder.
	/// The caller rebuilds the order over the survivors; an empty survivor list means the order dies.
	/// </summary>
	public static class BotOrderGroup
	{
		/// <summary>
		/// Which members of an order the gate may judge. Mirrors the single-order path: a unit worth
		/// judging is alive, owned by the bot, not the player actor, and mobile. Dead, foreign-owned and
		/// non-moving members pass through unjudged — the engine drops or ignores them at resolve time,
		/// exactly as it does for single orders.
		/// </summary>
		public static bool UnitInScope(Actor unit, OpenRA.Player owner)
		{
			return unit != null
				&& !unit.IsDead
				&& unit.Owner == owner
				&& unit != owner.PlayerActor
				&& unit.Info.HasTraitInfo<IMoveInfo>();
		}

		/// <summary>
		/// Runs the keep predicate over a group and returns the survivors. `Filtered` is false and the
		/// ORIGINAL array is returned when nothing dropped — the caller enqueues the order it was given.
		/// An empty `Members` array means every member was refused: there is nothing left to send.
		/// </summary>
		public static (TKey[] Members, bool Filtered) Filter<TKey>(TKey[] members, Func<TKey, bool> keep)
		{
			List<TKey> survivors = null;
			for (var i = 0; i < members.Length; i++)
			{
				if (keep(members[i]))
				{
					survivors?.Add(members[i]);
					continue;
				}

				if (survivors == null)
				{
					survivors = new List<TKey>(members.Length - 1);
					for (var j = 0; j < i; j++)
						survivors.Add(members[j]);
				}
			}

			return survivors == null ? (members, false) : (survivors.ToArray(), true);
		}

		/// <summary>
		/// Rebuilds a grouped order over a filtered member set. Every field the serialized order carries is
		/// preserved; VisualFeedbackTarget cannot be — grouped orders never have one (neither the public
		/// constructors nor Deserialize accept one), so nothing is lost.
		/// </summary>
		public static Order RebuildWithMembers(Order order, Actor[] members)
		{
			return new Order(order.OrderString, order.Subject, order.Target, order.Queued,
				order.ExtraActors, members)
			{
				Type = order.Type,
				TargetString = order.TargetString,
				ExtraLocation = order.ExtraLocation,
				ExtraData = order.ExtraData,
				IsImmediate = order.IsImmediate,
				SuppressVisualFeedback = order.SuppressVisualFeedback,
			};
		}
	}
}
