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
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SquadMicroEvalTest
	{
		static readonly BitSet<TargetableType> Ground = new("Ground");
		static readonly BitSet<TargetableType> Air = new("Air");
		static readonly BitSet<TargetableType> None = default;

		static BotUnitProfile Unit(string name, int hp, int cost, double dpt, WDist range,
			BitSet<TargetableType>? targets = null, BitSet<TargetableType>? hits = null)
		{
			var weapons = dpt > 0 ? new[] { new BotWeaponProfile(dpt, range, hits ?? Ground, None, null) } : [];
			return new BotUnitProfile(name, cost, hp, null, 50, false, false, targets ?? Ground, weapons);
		}

		[Test]
		public void FocusPicksFastestKill()
		{
			var squad = new[] { Unit("tank", 100, 100, 10, WDist.FromCells(5)) };
			var targets = new[]
			{
				Unit("wall", 2000, 10, 0, WDist.Zero),   // 200 ticks to kill
				Unit("harvester", 100, 800, 0, WDist.Zero), // 10 ticks to kill
			};
			Assert.That(SquadMicroEvalCA.PickFocusTarget(squad, targets), Is.EqualTo(1));
		}

		[Test]
		public void FocusSkipsUnkillableTargets()
		{
			var squad = new[] { Unit("tank", 100, 100, 10, WDist.FromCells(5)) }; // ground-only guns
			var targets = new[]
			{
				Unit("plane", 50, 500, 5, WDist.FromCells(6), targets: Air), // unreachable: air target vs ground-only weapon
				Unit("tank", 500, 500, 5, WDist.FromCells(5)),
			};
			Assert.That(SquadMicroEvalCA.PickFocusTarget(squad, targets), Is.EqualTo(1));
		}

		[Test]
		public void FocusReturnsMinusOneWhenNothingKillable()
		{
			var squad = new[] { Unit("tank", 100, 100, 10, WDist.FromCells(5)) };
			var targets = new[] { Unit("plane", 50, 500, 5, WDist.FromCells(6), targets: Air) };
			Assert.That(SquadMicroEvalCA.PickFocusTarget(squad, targets), Is.EqualTo(-1));
		}

		[Test]
		public void FocusTiebreakPrefersThreateningTarget()
		{
			var squad = new[] { Unit("tank", 100, 100, 10, WDist.FromCells(5)) };
			var targets = new[]
			{
				Unit("harvester", 100, 800, 0, WDist.Zero),    // same TTK, no threat
				Unit("tank", 100, 800, 9, WDist.FromCells(5)), // same TTK, shoots back
			};
			Assert.That(SquadMicroEvalCA.PickFocusTarget(squad, targets), Is.EqualTo(1));
		}

		[Test]
		public void PullBackAtThreshold()
		{
			Assert.That(SquadMicroEvalCA.ShouldPullBack(30, 100, 30), Is.True);  // exactly at the line
			Assert.That(SquadMicroEvalCA.ShouldPullBack(29, 100, 30), Is.True);
			Assert.That(SquadMicroEvalCA.ShouldPullBack(31, 100, 30), Is.False);
			Assert.That(SquadMicroEvalCA.ShouldPullBack(0, 0, 30), Is.False);    // dead/unknown never pulls
		}

		[Test]
		public void KiteStandoffOnlyWithRangeAdvantage()
		{
			var artillery = Unit("arty", 100, 900, 8, WDist.FromCells(12));
			var tank = Unit("tank", 300, 800, 10, WDist.FromCells(5));
			Assert.That(SquadMicroEvalCA.KiteStandoff(artillery, tank, WDist.FromCells(1)),
				Is.EqualTo(WDist.FromCells(6)));
			Assert.That(SquadMicroEvalCA.KiteStandoff(tank, artillery, WDist.FromCells(1)), Is.Null);
			Assert.That(SquadMicroEvalCA.KiteStandoff(tank, tank, WDist.FromCells(1)), Is.Null); // equal range: no kite
		}

		[Test]
		public void PullBackPointRetreatsDirectlyAwayFromThreat()
		{
			var squadPos = new WPos(5000, 0, 0);
			var threatPos = new WPos(2000, 0, 0);
			var point = SquadMicroEvalCA.PullBackPoint(squadPos, threatPos, WDist.FromCells(3));

			Assert.That(point.X, Is.EqualTo(5000 + WDist.FromCells(3).Length));
			Assert.That(point.Y, Is.EqualTo(0));
		}

		[Test]
		public void PullBackPointDiagonalKeepsDirection()
		{
			// away = (-3000, -4000), |away| = 5000: the point lands at
			// (-3000/5000*1024, -4000/5000*1024) = (-614, -819) after truncation.
			var point = SquadMicroEvalCA.PullBackPoint(new WPos(0, 0, 0), new WPos(3000, 4000, 0), WDist.FromCells(1));
			Assert.That(point.X, Is.EqualTo(-614));
			Assert.That(point.Y, Is.EqualTo(-819));
		}

		[Test]
		public void PullBackPointCoincidentPositionsStayPut()
		{
			var pos = new WPos(1234, 567, 0);
			Assert.That(SquadMicroEvalCA.PullBackPoint(pos, pos, WDist.FromCells(4)), Is.EqualTo(pos));
		}

		[Test]
		public void ConcaveArcDistinctSlotsLandOnDistinctPoints()
		{
			var targetPos = new WPos(10000, 10000, 0);
			var squadCenter = new WPos(10000, 15000, 0);
			var radius = WDist.FromCells(5);
			var points = Enumerable.Range(0, 5)
				.Select(i => SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, i, 5, radius))
				.ToList();

			Assert.That(points.Distinct().Count(), Is.EqualTo(points.Count));
		}

		[Test]
		public void ConcaveArcSlotsMirrorAcrossAxis()
		{
			// Axis along +X: mirrored slots share the along-axis coordinate and
			// flip the cross-axis one — the fan is symmetric about the axis.
			var targetPos = new WPos(0, 0, 0);
			var squadCenter = new WPos(8000, 0, 0);
			var radius = WDist.FromCells(4);
			const int count = 6;
			for (var i = 0; i < count; i++)
			{
				var a = SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, i, count, radius);
				var b = SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, count - 1 - i, count, radius);
				Assert.That(a.X, Is.EqualTo(b.X), $"slot {i} along-axis");
				Assert.That(a.Y, Is.EqualTo(-b.Y), $"slot {i} cross-axis");
			}
		}

		[Test]
		public void ConcaveArcSlotsSitOnRing()
		{
			var targetPos = new WPos(10000, -4000, 0);
			var squadCenter = new WPos(2000, -8000, 0);
			var radius = WDist.FromCells(6);
			for (var i = 0; i < 8; i++)
			{
				var p = SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, i, 8, radius);
				var d = (p - targetPos).HorizontalLength;
				Assert.That(d, Is.InRange(radius.Length - 4, radius.Length + 4), $"slot {i} radius");
			}
		}

		[Test]
		public void ConcaveArcSingleMemberLandsOnAxisPoint()
		{
			var targetPos = new WPos(1000, 1000, 0);
			var squadCenter = new WPos(4000, 1000, 0); // axis +X
			var radius = WDist.FromCells(3);
			var p = SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, 0, 1, radius);
			Assert.That(p, Is.EqualTo(new WPos(1000 + radius.Length, 1000, 0)));
		}

		[Test]
		public void ConcaveArcCoincidentAxisDoesNotThrow()
		{
			var pos = new WPos(5000, 5000, 0);
			var radius = WDist.FromCells(4);
			Assert.DoesNotThrow(() => SquadMicroEvalCA.ConcaveArcPoint(pos, pos, 2, 5, radius));

			// Fallback axis is +X: the slot still lands on the ring.
			var p = SquadMicroEvalCA.ConcaveArcPoint(pos, pos, 0, 5, radius);
			var d = (p - pos).HorizontalLength;
			Assert.That(d, Is.InRange(radius.Length - 4, radius.Length + 4));
		}

		[Test]
		public void ConcaveArcWingsNeverWrapBehindTarget()
		{
			// A 20-strong squad hits the 135° spread cap: every slot stays
			// within ±75° of the squad's side of the target.
			var targetPos = new WPos(0, 0, 0);
			var squadCenter = new WPos(6000, 0, 0);
			var radius = WDist.FromCells(5);
			var axisYaw = WAngle.ArcTan(squadCenter.Y - targetPos.Y, squadCenter.X - targetPos.X);
			const int wingCap = 75 * 1024 / 360; // 213 units ≈ 75°
			for (var i = 0; i < 20; i++)
			{
				var p = SquadMicroEvalCA.ConcaveArcPoint(targetPos, squadCenter, i, 20, radius);
				var slotYaw = WAngle.ArcTan(p.Y - targetPos.Y, p.X - targetPos.X);
				var diff = (slotYaw - axisYaw).Angle;
				var abs = Math.Min(diff, 1024 - diff);
				Assert.That(abs, Is.LessThanOrEqualTo(wingCap + 2), $"slot {i} angle");
			}
		}
	}
}
