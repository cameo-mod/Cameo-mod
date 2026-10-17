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
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class PlayerTelemetryMathTest
	{
		[Test]
		public void MobileCategoryPicksAirOverNavalAndInfantry()
		{
			Assert.That(PlayerTelemetryMath.MobileCategory(true, true, true), Is.EqualTo("air"));
			Assert.That(PlayerTelemetryMath.MobileCategory(false, true, true), Is.EqualTo("naval"));
			Assert.That(PlayerTelemetryMath.MobileCategory(false, false, true), Is.EqualTo("infantry"));
			Assert.That(PlayerTelemetryMath.MobileCategory(false, false, false), Is.EqualTo("vehicle"));
		}

		[Test]
		public void MarchingNeedsBothLegsOfTheAttack()
		{
			// Outward + toward the enemy: marching.
			Assert.That(PlayerTelemetryMath.Marching(10, 15, 40, 35, 4), Is.True);
			// Outward but not approaching an enemy start: a wandering patrol, not a wave member.
			Assert.That(PlayerTelemetryMath.Marching(10, 15, 40, 40, 4), Is.False);
			// Approaching the enemy but hugging the base: not enough outward movement.
			Assert.That(PlayerTelemetryMath.Marching(10, 12, 40, 30, 4), Is.False);
			// No previous enemy distance (-1): the approach leg cannot be proven.
			Assert.That(PlayerTelemetryMath.Marching(10, 20, -1, 5, 4), Is.False);
		}

		[Test]
		public void WaveHysteresisOpensAndCloses()
		{
			Assert.That(PlayerTelemetryMath.Wave(false, 5, 5, 2), Is.EqualTo(PlayerTelemetryMath.WaveEdge.Start));
			Assert.That(PlayerTelemetryMath.Wave(false, 4, 5, 2), Is.EqualTo(PlayerTelemetryMath.WaveEdge.None));
			Assert.That(PlayerTelemetryMath.Wave(true, 3, 5, 2), Is.EqualTo(PlayerTelemetryMath.WaveEdge.None));
			Assert.That(PlayerTelemetryMath.Wave(true, 1, 5, 2), Is.EqualTo(PlayerTelemetryMath.WaveEdge.End));
		}

		[Test]
		public void PositivePrereqsStripsHiddenAndNegated()
		{
			var tokens = PlayerTelemetryMath.PositivePrereqs(["fact", "~tech", "!enemy", "  ", null, "WEAP"]);
			Assert.That(tokens, Is.EqualTo(new[] { "fact", "tech", "weap" }));
			Assert.That(PlayerTelemetryMath.PositivePrereqs(null), Is.Empty);
		}

		static Dictionary<string, (IReadOnlyList<string>, IReadOnlyList<string>)> Specs(
			params (string Name, string[] Prereqs, string[] Provides)[] defs)
		{
			var specs = new Dictionary<string, (IReadOnlyList<string>, IReadOnlyList<string>)>();
			foreach (var d in defs)
				specs[d.Name] = (d.Prereqs, d.Provides);
			return specs;
		}

		[Test]
		public void TechDepthsChainsThroughProviders()
		{
			var depths = PlayerTelemetryMath.TechDepths(Specs(
				("conyard", [], []),
				("power", ["conyard"], []),
				("refinery", ["conyard", "power"], []),
				("factory", ["refinery"], []),
				("tech", ["factory", "techtoken"], ["techtoken"])));

			Assert.That(depths["conyard"], Is.EqualTo(1));
			Assert.That(depths["power"], Is.EqualTo(2));
			Assert.That(depths["refinery"], Is.EqualTo(3));
			Assert.That(depths["factory"], Is.EqualTo(4));
			// "techtoken" is self-provided, so only the factory leg counts.
			Assert.That(depths["tech"], Is.EqualTo(5));
		}

		[Test]
		public void TechDepthsUsesShallowestProviderPerToken()
		{
			var depths = PlayerTelemetryMath.TechDepths(Specs(
				("conyard", [], []),
				("power", ["conyard"], []),
				("late_power", ["power"], ["power"]),
				("factory", ["power"], [])));

			// "power" is provided by both power (depth 2) and late_power (depth 3); the
			// shallowest provider wins, so the factory is 3 not 4.
			Assert.That(depths["factory"], Is.EqualTo(3));
		}

		[Test]
		public void TechDepthsUnknownTokensAddNothingAndCyclesDoNotHang()
		{
			var depths = PlayerTelemetryMath.TechDepths(Specs(
				("lonely", ["missing"], []),
				("loop_a", ["loop_b"], []),
				("loop_b", ["loop_a"], [])));

			Assert.That(depths["lonely"], Is.EqualTo(1));

			// The cycle is ungrounded, so the back-edge contributes 0; the node evaluated
			// first still layers on the completed second node — loop_a resolves first in
			// ordinal order, making the outcome deterministic: a=2, b=1.
			Assert.That(depths["loop_a"], Is.EqualTo(2));
			Assert.That(depths["loop_b"], Is.EqualTo(1));
		}

		[Test]
		public void QueueStatePrefersDisabledThenBusy()
		{
			Assert.That(PlayerTelemetryMath.QueueState(false, true, true), Is.EqualTo("disabled"));
			Assert.That(PlayerTelemetryMath.QueueState(false, false, false), Is.EqualTo("disabled"));
			Assert.That(PlayerTelemetryMath.QueueState(true, false, false), Is.EqualTo("idle"));
			Assert.That(PlayerTelemetryMath.QueueState(true, true, true), Is.EqualTo("paused"));
			Assert.That(PlayerTelemetryMath.QueueState(true, true, false), Is.EqualTo("busy"));
		}

		[Test]
		public void IdleReasonFollowsTheModuleGateOrder()
		{
			// Every flag true: the earliest gate (a reservation vote) wins the label.
			Assert.That(PlayerTelemetryMath.IdleReason(true, true, true, true, true), Is.EqualTo("cash_reserved"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, true, true, true, true), Is.EqualTo("no_cash"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, true, true, true), Is.EqualTo("saturation_stop"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, false, true, true), Is.EqualTo("prerequisite_missing"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, false, false, true), Is.EqualTo("unit_cap"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, false, false, false), Is.EqualTo("no_desire"));

			// A later gate never masks an earlier one.
			Assert.That(PlayerTelemetryMath.IdleReason(true, false, false, false, false), Is.EqualTo("cash_reserved"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, true, false, false, false), Is.EqualTo("no_cash"));
		}

		[Test]
		public void BuildingCategoryMapsTheSpecBuckets()
		{
			Assert.That(PlayerTelemetryMath.BuildingCategory(true, false, false), Is.EqualTo("defence"));
			Assert.That(PlayerTelemetryMath.BuildingCategory(false, true, false), Is.EqualTo("economy"));
			Assert.That(PlayerTelemetryMath.BuildingCategory(false, false, true), Is.EqualTo("crawl_link"));
			Assert.That(PlayerTelemetryMath.BuildingCategory(false, false, false), Is.EqualTo("tech"));

			// Defence outranks the other families when a type list overlaps.
			Assert.That(PlayerTelemetryMath.BuildingCategory(true, true, true), Is.EqualTo("defence"));
			Assert.That(PlayerTelemetryMath.BuildingCategory(false, true, true), Is.EqualTo("economy"));
		}

		[Test]
		public void SeamKeyIsIdenticalForSeamWriteAndPollLookup()
		{
			// The regression this guards: the seam wrote "{playerActorId}:{type}|{item}"
			// while the head-diff looked up "{queueHostActorId}:{type}|{item}" — queue
			// hosts are factory buildings, never the player entity, so every seamed
			// start double-counted. One constructor at both sites keeps them equal.
			var write = PlayerTelemetryMath.SeamKey(7, "VEHICLE", "td_gdi_mediumtank");
			var lookup = PlayerTelemetryMath.SeamKey(7, "VEHICLE", "td_gdi_mediumtank");
			Assert.That(write, Is.EqualTo(lookup));
			Assert.That(write, Is.EqualTo("7:VEHICLE|td_gdi_mediumtank"));

			// A different queue host actor id, queue type, or item can never collide.
			Assert.That(write, Is.Not.EqualTo(PlayerTelemetryMath.SeamKey(9, "VEHICLE", "td_gdi_mediumtank")));
			Assert.That(write, Is.Not.EqualTo(PlayerTelemetryMath.SeamKey(7, "INFANTRY", "td_gdi_mediumtank")));
			Assert.That(write, Is.Not.EqualTo(PlayerTelemetryMath.SeamKey(7, "VEHICLE", "td_gdi_humvee")));
		}

		[Test]
		public void ConsumeSeamStartCountsAnOrderIssueStartExactlyOnce()
		{
			var markers = new Dictionary<string, int>(StringComparer.Ordinal);
			var key = PlayerTelemetryMath.SeamKey(7, "VEHICLE", "td_gdi_mediumtank");

			// No marker: the head-diff counts the start itself.
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.False);

			// One marker: consumed once — a second look for the same episode must not
			// also count it (the accepted/spend double-count regression).
			markers[key] = 1;
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.True);
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.False);
			Assert.That(markers, Is.Empty);

			// Two seamed starts of the same item and queue type drain two markers.
			markers[key] = 2;
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.True);
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.True);
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.False);

			// A marker for a different item on the same queue type never matches.
			markers[PlayerTelemetryMath.SeamKey(7, "VEHICLE", "td_gdi_humvee")] = 1;
			Assert.That(PlayerTelemetryMath.ConsumeSeamStart(markers, key), Is.False);
		}

		[Test]
		public void UnionFragileTypesCoversModuleAbsentSeats()
		{
			// The regression this guards: crawl_link classification read the live CA
			// base builder's FragileTypes, so Frans seats (no CA module) classified
			// fragile-family items as tech. The ruleset union is identical content
			// for module seats and covers module-absent ones.
			var declared = new[]
			{
				new HashSet<string>(StringComparer.Ordinal) { "naxis_academy", "ra2_soviets_nuclearmissilesilo" },
				new HashSet<string>(StringComparer.Ordinal) { "yuri_battlelab" },
			};

			var union = PlayerTelemetryMath.UnionFragileTypes(declared);
			Assert.That(union.Contains("naxis_academy"), Is.True);
			Assert.That(union.Contains("ra2_soviets_nuclearmissilesilo"), Is.True);
			Assert.That(union.Contains("yuri_battlelab"), Is.True);
			Assert.That(union.Contains("td_gdi_mediumtank"), Is.False);
			Assert.That(union.Count, Is.EqualTo(3));

			// Missing or empty declarations degrade to an empty set, never to a null.
			Assert.That(PlayerTelemetryMath.UnionFragileTypes(null), Is.Empty);
			Assert.That(PlayerTelemetryMath.UnionFragileTypes(new IEnumerable<string>[] { null, new HashSet<string>() }), Is.Empty);
		}
	}
}
