#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using V = OpenRA.Mods.CA.Traits.RefineryFieldGateCA.EconomyVerdict;

namespace OpenRA.Mods.Cameo.Test
{
	// REF-FIELD-02 (USER PRIORITY 11-24, refinery spam; AUDIT_BASE_01 BASE-01): V04 placed 13-27 refineries per seat and lost 153. A field takes at most
	// 2 refineries, a field with a lost refinery is locked until it is safe, and the economy rung goes ref1, war factory, ref2, with an interval and a
	// bounded yield to first copies.
	[TestFixture]
	public sealed class RefField02Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void AFieldIsCappedAtItsRefineryLimit()
		{
			Assert.That(RefineryFieldGateCA.Capped(1, 2), Is.False);
			Assert.That(RefineryFieldGateCA.Capped(2, 2), Is.True);
			Assert.That(RefineryFieldGateCA.Capped(5, 0), Is.False, "a cap of 0 never binds");
		}

		[Test]
		public void ALostRefineryLocksItsFieldUntilTheLockRanAndTheFieldIsQuiet()
		{
			// lock 3000, quiet 1500, memory 12000; loss at 1000
			Assert.That(RefineryFieldGateCA.Locked(2000, 1000, -1, 3000, 1500, 12000), Is.True, "inside the minimum lock");
			Assert.That(RefineryFieldGateCA.Locked(4500, 1000, -1, 3000, 1500, 12000), Is.False, "lock over, never threatened");
			Assert.That(RefineryFieldGateCA.Locked(4500, 1000, 4000, 3000, 1500, 12000), Is.True, "lock over, enemy seen 500 ticks ago");
			Assert.That(RefineryFieldGateCA.Locked(6000, 1000, 4000, 3000, 1500, 12000), Is.False, "quiet for 2000 ticks");
			Assert.That(RefineryFieldGateCA.Locked(14000, 1000, 13990, 3000, 1500, 12000), Is.False, "a loss beyond the memory is forgotten");
			Assert.That(RefineryFieldGateCA.Locked(2000, -1, 1999, 3000, 1500, 12000), Is.False, "no loss, no lock");
		}

		[Test]
		public void ThePlannerBlockNeverBindsWithoutAnOwnRefineryOrWithTheRowOff()
		{
			Assert.That(RefineryFieldGateCA.Blocks(true, 0, true, true), Is.False, "no economy otherwise");
			Assert.That(RefineryFieldGateCA.Blocks(false, 3, true, true), Is.False);
			Assert.That(RefineryFieldGateCA.Blocks(true, 3, true, false), Is.True);
			Assert.That(RefineryFieldGateCA.Blocks(true, 3, false, true), Is.True);
			Assert.That(RefineryFieldGateCA.Blocks(true, 3, false, false), Is.False);
		}

