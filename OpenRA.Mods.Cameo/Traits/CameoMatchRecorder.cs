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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Conditions;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Observes the existing random personality conditions for record-only match telemetry.")]
	public class CameoMatchPlayerStateInfo : TraitInfo, IObservesVariablesInfo
	{
		public override object Create(ActorInitializer init)
		{
			// The existing declaration owns the list. No second personality registry.
			var names = init.Self.Info.TraitInfos<GrantRandomConditionInfo>()
				.SelectMany(i => i.Conditions).Where(c => c.StartsWith("personality-", StringComparison.Ordinal))
				.Distinct().OrderBy(c => c, StringComparer.Ordinal).ToArray();
			return new CameoMatchPlayerState(names);
		}
	}

	public sealed class CameoMatchPlayerState : IObservesVariables
	{
		readonly string[] names;
		public string Personality { get; private set; }
		public string Status { get; private set; } = "unavailable";

		public CameoMatchPlayerState(string[] names) { this.names = names; }

		IEnumerable<VariableObserver> IObservesVariables.GetVariableObservers()
		{
			yield return new VariableObserver((_, values) => Observe(values), names);
		}

		internal void Observe(IReadOnlyDictionary<string, int> values)
		{
			var active = names.Where(n => values.TryGetValue(n, out var count) && count > 0).ToArray();
			Personality = active.Length == 1 ? active[0] : null;
			Status = active.Length == 1 ? "observed" : active.Length == 0 ? "unavailable" : "ambiguous";
		}
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Writes bounded local completed-match JSONL telemetry without changing bot decisions or simulation state.")]
	public class CameoMatchRecorderInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new CameoMatchRecorder(); }
	}

	public sealed class CameoMatchRecorder : IWorldLoaded, IGameOver
	{
		MatchLogRecord pending;
		OpenRA.Player[] players;
		bool recorded;

		internal static bool Eligible(WorldType type, bool replay, bool loadingSave)
		{
			return type == WorldType.Regular && !replay && !loadingSave;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			// Capture this gate now: IsLoadingGameSave becomes false during replay-in.
			if (!Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave))
				return;

			players = world.Players.Where(p => !p.NonCombatant && !p.PlayerReference.Spectating)
				.OrderBy(p => p.InternalName, StringComparer.Ordinal).ToArray();
			pending = new MatchLogRecord
			{
				GameUid = world.LobbyInfo.GlobalSettings.GameUid,
				MapUid = world.Map.Uid,
				MapTitle = world.Map.Title,
				ModVersion = Game.ModData.Manifest.Metadata.Version,
				TimestepMs = world.Timestep,
				LobbyOptions = world.LobbyInfo.GlobalSettings.LobbyOptions
					.ToDictionary(kv => kv.Key, kv => kv.Value.Value),
				CodeModules = new[] { typeof(Game).Assembly, typeof(CameoMatchRecorder).Assembly,
					typeof(PlayerStatistics).Assembly }.Select(a => a.GetName().Name + ":" + a.ManifestModule.ModuleVersionId).ToArray(),
				Players = players.Select(p => new MatchPlayerRecord
				{
					Slot = p.InternalName,
					Faction = p.Faction.InternalName,
					BotType = p.BotType,
					IsBot = p.IsBot,
					PlayableSlot = p.Playable,
					Team = world.LobbyInfo.ClientInSlot(p.InternalName)?.Team ?? p.PlayerReference.Team,
					Handicap = p.Handicap,
					Allies = players.Where(other => other != p && p.IsAlliedWith(other)).Select(other => other.InternalName).ToArray(),
					InitialPersonality = p.IsBot ? p.PlayerActor.TraitOrDefault<CameoMatchPlayerState>()?.Personality : null,
					PersonalityStatus = p.IsBot ? p.PlayerActor.TraitOrDefault<CameoMatchPlayerState>()?.Status ?? "unavailable" : "not_bot"
				}).ToArray()
			};

			try
			{
				if (world.Map.InvalidCustomRules)
					pending.RulesetHashStatus = "unavailable:custom_rules_fallback";
				else
				{
					pending.RulesetHash = CameoMatchLog.Fingerprint(RulesInputs(world));
					pending.RulesetHashStatus = "captured_at_world_load";
				}
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or YamlException)
			{
				pending.RulesetHashStatus = "unavailable:" + e.GetType().Name;
			}
		}

		static IEnumerable<(string Name, Func<Stream> Open)> RulesInputs(World world)
		{
			var manifest = Game.ModData.Manifest;
			foreach (var (kind, files, extra) in new[]
			{
				("rules", manifest.Rules, world.Map.RuleDefinitions),
				("weapons", manifest.Weapons, world.Map.WeaponDefinitions)
			})
			{
				var sources = extra?.Value == null ? files : files.AddRange(
					FieldLoader.GetValue<ImmutableArray<string>>("value", extra.Value));
				foreach (var path in sources)
					yield return (kind + ":" + path, () => world.Map.Open(path));
				if (extra != null)
					yield return (kind + ":inline", () => new MemoryStream(Encoding.UTF8.GetBytes(extra.Nodes.WriteToString())));
			}
		}

		void IGameOver.GameOver(World world)
		{
			if (pending == null || recorded)
				return;

			recorded = true;
			pending.RecordedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
			pending.DurationTicks = world.WorldTick;
			// Timestep is readonly on World; this is simulated time, not elapsed wall time.
			pending.SimulationDurationMs = (long)world.WorldTick * world.Timestep;
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];
				var row = pending.Players[i];
				var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
				var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
				row.Outcome = player.WinState.ToString();
				row.FinalPersonality = player.IsBot ? player.PlayerActor.TraitOrDefault<CameoMatchPlayerState>()?.Personality : null;
				row.FinalPersonalityStatus = player.IsBot ? player.PlayerActor.TraitOrDefault<CameoMatchPlayerState>()?.Status ?? "unavailable" : "not_bot";
				row.ValueDestroyed = stats?.KillsCost;
				row.ValueLost = stats?.DeathsCost;
				row.ResourcesEarned = resources?.Earned;
				row.ResourcesSpent = resources?.Spent;
			}

			if (!CameoMatchLog.TryWrite(Path.Combine(Platform.SupportDir, "Logs", "cameo_matches"), pending, out var error))
			{
				// Do not retry a disk failure through the engine's asynchronous disk logger.
				try { Console.Error.WriteLine("Cameo match telemetry not written: " + error); }
				catch (IOException) { }
			}
		}
	}
}
