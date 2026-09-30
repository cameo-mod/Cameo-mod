#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// MC1 (docs/design/AI_MISSION_CARDS.md): one vocabulary and one line format for every bot stack's mission cards.
	[TestFixture]
	public sealed class BotMissionLogTest
	{
		[Test]
		public void TheGeneralsLogLineIsGreppableByMissionAndAttempt()
		{
			var line = BotMissionLog.FormatLine("Multi0", "capture:Multi1:td_gdi_constructionyard:412", 2,
				BotMissionAttemptState.Failed, BotMissionReasons.LostUnits, "Engineers", 5210);
			Assert.That(line, Is.EqualTo(
				"AI Multi0: MISSION capture:Multi1:td_gdi_constructionyard:412 ATTEMPT 2 FAILED reason=lost_units by=Engineers tick=5210"));
		}

		[Test]
		public void TheStateSetIsFransottosSixPlusFailedAndReleased()
		{
			Assert.That(Enum.GetNames<BotMissionAttemptState>(), Is.EqualTo(new[]
				{ "Denied", "Committed", "Progressing", "Stalled", "Recover", "Success", "Failed", "Released" }));
			Assert.That(BotMissionLog.IsTerminal(BotMissionAttemptState.Denied), Is.True);
			Assert.That(BotMissionLog.IsTerminal(BotMissionAttemptState.Released), Is.True);
			Assert.That(BotMissionLog.IsTerminal(BotMissionAttemptState.Stalled), Is.False);
			Assert.That(BotMissionLog.IsTerminal(BotMissionAttemptState.Recover), Is.False);
		}

		[Test]
		public void ReasonsAreTheSharedSetOrAPrivateXPrefix()
		{
			Assert.That(BotMissionReasons.IsValid(null), Is.True);
			Assert.That(BotMissionReasons.IsValid("lost_units"), Is.True);
			Assert.That(BotMissionReasons.IsValid("x_frans_board_closed"), Is.True);
			Assert.That(BotMissionReasons.IsValid("mission-board-closed"), Is.False, "a dialect must be caught, not archived");
			Assert.That(BotMissionReasons.IsValid("x_Board"), Is.False);
			Assert.That(BotMissionReasons.IsValid("x_"), Is.False);
		}

		[Test]
		public void TheArchiveLineIsOneJsonObjectWithTheSchemaAndIds()
		{
			var record = new BotMissionRecord
			{
				MissionId = "capture:Multi1:oilb:526", Attempt = 1, State = BotMissionAttemptState.Success,
				Reason = BotMissionReasons.Done, Executor = "Engineers", MissionType = "capture",
				TargetCell = new CPos(61, 33), Units = 1, Tick = 2561
			};

			var line = AiMissionLogWriter.BuildLine(record, "g-uid", "m-uid", "A Nuclear Winter", new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc));
			using var doc = JsonDocument.Parse(line);
			var root = doc.RootElement;
			Assert.That(root.GetProperty("schema").GetString(), Is.EqualTo("mission-card/1"));
			Assert.That(root.GetProperty("mission_id").GetString(), Is.EqualTo("capture:Multi1:oilb:526"));
			Assert.That(root.GetProperty("attempt_id").GetString(), Is.EqualTo("capture:Multi1:oilb:526|A1"));
			Assert.That(root.GetProperty("state").GetString(), Is.EqualTo("SUCCESS"));
			Assert.That(root.GetProperty("terminal").GetBoolean(), Is.True);
			Assert.That(root.GetProperty("target_cell").GetString(), Is.EqualTo("61,33"));
			Assert.That(root.GetProperty("tick").GetInt32(), Is.EqualTo(2561));
			Assert.That(root.TryGetProperty("region", out _), Is.False, "absent context stays absent");
		}

		[Test]
		public void ACaptureThatTurnedTheBuildingOursIsASuccessEvenThoughTheEngineerIsGone()
		{
			// The engineer is consumed by the capture, and Actor.IsDead includes Disposed.
			Assert.That(EngineerBotModule.CaptureVerdict(targetOurs: true, stuck: false, engineerDead: true, targetGone: false),
				Is.EqualTo((BotMissionAttemptState.Success, BotMissionReasons.Done)));
		}

		[Test]
		public void CaptureVerdictsForTheOtherEndings()
		{
			Assert.That(EngineerBotModule.CaptureVerdict(false, true, false, false),
				Is.EqualTo((BotMissionAttemptState.Released, BotMissionReasons.Stuck)));
			Assert.That(EngineerBotModule.CaptureVerdict(false, false, true, false),
				Is.EqualTo((BotMissionAttemptState.Failed, BotMissionReasons.LostUnits)));
			Assert.That(EngineerBotModule.CaptureVerdict(false, false, false, true),
				Is.EqualTo((BotMissionAttemptState.Released, BotMissionReasons.TargetGone)));
			Assert.That(EngineerBotModule.CaptureVerdict(false, false, false, false),
				Is.EqualTo((BotMissionAttemptState.Released, BotMissionReasons.Dropped)));
		}

		[Test]
		public void TheCaptureMissionIsTheBuildingNotTheEngineer()
		{
			Assert.That(EngineerBotModule.CaptureMissionId("Multi1", "oilb", 526), Is.EqualTo("capture:Multi1:oilb:526"));
		}
	}
}
