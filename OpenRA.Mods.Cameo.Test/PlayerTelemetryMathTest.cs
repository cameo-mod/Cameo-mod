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
	}
}
