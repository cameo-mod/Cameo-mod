#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;
using Candidate = OpenRA.Mods.Cameo.Traits.BotModules.BotRoleSetsInfo.RoleCandidate;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotRoleSetsTest
	{
		static readonly Dictionary<string, string[]> None = [];

		static Candidate Actor(string name, string[] traits, string[] roles = null, bool buildable = true) =>
			new(name, traits.ToHashSet(), (roles ?? []).ToFrozenSet(), buildable);

		static Dictionary<string, HashSet<string>> Resolve(IEnumerable<Candidate> actors,
			Dictionary<string, string[]> has, Dictionary<string, string[]> not = null,
			Dictionary<string, string[]> exclude = null, bool onlyBuildable = true) =>
			BotRoleSetsInfo.ResolveMembers(actors, has, not ?? None, exclude ?? None, onlyBuildable);

		[Test]
		public void DerivationNeedsEveryHasTraitAndNoNotTrait()
		{
			var actors = new[]
			{
				Actor("harv", ["Harvester", "Mobile"]),
				Actor("slave", ["Harvester", "Infantry"]),
				Actor("tank", ["Mobile"]),
			};

			var m = Resolve(actors, new() { ["harvester"] = ["Harvester"] }, new() { ["harvester"] = ["Infantry"] });

			Assert.That(m["harvester"], Is.EquivalentTo(new[] { "harv" }));
		}

		[Test]
		public void ExcludeBlocksDerivationButNotAnExplicitRole()
		{
			var actors = new[]
			{
				Actor("a", ["Harvester"]),
				Actor("b", ["Harvester"], roles: ["harvester"]),
			};

			var m = Resolve(actors, new() { ["harvester"] = ["Harvester"] },
				exclude: new() { ["harvester"] = ["a", "b"] });

			Assert.That(m["harvester"], Is.EquivalentTo(new[] { "b" }));
		}

		[Test]
		public void UnbuildableActorsAreNotDerivedButExplicitRolesStillCount()
		{
			var actors = new[]
			{
				Actor("husk", ["Harvester"], buildable: false),
				Actor("scripted", ["Mobile"], roles: ["guerrilla"], buildable: false),
			};

			var m = Resolve(actors, new() { ["harvester"] = ["Harvester"] });

			Assert.That(m.ContainsKey("harvester"), Is.False);
			Assert.That(m["guerrilla"], Is.EquivalentTo(new[] { "scripted" }));

			var all = Resolve(actors, new() { ["harvester"] = ["Harvester"] }, onlyBuildable: false);
			Assert.That(all["harvester"], Is.EquivalentTo(new[] { "husk" }));
		}

		[Test]
		public void UnionKeepsTheFieldsOwnSetType()
		{
			var current = new[] { "a" };
			var added = new[] { "b" };

			Assert.That(BotRoleSetsInfo.Union(typeof(FrozenSet<string>), current, added), Is.InstanceOf<FrozenSet<string>>());
			Assert.That(BotRoleSetsInfo.Union(typeof(HashSet<string>), current, added), Is.InstanceOf<HashSet<string>>());
			Assert.That(BotRoleSetsInfo.Union(typeof(ImmutableHashSet<string>), current, added), Is.InstanceOf<ImmutableHashSet<string>>());
			Assert.That((IEnumerable<string>)BotRoleSetsInfo.Union(typeof(FrozenSet<string>), current, added),
				Is.EquivalentTo(new[] { "a", "b" }));
		}

		[Test]
		public void UnionRejectsAFieldThatIsNotAStringSet()
		{
			Assert.Throws<YamlException>(() => BotRoleSetsInfo.Union(typeof(List<string>), ["a"], ["b"]));
		}
	}
}
