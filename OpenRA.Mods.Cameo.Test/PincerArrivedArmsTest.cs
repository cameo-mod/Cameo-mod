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
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// fix-03 (Sol V08F_CUMREV_SOL_02): item 1 (the pincer-at-push debug line is DEV-guarded) and item 2 (row pincer_arrived_arms: the arm plan reads the arrived units and value).
	[TestFixture]
	public sealed class PincerArrivedArmsTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheDebugLineIsDevGuarded()
		{
			var push = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(push, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tAIUtils.BotDebug(\"AI ({0}): pincer at push seq {1}"));
		}

		[Test]
		public void TheSwitchIsDefaultOffAndTheRowIsRegistered()
		{
			Assert.That(AblationIds.PincerArrivedArms, Is.EqualTo("pincer_arrived_arms"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.PincerArrivedArms));

			var info = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCAInfo.TeamLead.cs");
			Assert.That(info, Does.Contain("public readonly bool PincerArrivedArms = false;"));

			var yaml = Source("mods/cameo/ai/ai.yaml");
			Assert.That(yaml, Does.Not.Contain("PincerArrivedArms: true"), "master ships with the row off (untested)");
			Assert.That(yaml, Does.Contain("PincerArrivedArms: false"));
		}

		[Test]
		public void OffIsTheCommittedTotalsAndOnIsTheArrivedArms()
		{
			var push = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.PincerPush.cs");
			Assert.That(push, Does.Contain("internal bool PincerArrivedArmsOn => Info.PincerArrivedArms && !ablatePincerArrivedArms.Off(Player, AblationIds.PincerArrivedArms);"));
			Assert.That(push, Does.Contain("foreach (var (squad, squadUnits, squadValue) in PincerArrivedArmsOn ? order.ArrivedReported() : order.Reported())"));

			var logic = Source("mods/CA/Traits/BotModules/BotModuleLogic/TeamLeadCA.cs");
			Assert.That(logic, Does.Contain("public List<(object Squad, int Units, long Value)> ArrivedReported()"));
			Assert.That(logic, Does.Contain("if (n > 0)"), "a squad with none arrived is not an arm");
		}
	}
}
