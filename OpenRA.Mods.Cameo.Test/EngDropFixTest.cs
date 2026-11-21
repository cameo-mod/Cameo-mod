#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// ENGDROP stage 2 fixes (Haiku FIX 82C36CE7): F1 fail-closed constraint table, F2 no-production bound into the mission, N1 the vetted
	/// route bound to the commit, N2 the defended-target prefilter, N3 the plan throttle. The provider side (ConstraintsAllow, AbortRun,
	/// the no-production path, RecheckDropRuns) has no headless World fixture in this project; those are pinned on the source text, the pure
	/// parts are tested directly.
	/// </summary>
	[TestFixture]
	public sealed class EngDropFixTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Frans() => Source("mods/Fransbot/Traits/FransTransportCommanderBotModule.cs");

		static string Engineer() => Source("mods/Cameo/Traits/BotModules/EngineerBotModule.cs");

		static string Section(string text, string from, string to)
		{
			var a = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(a, Is.GreaterThanOrEqualTo(0), from);
			var b = text.IndexOf(to, a + from.Length, StringComparison.Ordinal);
			Assert.That(b, Is.GreaterThan(a), to);
			return text.Substring(a, b - a);
		}

		static AreaThreat Threat(int x, bool antiAir = false) => new(new CPos(x, 0), 100, 6, true, false, antiAir, false, 0);

		static WPos Centre(CPos c) => new(c.X * 1024 + 512, c.Y * 1024 + 512, 0);

		// ---- F1: more threats than the table carries means NO constraints (fail closed), never a clipped table with holes ----

		[Test]
		public void TheConstraintTableIsExactAtTheCapAndNullAboveIt()
		{
			var atCap = Enumerable.Range(0, EngineerBotModule.MaxDropZones).Select(i => Threat(i)).ToList();
			var constraints = EngineerBotModule.ConstraintsFor(atCap, Centre, 7);
			Assert.That(constraints, Is.Not.Null);
			Assert.That(constraints.Zones, Has.Count.EqualTo(EngineerBotModule.MaxDropZones));
			Assert.That(constraints.ObservedTick, Is.EqualTo(7));

			var over = Enumerable.Range(0, EngineerBotModule.MaxDropZones + 1).Select(i => Threat(i)).ToList();
			Assert.That(EngineerBotModule.ConstraintsFor(over, Centre, 7), Is.Null, "257 zones: refuse, do not drop the 257th");
		}

		[Test]
		public void TheConstraintTableKeepsTheKindAndEveryZone()
		{
			var constraints = EngineerBotModule.ConstraintsFor([Threat(1), Threat(5, antiAir: true)], Centre, 0);
			Assert.That(constraints.Zones.Select(z => z.Kind), Is.EqualTo(new[] { DropExclusionKind.Ground, DropExclusionKind.AntiAir }));
			Assert.That(constraints.RejectsPoint(Centre(new CPos(5, 0)), air: true), Is.True, "an anti-air zone bars aircraft");
			Assert.That(constraints.RejectsPoint(Centre(new CPos(40, 0)), air: true), Is.False);
		}

		[Test]
		public void TriggerARefusesWhenThereAreNoConstraints()
		{
			var run = Section(Engineer(), "bool TryOpportunityRun(", "void RecheckDropRuns(");
			Assert.That(run, Does.Contain("if (constraints == null || !provider.TryPlanCaptureRoute("));
			Assert.That(Engineer(), Does.Not.Contain("Take(256)"));
		}

		// ---- F2: the no-production rule travels with the mission and gates the production request itself ----

		[Test]
		public void ANoProductionRunNeverRequestsATransportFromAnyCaller()
		{
			var frans = Frans();
			var request = Section(frans, "void RequestBestTransport(IBot bot, TransportMission mission, bool landPathAvailable)", "var unitBuilder");
			Assert.That(request, Does.Contain("if (mission.NoProduction)"));
			Assert.That(request.IndexOf("if (mission.NoProduction)", StringComparison.Ordinal), Is.LessThan(request.IndexOf("playerResources == null", StringComparison.Ordinal)));
			Assert.That(frans, Does.Contain("NoProduction = !queueProduction,"));
			Assert.That(frans, Does.Contain("public bool NoProduction;"));
		}

		// ---- N1: the vetted route is bound to the commit (passengers, craft, plan) ----

		[Test]
		public void TheCommitRefusesUnlessTheCraftPlansTheVettedJourney()
		{
			var core = Section(Frans(), "bool TryRequestCaptureRunCore(", "bool IFransCaptureTransportService.TryConsumeDelivered");
			var bind = core.IndexOf("vettedRoute != null &&", StringComparison.Ordinal);
			Assert.That(bind, Is.GreaterThan(0));
			Assert.That(core, Does.Contain("FindStartableCraft(passengers, targets, constraints ?? DropConstraints.None, out var vetCraft, out var vetPlan)"));
			Assert.That(core, Does.Contain("!vetJourney.SequenceEqual(vettedRoute)"));
			Assert.That(bind, Is.LessThan(core.IndexOf("BotUnitLeases.Transfer(", StringComparison.Ordinal)), "refused before any lease or state is touched");
		}

		[Test]
		public void TriggerACommitsOnlyTheVettedPassengerWithTheVettedRoute()
		{
			var run = Section(Engineer(), "int TryTransportRun(", "string runId = null;");
			Assert.That(run, Does.Contain("var bindBroken = opportunity && (passengers.Count != 1 || passengers[0] != candidates[0]);"));
			var engineer = Engineer();
			Assert.That(engineer, Does.Contain("allowProduction, opportunity ? vettedRoute : null, out runId)"));
			Assert.That(engineer, Does.Contain("allowProduction: false, constraints, opportunity: true, samples, journey) > 0;"));
		}

		// ---- N2: defended targets leave the candidate list BEFORE the CaptureTargetTries cut ----

		[Test]
		public void TheDefendedTargetPrefilterRunsBeforeTheTryCut()
		{
			var scheduled = Section(Engineer(), "bool TryScheduledDrop(IBot bot, bool opportunity)", "bool TryOpportunityRun(");
			var filter = scheduled.IndexOf("if (opportunity)\n\t\t\t\tranked = ranked.Where(", StringComparison.Ordinal);
			var cut = scheduled.IndexOf("ranked.Take(Math.Max(1, Info.CaptureTargetTries))", StringComparison.Ordinal);
			Assert.That(filter, Is.GreaterThan(0));
			Assert.That(cut, Is.GreaterThan(filter));
			Assert.That(scheduled, Does.Contain(".ThreatValue == 0);"));
		}

		// ---- N3: one plan per target per DropPlanRetryTicks ----

		[Test]
		public void ATargetIsPlannedAgainOnlyAfterTheRetryInterval()
		{
			Assert.That(EngineerBotModule.PlanDue(1000, 0, 100), Is.True, "never planned");
			Assert.That(EngineerBotModule.PlanDue(1000, 950, 100), Is.False);
			Assert.That(EngineerBotModule.PlanDue(1049, 950, 100), Is.False);
			Assert.That(EngineerBotModule.PlanDue(1050, 950, 100), Is.True);
			Assert.That(EngineerBotModule.PlanDue(1050, 1050, 0), Is.True, "a zero interval plans every scan");
		}

		[Test]
		public void TheThrottleStampsTheTargetBeforePlanning()
		{
			var run = Section(Engineer(), "bool TryOpportunityRun(", "void RecheckDropRuns(");
			var stamp = run.IndexOf("dropPlanTick[target.ActorID] = Math.Max(1, world.WorldTick);", StringComparison.Ordinal);
			var plan = run.IndexOf("TryPlanCaptureRoute(", StringComparison.Ordinal);
			Assert.That(stamp, Is.GreaterThan(0));
			Assert.That(stamp, Is.LessThan(plan), "a refused or failed plan counts as a plan");
		}

		// ---- F3: pins on the provider-side members that have no headless fixture ----

		[Test]
		public void TheConstraintCheckRunsBeforeACraftIsReservedAndTheAbortIsGenerationBound()
		{
			var frans = Frans();
			var assign = Section(frans, "bool ConstraintsAllow(TransportMission mission, Actor transport, TransportPlan plan)", "\n\t\t}");
			Assert.That(assign, Does.Contain("mission.Constraints"));
			Assert.That(frans, Does.Contain("static string RunIdentity(TransportMission mission) => mission.RunMissionId + \"#\" + mission.RunGeneration;"));
			var abort = Section(frans, "bool AbortRun(", "\n\t\t}");
			Assert.That(abort, Does.Contain("RunIdentity"), "a stale generation is refused");
			Assert.That(abort, Does.Contain("AbortRequested"), "idempotent: one honoured request");
		}
	}
}
