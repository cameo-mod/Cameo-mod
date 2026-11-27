#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// LEARN-ALL-02 P1c: tools/tests/learn_all_golden.json is authored from the python fitter (tools/ai/fit_learn_all.py) and replayed here against the C# core,
	// so the DEV fitter and the release reader's math cannot drift apart. test_learn_all_fitter.py replays the same file against the python mirror.
	[TestFixture]
	public sealed class LearnAllGoldenTest
	{
		static JsonElement Golden() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "learn_all_golden.json"))).RootElement;

		static int[] Ints(JsonElement e) => e.EnumerateArray().Select(x => x.GetInt32()).ToArray();

		[Test]
		public void GeometricMeansMatchThePythonMirror()
		{
			foreach (var v in Golden().GetProperty("geomean").EnumerateArray())
				Assert.That(LearnAllMath.GeoMean(Ints(v.GetProperty("values"))), Is.EqualTo(v.GetProperty("expect").GetInt32()), v.GetProperty("values").ToString());
		}

		[Test]
		public void ResolveAndTeamEnemyMatchThePythonMirror()
		{
			foreach (var v in Golden().GetProperty("resolve").EnumerateArray())
				Assert.That(LearnAllMath.Resolve(v.GetProperty("g").GetInt32(), v.GetProperty("f").GetInt32(), v.GetProperty("e").GetInt32(), v.GetProperty("s").GetInt32()),
					Is.EqualTo(v.GetProperty("expect").GetInt32()), v.ToString());

			foreach (var v in Golden().GetProperty("team_enemy").EnumerateArray())
				Assert.That(LearnAllMath.TeamEnemy(Ints(v.GetProperty("per")), v.GetProperty("main").GetInt32()), Is.EqualTo(v.GetProperty("expect").GetInt32()), v.ToString());
		}

		static double? Signal(JsonElement e) => e.ValueKind switch
		{
			JsonValueKind.Null => null,
			JsonValueKind.String => e.GetString() == "nan" ? double.NaN : double.PositiveInfinity,
			_ => e.GetDouble(),
		};

		[Test]
		public void TheStepMatchesThePythonMirrorToTheLastPermille()
		{
			foreach (var v in Golden().GetProperty("step").EnumerateArray())
				Assert.That(LearnAllStep.Next(v.GetProperty("current").GetInt32(), Signal(v.GetProperty("signal"))), Is.EqualTo(v.GetProperty("expect").GetInt32()), v.ToString());
		}

		[Test]
		public void WholeGameSequencesProduceTheSameSnapshotText()
		{
			foreach (var g in Golden().GetProperty("games").EnumerateArray())
			{
				var table = new LearnTable();
				var changed = new List<bool>();
				foreach (var a in g.GetProperty("applications").EnumerateArray())
				{
					var enemies = a.GetProperty("enemies").EnumerateArray()
						.Select(e => new EnemyObservation(e.GetProperty("faction").GetString(),
							e.GetProperty("style").ValueKind == JsonValueKind.Null ? null : e.GetProperty("style").GetString(), e.GetProperty("weight").GetInt32())).ToArray();
					changed.Add(table.ApplyGame(a.GetProperty("game_id").GetString(), a.GetProperty("personality").GetString(), a.GetProperty("knob").GetString(),
						a.GetProperty("own").GetString(), enemies, Signal(a.GetProperty("signal"))));
				}

				Assert.That(changed, Is.EqualTo(g.GetProperty("changed").EnumerateArray().Select(x => x.GetBoolean()).ToList()), g.GetProperty("name").GetString());
				Assert.That(LearnAllSnapshot.Write(table), Is.EqualTo(g.GetProperty("expect_text").GetString()), g.GetProperty("name").GetString());
			}
		}
	}
}
