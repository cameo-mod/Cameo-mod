#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Server;
using OpenRA.Support;

using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Cameo.ServerTraits
{
	/// <summary>
	/// DEV-ONLY parity hook (BOT-DETERMINISM): pins the lobby RNG seed so repeated
	/// launches produce identical game seeds for BASE==BASE order-stream proof runs.
	/// Inert unless `Cameo.DevSeed=&lt;int&gt;` appears on the server command line.
	///
	/// The seed lives in <c>LobbyInfo.GlobalSettings.RandomSeed</c>: it is synced to
	/// every client, drives faction resolution and SharedRandom initialisation, and
	/// is the root of every per-bot BotRng stream — pinning it once before the
	/// server loop starts covers all decision RNG uniformly. Registered in mod.yaml
	/// ServerTraits but changes nothing without the arg.
	/// </summary>
	public sealed class CameoDevSeed : ServerTrait, INotifyServerStart
	{
		void INotifyServerStart.ServerStarted(S server)
		{
			var arg = CameoDevArgs.Value("Cameo.DevSeed");
			if (arg == null || !int.TryParse(arg, out var seed))
				return;

			server.LobbyInfo.GlobalSettings.RandomSeed = seed;
			server.SyncLobbyInfo();
			Log.Write("server", $"CAMEO DEV SEED pinned - RandomSeed={seed} (parity harness)");
		}
	}
}
