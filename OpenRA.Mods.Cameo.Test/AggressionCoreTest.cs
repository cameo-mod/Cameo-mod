#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

// PLAYTEST-PORT (Opus ruling, 2026-11-10): the yaml-OFF guard assertions in this file were flipped to the ON values
// that the ported mods/cameo/ai/ai.yaml carries (switches armed on the six personalities; classic unchanged).
// Test names are unchanged. The fixture mcv_deploy_ai.yaml is the build-time copy of mods/cameo/ai/ai.yaml (csproj Link).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL02-AGG-CORE (AGGRESSION_PLAN_01 A1-A3 and the A4 no-abandonment part), switch-ON path only.
	// Evidence: the FREEZE7 user game had 1143 retreats against 339 squads formed, 99% of them idle_engage at a median squad
	// size of 2. The decisions live in AggressionPolicyCA (tested on state); the world call sites are pinned in the source.
	// The OFF arm: AggressionOn is false unless StalemateBreakerEnabled (group CD) and AggressionEnabled (group CF, Opus S4)
	// are both set by the ON arm, and each call site is behind it.
	[TestFixture]
	public class AggressionCoreTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string GroundStates() => Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static int Count(string text, string needle) => Regex.Matches(text, Regex.Escape(needle)).Count;

		// ---- the ON gate (OFF-arm identity) ----

		[Test]
		public void TheGateIsOpenOnlyWithTheBreakerSwitchTheKillSwitchAndThePredictor()
		{
			foreach (var breaker in new[] { false, true })
				foreach (var aggression in new[] { false, true })
					foreach (var predictor in new[] { false, true })
						Assert.That(AggressionPolicyCA.Enabled(breaker, aggression, predictor), Is.EqualTo(breaker && aggression && predictor),
							$"breaker={breaker} aggression={aggression} predictor={predictor}");
		}

		[Test]
		public void TheOffArmDefaultsKeepTheGateClosed()
		{
			// The OFF arm never sets StalemateBreakerEnabled (default false): the gate stays closed whatever the other values are.
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(info.StalemateBreakerEnabled, Is.False);
			Assert.That(AggressionPolicyCA.Enabled(info.StalemateBreakerEnabled, info.AggressionEnabled, true), Is.False);
			Assert.That(info.AggressionEnabled, Is.False, "group CF_agg_core sets it on the ON arm only (Opus S4)");
			Assert.That(AggressionPolicyCA.Enabled(true, info.AggressionEnabled, true), Is.False, "CD alone does not open agg-core");
		}

		[Test]
		public void TheParametersAreNamedValuesWithThePlanDefaults()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(info.AggressionEngageRatioPct, Is.EqualTo(80));
			Assert.That(info.AggressionFleeRatioPct, Is.EqualTo(50));
			Assert.That(info.AggressionMinCommitTicks, Is.EqualTo(250));
			Assert.That(info.AggressionCommitBreakLossPct, Is.EqualTo(50));
			Assert.That(info.AggressionMinDispatchUnits, Is.EqualTo(3));
			Assert.That(info.AggressionMinDispatchValue, Is.EqualTo(1000));
			Assert.That(info.AggressionCoalitionCells, Is.EqualTo(12));
			Assert.That(info.AggressionAllyEngagedCells, Is.EqualTo(20));
			Assert.That(info.AggressionRegroupMarginCells, Is.EqualTo(4));
			Assert.That(info.AggressionRegroupMaxTicks, Is.EqualTo(750));
		}

		// ---- A2 bands ----

		[TestCase(0.0, AggressionBandCA.Flee)]
		[TestCase(0.49, AggressionBandCA.Flee)]
		[TestCase(0.5, AggressionBandCA.Regroup)]
		[TestCase(0.79, AggressionBandCA.Regroup)]
		[TestCase(0.8, AggressionBandCA.Attack)]
		[TestCase(3.0, AggressionBandCA.Attack)]
		public void TheBandsSplitAtFleeAndEngageRatio(double ratio, AggressionBandCA expected)
		{
			Assert.That(AggressionPolicyCA.Band(ratio, 50, 80), Is.EqualTo(expected));
		}

		[Test]
		public void TheAttackerBiasEngagesALosingButCloseFight()
		{
			// The legacy hard-tier bar is 0.75 of the squad ALONE; the coalition bar of 0.8 is below an even fight.
			Assert.That(AggressionPolicyCA.Band(0.85, 50, 80), Is.EqualTo(AggressionBandCA.Attack));
		}

		[Test]
		public void AnInvertedRatioPairHasNoRegroupBand()
		{
			Assert.That(AggressionPolicyCA.Band(0.6, 90, 70), Is.EqualTo(AggressionBandCA.Flee));
			Assert.That(AggressionPolicyCA.Band(0.7, 90, 70), Is.EqualTo(AggressionBandCA.Attack));
		}

		// ---- A3 tiny squads ----

		[TestCase(1, 5000, true)]
		[TestCase(2, 5000, true)]
		[TestCase(3, 999, true)]
		[TestCase(3, 1000, false)]
		[TestCase(8, 6000, false)]
		public void ASquadBelowTheDispatchFloorIsTiny(int units, long value, bool tiny)
		{
			Assert.That(AggressionPolicyCA.IsTiny(units, value, 3, 1000), Is.EqualTo(tiny));
		}

		[Test]
		public void ATinySquadNeverRunsTheFleeLoopNorRegroups()
		{
			Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Flee, tiny: true, vetoed: false), Is.Null);
			Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Regroup, tiny: true, vetoed: false), Is.Null);
			Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Attack, tiny: true, vetoed: true), Is.Null);
		}

		[Test]
		public void ATinySquadStillJoinsAWinningCoalition()
		{
			Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Attack, tiny: true, vetoed: false), Is.EqualTo(AggressionBandCA.Attack));
		}

		[Test]
		public void AVetoTurnsACommitIntoARegroupNotAFlight()
		{
			Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Attack, tiny: false, vetoed: true), Is.EqualTo(AggressionBandCA.Regroup));
		}

		[Test]
		public void AFullSquadKeepsItsBand()
		{
			foreach (var band in Enum.GetValues<AggressionBandCA>())
				Assert.That(AggressionPolicyCA.IdleDecision(band, tiny: false, vetoed: false), Is.EqualTo(band), band.ToString());
		}

		// ---- A2 commit window, trade rule, coalition; A4 ally engaged ----

		static AggressionPolicyCA.HoldReason Hold(int since, long committed = 4000, long lost = 0, double coalition = 0.6,
			double ally = 0.6, bool march = false) =>
			AggressionPolicyCA.HoldsInFight(since, 250, committed, lost, 50, coalition, ally, 50, march);

		[Test]
		public void TheCommitWindowNamesTheHoldOfAFreshFight()
		{
			Assert.That(Hold(0), Is.EqualTo(AggressionPolicyCA.HoldReason.CommitWindow));
			Assert.That(Hold(249, lost: 1999), Is.EqualTo(AggressionPolicyCA.HoldReason.CommitWindow));
		}

		[Test]
		public void TheCommitWindowEndsAtMinCommitTicks()
		{
			Assert.That(Hold(250), Is.EqualTo(AggressionPolicyCA.HoldReason.Coalition));
		}

		[Test]
		public void HeavyLossesBreakTheCommitWindow()
		{
			Assert.That(Hold(10, lost: 2000), Is.EqualTo(AggressionPolicyCA.HoldReason.Coalition));
			Assert.That(AggressionPolicyCA.CommitBroken(4000, 1999, 50), Is.False);
			Assert.That(AggressionPolicyCA.CommitBroken(4000, 2000, 50), Is.True);
			Assert.That(AggressionPolicyCA.CommitBroken(0, 0, 50), Is.True, "a worthless commit cannot hold a window open");
		}

		[Test]
		public void AnUncommittedSquadHasNoWindow()
		{
			Assert.That(Hold(-1), Is.EqualTo(AggressionPolicyCA.HoldReason.Coalition));
		}

		[Test]
		public void ADecisiveLossFleesEvenInsideTheCommitWindow()
		{
			// Opus ruling b, review C1: the decisive-loss flee is checked first, before the window.
			Assert.That(Hold(0, coalition: 0.49, ally: 0.49), Is.EqualTo(AggressionPolicyCA.HoldReason.None));
			Assert.That(Hold(0, coalition: 0.1, ally: 0.1), Is.EqualTo(AggressionPolicyCA.HoldReason.None));
			Assert.That(Hold(0, coalition: 0.1, ally: 0.5), Is.EqualTo(AggressionPolicyCA.HoldReason.CommitWindow), "an engaged ally: not decisive");
			Assert.That(Hold(0, coalition: 0.5, ally: 0.1), Is.EqualTo(AggressionPolicyCA.HoldReason.CommitWindow));
		}

		[Test]
		public void TheTradeIsReportOnly()
		{
			// Opus S6 (i): winning the trade never holds a squad in; it is a predicate for the A6 telemetry.
			Assert.That(Enum.GetNames(typeof(AggressionPolicyCA.HoldReason)), Is.EqualTo(new[] { "None", "CommitWindow", "Coalition", "AllyEngaged" }));
			Assert.That(AggressionPolicyCA.IsWinningTrade(1501, 1500), Is.True);
			Assert.That(AggressionPolicyCA.IsWinningTrade(1500, 1500), Is.False, "an even trade is not winning");
			Assert.That(AggressionPolicyCA.IsWinningTrade(0, 0), Is.False);
		}

		[Test]
		public void ACoalitionAtFleeRatioStays()
		{
			Assert.That(Hold(900, coalition: 0.5, ally: 0.2), Is.EqualTo(AggressionPolicyCA.HoldReason.Coalition));
			Assert.That(Hold(900, coalition: 0.49, ally: 0.2), Is.EqualTo(AggressionPolicyCA.HoldReason.None));
		}

		[Test]
		public void ASquadDoesNotAbandonAnEngagedAlly()
		{
			Assert.That(Hold(900, coalition: 0.3, ally: 0.5), Is.EqualTo(AggressionPolicyCA.HoldReason.AllyEngaged));
			Assert.That(Hold(900, coalition: 0.3, ally: 0.49), Is.EqualTo(AggressionPolicyCA.HoldReason.None));
		}

		[Test]
		public void ADecisiveLossStillFlees()
		{
			Assert.That(Hold(900, committed: 4000, lost: 3000, coalition: 0.1, ally: 0.1), Is.EqualTo(AggressionPolicyCA.HoldReason.None));
		}

		[Test]
		public void OnTheMarchOnlyTheRatioReasonsHold()
		{
			// Review C3 (ruling c): the commit window does not cancel a verdict computed from the defences seen at the target.
			Assert.That(Hold(0, march: true), Is.EqualTo(AggressionPolicyCA.HoldReason.Coalition));
			Assert.That(Hold(0, coalition: 0.3, ally: 0.5, march: true), Is.EqualTo(AggressionPolicyCA.HoldReason.AllyEngaged));
			Assert.That(Hold(0, coalition: 0.3, ally: 0.3, march: true), Is.EqualTo(AggressionPolicyCA.HoldReason.None), "window open, decisive defences: the veto stands");
			Assert.That(Hold(0, march: false), Is.EqualTo(AggressionPolicyCA.HoldReason.CommitWindow));
		}

		[Test]
		public void TheFightHoldTakesTheRatiosFirstAndPassesTheMarch()
		{
			var body = Section(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Aggression.cs"),
				"internal AggressionPolicyCA.HoldReason AggressionHoldsInFight(", "\n\t\t}\n");
			Assert.That(Count(body, "AggressionPolicyCA.HoldsInFight("), Is.EqualTo(1), "one decision call");
			Assert.That(body, Does.Not.Contain("return AggressionPolicyCA.HoldReason."), "no hold before the decision call");
			Assert.That(body.IndexOf("var coalition = AggressionRatio(", StringComparison.Ordinal),
				Is.LessThan(body.IndexOf("AggressionPolicyCA.HoldsInFight(", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("var withAllies = coalition;"));
			Assert.That(body, Does.Contain(", coalition, withAllies, flee, onMarch: target != null);"));
		}

		[Test]
		public void TheEngagedStatesAreTheRealFightStates()
		{
			Assert.That(AggressionPolicyCA.IsEngagedState(new GroundUnitsAttackState()), Is.True);
			Assert.That(AggressionPolicyCA.IsEngagedState(new GuerrillaUnitsHitState()), Is.True);
			Assert.That(AggressionPolicyCA.IsEngagedState(new GroundUnitsIdleStateCA()), Is.False);
			Assert.That(AggressionPolicyCA.IsEngagedState(new GroundUnitsAttackMoveStateCA()), Is.False);
			Assert.That(AggressionPolicyCA.IsEngagedState(new GroundUnitsStageStateCA()), Is.False);
			Assert.That(AggressionPolicyCA.IsEngagedState(new GroundUnitsFleeStateCA()), Is.False);
			Assert.That(AggressionPolicyCA.IsEngagedState(null), Is.False);
		}

		// ---- A2 forward regroup point ----

		[Test]
		public void AFarSquadRegroupsForwardAtTheStandoff()
		{
			var target = new WPos(10240, 10240, 0);
			var squad = new WPos(10240 + 40960, 10240, 0);
			var point = AggressionPolicyCA.RegroupPoint(squad, target, 29 * 1024);
			Assert.That(point, Is.EqualTo(new WPos(10240 + 29 * 1024, 10240, 0)));
			Assert.That((point - target).HorizontalLength, Is.LessThan((squad - target).HorizontalLength), "forward, not home");
		}

		[Test]
		public void ASquadInsideTheStandoffStepsBackOnlyToIt()
		{
			var target = new WPos(0, 0, 0);
			var squad = new WPos(3000, 4000, 0);
			var point = AggressionPolicyCA.RegroupPoint(squad, target, 10000);
			Assert.That(point, Is.EqualTo(new WPos(6000, 8000, 0)));
		}

		[Test]
		public void ASquadOnItsTargetHoldsWhereItIs()
		{
			var at = new WPos(5, 6, 7);
			Assert.That(AggressionPolicyCA.RegroupPoint(at, at, 10000), Is.EqualTo(at));
		}

		// ---- the world call sites (source pins) ----

		[Test]
		public void TheIdleStateTakesTheAggressionPathOnlyBehindTheGate()
		{
			var idle = Section(GroundStates(), "class GroundUnitsIdleStateCA", "class GroundUnitsStageStateCA");
			Assert.That(Count(idle, "AggressionTick(owner, enemyUnits);"), Is.EqualTo(1));
			Assert.That(idle, Does.Contain("if (owner.SquadManager.AggressionOn)\n\t\t\t{\n\t\t\t\tAggressionTick(owner, enemyUnits);\n\t\t\t\treturn;\n\t\t\t}"));
		}

		[Test]
		public void TheLegacyIdleDecisionIsUnchangedAfterTheGate()
		{
			// The FREEZE8 0b68a7bc text of the squad-alone engage-or-flee call, byte for byte: the OFF arm runs exactly this.
			const string Legacy = "\t\t\tvar engage = owner.SquadManager.Info.UseCombatPredictor\n"
				+ "\t\t\t\t? owner.SquadManager.PredictsWin(owner, enemyUnits)\n"
				+ "\t\t\t\t: AttackOrFleeFuzzyCA.Default.CanAttack(owner.Units.ConvertAll(u => u.Actor), enemyUnits);\n";
			var idle = Section(GroundStates(), "class GroundUnitsIdleStateCA", "class GroundUnitsStageStateCA");
			Assert.That(idle, Does.Contain(Legacy));
			var gate = idle.IndexOf("if (owner.SquadManager.AggressionOn)", StringComparison.Ordinal);
			Assert.That(gate, Is.LessThan(idle.IndexOf(Legacy, StringComparison.Ordinal)), "the gate precedes the legacy call");
		}

		[Test]
		public void TheFightHoldIsGatedAndCanOnlyCancelALegacyFlight()
		{
			var text = GroundStates();
			Assert.That(text, Does.Contain("protected static bool AggressionHolds(SquadCA owner) =>\n\t\t\towner.SquadManager.AggressionOn && "));
			Assert.That(text, Does.Contain("if (ShouldFlee(owner) && !AggressionHolds(owner) && !owner.SquadManager.StalemateDefersRetreat(owner, \"attack_should_flee\"))"));
			Assert.That(Count(text, "AggressionHolds(owner)"), Is.EqualTo(1));
		}

		[Test]
		public void TheAttackStateCommitIsGated()
		{
			var attack = Section(GroundStates(), "class GroundUnitsAttackState", "public void Tick(SquadCA owner)");
			Assert.That(attack, Does.Contain("if (owner.SquadManager.AggressionOn && owner.AggressionCommitTick < 0)\n\t\t\t\towner.SquadManager.AggressionCommit(owner);"));
		}

		[Test]
		public void EveryAggressionCallInTheGroundStatesIsInsideAGatedSite()
		{
			// Outside the gated AggressionTick/RegroupForward bodies, the only Aggression* uses are the gate itself, the gated
			// AggressionHolds helper, its one call site and the gated attack-state commit. FREEZE9 CF seam: AggressionSquadDefends
			// in the march helper, behind its AggressionOn term (TheMarchHoldIsGatedAndJudgedAtTheTarget pins the order).
			var text = GroundStates();
			var body = Section(text, "void AggressionTick(SquadCA owner, List<Actor> enemyUnits)", "public void Deactivate(SquadCA owner) { }");
			var outside = text.Replace(body, "");
			var uses = Regex.Matches(outside, @"\.Aggression\w+|\bAggression(HoldsMarch|Holds|Tick)\b|AggressionPolicyCA\.\w+");
			foreach (Match m in uses)
				Assert.That(m.Value, Is.AnyOf(".AggressionOn", "AggressionHolds", "AggressionHoldsMarch", "AggressionTick", ".AggressionHoldsInFight",
					"AggressionPolicyCA.HoldReason", ".AggressionCommitTick", ".AggressionCommit", ".AggressionSquadDefends"), m.Value);
		}

		// ---- A2 on the march (user evidence: large groups retreated from almost nothing) ----

		[Test]
		public void TheMarchHoldIsGatedAndJudgedAtTheTarget()
		{
			var text = GroundStates();
			Assert.That(text, Does.Contain("protected static bool AggressionHoldsMarch(SquadCA owner) =>\n\t\t\towner.SquadManager.AggressionOn && owner.IsTargetValid && !owner.SquadManager.AggressionSquadDefends(owner)\n"
				+ "\t\t\t&& owner.SquadManager.AggressionHoldsInFight(owner, owner.Target.CenterPosition) != AggressionPolicyCA.HoldReason.None;"));
		}

		[Test]
		public void ARememberedDefenceVerdictCanOnlyBeCancelledOnTheMarch()
		{
			var march = Section(GroundStates(), "class GroundUnitsAttackMoveStateCA", "// Since units have different movement speeds");
			Assert.That(march, Does.Contain("if (siegeVerdict == SiegeVerdict.Retreat && !AggressionHoldsMarch(owner))"));
			Assert.That(march, Does.Contain("if (owner.SquadManager.VetoEngage(owner, vetoEnemies, alreadyCommitted: true, out _) && !AggressionHoldsMarch(owner))"));
			Assert.That(Count(GroundStates(), "!AggressionHoldsMarch(owner)"), Is.EqualTo(2));
		}

		[Test]
		public void AnExpiredRegroupRetreatsUnderItsOwnReason()
		{
			// Opus ruling d, review C2: CHECK01 counts an expired regroup apart from a Flee-band retreat (retreat_start and
			// guard_bypass carry the reason). The new reason is listed in the CHECK01 DRAFT03 GUARD_SITES (Frontrunner).
			var body = Section(GroundStates(), "void AggressionTick(SquadCA owner, List<Actor> enemyUnits)", "void RegroupForward(SquadCA owner)");
			Assert.That(Count(body, "\"idle_regroup_expired\""), Is.EqualTo(1));
			Assert.That(Count(body, "\"idle_engage\""), Is.EqualTo(1));
			Assert.That(body, Does.Contain("var site = \"idle_engage\";\n\t\t\tif (decision == AggressionBandCA.Regroup)"));
			Assert.That(body, Does.Contain("\t\t\t\tregroupSinceTick = -1;\n\t\t\t\tsite = \"idle_regroup_expired\";\n\t\t\t}\n"));
			Assert.That(body, Does.Contain("if (manager.StalemateDefersRetreat(owner, site))"));
			Assert.That(body, Does.Contain("manager.NoteRetreatStart(owner, site);"));
		}

		// ---- A9 registry: named values, documented ranges ----

		static readonly Dictionary<string, int> Defaults = new()
		{
			["AggressionEngageRatioPct"] = 80, ["AggressionFleeRatioPct"] = 50, ["AggressionCoalitionCells"] = 12,
			["AggressionAllyEngagedCells"] = 20, ["AggressionRegroupMarginCells"] = 4, ["AggressionRegroupMaxTicks"] = 750,
			["AggressionMinCommitTicks"] = 250, ["AggressionCommitBreakLossPct"] = 50, ["AggressionMinDispatchUnits"] = 3,
			["AggressionMinDispatchValue"] = 1000,
		};

		[Test]
		public void TheRegistryNamesEveryThresholdOnceAndTheInfoServesEachKey()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(AggressionPolicyCA.Ranges.Select(r => r.Key), Is.EquivalentTo(Defaults.Keys));
			foreach (var (key, value) in Defaults)
				Assert.That(info.AggressionValue(key), Is.EqualTo(value), key);
			Assert.Throws<ArgumentOutOfRangeException>(() => info.AggressionValue("AggressionNoSuchKey"));
		}

		[Test]
		public void TheDefaultsAreInsideTheirRanges()
		{
			Assert.That(AggressionPolicyCA.RangeError(k => Defaults[k]), Is.Null);
		}

		[Test]
		public void EveryRangeEdgeIsEnforced()
		{
			foreach (var (key, min, max) in AggressionPolicyCA.Ranges)
			{
				int With(string k, int v) => k == key ? v : Defaults[k];
				var loOk = key == "AggressionEngageRatioPct" ? Math.Max(min, Defaults["AggressionFleeRatioPct"]) : min;
				var hiOk = key == "AggressionFleeRatioPct" ? Defaults["AggressionEngageRatioPct"] : max;
				Assert.That(AggressionPolicyCA.RangeError(k => With(k, loOk)), Is.Null, key + " low edge");
				Assert.That(AggressionPolicyCA.RangeError(k => With(k, hiOk)), Is.Null, key + " high edge");
				Assert.That(AggressionPolicyCA.RangeError(k => With(k, min - 1)), Does.Contain(key), key + " below");
				Assert.That(AggressionPolicyCA.RangeError(k => With(k, max + 1)), Does.Contain(key), key + " above");
			}
		}

		[Test]
		public void EveryThresholdDescStatesItsRangeInTheResolverForm()
		{
			// Sonnet's AggressionParams.Resolve takes the range from `Range a-b` in the field's Desc and throws without one.
			foreach (var (key, min, max) in AggressionPolicyCA.Ranges)
			{
				var desc = typeof(SquadManagerBotModuleCAInfo).GetField(key).GetCustomAttributes(typeof(DescAttribute), false)
					.Cast<DescAttribute>().SelectMany(d => d.Lines).Aggregate("", (x, y) => x + " " + y);
				Assert.That(desc, Does.Contain($"Range {min}-{max}"), key);
			}
		}

		[Test]
		public void TheDocumentedRangesArePinned()
		{
			// Independent of the registry table, so a widened or narrowed edge fails here (mutation M26).
			var expected = new (string, int, int)[]
			{
				("AggressionEngageRatioPct", 1, 300), ("AggressionFleeRatioPct", 0, 300), ("AggressionCoalitionCells", 1, 64),
				("AggressionAllyEngagedCells", 1, 64), ("AggressionRegroupMarginCells", 0, 32), ("AggressionRegroupMaxTicks", 0, 6000),
				("AggressionMinCommitTicks", 0, 3000), ("AggressionCommitBreakLossPct", 0, 100), ("AggressionMinDispatchUnits", 0, 20),
				("AggressionMinDispatchValue", 0, 20000),
			};
			Assert.That(AggressionPolicyCA.Ranges, Is.EqualTo(expected));
		}

		[Test]
		public void EveryThresholdIsReadThroughTheA9Accessor()
		{
			// Opus A9 ruling D1912510: one accessor, which the layered resolver replaces; no direct Info reads in A1-A4.
			var module = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Aggression.cs");
			var states = GroundStates();
			Assert.That(Regex.Matches(module + states, @"\b(?:info|Info)\.Aggression(?!Enabled\b|Value\b)\w+").Count, Is.EqualTo(0));
			Assert.That(module, Does.Contain("internal int AggressionParam(string key, SquadCA squad) => AggP(key, AggressionEnemyOf(squad));"));
			Assert.That(Count(states, "), owner)"), Is.GreaterThanOrEqualTo(6));
			Assert.That(Regex.Matches(states, @"manager\.AggressionParam\(nameof\(SquadManagerBotModuleCAInfo\.\w+\), owner\)").Count, Is.EqualTo(6));
			// FREEZE10 DIAG-FIX-01: +2 (flee and engage bars of the idle allied-band seam, AggressionIdleAlliedBand): 5 -> 7.
			Assert.That(Regex.Matches(module, @"AggressionParam\(nameof\(SquadManagerBotModuleCAInfo\.\w+\), squad\)").Count, Is.EqualTo(7));
		}

		// ---- A7 seam (Sonnet's finishing push) ----

		[Test]
		public void TheFinishingPushAttacksInEveryBandButFlee()
		{
			foreach (var tiny in new[] { false, true })
				foreach (var vetoed in new[] { false, true })
				{
					var at = $"tiny={tiny} vetoed={vetoed}";
					Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Regroup, tiny, vetoed, finish: true), Is.EqualTo(AggressionBandCA.Attack), at);
					Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Attack, tiny, vetoed, finish: true), Is.EqualTo(AggressionBandCA.Attack), at);
					Assert.That(AggressionPolicyCA.IdleDecision(AggressionBandCA.Flee, tiny, vetoed, finish: true), Is.EqualTo(AggressionBandCA.Flee), at);
				}
		}

		[Test]
		public void TheFinishSeamReadsTheAggTeamFlagAndIsPassedToTheIdleDecision()
		{
			// FREEZE9 seam S2: the agg-team flag (false with CG off and under a RecallAll; FreezeNineSeamsTest).
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Aggression.cs"),
				Does.Contain("internal bool AggressionFinishActive => AggTeamFinishActive;"));
			Assert.That(GroundStates(), Does.Contain("AggressionPolicyCA.IdleDecision(band, tiny, vetoed, manager.AggressionFinishActive);"));
			// Review L4: the finishing push ignores the veto, so the veto is not asked (no telemetry side effect) then.
			Assert.That(GroundStates(), Does.Contain("var vetoed = band == AggressionBandCA.Attack && !manager.AggressionFinishActive\n"));
		}

		[Test]
		public void AFleeRatioAboveTheEngageRatioIsRejected()
		{
			Assert.That(AggressionPolicyCA.RangeError(k => k == "AggressionFleeRatioPct" ? 81 : Defaults[k]), Does.Contain("must not exceed"));
		}

		static List<MiniYamlNode> PlayerNodes() => MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai").Single(n => n.Key == "Player").Value.Nodes.ToList();

		static readonly string[] Personalities = { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" };

		[Test]
		public void EachPersonalityCarriesTheWholeRegistryBlockInsideItsRanges()
		{
			var nodes = PlayerNodes();
			foreach (var personality in Personalities)
			{
				var node = nodes.Single(n => n.Key == "SquadManagerBotModuleCA@" + personality);
				var keys = node.Value.Nodes.Select(n => n.Key).ToHashSet();
				Assert.That(keys, Does.Contain("AggressionEnabled"), personality + ": the CF switch is armed in the ported ai.yaml");
				foreach (var key in Defaults.Keys)
					Assert.That(keys, Does.Contain(key), personality + " " + key);

				var info = FieldLoader.Load<SquadManagerBotModuleCAInfo>(node.Value);
				Assert.That(info.UseCombatPredictor, Is.True, personality);
				Assert.That(AggressionPolicyCA.RangeError(info.AggressionValue), Is.Null, personality);
				foreach (var (key, value) in Defaults)
					Assert.That(info.AggressionValue(key), Is.EqualTo(value), personality + " " + key);
			}
		}

		[Test]
		public void TheBaseYamlLeavesTheGateClosedAndClassicUntouched()
		{
			// The ported ai.yaml arms both StalemateBreakerEnabled (CD) and AggressionEnabled (CF) on the six personalities, and
			// CD without CF still keeps the registry inert (logic check, not yaml-dependent).
			var nodes = PlayerNodes();
			foreach (var personality in Personalities)
			{
				var info = FieldLoader.Load<SquadManagerBotModuleCAInfo>(nodes.Single(n => n.Key == "SquadManagerBotModuleCA@" + personality).Value);
				Assert.That(AggressionPolicyCA.Enabled(info.StalemateBreakerEnabled, info.AggressionEnabled, info.UseCombatPredictor), Is.True, personality);
				Assert.That(info.AggressionEnabled, Is.True, personality);
				Assert.That(AggressionPolicyCA.Enabled(true, false, info.UseCombatPredictor), Is.False, "CD without CF: " + personality);
			}

			var classic = nodes.Single(n => n.Key == "SquadManagerBotModuleCA@classic");
			Assert.That(classic.Value.Nodes.Any(n => n.Key.StartsWith("Aggression", StringComparison.Ordinal)), Is.False);
		}
	}
}
