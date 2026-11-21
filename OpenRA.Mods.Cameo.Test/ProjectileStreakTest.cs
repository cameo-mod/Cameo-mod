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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cameo.Test
{
	// TRACER-01: pure geometry of the tapered projectile streak (engine ProjectileStreak helper, Blackrobe's taper keys).
	// Needs the engine pin that contains OpenRA.Mods.Common.Graphics.ProjectileStreak.
	[TestFixture]
	public sealed class ProjectileStreakTest
	{
		static readonly Color Body = Color.FromArgb(0xFF, 0xF4, 0xC2);
		static readonly WPos Tail = new(1000, 2000, 0);
		static readonly WPos Head = new(1960, 2000, 0);

		static ProjectileStreakStyle Tower(int taperLength = 192) =>
			new(new WDist(45), new WDist(6), new WDist(24), new WDist(taperLength),
				Body, Color.FromArgb(0xFF, 0xB0, 0x60), 176, 2047);

		[Test]
		public void KeysAtDefaultDrawOneFlatSegment()
		{
			var style = new ProjectileStreakStyle(new WDist(45), null, null, WDist.Zero, Body, null, 255, 2047);
			var segments = ProjectileStreak.Build(style, Head, Tail, 960);

			Assert.That(segments, Has.Count.EqualTo(1));
			Assert.That(segments[0].StartWidth, Is.EqualTo(new WDist(45)));
			Assert.That(segments[0].EndWidth, Is.EqualTo(new WDist(45)));
			Assert.That(segments[0].StartColor, Is.EqualTo(Body));
			Assert.That(segments[0].EndColor, Is.EqualTo(Body));
			Assert.That(segments[0].Start, Is.EqualTo(Tail));
			Assert.That(segments[0].Vector, Is.EqualTo(Head - Tail));
		}

		[Test]
		public void TowerValuesDrawABodyAndATaper()
		{
			var segments = ProjectileStreak.Build(Tower(), Head, Tail, 960);

			Assert.That(segments, Has.Count.EqualTo(2));

			// Body: thin faint tail widening to the full width and colour, 768 long.
			Assert.That(segments[0].Start, Is.EqualTo(Tail));
			Assert.That(segments[0].Vector.Length, Is.EqualTo(768));
			Assert.That(segments[0].StartWidth, Is.EqualTo(new WDist(6)));
			Assert.That(segments[0].EndWidth, Is.EqualTo(new WDist(45)));
			Assert.That(segments[0].StartColor, Is.EqualTo(Color.FromArgb(176, Color.FromArgb(0xFF, 0xB0, 0x60))));
			Assert.That(segments[0].EndColor, Is.EqualTo(Body));

			// Taper: starts where the body ends, 192 long, full width narrowing to the head width.
			Assert.That(segments[1].Start, Is.EqualTo(Tail + new WVec(768, 0, 0)));
			Assert.That(segments[1].Vector.Length, Is.EqualTo(192));
			Assert.That(segments[1].StartWidth, Is.EqualTo(new WDist(45)));
			Assert.That(segments[1].EndWidth, Is.EqualTo(new WDist(24)));
			Assert.That(segments[1].Start + segments[1].Vector, Is.EqualTo(Head));
		}

		[Test]
		public void TaperLongerThanTheStreakIsClamped()
		{
			var segments = ProjectileStreak.Build(Tower(2000), Head, Tail, 960);

			Assert.That(segments, Has.Count.EqualTo(1));
			Assert.That(segments[0].Start, Is.EqualTo(Tail));
			Assert.That(segments[0].Vector.Length, Is.EqualTo(960));
			Assert.That(segments[0].StartWidth, Is.EqualTo(new WDist(45)));
			Assert.That(segments[0].EndWidth, Is.EqualTo(new WDist(24)));
		}

		[Test]
		public void EmptyStreakDrawsNothing()
		{
			Assert.That(ProjectileStreak.Build(Tower(), Head, Tail, 0), Is.Empty);
			Assert.That(ProjectileStreak.Build(Tower(), Head, Tail, -5), Is.Empty);
		}

		[TestCase(999, 255)]
		[TestCase(-5, 0)]
		[TestCase(176, 176)]
		public void TailAlphaIsClampedInsteadOfThrowing(int alpha, int expected)
		{
			var style = new ProjectileStreakStyle(new WDist(45), null, null, WDist.Zero, Body,
				Color.FromArgb(0xFF, 0xB0, 0x60), alpha, 2047);

			Assert.That(style.TailColor, Is.EqualTo(Color.FromArgb(expected, Color.FromArgb(0xFF, 0xB0, 0x60))));
		}

		[Test]
		public void TaperOverALongStreakDoesNotOverflow()
		{
			// (head - tail) * bodyLength was an int product: 1.9e9 * 1.9e9 wrapped. Per component in long it stays exact.
			var tail = new WPos(0, 0, 0);
			var head = new WPos(1_900_000_000, 950_000_000, 0);
			var length = (head - tail).Length;
			var segments = ProjectileStreak.Build(Tower(192), head, tail, length);

			Assert.That(segments, Has.Count.EqualTo(2));
			Assert.That(segments[1].Start.X, Is.GreaterThan(head.X - 400).And.LessThanOrEqualTo(head.X));
			Assert.That(segments[1].Start.Y, Is.GreaterThan(head.Y - 200).And.LessThanOrEqualTo(head.Y));
			Assert.That(segments[1].Start + segments[1].Vector, Is.EqualTo(head));
			Assert.That(segments[1].Vector.Length, Is.InRange(190, 194));
		}

		[Test]
		public void ScaleIsExactInLongAndRejectsANonPositiveDenominator()
		{
			Assert.That(ProjectileStreak.Scale(new WVec(2_000_000_000, -2_000_000_000, 6), 2_000_000_000, 2_000_000_000), Is.EqualTo(new WVec(2_000_000_000, -2_000_000_000, 6)));
			Assert.That(ProjectileStreak.Scale(new WVec(1000, 0, 0), 1, 3), Is.EqualTo(new WVec(333, 0, 0)));
			Assert.That(ProjectileStreak.Scale(new WVec(-1000, 0, 0), 1, 3), Is.EqualTo(new WVec(-333, 0, 0)));
			Assert.Throws<System.ArgumentOutOfRangeException>(() => ProjectileStreak.Scale(new WVec(1, 1, 1), 1, 0));
		}

		[TestCase(960, 96, 0UL, 864)]
		[TestCase(960, 96, 96UL, 960)]
		[TestCase(960, 96, 192UL, 1056)]
		[TestCase(960, 96, 193UL, 864)]
		[TestCase(960, 0, 12345UL, 960)]
		[TestCase(960, -50, 12345UL, 960)]
		[TestCase(50, 96, 0UL, 1)]
		[TestCase(int.MaxValue, int.MaxValue, ulong.MaxValue, 1)]
		[TestCase(int.MaxValue, int.MaxValue, 4294967294UL, int.MaxValue)]
		[TestCase(int.MaxValue, int.MaxValue, 0UL, 1)]
		[TestCase(int.MaxValue, 1, 2UL, int.MaxValue)]
		[TestCase(1, int.MaxValue, 0UL, 1)]
		[TestCase(0, 0, 0UL, 1)]
		public void VariedLengthStaysInBoundsAtTheExtremes(int length, int variation, ulong roll, int expected)
		{
			Assert.That(ProjectileStreak.VariedLength(length, variation, roll), Is.EqualTo(expected));
		}

		[Test]
		public void VariedLengthNeverLeavesItsRange()
		{
			for (ulong roll = 0; roll < 5000; roll += 7)
			{
				var len = ProjectileStreak.VariedLength(960, 96, roll * 2654435761UL);
				Assert.That(len, Is.InRange(864, 1056));
			}
		}
	}
}
