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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// What a squad last ordered one member to do. Cell targets are quantized to CPos so a
	/// drifting WPos or a moving leader only re-issues once the destination cell actually
	/// changes; actor targets key on the target's identity; target-less orders (Stop,
	/// Scatter, ReturnToBase) key on the order name alone.
	/// </summary>
	public readonly struct SquadOrderKey
	{
		public readonly string Order;
		public readonly CPos Cell;
		public readonly uint TargetActorId;

		public SquadOrderKey(string order, CPos cell, uint targetActorId)
		{
			Order = order;
			Cell = cell;
			TargetActorId = targetActorId;
		}

		public static SquadOrderKey ForCell(string order, CPos cell) => new(order, cell, 0);
		public static SquadOrderKey ForActor(string order, Actor target) => new(order, CPos.Zero, target?.ActorID ?? 0);
		public static SquadOrderKey Plain(string order) => new(order, CPos.Zero, 0);
	}

	/// <summary>
	/// AR-S residual lane (2026-10-04): the per-tick identical-order guard. A re-issued order
	/// identical to the in-flight one cancels the activity and restarts the path — the
	/// move-one-tile, stand, move-one-tile march the maintainer reported. The rule keeps
	/// micro-management (the maintainer explicitly allows per-second re-tasking while a unit
	/// is in motion or attacking): suppression applies ONLY when the queued order is the same
	/// (order, quantized target) the member already carries.
	///
	/// Terminal orders (Stop, Scatter, ReturnToBase — the ones whose completion leaves the
	/// member idle) are suppressed whenever the same order was already issued: re-sending
	/// them does nothing the first order did not. Mobile orders (Move, AttackMove, Attack)
	/// additionally re-issue when the member is idle — its earlier order completed (arrived,
	/// path exhausted), so the repeat is a genuine retry, not a cancellation.
	/// </summary>
	public static class SquadOrderDedup
	{
		/// <summary>The pure rule. All branches pinned by SquadOrderDedupTest.</summary>
		public static bool ShouldIssue(bool armed, bool hasPrev, bool sameKey, bool idle, bool terminal)
		{
			// Unarmed: the pre-change order stream, bit-identical.
			if (!armed)
				return true;

			// First order for this member, or a real change of intent.
			if (!hasPrev || !sameKey)
				return true;

			// Same key, member idle, mobile order: the earlier order completed — retry is real work.
			if (idle && !terminal)
				return true;

			// Same order already in flight (or a terminal order already given): suppress.
			return false;
		}

		/// <summary>
		/// Records and answers whether <paramref name="member"/>'s queued order is a change.
		/// Callers filter their member lists through this before building the order, so a
		/// grouped order only carries members whose (order, target) actually moved.
		/// </summary>
		public static bool Changed(Dictionary<Actor, SquadOrderKey> memory, bool armed, Actor member, SquadOrderKey key, bool terminal)
		{
			var hasPrev = memory.TryGetValue(member, out var prev);
			var idle = member.IsIdle;
			var same = hasPrev && prev.Equals(key);

			if (!ShouldIssue(armed, hasPrev, same, idle, terminal))
				return false;

			if (armed)
				memory[member] = key;

			return true;
		}

		/// <summary>
		/// The member set a grouped order carries, or null when the order is suppressed.
		/// Unarmed always returns the full member array — even an empty one — because the
		/// pre-change stream issued the grouped order unconditionally (EMBER gating fix:
		/// "off" must be byte-identical, no empty-skip and no member filtering). Callers
		/// queue when this returns non-null.
		/// </summary>
		public static Actor[] EmitSet(Dictionary<Actor, SquadOrderKey> memory, bool armed, IEnumerable<Actor> members, SquadOrderKey key, bool terminal)
		{
			if (!armed)
				return members.ToArray();

			var changed = members.Where(a => Changed(memory, true, a, key, terminal)).ToArray();
			return changed.Length > 0 ? changed : null;
		}
	}
}
