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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P3 (a): the refinery-want verdict vocabulary (RefineryWant / RefineryGate) and the single recorded chain of ChooseWantedRefinery. No behaviour row.
	[TestFixture]
	public sealed class RefineryWantTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheLabelsAreTheOldHoldGateStrings()
		{
			Assert.That(RefineryWant.Label(RefineryGate.NoTypes, true, "Allow"), Is.EqualTo("adequate"));
			Assert.That(RefineryWant.Label(RefineryGate.Met, true, "Allow"), Is.EqualTo("adequate"));
			Assert.That(RefineryWant.Label(RefineryGate.NoPowerPlant, false, "Allow"), Is.EqualTo("adequate"));
			Assert.That(RefineryWant.Label(RefineryGate.NoYard, false, "Allow"), Is.EqualTo("adequate"));
			Assert.That(RefineryWant.Label(RefineryGate.UnsafeAll, true, "Allow"), Is.EqualTo("unsafe_all"));
			Assert.That(RefineryWant.Label(RefineryGate.UnsafeField, true, "Allow"), Is.EqualTo("unsafe_field"));
			Assert.That(RefineryWant.Label(RefineryGate.NotBuildable, true, "Allow"), Is.EqualTo("not_buildable"));
			Assert.That(RefineryWant.Label(RefineryGate.MaxRefineries, true, "Allow"), Is.EqualTo("max_refineries"));
			Assert.That(RefineryWant.Label(RefineryGate.Economy, true, "FactoryWait"), Is.EqualTo("economy_FactoryWait"));
			Assert.That(RefineryWant.Label(RefineryGate.None, true, "Allow"), Is.EqualTo("economy_Allow"), "the chain passed but the pick was empty: the economy verdict");
			Assert.That(RefineryWant.Label(RefineryGate.None, false, "Allow"), Is.EqualTo("power"), "economy gate off: the power substitute");
		}

		[Test]
		public void AdequacyMapsToItsGateAndOnlyTheFourAreAdequacyGates()
		{
			Assert.That(RefineryWant.ToGate(RefineryAdequacy.Inadequate), Is.EqualTo(RefineryGate.None));
			Assert.That(RefineryWant.ToGate(RefineryAdequacy.NoTypes), Is.EqualTo(RefineryGate.NoTypes));
			Assert.That(RefineryWant.ToGate(RefineryAdequacy.Met), Is.EqualTo(RefineryGate.Met));
			Assert.That(RefineryWant.ToGate(RefineryAdequacy.NoPowerPlant), Is.EqualTo(RefineryGate.NoPowerPlant));
			Assert.That(RefineryWant.ToGate(RefineryAdequacy.NoYard), Is.EqualTo(RefineryGate.NoYard));

			foreach (RefineryGate g in Enum.GetValues(typeof(RefineryGate)))
				Assert.That(RefineryWant.IsAdequacy(g), Is.EqualTo(g is RefineryGate.NoTypes or RefineryGate.Met or RefineryGate.NoPowerPlant or RefineryGate.NoYard), g.ToString());
		}

		[Test]
		public void TheChainRecordsOneGatePerExitInTheOldOrder()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			var chain = qm.Substring(qm.IndexOf("ActorInfo ChooseWantedRefinery(", StringComparison.Ordinal));
			chain = chain.Substring(0, chain.IndexOf("// REF-FIELD-02: true = the refinery pick waits", StringComparison.Ordinal));

			var order = new[]
			{
				"var adequacy = baseBuilder.AdequateReason();",
				"return RefineryHeld(RefineryWant.ToGate(adequacy));",
				"return RefineryHeld(RefineryGate.UnsafeAll);",
				"return RefineryHeld(RefineryGate.UnsafeField);",
				"var refinery = GetProducibleBuilding(baseBuilder.Info.RefineryTypes, buildableThings);",
				"return RefineryHeld(RefineryGate.NotBuildable);",
				"baseBuilder.HasMaxRefineriesFor(refinery)",
				"return RefineryHeld(RefineryGate.MaxRefineries);",
				"RefineryEconomyGateOn && RefineryEconomyHolds(queue, buildableThings)",
				"return RefineryHeld(RefineryGate.Economy);",
				"var pick = BaseBuilderQueueEvalCA.PickOrPower(refinery, power, HasSufficientPowerForActor);",
			};
			var at = -1;
			foreach (var step in order)
			{
				var next = chain.IndexOf(step, at + 1, StringComparison.Ordinal);
				Assert.That(next, Is.GreaterThan(at), step);
				at = next;
			}

			Assert.That(chain, Does.Not.Contain("return null;"), "every hold exit records its gate");
		}

		[Test]
		public void TheHoldLabelIsReadFromTheRecordedGateNotAskedAgain()
		{
			var stages = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs");
			var gate = stages.Substring(stages.IndexOf("string RefineryHoldGate(", StringComparison.Ordinal));
			gate = gate.Substring(0, gate.IndexOf("// ref_s2_hold_log", StringComparison.Ordinal));
			Assert.That(gate, Does.Contain("RefineryWant.Label(lastRefineryGate, RefineryEconomyGateOn, lastRefineryEconomyVerdict.ToString());"));
			Assert.That(gate, Does.Not.Contain("HasAdequateRefineryCount"), "no second copy of the chain");
			Assert.That(gate, Does.Not.Contain("ExpansionRefineryBlockedByAllUnsafe"));

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool HasAdequateRefineryCount() => AdequateReason() != RefineryAdequacy.Inadequate;"));
			Assert.That(bb, Does.Contain("return AIUtils.CountActorByCommonName(ConstructionYardBuildings) == 0 ? RefineryAdequacy.NoYard : RefineryAdequacy.Inadequate;"));
		}

		[Test]
		public void TheDevStreamIsCompiledOutOfARelease()
		{
			var s = Source(Dir + "BaseBuilderQueueManagerCA.RefineryWant.cs");
			Assert.That(s, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid RefineryWantObserved("));
			Assert.That(s, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\tulong refineryWantStream"));
			Assert.That(s, Does.Contain("BB-REFWANT-STREAM"));
			Assert.That(s, Does.Contain("1099511628211UL"), "FNV-1a 64");
		}
	}
}
