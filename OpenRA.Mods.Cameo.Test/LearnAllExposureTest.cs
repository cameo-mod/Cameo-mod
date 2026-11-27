#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// LEARN-ALL-02 P1c-2: the DEV exposure record (pure half).
	[TestFixture]
	public sealed class LearnAllExposureTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		// The same line is a fixture of tools/tests/test_learn_all_fitter.py (ExposureWriterFormat): the writer and the fitter agree on it byte for byte.
		public const string GoldenLine = "{\"game_id\":\"g1\",\"seq\":1700000000,\"seat\":\"seat_2\",\"personality\":\"rush\",\"own_faction\":\"gdi\",\"main_target\":\"nod\",\"main_target_style\":\"boom\","
			+ "\"enemies\":[{\"faction\":\"nod\",\"weight\":2},{\"faction\":null,\"weight\":1}],\"knobs\":[{\"knob\":\"wave_role.frontline\",\"signal\":0.4},{\"knob\":\"refinery.spacing\",\"signal\":null}]}";

		static readonly LearnSeatContext Ctx = new("gdi", ["nod", "", "nod"], "nod", "boom", "rush");

		[Test]
		public void TheRecordIsExactlyTheFitterFormatWithPerPlayerWeightsAndRandomAsNull()
		{
			var signals = LearnAllExposure.Sanitize([new LearnSignal("wave_role.frontline", 0.4), new LearnSignal("refinery.spacing", null)], out var dropped);
			Assert.That(dropped, Is.EqualTo(0));
			Assert.That(LearnAllExposure.Record("g1", 1700000000, LearnAllExposure.SeatId(2), Ctx, signals), Is.EqualTo(GoldenLine));
		}

		[Test]
		public void SignalsAreSanitizedAndAnUnarmedOrUnsafeSeatIsNotWritten()
		{
			var s = LearnAllExposure.Sanitize([new LearnSignal("a.b", 5.0), new LearnSignal("a.b", -1.0), new LearnSignal("Bad Knob", 0.1), new LearnSignal("x__y", 0.1), new LearnSignal("n", double.NaN)], out var dropped);
			Assert.That(dropped, Is.EqualTo(3), "a duplicate keeps the first; an uppercase or '__' knob id is dropped");
			Assert.That(s[0].Value, Is.EqualTo(1.0), "clamped to [-1, 1]");
			Assert.That(s[1].Value, Is.Null, "NaN is no evidence");
			Assert.That(LearnAllExposure.Record("g1", 1, "seat_0", Ctx, []), Is.Null, "no armed knob = no record");
			Assert.That(LearnAllExposure.Record("g1", 1, "seat_0", new LearnSeatContext("gdi", [], "", null, "a__b"), s), Is.Null, "a personality with '__' would make the fitter refuse the whole game");
			Assert.That(LearnAllExposure.Record("g 1", 1, "seat_0", Ctx, s), Is.Null, "a game id the fitter refuses");
		}

		[Test]
		public void ReleaseNeverCallsTheSignalSeamAndTheWriterIsDevOnlyAndRegistered()
		{
			foreach (var file in new[] { "mods/Cameo/Traits/BotModules/LearnAll/LearnAllBotModule.cs", "mods/CA/Traits/BotModules/IBotLearnSignals.cs" })
				Assert.That(Source(file), Does.Not.Contain(".CollectLearnSignals("), file + ": release code never calls the signal seam");

			var writer = Source("mods/Cameo.Dev/LearnAllExposureWriterDev.cs");
			Assert.That(writer, Does.Contain("IGameOver.GameOver").And.Contain("provider.CollectLearnSignals(end, raw)"), "called once per seat from the game-over hook");
			Assert.That(writer, Does.Contain("p.IsBot && !p.NonCombatant && !p.Spectating"), "bot seats only");
			Assert.That(Source("packaging/dev-logging.yaml"), Does.Contain("LearnAllExposureWriterDev:"), "registered in the DEV rules only");
			Assert.That(Source("packaging/ai.yaml"), Does.Not.Contain("LearnAllExposureWriterDev"), "the release rules never mention the DEV writer");

			var module = Source("mods/Cameo/Traits/BotModules/LearnAll/LearnAllBotModule.cs");
			Assert.That(module, Does.Contain("public LearnSeatContext SeatContext()"));
			Assert.That(module.Split("lastMainTarget").Length - 1, Is.EqualTo(3), "lastMainTarget: field, set once in ObserveMainTarget, read once in SeatContext; no decision path reads it");

			// The writer's tick must not move the decision path's refresh phase or cache (CM review of c3e1e89b): it only calls the non-caching observer; SeatContext (which refreshes) is game-over only.
			var tick = writer.Substring(writer.IndexOf("void ITick.Tick", StringComparison.Ordinal), writer.IndexOf("void IGameOver.GameOver", StringComparison.Ordinal) - writer.IndexOf("void ITick.Tick", StringComparison.Ordinal));
			Assert.That(tick, Does.Contain("ObserveMainTarget()").And.Not.Contain("SeatContext(").And.Not.Contain("Refresh"), "the tick path never refreshes the module");
			var observeStart = module.IndexOf("public void ObserveMainTarget()", StringComparison.Ordinal);
			var readStart = module.IndexOf("(string Faction, string Style) ReadMainTarget()", observeStart, StringComparison.Ordinal);
			var observe = module.Substring(observeStart, readStart - observeStart);
			Assert.That(observe, Does.Contain("ReadMainTarget()").And.Not.Contain("Refresh(").And.Not.Contain("refreshedAt").And.Not.Contain("ownFaction =").And.Not.Contain("enemies ="), "the observer reads the live providers and touches no decision-path state");
			var read = module.Substring(readStart, module.IndexOf("internal static (string Faction, string Style) MainTargetContext", readStart, StringComparison.Ordinal) - readStart);
			Assert.That(read, Does.Not.Contain("Refresh(").And.Not.Contain("refreshedAt").And.Not.Contain("ownFaction =").And.Not.Contain("enemies ="), "the shared live read caches nothing (only the provider arrays are resolved once)");
			Assert.That(writer.Split("SeatContext(").Length - 1, Is.EqualTo(1), "SeatContext is read once, in the game-over hook");
		}
	}
}
