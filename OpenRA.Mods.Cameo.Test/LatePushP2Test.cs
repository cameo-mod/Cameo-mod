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
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// LATEPUSH-01 P2 (rows lp_army_cap, lp_all_in_publish, lp_reinforce, lp_handover; design 01 D3 to D5, user decision audit A6). The pure halves are integer functions; the wiring keeps the old text of every call site
	/// behind its row, the learned cap and the all-in claim read no personality number, and every switch is off in code, in classic and in the six blocks.
	/// </summary>
	[TestFixture]
	public class LatePushP2Test
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

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		[Test]
		public void TheCapInPermilleIsTheFixedPercentBarWhenTheCapIsTenTimesThePercentAndTheLearnedShareIsClamped()
		{
			foreach (var bar in new[] { 0, 5, 12, 50, 300 })
				foreach (var waveMin in new[] { 0, 12, 24 })
					foreach (var eligible in new long[] { 0, 1, 7, 40, 155, 1000 })
						foreach (var percent in new[] { 0, 20, 40, 60, 80, 100 })
							Assert.That(LatePush.EffectiveBarPermille(bar, waveMin, percent * 10, eligible), Is.EqualTo(LatePush.EffectiveBar(bar, waveMin, percent, eligible)), "bar " + bar + " floor " + waveMin + " eligible " + eligible + " cap " + percent);

			// The learned prior 800: 80 of 100 eligible; never below the floor; with no eligible army the floor is the bar (as before).
			Assert.That(LatePush.EffectiveBarPermille(90, 12, 800, 100), Is.EqualTo(80));
			Assert.That(LatePush.EffectiveBarPermille(90, 12, 800, 10), Is.EqualTo(12), "the floor wins over a small cap");
			Assert.That(LatePush.EffectiveBarPermille(70, 12, 800, 100), Is.EqualTo(70), "a bar under the cap stays");
			Assert.That(LatePush.EffectiveBarPermille(90, 12, 800, 0), Is.EqualTo(12));
			Assert.That(LatePush.EffectiveBarPermille(90, 12, 800, 101), Is.EqualTo(81), "ceiling");
			Assert.That(LatePush.EffectiveBarPermille(90, 12, 5000, 100), Is.EqualTo(90), "a cap above 1.0 is clamped to the whole eligible army");

			var table = new LatePushTable(100, 64);
			Assert.That(LatePushTable.ArmyCapKey, Is.EqualTo("latepush.army_cap"));
			Assert.That(table.ArmyCapPermille, Is.EqualTo(800), "the prior");
			table.Override(LatePushTable.ArmyCapKey, 300);
			Assert.That(table.ArmyCapPermille, Is.EqualTo(500), "clamp 0.5");
			table.Override(LatePushTable.ArmyCapKey, 2000);
			Assert.That(table.ArmyCapPermille, Is.EqualTo(1000), "clamp 1.0");
			table.Override(LatePushTable.ArmyCapKey, 700);
			Assert.That(table.ArmyCapPermille, Is.EqualTo(700));
			table.ClearOverrides();
			Assert.That(table.ArmyCapPermille, Is.EqualTo(800), "clearing restores the prior");
			Assert.That(new LatePushTable(100, 64, 650).ArmyCapPermille, Is.EqualTo(650));
			Assert.That(new LatePushTable(100, 64, 100).ArmyCapPermille, Is.EqualTo(500), "a yaml prior outside the clamp is clamped too");
			Assert.That(LatePush.ArmyCapMin, Is.EqualTo(500));
			Assert.That(LatePush.ArmyCapMax, Is.EqualTo(1000));
			Assert.That(LatePush.ArmyCapPrior, Is.EqualTo(800));
		}

		[Test]
		public void TheAllInClaimIsNeverBelowTheBlobAndTheReinforcementIsDueOnceBelowTheShareAndNotWhenBehind()
		{
			Assert.That(LatePush.AllInClaim(10, 5000, 25, 9000), Is.EqualTo((25, 9000L)));
			Assert.That(LatePush.AllInClaim(30, 10000, 25, 9000), Is.EqualTo((30, 10000L)), "never below the blob");
			Assert.That(LatePush.AllInClaim(10, 5000, 25, 4000), Is.EqualTo((25, 5000L)), "units and value separately");
			Assert.That(LatePush.AllInClaim(0, 0, 0, 0), Is.EqualTo((0, 0L)));

			// Due: strictly below the share of the committed units, committed known, not behind (an unseen enemy reads 100).
			Assert.That(LatePush.ReinforceDue(11, 20, 60, 100, 100), Is.True, "55 percent of the committed");
			Assert.That(LatePush.ReinforceDue(12, 20, 60, 100, 100), Is.False, "exactly 60 percent is not below");
			Assert.That(LatePush.ReinforceDue(0, 0, 60, 100, 100), Is.False, "nothing committed");
			Assert.That(LatePush.ReinforceDue(5, 20, 60, 99, 100), Is.False, "behind: an observed ratio under the minimum");
			Assert.That(LatePush.ReinforceDue(5, 20, 60, 160, 100), Is.True);
			Assert.That(LatePush.ReinforceDue(5, 20, 0, 160, 100), Is.False, "a share of 0 never fires");

			Assert.That(LatePush.ReinforceSend(20, 11, 30), Is.EqualTo(9), "what the squads lost");
			Assert.That(LatePush.ReinforceSend(20, 11, 5), Is.EqualTo(5), "never more than the pool");
			Assert.That(LatePush.ReinforceSend(20, 25, 30), Is.EqualTo(0), "never negative");
		}

		[Test]
		public void TheReinforcementKeyAndTheHandoverRecordLiveOnTheOrderPerSeat()
		{
			var order = new TeamLeadOrder(7, "leaderA", WPos.Zero, WPos.Zero, 0, 100);
			var a = order.ReinforceKeyOf("seatA");
			Assert.That(order.ReinforceKeyOf("seatA"), Is.SameAs(a), "one key per seat and order");
			Assert.That(order.ReinforceKeyOf("seatB"), Is.Not.SameAs(a));
			Assert.That(a.Sent, Is.False);
			Assert.That(a.Committed, Is.EqualTo(0));
			Assert.That(a.HoldUntil, Is.EqualTo(-1));
			a.Sent = true;
			Assert.That(order.ReinforceKeyOf("seatA").Sent, Is.True, "a spent key stays spent for every module of the seat");
			Assert.That(new TeamLeadOrder(8, "leaderA", WPos.Zero, WPos.Zero, 0, 200).ReinforceKeyOf("seatA").Sent, Is.False, "the next order is a new assignment with its own key");

			Assert.That(order.HasHandover("seatA"), Is.False);
			order.RecordHandover("seatA", new List<uint> { 11, 12, 13 });
			Assert.That(order.HasHandover("seatA"), Is.True);
			Assert.That(order.HasHandover("seatB"), Is.False);
			Assert.That(order.TryGetHandover("seatA", out var ids), Is.True);
			Assert.That(ids, Is.EqualTo(new uint[] { 11, 12, 13 }));
			order.ClearHandover("seatA");
			Assert.That(order.HasHandover("seatA"), Is.False);
			Assert.That(order.TryGetHandover("seatA", out _), Is.False);
			Assert.That(order.ReinforceKeyOf("seatA").Sent, Is.True, "the rejoin does not rearm the key");
		}

		[Test]
		public void EverySwitchIsOffInCodeInClassicAndInTheSixBlocksWithItsOwnAblationIdAndThePriorsInPlace()
		{
			Assert.That(AblationIds.LpArmyCap, Is.EqualTo("lp_army_cap"));
			Assert.That(AblationIds.LpAllInPublish, Is.EqualTo("lp_all_in_publish"));
			Assert.That(AblationIds.LpReinforce, Is.EqualTo("lp_reinforce"));
			Assert.That(AblationIds.LpHandover, Is.EqualTo("lp_handover"));
			foreach (var id in new[] { AblationIds.LpArmyCap, AblationIds.LpAllInPublish, AblationIds.LpReinforce, AblationIds.LpHandover })
				Assert.That(Regex.IsMatch(id, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list: " + id);

			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.LatePushP2.cs");
			foreach (var off in new[] { "LatePushArmyCapEnabled", "LatePushAllInPublishEnabled", "LatePushReinforceEnabled", "LatePushHandoverEnabled" })
				Assert.That(info, Does.Contain("public readonly bool " + off + " = false;"), "off in code: " + off);
			Assert.That(info, Does.Contain("public readonly int LatePushArmyCapPermille = 800;"));
			Assert.That(info, Does.Contain("public readonly int LatePushReinforceBelowPercent = 60;"));
			Assert.That(info, Does.Contain("public readonly int LatePushReinforceMinRatioPercent = 100;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(yaml[classic..], Does.Not.Contain("LatePush"), "classic untouched");
			foreach (var key in new[]
			{
				"LatePushArmyCapEnabled: false", "LatePushArmyCapPermille: 800", "LatePushAllInPublishEnabled: false", "LatePushReinforceEnabled: false",
				"LatePushReinforceBelowPercent: 60", "LatePushReinforceMinRatioPercent: 100", "LatePushHandoverEnabled: false"
			})
				Assert.That(Count(yaml[..classic], "\n\t\t" + key + "\n"), Is.EqualTo(6), key);

			Assert.That(Regex.IsMatch(yaml, "LatePush\\w*Enabled: true"), Is.False, "every row ships OFF until its A/B passes");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs"), Does.Contain("internal bool LatePushArmyCapOn => LatePushAgeMinOn && Info.LatePushArmyCapEnabled && !ablateLpArmyCap.Off(Player, AblationIds.LpArmyCap);"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs"), Does.Contain("internal bool LatePushAllInPublishOn => ArmyLaunchArbiterOn && TeamLeadAllInOn && Info.LatePushAllInPublishEnabled && !ablateLpAllInPublish.Off(Player, AblationIds.LpAllInPublish);"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs"), Does.Contain("internal bool LatePushReinforceOn => ArmyLaunchArbiterOn && Info.LatePushReinforceEnabled && !ablateLpReinforce.Off(Player, AblationIds.LpReinforce);"));
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs"), Does.Contain("internal bool LatePushHandoverOn => ArmyLaunchArbiterOn && Info.LatePushHandoverEnabled && !ablateLpHandover.Off(Player, AblationIds.LpHandover);"));
		}

		[Test]
		public void TheLearnedCapOnlyShapesTheLeadersOrdinaryBarAnAllInAndTheMemberFloorsNeverApplyIt()
		{
			var bars = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushBars.cs");
			var allInExit = bars.IndexOf("if (allIn || team == null || !LatePushAgeMinOn || !WaveMinCA.Limits(bars.Units, bars.Value))\n\t\t\t\treturn bars;", StringComparison.Ordinal);
			var cap = bars.IndexOf("var cap = LatePushArmyCapNow();", StringComparison.Ordinal);
			Assert.That(allInExit, Is.GreaterThan(0));
			Assert.That(cap, Is.GreaterThan(allInExit), "an open all-in returns the WAVE-MIN bars before the cap is read");
			Assert.That(Count(bars, "LatePushArmyCapNow("), Is.EqualTo(1));

			// Nowhere else: no member-side floor (squad launch, keeps-going, pincer, reinforce, stalemate), no all-in publish, no team-leader order reads the cap.
			foreach (var name in new[] { "SquadManagerBotModuleCA.TeamLead.cs", "SquadManagerBotModuleCA.V07.cs", "SquadManagerBotModuleCA.Pipeline02.cs", "SquadManagerBotModuleCA.PincerPush.cs", "SquadManagerBotModuleCA.Stalemate01.cs",
				"SquadManagerBotModuleCA.WaveMin.cs", "SquadManagerBotModuleCA.cs", "SquadManagerBotModuleCA.AllInAhead.cs", "SquadManagerBotModuleCA.GuardLend.cs", "SquadManagerBotModuleCA.GuardFloor.cs" })
			{
				var text = RepoFile(Bots + name);
				Assert.That(text, Does.Not.Contain("LatePushArmyCapNow("), name);
				Assert.That(text, Does.Not.Contain("EffectiveBarPermille("), name);
			}

			var p2 = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs");
			Assert.That(p2, Does.Contain("int LatePushArmyCapNow() => LatePushArmyCapOn ? LatePushParameters.ArmyCapPermille : Info.LatePushCapPercent * 10;"), "row off = the fixed P1 percent x 10");
			Assert.That(p2, Does.Contain("TODO(LA02, knob latepush.army_cap)"), "the marker for the registry and the IBotLearnedKnobs read");
			Assert.That(RepoFile(Bots + "Squads/LatePush.cs"), Does.Contain("public const string ArmyCapKey = \"latepush.army_cap\";"));

			// Learning is the DEV trainer's job: neither the bar nor the P2 wiring writes the table.
			foreach (var text in new[] { bars, p2 })
			{
				Assert.That(text, Does.Not.Contain(".Override("));
				Assert.That(text, Does.Not.Contain("ClearOverride"));
			}
		}

		[Test]
		public void TheAllInPublishAndClaimReadNoPersonalityScaledNumberAndTheOldPathsKeepTheirTextWithTheRowOff()
		{
			var p2 = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs");
			var claim = Between(p2, "(int Units, long Value) LatePushAllInBlob(TeamLeadTeam team, int now, (int Units, long Value) blob)", "// ---- one group out of the pool");
			Assert.That(claim, Does.Contain("if (!LatePushAllInPublishOn)\n\t\t\t\treturn blob;"), "row off = the blob");
			Assert.That(claim, Does.Contain("LatePush.AllInClaim(blob.Units, blob.Value, eligible.Units, eligible.Value)"));
			Assert.That(claim, Does.Contain("int LatePushAllInClaim(int spareUnits) => Math.Min(spareUnits, armyGuardLend.Count);"));

			// D4a: the all-in claim and publish read none of the personality-scaled numbers.
			var pure = Between(RepoFile(Bots + "Squads/LatePush.cs"), "public static (int Units, long Value) AllInClaim(", "public static bool ReinforceDue(");
			foreach (var banned in new[] { "MaxIdleUnits", "SquadValue", "AggTeamReserve", "MinimumAttackForceDelay", "Personality", "HarassCap", "LatePushCapPercent", "LatePushAgeTicks", "AgePermille" })
			{
				Assert.That(claim, Does.Not.Contain(banned), "claim: " + banned);
				Assert.That(pure, Does.Not.Contain(banned), "pure claim: " + banned);
			}

			var lead = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var publish = Between(lead, "void TeamLeadPublish(TeamLeadTeam team, int now)", "void TeamLeadPromoteRally(TeamLeadOrder order, int now)");
			Assert.That(publish, Does.Contain("var readyBlob = allIn != null ? LatePushAllInBlob(team, now, blob) : blob;"));
			Assert.That(publish, Does.Contain("var (barUnits, barValue) = WaveMinActive ? LatePushBars(team, now, allIn != null) : (0, 0);"), "the all-in bars stay WAVE-MIN (no age factor, no cap)");

			var barsText = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushBars.cs");
			var lend = Between(barsText, "int LatePushLend(SquadCA guard, int ordinaryLend)", "int LatePushGuardSpare(");
			var allInClaim = lend.IndexOf("if (LatePushAllInPublishOn && TeamLeadOpenOrder() is { AllIn: true })\n\t\t\t\treturn LatePushAllInClaim(spare);", StringComparison.Ordinal);
			var oldReturn = lend.IndexOf("if (!LatePushCommitOn || TeamLeadOpenOrder() is { AllIn: true })\n\t\t\t\treturn ordinaryLend;", StringComparison.Ordinal);
			Assert.That(allInClaim, Is.GreaterThan(0));
			Assert.That(oldReturn, Is.GreaterThan(allInClaim), "the P1 all-in return stays, after the claim");
			Assert.That(barsText, Does.Contain("bool LatePushOn => LatePushAgeMinOn || LatePushCommitOn || LatePushAllInPublishOn;"), "the eligible figure is published for the all-in publish too");

			// The guard floor stays the one lend contract: the claim goes through GuardFloorLend like every other lend.
			var guardLend = RepoFile(Bots + "SquadManagerBotModuleCA.GuardLend.cs");
			Assert.That(guardLend.IndexOf("lend = LatePushLend(guard, lend);", StringComparison.Ordinal), Is.LessThan(guardLend.IndexOf("lend = GuardFloorLend(guard, lend);", StringComparison.Ordinal)));
		}

		[Test]
		public void TheReinforcementIsOneGroupPerSeatAndOrderFromTheOrdersKeyAndTheHandoverDoesNotRearmIt()
		{
			var p2 = RepoFile(Bots + "SquadManagerBotModuleCA.LatePushP2.cs");
			var reinforce = Between(p2, "bool LatePushReinforce(IBot bot, TeamLeadOrder order, int now)", "// ---- lp_handover (design 01 D4b) ----");
			Assert.That(reinforce, Does.Contain("if (!LatePushReinforceOn || teamLeadKey == null)\n\t\t\t\treturn false;"), "row off = the old WAVE-MIN group path");
			Assert.That(reinforce, Does.Contain("var key = order.ReinforceKeyOf(teamLeadKey);"), "the key lives on the order, so a module change of the seat does not rearm it");
			Assert.That(reinforce, Does.Contain("if (key.Sent || key.Committed == 0 || now < key.HoldUntil || order.HasHandover(teamLeadKey))\n\t\t\t\treturn true;"));
			Assert.That(reinforce, Does.Contain("LatePush.ReinforceDue(alive, key.Committed, Info.LatePushReinforceBelowPercent, ratio, Info.LatePushReinforceMinRatioPercent)"));
			Assert.That(reinforce, Does.Contain("if (send < Info.TeamLeadJoinMinUnits)\n\t\t\t\treturn true;"), "one real group or nothing, the key stays unspent");
			Assert.That(reinforce, Does.Contain("var sent = LatePushSendGroup(bot, send, null);\n\t\t\tif (sent > 0)\n\t\t\t{\n\t\t\t\tkey.Sent = true;"), "spent only when units really left the pool");
			Assert.That(Count(reinforce, "key.Sent = true;"), Is.EqualTo(1));
			Assert.That(reinforce, Does.Contain("if (!key.Sent && alive > key.Committed)\n\t\t\t\tkey.Committed = alive;"), "committed is the peak before the send");
			Assert.That(reinforce, Does.Contain("s.TeamPushSeq == order.Seq"), "only the squads pushed with this order");

			var pipeline = RepoFile(Bots + "SquadManagerBotModuleCA.Pipeline02.cs");
			var hook = pipeline.IndexOf("if (LatePushHandover(bot, order, now) || LatePushReinforce(bot, order, now))\n\t\t\t\treturn;", StringComparison.Ordinal);
			Assert.That(hook, Is.GreaterThan(pipeline.IndexOf("void ArmyReinforce(IBot bot, TeamLeadOrder order, int now)", StringComparison.Ordinal)));
			Assert.That(pipeline.IndexOf("if (!TeamLeadAllInRejoinGroup())\n\t\t\t\treturn;\n\n\t\t\tvar before = unitsHangingAroundTheBase.Count;", StringComparison.Ordinal), Is.GreaterThan(hook), "the old WAVE-MIN group path is untouched after the hook");

			var handover = p2[p2.IndexOf("void LatePushHandoverRecord()", StringComparison.Ordinal)..];
			Assert.That(handover, Does.Contain("TeamLeadOpenOrder() is not { Persistent: true, Pushed: true } order"), "only a pushed persistent order is handed over");
			Assert.That(handover, Does.Contain("order.RecordHandover(teamLeadKey, ids);"));
			Assert.That(handover, Does.Contain("order.ClearHandover(teamLeadKey);\n\t\t\torder.ReinforceKeyOf(teamLeadKey).HoldUntil = now + 2 * Info.TeamLeadCheckTicks;"));
			Assert.That(handover, Does.Not.Contain(".Sent"), "the rejoin neither spends nor rearms the reinforcement");
			Assert.That(handover, Does.Contain("LatePushSendGroup(bot, available, only)"), "one group, whatever its size");

			var main = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			var record = main.IndexOf("LatePushHandoverRecord();", StringComparison.Ordinal);
			Assert.That(record, Is.GreaterThan(main.IndexOf("protected override void TraitDisabled(Actor self)", StringComparison.Ordinal)));
			Assert.That(main.IndexOf("foreach (var squad in Squads)\n\t\t\t\tDismissSquad(squad);", record, StringComparison.Ordinal), Is.GreaterThan(record), "the ids are recorded before the squads are dismissed");

			// The group launch: the pool is set aside around the launch and put back; nothing is scanned.
			var group = Between(p2, "int LatePushSendGroup(IBot bot, int send, HashSet<uint> only)", "// ---- lp_reinforce (design 01 D5) ----");
			Assert.That(group, Does.Contain("unitsHangingAroundTheBase.AddRange(aside);"));
			Assert.That(group, Does.Contain("var sent = keep.Count - unitsHangingAroundTheBase.Count;"));
			foreach (var banned in new[] { "FindClosestEnemy", "FindActorsInCircle", "World.Actors", "ActorsWithTrait", "FrozenActorLayer", "Random", "IBotCurrentPersonality", "Trait<", "TraitsImplementing" })
				Assert.That(p2, Does.Not.Contain(banned), "no scan, no RNG, no enemy state: " + banned);
		}
	}
}
