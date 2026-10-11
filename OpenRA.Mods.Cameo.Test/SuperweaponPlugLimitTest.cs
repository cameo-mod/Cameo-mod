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
using System.Linq;
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

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" });

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

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "nodnuke" });

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

			// Declaring the ungated sibling token too must not capture the plug:
			// its provider lacks global-swlimit and the plug negates only
			// !cabalnuke_swlimit.
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug },
				new[] { "cabalnuke", "cabalnuke_swlimit" });

			Assert.Multiple(() =>
			{
				Assert.That(map.Count, Is.EqualTo(1));
				Assert.That(map["cabal_missilesilo"], Is.EqualTo("cabalnuke_swlimit"));
			});
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

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, ionPlug, droppodPlug },
				new[] { "tsionc" });

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

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" });

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

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" });

			Assert.That(map, Is.Empty);
		}

		[Test]
		public void TokenMap_OrdinaryConditionProvider_NegatedItem_Excluded()
		{
			// Review regression: an ordinary host whose conditional provider is NOT
			// wired to the lobby cap, combined with an item that negates that
			// provider's token, must never be classified as SW-cap wiring.
			var host = new ActorInfo("naxis_techcenter",
				Provides("mgcap", "mg"),
				Pluggable(("mg", "mg")));
			var plug = new ActorInfo("naxis_mg_nest",
				Plug("mg"),
				Buildable("~naxis_constructionyard, naxis_techcenter, !mgcap"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "mgcap" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map, Is.Empty);
				Assert.That(diags.Any(d => d.Contains("mgcap")), Is.True,
					"expected a diagnostic naming the declared token");
			});
		}

		[Test]
		public void TokenMap_NonSwlimitGatedProvider_NegatedItem_Excluded()
		{
			// The provider is gated on a DIFFERENT prerequisite token, not the
			// lobby cap — still not capacity wiring.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "techlevel.superweapons"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" });

			Assert.That(map, Is.Empty);
		}

		[Test]
		public void TokenMap_InvertedConditionProvider_Excluded()
		{
			// Review regression: a provider gated on a NEGATED variable
			// (RequiresCondition: !ionc) can never be an install condition even
			// when it carries the lobby-cap prerequisite.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "!ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" });

			Assert.That(map, Is.Empty);
		}

		// ---------------------------------------------------------------
		// Declared-catalogue semantics (OccupancyTokens on the trait Info)
		// ---------------------------------------------------------------

		[Test]
		public void TokenMap_NoDeclaredTokens_AllUncapped()
		{
			// Zero declared candidates: even fully-wired SW plugs stay uncapped.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new string[0]);

			Assert.That(map, Is.Empty);
		}

		[Test]
		public void TokenMap_UndeclaredToken_PlugUncapped()
		{
			// Fully-wired ionc plug, but only nodnuke is declared — the declared
			// catalogue is authoritative, not the wiring.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "nodnuke" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map, Is.Empty);
				Assert.That(diags.Any(d => d.Contains("nodnuke") && d.Contains("no ProvidesPrerequisite")), Is.True);
			});
		}

		[Test]
		public void TokenMap_DeclaredToken_MissingProvider_Diagnostic()
		{
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { plug }, new[] { "ionc" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map, Is.Empty);
				Assert.That(diags.Any(d => d.Contains("'ionc'") && d.Contains("no ProvidesPrerequisite")), Is.True);
				Assert.That(diags.Any(d => d.Contains("'ionc'") && d.Contains("nothing capped")), Is.True);
			});
		}

		[Test]
		public void TokenMap_ResolvedProvider_EchoesGateDiagnostic()
		{
			// Diagnostic echo of the resolved provider gate for review.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plug = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "ionc" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map["td_gdi_ioncannonuplink"], Is.EqualTo("ionc"));
				Assert.That(diags.Any(d => d.Contains("'ionc'") && d.Contains("gated on 'ionc'")), Is.True,
					"expected resolved-gate echo naming the token and condition");
			});
		}

		[Test]
		public void TokenMap_SharedToken_TwoPlugs_BothMapped_WithDiagnostic()
		{
			// Two plug actors negating the same declared token share one capacity
			// slot — both map, and the ambiguity is surfaced for review.
			var host = new ActorInfo("td_gdi_advancedcommunicationscenter",
				Provides("ionc", "ionc", "global-swlimit"),
				Pluggable(("td_gdi_advancedcommunicationscenter", "ionc")));
			var plugA = new ActorInfo("td_gdi_ioncannonuplink",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));
			var plugB = new ActorInfo("td_gdi_ioncannonuplink_mk2",
				Plug("td_gdi_advancedcommunicationscenter"),
				Buildable("!ionc"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plugA, plugB }, new[] { "ionc" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map.Count, Is.EqualTo(2));
				Assert.That(map.Values, Is.All.EqualTo("ionc"));
				Assert.That(diags.Any(d => d.Contains("shared-slot") || d.Contains("resolves 2")), Is.True);
			});
		}

		[Test]
		public void TokenMap_CrossHostProviderSocket_NeverJoins()
		{
			// F3 regression: provider on host A + socket on host B granting the
			// same condition must NOT fabricate wiring — installing host B's
			// plug never publishes the token.
			var hostA = new ActorInfo("provider_only_host",
				Provides("cap", "sharedcond", "global-swlimit"));
			var hostB = new ActorInfo("socket_only_host",
				Pluggable(("addon", "sharedcond")));
			var plug = new ActorInfo("ordinary_addon",
				Plug("addon"),
				Buildable("~socket_only_host, socket_only_host, !cap"));

			var diags = new List<string>();
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { hostA, hostB, plug }, new[] { "cap" }, diags);

			Assert.Multiple(() =>
			{
				Assert.That(map, Is.Empty, "cross-host provider+socket must not produce a mapping");
				Assert.That(diags.Any(d => d.Contains("'cap'") && d.Contains("same-host")), Is.True,
					"expected a missing same-host socket diagnostic for 'cap'");
				Assert.That(diags.Any(d => d.Contains("ordinary_addon") && d.Contains("uncapped")), Is.True,
					"expected an uncapped diagnostic for the negating plug");
			});
		}

		[Test]
		public void TokenMap_CrossHost_SameActorStillMaps()
		{
			// Control for F3: when the SAME host carries provider and socket,
			// the wiring resolves normally.
			var host = new ActorInfo("full_host",
				Provides("cap", "sharedcond", "global-swlimit"),
				Pluggable(("addon", "sharedcond")));
			var plug = new ActorInfo("ordinary_addon",
				Plug("addon"),
				Buildable("~full_host, full_host, !cap"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "cap" });

			Assert.That(map["ordinary_addon"], Is.EqualTo("cap"));
		}

		[Test]
		public void TokenMap_ItemNegatingTwoResolvedTokens_Rejected_OrderIndependent()
		{
			// F4 regression: one plug item negating TWO declared tokens that both
			// resolve for its plug type is ambiguous — it must be rejected, not
			// bound to whichever token is iterated first. Both token orders in
			// the item prerequisites and both actor orders must give the same
			// empty result.
			ActorInfo Host(string name, string token, string condition) =>
				new(name,
					Provides(token, condition, "global-swlimit"),
					Pluggable(("shared_sw", condition)));

			ActorInfo PlugActor(string prereqs) =>
				new("ambiguous_plug",
					Plug("shared_sw"),
					Buildable(prereqs));

			var hostA = Host("host_a", "tok_a", "cond_a");
			var hostB = Host("host_b", "tok_b", "cond_b");
			var plugAB = PlugActor("!tok_a, !tok_b");
			var plugBA = PlugActor("!tok_b, !tok_a");
			var declared = new[] { "tok_a", "tok_b" };

			var diagsForward = new List<string>();
			var mapForward = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { hostA, hostB, plugAB }, declared, diagsForward);
			var diagsReverse = new List<string>();
			var mapReverse = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { hostB, hostA, plugBA }, declared, diagsReverse);

			Assert.Multiple(() =>
			{
				Assert.That(mapForward, Is.Empty, "ambiguous multi-token plug must be rejected");
				Assert.That(mapReverse, Is.Empty, "rejection must be order-independent");
				Assert.That(diagsForward.Any(d => d.Contains("ambiguous_plug") && d.Contains("ambiguous")), Is.True,
					"expected an ambiguity diagnostic naming the plug");
				Assert.That(diagsReverse.Any(d => d.Contains("ambiguous_plug") && d.Contains("ambiguous")), Is.True);
			});
		}

		[Test]
		public void TokenMap_SingleTokenOfTwoNegated_StillMaps()
		{
			// F4 control: an item negating a resolving token plus a declared
			// token whose chain does NOT resolve for its type is unambiguous —
			// it maps to the single resolving token.
			var host = new ActorInfo("host_a",
				Provides("tok_a", "cond_a", "global-swlimit"),
				Pluggable(("shared_sw", "cond_a")));
			var plug = new ActorInfo("typed_plug",
				Plug("shared_sw"),
				Buildable("!tok_a, !tok_b"));

			var map = SuperweaponPlugLimit.BuildPlugTokenMap(new[] { host, plug }, new[] { "tok_a", "tok_b" });

			Assert.That(map["typed_plug"], Is.EqualTo("tok_a"));
		}

		[Test]
		public void TokenMap_ExactlyFourActiveMappings()
		{
			// Resolved-wiring assertion: the four real host/plug pairs and nothing
			// else produce capacity mappings. The CABAL host's ungated
			// @cabalnuke provider and the droppod plug derive out.
			var actors = new[]
			{
				new ActorInfo("td_gdi_advancedcommunicationscenter",
					Provides("ionc", "ionc", "global-swlimit"),
					Provides("techcenter", "!(powerdown || infiltrated)"),
					Pluggable(("td_gdi_advancedcommunicationscenter", "ionc"))),
				new ActorInfo("td_gdi_ioncannonuplink",
					Plug("td_gdi_advancedcommunicationscenter"),
					Buildable("~td_gdi_constructionyard, td_gdi_advancedcommunicationscenter, !ionc, ~techlevel.superweapons")),
				new ActorInfo("td_nod_templeofnod",
					Provides("nodnuke", "nuke", "global-swlimit"),
					Pluggable(("td_nod_templeofnod", "nuke"))),
				new ActorInfo("td_nod_nuclearmissilesilo",
					Plug("td_nod_templeofnod"),
					Buildable("~td_nod_constructionyard, td_nod_templeofnod, !nodnuke, ~techlevel.superweapons")),
				new ActorInfo("cabal_core",
					Provides("cabalnuke", "cabalnuke"),
					Provides("cabalnuke_swlimit", "cabalnuke", "global-swlimit"),
					Pluggable(("cabalcore_silo", "cabalnuke"))),
				new ActorInfo("cabal_missilesilo",
					Plug("cabalcore_silo"),
					Buildable("~cabal_core, cabal_core, !cabalnuke_swlimit, ~techlevel.superweapons")),
				new ActorInfo("ts_gdi_upgradecenter",
					Provides("tsionc", "ionc", "global-swlimit"),
					Pluggable(("ioncannon", "ionc"), ("droppod", "droppod"))),
				new ActorInfo("ts_gdi_ioncannonuplink",
					Plug("ioncannon"),
					Buildable("~ts_gdi_upgradecenter, !droppod, !tsionc, ~techlevel.superweapons")),
				new ActorInfo("ts_gdi_droppoduplink",
					Plug("droppod"),
					Buildable("~ts_gdi_upgradecenter, !ionc, !droppod")),
			};

			// Declared catalogue = the world.yaml OccupancyTokens list.
			var map = SuperweaponPlugLimit.BuildPlugTokenMap(actors,
				new[] { "ionc", "nodnuke", "cabalnuke_swlimit", "tsionc" });

			Assert.Multiple(() =>
			{
				Assert.That(map.Count, Is.EqualTo(4));
				Assert.That(map["td_gdi_ioncannonuplink"], Is.EqualTo("ionc"));
				Assert.That(map["td_nod_nuclearmissilesilo"], Is.EqualTo("nodnuke"));
				Assert.That(map["cabal_missilesilo"], Is.EqualTo("cabalnuke_swlimit"));
				Assert.That(map["ts_gdi_ioncannonuplink"], Is.EqualTo("tsionc"));
			});
		}
	}
}
