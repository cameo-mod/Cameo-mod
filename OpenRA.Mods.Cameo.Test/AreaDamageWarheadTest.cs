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

using NUnit.Framework;
using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Mods.Cameo.Warheads;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class AreaDamageWarheadTest
	{
		[Test]
		public void PercentageDamageKeepsWideIntermediateUntilAfterBasisPointDivision()
		{
			Assert.That(
				TestAreaDamageWarhead.Apply(3750000, 10000, 30000, 200),
				Is.EqualTo(22500000));
		}

		[Test]
		public void PercentageDamageClampsResultsAboveInt32Range()
		{
			Assert.That(
				TestAreaDamageWarhead.Apply(int.MaxValue, 100, 200),
				Is.EqualTo(int.MaxValue));
		}

		[Test]
		public void PercentageHealingTruncatesTowardZero()
		{
			Assert.That(
				TestAreaDamageWarhead.Apply(101, 100, -1),
				Is.EqualTo(-1));
		}

		[Test]
		public void DirectActorImpactAddsFoldedPercentageExactlyOnce()
		{
			var warhead = new TestAreaDamageWarhead();
			FieldLoader.LoadFieldOrProperty(warhead, nameof(AreaDamageWarhead.PercentageScale), "10000");

			warhead.ApplyDirectActor();

			Assert.That(warhead.FlatCalls, Is.EqualTo(1));
			Assert.That(warhead.PercentageCalls, Is.EqualTo(1));
			Assert.That(warhead.CallOrder, Is.EqualTo(new[] { "flat", "percentage" }));
		}

		[Test]
		public void DirectActorImpactAtDefaultScaleOnlyAppliesMainHit()
		{
			var warhead = new TestAreaDamageWarhead();

			warhead.ApplyDirectActor();

			Assert.That(warhead.FlatCalls, Is.EqualTo(1));
			Assert.That(warhead.PercentageCalls, Is.Zero);
		}

		sealed class TestAreaDamageWarhead : AreaDamageWarhead
		{
			public int FlatCalls { get; private set; }
			public int PercentageCalls { get; private set; }
			public List<string> CallOrder { get; } = new();

			public static int Apply(int health, int denominator, params int[] modifiers)
			{
				return ApplyPercentageDamage(health, modifiers, denominator);
			}

			public void ApplyDirectActor()
			{
				InflictDirectActor(null, null, null, null);
			}

			protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
			{
				FlatCalls++;
				CallOrder.Add("flat");
			}

			protected override void InflictPercentage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
			{
				PercentageCalls++;
				CallOrder.Add("percentage");
			}
		}
	}
}
