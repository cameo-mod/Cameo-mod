#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Support;

namespace OpenRA.Mods.Cameo.Test
{
	// A1-RNG (BOT-DETERMINISM F1): per-module BotRng streams. The world-facing
	// For(player, moduleKey) composes PlayerSeed + ModuleSeed; both are pure and
	// world-free, so these tests drive the same seed math the production call does.
	[TestFixture]
	public class BotRngTest
	{
		// Canonical FNV-1a 32-bit vectors (offset basis 2166136261, prime 16777619).
		// Module keys are compile-time nameof() identifiers — pure ASCII, so the
		// UTF-16 char walk and the byte-wise spec coincide.
		[Test]
		public void Fnv1aMatchesPublishedVectors()
		{
			Assert.That(BotRng.Fnv1a(""), Is.EqualTo(0x811C9DC5u), "empty string is the offset basis");
			Assert.That(BotRng.Fnv1a("a"), Is.EqualTo(0xE40C292Cu), "FNV-1a32(\"a\")");
			Assert.That(BotRng.Fnv1a("foobar"), Is.EqualTo(0xBF9CF968u), "FNV-1a32(\"foobar\")");
		}

		[Test]
		public void NullAndEmptyKeyShareOneStream()
		{
			var seed = BotRng.PlayerSeed(12345, 7);
			Assert.That(BotRng.ModuleSeed(seed, null), Is.EqualTo(BotRng.ModuleSeed(seed, string.Empty)),
				"the unkeyed For(player) path normalizes to the empty key");
		}

		[Test]
		public void DifferentModulesGetIndependentStreams()
		{
			var playerSeed = BotRng.PlayerSeed(0x5EED, 3);
			var a = new MersenneTwister(BotRng.ModuleSeed(playerSeed, "BaseBuilderBotModuleCA"));
			var b = new MersenneTwister(BotRng.ModuleSeed(playerSeed, "SquadManagerBotModuleCA"));

			var differ = false;
			for (var i = 0; i < 64; i++)
				differ |= a.Next() != b.Next();
			Assert.That(differ, Is.True, "two keyed streams must not share one sequence");
		}

		[Test]
		public void ModuleDrawsNeverPerturbSiblingStreams()
		{
			// The F1 invariant: adding/removing draws in one module cannot shift
			// another module's sequence. Module B's stream depends only on
			// (playerSeed, key) — draws on A leave B bit-identical.
			var playerSeed = BotRng.PlayerSeed(0x5EED, 3);
			var a = new MersenneTwister(BotRng.ModuleSeed(playerSeed, "HarvesterBotModuleCA"));
			var b = new MersenneTwister(BotRng.ModuleSeed(playerSeed, "CaptureManagerBotModuleCA"));
			var bReference = new MersenneTwister(BotRng.ModuleSeed(playerSeed, "CaptureManagerBotModuleCA"));

			for (var i = 0; i < 200; i++)
				a.Next(0, 100 + i); // perturb module A arbitrarily

			for (var i = 0; i < 64; i++)
				Assert.That(b.Next(), Is.EqualTo(bReference.Next()), "module B must be unaffected by A's draws");
		}

		[Test]
		public void SameSeedSameModuleReproducesSequence()
		{
			// Parity property, world-free: two clients deriving the same
			// (lobbySeed, playerSalt, moduleKey) must draw identical sequences.
			const int LobbySeed = -1772345678;
			const int Salt = 41;
			const string Key = "UnitBuilderBotModuleCA";

			var r1 = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, Salt), Key));
			var r2 = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, Salt), Key));

			for (var i = 0; i < 256; i++)
			{
				Assert.That(r1.Next(), Is.EqualTo(r2.Next()), $"Next diverged at draw {i}");
				Assert.That(r1.Next(0, 997), Is.EqualTo(r2.Next(0, 997)), $"Next(hi) diverged at draw {i}");
				Assert.That(r1.Next(5, 42), Is.EqualTo(r2.Next(5, 42)), $"Next(lo,hi) diverged at draw {i}");
				Assert.That(r1.NextFloat(), Is.EqualTo(r2.NextFloat()), $"NextFloat diverged at draw {i}");
			}
		}

		[Test]
		public void DifferentPlayersSameModuleGetDifferentStreams()
		{
			const int LobbySeed = 0x5EED;
			const string Key = "ScoutBotModule";
			var p1 = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, 1), Key));
			var p2 = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, 2), Key));

			var differ = false;
			for (var i = 0; i < 64; i++)
				differ |= p1.Next() != p2.Next();
			Assert.That(differ, Is.True, "player isolation must survive identical module keys");
		}

		// M14 bandit same-arm: three allied bots collapsed onto one stream because
		// the pre-spawn salt (ClientIndex) is shared by every host-owned/map player —
		// engine sets it to the admin's index. The salt is now the World.Players slot
		// index, which is distinct per player and exists before any PlayerActor spawns,
		// so a first call that happens pre-spawn can never memoize a colliding seed.
		[Test]
		public void AlliedBotsGetDistinctStreamsForSameModuleKey()
		{
			const int LobbySeed = 0x5EED;
			const string Key = "PlanBanditBotModule";

			var draws = new int[3][];
			for (var slot = 0; slot < 3; slot++)
			{
				var rng = new MersenneTwister(
					BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, slot + 1), Key));
				draws[slot] = new int[64];
				for (var i = 0; i < draws[slot].Length; i++)
					draws[slot][i] = rng.Next();
			}

			for (var i = 0; i < 64; i++)
			{
				Assert.That(draws[0][i] != draws[1][i] || draws[1][i] != draws[2][i],
					Is.True, $"teammate streams identical at draw {i} — same-arm collapse");
			}

			// Sequences must differ as whole streams, not just at one draw.
			Assert.That(draws[0], Is.Not.EqualTo(draws[1]), "bot 0 and bot 1 share a stream");
			Assert.That(draws[1], Is.Not.EqualTo(draws[2]), "bot 1 and bot 2 share a stream");
			Assert.That(draws[0], Is.Not.EqualTo(draws[2]), "bot 0 and bot 2 share a stream");
		}

		[Test]
		public void AdjacentSaltsProduceUncorrelatedStreams()
		{
			// Alternate M14 hypothesis: adjacent salt values feed correlated MT seeds.
			// The salt multiplier is the golden-ratio constant, so slot n and n+1
			// must still produce wholly different sequences.
			const int LobbySeed = 0x5EED;
			const string Key = "PlanBanditBotModule";
			var a = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, 5), Key));
			var b = new MersenneTwister(BotRng.ModuleSeed(BotRng.PlayerSeed(LobbySeed, 6), Key));

			var equal = 0;
			for (var i = 0; i < 128; i++)
				if (a.Next() == b.Next())
					equal++;
			Assert.That(equal, Is.LessThanOrEqualTo(3),
				$"adjacent salts correlated — {equal}/128 equal draws");
		}
	}
}
