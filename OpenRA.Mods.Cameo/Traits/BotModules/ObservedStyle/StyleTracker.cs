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

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE (DESIGN_OBSERVED_STYLE_02 section 1, "Tracker"): the history the fog memory does not keep. One tracker per (viewer, enemy) lives inside the
	/// style module; it needs no change to the fog memory. Input per update: the allowlisted snapshot of the enemy, the viewer's own yard cells and the tick.
	/// It records first-seen ticks (first combat unit outside its base, 2nd yard, first production, first tech-only building), the army value at the 3/6/10/15 minute
	/// checkpoints, attack-group events (distinct groups of combat units entering the contact radius of the viewer's own base) and "seen then gone without contact".
	/// "Gone" means an entry left the table; the tracker does not claim death versus hiding. Pure: no clock, no random draw, no world access; sorted-key iteration where order matters.
	/// </summary>
	internal sealed class StyleTracker
	{
		public const int MinuteTicks = 1500;

		/// <summary>The F2 checkpoints, in ticks (minutes 3, 6, 10, 15).</summary>
		public static readonly int[] CheckpointTicks = { 3 * MinuteTicks, 6 * MinuteTicks, 10 * MinuteTicks, 15 * MinuteTicks };

		/// <summary>A combat unit farther than this (cells, Chebyshev) from EVERY remembered enemy building is outside its base.</summary>
		public const int BaseRadiusCells = 20;

		/// <summary>A combat unit within this (cells) of one of the viewer's own yards is in contact range of the viewer's base.</summary>
		public const int ContactRadiusCells = 30;

		/// <summary>Units that enter contact range in the same update and lie within this (cells) of the group's first member form one group.</summary>
		public const int GroupLinkCells = 8;

		/// <summary>The F8 window: groups entering per this many ticks.</summary>
		public const int GroupWindowTicks = 3000;

		const int MaxEvents = 64;

		struct Seen
		{
			public int FirstSeen;
			public int LastPresent;
			public bool Combat;
			public bool Contact;
		}

		readonly struct GroupEvent
		{
			public readonly int Tick;
			public readonly int Size;

			public GroupEvent(int tick, int size)
			{
				Tick = tick;
				Size = size;
			}
		}

		readonly Dictionary<uint, Seen> seen = new(128);
		readonly List<GroupEvent> events = new();
		readonly List<CPos> groupAnchors = new(128);
		readonly List<int> groupSizes = new(128);
		readonly List<CPos> buildingCells = new();
		readonly List<(uint Id, CPos Cell)> entered = new();
		readonly List<uint> gone = new();
		readonly List<int> window = new();

		/// <summary>First update that carried any actor; -1 before.</summary>
		public int FirstSeenTick { get; private set; } = -1;

		/// <summary>Tick and value of the first combat unit seen outside the enemy's base; -1 until one is seen.</summary>
		public int FirstOutsideTick { get; private set; } = -1;
		public int FirstOutsideValue { get; private set; } = -1;

		public int FirstProductionTick { get; private set; } = -1;

		/// <summary>First building that is only tech (not a yard, production, refinery or defence); -1 until seen.</summary>
		public int FirstTechTick { get; private set; } = -1;

		public int SecondYardTick { get; private set; } = -1;

		/// <summary>Army value (sum of Value over combat units) at the first update at or after each checkpoint; -1 until then.</summary>
		public readonly int[] ArmyAtCheckpoint = { -1, -1, -1, -1 };

		/// <summary>Combat units that left the table without ever having been in contact range of the viewer's base.</summary>
		public int SeenThenGone { get; private set; }

		public int LastUpdateTick { get; private set; } = -1;

		/// <summary>True once any update carried at least one own yard cell, i.e. contact with the viewer's base is measurable (F8 is unknown before).</summary>
		public bool ContactKnown { get; private set; }

		/// <summary>Number of group events in the window ending at <paramref name="tick"/>, and the median and maximum group size over them (0 when none).</summary>
		public void Groups(int tick, out int count, out int median, out int max)
		{
			window.Clear();
			for (var i = 0; i < events.Count; i++)
				if (tick - events[i].Tick <= GroupWindowTicks)
					window.Add(events[i].Size);

			count = window.Count;
			max = 0;
			median = 0;
			if (count == 0)
				return;

			window.Sort();
			median = window[count / 2];
			max = window[count - 1];
		}

		public void Update(ReadOnlySpan<FogStyleActor> actors, ReadOnlySpan<CPos> ownYards, int tick)
		{
			LastUpdateTick = tick;
			if (ownYards.Length > 0)
				ContactKnown = true;

			if (actors.Length > 0 && FirstSeenTick < 0)
				FirstSeenTick = tick;

			buildingCells.Clear();
			var yards = 0;
			var armyValue = 0;
			for (var i = 0; i < actors.Length; i++)
			{
				ref readonly var a = ref actors[i];
				if (a.Building)
				{
					buildingCells.Add(a.Cell);
					if (a.BaseBuilding)
						yards++;

					if (a.Production && FirstProductionTick < 0)
						FirstProductionTick = tick;

					if (a.Tech && !a.BaseBuilding && !a.Production && !a.Refinery && !a.Defence && FirstTechTick < 0)
						FirstTechTick = tick;
				}
				else if (a.Combat)
				{
					armyValue += a.Value;
				}
			}

			if (yards >= 2 && SecondYardTick < 0)
				SecondYardTick = tick;

			if (actors.Length > 0)
				for (var c = 0; c < CheckpointTicks.Length; c++)
					if (ArmyAtCheckpoint[c] < 0 && tick >= CheckpointTicks[c])
						ArmyAtCheckpoint[c] = armyValue;

			entered.Clear();
			for (var i = 0; i < actors.Length; i++)
			{
				ref readonly var a = ref actors[i];
				if (a.Building || !a.Combat)
					continue;

				if (!seen.TryGetValue(a.ActorId, out var s))
					s = new Seen { FirstSeen = tick, Combat = true };

				s.LastPresent = tick;
				s.Combat = true;

				if (FirstOutsideTick < 0 && buildingCells.Count > 0 && OutsideBase(a.Cell))
				{
					FirstOutsideTick = tick;
					FirstOutsideValue = a.Value;
				}

				var contact = ownYards.Length > 0 && InContact(a.Cell, ownYards);
				if (contact && !s.Contact)
					entered.Add((a.ActorId, a.Cell));

				s.Contact = contact;
				seen[a.ActorId] = s;
			}

			Cluster(tick);

			// Entries that did not appear in this update have left the table.
			gone.Clear();
			foreach (var pair in seen)
				if (pair.Value.LastPresent != tick)
					gone.Add(pair.Key);

			gone.Sort();
			for (var i = 0; i < gone.Count; i++)
			{
				var s = seen[gone[i]];
				if (s.Combat && !s.Contact)
					SeenThenGone++;

				seen.Remove(gone[i]);
			}
		}

		bool OutsideBase(CPos cell)
		{
			for (var i = 0; i < buildingCells.Count; i++)
				if (Chebyshev(cell, buildingCells[i]) <= BaseRadiusCells)
					return false;

			return true;
		}

		static bool InContact(CPos cell, ReadOnlySpan<CPos> ownYards)
		{
			for (var i = 0; i < ownYards.Length; i++)
				if (Chebyshev(cell, ownYards[i]) <= ContactRadiusCells)
					return true;

			return false;
		}

		static int Chebyshev(CPos a, CPos b) => Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

		// Greedy, order-stable grouping of the units that entered contact range in this update: sorted by actor id, each unit joins the first
		// group whose first member lies within GroupLinkCells, else it starts a group. One event per group.
		void Cluster(int tick)
		{
			if (entered.Count == 0)
				return;

			entered.Sort((x, y) => x.Id.CompareTo(y.Id));
			var anchors = groupAnchors;
			anchors.Clear();
			var sizes = groupSizes;
			sizes.Clear();
			foreach (var e in entered)
			{
				var placed = false;
				for (var g = 0; g < anchors.Count && !placed; g++)
				{
					if (Chebyshev(e.Cell, anchors[g]) <= GroupLinkCells)
					{
						sizes[g]++;
						placed = true;
					}
				}

				if (!placed)
				{
					anchors.Add(e.Cell);
					sizes.Add(1);
				}
			}

			for (var g = 0; g < sizes.Count; g++)
			{
				events.Add(new GroupEvent(tick, sizes[g]));
				if (events.Count > MaxEvents)
					events.RemoveAt(0);
			}
		}
	}
}
