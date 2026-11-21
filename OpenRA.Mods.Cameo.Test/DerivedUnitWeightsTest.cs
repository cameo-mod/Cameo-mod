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

using System;
using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class DerivedUnitWeightsTest
	{
		// Minimal IBotUnitRoles: a unit counts toward its stored roles through the real
		// PrimaryRole precedence, exactly like the shipping BotUnitRoles provider.
		sealed class FakeRoles : IBotUnitRoles
		{
			readonly Dictionary<string, HashSet<string>> actorRoles = new(StringComparer.Ordinal);

			public FakeRoles(params (string Actor, string Role)[] entries)
			{
				var members = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
				foreach (var (actor, role) in entries)
				{
					if (!actorRoles.TryGetValue(actor, out var set))
						actorRoles[actor] = set = new HashSet<string>(StringComparer.Ordinal);
					set.Add(role);

					if (!members.TryGetValue(role, out var m))
						members[role] = m = new HashSet<string>(StringComparer.Ordinal);
					m.Add(actor);
				}

				RoleMembers = members;
			}

			public IReadOnlyDictionary<string, HashSet<string>> RoleMembers { get; }
			public IReadOnlyDictionary<string, HashSet<string>> ActorRoles => actorRoles;
			public string PrimaryRoleOf(string actorName) =>
				actorRoles.TryGetValue(actorName, out var roles) ? BotUnitRoles.PrimaryRole(roles) : null;
		}

		static BotUnitProfile Profile(params BotWeaponProfile[] weapons) =>
			new("unit", 0, 0, null, 0, false, false,
				new BitSet<TargetableType>(), weapons);

		static BotWeaponProfile Weapon(double damagePerTick, Dictionary<string, int> versus) =>
			new(damagePerTick, new WDist(1024), new BitSet<TargetableType>(), new BitSet<TargetableType>(), versus);

		[Test]
		public void RoleShareSplitsByRelativeStrength()
		{
			// PROD-IDLE_PLAN_05, RoleMixUnnamedTotalPct U = 20: scout is the only unnamed buildable role,
			// so it takes all of U; frontline (the only named role) keeps all of 100 - U = 80, split
			// 1:3 by strength around mean 20: tank 80 x 10/20, heavy 80 x 30/20.
			var yaml = new Dictionary<string, int> { { "tank", 1 }, { "heavy", 1 }, { "scout", 1 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline), ("heavy", BotUnitRole.Frontline), ("scout", BotUnitRole.Scout));
			var strength = new Dictionary<string, double> { { "tank", 10 }, { "heavy", 30 }, { "scout", 8 } };

			var table = DerivedUnitWeights.Derive(yaml, mix, 20, roles, n => strength[n]);

			Assert.That(table["tank"], Is.EqualTo(40), "80 x 10/20");
			Assert.That(table["heavy"], Is.EqualTo(120), "80 x 30/20");

			// Scout is unnamed: U / |N| = 20 / 1, alone in its role -> 20 x 8/8.
			Assert.That(table["scout"], Is.EqualTo(20));
		}

		[Test]
		public void UnroledUnitsKeepTheirYamlWeightVerbatim()
		{
			// The yaml rows stay the membership gate AND the override channel: units the
			// provider cannot place in a combat role keep their hand-written share.
			var yaml = new Dictionary<string, int> { { "tank", 1 }, { "harvesterish", 7 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 30 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline));

			var table = DerivedUnitWeights.Derive(yaml, mix, 5, roles, _ => 4.0);

			Assert.That(table["tank"], Is.EqualTo(30), "sole frontline member gets the whole share");
			Assert.That(table["harvesterish"], Is.EqualTo(7), "unroled row untouched");
			Assert.That(table.Keys, Is.EquivalentTo(yaml.Keys), "same key set: gate preserved");
		}

		[Test]
		public void DerivedWeightNeverDropsBelowOne()
		{
			// A weak member of a floor-share role rounds to 0 -> the max(1, ...) floor binds.
			var yaml = new Dictionary<string, int> { { "weak", 3 }, { "strong", 3 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Skirmisher, 1 } };
			var roles = new FakeRoles(("weak", BotUnitRole.Skirmisher), ("strong", BotUnitRole.Skirmisher));
			var strength = new Dictionary<string, double> { { "weak", 1 }, { "strong", 99 } };

			var table = DerivedUnitWeights.Derive(yaml, mix, 5, roles, n => strength[n]);

			Assert.That(table["weak"], Is.EqualTo(1));
			Assert.That(table["strong"], Is.EqualTo(2), "1 x 99/50 = 1.98 rounds to 2");
		}

		[Test]
		public void SelectIsByteIdenticalWhenOff()
		{
			// Flag off returns the yaml dictionary itself — the pre-derivation path.
			var yaml = new Dictionary<string, int> { { "tank", 1 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline));

			var table = new DerivedUnitWeights().Select(false, null, yaml, mix, 5, roles, _ => 9.0);

			Assert.That(table, Is.SameAs(yaml));
		}

		[Test]
		public void SelectFallsBackToYamlWithoutMixOrProvider()
		{
			var yaml = new Dictionary<string, int> { { "tank", 1 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline));
			var weights = new DerivedUnitWeights();

			Assert.That(weights.Select(true, null, yaml, null, 5, roles, _ => 9.0), Is.SameAs(yaml), "no RoleMix");
			Assert.That(weights.Select(true, null, yaml, new Dictionary<string, int>(), 5, roles, _ => 9.0), Is.SameAs(yaml), "empty RoleMix");
			Assert.That(weights.Select(true, null, yaml, mix, 5, null, _ => 9.0), Is.SameAs(yaml), "no provider");
		}

		[Test]
		public void CompositionWinsOverDerived()
		{
			var yaml = new Dictionary<string, int> { { "tank", 1 } };
			var composition = new Dictionary<string, int> { { "tank", 9 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline));
			var weights = new DerivedUnitWeights();

			Assert.That(weights.Select(true, composition, yaml, mix, 5, roles, _ => 9.0), Is.SameAs(composition));
			Assert.That(weights.Select(false, composition, yaml, mix, 5, roles, _ => 9.0), Is.SameAs(composition),
				"a composition wins flag-off too — the pre-derivation behaviour");
		}

		[Test]
		public void SelectCachesOnTheMixReference()
		{
			var yaml = new Dictionary<string, int> { { "tank", 1 } };
			var mixA = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var mixB = new Dictionary<string, int> { { BotUnitRole.Frontline, 80 } };
			var roles = new FakeRoles(("tank", BotUnitRole.Frontline));
			var weights = new DerivedUnitWeights();

			var first = weights.Select(true, null, yaml, mixA, 5, roles, _ => 9.0);
			Assert.That(weights.Select(true, null, yaml, mixA, 5, roles, _ => 9999.0), Is.SameAs(first),
				"same mix dictionary serves the cached table");

			// A personality switch swaps the manager — hence the RoleMix reference — and rebuilds.
			var second = weights.Select(true, null, yaml, mixB, 5, roles, _ => 9.0);
			Assert.That(second, Is.Not.SameAs(first));
			Assert.That(second["tank"], Is.EqualTo(80));
		}

		[Test]
		public void StrengthIsBestVersusWeightedDamage()
		{
			// mean(Versus 50,150) = 100 -> 2.0 x 100/100 = 2; mean(Versus 200) -> 9 x 2 = 18 wins.
			var profile = Profile(
				Weapon(2.0, new Dictionary<string, int> { { "a", 50 }, { "b", 150 } }),
				Weapon(9.0, new Dictionary<string, int> { { "a", 200 } }));
			Assert.That(DerivedUnitWeights.Strength(profile), Is.EqualTo(18.0));

			// No Versus overrides reads 100 — the DamageWarhead default.
			Assert.That(DerivedUnitWeights.Strength(Profile(Weapon(3.0, new Dictionary<string, int>()))), Is.EqualTo(3.0));
		}

		[Test]
		public void StrengthFloorsAtOne()
		{
			Assert.That(DerivedUnitWeights.Strength(Profile()), Is.EqualTo(1.0), "weaponless");
			Assert.That(DerivedUnitWeights.Strength(null), Is.EqualTo(1.0), "missing profile");
			Assert.That(DerivedUnitWeights.Strength(Profile(Weapon(0.2, new Dictionary<string, int> { { "a", 50 } }))),
				Is.EqualTo(1.0), "0.2 x 0.5 = 0.1 floors to 1");
		}

		// PROD-IDLE_PLAN_05 tests. Roster: tank=frontline, rifle=anti_infantry, aa=anti_air,
		// scout=scout, truck=transport, lifter=air_transport; "ghost" is an artillery unit that is
		// NOT in the yaml gate, so artillery has a member but none inside the membership gate.
		static FakeRoles Roster() => new(
			("tank", BotUnitRole.Frontline), ("rifle", BotUnitRole.AntiInfantry), ("aa", BotUnitRole.AntiAir),
			("scout", BotUnitRole.Scout), ("truck", BotUnitRole.Transport), ("lifter", BotUnitRole.AirTransport),
			("ghost", BotUnitRole.Artillery));

		static Dictionary<string, int> Gate(params string[] names)
		{
			var gate = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var n in names)
				gate[n] = 1;
			return gate;
		}

		[Test]
		public void UnnamedSetExcludesNamedTransportsAndMemberlessRoles()
		{
			var gate = Gate("tank", "rifle", "aa", "scout", "truck", "lifter");
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 60 }, { BotUnitRole.AntiInfantry, 20 } };

			var shares = DerivedUnitWeights.RoleShares(mix, 15, Roster(), gate.ContainsKey);

			Assert.That(shares.Keys, Is.EquivalentTo(new[]
			{
				BotUnitRole.Frontline, BotUnitRole.AntiInfantry, BotUnitRole.AntiAir, BotUnitRole.Scout,
			}), "transport, air_transport and the memberless artillery role get no share");
		}

		[Test]
		public void UnnamedRolesSplitUEvenlyAndNamedKeepTheirRatioInTheRest()
		{
			var gate = Gate("tank", "rifle", "aa", "scout", "truck", "lifter");
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 60 }, { BotUnitRole.AntiInfantry, 20 } };

			var shares = DerivedUnitWeights.RoleShares(mix, 15, Roster(), gate.ContainsKey);

			Assert.That(shares[BotUnitRole.AntiAir], Is.EqualTo(7.5).Within(1e-9), "15 / 2");
			Assert.That(shares[BotUnitRole.Scout], Is.EqualTo(7.5).Within(1e-9));
			Assert.That(shares[BotUnitRole.Frontline], Is.EqualTo(63.75).Within(1e-9), "85 x 60 / 80");
			Assert.That(shares[BotUnitRole.AntiInfantry], Is.EqualTo(21.25).Within(1e-9), "85 x 20 / 80");
			Assert.That(shares[BotUnitRole.Frontline] / shares[BotUnitRole.AntiInfantry], Is.EqualTo(3.0).Within(1e-9),
				"relative weights equal the configured ratio");
			double total = 0;
			foreach (var v in shares.Values)
				total += v;
			Assert.That(total, Is.EqualTo(100.0).Within(1e-9), "unnamed U plus named 100 - U");
		}

		[Test]
		public void EmptyUnnamedSetLeavesNamedEntriesUnscaled()
		{
			// The mix names every buildable role: nowhere to put the reserve.
			var gate = Gate("tank", "rifle");
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 60 }, { BotUnitRole.AntiInfantry, 20 } };

			var shares = DerivedUnitWeights.RoleShares(mix, 15, Roster(), gate.ContainsKey);

			Assert.That(shares[BotUnitRole.Frontline], Is.EqualTo(60).Within(1e-9));
			Assert.That(shares[BotUnitRole.AntiInfantry], Is.EqualTo(20).Within(1e-9));
			Assert.That(shares.Count, Is.EqualTo(2));
		}

		[Test]
		public void ZeroSumOrEmptyMixGivesNoSharesAndNoDerivation()
		{
			var gate = Gate("tank", "scout");
			Assert.That(DerivedUnitWeights.RoleShares(new Dictionary<string, int>(), 15, Roster(), gate.ContainsKey), Is.Empty);
			Assert.That(DerivedUnitWeights.RoleShares(null, 15, Roster(), gate.ContainsKey), Is.Empty);
			Assert.That(DerivedUnitWeights.RoleShares(
				new Dictionary<string, int> { { BotUnitRole.Frontline, 0 } }, 15, Roster(), gate.ContainsKey), Is.Empty);
			Assert.That(DerivedUnitWeights.RoleShares(
				new Dictionary<string, int> { { "guerrilla", 50 } }, 15, Roster(), gate.ContainsKey), Is.Empty,
				"keys outside the combat taxonomy stay ignored");

			// Derive leaves the yaml rows verbatim when there is no usable mix.
			var yaml = new Dictionary<string, int> { { "tank", 3 }, { "scout", 4 } };
			var table = DerivedUnitWeights.Derive(yaml, new Dictionary<string, int> { { BotUnitRole.Frontline, 0 } },
				15, Roster(), _ => 5.0);
			Assert.That(table["tank"], Is.EqualTo(3));
			Assert.That(table["scout"], Is.EqualTo(4));
		}

		[Test]
		public void UnnamedTransportKeepsItsYamlWeightAndANamedOneIsServed()
		{
			var yaml = new Dictionary<string, int> { { "tank", 1 }, { "scout", 1 }, { "truck", 9 }, { "lifter", 7 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 60 } };

			var table = DerivedUnitWeights.Derive(yaml, mix, 10, Roster(), _ => 4.0);
			Assert.That(table["truck"], Is.EqualTo(9), "unnamed transport: yaml weight verbatim");
			Assert.That(table["lifter"], Is.EqualTo(7), "unnamed air_transport: yaml weight verbatim");

			var named = new Dictionary<string, int> { { BotUnitRole.Frontline, 50 }, { BotUnitRole.Transport, 50 } };
			var shares = DerivedUnitWeights.RoleShares(named, 10, Roster(), yaml.ContainsKey);
			Assert.That(shares[BotUnitRole.Transport], Is.EqualTo(45).Within(1e-9), "named transport is an ordinary named role");
			Assert.That(shares[BotUnitRole.Scout], Is.EqualTo(10).Within(1e-9));
			Assert.That(shares.ContainsKey(BotUnitRole.AirTransport), Is.False);
		}

		[Test]
		public void DerivedTableUsesTheSameSharesAsTheDeficitChooser()
		{
			// One role per candidate, so a derived weight is its role's share (strength / mean = 1).
			var yaml = Gate("tank", "rifle", "aa", "scout");
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 60 }, { BotUnitRole.AntiInfantry, 20 } };
			var roles = Roster();

			var shares = DerivedUnitWeights.RoleShares(mix, 15, roles, yaml.ContainsKey);
			var table = DerivedUnitWeights.Derive(yaml, mix, 15, roles, _ => 3.0);

			Assert.That(table["tank"], Is.EqualTo((int)Math.Round(shares[BotUnitRole.Frontline], MidpointRounding.AwayFromZero)));
			Assert.That(table["rifle"], Is.EqualTo((int)Math.Round(shares[BotUnitRole.AntiInfantry], MidpointRounding.AwayFromZero)));
			Assert.That(table["aa"], Is.EqualTo((int)Math.Round(shares[BotUnitRole.AntiAir], MidpointRounding.AwayFromZero)));
			Assert.That(table["scout"], Is.EqualTo((int)Math.Round(shares[BotUnitRole.Scout], MidpointRounding.AwayFromZero)));
		}

		[Test]
		public void ChangingTheUnnamedTotalInvalidatesTheDerivedCache()
		{
			var yaml = new Dictionary<string, int> { { "tank", 1 }, { "scout", 1 } };
			var mix = new Dictionary<string, int> { { BotUnitRole.Frontline, 40 } };
			var roles = Roster();
			var weights = new DerivedUnitWeights();

			var u10 = weights.Select(true, null, yaml, mix, 10, roles, _ => 9.0);
			Assert.That(u10["tank"], Is.EqualTo(90));
			Assert.That(weights.Select(true, null, yaml, mix, 10, roles, _ => 9999.0), Is.SameAs(u10), "same (mix, U): cached");

			var u30 = weights.Select(true, null, yaml, mix, 30, roles, _ => 9.0);
			Assert.That(u30, Is.Not.SameAs(u10));
			Assert.That(u30["tank"], Is.EqualTo(70));
			Assert.That(u30["scout"], Is.EqualTo(30));
		}
	}
}
