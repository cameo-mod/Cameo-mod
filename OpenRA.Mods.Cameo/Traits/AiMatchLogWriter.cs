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
using System.Globalization;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Appends record-only AI match logs for bot players.")]
	public class AiMatchLogWriterInfo : TraitInfo
	{
		[Desc("Name of the append-only AI match log file.")]
		public readonly string FileName = "cameo-ai-matches.jsonl";

		[Desc("World ticks between two stats_timeline samples (earned, spent, army, assets, kills/deaths cost). 0 disables it.")]
		public readonly int SampleIntervalTicks = 750;

		public override object Create(ActorInitializer init) { return new AiMatchLogWriter(this); }
	}

	public class AiMatchLogWriter : IWorldLoaded, IGameOver, ITick
	{
		readonly AiMatchLogWriterInfo info;

		string fallbackGameUid;
		string pendingText;
		AiLogFileAppender appender;
		bool written;
		bool eligibleAtWorldLoad;
		int nextAttemptTick;
		BotTakeoverTracker takeover;
		readonly Dictionary<OpenRA.Player, List<int[]>> samples = new();
		readonly Dictionary<OpenRA.Player, BotModules.BotFogMemory> signatureMemory = new();
		readonly Dictionary<(OpenRA.Player Observer, OpenRA.Player Enemy), List<SignatureSample>> signatures = new();
		readonly BotModules.MasterAiBotModuleInfo signatureInfo = new();
		bool gameEnded;

		internal sealed class SignatureSample
		{
			public int Tick;
			public BotModules.EnemyProfile Seen;
			public BotModules.ObservedActor[] OwnActors;
		}

		internal static bool SignatureSampleDue(int tick, int lastTick, bool changed, int floorTicks) =>
			lastTick < 0 || changed || tick - lastTick >= Math.Max(1, floorTicks);

		internal static bool TruthCaptureReady(bool gameEnded, bool allResolved) => gameEnded;


		public AiMatchLogWriter(AiMatchLogWriterInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			takeover = world.WorldActor.TraitOrDefault<BotTakeoverTracker>();

			// Save replay-in eventually clears IsLoadingGameSave. Keep the exclusion
			// for this world's entire lifetime, including its eventual GameOver.
			// The owner check (admin host or elected takeover controller) is deferred to
			// capture: the controller is only elected once the tracker itself loads.
			eligibleAtWorldLoad = Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, host: true);
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
			var world = self.World;
			// Separate, record-only fog memory: never reuse an omniscient bot profile.
			if (!written && eligibleAtWorldLoad && IsLogOwner() && world.WorldTick % 25 == 0)
				SampleSignatures(world);
			if (!written && info.SampleIntervalTicks > 0 && world.WorldTick % info.SampleIntervalTicks == 0)
				Sample(world);

			if (written || world.WorldTick < nextAttemptTick)
				return;

			if (pendingText != null)
			{
				TryAppend(world.WorldTick);
				return;
			}


		}

		void IGameOver.GameOver(World world)
		{
			// World.EndGame pauses before dispatching IGameOver. Finish the appender's
			// bounded mutex retries here, because no future simulation tick is guaranteed.
			gameEnded = true;
			if (eligibleAtWorldLoad && IsLogOwner())
				SampleSignatures(world);
			CaptureAndAppend(world);
			for (var i = 0; i < 7 && !written && pendingText != null; i++)
				TryAppend(world.WorldTick);
		}

		void CaptureAndAppend(World world)
		{
			if (!TruthCaptureReady(gameEnded, false))
				return;
			if (written)
				return;

			if (!eligibleAtWorldLoad || world.Type != WorldType.Regular || world.IsReplay)
			{
				written = true;
				return;
			}

			// Not (yet) the writing process: a client that is neither host nor the current
			// controller stays silent but keeps the record open — it may be elected after
			// the admin drops, and latching written here would lose that forever
			// (boss_review T1/T4).
			if (!IsLogOwner())
				return;

			pendingText ??= BuildLog(world);
			if (string.IsNullOrEmpty(pendingText))
			{
				// An empty record only means nothing loggable has resolved yet. It latches
				// "done" ONLY when nothing more can appear: while a bound human seat can
				// still convert to a takeover bot the capture stays open (boss_review T1).
				pendingText = null;
				if (!OpenTakeoverSeats())
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

		// Log ownership in a takeover-enabled match belongs to the elected controller from
		// match start (boss_review T4 rev-2): a bound client re-elected on every synced
		// disconnect is the sole writer, so spectator connectivity — including a departed
		// spectator admin no notify can ever report — never governs the record. Every client
		// computes the same controller from synced state, so exactly one process writes.
		// Without takeover (or with no electable client left) the host writes as before.
		bool IsLogOwner()
		{
			return OwnerIsLocal(Game.IsHost, takeover != null && takeover.Enabled,
				takeover?.Controller ?? -1, Game.LocalClientId);
		}

		internal static bool OwnerIsLocal(bool host, bool takeoverEnabled, int controller, int localClientId)
		{
			if (takeoverEnabled && controller >= 0)
				return controller == localClientId;

			return host;
		}

		// A seat converted to the takeover AI is a bot for logging purposes even though
		// Player.IsBot stays false (the field is read-only engine state).
		bool IsLoggedPlayer(OpenRA.Player player)
		{
			return IsLoggableBot(player) || (takeover != null && takeover.IsTakenOver(player));
		}

		// Fixed tick cadence, unlike PlayerStatistics' own graph samples, which follow wall-clock
		// game time (every 3000 ticks at the harness's maximum speed, 750 at normal speed).
		void Sample(World world)
		{
			foreach (var player in world.Players.Where(IsEligiblePlayer))
			{
				var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
				var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
				if (!samples.TryGetValue(player, out var list))
					samples[player] = list = new List<int[]>();

				list.Add(new[]
				{
					world.WorldTick, resources?.Earned ?? 0, resources?.Spent ?? 0, stats?.ArmyValue ?? 0,
					stats?.AssetsValue ?? 0, stats?.KillsCost ?? 0, stats?.DeathsCost ?? 0,
					(resources?.Cash ?? 0) + (resources?.Resources ?? 0), IdleQueues(player)
				});
			}
		}

		// The arsenal ledger (AI_ARCHITECTURE.md §12.3): per own actor type, created / lost / value lost / value
		// destroyed, and what it destroyed by victim type — the input of the offline fitter (CA-1b) and the
		// per-enemy-faction profiles (DESIGN.md §19.2).
		/// <summary>
		/// LC5: the ownership watchdog's distinct violations for this bot, by kind and by actor type, plus the first
		/// few violations of each kind with the holder detail (`examples`). Absent when the watchdog did not run
		/// (a match with a human, or a bot without it), so "no field" never reads as "no violations".
		/// </summary>
		internal static void AppendOwnership(StringBuilder builder, BotModules.BotOwnershipWatchdog watchdog)
		{
			if (watchdog != null)
				AppendOwnership(builder, watchdog.Passes, watchdog.Counts, watchdog.CountsByType, watchdog.ExamplesByKind);
		}

		internal static void AppendOwnership(StringBuilder builder, int checks,
			IReadOnlyDictionary<BotModules.BotOwnershipViolation, int> counts,
			IReadOnlyDictionary<(BotModules.BotOwnershipViolation Kind, string Type), int> byType,
			IReadOnlyDictionary<BotModules.BotOwnershipViolation, List<(int Tick, string Type, uint ActorId, string Detail)>> examples = null)
		{
			AppendObjectPropertyStart(builder, "ownership");
			AppendNumber(builder, "checks", checks, true);
			foreach (var kind in Enum.GetValues<BotModules.BotOwnershipViolation>())
				AppendNumber(builder, SnakeCase(kind.ToString()), counts.GetValueOrDefault(kind));

			AppendArrayPropertyStart(builder, "by_type");
			var i = 0;
			foreach (var ((kind, type), count) in byType.OrderByDescending(kv => kv.Value)
				.ThenBy(kv => kv.Key.Kind).ThenBy(kv => kv.Key.Type, StringComparer.Ordinal))
			{
				if (i++ > 0)
					builder.Append(',');

				AppendObjectStart(builder);
				AppendString(builder, "kind", SnakeCase(kind.ToString()), true);
				AppendString(builder, "type", type);
				AppendNumber(builder, "units", count);
				builder.Append('}');
			}

			builder.Append(']');
			AppendArrayPropertyStart(builder, "examples");
			i = 0;
			if (examples != null)
				foreach (var (kind, list) in examples.OrderBy(kv => kv.Key))
					foreach (var e in list.OrderBy(e => e.Tick).ThenBy(e => e.Type, StringComparer.Ordinal).ThenBy(e => e.ActorId))
					{
						if (i++ > 0)
							builder.Append(',');

						AppendObjectStart(builder);
						AppendString(builder, "kind", SnakeCase(kind.ToString()), true);
						AppendNumber(builder, "tick", e.Tick);
						AppendString(builder, "type", e.Type);
						AppendNumber(builder, "actor_id", (long)e.ActorId);
						AppendString(builder, "detail", e.Detail);
						builder.Append('}');
					}

			builder.Append("]}");
		}

		/// <summary>
		/// DESIGN §19.6: what the order gate did for this bot — orders refused (another module held the unit), preempted
		/// (an emergency took it), conflicts (watch mode: would have been refused), crossed orders (two modules ordered one
		/// unit inside the window), plus the module pairs. Written only for bots with a lease registry (genericbot).
		/// </summary>
		internal static void AppendOrderGate(StringBuilder builder, ModularBot bot)
		{
			if (bot == null)
				return;

			var g = bot.OrderGate;
			AppendObjectPropertyStart(builder, "order_gate");
			AppendNumber(builder, "refused", g.Refused, true);
			AppendNumber(builder, "preempted", g.Preempted);
			AppendNumber(builder, "conflicts", g.Conflicts);
			AppendNumber(builder, "crossed", g.Crossed);
			AppendNumber(builder, "unattributed", g.Unattributed);
			AppendNumber(builder, "dropped_full_queue", bot.DroppedOrders);
			AppendArrayPropertyStart(builder, "pairs");
			var i = 0;
			foreach (var ((issuer, holder, verdict), n) in g.Pairs.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.Issuer, StringComparer.Ordinal))
			{
				if (i++ > 0)
					builder.Append(',');

				AppendObjectStart(builder);
				AppendString(builder, "issuer", issuer ?? "", true);
				AppendString(builder, "holder", holder ?? "");
				AppendString(builder, "verdict", SnakeCase(verdict.ToString()));
				AppendNumber(builder, "orders", n);
				builder.Append('}');
			}

			builder.Append(']');
			AppendArrayPropertyStart(builder, "crossed_pairs");
			i = 0;
			foreach (var ((first, second), n) in g.CrossedPairs.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key.First, StringComparer.Ordinal))
			{
				if (i++ > 0)
					builder.Append(',');

				AppendObjectStart(builder);
				AppendString(builder, "first", first, true);
				AppendString(builder, "then", second);
				AppendNumber(builder, "orders", n);
				builder.Append('}');
			}

			builder.Append("]}");
		}

		static string SnakeCase(string pascal) =>
			string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

		internal static void AppendArsenal(StringBuilder builder, BotArsenalLedger ledger, bool first = false)
		{
			AppendArrayPropertyStart(builder, "arsenal", first);
			if (ledger != null)
			{
				var i = 0;
				foreach (var (type, e) in ledger.Ordered())
				{
					if (i++ > 0)
						builder.Append(',');
					AppendObjectStart(builder);
					AppendString(builder, "type", type, true);
					AppendNumber(builder, "created", e.Created);
					AppendNumber(builder, "lost", e.Lost);
					AppendNumber(builder, "lost_value", e.LostValue);
					AppendNumber(builder, "killed_value", e.KilledValue);
					AppendObjectPropertyStart(builder, "killed_by_victim");
					var j = 0;
					foreach (var (victim, value) in e.KilledValueByVictim.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal))
						AppendNumber(builder, victim, value, j++ == 0);
					builder.Append("}}");
				}
			}

			builder.Append(']');
		}

		internal const string StatsTimelineFields = "tick,earned,spent,army_value,assets_value,kills_cost,deaths_cost,banked,idle_queues";

		// Discipline telemetry (AI_DEEP_RESEARCH.md §13 item 1): player-level production queues that could build
		// something but have nothing queued. Humans leave factories idle; a strong bot should not. Building-level
		// queues (per-factory production) are not counted.
		static int IdleQueues(OpenRA.Player player)
		{
			var idle = 0;
			foreach (var q in player.PlayerActor.TraitsImplementing<ProductionQueue>())
				if (q.Enabled && !q.AllQueued().Any() && q.BuildableItems().Any())
					idle++;

			return idle;
		}

		internal static void AppendStatsTimeline(StringBuilder builder, IReadOnlyList<int[]> timeline, bool first = false)
		{
			AppendString(builder, "stats_timeline_fields", StatsTimelineFields, first);
			AppendArrayPropertyStart(builder, "stats_timeline");
			if (timeline != null)
				for (var i = 0; i < timeline.Count; i++)
				{
					if (i > 0)
						builder.Append(',');
					builder.Append('[').Append(string.Join(",", timeline[i].Select(v => v.ToString(CultureInfo.InvariantCulture)))).Append(']');
				}

			builder.Append(']');
		}

		internal static bool CaptureReady(IEnumerable<WinState> loggedStates, bool openTakeoverSeats)
		{
			return !openTakeoverSeats && loggedStates.All(s => s != WinState.Undefined);
		}

		bool OpenTakeoverSeats()
		{
			return takeover != null && takeover.HasOpenSeats;
		}

		static int LoggedTeam(World world, BotTakeoverTracker tracker, OpenRA.Player player)
		{
			// Lobby teams live on client rows: a departed takeover seat's row is gone, so the
			// tracker's load-time snapshot (lobby teams for every slot incl. bots, boss_review
			// T2) is the source of truth when mounted. Without it, the live row still serves.
			if (tracker != null)
				return tracker.TeamOfPlayer(player);

			return world.LobbyInfo.ClientWithIndex(player.ClientIndex)?.Team ?? player.PlayerReference.Team;
		}

		internal static bool Eligible(WorldType type, bool replay, bool loadingSave, bool host)
		{
			return type == WorldType.Regular && !replay && !loadingSave && host;
		}

		// Stable, anonymous within-match key. PlayerReference.Name can be map-authored,
		// so never persist it even when it looks like a lobby slot.
		internal static string SeatKey(World world, OpenRA.Player player) =>
			"seat_" + (Array.IndexOf(world.Players, player) + 1).ToString(CultureInfo.InvariantCulture);

		string BuildLog(World world)
		{
			var gameUid = world.LobbyInfo.GlobalSettings.GameUid;
			if (string.IsNullOrEmpty(gameUid))
				gameUid = fallbackGameUid;

			var lines = new StringBuilder();
			foreach (var player in world.Players.Where(IsEligiblePlayer))
			{
				var recorder = player.PlayerActor.TraitOrDefault<AiMatchLogRecorder>();
				var stats = player.PlayerActor.TraitOrDefault<PlayerStatistics>();
				var resources = player.PlayerActor.TraitOrDefault<PlayerResources>();
				var team = LoggedTeam(world, takeover, player);
				TakeoverRecord takeoverRecord = null;
				takeover?.TryGetTakeoverRecord(player, out takeoverRecord);

				AppendObjectStart(lines);
				AppendNumber(lines, "schema", 3, true);
				AppendString(lines, "record_id", gameUid + "|" + SeatKey(world, player));
				AppendString(lines, "recorded_utc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
				AppendString(lines, "mod_version", Game.ModData.Manifest.Metadata.Version);
				AppendString(lines, "game_uid", gameUid);
				AppendString(lines, "map_uid", world.Map.Uid);
				AppendString(lines, "map_title", world.Map.Title);
				AppendNumber(lines, "duration_ticks", world.WorldTick);
				AppendNumber(lines, "timestep", world.Timestep);

				AppendObjectPropertyStart(lines, "player");
				AppendString(lines, "seat", SeatKey(world, player), true);
				AppendString(lines, "bot_type", player.BotType ?? takeoverRecord?.BotType);
				AppendString(lines, "faction", player.Faction.InternalName);
				AppendNumber(lines, "team", team);
				AppendNumber(lines, "handicap", player.Handicap);
				AppendNumber(lines, "spawn", player.SpawnPoint);

				// SpawnPoint is the lobby's choice and stays 0 for map-side players (the A/B harness),
				// so the home cell is what tells the two sides of a duel apart.
				AppendString(lines, "home", $"{player.HomeLocation.X},{player.HomeLocation.Y}");
				AppendString(lines, "outcome", Outcome(player.WinState));
				AppendString(lines, "personality", recorder?.CurrentPersonality ?? "");
				AppendNumber(lines, "personality_switches", recorder?.PersonalitySwitches ?? 0);
				var priorsStates = player.PlayerActor.TraitsImplementing<OpenRA.Mods.CA.Traits.BotModuleLogic.IBotEngagementPriors>()
					.Select(p => p.PriorsState).Where(s => s != null).ToArray();
				if (priorsStates.Length > 0)
					AppendString(lines, "priors_state", string.Join("+", priorsStates));

				AppendTimeline(lines, recorder?.PersonalityTimeline);
				AppendString(lines, "composition", recorder?.CurrentComposition ?? "");
				AppendNumber(lines, "composition_switches", recorder?.CompositionSwitches ?? 0);
				AppendCompositionTimeline(lines, recorder?.CompositionTimeline);
				AppendEpisodeTimeline(lines, recorder?.EpisodeTimeline);
				lines.Append('}');

				// Takeover provenance for learning/A-B exclusion; absent for ordinary bots.
				if (takeoverRecord != null)
				{
					AppendObjectPropertyStart(lines, "takeover");
					AppendNumber(lines, "taken_over_at", takeoverRecord.Tick, true);
					AppendString(lines, "trigger", takeoverRecord.Trigger == TakeoverTrigger.Surrender ? "surrender" : "disconnect");
					AppendString(lines, "bot_type", takeoverRecord.BotType);
					lines.Append('}');
				}

				AppendObjectPropertyStart(lines, "stats");
				AppendNumber(lines, "units_killed", stats?.UnitsKilled ?? 0, true);
				AppendNumber(lines, "units_lost", stats?.UnitsDead ?? 0);
				AppendNumber(lines, "buildings_killed", stats?.BuildingsKilled ?? 0);
				AppendNumber(lines, "buildings_lost", stats?.BuildingsDead ?? 0);
				AppendNumber(lines, "kills_cost", stats?.KillsCost ?? 0);
				AppendNumber(lines, "deaths_cost", stats?.DeathsCost ?? 0);
				AppendNumber(lines, "army_value", stats?.ArmyValue ?? 0);
				AppendNumber(lines, "assets_value", stats?.AssetsValue ?? 0);
				AppendNumber(lines, "resources_earned", resources?.Earned ?? 0);
				AppendNumber(lines, "resources_spent", resources?.Spent ?? 0);
				samples.TryGetValue(player, out var timeline);
				AppendStatsTimeline(lines, timeline);
				lines.Append('}');

				AppendArsenal(lines, player.PlayerActor.TraitOrDefault<BotArsenalLedger>());
				AppendOwnership(lines, player.PlayerActor.TraitsImplementing<BotModules.BotOwnershipWatchdog>().FirstOrDefault(w => w.Passes > 0));
				if (OpenRA.Mods.CA.Traits.BotUnitLeases.Of(player) != null)
					AppendOrderGate(lines, player.PlayerActor.TraitsImplementing<ModularBot>().FirstOrDefault(b => b.IsEnabled));

				AppendRelationships(lines, world, takeover, player, "opponents", false);
				AppendRelationships(lines, world, takeover, player, "allies", true);
				AppendSeats(lines, world);
				AppendOpponentSignatures(lines, world, player);
				lines.Append("}\n");
			}

			return lines.ToString();
		}

		void SampleSignatures(World world)
		{
			var players = world.Players.Where(IsEligiblePlayer).ToArray();
			// Snapshot each seat's OWN actors for the deferred truth relabel. These buffers
			// have no runtime consumer and are not serialized before IGameOver.
			var byOwner = world.Actors.Where(a => a.IsInWorld && !a.IsDead && a.Info.HasTraitInfo<IOccupySpaceInfo>())
				.GroupBy(a => a.Owner).ToDictionary(g => g.Key, g => g.ToArray());
			foreach (var observer in players)
			{
				if (observer.Shroud == null)
					continue;
				if (!signatureMemory.TryGetValue(observer, out var memory))
					signatureMemory[observer] = memory = new BotModules.BotFogMemory(observer, signatureInfo);
				foreach (var enemy in players.Where(p => p != observer && !p.AlliedPlayersMask.Overlaps(observer.PlayerMask)))
				{
					var owned = byOwner.GetValueOrDefault(enemy, Array.Empty<Actor>());
					memory.Observe(enemy, owned, world.WorldTick); // CanBeViewedByPlayer/frozen sightings only.
					var seen = ProfileOf(memory.Remembered(enemy));
					seen.FactionName = BotModules.BotFactionView.PublicFactionOf(enemy);
					var key = (observer, enemy);
					if (!signatures.TryGetValue(key, out var history))
						signatures[key] = history = new List<SignatureSample>();
					var previous = history.LastOrDefault();
					if (!SignatureSampleDue(world.WorldTick, previous?.Tick ?? -1,
						previous == null || SignatureChanged(previous.Seen, seen), Math.Max(750, info.SampleIntervalTicks)))
						continue;
					var ownActors = owned.Select(a => BotModules.BotFogMemory.Classify(a.Info,
						a.ActorID, a.Location, a.GetEnabledTargetTypes(), world.WorldTick, signatureInfo)).ToArray();
					history.Add(new SignatureSample { Tick = world.WorldTick, Seen = seen, OwnActors = ownActors });
				}
			}
		}

		internal static bool SignatureChanged(BotModules.EnemyProfile a, BotModules.EnemyProfile b) =>
			a.FactionName != b.FactionName || a.ArmyValue != b.ArmyValue || a.InfantryValue != b.InfantryValue ||
			a.VehicleValue != b.VehicleValue || a.AirValue != b.AirValue || a.NavalValue != b.NavalValue ||
			a.DefenceValue != b.DefenceValue || a.BuildingCount != b.BuildingCount ||
			a.HarvesterCount != b.HarvesterCount || a.KnownRegions != b.KnownRegions;

		static BotModules.EnemyProfile ProfileOf(IEnumerable<BotModules.ObservedActor> actors)
		{
			var p = new BotModules.EnemyProfile();
			var regions = new HashSet<(int, int)>();
			foreach (var a in actors)
			{
				if (a.Combat)
				{
					p.ArmyValue += a.Value;
					if (a.Aircraft) p.AirValue += a.Value;
					else
					{
						if (a.Infantry) p.InfantryValue += a.Value;
						if (a.Vehicle) p.VehicleValue += a.Value;
						if (a.Naval) p.NavalValue += a.Value;
					}
				}
				if (a.Defence) p.DefenceValue += a.Value;
				if (a.Building) p.BuildingCount++;
				if (a.Harvester) p.HarvesterCount++;
				p.LastSeenTick = Math.Max(p.LastSeenTick, a.LastSeenTick);
				regions.Add((a.Location.X / 8, a.Location.Y / 8));
			}
			p.KnownRegions = regions.Count;
			return p;
		}

		void AppendOpponentSignatures(StringBuilder builder, World world, OpenRA.Player subject)
		{
			AppendArrayPropertyStart(builder, "opponent_signatures");
			var first = true;
			foreach (var enemy in world.Players.Where(IsEligiblePlayer)
				.Where(p => p != subject && !p.AlliedPlayersMask.Overlaps(subject.PlayerMask)))
				if (signatures.TryGetValue((subject, enemy), out var history))
					foreach (var sample in history)
					{
						if (!first) builder.Append(',');
						first = false;
						AppendOpponentSignature(builder, SeatKey(world, enemy), sample.Seen,
							enemy.Faction.InternalName, Outcome(enemy.WinState), ProfileOf(sample.OwnActors), sample.Tick);
					}
			builder.Append(']');
		}

		internal static void AppendOpponentSignature(StringBuilder builder, string seat,
			BotModules.EnemyProfile profile, string trueFaction, string outcome, BotModules.EnemyProfile truth = null, int tick = 0)
		{
			AppendObjectStart(builder);
			AppendString(builder, "seat", seat, true);
			AppendNumber(builder, "tick", tick);
			builder.Append(",\"seen\":");
			if (profile == null) builder.Append("null");
			else
			{
				builder.Append('{');
				AppendString(builder, "faction", profile.FactionName ?? "", true);
				AppendNumber(builder, "army_value", profile.ArmyValue);
				AppendNumber(builder, "infantry_value", profile.InfantryValue);
				AppendNumber(builder, "vehicle_value", profile.VehicleValue);
				AppendNumber(builder, "air_value", profile.AirValue);
				AppendNumber(builder, "naval_value", profile.NavalValue);
				AppendNumber(builder, "defence_value", profile.DefenceValue);
				AppendNumber(builder, "building_count", profile.BuildingCount);
				AppendNumber(builder, "harvester_count", profile.HarvesterCount);
				AppendNumber(builder, "known_regions", profile.KnownRegions);
				AppendNumber(builder, "last_seen_tick", profile.LastSeenTick);
				builder.Append('}');
			}
			AppendObjectPropertyStart(builder, "truth");
			AppendString(builder, "faction", trueFaction, true);
			AppendString(builder, "outcome", outcome);
			truth ??= new BotModules.EnemyProfile();
			AppendNumber(builder, "army_value", truth.ArmyValue);
			AppendNumber(builder, "infantry_value", truth.InfantryValue);
			AppendNumber(builder, "vehicle_value", truth.VehicleValue);
			AppendNumber(builder, "air_value", truth.AirValue);
			AppendNumber(builder, "naval_value", truth.NavalValue);
			AppendNumber(builder, "defence_value", truth.DefenceValue);
			AppendNumber(builder, "building_count", truth.BuildingCount);
			AppendNumber(builder, "harvester_count", truth.HarvesterCount);
			AppendNumber(builder, "known_regions", truth.KnownRegions);
			AppendNumber(builder, "last_seen_tick", truth.LastSeenTick);
			builder.Append('}').Append('}');
		}

		static void AppendSeats(StringBuilder builder, World world)
		{
			AppendArrayPropertyStart(builder, "seats");
			var players = world.Players.Where(IsEligiblePlayer).ToArray();
			for (var i = 0; i < players.Length; i++)
			{
				if (i > 0) builder.Append(',');
				var p = players[i];
				AppendObjectStart(builder);
				AppendString(builder, "seat", SeatKey(world, p), true);
				AppendString(builder, "faction", p.Faction.InternalName);
				AppendString(builder, "home", $"{p.HomeLocation.X},{p.HomeLocation.Y}");
				builder.Append('}');
			}
			builder.Append(']');
		}

		static void AppendRelationships(StringBuilder builder, World world, BotTakeoverTracker tracker, OpenRA.Player subject, string property, bool allies, bool first = false)
		{
			AppendArrayPropertyStart(builder, property, first);
			// Evaluate stances from the masks assigned at world creation rather than
			// Player.IsAlliedWith: once the match resolves, decided players report
			// Spectating (WinState != Undefined) and IsAlliedWith short-circuits to
			// ally on non-mission maps, which would record every loser as an ally.
			var relationships = world.Players
				.Where(IsEligiblePlayer)
				.Where(p => p != subject && p.AlliedPlayersMask.Overlaps(subject.PlayerMask) == allies)
				.OrderBy(p => SeatKey(world, p), StringComparer.Ordinal)
				.ToArray();

			for (var i = 0; i < relationships.Length; i++)
			{
				if (i > 0)
					builder.Append(',');

				var player = relationships[i];
				var team = LoggedTeam(world, tracker, player);
				AppendObjectStart(builder);
				AppendString(builder, "seat", SeatKey(world, player), true);
				if (player.IsBot)
					AppendString(builder, "bot_type", player.BotType ?? "");
				AppendString(builder, "faction", player.Faction.InternalName);
				AppendString(builder, "home", $"{player.HomeLocation.X},{player.HomeLocation.Y}");
				if (player.IsBot)
				{
					AppendNumber(builder, "team", team);
					AppendNumber(builder, "handicap", player.Handicap);
				}
				AppendString(builder, "outcome", Outcome(player.WinState));
				builder.Append('}');
			}

			builder.Append(']');
		}

		internal static void AppendTimeline(StringBuilder builder, IReadOnlyList<AiMatchLogPersonalityTransition> timeline, bool first = false)
		{
			AppendArrayPropertyStart(builder, "personality_timeline", first);
			if (timeline != null)
				for (var i = 0; i < timeline.Count; i++)
				{
					if (i > 0)
						builder.Append(',');
					AppendObjectStart(builder);
					AppendNumber(builder, "tick", timeline[i].Tick, true);
					AppendString(builder, "personality", timeline[i].Personality);
					builder.Append('}');
				}

			builder.Append(']');
		}

		internal static void AppendCompositionTimeline(StringBuilder builder, IReadOnlyList<AiMatchLogCompositionTransition> timeline, bool first = false)
		{
			AppendArrayPropertyStart(builder, "composition_timeline", first);
			if (timeline != null)
				for (var i = 0; i < timeline.Count; i++)
				{
					if (i > 0)
						builder.Append(',');
					AppendObjectStart(builder);
					AppendNumber(builder, "tick", timeline[i].Tick, true);
					AppendString(builder, "composition", timeline[i].Composition);
					builder.Append('}');
				}

			builder.Append(']');
		}

		internal static void AppendEpisodeTimeline(StringBuilder builder, IReadOnlyList<AiMatchLogEpisodeTransition> timeline, bool first = false)
		{
			AppendArrayPropertyStart(builder, "episode_timeline", first);
			if (timeline != null)
				for (var i = 0; i < timeline.Count; i++)
				{
					if (i > 0)
						builder.Append(',');
					AppendObjectStart(builder);
					AppendNumber(builder, "tick", timeline[i].Tick, true);
					AppendString(builder, "personality", timeline[i].Personality);
					AppendString(builder, "composition", timeline[i].Composition);
					AppendNumber(builder, "kills_cost", timeline[i].KillsCost);
					AppendNumber(builder, "deaths_cost", timeline[i].DeathsCost);
					builder.Append('}');
				}

			builder.Append(']');
		}

		internal static bool IsEligiblePlayer(OpenRA.Player player)
		{
			// Player.NonCombatant only applies to map-side players: the lobby-client
			// branch of the Player ctor ignores it, so a map-declared inert slot
			// (e.g. the ai_duel referee) occupied by a real client keeps its intent
			// only in PlayerReference. Honor the declared flag here so such slots
			// never leak into opponents/allies and break the 1v1 contract.
			// Map-declared bots are never Playable (only lobby clients are), but a
			// headless bot-vs-bot match still fights real opponents, so IsBot
			// admits them where Playable cannot.
			return !player.NonCombatant && !player.PlayerReference.NonCombatant && (player.Playable || player.IsBot);
		}

		// Map-declared bots are real bot players even when they are not playable lobby slots.
		// Same declared-intent rule as IsEligiblePlayer: a lobby-occupied slot ignores
		// Player.NonCombatant, so PlayerReference.NonCombatant is what keeps an inert
		// bot slot (or a hypothetical declared-noncombatant bot) out of the log.
		internal static bool IsLoggableBot(OpenRA.Player player)
		{
			return player.IsBot && !player.NonCombatant && !player.PlayerReference.NonCombatant;
		}

		static string Outcome(WinState state)
		{
			return state switch
			{
				WinState.Won => "won",
				WinState.Lost => "lost",
				_ => "undecided"
			};
		}

		internal static void AppendObjectStart(StringBuilder builder) { builder.Append('{'); }

		internal static void AppendObjectPropertyStart(StringBuilder builder, string name, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":{");
		}

		internal static void AppendArrayPropertyStart(StringBuilder builder, string name, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":[");
		}

		internal static void AppendString(StringBuilder builder, string name, string value, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":\"");
			AppendEscaped(builder, value ?? "");
			builder.Append('"');
		}

		internal static void AppendNumber(StringBuilder builder, string name, int value, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":")
				.Append(value.ToString(CultureInfo.InvariantCulture));
		}

		internal static void AppendNumber(StringBuilder builder, string name, long value, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":")
				.Append(value.ToString(CultureInfo.InvariantCulture));
		}

		internal static void AppendNumber(StringBuilder builder, string name, double value, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":")
				.Append(value.ToString(CultureInfo.InvariantCulture));
		}

		internal static void AppendBoolean(StringBuilder builder, string name, bool value, bool first = false)
		{
			if (!first)
				builder.Append(',');
			builder.Append('"').Append(name).Append("\":")
				.Append(value ? "true" : "false");
		}

		static void AppendEscaped(StringBuilder builder, string value)
		{
			foreach (var c in value)
			{
				switch (c)
				{
					case '\\': builder.Append("\\\\"); break;
					case '"': builder.Append("\\\""); break;
					case '\b': builder.Append("\\b"); break;
					case '\f': builder.Append("\\f"); break;
					case '\n': builder.Append("\\n"); break;
					case '\r': builder.Append("\\r"); break;
					case '\t': builder.Append("\\t"); break;
					default:
						if (char.IsControl(c))
							builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else
							builder.Append(c);
						break;
				}
			}
		}
	}
}
