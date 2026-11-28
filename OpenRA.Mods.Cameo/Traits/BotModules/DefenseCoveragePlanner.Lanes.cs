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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	// DEF-LANES-01 (user 11-28): the lane memory of the planner. P1 = own lanes (damage events only, no world scan). P2 = the team's lanes through
	// TeamBrain (TEAM-HIVEMIND-01): each bot weighs its own and its allies' lanes with the learned ally share. Off (LaneDefences false, the planner
	// off or disabled, or the row def_lanes) = no memory, no budget, no lane placement: every entry point returns before it touches any state.
	public partial class DefenseCoveragePlanner : IBotRespondToAttack, IBotDefenceLaneBudget, IGameSaveTraitData
	{
		AblationLatch ablateLanes;
		AblationLatch ablateLaneTeam;
		DefenseLanes lanes;
		readonly LaneSplit laneSplit = new();
		TeamLaneBoard laneBoard;
		string laneTeamKey;
		TeamLaneBoard.Member laneMember;
		bool skipAllyLane;

		// The learned knobs (def_lanes.*), re-read once per decay interval. No provider or no stored knob = the yaml prior.
		IBotLearnedKnobs laneKnobs;
		bool laneKnobsResolved;
		int laneKnobTick = int.MinValue;
		int laneBaseBump, laneGainAttack, laneGainStrength, laneDecay, laneShare, laneAllyShare;

		bool LanesArmed => Info.LaneDefences && Info.Enabled && !IsTraitDisabled && !ablateLanes.Off(player, AblationIds.DefLanes);

		// Call RefreshLaneKnobs first: the ally share is a learned knob (prior 0 = own lanes only).
		bool LaneTeamArmed => laneAllyShare > 0 && !ablateLaneTeam.Off(player, AblationIds.DefLanesTeam);

		void RefreshLaneKnobs()
		{
			var tick = world.WorldTick;
			if (laneKnobTick != int.MinValue && tick - laneKnobTick < Info.LaneDecayIntervalTicks)
				return;

			laneKnobTick = tick;
			if (!laneKnobsResolved)
			{
				laneKnobs = player.PlayerActor.TraitsImplementing<IBotLearnedKnobs>().FirstOrDefault();
				laneKnobsResolved = true;
			}

			// The bounds are the registry bounds (tools/ai/learnables.yaml, def_lanes.*).
			laneBaseBump = RefineryLearn.Resolve(laneKnobs, "def_lanes.base_bump", Info.LaneBaseBumpPermille, 0, 500);
			laneGainAttack = RefineryLearn.Resolve(laneKnobs, "def_lanes.gain_attack", Info.LaneGainPerAttack, 0, 300);
			laneGainStrength = RefineryLearn.Resolve(laneKnobs, "def_lanes.gain_strength", Info.LaneGainPerStrength, 0, 100);
			laneDecay = RefineryLearn.Resolve(laneKnobs, "def_lanes.decay", Info.LaneDecayPermille, 10, 500);
			laneShare = RefineryLearn.Resolve(laneKnobs, "def_lanes.split", Info.LaneSharePercent, 0, 100);
			laneAllyShare = RefineryLearn.Resolve(laneKnobs, "def_lanes.ally_share", Info.LaneAllySharePercent, 0, 100);
			if (laneMember != null)
				laneMember.DecayPermille = laneDecay;

			// The team is the ally set at decision time: after an alliance change the memory moves to the board of the new team.
			var teamKey = TeamBrain.TeamKey(world, player);
			if (laneBoard == null || !string.Equals(teamKey, laneTeamKey, StringComparison.Ordinal))
			{
				LeaveLaneBoard();
				laneTeamKey = teamKey;
				laneBoard = TeamBrain.For(world, teamKey).Board<TeamLaneBoard>();
				if (lanes != null)
					JoinLaneBoard();
			}
		}

		// Call RefreshLaneKnobs first. The only place a lane memory is created.
		void EnsureLanes()
		{
			if (lanes != null)
				return;

			lanes = new DefenseLanes(Info.LaneBuckets);
			JoinLaneBoard();
		}

		void JoinLaneBoard()
		{
			var home = player.HomeLocation;
			laneMember = laneBoard.Join(player.InternalName, lanes, home.X, home.Y, laneDecay, Info.LaneDecayIntervalTicks);
		}

		void IBotRespondToAttack.RespondToAttack(IBot bot, Actor self, AttackInfo e)
		{
			if (!Info.LaneDefences || !LanesArmed)
				return;

			if (self.Owner != player || !self.IsInWorld || e.Damage.Value <= 0 || !self.Info.HasTraitInfo<BuildingInfo>())
				return;

			var attacker = e.Attacker;
			if (attacker == null || attacker.Disposed || player.RelationshipWith(attacker.Owner) != PlayerRelationship.Enemy)
				return;

			// Observed only: the bearing of an attacker the bot can see, else the bearing of the building that was hit.
			var home = player.HomeLocation;
			var seen = attacker.IsInWorld && attacker.CanBeViewedByPlayer(player);
			var toAttacker = seen ? world.Map.CellContaining(attacker.CenterPosition) - home : CVec.Zero;
			var toBuilding = self.Location - home;
			var bucket = DefenseLanes.OriginBucket(seen, toAttacker.X, toAttacker.Y, toBuilding.X, toBuilding.Y, Info.LaneBuckets);
			if (bucket < 0)
				return;

			RefreshLaneKnobs();
			EnsureLanes();
			var tick = world.WorldTick;
			lanes.Decay(tick, laneDecay, Info.LaneDecayIntervalTicks);
			lanes.Hit(bucket, tick, e.Damage.Value, laneGainAttack, laneGainStrength, Info.LaneAttackWindowTicks, Info.LaneMaxExtraPermille);
		}

		int IBotDefenceLaneBudget.ExtraDefencePermille
		{
			get
			{
				if (!Info.LaneDefences || !LanesArmed)
					return 0;

				RefreshLaneKnobs();
				var tick = world.WorldTick;
				var ownTotal = 0;
				if (lanes != null)
				{
					lanes.Decay(tick, laneDecay, Info.LaneDecayIntervalTicks);
					ownTotal = lanes.Total();
				}

				var allyTotal = 0;
				if (LaneTeamArmed)
				{
					laneBoard.Decay(tick);
					allyTotal = laneBoard.MaxAllyTotal(player.InternalName);
				}

				laneSplit.Observe(ownTotal > 0 || allyTotal > 0);
				var own = DefenseLanes.ExtraPermille(ownTotal, laneBaseBump, Info.LaneMaxExtraPermille);
				if (allyTotal <= 0)
					return own;

				var ally = DefenseLanes.ExtraPermille(allyTotal, laneBaseBump, Info.LaneMaxExtraPermille);
				return DefenseLanes.TeamExtra(own, ally, laneAllyShare, Info.LaneMaxExtraPermille);
			}
		}

		/// <summary>
		/// Layer 2: the lane the next defence goes to, or -1 for a coverage placement. <paramref name="ally"/> is the team member whose base the
		/// lane belongs to (null = our own). <paramref name="hot"/> = some lane of ours or of an ally holds weight (an episode is running).
		/// </summary>
		int PickLane(out TeamLaneBoard.Member ally, out bool hot)
		{
			ally = null;
			hot = false;
			if (!Info.LaneDefences || !LanesArmed)
				return -1;

			RefreshLaneKnobs();
			var tick = world.WorldTick;
			var lane = -1;
			var ownWeight = 0;
			if (lanes != null)
			{
				lanes.Decay(tick, laneDecay, Info.LaneDecayIntervalTicks);
				lane = lanes.Heaviest();
				ownWeight = lane >= 0 ? lanes.Weight(lane) : 0;
			}

			// P2: an ally's lane that outweighs our own after the ally share takes the placement, at the ally's base.
			var allyHot = false;
			if (LaneTeamArmed)
			{
				laneBoard.Decay(tick);
				if (laneBoard.HeaviestAlly(player.InternalName, out var member, out var allyLane, out var allyWeight))
				{
					allyHot = true;
					if (!skipAllyLane && DefenseLanes.AllyLaneWins(ownWeight, allyWeight, laneAllyShare))
					{
						ally = member;
						lane = allyLane;
					}
				}
			}

			hot = ownWeight > 0 || allyHot;
			laneSplit.Observe(hot);
			if (lane < 0 || !laneSplit.LaneTurn(laneShare))
			{
				ally = null;
				return -1;
			}

			return lane;
		}

		void LeaveLaneBoard()
		{
			if (laneMember != null)
				laneBoard?.Leave(player.InternalName);

			laneMember = null;
		}

		// Save/load (the TEAM-EXPAND pattern): each bot saves its own memory; the team board is rebuilt as the members restore.
		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (lanes == null || !LanesArmed)
				return null;

			return [new MiniYamlNode(SavedLanes.SaveKey, SavedLanes.Encode(lanes.Save(laneSplit)))];
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (world.IsReplay || !LanesArmed || data.NodeWithKeyOrDefault(SavedLanes.SaveKey) is not MiniYamlNode node)
				return;

			// A damaged, other-version or other-shape text restores nothing: the bot starts with no lane memory.
			var state = SavedLanes.Decode(node.Value.Value);
			if (state == null || state.Weight.Length != Math.Max(1, Info.LaneBuckets))
				return;

			RefreshLaneKnobs();
			EnsureLanes();
			lanes.Restore(state, laneSplit);
		}
	}
}
