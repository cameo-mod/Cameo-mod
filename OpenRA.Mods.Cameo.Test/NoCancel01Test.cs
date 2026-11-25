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
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// NO-CANCEL-01 (row bb_no_cancel, BS-03 c10 port, Opus V08G): a building in production is never cancelled for a choice. The pure gate is tested by
	// tables, the six sites by source pins (each keeps its existing condition and adds the guard with &&; the order sites stay exactly six), the arm by yaml.
	[TestFixture]
	public sealed class NoCancel01Test
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		static int Count(string text, string needle) => text.Split(needle).Length - 1;

		[Test]
		public void MayCancelIsTheLegacyAnswerWhenTheRowIsOffAndReadyPlusProofWhenOn()
		{
			foreach (var ready in new[] { false, true })
				foreach (var proof in new[] { false, true })
					Assert.That(NoCancelGatesCA.MayCancel(false, ready, proof), Is.True, $"row off ready={ready} proof={proof}: today's answer");

			Assert.That(NoCancelGatesCA.MayCancel(true, true, true), Is.True, "READY with an unplaceable proof");
			Assert.That(NoCancelGatesCA.MayCancel(true, false, true), Is.False, "in production: finish, never remove");
			Assert.That(NoCancelGatesCA.MayCancel(true, true, false), Is.False, "a choice (the limit) is never a proof");
			Assert.That(NoCancelGatesCA.MayCancel(true, false, false), Is.False);
		}

		[Test]
		public void TheDemandUnwindOnlyTurnsAnIssuedVerdictForAnInProgressEntryIntoAHold()
		{
			foreach (var v in new[] { DemandCancelDisposition.Absent, DemandCancelDisposition.AmbiguousHold, DemandCancelDisposition.ProtectionHold })
			{
				Assert.That(NoCancelGatesCA.DemandCancelGate(v, true, false), Is.EqualTo(v), "other verdicts pass through " + v);
				Assert.That(NoCancelGatesCA.DemandCancelGate(v, true, true), Is.EqualTo(v));
			}

			Assert.That(NoCancelGatesCA.DemandCancelGate(DemandCancelDisposition.Issued, true, true), Is.EqualTo(DemandCancelDisposition.Issued), "a Done entry may go");
			Assert.That(NoCancelGatesCA.DemandCancelGate(DemandCancelDisposition.Issued, true, false), Is.EqualTo(DemandCancelDisposition.ProtectionHold), "in progress: held");
			Assert.That(NoCancelGatesCA.DemandCancelGate(DemandCancelDisposition.Issued, false, false), Is.EqualTo(DemandCancelDisposition.Issued), "row off: today");
		}

		[Test]
		public void TheSixSitesKeepTheirConditionAndAddTheGuard()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");

			Assert.That(qm, Does.Contain("bool NoCancelRowOn => baseBuilder.NoCancelArmed;"));

			// site 2 (relocation latch): READY read, a Held record, then continue (no order)
			Assert.That(qm, Does.Contain("if (!NoCancelGatesCA.MayCancel(noCancelOn, item.Done, true))"));
			Assert.That(qm, Does.Contain("baseBuilder.TraceNoCancel(noCancelOn, \"CANCEL-SUPPRESSED\", 2, item.Item, queue.Actor);"));

			// site 3 (limit reached): a choice, proof = false
			Assert.That(qm, Does.Contain("var limitMayCancel = NoCancelGatesCA.MayCancel(noCancelOn, currentBuilding.Done, false);"));
			Assert.That(qm, Does.Contain("if (!limitMayCancel)"));
			Assert.That(qm, Does.Contain("else if (!ProtectionApplies(currentBuilding.Item)"));

			// sites 4, 5, 6: class (b), READY + proof, with && so the guard can only withhold
			Assert.That(qm, Does.Contain("&& NoCancelGatesCA.MayCancel(NoCancelRowOn, currentBuilding.Done, true))"));
			Assert.That(Count(qm, "&& NoCancelGatesCA.MayCancel(NoCancelRowOn, currentBuilding.Done, true))"), Is.EqualTo(2), "sites 4 and 5");
			Assert.That(qm, Does.Contain("!NoCancelGatesCA.MayCancel(NoCancelRowOn, item.Done, true))"), "site 6 (episode certificate)");

			// no order site was added or removed in the queue manager
			Assert.That(Count(qm, "Order.CancelProduction("), Is.EqualTo(5), "sites 2 to 6 stay the only order sites of the manager");
			foreach (var site in new[] { 2, 4, 5, 6 })
				Assert.That(qm, Does.Contain($"\"CANCEL-UNPLACEABLE\", {site},"), "trace site " + site);
			Assert.That(qm, Does.Contain("\"LIMIT-OVERRIDE\", 3,"));
		}

		[Test]
		public void TheDemandUnwindIsGatedBeforeTheRecordAndTheOrder()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			var gate = bb.IndexOf("var gated = NoCancelGatesCA.DemandCancelGate(disposition, noCancelOn, entryDone);", StringComparison.Ordinal);
			var record = bb.IndexOf("b?.NoteExternalCancel(producer, item);", gate, StringComparison.Ordinal);
			var order = bb.IndexOf("bot.QueueOrder(Order.CancelProduction(producer, item, 1));", gate, StringComparison.Ordinal);
			Assert.That(gate, Is.GreaterThan(0));
			Assert.That(record, Is.GreaterThan(gate), "the hold returns before the intent record");
			Assert.That(order, Is.GreaterThan(record));
			Assert.That(Count(bb, "Order.CancelProduction("), Is.EqualTo(1), "still exactly one order site in the module");
			Assert.That(bb, Does.Contain("public bool NoCancelArmed => Info.BbNoCancel && switchesActive && !ablateNoCancel.Off(player, AblationIds.BbNoCancel);"));
			Assert.That(bb, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tpublic void TraceNoCancel("), "DEV logging only");
		}

		[Test]
		public void TheGateReadsNoWorld()
		{
			var gates = Source(Dir + "NoCancelGatesCA.cs");
			Assert.That(gates, Does.Not.Contain("world"));
			Assert.That(gates, Does.Not.Contain("Actor"));
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegisteredAndOffByDefault()
		{
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbNoCancel"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbNoCancel"), Is.Null);
			Assert.That(AblationIds.BbNoCancel, Is.EqualTo("bb_no_cancel"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbNoCancel));
			Assert.That(new BaseBuilderBotModuleCAInfo().BbNoCancel, Is.False);
		}
	}
}
