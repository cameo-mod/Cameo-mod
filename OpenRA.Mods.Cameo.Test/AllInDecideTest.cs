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

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P10 phase (a) (DESIGN_02): every all-in decision of TeamLeadAllInWhy passes through AllInReturn unchanged; the stalemate signal stays a live read.
	[TestFixture]
	public sealed class AllInDecideTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void EveryDecisionOfTheTeamAllInGoesThroughTheRecorder()
		{
			var v07 = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("return AllInReturn(now, ahead);"));
			Assert.That(v07, Does.Contain("return AllInReturn(now, AllInCA.WhyLate);"));
			Assert.That(v07, Does.Contain("return AllInReturn(now, AllInCA.Why(gate.AtCap, gate.Huge, gate.Won)"));
			Assert.That(v07, Does.Contain("var gate = new AllInGate(now, atCap, huge, won, blob.Units, blob.Value);"));
			// Both explicit null exits (not enabled; open order that is all-in or gathers) go through the recorder: exactly two.
			Assert.That(v07.Split(new[] { "return AllInReturn(now, null);" }, StringSplitOptions.None).Length - 1, Is.EqualTo(2));
			Assert.That(v07, Does.Not.Contain("return ahead;"), "a bare decision return would skip the recorder");
			Assert.That(v07, Does.Not.Contain("return AllInCA.WhyLate;"));
		}

		[Test]
		public void TheRecorderIsDevOnlyAndHandsBackTheSameDecision()
		{
			var decide = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AllInDecide.cs");
			Assert.That(decide, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid AllInRecord(int now, string why)"));
			Assert.That(decide, Does.Contain("AllInRecord(now, why);\n\t\t\treturn why;"), "the decision handed back is the one recorded");
			Assert.That(decide, Does.Contain("const int AllInDecideRowTicks = 5000;"));
		}

		[Test]
		public void TheAheadPickWritesNothingAndTakesTheDefeatCountIn()
		{
			var ahead = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AllInAhead.cs");
			Assert.That(ahead, Does.Contain("long AheadFired(AheadTrigger trigger, AheadView v, int defeatsHandled) => trigger switch"));
			Assert.That(ahead, Does.Contain("AllInAhead.Defeat(v.Defeated, defeatsHandled, "));
			var start = ahead.IndexOf("(int Index, long Ratio) AheadPick(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			var end = ahead.IndexOf("\n\t\t}", start, StringComparison.Ordinal) + 4;
			var body = ahead.Substring(start, end - start);
			Assert.That(body, Does.Contain("AheadFired(trigger, v, defeatsHandled)"));
			Assert.That(body, Does.Not.Contain("aheadLastFired"), "the pick writes no latch");
			Assert.That(body, Does.Not.Contain("aheadDefeatsHandled"), "the pick writes no defeat count");
			Assert.That(body, Does.Not.Contain("AheadRecord("), "the pick records nothing");
			Assert.That(ahead, Does.Contain("aheadDefeatsHandled = v.Defeated;"), "the defeat write stays in AheadWhy, on the branch that fired");
		}

		[Test]
		public void TheStalemateSignalStaysLiveAtTheDecision()
		{
			var v07 = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("AllInCA.StalemateSignal(now, team.StalemateSignalTick, Info.ArmyStalemateSignalTicks)"));
			var fold = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AllInFold.cs");
			Assert.That(fold, Does.Contain("AllInCA.StalemateSignal(now, teamLeadTeam?.StalemateSignalTick ?? -1, Info.ArmyStalemateSignalTicks)"), "the late check reads the signal as of now");
		}
	}
}
