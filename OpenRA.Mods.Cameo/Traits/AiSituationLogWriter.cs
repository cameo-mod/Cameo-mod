#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Appends record-only AI situation snapshots for bot players.")]
	public class AiSituationLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-situations.jsonl";

		public override object Create(ActorInitializer init) { return new AiSituationLogWriter(this); }
	}

	public class AiSituationLogWriter : IWorldLoaded, IGameOver, ITick
	{
		readonly AiSituationLogWriterInfo info;
		string fallbackGameUid;
		string pendingText;
		AiLogFileAppender appender;
		bool written;
		int nextAttemptTick;

		public AiSituationLogWriter(AiSituationLogWriterInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			fallbackGameUid = Guid.NewGuid().ToString("N");
			appender = new AiLogFileAppender(info.FileName);
		}

		void ITick.Tick(Actor self)
		{
			var world = self.World;
			if (written || world.WorldTick < nextAttemptTick)
				return;
			if (pendingText != null)
			{
				TryAppend(world.WorldTick);
				return;
			}
			if (AllBotsResolved(world))
				CaptureAndAppend(world);
		}

		void IGameOver.GameOver(World world) { CaptureAndAppend(world); }

		void CaptureAndAppend(World world)
		{
			if (written)
				return;
			if (world.Type != WorldType.Regular || world.IsReplay || !Game.IsHost)
			{
				written = true;
				return;
			}

			pendingText ??= BuildLog(world);
			if (string.IsNullOrEmpty(pendingText))
			{
				written = true;
				return;
			}
			TryAppend(world.WorldTick);
		}

		void TryAppend(int worldTick)
		{
			var result = appender.TryAppend(pendingText, worldTick, out nextAttemptTick);
			if (result != AiLogAppendResult.RetryableFailure || appender.IsTerminal)
			{
				written = true;
				return;
			}
		}

		static bool AllBotsResolved(World world)
		{
			return world.Players.Where(p => p.IsBot && !p.NonCombatant && p.Playable)
				.All(p => p.WinState != WinState.Undefined);
		}

		string BuildLog(World world)
		{
			var gameUid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(gameUid))
				gameUid = fallbackGameUid;
			var lines = new StringBuilder();
			foreach (var player in world.Players.Where(p => p.IsBot && !p.NonCombatant && p.Playable)
				.OrderBy(p => p.InternalName, StringComparer.Ordinal))
			{
				var module = player.PlayerActor.TraitsImplementing<MasterAiBotModule>().FirstOrDefault();
				if (module == null)
					continue;
				foreach (var situation in module.PendingSituations)
					AppendSituation(lines, gameUid, world.LobbyInfo.GlobalSettings.GameUid ?? "", world.Map.Uid,
						player.InternalName, player.Faction.InternalName, player.BotType ?? "",
						player.PlayerActor.TraitOrDefault<AiMatchLogRecorder>()?.CurrentPersonality ?? "",
						situation,
						situation.OwnArmyValue, situation.OwnDefenceValue,
						situation.OwnBuildings, situation.OwnHarvesters,
						situation.OwnKillsCostWindow, situation.OwnDeathsCostWindow);
			}
			return lines.ToString();
		}

		internal static void AppendSituation(StringBuilder builder, string gameUid, World world,
			OpenRA.Player player, BotSituation situation, bool first = false)
		{
			var ownActors = world.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Owner == player).ToArray();
			var ownStats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
			AppendSituation(builder, gameUid, world.LobbyInfo.GlobalSettings.GameUid ?? "", world.Map.Uid,
				player.InternalName, player.Faction.InternalName, player.BotType ?? "",
				player.PlayerActor.TraitOrDefault<AiMatchLogRecorder>()?.CurrentPersonality ?? "",
				situation, ownActors.Where(IsCombatUnit).Sum(Value), ownActors.Where(IsDefence).Sum(Value),
				ownActors.Count(IsBuilding), ownActors.Count(a => a.Info.HasTraitInfo<HarvesterInfo>()),
				ownStats?.KillsCost ?? 0, ownStats?.DeathsCost ?? 0, first);
		}

		internal static void AppendSituation(StringBuilder builder, string gameUid, string worldGameUid,
			string mapUid, string playerName, string faction, string botType, string currentPersonality,
			BotSituation situation, int ownArmyValue, int ownDefenceValue, int ownBuildings,
			int ownHarvesters, int killsCostWindow, int deathsCostWindow, bool first = false)
		{
			AiMatchLogWriter.AppendObjectStart(builder);
			AiMatchLogWriter.AppendNumber(builder, "schema", 1, true);
			AiMatchLogWriter.AppendString(builder, "kind", "situation");
			AiMatchLogWriter.AppendString(builder, "record_id", gameUid + "|" + playerName + "|" + situation.Tick);
			AiMatchLogWriter.AppendString(builder, "game_uid", worldGameUid);
			AiMatchLogWriter.AppendString(builder, "map_uid", mapUid);
			AiMatchLogWriter.AppendString(builder, "player", playerName);
			AiMatchLogWriter.AppendString(builder, "faction", faction);
			AiMatchLogWriter.AppendString(builder, "bot_type", botType);
			AiMatchLogWriter.AppendNumber(builder, "tick", situation.Tick);
			AiMatchLogWriter.AppendString(builder, "urgency", situation.Urgency.ToString().ToLowerInvariant());
			AiMatchLogWriter.AppendString(builder, "personality_current", currentPersonality);
			AiMatchLogWriter.AppendString(builder, "personality_candidate", situation.Personality ?? "");
			AiMatchLogWriter.AppendString(builder, "main_target", situation.MainTarget?.InternalName ?? "");
			AiMatchLogWriter.AppendNumber(builder, "main_target_score",
				situation.MainTarget != null && situation.Enemies.TryGetValue(situation.MainTarget, out var target) ? target.Score : 0);

			AiMatchLogWriter.AppendObjectPropertyStart(builder, "hints");
			AiMatchLogWriter.AppendNumber(builder, "defence_fraction", situation.DefenceFractionHint, true);
			AiMatchLogWriter.AppendNumber(builder, "expansion_appetite", situation.ExpansionAppetiteHint);
			builder.Append('}');
			AiMatchLogWriter.AppendObjectPropertyStart(builder, "demand");
			AiMatchLogWriter.AppendNumber(builder, "anti_air", situation.Demand.AntiAir, true);
			AiMatchLogWriter.AppendNumber(builder, "anti_armour", situation.Demand.AntiArmour);
			AiMatchLogWriter.AppendNumber(builder, "anti_infantry", situation.Demand.AntiInfantry);
			AiMatchLogWriter.AppendNumber(builder, "detector", situation.Demand.Detector);
			AiMatchLogWriter.AppendNumber(builder, "artillery", situation.Demand.Artillery);
			builder.Append('}');

			AiMatchLogWriter.AppendObjectPropertyStart(builder, "own");
			AiMatchLogWriter.AppendNumber(builder, "army_value", ownArmyValue, true);
			AiMatchLogWriter.AppendNumber(builder, "defence_value", ownDefenceValue);
			AiMatchLogWriter.AppendNumber(builder, "buildings", ownBuildings);
			AiMatchLogWriter.AppendNumber(builder, "harvesters", ownHarvesters);
			AiMatchLogWriter.AppendNumber(builder, "kills_cost_window", killsCostWindow);
			AiMatchLogWriter.AppendNumber(builder, "deaths_cost_window", deathsCostWindow);
			builder.Append('}');

			builder.Append(",\"enemies\":[");
			var enemies = situation.Enemies.Values.OrderBy(e => e.Player?.InternalName ?? "", StringComparer.Ordinal).ToArray();
			for (var i = 0; i < enemies.Length; i++)
			{
				if (i > 0)
					builder.Append(',');
				var enemy = enemies[i];
				AiMatchLogWriter.AppendObjectStart(builder);
				AiMatchLogWriter.AppendString(builder, "name", enemy.Player?.InternalName ?? "", true);
				AiMatchLogWriter.AppendString(builder, "faction", enemy.Player?.Faction?.InternalName ?? "");
				AiMatchLogWriter.AppendBoolean(builder, "alive", enemy.Alive);
				AiMatchLogWriter.AppendNumber(builder, "army_value", enemy.ArmyValue);
				AiMatchLogWriter.AppendNumber(builder, "infantry_value", enemy.InfantryValue);
				AiMatchLogWriter.AppendNumber(builder, "vehicle_value", enemy.VehicleValue);
				AiMatchLogWriter.AppendNumber(builder, "air_value", enemy.AirValue);
				AiMatchLogWriter.AppendNumber(builder, "naval_value", enemy.NavalValue);
				AiMatchLogWriter.AppendNumber(builder, "defence_count", enemy.DefenceCount);
				AiMatchLogWriter.AppendNumber(builder, "defence_value", enemy.DefenceValue);
				AiMatchLogWriter.AppendNumber(builder, "tech_buildings", enemy.TechBuildings);
				AiMatchLogWriter.AppendNumber(builder, "production_buildings", enemy.ProductionBuildings);
				AiMatchLogWriter.AppendNumber(builder, "expansion_clusters", enemy.ExpansionClusters);
				AiMatchLogWriter.AppendNumber(builder, "harvesters", enemy.Harvesters);
				AiMatchLogWriter.AppendNumber(builder, "refineries", enemy.Refineries);
				AiMatchLogWriter.AppendNumber(builder, "pressure_value", enemy.PressureValue);
				AiMatchLogWriter.AppendNumber(builder, "stealth_share", enemy.StealthShare);
				AiMatchLogWriter.AppendNumber(builder, "nearest_cells", enemy.NearestCells);
				AiMatchLogWriter.AppendNumber(builder, "last_seen_tick", enemy.LastSeenTick);
				AiMatchLogWriter.AppendNumber(builder, "score", enemy.Score);
				builder.Append('}');
			}
			builder.Append("]}\n");
		}

		static bool IsBuilding(Actor a) => a.Info.HasTraitInfo<BuildingInfo>();
		static bool IsDefence(Actor a) => IsBuilding(a) && (a.Info.HasTraitInfo<AttackBaseInfo>() ||
			a.GetEnabledTargetTypes().Overlaps(new BitSet<TargetableType>("Defense")));
		static bool IsCombatUnit(Actor a) => a.Info.HasTraitInfo<AttackBaseInfo>() && !IsBuilding(a) && !a.Info.HasTraitInfo<HarvesterInfo>();
		static int Value(Actor a) => a.Info.TraitInfoOrDefault<ValuedInfo>()?.Cost ?? 0;
	}
}
