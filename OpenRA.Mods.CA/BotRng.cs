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
	/// Synced world state must still use <c>World.SharedRandom</c> (mirrored on all clients).
	/// Host-only cosmetic state stays on <c>World.LocalRandom</c>.
	/// </para>
	/// </summary>
	public static class BotRng
	{
		static readonly ConditionalWeakTable<Player, MersenneTwister> Streams = new();

		public static MersenneTwister For(IBot bot)
		{
			return For(bot.Player);
		}

		public static MersenneTwister For(Player player)
		{
			return Streams.GetValue(player, p =>
			{
				var lobbySeed = p.World.LobbyInfo.GlobalSettings.RandomSeed;
				var salt = p.PlayerActor != null ? unchecked((int)p.PlayerActor.ActorID + 1) : p.ClientIndex + 1;
				return new MersenneTwister(unchecked(lobbySeed + salt * (int)0x9E3779B9u));
			});
		}
	}
}
