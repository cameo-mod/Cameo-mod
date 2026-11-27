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
using OpenRA.Mods.Cameo.Dev;
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

		static string Str(JsonElement e) => e.ValueKind == JsonValueKind.Null ? null : e.GetString();

		static List<SeatObservation> Seats(JsonElement application) =>
			application.GetProperty("seats").EnumerateArray().Select(s => new SeatObservation(Str(s.GetProperty("personality")), Str(s.GetProperty("own")),
				s.GetProperty("enemies").EnumerateArray().Select(e => new EnemyObservation(Str(e.GetProperty("faction")), Str(e.GetProperty("style")), e.GetProperty("weight").GetInt32())).ToList(),
				Str(s.GetProperty("main_target")), Signal(s.GetProperty("signal")))).ToList();

		static (LearnTable Table, List<bool> Changed) Replay(JsonElement game, bool reverseSeats)
		{
			var table = new LearnTable();
			var changed = new List<bool>();
			foreach (var a in game.GetProperty("applications").EnumerateArray())
			{
				var seats = Seats(a);
				if (reverseSeats)
					seats.Reverse();
				changed.Add(LearnAllTrainer.ApplyGame(table, a.GetProperty("game_id").GetString(), a.GetProperty("knob").GetString(), seats));
			}

			return (table, changed);
		}

		[Test]
		public void WholeGameSequencesProduceTheSameSnapshotText()
		{
			foreach (var g in Golden().GetProperty("games").EnumerateArray())
			{
				var (table, changed) = Replay(g, false);
				Assert.That(changed, Is.EqualTo(g.GetProperty("changed").EnumerateArray().Select(x => x.GetBoolean()).ToList()), g.GetProperty("name").GetString());
				Assert.That(LearnAllSnapshotWriter.Write(table), Is.EqualTo(g.GetProperty("expect_text").GetString()), g.GetProperty("name").GetString());
			}
		}

		[Test]
		public void TheSeatOrderOfAGameDoesNotChangeTheSnapshot()
		{
			foreach (var g in Golden().GetProperty("games").EnumerateArray())
				Assert.That(LearnAllSnapshotWriter.Write(Replay(g, true).Table), Is.EqualTo(g.GetProperty("expect_text").GetString()), g.GetProperty("name").GetString());
		}
	}
}
