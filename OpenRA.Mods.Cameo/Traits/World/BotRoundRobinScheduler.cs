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
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	// STAGGER_DESIGN_06 H1: the host trait around BotRoundRobinCore. N live activated Cameo-family bots each act
	// once every N ticks, in spawn-index order. With no gated bot it only keeps its roster (T1a no-op proof); with
	// the trait absent ModularBot behaves exactly as before.
	[TraitLocation(SystemActors.World)]
	[Desc("Round-robin scheduler for Cameo-family bots (STAGGER_DESIGN_06). Mounted on the world actor; inert until a bot type sets RoundRobin.")]
	public sealed class BotRoundRobinSchedulerInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new BotRoundRobinScheduler(init.World);
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

	public sealed class BotRoundRobinScheduler : ITick, IGameSaveTraitData, INotifyGameLoaded
	{
		// 11.2: the scheduler's own data is versioned. A newer version refuses the load, naming the version.
		public const int SchedulerDataVersion = 1;

		readonly World world;
		bool resolvedFromSave;

		public readonly BotRoundRobinCore Core;

		// Heavy-op shared jobs (design section 10). No consumer yet; owners follow the live roster at every cycle boundary (10.3).
		public readonly BotSharedJobRegistry SharedJobs = new(new BotJobSeq());

		public BotRoundRobinScheduler(World world)
		{
			this.world = world;
			Core = new BotRoundRobinCore(IsLive, PlayerIndexOf);
			Core.RosterRepacked += roster => SharedJobs.TransferOwners(roster.Select(s => s.Name).ToList());
		}

		// Live = enabled, undefeated, not disposed (5.1).
		static bool IsLive(BotSeat s)
		{
			var b = (SeatBinding)s.Bot;
			return b.Bot.IsEnabled && b.Player.WinState == WinState.Undefined && !b.Player.PlayerActor.Disposed;
		}

		// The player list is fixed after the bots activate, so the index is read at a cycle boundary.
		int PlayerIndexOf(BotSeat s) => Array.IndexOf(world.Players, ((SeatBinding)s.Bot).Player);

		// Called from ModularBot.Activate after IsEnabled = true; host only (engine Player.cs:223).
		public BotSeat Register(OpenRA.Player p, ModularBot bot, bool gated)
		{
			var spawnKey = p.SpawnPoint > 0 ? p.SpawnPoint : BotRoundRobinCore.NoSpawnKey;
			return Core.Register(p.InternalName, spawnKey, Array.IndexOf(world.Players, p), gated, new SeatBinding(p, bot), world.WorldTick);
		}

		void ITick.Tick(Actor self)
		{
			if (world.IsLoadingGameSave)
				return;

			Core.Advance(world.WorldTick);
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
