#region Copyright & License Information
/*
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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// OBS-STYLE-PROVIDER-01 (design 03 02f2f333; Opus open items 11-27): the switch and the ablation row, the Release inference, the apparent-only
	/// projection (disguise and mirage), the bounded recency copy and the identity-keyed reading of the main target.
	/// </summary>
	[TestFixture]
	public sealed class ObservedStyleProviderTest
	{
		const string Dir = "OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Code(string file) => Regex.Replace(Regex.Replace(RepoFile(file), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\n]*", string.Empty);

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		static ObservedActor Seen(uint id, int tick) =>
			new() { ActorID = id, LastSeenTick = tick, StyleAppearance = new FogStyleActor { ActorId = id, LastSeenTick = tick, Value = 100, Combat = true, Vehicle = true } };

		[Test]
		public void TheSwitchIsOffByDefaultAndTheRowHasItsOwnAblationId()
		{
			Assert.That(AblationIds.ObservedStyleProvider, Is.EqualTo("obs_style_provider"));
			Assert.That(Regex.IsMatch(AblationIds.ObservedStyleProvider, "^[a-z][a-z0-9]*(_[a-z0-9]+)+$"), Is.True, "safe in the lobby list");

			var yaml = RepoFile("mods/cameo/ai/ai.yaml");
			Assert.That(yaml, Does.Contain("\tGrantConditionOnBotOwner@observedstyleprovider:\n\t\tCondition: obs_style_provider\n\t\tBots:\n\t"), "no bot named: off until an A/B arms it");
			Assert.That(yaml, Does.Contain("\tObservedStyleBotModule:\n\t\tRequiresCondition: genericbot && obs_style_provider\n"));
			Assert.That(Count(yaml, "ObservedStyleBotModule:"), Is.EqualTo(1));
			Assert.That(yaml, Does.Contain("\t\tLearnedFile: ai/learned/learn_all.yaml\n\t\tRefreshTicks: 250\n"), "the LearnAll refresh stays LearnAll's");

			var module = Code(Dir + "ObservedStyleBotModule.cs");
			Assert.That(module, Does.Contain("internal bool CollectAppearance => !IsTraitDisabled && !ablate.Off(viewer, AblationIds.ObservedStyleProvider);"));
			Assert.That(Count(module, "CollectAppearance"), Is.GreaterThanOrEqualTo(3), "the reading, the tick and the fog projection all ask the one switch");
			Assert.That(Code("OpenRA.Mods.Cameo/Traits/BotModules/BotFogMemory.cs"), Does.Contain("var recordStyle = styleObserver?.CollectAppearance == true;"),
				"off: the fog memory projects nothing");
		}

		[Test]
		public void ReleaseInfersAndNothingIsTrainedWrittenOrLogged()
		{
			foreach (var file in new[] { "ObservedStyleBotModule.cs", "ObservedStyleQualification.cs", "FogStyleActor.cs", "StyleTracker.cs", "StyleFeatures.cs", "StyleClassifier.cs" })
			{
				var code = Code(Dir + file);
				Assert.That(code, Does.Not.Contain("CAMEO_DEV_LOGGING"), file + ": the inference is the same code in Release and DEV");
				foreach (var banned in new[] { "File.", "StreamWriter", "Diagnostic", "Telemetry", "Log.Write", "Console." })
					Assert.That(code, Does.Not.Contain(banned), file + ": no writes or diagnostic emissions: " + banned);
			}

			Assert.That(Code(Dir + "ObservedStyleBotModule.cs"), Does.Contain("new(StyleTable.HandPrior)"), "the accepted immutable prior table; no fit at runtime");
		}

		[Test]
		public void TheProjectionIsTheApparentOneAndAMirageGivesNoEvidence()
		{
			var module = Code(Dir + "ObservedStyleBotModule.cs");
			Assert.That(module, Does.Contain("var effective = actor.EffectiveOwner;\n\t\t\tif (effective?.Disguised != true)\n\t\t\t\treturn actor.Owner == enemy ? PublicImage(actor.Info) : null;"));
			Assert.That(module, Does.Contain("var apparent = EffectiveAppearance(enemy, effective.Owner, disguise?.AsActor);"));

			// A mirage is disguised with the Neutral player as its effective owner: never the enemy, so it is not featured at all.
			var mirage = Code("OpenRA.Mods.CA/Traits/Mirage.cs");
			Assert.That(mirage, Does.Contain("public bool Disguised { get { return IsMirage; } }"));
			Assert.That(mirage, Does.Contain("public readonly string EffectiveOwner = \"Neutral\";"));

			var enemy = (OpenRA.Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var neutral = (OpenRA.Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var tree = (ActorInfo)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ActorInfo));
			Assert.That(ObservedStyleBotModule.EffectiveAppearance(enemy, neutral, tree), Is.Null, "a mirage tank shown as a tree is not the enemy's");
			Assert.That(ObservedStyleBotModule.EffectiveAppearance(enemy, neutral, null), Is.Null);

			// Frozen records feature only what the viewer saw (Hidden / Shrouded excluded) and only by their tooltip owner.
			var fog = Code("OpenRA.Mods.Cameo/Traits/BotModules/BotFogMemory.cs");
			Assert.That(fog, Does.Contain("if (recordStyle && !fa.Hidden && !fa.Shrouded && styleObserver.ApparentInfo(fa, enemy) is ActorInfo apparent)"));
			Assert.That(module, Does.Contain("if (actor.TooltipOwner != enemy || actor.TooltipInfo == null)\n\t\t\t\treturn null;"));
		}

		[Test]
		public void ARecordWithoutAnAppearanceLeavesTheCopyIdentical()
		{
			var seen = new List<ObservedActor> { Seen(1, 100), Seen(2, 150) };
			var withHidden = new List<ObservedActor> { Seen(1, 100), new() { ActorID = 9, LastSeenTick = 175, Harvester = true, Value = 2000 }, Seen(2, 150) };
			var a = new FogStyleActor[8];
			var b = new FogStyleActor[8];
			Assert.That(FogStyleAdapter.Copy(seen, a), Is.EqualTo(2));
			Assert.That(FogStyleAdapter.Copy(withHidden, b), Is.EqualTo(2), "the true type of an actor that never appeared to the viewer is not evidence");
			Assert.That(b, Is.EqualTo(a));
			Assert.That(FogStyleAdapter.Copy(null, a), Is.Zero);
			Assert.That(FogStyleAdapter.Copy(seen, Span<FogStyleActor>.Empty), Is.Zero);
		}

		[Test]
		public void TheBoundedCopyKeepsTheMostRecentInAStableOrderWhateverTheMemoryOrder()
		{
			var remembered = new List<ObservedActor>();
			for (uint i = 0; i < 300; i++)
				remembered.Add(Seen(i, (int)(i * 7919 % 50) * 75));

			var forward = new FogStyleActor[128];
			var backward = new FogStyleActor[128];
			Assert.That(FogStyleAdapter.Copy(remembered, forward, out var truncated), Is.EqualTo(128));
			Assert.That(truncated, Is.True);
			remembered.Reverse();
			Assert.That(FogStyleAdapter.Copy(remembered, backward, out _), Is.EqualTo(128));
			Assert.That(backward, Is.EqualTo(forward), "the memory's enumeration order does not change the kept set or its order");

			var expected = remembered.Select(o => o.StyleAppearance.Value).OrderByDescending(a => a.LastSeenTick).ThenBy(a => a.ActorId).Take(128).ToArray();
			Assert.That(forward, Is.EqualTo(expected), "newest sighting first, the lower actor id on a tie");

			Assert.That(FogStyleAdapter.Copy(remembered.Take(100).ToList(), forward, out truncated), Is.EqualTo(100));
			Assert.That(truncated, Is.False);
			Assert.That(Code(Dir + "ObservedStyleBotModule.cs"), Does.Contain("const int MaxObservations = 128;"));
		}

		[Test]
		public void TheBoundedCopyAndTheClassifierAllocateNothingAfterWarmUp()
		{
			var remembered = new List<ObservedActor>();
			for (uint i = 0; i < 200; i++)
				remembered.Add(Seen(i, (int)(i % 13) * 75));

			var buffer = new FogStyleActor[128];
			var features = new int[StyleFeatures.Count];
			var tracker = new StyleTracker();
			var classifier = new StyleClassifier(StyleTable.HandPrior);
			var yards = new[] { new CPos(1, 1) };
			for (var i = 0; i < 50; i++)
			{
				var n = FogStyleAdapter.Copy(remembered, buffer, out _);
				tracker.Update(buffer.AsSpan(0, n), yards, 4000 + i * 75);
				StyleFeatures.Extract(buffer.AsSpan(0, n), 4000 + i * 75, tracker, features);
				classifier.Update(features, buffer.AsSpan(0, n), tracker, 4000 + i * 75);
			}

			var count = FogStyleAdapter.Copy(remembered, buffer, out _);
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 200; i++)
			{
				StyleFeatures.Extract(buffer.AsSpan(0, count), 10000 + i * 75, tracker, features);
				classifier.Update(features, buffer.AsSpan(0, count), tracker, 10000 + i * 75);
			}

			Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero, "the features and the classifier allocate nothing");

			// The copy walks the memory through its IReadOnlyCollection (as every reader of BotSituation.Remembered does): one boxed enumerator per
			// visit, never per record; the bounded selection and its sort allocate nothing.
			before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 200; i++)
				FogStyleAdapter.Copy(remembered, buffer, out _);

			Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.LessThanOrEqualTo(200 * 64));
		}

		[Test]
		public void TheReadingIsKeyedByTheAuthoritativeTargetAndOnlyTheTargetQualifies()
		{
			var module = Code(Dir + "ObservedStyleBotModule.cs");
			Assert.That(module, Does.Contain("if (!CollectAppearance || enemy != target || enemy != MainTarget() || !IsEnemy(enemy))\n\t\t\t\treturn null;"),
				"a known non-target style never answers for the target; no target = null = the exact three-value result");
			Assert.That(module, Does.Contain("OpenRA.Player MainTarget() => ((IBotMainTargetProvider)master)?.MainTarget;"), "the same provider LA02 reads; no dominant-faction fallback");
			Assert.That(module, Does.Contain("if (current != target)\n\t\t\t{\n\t\t\t\ttarget = current;\n\t\t\t\tqualification.Reset();"), "a target switch invalidates at once");
			Assert.That(module, Does.Contain("if (!isTarget || newest <= generationTick)\n\t\t\t\treturn count;"), "only fresh target generations vote");
			Assert.That(module, Does.Contain("nextTick = now + Math.Max(0, viewer.ClientIndex) % Interval;"), "the seats are staggered deterministically");
			Assert.That(module, Does.Contain("const int Interval = 75;"));
			foreach (var banned in new[] { "IBotCurrentPersonality", "PlayerResources", "ProductionQueue", "Personality", ".Cash", "EnemyProfile" })
				Assert.That(module, Does.Not.Contain(banned), "observation only: " + banned);
		}
	}
}
