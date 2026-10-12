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

		[Desc("World ticks between per-bot production-queue samples (WBL-P1 conversion diagnostics).")]
		public readonly int QueueIntervalTicks = 250;

		[Desc("Per-seat cap on queue rows inside one bot-queue sample.")]
		public readonly int MaxQueueRowsPerSeat = 32;

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

		/// <summary>Spend categories of the bot-queue row (WBL-P1 spec: unit, defence, crawl/link, tech, economy).</summary>
		public static readonly string[] QueueCategories = ["unit", "defence", "crawl_link", "tech", "economy"];

		/// <summary>Queue row state: disabled beats busy; a paused head item reads paused, not idle.</summary>
		public static string QueueState(bool enabled, bool hasHead, bool paused)
		{
			if (!enabled)
				return "disabled";
			if (!hasHead)
				return "idle";
			return paused ? "paused" : "busy";
		}

		/// <summary>
		/// Idle-queue hold reason in the unit builder's own gate order: a pause vote (cash
		/// reserved by a named provider) outranks the cash floor, which outranks the
		/// bank-saturation sweep stop, then prerequisites and the unit cap; the remainder
		/// is genuinely no desire. Inferred from the same public signals the module reads.
		/// </summary>
		public static string IdleReason(bool reserved, bool belowCashFloor, bool saturated,
			bool noBuildable, bool atUnitCap)
		{
			if (reserved)
				return "cash_reserved";
			if (belowCashFloor)
				return "no_cash";
			if (saturated)
				return "saturation_stop";
			if (noBuildable)
				return "prerequisite_missing";
			if (atUnitCap)
				return "unit_cap";
			return "no_desire";
		}

		/// <summary>
		/// Spend category of a produced item: defence types and the defence tag map to
		/// defence, refinery types/tag to economy, fragile (crawl/link family) types to
		/// crawl_link, every other building to tech. Unit-queue items are always unit.
		/// </summary>
		public static string BuildingCategory(bool defence, bool refinery, bool crawlLink)
		{
			if (defence)
				return "defence";
			if (refinery)
				return "economy";
			if (crawlLink)
				return "crawl_link";
			return "tech";
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
		public long ActorId;
		public CPos Cell;
		public int Tick;
		public string Cause;
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
		public int EventSeq;
		public int IdleProductionTicks;
		public int TechTier;
		public readonly SeatSample Sample = new();
		public readonly Dictionary<uint, CPos> Units = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> Dists = new();
		public readonly Dictionary<uint, (double Home, double Enemy)> PreviousDists = new();
		public readonly Dictionary<string, bool> PowerReady = new(StringComparer.Ordinal);

		// WBL-P1 conversion diagnostics (bot seats only): per-window counters reset after
		// each queue sample; QueueHeads persists so a new head item reads as an acceptance.
		public bool QueueHelpersResolved;
		public int LastSpent = -1;
		public BaseBuilderBotModuleCA BaseBuilder;
		public UnitBuilderBotModuleCA UnitBuilder;
		public BotLimits Limits;
		public PlayerResources Resources;
		public IBotPersonalityLeadProvider[] LeadProviders = [];
		public readonly Dictionary<string, int> QueueRequested = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueAccepted = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueCompleted = new(StringComparer.Ordinal);
		public readonly Dictionary<string, long> QueueSpend = new(StringComparer.Ordinal);
		public readonly Dictionary<string, int> QueueSkipped = new(StringComparer.Ordinal);
		public readonly Dictionary<string, ProductionItem> QueueHeads = new(StringComparer.Ordinal);

		// Order-issue markers ("{actorId}:{queueType}|{item}") left by the unit-production
		// seam so the head-diff does not count the same acceptance a second time.
		public readonly Dictionary<string, int> SeamStarts = new(StringComparer.Ordinal);
	}

	public sealed class AiPlayerTelemetry : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing, IBotUnitProductionObserver
	{
		readonly AiPlayerTelemetryInfo info;
		readonly Dictionary<uint, BuildingTrack> buildings = new();
		readonly List<DeployEvent> mcvRemoved = [];
		readonly List<PendingConyardRemoval> pendingConyardRemoved = [];
		readonly Dictionary<OpenRA.Player, SeatTelemetry> seats = new();
		static readonly List<(string Seat, CPos Home)> NoEnemies = [];
		int lastSampleTick = -1;
		int lastQueueTick = -1;

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
					var deployMatched = IsConyardBuilding(actor, tagMap)
						&& TryMatchDeploy(mcvRemoved, actor.Owner, actor.Location, world.WorldTick);
					if (deployMatched)
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
							startDist: Cells(actor.Location, seat.Home), actorId: actor.ActorID);
					}
					else
					{
						// A ConYard only ever appears through an MCV transform, never from a
						// queue — deploys are not completed production; the MCV was counted
						// as a completed unit when it appeared.
						var bot = BotSeat(actor.Owner);
						if (bot != null)
							Bump(bot.QueueCompleted, BuildingCategoryOf(bot, actor.Info.Name));
					}

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
						Bump(bot.QueueCompleted, "unit");
				}
				else if (BotSeat(actor.Owner) is { } bot)
				{
					// A mobile actor appearing is a completed unit production for the bot-queue
					// row (transforms land in the building branch's deploy match instead).
					Bump(bot.QueueCompleted, "unit");
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
					Bump(seat.QueueRequested, "unit");
			}
			catch (Exception e)
			{
				Fail(e);
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
				seat.QueueSpend["unit"] = seat.QueueSpend.GetValueOrDefault("unit") + CostOfName(actor);

				// The poll's head-diff would see this order again when the item reaches
				// the head — mark it so the acceptance is counted exactly once. A stale
				// marker (item cancelled before heading) at most skips one later same-name
				// head, which is the same single acceptance in the window accounting.
				var actorId = owner.PlayerActor?.ActorID;
				if (actorId.HasValue)
					Bump(seat.SeamStarts, actorId.Value + ":" + queueType + "|" + actor);
			}
			catch (Exception e)
			{
				Fail(e);
			}
		}

		void IBotUnitProductionObserver.UnitProductionSkipped(OpenRA.Player owner, int tick, string actor, string reason)
		{
			try
			{
				var seat = BotSeat(owner);
				if (seat != null && reason != null)
					Bump(seat.QueueSkipped, reason);
			}
			catch (Exception e)
			{
				Fail(e);
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
			var crawl = builder != null && builder.Info.FragileTypes.Contains(actorName);
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

			seat.UnitBuilder = actor.TraitsImplementing<UnitBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			seat.BaseBuilder = actor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			seat.Limits = actor.TraitsImplementing<BotLimits>().FirstEnabledTraitOrDefault();
			seat.Resources = actor.TraitOrDefault<PlayerResources>();
			seat.LeadProviders = actor.TraitsImplementing<IBotPersonalityLeadProvider>().ToArray();
		}

		bool UnitCapped(SeatTelemetry seat, string[] buildable,
			Dictionary<string, int> owned, Dictionary<string, int> queued)
		{
			var limits = seat.UnitBuilder?.Info.UnitLimits;
			if (limits == null || buildable.Length == 0)
				return false;

			foreach (var name in buildable)
				if (!limits.TryGetValue(name, out var cap)
					|| owned.GetValueOrDefault(name) + queued.GetValueOrDefault(name) < cap)
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

				// The same gates the unit builder consults, in its own decision order.
				var voterActor = seat.Player.PlayerActor;
				var voters = voterActor == null || voterActor.IsDead || voterActor.Disposed
					? []
					: voterActor.TraitsImplementing<IBotRequestPauseUnitProduction>()
						.Where(v => v.IsTraitEnabled() && v.PauseUnitProduction)
						.Select(v => v.GetType().Name)
						.OrderBy(n => n, StringComparer.Ordinal)
						.ToArray();
				var cashFloor = seat.UnitBuilder != null
					? BotPersonalityLeads.Scaled(seat.UnitBuilder.Info.ProductionMinCashRequirement,
						BotPersonalityLeads.Lean(seat.LeadProviders, "steamroller"))
					: 0;
				var threshold = seat.Limits?.Info.BankSaturationCash > 0
					&& (seat.UnitBuilder?.Info.BankSaturationCash ?? 0) > 0
					? seat.Limits.Info.BankSaturationCash
					: 0;
				var saturated = ProductionSaturationCA.Saturated(cash, threshold);
				var belowFloor = cash < cashFloor;
				var reserved = voters.Length > 0;

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
							if (names.Length > 0 && !reserved && !belowFloor && !saturated)
							{
								owned ??= CountOwned(seat);
								queued ??= CountQueued(seat);
								capped = UnitCapped(seat, names, owned, queued);
							}

							reason = PlayerTelemetryMath.IdleReason(reserved, belowFloor, saturated,
								names.Length == 0, capped);
							if (reason == "cash_reserved")
								reservedBy = voters;
							break;
						}
					}

					// Acceptance: a different head item than the last poll means this queue
					// started producing that item between samples (reference compare — a
					// same-name re-queue is still a new episode). Starts already counted by
					// the unit-production seam are consumed, not counted twice; starts from
					// other paths (building queues, direct orders) are counted here.
					var key = pair.Actor.ActorID + ":" + queue.Info.Type;
					if (!seat.QueueHeads.TryGetValue(key, out var prev) || !ReferenceEquals(prev, head))
					{
						if (head != null)
						{
							var seamKey = key + "|" + head.Item;
							if (seat.SeamStarts.TryGetValue(seamKey, out var pending) && pending > 0)
							{
								if (pending == 1)
									seat.SeamStarts.Remove(seamKey);
								else
									seat.SeamStarts[seamKey] = pending - 1;
							}
							else
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
			double startDist = -1, string cause = null, string toSeat = null, int units = 0,
			long actorId = -1)
		{
			// seq is per-seat monotonic: same-tick events keep a recoverable total order
			// (Boss P3a request — placement/build order must survive same-tick ties).
			var seq = ++seat.EventSeq;
			var b = new StringBuilder(256);
			AiMatchLogWriter.AppendObjectStart(b);
			AiMatchLogWriter.AppendString(b, "schema", "cameo-player-event", true);
			AiMatchLogWriter.AppendNumber(b, "version", 1);
			AiMatchLogWriter.AppendString(b, "game_uid", uid);
			AiMatchLogWriter.AppendString(b, "record_id", uid + "|" + seat.Seat + "|" + kind + "|" + world.WorldTick + "|" + seq);
			AiMatchLogWriter.AppendNumber(b, "tick", world.WorldTick);
			AiMatchLogWriter.AppendString(b, "seat", seat.Seat);
			AiMatchLogWriter.AppendString(b, "kind", kind);
			AiMatchLogWriter.AppendNumber(b, "seq", seq);
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
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			Unsubscribe();
			writer?.Dispose();
		}
	}
}
