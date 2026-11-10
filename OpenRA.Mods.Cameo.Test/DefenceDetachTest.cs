// IMPL02 DEFENCE-DETACH (Opus ruling on agg-team C1/C2) and C3/C4: pure plan tests plus source pins for the wiring
// (the squad manager has no headless world fixture; the first real exercise is the ON smoke).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class DefenceDetachTest
	{
		static DetachCandidate U(uint id, int value, long distSq, bool atHome = true) => new(id, value, distSq, atHome);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Wiring() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		// ---- the threat ----

		[Test]
		public void TheNetThreatIsTheObservedValueMinusTheDefenceShareAndNeverNegative()
		{
			Assert.That(DefenceDetachment.NetThreat(3000, 2000, 50), Is.EqualTo(2000));
			Assert.That(DefenceDetachment.NetThreat(3000, 10000, 50), Is.EqualTo(0), "strong defences cover the raid");
			Assert.That(DefenceDetachment.NetThreat(3000, 2000, 0), Is.EqualTo(3000), "no defence share");
			Assert.That(DefenceDetachment.NetThreat(3000, 2000, 500), Is.EqualTo(1000), "the share is clamped to 100");
			Assert.That(DefenceDetachment.NetThreat(-5, 0, 50), Is.EqualTo(0));
		}

		[Test]
		public void NoThreatDetachesNothing()
		{
			var plan = DefenceDetachment.Plan(0, 20000, new[] { U(1, 500, 10) }, 130, 35, 60);
			Assert.That(plan.IsActive, Is.False);
			Assert.That(plan.Contains(1), Is.False);
			Assert.That(plan.RecallAll, Is.False);
			Assert.That(DefenceDetachment.Plan(0, 0, new[] { U(1, 500, 10) }, 130, 35, 60).RecallAll, Is.False, "no threat is never a recall, even with no army");
		}

		// ---- a small raid detaches a small part and the push goes on ----

		[Test]
		public void ASmallRaidDetachesJustEnoughNearUnitsAndTheRestKeepsPushing()
		{
			var units = new[]
			{
				U(1, 700, 100), U(2, 700, 200), U(3, 700, 300), U(4, 700, 400), U(5, 5000, 90000, false), U(6, 5000, 95000, false),
			};
			var plan = DefenceDetachment.Plan(1000, 20000, units, 130, 35, 60);

			Assert.That(plan.RecallAll, Is.False, "a raid of 5% of the army does not recall");
			Assert.That(plan.Detached, Is.EquivalentTo(new uint[] { 1, 2 }), "1400 >= 1.3 x 1000 with the two nearest");
			Assert.That(plan.DetachedValue, Is.EqualTo(1400));
			Assert.That(plan.Contains(3), Is.False, "the others keep pushing");
			Assert.That(plan.Contains(5), Is.False);
		}

		[Test]
		public void TheTargetRoundsUpSoTheMarginIsAlwaysMet()
		{
			var plan = DefenceDetachment.Plan(101, 100000, new[] { U(1, 131, 1), U(2, 5, 2) }, 130, 35, 60);
			Assert.That(plan.DetachedValue, Is.EqualTo(136), "131.3 rounds up to 132: 131 is not enough, the second unit is needed");
		}

		[Test]
		public void ADetachedValueExactlyAtTheTargetStopsAndExactlyAtTheCapStillFits()
		{
			var exactTarget = DefenceDetachment.Plan(1000, 100000, new[] { U(1, 500, 1), U(2, 500, 2), U(3, 500, 3) }, 100, 100, 0);
			Assert.That(exactTarget.Detached, Is.EquivalentTo(new uint[] { 1, 2 }), "1000 reaches a target of 1000");
			var exactCap = DefenceDetachment.Plan(5000, 20000, new[] { U(1, 3500, 1), U(2, 3500, 2) }, 130, 35, 0);
			Assert.That(exactCap.Detached, Is.EquivalentTo(new uint[] { 1, 2 }), "7000 is exactly the cap of 35% of 20000");
			var six = Enumerable.Range(1, 6).Select(i => U((uint)i, 6000, i)).ToArray();
			var wideCap = DefenceDetachment.Plan(20000, 20000, six, 130, 500, 0);
			var fullCap = DefenceDetachment.Plan(20000, 20000, six, 130, 100, 0);
			Assert.That(fullCap.Detached, Is.EquivalentTo(new uint[] { 1, 2, 3 }), "the cap (100% of 20000) stops at 18000");
			Assert.That(wideCap.Detached, Is.EquivalentTo(fullCap.Detached), "a share above 100 is clamped to 100");
		}

		// ---- a big counter-attack recalls the whole push ----

		[Test]
		public void AThreatOfTheRecallShareOfTheArmyRecallsEveryCandidate()
		{
			var units = new[] { U(1, 1000, 10), U(2, 1000, 20, false), U(3, 1000, 30, false) };
			var plan = DefenceDetachment.Plan(12000, 20000, units, 130, 35, 60);
			Assert.That(plan.RecallAll, Is.True);
			Assert.That(plan.Detached, Is.EquivalentTo(new uint[] { 1, 2, 3 }));
			Assert.That(plan.Contains(999), Is.True, "a recall holds every unit, even one the plan did not list");
		}

		[Test]
		public void TheRecallStartsExactlyAtTheRecallShare()
		{
			var units = new[] { U(1, 1000, 10) };
			Assert.That(DefenceDetachment.Plan(11999, 20000, units, 130, 35, 60).RecallAll, Is.False);
			Assert.That(DefenceDetachment.Plan(12000, 20000, units, 130, 35, 60).RecallAll, Is.True);
		}

		[Test]
		public void ARecallShareOfZeroDisablesTheRecall()
		{
			var plan = DefenceDetachment.Plan(50000, 20000, new[] { U(1, 1000, 10) }, 130, 35, 0);
			Assert.That(plan.RecallAll, Is.False);
		}

		[Test]
		public void AnyThreatWithoutAnArmyRecalls()
		{
			Assert.That(DefenceDetachment.Plan(100, 0, new[] { U(1, 1000, 10) }, 130, 35, 60).RecallAll, Is.True);
		}

		// ---- who is chosen ----

		[Test]
		public void UnitsAtHomeComeBeforeUnitsInTheFieldThenTheNearestThenTheLowerId()
		{
			var units = new[]
			{
				U(9, 1000, 5, false), U(4, 1000, 300), U(3, 1000, 300), U(7, 1000, 100), U(8, 1000, 100, false),
			};
			// threat 2000 -> target 2600: three units; the field unit (9) is nearest of all but stands in the field
			var plan = DefenceDetachment.Plan(2000, 100000, units, 130, 100, 60);
			Assert.That(plan.Detached, Is.EquivalentTo(new uint[] { 7, 3, 4 }), "home first (7 is nearest), then the id tie-break 3 before 4");
		}

		[Test]
		public void WhenHomeCannotCoverTheTargetTheNearestFieldUnitsFollow()
		{
			var units = new[] { U(1, 1000, 100), U(2, 1000, 50000, false), U(3, 1000, 20000, false) };
			var plan = DefenceDetachment.Plan(2000, 100000, units, 130, 100, 60);
			Assert.That(plan.Detached, Is.EquivalentTo(new uint[] { 1, 3, 2 }), "2600 needs three 1000-value units; the nearer field unit is taken before the farther");
			var twoNeeded = DefenceDetachment.Plan(1500, 100000, units, 130, 100, 60);
			Assert.That(twoNeeded.Detached, Is.EquivalentTo(new uint[] { 1, 3 }));
		}

		[Test]
		public void TheDetachedValueNeverExceedsTheShareCapAndACheaperLaterUnitMayStillFit()
		{
			var units = new[] { U(1, 3000, 1), U(2, 3000, 2), U(3, 3000, 3), U(4, 500, 4) };
			var plan = DefenceDetachment.Plan(5000, 20000, units, 130, 35, 0);
			Assert.That(plan.Detached, Is.EquivalentTo(new uint[] { 1, 2, 4 }), "cap 7000: the third 3000 is skipped, the 500 fits");
			Assert.That(plan.DetachedValue, Is.EqualTo(6500));
			Assert.That(plan.DetachedValue, Is.LessThanOrEqualTo(7000));
		}

		[Test]
		public void AZeroCapDetachesNothingAndWorthlessUnitsAreNeverDetached()
		{
			Assert.That(DefenceDetachment.Plan(1000, 20000, new[] { U(1, 500, 1) }, 130, 0, 0).IsActive, Is.False);
			Assert.That(DefenceDetachment.Plan(1000, 20000, new[] { U(1, 0, 1), U(2, -3, 2) }, 130, 35, 0).IsActive, Is.False);
		}

		[Test]
		public void ThePlanIsIndependentOfTheInputOrder()
		{
			var units = new[] { U(5, 600, 7), U(2, 600, 7), U(9, 600, 3), U(1, 600, 9, false) };
			var a = DefenceDetachment.Plan(900, 40000, units, 130, 50, 60);
			var b = DefenceDetachment.Plan(900, 40000, units.Reverse(), 130, 50, 60);
			Assert.That(a.Detached, Is.EquivalentTo(b.Detached));
			Assert.That(a.Detached, Is.EquivalentTo(new uint[] { 9, 2 }));
		}

		[Test]
		public void AllAndAnyAreTrueOnlyForTheRightGroups()
		{
			var plan = DefenceDetachment.Plan(1000, 20000, new[] { U(1, 700, 1), U(2, 700, 2), U(3, 700, 3) }, 130, 35, 60);
			Assert.That(plan.ContainsAny(new uint[] { 3, 2 }), Is.True);
			Assert.That(plan.ContainsAny(new uint[] { 3 }), Is.False);
			Assert.That(plan.ContainsAll(new uint[] { 1, 2 }), Is.True);
			Assert.That(plan.ContainsAll(new uint[] { 1, 3 }), Is.False);
			Assert.That(plan.ContainsAll(Array.Empty<uint>()), Is.False, "an empty group is not a defender");
			Assert.That(plan.ContainsAny(null), Is.False);
		}

		// ---- the latch ----

		[Test]
		public void ALiveThreatUsesTheFreshPlanAndTheLatchKeepsTheLastPlanOnlyWhileActive()
		{
			var last = DefenceDetachment.Plan(1000, 20000, new[] { U(1, 700, 1), U(2, 700, 2) }, 130, 35, 60);
			var fresh = DefenceDetachment.Plan(300, 20000, new[] { U(1, 700, 1), U(2, 700, 2) }, 130, 35, 60);
			Assert.That(DefenceDetachment.Latch(last, fresh, true, true), Is.SameAs(fresh));
			Assert.That(DefenceDetachment.Latch(last, fresh, false, true), Is.SameAs(last), "threat out of sight, latch still active");
			Assert.That(DefenceDetachment.Latch(last, fresh, false, false).IsActive, Is.False, "cleared: the detachment returns to the reserve/push");
			Assert.That(DefenceDetachment.Latch(null, null, true, false).IsActive, Is.False);
		}

		// ---- C3 / C4 pure parts ----

		[Test]
		public void WallsAndDefencesNeverMakeASeatLookWithoutProduction()
		{
			var wall = (Wall: true, Armed: false, Key: false);
			var pillbox = (Wall: false, Armed: true, Key: false);
			var power = (Wall: false, Armed: false, Key: false);
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { wall, wall, pillbox, pillbox }, 4), Is.False, "four walls and defences prove nothing");
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { pillbox, pillbox, pillbox, pillbox }, 4), Is.False, "four defences");
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { wall, wall, wall, wall }, 4), Is.False, "four walls");
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { power, power, power, power }, 4), Is.True);
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { power, power, power, wall, pillbox }, 4), Is.False, "three counted buildings are below four");
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(null, 4), Is.False);
		}

		[Test]
		public void AKeyBuildingAlwaysCountsAndAlwaysPreventsTheTrigger()
		{
			var power = (Wall: false, Armed: false, Key: false);
			var armedKey = (Wall: false, Armed: true, Key: true);
			Assert.That(AggTeamPolicyCA.SeatLacksProduction(new[] { power, power, power, armedKey }, 4), Is.False);
		}

		[Test]
		public void AForcedLaunchNeedsTheDispatchFloorInUnitsAndInValue()
		{
			Assert.That(AggTeamPolicyCA.LaunchMeetsFloor(1, 5000, 3, 1000), Is.False, "one unit of any value");
			Assert.That(AggTeamPolicyCA.LaunchMeetsFloor(3, 999, 3, 1000), Is.False);
			Assert.That(AggTeamPolicyCA.LaunchMeetsFloor(3, 1000, 3, 1000), Is.True);
			Assert.That(AggTeamPolicyCA.LaunchMeetsFloor(0, 0, 0, 0), Is.True, "floors of 0 never block");
		}

		// ---- wiring pins ----

		[Test]
		public void TheTickAppliesThePlanBlocksOnlyOnARecallAndChecksTheDraftFloorBeforeALaunch()
		{
			var tick = Section(Wiring(), "void AggTeamTick(IBot bot)", "List<UnitWposWrapper> AggTeamTakeReserve()");
			foreach (var guard in new[]
			{
				"var defence = AggTeamDefence();", "var blocked = provider == null || defence.RecallAll;", "AggTeamApplyDetachment(bot, defence);",
				"&& AggTeamDraftMeetsFloor(reserve, defence))",
			})
				Assert.That(tick, Does.Contain(guard), guard);
			Assert.That(tick, Does.Not.Contain("StalemateHomeBlocks"), "a home threat no longer freezes the attack");
			Assert.That(tick.IndexOf("AggTeamApplyDetachment(", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("if (blocked)", StringComparison.Ordinal)),
				"a recall is applied before the block returns");
		}

		[Test]
		public void ThePlanIsLatchedFailsClosedAndIsFedFromTheProviderValues()
		{
			var plan = Section(Wiring(), "internal DefenceDetachmentPlan AggTeamDefence()", "internal bool AggTeamSquadDefends(");
			foreach (var guard in new[]
			{
				"if (!Info.AggTeamEnabled)", "if (provider == null)\n\t\t\t\treturn aggDetachPlan = AggRecallAllPlan;", "provider.StalemateHomeThreatValue, provider.StalemateHomeDefenceValue",
				"AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefenceSharePercent))", "AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefendMarginPercent))",
				"AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamMaxDetachSharePercent))", "AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamRecallSharePercent))",
				"DefenceDetachment.Latch(aggDetachPlan, fresh, provider.StalemateHomeThreat, provider.StalemateHomeLatch)", "if (aggDetachTick == now)",
				"net > 0",
			})
				Assert.That(plan, Does.Contain(guard), guard);
		}

		[Test]
		public void TheCandidatesAreThePoolAndTheFieldRushSquadsWithoutTheBreakerSquadAndHomeIsADistanceTest()
		{
			var cand = Section(Wiring(), "IEnumerable<DetachCandidate> AggTeamDetachCandidates()", "internal DefenceDetachmentPlan AggTeamDefence()");
			foreach (var guard in new[]
			{
				"foreach (var u in unitsHangingAroundTheBase)", "d <= radiusSquared);",
				"if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad))\n\t\t\t\t\tcontinue;\n\t\t\t\tforeach (var u in s.Units)",
				"AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamDefenceHomeRadiusCells))",
			})
				Assert.That(cand, Does.Contain(guard), guard);
			Assert.That(System.Text.RegularExpressions.Regex.Matches(cand, System.Text.RegularExpressions.Regex.Escape("d <= radiusSquared);")).Count, Is.EqualTo(2));
		}

		[Test]
		public void TheApplyStepSkipsTheBreakerSquadAndQuietSquadsAndSendsTheUnitsHome()
		{
			var apply = Section(Wiring(), "void AggTeamApplyDetachment(", "void AggTeamSendHome(");
			Assert.That(apply, Does.Contain("if (s.Type != SquadCAType.Rush || !s.IsValid || ReferenceEquals(s, stalemateSquad) || AggTeamIsQuietState(s.FuzzyStateMachine?.CurrentState))\n\t\t\t\t\tcontinue;\n\n\t\t\t\tvar ids"));
			Assert.That(apply.Split("AggTeamSendHome(bot,").Length - 1, Is.EqualTo(2), "both the stand-down and the partial release send the units home");
			Assert.That(Section(Wiring(), "void AggTeamSendHome(", "List<UnitWposWrapper> AggTeamTakeDetached()"),
				Does.Contain("bot.QueueOrder(new Order(\"AttackMove\", u.Actor, Target.FromCell(World, homeCell), false));"));
		}

		[Test]
		public void TheJoinTheA8PickAndTheFinishFlagAllYieldToTheDetachment()
		{
			var wiring = Wiring();
			Assert.That(Section(wiring, "void AggTeamPrepare(SquadCA s)", "TeamAttackIntent AggTeamPushIntent("),
				Does.Contain("var blocked = provider == null || defence.RecallAll || AggTeamSquadDefends(s);"));
			Assert.That(Section(wiring, "Actor AggTeamPickVisible(", "return picked ?? nearest;"),
				Does.Contain("visible.Count < 2 || AggTeamSquadDefends(owner))\n\t\t\t\treturn nearest;"));
			Assert.That(wiring, Does.Contain("internal bool AggTeamFinishActive => Info.AggTeamEnabled && aggFinishActive && !AggTeamDefence().RecallAll;"));
		}

		[Test]
		public void TheFieldSquadsAreStoodDownOrReleasedAndTheDetachedPoolUnitsStayHomeInEveryCgLaunch()
		{
			var apply = Section(Wiring(), "void AggTeamApplyDetachment(", "void AggTeamSendHome(");
			foreach (var guard in new[]
			{
				"if (!plan.IsActive)", "ReferenceEquals(s, stalemateSquad)", "AggTeamIsQuietState(", "if (plan.ContainsAll(ids))", "DismissSquad(s);",
				"else if (plan.ContainsAny(ids))", "leases.Release(u.Actor, LeaseOwner)", "unitsHangingAroundTheBase.Add(u);",
			})
				Assert.That(apply, Does.Contain(guard), guard);
			Assert.That(Section(Wiring(), "List<UnitWposWrapper> AggTeamTakeReserve()", "List<UnitWposWrapper> AggTeamTakeReserveOnly()"),
				Does.Contain("AggTeamTakeDetached()").And.Contain("detached.AddRange(reserved);"));
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs"),
				Does.Contain("var aggKeep = aggForce ? AggTeamTakeReserve() : Info.AggTeamEnabled ? AggTeamTakeDetached() : null;"));
		}

		[Test]
		public void TheSeatWithoutProductionTriggerUsesThePureCounterAndTheDraftFloorUsesTheMergeFloor()
		{
			var wiring = Wiring();
			var seat = Section(wiring, "bool AggTeamEnemySeatWithoutProduction()", "Actor AggTeamPickVisible(");
			foreach (var guard in new[] { "HasTraitInfo<LineBuildInfo>()", "HasTraitInfo<AttackBaseInfo>()", "AggTeamPolicyCA.SeatLacksProduction(list, minKnown)" })
				Assert.That(seat, Does.Contain(guard), guard);
			var draft = Section(wiring, "bool AggTeamDraftMeetsFloor(", "void AggTeamUpdatePush(");
			foreach (var guard in new[] { "!plan.Contains(u.Actor.ActorID)", "AggTeamPolicyCA.KeepForReserve(pool, need)", "AggTeamPolicyCA.LaunchMeetsFloor(", "AggTeamTinyMergeMinUnits", "AggTeamTinyMergeValue" })
				Assert.That(draft, Does.Contain(guard), guard);
		}

		[Test]
		public void TheProviderValuesComeFromTheSameSnapshotAsHomeThreat()
		{
			var text = Source("mods/Cameo/Traits/BotModules/BotSituation.cs");
			var snapshot = Section(text, "stalemate.HomeThreat = homeThreat;", "long allied = 0;");
			foreach (var guard in new[]
			{
				"seen.Combat && seen.LastSeenTick == tick", "threatValue += seen.Info?.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;",
				"HasSpace(a) && IsCombatUnit(a) && MasterAiEval.WithinAnyAnchor(a.Location.X, a.Location.Y, anchors, stalemate.RadiusFor(a.Info))", "threatValue += Value(a);",
				"stalemate.HomeThreatValue = threatValue;", "b.Info.HasTraitInfo<AttackBaseInfo>() && !b.Info.HasTraitInfo<LineBuildInfo>()", "StalemateUrgencyState.HomeRadiusCells",
			})
				Assert.That(snapshot, Does.Contain(guard), guard);
			Assert.That(text, Does.Contain("long IBotStalemateUrgencyProvider.StalemateHomeThreatValue => stalemate?.HomeThreatValue ?? 0;"));
			Assert.That(text, Does.Contain("long IBotStalemateUrgencyProvider.StalemateHomeDefenceValue => stalemate?.HomeDefenceValue ?? 0;"));
		}

		[Test]
		public void TheDefaultsMatchTheRuling()
		{
			var info = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCAInfo.AggTeam.cs");
			Assert.That(info, Does.Contain("public readonly int AggTeamDefendMarginPercent = 130;"));
			Assert.That(info, Does.Contain("public readonly int AggTeamMaxDetachSharePercent = 35;"));
			Assert.That(info, Does.Contain("public readonly int AggTeamRecallSharePercent = 60;"));
			Assert.That(info, Does.Contain("Range 100-500."));
		}
	}
}