		[Test]
		public void TheEconomyRungGoesRefineryWarFactoryRefinery()
		{
			Assert.That(RefineryFieldGateCA.Economy(true, 0, false, true, false, false, 100, -1, 1500, false, -1, 1500), Is.EqualTo(V.Allow), "ref1 is never gated");
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, false, false, 5000, 100, 1500, false, -1, 1500), Is.EqualTo(V.WaitFactory));
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, true, false, 5000, 100, 1500, false, -1, 1500), Is.EqualTo(V.Allow));
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, false, false, false, 5000, 100, 1500, false, -1, 1500), Is.EqualTo(V.Allow),
				"a faction that cannot build a vehicle factory yet is not held");
		}

		[Test]
		public void TheWarFactoryWaitNeverDeadlocks()
		{
			// CM FIX / Opus ruling: banned or out of placement budget, or 1500 ticks after ref1 stood; never before ref1 stood otherwise.
			Assert.That(RefineryFieldGateCA.FactoryWaitReleased(false, false, 2000, 1000, 1500), Is.False);
			Assert.That(RefineryFieldGateCA.FactoryWaitReleased(true, false, 2000, 1000, 1500), Is.True, "no-space banned");
			Assert.That(RefineryFieldGateCA.FactoryWaitReleased(false, true, 2000, 1000, 1500), Is.True, "placement budget used up");
			Assert.That(RefineryFieldGateCA.FactoryWaitReleased(false, false, 2500, 1000, 1500), Is.True, "1500 ticks after ref1");
			Assert.That(RefineryFieldGateCA.FactoryWaitReleased(false, false, 99999, -1, 1500), Is.False, "ref1 still in production");

			// A buildable but banned factory: ref2 waits for the factory while not released, and goes ahead once released.
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, false, false, 5000, 100, 1500, false, -1, 1500), Is.EqualTo(V.WaitFactory));
			var released = RefineryFieldGateCA.FactoryWaitReleased(true, false, 5000, 4900, 1500);
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, false, released, 5000, 100, 1500, false, -1, 1500), Is.EqualTo(V.Allow));

			// The interval still applies after the release.
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, false, true, 5000, 4000, 1500, false, -1, 1500), Is.EqualTo(V.WaitInterval));
		}

		[Test]
		public void TheFactoryPlacementBudgetFeedsTheReleaseAndTheHoldLogIsDebounced()
		{
			var s = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			Assert.That(s, Does.Contain("if (baseBuilder.Info.VehiclesFactoryTypes.Contains(currentBuilding.Item))\n							baseBuilder.RefineryGateFactoryPlacementFailed = true;"));
			Assert.That(s, Does.Contain("RefineryFieldGateCA.FactoryWaitReleased(b.VehiclesFactoryTypes.Any(NoSpaceBanned),"));
			Assert.That(s, Does.Contain("if (verdict != lastRefineryEconomyVerdict)"));

			// The placement-budget flag is set inside the saturation branch, after the max-fail check.
			var budget = s.IndexOf("else if (++failCount >= baseBuilder.Info.MaximumFailedPlacementAttempts)", StringComparison.Ordinal);
			var flag = s.IndexOf("baseBuilder.RefineryGateFactoryPlacementFailed = true;", StringComparison.Ordinal);
			Assert.That(budget, Is.GreaterThan(0));
			Assert.That(flag, Is.GreaterThan(budget));
		}

		[Test]
		public void RefineriesKeepTheMinimumIntervalAndYieldToFirstCopiesForABoundedTime()
		{
			Assert.That(RefineryFieldGateCA.Economy(true, 2, false, true, true, false, 1000, 100, 1500, false, -1, 1500), Is.EqualTo(V.WaitInterval));
			Assert.That(RefineryFieldGateCA.Economy(true, 2, false, true, true, false, 1600, 100, 1500, false, -1, 1500), Is.EqualTo(V.Allow));
			Assert.That(RefineryFieldGateCA.Economy(true, 2, false, true, true, false, 1600, 100, 1500, true, 1600, 1500), Is.EqualTo(V.YieldEssential));
			Assert.That(RefineryFieldGateCA.Economy(true, 2, false, true, true, false, 3200, 100, 1500, true, 1600, 1500), Is.EqualTo(V.Allow),
				"the yield ends after the yield time");
			Assert.That(RefineryFieldGateCA.Economy(true, 1, false, true, true, false, 3200, 100, 1500, true, 3200, 1500), Is.EqualTo(V.Allow),
				"ref2 does not yield (only the third refinery on)");
		}

		[Test]
		public void AYardWithoutARefinerySkipsTheEconomyGateAndTheRowOffIsUnchanged()
		{
			Assert.That(RefineryFieldGateCA.Economy(true, 3, true, true, false, false, 1000, 990, 1500, true, 1000, 1500), Is.EqualTo(V.Allow), "expansion refinery first");
			Assert.That(RefineryFieldGateCA.Economy(false, 3, false, true, false, false, 1000, 990, 1500, true, 1000, 1500), Is.EqualTo(V.Allow));
		}

		[Test]
		public void ThePlannerBlockSitsInTheSharedClaimFilterAndLossesAreTrackedOnEveryAnchorUpdate()
		{
			var s = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(s, Does.Contain("				|| anchorReservations.LiveAt(anchor, tick)\n				|| FieldGateBlocks(i, tick);"));
			Assert.That(s, Does.Contain("bool RefineryFieldGateOn => Info.RefineryFieldGate && LawActive && !ablateRefineryFieldGate.Off(player, AblationIds.RefineryFieldGate);"));
			Assert.That(s, Does.Contain("			EnsureAnchorModel(refineryCells, refineryTiles, buildingTiles, refineryDocks);\n			FieldGateTrackLosses(refineryCells, world.WorldTick);"));
			Assert.That(s, Does.Contain("a.Owner.RelationshipWith(player) == PlayerRelationship.Enemy && a.Info.HasTraitInfo<AttackBaseInfo>() && a.CanBeViewedByPlayer(player)"),
				"the threat test is fog-honest");

			// Every claim path filters through AnchorBlockedForClaims, so the claimable count and the placement claim agree.
			var paths = s.Split("i => AnchorBlockedForClaims(i, tick),").Length - 1;
			Assert.That(paths, Is.EqualTo(3));
		}

		[Test]
		public void TheEconomyGateRunsInsideTheWantedRefineryPickAfterTheCaps()
		{
			var s = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			var caps = s.IndexOf("if (baseBuilder.HasMaxRefineriesFor(refinery))", StringComparison.Ordinal);
			var gate = s.IndexOf("if (RefineryEconomyGateOn && RefineryEconomyHolds(queue, buildableThings))", StringComparison.Ordinal);
			var pick = s.IndexOf("var pick = BaseBuilderQueueEvalCA.PickOrPower(refinery, power, HasSufficientPowerForActor);", StringComparison.Ordinal);
			Assert.That(caps, Is.GreaterThan(0));
			Assert.That(gate, Is.GreaterThan(caps));
			Assert.That(pick, Is.GreaterThan(gate));
			Assert.That(s, Does.Contain("bool RefineryEconomyGateOn => baseBuilder.RefineryEconomyGateArmed && !ablateRefineryEconomyGate.Off(player, AblationIds.RefineryEconomyGate);"));
			Assert.That(s, Does.Contain("var yardWithoutRefinery = count > 0 && baseBuilder.YardWithoutRefinery();"));
		}

		[Test]
		public void BothRowsAreRegisteredAndOnlyGenericbotArmsThem()
		{
			var ids = Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.RefField02.cs");
			Assert.That(ids, Does.Contain("RefineryFieldGate = \"rf_field_gate\""));
			Assert.That(ids, Does.Contain("RefineryEconomyGate = \"rf_economy_gate\""));

			var yaml = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var planners = yaml.Where(n => n.Value.Nodes.Any(x => x.Key == "RefineryFieldGate" && x.Value.Value == "true")).ToArray();
			Assert.That(planners.Select(n => n.Key), Is.EqualTo(new[] { "ExpansionPlannerBotModule" }));

			var builders = yaml.Where(n => n.Value.Nodes.Any(x => x.Key == "RefineryEconomyGate" && x.Value.Value == "true")).ToArray();
			Assert.That(builders.Select(n => n.Key), Is.EqualTo(new[] { "BaseBuilderBotModuleCA@generic" }));
			Assert.That(builders[0].Value.Nodes.Single(x => x.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"),
				"the shared classic block is armed only for genericbot");
		}
	}
}
