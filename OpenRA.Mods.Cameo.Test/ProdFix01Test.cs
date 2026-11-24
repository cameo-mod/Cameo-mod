#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-FIX-01 (AUDIT_PRODUCTION_01): unit production gates, one switch and one ablation row each.
	[TestFixture]
	public class ProdFix01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) => MiniYaml.FromString(Source("packaging/ai.yaml"), "ai")
			.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string Field(MiniYamlNode node, string key) => node.Value.Nodes.Single(n => n.Key == key).Value.Value.Trim();

		[Test]
		public void FA_UnitReserveDropsPOnlyWhenOnAndKeepsTheUnsetSentinelClosed()
		{
			Assert.That(ProdBusyCA.UnitReserveLine(true, 1800), Is.EqualTo(0));
			Assert.That(ProdBusyCA.UnitReserveLine(false, 1800), Is.EqualTo(1800), "off = P as before");
			Assert.That(ProdBusyCA.UnitReserveLine(true, -1), Is.EqualTo(-1), "unset line still fails closed");

			var general = RecoverySpendPolicyCA.Admission.General;
			Assert.That(RecoverySpendPolicyCA.Admits(1500, 400, 0, 0, 0, false, general, ProdBusyCA.UnitReserveLine(true, 1800)), Is.True);
			Assert.That(RecoverySpendPolicyCA.Admits(1500, 400, 0, 0, 0, false, general, ProdBusyCA.UnitReserveLine(false, 1800)), Is.False);
		}

		[Test]
		public void FB_RotateStartVisitsEveryCategoryFirstInTurn()
		{
			var length = 5;
			var index = 2;
			var firsts = Enumerable.Range(0, length).Select(_ =>
			{
				index = ProdBusyCA.RotateStart(index, length);
				var first = (index + 1) % length; // the loop pre-increments
				for (var i = 0; i < length; i++)
					index = (index + 1) % length; // a full sweep returns to the same cursor
				return first;
			}).ToArray();
			Assert.That(firsts.Distinct().Count(), Is.EqualTo(length));
			Assert.That(ProdBusyCA.RotateStart(4, 5), Is.EqualTo(0));
			Assert.That(ProdBusyCA.RotateStart(0, 0), Is.EqualTo(0));
		}

		[Test]
		public void FB_SaturationReadsTheBatchBankWhenConsistent()
		{
			Assert.That(ProdBusyCA.SaturationBank(true, 6000, 2000), Is.EqualTo(2000));
			Assert.That(ProdBusyCA.SaturationBank(false, 6000, 2000), Is.EqualTo(6000), "off = the pass-start bank");
			Assert.That(ProdBusyCA.SaturationBank(true, 6000, null), Is.EqualTo(6000), "no batch (legacy path) = live bank");
			Assert.That(ProductionSaturationCA.Saturated(ProdBusyCA.SaturationBank(true, 6000, 2000), 5250), Is.False);
			Assert.That(ProductionSaturationCA.Saturated(ProdBusyCA.SaturationBank(false, 6000, 2000), 5250), Is.True);
		}

		[Test]
		public void FD_IntervalDropsByElapsedTicks()
		{
			Assert.That(ProdBusyCA.IntervalStep(true, 100, 133), Is.EqualTo(33));
			Assert.That(ProdBusyCA.IntervalStep(true, 100, 100), Is.EqualTo(1));
			Assert.That(ProdBusyCA.IntervalStep(true, -1, 500), Is.EqualTo(1), "first visit");
			Assert.That(ProdBusyCA.IntervalStep(false, 100, 133), Is.EqualTo(1), "off = one per visit");

			// An interval of 100 ticks set at t=0 with visits every 11 ticks: gone by t=110 (10 visits), not t=1100.
			var left = 100;
			var last = 0;
			var t = 0;
			while (left > 0)
			{
				t += 11;
				left -= ProdBusyCA.IntervalStep(true, last, t);
				last = t;
			}

			Assert.That(t, Is.EqualTo(110));
		}

		[Test]
		public void FE_IdleReasonReportsTheFirstFailingGateBeforeSaturation()
		{
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, true, true, false, true), Is.EqualTo("prerequisite_missing"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, true, false, true, true), Is.EqualTo("unit_cap"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, true, false, false, true), Is.EqualTo("saturation_stop"));
			Assert.That(PlayerTelemetryMath.IdleReason(true, true, true, true, true, true), Is.EqualTo("cash_reserved"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, true, true, true, true, true), Is.EqualTo("no_cash"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, false, false, false, true), Is.EqualTo("no_desire"));
			Assert.That(PlayerTelemetryMath.IdleReason(false, false, true, true, true), Is.EqualTo("saturation_stop"), "off = old precedence");
		}

		[Test]
		public void EachSwitchHasARowIsSwitchGatedAndArmedOnTheGenericInstanceOnly()
		{
			var ids = AblationSpec.RegistryIds();
			foreach (var id in new[] { "pb_unit_reserve_excl_upgrades", "pb_rotate_start", "pb_sat_batch_consistent", "pb_interval_elapsed", "pb_no_unit_floor", "tm_idle_reason_order" })
				Assert.That(ids, Does.Contain(id));

			var unit = Source("mods/CA/Traits/BotModules/UnitBuilderBotModuleCA.cs");
			Assert.That(unit, Does.Contain("bool UnitReserveExclOn => switchesActive && Info.UnitReserveExclUpgrades && !ablateUnitReserve.Off(player, AblationIds.ProdFixUnitReserveExclUpgrades);"));
			Assert.That(unit, Does.Contain("bool RotateStartOn => switchesActive && Info.RotateStart && !ablateRotateStart.Off(player, AblationIds.ProdFixRotateStart);"));
			Assert.That(unit, Does.Contain("bool SatBatchOn => switchesActive && Info.SatBatchConsistent && !ablateSatBatch.Off(player, AblationIds.ProdFixSatBatchConsistent);"));
			Assert.That(unit, Does.Contain("bool IntervalElapsedOn => switchesActive && Info.IntervalElapsed && !ablateIntervalElapsed.Off(player, AblationIds.ProdFixIntervalElapsed);"));
			Assert.That(unit, Does.Contain("bool NoUnitFloorOn => switchesActive && Info.NoUnitFloor && !ablateNoUnitFloor.Off(player, AblationIds.ProdFixNoUnitFloor);"));

			var node = PlayerNode("UnitBuilderBotModuleCA@generic");
			Assert.That(Field(node, "SwitchCondition"), Is.EqualTo("genericbot"));
			foreach (var key in new[] { "UnitReserveExclUpgrades", "RotateStart", "SatBatchConsistent", "IntervalElapsed", "NoUnitFloor" })
				Assert.That(Field(node, key), Is.EqualTo("true"));

			// Every Info field defaults to false (classic stays byte-identical).
			foreach (var field in new[] { "UnitReserveExclUpgrades", "RotateStart", "SatBatchConsistent", "IntervalElapsed", "NoUnitFloor" })
				Assert.That(unit, Does.Contain("public readonly bool " + field + " = false;"));
		}
	}
}
