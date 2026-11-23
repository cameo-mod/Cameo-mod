#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	public readonly struct CellXY
	{
		public int X { get; }
		public int Y { get; }
		public CellXY(int x, int y) { X = x; Y = y; }
	}

	public readonly struct PlayerIdentity
	{
		public int PlayerIndex { get; }
		public string PlayerInternalName { get; }
		public string FactionInternalName { get; }
		public string BotType { get; }
		public bool IsBot { get; }
		public bool NonCombatant { get; }

		public PlayerIdentity(int playerIndex, string playerInternalName, string factionInternalName,
			string botType, bool isBot, bool nonCombatant)
		{
			PlayerIndex = playerIndex;
			PlayerInternalName = playerInternalName;
			FactionInternalName = factionInternalName;
			BotType = botType;
			IsBot = isBot;
			NonCombatant = nonCombatant;
		}

		public static PlayerIdentity Of(Player player) => new(
			Array.IndexOf(player.World.Players, player), player.InternalName, player.Faction?.InternalName,
			player.BotType, player.IsBot, player.NonCombatant || player.PlayerReference.NonCombatant);
	}

	public sealed class MissionEvent
	{
		public int PlayerIndex { get; private init; }
		public string PlayerInternalName { get; private init; }
		public string FactionInternalName { get; private init; }
		public string BotType { get; private init; }
		public bool IsBot { get; private init; }
		public bool NonCombatant { get; private init; }
		public string MissionId { get; private init; }
		public BotMissionEvent? Event { get; private init; }
		public int Attempt { get; private init; }
		public BotMissionAttemptState State { get; private init; }
		public string Reason { get; private init; }
		public string Executor { get; private init; }
		public string MissionType { get; private init; }
		public int? RegionIndex { get; private init; }
		public CellXY? TargetCell { get; private init; }
		public CellXY? UnitCell { get; private init; }
		public int? Units { get; private init; }
		public int? Value { get; private init; }
		public int Tick { get; private init; }

		MissionEvent() { }

		public static MissionEvent Detach(BotMissionRecord record, PlayerIdentity player) => new()
		{
			PlayerIndex = player.PlayerIndex,
			PlayerInternalName = player.PlayerInternalName,
			FactionInternalName = player.FactionInternalName,
			BotType = player.BotType,
			IsBot = player.IsBot,
			NonCombatant = player.NonCombatant,
			MissionId = record.MissionId,
			Event = record.Event,
			Attempt = record.Attempt,
			State = record.State,
			Reason = record.Reason,
			Executor = record.Executor,
			MissionType = record.MissionType,
			RegionIndex = record.RegionIndex,
			TargetCell = record.TargetCell.HasValue ? new CellXY(record.TargetCell.Value.X, record.TargetCell.Value.Y) : null,
			UnitCell = record.UnitCell.HasValue ? new CellXY(record.UnitCell.Value.X, record.UnitCell.Value.Y) : null,
			Units = record.Units,
			Value = record.Value,
			Tick = record.Tick,
		};
	}
}

namespace OpenRA.Mods.CA.Traits.BotModules
{
	public sealed class BotPassiveSquadEventSnapshot
	{
		public int Tick { get; init; }
		public string SquadId { get; init; }
		public int Generation { get; init; }
		public string Event { get; init; }
		public string MemberActorId { get; init; }
		public int Size { get; init; }
		public long Value { get; init; }
		public string StateFrom { get; init; }
		public string StateTo { get; init; }
		public string TargetType { get; init; }
		public string TargetCell { get; init; }
		public string Reason { get; init; }
		public string LeaseOwner { get; init; }

		/// <summary>IMPL02 A6 evidence (switch ON only). A null member is not written; see AggressionEvidenceCA for the meanings.</summary>
		public int? PredictedRatioMilli { get; init; }
		public int? CoalitionRatioMilli { get; init; }
		public string Band { get; init; }
		public long? TradeDelta { get; init; }
		public int? AlliesEngaged { get; init; }
		public string OwnFaction { get; init; }
		public string EnemyFaction { get; init; }
	}

	public sealed class BotPassiveUnitPositionSnapshot
	{
		public long ActorId { get; init; }
		public string ActorType { get; init; }
		public int X { get; init; }
		public int Y { get; init; }
		public string SquadId { get; init; }
		public string Activity { get; init; }
		public bool Live { get; init; }
		public bool InWorld { get; init; }
	}

	public sealed class BotPassiveAttackCommitSnapshot
	{
		public int Tick { get; init; }

		/// <summary>"ordinary" or "breaker".</summary>
		public string Kind { get; init; }
		public string SquadId { get; init; }
		public int Generation { get; init; }
		public long? WaveId { get; init; }
		public int CommittedCount { get; init; }
		public long CommittedValue { get; init; }
		public int PoolCount { get; init; }
		public long PoolValue { get; init; }
		public int ArmyCount { get; init; }
		public long ArmyValue { get; init; }
		public string TargetSeat { get; init; }

		/// <summary>"x,y" or null.</summary>
		public string TargetCell { get; init; }
	}

