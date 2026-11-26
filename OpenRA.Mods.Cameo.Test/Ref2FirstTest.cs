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
	// REF2-FIRST-01 (USER BINDING 11-26, Opus rulings; one switch Ref2First, rows ref2_before_radar, ref2_before_prod2, ref2_floor): the pure decisions, the two personality exemptions,
	// the skip when no reachable field has room, the bound, and the wiring pins (off = identical).
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
		public void TheBoundEndsTheHoldSoTheOrderNeverDeadlocks()
		{
			Assert.That(Ref2First.BoundExpired(-1, 99999, 3000), Is.False, "never due");
			Assert.That(Ref2First.BoundExpired(1000, 4000, 3000), Is.False, "exactly the bound");
			Assert.That(Ref2First.BoundExpired(1000, 4001, 3000), Is.True);
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
			Assert.That(r2, Does.Contain("var fieldUnderCap = law == null || law.UnclaimedAnchorsInReach > 0;"), "no reachable field with room = skip");

			// the planner lifts the zone cap by one while fewer than two own refineries stand; the time floor stays as it is
			var rd = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs");
			Assert.That(rd, Does.Contain("int EcoFloorLift() => Math.Max(EcoTimeFloorLift(), Ref2FirstLift());"));
			Assert.That(rd, Does.Contain("ref2First != null && ref2First.Ref2FloorArmed"));
			Assert.That(rd, Does.Contain("ref2Lift = own < Ref2First.RefineryTarget ? 1 : 0;"));
			Assert.That(rd, Does.Contain("own += inProduction;"), "own standing PLUS in production: with ref2 queued the lift drops, so no 3rd claim opens before ref2 lands");
			Assert.That(rd, Does.Contain("foreach (var r in builder.Info.RefineryTypes)"));
		}

		[Test]
		public void TheDevVerdictRowIsDevOnlyDedupedAndTheObserverMemberHasADefault()
		{
			var r2 = Source(Dir + "BaseBuilderQueueManagerCA.Ref2First.cs");
			var at = r2.IndexOf("void Ref2Observed(", StringComparison.Ordinal);
			Assert.That(at, Is.GreaterThan(0));
			Assert.That(r2.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), "compiled out of a release build");
			foreach (var verdict in new[] { "\"held_radar\"", "\"held_prod2\"", "\"bound_expired\"", "\"not_due\"", "\"due\"" })
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
			foreach (var id in new[] { "ref2_before_radar", "ref2_before_prod2", "ref2_floor" })
				Assert.That(AblationSpec.RegistryIds(), Does.Contain(id));

			// off = identical: every armed property needs the switch, and the planner lift is 0 without it
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool Ref2RadarArmed => Info.Ref2First && switchesActive && !ablateRef2Radar.Off(player, AblationIds.Ref2BeforeRadar);"));
			Assert.That(bb, Does.Contain("public bool Ref2Prod2Armed => Info.Ref2First && switchesActive && !ablateRef2Prod2.Off(player, AblationIds.Ref2BeforeProd2);"));
			Assert.That(bb, Does.Contain("public bool Ref2FloorArmed => Info.Ref2First && switchesActive && !ablateRef2Floor.Off(player, AblationIds.Ref2Floor);"));
		}
	}
}
