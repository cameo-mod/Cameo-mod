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
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// REF2-FIRST-01 (USER BINDING 11-26, Opus rulings; one switch Ref2First, rows ref2_before_radar, ref2_before_prod2, ref2_floor, ref2_stall_release): the pure decisions, the two personality
	// exemptions, the skip when no reachable field has room (and when there is no law provider), the bound, the stall release, and the wiring pins (off = identical).
	[TestFixture]
	public sealed class Ref2FirstTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void Ref2IsDueOnlyBelowTwoRefineriesWithABuildableUnmaxedRefineryAndAFieldWithRoom()
		{
			Assert.That(Ref2First.Due(true, false, 0, true, false, true), Is.True);
			Assert.That(Ref2First.Due(true, false, 1, true, false, true), Is.True, "ref1 stands: ref2 is due");
			Assert.That(Ref2First.Due(true, false, 2, true, false, true), Is.False, "two refineries stand or are queued");
			Assert.That(Ref2First.Due(false, false, 1, true, false, true), Is.False, "switch off");
			Assert.That(Ref2First.Due(true, true, 1, true, false, true), Is.False, "the gate has finished for good");
			Assert.That(Ref2First.Due(true, false, 1, false, false, true), Is.False, "no refinery buildable");
			Assert.That(Ref2First.Due(true, false, 1, true, true, true), Is.False, "refinery max reached");
			Assert.That(Ref2First.Due(true, false, 1, true, false, false), Is.False, "no reachable field under its cap: the gate is skipped, nothing is held for a refinery that cannot be placed");
		}

		[Test]
		public void TheCapLiftDropsAsSoonAsRef2IsInProductionSoNoThirdRefineryIsClaimed()
		{
			Assert.That(Ref2First.LiftsCap(true, 0, 0), Is.True);
			Assert.That(Ref2First.LiftsCap(true, 1, 0), Is.True, "ref1 stands, ref2 not yet ordered: the lift opens the second claim");
			Assert.That(Ref2First.LiftsCap(true, 1, 1), Is.False, "ref2 queued (the in-flight window): lift off, claimable anchors cannot exceed the refineries in flight");
			Assert.That(Ref2First.LiftsCap(true, 0, 2), Is.False, "two in production at the start");
			Assert.That(Ref2First.LiftsCap(true, 2, 0), Is.False, "two stand");
			Assert.That(Ref2First.LiftsCap(false, 0, 0), Is.False, "row off: no lift");
		}

		[Test]
		public void TheBoundEndsTheHoldSoTheOrderNeverDeadlocks()
		{
			Assert.That(Ref2First.BoundExpired(-1, 99999, 3000), Is.False, "never due");
			Assert.That(Ref2First.BoundExpired(1000, 4000, 3000), Is.False, "exactly the bound");
			Assert.That(Ref2First.BoundExpired(1000, 4001, 3000), Is.True);
		}

		[Test]
		public void Ref2IsStartableOnlyOnceTheWarFactoryWaitIsOverAndMirrorsTheEconomyGatesWaitFactoryRung()
		{
			Assert.That(Ref2First.Startable(true, true, false, false), Is.False, "ref1, war factory, THEN ref2: the order itself says wait for the war factory");
			Assert.That(Ref2First.Startable(true, true, true, false), Is.True, "a war factory stands or is in production");
			Assert.That(Ref2First.Startable(true, true, false, true), Is.True, "the war-factory wait was released (banned, placement failed, or the wait ran out)");
			Assert.That(Ref2First.Startable(true, false, false, false), Is.True, "no vehicle factory buildable: the wait never applies");
			Assert.That(Ref2First.Startable(false, false, true, true), Is.False, "not due");

			// one truth table with the economy gate: startable iff its WaitFactory rung does not hold a refinery (one refinery stands, no interval pending, nothing essential pending)
			foreach (var buildable in new[] { false, true })
				foreach (var owned in new[] { false, true })
					foreach (var released in new[] { false, true })
					{
						var verdict = RefineryFieldGateCA.Economy(true, 1, false, buildable, owned, released, 1000, -1, 1500, false, -1, 1500);
						Assert.That(Ref2First.Startable(true, buildable, owned, released), Is.EqualTo(verdict != RefineryFieldGateCA.EconomyVerdict.WaitFactory),
							"buildable " + buildable + " owned " + owned + " released " + released);
					}
		}

		[Test]
		public void TheStallClockRunsOnConsecutiveStartableTicksAndReleasesAfterTheStallTicks()
		{
			Assert.That(Ref2First.StartableSince(-1, true, 600), Is.EqualTo(600), "starts the clock");
			Assert.That(Ref2First.StartableSince(600, true, 900), Is.EqualTo(600), "kept while it stays startable");
			Assert.That(Ref2First.StartableSince(600, false, 900), Is.EqualTo(-1), "not startable (not due, or the war-factory wait is back): the clock stops");
			Assert.That(Ref2First.StartableSince(-1, false, 900), Is.EqualTo(-1));

			Assert.That(Ref2First.StallExpired(-1, 99999, 750), Is.False, "never startable: no stall, only the outer bound can end the hold");
			Assert.That(Ref2First.StallExpired(600, 1350, 750), Is.False, "exactly 750 ticks");
			Assert.That(Ref2First.StallExpired(600, 1351, 750), Is.True);

			// the walk of the gate: startable from 600, no refinery starts, the hold lifts at 1351; a gap in startable restarts the clock
			var since = -1;
			var released = -1;
			for (var now = 0; now <= 3000 && released < 0; now++)
			{
				since = Ref2First.StartableSince(since, now >= 600, now);
				if (Ref2First.StallExpired(since, now, 750))
					released = now;
			}

			Assert.That(released, Is.EqualTo(1351));
			since = -1;
			released = -1;
			for (var now = 0; now <= 4000 && released < 0; now++)
			{
				since = Ref2First.StartableSince(since, now >= 600 && now != 1000, now);
				if (Ref2First.StallExpired(since, now, 750))
					released = now;
			}

			Assert.That(released, Is.EqualTo(1752), "one tick not startable at 1000 restarts the clock at 1001: 1001 + 750 < 1752");

			// the stall is sooner than the outer bound, and the outer bound is untouched
			Assert.That(new BaseBuilderBotModuleCAInfo().Ref2StallTicks, Is.EqualTo(750));
			Assert.That(new BaseBuilderBotModuleCAInfo().Ref2StallTicks, Is.LessThan(new BaseBuilderBotModuleCAInfo().Ref2FirstBoundTicks));
			Assert.That(Ref2First.BoundExpired(600, 3601, 3000), Is.True, "the 3000-tick bound is still the outer cap");
		}

		[Test]
		public void TheStallClockIsSampledOncePerBotVisitSoANotStartableSpellIsNeverUnseen()
		{
			Assert.That(Ref2First.SampleWindow(true, false, -1, 0, 3000), Is.True, "from the start of the game: the clock begins when ref2 really becomes startable");
			Assert.That(Ref2First.SampleWindow(true, false, -1, 2999, 3000), Is.True);
			Assert.That(Ref2First.SampleWindow(true, false, -1, 3000, 3000), Is.False, "never due within the bound: left to the pick-time path");
			Assert.That(Ref2First.SampleWindow(true, false, 1200, 5000, 3000), Is.True, "due once: the sample keeps running until the gate finishes");
			Assert.That(Ref2First.SampleWindow(true, true, 1200, 1300, 3000), Is.False, "finished for good");
			Assert.That(Ref2First.SampleWindow(false, false, 1200, 1300, 3000), Is.False, "row off (switch or ablation): the sample never runs");
			Assert.That(Ref2First.SampleTicks, Is.EqualTo(50));
			Assert.That(Ref2First.SampleTicks, Is.LessThan(new BaseBuilderBotModuleCAInfo().Ref2StallTicks / 10), "the clock error is a small part of the stall");

			// the walk at the sample cadence (Haiku's gap case): startable from 600, lost at the sample of 650 and back at 700. The sampled clock restarts at 700 and releases at 1500, not at 1351
			// (an unsampled spell would have left the old clock running)
			var since = -1;
			var released = -1;
			for (var now = 0; now <= 3000 && released < 0; now += Ref2First.SampleTicks)
			{
				since = Ref2First.StartableSince(since, now >= 600 && now != 650, now);
				if (Ref2First.StallExpired(since, now, 750))
					released = now;
			}

			Assert.That(released, Is.EqualTo(1500), "700 + 750 < 1500 (the first sample past the stall)");
		}

		[Test]
		public void TheStallSampleIsCalledEveryBotVisitFromTheModuleSingleWriterAndCheap()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			var r2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");

			// the call site: the module's BotTick, every bot visit, ahead of the manager rotation, flags only before the sampler
			var armed = bb.IndexOf("if (Ref2StallArmed && !Ref2Finished)", StringComparison.Ordinal);
			var tick = bb.IndexOf("builders[currentBuilderIndex].Tick(bot);", StringComparison.Ordinal);
			var sample = bb.IndexOf("ref2Sampler?.Ref2StallSample();", StringComparison.Ordinal);
			Assert.That(armed, Is.GreaterThan(0));
			Assert.That(sample, Is.GreaterThan(armed));
			Assert.That(tick, Is.GreaterThan(sample), "ahead of the queue managers' rotation and their sweep cooldown");
			Assert.That(bb, Does.Contain("Info.BuildingQueues.Contains(builder.Category)"), "one building manager is the sampler");

			// the sampler: window, spacing, union of the building queues, flag sampleStall = true
			Assert.That(r2, Does.Contain("public void Ref2StallSample()"));
			Assert.That(r2, Does.Contain("Ref2First.SampleWindow(baseBuilder.Ref2StallArmed, baseBuilder.Ref2Finished, baseBuilder.Ref2DueSince, now, baseBuilder.Info.Ref2FirstBoundTicks)"));
			Assert.That(r2, Does.Contain("ref2NextSample = now + Ref2First.SampleTicks;"));
			Assert.That(r2, Does.Contain("ComputeRef2Due(ref2SampleBuildable, now, true);"));
			Assert.That(r2, Does.Contain("ref2SampleBuildable ??= Ref2BuildableInAnyQueue;"), "one cached delegate");

			// the pick-time path never writes the clock: the single writer is the sampler
			Assert.That(r2, Does.Contain("ref2DueValue = ComputeRef2Due(buildable, now, false);"));
			Assert.That(r2.Split("baseBuilder.Ref2StartableSince = ").Length - 1, Is.EqualTo(1), "one write site, inside the sampleStall block");
			Assert.That(r2, Does.Contain("if (sampleStall && baseBuilder.Ref2StallArmed)"));

			// cheap: no LINQ, no list or set allocation in the sampler, its buildable union or the stall block
			var from = r2.IndexOf("public void Ref2StallSample()", StringComparison.Ordinal);
			var to = r2.IndexOf("bool ComputeRef2Due(", StringComparison.Ordinal);
			var stallFrom = r2.IndexOf("if (sampleStall && baseBuilder.Ref2StallArmed)", StringComparison.Ordinal);
			var stallTo = r2.IndexOf("Ref2Observed(due ? \"due\"", StringComparison.Ordinal);
			Assert.That(to, Is.GreaterThan(from));
			Assert.That(stallTo, Is.GreaterThan(stallFrom));
			foreach (var region in new[] { r2.Substring(from, to - from), r2.Substring(stallFrom, stallTo - stallFrom) })
				foreach (var banned in new[] { ".Any(", ".Where(", ".Select(", ".ToList(", ".ToArray(", ".ToHashSet(", ".Concat(", ".Count(", "new List<", "new HashSet<", "new Dictionary<" })
					Assert.That(region, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void TheStallReleaseIsWiredAfterTheBoundAndBeforeTheDueVerdictAndEndsTheGateForGood()
		{
			var r2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");
			var bound = r2.IndexOf("Ref2First.BoundExpired(baseBuilder.Ref2DueSince, now, b.Ref2FirstBoundTicks)", StringComparison.Ordinal);
			var armed = r2.IndexOf("if (sampleStall && baseBuilder.Ref2StallArmed)", StringComparison.Ordinal);
			var due = r2.IndexOf("Ref2Observed(due ? \"due\" : \"not_due\"", StringComparison.Ordinal);
			Assert.That(bound, Is.GreaterThan(0));
			Assert.That(armed, Is.GreaterThan(bound), "the outer bound is checked first");
			Assert.That(due, Is.GreaterThan(armed), "the stall release is decided before the due verdict is returned");

			var block = r2.Substring(armed, due - armed);
			Assert.That(block, Does.Contain("Ref2First.Startable(due, factoryBuildable, factoryOwnedOrQueued, factoryWaitReleased)"));
			Assert.That(block, Does.Contain("baseBuilder.Ref2StartableSince = Ref2First.StartableSince(baseBuilder.Ref2StartableSince, startable, now);"));
			Assert.That(block, Does.Contain("Ref2First.StallExpired(baseBuilder.Ref2StartableSince, now, b.Ref2StallTicks)"));
			Assert.That(block, Does.Contain("baseBuilder.Ref2Finished = true;"), "the gate finishes for good: the hold lifts and the old order runs");
			Assert.That(block, Does.Contain("return false;"));
			Assert.That(block, Does.Contain("Ref2Observed(\"stall_released\","));
			Assert.That(block, Does.Not.Contain("Cancel"), "nothing is cancelled");

			// the same war-factory inputs as the economy gate
			Assert.That(block, Does.Contain("var factoryWaitReleased = RefineryFieldGateCA.FactoryWaitReleased(factoryBanned,"));
			Assert.That(block, Does.Contain("baseBuilder.RefineryGateFactoryPlacementFailed, now, baseBuilder.RefineryGateFirstStoodTick, b.RefineryFactoryWaitTicks);"));

			// the DEV detail names the downstream blocker: the last refinery-pick economy verdict, the claimable anchors, standing and queued refineries, the war-factory state
			foreach (var part in new[] { "startable_since_", "due_since_", "eco_{lastRefineryEconomyVerdict}", "anchors_{law?.UnclaimedAnchorsInReach}", "standing_", "queued_", "factory_" })
				Assert.That(block, Does.Contain(part), part);
		}

		[Test]
		public void TheRefineryPickHoldReachesTheDebugLogFileInTheDevBuildOnly()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			var at = qm.IndexOf("REF-HOLD refinery pick held", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(0));
			Assert.That(qm.LastIndexOf("#if CAMEO_DEV_LOGGING", at, StringComparison.Ordinal), Is.InRange(at - 400, at - 1), "inside a DEV-only region");
			Assert.That(qm.IndexOf("#endif", at, StringComparison.Ordinal), Is.InRange(at, at + 200), "the region closes right after the line");
			Assert.That(qm, Does.Contain("AIUtils.BotDebug(\"{0} holds the refinery pick: {1} (refineries {2})\", queue.Actor.Owner, verdict, count);"), "the notification line stays");

			// the two other V08G blockers the game must name already log in the DEV build: the structured bb_refinery_held row (gate + economy verdict) and the REF-1 park line (debug log)
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs"), Does.Contain("return RefineryEconomyGateOn ? \"economy_\" + lastRefineryEconomyVerdict : \"power\";"));
			var planner = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			var park = planner.IndexOf("REF-1 parked anchor", StringComparison.Ordinal);
			Assert.That(park, Is.GreaterThan(0));
			Assert.That(planner.LastIndexOf("#if CAMEO_DEV_LOGGING", park, StringComparison.Ordinal), Is.InRange(park - 300, park - 1));
			Assert.That(planner.IndexOf("Log.Write(\"debug\", parked);", park, StringComparison.Ordinal), Is.GreaterThan(park));
		}

		[Test]
		public void TheRadarIsHeldWhileRef2IsDueAndTechIsExempt()
		{
			Assert.That(Ref2First.HoldsRadar(true, true, "steamroller"), Is.True);
			Assert.That(Ref2First.HoldsRadar(true, true, null), Is.True, "no personality");
			Assert.That(Ref2First.HoldsRadar(true, true, "rush"), Is.True, "RUSH is exempt from the production half only");
			Assert.That(Ref2First.HoldsRadar(true, true, "tech"), Is.False, "TECH personality: radar rule exempt");
			Assert.That(Ref2First.HoldsRadar(true, true, "TECH"), Is.False);
			Assert.That(Ref2First.HoldsRadar(false, true, "turtle"), Is.False, "ref2 not due");
			Assert.That(Ref2First.HoldsRadar(true, false, "turtle"), Is.False, "not a radar provider");
		}

		[Test]
		public void TheSecondProductionBuildingIsHeldWhileRef2IsDueAndRushIsExempt()
		{
			Assert.That(Ref2First.HoldsSecondProduction(true, true, 1, "steamroller"), Is.True, "2nd barracks / war factory / airfield");
			Assert.That(Ref2First.HoldsSecondProduction(true, true, 3, null), Is.True, "3rd as well");
			Assert.That(Ref2First.HoldsSecondProduction(true, true, 0, "turtle"), Is.False, "the first copy of a role is never held: ref1, war factory, ref2");
			Assert.That(Ref2First.HoldsSecondProduction(true, true, 1, "rush"), Is.False, "RUSH personality: an early all-in stays possible");
			Assert.That(Ref2First.HoldsSecondProduction(true, true, 1, "tech"), Is.True, "TECH is exempt from the radar half only");
			Assert.That(Ref2First.HoldsSecondProduction(false, true, 1, "turtle"), Is.False, "ref2 not due");
			Assert.That(Ref2First.HoldsSecondProduction(true, false, 1, "turtle"), Is.False, "not a production building");
		}

		[Test]
		public void Ref2OutranksTheIntervalWaitOnlyWhileItIsDue()
		{
			Assert.That(Ref2First.OutranksIntervalWait(true, 1), Is.True);
			Assert.That(Ref2First.OutranksIntervalWait(true, 2), Is.False, "the interval stays between the 2nd and 3rd refinery");
			Assert.That(Ref2First.OutranksIntervalWait(false, 1), Is.False);

			// the war-factory wait is not touched: the economy verdict order puts it before the interval, and only WaitInterval is outranked
			var wait = RefineryFieldGateCA.Economy(true, 1, false, true, false, false, 1000, 900, 1500, false, -1, 1500);
			Assert.That(wait, Is.EqualTo(RefineryFieldGateCA.EconomyVerdict.WaitFactory), "ref1, first war factory, THEN ref2");
			var interval = RefineryFieldGateCA.Economy(true, 1, false, true, true, false, 1000, 900, 1500, false, -1, 1500);
			Assert.That(interval, Is.EqualTo(RefineryFieldGateCA.EconomyVerdict.WaitInterval));
		}

		[Test]
		public void TheGateIsWiredIntoEveryStageTheLegacyLadderAndTheEconomyGateAndNothingIsCancelled()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			var r2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");

			// one precedence gate: the staged planner's basic feasibility (every stage) and the legacy ladder's pick
			Assert.That(p1, Does.Contain("if (Ref2Holds(info, buildableNames.Contains))"));
			Assert.That(qm, Does.Contain("if (item != null && Ref2Holds(item, ref2Name =>"));
			Assert.That(qm, Does.Contain("Ref2OutranksInterval(buildableThings, count)"));
			Assert.That(qm, Does.Contain("verdict == RefineryFieldGateCA.EconomyVerdict.WaitInterval && Ref2OutranksInterval"), "only the interval wait is outranked, never the war-factory wait");

			// the rows are separate latches; a hold only ever means 'not offered now' (no cancel call anywhere in the gate)
			Assert.That(r2, Does.Contain("baseBuilder.Ref2RadarArmed && IsRadarProvider(info)"));
			Assert.That(r2, Does.Contain("baseBuilder.Ref2Prod2Armed && baseBuilder.Info.ProductionTypes.Contains(info.Name)"));
			Assert.That(r2, Does.Not.Contain("Cancel"), "the gate never cancels");
			Assert.That(r2, Does.Contain("Ref2First.BoundExpired(baseBuilder.Ref2DueSince, now, b.Ref2FirstBoundTicks)"));
			Assert.That(r2, Does.Contain("var fieldUnderCap = law != null && law.UnclaimedAnchorsInReach > 0;"), "no reachable field with room = skip");
			Assert.That(r2, Does.Not.Contain("law == null ||"), "no law provider = no field verdict and no planner lift: ref2 is not due, nothing is held for it");

			// the planner lifts the zone cap by one while fewer than two own refineries stand; the time floor stays as it is
			var rd = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs");
			Assert.That(rd, Does.Contain("int EcoFloorLift() => Math.Max(Math.Max(EcoTimeFloorLift(), Ref2FirstLift()), RefTierLift());"), "REF-TIER-01 adds its own floor to the maximum");
			Assert.That(rd, Does.Contain("ref2First != null && ref2First.Ref2FloorArmed"));
			Assert.That(rd, Does.Contain("ref2Lift = Ref2First.LiftsCap(true, own, queued) ? 1 : 0;"));
			Assert.That(rd, Does.Contain("queued += inProduction;"), "own standing PLUS in production: with ref2 queued the lift drops, so no 3rd claim opens before ref2 lands");
			Assert.That(rd, Does.Contain("foreach (var r in builder.Info.RefineryTypes)"));
		}

		[Test]
		public void TheDevVerdictRowIsDevOnlyDedupedAndTheObserverMemberHasADefault()
		{
			var r2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");
			var at = r2.IndexOf("void Ref2Observed(", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(0));
			Assert.That(r2.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), "compiled out of a release build");
			foreach (var verdict in new[] { "\"held_radar\"", "\"held_prod2\"", "\"bound_expired\"", "\"stall_released\"", "\"not_due\"", "\"due\"" })
				Assert.That(r2, Does.Contain(verdict), verdict);
			foreach (var why in new[] { "\"two_refineries\"", "\"finished\"", "\"refinery_not_buildable\"", "\"refinery_max\"", "\"no_field_with_room\"" })
				Assert.That(r2, Does.Contain(why), why);

			Assert.That(Source("mods/CA/Traits/BotModules/IBotBuildingPickObserver.cs"), Does.Contain("void Ref2Verdict(Player owner, int tick, string verdict, string detail, int refineries, string queue) { }"),
				"a default member: an observer without the row keeps compiling");
			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			Assert.That(dev, Does.Contain("ObserverRow(owner, \"ref2_verdict\""));
			Assert.That(dev, Does.Contain("cov.LastRef2Key == key && cov.LastRef2Tick >= 0 && tick - cov.LastRef2Tick < RefineryHeldRepeatTicks"), "one row per changed verdict, or again after 750 ticks");
		}

		[Test]
		public void OffByDefaultArmedForTheGenericBaseBuilderOnlyAndTheRowsAreRegistered()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().Ref2First, Is.False, "default false in code");
			Assert.That(new BaseBuilderBotModuleCAInfo().Ref2FirstBoundTicks, Is.EqualTo(3000));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value;
			string Field(string module) => player.Nodes.Single(n => n.Key == module).Value.Nodes.SingleOrDefault(n => n.Key == "Ref2First")?.Value.Value?.Trim();
			Assert.That(Field("BaseBuilderBotModuleCA@generic"), Is.EqualTo("true"), "true in the V08 launcher rows");
			Assert.That(Field("BaseBuilderBotModuleCA@classic"), Is.Null, "classic stays as before");

			Assert.That(AblationIds.Ref2BeforeRadar, Is.EqualTo("ref2_before_radar"));
			Assert.That(AblationIds.Ref2BeforeProd2, Is.EqualTo("ref2_before_prod2"));
			Assert.That(AblationIds.Ref2Floor, Is.EqualTo("ref2_floor"));
			Assert.That(AblationIds.Ref2StallRelease, Is.EqualTo("ref2_stall_release"));
			foreach (var id in new[] { "ref2_before_radar", "ref2_before_prod2", "ref2_floor", "ref2_stall_release" })
				Assert.That(AblationSpec.RegistryIds(), Does.Contain(id));

			// off = identical: every armed property needs the switch, and the planner lift is 0 without it
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool Ref2RadarArmed => Info.Ref2First && switchesActive && !ablateRef2Radar.Off(player, AblationIds.Ref2BeforeRadar);"));
			Assert.That(bb, Does.Contain("public bool Ref2Prod2Armed => Info.Ref2First && switchesActive && !ablateRef2Prod2.Off(player, AblationIds.Ref2BeforeProd2);"));
			Assert.That(bb, Does.Contain("public bool Ref2FloorArmed => Info.Ref2First && switchesActive && !ablateRef2Floor.Off(player, AblationIds.Ref2Floor);"));
			Assert.That(bb, Does.Contain("public bool Ref2StallArmed => Info.Ref2First && switchesActive && !ablateRef2Stall.Off(player, AblationIds.Ref2StallRelease);"), "the stall row rides the same switch and the same latch pattern");
			Assert.That(bb, Does.Contain("public int Ref2StartableSince = -1;"));
		}
	}
}
