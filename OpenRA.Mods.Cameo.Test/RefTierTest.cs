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

namespace OpenRA.Mods.Cameo.Test
{
	// REF-TIER-01 (USER BINDING 11-26, Opus rulings; one switch RefTierFloor, rows ref_tier_floor and ref_tier_personality): the refinery target is a FLOOR for refinery demand (2 early, +2 after a
	// radar provider, +2 after a tech centre, scaled by personality, clamped 1..8), never a hold. The pure decisions, the wiring pins (planner lift, interval outrank with the first-copy yield kept,
	// the standing-only tier, no hold and no sell), the DEV row and the yaml.
	[TestFixture]
	public sealed class RefTierTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheTierTargetIsTwoFourSixFromTheCompletedRadarAndTechCentre()
		{
			Assert.That(RefTier.BaseTarget(2, 2, 2, false, false), Is.EqualTo(2), "early");
			Assert.That(RefTier.BaseTarget(2, 2, 2, true, false), Is.EqualTo(4), "own radar provider standing");
			Assert.That(RefTier.BaseTarget(2, 2, 2, true, true), Is.EqualTo(6), "own tech centre standing too");
			Assert.That(RefTier.BaseTarget(2, 2, 2, false, true), Is.EqualTo(4), "a lost radar drops its two: the tiers add up from what stands");
			Assert.That(RefTier.BaseTarget(2, 2, 2, false, false), Is.EqualTo(2), "both lost: back to the early target");
		}

		[Test]
		public void ThePersonalityIsAPriorPercentRoundedHalfUpAndClamped()
		{
			// expansion x1.25 on every tier, rush x0.75 only before the radar, everyone else x1.0
			Assert.That(RefTier.PersonalityPct(true, "expansion", false, 125, 75), Is.EqualTo(125));
			Assert.That(RefTier.PersonalityPct(true, "expansion", true, 125, 75), Is.EqualTo(125));
			Assert.That(RefTier.PersonalityPct(true, "rush", false, 125, 75), Is.EqualTo(75));
			Assert.That(RefTier.PersonalityPct(true, "rush", true, 125, 75), Is.EqualTo(100), "rush only scales before the radar");
			foreach (var other in new[] { "tech", "steamroller", "turtle", "guerilla", "unknown" })
				Assert.That(RefTier.PersonalityPct(true, other, false, 125, 75), Is.EqualTo(100), other);
			Assert.That(RefTier.PersonalityPct(true, null, false, 125, 75), Is.EqualTo(100), "no personality");
			Assert.That(RefTier.PersonalityPct(false, "expansion", false, 125, 75), Is.EqualTo(100), "row ref_tier_personality off");

			Assert.That(RefTier.Target(2, 125), Is.EqualTo(3), "2 x 1.25 = 2.5 rounds up");
			Assert.That(RefTier.Target(4, 125), Is.EqualTo(5));
			Assert.That(RefTier.Target(6, 125), Is.EqualTo(8), "7.5 rounds up to 8");
			Assert.That(RefTier.Target(2, 75), Is.EqualTo(2), "rush before the radar: 1.5 rounds up, so the ref2 minimum of two stays");
			Assert.That(RefTier.Target(6, 100), Is.EqualTo(6));
			Assert.That(RefTier.Target(6, 400), Is.EqualTo(RefTier.MaxTarget), "clamped to 8");
			Assert.That(RefTier.Target(2, 0), Is.EqualTo(RefTier.MinTarget), "clamped to 1");
			Assert.That(RefTier.MinTarget, Is.EqualTo(1));
			Assert.That(RefTier.MaxTarget, Is.EqualTo(8));
		}

		[Test]
		public void TheLiftAndTheIntervalOutrankHoldOnlyBelowTheTargetAndOnlyWhileArmed()
		{
			Assert.That(RefTier.LiftsCap(true, 2, 0, 4), Is.True, "below the target");
			Assert.That(RefTier.LiftsCap(true, 3, 1, 4), Is.False, "the fourth is in production: the lift drops, no extra claim opens before it lands");
			Assert.That(RefTier.LiftsCap(true, 4, 0, 4), Is.False, "met");
			Assert.That(RefTier.LiftsCap(true, 5, 0, 4), Is.False, "above the target: nothing is sold, nothing is lifted");
			Assert.That(RefTier.LiftsCap(false, 0, 0, 4), Is.False, "row off");

			Assert.That(RefTier.OutranksIntervalWait(true, 3, 6, true), Is.True, "below the target with a claimable field");
			Assert.That(RefTier.OutranksIntervalWait(true, 3, 6, false), Is.False, "below the target with 0 anchors (Opus 11-26): the normal interval applies, no refinery is preferred that cannot be placed");
			Assert.That(RefTier.OutranksIntervalWait(true, 6, 6, true), Is.False);
			Assert.That(RefTier.OutranksIntervalWait(false, 0, 6, true), Is.False);
		}

