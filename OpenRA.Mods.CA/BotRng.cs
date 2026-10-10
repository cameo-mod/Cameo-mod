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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA
{
	/// <summary>
	/// Per-bot-player random streams seeded from the lobby seed, for bot DECISION draws only.
	/// <para>
	/// <c>World.LocalRandom</c> is engine-seeded per process with no lobby-seed tie-in and is
	/// shared with cosmetic consumers (sound clip picks, weather, visual variants). Any
	/// consumer that draws a varying number of times — or an unseeded stream — shifts every
	/// later pick on that stream, which is what made same-seed bot runs diverge. Bot modules
	/// therefore draw from their own stream: same lobby seed ⇒ same decisions, different
	/// lobby seed ⇒ different play, and each bot player gets an independent sequence so one
	/// bot's rolls never perturb another's.
	/// </para>
	/// <para>
	/// Within one bot player each module draws from its own keyed stream
	/// (<see cref="For(Player, string)"/>): the module key is mixed in with an FNV-1a salt,
	/// so adding, removing, or reordering draws in one module can never shift another
	/// module's sequence. Keys must be compile-time constants (<c>nameof</c>) — never a
	/// runtime-derived or machine-dependent string, or cross-client seeds would diverge.
	/// </para>
	/// <para>
	/// Synced world state must still use <c>World.SharedRandom</c> (mirrored on all clients).
	/// Host-only cosmetic state stays on <c>World.LocalRandom</c>.
	/// </para>
	/// </summary>
	public static class BotRng
	{
		sealed class ModuleStreams
		{
			public readonly int BaseSeed;
			public readonly Dictionary<string, MersenneTwister> ByKey = new();

			public ModuleStreams(int baseSeed)
			{
				BaseSeed = baseSeed;
			}
		}

		static readonly ConditionalWeakTable<Player, ModuleStreams> Streams = new();

		public static MersenneTwister For(IBot bot)
		{
			return For(bot.Player);
		}

		public static MersenneTwister For(Player player)
		{
			return For(player, null);
		}

		public static MersenneTwister For(IBot bot, string moduleKey)
		{
			return For(bot.Player, moduleKey);
		}

		public static MersenneTwister For(Player player, string moduleKey)
		{
			var streams = Streams.GetValue(player, p => new ModuleStreams(PlayerSeed(p)));
			var key = moduleKey ?? string.Empty;
			if (!streams.ByKey.TryGetValue(key, out var stream))
				stream = streams.ByKey[key] = new MersenneTwister(ModuleSeed(streams.BaseSeed, key));
			return stream;
		}

		static int PlayerSeed(Player p)
		{
			var lobbySeed = p.World.LobbyInfo.GlobalSettings.RandomSeed;
			var salt = p.PlayerActor != null ? unchecked((int)p.PlayerActor.ActorID + 1) : p.ClientIndex + 1;
			return PlayerSeed(lobbySeed, salt);
		}

		/// <summary>
		/// The per-bot-player base seed: lobby seed mixed with the bot's stable player salt.
		/// Pure function, world-free — exposed so tests can verify same-seed reproducibility
		/// end to end without constructing a World.
		/// </summary>
		public static int PlayerSeed(int lobbySeed, int playerSalt)
		{
			return unchecked(lobbySeed + playerSalt * (int)0x9E3779B9u);
		}

		/// <summary>
		/// The seed for one module's stream: the player base seed XORed with the FNV-1a
		/// hash of the module key. Pure function — two different keys give two different
		/// streams, and the same (seed, key) pair always gives the same stream.
		/// </summary>
		public static int ModuleSeed(int playerSeed, string moduleKey)
		{
			return unchecked(playerSeed ^ (int)Fnv1a(moduleKey ?? string.Empty));
		}

		/// <summary>FNV-1a 32-bit — stable, platform-independent string salt.</summary>
		public static uint Fnv1a(string text)
		{
			var hash = 2166136261u;
			foreach (var c in text)
			{
				hash ^= c;
				hash *= 16777619u;
			}

			return hash;
		}
	}
}
