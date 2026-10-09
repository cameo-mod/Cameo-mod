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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SuperweaponPlugLimitTest
	{
		static T Info<T>(params MiniYamlNode[] nodes) where T : new()
		{
			return FieldLoader.Load<T>(new MiniYaml(null, nodes));
		}

		static ProvidesPrerequisiteInfo Provides(string prerequisite, string requiresCondition, string requiresPrerequisites = null)
		{
			var nodes = new List<MiniYamlNode>
			{
				new(nameof(ProvidesPrerequisiteInfo.Prerequisite), prerequisite),
				new(nameof(ProvidesPrerequisiteInfo.RequiresCondition), requiresCondition),
			};
			if (requiresPrerequisites != null)
				nodes.Add(new MiniYamlNode(nameof(ProvidesPrerequisiteInfo.RequiresPrerequisites), requiresPrerequisites));

			return Info<ProvidesPrerequisiteInfo>(nodes.ToArray());
		}

		static PluggableInfo Pluggable(params (string Type, string Condition)[] conditions)
		{
			var children = new List<MiniYamlNode>();
			foreach (var (type, condition) in conditions)
				children.Add(new MiniYamlNode(type, condition));

			return Info<PluggableInfo>(
				new MiniYamlNode(nameof(PluggableInfo.Conditions), (string)null, children));
		}

		static PlugInfo Plug(string type)
		{
			return Info<PlugInfo>(new MiniYamlNode(nameof(PlugInfo.Type), type));
		}

		static BuildableInfo Buildable(string prerequisites)
		{
			return Info<BuildableInfo>(new MiniYamlNode(nameof(BuildableInfo.Prerequisites), prerequisites));
		}

		// ---------------------------------------------------------------
		// Admission gate (synced IValidateOrder seam)
		// ---------------------------------------------------------------

		[Test]
		public void Admit_Limited_FirstPlug_Allowed()
		{
			Assert.That(SuperweaponPlugLimit.Admit(true, pending: 0, installed: false, requested: 1), Is.True);
		}

		[Test]
		public void Admit_Limited_SecondPending_Rejected()
		{
			Assert.That(SuperweaponPlugLimit.Admit(true, pending: 1, installed: false, requested: 1), Is.False);
		}

		[Test]
		public void Admit_Limited_Installed_Rejected()
		{
			Assert.That(SuperweaponPlugLimit.Admit(true, pending: 0, installed: true, requested: 1), Is.False);
		}

		[Test]
		public void Admit_Limited_BatchOfTwo_Rejected()
		{
			Assert.That(SuperweaponPlugLimit.Admit(true, pending: 0, installed: false, requested: 2), Is.False);
		}

		[Test]
		public void Admit_Unlimited_NeverGates()
		{
			Assert.That(SuperweaponPlugLimit.Admit(false, pending: 3, installed: true, requested: 2), Is.True);
		}

		// ---------------------------------------------------------------
		// Reconciliation excess selection
		// ---------------------------------------------------------------

		[Test]
		public void ExcessCount_Matrix()
		{
			Assert.Multiple(() =>
			{
				Assert.That(SuperweaponPlugLimit.ExcessCount(0, false), Is.EqualTo(0));
				Assert.That(SuperweaponPlugLimit.ExcessCount(1, false), Is.EqualTo(0));
				Assert.That(SuperweaponPlugLimit.ExcessCount(2, false), Is.EqualTo(1));
				Assert.That(SuperweaponPlugLimit.ExcessCount(0, true), Is.EqualTo(0));
				Assert.That(SuperweaponPlugLimit.ExcessCount(1, true), Is.EqualTo(1)); // installed + replenished
				Assert.That(SuperweaponPlugLimit.ExcessCount(3, true), Is.EqualTo(3));
			});
		}

		// ---------------------------------------------------------------
		// Refund split (engine parity with CancelProductionInner :843-849)
		// ---------------------------------------------------------------

		[Test]
		public void RefundCash_ExcludesResourcesRefund_IntegratorExample()
		{
			// F1 double-credit: TotalCost=1000 fully paid (RemainingCost=0) with
			// 600 paid as resources must refund 600 resources + 400 cash, not
			// 600 + 1000.
			Assert.That(SuperweaponPlugLimit.RefundCash(totalCost: 1000, remainingCost: 0, resourcesPaid: 600),
				Is.EqualTo(400));
		}

		[Test]
		public void RefundCash_NoResources_FullCashBack()
		{
			Assert.That(SuperweaponPlugLimit.RefundCash(1000, 0, 0), Is.EqualTo(1000));
		}

		[Test]
		public void RefundCash_PartialProgress_RefundsOnlyPaid()
		{
			Assert.That(SuperweaponPlugLimit.RefundCash(1000, 500, 0), Is.EqualTo(500));
			Assert.That(SuperweaponPlugLimit.RefundCash(1000, 500, 200), Is.EqualTo(300));
		}

		[Test]
		public void RefundCash_ZeroCost_ZeroRefund()
		{
			Assert.That(SuperweaponPlugLimit.RefundCash(0, 0, 0), Is.EqualTo(0));
		}

		[Test]
		public void RefundCash_AllResourcePaid_ZeroCash()
		{
			// Paid entirely in resources: cash share is zero, not TotalCost.
			Assert.That(SuperweaponPlugLimit.RefundCash(1000, 0, 1000), Is.EqualTo(0));
		}

		// ---------------------------------------------------------------
		// Token derivation from rules wiring
		// ---------------------------------------------------------------

		[Test]
		public void TokenMap_TdGdi_Wiring()
		{
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Provides("techcenter", "!(powerdown || infiltrated)"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("~td_gdi_constructionyard, td_gdi_advancedcommunicationscenter, !ionc, ~techlevel.superweapons"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug });

			Assert.That(map, Does.ContainKey("td_gdi_ioncannonuplink"));
			Assert.That(map["td_gdi_ioncannonuplink"], Is.EqualTo("ionc"));
		}

		[Test]
		public void TokenMap_TdNod_ConditionDiffersFromToken()
		{
			var host = new ActorInfo("td_nod_templeofnod",
				Provides("nodnuke", "nuke", "global-swlimit"),
				Pluggable(("td_nod_templeofnod", "nuke")));
			var plug = new ActorInfo("td_nod_nuclearmissilesilo",
				Plug("td_nod_templeofnod"),
				Buildable("~td_nod_constructionyard, td_nod_templeofnod, !nodnuke, ~techlevel.superweapons"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug });

			Assert.That(map["td_nod_nuclearmissilesilo"], Is.EqualTo("nodnuke"));
		}

		[Test]
		public void TokenMap_Cabal_TwoProviders_PicksNegatedCandidate()
		{
			// The host grants `cabalnuke`; two providers hang off the same condition —
			// the unconditional `cabalnuke` token and the `global-swlimit`-gated
			// `cabalnuke_swlimit` token. Only the latter is negated by the plug.
			var host = new ActorInfo("cabal_core",
				Provides("cabalnuke", "cabalnuke"),
				Provides("cabalnuke_swlimit", "cabalnuke", "global-swlimit"),
				Pluggable(("cabalcore_silo", "cabalnuke")));
			var plug = new ActorInfo("cabal_missilesilo",
				Plug("cabalcore_silo"),
				Buildable("~cabal_core, cabal_core, !cabalnuke_swlimit, ~techlevel.superweapons"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug });

			Assert.That(map["cabal_missilesilo"], Is.EqualTo("cabalnuke_swlimit"));
		}

		[Test]
		public void TokenMap_TsGdi_MultiPlugTypeHost_OnlySwMapped()
		{
			var host = new ActorInfo("ts_gdi_upgradecenter",
				Provides("tsionc", "ionc", "global-swlimit"),
				Pluggable(("ioncannon", "ionc"), ("droppod", "droppod")));
			var ionPlug = new ActorInfo("ts_gdi_ioncannonuplink",
				Plug("ioncannon"),
				Buildable("~ts_gdi_upgradecenter, !droppod, !tsionc, ~techlevel.superweapons"));
			var droppodPlug = new ActorInfo("ts_gdi_droppoduplink",
				Plug("droppod"),
				Buildable("~ts_gdi_upgradecenter, !ionc, !droppod"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, ionPlug, droppodPlug });

			Assert.Multiple(() =>
			{
				Assert.That(map["ts_gdi_ioncannonuplink"], Is.EqualTo("tsionc"));
				Assert.That(map, Does.Not.ContainKey("ts_gdi_droppoduplink"));
			});
		}

		[Test]
		public void TokenMap_OrdinaryPlug_Excluded()
		{
			// An ordinary plug whose host provides no condition-gated prerequisite is
			// never SW-capped (e.g. Naxis addon plugs).
			var host = new ActorInfo("naxis_techcenter",
				Provides("techcenter", "!(powerdown || infiltrated)"),
				Pluggable(("mg", "mg_condition")));
			var plug = new ActorInfo("naxis_mg_nest",
				Plug("mg"),
				Buildable("~naxis_constructionyard, naxis_techcenter"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug });

			Assert.That(map, Is.Empty);
		}

		[Test]
		public void TokenMap_PlugWithoutNegatedToken_Excluded()
		{
			// A plug on an SW-typed host that does NOT negate the occupancy token is
			// not itself capped (installed-only gating requires the !token).
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("some_addon",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("~td_gdi_constructionyard, td_gdi_advancedcommunicationscenter"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug });

			Assert.That(map, Is.Empty);
		}
	}
}
