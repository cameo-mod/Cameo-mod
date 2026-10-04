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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	public enum TakeoverTrigger { Disconnect, Surrender }

	// The single decision point for the last-undefeated-player DISCONNECT case (rule R4):
	// surrender always takes today's ForceDefeat path; a disconnect follows this policy.
	public enum TakeoverLastPlayerPolicy { Defeat, Idle, Takeover }

	public enum TakeoverDecision { Takeover, Defeat, Idle }

	/// <summary>Synced record of one takeover seat, written at the synced trigger frame.</summary>
	public sealed class TakeoverRecord
	{
		public readonly int Tick;
		public readonly TakeoverTrigger Trigger;
		public readonly string BotType;
		public readonly int ControllerClientId;

		public TakeoverRecord(int tick, TakeoverTrigger trigger, string botType, int controllerClientId)
		{
			Tick = tick;
			Trigger = trigger;
			BotType = botType;
			ControllerClientId = controllerClientId;
		}
	}

	/// <summary>
	/// A lobby-seat snapshot for the rule logic. Pure data so the R2-R4 rule table is
	/// unit-testable without a live World.
	/// </summary>
	public readonly struct TakeoverSeat
	{
		public readonly string Name;
		public readonly int Team;
		public readonly WinState State;
		public readonly bool Combatant;

		public TakeoverSeat(string name, int team, WinState state, bool combatant)
		{
			Name = name;
			Team = team;
			State = state;
			Combatant = combatant;
		}
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Replaces a disconnected or surrendered lobby player with the hard genericbot stack, " +
		"per the takeover rules in docs/DESIGN.md. All state is updated only at synced events.")]
	public class BotTakeoverTrackerInfo : TraitInfo
	{
		[Desc("Lobby boolean option id backing the 'Replace disconnected players with AI' checkbox " +
			"(LobbySystemActorConditionCheckbox@BOT_TAKEOVER).")]
		public readonly string LobbyOptionId = "bot_takeover";

		[Desc("Default when the lobby option is absent.")]
		public readonly bool Enabled = true;

		[Desc("Modular bot type activated for a taken-over seat ('hard' is the neutral lobby hard stack, no cheats).")]
		public readonly string BotType = "hard";

		[Desc("Conditions granted on the player actor so the matching genericbot module stack wakes up.")]
		public readonly string[] Conditions = { "genericbot", "hardbot" };

		[Desc("Ticks between bot-stance re-applies on takeover seats. Units produced after takeover " +
			"still arrive with the human default stance because AutoTarget reads Owner.IsBot at " +
			"creation and that stays false for a taken-over slot; this interval re-imposes the " +
			"bot stance so the seat fights like a real bot.")]
		public readonly int StanceRefreshIntervalTicks = 25;

		[Desc("DISCONNECT of the last undefeated player of a team while <= 2 teams remain: " +
			"Takeover replaces them anyway (default, maintainer 2026-10-04), Defeat runs today's " +
			"ForceDefeat path, Idle leaves the forces inert. The single switchable decision point.")]
		public readonly TakeoverLastPlayerPolicy LastPlayerDisconnect = TakeoverLastPlayerPolicy.Takeover;

		public override object Create(ActorInitializer init) { return new BotTakeoverTracker(this); }
	}

	public class BotTakeoverTracker : IWorldLoaded, INotifyPlayerDisconnected, ITick
	{
		readonly BotTakeoverTrackerInfo info;

		// Synced state only: the lobby-bound humans still connected, the lobby client each seat
		// belonged to, team ids frozen at load, and the takeover seats.
		readonly HashSet<int> connectedClients = [];
		readonly HashSet<int> boundClients = [];
		readonly HashSet<int> takenOverClients = [];
		readonly Dictionary<int, OpenRA.Player> boundPlayers = [];
		readonly Dictionary<OpenRA.Player, int> teamOf = [];
		readonly Dictionary<OpenRA.Player, TakeoverRecord> records = [];
		readonly HashSet<OpenRA.Player> activatedHere = [];

		World world;
		bool enabled;

		/// <summary>Client index running every takeover bot; -1 when no electable client remains.</summary>
		public int Controller { get; private set; } = -1;

		public bool Enabled => enabled;
		public bool IsLocalClientController => Controller >= 0 && Controller == Game.LocalClientId;

		public bool IsTakenOver(OpenRA.Player p) => records.ContainsKey(p);
		public bool TryGetTakeoverRecord(OpenRA.Player p, out TakeoverRecord record) => records.TryGetValue(p, out record);

		// The load-time lobby-team snapshot survives the departure of the player's client row —
		// the AI match log uses it for takeover seats (boss_review T2).
		public int TeamOfPlayer(OpenRA.Player p) => teamOf.GetValueOrDefault(p, p.PlayerReference.Team);
		public IEnumerable<KeyValuePair<OpenRA.Player, TakeoverRecord>> Records => records;

		public BotTakeoverTracker(BotTakeoverTrackerInfo info)
		{
			this.info = info;
		}

		void IWorldLoaded.WorldLoaded(World w, WorldRenderer worldRenderer)
		{
			world = w;

			// Multiplayer only: in single player the classic surrender/defeat behaviour stays
			// untouched — there is no teammate to continue for and missions own their flow.
			enabled = !w.LobbyInfo.GlobalSettings.EnableSingleplayer
				&& w.LobbyInfo.GlobalSettings.OptionOrDefault(info.LobbyOptionId, info.Enabled);

			// Session clients and world players are identical on every client at load: snapshotting
			// them here keeps every later decision on synced data. Client.State is UI-side and unsynced,
			// so connectivity comes from our own set, updated at the synced disconnect marker.
			// Connectivity is the whole human session — spectators (a spectator admin included) are
			// connected clients too, even though they bind no player (boss_review T4). Caveat: a
			// spectator's DEPARTURE produces no playable player, so the synced disconnect notify never
			// reaches it — the set can retain a ghost spectator. Harmless: only bound clients elect
			// the controller, and log ownership follows the elected controller, never admin
			// connectivity (boss_review T4 rev-2).
			foreach (var client in w.LobbyInfo.NonBotClients)
			{
				connectedClients.Add(client.Index);
				if (client.Slot == null)
					continue;

				var bound = w.Players.FirstOrDefault(p => p.InternalName == client.Slot);
				if (bound != null)
				{
					boundPlayers[client.Index] = bound;
					boundClients.Add(client.Index);
				}
			}

			// Teams come from the lobby client for EVERY slot-bound player, bots included (boss_review
			// T2): the engine builds players from the unchanged map reference (CreateMapPlayers) and
			// applies lobby teams only via SetupPlayerMasks — a lobby bot would otherwise keep its map
			// team id, and a human-vs-bots lobby would read one team too many.
			foreach (var p in w.Players)
				teamOf[p] = LobbyTeamOf(w.LobbyInfo, p.InternalName, p.PlayerReference.Team);

			ElectController();
		}

		void INotifyPlayerDisconnected.PlayerDisconnected(Actor self, OpenRA.Player p)
		{
			// The callback also fires for a map-declared playable player that shares the
			// disconnected client's index; only the lobby-bound player is a takeover seat.
			if (connectedClients.Remove(p.ClientIndex))
			{
				ElectController();

				// Controller failover: the newly elected client picks up every takeover bot.
				if (IsLocalClientController)
					foreach (var seat in records.Keys)
						ActivateBotHere(seat);
			}

			if (!enabled || !IsTakeoverCandidate(p))
				return;

			var decision = Decide(CountTeamsAlive(Seats()), HasUndefeatedTeammate(Seats(), SeatOf(p)),
				TakeoverTrigger.Disconnect, info.LastPlayerDisconnect);
			ApplyDecision(p, decision, TakeoverTrigger.Disconnect);
		}

		/// <summary>
		/// The synced Surrender order path (CameoMissionObjectives calls this before ForceDefeat).
		/// Returns true when the surrender was converted into a takeover and the caller must skip the defeat path.
		/// </summary>
		public bool TryTakeoverOnSurrender(OpenRA.Player p)
		{
			if (!enabled || !IsTakeoverCandidate(p))
				return false;

			if (Decide(CountTeamsAlive(Seats()), HasUndefeatedTeammate(Seats(), SeatOf(p)),
				TakeoverTrigger.Surrender, info.LastPlayerDisconnect) != TakeoverDecision.Takeover)
				return false;

			ApplyDecision(p, TakeoverDecision.Takeover, TakeoverTrigger.Surrender);
			return true;
		}

		// ---------------------------------------------------------------------
		// Rule table (R2-R4). Static and pure for unit tests.
		// ---------------------------------------------------------------------

		internal static TakeoverDecision Decide(int teamsAlive, bool hasUndefeatedTeammate,
			TakeoverTrigger trigger, TakeoverLastPlayerPolicy lastPlayerDisconnect)
		{
			// R3: more than two teams alive -> always take over, even a solo player.
			// R4 (2026-10-04): <= 2 teams -> take over unless the trigger seat is the last
			// undefeated player of ANY kind on its team. That last player's SURRENDER takes
			// today's ForceDefeat path; a DISCONNECT still takes over (policy default Takeover;
			// Defeat and Idle remain selectable for the maintainer).
			if (teamsAlive > 2 || hasUndefeatedTeammate)
				return TakeoverDecision.Takeover;

			if (trigger == TakeoverTrigger.Disconnect)
				return lastPlayerDisconnect switch
				{
					TakeoverLastPlayerPolicy.Idle => TakeoverDecision.Idle,
					TakeoverLastPlayerPolicy.Defeat => TakeoverDecision.Defeat,
					_ => TakeoverDecision.Takeover,
				};

			return TakeoverDecision.Defeat;
		}

		// R2: distinct teams (a no-team player is a team of one) with at least one combatant
		// player whose WinState is Undefined. Humans, bots and takeover seats all count.
		internal static int CountTeamsAlive(IEnumerable<TakeoverSeat> seats)
		{
			var keys = new HashSet<string>();
			foreach (var s in seats)
			{
				if (!s.Combatant || s.State != WinState.Undefined)
					continue;

				keys.Add(s.Team == 0 ? "solo:" + s.Name : "team:" + s.Team);
			}

			return keys.Count;
		}

		// R4: any other teammate (human, lobby bot, takeover AI, or merely inert) with
		// WinState Undefined means the subject is not the team's last undefeated player.
		internal static bool HasUndefeatedTeammate(IEnumerable<TakeoverSeat> seats, TakeoverSeat subject)
		{
			if (subject.Team == 0)
				return false;

			foreach (var s in seats)
				if (s.Combatant && s.State == WinState.Undefined && s.Team == subject.Team && s.Name != subject.Name)
					return true;

			return false;
		}

		internal static int ElectController(IEnumerable<int> electable)
		{
			var min = -1;
			foreach (var id in electable)
				if (min < 0 || id < min)
					min = id;

			return min;
		}

		internal static IEnumerable<int> ElectableClients(IEnumerable<int> connected, IReadOnlySet<int> bound, IReadOnlySet<int> takenOver)
		{
			return connected.Where(id => bound.Contains(id) && !takenOver.Contains(id));
		}

		internal static int LobbyTeamOf(Session lobby, string slot, int mapTeam)
		{
			return lobby.ClientInSlot(slot)?.Team ?? mapTeam;
		}

		internal static string[] ResolvedBotConditions(IReadOnlyList<string> conditions,
			IEnumerable<GrantConditionOnBotOwnerInfo> grants, string botType)
		{
			return conditions
				.Concat(grants.Where(t => t.Bots.Contains(botType)).Select(t => t.Condition))
				.Distinct()
				.ToArray();
		}

		internal static bool SeatStillOpen(WinState state, bool takenOver, bool stillConnected)
		{
			return state == WinState.Undefined && !takenOver && stillConnected;
		}

		/// <summary>True while a bound lobby-human seat can still convert to a takeover bot:
		/// undefeated, still connected, not already taken over. The AI match log stays open
		/// for exactly this window (boss_review T1).</summary>
		public bool HasOpenSeats
		{
			get
			{
				if (!enabled)
					return false;

				foreach (var p in boundPlayers.Values)
					if (SeatStillOpen(p.WinState, records.ContainsKey(p), connectedClients.Contains(p.ClientIndex)))
						return true;

				return false;
			}
		}

		// ---------------------------------------------------------------------

		void ElectController()
		{
			// Deterministic on every client: same connected-set minus taken-over clients.
			// A taken-over seat's former client is excluded so a surrendered player can never
			// become the controller of their own (or any) takeover seat. Only bound clients elect —
			// a connected spectator can never drive a bot (boss_review T4).
			Controller = ElectController(ElectableClients(connectedClients, boundClients, takenOverClients));
		}

		IEnumerable<TakeoverSeat> Seats()
		{
			foreach (var q in world.Players)
				yield return SeatOf(q);
		}

		TakeoverSeat SeatOf(OpenRA.Player q)
		{
			return new TakeoverSeat(q.InternalName, teamOf.GetValueOrDefault(q), q.WinState, IsCombatant(q));
		}

		static bool IsCombatant(OpenRA.Player q)
		{
			return !q.NonCombatant && !q.PlayerReference.NonCombatant && !q.Spectating;
		}

		bool IsTakeoverCandidate(OpenRA.Player p)
		{
			return boundPlayers.TryGetValue(p.ClientIndex, out var bound) && ReferenceEquals(bound, p)
				&& p.Playable && !p.IsBot
				&& !p.NonCombatant && !p.PlayerReference.NonCombatant
				&& p.WinState == WinState.Undefined
				&& !records.ContainsKey(p);
		}

		void ApplyDecision(OpenRA.Player p, TakeoverDecision decision, TakeoverTrigger trigger)
		{
			switch (decision)
			{
				case TakeoverDecision.Takeover:
					PerformTakeover(p, trigger);
					break;
				case TakeoverDecision.Defeat:
					ApplyDefeat(p);
					break;

				// Idle: today's disconnect behaviour — the seat stays inert.
			}
		}

		void PerformTakeover(OpenRA.Player p, TakeoverTrigger trigger)
		{
			// Excluding the former client first keeps a surrendered human from becoming
			// the controller of their own seat; the record then stores the final electee.
			takenOverClients.Add(p.ClientIndex);
			ElectController();
			records[p] = new TakeoverRecord(world.WorldTick, trigger, info.BotType, Controller);

			// R6 parity (boss_review T3): a real lobby bot of this type would also carry every
			// GrantConditionOnBotOwner whose Bots list names it (inc3_frans_services for hard arms
			// eight Frans service modules). The seat resolves the same set from the rules — identical
			// on all clients — so the matching stack wakes identically.
			foreach (var condition in ResolvedBotConditions(info.Conditions,
				p.PlayerActor.Info.TraitInfos<GrantConditionOnBotOwnerInfo>(), info.BotType))
				p.PlayerActor.GrantCondition(condition);

			// The inherited army keeps the human default stance: re-impose the bot stance
			// now (post-takeover production is covered by the periodic pass below).
			// records[p] is already set, so the single scan covers the new seat too.
			ApplyBotStances();

			// The electee may have just changed (a surrendered client was running earlier
			// seats): this client re-activates every takeover bot it now controls.
			if (IsLocalClientController)
				foreach (var seat in records.Keys)
					ActivateBotHere(seat);

			TextNotificationsManager.AddSystemLine("Server", $"{p.ResolvedPlayerName} was replaced by a hard AI.");
		}

		void ITick.Tick(Actor self)
		{
			// Units produced after the takeover still arrive with the human default stance —
			// AutoTarget resolves Owner.IsBot at actor creation, and that stays false for a
			// taken-over slot. A coarse periodic pass re-imposes the bot stance on every unit
			// a takeover seat owns; deterministic on all clients (same scan, same tick).
			if (!enabled || records.Count == 0 || world.WorldTick % Math.Max(1, info.StanceRefreshIntervalTicks) != 0)
				return;

			ApplyBotStances();
		}

		void ApplyBotStances()
		{
			// A bot-built unit's AutoTarget stance comes from InitialStanceAI; a takeover seat's
			// units (inherited army plus new production) hold the human default. Re-apply the
			// per-type bot stance — SetStance itself is a no-op when already matching, and it
			// keeps ConditionByStance consumers and stance-change listeners consistent.
			// One world scan per refresh regardless of seat count: the per-actor
			// records lookup replaces one ActorsHavingTrait pass per taken-over seat.
			foreach (var a in world.ActorsHavingTrait<AutoTarget>())
			{
				if (a.IsDead || !a.IsInWorld || !records.ContainsKey(a.Owner))
					continue;

				var at = a.Trait<AutoTarget>();
				if (at.Stance != at.Info.InitialStanceAI)
					at.SetStance(a, at.Info.InitialStanceAI);
			}
		}

		void ActivateBotHere(OpenRA.Player p)
		{
			// Once per local process per seat: the elected controller runs the bot like the
			// host runs lobby bots — unsynced logic that only emits orders into the stream.
			if (!activatedHere.Add(p))
				return;

			var logic = p.PlayerActor.TraitsImplementing<IBot>().FirstOrDefault(b => b.Info.Type == info.BotType);
			if (logic == null)
			{
				Log.Write("debug", $"bot_takeover: no IBot of type '{info.BotType}' on the player actor");
				return;
			}

			Log.Write("debug", $"bot_takeover: activated '{info.BotType}' bot for {p.InternalName} on controller client {Controller}");
			logic.Activate(p);
		}

		void ApplyDefeat(OpenRA.Player p)
		{
			// Same outcome MissionObjectives produces for Surrender: fail incomplete objectives,
			// which flips WinState and runs the owner-lost destruction path.
			var mo = p.PlayerActor.TraitOrDefault<MissionObjectives>();
			if (mo != null && mo.Objectives.Any(o => o.State == ObjectiveState.Incomplete))
			{
				mo.ForceDefeat(p);
				return;
			}

			// No incomplete objectives -> ForceDefeat would be a no-op. Run the same lost
			// path directly so the seat still dies like a surrender.
			if (p.WinState == WinState.Undefined)
				foreach (var inwc in p.PlayerActor.TraitsImplementing<INotifyWinStateChanged>())
					inwc.OnPlayerLost(p);
		}
	}
}
