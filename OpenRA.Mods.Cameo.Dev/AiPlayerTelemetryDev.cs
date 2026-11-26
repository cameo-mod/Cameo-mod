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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only player telemetry.")]
	public sealed class AiPlayerTelemetryDevInfo : TraitInfo, Requires<AiPlayerTelemetryInfo>
	{
		public override object Create(ActorInitializer init) =>
			new AiPlayerTelemetryDev(init.Self.Info.TraitInfo<AiPlayerTelemetryInfo>());
	}

	public sealed partial class AiPlayerTelemetryDev : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing, IBotUnitProductionObserver, IBotFrontierStageObserver
	{
		readonly AiPlayerTelemetryInfo info;
		readonly Dictionary<uint, BuildingTrack> buildings = new();
		readonly List<DeployEvent> mcvRemoved = [];
		readonly List<PendingConyardRemoval> pendingConyardRemoved = [];

		// MCV-SAFE-01 (IMPL02-E): the engine's Transform creates the yard (World.Add is immediate) BEFORE its queued Dispose removes the MCV, so a
		// deploy is seen as add-then-remove. A yard that matched no earlier removal waits here for the MCV removal (or expires to the old path).
		readonly List<PendingDeployAdd> pendingDeployAdds = [];
		readonly Dictionary<OpenRA.Player, SeatTelemetry> seats = new();
		static readonly List<(string Seat, CPos Home)> NoEnemies = [];
		int lastSampleTick = -1;
		int lastQueueTick = -1;

		IReadOnlyDictionary<string, HashSet<string>> tagMap;
		HashSet<string> rulesetFragileTypes;
		Dictionary<string, string> categoryCache;
		Dictionary<string, int> techDepth;
		HashSet<string> superpowerTokens;
		AiEconomyHealthLogWriter writer;
		// PROD-SAMPLE-LOG-01 FIX: unit-queue events on their own queued writer (own file, own cap), never the sync players writer.
		AiQueuedJsonlWriter unitWriter;
		// Requested-but-not-started (or skipped) unit requests per seat, DEV only: a repeat request for a pending type emits no row.
		readonly HashSet<string> pendingUnitRequests = new();
		World world;
		string uid;
		bool ended;

		public AiPlayerTelemetryDev(AiPlayerTelemetryInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!world.WorldActor.Trait<CameoLogHub>().Sink.Enabled)
				return;

			this.world = world;
			if (info.SampleIntervalTicks <= 0 || info.MovementIntervalTicks <= 0)
				return;

			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;

			uid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(uid) || uid.Length > 128)
				return;

			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uid))).ToLowerInvariant();
				writer = new AiEconomyHealthLogWriter(Path.Combine(directory, $"cameo-ai-players-{key}.jsonl"));
				unitWriter = new AiQueuedJsonlWriter($"cameo-ai-unit-queue-{key}.jsonl", "unit-queue");
			}
			// Fail closed on any setup fault — telemetry must never prevent a world from loading.
			catch (Exception e)
			{
				writer?.Dispose();
				writer = null;
				unitWriter?.Close(0);
				unitWriter = null;
				Log.Write("debug", "Player telemetry unavailable: " + e.GetType().Name);
				return;
			}

			try
			{
				tagMap = BotTargetTags.BuildTagMap(world.Map.Rules);
				rulesetFragileTypes = PlayerTelemetryMath.UnionFragileTypes(
					world.Map.Rules.Actors.TryGetValue(SystemActors.Player.ToString().ToLowerInvariant(), out var playerActorInfo)
						? playerActorInfo.TraitInfos<BaseBuilderBotModuleCAInfo>().Select(i => i.FragileTypes)
						: null);
				categoryCache = new Dictionary<string, string>(StringComparer.Ordinal);
				techDepth = BuildTechDepths(world.Map.Rules);
				superpowerTokens = BuildSuperpowerTokens(world.Map.Rules);

				// Register every eligible seat up front: the movement sampler assumes seats
				// are stable while it enumerates them, so there is no lazy registration mid-game.
				foreach (var player in world.Players.Where(AiMatchLogWriter.IsEligiblePlayer))
					Seat(player);

				// Seed the building set: actors created during world init fired before the event
				// subscription, and they were spawned — not placed — so they get no event but must
				// still produce structure_lost rows when they die.
				foreach (var actor in world.Actors)
					if (actor.IsInWorld && IsBuilding(actor))
						buildings[actor.ActorID] = new BuildingTrack { Actor = actor, Owner = actor.Owner };

				world.ActorAdded += ActorAdded;
				world.ActorRemoved += ActorRemoved;
			}
			// Fail closed: a rules/graph fault ends the channel, never the game.
			catch (Exception e)
			{
				Fail(e, "init");
			}
		}

		static Dictionary<string, int> BuildTechDepths(Ruleset rules)
		{
			var specs = new Dictionary<string, (IReadOnlyList<string>, IReadOnlyList<string>)>(StringComparer.Ordinal);
			foreach (var kv in rules.Actors)
			{
				var ai = kv.Value;
				if (!ai.HasTraitInfo<BuildingInfo>())
					continue;

				var prereqs = PlayerTelemetryMath.PositivePrereqs(
					ai.TraitInfos<BuildableInfo>().SelectMany(b => b.Prerequisites));
				var provides = ai.TraitInfos<ITechTreePrerequisiteInfo>()
					.SelectMany(t => t.Prerequisites(ai))
					.Where(p => !string.IsNullOrEmpty(p))
					.Select(p => p.ToLowerInvariant())
					.ToList();
				specs[kv.Key] = (prereqs, provides);
			}

			return PlayerTelemetryMath.TechDepths(specs);
		}

		HashSet<string> BuildSuperpowerTokens(Ruleset rules)
		{
			// Prerequisite tokens that superweapon-tagged buildings provide, so a support
			// power is logged as superweapon_fired only when a superweapon unlocks it.
			var tokens = new HashSet<string>(StringComparer.Ordinal);
			foreach (var kv in rules.Actors)
			{
				if (!tagMap.TryGetValue(kv.Key, out var tags) || !tags.Contains(BotTargetTags.Superweapon))
					continue;

				tokens.Add(kv.Key.ToLowerInvariant());
				foreach (var p in kv.Value.TraitInfos<ITechTreePrerequisiteInfo>()
					.SelectMany(t => t.Prerequisites(kv.Value)))
					if (!string.IsNullOrEmpty(p))
						tokens.Add(p.ToLowerInvariant());
			}

			return tokens;
		}

		bool IsSuperweaponPower(SupportPowerInfo info)
		{
			if (info?.Prerequisites == null)
				return false;

			foreach (var tokens in info.Prerequisites.Values)
				foreach (var token in tokens)
					if (superpowerTokens.Contains(token.ToLowerInvariant()))
						return true;

			return false;
		}

		SeatTelemetry Seat(OpenRA.Player player)
		{
			if (player == null || !AiMatchLogWriter.IsEligiblePlayer(player) || seats.Count >= 64 && !seats.ContainsKey(player))
				return null;

			if (!seats.TryGetValue(player, out var seat))
			{
				seat = new SeatTelemetry
				{
					Player = player,
					Seat = AiMatchLogWriter.SeatKey(world, player),
					Home = player.HomeLocation,
					Team = world.LobbyInfo.ClientWithIndex(player.ClientIndex)?.Team ?? player.PlayerReference.Team
				};
				seats[player] = seat;
			}

			return seat;
		}

		static bool IsBuilding(Actor actor)
		{
			return actor.Info.HasTraitInfo<BuildingInfo>();
		}

		static bool IsConyardBuilding(Actor actor, IReadOnlyDictionary<string, HashSet<string>> tags)
		{
			return tags.TryGetValue(actor.Info.Name, out var t) && t.Contains(BotTargetTags.Conyard);
		}

		static bool IsMobileConyard(Actor actor)
		{
			// The deploy source carries BaseBuilding but no Building trait (MCV and mobile base units).
			return actor.Info.HasTraitInfo<BaseBuildingInfo>() && !actor.Info.HasTraitInfo<BuildingInfo>();
		}

		bool IsCombatUnit(Actor actor)
		{
			var ai = actor.Info;
			if (!ai.HasTraitInfo<MobileInfo>() && !ai.HasTraitInfo<AircraftInfo>())
				return false;

			if (!ai.TraitInfos<ArmamentInfo>().Any())
				return false;

			return !(tagMap.TryGetValue(ai.Name, out var tags) && tags.Contains(BotTargetTags.Harvester));
		}

		string CategoryOf(ActorInfo ai)
		{
			if (categoryCache.TryGetValue(ai.Name, out var category))
				return category;

			if (ai.HasTraitInfo<BuildingInfo>())
				category = tagMap.TryGetValue(ai.Name, out var tags) && tags.Contains(BotTargetTags.Defence)
					? "defence" : "building";
			else if (tagMap.TryGetValue(ai.Name, out var tags) && tags.Contains(BotTargetTags.Harvester)
				|| ai.HasTraitInfo<TransformsInfo>())
				category = "economy";
			else
			{
				var naval = ai.TraitInfos<MobileInfo>()
					.Any(m => m.Locomotor != null && m.Locomotor.IndexOf("naval", StringComparison.OrdinalIgnoreCase) >= 0);
				category = PlayerTelemetryMath.MobileCategory(
					ai.HasTraitInfo<AircraftInfo>(), naval, ai.GetAllTargetTypes().Contains("Infantry"));
			}

			return categoryCache[ai.Name] = category;
		}

		static double Cells(CPos a, CPos b)
		{
			var dx = a.X - b.X;
			var dy = a.Y - b.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		static int CostOf(Actor actor)
		{
			return actor.Info.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0;
		}

		static string LossCause(Actor actor)
		{
			// actor.IsDead conflates kill with disposal: Dispose() marks it dead for sells,
			// transforms and cleanups alike. The truth is the health trait — only a real
			// kill leaves IsDead. Everything else is "removed": a sell vs a transform vs a
			// mission cleanup cannot be told apart from actor state at removal time.
			return actor.TraitOrDefault<IHealth>()?.IsDead ?? false ? "killed" : "removed";
		}

		void ActorAdded(Actor actor)
		{
			if (ended || writer == null)
				return;

			try
			{
				if (IsBuilding(actor))
				{
					buildings[actor.ActorID] = new BuildingTrack { Actor = actor, Owner = actor.Owner };
					var seat = Seat(actor.Owner);
					if (seat == null)
						return;

					EmitEvent(seat, "building_placed", actor.Info.Name, actor.Location,
						startDist: Cells(actor.Location, seat.Home), actorId: actor.ActorID);
					var isConyard = IsConyardBuilding(actor, tagMap);
					var deployMatched = isConyard && TryMatchDeploy(mcvRemoved, actor.Owner, actor.Location, world.WorldTick);
					if (deployMatched)
						FinishDeploy(seat, actor);
					else if (isConyard && pendingDeployAdds.Count < info.MaxPendingConyardRemovals)
						pendingDeployAdds.Add(new PendingDeployAdd { Actor = actor, Owner = actor.Owner, Cell = actor.Location, Tick = world.WorldTick });
					else
						BumpBuildingCompleted(actor);

					if (tagMap.TryGetValue(actor.Info.Name, out var tags) && tags.Contains(BotTargetTags.Superweapon))
						EmitEvent(seat, "superweapon_built", actor.Info.Name, actor.Location,
							startDist: Cells(actor.Location, seat.Home), actorId: actor.ActorID);
				}
				else if (IsMobileConyard(actor))
				{
					// Transform removes the source first (Transform.cs: Dispose then CreateActor), so
					// an MCV arriving now can close a ConYard removal that is still pending — repack.
					var repacked = false;
					for (var i = pendingConyardRemoved.Count - 1; i >= 0; i--)
					{
						var p = pendingConyardRemoved[i];
						if (p.Owner != actor.Owner
							|| Math.Abs(p.Tick - world.WorldTick) > info.DeployMatchWindowTicks
							|| Math.Max(Math.Abs(p.Cell.X - actor.Location.X), Math.Abs(p.Cell.Y - actor.Location.Y)) > info.DeployMatchCells)
							continue;

						repacked = true;
						pendingConyardRemoved.RemoveAt(i);
						var seat = Seat(actor.Owner);
						if (seat != null)
							EmitEvent(seat, "conyard_packed", p.ActorName, p.Cell,
								startDist: Cells(p.Cell, seat.Home), actorId: p.ActorId);
						break;
					}

					// An MCV that closes no repack window was produced — completed unit output.
					if (!repacked && BotSeat(actor.Owner) is { } bot)
					{
						Bump(bot.QueueCompleted, "unit");
						EmitUnitEvent(bot, actor.World.WorldTick, "ready", actor.Info.Name);
					}
				}
				else if (BotSeat(actor.Owner) is { } bot)
				{
					// A mobile actor appearing is a completed unit production for the bot-queue
					// row (transforms land in the building branch's deploy match instead).
					Bump(bot.QueueCompleted, "unit");
					EmitUnitEvent(bot, actor.World.WorldTick, "ready", actor.Info.Name);
				}
			}
			catch (Exception e)
			{
				Fail(e, "actor_added");
			}
		}

		void ActorRemoved(Actor actor)
		{
			if (ended || writer == null)
				return;

			try
			{
				HarvesterLost(actor); // BUILD-ALL-B (AiPlayerTelemetryDev.Coverage.cs)

				if (IsMobileConyard(actor))
				{
					if (TryMatchPendingDeploy(actor.Owner, actor.Location, world.WorldTick, out var deployedYard))
					{
						FinishDeploy(Seat(actor.Owner), deployedYard);
						return;
					}

					if (mcvRemoved.Count < info.MaxPendingConyardRemovals)
						mcvRemoved.Add(new DeployEvent
						{
							Owner = actor.Owner, Cell = actor.Location, Tick = world.WorldTick,
							ActorName = actor.Info.Name, ActorId = actor.ActorID, Cause = LossCause(actor)
						});
					return;
				}

				if (!IsBuilding(actor) || !buildings.Remove(actor.ActorID))
					return;

				if (IsConyardBuilding(actor, tagMap))
				{
					// Hold the verdict: a repacking ConYard disappears before its MCV is created,
					// so the loss/structure question is answered when the window closes.
					if (pendingConyardRemoved.Count < info.MaxPendingConyardRemovals)
						pendingConyardRemoved.Add(new PendingConyardRemoval
						{
							Owner = actor.Owner,
							ActorName = actor.Info.Name,
							ActorId = actor.ActorID,
							Cell = actor.Location,
							Tick = world.WorldTick,
							Cause = LossCause(actor)
						});
					return;
				}

				var seat = Seat(actor.Owner);
				if (seat != null)
					EmitEvent(seat, "structure_lost", actor.Info.Name, actor.Location,
						cause: LossCause(actor),
						startDist: Cells(actor.Location, seat.Home), actorId: actor.ActorID);
			}
			catch (Exception e)
			{
				Fail(e, "actor_removed");
			}
		}

		/// <summary>An expansion deploy adds a ConYard while one already stands; the starting MCV's first deploy and a pack/redeploy relocation do not count.</summary>
		void FinishDeploy(SeatTelemetry seat, Actor yard)
		{
			if (seat == null)
				return;

			var conyards = 0;
			foreach (var other in world.Actors)
				if (other != yard && other.IsInWorld && !other.IsDead
					&& other.Owner == yard.Owner && IsConyardBuilding(other, tagMap))
					conyards++;

			if (conyards > 0)
				seat.ExpansionDeploys++;

			EmitEvent(seat, "conyard_deployed", yard.Info.Name, yard.Location,
				startDist: Cells(yard.Location, seat.Home), actorId: yard.ActorID);
		}

		/// <summary>A ConYard only ever appears through an MCV transform, never from a queue; a yard no MCV removal claimed counts as completed production as before.</summary>
		void BumpBuildingCompleted(Actor actor)
		{
			var bot = BotSeat(actor.Owner);
			if (bot != null)
				Bump(bot.QueueCompleted, BuildingCategoryOf(bot, actor.Info.Name));
		}

		bool TryMatchPendingDeploy(OpenRA.Player owner, CPos cell, int tick, out Actor yard)
		{
			var entries = pendingDeployAdds.ConvertAll(e => (e.Owner, e.Cell, e.Tick));
			var index = DeployMatchMath.Find(entries, owner, cell, tick, info.DeployMatchWindowTicks, info.DeployMatchCells);
			if (index < 0)
			{
				yard = null;
				return false;
			}

			yard = pendingDeployAdds[index].Actor;
			pendingDeployAdds.RemoveAt(index);
			return true;
		}

		bool TryMatchDeploy(List<DeployEvent> recent, OpenRA.Player owner, CPos cell, int tick)
		{
			for (var i = recent.Count - 1; i >= 0; i--)
			{
				var e = recent[i];
				if (e.Owner != owner
					|| Math.Abs(e.Tick - tick) > info.DeployMatchWindowTicks
					|| Math.Max(Math.Abs(e.Cell.X - cell.X), Math.Abs(e.Cell.Y - cell.Y)) > info.DeployMatchCells)
					continue;

				recent.RemoveAt(i);
				return true;
			}

			return false;
		}

		static bool McvSafetyOn(OpenRA.Player owner) =>
			owner?.PlayerActor != null && owner.PlayerActor.TraitsImplementing<OpenRA.Mods.CA.Traits.BotModules.IBotMcvSafetyPolicy>().Any(t => t.McvSafeEnabled);

		void PruneEvents(int tick)
		{
			var horizon = tick - info.DeployMatchWindowTicks * 2;
			foreach (var e in mcvRemoved.Where(e => e.Tick < horizon && e.Cause == "killed" && McvSafetyOn(e.Owner)).ToList())
			{
				// MCV-SAFE-01: a killed MCV no yard ever claimed (the planner's loss zone has the same signal); only with the CG policy on.
				var seat = Seat(e.Owner);
				if (seat != null)
					EmitEvent(seat, "mcv_lost", e.ActorName, e.Cell, cause: e.Cause, startDist: Cells(e.Cell, seat.Home), actorId: e.ActorId);
			}

			mcvRemoved.RemoveAll(e => e.Tick < horizon);

			for (var i = pendingDeployAdds.Count - 1; i >= 0; i--)
			{
				var p = pendingDeployAdds[i];
				if (p.Tick >= horizon)
					continue;

				pendingDeployAdds.RemoveAt(i);
				if (p.Actor != null && !p.Actor.Disposed)
					BumpBuildingCompleted(p.Actor);
			}

			for (var i = pendingConyardRemoved.Count - 1; i >= 0; i--)
			{
				var p = pendingConyardRemoved[i];
				if (p.Tick >= horizon)
					continue;

				pendingConyardRemoved.RemoveAt(i);
				var seat = Seat(p.Owner);
				if (seat != null)
					EmitEvent(seat, "structure_lost", p.ActorName, p.Cell,
						cause: p.Cause,
						startDist: Cells(p.Cell, seat.Home), actorId: p.ActorId);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (writer == null || ended || self.World.IsGameOver)
				return;

			var tick = self.World.WorldTick;
			try
			{
				if (tick % info.MovementIntervalTicks == 0)
				{
					PruneEvents(tick);
					MovementSample(self.World);
					CheckBuildingOwners();
				}

				PollSupportPowers();

				if (info.QueueIntervalTicks > 0 && tick % info.QueueIntervalTicks == 0)
					QueueSample(self.World, tick);

				if (tick % info.SampleIntervalTicks == 0)
					EconomySample(self.World, tick);

				if (tick % CoveragePollTicks == 0)
					CoveragePoll(self.World); // BUILD-ALL-B (AiPlayerTelemetryDev.Coverage.cs)
			}
			// Fail closed: a sampling fault ends the channel, never the game.
			catch (Exception e)
			{
				Fail(e, "tick");
			}
		}

		void CheckBuildingOwners()
		{
			foreach (var kv in buildings)
			{
				var track = kv.Value;
				var owner = track.Actor.Owner;
				if (owner == null || owner == track.Owner)
					continue;

				var loser = Seat(track.Owner);
				var winner = Seat(owner);
				track.Owner = owner;

				if (loser != null)
					EmitEvent(loser, "structure_lost", track.Actor.Info.Name, track.Actor.Location,
						cause: "captured", toSeat: winner?.Seat,
						startDist: Cells(track.Actor.Location, loser.Home),
						actorId: track.Actor.ActorID);
			}
		}

		void MovementSample(World world)
		{
			var eligible = world.Players.Where(AiMatchLogWriter.IsEligiblePlayer).ToArray();
			var enemyHomes = new Dictionary<SeatTelemetry, List<(string Seat, CPos Home)>>();
			foreach (var seat in seats.Values)
				enemyHomes[seat] = eligible
					.Where(e => e != seat.Player && !e.IsAlliedWith(seat.Player))
					.Select(e => (Seat(e)?.Seat, e.HomeLocation))
					.Where(e => e.Seat != null)
					.ToList();

			var seen = new Dictionary<SeatTelemetry, HashSet<uint>>();
			foreach (var seat in seats.Values)
				seen[seat] = [];

			foreach (var actor in world.Actors)
			{
				if (!actor.IsInWorld || actor.IsDead || actor.Owner == null || actor.OccupiesSpace == null)
					continue;

				var seat = Seat(actor.Owner);
				if (seat == null)
					continue;

				if (!enemyHomes.TryGetValue(seat, out var enemies))
					enemies = NoEnemies;

				if (!IsBuilding(actor) && !seat.FirstUnitLogged)
				{
					var (reach, reachSeat) = NearestEnemy(actor.Location, enemies);
					if (reach >= 0 && reach <= info.EnemyTerritoryCells)
					{
						seat.FirstUnitLogged = true;
						EmitEvent(seat, "first_unit_enemy_territory", actor.Info.Name,
							actor.Location, toSeat: reachSeat, actorId: actor.ActorID);
					}
				}

				if (!IsCombatUnit(actor) || !seen.TryGetValue(seat, out var ids))
					continue;

				if (ids.Count >= info.MaxTrackedUnitsPerPlayer && !seat.Units.ContainsKey(actor.ActorID))
					continue;

				ids.Add(actor.ActorID);
				var cell = actor.Location;
				seat.Units[actor.ActorID] = cell;
				var (enemy, _) = NearestEnemy(cell, enemies);
				seat.Dists[actor.ActorID] = (Cells(cell, seat.Home), enemy);
			}

			foreach (var seat in seats.Values)
			{
				var marching = 0;
				double cx = 0, cy = 0;
				foreach (var id in seen.GetValueOrDefault(seat, []))
				{
					if (!seat.Dists.TryGetValue(id, out var now) || !seat.PreviousDists.TryGetValue(id, out var before))
						continue;
					if (!PlayerTelemetryMath.Marching(before.Home, now.Home, before.Enemy, now.Enemy, info.MarchMinCells))
						continue;

					marching++;
					cx += seat.Units[id].X;
					cy += seat.Units[id].Y;
				}

				var edge = PlayerTelemetryMath.Wave(seat.WaveActive, marching, info.WaveMinUnits, info.WaveEndUnits);
				if (edge == PlayerTelemetryMath.WaveEdge.Start)
				{
					seat.WaveActive = true;
					var centroid = new CPos((int)Math.Round(cx / marching), (int)Math.Round(cy / marching));
					var (_, toward) = NearestEnemy(centroid,
						enemyHomes.TryGetValue(seat, out var enemies) ? enemies : NoEnemies);
					EmitEvent(seat, "attack_wave_start", null, centroid, units: marching, toSeat: toward);
				}
				else if (edge == PlayerTelemetryMath.WaveEdge.End)
					seat.WaveActive = false;

				foreach (var kv in seat.Dists)
					seat.PreviousDists[kv.Key] = kv.Value;

				foreach (var id in seat.Units.Keys.ToArray())
					if (!seen[seat].Contains(id))
					{
						seat.Units.Remove(id);
						seat.Dists.Remove(id);
						seat.PreviousDists.Remove(id);
					}
			}
		}

		static (double Distance, string Seat) NearestEnemy(CPos cell, List<(string Seat, CPos Home)> enemies)
		{
			var best = -1.0;
			string bestSeat = null;
			foreach (var e in enemies)
			{
				var d = Cells(cell, e.Home);
				if (best < 0 || d < best)
				{
					best = d;
					bestSeat = e.Seat;
				}
			}

			return (best, bestSeat);
		}

		void PollSupportPowers()
		{
			foreach (var seat in seats.Values)
			{
				var manager = seat.Player.PlayerActor?.TraitOrDefault<SupportPowerManager>();
				if (manager == null)
					continue;

				foreach (var kv in manager.Powers)
				{
					var inst = kv.Value;
					var ready = inst.Ready;

					// Fired = was Ready, charge restarted, and at least one source building is
					// still enabled. Active survives a fire cycle but drops when the last
					// source is disabled or the player is defeated; destroyed sources remove
					// the instance outright. One-shots spend Active, so they qualify via
					// their info flag plus a still-enabled instance.
					if (seat.PowerReady.TryGetValue(kv.Key, out var was) && was && !ready
						&& inst.Instances.Any(i => !i.IsTraitDisabled)
						&& (inst.Active || (inst.Info?.OneShot ?? false))
						&& IsSuperweaponPower(inst.Info))
						EmitEvent(seat, "superweapon_fired", kv.Key, null);
					seat.PowerReady[kv.Key] = ready;
				}
			}
		}

		// ----- WBL-P1 conversion diagnostics (bot seats only) -----
		// Record-only: the observer seam is notified inside the unit builders, the 250-tick
		// poll reads the same public surfaces the modules decide on. Nothing here writes to
		// synced state, orders, or RNG, and a fault ends the channel through Fail(), never
		// the game or the notifying module.

		SeatTelemetry BotSeat(OpenRA.Player player)
		{
			return player != null && AiMatchLogWriter.IsLoggableBot(player) && seats.TryGetValue(player, out var seat)
				? seat
				: null;
		}

		static void Bump(Dictionary<string, int> counts, string key)
		{
			counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
		}

		void IBotUnitProductionObserver.UnitProductionRequested(OpenRA.Player owner, int tick, string actor)
		{
			try
			{
				var seat = BotSeat(owner);
				if (seat != null)
				{
					Bump(seat.QueueRequested, "unit");
					// PROD-SAMPLE-LOG-01 FIX: the first pending request per type per seat emits a row; repeats while pending do not.
					if (pendingUnitRequests.Add(seat.Seat + "|" + actor))
						EmitUnitEvent(seat, tick, "requested", actor);
				}
			}
			catch (Exception e)
			{
				Fail(e, "unit_requested");
			}
		}

		void IBotUnitProductionObserver.UnitProductionStarted(OpenRA.Player owner, int tick, string actor, string queueType)
		{
			try
			{
				var seat = BotSeat(owner);
				if (seat == null)
					return;

				Bump(seat.QueueAccepted, "unit");
				pendingUnitRequests.Remove(seat.Seat + "|" + actor);
				EmitUnitEvent(seat, tick, "queued", actor);
				seat.QueueSpend["unit"] = seat.QueueSpend.GetValueOrDefault("unit") + CostOfName(actor);

				// The poll's head-diff would see this order again when the item reaches
				// the head — mark it so the acceptance is counted exactly once. A stale
				// marker (item cancelled before heading) at most skips one later same-name
				// head, which is the same single acceptance in the window accounting.
				// The key is player-actor-scoped: queue hosts are factory buildings in
				// this mod, and PlayerActor.ActorID is what the poll looks the marker up by.
				var actorId = owner.PlayerActor?.ActorID;
				if (actorId.HasValue)
					Bump(seat.SeamStarts, PlayerTelemetryMath.SeamKey(actorId.Value, queueType, actor));
			}
			catch (Exception e)
			{
				Fail(e, "unit_started");
			}
		}

		void IBotUnitProductionObserver.UnitProductionSkipped(OpenRA.Player owner, int tick, string actor, string reason)
		{
			try
			{
				var seat = BotSeat(owner);
				if (seat != null)
					pendingUnitRequests.Remove(seat.Seat + "|" + actor);
				if (seat != null && reason != null)
					Bump(seat.QueueSkipped, reason);
			}
			catch (Exception e)
			{
				Fail(e, "unit_skipped");
			}
		}

		void IBotFrontierStageObserver.FrontierStageChanged(OpenRA.Player owner, int tick, in FrontierStageRow row)
		{
			try
			{
				var seat = BotSeat(owner);
				if (seat != null)
					EmitStageEvent(seat, in row);
			}
			catch (Exception e)
			{
				Fail(e, "frontier_stage");
			}
		}

		int CostOfName(string actorName)
		{
			return world.Map.Rules.Actors.TryGetValue(actorName, out var ai)
				? ai.TraitInfos<ValuedInfo>().FirstOrDefault()?.Cost ?? 0
				: 0;
		}

		string BuildingCategoryOf(SeatTelemetry seat, string actorName)
		{
			var builder = seat.BaseBuilder;
			var refinery = builder != null && builder.Info.RefineryTypes.Contains(actorName)
				|| tagMap.TryGetValue(actorName, out var refTags) && refTags.Contains(BotTargetTags.Refinery);
			var defence = builder != null && builder.Info.DefenseTypes.Contains(actorName)
				|| tagMap.TryGetValue(actorName, out var defTags) && defTags.Contains(BotTargetTags.Defence);
			var crawl = builder != null
				? builder.Info.FragileTypes.Contains(actorName)
				: rulesetFragileTypes != null && rulesetFragileTypes.Contains(actorName);
			return PlayerTelemetryMath.BuildingCategory(defence, refinery, crawl);
		}

		string ItemCategory(SeatTelemetry seat, string actorName)
		{
			return world.Map.Rules.Actors.TryGetValue(actorName, out var ai) && ai.HasTraitInfo<BuildingInfo>()
				? BuildingCategoryOf(seat, actorName)
				: "unit";
		}

		void ResolveQueueHelpers(SeatTelemetry seat)
		{
			if (seat.QueueHelpersResolved)
				return;
			seat.QueueHelpersResolved = true;

			var actor = seat.Player.PlayerActor;
			if (actor == null || actor.IsDead || actor.Disposed)
				return;

			seat.BaseBuilder = actor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			seat.Resources = actor.TraitOrDefault<PlayerResources>();
		}

		// The gate is resolved per poll, not cached: a conditional unit builder can
		// arm late (grant conditions) or go dormant, and only the currently-enabled
		// module's verdicts are gate truth.
		IBotUnitProductionGateInfo GateFor(SeatTelemetry seat)
		{
			var actor = seat.Player.PlayerActor;
			return actor == null || actor.IsDead || actor.Disposed
				? null
				: actor.TraitsImplementing<IBotUnitProductionGateInfo>().FirstEnabledTraitOrDefault();
		}

		bool UnitCapped(SeatTelemetry seat, string[] buildable,
			Dictionary<string, int> owned, Dictionary<string, int> queued,
			IBotUnitProductionGateInfo gate)
		{
			if (gate == null || buildable.Length == 0)
				return false;

			foreach (var name in buildable)
				if (!gate.UnitProductionLimited(name,
					owned.GetValueOrDefault(name), queued.GetValueOrDefault(name)))
					return false;

			return true;
		}

		sealed class QueueRow
		{
			public string Queue;
			public string Group;
			public string State;
			public string Item;
			public string Reason;
			public string[] ReservedBy;
		}

		void QueueSample(World world, int tick)
		{
			// PROD-SAMPLE-LOG-01 FIX (review nit): the requested dedupe window is one queue sample, so a request that is dropped
			// without a started or skipped callback logs again at most once per sample.
			pendingUnitRequests.Clear();
			lastQueueTick = tick;
			foreach (var seat in seats.Values)
			{
				if (!AiMatchLogWriter.IsLoggableBot(seat.Player))
					continue;

				ResolveQueueHelpers(seat);
				var resources = seat.Resources;
				var cash = (resources?.Cash ?? 0) + (resources?.Resources ?? 0);
				var spent = resources?.Spent ?? 0;
				var spentDelta = seat.LastSpent < 0 ? spent : spent - seat.LastSpent;
				seat.LastSpent = spent;

				// Enforced gate truth, from whichever unit-builder module the seat runs
				// (CA or Frans — both expose IBotUnitProductionGateInfo). No gate impl
				// means only the queue-physical reasons (disabled/busy/paused, missing
				// prerequisites, no desire) can ever be reported for the seat.
				var gate = GateFor(seat);
				var voters = gate?.UnitProductionReservedBy ?? [];
				var cashFloor = gate?.UnitProductionCashFloor ?? 0;
				var threshold = gate?.UnitProductionSaturationCash ?? 0;
				var saturated = ProductionSaturationCA.Saturated(cash, threshold);
				var belowFloor = cash < cashFloor;
				var reserved = voters.Length > 0;
				var playerActorId = seat.Player.PlayerActor?.ActorID;

				// Owned + already-queued counts, shared across this seat's queues — only
				// computed when an idle queue needs the unit-cap verdict.
				Dictionary<string, int> owned = null;
				Dictionary<string, int> queued = null;
				var rows = new List<QueueRow>();
				foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
				{
					if (rows.Count >= info.MaxQueueRowsPerSeat)
						break;
					if (pair.Actor.Owner != seat.Player || pair.Actor.IsDead || !pair.Actor.IsInWorld)
						continue;

					var queue = pair.Trait;
					var head = queue.Enabled ? queue.CurrentItem() : null;
					var state = PlayerTelemetryMath.QueueState(queue.Enabled, head != null, head?.Paused ?? false);
					string reason = null;
					string[] reservedBy = null;
					switch (state)
					{
						case "disabled":
							reason = "queue_disabled";
							break;
						case "busy":
							reason = "producing";
							break;
						case "paused":
							reason = "paused";
							break;
						default:
						{
							var names = queue.BuildableItems().Select(bi => bi.Name).ToArray();
							var capped = false;
							var gateOrder = info.IdleReasonGateOrder && !AblationGate.Off(seat.Player, AblationIds.TelemetryIdleReasonOrder);
							if (names.Length > 0 && !reserved && !belowFloor && (gateOrder || !saturated))
							{
								owned ??= CountOwned(seat);
								queued ??= CountQueued(seat);
								capped = UnitCapped(seat, names, owned, queued, gate);
							}

							reason = PlayerTelemetryMath.IdleReason(reserved, belowFloor, saturated,
								names.Length == 0, capped, gateOrder);
							if (reason == "cash_reserved")
								reservedBy = voters;
							break;
						}
					}

					// Acceptance: a different head item than the last poll means this queue
					// started producing that item between samples (reference compare — a
					// same-name re-queue is still a new episode). QueueHeads stays keyed on
					// the queue HOST actor (queues live on factory buildings here); the
					// order-issue marker is looked up under the same player-actor key the
					// seam wrote — consumed markers are not counted again, while starts
					// from paths the seam does not observe still count exactly once.
					var key = pair.Actor.ActorID + ":" + queue.Info.Type;
					if (!seat.QueueHeads.TryGetValue(key, out var prev) || !ReferenceEquals(prev, head))
					{
						if (head != null)
						{
							var consumed = playerActorId.HasValue
								&& PlayerTelemetryMath.ConsumeSeamStart(seat.SeamStarts,
									PlayerTelemetryMath.SeamKey(playerActorId.Value, queue.Info.Type, head.Item));
							if (!consumed)
							{
								var cat = ItemCategory(seat, head.Item);
								Bump(seat.QueueAccepted, cat);
								seat.QueueSpend[cat] = seat.QueueSpend.GetValueOrDefault(cat) + CostOfName(head.Item);
							}
						}

						seat.QueueHeads[key] = head;
					}

					rows.Add(new QueueRow
					{
						Queue = queue.Info.Type,
						Group = queue.Info.Group ?? "",
						State = state,
						Item = head?.Item,
						Reason = reason,
						ReservedBy = reservedBy
					});
				}

				unitWriter?.Flush(tick); // PROD-SAMPLE-LOG-01 FIX: one non-blocking hand-off of pending unit events per queue sample.
				EmitQueueRow(seat, tick, cash, spentDelta, rows);

				// The counters are per-window: emitted, then reset for the next interval.
				seat.QueueRequested.Clear();
				seat.QueueAccepted.Clear();
				seat.QueueCompleted.Clear();
				seat.QueueSpend.Clear();
				seat.QueueSkipped.Clear();
			}
		}

		Dictionary<string, int> CountOwned(SeatTelemetry seat)
		{
			var counts = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var a in world.Actors)
				if (a.IsInWorld && !a.IsDead && a.Owner == seat.Player)
					Bump(counts, a.Info.Name);
			return counts;
		}

		Dictionary<string, int> CountQueued(SeatTelemetry seat)
		{
			var counts = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var pair in world.ActorsWithTrait<ProductionQueue>())
				if (pair.Actor.Owner == seat.Player)
					foreach (var item in pair.Trait.AllQueued())
						Bump(counts, item.Item);
			return counts;
		}

		// PROD-SAMPLE-LOG-01 (DEV only, assembly compiled out of release): one row per unit-queue step on the same
		// cameo-bot-queue stays on the players writer; these rows go to cameo-ai-unit-queue-{key}.jsonl (own cap).
		// kind = requested | queued | ready. Queue is always "unit" here; building queues are already in cameo-bot-queue.
		// requested: first pending request per type per seat only. ready: also counts non-produced mobile spawns
		// (crate units, payouts), the same as the Bump on QueueCompleted; filter by production when analysing.
		void EmitUnitEvent(SeatTelemetry seat, int tick, string kind, string type)
		{
			if (ended || unitWriter == null || seat == null)
				return;

			var b = new StringBuilder(256);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-unit-queue-event", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendNumber(b, "tick", tick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendString(b, "kind", kind);
			AiMatchLogWriter.AppendString(b, "queue", "unit");
			AiMatchLogWriter.AppendString(b, "type", type);
			b.Append('}');
			unitWriter.TryWrite(b.ToString());
		}

		// The configured ProductionCashReserve of the seat's BotLimits (-1 when the seat has none). The effective
		// reserve can be personality-leaned (RecoverySpendPolicyCA.EffectiveReserve); this is the configured value.
		static int ReserveOf(OpenRA.Player player)
		{
			var limits = player?.PlayerActor?.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			return limits == null ? -1 : limits.Info.ProductionCashReserve;
		}

		void EmitQueueRow(SeatTelemetry seat, int tick, int banked, long spentDelta, List<QueueRow> rows)
		{
			var b = new StringBuilder(768);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-bot-queue", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendString(b, "record_id", uid + "|" + seat.Seat + "|queue|" + tick);
			AiMatchLogWriter.AppendNumber(b, "tick", tick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendNumber(b, "banked", banked);
			AiMatchLogWriter.AppendNumber(b, "reserve", ReserveOf(seat.Player));
			AiMatchLogWriter.AppendNumber(b, "spent_delta", spentDelta);

			AppendCategoryMap(b, "requested", seat.QueueRequested);
			AppendCategoryMap(b, "accepted", seat.QueueAccepted);
			AppendCategoryMap(b, "completed", seat.QueueCompleted);

			AiMatchLogWriter.AppendObjectPropertyStart(b, "skipped");
			var first = true;
			foreach (var kv in seat.QueueSkipped.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				AiMatchLogWriter.AppendNumber(b, kv.Key, kv.Value, first);
				first = false;
			}

			b.Append('}');

			AiMatchLogWriter.AppendObjectPropertyStart(b, "spend");
			foreach (var category in PlayerTelemetryMath.QueueCategories)
				AiMatchLogWriter.AppendNumber(b, category, seat.QueueSpend.GetValueOrDefault(category),
					category == PlayerTelemetryMath.QueueCategories[0]);
			b.Append('}');

			AiMatchLogWriter.AppendArrayPropertyStart(b, "queues");
			first = true;
			foreach (var row in rows)
			{
				if (!first)
					b.Append(',');
				AiMatchLogWriter.AppendObjectStart(b);
				AiMatchLogWriter.AppendString(b, "queue", row.Queue, true);
				AiMatchLogWriter.AppendString(b, "group", row.Group);
				AiMatchLogWriter.AppendString(b, "state", row.State);
				if (row.Item != null)
					AiMatchLogWriter.AppendString(b, "item", row.Item);
				if (row.Reason != null)
					AiMatchLogWriter.AppendString(b, "reason", row.Reason);
				if (row.ReservedBy != null)
				{
					AiMatchLogWriter.AppendArrayPropertyStart(b, "reserved_by");
					var inner = true;
					foreach (var voter in row.ReservedBy)
					{
						if (!inner)
							b.Append(',');
						b.Append('"');
						b.Append(voter);
						b.Append('"');
						inner = false;
					}

					b.Append(']');
				}

				b.Append('}');
				first = false;
			}

			b.Append("]}");
			writer.TryWrite(b.ToString());
		}

		static void AppendCategoryMap(StringBuilder b, string name, Dictionary<string, int> counts)
		{
			AiMatchLogWriter.AppendObjectPropertyStart(b, name);
			foreach (var category in PlayerTelemetryMath.QueueCategories)
				AiMatchLogWriter.AppendNumber(b, category, counts.GetValueOrDefault(category),
					category == PlayerTelemetryMath.QueueCategories[0]);
			b.Append('}');
		}

		void EconomySample(World world, int tick)
		{
			lastSampleTick = tick;
			foreach (var seat in seats.Values)
			{
				seat.Sample.Harvesters = seat.Sample.Refineries = seat.Sample.Conyards
					= seat.Sample.Production = seat.Sample.Buildings = seat.Sample.Units = 0;
				seat.Sample.ArmyInfantry = seat.Sample.ArmyVehicle = seat.Sample.ArmyAir
					= seat.Sample.ArmyNaval = seat.Sample.ArmyDefence = 0;
				seat.Sample.UnitTypes.Clear();
				seat.TechTier = 0;
			}

			foreach (var actor in world.Actors)
			{
				if (!actor.IsInWorld || actor.IsDead || actor.Owner == null)
					continue;

				var seat = Seat(actor.Owner);
				if (seat == null)
					continue;

				var s = seat.Sample;
				var name = actor.Info.Name;
				if (IsBuilding(actor))
				{
					s.Buildings++;
					seat.TechTier = Math.Max(seat.TechTier, techDepth.GetValueOrDefault(name, 0));
					if (tagMap.TryGetValue(name, out var tags))
					{
						if (tags.Contains(BotTargetTags.Refinery))
							s.Refineries++;
						if (tags.Contains(BotTargetTags.Conyard))
							s.Conyards++;
						if (tags.Contains(BotTargetTags.Production))
							s.Production++;
						if (tags.Contains(BotTargetTags.Defence))
							s.ArmyDefence += CostOf(actor);
					}
				}
				else
				{
					if (tagMap.TryGetValue(name, out var tags) && tags.Contains(BotTargetTags.Harvester))
						s.Harvesters++;

					var category = CategoryOf(actor.Info);
					if (category == "economy")
						continue;

					s.Units++;
					if (s.UnitTypes.Count < info.MaxUnitTypesPerSample || s.UnitTypes.ContainsKey(name))
						s.UnitTypes[name] = s.UnitTypes.TryGetValue(name, out var n) ? n + 1 : 1;
					else
						s.UnitTypes["other"] = s.UnitTypes.TryGetValue("other", out var n) ? n + 1 : 1;

					var cost = CostOf(actor);
					switch (category)
					{
						case "infantry": s.ArmyInfantry += cost; break;
						case "vehicle": s.ArmyVehicle += cost; break;
						case "air": s.ArmyAir += cost; break;
						case "naval": s.ArmyNaval += cost; break;
					}
				}
			}

			foreach (var seat in seats.Values)
			{
				if (seat.Sample.Production > 0 && seat.Player.PlayerActor != null
					&& !seat.Player.PlayerActor.TraitsImplementing<ProductionQueue>().Any(q => q.Enabled && q.AllQueued().Any()))
					seat.IdleProductionTicks += info.SampleIntervalTicks;

				EmitSample(seat, tick);
			}
		}

		void EmitSample(SeatTelemetry seat, int tick)
		{
			var s = seat.Sample;
			var b = new StringBuilder(768);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-player-sample", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendString(b, "record_id", uid + "|" + seat.Seat + "|sample|" + tick);
			AiMatchLogWriter.AppendNumber(b, "tick", tick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendString(b, "bot_type", seat.Player.BotType ?? "");
			AiMatchLogWriter.AppendString(b, "faction", seat.Player.Faction?.InternalName ?? "");
			AiMatchLogWriter.AppendNumber(b, "team", seat.Team);
			AiMatchLogWriter.AppendNumber(b, "harvesters", s.Harvesters);
			AiMatchLogWriter.AppendNumber(b, "refineries", s.Refineries);
			AiMatchLogWriter.AppendNumber(b, "conyards", s.Conyards);
			AiMatchLogWriter.AppendNumber(b, "expansions", Math.Max(0, s.Conyards - 1));

			// Expansion deploys: MCV→ConYard deploys while another ConYard already stood.
			// Individual deploys are all present in the event stream.
			AiMatchLogWriter.AppendNumber(b, "mcv_deploys", seat.ExpansionDeploys);
			AiMatchLogWriter.AppendNumber(b, "production_buildings", s.Production);
			AiMatchLogWriter.AppendNumber(b, "tech_tier", seat.TechTier);
			AiMatchLogWriter.AppendNumber(b, "idle_production_ticks", seat.IdleProductionTicks);
			var unitGate = GateFor(seat);
			if (unitGate?.UnitCashAdmission is { } cash)
			{
				AiMatchLogWriter.AppendObjectPropertyStart(b, "unit_cash_blocked");
				AiMatchLogWriter.AppendNumber(b, "blocked_decisions", cash.Blocked, true);
				AiMatchLogWriter.AppendNumber(b, "classified_decisions", cash.Eligible);
				b.Append('}');
			}

			if (unitGate?.UnitEconomyRoles is { } roles)
			{
				AiMatchLogWriter.AppendString(b, "harvester_role_types", string.Join(",", roles.Harvesters.OrderBy(x => x, StringComparer.Ordinal)));
				AiMatchLogWriter.AppendString(b, "refinery_role_types", string.Join(",", roles.Refineries.OrderBy(x => x, StringComparer.Ordinal)));
			}
			AiMatchLogWriter.AppendNumber(b, "buildings", s.Buildings);
			AiMatchLogWriter.AppendNumber(b, "units", s.Units);

			AiMatchLogWriter.AppendObjectPropertyStart(b, "army");
			AiMatchLogWriter.AppendNumber(b, "infantry", s.ArmyInfantry, true);
			AiMatchLogWriter.AppendNumber(b, "vehicle", s.ArmyVehicle);
			AiMatchLogWriter.AppendNumber(b, "air", s.ArmyAir);
			AiMatchLogWriter.AppendNumber(b, "naval", s.ArmyNaval);
			AiMatchLogWriter.AppendNumber(b, "defence", s.ArmyDefence);
			b.Append('}');

			AiMatchLogWriter.AppendObjectPropertyStart(b, "unit_types");
			var first = true;
			foreach (var kv in s.UnitTypes.OrderBy(x => x.Key, StringComparer.Ordinal))
			{
				AiMatchLogWriter.AppendNumber(b, kv.Key, kv.Value, first);
				first = false;
			}

			b.Append("}}");
			writer.TryWrite(b.ToString());
		}

		// CB_frontier_observe (P3a, integrator ruling O3): the shared record head, extracted so
		// version-2 rows reuse it byte-for-byte — every existing kind keeps emitting at version 1.
		StringBuilder BeginEvent(SeatTelemetry seat, string kind, int version, out int seq)
		{
			// seq is per-seat monotonic across both emit paths: same-tick events keep a
			// recoverable total order (Boss P3a request — placement/build order must survive
			// same-tick ties).
			seq = ++seat.EventSeq;
			var b = new StringBuilder(256);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-player-event", true);
			AiMatchLogWriter.AppendNumber(b, "version", version);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendString(b, "record_id", uid + "|" + seat.Seat + "|" + kind + "|" + world.WorldTick + "|" + seq);
			AiMatchLogWriter.AppendNumber(b, "tick", world.WorldTick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendString(b, "kind", kind);
			AiMatchLogWriter.AppendNumber(b, "seq", seq);
			return b;
		}

		void EmitEvent(SeatTelemetry seat, string kind, string actor, CPos? cell,
			double startDist = -1, string cause = null, string toSeat = null, int units = 0,
			long actorId = -1)
		{
			var b = BeginEvent(seat, kind, 1, out _);
			if (actor != null)
				AiMatchLogWriter.AppendString(b, "actor", actor);
			if (actorId >= 0)
				AiMatchLogWriter.AppendNumber(b, "actor_id", actorId);
			if (cell.HasValue)
				AiMatchLogWriter.AppendString(b, "cell",
					cell.Value.X.ToString(CultureInfo.InvariantCulture) + "," + cell.Value.Y.ToString(CultureInfo.InvariantCulture));
			if (startDist >= 0)
				AiMatchLogWriter.AppendNumber(b, "start_dist", Math.Round(startDist, 1));
			if (cause != null)
				AiMatchLogWriter.AppendString(b, "cause", cause);
			if (toSeat != null)
				AiMatchLogWriter.AppendString(b, "to_seat", toSeat);
			if (units > 0)
				AiMatchLogWriter.AppendNumber(b, "units", units);

			b.Append('}');
			writer.TryWrite(b.ToString());
		}

		// CB_frontier_observe (P3a): version-2 rows carry the shadow stage-machine transition.
		// cell = frozen field centre (the outpost key, O1); reason carries entry/retire/decline
		// causes; escort_seq is always 0 — no escort episode exists until P3c.
		void EmitStageEvent(SeatTelemetry seat, in FrontierStageRow row)
		{
			var b = BeginEvent(seat, "outpost_stage", 2, out _);
			AiMatchLogWriter.AppendString(b, "cell",
				row.Cell.X.ToString(CultureInfo.InvariantCulture) + "," + row.Cell.Y.ToString(CultureInfo.InvariantCulture));
			AiMatchLogWriter.AppendString(b, "stage", row.Stage);
			AiMatchLogWriter.AppendNumber(b, "cover_value", row.CoverValue);
			AiMatchLogWriter.AppendNumber(b, "at_risk_value", row.AtRiskValue);
			if (row.Reason != null)
				AiMatchLogWriter.AppendString(b, "reason", row.Reason);
			AiMatchLogWriter.AppendNumber(b, "episode", row.Episode);
			AiMatchLogWriter.AppendNumber(b, "escort_seq", row.EscortSeq);
			b.Append('}');
			writer.TryWrite(b.ToString());
		}

		void Fail(Exception e, string site)
		{
			ended = true;
			Unsubscribe();
			writer?.Dispose();
			writer = null;
			unitWriter?.Close(0);
			unitWriter = null;
			var frame = e.StackTrace == null ? "unknown" : e.StackTrace.Split('\n')[0].Trim();
			Log.Write("debug", "Player telemetry terminated after a telemetry error: " + e.GetType().Name
				+ " site=" + site + " frame=" + frame);
		}

		void Unsubscribe()
		{
			if (world == null)
				return;
			world.ActorAdded -= ActorAdded;
			world.ActorRemoved -= ActorRemoved;
		}

		void IGameOver.GameOver(World world)
		{
			if (writer == null || ended)
				return;

			ended = true;
			Unsubscribe();
			try
			{
				// A periodic sample on the same tick already holds the snapshot.
				if (world.WorldTick != lastSampleTick)
					EconomySample(world, world.WorldTick);
				if (world.WorldTick != lastQueueTick)
					QueueSample(world, world.WorldTick);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Player telemetry final sample failed: " + e.GetType().Name);
			}

			writer?.Dispose();
			// No terminal row, as the players and memory writers write none. Close hands over complete rows only and the shared
			// drain makes them durable; the file is judged by its records, not by a terminal marker (no reader uses Complete here).
			unitWriter?.Close(world.WorldTick);
			unitWriter = null;
			AiLogWriteQueue.FlushSharedForGameEnd(); // PROD-SAMPLE-LOG-01 FIX: bounded drain, as the memory writer does.
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			Unsubscribe();
			writer?.Dispose();
			unitWriter?.Close(self.World.WorldTick);
			unitWriter = null;
		}
	}
}
