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
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// TEAM-HIVEMIND-01: the shared memory of one allied team, per world. A module keeps its team-wide state in a board (any class with a
	/// parameterless constructor) and every allied bot reads and writes the same instance through <see cref="Board{T}"/>. The brain itself
	/// holds no game logic and never scans the world: boards are fed by their own modules from what those bots observe, and allies share what
	/// they see, so a board never shows a reader more than its team knows. First board: <see cref="TeamLaneBoard"/> (DEF-LANES-01 P2).
	/// Later boards (claims, targets, build split) plug in the same way with no change here.
	/// </summary>
	public sealed class TeamBrain
	{
		static readonly ConditionalWeakTable<World, Dictionary<string, TeamBrain>> Brains = new();

		readonly Dictionary<Type, object> boards = new();

		public string Key { get; }

		TeamBrain(string key)
		{
			Key = key;
		}

		/// <summary>The key of a team: the lowest member name (ordinal). Every member of the same team gets the same key. Null for no members.</summary>
		public static string TeamKey(IEnumerable<string> memberNames)
		{
			string key = null;
			foreach (var name in memberNames)
				if (name != null && (key == null || string.CompareOrdinal(name, key) < 0))
					key = name;

			return key;
		}

		/// <summary>The team key of a player: itself and its allied combatants. Reads the player list only.</summary>
		public static string TeamKey(World world, OpenRA.Player player)
		{
			return TeamKey(world.Players.Where(p => p == player || (!p.NonCombatant && player.IsAlliedWith(p))).Select(p => p.InternalName));
		}

		public static TeamBrain For(World world, OpenRA.Player player)
		{
			return For(world, TeamKey(world, player));
		}

		public static TeamBrain For(World world, string teamKey)
		{
			var brains = Brains.GetValue(world, _ => new Dictionary<string, TeamBrain>(StringComparer.Ordinal));
			if (!brains.TryGetValue(teamKey, out var brain))
				brains[teamKey] = brain = new TeamBrain(teamKey);

			return brain;
		}

		/// <summary>The team's board of type <typeparamref name="T"/>, created on first use and shared by every member.</summary>
		public T Board<T>() where T : class, new()
		{
			if (boards.TryGetValue(typeof(T), out var existing))
				return (T)existing;

			var board = new T();
			boards[typeof(T)] = board;
			return board;
		}
	}

	/// <summary>
	/// DEF-LANES-01 P2 / TEAM-HIVEMIND-01: the lane threat of a whole team. Each member bot lists its own lane memory (fed only by its own damage
	/// events) with its home cell; allies read the list to find where the team is attacked most and hardest. World-free (tested). Members are kept
	/// in the ordinal order of their keys, never in join or enumeration order, so every read folds the team the same way.
	/// </summary>
	public sealed class TeamLaneBoard
	{
		public sealed class Member
		{
			public string Key;
			public DefenseLanes Lanes;
			public int HomeX, HomeY;
			public int DecayPermille, DecayIntervalTicks;
		}

		readonly List<Member> members = new();

		public IReadOnlyList<Member> Members => members;

		/// <summary>Lists a member's lane memory; a member that joins again replaces its earlier entry in place.</summary>
		public Member Join(string key, DefenseLanes lanes, int homeX, int homeY, int decayPermille, int decayIntervalTicks)
		{
			var member = new Member
			{
				Key = key, Lanes = lanes, HomeX = homeX, HomeY = homeY, DecayPermille = decayPermille, DecayIntervalTicks = decayIntervalTicks
			};

			for (var i = 0; i < members.Count; i++)
				if (string.Equals(members[i].Key, key, StringComparison.Ordinal))
				{
					members[i] = member;
					return member;
				}

			var at = 0;
			while (at < members.Count && string.CompareOrdinal(members[at].Key, key) < 0)
				at++;

			members.Insert(at, member);
			return member;
		}

		public void Leave(string key)
		{
			members.RemoveAll(m => string.Equals(m.Key, key, StringComparison.Ordinal));
		}

		/// <summary>Applies each member's own decay up to <paramref name="tick"/>.</summary>
		public void Decay(int tick)
		{
			foreach (var m in members)
				m.Lanes.Decay(tick, m.DecayPermille, m.DecayIntervalTicks);
		}

		/// <summary>The heaviest lane of any member but <paramref name="selfKey"/> (the lowest key on a tie). False when no ally lane holds weight.</summary>
		public bool HeaviestAlly(string selfKey, out Member member, out int lane, out int weight)
		{
			member = null;
			lane = -1;
			weight = 0;
			foreach (var m in members)
			{
				if (string.Equals(m.Key, selfKey, StringComparison.Ordinal))
					continue;

				var h = m.Lanes.Heaviest();
				if (h >= 0 && m.Lanes.Weight(h) > weight)
				{
					weight = m.Lanes.Weight(h);
					lane = h;
					member = m;
				}
			}

			return member != null;
		}

		/// <summary>The largest summed lane weight of any member but <paramref name="selfKey"/>: the hardest-hit ally.</summary>
		public int MaxAllyTotal(string selfKey)
		{
			var max = 0;
			foreach (var m in members)
				if (!string.Equals(m.Key, selfKey, StringComparison.Ordinal))
					max = Math.Max(max, m.Lanes.Total());

			return max;
		}
	}
}
