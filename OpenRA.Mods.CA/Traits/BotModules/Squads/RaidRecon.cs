#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum RaidPhase
	{
		Travel,
		Strike,
		LostRecon,
		Retreat,
		Released,
	}

	public enum RaidEvent
	{
		/// <summary>The target is visible and the package is in range.</summary>
		InStrikeRange,

		/// <summary>The target is no longer visible.</summary>
		TargetLost,

		/// <summary>The lost target was seen again.</summary>
		TargetReacquired,

		/// <summary>The lost-target recon budget (raid.lost_recon) is used up.</summary>
		ReconBudgetUsed,

		/// <summary>A risk incident: the squad lost the learned share of its value inside the window, or the next waypoint fails the risk gate with the package value.</summary>
		Incident,

		/// <summary>The retreat reached its explored threat-free cell.</summary>
		RetreatDone,

		// The release events: each ends the raid at once from any phase.
		AssignmentLost,
		JointOff,
		ValueBelowMin,
		AllInRecall,
	}

	/// <summary>
	/// The raid's small recon-safety state machine (design 01 D4, delta 02 section 3): four working phases and the terminal Released, driven by nine events. Not a port of Fransbot's 633-line class: the risk memory is the existing threat
	/// providers and the scout memory, and a released raid goes back to the normal harass states.
	/// </summary>
	public static class RaidRecon
	{
		public static bool IsRelease(RaidEvent ev) => ev is RaidEvent.AssignmentLost or RaidEvent.JointOff or RaidEvent.ValueBelowMin or RaidEvent.AllInRecall;

		/// <summary>The next phase. Released is terminal; a release event ends the raid from any phase; an event that does not apply to the phase changes nothing.</summary>
		public static RaidPhase Next(RaidPhase phase, RaidEvent ev)
		{
			if (phase == RaidPhase.Released || IsRelease(ev))
				return RaidPhase.Released;

			switch (ev)
			{
				case RaidEvent.InStrikeRange:
					return phase == RaidPhase.Travel ? RaidPhase.Strike : phase;
				case RaidEvent.TargetLost:
					return phase is RaidPhase.Travel or RaidPhase.Strike ? RaidPhase.LostRecon : phase;
				case RaidEvent.TargetReacquired:
					return phase == RaidPhase.LostRecon ? RaidPhase.Strike : phase;
				case RaidEvent.ReconBudgetUsed:
					return phase == RaidPhase.LostRecon ? RaidPhase.Released : phase;
				case RaidEvent.Incident:
					return phase is RaidPhase.Travel or RaidPhase.Strike or RaidPhase.LostRecon ? RaidPhase.Retreat : phase;
				case RaidEvent.RetreatDone:
					return phase == RaidPhase.Retreat ? RaidPhase.Released : phase;
				default:
					return phase;
			}
		}

		/// <summary>The incident trigger: the squad's value fell by at least <paramref name="lossPercent"/> percent of its value at the window start. No start value = no trigger.</summary>
		public static bool IncidentTriggered(int valueAtWindowStart, int valueNow, int lossPercent) =>
			valueAtWindowStart > 0 && ((long)(valueAtWindowStart - valueNow) * 100) >= (long)valueAtWindowStart * Math.Max(0, lossPercent);

		/// <summary>An incident is active from its tick until the decay (inclusive of the start, exclusive of the end).</summary>
		public static bool IncidentActive(int incidentTick, int now, int decayTicks) => now >= incidentTick && now - incidentTick < Math.Max(0, decayTicks);
	}

	/// <summary>
	/// One raid squad's queued intent: the target, the waypoint index and a sequence number. A stale callback or a later pass can issue a raid order only while its sequence is current; <see cref="Release"/> bumps the sequence FIRST, then clears
	/// the queued orders, and only then lets the squad return to its normal state (delta 02 section 3).
	/// </summary>
	public sealed class RaidIntent
	{
		public RaidIntent(uint targetId, int seq)
		{
			TargetId = targetId;
			Seq = seq;
		}

		public uint TargetId { get; }
		public int Seq { get; private set; }
		public int WaypointIndex { get; set; }
		public bool Valid { get; private set; } = true;

		/// <summary>True while the intent has not been released and the sequence is the caller's.</summary>
		public bool IsCurrent(int seq) => Valid && seq == Seq;

		/// <summary>Invalidate (sequence bump), then <paramref name="stopQueuedOrders"/> (the non-queued Stop of the squad's units), then <paramref name="returnToNormalState"/>. A second release does nothing.</summary>
		public void Release(Action stopQueuedOrders, Action returnToNormalState)
		{
			if (!Valid)
				return;

			Valid = false;
			Seq++;
			stopQueuedOrders?.Invoke();
			returnToNormalState?.Invoke();
		}
	}

	/// <summary>The raid's own incident memory (cells with a tick): the module reads it as an extra threat at those cells until the learned decay.</summary>
	public sealed class RaidIncidents
	{
		readonly List<(CPos Cell, int Tick)> list = new();

		public int Count => list.Count;

		public void Add(CPos cell, int tick) => list.Add((cell, tick));

		/// <summary>Drops every incident whose decay has passed.</summary>
		public void Prune(int now, int decayTicks) => list.RemoveAll(i => !RaidRecon.IncidentActive(i.Tick, now, decayTicks));

		/// <summary>True when an active incident lies within <paramref name="radiusCells"/> (Chebyshev) of the cell.</summary>
		public bool Near(CPos cell, int now, int decayTicks, int radiusCells)
		{
			foreach (var (c, t) in list)
				if (RaidRecon.IncidentActive(t, now, decayTicks) && Math.Max(Math.Abs(c.X - cell.X), Math.Abs(c.Y - cell.Y)) <= radiusCells)
					return true;

			return false;
		}
	}

	/// <summary>What value the learner reads for the style axis of a raid key (delta 02 section 2): the MAIN TARGET OWNER's qualified observed-style estimate only, else the exact three-axis result V3. No other enemy's style is an input.</summary>
	public enum RaidStyleAxis
	{
		/// <summary>The target owner's style is unseen or unqualified: the value is V3 exactly.</summary>
		ThreeAxis,

		/// <summary>The target owner's style is qualified and has an entry: the value is V4 with that entry.</summary>
		FourAxis,

		/// <summary>First sighting of a qualified style: the entry is created at V3, so V4 == V3 at that moment.</summary>
		SeedAtThreeAxisThenFourAxis,
	}

	public static class RaidStyle
	{
		/// <summary>The selector of the style axis. Its signature carries no other enemy's style, so a known style of another enemy can never change the result.</summary>
		public static RaidStyleAxis Choose(bool targetOwnerStyleQualified, bool entryExists)
		{
			if (!targetOwnerStyleQualified)
				return RaidStyleAxis.ThreeAxis;

			return entryExists ? RaidStyleAxis.FourAxis : RaidStyleAxis.SeedAtThreeAxisThenFourAxis;
		}
	}
}