		[Test]
		public void TheDevReasonNamesTheMapCapAndNeverMeansAHold()
		{
			Assert.That(RefTier.Reason(false, 0, 2, 5), Is.EqualTo("off"));
			Assert.That(RefTier.Reason(true, 4, 4, 0), Is.EqualTo("met"));
			Assert.That(RefTier.Reason(true, 2, 4, 3), Is.EqualTo("below_target"));
			Assert.That(RefTier.Reason(true, 2, 4, 0), Is.EqualTo("below_no_anchor"), "a map with no claimable field: reported once, nothing is held");
		}

		[Test]
		public void ThePlannerLiftsTheZoneCapsThroughTheMaximumAndSharesTheOwnQueuedCount()
		{
			var rd = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs");
			Assert.That(rd, Does.Contain("int EcoFloorLift() => Math.Max(Math.Max(EcoTimeFloorLift(), Ref2FirstLift()), RefTierLift());"));
			Assert.That(rd, Does.Contain("refTier != null && refTier.RefTierArmed"));
			Assert.That(rd, Does.Contain("refTierLift = RefTier.LiftsCap(true, own, queued, refTier.RefTierTarget) ? 1 : 0;"));
			Assert.That(rd.Split("OwnRefineriesStandingAndQueued(out var own, out var queued);").Length - 1, Is.EqualTo(2), "ref2 and the tier read the same standing + in-production pair");
			Assert.That(rd.Split("queued += inProduction;").Length - 1, Is.EqualTo(1), "one place counts it");
			Assert.That(rd, Does.Contain("if (!RefDemandArmed)\n\t\t\t\treturn 0;\n\n\t\t\tvar tick = world.WorldTick;\n\t\t\tif (refTierLiftTick == tick)"), "no law = no lift, one value per tick");

			// the lift reads the target only; the planner never reads a personality, a radar or a tech centre itself
			var lift = rd.Substring(rd.IndexOf("int RefTierLift()", StringComparison.Ordinal));
			lift = lift.Substring(0, lift.IndexOf("int EcoFloorLift()", StringComparison.Ordinal));
			foreach (var banned in new[] { "Personality", "Radar", "TechChain", "Sell", "Cancel" })
				Assert.That(lift, Does.Not.Contain(banned), banned);

			Assert.That(Source("mods/CA/Traits/BotModules/IBotRefTier.cs"), Does.Contain("bool RefTierArmed { get; }"));
			Assert.That(Source("mods/CA/Traits/BotModules/IBotRefTier.cs"), Does.Contain("int RefTierTarget { get; }"));
		}

		[Test]
		public void TheIntervalIsOutrankedBelowTheTargetButTheBoundedFirstCopyYieldAndTheFactoryWaitStay()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			var ref2 = qm.IndexOf("Ref2OutranksInterval(buildableThings, count))\n\t\t\t\treturn false;", StringComparison.Ordinal);
			var tier = qm.IndexOf("RefTierOutranksInterval(count))", StringComparison.Ordinal);
			var ret = qm.IndexOf("return verdict != RefineryFieldGateCA.EconomyVerdict.Allow;", tier, StringComparison.Ordinal);
			Assert.That(ref2, Is.GreaterThan(0));
			Assert.That(tier, Is.GreaterThan(ref2), "after the ref2 outrank, before the plain verdict");
			Assert.That(ret, Is.GreaterThan(tier));
			Assert.That(qm.Substring(tier - 80, 80), Does.Contain("verdict == RefineryFieldGateCA.EconomyVerdict.WaitInterval"), "only the interval wait is outranked (WaitFactory and YieldEssential come back unchanged)");

