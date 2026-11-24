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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace OpenRA.Mods.Cameo.Test
{
	// TECH-ALL-01 (Opus 11-24): MiniYaml is last-writer-wins on a scalar, so a per-faction pack line `TechChainTypes: a, b` on the shared BaseBuilderBotModuleCA@generic kept only ONE
	// faction's tech chain (radar + tech centre) exempt from the army-share vote: the Ixian T1 stall (designer-tech-progression-never-blocked). One central list; the packs carry no scalar.
	// Also: the genericbot SquadManager personality blocks carried a zerg-only ConstructionYardTypes, so every other faction had no yard for rally/defend centres.
	[TestFixture]
	public sealed class TechAll01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static System.Collections.Immutable.ImmutableArray<MiniYamlNode> Player() => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;

		static string[] List(System.Collections.Immutable.ImmutableArray<MiniYamlNode> nodes, string key) =>
			nodes.Single(n => n.Key == key).Value.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		// each faction's (radar, tech centre) pair as the packs declared them before TECH-ALL-01
		static readonly string[][] Pairs =
		[
			["td_gdi_communicationscenter", "td_gdi_advancedcommunicationscenter"],
			["td_nod_communicationscenter", "td_nod_templeofnod"],
			["ra1_allies_radardome", "ra1_allies_techcenter"],
			["ra1_soviets_radardome", "ra1_soviets_techcenter"],
			["japan_japaneseradararray", "japan_japanesetechcenter"],
			["ixian_outpost", "ixian_ixresearchcenter"],
			["atreides_outpost", "atreides_ixresearchcenter"],
			["corrino_outpost", "corrino_ixresearchcenter"],
			["harkonnen_outpost", "harkonnen_ixresearchcenter"],
			["ordos_outpost", "ordos_ixresearchcenter"],
			["ra2_allies_airforcecommandhq", "ra2_allies_battlelab"],
			["ra2_soviets_radar", "ra2_soviets_battlelab"],
			["yuri_psychicsensor", "yuri_battlelab"],
			["asianalliance_radar", "asianalliance_battlelab"],
			["steelconsortium_consortiumradar", "steelconsortium_consortiumbattlelab"],
			["futuretech_transmissioncenter", "futuretech_battlelab"],
			["naxis_radar", "naxis_techcenter"],
			["schwarzermond_radar", "schwarzermond_techcenter"],
			["latinsyndicate_latinempradar", "latinsyndicate_spycenter"],
			["tkm_observationvan", "tkm_techcenter"],
			["protoss_cyberneticscore", "protoss_templararchives"],
			["terran_starport", "terran_sciencefacility"],
			["cabal_radar", "cabal_techcenter"],
			["forgotten_radar", "forgotten_church"],
			["ts_gdi_radar", "ts_gdi_techcenter"],
			["ts_nod_radar", "ts_nod_techcenter"],
		];

		[Test]
		public void TheCentralTechChainListHoldsEveryFactionsPair()
		{
			var generic = Player().Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			var list = List(generic, "TechChainTypes");
			foreach (var pair in Pairs)
				foreach (var name in pair)
					Assert.That(list, Does.Contain(name), name);

			// the six the user named, explicitly
			foreach (var name in new[] { "td_gdi_communicationscenter", "td_nod_communicationscenter", "ra1_allies_radardome", "ra1_soviets_radardome",
				"japan_japaneseradararray", "ixian_outpost", "ixian_ixresearchcenter" })
				Assert.That(list, Does.Contain(name), name);

			Assert.That(list.Distinct().Count(), Is.EqualTo(list.Length), "no duplicates");
			Assert.That(list.Length, Is.EqualTo(Pairs.Sum(p => p.Length)));
		}

		[Test]
		public void NoPackSetsAScalarOnASharedTrait()
		{
			// A pack may add dictionary rows (they union); a scalar or list line at trait-key level is last-writer-wins across packs.
			var root = Path.Combine(AppContext.BaseDirectory, "source", "packs");
			Assert.That(Directory.Exists(root), Is.True);
			var files = Directory.GetFiles(root, "ai.yaml", SearchOption.AllDirectories);
			Assert.That(files.Length, Is.GreaterThan(20));
			var scalar = new Regex(@"^\t\t[A-Za-z][A-Za-z0-9_]*: *[^ #\r\n]", RegexOptions.Multiline);
			foreach (var file in files)
				Assert.That(scalar.IsMatch(File.ReadAllText(file)), Is.False, file);
		}

		[Test]
		public void TheClassicBaseBuilderIsUntouched()
		{
			var classic = Player().Single(n => n.Key == "BaseBuilderBotModuleCA@classic").Value.Nodes;
			Assert.That(classic.Any(n => n.Key == "TechChainTypes"), Is.False, "classic keeps no tech chain, as before");
			Assert.That(classic.Single(n => n.Key == "ConstructionYardTypes").Value.Value, Is.EqualTo("zerg_lair, zerg_hive"));
		}

		[Test]
		public void EveryGenericbotSquadPersonalityKnowsEveryFactionsYard()
		{
			var player = Player();
			var full = List(player.Single(n => n.Key == "SquadManagerBotModuleCA@classic").Value.Nodes, "ConstructionYardTypes");
			Assert.That(full, Does.Contain("td_gdi_constructionyard"));
			Assert.That(full, Does.Contain("ixian_constructionyard"));
			foreach (var key in new[] { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" })
			{
				var yards = List(player.Single(n => n.Key == "SquadManagerBotModuleCA@" + key).Value.Nodes, "ConstructionYardTypes");
				Assert.That(yards, Is.EqualTo(full), key);
			}
		}
	}
}
