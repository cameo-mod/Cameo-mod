#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>WAVE-MIN-01 (launch gate) and ARMY-MIX-01 (mixed waves), USER P0 2026-11-23: the pure halves and the wiring pins.</summary>
	[TestFixture]
	public class WaveMinMixTest
	{
		// --- WAVE-MIN-01 ---------------------------------------------------------------------------------------------------------------

		[Test]
		public void ThePersonalityMayRaiseTheTierBarNeverLowerIt()
		{
			Assert.That(WaveMinCA.Effective(12, 0), Is.EqualTo(12), "no floor");
			Assert.That(WaveMinCA.Effective(12, 8), Is.EqualTo(12), "a lower floor changes nothing");
			Assert.That(WaveMinCA.Effective(12, 15), Is.EqualTo(15), "a higher floor raises it");
			Assert.That(WaveMinCA.Effective(-1, 0), Is.EqualTo(0), "a tier line that is not written means no limit");
			Assert.That(WaveMinCA.Effective(0, 5), Is.EqualTo(5), "noob with a personality floor");
			Assert.That(WaveMinCA.Effective(12, -3), Is.EqualTo(12), "a negative floor is ignored");
		}

		[Test]
		public void TheGateNeedsBothTheUnitCountAndTheValue()
		{
			Assert.That(WaveMinCA.Passes(12, 6000, 12, 6000), Is.True, "both bars met exactly");
			Assert.That(WaveMinCA.Passes(11, 9000, 12, 6000), Is.False, "enough value, too few units");
			Assert.That(WaveMinCA.Passes(20, 5999, 12, 6000), Is.False, "enough units, too little value");
			Assert.That(WaveMinCA.Passes(0, 0, 0, 0), Is.True, "noob: no limit");
			Assert.That(WaveMinCA.Limits(0, 0), Is.False);
			Assert.That(WaveMinCA.Limits(2, 0), Is.True);
			Assert.That(WaveMinCA.Limits(0, 1200), Is.True);
		}

		[Test]
		public void TheLaunchRecordListsRolesInOrderAndCallsUnclassifiedUnitsNone()
		{
			Assert.That(WaveMinCA.RoleSummary(["frontline", null, "anti_air", "frontline", ""]), Is.EqualTo("anti_air:1,frontline:2,none:2"));
			Assert.That(WaveMinCA.RoleSummary([]), Is.Empty);
		}

		[Test]
		public void EveryTierWritesTheUserLine()
		{
			var expected = new (string Tier, int Units, int Value)[]
			{
				("noob", 0, 0), ("easiest", 2, 1200), ("veryeasy", 5, 2400), ("easy", 7, 3600), ("medium", 10, 4800), ("hard", 12, 6000),
				("veryhard", 14, 7200), ("brutal", 17, 8400), ("challenger", 19, 9600), ("unbeatable", 22, 10800), ("god", 24, 12000)
			};

			var segments = RepoFile("mods/cameo/ai/ai.yaml").Split("\n\tBotLimits@", StringSplitOptions.None).Skip(1).ToList();
			Assert.That(segments.Count, Is.EqualTo(11), "eleven BotLimits tiers");
			foreach (var (tier, units, value) in expected)
			{
				var segment = segments.Single(s => s.StartsWith(tier + ":", StringComparison.Ordinal));
				var next = Regex.Match(segment, @"\n\t(?!\t)");
				var block = next.Success ? segment[..next.Index] : segment;
				Assert.That(Regex.Match(block, @"\n\t\tWaveMinUnits: (\d+)").Groups[1].Value, Is.EqualTo(units.ToString()), tier + " units");
				Assert.That(Regex.Match(block, @"\n\t\tWaveMinValue: (\d+)").Groups[1].Value, Is.EqualTo(value.ToString()), tier + " value");
			}
		}

		[Test]
		public void TheLineIsTwoPointFourUnitsAndTwelveHundredCreditsPerRank()
		{
			var units = new[] { 0, 2, 5, 7, 10, 12, 14, 17, 19, 22, 24 };
			for (var rank = 0; rank < units.Length; rank++)
				Assert.That((24 * rank + 5) / 10, Is.EqualTo(units[rank]), "rank " + rank + " is 2.4 x rank rounded");
		}

		// --- ARMY-MIX-01 ---------------------------------------------------------------------------------------------------------------

		[Test]
		public void OnlyWaveRolesCountTowardTheMix()
		{
			var roles = WaveMixCA.DistinctRoles(["frontline", "frontline", "anti_air", null, "scout", "support", "transport", "artillery"]);
			Assert.That(roles, Is.EquivalentTo(new[] { "frontline", "anti_air", "artillery" }), "scouts, support and transport are not wave roles");
			Assert.That(WaveMixCA.DistinctRoles([]), Is.Empty);
		}

		[Test]
		public void AWaveNeedsThreeRolesOrEveryRoleThereIs()
		{
			Assert.That(WaveMixCA.Needed(3, 5), Is.EqualTo(3));
			Assert.That(WaveMixCA.Needed(3, 2), Is.EqualTo(2), "only two roles available: those two are the maximum");
			Assert.That(WaveMixCA.Needed(3, 0), Is.EqualTo(0));
			Assert.That(WaveMixCA.Needed(-1, 4), Is.EqualTo(0));
		}

		[Test]
		public void TheWaitForAMissingRoleIsBoundedAndNeverStartsWhenSatisfied()
		{
			var idle = WaveMixClock.Idle;
			Assert.That(WaveMixCA.Step(100, idle, 1500, true), Is.EqualTo((false, idle)), "satisfied: go, clock reset");
			Assert.That(WaveMixCA.Step(100, idle, 1500, false), Is.EqualTo((true, new WaveMixClock(100, false))), "first miss starts the clock");
			Assert.That(WaveMixCA.Step(1599, new WaveMixClock(100, false), 1500, false), Is.EqualTo((true, new WaveMixClock(100, false))), "still waiting");
			Assert.That(WaveMixCA.Step(1600, new WaveMixClock(100, false), 1500, false), Is.EqualTo((false, new WaveMixClock(-1, true))), "timeout: released");
			Assert.That(WaveMixCA.Step(100, idle, 0, false), Is.EqualTo((false, idle)), "0 = never wait");
			Assert.That(WaveMixCA.Step(900, new WaveMixClock(100, false), 1500, true), Is.EqualTo((false, idle)), "the role arrived while waiting");
		}

		[Test]
		public void AReleasedWaitStaysReleasedWhenALaterGateStopsTheLaunchAndEndsOnlyWithTheRoleOrTheLaunch()
		{
			// Code Monkey's FIX item: after the timeout a later launch gate (veto, staging) may return before the wave leaves. The next
			// attempts must not re-arm the clock and hold the wave for another full wait.
			var clock = WaveMixClock.Idle;
			(bool Hold, WaveMixClock Next) Try(int now, bool satisfied)
			{
				var r = WaveMixCA.Step(now, clock, 1500, satisfied);
				clock = r.Next;
				return r;
			}

			Assert.That(Try(0, false).Hold, Is.True);
			Assert.That(Try(1500, false).Hold, Is.False, "timeout releases the wave");
			for (var now = 1501; now < 6000; now += 75)
				Assert.That(Try(now, false).Hold, Is.False, "the later gate returned; the wave is still released at tick " + now);

			Assert.That(Try(6000, true).Hold, Is.False);
			Assert.That(clock, Is.EqualTo(WaveMixClock.Idle), "the role arrived: the next wave starts fresh");

			Assert.That(Try(7000, false).Hold, Is.True, "a new wave waits again");
			Assert.That(Try(8500, false).Hold, Is.False);
			clock = WaveMixClock.Idle; // WaveMixLaunched(): the wave left
			Assert.That(Try(9000, false).Hold, Is.True, "after the launch the next wave waits again");

			var manager = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var register = manager.IndexOf("var attackForce = RegisterNewSquad(bot, SquadCAType.Rush);", StringComparison.Ordinal);
			Assert.That(manager.IndexOf("WaveMixLaunched();", register, StringComparison.Ordinal) - register, Is.InRange(1, 160),
				"the released latch is cleared right where the wave squad is created");
		}

		[Test]
		public void MissingRolesComeInTheFixedOrderUntilALearnerWeighsThem()
		{
			var available = new[] { "skirmisher", "anti_air", "frontline", "anti_armour", "artillery" };
			var pool = new HashSet<string> { "frontline" };

			Assert.That(WaveMixCA.MissingRoles(available, pool, _ => 1.0),
				Is.EqualTo(new[] { "anti_armour", "anti_air", "artillery", "skirmisher" }));

			Assert.That(WaveMixCA.MissingRoles(available, pool, r => r == "artillery" ? 2.0 : 1.0).First(), Is.EqualTo("artillery"),
				"a learned higher value moves a role to the front");
			Assert.That(WaveMixCA.MissingRoles(["frontline"], pool, _ => 1.0), Is.Empty, "nothing is missing");
		}

		[Test]
		public void OnePickCoversARoleSoParallelQueuesBuildTheNextOne()
		{
			var covered = new Dictionary<string, int> { ["anti_armour"] = 500 };
			int Until(string role) => covered.GetValueOrDefault(role);

			Assert.That(WaveMixCA.NextMissing(["anti_armour", "anti_air"], Until, 400), Is.EqualTo("anti_air"), "anti_armour is covered until 500");
			Assert.That(WaveMixCA.NextMissing(["anti_armour", "anti_air"], Until, 500), Is.EqualTo("anti_armour"), "the cover has ended");
			Assert.That(WaveMixCA.NextMissing(["anti_armour"], Until, 400), Is.Null, "every missing role is covered");
		}

		[Test]
		public void TheMatchupIsTheFourthRootOfTheFourValuesAndAnUnseenStyleChangesNothing()
		{
			// G x F x E x S = 1 x 16 x 1 x 1 -> 16^(1/4) = 2
			Assert.That(WaveMixCA.Matchup(new MixRoleValues(1, 16, 1, 1)), Is.EqualTo(2.0).Within(1e-9));

			// A new style value starts at the three-value result: the matchup equals (G x F x E)^(1/3).
			var three = Math.Pow(2.0 * 4.0 * 8.0, 1.0 / 3.0);
			Assert.That(WaveMixCA.Matchup(new MixRoleValues(2, 4, 8, 0)), Is.EqualTo(three).Within(1e-9));

			// A seen style pulls the mean toward it.
			Assert.That(WaveMixCA.Matchup(new MixRoleValues(2, 4, 8, 16)), Is.EqualTo(Math.Pow(2.0 * 4.0 * 8.0 * 16.0, 0.25)).Within(1e-9));

			Assert.That(WaveMixCA.Matchup(new MixRoleValues(0, 0, 0, 0)), Is.EqualTo(1.0), "nothing known = neutral");
			Assert.That(WaveMixCA.Matchup(new MixRoleValues(4, 0, 0, 0)), Is.EqualTo(4.0).Within(1e-9), "a missing value is dropped, not zeroed");
		}

		// --- wiring pins ---------------------------------------------------------------------------------------------------------------

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		[Test]
		public void BothSwitchesDefaultOffAndAreOnInSixPersonalitiesNotClassic()
		{
			var info = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(info, Does.Contain("public readonly bool WaveMinEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly bool WaveMixEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int WaveMinUnits = 0;"), "a personality floor defaults to no raise");
			Assert.That(info, Does.Contain("public readonly int WaveMinValue = 0;"));

			var limits = RepoFile("OpenRA.Mods.CA/Traits/BotModules/BotLimits.cs");
			Assert.That(limits, Does.Contain("public readonly int WaveMinUnits = -1;"), "not written = off, never a C# default limit");
			Assert.That(limits, Does.Contain("public readonly int WaveMinValue = -1;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tWaveMinEnabled: true").Count, Is.EqualTo(6));
			Assert.That(Regex.Matches(yaml[..classic], @"\n\t\tWaveMixEnabled: true").Count, Is.EqualTo(6));
			Assert.That(yaml[classic..], Does.Not.Contain("WaveMinEnabled"), "@classic keeps the defaults");
			Assert.That(yaml[classic..], Does.Not.Contain("WaveMixEnabled"));
		}

		[Test]
		public void TheGatesSitInFrontOfTheLaunchAndOnlyThere()
		{
			var manager = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var gate = manager.IndexOf("if (WaveBelowMinimum())", StringComparison.Ordinal);
			var mix = manager.IndexOf("if (WaveMixHolds())", StringComparison.Ordinal);
			var staged = manager.IndexOf("if (!StagedCompositionReady())", StringComparison.Ordinal);
			var register = manager.IndexOf("var attackForce = RegisterNewSquad(bot, SquadCAType.Rush);", Math.Max(staged, 0), StringComparison.Ordinal);
			Assert.That(gate, Is.GreaterThan(0));
			Assert.That(mix, Is.GreaterThan(gate));
			Assert.That(staged, Is.GreaterThan(mix));
			Assert.That(register, Is.GreaterThan(staged), "the gates run before the wave squad exists");
			Assert.That(Regex.Matches(manager, @"WaveBelowMinimum\(\)").Count, Is.EqualTo(1), "no other path (defence, retaliation, drops) asks the gate");

			var wave = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.WaveMin.cs");
			Assert.That(wave, Does.Contain("Info.WaveMinEnabled && !ablateWaveMin.Off(Player, AblationIds.WaveMin)"));
			Assert.That(RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.WaveMix.cs"),
				Does.Contain("Info.WaveMixEnabled && !ablateArmyMix.Off(Player, AblationIds.ArmyMix)"));
			Assert.That(AblationIds.WaveMin, Is.EqualTo("wm_wave_min"));
			Assert.That(AblationIds.ArmyMix, Is.EqualTo("am_army_mix"));
		}

		[Test]
		public void HelpingOwnAndAlliedUnitsRunsBeforeAnyLaunchAndMayDraftStagingSquads()
		{
			var manager = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var defend = manager.IndexOf("DefendAllyTick(bot);", StringComparison.Ordinal);
			var urgent = manager.IndexOf("var urgentDispatched = ", StringComparison.Ordinal);
			var launch = manager.IndexOf("CreateAttackForce(bot);", urgent, StringComparison.Ordinal);
			Assert.That(defend, Is.GreaterThan(0));
			Assert.That(urgent, Is.GreaterThan(defend), "DEFEND-ALLY runs before the urgent attack and the ordinary launch");
			Assert.That(launch, Is.GreaterThan(urgent));

			var ally = RepoFile("OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("is GroundUnitsIdleStateCA or GroundUnitsStageStateCA"), "idle, staging and gathering squads answer");
			Assert.That(ally, Does.Not.Contain("WaveBelow"), "a defence response ignores the wave minimum");
		}

		[Test]
		public void TheUnitBuilderPicksTheMissingRoleAfterCountersAndNeverAddsABuild()
		{
			var builder = RepoFile("OpenRA.Mods.CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(Regex.Matches(builder, @"var waveMix = ChooseWaveMixRole\(").Count, Is.EqualTo(2), "both pick paths");
			Assert.That(builder, Does.Contain("if (manager == null || !manager.WaveMixActive)"));

			foreach (var call in new[] { "var counter = ChooseCounter(", "var waveMix = ChooseWaveMixRole(", "var deficit = ChooseRoleDeficit(" })
				Assert.That(builder, Does.Contain(call));

			var first = builder.IndexOf("var waveMix = ChooseWaveMixRole(", StringComparison.Ordinal);
			Assert.That(builder.LastIndexOf("var counter = ChooseCounter(", first, StringComparison.Ordinal), Is.GreaterThan(0), "counters keep first refusal");
			Assert.That(builder.IndexOf("var deficit = ChooseRoleDeficit(", first, StringComparison.Ordinal), Is.GreaterThan(first));
		}
	}
}
