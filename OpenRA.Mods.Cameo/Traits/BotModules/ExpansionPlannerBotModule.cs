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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using CAAIUtils = OpenRA.Mods.CA.AIUtils;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[Desc("Phase EX-0 of the expansion planner (docs/design/AI_ARCHITECTURE.md §12.13): scores every resource field",
		"we hold no refinery at by value x safety / time-until-it-pays and keeps the best one as the target field.",
		"Telemetry only: nothing reads the target yet (EX-1 makes the base builder walk toward it).")]
	public class ExpansionPlannerBotModuleInfo : ConditionalTraitInfo, NotBefore<ResourceMapBotModuleInfo>
	{
		[Desc("Ticks between two re-plans.")]
		public readonly int ReplanTicks = 250;

		[Desc("How far (cells) one new building extends the base: the step of the building line toward a field.")]
		public readonly int LinkStepCells = 4;

		[Desc("How far (cells) from a building that gives buildable area a refinery can still be placed next to a field.",
			"A field closer than this needs no link buildings.")]
		public readonly int ReachCells = 6;

		[Desc("Radius (cells) around a field's resource centre in which our own combat units count as its guard.")]
		public readonly int GuardRadiusCells = 12;

		[Desc("Smoothing (ticks) added to the time-until-it-pays, so a field next to the yard does not divide by ~0.")]
		public readonly int TauTicks = 750;

		[Desc("Window (ticks) over which the income used to turn costs into time is measured.")]
		public readonly int IncomeWindowTicks = 1500;

		[Desc("EX-1: publish the target field to the base builder, whose BaseCrawl placements then walk toward it.",
			"False = telemetry only (EX-0).")]
		public readonly bool DriveBaseCrawl = false;

		[Desc("EX-2: while the target field is in reach and unclaimed, ask the base builder for a refinery there, beyond",
			"its fixed optimum (every field in reach gets one). Needs DriveBaseCrawl, which publishes the target.")]
		public readonly bool DriveRefineries = false;

		[Desc("EX-2: an own refinery this close (cells) to a field's resource centre claims it.")]
		public readonly int ClaimRadiusCells = 8;

		[Desc("EX-2: refineries built while a field stayed unclaimed before the field is parked (a placement that keeps",
			"missing it must not turn into a refinery loop).")]
		public readonly int MaxClaimAttempts = 2;

		[Desc("EX-2: ticks a parked field is left out of the candidates.")]
		public readonly int ParkTicks = 3000;

		[Desc("Building queues searched for the refinery and the cheapest link building. Empty = the enabled base",
			"builder's own BuildingQueues (Cameo's classic mode builds from the player-level RABuilding queue).")]
		public readonly HashSet<string> BuildingQueues = new();

		public override object Create(ActorInitializer init) { return new ExpansionPlannerBotModule(init.Self, this); }
	}

	public class ExpansionPlannerBotModule : ConditionalTrait<ExpansionPlannerBotModuleInfo>, IBotTick, IBotExpansionTargetProvider
	{
		public readonly struct FieldScore
		{
			public readonly int Index;
			public readonly CPos Center;
			public readonly int Value;
			public readonly int Hops;
			public readonly int PaybackTicks;
			public readonly int Threat;
			public readonly double Score;

			public FieldScore(int index, CPos center, int value, int hops, int paybackTicks, int threat, double score)
			{
				Index = index;
				Center = center;
				Value = value;
				Hops = hops;
				PaybackTicks = paybackTicks;
				Threat = threat;
				Score = score;
			}
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly Dictionary<int, int> initialCells = new();
		readonly Queue<(int Tick, int Earned)> income = new();

		ResourceMapBotModule resourceMap;
		PlayerResources resources;
		IBotRegionThreatProvider[] threatProviders;
		BaseBuilderBotModuleCA[] baseBuilders;
		int ticks;
		string lastIdleReason;
		readonly Dictionary<int, int> claimAttempts = new();
		readonly Dictionary<int, int> parkedUntil = new();
		(int Field, int Refineries) wanting = (-1, 0);
		bool wantsRefinery;

		public ExpansionPlannerBotModule(Actor self, ExpansionPlannerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		/// <summary>The best field of the last re-plan; null when there is none (EX-1 reads it).</summary>
		public FieldScore? Target { get; private set; }

		public IReadOnlyList<FieldScore> LastScores { get; private set; } = Array.Empty<FieldScore>();

		CPos? IBotExpansionTargetProvider.ExpansionTarget => IsTraitDisabled || !Info.DriveBaseCrawl ? null : Target?.Center;

		bool IBotExpansionTargetProvider.WantsRefineryAtExpansionTarget => !IsTraitDisabled && Info.DriveRefineries && wantsRefinery;

		int IBotExpansionTargetProvider.ExpansionTargetClaimRadius => Info.ClaimRadiusCells;

		/// <summary>EX-2: a field counts as ours when an own refinery stands within the claim radius of its resource centre.</summary>
		public static bool Claimed(CPos center, IEnumerable<CPos> refineries, int claimRadiusCells)
		{
			return refineries.Any(r => (r - center).LengthSquared <= claimRadiusCells * claimRadiusCells);
		}

		/// <summary>
		/// EX-2 loop guard: while we want a refinery at `field`, every refinery we gain without the field turning ours is a
		/// missed claim. Returns the updated state and whether the field must now be parked.
		/// </summary>
		public static ((int Field, int Refineries) Wanting, int Attempts, bool Park) TrackClaim(
			(int Field, int Refineries) wanting, int field, int refineries, int attempts, int maxAttempts)
		{
			if (wanting.Field != field)
				return ((field, refineries), attempts, false);

			if (refineries > wanting.Refineries)
				attempts += refineries - wanting.Refineries;

			return ((field, refineries), attempts, attempts >= maxAttempts);
		}

		protected override void TraitEnabled(Actor self)
		{
			resourceMap = self.TraitsImplementing<ResourceMapBotModule>().FirstOrDefault(t => t.IsTraitEnabled());
			resources = self.TraitOrDefault<PlayerResources>();
			threatProviders = self.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			baseBuilders = self.TraitsImplementing<BaseBuilderBotModuleCA>().ToArray();
		}

		/// <summary>
		/// §12.13: score = V x S / (T + tau), S = 1 / (1 + threat / max(guard, 1)),
		/// T = cost / income + hops x link build time + refinery build time. Pure, for the tests.
		/// </summary>
		public static double Score(int value, int threat, int guard, int costCredits, double incomePerTick,
			int hops, int linkBuildTicks, int refineryBuildTicks, int tauTicks, out int paybackTicks)
		{
			// No income yet: every credit costs a tick, so fields still rank by cost, distance and value.
			var costTicks = incomePerTick > 0 ? costCredits / incomePerTick : costCredits;
			paybackTicks = (int)Math.Min(int.MaxValue, costTicks + (long)hops * linkBuildTicks + refineryBuildTicks);
			var safety = 1.0 / (1.0 + threat / (double)Math.Max(guard, 1));
			return value * safety / (paybackTicks + Math.Max(tauTicks, 1));
		}

		/// <summary>Buildings needed to bring the base within reach of a field `distance` cells away.</summary>
		public static int Hops(int distanceCells, int reachCells, int stepCells)
		{
			var gap = distanceCells - reachCells;
			return gap <= 0 ? 0 : (gap + Math.Max(stepCells, 1) - 1) / Math.Max(stepCells, 1);
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (resourceMap == null || resources == null)
			{
				if (++ticks % Info.ReplanTicks == 0)
					Idle(resourceMap == null ? "no enabled ResourceMapBotModule" : "no PlayerResources");
				return;
			}

			income.Enqueue((world.WorldTick, resources.Earned));
			while (income.Count > 1 && world.WorldTick - income.Peek().Tick > Info.IncomeWindowTicks)
				income.Dequeue();

			if (++ticks % Info.ReplanTicks != 0)
				return;

			Replan();
		}

		void Replan()
		{
			var (refinery, link, queues) = CheapestBuildables();
			if (refinery.Info == null)
			{
				Target = null;
				LastScores = Array.Empty<FieldScore>();
				Idle($"no refinery buildable ({queues} building queue(s) searched)");
				return;
			}

			// One pass over OUR OWN actors (always visible to us: fog-honest): the cells of the buildings that extend the
			// base (GivesBuildableArea; a captured derrick or a garrisoned house does not), and guard units.
			var buildingCells = new List<CPos>();
			var refineryCells = new List<CPos>();
			var guards = new List<(CPos Cell, int Value)>();
			foreach (var a in world.Actors)
			{
				if (a.Owner != player || a.IsDead || !a.IsInWorld)
					continue;

				// The Refinery trait, from rules (the by-name lists are filled by the role rollout, §2.8).
				if (a.Info.HasTraitInfo<RefineryInfo>())
					refineryCells.Add(a.Location);

				if (a.Info.HasTraitInfo<GivesBuildableAreaInfo>())
					buildingCells.Add(a.Location);
				else if (a.Info.HasTraitInfo<BuildingInfo>())
					continue;
				else if (a.Info.HasTraitInfo<AttackBaseInfo>() && !a.Info.HasTraitInfo<HarvesterInfo>())
					guards.Add((a.Location, a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0));
			}

			if (buildingCells.Count == 0)
			{
				Idle("no own building gives buildable area");
				return;
			}

			var first = income.Count > 0 ? income.Peek() : (Tick: world.WorldTick, Earned: resources.Earned);
			var span = Math.Max(1, world.WorldTick - first.Tick);
			var incomePerTick = (resources.Earned - first.Earned) / (double)span;
			var reach = Info.ReachCells;
			var scores = new List<FieldScore>();
			var fields = resourceMap.GetIndicesLength();
			var owned = 0;

			for (var i = 0; i < resourceMap.GetIndicesLength(); i++)
			{
				var field = resourceMap.GetIndice(i);
				if (field == null || (field.ResourceCellsCount <= 0 && !initialCells.ContainsKey(i)))
					continue;

				// Ruling (c): the field's size at match start is public map data. The first scan of a field
				// fixes its value; depletion under the fog is never read (EX-1 adds depletion that was seen).
				if (!initialCells.TryGetValue(i, out var value))
					initialCells[i] = value = field.ResourceCellsCount;

				var center = field.ResourceCellsCenter;
				if (field.PlayerRefineryCount > 0 || Claimed(center, refineryCells, Info.ClaimRadiusCells) || value <= 0)
				{
					owned += value > 0 ? 1 : 0;
					continue;
				}

				if (parkedUntil.TryGetValue(i, out var until) && world.WorldTick < until)
					continue;

				var distance = buildingCells.Min(c => (c - center).Length);
				var hops = Hops(distance, reach, Info.LinkStepCells);
				var threat = threatProviders.Sum(p => p.RememberedEnemyThreatAt(center));
				var guardValue = guards.Where(g => (g.Cell - center).LengthSquared <= Info.GuardRadiusCells * Info.GuardRadiusCells)
					.Sum(g => g.Value);
				var cost = refinery.Cost + hops * link.Cost;
				var score = Score(value, threat, guardValue, cost, incomePerTick, hops, link.BuildTicks, refinery.BuildTicks,
					Info.TauTicks, out var payback);
				scores.Add(new FieldScore(i, center, value, hops, payback, threat, score));
			}

			scores.Sort((a, b) => b.Score.CompareTo(a.Score));
			LastScores = scores;
			var previous = Target?.Index;
			Target = scores.Count > 0 ? scores[0] : null;
			if (Target == null)
				Idle($"no free field ({fields} map indices, {initialCells.Count} with resources, {owned} already ours)");
			else
				lastIdleReason = null;

			// EX-2: want a refinery at the target only while it is in reach; park a field that keeps being missed.
			wantsRefinery = Target is FieldScore w && w.Hops == 0;
			if (wantsRefinery && Target is FieldScore want)
			{
				claimAttempts.TryGetValue(want.Index, out var attempts);
				var (state, now, park) = TrackClaim(wanting, want.Index, refineryCells.Count, attempts, Info.MaxClaimAttempts);
				wanting = state;
				claimAttempts[want.Index] = now;
				if (park)
				{
					parkedUntil[want.Index] = world.WorldTick + Info.ParkTicks;
					claimAttempts.Remove(want.Index);
					wantsRefinery = false;
					var parked = $"AI ({player.ClientIndex}): EX-2 parked field {want.Index} at {want.Center} for {Info.ParkTicks} ticks: {now} refinery(ies) built without claiming it, at tick {world.WorldTick}";
					Log.Write("debug", parked);
					AIUtils.BotDebug(parked);
				}
			}
			else
				wanting = (-1, refineryCells.Count);

			// Telemetry (EX-0): a debug.log line whenever the target field changes, so a batch shows where each bot
			// wanted to expand and why. BotDebug alone only reaches the in-game chat.
			if (Target is FieldScore t && t.Index != previous)
			{
				var line = string.Format("AI ({0}): EX-0 target field {1} at {2}: value {3}, hops {4}, payback {5} ticks, threat {6}, score {7:F4} ({8} fields) at tick {9}",
					player.ClientIndex, t.Index, t.Center, t.Value, t.Hops, t.PaybackTicks, t.Threat, t.Score, scores.Count, world.WorldTick);
				Log.Write("debug", line);
				AIUtils.BotDebug(line);
			}
		}

		void Idle(string reason)
		{
			if (reason == lastIdleReason)
				return;

			lastIdleReason = reason;
			var line = $"AI ({player.ClientIndex}): EX-0 no target at tick {world.WorldTick}: {reason}";
			Log.Write("debug", line);
			AIUtils.BotDebug(line);
		}

		((ActorInfo Info, int Cost, int BuildTicks) Refinery, (ActorInfo Info, int Cost, int BuildTicks) Link, int Queues) CheapestBuildables()
		{
			(ActorInfo Info, int Cost, int BuildTicks) refinery = default, link = default;
			var queues = 0;
			// Resolved per re-plan: the base builder's condition may switch on after ours.
			var types = Info.BuildingQueues.Count > 0 ? Info.BuildingQueues
				: baseBuilders.FirstOrDefault(t => t.IsTraitEnabled())?.Info.BuildingQueues ?? (IEnumerable<string>)new[] { "Building" };
			foreach (var queue in types.SelectMany(type => CAAIUtils.FindQueues(player, type)).Distinct())
			{
				queues++;
				foreach (var item in queue.BuildableItems())
				{
					var bi = item.TraitInfoOrDefault<BuildableInfo>();
					if (bi == null || !item.HasTraitInfo<BuildingInfo>())
						continue;

					var cost = item.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
					var entry = (item, cost, queue.GetBuildTime(item, bi));
					// The Refinery trait, from rules: the by-name lists were emptied by the role rollout (§2.8).
					if (item.HasTraitInfo<RefineryInfo>())
					{
						if (refinery.Info == null || cost < refinery.Cost)
							refinery = entry;
					}
					else if (cost > 0 && (link.Info == null || cost < link.Cost))
						link = entry;
				}
			}

			return (refinery, link, queues);
		}
	}
}
