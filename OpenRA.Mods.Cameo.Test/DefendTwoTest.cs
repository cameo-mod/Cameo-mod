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

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// TEAM-DEFEND-02 (row tm_defend_two_tier; design pending/TEAM_DEFEND_02_DESIGN_01.md, section 7): the two-tier ally defence's T2. Pure golden numbers for the sizing, the quota split, the commit rule, the replace rule and the team ledger,
	/// then the repo's usual source pins (fog, precedence, ledger order, switch, yaml).
	/// </summary>
	[TestFixture]
	public class DefendTwoTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		static TeamDefendCA.Alarm Seen(string victim, int tick, long attack, long defence = 0) =>
			new(tick, victim, 1, new WPos(0, 0, 0), 77, new WPos(1024, 0, 0), attack, defence);

		static TeamDefendCA.Alarm Blind(string victim, int tick, long defence = 0) =>
			new(tick, victim, 1, new WPos(0, 0, 0), 0, new WPos(0, 0, 0), 0, defence);

		// ---- need ----

		[Test]
		public void NeedIsTheMarginTimesTheSeenAttackMinusTheDefenceThere()
		{
			Assert.That(DefendTwo.Need(3000, 0, 125), Is.EqualTo(3750));
			Assert.That(DefendTwo.Need(3000, 500, 125), Is.EqualTo(3250), "1.25 x attack - defence; the old (attack - defence) x 1.25 would give 3125");
			Assert.That(DefendTwo.Need(1000, 800, 125), Is.EqualTo(450));
			Assert.That(DefendTwo.Need(1000, 1250, 125), Is.EqualTo(0), "defence at the margin: nothing to send");
			Assert.That(DefendTwo.Need(1000, 5000, 125), Is.EqualTo(0), "never negative");
			Assert.That(DefendTwo.Need(0, 0, 125), Is.EqualTo(0), "no attack seen = no sized need (a blind alarm uses BlindNeed)");
			Assert.That(DefendTwo.Need(-5, 0, 125), Is.EqualTo(0));
		}

		[Test]
		public void NeedRoundsTheMarginUpClampsItAndSurvivesBigNumbers()
		{
			Assert.That(DefendTwo.Need(1001, 0, 125), Is.EqualTo(1252), "ceil(1251.25)");
			Assert.That(DefendTwo.Need(1000, 0, 50), Is.EqualTo(1000), "margin floor 100");
			Assert.That(DefendTwo.Need(1000, 0, 900), Is.EqualTo(4000), "margin cap 400");
			Assert.That(DefendTwo.Need(1000, -300, 125), Is.EqualTo(1250), "a negative defence reads as 0");
			Assert.That(DefendTwo.Need(long.MaxValue / 1000, 0, 125), Is.GreaterThan(0), "no overflow");
		}

		[Test]
		public void ABlindAlarmSendsTheBlindValueMinusTheDefenceThere()
		{
			Assert.That(DefendTwo.BlindNeed(600, 0), Is.EqualTo(600));
			Assert.That(DefendTwo.BlindNeed(600, 250), Is.EqualTo(350));
			Assert.That(DefendTwo.BlindNeed(600, 900), Is.EqualTo(0));
			Assert.That(DefendTwo.BlindNeed(-1, 0), Is.EqualTo(0));
			Assert.That(DefendTwo.IsBlind(0), Is.True);
			Assert.That(DefendTwo.IsBlind(12), Is.False);
		}

		// ---- quota and the ledger passes ----

		[Test]
		public void TheQuotaSplitsTheMissingValueBySpare()
		{
			Assert.That(DefendTwo.Quota(3250, 0, 1000, 5000), Is.EqualTo(650));
			Assert.That(DefendTwo.Quota(3250, 0, 2000, 5000), Is.EqualTo(1300));
			Assert.That(650 + 1300 + 1300, Is.EqualTo(3250), "the shares add up to the need");
			Assert.That(DefendTwo.Quota(3250, 1250, 2000, 5000), Is.EqualTo(800), "what the team has on its way is no longer missing");
		}

		[Test]
		public void TheQuotaIsNeverMoreThanTheSpareAndZeroWhenThereIsNothingToSendOrMissing()
		{
			Assert.That(DefendTwo.Quota(1000, 0, 300, 400), Is.EqualTo(300), "a poor ally sends what it has");
			Assert.That(DefendTwo.Quota(1000, 1000, 300, 400), Is.EqualTo(0), "commits that reach the need");
			Assert.That(DefendTwo.Quota(1000, 5000, 300, 400), Is.EqualTo(0));
			Assert.That(DefendTwo.Quota(1000, 0, 0, 400), Is.EqualTo(0), "no spare");
			Assert.That(DefendTwo.Quota(1000, 0, 300, 0), Is.EqualTo(0), "no answerer value");
			Assert.That(DefendTwo.Quota(0, 0, 300, 400), Is.EqualTo(0));
			Assert.That(DefendTwo.Quota(500, 0, 300, 100), Is.EqualTo(300), "a total below the own spare (stale ledger) is read as the own spare: the whole need, bounded by the spare");
			Assert.That(DefendTwo.Quota(200, 0, 300, 100), Is.EqualTo(200), "...and never above the missing value");
		}

		[Test]
		public void ThreeSeatsPassingOneAfterTheOtherSendTheNeedAndNotMoreThanOneUnitOverIt()
		{
			// need 600; seats with 400 spare each in units of 100. Each seat publishes its pre-send spare, takes the quota of the sums, sends whole units, then publishes its commit.
			long[] spare = { 400, 400, 400 };
			long[] published = { 0, 0, 0 };
			long[] commit = { 0, 0, 0 };
			const long need = 600;
			for (var pass = 0; pass < 4; pass++)
			{
				for (var i = 0; i < 3; i++)
				{
					published[i] = spare[i];
					var total = published[0] + published[1] + published[2];
					var quota = DefendTwo.Quota(need, commit[0] + commit[1] + commit[2], spare[i], total);
					long sent = 0;
					while (sent < quota && spare[i] > 0)
					{
						spare[i] -= 100;
						commit[i] += 100;
						sent += 100;
					}

					Assert.That(commit[0] + commit[1] + commit[2], Is.LessThanOrEqualTo(need + 100), "pass " + pass + " seat " + i + ": at most one unit over the need");
				}
			}

			Assert.That(commit[0] + commit[1] + commit[2], Is.EqualTo(need), "the team sent the need");
			Assert.That(commit[0], Is.EqualTo(400), "the first seat of the first pass has no published peers yet, so it takes its whole spare: a seat that has not published is simply absent from the sum");
		}

		[Test]
		public void ASeatThatHasNotPublishedIsAbsentAndAStaleEntryIsIgnored()
		{
			var team = new TeamLeadTeam();
			team.BindDefendLedger("A");
			team.PublishDefendSpare("s1", 700, 1000);
			team.PublishDefendCommit("s1", 300, 1000);
			Assert.That(team.DefendSumsExcept("s2", 1050, DefendTwo.LedgerMaxAgeTicks), Is.EqualTo((700L, 300L)), "fresh peer entries count");
			Assert.That(team.DefendSumsExcept("s1", 1050, DefendTwo.LedgerMaxAgeTicks), Is.EqualTo((0L, 0L)), "the caller's own entry is never summed (it adds its live figures)");
			Assert.That(team.DefendSumsExcept("s2", 1100, DefendTwo.LedgerMaxAgeTicks), Is.EqualTo((700L, 300L)), "exactly 100 ticks old still counts");
			Assert.That(team.DefendSumsExcept("s2", 1101, DefendTwo.LedgerMaxAgeTicks), Is.EqualTo((0L, 0L)), "older than 100 ticks is ignored");
			Assert.That(team.DefendSumsExcept("s2", 999, DefendTwo.LedgerMaxAgeTicks), Is.EqualTo((0L, 0L)), "an entry from the future is not usable");
			Assert.That(DefendTwo.Fresh(10, -1, 100), Is.False);
		}

		[Test]
		public void OnANewVictimOnlyTheCommitsAreWipedOnceTheSpareStaysAndAnInactiveAlarmClearsTheCommits()
		{
			var team = new TeamLeadTeam();
			team.BindDefendLedger("A");
			team.PublishDefendSpare("s1", 500, 10);
			team.PublishDefendCommit("s1", 300, 10);
			team.BindDefendLedger("A");
			Assert.That(team.DefendSumsExcept("s2", 20, 100), Is.EqualTo((500L, 300L)), "a same-victim re-raise keeps both (the units still marching stay committed)");

			team.BindDefendLedger("B");
			Assert.That(team.DefendSumsExcept("s2", 20, 100), Is.EqualTo((500L, 0L)), "the first seat to see the new victim wipes the commits; the spare is a property of the seat and stays");
			team.PublishDefendCommit("s1", 200, 25);
			team.BindDefendLedger("B");
			Assert.That(team.DefendSumsExcept("s2", 30, 100), Is.EqualTo((500L, 200L)), "the seats after it find the victim bound already and do not wipe what the first one published");

			team.ClearDefendLedger();
			Assert.That(team.DefendSumsExcept("s2", 30, 100), Is.EqualTo((500L, 0L)), "an inactive alarm clears the commits");
			Assert.That(team.DefendSumsExcept("s2", 200, 100), Is.EqualTo((0L, 0L)), "the spare only ages out");
		}

		[Test]
		public void AWarmLedgerSplitsTheFirstRoundOfANewVictimBySpareInsteadOfFirstComeAndAColdOneIsFirstCome()
		{
			var team = new TeamLeadTeam();
			team.BindDefendLedger("A");
			foreach (var seat in new[] { "s0", "s1", "s2" })
			{
				team.PublishDefendSpare(seat, 400, 50);
				team.PublishDefendCommit(seat, 300, 50);
			}

			team.BindDefendLedger("B");
			var others = team.DefendSumsExcept("s0", 60, DefendTwo.LedgerMaxAgeTicks);
			Assert.That(others, Is.EqualTo((800L, 0L)), "the peers' spare is still there, their commits for the old victim are gone");
			Assert.That(DefendTwo.Quota(600, others.Commit, 400, others.Spare + 400), Is.EqualTo(200), "600 x 400 / 1200: a share, not the whole 400 first-come");

			var cold = new TeamLeadTeam();
			cold.BindDefendLedger("B");
			var none = cold.DefendSumsExcept("s0", 60, DefendTwo.LedgerMaxAgeTicks);
			Assert.That(DefendTwo.Quota(600, none.Commit, 400, none.Spare + 400), Is.EqualTo(400), "a cold ledger (no peer published): the first seat takes min(spare, need)");
		}

		// ---- commit ----

		[Test]
		public void AUnitThatArrivedIsInTheDefenceThereAndNeverInTheCommit()
		{
			long radiusSq = 100;
			var units = new[] { (50L, 400L), (70L, 100L), (30L, 10000L), (-9L, 99999L) };
			Assert.That(DefendTwo.CommitValue(units, radiusSq), Is.EqualTo(80), "only units farther than the scan radius count; exactly on it is arrived; a negative value reads as 0");
			Assert.That(DefendTwo.CommitValue(Array.Empty<(long, long)>(), radiusSq), Is.EqualTo(0));
		}

		// ---- replace rule ----

		[Test]
		public void ABlindRefreshNeverReplacesAnActiveSeenAlarmOfTheSameVictimButASeenOneReplacesABlindOne()
		{
			var seen = Seen("A", 100, 2000);
			Assert.That(DefendTwo.Replaces(seen, Blind("A", 150), 160, 450), Is.False, "a blind refresh would erase the seen attack value");
			Assert.That(DefendTwo.Replaces(seen, Blind("A", 600), 700, 450), Is.True, "once the seen alarm has expired the blind one takes the board");
			Assert.That(DefendTwo.Replaces(seen, Blind("B", 150), 160, 450), Is.False, "a blind alarm of another victim (attack 0) does not take the board either");
			Assert.That(DefendTwo.Replaces(Blind("A", 100), Seen("A", 150, 500), 160, 450), Is.True);
			Assert.That(DefendTwo.Replaces(Blind("A", 100), Seen("B", 150, 500), 160, 450), Is.True, "a seen attack elsewhere beats a blind alarm (0)");
			Assert.That(DefendTwo.Replaces(Blind("A", 100), Blind("A", 150), 160, 450), Is.True, "a blind refresh of a blind alarm");
			Assert.That(DefendTwo.Replaces(null, Blind("A", 150), 160, 450), Is.True);
			Assert.That(DefendTwo.Replaces(seen, Seen("A", 150, 100), 160, 450), Is.True, "the same-victim refresh takes the newest numbers, as today");
			Assert.That(DefendTwo.Replaces(seen, Seen("B", 150, 100), 160, 450), Is.False, "another victim with a smaller attack, as today");
			Assert.That(DefendTwo.Replaces(seen, Seen("B", 150, 9000), 160, 450), Is.True, "another victim with a larger attack takes the board, as today");
		}

		[Test]
		public void TheDevRowOfAPassIsOneFlatString()
		{
			Assert.That(DefendTwo.Detail("p1", true, 600, 100, 400, 1200, 200, 5, 2, 1, 0, false),
				Is.EqualTo("victim=p1;blind=1;seen=0;need=600;committed=100;spare=400;total_spare=1200;quota=200;pool=5;drafted=2;squads=1;blobs=0"));
		}

		// ---- source pins ----

		[Test]
		public void TheRowHasItsOwnAblationIdAndOnlyTheSixNonClassicBlocksArmIt()
		{
			Assert.That(AblationIds.TeamDefendTwoTier, Is.EqualTo("tm_defend_two_tier"));
			Assert.That(Regex.IsMatch(AblationIds.TeamDefendTwoTier, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.TeamDefendTwoTier.cs"), Does.Contain("public const string TeamDefendTwoTier = \"tm_defend_two_tier\";"));

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.V07Defend.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamDefendTwoTierEnabled = false;"), "off in code: only the yaml arms it");
			Assert.That(info, Does.Contain("public readonly int TeamDefendMarginPercent = 125;"));
			Assert.That(info, Does.Contain("public readonly int TeamDefendBlindValue = 600;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Count(yaml[..classic], "\n\t\tTeamDefendTwoTierEnabled: true\n"), Is.EqualTo(6), "the six blocks that carry both ally-defend switches");
			Assert.That(yaml[classic..], Does.Not.Contain("TeamDefendTwoTierEnabled"), "classic untouched");
			Assert.That(Count(yaml, "TeamDefendTwoTierEnabled:"), Is.EqualTo(6));
		}

		[Test]
		public void TheRowNeedsTheTeamSwitchTheAllySwitchTheInfoSwitchAndTheLatch()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateTeamDefendTwo;"));
			Assert.That(part, Does.Contain("internal bool TeamDefendTwoOn => TeamDefendOn && DefendAllyActive && Info.TeamDefendTwoTierEnabled && !ablateTeamDefendTwo.Off(Player, AblationIds.TeamDefendTwoTier);"));
		}

		[Test]
		public void TwoTierBranchesBeforeTheOldDraftPathsAndTheOldPathsAreUntouched()
		{
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			var two = team.IndexOf("if (TeamDefendTwoOn)\n\t\t\t{\n\t\t\t\tTeamDefendTwoTick(bot);\n\t\t\t\treturn;\n\t\t\t}", StringComparison.Ordinal);
			var list = team.IndexOf("var draftable = unitsHangingAroundTheBase", StringComparison.Ordinal);
			var one = team.IndexOf("if (DefendOneOn)", StringComparison.Ordinal);
			Assert.That(two, Is.GreaterThan(0));
			Assert.That(two, Is.LessThan(list), "precedence: TwoTier > sq_defend_one > Q4b arbiter > whole pool");
			Assert.That(two, Is.LessThan(one));
			Assert.That(Count(team, "TeamDefendTwoTick("), Is.EqualTo(1));

			// row off: the one-attacker alarm, the pick < 0 return and the Serious floor are the old tests (baseAsset is false without the row)
			Assert.That(team, Does.Contain("var baseAsset = two && TeamDefendBaseAsset(victim);"));
			Assert.That(team, Does.Contain("if (!TeamDefendCA.Serious(attack, baseAsset ? 1 : Info.TeamLeadDefendTeamMinAttackValue))"));
			Assert.That(team, Does.Contain("two ? DefendTwo.Replaces(team.DefendAlarm, alarm, now, Info.TeamLeadDefendTeamAlarmTicks) : TeamDefendCA.Replaces(team.DefendAlarm, alarm, now, Info.TeamLeadDefendTeamAlarmTicks)"));
			Assert.That(team, Does.Contain("var need = DefendArbiterCA.AllyPoolNeed(alarm.AttackValue, Info.ArbiterDefendMarginPercent, teamLeadAliveSeats - 1);"));
			Assert.That(Count(team, "team.DefendAlarm = alarm;"), Is.EqualTo(1));
		}

		[Test]
		public void FogOnlyTheSeenAttackersCountAndNothingOfAnUnseenOneIsRead()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");

			// the assessment: whatever FoggedScans says, the T2 attacker must be viewable now
			Assert.That(part, Does.Contain("bool TeamDefendCountsAttacker(Actor e, bool two) => two ? IsPreferredEnemyUnit(e) && IsNotHiddenUnit(e) : IsPreferredObservedEnemyUnit(e);"));
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(team, Does.Contain("if (!TeamDefendCountsAttacker(e, two))"));
			Assert.That(team, Does.Not.Contain("!IsPreferredObservedEnemyUnit(e)"), "the old test is reached only through the helper");
			var notHidden = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(notHidden, Does.Contain("public bool IsNotHiddenUnit(Actor a)"));

			// the one place that resolves the alarm's attacker id: visibility first, then liveness
			Assert.That(Count(part, "GetActorById("), Is.EqualTo(1), "only DefendTwoSeenAttacker resolves the attacker");
			Assert.That(part, Does.Contain("return a != null && a.CanBeViewedByPlayer(Player) && a.IsInWorld && !a.IsDead ? a : null;"));
			Assert.That(part, Does.Contain("if (DefendTwo.IsBlind(alarm.AttackerId))\n\t\t\t\treturn null;"));

			// an unseen attacker: the terrain target at the alarm's position, the pick ordered by it, never the attacker actor
			Assert.That(part, Does.Contain("squad.Target = Target.FromPos(alarm.AttackerPos);"));
			Assert.That(part, Does.Contain("var attackerPos = alarm.AttackerPos;"));
			Assert.That(part, Does.Contain("(draftable[i].Actor.CenterPosition - attackerPos).HorizontalLengthSquared"));
			Assert.That(part, Does.Contain("(s.CenterPosition - attackerPos).HorizontalLengthSquared"));
			Assert.That(part, Does.Not.Contain("alarm.VictimId"), "the ally victim actor is never a target (an ally is not targetable)");
		}

		[Test]
		public void ABaseAssetAlarmsAtAnySizeAndABlindAlarmCarriesOnlyTheDamagedLocation()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Contain("static bool TeamDefendBaseAsset(Actor a) => a.Info.HasTraitInfo<HarvesterInfo>() || a.Info.HasTraitInfo<BuildingInfo>() || a.Info.HasTraitInfo<BaseBuildingInfo>();"));
			Assert.That(part, Does.Contain("new TeamDefendCA.Alarm(now, TeamLeadKeyOf(victim.Owner), victim.ActorID, victim.CenterPosition, 0, victim.CenterPosition, 0, defence);"));

			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(team, Does.Contain("return baseAsset && TeamDefendRaiseBlind(team, victim, defence, now);"), "a non-base friendly with no seen attacker raises nothing, and a base asset raises the blind alarm");
		}

		[Test]
		public void TheLedgerOrderIsPublishSpareThenQuotaThenDraftThenPublishCommit()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			var spare = part.IndexOf("team.PublishDefendSpare(key, spare, now);", StringComparison.Ordinal);
			var sums = part.IndexOf("team.DefendSumsExcept(key, now, DefendTwo.LedgerMaxAgeTicks)", StringComparison.Ordinal);
			var quota = part.IndexOf("DefendTwo.Quota(need, committed, spare, others.Spare + spare)", StringComparison.Ordinal);
			var pick = part.IndexOf("DefendArbiterCA.PickNearest(candidates, quota)", StringComparison.Ordinal);
			var commit = part.LastIndexOf("team.PublishDefendCommit(key, DefendTwoCommit(alarm, home), now);", StringComparison.Ordinal);
			Assert.That(spare, Is.GreaterThan(0));
			Assert.That(sums, Is.GreaterThan(spare));
			Assert.That(quota, Is.GreaterThan(sums));
			Assert.That(pick, Is.GreaterThan(quota));
			Assert.That(commit, Is.GreaterThan(pick));
			Assert.That(part, Does.Contain("team.BindDefendLedger(alarm.VictimKey);"));
			Assert.That(part, Does.Contain("team.ClearDefendLedger();"));
		}

		[Test]
		public void APushedBlobKeepsItsOrderUnlessTheAllyWouldDieAndTheOwnBaseKeepsItsUnits()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Contain("var blob = !free && s.Type == SquadCAType.Rush && s.TeamPushSeq > 0 && s.FuzzyStateMachine?.CurrentState is GroundUnitsAttackMoveStateCA;"));
			Assert.That(part, Does.Contain("if (!TeamDefendCA.SquadResponds(free, blob, wouldDie))"), "a blob answers only when the ally would die");
			Assert.That(part, Does.Contain("DefendArbiterCA.MayTake(priority, ArmyRole.Main, holder)"));

			// own-base precedence: the victim's own bot does not answer, a seat fighting at home publishes spare 0 and keeps its units
			Assert.That(part, Does.Contain("if (alarm.VictimKey == key || fightingHome)\n\t\t\t{\n\t\t\t\tteam.PublishDefendSpare(key, 0, now);"));
			Assert.That(part, Does.Contain("var fightingHome = !defendTwoHome && home is { IsValid: true } && home.IsTargetValid"));
		}

		[Test]
		public void TheDefendersAreReleasedWhenTheAlarmIsOverOrNothingIsLeftAtThePosition()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Contain("if (!active || (arrived && DefendTwoSeenAttacker(alarm) == null))\n\t\t\t\thome.Target = Target.Invalid;"), "the quiet Protection squad then takes the existing ShouldReleaseDefenders path");
			Assert.That(part, Does.Contain("var arrived = (home.Units[0].Actor.CenterPosition - home.Target.CenterPosition).HorizontalLengthSquared <= DefendTwoScanSquared();"));
			Assert.That(part, Does.Contain("DefendTwoMaintain(home, alarm, false);"), "the inactive alarm releases too");
			Assert.That(part, Does.Contain("home.Target.Type != TargetType.Terrain"), "only the terrain target T2 set is cleared; an actor target ends through the state's own validity test");
		}

		[Test]
		public void ATerrainTargetNeverReachesATargetActorReadInTheProtectionStateAndTheReleaseGuardIsPinned()
		{
			// No harness in this repo can run a squad state (it needs a live World, Actors and the squad manager), so the branch is pinned by source: it sits after the quiet/release path and before the first TargetActor dereference.
			var state = RepoFile(Bots + "Squads/States/ProtectionStatesCA.cs");
			var quiet = state.IndexOf("if (closestEnemy == null && (!owner.IsTargetValid || !owner.IsTargetVisible))", StringComparison.Ordinal);
			var branch = state.IndexOf("if (closestEnemy == null && owner.TargetActor == null && owner.Target.Type == TargetType.Terrain)", StringComparison.Ordinal);
			var canAttack = state.IndexOf("CanAttackTarget(u.Actor, owner.TargetActor)", StringComparison.Ordinal);
			var read = state.IndexOf("owner.TargetActor.CenterPosition", StringComparison.Ordinal);
			Assert.That(quiet, Is.GreaterThan(0));
			Assert.That(branch, Is.GreaterThan(quiet), "the quiet release path stays first");
			Assert.That(branch, Is.LessThan(canAttack));
			Assert.That(branch, Is.LessThan(read));

			var open = state.IndexOf('{', branch);
			var body = state[open..state.IndexOf("return;", open, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("QueueDeduped(owner, \"AttackMove\", SquadOrderKey.ForCell(\"AttackMove\", cell), Target.FromCell(owner.World, cell), owner.Units.Select(u => u.Actor));"));
			Assert.That(body, Does.Not.Contain("TargetActor"), "the terrain branch reads no actor target");

			// F3: the squad is valid (at least one unit) before any Units[0] read of the release test
			Assert.That(RepoFile(Bots + "Squads/SquadCA.cs"), Does.Contain("public bool IsValid => Units.Count > 0;"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs"), Does.Contain("if (!defendTwoHome || home is not { IsValid: true } || home.Target.Type != TargetType.Terrain)\n\t\t\t\treturn;"));
		}

		[Test]
		public void TheT1LocalResponseAndTheSharedCooldownAreNotTouched()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Not.Contain("defendAllyNextResponse"), "T2 neither reads nor writes the T1 cooldown");
			Assert.That(part, Does.Not.Contain("DefendAllyRespond("));
			var ally = RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Not.Contain("TeamDefendTwo"), "DEFEND-ALLY (T1) has no T2 branch");
			Assert.That(ally, Does.Contain("if (defendAllyAttacked.Count == 0 || now < defendAllyNextResponse)"));
		}

		[Test]
		public void TheDevRowIsRegisteredAndThePureSeamIsEngineFree()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendTwo.cs");
			Assert.That(part, Does.Contain("DiagnosticTeamEvent(\"team_defend_two\", DefendTwo.Detail("));
			var pure = RepoFile(Bots + "BotModuleLogic/DefendTwo.cs");
			foreach (var banned in new[] { "Random", "World ", "Actor ", "Player", "FindActors", "GetActorById" })
				Assert.That(pure, Does.Not.Contain(banned), "the pure seam is engine-free: " + banned);
		}
	}
}
