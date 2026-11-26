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
			Assert.That(v07, Does.Contain("return AllInReturn(now, AllInCA.Why(atCap, huge, won)"));
			Assert.That(v07, Does.Contain("return AllInReturn(now, null);"));
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
		public void TheStalemateSignalStaysLiveAtTheDecision()
		{
			var v07 = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.V07.cs");
			Assert.That(v07, Does.Contain("AllInCA.StalemateSignal(now, team.StalemateSignalTick, Info.ArmyStalemateSignalTicks)"));
			var fold = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AllInFold.cs");
			Assert.That(fold, Does.Contain("AllInCA.StalemateSignal(now, teamLeadTeam?.StalemateSignalTick ?? -1, Info.ArmyStalemateSignalTicks)"), "the late check reads the signal as of now");
		}
	}
}
