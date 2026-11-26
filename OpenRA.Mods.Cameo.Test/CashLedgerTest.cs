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
	// CONSOLIDATE-03 P2 (a) (CONSOLIDATE_03_P2_DESIGN_02.md): the ONE cash ledger. Wrapper agreement over a grid: every ledger figure equals the old helper.
	// This is not the old-vs-new parity gate (Opus ruling 11-26: OLD build vs NEW build, same seed, the replay order stream and sync hash identical).
	[TestFixture]
	public sealed class CashLedgerTest
	{
		const string Dir = "mods/CA/Traits/BotModules/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static readonly int[] Banks = [-500, 0, 1, 299, 300, 1000, 2999, 3000, 3300, 20000];
		static readonly int[] Costs = [0, 1, 300, 1000, 2500];
		static readonly int[] Reserves = [-1, 0, 300, 3300];

		[Test]
		public void WrapperAgreementTheBuildingFiguresEqualTheOldHelpers()
		{
			foreach (var all in new[] { false, true })
				foreach (var bank in Banks)
					foreach (var ordered in Costs)
						Assert.That(CashLedger.BuildingBank(all, bank, ordered), Is.EqualTo(ProdBusyCA.SweepBank(all, bank, ordered)));

			foreach (BaseBuildStage stage in Enum.GetValues(typeof(BaseBuildStage)))
				foreach (var reserve in Reserves)
				{
					var line = CashLedger.BuildingReserve(stage, reserve);
					Assert.That(line, Is.EqualTo(BaseBuildPlanCA.ReserveFor(stage, reserve)));
					foreach (var bank in Banks)
						foreach (var cost in Costs)
						{
							var a = CashLedger.AdmitBuilding(bank, cost, line);
							Assert.That(a.Ok, Is.EqualTo(BaseBuildPlanCA.Affordable(bank, cost, line)), $"{stage} bank {bank} cost {cost} reserve {line}");
							Assert.That(a.Bank, Is.EqualTo(bank));
							Assert.That(a.Reserve, Is.EqualTo(line));
							Assert.That(a.Why, Is.EqualTo(a.Ok ? CashReason.Affordable : bank > 0 ? CashReason.BelowReserve : CashReason.BankEmpty));
						}
				}
		}

		[Test]
		public void WrapperAgreementTheUnitFiguresEqualTheOldHelpers()
		{
			foreach (var excl in new[] { false, true })
				foreach (var lean in new[] { 0.0, 0.5, 0.99, 1.0, 1.5 })
					foreach (var reserve in Reserves)
					{
						var line = CashLedger.UnitReserve(reserve, lean, excl);
						Assert.That(line, Is.EqualTo(ProdBusyCA.UnitReserveLine(excl, RecoverySpendPolicyCA.EffectiveReserve(reserve, lean))));
						foreach (var bank in Banks)
							foreach (var unpaid in new long[] { 0, 1, 1000, 50000 })
								foreach (var cost in Costs)
								{
									Assert.That(CashLedger.UnitReserveAdmits(bank, unpaid, cost, line), Is.EqualTo(RecoverySpendPolicyCA.Admits(
										SpendCommitmentsCA.Available(bank, unpaid), cost, 0, 0, 0, false, RecoverySpendPolicyCA.Admission.General, line)));
									Assert.That(CashLedger.UnitDepthAdmits(bank, unpaid, cost), Is.EqualTo(QueueDepthCA.CashAdmits(bank, unpaid, cost)));
								}
					}
		}

		[Test]
		public void TheLeanOnlyLowersTheReserve()
		{
			// Sonnet condition 2 (for row cl_one_reserve): lean >= 1 (no trailing lead) is the identity, a reserve of 0 stays 0, and no lean raises a line.
			foreach (var reserve in new[] { 0, 1, 300, 1000, 3300 })
			{
				Assert.That(RecoverySpendPolicyCA.EffectiveReserve(reserve, 1.0), Is.EqualTo(reserve));
				Assert.That(RecoverySpendPolicyCA.EffectiveReserve(0, 0.25), Is.EqualTo(0));
				foreach (var lean in new[] { 0.0, 0.25, 0.5, 0.99, 1.0, 2.0 })
					Assert.That(RecoverySpendPolicyCA.EffectiveReserve(reserve, lean), Is.InRange(0, reserve));
			}

			Assert.That(RecoverySpendPolicyCA.EffectiveReserve(-1, 0.5), Is.EqualTo(-1), "unset stays unset (fails closed in the policy)");
		}

		[Test]
		public void TheFoldSitesReadTheLedger()
		{
			var p = Source(Dir + "BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p, Does.Contain("Bank = CashLedger.BuildingBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep),"));
			Assert.That(p, Does.Contain("if (!CashLedger.AdmitBuilding(s.Bank, startCost, CashLedger.BuildingReserve(c.Stage, s.ProductionCashReserve)).Ok)"));

			var d = Source(Dir + "BotModuleLogic/BaseBuilderQueueManagerCA.Pipeline01Defence.cs");
			Assert.That(d, Does.Contain("var bank = CashLedger.BuildingBank(AllProducersOn, playerResources.GetCashAndResources(), orderedCostThisSweep);"));

			var u = Source(Dir + "UnitBuilderBotModuleCA.cs");
			Assert.That(u, Does.Contain("? CashLedger.UnitReserve(botLimits.Info.ProductionCashReserve, BotPersonalityLeads.Lean(leadProviders, \"steamroller\"), UnitReserveExclOn)\n\t\t\t\t: -1;"));
			Assert.That(u, Does.Contain("return CashLedger.UnitReserveAdmits(batch.BankLeft, UnpaidQueuedCost(), cost, line);"));
			Assert.That(u, Does.Contain("|| depthQueue && held > 0 && !CashLedger.UnitDepthAdmits(batch.BankLeft, unpaidQueued, cost))"));

			// Sonnet note 1 on 72f4359a: the legacy and essentials sweeps read the same ledger bank, so the ledger is the one owner of the building bank.
			var m = Source(Dir + "BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(m, Does.Not.Contain("ProdBusyCA.SweepBank("));
			Assert.That(System.Text.RegularExpressions.Regex.Matches(m, @"CashLedger\.BuildingBank\(").Count, Is.EqualTo(3));

			var ledger = Source(Dir + "BotModuleLogic/CashLedger.cs");
			Assert.That(ledger, Does.Not.Contain("GetCashAndResources"), "the ledger snapshots nothing: the caller passes the live bank (Sonnet condition 1)");
		}
	}
}
