#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("SUPPORTPOWER-SLICE-01 (row sp_slice, default off): the bot's support power target scan, a bounded number of candidates per bot tick.",
		"While it is active it grants ActiveCondition, which the yaml uses to switch the engine's SupportPowerBotASModule instances off.",
		"It has no decisions of its own: it uses the Decisions of every SupportPowerBotASModule declared on the player actor (so it must require a condition under which all of them apply).")]
	public class SupportPowerSliceBotModuleInfo : ConditionalTraitInfo, Requires<SupportPowerManagerInfo>
	{
		[Desc("The switch. Off (the default) = this module does nothing and the engine module runs as before.")]
		public readonly bool SliceEnabled = false;

		[Desc("Candidates scored per bot tick, shared by all powers that are scanning. 0 or less = no limit (the whole scan in one tick, as the engine module does).")]
		public readonly int CandidatesPerTick = 32;

		[GrantedConditionReference]
		[Desc("Granted on the player actor while the module is active.")]
		public readonly string ActiveCondition = "sp-slice";

		public override object Create(ActorInitializer init) { return new SupportPowerSliceBotModule(init.Self, this); }
	}

	public class SupportPowerSliceBotModule : ConditionalTrait<SupportPowerSliceBotModuleInfo>, IBotTick
	{
		// One power's scan in progress: the candidates as they were when the scan began, and the position the best one had when it was scored.
		sealed class Running
		{
			public readonly SupportPowerSliceScan<Actor> Scan;
			public WPos BestPos;

			public Running(SupportPowerSliceScan<Actor> scan) { Scan = scan; }
		}

		readonly Actor self;
		readonly World world;
		readonly OpenRA.Player player;
		readonly MersenneTwister random;
		readonly Dictionary<string, SupportPowerDecisionAS> decisions = [];
		readonly Dictionary<SupportPowerInstance, int> nextScanTick = [];
		readonly Dictionary<SupportPowerInstance, Running> running = [];
		readonly List<SupportPowerInstance> stale = [];
		PlayerResources playerResource;
		SupportPowerManager supportPowerManager;
		AblationLatch ablate;
		int activeToken = Actor.InvalidConditionToken;

		public SupportPowerSliceBotModule(Actor self, SupportPowerSliceBotModuleInfo info)
			: base(info)
		{
			this.self = self;
			world = self.World;
			player = self.Owner;

			// The engine module's stream (same salt): it is switched off while this module is active, and this module draws nothing while it is not.
			random = BotRandom.Create(world, player, 0x17);
		}

		bool Active => Info.SliceEnabled && !ablate.Off(player, AblationIds.SupportPowerSlice);

		protected override void TraitEnabled(Actor self)
		{
			supportPowerManager = player.PlayerActor.Trait<SupportPowerManager>();
			playerResource = player.PlayerActor.Trait<PlayerResources>();
			decisions.Clear();
			foreach (var source in player.PlayerActor.Info.TraitInfos<SupportPowerBotASModuleInfo>())
				foreach (var decision in source.Decisions)
					decisions.TryAdd(decision.OrderName, decision);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (activeToken != Actor.InvalidConditionToken)
				activeToken = self.RevokeCondition(activeToken);

			running.Clear();
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (!Active)
				return;

			if (activeToken == Actor.InvalidConditionToken)
				activeToken = self.GrantCondition(Info.ActiveCondition);

			var unlimited = Info.CandidatesPerTick <= 0;
			var budget = Info.CandidatesPerTick;
			var now = world.WorldTick;

			foreach (var sp in supportPowerManager.Powers.Values)
			{
				if (sp.Disabled || !sp.Ready || !decisions.TryGetValue(sp.Info.OrderName, out var decision))
				{
					running.Remove(sp);
					continue;
				}

				// A recent attempt (no target, no money, or an order just issued): wait, as the engine module does. World ticks, not calls, so a gated bot needs no replay.
				if (nextScanTick.TryGetValue(sp, out var at) && now < at)
					continue;

				if (!running.TryGetValue(sp, out var run))
				{
					if (sp.Info.Cost != 0 && playerResource.Cash + playerResource.Resources < sp.Info.Cost)
					{
						nextScanTick[sp] = now + decision.GetNextScanTime(random);
						continue;
					}

					run = new Running(new SupportPowerSliceScan<Actor>(Candidates(decision), now));
					running[sp] = run;
				}

				if (!unlimited && budget <= 0)
					continue;

				var examined = run.Scan.Step(unlimited ? 0 : budget,
					a => Legal(a, decision),
					a => decision.GetAttractiveness(a.CenterPosition, player),
					decision.MinimumAttractiveness,
					a => run.BestPos = a.CenterPosition);
				budget -= examined;

				if (!run.Scan.Done)
					continue;

				running.Remove(sp);

				// A scan that took more than one tick scored its best a while ago: the place must still be worth it now. A one-tick scan is the engine's loop and needs no second look.
				var found = run.Scan.HasBest &&
					(run.Scan.StartTick == now || decision.GetAttractiveness(run.BestPos, player) >= decision.MinimumAttractiveness);
				ScanLog(sp, run, found);
				if (!found)
				{
					nextScanTick[sp] = now + decision.GetNextScanTime(random);
					continue;
				}

				// Valid target found: the same short wait and the same order as the engine module.
				nextScanTick[sp] = now + 10;
				bot.QueueOrder(
					new Order(sp.Key, supportPowerManager.Self, Target.FromCell(world, world.Map.CellContaining(run.BestPos)), false)
					{ SuppressVisualFeedback = true, ExtraData = uint.MaxValue });
			}

			// Powers that no longer exist
			foreach (var sp in nextScanTick.Keys)
				if (!supportPowerManager.Powers.ContainsKey(sp.Key))
					stale.Add(sp);

			foreach (var sp in running.Keys)
				if (!supportPowerManager.Powers.ContainsKey(sp.Key))
					stale.Add(sp);

			foreach (var sp in stale)
			{
				nextScanTick.Remove(sp);
				running.Remove(sp);
			}

			stale.Clear();
		}

		// The engine module's candidate rule (FindAttackLocationToSupportPower), unchanged: nothing is a candidate here that is not one there.
		bool Legal(Actor x, SupportPowerDecisionAS decision) =>
			x.IsInWorld && !x.IsDead &&
			(decision.IgnoreVisibility || x.CanBeViewedByPlayer(player)) &&
			decision.Against.HasRelationship(player.RelationshipWith(x.Owner)) &&
			decision.Types.Overlaps(x.GetEnabledTargetTypes());

		List<Actor> Candidates(SupportPowerDecisionAS decision) =>
			world.ActorsHavingTrait<IOccupySpace>().Where(x => Legal(x, decision)).ToList();

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void ScanLog(SupportPowerInstance sp, Running run, bool found)
		{
			Log.Write("debug", $"AI ({player.ClientIndex}): SUPPORTPOWER-SLICE-01 scan {sp.Info.OrderName} candidates {run.Scan.Count} ticks {world.WorldTick - run.Scan.StartTick} " +
				$"best {run.Scan.BestScore} found {(found ? 1 : 0)} tick {world.WorldTick}");
		}
	}
}
