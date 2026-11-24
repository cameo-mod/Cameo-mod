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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// TECH-LOG-01 (DEV only): tech_log.csv rows, the 750-tick reject throttle, and the instrumentation points in the base builder.
	// Owner: Code Monkey. Reviewer: Sonnet.
	[TestFixture]
	public sealed class TechLog01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[TestCase(749, 750, false)]
		[TestCase(750, 750, true)]
		[TestCase(751, 750, true)]
		public void ARejectRowWaitsForItsThrottleWindow(int now, int nextAllowed, bool due) =>
			Assert.That(TechLogCA.RejectRowDue(now, nextAllowed), Is.EqualTo(due));

		[Test]
		public void TheRowCarriesTickSeatFactionKindTypeAndReason()
		{
			Assert.That(TechLogCA.Row(1234, "seat_2", "allies", "rejected", "dome", "vote"), Is.EqualTo("1234,seat_2,allies,rejected,dome,vote"));
			Assert.That(TechLogCA.Header, Is.EqualTo("tick,seat,faction,kind,type,reason"));
			Assert.That(TechLogCA.RejectThrottleTicks, Is.EqualTo(750));
			Assert.That(TechLogCA.FileName, Is.EqualTo("tech_log.csv"));
		}

		// queueItems = what this seat's queue can build (AllItems); foreignTech is another faction's type and is not among them.
		[TestCase(new[] { "own_tech" }, new[] { "own_tech" }, new[] { "own_tech" }, new string[0])]
		[TestCase(new[] { "own_tech" }, new string[0], new[] { "own_tech" }, new[] { "own_tech" })]
		[TestCase(new[] { "own_tech", "plain" }, new string[0], new[] { "own_tech" }, new[] { "own_tech" })]
		[TestCase(new string[0], new string[0], new[] { "foreign_tech" }, new string[0])]
		public void OnlyATechTypeThisQueueOffersAndCannotBuildYetIsAPrereqRow(string[] queueItems, string[] buildable, string[] chain, string[] expected)
		{
			var missing = TechLogCA.PrereqMissing(queueItems, new HashSet<string>(chain).Contains, new HashSet<string>(buildable)).ToArray();
			Assert.That(missing, Is.EqualTo(expected));
		}

		[Test]
		public void AForeignTechTypeNeverGetsAPrereqRow()
		{
			var missing = TechLogCA.PrereqMissing(new string[0], s => s == "foreign_tech", new HashSet<string>()).ToArray();
			Assert.That(missing, Is.Empty);
		}

		[Test]
		public void TheBaseBuilderReportsEachRejectionReasonAndFirstCompletions()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(q, Does.Contain("TechLogCompletions();"));
			Assert.That(q, Does.Contain("TechReject(name, \"nospace\");"));
			Assert.That(q, Does.Contain("TechReject(actorInfo.Name, \"power\");"));
			Assert.That(q, Does.Contain("TechReject(name, \"delay\");"));
			Assert.That(q, Does.Contain("TechReject(item.Name, \"vote\");"));
			Assert.That(q, Does.Contain("TechReject(prereqType, \"prereq\");"));
			Assert.That(q, Does.Contain("TechLogCA.PrereqMissing(queue.AllItems().Select(i => i.Name), baseBuilder.Info.TechChainTypes.Contains, techBuildable)"));
		}

		[Test]
		public void ReleaseHasANoOpSinkForTheRow()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/CameoLogSink.cs"),
				Does.Contain("public void TechLogEvent(int playerIndex, string faction, int tick, string kind, string type, string reason) { }"));
		}
	}
}