	public sealed class BotPassiveDispatchCheckSnapshot
	{
		public int Tick { get; init; }
		public int CandIndex { get; init; }
		public string TargetSeat { get; init; }
		public long? TargetActor { get; init; }
		public long NumL { get; init; }
		public long DenL { get; init; }
		public long DenDefenceFresh { get; init; }
		public long NumT { get; init; }
		public long DenT { get; init; }
		public long ScoreMilli { get; init; }
		public bool ForceOk { get; init; }
		public bool ClockOk { get; init; }
		public int TicksSinceKill { get; init; }
		public string Result { get; init; }
		public string Urgency { get; init; }
		public string UrgencyLegacy { get; init; }
		public bool HomeThreat { get; init; }
		public bool HomeLatch { get; init; }
	}

	public sealed class BotPassiveCandidateFailure
	{
		public long TargetActor { get; init; }
		public string Reason { get; init; }
	}

	public sealed class BotPassiveJobEndSnapshot
	{
		public int Tick { get; init; }

		/// <summary>"admit" or "hold".</summary>
		public string Result { get; init; }
		public string HoldReason { get; init; }

		/// <summary>At most 4 entries.</summary>
		IReadOnlyList<BotPassiveCandidateFailure> candidateFailures;
		public IReadOnlyList<BotPassiveCandidateFailure> CandidateFailures
		{
			get => candidateFailures;
			init => candidateFailures = value == null ? null : Array.AsReadOnly(value.ToArray());
		}
		public int DeferredTicks { get; init; }
		public int Paths { get; init; }
		public int ReachChecks { get; init; }
		public int CandidatesTried { get; init; }
		public int UnitsAdmitted { get; init; }
		public int UnitsUnreachable { get; init; }
		public int UnitsExcludedGroups { get; init; }
		public long ValueAdmitted { get; init; }
	}

	public sealed class BotPassiveWaveGroup
	{
		public string Locomotor { get; init; }
		public long RepActor { get; init; }
		public int L { get; init; }
		public int S { get; init; }

		/// <summary>Resolved target cell, "x,y".</summary>
		public string Tc { get; init; }

		/// <summary>Group centroid cell, "x,y".</summary>
		public string Cen { get; init; }
	}

	public sealed class BotPassiveWaveStartSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }
		public int C { get; init; }
		public int E { get; init; }

		/// <summary>At most 3 entries.</summary>
		IReadOnlyList<BotPassiveWaveGroup> groups;
		public IReadOnlyList<BotPassiveWaveGroup> Groups
		{
			get => groups;
			init => groups = value == null ? null : Array.AsReadOnly(value.ToArray());
		}
		public int RallyDuration { get; init; }
		public int NoProgressDuration { get; init; }
		public int AbsoluteDuration { get; init; }
		public int RallyDeadline { get; init; }
		public int AbsoluteDeadline { get; init; }
		public int ArmTick { get; init; }
		public int N0 { get; init; }
		public long V0 { get; init; }
		public int CommittedCount { get; init; }
		public long CommittedValue { get; init; }
		public long LeaderActor { get; init; }
		public string TargetSeat { get; init; }
		public long TargetActor { get; init; }
		public string TargetCell { get; init; }
	}

	public sealed class BotPassiveWaveSampleSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }

		/// <summary>"periodic", "leader_change" or "retarget".</summary>
		public string Event { get; init; }
		public int LastProgressTick { get; init; }
		public int BestL { get; init; }
		public int? CurL { get; init; }
		public bool Arrived { get; init; }
		public int UnitsAlive { get; init; }
		public long ValueAlive { get; init; }
		public long DeadCost { get; init; }
		public long LeaderActor { get; init; }
	}

	public sealed class BotPassiveRallyEndSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }
		public int AssembledCount { get; init; }
		public long AssembledValue { get; init; }
		public int N0 { get; init; }
		public long V0 { get; init; }
		public bool Pass { get; init; }
	}

	public sealed class BotPassiveWaveExitSnapshot
	{
		public int Tick { get; init; }
		public long WaveId { get; init; }

		/// <summary>StalemateBreakerWaveCA.WireName of the end reason.</summary>
		public string ExitReason { get; init; }
		public int LastProgressTick { get; init; }
		public int BestL { get; init; }
		public int UnitsAlive { get; init; }
		public long ValueLost { get; init; }
		public int Duration { get; init; }
		public int RouteEstL { get; init; }
		public int RouteActualCells { get; init; }
		public int RouteBestL { get; init; }
	}

	public sealed class BotPassiveHealthSnapshot
	{
		public int Tick { get; init; }
		public int IdleCombatUnits { get; init; }
		public int UnsquaddedCombatUnits { get; init; }
		public int HomeThirdUnits { get; init; }
		public int CombatUnits { get; init; }
		public int StuckSquads { get; init; }
		public int RetreatsWindow { get; init; }
		public int KillsWindow { get; init; }
		public string Urgency { get; init; }
		public string UrgencyLegacy { get; init; }
	}
}
