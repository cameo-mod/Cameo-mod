#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// TEAM-EXPAND P1 activation blockers: the save text round-trips every estimator/claim field, a damaged text restores nothing,
	// and a produced MCV binds to an expansion request only in FIFO order and only when it answers one.
	[TestFixture]
	public sealed class TeamExpandSaveBindTest
	{
		static string Source() => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.TeamExpand.cs")).Replace("\r\n", "\n");

		static ExpansionPlannerBotModule.SavedExpansion Sample() => new(
			2, 41,
			[new(3, 4, 900, null), new(9, 9, 0, 77u)],
			[new(77u, new(9, 9))],
			[new(1, 2), new(5, 6)], 1200, 2,
			[new(3, 4, 800, "mcv", null), new(7, 7, 850, "mcv", 12u)],
			750, 4000, 123456789012L,
			[new(10, 10), new(10, 11)]);

		[Test]
		public void TheSaveTextRoundTripsEveryField()
		{
			var state = Sample();
			var text = ExpansionPlannerBotModule.ExpansionSave.Encode(state);
			var back = ExpansionPlannerBotModule.ExpansionSave.Decode(text);
			Assert.That(back, Is.Not.Null);
			Assert.That(back.Version, Is.EqualTo(2));
			Assert.That(back.Epoch, Is.EqualTo(41));
			Assert.That(back.Claims, Is.EqualTo(state.Claims));
			Assert.That(back.Mcvs, Is.EqualTo(state.Mcvs));
			Assert.That(back.BidSites, Is.EqualTo(state.BidSites));
			Assert.That(back.BidDeadline, Is.EqualTo(1200));
			Assert.That(back.BidLimit, Is.EqualTo(2));
			Assert.That(back.Pending, Is.EqualTo(state.Pending));
			Assert.That(back.ForecastTick, Is.EqualTo(750));
			Assert.That(back.Deadline, Is.EqualTo(4000));
			Assert.That(back.Stock, Is.EqualTo(123456789012L));
			Assert.That(back.Observed, Is.EqualTo(state.Observed));
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Encode(back), Is.EqualTo(text), "encode is deterministic");
		}

		[Test]
		public void ADamagedForeignOrOtherVersionTextRestoresNothing()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode(null), Is.Null);
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode(string.Empty), Is.Null);
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode("not base64 !!"), Is.Null);
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode(Convert.ToBase64String(new byte[] { 1, 2, 3 })), Is.Null);
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"Version\":1}"))), Is.Null);
			var wrong = ExpansionPlannerBotModule.ExpansionSave.Encode(Sample() with { Version = 3 });
			Assert.That(ExpansionPlannerBotModule.ExpansionSave.Decode(wrong), Is.Null);
		}

		[Test]
		public void MissingArraysDecodeAsEmpty()
		{
			var json = "{\"Version\":2,\"Epoch\":5,\"BidLimit\":0,\"Deadline\":2147483647}";
			var back = ExpansionPlannerBotModule.ExpansionSave.Decode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)));
			Assert.That(back, Is.Not.Null);
			Assert.That(back.Claims, Is.Empty);
			Assert.That(back.Pending, Is.Empty);
			Assert.That(back.Observed, Is.Empty);
		}

		static ExpansionPlannerBotModule.PendingExpansion[] Queue() =>
		[
			new(1, 1, 100, "mcv", null),
			new(2, 2, 200, "mcv", 12u),
			new(3, 3, 300, "other", null),
		];

		[Test]
		public void AProducedMcvBindsToTheOldestRequestOfItsType()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), "mcv", 99u, true), Is.EqualTo(0));
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), "other", 99u, true), Is.EqualTo(2));
		}

		[Test]
		public void ARequestThatRecordedItsFactoryBindsOnThatFactoryFirst()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), "mcv", 12u, true), Is.EqualTo(1));
		}

		[Test]
		public void ARecoveryOrBootstrapMcvWithNoLiveYardNeverBinds()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), "mcv", 12u, false), Is.EqualTo(-1));
		}

		[Test]
		public void ATypeWithNoRequestAndAnEmptyQueueNeverBind()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), "harvester", 12u, true), Is.EqualTo(-1));
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match([], "mcv", 12u, true), Is.EqualTo(-1));
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestBinder.Match(Queue(), null, 12u, true), Is.EqualTo(-1));
		}

		[Test]
		public void ARequestThatDidNotLandIsNeverCommitted()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.ShouldCommit(0), Is.False, "not on the builder's books: no pending entry");
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.ShouldCommit(1), Is.True);
		}

		[Test]
		public void APendingRequestWithNoIntentAndNothingQueuedIsDroppedAfterOneEpoch()
		{
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.Orphaned(249, false, false), Is.False, "grace: the intent may still be turning into a queue item");
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.Orphaned(250, false, false), Is.True);
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.Orphaned(2000, true, false), Is.False, "intent still live");
			Assert.That(ExpansionPlannerBotModule.ExpansionRequestPolicy.Orphaned(2000, false, true), Is.False, "being built");
		}

		[Test]
		public void TheClaimIsCommittedOnlyAfterTheProductionRequestAndOnlyWhenItLanded()
		{
			var planner = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", "mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs")).Replace("\r\n", "\n");
			var request = planner.IndexOf("unitBuilder.RequestUnitProduction(bot, mcvType);", StringComparison.Ordinal);
			var commit = planner.IndexOf("CommitExpansionRequest(mcvType);", StringComparison.Ordinal);
			Assert.That(request, Is.GreaterThan(0));
			Assert.That(commit, Is.GreaterThan(request), "request first, commit after");
			Assert.That(planner, Does.Contain("ExpansionClaimOn && ExpansionRequestPolicy.ShouldCommit(unitBuilder.RequestedProductionCount(bot, mcvType))"));
			Assert.That(Source(), Does.Contain("ExpansionRequestPolicy.Orphaned(age,"));
		}

		[Test]
		public void TheWiringSavesOnAnySwitchBindsOnlyThroughTheBinderAndRaisesNoRequestWithoutAYard()
		{
			var code = Source();
			Assert.That(code, Does.Contain("if (!ExpansionAtlasOn)\n\t\t\t\treturn null;"));
			Assert.That(code, Does.Contain("ExpansionRequestBinder.Match(expansionPending, produced.Info.Name, producer.ActorID, HasLiveConstructionYard())"));
			Assert.That(code, Does.Contain("if (!HasLiveConstructionYard())\n\t\t\t\treturn false;"));
			Assert.That(code, Does.Contain("registry.Epoch = Math.Max(registry.Epoch, state.Epoch);"));
			Assert.That(code, Does.Not.Contain("expansionPendingSite"));
		}
	}
}
