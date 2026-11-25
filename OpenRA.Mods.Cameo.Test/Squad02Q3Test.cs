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
	/// CONSOLIDATE-02 Q3 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 4, Opus D2): the one defend arbiter DefendArbiterCA (row sq_defend_arbiter under sq_pipeline_02).
	/// X6 a raid never dismisses a gathering blob; X7 one harass never drains the pool; X8 every teammate answers with pool, idle squads and home guard, the pushed
	/// blob only when the ally would fall (attack value above its defence value); X9 a local hit never hijacks a squad; P0 recalls only the share needed.
	/// </summary>
	[TestFixture]
	public class Squad02Q3Test
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

		[Test]
		public void FallsIsTheAttackAboveTheDefenceWithNoKnob()
		{
			Assert.That(DefendArbiterCA.Falls(3001, 3000), Is.True);
			Assert.That(DefendArbiterCA.Falls(3000, 3000), Is.False, "equal holds");
			Assert.That(DefendArbiterCA.Falls(0, -5), Is.False, "no attack never falls");
			Assert.That(DefendArbiterCA.Falls(100, -5), Is.True, "no defence at all");

			Assert.That(DefendArbiterCA.Own(true, 5000, 4000), Is.EqualTo(DefendPriority.P0OwnBaseFalls));
			Assert.That(DefendArbiterCA.Own(false, 5000, 4000), Is.EqualTo(DefendPriority.P1OwnThreat), "a harvester hit is never P0");
			Assert.That(DefendArbiterCA.Own(true, 3000, 4000), Is.EqualTo(DefendPriority.P1OwnThreat), "the base holds");
			Assert.That(DefendArbiterCA.Ally(5000, 4000), Is.EqualTo(DefendPriority.P2AllyFalls));
			Assert.That(DefendArbiterCA.Ally(4000, 4000), Is.EqualTo(DefendPriority.P3AllySerious));
		}

		// X6: a raid of 1500 hits the base while the seat's blob gathers at the mid rally (inside the home radius). P1 takes the pool and the quiet squads only.
		[Test]
		public void ARaidOnTheBaseNeverTakesTheGatheringOrPushedBlob()
		{
			Assert.That(DefendArbiterCA.Own(true, 1500, 6000), Is.EqualTo(DefendPriority.P1OwnThreat));
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Pool), Is.True);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Quiet), Is.True);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Gathering), Is.False);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Pushed), Is.False);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P1OwnThreat, ArmyRole.Main, ArmyHolder.Busy), Is.False);

			// The detachment planner fed with the P1 candidates (pool + quiet only): the gathering units (ids 100+) are not even candidates, so nothing of the blob is detached.
			var pool = Enumerable.Range(1, 4).Select(i => new DetachCandidate((uint)i, 600, 100L * i, true));
			var plan = DefenceDetachment.Plan(1500, 20000, pool, 150, 35, 0);
			Assert.That(plan.RecallAll, Is.False, "the arbiter never recalls everything (recall share 0)");
			Assert.That(plan.Detached.All(id => id < 100), Is.True);
			Assert.That(plan.DetachedValue, Is.GreaterThanOrEqualTo(2250), "150% of the threat from the pool");
		}

		// P0: an own yard is hit and the base would fall: the blob may be recalled, but only the share needed, nearest first.
		[Test]
		public void TheOwnBaseFallingRecallsOnlyTheShareNeededNearestFirst()
		{
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P0OwnBaseFalls, ArmyRole.Main, ArmyHolder.Pushed), Is.True);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P0OwnBaseFalls, ArmyRole.Main, ArmyHolder.Gathering), Is.True);
			Assert.That(DefendArbiterCA.MayTake(DefendPriority.P0OwnBaseFalls, ArmyRole.Harass, ArmyHolder.Busy), Is.False, "raids keep their job");

			var need = DefendArbiterCA.Need(6000, 4000, 150);
			Assert.That(need, Is.EqualTo(3000), "150% of the 2000 the defence does not cover");
			var blob = Enumerable.Range(0, 30).Select(i => new DefendCandidate(i, 800, (30L - i) * 1000)).ToList();
			var pick = DefendArbiterCA.PickNearest(blob, need);
			Assert.That(pick.Count, Is.EqualTo(4), "4 x 800 >= 3000: 4 of 30 units come back");
			Assert.That(pick, Is.EqualTo(new[] { 29, 28, 27, 26 }), "the nearest ones");
			Assert.That(DefendArbiterCA.PickNearest(blob, 0), Is.Empty, "nothing needed, nothing taken");
			Assert.That(DefendArbiterCA.PickNearest(blob, long.MaxValue).Count, Is.EqualTo(30));
		}

		// X7: an enemy harasser (600) shoots a harvester. The whole pool (20 units) used to be drafted; the arbiter sends what the harass needs.
		[Test]
		public void OneHarassNeverDrainsThePool()
		{
			var need = DefendArbiterCA.Need(600, 0, 150);
			Assert.That(need, Is.EqualTo(900));
			var pool = Enumerable.Range(0, 20).Select(i => new DefendCandidate(i, 700, i * 10L));
			Assert.That(DefendArbiterCA.PickNearest(pool, need).Count, Is.EqualTo(2));

			Assert.That(DefendArbiterCA.BlindDraft(20, 3), Is.EqualTo(3), "an attacker in the fog: a few units, not the pool");
			Assert.That(DefendArbiterCA.BlindDraft(2, 3), Is.EqualTo(2));
			Assert.That(DefendArbiterCA.BlindDraft(5, -1), Is.EqualTo(0));
			Assert.That(DefendArbiterCA.Need(600, 900, 150), Is.EqualTo(0), "covered by the defence there");
			Assert.That(DefendArbiterCA.Need(1000, 0, 50), Is.EqualTo(1000), "the margin is at least 100%");
		}

		// X8 / Opus D2: every teammate answers an ally alarm with its home, idle and defence pools; a pushed blob is recalled only when the ally would otherwise fall.
		[Test]
		public void EveryTeammateAnswersWithPoolIdleAndHomeGuardAndThePushedBlobOnlyWhenTheAllyFalls()
		{
			var serious = DefendArbiterCA.Ally(2000, 5000);
			Assert.That(serious, Is.EqualTo(DefendPriority.P3AllySerious));
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.Main, ArmyHolder.Pool), Is.True);
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.Main, ArmyHolder.Quiet), Is.True);
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.DefenceHome, ArmyHolder.Quiet), Is.True, "an idle home guard answers");
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.DefenceHome, ArmyHolder.Busy), Is.False, "a home guard fighting at home keeps its job");
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.Main, ArmyHolder.Pushed), Is.False);
			Assert.That(DefendArbiterCA.MayTake(serious, ArmyRole.Main, ArmyHolder.Gathering), Is.False);

			var falls = DefendArbiterCA.Ally(5001, 5000);
			Assert.That(falls, Is.EqualTo(DefendPriority.P2AllyFalls));
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Main, ArmyHolder.Pushed), Is.True);
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Main, ArmyHolder.Gathering), Is.True);
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Main, ArmyHolder.Busy), Is.False, "a staging or marching squad outside the order keeps going");
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Harass, ArmyHolder.Quiet), Is.False);
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Attached, ArmyHolder.Quiet), Is.False);
			Assert.That(DefendArbiterCA.MayTake(falls, ArmyRole.Other, ArmyHolder.Quiet), Is.False);
		}

		// X9: a hit ally harvester 25 cells from a staging or gathering squad used to hijack it. A local hit takes pool units in the radius only.
		[Test]
		public void ALocalHitOrAnEscortNeverTakesASquad()
		{
			foreach (var p in new[] { DefendPriority.P4LocalHit, DefendPriority.P5Escort })
			{
				Assert.That(DefendArbiterCA.MayTake(p, ArmyRole.Main, ArmyHolder.Pool), Is.True, p.ToString());
				foreach (var h in new[] { ArmyHolder.Quiet, ArmyHolder.Gathering, ArmyHolder.Pushed, ArmyHolder.Busy })
					Assert.That(DefendArbiterCA.MayTake(p, ArmyRole.Main, h), Is.False, p + " " + h);
				Assert.That(DefendArbiterCA.MayTake(p, ArmyRole.DefenceHome, ArmyHolder.Busy), Is.True, "the home guard itself");
			}
		}

		[Test]
		public void TheFeedersAskTheArbiterBehindItsRowAndTheOldPathsStayWithItOff()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			Assert.That(wiring, Does.Contain("internal bool DefendArbiterOn => ArmyPipelineOn && !ablateSqDefendArbiter.Off(Player, AblationIds.SqDefendArbiter);"));
			var own = Between(wiring, "void ArbiterProtectOwn(", "#if CAMEO_DEV_LOGGING");
			Assert.That(own, Does.Contain("var priority = DefendArbiterCA.Own(core, attack, defence + poolValue);"));
			Assert.That(own, Does.Contain("need = DefendArbiterCA.Need(attack, defence, Info.ArbiterDefendMarginPercent);"));
			Assert.That(own, Does.Contain("!DefendArbiterCA.MayTake(priority, ArmyRole.Main, ArmyHolderOf(s))"));
			Assert.That(own, Does.Contain("DefendArbiterCA.BlindDraft(draftable.Count, Info.ArbiterBlindDraftUnits)"));

			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Contain("if (DefendArbiterOn)\n\t\t\t\tArbiterProtectOwn(attacker, protectTarget, victim, protectSq);\n\t\t\telse if (!protectSq.IsValid)"), "off = the old whole-pool draft");
			Assert.That(sm, Does.Contain("protectOwnVictim = self;\n\t\t\t\tProtectOwn(e.Attacker);"));
			Assert.That(sm, Does.Contain("if (request == null && Info.FastSquadsReactToThreats && !DefendArbiterOn)"));

			var ally = RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("if (DefendArbiterOn && !DefendArbiterCA.MayTake(DefendPriority.P4LocalHit, ArmyRole.Main, ArmyHolderOf(s)))"));

			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(team, Does.Contain("var wouldDie = arbiter ? priority == DefendPriority.P2AllyFalls : TeamDefendCA.WouldDie(alarm.AttackValue, alarm.DefenceValue, Info.TeamLeadDefendTeamDiePercent);"));
			Assert.That(team, Does.Contain("|| !DefendArbiterCA.MayTake(priority, ArmyRole.Main, holder))"));

			var agg = RepoFile(Bots + "SquadManagerBotModuleCA.AggTeam.cs");
			Assert.That(agg, Does.Contain("AggTeamDetachCandidates(arbiter && !falls)"), "P1 = pool and quiet squads only");
			Assert.That(agg, Does.Contain("arbiter ? 0 : AggS(nameof(SquadManagerBotModuleCAInfo.AggTeamRecallSharePercent))"), "never a recall of everything under the arbiter");
		}

		[Test]
		public void TheParametersAndTheRowAreRegistered()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.Pipeline02.cs");
			Assert.That(info, Does.Contain("public readonly int ArbiterDefendMarginPercent = 150;"));
			Assert.That(info, Does.Contain("public readonly int ArbiterBlindDraftUnits = 3;"));
			Assert.That(AblationIds.SqDefendArbiter, Is.EqualTo("sq_defend_arbiter"));
		}
	}
}
