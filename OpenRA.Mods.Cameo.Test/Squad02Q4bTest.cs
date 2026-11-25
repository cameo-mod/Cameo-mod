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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q4b (the independent review of Q2-Q4, findings F1-F6): a finite cap for a persistent order held below the bar (F1), the defend arbiter replaces the
	/// recall only together with the one launcher (F2), the TEAM-DEFEND pool answer is a share that walks home again (F3), the arbiter never takes a squad the team lead keeps
	/// (F4), the blind draft reads only what the bot sees (F5), and the pool is not counted twice in the P0 test (F6).
	/// </summary>
	[TestFixture]
	public class Squad02Q4bTest
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

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		// F1: a persistent order below the bar never dissolved (below_bar is skipped for it, max_age needs !Extending, the grace hold skips no_reports): it gathered forever.
		[Test]
		public void APersistentOrderHeldBelowTheBarIsClosedAtAFiniteCap()
		{
			Assert.That(TeamLeadCA.PersistentGatherCap(9000, 3000), Is.EqualTo(9000));
			Assert.That(TeamLeadCA.PersistentGatherCap(1000, 3000), Is.EqualTo(3000), "never shorter than the plain dissolve");
			Assert.That(TeamLeadCA.PersistentGatherCap(int.MaxValue, 3000), Is.EqualTo(30000), "a huge value is bounded");
			Assert.That(TeamLeadCA.PersistentGatherCap(0, 0), Is.EqualTo(1));

			var cap = TeamLeadCA.PersistentGatherCap(9000, 3000);
			Assert.That(TeamLeadCA.PushBarDissolve(9099, 100, cap), Is.False);
			Assert.That(TeamLeadCA.PushBarDissolve(9100, 100, cap), Is.True, "9000 ticks after the first held push");
			Assert.That(TeamLeadCA.PushBarDissolve(999999, -1, cap), Is.False, "never held = never closed by the cap");

			Assert.That(TeamLeadCA.EngagedMaxAge(0), Is.EqualTo(1500), "a yaml 0 cannot close a pushed order on its push tick");
			Assert.That(TeamLeadCA.EngagedMaxAge(6000), Is.EqualTo(6000));
			Assert.That(TeamLeadCA.EngagedMaxAge(999999), Is.EqualTo(30000));
		}

		[Test]
		public void TheGatherCapIsWiredBehindTheOldBelowBarAndBeforeTheMaxAgeTest()
		{
			var maintain = Between(RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs"), "void TeamLeadMaintainOrder(", "TEAM-BLOB-01: a rally order only gathers");
			var cap = maintain.IndexOf(": order.Extending && order.Persistent && TeamLeadCA.PushBarDissolve(now, order.PushBarSince, TeamLeadCA.PersistentGatherCap(Info.ArmyGatherMaxTicks, Info.TeamLeadPushBarMaxTicks)) ? \"gather_cap\"", StringComparison.Ordinal);
			var below = maintain.IndexOf("? \"below_bar\"", StringComparison.Ordinal);
			var maxAge = maintain.IndexOf("? \"max_age\"", StringComparison.Ordinal);
			Assert.That(cap, Is.GreaterThan(below), "after the plain below_bar");
			Assert.That(below, Is.GreaterThan(0));
			Assert.That(maxAge, Is.GreaterThan(cap), "before the max_age and grace tests it would otherwise skip");

			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs"), Does.Contain("public readonly int ArmyGatherMaxTicks = 9000;"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs"), Does.Contain(": now - order.PushTick > TeamLeadCA.EngagedMaxAge(Info.ArmyEngagedMaxTicks) ? \"done_max_age\""));
		}

		// F2: Q3 without Q4 changed the stalemate breaker's home gate (it reads RecallAll); the recall and the DF-3/DF-4 fold go only together with the one launcher.
		[Test]
		public void TheArbiterReplacesTheRecallOnlyTogetherWithTheOneLauncher()
		{
			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(pipe, Does.Contain("internal bool ArmyDefendOwnsRecall => DefendArbiterOn && ArmyLaunchArbiterOn;"));
			Assert.That(pipe, Does.Contain("internal bool ArmyLaunchArbiterOn => ArmyPipelineOn && TeamLeadCadenceActive && !ablateSqLaunchArbiter.Off(Player, AblationIds.SqLaunchArbiter);"),
				"no order is ever published without the cadence, and the squad issue is bypassed");

			var agg = RepoFile(Bots + "SquadManagerBotModuleCA.AggTeam.cs");
			Assert.That(agg, Does.Contain("ArmyDefendOwnsRecall ? 0 : AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamRecallSharePercent))"));
			Assert.That(agg, Does.Contain("AggTeamDetachCandidates(arbiter && !falls)"), "the quiet-only candidates (X6) stay on the arbiter row alone");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.cs"), Does.Contain("Info.FastSquadsReactToThreats && !ArmyDefendOwnsRecall"));
		}

		// F3: TEAM-DEFEND drafted the whole pool of every teammate; the team sends the attack's worth at the margin, split over the answering allies.
		[Test]
		public void EachTeammateSendsItsShareOfTheAttackNotItsWholePool()
		{
			Assert.That(DefendArbiterCA.AllyPoolNeed(2000, 150, 3), Is.EqualTo(1000), "3000 at the margin over 3 allies");
			Assert.That(DefendArbiterCA.AllyPoolNeed(2000, 150, 0), Is.EqualTo(3000), "no answering ally counted: the full share (at least 1)");
			Assert.That(DefendArbiterCA.AllyPoolNeed(1001, 150, 4), Is.EqualTo(376), "rounded up");
			Assert.That(DefendArbiterCA.AllyPoolNeed(0, 150, 3), Is.EqualTo(0), "no attack, nothing sent");
			Assert.That(DefendArbiterCA.AllyPoolNeed(1000, 50, 1), Is.EqualTo(1000), "the margin is at least 100%");

			var pool = Enumerable.Range(0, 20).Select(i => new DefendCandidate(i, 700, (20L - i) * 100));
			var pick = DefendArbiterCA.PickNearest(pool, DefendArbiterCA.AllyPoolNeed(2000, 150, 3));
			Assert.That(pick, Is.EqualTo(new[] { 19, 18 }), "two nearest units of twenty (1000 needs 2 x 700), not the whole pool");

			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(team, Does.Contain("var need = DefendArbiterCA.AllyPoolNeed(alarm.AttackValue, Info.ArbiterDefendMarginPercent, teamLeadAliveSeats - 1);"));
			Assert.That(team, Does.Contain("draftable = pick.Select(i => draftable[i]).ToList();"));
			var draft = Between(team, "var draftable = unitsHangingAroundTheBase", "if (draftable.Count > 0)");
			Assert.That(draft, Does.Contain("if (arbiter)"), "off = the whole pool, as before");
		}

		[Test]
		public void AReleasedDefenderFarFromHomeWalksBackAndTheFallsTestCountsOnlyTheHomePool()
		{
			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			var release = Between(sm, "internal void ReleaseDefenders(", "protectSq.Units.Clear();");
			Assert.That(release, Does.Contain("if (DefendArbiterOn)"));
			Assert.That(release, Does.Contain("farFromHome.Add(u);"));
			Assert.That(release, Does.Contain("AggTeamSendHome(bot, farFromHome, World.Map.CellContaining(homePos));"));

			var agg = RepoFile(Bots + "SquadManagerBotModuleCA.AggTeam.cs");
			Assert.That(agg, Does.Contain("long AggTeamHomeValue(bool atHomeOnly = false)"));
			Assert.That(agg, Does.Contain("provider.StalemateHomeDefenceValue + AggTeamHomeValue(true));"));
			Assert.That(agg, Does.Contain("AggTeamPolicyCA.MayLeaveHome(AggTeamHomeValue(), SquadValueOf(s), reserve, blocked)"), "the launch gate keeps counting the whole pool");
		}

		// F4: the arbiter's "quiet" ignored the old DefendAllyFreeSquad guard (a fresh squad, a squad gathering for the order) and read a pushed idle squad as quiet.
		[Test]
		public void AQuietSquadTheLeaderKeepsIsNeverTakenAsQuiet()
		{
			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			var holder = Between(pipe, "ArmyHolder ArmyDefendHolderOf(SquadCA s)", "// A construction yard");
			Assert.That(holder, Does.Contain("if (holder != ArmyHolder.Quiet)"));
			Assert.That(holder, Does.Contain("if (ArmyEngagedOrderOf(s) != null)"));
			Assert.That(holder, Does.Contain("return TeamLeadKeepsSquad(s) ? ArmyHolder.Gathering : holder;"));

			// The two quiet-squad feeders of the arbiter read it; DEFEND-ALLY (P4) never takes a squad, so its call stays on the plain holder.
			Assert.That(pipe, Does.Contain("|| !DefendArbiterCA.MayTake(priority, ArmyRole.Main, ArmyDefendHolderOf(s)))"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs"), Does.Contain("var holder = ArmyDefendHolderOf(s);"));

			// A Gathering or Pushed squad keeps its P0/P2 answers, a Quiet one the P1/P3 ones.
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Gathering), Is.False);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P3AllySerious, ArmyRole.Main, ArmyHolder.Pushed), Is.False);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P2AllyFalls, ArmyRole.Main, ArmyHolder.Gathering), Is.True);
		}

		// F5 + F6: the blind draft reads the VICTIM's position when the attacker is hidden; a visible armed attacker outside the scan radius is an attack of its own value;
		// the pool inside the scan radius is already in `defence` and is not added again.
		[Test]
		public void TheBlindDraftReadsOnlyWhatTheBotSeesAndTheP0TestCountsThePoolOnce()
		{
			var own = Between(RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs"), "void ArbiterProtectOwn(", "#if CAMEO_DEV_LOGGING");
			Assert.That(own, Does.Contain("var at = protectTarget != null && protectTarget.IsInWorld ? protectTarget.CenterPosition"));
			Assert.That(own, Does.Not.Contain("attacker.CenterPosition"), "a hidden attacker's position is never read");
			Assert.That(own, Does.Contain("attack = UnitValue(protectTarget);"));
			Assert.That(own, Does.Contain("protectTarget.Info.HasTraitInfo<AttackBaseInfo>()"));
			Assert.That(own, Does.Contain("if ((u.Actor.CenterPosition - at).HorizontalLengthSquared > scanSquared)"));
			Assert.That(own, Does.Contain("var priority = DefendArbiterCA.Own(core, attack, defence + poolValue);"), "the P0 test itself is unchanged");

			// Q4c (CM): the defence is measured on EVERY path (the pool inside the radius is counted nowhere else); the attack only from an attacker the bot sees.
			var assess = own.IndexOf("ArbiterAssess(at, out var seenAttack, out var defence);", StringComparison.Ordinal);
			var seen = own.IndexOf("if (protectTarget != null)", StringComparison.Ordinal);
			Assert.That(assess, Is.GreaterThan(0));
			Assert.That(seen, Is.GreaterThan(assess), "the assessment runs before, and outside, the seen-attacker branch");
			Assert.That(own, Does.Contain("attack = seenAttack;"));
		}

		[Test]
		public void TheNitsAreFixed()
		{
			var pipe = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(pipe, Does.Contain("if (DiagnosticWriter != null)\n\t\t\t\t\tDiagnosticSquadEvent(s, \"army_attach\""), "no string built for the DEV record in a release game");
			Assert.That(pipe, Does.Contain("counted ? total : total + candidateValue"), "a unit already in the home guard is in the total once");
		}
	}
}
