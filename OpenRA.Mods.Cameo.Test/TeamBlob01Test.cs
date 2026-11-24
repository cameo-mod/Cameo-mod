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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// TEAM-BLOB-01 (V02 RCA, USER P0 2026-11-24): V02 published 4 orders in 60000 ticks because the team's idle blob (0-9 units) almost never
	/// reached the WAVE-MIN bar, and the "1500-tick fallback" never fired (it still needed a ready blob). The no-order guarantee is real now
	/// (tl_fallback_publish), an order survives a leader re-election (tl_order_survive) and every cancel / push / abandon says why.
	/// </summary>
	[TestFixture]
	public class TeamBlob01Test
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative));
		}

		static TeamLeadCA.PublishWhy Why(int sinceOrder, int units, int waveMinUnits = 12, int fallback = 1500, int forced = 3000, int floor = 3) =>
			TeamLeadCA.FallbackPublish(sinceOrder, fallback, forced, units, BlobLaunch01CA.MinUnits(waveMinUnits, units), floor);

		// ---- the no-order guarantee ----

		[Test]
		public void BeforeTheFallbackNothingIsPublishedByTheGuarantee()
		{
			Assert.That(Why(1499, 20), Is.EqualTo(TeamLeadCA.PublishWhy.None));
			Assert.That(Why(0, 20), Is.EqualTo(TeamLeadCA.PublishWhy.None));
		}

		[Test]
		public void After1500TicksTheLeaderPublishesAsSoonAsTheBlobsUnitsReachTheBlobMinimum()
		{
			// WAVE-MIN 12 units: 12 units pass at 1500 (the value bar is not asked), 11 do not.
			Assert.That(Why(1500, 12), Is.EqualTo(TeamLeadCA.PublishWhy.Fallback));
			Assert.That(Why(1500, 40), Is.EqualTo(TeamLeadCA.PublishWhy.Fallback));
			Assert.That(Why(1500, 11), Is.EqualTo(TeamLeadCA.PublishWhy.None), "the blob is below the launch minimum");
			Assert.That(Why(2999, 11), Is.EqualTo(TeamLeadCA.PublishWhy.None));
		}

		[Test]
		public void After3000TicksTheLeaderPublishesWithWhateverTheBlobHoldsIfItIsNotEmpty()
		{
			Assert.That(Why(3000, 11), Is.EqualTo(TeamLeadCA.PublishWhy.Forced), "V02: 6-11 units never reached the bar for 3000 ticks");
			Assert.That(Why(3000, 3), Is.EqualTo(TeamLeadCA.PublishWhy.Forced), "the join floor");
			Assert.That(Why(3000, 2), Is.EqualTo(TeamLeadCA.PublishWhy.None), "an almost empty blob never goes out");
			Assert.That(Why(3000, 0), Is.EqualTo(TeamLeadCA.PublishWhy.None));
			Assert.That(Why(9000, 40), Is.EqualTo(TeamLeadCA.PublishWhy.Forced), "the later step wins once it is due");
		}

		[Test]
		public void AZeroTurnsAStepOffAndNoTierMeansTheFloorDecides()
		{
			Assert.That(Why(5000, 20, forced: 0), Is.EqualTo(TeamLeadCA.PublishWhy.Fallback));
			Assert.That(Why(5000, 5, forced: 0), Is.EqualTo(TeamLeadCA.PublishWhy.None));
			Assert.That(Why(5000, 20, fallback: 0, forced: 0), Is.EqualTo(TeamLeadCA.PublishWhy.None));

			// No WAVE-MIN tier (0 units): half of the blob is the minimum, so any blob at the floor passes the fallback.
			Assert.That(Why(1500, 4, waveMinUnits: 0), Is.EqualTo(TeamLeadCA.PublishWhy.Fallback));
		}

		/// <summary>The V02 sequence on a team that never reached the bar: nothing, nothing, then the forced push; one order resets the clock.</summary>
		[Test]
		public void TheV02TeamThatNeverReachedTheBarGetsAnOrderEvery3000Ticks()
		{
			var lastOrder = -1;
			var orders = 0;
			const int Start = 3000;
			for (var now = Start; now <= 60000; now += 100)
			{
				var since = BlobLaunch01CA.TicksSinceOrder(now, lastOrder, Start);
				var blobUnits = 8; // V02 team idle: 0-9 for most of the game, 12 is the Hard bar
				var ready = TeamLeadCA.BlobReady(blobUnits, 2500, 3, true, 12, 6000);
				var why = TeamLeadCA.OrderDue(false, ready, now, lastOrder, 1500)
					? TeamLeadCA.PublishWhy.Ready
					: TeamLeadCA.FallbackPublish(since, 1500, 3000, blobUnits, BlobLaunch01CA.MinUnits(12, blobUnits), 3);
				if (why == TeamLeadCA.PublishWhy.None)
					continue;

				Assert.That(why, Is.EqualTo(TeamLeadCA.PublishWhy.Forced));
				orders++;
				lastOrder = now;
			}

			Assert.That(orders, Is.GreaterThanOrEqualTo(18), "about one order per 3000 ticks over 57000 ticks (V02 had 4 in 60000)");
		}

		// ---- the order survives a leader re-election ----

		[Test]
		public void AHandedOverOrderKeepsItsSquadsAndItsClockAndOnlyTheLeaderChanges()
		{
			var order = new TeamLeadOrder(7, "Multi2", new WPos(1024, 1024, 0), new WPos(9000, 9000, 0), 42, 6003);
			var squad = new object();
			order.Report(squad, 4160, true);
			Assert.That(order.LeaderKey, Is.EqualTo("Multi2"));

			order.HandOver("Multi5");
			Assert.That(order.LeaderKey, Is.EqualTo("Multi5"));
			Assert.That(order.Cancelled, Is.False, "a hand-over is not a cancel");
			Assert.That(order.ReportCount, Is.EqualTo(1), "the gathered squads stay");
			Assert.That(order.IssueTick, Is.EqualTo(6003), "the order keeps its age: the gather window does not restart");
			Assert.That(order.Seq, Is.EqualTo(7));
			Assert.That(order.TargetActorId, Is.EqualTo(42u));
		}

		// ---- wiring pins ----

		[Test]
		public void TheSwitchesDefaultOffAreOnInSixPersonalitiesNotClassicAndHaveTheirOwnRows()
		{
			var info = RepoFile(Bots + "SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool TeamLeadFallbackPublishEnabled = false;"));
			Assert.That(info, Does.Contain("public readonly int TeamLeadForcedPublishTicks = 3000;"));
			Assert.That(info, Does.Contain("public readonly bool TeamLeadOrderSurviveEnabled = false;"));

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			var classic = yaml.IndexOf("SquadManagerBotModuleCA@classic:", StringComparison.Ordinal);
			Assert.That(classic, Is.GreaterThan(0));
			foreach (var key in new[] { "TeamLeadFallbackPublishEnabled", "TeamLeadOrderSurviveEnabled" })
			{
				Assert.That(Regex.Matches(yaml[..classic], @"\n\t\t" + key + ": true").Count, Is.EqualTo(6), key);
				Assert.That(yaml[classic..], Does.Not.Contain(key), key + ": @classic keeps the default (off)");
			}

			Assert.That(AblationIds.TeamLeadFallbackPublish, Is.EqualTo("tl_fallback_publish"));
			Assert.That(AblationIds.TeamLeadOrderSurvive, Is.EqualTo("tl_order_survive"));
		}

		[Test]
		public void ThePublishAsksTheReadyRuleFirstAndTheGuaranteeOnlyWhenThatSaysNo()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadPublish(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("void TeamLeadJoin(", start, StringComparison.Ordinal)];
			Assert.That(body.IndexOf("TeamLeadCA.OrderDue(", StringComparison.Ordinal), Is.GreaterThan(0));
			Assert.That(body.IndexOf("TeamLeadCA.FallbackPublish(", StringComparison.Ordinal), Is.GreaterThan(body.IndexOf("TeamLeadCA.OrderDue(", StringComparison.Ordinal)));
			Assert.That(body, Does.Contain("if (why == TeamLeadCA.PublishWhy.None && TeamLeadFallbackPublishOn)"));
			Assert.That(body, Does.Contain("BlobLaunch01CA.MinUnits(minUnits, blob.Units)"), "the same blob minimum as the leader's launch gate");
			Assert.That(body, Does.Contain("BlobLaunch01CA.TicksSinceOrder("));
			Assert.That(body, Does.Contain("if (now < initialAttackDelay || TeamLeadOpenOrder() != null)"), "never while an order is open or before the initial attack delay");
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_order\""));
			Assert.That(wiring, Does.Contain("bool TeamLeadFallbackPublishOn => TeamLeadCadenceOn && Info.TeamLeadFallbackPublishEnabled && !ablateFallbackPublish.Off(Player, AblationIds.TeamLeadFallbackPublish);"));
		}

		[Test]
		public void ALeaderChangeHandsTheOrderOverOnlyWithTheRowOnAndEveryCancelSaysWhy()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			var start = wiring.IndexOf("void TeamLeadMaintainOrder(", StringComparison.Ordinal);
			var body = wiring[start..wiring.IndexOf("TeamLeadOrder TeamLeadOpenOrder()", start, StringComparison.Ordinal)];
			Assert.That(body, Does.Contain("if (order.LeaderKey != team.Election.Leader && team.Election.Leader != null && TeamLeadOrderSurviveOn)"));
			Assert.That(body, Does.Contain("order.HandOver(team.Election.Leader);"));
			foreach (var why in new[] { "leader_changed", "max_age", "no_reports" })
				Assert.That(body, Does.Contain("\"" + why + "\""), why);
			Assert.That(body, Does.Contain("order.CancelReason = cancel;"));
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_cancel\""));
			Assert.That(body, Does.Contain("DiagnosticTeamEvent(\"team_push\""));
			Assert.That(wiring, Does.Contain("bool TeamLeadOrderSurviveOn => TeamLeadOn && Info.TeamLeadOrderSurviveEnabled && !ablateOrderSurvive.Off(Player, AblationIds.TeamLeadOrderSurvive);"));
		}

		[Test]
		public void TheGatherStateSaysWhyASquadLeavesTheOrderAndWhenItGoes()
		{
			var gather = RepoFile(Bots + "Squads/States/TeamLeadGatherStateCA.cs");
			Assert.That(gather, Does.Contain("\"not_engaged\""));
			Assert.That(gather, Does.Contain("\"cancelled:\" + (order.CancelReason ?? \"unknown\")"));
			Assert.That(gather, Does.Contain("\"resolve_failed\""));
			Assert.That(gather, Does.Contain("manager.TeamLeadNoteContact(owner, order);"));
			Assert.That(gather, Does.Contain("manager.TeamLeadNoteGo(owner, order);"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs");
			Assert.That(wiring, Does.Contain("internal void TeamLeadAbandon(SquadCA squad, string why = null)"));
			Assert.That(wiring, Does.Contain("DiagnosticSquadEvent(squad, \"team_abandon\", why);"));
			Assert.That(new[] { "team_order", "team_push", "team_cancel", "team_handover", "team_contact", "team_go" }.All(e => wiring.Contains("\"" + e + "\"")), Is.True);
		}
	}
}
