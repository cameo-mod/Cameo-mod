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
	// FOG-AIR-01 (Opus 11-26, Haiku FOG_RULING_VERIFY_HK_01): the @generic AA sizing counts enemy aircraft from the OBSERVED composition only. Under ObservedAirThreats a missing provider or a failed
	// TryGetEnemyComposition is 0 (nothing seen), never the omniscient world count; the world count is reachable only while ObservedThreats is off (classic, the switch off, or the row off).
	[TestFixture]
	public sealed class FogAirTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string CountBody()
		{
			var unit = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			var start = unit.IndexOf("\t\tint EnemyAirThreatCount()\n", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0), "EnemyAirThreatCount exists");
			var end = unit.IndexOf("\t\tList<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)", start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start));
			return unit.Substring(start, end - start);
		}

		[Test]
		public void AFailedProviderReadUnderTheObservedSwitchReturnsZeroAndNeverReachesTheWorldScan()
		{
			var body = CountBody();

			// the whole observed path is inside one if (ObservedThreats) block: provider read, then return 0
			Assert.That(body, Does.Contain("\t\t\tif (ObservedThreats)\n\t\t\t{\n\t\t\t\tif (compositionProvider != null && compositionProvider.TryGetEnemyComposition(out var valueByType))\n\t\t\t\t\treturn AirLimits.EstimateObservedThreatCount(valueByType, Info.AirThreatUnits,\n"));
			Assert.That(body, Does.Contain("\t\t\t\treturn 0;\n\t\t\t}\n\n\t\t\treturn player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && Info.AirThreatUnits.Contains(a.Info.Name));\n"),
				"a failed or missing provider is 0; the world scan is reached only after the observed block, i.e. while ObservedThreats is off");

			// the old form (a failed provider fell through to the world count) is gone
			Assert.That(body, Does.Not.Contain("if (ObservedThreats && compositionProvider != null"));

			// exactly one world scan, after the observed block
			Assert.That(body.Split(new[] { "World.Actors" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));
			Assert.That(body.IndexOf("return 0;", StringComparison.Ordinal), Is.LessThan(body.IndexOf("World.Actors", StringComparison.Ordinal)));
		}

		[Test]
		public void TheWorldCountIsReachableOnlyWhileTheObservedSwitchIsOffSoClassicIsUnchanged()
		{
			var unit = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");

			// the legacy scan line is byte-for-byte what it was
			Assert.That(unit, Does.Contain("return player.World.Actors.Count(a => a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && Info.AirThreatUnits.Contains(a.Info.Name));"));

			// ObservedThreats: code default false, switch condition, and the ablation row (off = the legacy scan again)
			Assert.That(unit, Does.Contain("public readonly bool ObservedAirThreats = false;"));
			Assert.That(unit, Does.Contain("\t\tbool ObservedThreats => Info.ObservedAirThreats && switchesActive && !ablateFogAir.Off(player, AblationIds.FogAirObserved);\n"));
			Assert.That(unit, Does.Contain("AblationLatch ablateFogAir;"));

			// the only branch on ObservedThreats is the air threat count's
			Assert.That(unit.Split(new[] { "(ObservedThreats" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));		}

		[Test]
		public void TheYamlArmsGenericOnlyAndTheRowIsRegistered()
		{
			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value;
			string Field(string module) => player.Nodes.Single(n => n.Key == module).Value.Nodes.SingleOrDefault(n => n.Key == "ObservedAirThreats")?.Value.Value?.Trim();
			Assert.That(Field("UnitBuilderBotModuleCA@generic"), Is.EqualTo("true"), "true in @generic");
			Assert.That(Field("UnitBuilderBotModuleCA@classic"), Is.Null, "classic stays as before (the line is a comment there)");

			Assert.That(AblationIds.FogAirObserved, Is.EqualTo("fog_air_observed"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("fog_air_observed"));
		}
	}
}
