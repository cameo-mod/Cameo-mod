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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// BUILD-ALL-B (switch bb_build_all_listed, user ruling 2026-11-25): S5 builds the buildable BuildingFractions entries once and nothing else (the
	// refinery included), and every S5 first copy rides with any bank above 0 while no other one is in flight. V08 seed 100775 is the case: an Ixian
	// bot placed about 30 unlisted concrete slabs and never its listed high-tech factory. The pure rule and the planner are tested directly, the world
	// wiring and the DEV rows by pins, the arm by the yaml.
	[TestFixture]
	public sealed class BuildAllListedTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		static BaseBuildSnapshot Snap(bool listedOnly, Dictionary<string, int> owned) => new()
		{
			Bank = 5000,
			Owned = owned,
			InFlight = new Dictionary<string, int>(),
			BuildingCount = 10,
			Fractions = new[] { new KeyValuePair<string, int>("hightech", 5), new KeyValuePair<string, int>("barracks", 15) },
			StageSource = stage => stage == BaseBuildStage.S5CoreOnce ? new[] { "concrete" } : null,
			CoreOnceListedOnly = listedOnly,
		};

		static List<string> S5Names(BaseBuildSnapshot s) =>
			BaseBuildPlanCA.StageCandidates(BaseBuildStage.S5CoreOnce, s, new MersenneTwister(7)).Select(c => c.Name).ToList();

		[Test]
		public void TheListedSetHasNoUnlistedFallback()
		{
			var owned = new Dictionary<string, int> { ["barracks"] = 1 };
			Assert.That(S5Names(Snap(true, owned)), Is.EqualTo(new[] { "hightech" }), "armed: the listed first copy only");
			Assert.That(S5Names(Snap(false, owned)), Is.EqualTo(new[] { "hightech", "concrete" }), "off: the roll, then the depth fallback as before");
		}

		[Test]
		public void WithEveryListedTypeOwnedS5OffersNothing()
		{
			var owned = new Dictionary<string, int> { ["barracks"] = 1, ["hightech"] = 1 };
			Assert.That(S5Names(Snap(true, owned)), Is.Empty);
		}

		[Test]
		public void AListedFirstCopyIsAPositiveWeightWithNoneCounted()
		{
			var f = new Dictionary<string, int> { ["a"] = 5, ["zero"] = 0 };
			Assert.That(BuildAllListed.ListedFirstCopy(f, "a", 0), Is.True);
			Assert.That(BuildAllListed.ListedFirstCopy(f, "a", 1), Is.False, "a second copy");
			Assert.That(BuildAllListed.ListedFirstCopy(f, "zero", 0), Is.False, "weight 0 is not built by the fractions");
			Assert.That(BuildAllListed.ListedFirstCopy(f, "unlisted", 0), Is.False);
			Assert.That(BuildAllListed.ListedFirstCopy(null, "a", 0), Is.False);
		}

		[Test]
		public void OnlyAnS5StartWithABankAboveZeroAndNothingInFlightRides()
		{
			Assert.That(BuildAllListed.Waives(BaseBuildStage.S5CoreOnce, 1, 0), Is.True);
			Assert.That(BuildAllListed.Waives(BaseBuildStage.S5CoreOnce, 0, 0), Is.False, "bank 0");
			Assert.That(BuildAllListed.Waives(BaseBuildStage.S5CoreOnce, 5000, 1), Is.False, "another first copy in flight");
			foreach (var stage in Enum.GetValues<BaseBuildStage>().Where(s => s != BaseBuildStage.S5CoreOnce))
				Assert.That(BuildAllListed.Waives(stage, 5000, 0), Is.False, stage.ToString());
		}

		[Test]
		public void AnItemInFlightInAnotherQueueDoesNotSuppressTheWaiver()
		{
			var fractions = new Dictionary<string, int> { ["tech"] = 10, ["radar"] = 10 };
			var inQueueB = new[] { "radar" };
			var inQueueA = Array.Empty<string>();
			int CountOf(string n) => n == "radar" ? 1 : 0; // the global in-flight map holds radar (queue B)
			Assert.That(BuildAllListed.FirstCopiesInQueue(inQueueA, fractions, CountOf), Is.EqualTo(0), "queue A sees nothing in flight");
			Assert.That(BuildAllListed.Waives(BaseBuildStage.S5CoreOnce, 100, BuildAllListed.FirstCopiesInQueue(inQueueA, fractions, CountOf)), Is.True);
			Assert.That(BuildAllListed.FirstCopiesInQueue(inQueueB, fractions, CountOf), Is.EqualTo(1), "queue B counts its own first copy");
			Assert.That(BuildAllListed.Waives(BaseBuildStage.S5CoreOnce, 100, BuildAllListed.FirstCopiesInQueue(inQueueB, fractions, CountOf)), Is.False);
			Assert.That(BuildAllListed.FirstCopiesInQueue(new[] { "radar" }, fractions, n => 3), Is.EqualTo(0), "a type that already stands is a second copy");
		}

		[Test]
		public void TheWorldWiringIsBehindTheSwitch()
		{
			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			Assert.That(p1, Does.Contain("CoreOnceListedOnly = baseBuilder.BuildAllListedArmed,"));
			Assert.That(p1, Does.Contain("var listedWaived = baseBuilder.BuildAllListedArmed && BuildAllListed.Waives(c.Stage, s.Bank, ListedFirstCopiesInFlight(queue, s));"));
			Assert.That(p1, Does.Contain("&& !(baseBuilder.BuildAllListedArmed && baseBuilder.RefineryCountWithQueued() <= 0))"), "A2: the last refinery is not held by the anchor max");

			var defence = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Defence.cs");
			Assert.That(defence, Does.Contain("&& (baseBuilder.BuildAllListedArmed\n\t\t\t\t\t\t\t|| (!baseBuilder.Info.RefineryTypes.Contains(candidate.Name)"), "S5 admits a listed refinery");
		}

		[Test]
		public void ThePlannerRowsAreDevOnly()
		{
			var p1 = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01.cs");
			foreach (var method in new[] { "void PickObserved(", "void RefineryHeldObserved(" })
			{
				var at = p1.IndexOf(method, StringComparison.Ordinal);
				Assert.That(at, Is.GreaterThan(0), method);
				Assert.That(p1.LastIndexOf("[System.Diagnostics.Conditional(\"CAMEO_DEV_LOGGING\")]", at, StringComparison.Ordinal), Is.InRange(at - 80, at - 1), method);
			}

			var stages = Source(Dir + "BaseBuilderQueueManagerCA.Pipeline01Stages.cs");
			Assert.That(stages.Split("RefineryHeldObserved(RefineryHoldGate(buildable));").Length - 1, Is.EqualTo(2), "S2 and S7 record the holding gate");
			foreach (var gate in new[] { "adequate", "unsafe_all", "unsafe_field", "not_buildable", "max_refineries" })
				Assert.That(stages, Does.Contain("return \"" + gate), gate);
			Assert.That(stages, Does.Contain("? \"economy_\" + lastRefineryEconomyVerdict"), "the economy verdict of the call just made");
		}

		[Test]
		public void TheCoverageRowsAreWritten()
		{
			var dev = Source("mods/Cameo.Dev/AiPlayerTelemetryDev.Coverage.cs");
			foreach (var kind in new[] { "bb_listed_set", "ub_listed_set", "bb_type_available", "ub_type_available", "harvester_lost", "bb_pick", "bb_a6_hold", "bb_refinery_held" })
				Assert.That(dev, Does.Contain("\"" + kind + "\""), kind);
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegistered()
		{
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "BbBuildAllListed"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "BbBuildAllListed"), Is.Null);
			Assert.That(AblationIds.BbBuildAllListed, Is.EqualTo("bb_build_all_listed"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.BbBuildAllListed));
		}

		[Test]
		public void TheSwitchIsOffByDefault()
		{
			Assert.That(new BaseBuilderBotModuleCAInfo().BbBuildAllListed, Is.False);
			Assert.That(new BaseBuildSnapshot().CoreOnceListedOnly, Is.False);
		}
	}
}
