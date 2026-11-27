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
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// ENGT-ORDERS: the Frans transport provider acts on actors ONLY through orders (a bot runs on the host alone; a direct actor
	// call would desync a multiplayer game). Source-text pins, no World fixture exists for this module. The tier grant itself is
	// NOT part of this pin set: master keeps `Bots: fransbot` until the grant is ruled on and proven in a DEV game.
	[TestFixture]
	public sealed class EngtOrdersTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// Code only: whole-line // comments and trailing // comments are dropped (the module explains itself in comments that name the banned calls).
		static string Code(string text) => string.Join("\n", text.Split('\n').Select(line =>
		{
			var i = line.IndexOf("//", StringComparison.Ordinal);
			return i >= 0 ? line[..i] : line;
		}));

		static string Frans() => Code(Source("mods/Fransbot/Traits/FransTransportCommanderBotModule.cs"));

		[Test]
		public void TheTransportModuleCallsNoActivityOrOrderApiOnAnActorDirectly()
		{
			var code = Frans();
			foreach (var banned in new[] { "CancelActivity", "QueueActivity", "QueueChild", "SetTargetLine", "IssueOrder(", "ResolveOrder(", "world.IssueOrder", ".Cancel(", "new Move(", "new Land(", "new Enter(", "new Wait(", "new Attack(" })
				Assert.That(code, Does.Not.Contain(banned), banned);
		}

		[Test]
		public void EveryOrderTheModuleBuildsIsQueuedOnTheBotNeverResolvedOnTheActor()
		{
			var code = Frans();
			var lines = code.Split('\n').Where(line => line.Contains("new Order(", StringComparison.Ordinal)).ToList();
			Assert.That(lines, Has.Count.EqualTo(18), "the 18 actor commands of the module (Stop, Move, SetUnitStance, Land, EnterTransport, Unload, Attack)");
			foreach (var line in lines)
				Assert.That(line, Does.Contain("QueueOrder("), "an Order that is not handed to the bot: " + line.Trim());

			var verbs = Regex.Matches(code, "new Order\\((?:\"(?<verb>[A-Za-z]+)\"|finalLeg \\? \"Land\" : \"Move\")").Select(m => m.Groups["verb"].Success ? m.Groups["verb"].Value : "LandOrMove").Distinct().OrderBy(v => v).ToList();
			Assert.That(verbs, Is.EqualTo(new[] { "Attack", "EnterTransport", "Land", "LandOrMove", "Move", "SetUnitStance", "Stop", "Unload" }));
		}

		[Test]
		public void EveryDeferredOrderPathGoesThroughTheIssuerGate()
		{
			var code = Frans();
			Assert.That(code, Does.Contain("string IssuerName => issuerName ??= BotIssuer.Of(this, player.PlayerActor);"));
			Assert.That(code, Does.Contain("using var _ = BotIssuer.IssueAs(IssuerName);"));
			Assert.That(Regex.Matches(code, "IssueAs\\(IssuerName\\)").Count, Is.GreaterThanOrEqualTo(8), "the Tick/entry points that issue orders each charge the issuer");
		}

		[Test]
		public void TheMasterGrantStaysInertAndTheStaleDirectSitesClaimIsGone()
		{
			var ai = Source("packaging/ai.yaml");
			Assert.That(ai, Does.Contain("\tGrantConditionOnBotOwner@engttransport:\n\t\tCondition: engt-transport\n\t\tBots: fransbot\n"), "no tier grant on master until Opus rules on it");
			Assert.That(ai, Does.Not.Contain("touches actors DIRECTLY"));
			Assert.That(ai, Does.Contain("ENGT-ORDERS"));
		}
	}
}
