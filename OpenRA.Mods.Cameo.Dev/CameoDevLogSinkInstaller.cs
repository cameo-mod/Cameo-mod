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
using System.IO;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>The development-only endpoint installed behind the resident hub.</summary>
	public sealed class CameoDevLogSink : ICameoLogSink
	{
		readonly Actor worldActor;
		readonly Dictionary<int, BotPassiveDiagnosticWriter> passive = new();
		readonly ContextLogWriter context = new();
		public CameoDevLogSink(Actor worldActor) { this.worldActor = worldActor; }
		public bool Enabled => true;
		public void InsurancePayout(int playerIndex, int tick, string reason, int requested, long before, long after) =>
			worldActor.TraitOrDefault<InsuranceTelemetryDev>()?.Record(playerIndex, tick, reason, requested, before, after);
		public void EngagementLine(string line) => worldActor.TraitOrDefault<AiEngagementLogWriterDev>()?.Append(line);
		public void EngagementEnd(int tick) => worldActor.TraitOrDefault<AiEngagementLogWriterDev>()?.End(tick);
		public void ContextRow(int playerIndex, string fileName, string row, int tick) => context.Enqueue(playerIndex, fileName, row, tick);
		public void FlushContext(int playerIndex, int tick) => context.Flush(playerIndex, tick);
		public void InfantryRejectRow(int playerIndex, string fileName, string row, int tick) =>
			worldActor.TraitOrDefault<InfantryRejectCsvWriterDev>()?.Enqueue(playerIndex, fileName, row, tick);

		// TECH-LOG-01: tech_log.csv rows go through the same DEV writer (flush every 750 ticks and at game end). Header once per seat.
		readonly HashSet<int> techHeaderWritten = new();

		public void TechLogEvent(int playerIndex, string faction, int tick, string kind, string type, string reason)
		{
			var writer = worldActor.TraitOrDefault<InfantryRejectCsvWriterDev>();
			if (writer == null || playerIndex < 0 || playerIndex >= worldActor.World.Players.Length)
				return;

			if (techHeaderWritten.Add(playerIndex))
				writer.Enqueue(playerIndex, TechLogCA.FileName, TechLogCA.Header, tick);

			var seat = AiMatchLogWriterDev.SeatKey(worldActor.World, worldActor.World.Players[playerIndex]);
			writer.Enqueue(playerIndex, TechLogCA.FileName, TechLogCA.Row(tick, seat, faction, kind, type, reason), tick);
		}

		public void MissionRecorded(MissionEvent record)
		{
			Log.Write("debug", record.Event is BotMissionEvent e
				? BotMissionLog.FormatEventLine(record.PlayerInternalName, record.MissionId, e, record.Reason, record.Executor, record.Tick)
				: BotMissionLog.FormatLine(record.PlayerInternalName, record.MissionId, record.Attempt, record.State, record.Reason, record.Executor, record.Tick));
			worldActor.TraitOrDefault<AiMissionLogWriterDev>()?.MissionRecorded(record);
		}

		public void BeginPassive(int playerIndex, bool enabled, string gameUid, string seat, string botType, int tick)
		{
			if (passive.ContainsKey(playerIndex))
				return;
			string path = null;
			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				path = Path.Combine(directory, $"cameo-ai-botpassive-{gameUid}-{seat}-{tick}.jsonl");
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
			{
				path = null;
			}
			BotPassiveDiagnosticWriter.TryCreate(enabled, path, gameUid, seat, botType, out var writer);
			passive[playerIndex] = writer;
		}

		BotPassiveDiagnosticWriter Writer(int playerIndex) => passive.GetValueOrDefault(playerIndex);
		public void PassiveSquadEvent(int playerIndex, BotPassiveSquadEventSnapshot s) => Writer(playerIndex)?.TryEnqueueSquadEvent(s);
		public void PassiveAttackCommit(int playerIndex, BotPassiveAttackCommitSnapshot s) => Writer(playerIndex)?.TryEnqueueAttackCommit(s);
		public void PassiveDispatchCheck(int playerIndex, BotPassiveDispatchCheckSnapshot s) => Writer(playerIndex)?.TryEnqueueDispatchCheck(s);
		public void PassiveJobEnd(int playerIndex, BotPassiveJobEndSnapshot s) => Writer(playerIndex)?.TryEnqueueJobEnd(s);
		public void PassiveWaveStart(int playerIndex, BotPassiveWaveStartSnapshot s) => Writer(playerIndex)?.TryEnqueueWaveStart(s);
		public void PassiveWaveSample(int playerIndex, BotPassiveWaveSampleSnapshot s) => Writer(playerIndex)?.TryEnqueueWaveSample(s);
		public void PassiveRallyEnd(int playerIndex, BotPassiveRallyEndSnapshot s) => Writer(playerIndex)?.TryEnqueueRallyEnd(s);
		public void PassiveWaveExit(int playerIndex, BotPassiveWaveExitSnapshot s) => Writer(playerIndex)?.TryEnqueueWaveExit(s);
		public void PassiveHealth(int playerIndex, BotPassiveHealthSnapshot s) => Writer(playerIndex)?.TryEnqueueHealth(s);
		public void PassivePositions(int playerIndex, int tick, IReadOnlyList<BotPassiveUnitPositionSnapshot> roster, int expectedCount, string checksum) =>
			Writer(playerIndex)?.TryEnqueueUnitPosition(tick, roster, expectedCount, checksum);
		public void CompletePassive(int playerIndex, int tick) => Writer(playerIndex)?.Complete(tick);
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Installs the development logging sink on the world actor.")]
	public sealed class CameoDevLogSinkInstallerInfo : TraitInfo, Requires<CameoLogHubInfo>
	{
		public override object Create(ActorInitializer init) => new CameoDevLogSinkInstaller();
	}

	public sealed class CameoDevLogSinkInstaller : INotifyCreated
	{
		void INotifyCreated.Created(Actor self)
		{
			self.Trait<CameoLogHub>().Install(new CameoDevLogSink(self));
		}
	}
}
