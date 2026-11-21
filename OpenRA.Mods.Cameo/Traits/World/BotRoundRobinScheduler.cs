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
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 H1: the host trait around BotRoundRobinCore. N live activated Cameo-family bots each act
	// once every N ticks, in spawn-index order. With no gated bot it only keeps its roster (T1a no-op proof); with
	// the trait absent ModularBot behaves exactly as before.
	[TraitLocation(SystemActors.World)]
	[Desc("Round-robin scheduler for Cameo-family bots (STAGGER_DESIGN_06). Mounted on the world actor; inert until a bot type sets RoundRobin.")]
	public sealed class BotRoundRobinSchedulerInfo : TraitInfo, ILobbyOptions
	{
		public const string OptionId = "botroundrobin";

		[Desc("Default state of the Bot Round-Robin lobby option. Off forces every bot to act every tick (the pre-scheduler behaviour).")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the Bot Round-Robin lobby option from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Display order for the Bot Round-Robin lobby option.")]
		public readonly int CheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption(map, OptionId,
				"Bot Round-Robin",
				"Each bot thinks once every N ticks (N = number of bots) instead of every tick: faster games, slightly coarser bot timing. Off = every bot acts every tick.",
				true, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) => new BotRoundRobinScheduler(init.World, this);
	}

	public sealed class SeatBinding
	{
		public readonly OpenRA.Player Player;
		public readonly ModularBot Bot;

		public SeatBinding(OpenRA.Player player, ModularBot bot)
		{
			Player = player;
			Bot = bot;
		}
	}

	public sealed class BotRoundRobinScheduler : ITick, IGameSaveTraitData, INotifyGameLoaded, IGameOver
	{
		// 11.2: the scheduler's own data is versioned. A newer version refuses the load, naming the version.
		public const int SchedulerDataVersion = 1;

		readonly World world;
		readonly BotRoundRobinSchedulerInfo info;
		bool resolvedFromSave;
#if CAMEO_DEV_LOGGING
		bool loggedFirstRoster;
		int loggedN = -1;
#endif

		public readonly BotRoundRobinCore Core;

		// Heavy-op shared jobs (design section 10). No consumer yet; owners follow the live roster at every cycle boundary (10.3).
		public readonly BotSharedJobRegistry SharedJobs = new(new BotJobSeq());

		public BotRoundRobinScheduler(World world, BotRoundRobinSchedulerInfo info)
		{
			this.world = world;
			this.info = info;
			Core = new BotRoundRobinCore(IsLive, PlayerIndexOf);
			Core.RosterRepacked += roster =>
			{
				SharedJobs.TransferOwners(roster.Select(s => s.Name).ToList());
#if CAMEO_DEV_LOGGING
				if (!loggedFirstRoster)
				{
					loggedFirstRoster = true;
					Log.Write("perf", string.Format(CultureInfo.InvariantCulture, "BotRoundRobinRoster n={0} tick={1} slots={2}",
						roster.Count, world.WorldTick, string.Join(",", roster.Select((s, i) => s.Name + ":" + i))));
				}

				LogSeatArms(roster);
#endif
			};
		}

		// Live = enabled, undefeated, not disposed (5.1).
		static bool IsLive(BotSeat s)
		{
			var b = (SeatBinding)s.Bot;
			return b.Bot.IsEnabled && b.Player.WinState == WinState.Undefined && !b.Player.PlayerActor.Disposed;
		}

		// The player list is fixed after the bots activate, so the index is read at a cycle boundary.
		int PlayerIndexOf(BotSeat s) => Array.IndexOf(world.Players, ((SeatBinding)s.Bot).Player);

		// The synced lobby option (default On). Off means N = 1 for every bot: its seat is registered ungated,
		// which is the byte-identical pre-scheduler behaviour.
		public bool OptionOn => world.LobbyInfo?.GlobalSettings.OptionOrDefault(BotRoundRobinSchedulerInfo.OptionId, info.CheckboxEnabled)
			?? info.CheckboxEnabled;

		/// <summary>
		/// DEV override spec: comma-separated <c>player:on</c> or <c>player:off</c>, player = InternalName or
		/// PlayerName (case-insensitive), e.g. <c>Multi0:on,Multi1:off</c>. Returns null when no entry names the player.
		/// </summary>
		public static bool? ParseOverride(string spec, params string[] playerKeys)
		{
			if (string.IsNullOrWhiteSpace(spec))
				return null;

			foreach (var entry in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				var i = entry.LastIndexOf(':');
				if (i <= 0)
					continue;

				var key = entry[..i].Trim();
				var value = entry[(i + 1)..].Trim();
				if (!playerKeys.Any(k => k != null && string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
					continue;

				if (value.Equals("on", StringComparison.OrdinalIgnoreCase))
					return true;
				if (value.Equals("off", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			return null;
		}

		bool IsGated(OpenRA.Player p, bool botTypeRoundRobin, out string source)
		{
#if CAMEO_DEV_LOGGING
			// DEV builds only, per process: every client of a multiplayer game must be given the same argument.
			var dev = ParseOverride(CameoDevArgs.Value("Cameo.DevRoundRobin"), p.InternalName, p.PlayerName);
			if (dev.HasValue)
			{
				source = "override";
				return dev.Value;
			}
#endif
			return ResolveGated(botTypeRoundRobin, OptionOn, null, out source);
		}

		/// <summary>Period used for the slot_phase field of the arm line: the 75-tick (3 s) timer period, the one the stalemate
		/// and the signal sweeps share. The real phase of a timer is floor(rank x K / N_fam) for its own K; slot_rank and
		/// slot_family let the analysis recompute it.</summary>
		public const int ArmLinePhasePeriod = 75;

		/// <summary>The tick residue (tick mod N) on which a seat is visited: slot i of a cycle that starts at <paramref name="cycleStart"/>
		/// is visited at cycleStart + i, and cycles repeat every N ticks. -1 for a seat with no slot.</summary>
		public static int VisitResidue(int cycleStart, int slotIndex, int n) => slotIndex < 0 || n <= 0 ? -1 : (cycleStart + slotIndex) % n;

		/// <summary>The DEV arm line of one player (STAGGER_EVAL_PLAN_01 O-7). Pure formatting, so it is testable without a world.
		/// An ungated seat reports n=1 and residue 0 (it acts every tick); a gated seat outside the roster reports its roster N
		/// and residue -1.</summary>
		public static string FormatSeatArm(string seat, string player, string faction, bool rrEnabled, bool gated, int n,
			int visitResidue, int slotRank, int slotFamily, int tick)
		{
			var phase = BotSlotPhase.Phase(slotRank, slotFamily, ArmLinePhasePeriod);
			return string.Format(CultureInfo.InvariantCulture,
				"BotRoundRobinSeat seat={0} player={1} faction={2} rr_enabled={3} gated={4} n={5} visit_residue={6} slot_rank={7} slot_family={8} slot_phase={9} phase_period={10} tick={11}",
				seat, player, faction, rrEnabled ? "true" : "false", gated ? "true" : "false", gated ? n : 1,
				gated ? visitResidue : 0, slotRank, slotFamily, phase, ArmLinePhasePeriod, tick);
		}

#if CAMEO_DEV_LOGGING
		// DEV: at the first roster, one line per registered bot seat (rr_enabled = the lobby option, gated = this seat's arm after the
		// DEV override); afterwards a BotRoundRobinN line each time the number of live gated seats changes. Read only.
		void LogSeatArms(IReadOnlyList<BotSeat> roster)
		{
			if (roster.Count == loggedN)
				return;

			var first = loggedN < 0;
			if (!first)
				Log.Write("perf", string.Format(CultureInfo.InvariantCulture, "BotRoundRobinN n={0} prev={1} tick={2}", roster.Count, loggedN, world.WorldTick));

			loggedN = roster.Count;
			if (!first)
				return;

			foreach (var seat in Core.Registered)
			{
				var player = ((SeatBinding)seat.Bot).Player;
				BotSlotPhase.Peek(world, player, out var rank, out var family);
				var slot = -1;
				for (var i = 0; i < roster.Count; i++)
					if (roster[i] == seat)
						slot = i;

				Log.Write("perf", FormatSeatArm(seat.Name, player.PlayerName, player.Faction?.InternalName, OptionOn, seat.Gated,
					roster.Count, VisitResidue(Core.CycleStart, slot, roster.Count), rank, family, world.WorldTick));
			}
		}
#endif

		/// <summary>The gating decision (ENABLE-01): a bot is gated only when its bot type sets RoundRobin and the lobby option is On; a DEV
		/// override, when present, wins. Option Off therefore always means ungated, the pre-scheduler behaviour.</summary>
		public static bool ResolveGated(bool botTypeRoundRobin, bool optionOn, bool? devOverride, out string source)
		{
			if (devOverride.HasValue)
			{
				source = "override";
				return devOverride.Value;
			}

			source = !botTypeRoundRobin ? "bot-type" : "lobby";
			return botTypeRoundRobin && optionOn;
		}

		// Called from ModularBot.Activate after IsEnabled = true; host only (engine Player.cs:223).
		public BotSeat Register(OpenRA.Player p, ModularBot bot, bool gated)
		{
			var spawnKey = p.SpawnPoint > 0 ? p.SpawnPoint : BotRoundRobinCore.NoSpawnKey;
			var arm = IsGated(p, gated, out var source);
			var seat = Core.Register(p.InternalName, spawnKey, Array.IndexOf(world.Players, p), arm, new SeatBinding(p, bot), world.WorldTick);
#if CAMEO_DEV_LOGGING
			// DEV: one line per bot seat at game start naming its round-robin arm (STAGGER_EVAL_PLAN_01 O-7).
			Log.Write("perf", string.Format(CultureInfo.InvariantCulture,
				"BotRoundRobinArm seat={0} player={1} arm={2} source={3} lobby_option={4} tick={5}",
				seat.Name, p.PlayerName, arm ? "On" : "Off", source, OptionOn ? "On" : "Off", world.WorldTick));
#endif
			return seat;
		}

		void ITick.Tick(Actor self)
		{
			if (world.IsLoadingGameSave)
				return;

			Core.Advance(world.WorldTick);
		}

		// DEV builds: the game-end per-module timing flush of every registered bot (ModularBot is a player trait and gets no IGameOver).
		void IGameOver.GameOver(World w)
		{
#if CAMEO_DEV_LOGGING
			foreach (var seat in Core.Registered)
				((SeatBinding)seat.Bot).Bot.FlushModuleTiming();
#endif
		}

		void INotifyGameLoaded.GameLoaded(World w)
		{
			// A save from before the scheduler carries no data: a new cycle starts at the first post-load tick.
			if (!resolvedFromSave)
				Core.ResetCycle();
		}

		static string Fmt(long v) => v.ToString(CultureInfo.InvariantCulture);
		static string Get(MiniYaml d, string key) => d.Nodes.First(n => n.Key == key).Value.Value;
		static int GetInt(MiniYaml d, string key) => int.Parse(Get(d, key), CultureInfo.InvariantCulture);
		static long GetLong(MiniYaml d, string key) => long.Parse(Get(d, key), CultureInfo.InvariantCulture);

		// 11.3. Saves are issued between ticks, so every seat is in Phase Off and the state is complete.
		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			var nodes = new List<MiniYamlNode>
			{
				new("SchedulerDataVersion", Fmt(SchedulerDataVersion)),
				new("CycleStart", Fmt(Core.CycleStart)),
				new("Epoch", Fmt(Core.Epoch)),
				new("NextSeq", Fmt(Core.NextSeq)),
				new("Roster", string.Join(",", Core.Roster.Select(s => s.Name))),
				new("EventsQueued", Fmt(Core.EventsQueued)),
				new("EventsDelivered", Fmt(Core.EventsDelivered)),
				new("DiscardedCase1", Fmt(Core.DiscardedCase1)),
				new("DiscardedCase2", Fmt(Core.DiscardedCase2)),
			};

			foreach (var seat in Core.Registered)
			{
				var children = new List<MiniYamlNode>
				{
					new("LastVisitTick", Fmt(seat.LastVisitTick)),
					new("VisitTick", Fmt(seat.VisitTick)),
					new("Elapsed", Fmt(seat.Elapsed)),
					new("Gated", seat.Gated ? "true" : "false"),
					new("TrackingSerial", Fmt(seat.TrackingSerial)),
					new("Visits", Fmt(seat.Visits)),
				};

				// Events are written in Seq order, which is FIFO order.
				foreach (var ev in seat.Fifo)
					children.Add(ev.Save());

				nodes.Add(new MiniYamlNode("Seat", new MiniYaml(seat.Name, children)));
			}

			return nodes;
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (world.IsReplay)
				return;

			var version = GetInt(data, "SchedulerDataVersion");
			if (version > SchedulerDataVersion)
				throw new InvalidOperationException($"BotRoundRobinScheduler save data version {version} is newer than the supported version {SchedulerDataVersion}.");

			resolvedFromSave = true;

			foreach (var node in data.Nodes.Where(n => n.Key == "Seat"))
			{
				var seat = Core.Find(node.Value.Value);
				if (seat == null)
					throw new InvalidOperationException($"BotRoundRobinScheduler save names seat '{node.Value.Value}', which is not an active bot in these rules.");

				var d = node.Value;
				if ((Get(d, "Gated") == "true") != seat.Gated)
					throw new InvalidOperationException($"BotRoundRobinScheduler save for seat '{seat.Name}' was written with a different RoundRobin setting.");

				seat.LastVisitTick = GetInt(d, "LastVisitTick");
				seat.VisitTick = GetInt(d, "VisitTick");
				seat.Elapsed = GetInt(d, "Elapsed");
				seat.TrackingSerial = GetLong(d, "TrackingSerial");
				seat.Visits = GetInt(d, "Visits");
				seat.Phase = DispatchPhase.Off;
				seat.Fifo.Clear();

				foreach (var evNode in d.Nodes.Where(n => n.Key == "Event"))
				{
					var ev = BotEvent.Load(evNode.Value);
					if (ev == null)
						throw new InvalidOperationException($"BotRoundRobinScheduler save for seat '{seat.Name}' holds unknown event kind '{evNode.Value.Value}'.");

					// An unresolved ModuleKey means the save came from different rules: refuse, never drop silently (7.5).
					if (ev.Recipient.Module.Length > 0 && !((SeatBinding)seat.Bot).Bot.CanResolveModule(ev.Recipient))
						throw new InvalidOperationException($"BotRoundRobinScheduler save for seat '{seat.Name}' addresses module '{ev.Recipient}', which these rules do not have.");

					seat.Fifo.Enqueue(ev);
				}
			}

			var rosterText = Get(data, "Roster");
			var names = rosterText.Length == 0 ? Array.Empty<string>() : rosterText.Split(',');
			var missing = Core.RestoreDispatch(GetInt(data, "CycleStart"), GetInt(data, "Epoch"), GetLong(data, "NextSeq"), names);
			if (missing != null)
				throw new InvalidOperationException($"BotRoundRobinScheduler save roster names seat '{missing}', which is not an active bot in these rules.");

			Core.RestoreCounters(GetLong(data, "EventsQueued"), GetLong(data, "EventsDelivered"),
				GetLong(data, "DiscardedCase1"), GetLong(data, "DiscardedCase2"));

			// D-3 case 2: an actor id that no longer resolves discards the event, counted. The queue keeps its order.
			foreach (var seat in Core.Registered)
			{
				if (seat.Fifo.Count == 0)
					continue;

				var kept = new Queue<BotEvent>();
				foreach (var ev in seat.Fifo)
				{
					var resolved = ev is AttackEvent a ? a.Resolve(world) : ev.ActorIds.All(id => world.GetActorById(id) != null);
					if (resolved)
						kept.Enqueue(ev);
					else
						Core.NoteCase2Discard();
				}

				seat.Fifo.Clear();
				foreach (var ev in kept)
					seat.Fifo.Enqueue(ev);
			}
		}
	}

	// 8: the bot clock. An ungated bot sees {WorldTick, 1, true}.
	public static class BotClock
	{
		public static (int VisitTick, int Elapsed, bool InVisit) Of(IBot bot)
		{
			if (bot is ModularBot mb)
				return mb.Clock;

			return (bot.Player.World.WorldTick, 1, true);
		}
	}
}
