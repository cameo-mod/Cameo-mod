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
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 Q7 (row sq_pincer_push under the master switch sq_pipeline_02; design 4.1 Step 2): the pincer is decided at PUSH time on the blob that has ARRIVED at the
	// rally, not at issue on the pool (that plan counted units that never came: three seats, one squad each, a 2:1 share, one arm below the bar, the pincer collapsed or never
	// planned). Row off = TEAM-PINCER-01 as before (the plan at issue). The wiring below the leader's push test:
	//   1. the arrived blob holds two arm bars, the target is far enough, a legal pair of staging points at 90-180 degrees exists (the fan-out search of PINCER-02);
	//   2. the squads that took part are shared out between the two arms (PincerArms): whole squads, then ONE squad divided, so both arms hold the arm bar;
	//   3. the order becomes a pincer in place (BeginPincer: its gather window restarts), each squad walks to its arm and the existing sync rules push: both arms ready, or the
	//      first ready arm waited the hold (PincerHoldCapTicks), one push tick, one target; an arm that fell below the arm bar collapses it into the plain blob push.
	// Any gate that fails leaves the order as the single blob it is, and it pushes as before.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqPincerPush;

		/// <summary>Q7 (row sq_pincer_push): the pincer is decided at push time. Needs the pincer switch (TeamLeadPincerEnabled) as the row it refines.</summary>
		internal bool ArmyPincerPushOn => ArmyPipelineOn && TeamLeadPincerOn && !ablateSqPincerPush.Off(Player, AblationIds.SqPincerPush);

			AblationLatch ablatePincerArrivedArms; // fix-03

			/// <summary>fix-03 (row pincer_arrived_arms): the arm plan is built from the squads' arrived units and value. Off = the committed totals, as before.</summary>
			internal bool PincerArrivedArmsOn => Info.PincerArrivedArms && !ablatePincerArrivedArms.Off(Player, AblationIds.PincerArrivedArms);

		/// <summary>
		/// The leader's push test passed (due, the push bar passed): make the order a pincer instead of pushing, when the arrived blob can field two arms. True = the order is a
		/// pincer now and does not push this pass; false = nothing changed, the caller pushes the single blob.
		/// </summary>
		bool TeamLeadPincerAtPush(TeamLeadOrder order, int now)
		{
			if (order.HasProngs || order.GatherOnly || order.Cancelled)
				return false;

			var (units, value) = order.ArrivedTotals();
			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
			var armPercent = Info.TeamLeadPincerArmPercent;
			if (!PincerWideCA.PincerBlob(units, value, Info.TeamLeadJoinMinUnits, WaveMinCA.Limits(minUnits, minValue), minUnits, minValue, armPercent))
				return false;

			if (!TeamLeadCA.PincerTargetFar(order.Rally, order.Target, Info.TeamLeadPincerMinTargetCells))
				return false;

			// The squads that took part, as the arm plan reads them: the key is the actor id of the squad's first unit (a squad owns its units, so it is unique).
			var squads = new List<ArmSquad>();
			var byKey = new Dictionary<uint, (object Squad, long Value)>();
			foreach (var (squad, squadUnits, squadValue) in PincerArrivedArmsOn ? order.ArrivedReported() : order.Reported())
			{
				if (squad is not SquadCA q || !q.IsValid)
					continue;

				var key = q.Units[0].Actor.ActorID;
				if (byKey.TryAdd(key, (squad, squadValue)))
					squads.Add(new ArmSquad(key, squadUnits, squadValue));
			}

			var floorUnits = PincerArms.FloorUnits(PincerWideCA.ArmBar(minUnits, armPercent), Info.TeamLeadJoinMinUnits);
			var plan = PincerArms.Balance(squads, floorUnits, PincerWideCA.ArmBar(minValue, armPercent));
			if (plan == null)
			{
				DiagnosticTeamEvent("pincer_skipped", "seq=" + order.Seq + " why=arms arrived=" + units + "/" + value + " squads=" + squads.Count + " bar=" + floorUnits + "/" + PincerWideCA.ArmBar(minValue, armPercent));
				return false;
			}

			if (!TeamLeadPincerStagingAtPush(order, out var rally0, out var rally1))
			{
				DiagnosticTeamEvent("pincer_skipped", "seq=" + order.Seq + " why=staging arrived=" + units + "/" + value);
				return false;
			}

			order.BeginPincer(now, rally0, rally1);
			foreach (var place in plan.Places)
			{
				// The squad the plan divides holds only a PROVISIONAL arm here: the units that really change arm are logged by TeamLeadSplitOff (event split, final). Every other squad's arm is final.
				order.AssignProng(byKey[place.Key].Squad, place.Arm, byKey[place.Key].Value);
				LedgerClaim(ArmyDuty.PincerArm, nameof(TeamLeadPincerAtPush), "arm" + place.Arm + (plan.Split is { } pending && pending.Key == place.Key ? "_provisional_split_pending" : "_final") + "_seq" + order.Seq,
					(byKey[place.Key].Squad as SquadCA)?.Units ?? new List<UnitWposWrapper>());
			}

			if (plan.Split is { } split)
				order.RequestSplit(byKey[split.Key].Squad, split.Units, split.ToArm);

			TeamLeadPincerRecord("pincer_planned", order, "at=push arrived=" + units + "/" + value + " arms=" + plan.Units0 + "/" + plan.Value0 + "," + plan.Units1 + "/" + plan.Value1
				+ " split=" + (plan.Split?.Units ?? 0));
			#if CAMEO_DEV_LOGGING
			AIUtils.BotDebug("AI ({0}): pincer at push seq {1} at {2}: arrived {3}/{4}, arms {5}/{6}", Player.ClientIndex, order.Seq, now, units, value, plan.Units0, plan.Units1);
#endif
			return true;
		}

		// The first legal PAIR of the fanned-out staging points (PINCER-02), measured from where the blob stands: terrain and the pathfinder only, no enemy actor is read.
		// False = no ground unit to ask the terrain for, no legal pair, or two points too close together.
		bool TeamLeadPincerStagingAtPush(TeamLeadOrder order, out WPos rally0, out WPos rally1)
		{
			rally0 = rally1 = default;
			var sample = TeamLeadPincerPushSample(order);
			var mobile = sample?.TraitOrDefault<Mobile>();
			if (mobile == null)
				return false;

			var map = World.Map;
			var pathFinder = World.WorldActor.Trait<IPathFinder>();
			var home = sample.Location;
			bool Legal(CPos c) => map.Contains(c) && mobile.Locomotor.MovementCostForCell(c) != PathGraph.MovementCostForUnreachableCell;

			// A staging point outside the scan radius: a gathering squad fights what it sees there and leaves the order.
			var standOff = System.Math.Max(Info.TeamLeadPincerStandOffCells, Info.AttackScanRadius + 4);
			var listA = PincerWideCA.Candidates(order.Rally, order.Target, -1, standOff, Info.TeamLeadPincerSpreadDegrees);
			var listB = PincerWideCA.Candidates(order.Rally, order.Target, 1, standOff, Info.TeamLeadPincerSpreadDegrees);
			WPos? Resolve(List<WPos> list, int i)
			{
				var cell = mobile.NearestCell(map.CellContaining(list[i]), Legal, 1, 3);
				return Legal(cell) && pathFinder.PathExistsForLocomotor(mobile.Locomotor, home, cell) ? map.CenterOfCell(cell) : null;
			}

			if (!PincerWideCA.PickPair(order.Target, listA.Count, i => Resolve(listA, i), listB.Count, i => Resolve(listB, i), Info.TeamLeadPincerMinAngleDegrees,
				Info.TeamLeadPincerMinSeparationCells, out var pointA, out var pointB, out _))
				return false;

			rally0 = map.CenterOfCell(map.CellContaining(pointA));
			rally1 = map.CenterOfCell(map.CellContaining(pointB));
			return TeamLeadCA.PincerSeparated(rally0, rally1, Info.TeamLeadPincerMinSeparationCells);
		}

		// A ground unit that stands with the blob (the first reported squad, by key, that has one): the locomotor the staging points are checked for. A squad that gathers is not
		// a quiet wave squad, so the issue-time sample (the pool, then quiet squads) would find nothing here.
		Actor TeamLeadPincerPushSample(TeamLeadOrder order)
		{
			Actor best = null;
			foreach (var (squad, _, _) in order.Reported())
			{
				if (squad is not SquadCA q || !q.IsValid)
					continue;

				foreach (var u in q.Units)
				{
					if (unitCannotBeOrdered(u.Actor) || u.Actor.TraitOrDefault<Mobile>() == null)
						continue;

					if (best == null || u.Actor.ActorID < best.ActorID)
						best = u.Actor;

					break;
				}
			}

			return best ?? TeamLeadPincerSample();
		}

		/// <summary>
		/// The order became a pincer while this squad gathered: when the arm plan divides it, the units that belong to the other arm leave it for a new squad of this seat, which gathers
		/// at that arm (the same state, the same order). The squad itself then walks to its own arm. Runs once, in the squad's own state tick: a seat divides only its own squads.
		/// </summary>
		internal void TeamLeadSplitOff(SquadCA squad, TeamLeadOrder order)
		{
			if (!order.TakeSplit(squad, out var count, out var toProng) || count <= 0 || squad.Units.Count - count < 1)
				return;

			// The highest actor ids go: a total order, no choice to make.
			var leaving = squad.Units.OrderByDescending(u => u.Actor.ActorID).Take(count).ToList();
			var half = new SquadCA(squad.Bot, this, squad.Type, null);
			half.PriorityTags = PriorityTagsFor(squad.Type);
			half.FormedTick = World.WorldTick;

			// The same wave as its parent for the liveness accounting (M13), not a new launch: no launch count, no new wave id.
			half.M13WaveId = squad.M13WaveId;
			foreach (var u in leaving)
			{
				squad.Units.Remove(u);
				half.Units.Add(u);
			}

			Squads.Add(half);
			order.AssignProng(half, toProng, SquadValueOf(half));
			LedgerSplit(nameof(TeamLeadSplitOff), order.Seq, toProng, half.Units, squad.Units);
			DiagnosticSquadEvent(half, "formed", "pincer_split seq=" + order.Seq + " units=" + count + " prong=" + toProng);
			half.FuzzyStateMachine.ChangeState(half, new TeamLeadGatherStateCA(order), false);
		}
	}
}
