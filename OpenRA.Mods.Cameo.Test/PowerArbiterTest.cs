#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// CONSOLIDATE-03 P4 (a): PowerArbiter is the old power maths moved verbatim. The tables compare it with the formulas it replaced, the pins show every power site goes through it.
	[TestFixture]
	public sealed class PowerArbiterTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// The formulas as they stood in BaseBuilderQueueManagerCA before P4 (QM.cs:926-933 and :1912-1917).
		static int OldRequired(int min, int max, int increment, int threshold, int buildings, bool capOn, int cap, int milli)
		{
			var bonus = increment * (buildings / (threshold < 1 ? 1 : threshold));
			var margin = min + bonus;
			margin = margin < min ? min : margin > max ? max : margin;
			if (capOn)
				margin = Math.Min(margin, Math.Max(min, cap));

			return milli == 1000 ? margin : (int)Math.Min(int.MaxValue, margin * (long)milli / 1000);
		}

		[Test]
		public void RequiredMatchesTheOldFormulaOverAGrid()
		{
			foreach (var min in new[] { 0, 50, 100 })
			foreach (var max in new[] { 0, 100, 400 })
			foreach (var increment in new[] { 0, 10, 25 })
			foreach (var threshold in new[] { 0, 1, 3 })
			foreach (var buildings in new[] { 0, 1, 7, 30 })
			foreach (var capOn in new[] { false, true })
			foreach (var milli in new[] { 1000, 500, 1500 })
				Assert.That(PowerArbiter.Required(min, max, increment, threshold, buildings, capOn, 300, milli),
					Is.EqualTo(OldRequired(min, max, increment, threshold, buildings, capOn, 300, milli)),
					$"min {min} max {max} inc {increment} thr {threshold} n {buildings} cap {capOn} milli {milli}");
		}

		[Test]
		public void FloorEffectiveShortAndAdmitsAreTheOldPredicates()
		{
			Assert.That(PowerArbiter.Floor(100, 1000, 30), Is.EqualTo(130), "base margin plus the radar reserve");
			Assert.That(PowerArbiter.Floor(100, 500, 30), Is.EqualTo(80), "the knob scales the base margin, not the radar reserve");

			Assert.That(PowerArbiter.Effective(40, 100, false), Is.EqualTo(40), "off: the raw surplus");
			Assert.That(PowerArbiter.Effective(40, 100, true), Is.EqualTo(140), "on: plus the plants in production");

			Assert.That(PowerArbiter.Short(true, 99, 100), Is.True);
			Assert.That(PowerArbiter.Short(true, 100, 100), Is.False);
			Assert.That(PowerArbiter.Short(false, -500, 100), Is.False, "no power manager: never short");

			Assert.That(PowerArbiter.Admits(true, -20, 120, 100), Is.True, "120 - 20 reaches the floor");
			Assert.That(PowerArbiter.Admits(true, -21, 120, 100), Is.False);
			Assert.That(PowerArbiter.Admits(false, -999, -999, 100), Is.True, "no power manager: always admitted");
		}

		[Test]
		public void EveryPowerSiteGoesThroughTheArbiter()
		{
			var qm = Source(Dir + "BaseBuilderQueueManagerCA.cs");
			Assert.That(qm, Does.Contain("minimumExcessPower = PowerArbiter.Required("));
			Assert.That(qm, Does.Contain("var powerOk = PowerArbiter.Admits(playerPower != null, candidatePower, excess, floor);"));
			Assert.That(qm, Does.Contain("return PowerArbiter.Floor(baseBuilder.Info.MinimumExcessPower, BuildOrderPowerMilli(), radarPowerMargin);"));
			Assert.That(qm, Does.Contain("PowerShortRaw(PowerArbiter.SiteEssential)"));
			Assert.That(qm, Does.Contain("if (PowerShortEmergency(PowerArbiter.SiteOverride) && power != null"));
			Assert.That(qm, Does.Not.Contain("playerPower.ExcessPower < minimumExcessPower"), "no second emergency formula");

			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.ModeBias.cs"), Does.Contain("PowerShortRaw(PowerArbiter.SiteModeBias)"));
			Assert.That(Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs"), Does.Contain("ExcessPower = EmergencyExcess(),"));

			var recovery = Source(Dir + "RecoveryEssentialPlannerCA.cs");
			Assert.That(recovery, Does.Contain("PowerArbiter.Short(powerManager != null, powerManager?.ExcessPower ?? 0, minimumExcessPower)"));
			Assert.That(recovery, Does.Contain("!PowerArbiter.Admits(true,"));
			Assert.That(recovery, Does.Not.Contain("powerManager.ExcessPower < minimumExcessPower"));
		}

		[Test]
		public void TheDevStreamHashesEveryDecisionAndIsCompiledOutOfARelease()
		{
			var power = Source(Dir + "BaseBuilderQueueManagerCA.Power.cs");
			Assert.That(power, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid PowerObserved("));
			Assert.That(power, Does.Contain("BB-POWER-STREAM"));
			Assert.That(power, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\tulong powerStream"), "the hash state exists only in a DEV build");
			Assert.That(power, Does.Contain("1099511628211UL"), "FNV-1a 64");
		}
	}
}
