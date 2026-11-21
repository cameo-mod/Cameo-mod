#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenRA.Mods.Cameo.Traits
{
	/// <summary>
	/// DEV-only (used only under CAMEO_DEV_LOGGING): defend trigger -> reaction records (STAGGER_EVAL_PLAN_04 F-B). A trigger is
	/// stamped at the event source (the victim's damage notification, before any bot gating); the reaction is the first order
	/// queued afterwards by a squad or defence module; a trigger with no reaction after <see cref="TimeoutTicks"/> is closed as
	/// reaction_kind=none. At most one trigger per kind is open at a time (hits while open only count), so the record volume is
	/// bounded. Pure logic: no world, no RNG, no orders.
	/// </summary>
	public sealed class DefendReactionTracker
	{
		public const int TimeoutTicks = 300;

		public const string BuildingAttacked = "building_attacked";
		public const string UnitAttacked = "unit_attacked";
		public const string HarvesterAttacked = "harvester_attacked";

		sealed class Pending
		{
			public int Tick;
			public string Kind;
			public string Id;
			public int X, Y;
			public int Hits = 1;
		}

		readonly string seat;
		readonly Action<string> sink;
		readonly List<Pending> open = new();

		public DefendReactionTracker(string seat, Action<string> sink)
		{
			this.seat = seat;
			this.sink = sink;
		}

		public int OpenCount => open.Count;

		/// <summary>A damage notification on an own actor. Returns the trigger id when this opened a new trigger, else null.</summary>
		public string Trigger(int tick, string kind, uint victimId, int x, int y)
		{
			Expire(tick);
			foreach (var p in open)
				if (p.Kind == kind)
				{
					p.Hits++;
					return null;
				}

			var id = victimId.ToString(CultureInfo.InvariantCulture) + "@" + tick.ToString(CultureInfo.InvariantCulture);
			open.Add(new Pending { Tick = tick, Kind = kind, Id = id, X = x, Y = y });
			return id;
		}

		/// <summary>An order was queued by a module of type <paramref name="issuerType"/>; closes every open trigger with it.</summary>
		public void Reaction(int tick, string issuerType)
		{
			Expire(tick);
			var kind = ReactionKindOf(issuerType);
			if (kind == null || open.Count == 0)
				return;

			foreach (var p in open)
				Write(p, tick, kind, issuerType);

			open.Clear();
		}

		/// <summary>Closes the triggers whose timeout has passed as reaction_kind=none: the bot had the full timeout and did nothing.</summary>
		public void Expire(int tick)
		{
			for (var i = open.Count - 1; i >= 0; i--)
			{
				var p = open[i];
				if (tick - p.Tick < TimeoutTicks)
					continue;

				Write(p, -1, "none", "");
				open.RemoveAt(i);
			}
		}

		/// <summary>
		/// Game end: closes every still-open trigger as reaction_kind=censored with censored_at=<paramref name="tick"/>. The game
		/// ended before the timeout, so the outcome is unknown: the analysis must not count it as "no reaction" (none), only as
		/// "no reaction in censored_at - trigger_tick ticks". Triggers already past the timeout are closed as none first.
		/// </summary>
		public void Flush(int tick)
		{
			Expire(tick);
			for (var i = 0; i < open.Count; i++)
				Write(open[i], -1, "censored", "", tick);

			open.Clear();
		}

		/// <summary>The reaction a module type stands for; null when its orders are not a defend reaction.</summary>
		public static string ReactionKindOf(string issuerType)
		{
			if (string.IsNullOrEmpty(issuerType))
				return null;

			if (issuerType.Contains("Squad", StringComparison.Ordinal) || issuerType.Contains("Protect", StringComparison.Ordinal))
				return "protection_squad";
			if (issuerType.Contains("HomeDef", StringComparison.Ordinal) || issuerType.Contains("BaseDefen", StringComparison.Ordinal)
				|| issuerType.Contains("Rally", StringComparison.Ordinal))
				return "rally_base";
			if (issuerType.Contains("UnitBuilder", StringComparison.Ordinal) || issuerType.Contains("CounterProduction", StringComparison.Ordinal))
				return "production_switch";
			return null;
		}

		void Write(Pending p, int reactionTick, string reactionKind, string issuer, int censoredAt = -1)
		{
			sink(string.Format(CultureInfo.InvariantCulture,
				"DefendReaction seat={0} trigger_tick={1} trigger_kind={2} trigger_id={3} x={4} y={5} hits={6} reaction_tick={7} reaction_kind={8} reaction_issuer={9} timeout={10}{11}",
				seat, p.Tick, p.Kind, p.Id, p.X, p.Y, p.Hits, reactionTick, reactionKind, string.IsNullOrEmpty(issuer) ? "-" : issuer, TimeoutTicks,
				censoredAt >= 0 ? " censored_at=" + censoredAt.ToString(CultureInfo.InvariantCulture) : ""));
		}
	}
}
