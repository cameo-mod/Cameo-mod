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
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P4 (b): rows pw_inflight_emergency and pw_inflight_admit. The plants in production count in the emergency picks (S1, the legacy override) and in the admission test.
	[TestFixture]
	public sealed class PowerInflightTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		[Test]
		public void APlantInProductionAnswersTheShortageOnlyWhenTheRowIsOn()
		{
			// excess -20 against a margin of 50, a 100 power plant in production (V08H join: the margin was 100, under which -20 + 100 = 80 is still short)
			Assert.That(PowerArbiter.Short(true, PowerArbiter.Effective(-20, 100, false), 50), Is.True, "off: a second emergency plant is picked");
			Assert.That(PowerArbiter.Short(true, PowerArbiter.Effective(-20, 100, true), 50), Is.False, "on: the plant in flight covers it");
			Assert.That(PowerArbiter.Short(true, PowerArbiter.Effective(-20, 0, true), 50), Is.True, "on, nothing in flight: still short");
			Assert.That(PowerArbiter.Short(true, PowerArbiter.Effective(-20, 50, true), 50), Is.True, "on: a plant in flight that does not cover it keeps the emergency");
		}

		[Test]
		public void TheAdmissionTestCountsTheInFlightPlantOnlyWhenTheRowIsOn()
		{
			// a building drawing 60 with excess 20 and a floor of 50: refused raw, admitted with a 100 power plant in flight
			Assert.That(PowerArbiter.Admits(true, -60, PowerArbiter.Effective(20, 100, false), 50), Is.False);
			Assert.That(PowerArbiter.Admits(true, -60, PowerArbiter.Effective(20, 100, true), 50), Is.True);
		}

		[Test]
		public void TheWiringIsPinned()
		{
			var power = Source(Dir + "BaseBuilderQueueManagerCA.Power.cs");
			Assert.That(power, Does.Contain("PowerArbiter.Effective(playerPower?.ExcessPower ?? 0, baseBuilder.InFlightPower, baseBuilder.PwInflightEmergencyArmed);"));
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.cs"), Does.Contain("PowerArbiter.Effective(playerPower?.ExcessPower ?? 0, baseBuilder.InFlightPower, baseBuilder.PwInflightAdmitArmed);"));

			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool PwInflightEmergencyArmed => Info.PwInflightEmergency && switchesActive && !ablatePwInflightEmergency.Off(player, AblationIds.PwInflightEmergency);"));
			Assert.That(bb, Does.Contain("public bool PwInflightAdmitArmed => Info.PwInflightAdmit && switchesActive && !ablatePwInflightAdmit.Off(player, AblationIds.PwInflightAdmit);"));
			Assert.That(bb, Does.Contain("InFlightPower += producingPower;"));
			Assert.That(bb, Does.Contain("ExcessPower += producingPower;"), "the existing forecast is unchanged");
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsThemAndTheRowsAreRegisteredAndOffByDefault()
		{
			var generic = PlayerNode("BaseBuilderBotModuleCA@generic");
			var classic = PlayerNode("BaseBuilderBotModuleCA@classic");
			foreach (var key in new[] { "PwInflightEmergency", "PwInflightAdmit" })
			{
				Assert.That(FieldOrNull(generic, key), Is.EqualTo("true"), key);
				Assert.That(FieldOrNull(classic, key), Is.Null, key);
			}

			Assert.That(AblationIds.PwInflightEmergency, Is.EqualTo("pw_inflight_emergency"));
			Assert.That(AblationIds.PwInflightAdmit, Is.EqualTo("pw_inflight_admit"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.PwInflightEmergency));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.PwInflightAdmit));

			var info = new BaseBuilderBotModuleCAInfo();
			Assert.That(info.PwInflightEmergency, Is.False);
			Assert.That(info.PwInflightAdmit, Is.False);
		}
	}
}