			var rt = Source(Dir + "BaseBuilderQueueManagerCA.RefTier.cs");
			// the outrank needs a claimable field: the same fieldUnderCap term as Ref2First.Due (law != null && an unclaimed anchor in reach)
			Assert.That(rt, Does.Contain("var fieldUnderCap = law != null && law.UnclaimedAnchorsInReach > 0;"));
			Assert.That(rt, Does.Contain("return RefTier.OutranksIntervalWait(true, count, baseBuilder.RefTierTarget, fieldUnderCap);"));
			Assert.That(rt, Does.Contain("if (!baseBuilder.RefTierArmed)\n\t\t\t\treturn false;"));
			Assert.That(rt, Does.Not.Contain("law == null ||"), "no law provider = no field verdict: no outrank");
			Assert.That(qm.Split("RefTierOutranksInterval(").Length - 1, Is.EqualTo(1), "one call site in the economy gate");
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs"), Does.Contain("var fieldUnderCap = law != null && law.UnclaimedAnchorsInReach > 0;"), "the term ref2's Due uses");

			Assert.That(rt, Does.Contain("var essential = count >= 2 && EssentialFirstCopyPending(buildableThings);"), "the yield never applies below the ref2 minimum of two");
			Assert.That(rt, Does.Contain("baseBuilder.RefineryGateLastRiseTick, 0, essential, baseBuilder.RefineryGateYieldSince, baseBuilder.Info.RefineryEssentialYieldTicks);"), "the gate's own bounded yield clock, interval 0");
			Assert.That(rt, Does.Not.Contain("Cancel"));
			Assert.That(rt, Does.Not.Contain("Sell"));
			Assert.That(rt, Does.Not.Contain("QueueOrder"));

