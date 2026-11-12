#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	// FREEZE9 CF seams (Workhorse; Opus design): S1 A9 resolver, S2 A7 finish flag, S3 rendezvous row, the breaker's three
	// home-block sites and the agg-core idle/march holds for squads the agg-team defence plan keeps home.
	[TestFixture]
	public sealed class FreezeNineSeamsTest
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string GroundStates() => Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");

		static string Stalemate() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate.cs");

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

		static bool Blocked(SquadManagerBotModuleCA m) =>
			(bool)typeof(SquadManagerBotModuleCA).GetMethod("StalemateBlocked", Any).Invoke(m, new object[] { null });

		// ---- S1: the A9 accessor reads the layered resolver ----

		[Test]
		public void WithEmptyTablesEveryAggressionThresholdResolvesToItsYamlValue()
		{
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var (key, _, _) in AggressionPolicyCA.Ranges)
				foreach (var (own, enemy) in new[] { ((string)null, (string)null), ("allies", "soviet"), ("gdi", null) })
					Assert.That(AggressionParams.Resolve(info, key, own, enemy), Is.EqualTo(info.AggressionValue(key)), $"{key} {own}>{enemy}");
		}

		[Test]
		public void TheResolverRangeOfEveryAggressionThresholdIsItsRulesetRange()
		{
			// Resolve clamps to the Desc range; a legal yaml value at either end of the RulesetLoaded range must pass unchanged.
			foreach (var (key, min, max) in AggressionPolicyCA.Ranges)
			{
				foreach (var v in new[] { min, max })
				{
					var info = new SquadManagerBotModuleCAInfo();
					typeof(SquadManagerBotModuleCAInfo).GetField(key).SetValue(info, v);
					Assert.That(AggressionParams.Resolve(info, key, "allies", "soviet"), Is.EqualTo(v), $"{key} = {v}");
				}
			}
		}

		// ---- S2: the finish seam is the agg-team flag ----

		[Test]
		public void TheFinishSeamStaysClosedWithCgOffEvenWithALatchedPush()
		{
			var m = Shell(false);
			typeof(SquadManagerBotModuleCA).GetField("aggFinishActive", Any).SetValue(m, true);
			Assert.That(m.AggressionFinishActive, Is.False, "the OFF arm and a CG-off arm never see the finishing push");
		}

		// ---- S3: a squad at the rendezvous is not engaged ----

		[Test]
		public void ASquadWaitingAtTheRendezvousIsNotEngaged()
		{
			Assert.That(AggressionPolicyCA.IsEngagedState(new AggTeamRendezvousStateCA(null, 0)), Is.False);
		}

		// ---- the breaker's home block ----

		[Test]
		public void TheBreakerFailsClosedWithoutAProviderOnBothCgArms()
		{
			Assert.That(Blocked(Shell(false)), Is.True, "CG off: the HOTFIX03 predicate");
			Assert.That(Blocked(Shell(true)), Is.True, "CG on: no provider is a recall");
		}

		[Test]
		public void TheBreakerTakesTheDefencePlanWithCgOnAndTheHotfix03PredicateWithCgOff()
		{
			var src = Stalemate();
			Assert.That(src, Does.Contain("bool StalemateBlocked(IBotStalemateUrgencyProvider provider) =>\n"
				+ "\t\t\tInfo.AggTeamEnabled ? provider == null || AggTeamDefence().RecallAll : StalemateHomeBlocks(provider);"));
			Assert.That(Regex.Matches(src, @"StalemateBlocked\(provider\)").Count, Is.EqualTo(3), "admission, wave.Continue, DeferRetreat");
			Assert.That(Regex.Matches(src, @"StalemateHomeBlocks\(").Count, Is.EqualTo(2), "its definition and the CG-off branch only");
			var admission = Section(src, "void StalemateTryAdmission(", "void StalemateRunAdmission(");
			Assert.That(admission, Does.Contain("else if (StalemateBlocked(provider))"));
			Assert.That(src, Does.Contain("&& StalemateBreakerPolicyCA.DeferRetreat(true, provider != null, status.Known, status.Destroyed,\n"
				+ "\t\t\t\t\tStalemateBlocked(provider), t, wave.C"));
		}

		// ---- agg-core holds a defending squad ----

		[Test]
		public void ADefendingSquadIsNeverReportedWithCgOffOrWithoutASquad()
		{
			Assert.That(Shell(false).AggressionSquadDefends(null), Is.False);
			Assert.That(Shell(true).AggressionSquadDefends(null), Is.False);
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Aggression.cs"),
				Does.Contain("internal bool AggressionSquadDefends(SquadCA squad) => squad?.Type == SquadCAType.Rush && AggTeamSquadDefends(squad);"));
		}

		[Test]
		public void TheIdleDecisionHoldsADefendingSquadBeforeItLooksAtTheFight()
		{
			var tick = Section(GroundStates(), "void AggressionTick(SquadCA owner, List<Actor> enemyUnits)", "void RegroupForward(");
			var uncommit = tick.IndexOf("SquadManagerBotModuleCA.AggressionUncommit(owner);", StringComparison.Ordinal);
			var hold = tick.IndexOf("if (manager.AggressionSquadDefends(owner))\n\t\t\t{\n\t\t\t\tregroupSinceTick = -1;\n\t\t\t\treturn;\n\t\t\t}", StringComparison.Ordinal);
			var friends = tick.IndexOf("manager.AggressionFriends(owner", StringComparison.Ordinal);
			Assert.That(uncommit, Is.GreaterThanOrEqualTo(0));
			Assert.That(hold, Is.GreaterThan(uncommit), "an idle defender is uncommitted, then held");
			Assert.That(friends, Is.GreaterThan(hold), "no ratio, veto or telemetry is computed for a held defender");
		}

		[Test]
		public void TheMarchNeverCancelsARetreatForADefendingSquad()
		{
			var march = Section(GroundStates(), "protected static bool AggressionHoldsMarch(SquadCA owner) =>", "protected Actor FindClosestEnemy(");
			Assert.That(march, Does.Contain("owner.SquadManager.AggressionOn && owner.IsTargetValid && !owner.SquadManager.AggressionSquadDefends(owner)\n"));
			var fight = Section(GroundStates(), "protected static bool AggressionHolds(SquadCA owner) =>", "protected static bool AggressionHoldsMarch(");
			Assert.That(fight, Does.Not.Contain("AggressionSquadDefends"), "the in-fight hold is not part of the seam (agg-team's ApplyDetachment sends a detached squad home)");
		}
	}
}
