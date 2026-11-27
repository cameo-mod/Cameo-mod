#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// B4 OBSERVED-STYLE phase b (DESIGN_OBSERVED_STYLE_02 section 1 pins, ADD1-ADD4 unchanged): the typed fog-only input, the single adapter, the tracker, the feature extractor and the DEV row.
	// No behaviour: nothing here reads the classifier (phase d).
	// OBS-STYLE-PROVIDER-01: the B4 phase b tests (f14b8dac6 ObservedStyleB4Test) that still apply; the wiring file may read the public WinState (liveness metadata, design 03).
	[TestFixture]
	public sealed class ObservedStyleProviderFogTest
	{
		const string Dir = "OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/";
		static readonly string[] PureFiles = { "FogStyleActor.cs", "StyleTracker.cs", "StyleFeatures.cs" };
		const string WiringFile = "ObservedStyleBotModule.cs";

		static readonly string[] Allowlist =
		{
			"ActorId", "TypeName", "Cell", "LastSeenTick", "Value",
			"Building", "BaseBuilding", "Combat", "Defence", "Tech", "Production", "Refinery", "Harvester", "Mcv", "Aircraft", "ProducesAircraft", "AntiAir", "Infantry", "Vehicle", "Naval", "Cloaked",
		};

		static string Source(string file)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, file)).Replace("\r\n", "\n");
		}

		// Comments may name the forbidden things (they explain them); the scan reads code only.
		static string Code(string file) => Regex.Replace(Regex.Replace(Source(Dir + file), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\n]*", string.Empty);

		[Test]
		public void TheFogStyleActorFieldSetIsTheExactAllowlist()
		{
			var t = typeof(FogStyleActor);
			Assert.That(t.IsValueType, Is.True);
			Assert.That(t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance), Is.Empty, "fields only");
			var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Select(f => f.Name).OrderBy(x => x, StringComparer.Ordinal).ToArray();
			Assert.That(fields, Is.EqualTo(Allowlist.OrderBy(x => x, StringComparer.Ordinal).ToArray()),
				"adding or removing a field of FogStyleActor changes what the classifier may see: review it against DESIGN_OBSERVED_STYLE_02 section 1, then update this list");
		}

		[Test]
		public void TheStyleFilesCannotReachTheForbiddenInputs()
		{
			string[] forbidden =
			{
				"EnemyProfile", "WinState", ".Alive", "IBotCurrentPersonality", "BotPersonalityController", "PinnedPersonalities", "TraitsImplementing<",
				"World.Actors", "FrozenActorLayer", ".Owner.PlayerName", "FactionName", "Faction.InternalName", "Player.Name",
			};

			foreach (var file in PureFiles)
			{
				var code = Code(file);
				foreach (var f in forbidden)
					Assert.That(code, Does.Not.Contain(f), $"{file} must not contain '{f}'");

				Assert.That(Regex.IsMatch(code, @"(?<![A-Za-z0-9_.])Actor\s+[a-z_]\w*\s*[,)]"), Is.False, $"{file}: a plain Actor must not be a parameter");
			}

			// The wiring file may look up the DEV observers and its own yards, but reaches nothing else of the above.
			var wiring = Code(WiringFile);
			foreach (var f in forbidden.Where(x => x != "TraitsImplementing<" && x != "WinState"))
				Assert.That(wiring, Does.Not.Contain(f), $"{WiringFile} must not contain '{f}'");
		}

		[Test]
		public void NoFeatureOrTrackerReadsTheTypeName()
		{
			Assert.That(Code("StyleFeatures.cs"), Does.Not.Contain("TypeName"));
			Assert.That(Code("StyleTracker.cs"), Does.Not.Contain("TypeName"));
		}

		static FogStyleActor Building(uint id, int x, int y, int value, int seen) => new FogStyleActor { ActorId = id, Cell = new CPos(x, y), Value = value, LastSeenTick = seen, Building = true };

		static FogStyleActor Unit(uint id, int x, int y, int value, int seen) => new FogStyleActor { ActorId = id, Cell = new CPos(x, y), Value = value, LastSeenTick = seen, Combat = true, Infantry = true };

		static FogStyleActor[] BaseAt(int tick, string typeName)
		{
			var yard = Building(1, 50, 50, 2500, tick); yard.BaseBuilding = true;
			var refinery = Building(2, 52, 52, 2000, tick); refinery.Refinery = true;
			var barracks = Building(3, 55, 50, 500, tick); barracks.Production = true;
			var radar = Building(4, 50, 55, 1500, tick); radar.Tech = true;
			var turret = Building(5, 58, 58, 600, tick); turret.Defence = true;
			var harvester = new FogStyleActor { ActorId = 6, Cell = new CPos(53, 53), Value = 1400, LastSeenTick = tick, Harvester = true };
			var all = new[] { yard, refinery, barracks, radar, turret, harvester };
			for (var i = 0; i < all.Length; i++)
				all[i].TypeName = typeName + i;

			return all;
		}

		static int Index(string name) => Array.IndexOf(StyleFeatures.Names, name);

		static int[] Run(Func<int, (FogStyleActor[] Actors, int Tick)> script, int steps, CPos[] ownYards, string typeName, out StyleTracker tracker)
		{
			tracker = new StyleTracker();
			var features = new int[StyleFeatures.Count];
			for (var s = 0; s < steps; s++)
			{
				var (actors, tick) = script(s);
				tracker.Update(actors, ownYards, tick);
				StyleFeatures.Extract(actors, tick, tracker, features);
			}

			return features;
		}

		[Test]
		public void NothingSeenIsUnknownNeverZero()
		{
			var tracker = new StyleTracker();
			var f = new int[StyleFeatures.Count];
			tracker.Update(ReadOnlySpan<FogStyleActor>.Empty, new[] { new CPos(200, 200) }, 3000);
			StyleFeatures.Extract(ReadOnlySpan<FogStyleActor>.Empty, 3000, tracker, f);
			Assert.That(f, Is.All.EqualTo(StyleFeatures.Unknown));
			Assert.That(StyleFeatures.Names.Length, Is.EqualTo(30));
			Assert.That(StyleFeatures.Names.Distinct().Count(), Is.EqualTo(30));
		}

		[Test]
		public void AScriptedBaseSightingGivesTheBucketsOfTheDesign()
		{
			var own = new[] { new CPos(200, 200) };
			var f = Run(s => (BaseAt(2000, "x").Concat(new[] { Unit(10, 51, 51, 100, 2000), Unit(11, 52, 51, 100, 2000), Unit(12, 51, 52, 100, 2000) }).ToArray(), 2000), 1, own, "x", out _);

			Assert.That(f[Index("f4_yards")], Is.EqualTo(1));
			Assert.That(f[Index("f4_refineries")], Is.EqualTo(1));
			Assert.That(f[Index("f4_clusters")], Is.EqualTo(1));
			Assert.That(f[Index("f4_mcvs")], Is.Zero, "a base was seen and no MCV: zero, not unknown");
			Assert.That(f[Index("f3_prod_per_tech")], Is.EqualTo(9), "1 production over 1 tech-only building = 500 permille per tech+1, bucket 9");
			Assert.That(f[Index("f3_first_tech_tick")], Is.EqualTo(4));
			Assert.That(f[Index("f3_first_prod_tick")], Is.EqualTo(4));
			Assert.That(f[Index("f4_second_yard_tick")], Is.EqualTo(StyleFeatures.Unknown));
			Assert.That(f[Index("f5_defence_share")], Is.EqualTo(1));
			Assert.That(f[Index("f5_defence_count")], Is.EqualTo(1));
			Assert.That(f[Index("f6_max_tech_value")], Is.EqualTo(5));
			Assert.That(f[Index("f7_inf_share")], Is.EqualTo(8));
			Assert.That(f[Index("f7_veh_share")], Is.Zero);
			Assert.That(f[Index("f7_mean_unit_value")], Is.EqualTo(4));
			Assert.That(f[Index("f9_harv_per_ref")], Is.EqualTo(4));
			Assert.That(f[Index("f9_ref_per_yard")], Is.EqualTo(2));
			Assert.That(f[Index("f10_coverage")], Is.EqualTo(10));
			Assert.That(f[Index("f10_since_building")], Is.Zero);
			Assert.That(f[Index("f1_first_out_tick")], Is.EqualTo(StyleFeatures.Unknown), "the units are inside the base");
			Assert.That(f[Index("f2_army_pm_m3")], Is.EqualTo(StyleFeatures.Unknown), "before the minute-3 checkpoint");
			Assert.That(f[Index("f8_groups_3000")], Is.Zero, "own yards known, nothing in contact: zero groups");
		}

		[Test]
		public void TheTrackerFollowsOutsideContactGroupsAndDisappearance()
		{
			var own = new[] { new CPos(200, 200) };
			var tracker = new StyleTracker();
			var f = new int[StyleFeatures.Count];

			void Step(int tick, params FogStyleActor[] units)
			{
				var actors = BaseAt(tick, "t").Concat(units).ToArray();
				tracker.Update(actors, own, tick);
				StyleFeatures.Extract(actors, tick, tracker, f);
			}

			Step(2000, Unit(10, 51, 51, 100, 2000), Unit(11, 52, 51, 100, 2000), Unit(12, 51, 52, 100, 2000));

			// minute-3 checkpoint (tick 4500) and the first sighting outside the base, far from the viewer
			Step(4600, Unit(10, 90, 90, 100, 4600), Unit(11, 91, 90, 100, 4600), Unit(12, 90, 91, 100, 4600));
			Assert.That(tracker.FirstOutsideTick, Is.EqualTo(4600));
			Assert.That(f[Index("f1_first_out_tick")], Is.EqualTo(5));
			Assert.That(f[Index("f1_first_out_value")], Is.EqualTo(4));
			Assert.That(tracker.ArmyAtCheckpoint[0], Is.EqualTo(300));
			Assert.That(f[Index("f2_army_pm_m3")], Is.EqualTo(7), "300 per 3 minutes = 100, bucket 7");
			Assert.That(f[Index("f2_army_pm_m6")], Is.EqualTo(StyleFeatures.Unknown));

			// the same three units reach the viewer's base together: ONE group of three
			Step(4700, Unit(10, 180, 190, 100, 4700), Unit(11, 181, 190, 100, 4700), Unit(12, 180, 191, 100, 4700));
			tracker.Groups(4700, out var count, out var median, out var max);
			Assert.That((count, median, max), Is.EqualTo((1, 3, 3)));
			Assert.That(f[Index("f8_groups_3000")], Is.EqualTo(1));
			Assert.That(f[Index("f8_group_median")], Is.EqualTo(2));
			Assert.That(f[Index("f8_group_max")], Is.EqualTo(2));

			// they stay in contact (no new group) and then leave the table: they were in contact, so not "gone without contact"
			Step(4775, Unit(10, 181, 190, 100, 4775), Unit(11, 182, 190, 100, 4775), Unit(12, 181, 191, 100, 4775));
			Step(4850);
			Assert.That(tracker.SeenThenGone, Is.Zero);
			tracker.Groups(4850, out count, out _, out _);
			Assert.That(count, Is.EqualTo(1));

			// a unit seen once out in the field and then gone without ever reaching the viewer
			Step(4925, Unit(20, 120, 120, 300, 4925));
			Step(5000);
			Assert.That(tracker.SeenThenGone, Is.EqualTo(1));
			Assert.That(f[Index("f8_gone_count")], Is.EqualTo(1));

			// two groups of different place in one update are two events; the window forgets old ones
			Step(5100, Unit(30, 175, 190, 100, 5100), Unit(31, 177, 190, 100, 5100), Unit(32, 225, 210, 100, 5100));
			tracker.Groups(5100, out count, out median, out max);
			Assert.That(count, Is.EqualTo(3), "the earlier group of three plus the new group of two and the single unit");
			tracker.Groups(5100 + StyleTracker.GroupWindowTicks + 1, out count, out _, out _);
			Assert.That(count, Is.Zero);
		}

		[Test]
		public void ASecondYardIsRecordedOnceAndTheCountsAreTheSightings()
		{
			var tracker = new StyleTracker();
			var a = BaseAt(1000, "y");
			tracker.Update(a, ReadOnlySpan<CPos>.Empty, 1000);
			Assert.That(tracker.SecondYardTick, Is.EqualTo(-1));
			var second = Building(99, 120, 120, 2500, 1500);
			second.BaseBuilding = true;
			var b = BaseAt(1500, "y").Concat(new[] { second }).ToArray();
			tracker.Update(b, ReadOnlySpan<CPos>.Empty, 1500);
			Assert.That(tracker.SecondYardTick, Is.EqualTo(1500));
			tracker.Update(b, ReadOnlySpan<CPos>.Empty, 1600);
			Assert.That(tracker.SecondYardTick, Is.EqualTo(1500), "recorded once");
			var f = new int[StyleFeatures.Count];
			StyleFeatures.Extract(b, 1600, tracker, f);
			Assert.That(f[Index("f4_yards")], Is.EqualTo(2));
			Assert.That(f[Index("f4_clusters")], Is.EqualTo(2), "the second yard is farther than 24 cells from the first cluster");
			Assert.That(f[Index("f8_groups_3000")], Is.EqualTo(StyleFeatures.Unknown), "no own yard was ever given: contact is not measurable, so unknown");
		}

		[Test]
		public void TheFeaturesAreDeterministicAndIgnoreTheTypeNames()
		{
			var own = new[] { new CPos(200, 200) };
			(FogStyleActor[], int) Script(int step, string names) => (BaseAt(2000 + step * 100, names).Concat(new[] { Unit(10, 90 + step, 90, 100, 2000 + step * 100) }).ToArray(), 2000 + step * 100);

			var a1 = Run(s => Script(s, "alpha"), 6, own, "alpha", out _);
			var a2 = Run(s => Script(s, "alpha"), 6, own, "alpha", out _);
			var b = Run(s => Script(s, "something-else-entirely"), 6, own, "other", out _);
			Assert.That(a2, Is.EqualTo(a1));
			Assert.That(b, Is.EqualTo(a1), "renaming every actor type must leave the reading bit-identical");
		}

		[Test]
		public void BucketingIsLogTwoCappedAndZeroForNothing()
		{
			Assert.That(StyleFeatures.Bucket(-5), Is.Zero);
			Assert.That(StyleFeatures.Bucket(0), Is.Zero);
			Assert.That(StyleFeatures.Bucket(1), Is.EqualTo(1));
			Assert.That(StyleFeatures.Bucket(2), Is.EqualTo(2));
			Assert.That(StyleFeatures.Bucket(3), Is.EqualTo(2));
			Assert.That(StyleFeatures.Bucket(4), Is.EqualTo(3));
			Assert.That(StyleFeatures.Bucket(1L << 40), Is.EqualTo(15));
		}
	}
}
