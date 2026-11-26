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
	/// CONSOLIDATE-03 P7 (b), row sq_defend_one (design 01 contradiction 2; Opus review change 4). Two ally-defend copies answered the same attack with different draft limits and pick rules (DEFEND-ALLY: pool inside the
	/// radius, the draft limit, one cooldown; TEAM-DEFEND: the whole pool or the Q4b share, no radius). With the row on the alarm drafts like DEFEND-ALLY and the two share one cooldown.
	/// </summary>
	[TestFixture]
	public class DefendOneTest
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

		[Test]
		public void ThePickKeepsTheRadiusNearestFirstAndTheLowerIdOnATie()
		{
			var c = new[]
			{
				new DefendOne.Candidate(0, 90, 400), // inside, far
				new DefendOne.Candidate(1, 50, 100), // inside, nearest, id 50
				new DefendOne.Candidate(2, 40, 100), // inside, nearest, id 40: wins the tie
				new DefendOne.Candidate(3, 10, 2500), // outside (radius^2 = 1000)
				new DefendOne.Candidate(4, 20, 1000), // exactly on the radius: inside (as DefendAllyCA.InRadius)
			};

			Assert.That(DefendOne.InRadiusNearestFirst(c, 1000), Is.EqualTo(new[] { 2, 1, 0, 4 }));
			Assert.That(DefendOne.InRadiusNearestFirst(c, 0), Is.Empty, "a zero radius takes nothing");
			Assert.That(DefendOne.InRadiusNearestFirst(Array.Empty<DefendOne.Candidate>(), 1000), Is.Empty);
		}

		[Test]
		public void ThePoolOutsideTheRadiusStaysHomeAndTheLimitIsTheDefendAllyOne()
		{
			// 6 pool units: 4 inside the radius, 2 far. The alarm may take only the inside ones, the DEFEND-ALLY limit says how many of them.
			var c = new DefendOne.Candidate[6];
			for (var i = 0; i < 6; i++)
				c[i] = new DefendOne.Candidate(i, (uint)(100 + i), i < 4 ? 100 * (i + 1) : 100000);

			var inside = DefendOne.InRadiusNearestFirst(c, 1000);
			Assert.That(inside, Is.EqualTo(new[] { 0, 1, 2, 3 }));
			Assert.That(DefendOne.Detail(DefendOne.SourceAlarm, 6, inside.Count, 2, false),
				Is.EqualTo("src=alarm;radius=DefendAlly;limit=DefendAlly;order=nearest_victim;squads=TeamDefend;guard=TeamDefend;pool=6;in_radius=4;far_kept=2;drafted=2;cooldown_skip=0"));
		}

		[Test]
		public void OneCooldownCoversTheLocalScanAndTheAlarm()
		{
			Assert.That(DefendOne.MayDraft(100, 100), Is.True, "the cooldown has run out");
			Assert.That(DefendOne.MayDraft(99, 100), Is.False, "the local scan answered: the alarm does not draft the same attack again");
			Assert.That(DefendOne.MayDraft(0, 0), Is.True);
			Assert.That(DefendOne.Detail(DefendOne.SourceAlarm, 3, 3, 0, true), Does.EndWith("drafted=0;cooldown_skip=1"));
		}

		[Test]
		public void TheRowHasItsOwnAblationIdAndInfoSwitchOffInCodeOnInTheSixNonClassicBlocks()
		{
			Assert.That(AblationIds.SqDefendOne, Is.EqualTo("sq_defend_one"));
			Assert.That(RepoFile(Bots + "BotModuleLogic/AblationIds.SqDefendOne.cs"), Does.Contain("public const string SqDefendOne = \"sq_defend_one\";"));
			Assert.That(Regex.IsMatch(AblationIds.SqDefendOne, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");
			Assert.That(RepoFile(Bots + "SquadManagerBotModuleCA.cs"), Does.Contain("public readonly bool DefendOneEnabled = false;"), "off in code: only the yaml arms it");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			Assert.That(Count(yaml[..classic], "\n\t\tDefendOneEnabled: true\n"), Is.EqualTo(6), "the six blocks that carry both ally-defend switches");
			Assert.That(yaml[classic..], Does.Not.Contain("DefendOneEnabled"), "classic untouched");
			Assert.That(Count(yaml, "DefendOneEnabled:"), Is.EqualTo(6));
		}

		[Test]
		public void TheRowMergesBothCopiesSoItNeedsBothSwitchesAndTheLatch()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendOne.cs");
			Assert.That(part, Does.Contain("AblationLatch ablateDefendOne;"));
			Assert.That(part, Does.Contain("bool DefendOneOn => DefendAllyActive && Info.DefendOneEnabled && !ablateDefendOne.Off(Player, AblationIds.SqDefendOne);"));
		}

		[Test]
		public void TheAlarmDraftsThroughTheOnePickOnlyWhileTheRowIsOnAndTheOldPathsStayOff()
		{
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			var list = team.IndexOf("var draftable = unitsHangingAroundTheBase", StringComparison.Ordinal);
			var one = team.IndexOf("if (DefendOneOn)\n\t\t\t\tdraftable = DefendOnePick(draftable, alarm, now);\n\t\t\telse if (arbiter)", StringComparison.Ordinal);
			var draft = team.IndexOf("if (draftable.Count > 0)", StringComparison.Ordinal);
			Assert.That(list, Is.GreaterThan(0));
			Assert.That(one, Is.GreaterThan(list), "after the draftable list, before the draft");
			Assert.That(draft, Is.GreaterThan(one));
			Assert.That(Count(team, "DefendOnePick("), Is.EqualTo(1));

			// row off: the Q4b share (arbiter) and the whole pool are exactly as before
			Assert.That(team, Does.Contain("var need = DefendArbiterCA.AllyPoolNeed(alarm.AttackValue, Info.ArbiterDefendMarginPercent, teamLeadAliveSeats - 1);"));

			// the own-base guard, the home guard and the squad redirect of the alarm are not replaced
			Assert.That(team, Does.Contain("if (home is { IsValid: true } && home.IsTargetValid && (World.Map.CellContaining(home.Target.CenterPosition) - initialBaseCenter).LengthSquared <= baseRadius)\n\t\t\t\treturn;"));
			Assert.That(team, Does.Contain("DefendArbiterCA.MayTake(priority, ArmyRole.Main, holder)"));
		}

		[Test]
		public void ThePickUsesTheRadiusTheDraftLimitAndTheSharedCooldownOfDefendAlly()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendOne.cs");
			Assert.That(part, Does.Contain("var radius = WDist.FromCells(Info.DefendAllyRadiusCells).Length;"), "the 25-cell ally radius");
			Assert.That(part, Does.Contain("(draftable[i].Actor.CenterPosition - alarm.VictimPos).HorizontalLengthSquared"), "distance to the attacked friendly");
			Assert.That(part, Does.Contain("DefendDraftLimit(inside.Count, false, Info, utilityAxesProviders)"), "the DEFEND-ALLY draft limit, as DefendAllyRespond");
			Assert.That(part, Does.Contain("DefendOne.MayDraft(now, defendAllyNextResponse)"), "the one cooldown");
			Assert.That(part, Does.Contain("defendAllyNextResponse = now + Math.Max(1, Info.DefendAllyCooldownTicks);"), "a draft starts the cooldown the local scan reads");

			// DEFEND-ALLY itself: same field, same cooldown
			var ally = RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("if (defendAllyAttacked.Count == 0 || now < defendAllyNextResponse)"));
			Assert.That(ally, Does.Contain("defendAllyNextResponse = now + Math.Max(1, Info.DefendAllyCooldownTicks);"));
		}

		[Test]
		public void TheReadyTurretHookKeepsItsInputBecauseTheRowNeverWritesTheAlarm()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendOne.cs");
			Assert.That(part, Does.Not.Contain("DefendAlarm"), "the alarm (team.DefendAlarm) is the named input of the ready-turret placement; the row only reads the alarm it is handed");
			Assert.That(part, Does.Not.Contain("RegisterNewSquad"));
			Assert.That(part, Does.Not.Contain("unitsHangingAroundTheBase"), "the row only picks; the alarm's own draft block moves the units");

			// the raise stays where it was
			var team = RepoFile(Bots + "SquadManagerBotModuleCA.V07Defend.cs");
			Assert.That(Count(team, "team.DefendAlarm = alarm;"), Is.EqualTo(1));
		}

		[Test]
		public void TheDevRowNamesWhichOldCopySuppliedWhichPart()
		{
			var part = RepoFile(Bots + "SquadManagerBotModuleCA.DefendOne.cs");
			Assert.That(part, Does.Contain("DiagnosticTeamEvent(\"sq_defend_one\", DefendOne.Detail(DefendOne.SourceAlarm,"));
			var pure = RepoFile(Bots + "BotModuleLogic/DefendOne.cs");
			Assert.That(pure, Does.Contain("\"src=\" + source + \";radius=\" + CopyDefendAlly + \";limit=\" + CopyDefendAlly + \";order=nearest_victim;squads=\" + CopyTeamDefend + \";guard=\" + CopyTeamDefend"));
			foreach (var banned in new[] { "Random", "World", "Actor ", "Player", "FindActors" })
				Assert.That(pure, Does.Not.Contain(banned), "the pure seam is engine-free: " + banned);
		}
	}
}
