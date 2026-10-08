#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License as
 * published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Linq;
using NUnit.Framework;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Mods.Cameo.Warheads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class AreaDamageWarheadTest
	{
		sealed class TargetingProbe : AreaDamageWarhead
		{
			public int ActualVersus(Actor victim, HitShape shape) => DamageVersus(victim, shape, default);
		}
		[TestCase(-1)]
		[TestCase(0)]
		[TestCase(2000)]
		public void AutomaticTargetScoreUsesEffectiveDamageVersus(int heaviness)
		{
			var fixture = new OpenRA.Test.AttackGarrisonedTest.Fixture(dynamicPriority: true);
			var info = FieldLoader.Load<TargetingProbe>(new MiniYaml("", new[]
			{
				new MiniYamlNode("Heaviness", new MiniYaml(heaviness.ToString())),
				new MiniYamlNode("Versus", new MiniYaml("", new[]
				{
					new MiniYamlNode("Heavy", new MiniYaml("120")),
				})),
			}));
			((IRulesetLoaded<WeaponInfo>)info).RulesetLoaded(null, null);
			var shape = fixture.Tank.TraitsImplementing<HitShape>().Single();
			Assert.That(info.TargetingVersus(fixture.Tank, shape).CompareTo(
				new WeaponTargetScore(info.ActualVersus(fixture.Tank, shape), 1)), Is.Zero);
		}

		[TestCase(2010, 10000, 101)]
		[TestCase(240000, 10000, 12000)]
		[TestCase(300000, 10000, 15000)]
		[TestCase(600000, 10000, 30000)]
		public void FoldedPercentageUsesWideIntermediate(int damage, int scale, int expected)
		{
			Assert.That(AreaDamageWarhead.FoldedPercentageUnits(damage, scale), Is.EqualTo(expected));
		}

		[Test]
		public void PercentageDenominatorUsesWideIntermediate()
		{
			Assert.That(
				AreaDamageWarhead.ApplyPercentageDenominator(1125000000, 200),
				Is.EqualTo(562500000));
		}

		[TestCase(101, 100, 101)]
		[TestCase(101, 300, 33)]
		public void PercentageDenominatorPreservesFastPathAndTruncation(
			int damage, int denominator, int expected)
		{
			Assert.That(
				AreaDamageWarhead.ApplyPercentageDenominator(damage, denominator),
				Is.EqualTo(expected));
		}

		[Test]
		public void DamageStateScalingUsesWideIntermediate()
		{
			Assert.That(AreaDamageWarhead.ScaleDamage(1200000000, 150), Is.EqualTo(1800000000));
		}

		[TestCase(-101, 50, -50)]
		[TestCase(101, -50, -50)]
		public void DamageStateScalingTruncatesSignedValuesTowardZero(
			int damage, int percentage, int expected)
		{
			Assert.That(AreaDamageWarhead.ScaleDamage(damage, percentage), Is.EqualTo(expected));
		}

		[Test]
		public void FinalRuntimeValuesRemainCheckedInt32()
		{
			Assert.Throws<System.OverflowException>(() =>
				AreaDamageWarhead.FoldedPercentageUnits(int.MaxValue, int.MaxValue));
			Assert.Throws<System.OverflowException>(() =>
				AreaDamageWarhead.ApplyPercentageDenominator(int.MaxValue, 1));
			Assert.Throws<System.OverflowException>(() =>
				AreaDamageWarhead.ScaleDamage(int.MaxValue, 101));
		}
	}
}
