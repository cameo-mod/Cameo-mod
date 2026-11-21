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
using OpenRA.Mods.CA.Traits;
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>
	/// DEV-only ABLATE-01 provider (ABLATE_01_PLAN_03 section 2): reads the SYNCED lobby option `cameo-ablate`, so every
	/// client and the replay see the same value, and answers the shared <see cref="AblationGate"/>. Mounted only by the
	/// DEV rules overlay (rules/dev/logging.yaml); a release build has no type, no option and no string.
	/// </summary>
	[TraitLocation(SystemActors.World)]
	[Desc("DEV only: ablation provider reading the synced cameo-ablate lobby option. No orders, no RNG.")]
	public sealed class AblationProviderDevInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AblationProviderDev(init.World); }
	}

	public sealed class AblationProviderDev : IAblationProvider
	{
		readonly World world;
		readonly AblationSpec spec;

		public AblationProviderDev(World world)
		{
			this.world = world;
			var value = world.LobbyInfo.GlobalSettings.LobbyOptions.TryGetValue(AblationSpec.OptionId, out var state) ? state.Value : null;
			if (!AblationSpec.TryParse(value, AblationSpec.RegistryIds(), out spec, out var error))
				throw new InvalidOperationException("CAMEO DEV ABLATE rejected by the provider: " + error);
		}

		bool IAblationProvider.IsOff(OpenRA.Player owner, string id)
		{
			if (owner == null)
				return false;

			var team = world.LobbyInfo.ClientWithIndex(owner.ClientIndex)?.Team ?? 0;
			return spec.IsOff(id, team);
		}
	}

	/// <summary>
	/// DEV-only server half: `Cameo.DevAblate=&lt;ids&gt;[@team&lt;k&gt;]` on the server command line or `CAMEO_DEV_ABLATE` in the
	/// environment is validated against the registry (unknown id = the DEV start aborts) and written into the synced
	/// lobby options, so sim-changing rows run identically on every client. Mounted only by the DEV manifest generator.
	/// </summary>
	public sealed class AblationServerDev : ServerTrait, INotifyServerStart, INotifySyncLobbyInfo
	{
		string configured;

		void INotifyServerStart.ServerStarted(S server)
		{
			var value = CameoDevArgs.Value(AblationSpec.ArgumentName) ?? Environment.GetEnvironmentVariable(AblationSpec.EnvironmentName);
			if (string.IsNullOrWhiteSpace(value))
				return;

			if (!AblationSpec.TryParse(value, AblationSpec.RegistryIds(), out _, out var error))
				throw new InvalidOperationException("CAMEO DEV ABLATE rejected: " + error);

			configured = value.Trim();
			WriteOption(server);
			server.SyncLobbyInfo();
			Log.Write("server", "CAMEO DEV ABLATE set - " + configured);
		}

		// Map selection (LoadMapSettings) only touches the options a map declares, so this key survives it. The lobby
		// "reset options" command (LobbyCommands.ResetOptions) REPLACES the whole dictionary and would drop it, which
		// would silently turn an ablation run into a full-bundle run. Re-assert after every sync; idempotent (a present,
		// equal value does nothing), so the nested SyncLobbyGlobalSettings below cannot loop.
		void INotifySyncLobbyInfo.LobbyInfoSynced(S server)
		{
			if (configured == null)
				return;

			var options = server.LobbyInfo.GlobalSettings.LobbyOptions;
			if (options.TryGetValue(AblationSpec.OptionId, out var state) && state.Value == configured)
				return;

			WriteOption(server);
			server.SyncLobbyGlobalSettings();
			Log.Write("server", "CAMEO DEV ABLATE re-asserted after the lobby dropped it - " + configured);
		}

		void WriteOption(S server)
		{
			server.LobbyInfo.GlobalSettings.LobbyOptions[AblationSpec.OptionId] =
				new OpenRA.Network.Session.LobbyOptionState { Value = configured, PreferredValue = configured, IsLocked = true };
		}
	}
}
