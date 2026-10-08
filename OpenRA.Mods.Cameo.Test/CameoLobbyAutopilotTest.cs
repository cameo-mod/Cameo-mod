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

using NUnit.Framework;
using OpenRA.Mods.Cameo.ServerTraits;
using OpenRA.Server;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class CameoLobbyAutopilotTest
	{
		[Test]
		public void StartAttemptConfirmsOnFinalAttempt()
		{
			// Regression: InterpretCommand is synchronous - the 20th startgame can
			// succeed, so exhaustion must not be declared before the result is read.
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.GameStarted, 20, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Confirmed));
		}

		[Test]
		public void StartAttemptConfirmsEarlySuccess()
		{
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.GameStarted, 1, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Confirmed));
		}

		[Test]
		public void StartAttemptRetriesWhileWaiting()
		{
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.WaitingPlayers, 19, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Retry));
		}

		[Test]
		public void StartAttemptExhaustsOnlyWhileStillWaiting()
		{
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.WaitingPlayers, 20, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Exhausted));
		}

		[Test]
		public void StartAttemptShutdownIsAbortNotConfirm()
		{
			// Regression: only GameStarted confirms a start - ShuttingDown during the
			// Starting phase must not log "game start confirmed".
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.ShuttingDown, 1, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Aborted));
			Assert.That(CameoLobbyAutopilot.EvaluateStartAttempt(ServerState.ShuttingDown, 20, 20),
				Is.EqualTo(CameoLobbyAutopilot.StartAttemptOutcome.Aborted));
		}
	}
}
