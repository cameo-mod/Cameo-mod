from pathlib import Path
root=Path('C:/cameo-wt/codex-multi-turret-engine')
license=(root/'OpenRA.Mods.Common/Traits/AutoTarget.cs').read_text(encoding='utf-8').split('using System;')[0]
code='''using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	/// <summary>Exact weapon effectiveness, independent of raw damage and target identity.</summary>
	public readonly struct WeaponTargetScore : IComparable<WeaponTargetScore>
	{
		public readonly BigInteger Numerator;
		public readonly BigInteger Denominator;
		public WeaponTargetScore(BigInteger numerator, BigInteger denominator)
		{
			if (denominator <= 0)
				throw new ArgumentOutOfRangeException(nameof(denominator));
			Numerator = numerator;
			Denominator = denominator;
		}
		public int CompareTo(WeaponTargetScore other) =>
			(Numerator * other.Denominator).CompareTo(other.Numerator * Denominator);
		public static readonly WeaponTargetScore Neutral = new(100, 1);

		// A target with the stronger Versus wins even when it is farther away.
		// Distance and actor id only settle equal effectiveness scores.
		public static bool Prefer(WeaponTargetScore candidate, long distanceSquared, uint actorId,
			WeaponTargetScore current, long currentDistanceSquared, uint currentActorId)
		{
			var comparison = candidate.CompareTo(current);
			return comparison > 0 || comparison == 0 &&
				(distanceSquared < currentDistanceSquared || distanceSquared == currentDistanceSquared && actorId < currentActorId);
		}

		public static WeaponTargetScore ArmorMultiplier(IEnumerable<int> percentages)
		{
			BigInteger numerator = 100, denominator = 1;
			foreach (var percentage in percentages)
			{
				numerator *= percentage;
				denominator *= 100;
			}
			return new WeaponTargetScore(numerator, denominator);
		}

		/// <summary>Damage-weighted Versus for the weapon's valid positive-damage warheads.
		/// The denominator removes raw damage, so this never ranks DPS instead of Versus.
		/// Call only for a visible actor; no frozen actor's live backing state is read.</summary>
		public static WeaponTargetScore Against(Armament armament, Actor source, Actor victim)
		{
			var shape = victim.EnabledTargetablePositions.OfType<HitShape>()
				.MinByOrDefault(h => h.DistanceFromEdge(victim, victim.CenterPosition));
			if (shape == null)
				return Neutral;
			var total = new WeaponTargetScore(0, 1);
			BigInteger weight = 0;
			foreach (var warhead in armament.Weapon.Warheads.OfType<DamageWarhead>())
			{
				if (warhead.Damage <= 0 || !warhead.IsValidAgainst(victim, source))
					continue;
				var multiplier = ArmorMultiplier(victim.TraitsImplementing<Armor>()
					.Where(a => !a.IsTraitDisabled && a.Info.Type != null &&
						warhead.Versus.ContainsKey(a.Info.Type) &&
						(shape.Info.ArmorTypes.IsEmpty || shape.Info.ArmorTypes.Contains(a.Info.Type)))
					.Select(a => warhead.Versus[a.Info.Type]));
				total = new WeaponTargetScore(total.Numerator * multiplier.Denominator +
					multiplier.Numerator * warhead.Damage * total.Denominator,
					total.Denominator * multiplier.Denominator);
				weight += warhead.Damage;
			}
			return weight == 0 ? Neutral : new WeaponTargetScore(total.Numerator, total.Denominator * weight);
		}
	}
}
'''
(root/'OpenRA.Mods.Common/Traits/WeaponTargetScore.cs').write_text(license+code,encoding='utf-8')
test='''using System.Numerics;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Test
{
	[TestFixture]
	public class WeaponTargetScoreTest
	{
		[Test]
		public void VersusWinsBeforeDistance() => Assert.That(WeaponTargetScore.Prefer(
			new(120, 1), 10000, 20, new(80, 1), 1, 1), Is.True);
		[Test]
		public void EqualVersusPrefersNearest() => Assert.That(WeaponTargetScore.Prefer(
			new(120, 1), 10, 20, new(240, 2), 20, 1), Is.True);
		[Test]
		public void ExactTieUsesActorId() => Assert.That(WeaponTargetScore.Prefer(
			new(120, 1), 10, 1, new(120, 1), 10, 20), Is.True);
		[Test]
		public void FractionalVersusDoesNotTruncate() => Assert.That(new WeaponTargetScore(1201, 10)
			.CompareTo(new WeaponTargetScore(120, 1)), Is.GreaterThan(0));
		[Test]
		public void MissingArmorIsNeutral() => Assert.That(WeaponTargetScore.ArmorMultiplier([])
			.CompareTo(WeaponTargetScore.Neutral), Is.Zero);
		[Test]
		public void ArmorModifiersCompose() => Assert.That(WeaponTargetScore.ArmorMultiplier([120, 80])
			.CompareTo(new WeaponTargetScore(96, 1)), Is.Zero);
		[Test]
		public void LargeProductsDoNotOverflow() => Assert.That(WeaponTargetScore.ArmorMultiplier([int.MaxValue, int.MaxValue])
			.CompareTo(new WeaponTargetScore((BigInteger)int.MaxValue * int.MaxValue, 100)), Is.Zero);
		[Test]
		public void ImmunityIsZero() => Assert.That(WeaponTargetScore.ArmorMultiplier([120, 0])
			.CompareTo(new WeaponTargetScore(0, 1)), Is.Zero);
	}
}
'''
(root/'OpenRA.Test/OpenRA.Mods.Common/WeaponTargetScoreTest.cs').write_text(license+test,encoding='utf-8')
print('Added exact scorer and eight focused tests')
