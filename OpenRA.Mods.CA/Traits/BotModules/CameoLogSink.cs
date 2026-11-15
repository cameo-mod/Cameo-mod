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
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>The resident boundary between simulation code and optional development logging.</summary>
	public interface ICameoLogSink
	{
		bool Enabled { get; }
		void MissionRecorded(MissionEvent record);
		void BeginPassive(int playerIndex, bool enabled, string gameUid, string seat, string botType, int tick);
		void PassiveSquadEvent(int playerIndex, BotPassiveSquadEventSnapshot snapshot);
		void PassiveAttackCommit(int playerIndex, BotPassiveAttackCommitSnapshot snapshot);
		void PassiveDispatchCheck(int playerIndex, BotPassiveDispatchCheckSnapshot snapshot);
		void PassiveJobEnd(int playerIndex, BotPassiveJobEndSnapshot snapshot);
		void PassiveWaveStart(int playerIndex, BotPassiveWaveStartSnapshot snapshot);
		void PassiveWaveSample(int playerIndex, BotPassiveWaveSampleSnapshot snapshot);
		void PassiveRallyEnd(int playerIndex, BotPassiveRallyEndSnapshot snapshot);
		void PassiveWaveExit(int playerIndex, BotPassiveWaveExitSnapshot snapshot);
		void PassiveHealth(int playerIndex, BotPassiveHealthSnapshot snapshot);
		void PassivePositions(int playerIndex, int tick, IReadOnlyList<BotPassiveUnitPositionSnapshot> roster, int expectedCount, string checksum);
		void CompletePassive(int playerIndex, int tick);
	}

	/// <summary>The release default: no logger is installed.</summary>
	public sealed class NullCameoLogSink : ICameoLogSink
	{
		public static readonly NullCameoLogSink Instance = new();

		NullCameoLogSink() { }

		public bool Enabled => false;
		public void MissionRecorded(MissionEvent record) { }
		public void BeginPassive(int playerIndex, bool enabled, string gameUid, string seat, string botType, int tick) { }
		public void PassiveSquadEvent(int playerIndex, BotPassiveSquadEventSnapshot snapshot) { }
		public void PassiveAttackCommit(int playerIndex, BotPassiveAttackCommitSnapshot snapshot) { }
		public void PassiveDispatchCheck(int playerIndex, BotPassiveDispatchCheckSnapshot snapshot) { }
		public void PassiveJobEnd(int playerIndex, BotPassiveJobEndSnapshot snapshot) { }
		public void PassiveWaveStart(int playerIndex, BotPassiveWaveStartSnapshot snapshot) { }
		public void PassiveWaveSample(int playerIndex, BotPassiveWaveSampleSnapshot snapshot) { }
		public void PassiveRallyEnd(int playerIndex, BotPassiveRallyEndSnapshot snapshot) { }
		public void PassiveWaveExit(int playerIndex, BotPassiveWaveExitSnapshot snapshot) { }
		public void PassiveHealth(int playerIndex, BotPassiveHealthSnapshot snapshot) { }
		public void PassivePositions(int playerIndex, int tick, IReadOnlyList<BotPassiveUnitPositionSnapshot> roster, int expectedCount, string checksum) { }
		public void CompletePassive(int playerIndex, int tick) { }
	}

	// A value-only diagnostic channel: it never holds a player, actor, world or writer.
	internal sealed class BotPassiveChannel
	{
		public const int MaximumPositionChunkEntries = 256;
		public const int MaximumPositionChunks = 16;
		public const int PositionIntervalTicks = 250;
		public const int MaximumPositionTick = 50000;
		readonly ICameoLogSink sink;
		readonly int playerIndex;
		public bool Enabled { get; }

		public BotPassiveChannel(ICameoLogSink sink, int playerIndex, bool enabled, string uid, string seat, string bot, int tick)
		{
			this.sink = sink;
			this.playerIndex = playerIndex;
			Enabled = enabled;
			sink.BeginPassive(playerIndex, enabled, uid, seat, bot, tick);
		}

		public static string Cell(int x, int y) => x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ","
			+ y.ToString(System.Globalization.CultureInfo.InvariantCulture);
		public void TryEnqueueSquadEvent(BotPassiveSquadEventSnapshot s) => sink.PassiveSquadEvent(playerIndex, s);
		public void TryEnqueueAttackCommit(BotPassiveAttackCommitSnapshot s) => sink.PassiveAttackCommit(playerIndex, s);
		public void TryEnqueueDispatchCheck(BotPassiveDispatchCheckSnapshot s) => sink.PassiveDispatchCheck(playerIndex, s);
		public void TryEnqueueJobEnd(BotPassiveJobEndSnapshot s) => sink.PassiveJobEnd(playerIndex, s);
		public void TryEnqueueWaveStart(BotPassiveWaveStartSnapshot s) => sink.PassiveWaveStart(playerIndex, s);
		public void TryEnqueueWaveSample(BotPassiveWaveSampleSnapshot s) => sink.PassiveWaveSample(playerIndex, s);
		public void TryEnqueueRallyEnd(BotPassiveRallyEndSnapshot s) => sink.PassiveRallyEnd(playerIndex, s);
		public void TryEnqueueWaveExit(BotPassiveWaveExitSnapshot s) => sink.PassiveWaveExit(playerIndex, s);
		public void TryEnqueueHealth(BotPassiveHealthSnapshot s) => sink.PassiveHealth(playerIndex, s);
		public void TryEnqueueUnitPosition(int tick, IReadOnlyList<BotPassiveUnitPositionSnapshot> roster, int count, string checksum) =>
			sink.PassivePositions(playerIndex, tick, roster == null ? null : Array.AsReadOnly(roster.ToArray()), count, checksum);
		public void Complete(int tick) => sink.CompletePassive(playerIndex, tick);
	}
}
