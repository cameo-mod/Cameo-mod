#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// V01 RCA (Opus 11-24, P0-V01-INT-6V6 60k timeout): PROD-BUSY-03 (a met share mix must not idle a unit queue once
	// IdleBaseUnitsMaximum units sit at base) and STALEMATE-01 r4 (the late attack delay shrinks again every 5k down to a floor).
	[TestFixture]
	public class V01RcaFixTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai")
			.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string Field(MiniYamlNode node, string key) => node.Value.Nodes.Single(n => n.Key == key).Value.Value.Trim();

		[Test]
		public void ShareFallbackRunsWhateverTheBankWhileTheSwitchIsOn()
		{
			Assert.That(ProdBusyCA.ShareFallback(true, false), Is.True, "on: an unsaturated bank no longer idles the queue");
			Assert.That(ProdBusyCA.ShareFallback(true, true), Is.True);
			Assert.That(ProdBusyCA.ShareFallback(false, true), Is.True, "off: the old saturated-bank retry");
			Assert.That(ProdBusyCA.ShareFallback(false, false), Is.False, "off: as before, a met mix idles the queue");
		}

		[Test]
		public void IdleCapSwitchIsArmedGatedAndHasItsOwnRow()
		{
			var unit = PlayerNode("UnitBuilderBotModuleCA@generic");
			Assert.That(Field(unit, "SwitchCondition"), Is.EqualTo("genericbot"));
			Assert.That(Field(unit, "ShareTestNeverIdles"), Is.EqualTo("true"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("pb_idle_cap"));

			var src = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(src, Does.Contain("bool IdleCapOn => switchesActive && Info.ShareTestNeverIdles && !ablateIdleCap.Off(player, AblationIds.ProdBusyIdleCap);"));
			Assert.That(src, Does.Contain("if (ProdBusyCA.ShareFallback(IdleCapOn, saturated))"));
			Assert.That(src, Does.Not.Contain("\t\t\tif (saturated)\n\t\t\t{\n\t\t\t\tvar fallback"), "the retry reads the switch-aware gate");
		}

		[Test]
		public void LateDelayPercentStepsEveryFiveThousandTicksToTheFloor()
		{
			// Defaults: 50% from 25k, x0.75 per further 5k, floor 20%.
			int At(int tick) => Stalemate01CA.SteppedPercent(tick, 25000, 50, 5000, 75, 20);
			Assert.That(At(25000), Is.EqualTo(50));
			Assert.That(At(29999), Is.EqualTo(50), "a step needs a full 5k");
			Assert.That(At(30000), Is.EqualTo(37));
			Assert.That(At(35000), Is.EqualTo(27));
			Assert.That(At(40000), Is.EqualTo(20), "27 x 0.75 = 20");
			Assert.That(At(60000), Is.EqualTo(20), "the floor holds");
			Assert.That(Stalemate01CA.Scale(2500, At(40000)), Is.EqualTo(500), "turtle delay reset at 40k");
		}

		[Test]
		public void LateDelayPercentEdgeCases()
		{
			Assert.That(Stalemate01CA.SteppedPercent(40000, 25000, 50, 0, 75, 20), Is.EqualTo(50), "0 step ticks = the r1 constant percent");
			Assert.That(Stalemate01CA.SteppedPercent(20000, 25000, 50, 5000, 75, 20), Is.EqualTo(50), "before the start: the base percent");
			Assert.That(Stalemate01CA.SteppedPercent(60000, 25000, 50, 5000, 75, 80), Is.EqualTo(50), "a floor above the base never raises it");
			Assert.That(Stalemate01CA.SteppedPercent(60000, 25000, 50, 5000, 0, 20), Is.EqualTo(20), "a zero step drops straight to the floor");
			Assert.That(Stalemate01CA.SteppedPercent(int.MaxValue, 0, 50, 1, 75, 0), Is.EqualTo(0), "no floor: the loop ends at 0");
		}

		[Test]
		public void LateDelayUsesTheSteppedPercentAndTheBarIsUnchanged()
		{
			var src = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate01.cs");
			Assert.That(src, Does.Contain("Stalemate01CA.SteppedPercent(World.WorldTick, Info.LateEscalationStartTick, Info.LateEscalationDelayPercent,"));
			Assert.That(src, Does.Contain("int LateBar(int bar) => LateEscalationActive ? Stalemate01CA.Scale(bar, Info.LateEscalationForcePercent) : bar;"),
				"the launch bar keeps its r1 scale; WAVE-MIN still runs after it");
		}
	}
}
