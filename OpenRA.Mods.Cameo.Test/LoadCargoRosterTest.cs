#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// AR-9 (ORDERS_2026-10-04b): LoadCargoBotModuleAS resolves empty rosters from the paired classic
	// LoadCargoBotModuleInfo block (same @InstanceName) so the yaml carries each list exactly once.
	// The resolution must never return null — the tick path calls Contains/ContainsKey unguarded.
	[TestFixture]
	public sealed class LoadCargoRosterTest
	{
		[Test]
		public void PopulatedOwnTransportsAlwaysWin()
		{
			var own = new Dictionary<string, LoadRequirement> { ["miss"] = LoadRequirement.All };
			var peer = new Dictionary<string, LoadRequirement> { ["fcom"] = LoadRequirement.IdleUnit };
			var resolved = LoadCargoBotModuleAS.ResolveRoster(own, peer);
			Assert.That(resolved, Is.SameAs(own));
			Assert.That(resolved, Does.Not.ContainKey("fcom"), "a populated own table replaces the peer's entirely");
		}

		[Test]
		public void EmptyOwnTransportsFallBackToPeer()
		{
			var own = new Dictionary<string, LoadRequirement>();
			var peer = new Dictionary<string, LoadRequirement> { ["miss"] = LoadRequirement.All };
			Assert.That(LoadCargoBotModuleAS.ResolveRoster(own, peer), Is.SameAs(peer));
		}

		[Test]
		public void NullOwnTransportsFallBackToPeer()
		{
			var peer = new Dictionary<string, LoadRequirement> { ["ramiss"] = LoadRequirement.IdleUnit };
			Assert.That(LoadCargoBotModuleAS.ResolveRoster(null, peer), Is.SameAs(peer));
		}

		[Test]
		public void MissingPeerLeavesEmptyTransportsNotNull()
		{
			var resolved = LoadCargoBotModuleAS.ResolveRoster((Dictionary<string, LoadRequirement>)null, null);
			Assert.That(resolved, Is.Not.Null, "ContainsKey is called unguarded on the resolved table");
			Assert.That(resolved, Is.Empty);
		}

		[Test]
		public void EmptyPeerIsNotPreferredOverOwn()
		{
			var own = new Dictionary<string, LoadRequirement> { ["fcom"] = LoadRequirement.All };
			var resolved = LoadCargoBotModuleAS.ResolveRoster(own, new Dictionary<string, LoadRequirement>());
			Assert.That(resolved, Is.SameAs(own));
		}

		[Test]
		public void PassengerTypesFollowTheSameRules()
		{
			var own = new HashSet<string> { "light_inf" };
			var peer = new HashSet<string> { "e1" };
			Assert.That(LoadCargoBotModuleAS.ResolveRoster(own, peer), Is.SameAs(own));
			Assert.That(LoadCargoBotModuleAS.ResolveRoster(new HashSet<string>(), peer), Is.SameAs(peer));
			var resolved = LoadCargoBotModuleAS.ResolveRoster(null, (HashSet<string>)null);
			Assert.That(resolved, Is.Not.Null.And.Empty);
		}
	}
}
