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
	[Desc("PLAYTEST-TELEMETRY-V2: per-player match telemetry for EVERY seat (humans and bots). " +
		"Appends bounded cameo-ai-players-*.jsonl rows: a 750-tick economy/army sample per player " +
		"plus an observed event stream (building placed/lost, ConYard deploys, first enemy-territory " +
		"contact, attack-wave starts from observed movement, superweapon built/fired). Record-only: " +
		"no synced state, no orders, no RNG, same Eligible gate as the other log channels.")]
	public sealed class AiPlayerTelemetryInfo : TraitInfo
	{
		[Desc("World ticks between per-player economy/army samples.")]
		public readonly int SampleIntervalTicks = 750;

		[Desc("World ticks between combat-unit position samples for the wave/territory detectors.")]
		public readonly int MovementIntervalTicks = 50;

		[Desc("Ticks an MCV removal and a ConYard appearance may be apart and still read as a deploy (or repack).")]
		public readonly int DeployMatchWindowTicks = 10;

		[Desc("Maximum cell distance between a removed MCV and a new ConYard that still reads as a deploy.")]
		public readonly int DeployMatchCells = 3;

		[Desc("Combat units simultaneously marching outward needed to open an attack wave.")]
		public readonly int WaveMinUnits = 5;

		[Desc("Marching units below which an open wave closes again (hysteresis).")]
		public readonly int WaveEndUnits = 2;

		[Desc("Cells a unit must gain away from its own start and toward an enemy inside one movement window to count as marching.")]
		public readonly int MarchMinCells = 4;

		[Desc("Cells around an enemy start that count as that enemy's territory.")]
		public readonly int EnemyTerritoryCells = 20;

		[Desc("Per-player cap on tracked combat units for the wave detector.")]
		public readonly int MaxTrackedUnitsPerPlayer = 512;

		[Desc("Distinct unit-type names kept per sample; the rest aggregate into `other`.")]
		public readonly int MaxUnitTypesPerSample = 48;

		[Desc("ConYard removals held back while a matching MCV appearance can still arrive (Transforms remove the source first).")]
		public readonly int MaxPendingConyardRemovals = 64;

		public override object Create(ActorInitializer init) { return new AiPlayerTelemetry(this); }
	}

	/// <summary>Pure helpers of the player telemetry, free of world state so they can be unit-tested.</summary>
	internal static class PlayerTelemetryMath
	{
		/// <summary>Army categories of the sample row, mirroring army_mix_report's class split.</summary>
		public static string MobileCategory(bool aircraft, bool naval, bool infantry)
		{
			if (aircraft)
				return "air";
			if (naval)
				return "naval";
			return infantry ? "infantry" : "vehicle";
		}

		/// <summary>
		/// A combat unit is marching when, inside one movement window, it gained at least
		/// <paramref name="minCells"/> away from its own start AND got closer to an enemy start.
		/// Distances are Euclidean cells; a missing previous enemy distance (-1) cannot prove the
		/// approach leg, so it never marches.
		/// </summary>
		public static bool Marching(double prevHome, double nowHome, double prevEnemy, double nowEnemy, int minCells)
		{
			return prevEnemy >= 0 && nowHome - prevHome >= minCells && nowEnemy < prevEnemy;
		}

		public enum WaveEdge { None, Start, End }

		/// <summary>Hysteresis: a wave opens at <paramref name="startMin"/> marchers, closes below <paramref name="endMin"/>.</summary>
		public static WaveEdge Wave(bool active, int marching, int startMin, int endMin)
		{
			if (!active && marching >= startMin)
				return WaveEdge.Start;
			if (active && marching < endMin)
				return WaveEdge.End;
			return WaveEdge.None;
		}

		/// <summary>Positive (non-negated) prerequisite tokens: `~` hidden markers stripped, `!` inversions dropped.</summary>
		public static List<string> PositivePrereqs(IEnumerable<string> prerequisites)
		{
			var tokens = new List<string>();
			if (prerequisites == null)
				return tokens;

			foreach (var raw in prerequisites)
			{
				var token = (raw ?? "").Trim().TrimStart('~').Trim();
				if (token.Length == 0 || token.StartsWith('!'))
					continue;
				tokens.Add(token.ToLowerInvariant());
			}

			return tokens;
		}

		/// <summary>
		/// Tech depth of every building: 1 + the deepest prerequisite chain, each token resolved
		/// through its shallowest building provider so the depth is the shortest build order that
		/// reaches it. Tokens with no building provider add 0. Deterministic: recursion is
		/// memoized, cycles contribute 0 through the back-edge, roots scan in ordinal order.
		/// </summary>
		public static Dictionary<string, int> TechDepths(
			IReadOnlyDictionary<string, (IReadOnlyList<string> Prereqs, IReadOnlyList<string> Provides)> buildings)
		{
			var providers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
			foreach (var kv in buildings)
				foreach (var token in kv.Value.Provides.Append(kv.Key))
				{
					if (!providers.TryGetValue(token, out var list))
						providers[token] = list = [];
					list.Add(kv.Key);
				}

			var depths = new Dictionary<string, int>(StringComparer.Ordinal);
			var visiting = new HashSet<string>(StringComparer.Ordinal);

			int Depth(string name)
			{
				if (depths.TryGetValue(name, out var d))
					return d;
				if (!buildings.TryGetValue(name, out var spec) || visiting.Contains(name))
					return 0;

				visiting.Add(name);
				var best = 0;
				foreach (var token in spec.Prereqs)
				{
					var shallowest = int.MaxValue;
					if (providers.TryGetValue(token, out var list))
						foreach (var provider in list)
							if (provider != name)
							{
								var candidate = Depth(provider);
								if (candidate > 0 && candidate < shallowest)
									shallowest = candidate;
							}

					if (shallowest != int.MaxValue && shallowest + 1 > best)
						best = shallowest + 1;
				}

				visiting.Remove(name);
				return depths[name] = Math.Max(1, best);
			}

			foreach (var name in buildings.Keys.OrderBy(n => n, StringComparer.Ordinal))
				Depth(name);

			return depths;
		}
	}

	internal sealed class BuildingTrack
	{
		public Actor Actor;
		public OpenRA.Player Owner;
	}

	internal sealed class DeployEvent
	{
		public OpenRA.Player Owner;
		public CPos Cell;
		public int Tick;
	}

	internal sealed class PendingConyardRemoval
	{
		public OpenRA.Player Owner;
		public string ActorName;
		public CPos Cell;
		public int Tick;
		public bool WasDead;
	}

	internal sealed class SeatSample
	{
		public int Harvesters, Refineries, Conyards, Production, Buildings, Units;
		public long ArmyInfantry, ArmyVehicle, ArmyAir, ArmyNaval, ArmyDefence;
		public readonly Dictionary<string, int> UnitTypes = new(StringComparer.Ordinal);
	}

	internal sealed class SeatTelemetry
	{
		public OpenRA.Player Player;
		public string Seat;
		public CPos Home;
		public int Team;
		public bool WaveActive;
		public bool FirstUnitLogged;
		public int ExpansionDeploys;
		public int IdleProductionTicks;
		public int TechTier;
		public readonly SeatSample Sample = new();
		public readonly Dictionary<uint, CPos> Units = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> Dists = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> PreviousDists = new();
		public readonly Dictionary<string, bool> PowerReady = new(StringComparer.Ordinal);
	}

	public sealed class AiPlayerTelemetry : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		readonly AiPlayerTelemetryInfo info;
		readonly Dictionary<uint, BuildingTrack> buildings = new();
		readonly List<DeployEvent> mcvRemoved = [];
		readonly List<PendingConyardRemoval> pendingConyardRemoved = [];
		readonly Dictionary<OpenRA.Player, SeatTelemetry> seats = new();
		static readonly List<(string Seat, CPos Home)> NoEnemies = [];

		IReadOnlyDictionary<string, HashSet<string>> tagMap;
		Dictionary<string, string> categoryCache;
		Dictionary<string, int> techDepth;
		HashSet<string> superpowerTokens;
		AiEconomyHealthLogWriter writer;
		World world;
		string uid;
		bool ended;

		public AiPlayerTelemetry(AiPlayerTelemetryInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
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
			}
			// Fail closed on any setup fault — telemetry must never prevent a world from loading.
			catch (Exception e)
			{
				writer?.Dispose();
				writer = null;
				Log.Write("debug", "Player telemetry unavailable: " + e.GetType().Name);
				return;
			}

			try
			{
				tagMap = BotTargetTags.BuildTagMap(world.Map.Rules);
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
				Fail(e);
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
						startDist: Cells(actor.Location, seat.Home));
					if (IsConyardBuilding(actor, tagMap)
						&& TryMatchDeploy(mcvRemoved, actor.Owner, actor.Location, world.WorldTick))
					{
						// An expansion deploy adds a ConYard while one already stands; the
						// starting MCV's first deploy (none owned) and a pack/redeploy
						// relocation (the last ConYard left) do not count.
						var conyards = 0;
						foreach (var other in world.Actors)
							if (other != actor && other.IsInWorld && !other.IsDead
								&& other.Owner == actor.Owner && IsConyardBuilding(other, tagMap))
								conyards++;

						if (conyards > 0)
							seat.ExpansionDeploys++;

						EmitEvent(seat, "conyard_deployed", actor.Info.Name, actor.Location,
							startDist: Cells(actor.Location, seat.Home));
					}

					if (tagMap.TryGetValue(actor.Info.Name, out var tags) && tags.Contains(BotTargetTags.Superweapon))
						EmitEvent(seat, "superweapon_built", actor.Info.Name, actor.Location,
							startDist: Cells(actor.Location, seat.Home));
				}
				else if (IsMobileConyard(actor))
				{
					// Transform removes the source first (Transform.cs: Dispose then CreateActor), so
					// an MCV arriving now can close a ConYard removal that is still pending — repack.
					for (var i = pendingConyardRemoved.Count - 1; i >= 0; i--)
					{
						var p = pendingConyardRemoved[i];
						if (p.Owner != actor.Owner
							|| Math.Abs(p.Tick - world.WorldTick) > info.DeployMatchWindowTicks
							|| Math.Max(Math.Abs(p.Cell.X - actor.Location.X), Math.Abs(p.Cell.Y - actor.Location.Y)) > info.DeployMatchCells)
							continue;

						pendingConyardRemoved.RemoveAt(i);
						var seat = Seat(actor.Owner);
						if (seat != null)
							EmitEvent(seat, "conyard_packed", p.ActorName, p.Cell,
								startDist: Cells(p.Cell, seat.Home));
						break;
					}
				}
			}
			catch (Exception e)
			{
				Fail(e);
			}
		}

		void ActorRemoved(Actor actor)
		{
			if (ended || writer == null)
				return;

			try
			{
				if (IsMobileConyard(actor))
				{
					if (mcvRemoved.Count < info.MaxPendingConyardRemovals)
						mcvRemoved.Add(new DeployEvent { Owner = actor.Owner, Cell = actor.Location, Tick = world.WorldTick });
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
							Cell = actor.Location,
							Tick = world.WorldTick,
							WasDead = actor.IsDead
						});
					return;
				}

				var seat = Seat(actor.Owner);
				if (seat != null)
					EmitEvent(seat, "structure_lost", actor.Info.Name, actor.Location,
						cause: actor.IsDead ? "killed" : "sold",
						startDist: Cells(actor.Location, seat.Home));
			}
			catch (Exception e)
			{
				Fail(e);
			}
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

		void PruneEvents(int tick)
		{
			var horizon = tick - info.DeployMatchWindowTicks * 2;
			mcvRemoved.RemoveAll(e => e.Tick < horizon);

			for (var i = pendingConyardRemoved.Count - 1; i >= 0; i--)
			{
				var p = pendingConyardRemoved[i];
				if (p.Tick >= horizon)
					continue;

				pendingConyardRemoved.RemoveAt(i);
				var seat = Seat(p.Owner);
				if (seat != null)
					EmitEvent(seat, "structure_lost", p.ActorName, p.Cell,
						cause: p.WasDead ? "killed" : "sold",
						startDist: Cells(p.Cell, seat.Home));
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

				if (tick % info.SampleIntervalTicks == 0)
					EconomySample(self.World, tick);
			}
			// Fail closed: a sampling fault ends the channel, never the game.
			catch (Exception e)
			{
				Fail(e);
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
						startDist: Cells(track.Actor.Location, loser.Home));
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
				if (!actor.IsInWorld || actor.IsDead || actor.Owner == null)
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
							actor.Location, toSeat: reachSeat);
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

		void EconomySample(World world, int tick)
		{
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

		void EmitEvent(SeatTelemetry seat, string kind, string actor, CPos? cell,
			double startDist = -1, string cause = null, string toSeat = null, int units = 0)
		{
			var b = new StringBuilder(256);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-player-event", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendString(b, "record_id", uid + "|" + seat.Seat + "|" + kind + "|" + world.WorldTick + "|" + (actor ?? ""));
			AiMatchLogWriter.AppendNumber(b, "tick", world.WorldTick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendString(b, "kind", kind);
			if (actor != null)
				AiMatchLogWriter.AppendString(b, "actor", actor);
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

		void Fail(Exception e)
		{
			ended = true;
			Unsubscribe();
			writer?.Dispose();
			writer = null;
			Log.Write("debug", "Player telemetry terminated after a telemetry error: " + e.GetType().Name);
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
				EconomySample(world, world.WorldTick);
			}
			catch (Exception e)
			{
				Log.Write("debug", "Player telemetry final sample failed: " + e.GetType().Name);
			}

			writer?.Dispose();
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			Unsubscribe();
			writer?.Dispose();
		}
	}
}