			// a pure verdict check of the same call: interval outranked, yield kept
			Assert.That(RefineryFieldGateCA.Economy(true, 3, false, true, true, true, 1000, 900, 0, true, 950, 1500), Is.EqualTo(RefineryFieldGateCA.EconomyVerdict.YieldEssential), "3rd refinery, a first copy pending: the yield holds");
			Assert.That(RefineryFieldGateCA.Economy(true, 3, false, true, true, true, 3000, 900, 0, true, 950, 1500), Is.EqualTo(RefineryFieldGateCA.EconomyVerdict.Allow), "the yield ends after its bound");
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, true, true, 1000, 900, 0, false, -1, 1500), Is.EqualTo(RefineryFieldGateCA.EconomyVerdict.Allow), "below the ref2 minimum nothing yields");
		}

		[Test]
		public void TheTargetComesFromCompletedStandingBuildingsOnlyAndNothingIsHeldOrSold()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool RefTierArmed => Info.RefTierFloor && switchesActive && !ablateRefTier.Off(player, AblationIds.RefTierFloor);"));
			Assert.That(bb, Does.Contain("bool RefTierPersonalityArmed => RefTierArmed && !ablateRefTierPersonality.Off(player, AblationIds.RefTierPersonality);"));
			Assert.That(bb, Does.Contain("public bool RefTierRadarStanding => radarBuildings != null && AIUtils.CountActorByCommonName(radarBuildings) > 0;"), "completed own radar providers (the actor index: a building in production is not an actor)");
			Assert.That(bb, Does.Contain("public bool RefTierTechStanding => techChainBuildings != null && AIUtils.CountActorByCommonName(techChainBuildings) > 0;"));
			Assert.That(bb, Does.Contain("RefTier.BaseTarget(Info.RefTierEarlyTarget, Info.RefTierRadarStep, Info.RefTierTechStep, radar, RefTierTechStanding)"), "recomputed from what stands now: a lost tier drops the target");
			Assert.That(bb, Does.Contain("if (refTierTick >= 0 && tick - refTierTick < RefTier.RecomputeTicks)\n\t\t\t\t\treturn refTierValue;"), "cached for RecomputeTicks only (the two index counts are not free)");
			Assert.That(RefTier.RecomputeTicks, Is.EqualTo(25));
			Assert.That(bb, Does.Contain("RefTierPersonalityPct(radar));"), "the radar flag is read once per recompute");
			Assert.That(bb, Does.Contain("if (info.RefTierFloor)\n\t\t\t{\n\t\t\t\t// REF-TIER-01"), "the two indexes exist only while the switch is on");
			Assert.That(bb, Does.Contain("BaseBuilderQueueManagerCA.IsRadarProvider(a)"), "the same radar test as the opening order");
			Assert.That(bb, Does.Contain("IBotPlacementVeto, IBotRef2First, IBotRefTier"));

			// the radar / production hold is still ref2's alone: the tier adds no hold anywhere in the pick path
			var pipeline = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(pipeline, Does.Not.Contain("RefTier"));
			var ref2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");
			Assert.That(ref2, Does.Not.Contain("RefTier"));
			var rt = Source(Dir + "BaseBuilderQueueManagerCA.RefTier.cs");
			Assert.That(rt.Split("return").Length - 1, Is.EqualTo(5), "the outrank (off, decision), the yield verdict, and the two early outs of the DEV record: nothing else returns a hold");
		}

		[Test]
		public void TheDevRowIsDevOnlySpacedAndDedupedAndTheObserverMemberHasADefault()
		{
			var rt = Source(Dir + "BaseBuilderQueueManagerCA.RefTier.cs");
			var at = rt.IndexOf("internal void RefTierObserve(", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(0));
			Assert.That(rt.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), "compiled out of a release build");
			Assert.That(rt, Does.Contain("refTierNextObserve = now + 25;"), "spaced");
			Assert.That(rt, Does.Contain("o.RefTierVerdict(player, now, reason, detail, target, count, Category);"));
			Assert.That(rt, Does.Contain("anchor_{(anchors > 0 ? 1 : 0)}"), "the anchor COUNT is not in the key: only a change of whether a field can be claimed is a change");

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			var dev = bb.IndexOf("void RefTierObserveDev()", StringComparison.Ordinal);
			Assert.That(dev, Is.GreaterThan(0));
			Assert.That(bb.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", dev, StringComparison.Ordinal), Is.InRange(dev - 80, dev - 1));
			Assert.That(bb, Does.Contain("// REF-TIER-01 (DEV only): one record per change of the tier state.\n\t\t\tRefTierObserveDev();"));

			Assert.That(Source("mods/CA/Traits/BotModules/IBotBuildingPickObserver.cs"),
				Does.Contain("void RefTierVerdict(Player owner, int tick, string verdict, string detail, int target, int refineries, string queue) { }"), "a default member");

			var dv = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			var from = dv.IndexOf("void IBotBuildingPickObserver.RefTierVerdict(", StringComparison.Ordinal);
			Assert.That(from, Is.GreaterThan(0));
			var row = dv.Substring(from, dv.IndexOf("static string DemandCell", from, StringComparison.Ordinal) - from);
			Assert.That(row, Does.Contain("ObserverRow(owner, \"ref_tier\""));
			Assert.That(row, Does.Contain("cov.LastRefTierKey == key"), "one row per change, never repeated");
			Assert.That(row, Does.Not.Contain("RefineryHeldRepeatTicks"));
			foreach (var field in new[] { "AppendString(b, \"verdict\", verdict)", "AppendString(b, \"detail\", detail)", "AppendNumber(b, \"target\", target)", "AppendNumber(b, \"refineries\", refineries)", "AppendString(b, \"queue\", queue)" })
				Assert.That(row, Does.Contain(field), field);
		}

		[Test]
		public void OffInCodeTrueInTheGenericRowsAndTheRowsAreRegistered()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.RefTierFloor, Is.False, "default false in code");
			Assert.That(info.RefTierEarlyTarget, Is.EqualTo(2));
			Assert.That(info.RefTierRadarStep, Is.EqualTo(2));
			Assert.That(info.RefTierTechStep, Is.EqualTo(2));
			Assert.That(info.RefTierExpansionPct, Is.EqualTo(125));
			Assert.That(info.RefTierRushPreRadarPct, Is.EqualTo(75));

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value;
			string Field(string module) => player.Nodes.Single(n => n.Key == module).Value.Nodes.SingleOrDefault(n => n.Key == "RefTierFloor")?.Value.Value?.Trim();
			Assert.That(Field("BaseBuilderBotModuleCA@generic"), Is.EqualTo("true"), "true in @generic");
			Assert.That(Field("BaseBuilderBotModuleCA@classic"), Is.Null, "classic stays as before");

			Assert.That(AblationIds.RefTierFloor, Is.EqualTo("ref_tier_floor"));
			Assert.That(AblationIds.RefTierPersonality, Is.EqualTo("ref_tier_personality"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_tier_floor"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_tier_personality"));
		}
	}
}
