// FREEZE10 DIAG-FIX-01 (Sonnet; Opus order from SMOKE04_DIAG_01): (a) the idle flee decision counts engaged allies as the in-fight rule does,
// (b) the MCV guard abort goes home, Eligible refuses fields in the enemy half, A9 MCV defaults 8 -> 14 and 6000 -> 18000, EX-3 zone logging.
// Every change is behind the CG switch (AggTeamEnabled): the pure rules are tested directly, the world wiring (no headless world fixture) by source pins,
// and the OFF identity by the seam returning its input and by the guards that keep the new code behind the CG-only context.
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class DiagFix01Test
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static CPos C(int x, int y) => new(x, y);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Planner() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");

		static string Safe() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.McvSafe.cs");

		static string GroundStates() => Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static SquadManagerBotModuleCA Shell(bool aggTeam)
		{
			var info = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(nameof(SquadManagerBotModuleCAInfo.AggTeamEnabled)).SetValue(info, aggTeam);
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			typeof(SquadManagerBotModuleCA).BaseType.GetField("Info", Any).SetValue(m, info);
			return m;
		}

		// ---- (a) idle path counts engaged allies ----

		[TestCase(0.30, AggressionBandCA.Regroup, "own coalition 30%, allies lift it to 30% (flee 25): regroup")]
		[TestCase(0.60, AggressionBandCA.Regroup, "ratio 60%: regroup, never straight into an attack")]
		[TestCase(2.50, AggressionBandCA.Regroup, "a huge allied ratio is still only a regroup")]
		[TestCase(0.25, AggressionBandCA.Regroup, "exactly FleeRatio is not below it")]
		[TestCase(0.24, AggressionBandCA.Flee, "still below FleeRatio with the allies: flees")]
		[TestCase(0.0, AggressionBandCA.Flee, "no strength at all: flees")]
		public void AFleeBandIdleSquadIsRegroupedOnlyWhenTheAlliedRatioReachesFleeRatio(double alliedRatio, AggressionBandCA expected, string why)
		{
			Assert.That(AggressionPolicyCA.IdleBandWithAllies(AggressionBandCA.Flee, alliedRatio, 25, 80), Is.EqualTo(expected), why);
		}

		[TestCase(AggressionBandCA.Regroup)]
		[TestCase(AggressionBandCA.Attack)]
		public void ANonFleeBandIsNeverChangedByTheAlliedRatio(AggressionBandCA band)
		{
			foreach (var ratio in new[] { 0.0, 0.3, 0.9, 5.0 })
				Assert.That(AggressionPolicyCA.IdleBandWithAllies(band, ratio, 25, 80), Is.EqualTo(band));
		}

		[Test]
		public void AnInvertedYamlPairStillCapsTheFleeBarAtTheEngageBar()
		{
			// Flee 90 > engage 70 is treated as 70 by Band: 0.7 is then outside the flee band.
			Assert.That(AggressionPolicyCA.IdleBandWithAllies(AggressionBandCA.Flee, 0.7, 90, 70), Is.EqualTo(AggressionBandCA.Regroup));
			Assert.That(AggressionPolicyCA.IdleBandWithAllies(AggressionBandCA.Flee, 0.69, 90, 70), Is.EqualTo(AggressionBandCA.Flee));
		}

		[Test]
		public void TheAlliedRatioSeamReturnsItsInputUntouchedWithCgOff()
		{
			// OFF identity: with the CG switch off the seam reads nothing (null squad, friends and enemies are never touched) and returns the ratio as given.
			var off = Shell(false);
			foreach (var band in new[] { AggressionBandCA.Flee, AggressionBandCA.Regroup, AggressionBandCA.Attack })
				foreach (var ratio in new[] { 0.0, 0.1, 0.49, 1.0, 3.5 })
					Assert.That(off.AggressionIdleAlliedBand(null, null, null, band, ratio), Is.EqualTo((band, ratio)));
		}

		[Test]
		public void TheSeamReadsTheCgSwitchFirstAndTheIdleSiteNamesNoAggTeamType()
		{
			var seam = Section(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Aggression.cs"),
				"internal (AggressionBandCA Band, double Ratio) AggressionIdleAlliedBand(", "/// <summary>A2: start (or restart) the commit");
			Assert.That(seam, Does.Contain("{\n\t\t\tif (!Info.AggTeamEnabled || band != AggressionBandCA.Flee)\n\t\t\t\treturn (band, ratio);\n"), "the first statement is the CG and flee-band gate");
			Assert.That(seam, Does.Contain("AggressionAllyEngagedUnits(squad)").And.Contain("AggressionRatio(squad, friends.Concat(engaged), enemies)").And.Contain("AggressionPolicyCA.IdleBandWithAllies(band, withAllies,"),
				"the same engaged-ally set and ratio as AggressionHoldsInFight");
			Assert.That(GroundStates(), Does.Not.Contain("AggTeam"), "GroundStatesCA keeps its no-AggTeam invariant");
		}

		[Test]
		public void TheIdleTickAsksTheSeamOnlyInTheFleeBandAndBeforeTheDecisionAndTheEvidence()
		{
			var tick = Section(GroundStates(), "void AggressionTick(SquadCA owner, List<Actor> enemyUnits)", "void RegroupForward(");
			var band = tick.IndexOf("var band = AggressionPolicyCA.Band(ratio,", StringComparison.Ordinal);
			var guard = tick.IndexOf("if (band == AggressionBandCA.Flee)\n\t\t\t\t(band, ratio) = manager.AggressionIdleAlliedBand(owner, friends, enemyUnits, band, ratio);", StringComparison.Ordinal);
			var decision = tick.IndexOf("var decision = AggressionPolicyCA.IdleDecision(", StringComparison.Ordinal);
			var evidence = tick.IndexOf("manager.AggressionRecordEvidence(owner, band, ratio, enemyUnits, null);", StringComparison.Ordinal);
			Assert.That(band, Is.GreaterThanOrEqualTo(0));
			Assert.That(guard, Is.GreaterThan(band));
			Assert.That(decision, Is.GreaterThan(guard), "the decision uses the re-judged band");
			Assert.That(evidence, Is.GreaterThan(decision), "the retreat_start row records the ratio that decided");
			Assert.That(tick, Does.Not.Contain("IdleAlliedRatio"));
		}

		// ---- (b) A9 defaults ----

		[Test]
		public void TheMcvAbortThreatAndLossZoneDefaultsAreTheDiagFixValuesInsideTheirRanges()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(info.AggTeamMcvAbortThreatCells, Is.EqualTo(14));
			Assert.That(info.AggTeamMcvLossZoneTicks, Is.EqualTo(18000));
			Assert.That(info.AggTeamMcvAbortThreatCells, Is.InRange(1, 30));
			Assert.That(info.AggTeamMcvLossZoneTicks, Is.InRange(0, 30000));
			Assert.That(info.AggTeamMcvLossZoneCells, Is.EqualTo(16), "the zone radius is not part of this change");
			Assert.That(AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.AggTeamMcvAbortThreatCells), "allies", "soviet"), Is.EqualTo(14));
			Assert.That(AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.AggTeamMcvLossZoneTicks), null, null), Is.EqualTo(18000));
			Assert.That(info.AggTeamEnabled, Is.False, "the switch itself stays off by default");
		}

		// ---- (b) the abort goes home ----

		[Test]
		public void AnAbortedMcvMovesToTheBaseAndOnlyStopsWhenNoBaseIsKnown()
		{
			var tick = Section(Safe(), "void McvSafeTick(IBot bot)", "bool McvSafeAbortHome(");
			var home = tick.IndexOf("if (!McvSafeAbortHome(bot, policy, zones, now, mcv))\n\t\t\t\t\tbot.QueueOrder(new Order(\"Stop\", mcv, false));", StringComparison.Ordinal);
			var park = tick.IndexOf("parkedUntil[track.Field] = now + Info.ParkTicks;", StringComparison.Ordinal);
			Assert.That(home, Is.GreaterThanOrEqualTo(0), "the Stop is only the fallback of the move home");
			Assert.That(park, Is.GreaterThan(home), "the field is still parked");
			Assert.That(tick, Does.Contain("inflightMcvSites.Remove(mcv.ActorID);").And.Contain("MS-3 MCV"));

			var abort = Section(Safe(), "bool McvSafeAbortHome(", "void McvSafeVanished(");
			Assert.That(abort, Does.Contain("if (!baseCenter.HasValue)\n\t\t\t\treturn false;"));
			Assert.That(abort, Does.Contain("McvSafeRoute(ctx, mcv, baseCenter.Value, false, out var route);"), "the zone-costed route, any path accepted");
			Assert.That(abort, Does.Contain("new Order(\"Move\", mcv, OpenRA.Traits.Target.FromCell(world, wp), queued)"));
			Assert.That(abort, Does.Contain("if (!queued)\n\t\t\t\tbot.QueueOrder(new Order(\"Move\", mcv, OpenRA.Traits.Target.FromCell(world, baseCenter.Value), false));"), "no route: straight to the base cell");
			Assert.That(abort, Does.Not.Contain("\"Stop\""));
			Assert.That(abort, Does.Contain("MS-5 MCV"));
		}

		[Test]
		public void TheGuardStillRunsOnlyWithCgOn()
		{
			var tick = Section(Safe(), "void McvSafeTick(IBot bot)", "bool McvSafeAbortHome(");
			Assert.That(tick, Does.Contain("t => t.McvSafeEnabled").And.Contain("mcvTracks.Clear();"));
			Assert.That(tick.IndexOf("if (policy == null)", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("McvSafeAbortHome(", StringComparison.Ordinal)));
		}

		// ---- (b) Eligible refuses the enemy half ----

		[TestCase(60, 60, true, "the site is on the enemy start (0 against 800 squared)")]
		[TestCase(30, 30, false, "nearer to the own base")]
		[TestCase(50, 50, false, "an exact tie is not nearer")]
		public void AFieldIsRefusedOnlyWhenStrictlyNearerToAnEnemyStartThanToEveryOwnBase(int x, int y, bool expected, string why)
		{
			var enemy = new[] { C(60, 60) };
			var own = new[] { C(40, 40) };
			// (50,50) is 200 from each (squared: 10^2 + 10^2), a tie.
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(x, y), enemy, own), Is.EqualTo(expected), why);
		}

		[Test]
		public void TheNearestOwnBaseCountsSoAnExpansionYardProtectsItsOwnField()
		{
			var enemy = new[] { C(100, 20) };
			var main = C(10, 20);
			var expansion = C(80, 20);
			var site = C(75, 22);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(site, enemy, new[] { main }), Is.True, "against the main base alone it is in the enemy half");
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(site, enemy, new[] { main, expansion }), Is.False, "an own expansion yard beside it keeps it");
		}

		[Test]
		public void TheNearestOfSeveralEnemyStartsCounts()
		{
			var own = new[] { C(10, 10) };
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(70, 70), new[] { C(120, 120), C(80, 80) }, own), Is.True);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(20, 20), new[] { C(120, 120), C(80, 80) }, own), Is.False);
		}

		[Test]
		public void WithNoKnownEnemyStartOrNoOwnBaseNothingIsRefused()
		{
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(5, 5), Array.Empty<CPos>(), new[] { C(100, 100) }), Is.False);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(5, 5), null, new[] { C(100, 100) }), Is.False);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(5, 5), new[] { C(6, 6) }, Array.Empty<CPos>()), Is.False);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(5, 5), new[] { C(6, 6) }, null), Is.False);
		}

		[Test]
		public void TheDistanceIsExactIntegerMathEvenAcrossAHugeMap()
		{
			// 2 * 30000^2 overflows int; the comparison must stay exact.
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(30000, 30000), new[] { C(30001, 30000) }, new[] { C(0, 0) }), Is.True);
			Assert.That(McvSafePolicyCA.NearerToEnemyStart(C(30000, 30000), new[] { C(0, 0) }, new[] { C(30001, 30000) }), Is.False);
		}

		[Test]
		public void EligibleAppliesTheEnemyHalfRefusalOnlyThroughTheCgContextAndAfterTheZoneCheck()
		{
			var site = Section(Planner(), "CPos? IBotMcvExpansionSiteProvider.McvExpansionSite(Actor mcv)", "bool ScalingDeployCellExists(");
			var zone = site.IndexOf("if (safe != null && (rejected.Contains(f.Index) || !McvSafeSiteAllowed(safe, f)))", StringComparison.Ordinal);
			var half = site.IndexOf("if (safe != null && McvSafeSiteInEnemyHalf(safe, f))\n\t\t\t\t\treturn false;", StringComparison.Ordinal);
			var parked = site.IndexOf("parkedUntil.TryGetValue(f.Index, out var until)", StringComparison.Ordinal);
			Assert.That(zone, Is.GreaterThanOrEqualTo(0));
			Assert.That(half, Is.GreaterThan(zone));
			Assert.That(parked, Is.GreaterThan(half), "with CG off Eligible still starts at the parked-field check");
			Assert.That(System.Text.RegularExpressions.Regex.Matches(Planner(), Regex("McvSafeSiteInEnemyHalf(")).Count, Is.EqualTo(1), "one call site, the guarded one");
		}

		[Test]
		public void TheEnemyHalfCheckComparesAgainstPublicStartPointsAndOwnBuildingsOnly()
		{
			var half = Section(Safe(), "bool McvSafeSiteInEnemyHalf(", "string McvSafeZoneNote(");
			foreach (var guard in new[]
			{
				"d.Value.Value == \"mpspawn\"", "p == player || player.IsAlliedWith(p)",
				"var matchCells = ctx.P(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnMatchCells));",
				"ArmyStagingEval.EnemySpawnCandidates(mcvStarts, homes, matchCells)",
				"a.Owner == player && a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<BaseBuildingInfo>()", "ctx.OwnBases.Add(baseCenter.Value);",
				"McvSafePolicyCA.NearerToEnemyStart(f.Center, ctx.EnemyStarts, ctx.OwnBases)", "MS-6 field",
			})
				Assert.That(half, Does.Contain(guard), guard);
			Assert.That(half, Does.Not.Contain("Spectating"), "the real enemy assignment is not read");
			Assert.That(half, Does.Not.Contain("Rand").And.Not.Contain("Random"), "no RNG");
			Assert.That(half, Does.Not.Contain("double").And.Not.Contain("Math.Sqrt"), "integer math only");
		}

		// ---- (b) EX-3 zone logging ----

		[Test]
		public void TheEx3LineCarriesTheZoneStateOnlyWithCgOn()
		{
			var site = Section(Planner(), "CPos? IBotMcvExpansionSiteProvider.McvExpansionSite(Actor mcv)", "bool ScalingDeployCellExists(");
			Assert.That(site, Does.Contain("hand-out {mcvHandout.Count}{(safe != null ? McvSafeZoneNote(safe, mcv, s) : string.Empty)} at tick {tick}\");"),
				"with CG off the suffix is empty and the line is byte-identical to FREEZE9");
			var note = Section(Safe(), "string McvSafeZoneNote(", "bool McvSafeAbortHome(").Split(new[] { "\n\t\t/// <summary>" }, StringSplitOptions.None)[0];
			foreach (var guard in new[] { "ctx.Zones", "mcvLossZones.Count", "ctx.EnemyStarts?.Count", "field edge", "mcv edge", "McvSafePolicyCA.EdgeDistanceCells(f.Center", "McvSafePolicyCA.EdgeDistanceCells(mcv.Location" })
				Assert.That(note, Does.Contain(guard), guard);
		}

		static string Regex(string literal) => System.Text.RegularExpressions.Regex.Escape(literal);
	}
}
