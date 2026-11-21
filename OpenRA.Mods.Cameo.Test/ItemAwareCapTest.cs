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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// PROD-IDLE item-aware unit cap (Code Monkey F2 on 2a2bafb8): the budget's counting predicate for an item that is
	// about to be produced, and the unit builder's fail-closed gate. Written with the code; not run (no build in the
	// PERF WINDOW).
	[TestFixture]
	public class ItemAwareCapTest
	{
		static readonly ActorInfo Tank = new("tank", new MobileInfo());
		static readonly ActorInfo Plane = new("plane", new AircraftInfo());
		static readonly ActorInfo Harvester = new("harv", new MobileInfo(), new HarvesterInfo());
		static readonly ActorInfo Mcv = new("mcv", new MobileInfo());
		static readonly ActorInfo Barracks = new("barr", new BuildingInfo());

		static BotGlobalUnitBudgetInfo Budget(bool ignoreHarvesters = true, params string[] ignored)
		{
			var info = new BotGlobalUnitBudgetInfo();
			typeof(BotGlobalUnitBudgetInfo).GetField(nameof(BotGlobalUnitBudgetInfo.IgnoreHarvesters))
				.SetValue(info, ignoreHarvesters);
			foreach (var name in ignored)
				info.IgnoredActorTypes.Add(name);
			return info;
		}

		[Test]
		public void UnitsCountBuildingsDoNot()
		{
			var info = Budget();
			Assert.That(BotGlobalUnitBudget.Counts(info, Tank), Is.True);
			Assert.That(BotGlobalUnitBudget.Counts(info, Plane), Is.True);
			Assert.That(BotGlobalUnitBudget.Counts(info, Barracks), Is.False, "a building is not a unit");
		}

		[Test]
		public void HarvestersAreExcludedExactlyWhileIgnoreHarvestersIsOn()
		{
			Assert.That(BotGlobalUnitBudget.Counts(Budget(true), Harvester), Is.False);
			Assert.That(BotGlobalUnitBudget.Counts(Budget(false), Harvester), Is.True);
			Assert.That(BotGlobalUnitBudget.Excluded(Budget(true), Harvester), Is.True);
			Assert.That(BotGlobalUnitBudget.Excluded(Budget(false), Harvester), Is.False);
		}

		[Test]
		public void IgnoredNamesAreExcludedWhateverTheirTraits()
		{
			var info = Budget(true, "mcv");
			Assert.That(BotGlobalUnitBudget.Counts(info, Mcv), Is.False);
			Assert.That(BotGlobalUnitBudget.Counts(info, Tank), Is.True, "only the listed name");
			Assert.That(BotGlobalUnitBudget.Excluded(info, Mcv), Is.True);
		}

		[Test]
		public void ExclusionRuleIsTheOneTheLiveCountUses()
		{
			// ComputePaused calls Excluded for every live actor (source-level), so the predicate above and the live count
			// cannot drift apart: a harvester or an ignored name is skipped by both, anything else is counted by both.
			var info = Budget(true, "mcv");
			foreach (var actor in new[] { Tank, Plane, Harvester, Mcv })
				Assert.That(BotGlobalUnitBudget.Counts(info, actor), Is.EqualTo(!BotGlobalUnitBudget.Excluded(info, actor)), actor.Name);
		}

		sealed class CountsNamesStartingWith : IBotItemAwareUnitCap
		{
			readonly string prefix;
			public CountsNamesStartingWith(string prefix) { this.prefix = prefix; }
			public bool CountsAgainstCap(ActorInfo actor) => actor.Name.StartsWith(prefix, StringComparison.Ordinal);
		}

		[Test]
		public void NoPausingCapNeverCapsAnything()
		{
			Assert.That(ItemAwareCapCA.CappedOut([], Tank), Is.False);
			Assert.That(ItemAwareCapCA.CappedOut([], null), Is.False, "an unknown name is only capped while a cap is pausing");
		}

		[Test]
		public void UnknownItemFailsClosedWhileACapIsPausing()
		{
			Assert.That(ItemAwareCapCA.CappedOut([new CountsNamesStartingWith("zzz")], null), Is.True);
		}

		[Test]
		public void PausingCapRejectsOnlyTheItemsItCounts()
		{
			IBotItemAwareUnitCap[] pausing = [new CountsNamesStartingWith("tank")];
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Tank), Is.True);
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Harvester), Is.False, "the harvester stays buildable");
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Mcv), Is.False);
		}

		[Test]
		public void AnyPausingCapThatCountsTheItemRejectsIt()
		{
			IBotItemAwareUnitCap[] pausing = [new CountsNamesStartingWith("tank"), new CountsNamesStartingWith("harv")];
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Tank), Is.True);
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Harvester), Is.True);
			Assert.That(ItemAwareCapCA.CappedOut(pausing, Mcv), Is.False);
		}
	}
}
