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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// BUILD-ALL-B (switch bb_no_foundation, user ruling 2026-11-25): no concrete is queued by the base builder. A terrain foundation is matched by its
	// traits (a building without health that removes itself unconditionally), never by a list; the D2k concrete actors are checked to match that shape.
	[TestFixture]
	public sealed class FoundationExclusionTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		// The engine's KillsSelfInfo is internal to OpenRA.Mods.Common; the test makes one by reflection (RequiresCondition stays null).
		static TraitInfo KillsSelf() => (TraitInfo)Activator.CreateInstance(typeof(BuildingInfo).Assembly.GetType("OpenRA.Mods.Common.Traits.KillsSelfInfo", true), true);

		static readonly ActorInfo Slab = new("concreteabuilding", new BuildingInfo(), KillsSelf());
		static readonly ActorInfo Wall = new("wall", new BuildingInfo(), new HealthInfo());
		static readonly ActorInfo Factory = new("factory", new BuildingInfo(), new HealthInfo());
		static readonly ActorInfo Infantry = new("e1", new HealthInfo(), KillsSelf());

		[Test]
		public void AFoundationIsABuildingWithoutHealthThatRemovesItself()
		{
			Assert.That(FoundationExclusion.IsFoundation(true, false, true), Is.True);
			Assert.That(FoundationExclusion.IsFoundation(true, true, true), Is.False, "health = a structure that stands");
			Assert.That(FoundationExclusion.IsFoundation(true, false, false), Is.False, "stays = not a foundation");
			Assert.That(FoundationExclusion.IsFoundation(false, false, true), Is.False, "not a building");

			Assert.That(FoundationExclusion.IsFoundation(Slab), Is.True);
			Assert.That(FoundationExclusion.IsFoundation(Wall), Is.False);
			Assert.That(FoundationExclusion.IsFoundation(Factory), Is.False);
			Assert.That(FoundationExclusion.IsFoundation(Infantry), Is.False);
			Assert.That(FoundationExclusion.IsFoundation(null), Is.False);
		}

		[Test]
		public void NoConcreteIsQueued()
		{
			var cache = new Dictionary<string, bool>();
			var buildable = new List<ActorInfo> { Factory, Slab, Wall };
			Assert.That(FoundationExclusion.Without(buildable, cache).Select(a => a.Name), Is.EqualTo(new[] { "factory", "wall" }), "queue order kept");
			Assert.That(cache["concreteabuilding"], Is.True);

			var clean = new List<ActorInfo> { Factory, Wall };
			Assert.That(FoundationExclusion.Without(clean, cache), Is.SameAs(clean), "nothing removed = the same list");
			Assert.That(FoundationExclusion.Without(new List<ActorInfo> { Slab }, cache), Is.Empty);
		}

		[Test]
		public void TheD2kConcreteHasTheFoundationShape()
		{
			var yaml = RepoFile("mods/cameo/ContentPacks/D2k/Shared/yaml/templates.yaml");
			var start = yaml.IndexOf("\n^Concrete:\n", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThan(0));
			var end = yaml.IndexOf("\n^", start + 2, StringComparison.Ordinal);
			var block = end < 0 ? yaml[start..] : yaml[start..end];
			Assert.That(block, Does.Contain("\n\tKillsSelf:\n"));
			Assert.That(block, Does.Contain("\n\tD2kBuilding:\n"));
			Assert.That(block, Does.Not.Contain("Health:"));
			Assert.That(block, Does.Not.Contain("RequiresCondition"));

			var buildings = RepoFile("mods/cameo/ContentPacks/D2k/Shared/yaml/buildings.yaml");
			foreach (var slab in new[] { "concreteabuilding", "concreteadefense", "concretebbuilding", "concretebdefense" })
			{
				var at = buildings.IndexOf("\n" + slab + ":\n", StringComparison.Ordinal);
				if (at < 0 && buildings.StartsWith(slab + ":\n", StringComparison.Ordinal))
					at = 0;

				Assert.That(at, Is.GreaterThanOrEqualTo(0), slab);
				var next = buildings.IndexOf("\n\n", at + 1, StringComparison.Ordinal);
				var actor = next < 0 ? buildings[at..] : buildings[at..next];
				Assert.That(actor, Does.Contain("Inherits: ^Concrete"), slab);
				Assert.That(actor, Does.Not.Contain("Health:"), slab);
			}
		}

		[Test]
		public void BothCandidateSourcesDropTheFoundations()
		{
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs"),
				Does.Contain("if (baseBuilder.NoFoundationArmed)\n\t\t\t\tbuildable = FoundationExclusion.Without(buildable, baseBuilder.FoundationTypes);"), "the staged planner");
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.cs"),
				Does.Contain("if (baseBuilder.NoFoundationArmed)\n\t\t\t\tbuildableThings = FoundationExclusion.Without(buildableThings.ToList(), baseBuilder.FoundationTypes);"), "the old ladder (UnlistedBuildingsOnce)");
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegistered()
		{
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbNoFoundation"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbNoFoundation"), Is.Null);
			Assert.That(AblationIds.BbNoFoundation, Is.EqualTo("bb_no_foundation"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbNoFoundation));
			Assert.That(new BaseBuilderBotModuleCAInfo().BbNoFoundation, Is.False);
		}
	}
}
