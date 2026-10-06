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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Appends the record-only AI placement / build-order log (AI_ARCHITECTURE 12.24 FE-0, 12.25 BO-0): one line per building a bot places.")]
	public class AiPlacementLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-placements.jsonl";

		public override object Create(ActorInitializer init) { return new AiPlacementLogWriter(this); }
	}

	/// <summary>
	/// REF-1 telemetry follow-up (lead ruling A, 2026-10-04): the diff verdict for one tracked refinery
	/// between scans. An owner change is a capture — a loss for the old owner and an acquisition for the
	/// new one; leaving the world is a loss whose cause is the death flag (a sold actor is disposed
	/// alive). Pure so the cause mapping is unit-tested.
	/// </summary>
	internal readonly struct RefineryTransition
	{
		public readonly string LostCause;
		public readonly bool Acquired;

		public RefineryTransition(string lostCause, bool acquired)
		{
			LostCause = lostCause;
			Acquired = acquired;
		}
	}

	internal static class RefineryLifecycle
	{
		public static RefineryTransition Classify(bool ownerChanged, bool inWorld, bool isDead)
		{
			if (inWorld)
				return ownerChanged ? new RefineryTransition("captured", true) : new RefineryTransition(null, false);

			return new RefineryTransition(isDead ? "killed" : "sold", false);
		}
	}

	// The world-side refinery set the tracker diffs — owner and the last seen location, with the
	// actor kept so the death flag and the definition are still readable after it leaves the world.
	// File scope (not nested): the module-map generator splits a class's body at the next `class`
	// keyword, and a nested class would truncate the writer's scanned trait lookups.
	internal sealed class TrackedRefinery
	{
		public Actor Actor;
		public OpenRA.Player Owner;
		public CPos Cell;
	}

	// Observer only: it never feeds a decision. The base builder calls BuildingPlaced when it issues the placement order; the line
	// is built at once (the personality and the anchors are those of that moment) and appended at game over, like the situation log.
	public class AiPlacementLogWriter : IWorldLoaded, IGameOver, ITick, IBotPlacementObserver
	{
		readonly AiPlacementLogWriterInfo info;
		readonly StringBuilder lines = new();
		readonly Dictionary<uint, TrackedRefinery> trackedRefineries = new();
		IReadOnlyDictionary<string, HashSet<string>> tagMap;
		string fallbackGameUid;
		AiLogFileAppender appender;
		bool written;
		bool eligibleAtWorldLoad;
		bool flushing;
		int nextAttemptTick;
		World world;

		public AiPlacementLogWriter(AiPlacementLogWriterInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			eligibleAtWorldLoad = AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost);
			if (!eligibleAtWorldLoad)
			{
				written = true;
				return;
			}

			fallbackGameUid = Guid.NewGuid().ToString("N");
			appender = new AiLogFileAppender(info.FileName);
		}

		void ITick.Tick(Actor self)
		{
			// The refinery lifecycle diff runs every tick so a loss is never logged after the rebuild it
			// justifies (the checker reads events in tick order). It stops at game over: the teardown
			// dispose wave would otherwise read as a mass sell.
			if (eligibleAtWorldLoad && !self.World.IsGameOver)
				TrackRefineries(self.World);

			if (written || !flushing || self.World.WorldTick < nextAttemptTick)
				return;

			TryAppend(self.World.WorldTick);
		}

		// REF-1 telemetry follow-up: diff the world's refinery set against last tick. A refinery still in
		// the world under a new owner is a capture; one gone from the scan is dead (killed) or was
		// disposed alive (sold). A bot-built refinery entering the set needs no record — the placement
		// line already covers it, and an acquired record is only ever emitted on an owner change.
		void TrackRefineries(World world)
		{
			var tick = world.WorldTick;
			var alive = new HashSet<uint>();
			foreach (var pair in world.ActorsWithTrait<Refinery>())
			{
				var actor = pair.Actor;
				if (!actor.IsInWorld || actor.IsDead)
					continue;

				alive.Add(actor.ActorID);
				if (trackedRefineries.TryGetValue(actor.ActorID, out var t))
				{
					t.Cell = actor.Location;
					if (t.Owner != actor.Owner)
					{
						var transition = RefineryLifecycle.Classify(true, true, false);
						AppendRefineryLifecycle(world, "refinery_lost", t.Owner, tick, t, transition.LostCause);
						if (transition.Acquired)
							AppendRefineryLifecycle(world, "refinery_acquired", actor.Owner, tick, t, "captured");

						t.Owner = actor.Owner;
					}
				}
				else
					trackedRefineries[actor.ActorID] = new TrackedRefinery { Actor = actor, Owner = actor.Owner, Cell = actor.Location };
			}

			if (trackedRefineries.Count == alive.Count)
				return;

			var gone = new List<uint>();
			foreach (var id in trackedRefineries.Keys)
				if (!alive.Contains(id))
					gone.Add(id);

			foreach (var id in gone)
			{
				var t = trackedRefineries[id];
				var transition = RefineryLifecycle.Classify(false, false, t.Actor.IsDead);
				AppendRefineryLifecycle(world, "refinery_lost", t.Owner, tick, t, transition.LostCause);
				trackedRefineries.Remove(id);
			}
		}

		void AppendRefineryLifecycle(World world, string kind, OpenRA.Player player, int tick, TrackedRefinery t, string cause)
		{
			if (player == null || !AiMatchLogWriter.IsLoggableBot(player))
				return;

			var gameUid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(gameUid))
				gameUid = fallbackGameUid;

			var personality = player.PlayerActor.TraitOrDefault<BotPersonalityController>()?.CurrentPersonality
				?? player.PlayerActor.TraitOrDefault<AiMatchLogRecorder>()?.CurrentPersonality ?? "";

			var builder = lines;
			AiMatchLogWriter.AppendObjectStart(builder);
			AiMatchLogWriter.AppendNumber(builder, "schema", 2, true);
			AiMatchLogWriter.AppendString(builder, "kind", kind);
			AiMatchLogWriter.AppendString(builder, "game_uid", world.LobbyInfo.GlobalSettings.GameUid ?? "");
			AiMatchLogWriter.AppendString(builder, "record_id",
				gameUid + "|" + AiMatchLogWriter.SeatKey(world, player) + "|" + tick + "|" + kind + "|" + t.Actor.Info.Name + "|" + t.Cell.X + "," + t.Cell.Y);
			AiMatchLogWriter.AppendString(builder, "map_uid", world.Map.Uid);
			AiMatchLogWriter.AppendNumber(builder, "seed", world.LobbyInfo.GlobalSettings.RandomSeed);
			AiMatchLogWriter.AppendString(builder, "seat", AiMatchLogWriter.SeatKey(world, player));
			AiMatchLogWriter.AppendString(builder, "faction", player.Faction.InternalName);
			AiMatchLogWriter.AppendString(builder, "bot_type", player.BotType ?? "");
			AiMatchLogWriter.AppendString(builder, "personality", personality);
			AiMatchLogWriter.AppendNumber(builder, "tick", tick);
			AiMatchLogWriter.AppendString(builder, "actor", t.Actor.Info.Name);
			AiMatchLogWriter.AppendString(builder, "cell",
				t.Cell.X.ToString(CultureInfo.InvariantCulture) + "," + t.Cell.Y.ToString(CultureInfo.InvariantCulture));

			// The anchor and field the refinery bound to under the affected player's own model — the same
			// computation the placement record used, so the checker's per-anchor counters line up.
			var telemetry = player.PlayerActor.TraitsImplementing<MasterAiBotModule>().FirstOrDefault()?.ExpansionTelemetry;
			if (telemetry != null)
			{
				var (index, anchor, spreader, distance) = telemetry.NearestAnchor(player, t.Cell);
				if (index >= 0)
				{
					AiMatchLogWriter.AppendString(builder, "anchor_kind", spreader ? "spreader" : "field");
					AiMatchLogWriter.AppendString(builder, "anchor_cell", anchor.X + "," + anchor.Y);
					AiMatchLogWriter.AppendNumber(builder, "anchor_dist", Math.Round(distance, 1));

					var footprint = t.Actor.Info.TraitInfoOrDefault<BuildingInfo>()?.Tiles(t.Cell).ToList();
					var (fieldId, _, _) = telemetry.PlacementFieldContext(player, t.Cell, footprint);
					if (fieldId >= 0)
						AiMatchLogWriter.AppendNumber(builder, "field_id", fieldId);
				}
			}

			AiMatchLogWriter.AppendString(builder, "cause", cause);
			builder.Append("}\n");
		}

		void IGameOver.GameOver(World world)
		{
			if (written || !eligibleAtWorldLoad)
				return;

			flushing = true;
			TryAppend(world.WorldTick);
		}

		void TryAppend(int worldTick)
		{
			if (lines.Length == 0)
			{
				written = true;
				return;
			}

			var result = appender.TryAppend(lines.ToString(), worldTick, out nextAttemptTick);
			if (result != AiLogAppendResult.RetryableFailure || appender.IsTerminal)
				written = true;
		}

		void IBotPlacementObserver.BuildingPlaced(OpenRA.Player owner, int tick, string actor, CPos cell, string reason, int queuedTick,
			FrontBackClass? frontBackClass, FrontBackPick? frontBackPick)
		{
			if (written || !eligibleAtWorldLoad || !AiMatchLogWriter.IsLoggableBot(owner))
				return;

			var gameUid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(gameUid))
				gameUid = fallbackGameUid;

			tagMap ??= BotTargetTags.BuildTagMap(world.Map.Rules);
			tagMap.TryGetValue(actor, out var tags);
			var roles = owner.PlayerActor.TraitsImplementing<IBotUnitRoles>().FirstEnabledTraitOrDefault()?.ActorRoles;
			HashSet<string> actorRoles = null;
			roles?.TryGetValue(actor, out actorRoles);
			var category = ExpansionMath.Category(tags, actorRoles);

			var personality = owner.PlayerActor.TraitOrDefault<BotPersonalityController>()?.CurrentPersonality
				?? owner.PlayerActor.TraitOrDefault<AiMatchLogRecorder>()?.CurrentPersonality ?? "";

			var builder = lines;
			AiMatchLogWriter.AppendObjectStart(builder);
			AiMatchLogWriter.AppendNumber(builder, "schema", 2, true);
			AiMatchLogWriter.AppendString(builder, "kind", "placement");
			AiMatchLogWriter.AppendString(builder, "game_uid", world.LobbyInfo.GlobalSettings.GameUid ?? "");
			AiMatchLogWriter.AppendString(builder, "record_id", gameUid + "|" + AiMatchLogWriter.SeatKey(world, owner) + "|" + tick + "|" + actor + "|" + cell.X + "," + cell.Y);
			AiMatchLogWriter.AppendString(builder, "map_uid", world.Map.Uid);
			AiMatchLogWriter.AppendNumber(builder, "seed", world.LobbyInfo.GlobalSettings.RandomSeed);
			AiMatchLogWriter.AppendString(builder, "seat", AiMatchLogWriter.SeatKey(world, owner));
			AiMatchLogWriter.AppendString(builder, "faction", owner.Faction.InternalName);
			AiMatchLogWriter.AppendString(builder, "bot_type", owner.BotType ?? "");
			AiMatchLogWriter.AppendString(builder, "personality", personality);
			AiMatchLogWriter.AppendNumber(builder, "tick", tick);
			AiMatchLogWriter.AppendNumber(builder, "queued_tick", queuedTick);
			AiMatchLogWriter.AppendNumber(builder, "placed_tick", tick);
			AiMatchLogWriter.AppendString(builder, "actor", actor);
			AiMatchLogWriter.AppendString(builder, "cell", cell.X.ToString(CultureInfo.InvariantCulture) + "," + cell.Y.ToString(CultureInfo.InvariantCulture));
			AiMatchLogWriter.AppendString(builder, "category", category);
			AiMatchLogWriter.AppendString(builder, "reason", reason);

			// BP-2 (§19.15): the front/back advisor's class label for every placement while one is
			// active, plus the pick diagnostics (front, score, radar coverage) when it claimed the cell.
			if (frontBackClass.HasValue)
				AiMatchLogWriter.AppendString(builder, "class", frontBackClass.Value.ToString().ToLowerInvariant());
			if (frontBackPick.HasValue)
			{
				var pick = frontBackPick.Value;
				AiMatchLogWriter.AppendNumber(builder, "fb_front", pick.FrontId);
				AiMatchLogWriter.AppendNumber(builder, "fb_score", pick.FrontBackScore);
				if (pick.NewCoverageCells > 0 || pick.OverlapCells > 0 || pick.SetbackCells > 0)
				{
					AiMatchLogWriter.AppendNumber(builder, "fb_new_coverage", pick.NewCoverageCells);
					AiMatchLogWriter.AppendNumber(builder, "fb_overlap", pick.OverlapCells);
					AiMatchLogWriter.AppendNumber(builder, "fb_setback", pick.SetbackCells);
				}
			}

			// Refineries: the nearest anchor (a spreader, else the centre of a spreaderless field) and the distance in
			// cells. REF-1 (§12.24 v2): the anchor's field id, the claim tier (1 = the field's first refinery, 2 = an
			// extra spreader of a covered field) and the footprint's gap to the field's resource cells.
			if (category == "refinery")
			{
				var telemetry = owner.PlayerActor.TraitsImplementing<MasterAiBotModule>().FirstOrDefault()?.ExpansionTelemetry;
				if (telemetry != null)
				{
					var (index, anchor, spreader, distance) = telemetry.NearestAnchor(owner, cell);
					if (index >= 0)
					{
						AiMatchLogWriter.AppendString(builder, "anchor_kind", spreader ? "spreader" : "field");
						AiMatchLogWriter.AppendString(builder, "anchor_cell", anchor.X + "," + anchor.Y);
						AiMatchLogWriter.AppendNumber(builder, "anchor_dist", Math.Round(distance, 1));

						var footprint = world.Map.Rules.Actors.TryGetValue(actor, out var placed)
							? placed.TraitInfos<BuildingInfo>().FirstOrDefault()?.Tiles(cell).ToList()
							: null;
						var (fieldId, tier, gap) = telemetry.PlacementFieldContext(owner, cell, footprint);
						if (fieldId >= 0)
						{
							AiMatchLogWriter.AppendNumber(builder, "field_id", fieldId);
							AiMatchLogWriter.AppendNumber(builder, "tier", tier);
							if (gap >= 0)
								AiMatchLogWriter.AppendNumber(builder, "resource_gap", gap);
						}
					}
				}
			}

			builder.Append("}\n");
		}
	}
}
