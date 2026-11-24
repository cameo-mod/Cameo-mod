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

namespace OpenRA.Mods.Cameo.Test
{
	// BASE-FIX-02 (AUDIT_BASE_01, Opus 11-24): READY watchdog for every building (BASE-06), per-type no-space ban (BASE-07), the refinery safety test over every claim and the
	// fraction roll (BASE-12), the siteless-refinery hold cap (BASE-16). Each row is genericbot only.
	[TestFixture]
	public sealed class BaseFix02Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Queue() => Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");

		[Test]
		public void AHoldExpiresOnlyWhenArmedAndLongEnough()
		{
			Assert.That(BaseFix02CA.HoldExpired(true, 1075, 1000, 75), Is.True);
			Assert.That(BaseFix02CA.HoldExpired(true, 1074, 1000, 75), Is.False);
			Assert.That(BaseFix02CA.HoldExpired(false, 9999, 0, 75), Is.False, "row off = never");
			Assert.That(BaseFix02CA.HoldExpired(true, 9999, -1, 75), Is.False, "no hold started");
		}

		[Test]
		public void AHoldNotSeenForALongTimeStartsOver()
		{
			Assert.That(BaseFix02CA.TouchHold(false, 0, 0, 500, 300), Is.EqualTo((500, 500)));
			Assert.That(BaseFix02CA.TouchHold(true, 500, 560, 600, 300), Is.EqualTo((500, 600)), "continuous hold keeps its start");
			Assert.That(BaseFix02CA.TouchHold(true, 500, 560, 1000, 300), Is.EqualTo((1000, 1000)), "a stale hold restarts");
		}

		[Test]
		public void TheNoSpaceBanLastsUntilItsTickOrANewBaseProvider()
		{
			Assert.That(BaseFix02CA.BanActive(true, 1000, 4000, 3, 3), Is.True);
			Assert.That(BaseFix02CA.BanActive(true, 4000, 4000, 3, 3), Is.False, "ended");
			Assert.That(BaseFix02CA.BanActive(true, 1000, 4000, 4, 3), Is.False, "a new base provider lifts it");
			Assert.That(BaseFix02CA.BanActive(false, 1000, 4000, 3, 3), Is.False, "row off");
		}

		[Test]
		public void TheSafetyBlocksOnlyWhenEveryClaimIsUnsafe()
		{
			Assert.That(BaseFix02CA.SafetyBlocksAll(Array.Empty<bool>()), Is.False, "no claim, no gate");
			Assert.That(BaseFix02CA.SafetyBlocksAll(new[] { false, false }), Is.True);
			Assert.That(BaseFix02CA.SafetyBlocksAll(new[] { false, true }), Is.False, "an unsafe head claim no longer blocks the safe one");
		}

		[Test]
		public void TheWatchdogCoversTheCrawlAndFrontBackHoldsAndAPlacementClearsIt()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("if (ReadyWatchdogOn && BaseFix02CA.HoldExpired(true, world.WorldTick, world.WorldTick - ReadyHeldTicks(queue, currentBuilding.Item), baseBuilder.Info.ReadyWatchdogTicks))"));
			Assert.That(q, Does.Contain("type = BuildingType.Building;"));
			Assert.That(q, Does.Contain("if (frontBackHold && !(ReadyWatchdogOn && BaseFix02CA.HoldExpired("));
			Assert.That(q, Does.Contain("readyHoldSince.Remove(queue.Actor.ActorID);\n\t\t\t\t\tNotifyPlacement("));
		}

		[Test]
		public void TheBanIsRecordedAtSaturationAndFiltersEveryExtrasRung()
		{
			var q = Queue();
			Assert.That(q, Does.Contain("noSpaceBan[currentBuilding.Item] = (world.WorldTick + baseBuilder.Info.NoSpaceBanTicks"));
			Assert.That(q, Does.Contain("if (!baseBuilder.Info.ProductionTypes.Contains(name) || NoSpaceBanned(name))"), "overflow + cycled candidates");
			Assert.That(q, Does.Contain("|| OwnedOrQueued(item) || NoSpaceBanned(item.Name))"), "each building once / tech chain");
			Assert.That(q, Does.Contain("if (NoSpaceBanned(name))\n\t\t\t\t\tcontinue;"), "the fraction roll");
			Assert.That(q, Does.Contain("|| BuildingStillDelayed(item.Name) || NoSpaceBanned(item.Name))"), "the opening");
		}

		[Test]
		public void TheRefineryCapSitsBeforeTheRetainedHoldAndTheSafetyCoversTheRoll()
		{
			var q = Queue();
			var cap = q.IndexOf("if (RefineryHoldCapOn && BaseFix02CA.HoldExpired(", StringComparison.Ordinal);
			Assert.That(cap, Is.GreaterThan(0));
			Assert.That(cap, Is.LessThan(q.IndexOf("// REF-2 P1a (retained-refinery): a completed refinery is NEVER cancelled", StringComparison.Ordinal)));
			Assert.That(q, Does.Contain("if (SafetyOn && SafetyAllOn && baseBuilder.Info.RefineryTypes.Contains(name) && baseBuilder.ExpansionRefineryBlockedByAllUnsafe())"));
			Assert.That(q, Does.Contain("if (baseBuilder.ExpansionRefineryBlockedByAllUnsafe())\n\t\t\t\t\t\treturn null;"));
		}

		[Test]
		public void EveryRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			var ids = Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.BaseFix02.cs");
			foreach (var id in new[] { "ready_watchdog_all", "pb_nospace_ban", "pb_refinery_safety_all_claims", "refinery_hold_cap" })
				Assert.That(ids, Does.Contain($"\"{id}\""), id);

			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var generic = player.Single(n => n.Key == "BaseBuilderBotModuleCA@generic").Value.Nodes;
			Assert.That(generic.Single(n => n.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
			foreach (var key in new[] { "ReadyWatchdogAll", "NoSpaceBan", "RefinerySafetyAllClaims", "RefineryHoldCap" })
				Assert.That(generic.Single(n => n.Key == key).Value.Value, Is.EqualTo("true"), key);

			foreach (var o in player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA", StringComparison.Ordinal) && n.Key != "BaseBuilderBotModuleCA@generic"))
				Assert.That(o.Value.Nodes.Any(n => n.Key is "ReadyWatchdogAll" or "NoSpaceBan" or "RefinerySafetyAllClaims" or "RefineryHoldCap"), Is.False, o.Key);
		}
	}
}
