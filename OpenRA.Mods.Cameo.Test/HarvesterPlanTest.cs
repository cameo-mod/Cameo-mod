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
	// CONSOLIDATE-03 P9 (a) (CONSOLIDATE_03_P9_DESIGN_01.md): HarvesterPlan is the one place that compares the harvester counts. With countQueued off it is the old module formula
	// bit for bit; the unit builder's target and cap answers are HarvesterTarget's.
	[TestFixture]
	public sealed class HarvesterPlanTest
	{
		const string Dir = "mods/CA/Traits/BotModules/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheModuleNeedIsTheOldFormulaWhenQueuedIsNotCounted()
		{
			for (var refineries = 0; refineries <= 6; refineries++)
				for (var per = 0; per <= 4; per++)
					for (var add = 0; add <= 3; add++)
						for (var live = 0; live <= 20; live++)
							for (var queued = 0; queued <= 4; queued++)
							{
								var old = live < refineries * per + add;
								Assert.That(HarvesterPlan.ModuleTooLow(live, queued, refineries, per, add, false), Is.EqualTo(old), $"{live}/{queued}/{refineries}/{per}/{add}");
							}
		}

		[Test]
		public void CountingQueuedMakesAHarvesterInTheFactoryCount()
		{
			Assert.That(HarvesterPlan.ModuleTooLow(2, 0, 1, 3, 0, true), Is.True, "2 live, none queued, need 3");
			Assert.That(HarvesterPlan.ModuleTooLow(2, 1, 1, 3, 0, true), Is.False, "the third is already building");
			Assert.That(HarvesterPlan.ModuleTooLow(2, 1, 1, 3, 0, false), Is.True, "off: the queue is invisible, as before");
			Assert.That(HarvesterPlan.ModuleNeed(2, 3, 1), Is.EqualTo(7));
		}

		[Test]
		public void QueuedModuleCountHasItsOwnDefaultOffSwitchAndAblation()
		{
			var info = Source(Dir + "UnitBuilderBotModuleCA.cs");
			Assert.That(info, Does.Contain("public readonly bool HvModuleCountsQueued = false;"));

			var unitBuilder = Source(Dir + "UnitBuilderBotModuleCA.HarvesterTarget.cs");
			Assert.That(unitBuilder, Does.Contain("bool ModuleCountsQueuedOn => Info.HvModuleCountsQueued && HarvesterTargetOn"));
			Assert.That(unitBuilder, Does.Contain("ablateModuleCountsQueued.Off(player, AblationIds.HvModuleCountsQueued)"));
			Assert.That(unitBuilder, Does.Contain("bool IBotHarvesterCounts.TryGetQueued(out int queued)"));

			var ids = Source(Dir + "BotModuleLogic/AblationIds.HarvesterPlan.cs");
			Assert.That(ids, Does.Contain("HvModuleCountsQueued = \"hv_module_counts_queued\""));

			var module = Source(Dir + "HarvesterBotModuleCA.cs");
			Assert.That(module, Does.Contain("TraitsImplementing<IBotHarvesterCounts>()"));
			Assert.That(module, Does.Contain("counts.TryGetQueued(out queuedHarvesters)"));

			var yaml = Source("packaging/ai.yaml");
			Assert.That(yaml, Does.Contain("HvModuleCountsQueued: true"));
		}

		[Test]
		public void TargetAndCapAreHarvesterTargets()
		{
			for (var live = 0; live <= 8; live++)
				for (var queued = 0; queued <= 3; queued++)
				{
					Assert.That(HarvesterPlan.TargetDue(live, queued, 4, 1500, 1000), Is.EqualTo(HarvesterTarget.Due(live, queued, 4, 1500, 1000)));
					Assert.That(HarvesterPlan.TargetDue(live, queued, 4, 500, 1000), Is.False, "bank below the minimum");
					Assert.That(HarvesterPlan.OverCap(live, queued, 6), Is.EqualTo(HarvesterTarget.OverCap(live, queued, 6)));
				}
		}

		[Test]
		public void VerdictsMapTheBooleans()
		{
			Assert.That(HarvesterPlan.ModuleVerdict(true), Is.EqualTo(HarvesterVerdict.ModuleRequest));
			Assert.That(HarvesterPlan.ModuleVerdict(false), Is.EqualTo(HarvesterVerdict.Enough));
			Assert.That(HarvesterPlan.TargetVerdict(true), Is.EqualTo(HarvesterVerdict.TargetStart));
			Assert.That(HarvesterPlan.CapVerdict(true), Is.EqualTo(HarvesterVerdict.OverCapRefused));
			Assert.That(HarvesterPlan.CapVerdict(false), Is.EqualTo(HarvesterVerdict.Enough));
		}

		[Test]
		public void TheThreeSitesAskThePlanAndNoSecondFormulaRemains()
		{
			var module = Source(Dir + "HarvesterBotModuleCA.cs");
			var produce = module.Substring(module.IndexOf("protected void ProduceHarvesters(", StringComparison.Ordinal));
			produce = produce.Substring(0, produce.IndexOf("void FindAndOrderLowEffectHarvesterOnResourceMap(", StringComparison.Ordinal));
			Assert.That(produce, Does.Contain("HarvesterPlan.ModuleTooLow("));
			Assert.That(produce, Does.Not.Contain("* Info.HarvestersPerRefinery +"), "no second copy of the formula in ProduceHarvesters");

			var ub = Source(Dir + "UnitBuilderBotModuleCA.HarvesterTarget.cs");
			Assert.That(ub, Does.Contain("HarvesterPlan.TargetDue("));
			Assert.That(ub, Does.Contain("HarvesterPlan.OverCap("));
			Assert.That(ub, Does.Not.Contain("HarvesterTarget.Due("));
			Assert.That(ub, Does.Not.Contain("HarvesterTarget.OverCap("));
		}

		[Test]
		public void TheDevStreamIsCompiledOutOfARelease()
		{
			var stream = Source("mods/CA/Traits/BotModules/BotModuleLogic/HarvesterStream.cs");
			Assert.That(stream, Does.Contain("#if CAMEO_DEV_LOGGING\nnamespace"));
			Assert.That(stream, Does.Contain("HV-PLAN-STREAM"));
			Assert.That(stream, Does.Contain("1099511628211UL"), "FNV-1a 64");

			foreach (var file in new[] { "HarvesterBotModuleCA.cs", "UnitBuilderBotModuleCA.HarvesterTarget.cs" })
			{
				var s = Source(Dir + file);
				Assert.That(s, Does.Contain("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]\n\t\tvoid HarvesterObserved("), file);
				Assert.That(s, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\treadonly HarvesterStream harvesterStream = new();"), file);
			}
		}
	}
}
