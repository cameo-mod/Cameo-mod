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
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.Cameo.Traits
{
	// QW-B (STAGGER_DESIGN_06 section 15): the shared-phase rule. A bot's phase for a timer of period K is
	// floor(rank * K / N_fam), where rank is its place among the activated Cameo-family bots ordered by
	// (SpawnKey, PlayerIndex) and N_fam is their count. The roster is built once, at the first bot tick (every bot
	// activates in Player construction, before tick 1), and is fixed for the game: a defeat leaves its phase unused,
	// and nothing re-packs. Host only: bots do not run on other clients. Inputs are synced integers only
	// (spawn point, player index); no clock, no random draw, no hash-ordered collection.
	public static class BotSlotPhase
	{
		// A seat without a spawn point sorts after every seat with one (the same key the round-robin scheduler uses).
		public const int NoSpawnKey = int.MaxValue;

		/// <summary>floor(rank * period / count). Rank 0, a single seat, or a period of 1 or less gives 0, so with
		/// one bot every site keeps its absolute-modulo phase (N = 1 identity).</summary>
		public static int Phase(int rank, int count, int period)
		{
			if (period <= 1 || count <= 1 || rank <= 0)
				return 0;

			return (int)((long)Math.Min(rank, count - 1) * period / count);
		}

		/// <summary>Rank of each seat when ordered by (SpawnKey, PlayerIndex); the result is indexed like the input.</summary>
		public static int[] Ranks(IReadOnlyList<(int SpawnKey, int PlayerIndex)> seats)
		{
			var order = Enumerable.Range(0, seats.Count)
				.OrderBy(i => seats[i].SpawnKey)
				.ThenBy(i => seats[i].PlayerIndex)
				.ToArray();

			var ranks = new int[seats.Count];
			for (var rank = 0; rank < order.Length; rank++)
				ranks[order[rank]] = rank;

			return ranks;
		}

		// The family seats in rank order. Built once per world.
		sealed class Roster
		{
			public readonly OpenRA.Player[] Seats;
			public Roster(OpenRA.Player[] seats) { Seats = seats; }
		}

		static readonly ConditionalWeakTable<World, Roster> Rosters = new();

		/// <summary>Builds the family roster for this world if it is not built yet. Called only from a phase-offsetting bot's
		/// tick, so every bot has activated. An empty roster (no activated family bot) is not kept.</summary>
		public static void Prepare(World world)
		{
			if (Rosters.TryGetValue(world, out _))
				return;

			var seats = BuildSeats(world);
			if (seats.Length == 0)
				return;

			Rosters.Add(world, new Roster(seats));
		}

		// The family seats in rank order; empty when no activated family bot exists.
		static OpenRA.Player[] BuildSeats(World world)
		{
			var members = new List<(OpenRA.Player Player, int SpawnKey, int PlayerIndex)>();
			for (var i = 0; i < world.Players.Length; i++)
			{
				var p = world.Players[i];
				if (p.PlayerActor.TraitsImplementing<ModularBot>().Any(b => b.IsEnabled && b.InRoundRobinFamily))
					members.Add((p, p.SpawnPoint > 0 ? p.SpawnPoint : NoSpawnKey, i));
			}

			var ranks = Ranks(members.Select(m => (m.SpawnKey, m.PlayerIndex)).ToArray());
			var seats = new OpenRA.Player[members.Count];
			for (var i = 0; i < members.Count; i++)
				seats[ranks[i]] = members[i].Player;

			return seats;
		}

		/// <summary>DEV telemetry, read only: the rank of this player among the family seats and the family size. Uses the built
		/// roster when there is one, otherwise the roster Prepare would build now; it never builds or changes the roster, so it
		/// cannot affect a decision. Rank is -1 for a player outside the family.</summary>
		public static void Peek(World world, OpenRA.Player player, out int rank, out int count)
		{
			var seats = Rosters.TryGetValue(world, out var roster) ? roster.Seats : BuildSeats(world);
			rank = Array.IndexOf(seats, player);
			count = seats.Length;
		}

		/// <summary>The phase of a seat, or false when the site must keep its legacy schedule: a roster of one seat (N = 1
		/// identity: every site behaves exactly as today, including the sites whose legacy phase is not 0), a seat outside the
		/// roster (rank below 0: a bot activated after the first tick), or a period of 1 or less.</summary>
		public static bool TryPhase(int rank, int count, int period, out int phase)
		{
			phase = 0;
			if (period <= 1 || count <= 1 || rank < 0)
				return false;

			phase = Phase(rank, count, period);
			return true;
		}

		/// <summary>The phase of this player's bot for a timer of the given period, in [0, period). False (the site keeps its
		/// legacy schedule) until the roster is built (the first bot tick: a module that seeds earlier, from a trait-enabled
		/// callback at world load, would see a partial roster), for a roster of one, and for a player outside the roster.</summary>
		public static bool TryOf(OpenRA.Player player, int period, out int phase)
		{
			phase = 0;
			if (!Rosters.TryGetValue(player.World, out var roster))
				return false;

			return TryPhase(Array.IndexOf(roster.Seats, player), roster.Seats.Length, period, out phase);
		}

		/// <summary>N_fam for this world once the roster is built, else 0 (test and telemetry).</summary>
		public static int FamilyCount(World world) => Rosters.TryGetValue(world, out var roster) ? roster.Seats.Length : 0;
	}
}
