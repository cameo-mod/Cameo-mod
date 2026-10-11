#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Test.TestFixtures;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Mods.Common.UpdateRules.Rules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// TRAIT-U0 pins for the trait-unification machinery (SPEC_2026-10-05_trait_unification):
	/// 1. mod.yaml keeps OpenRA.Mods.Cameo.Unified.dll first in Assemblies so a migrated
	///    plain name wins ObjectCreator.FindType over every suffixed sibling.
	/// 2. FindType resolves the earliest assembly in the list — exercised through the real
	///    ObjectCreator with the RenderSpritesInfo collision that exists between
	///    OpenRA.Mods.Cameo and OpenRA.Mods.Common today.
	/// 3. The MigrateUnifiedTraits emitter renames trait keys (keeping - and @instance),
	///    rewrites only Projectile:/Warhead@x: values on weapons, skips pending entries,
	///    and reports collisions as manual steps instead of merging silently.
	/// </summary>
	[TestFixture]
	public class UnifiedTraitMigrationTest
	{
		static MigrateUnifiedTraits.Alias Alias(string oldName, string newName, string kind, string state)
		{
			return new MigrateUnifiedTraits.Alias { Old = oldName, New = newName, Kind = kind, State = state };
		}

		static MiniYamlNodeBuilder Node(string key, string value, params MiniYamlNode[] children)
		{
			return new MiniYamlNodeBuilder(new MiniYamlNode(key, new MiniYaml(value, children.ToList())));
		}

		static ObjectCreator CreatorWith(params Assembly[] ordered)
		{
			var creator = Uninitialized.Of<ObjectCreator>();
			var pairs = ordered
				.SelectMany(a => a.GetNamespaces().Select(ns => (a, ns)))
				.ToArray();
			typeof(ObjectCreator)
				.GetField("assemblies", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(creator, pairs);
			return creator;
		}

		static string RepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
				if (File.Exists(Path.Combine(dir.FullName, "mods", "cameo", "mod.yaml")))
					return dir.FullName;

			Assert.Fail("UnifiedTraitMigrationTest: repo root (mods/cameo/mod.yaml) not found");
			return null;
		}

		[Test]
		public void ModYamlListsUnifiedAssemblyBeforeItsDonors()
		{
			var path = Path.Combine(RepoRoot(), "mods", "cameo", "mod.yaml");
			var assembliesNode = MiniYaml.FromStream(File.OpenRead(path), path)
				.First(n => n.Key == "Assemblies");
			var order = assembliesNode.Value.Value
				.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
				.ToList();

			var unified = order.IndexOf("OpenRA.Mods.Cameo.Unified.dll");
			Assert.That(unified, Is.GreaterThanOrEqualTo(0), "mod.yaml Assemblies has no Unified dll");
			foreach (var later in new[]
			{
				"OpenRA.Mods.AS.dll", "OpenRA.Mods.CA.dll", "OpenRA.Mods.Cameo.dll",
				"OpenRA.Mods.Cnc.dll", "OpenRA.Mods.D2k.dll", "OpenRA.Mods.Common.dll",
			})
				Assert.That(order.IndexOf(later), Is.GreaterThan(unified),
					$"{later} must come after OpenRA.Mods.Cameo.Unified.dll or migrated types lose FindType");
		}

		[Test]
		public void FindTypeResolvesTheEarliestAssembly()
		{
			var common = typeof(Common.Traits.Render.RenderSpritesInfo).Assembly;
			var cameo = typeof(Traits.Render.RenderSpritesInfo).Assembly;

			// mod.yaml order: Cameo before Common -> the Cameo subclass wins.
			var asLoaded = CreatorWith(cameo, common);
			Assert.That(asLoaded.FindType("RenderSpritesInfo"),
				Is.SameAs(typeof(Traits.Render.RenderSpritesInfo)));

			// Reversed order flips the winner - the contract is positional, not nominal.
			var reversed = CreatorWith(common, cameo);
			Assert.That(reversed.FindType("RenderSpritesInfo"),
				Is.SameAs(typeof(Common.Traits.Render.RenderSpritesInfo)));
		}

		[Test]
		public void RuleAndCommandAreDiscoverableThroughObjectCreator()
		{
			var creator = CreatorWith(typeof(MigrateUnifiedTraits).Assembly);
			Assert.That(creator.GetTypesImplementing<UpdateRule>(),
				Has.Member(typeof(MigrateUnifiedTraits)));
			Assert.That(creator.GetTypesImplementing<IUtilityCommand>().Select(t => t.Name),
				Has.Member("UpdateUnifiedTraitsCommand"));
		}

		[Test]
		public void TraitRenameKeepsInstanceSuffixAndRemovalPrefix()
		{
			var renames = new Dictionary<string, string> { ["OldTraitCA"] = "UnifiedTrait" };
			var actor = Node("^actor", null,
				new MiniYamlNode("OldTraitCA@first", new MiniYaml(null)),
				new MiniYamlNode("-OldTraitCA@second", new MiniYaml(null)),
				new MiniYamlNode("Unrelated", new MiniYaml("OldTraitCA")));

			var steps = MigrateUnifiedTraits.RenameMatchingChildren(actor, renames).ToList();
			Assert.That(steps, Is.Empty);
			Assert.That(actor.Value.Nodes.Select(n => n.Key),
				Is.EqualTo(new[] { "UnifiedTrait@first", "-UnifiedTrait@second", "Unrelated" }));

			// Values of other keys are never rewritten - the name is not a global string.
			Assert.That(actor.Value.Nodes[2].Value.Value, Is.EqualTo("OldTraitCA"));
		}

		[Test]
		public void PendingAliasesAreNotRewritten()
		{
			var rule = new MigrateUnifiedTraits(new[]
			{
				Alias("OldTraitCA", "UnifiedTrait", "trait", "pending"),
				Alias("GoneTraitCA", "UnifiedTrait2", "trait", "retired"),
			});

			Assert.That(rule.ActiveRenames("trait"), Is.Empty);
		}

		[Test]
		public void CollisionReportsManualStepAndKeepsNode()
		{
			var renames = new Dictionary<string, string> { ["OldTraitCA"] = "UnifiedTrait" };
			var actor = Node("^actor", null,
				new MiniYamlNode("OldTraitCA@a", new MiniYaml(null)),
				new MiniYamlNode("UnifiedTrait@a", new MiniYaml(null)));

			var steps = MigrateUnifiedTraits.RenameMatchingChildren(actor, renames).ToList();
			Assert.That(steps, Has.Count.EqualTo(1));
			Assert.That(steps[0], Does.Contain("OldTraitCA@a"));
			Assert.That(actor.Value.Nodes[0].Key, Is.EqualTo("OldTraitCA@a"));

			// A removal on the same destination instance is still a collision: renaming
			// OldTraitCA@a over -UnifiedTrait@a would cancel/re-add out of the author's order.
			var cancel = Node("^actor", null,
				new MiniYamlNode("-UnifiedTrait@a", new MiniYaml(null)),
				new MiniYamlNode("OldTraitCA@a", new MiniYaml(null)));
			steps = MigrateUnifiedTraits.RenameMatchingChildren(cancel, renames).ToList();
			Assert.That(steps, Has.Count.EqualTo(1));
			Assert.That(cancel.Value.Nodes[1].Key, Is.EqualTo("OldTraitCA@a"));
		}

		[Test]
		public void WeaponValuesRewriteOnlyProjectileAndWarhead()
		{
			var projectiles = new Dictionary<string, string> { ["MissileCA"] = "Missile" };
			var warheads = new Dictionary<string, string> { ["SpreadDamageWH"] = "SpreadDamage" };
			var weapon = Node("weapon", null,
				new MiniYamlNode("Projectile", new MiniYaml("MissileCA")),
				new MiniYamlNode("Warhead@main", new MiniYaml("SpreadDamageWH")),
				new MiniYamlNode("Report", new MiniYaml("MissileCA")));

			var steps = MigrateUnifiedTraits.RenameWeaponValues(weapon, projectiles, warheads).ToList();
			Assert.That(steps, Is.Empty);
			Assert.That(weapon.Value.Nodes[0].Value.Value, Is.EqualTo("Missile"));
			Assert.That(weapon.Value.Nodes[1].Value.Value, Is.EqualTo("SpreadDamage"));
			Assert.That(weapon.Value.Nodes[1].Key, Is.EqualTo("Warhead@main"));

			// A same-string field that is not a semantic slot is left alone.
			Assert.That(weapon.Value.Nodes[2].Value.Value, Is.EqualTo("MissileCA"));
		}

		[Test]
		public void RegistryLoaderReadsTheRealFile()
		{
			var path = Path.Combine(RepoRoot(), MigrateUnifiedTraits.RegistryRelativePath);
			Assert.That(File.Exists(path), Is.True, "trait_aliases.json missing");
			var aliases = MigrateUnifiedTraits.LoadRegistry(path);
			Assert.That(aliases, Has.Count.GreaterThan(0));
			Assert.That(aliases.Select(a => a.Old), Is.Unique);
			Assert.That(aliases.All(a => a.State is "pending" or "active" or "retired"), Is.True);
		}
	}
}
