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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// B4-C2 on OBS-STYLE-PROVIDER-01 (15ac9965): the two OBS nits (the copy order below the cap, the apparent Cloaked) and the DEV `style_obs` row (source-pinned: the row types
	/// exist in a DEV build only, so a release test cannot reference them).
	/// </summary>
	[TestFixture]
	public sealed class ObservedStyleC2Test
	{
		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Code(string file) => Regex.Replace(Regex.Replace(RepoFile(file), @"/\*.*?\*/", string.Empty, RegexOptions.Singleline), @"//[^\n]*", string.Empty);

		static ObservedActor Seen(uint id, int tick) =>
			new() { ActorID = id, LastSeenTick = tick, StyleAppearance = new FogStyleActor { ActorId = id, LastSeenTick = tick, Value = 100, Combat = true, Vehicle = true } };

		[Test]
		public void TheCopyIsInRecencyOrderBelowTheCapToo()
		{
			var remembered = new List<ObservedActor>();
			for (uint i = 0; i < 100; i++)
				remembered.Add(Seen(i, (int)(i * 7919 % 17) * 75));

			var forward = new FogStyleActor[128];
			var backward = new FogStyleActor[128];
			Assert.That(FogStyleAdapter.Copy(remembered, forward, out var truncated), Is.EqualTo(100));
			Assert.That(truncated, Is.False, "below the cap");
			remembered.Reverse();
			Assert.That(FogStyleAdapter.Copy(remembered, backward, out _), Is.EqualTo(100));
			Assert.That(backward.Take(100), Is.EqualTo(forward.Take(100)), "the memory's enumeration order does not reach the tracker or the features");

			var expected = remembered.Select(o => o.StyleAppearance.Value).OrderByDescending(a => a.LastSeenTick).ThenBy(a => a.ActorId).ToArray();
			Assert.That(forward.Take(100), Is.EqualTo(expected), "newest sighting first, the lower actor id on a tie");

			// The sort no longer hangs on the truncation flag.
			var source = Code("OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/FogStyleActor.cs");
			Assert.That(source, Does.Not.Contain("if (truncated)\n\t\t\t\tfor"));
			Assert.That(source, Does.Contain("for (var i = 1; i < count; i++)"));
		}

		[Test]
		public void TheCopyAllocatesNothingWhenItSortsBelowTheCap()
		{
			var remembered = new List<ObservedActor>();
			for (uint i = 0; i < 100; i++)
				remembered.Add(Seen(i, (int)(i % 13) * 75));

			var buffer = new FogStyleActor[128];
			FogStyleAdapter.Copy(remembered, buffer, out _);
			var before = GC.GetAllocatedBytesForCurrentThread();
			for (var i = 0; i < 200; i++)
				FogStyleAdapter.Copy(remembered, buffer, out _);

			Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.LessThanOrEqualTo(200 * 64), "one boxed enumerator per visit at most; the sort allocates nothing");
		}

		[Test]
		public void CloakedIsTheApparentTypesOwnCloakNeverTheTrueActors()
		{
			var fog = Code("OpenRA.Mods.Cameo/Traits/BotModules/BotFogMemory.cs");
			var start = fog.IndexOf("FogStyleActor StyleAppearance(", StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0));
			var method = fog.Substring(start, 900);
			Assert.That(method, Does.Contain("var shown = ClassifyObserved(apparent, 0, default, types, 0);"));
			Assert.That(method, Does.Contain("shown.Cloaked = apparent.HasTraitInfo<CloakInfo>();"));
			Assert.That(method, Does.Contain("template = FogStyleAdapter.Project(shown);"));
			Assert.That(method, Does.Not.Contain("actor.Info"), "the true type never reaches the appearance");
		}

		[Test]
		public void TheDevRowHookIsCompiledOutOfAReleaseBuild()
		{
			var module = Code("OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/ObservedStyleBotModule.cs");
			Assert.That(module, Does.Contain("public sealed partial class ObservedStyleBotModule"));
			Assert.That(module, Does.Contain("partial void StyleRow(OpenRA.Player enemy, int now, int remembered, StyleClassifier classifier);"));
			Assert.That(module, Does.Contain("state.Classifier.Update(features, observations, state.Tracker, now);\n\t\t\tStyleRow(enemy, now, count, state.Classifier);"));
			Assert.That(module, Does.Not.Contain("CAMEO_DEV_LOGGING"), "the resident module has no DEV block: the row code lives in the Dev partial");

			var dev = RepoFile("OpenRA.Mods.Cameo/Traits/BotModules/ObservedStyle/ObservedStyleBotModule.Dev.cs");
			var body = dev.Substring(dev.IndexOf("#endregion", StringComparison.Ordinal) + "#endregion".Length).Trim();
			Assert.That(body, Does.StartWith("#if CAMEO_DEV_LOGGING"));
			Assert.That(body, Does.EndWith("#endif"));
			Assert.That(Regex.Matches(body, @"(?m)^#if ").Count, Is.EqualTo(1), "the whole file is one DEV block");
			Assert.That(dev, Does.Contain("const int RowIntervalTicks = 300;"));
			Assert.That(dev, Does.Contain("if (nextRow.TryGetValue(enemy, out var due) && now < due)\n\t\t\t\treturn;"));
			Assert.That(dev, Does.Contain("observer.StyleObserved(viewer, enemy, now, remembered, features, classifier.Reading, classifier.TableVersion);"));
		}

		[Test]
		public void TheStyleObsRowCarriesNoLabelAndNoIdentity()
		{
			var row = Code("OpenRA.Mods.Cameo.Dev/AiPlayerTelemetryDev.StyleObs.cs");
			Assert.That(row, Does.Contain("ObserverRow(viewer, \"style_obs\""));
			// The two array fields are appended as raw JSON text (so their quotes are escaped in the source); the rest go through AppendString/AppendNumber.
			var order = new[] { "\"enemy\"", "\"row_version\"", "\"remembered\"", "features\\\":[", "posterior\\\":[", "\"confidence\"", "\"table_version\"" };
			var at = -1;
			foreach (var name in order)
			{
				var next = row.IndexOf(name, at + 1, StringComparison.Ordinal);
				Assert.That(next, Is.GreaterThan(at), name + " follows the documented field order");
				at = next;
			}

			foreach (var banned in new[] { "Personality", "EnemyProfile", "WinState", "Faction", "ClientIndex", "PlayerName", "Condition", "Label" })
				Assert.That(row, Does.Not.Contain(banned), "the row is label-blind: " + banned);

			Assert.That(row, Does.Contain("seat != null ? seat.Seat : \"p\" + Array.IndexOf(world.Players, enemy)"), "an anonymous enemy key");
		}
	}
}
