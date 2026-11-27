#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as
 * published by the Free Software Foundation, version 3 or later.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class RefineryLearnTest
	{
		static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static void Set(object obj, string name, object value)
		{
			for (var t = obj.GetType(); t != null; t = t.BaseType)
				if (t.GetField(name, Fields | BindingFlags.DeclaredOnly) is FieldInfo f)
				{
					f.SetValue(obj, value);
					return;
				}

			throw new MissingFieldException(obj.GetType().FullName, name);
		}

		sealed class TestQueue : ProductionQueue
		{
			public TestQueue(ActorInitializer init, ProductionQueueInfo info) : base(init, info) { }
			public void Add(ProductionItem item) => Queue.Add(item);
			public void Cancel(string name) => CancelProduction(name, 1);
		}

		// Exercise the same engine cancellation and refund used by Order.CancelProduction after the certificate is accepted.
		public static void AssertEngineCancellationRefunds()
		{
			var owner = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
			var faction = new FactionInfo();
			Set(faction, nameof(FactionInfo.InternalName), "test");
			Set(owner, "Faction", faction);
			var queueInfo = new ProductionQueueInfo();
			Set(queueInfo, nameof(ProductionQueueInfo.Type), "Building");
			var buildable = new BuildableInfo();
			Set(buildable, nameof(BuildableInfo.Queue), new[] { "Building" }.ToFrozenSet());
			var info = new ActorInfo("refinery", buildable);
			var rules = new Ruleset(new Dictionary<string, ActorInfo> { ["refinery"] = info },
				new Dictionary<string, OpenRA.GameRules.WeaponInfo>(), null, null, null, null, null, null);
			var map = (Map)RuntimeHelpers.GetUninitializedObject(typeof(Map));
			typeof(Map).GetProperty(nameof(Map.Rules)).SetValue(map, rules);
			var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
			Set(world, "Map", map);
			var actor = (Actor)RuntimeHelpers.GetUninitializedObject(typeof(Actor));
			Set(actor, "World", world);
			typeof(Actor).GetProperty(nameof(Actor.Owner)).SetValue(actor, owner);
			var queue = new TestQueue(new ActorInitializer(actor, new TypeDictionary()), queueInfo);
			var resources = (PlayerResources)RuntimeHelpers.GetUninitializedObject(typeof(PlayerResources));
			resources.Cash = 50;
			resources.Resources = 5;
			resources.ResourceCapacity = 100;
			Set(queue, "playerResources", resources);
			var item = new ProductionItem(queue, "refinery", 100, null, null) { Started = true, ResourcesPaid = 10, RemainingCost = 40 };
			queue.Add(item);
			queue.Cancel(item.Item);
			Assert.That(queue.AllQueued(), Is.Empty);
			Assert.That(resources.Resources, Is.EqualTo(15));
			Assert.That(resources.Cash, Is.EqualTo(100));
		}
		sealed class Knobs : IBotLearnedKnobs
		{
			readonly int value;
			public Knobs(int value) { this.value = value; }
			public bool TryResolvePermille(string knob, out int permille) { permille = value; return true; }
		}

		[TestCase("refinery.zone_cap", 2, 1, 3)]
		[TestCase("refinery.spacing", 10, 6, 14)]
		[TestCase("refinery.target_t1", 2, 0, 4)]
		[TestCase("refinery.target_t2", 4, 2, 6)]
		[TestCase("refinery.target_t3", 6, 4, 8)]
		public void PriorsAndExtremeLearnedEstimatesStayWithinBounds(string knob, int prior, int min, int max)
		{
			Assert.That(RefineryLearn.Resolve(null, knob, prior, min, max), Is.EqualTo(prior));
			Assert.That(RefineryLearn.Resolve(new Knobs(1000), knob, prior, min, max), Is.EqualTo(prior));
			Assert.That(RefineryLearn.Resolve(new Knobs(0), knob, prior, min, max), Is.EqualTo(min));
			Assert.That(RefineryLearn.Resolve(new Knobs(int.MaxValue), knob, prior, min, max), Is.EqualTo(max));
		}

		[Test]
		public void TwoRefineriesFillAZoneAndDoNotFillTheNeighbour()
		{
			var anchors = new[] { new CPos(0, 0), new CPos(30, 0) };
			var counts = new int[2];
			foreach (var site in new[] { new CPos(0, 1), new CPos(5, 0) })
				counts[RefineryLearn.NearestZone(site, anchors)]++;

			Assert.That(RefineryDemand.Classify(true, true, false, counts[0], 2), Is.EqualTo(RefineryDemand.Verdict.ZoneCap));
			Assert.That(RefineryDemand.Classify(true, true, false, counts[1], 2), Is.EqualTo(RefineryDemand.Verdict.Demand));
			Assert.That(RefineryLearn.NearestZone(new CPos(15, 0), anchors), Is.EqualTo(0));
		}

		static string Source(string file) => System.IO.File.ReadAllText(System.IO.Path.Combine(System.AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void TheLearnedCapReplacesTheStaticCapButKeepsTheEcoFloorAndTheRowIsOffAndAblatable()
		{
			var rd = Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs");
			Assert.That(rd, Does.Contain("RefineryDemand.ZoneCapFor(RefLearnArmed ? LearnedZoneCap : Info.RefineryPerZoneCap,"), "armed or not, the small-zone cap of 1 goes through ZoneCapFor (safety rule kept)");
			Assert.That(rd, Does.Contain("ZoneStandingCount(z) >= ZoneCapAt(z) + EcoFloorLift()"), "the spacing refusal uses the cap the classification and the field gate use, lift included (CM FIX 2)");
			var standing = rd.Substring(rd.IndexOf("int ZoneStandingCount(int zone)", System.StringComparison.Ordinal));
			Assert.That(standing.IndexOf("zoneAnchors == null", System.StringComparison.Ordinal), Is.InRange(0, standing.IndexOf("zoneAnchors.Count", System.StringComparison.Ordinal) - 1), "ZoneStandingCount guards a missing zone model before reading it (CM FIX 1)");
			Assert.That(Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.RefDemand.cs"), Does.Contain("RefineryLearnTierBase(int tier)").And.Contain("tier * 2"), "the tier prior is the 2/4/6 baseline (user D4)");
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("refineryLaw.RefineryLearnTierBase(RefTierTechStanding ? 3 : radar ? 2 : 1)").And.Contain("refTierValue = RefTier.Target(tierBase, RefTierPersonalityPct(radar));"), "the learned value replaces the BASE only; the personality percent and the clamp stay on top");
			Assert.That(rd, Does.Contain("bool RefLearnArmed => Info.RefLearn && RefDemandArmed && !ablateRefLearn.Off(player, AblationIds.RefLearn);"));
			Assert.That(rd, Does.Contain("ZoneCapAt(z) + EcoFloorLift()"), "the ECO-TIMING-FLOOR lift stays on top of the learned cap");
			Assert.That(Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs"), Does.Contain("ZoneCapAt(field) + EcoFloorLift()"));
			Assert.That(Source("packaging/ai.yaml"), Does.Contain("RefLearn: false"), "off until the A/B arms it");
		}

		[Test]
		public void ASmallZoneKeepsItsCapOfOneWhateverIsLearned()
		{
			Assert.That(RefineryDemand.ZoneCapFor(3, 10, 40), Is.EqualTo(1), "fewer cells than RefinerySecondPerZoneMinCells = cap 1, learned or not");
			Assert.That(RefineryDemand.ZoneCapFor(3, 80, 40), Is.EqualTo(3), "a big zone takes the learned cap");
			Assert.That(RefineryDemand.ZoneCapFor(1, 80, 40), Is.EqualTo(1));
		}

		[Test]
		public void RelaxedPriorAndOffRowPreserveThePreviousGate()
		{
			Assert.That(RefineryLearn.RelaxedSpacing(10), Is.EqualTo(5));
			Assert.That(RefineryLearn.RequireTwoVerdicts(false, true), Is.True);
			Assert.That(RefineryLearn.RequireTwoVerdicts(false, false), Is.False);
			Assert.That(new ExpansionPlannerBotModuleInfo().RefLearn, Is.False);
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("ref_learn"));
		}
	}
}
