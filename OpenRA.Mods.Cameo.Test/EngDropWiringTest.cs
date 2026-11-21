#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System.Collections.Generic;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>ENGDROP stage 1 (ENGDROP_IMPL_SPEC_02 + erratum 01): the module-side seams that need no World.</summary>
	[TestFixture]
	public sealed class EngDropWiringTest
	{
		// ai.yaml EngineerBotModule: DropChancePct 55..100, DropIntervalTicks 495..225, DropInitialDelayTicks 615..75
		// (Easy for the easiest tier, Hard for cameogod). 10 listed tiers, so 9 steps; noob is rank -1 (extrapolated).
		[Test]
		public void YamlLadderLinesGiveTheScheduleVectorOnEveryTier()
		{
			for (var rank = BotDifficultyLadder.NoobRank; rank <= 9; rank++)
			{
				var tier = DropScheduler.ForTier(rank + 1);
				Assert.That(BotDifficultyLadder.InterpolateByRank(55, 100, rank, BotDifficultyLadder.Difficulties.Length), Is.EqualTo(tier.ChancePercent), $"chance rank {rank}");
				Assert.That(BotDifficultyLadder.InterpolateByRank(495, 225, rank, BotDifficultyLadder.Difficulties.Length), Is.EqualTo(tier.IntervalTicks), $"interval rank {rank}");
				Assert.That(BotDifficultyLadder.InterpolateByRank(615, 75, rank, BotDifficultyLadder.Difficulties.Length), Is.EqualTo(tier.InitialDelayTicks), $"initial rank {rank}");
			}
		}

		// A provider that predates ENGDROP must refuse an opportunity drop, never queue a transport on its behalf.
		[Test]
		public void ProviderWithoutTheProbeRefusesAnOpportunityDrop()
		{
			IBotCaptureTransportProvider provider = new LegacyProvider();
			Assert.That(provider.TryRequestCaptureRunWithoutProduction(null, new List<Actor>(), new List<Actor>()), Is.False);
			Assert.That(((LegacyProvider)provider).Requests, Is.EqualTo(0), "the fail-closed default must not reach the production-capable request");
		}

		// A and B due on the same visit: B's one draw is consumed and B's timer reloads, but a run A admitted blocks B's run.
		[Test]
		public void OpportunityAdmissionConsumesTheRegularDrawWithoutRunning()
		{
			var scheduler = DropScheduler.ForTier(10);
			var visit = scheduler.Visit(scheduler.Start(0), scheduler.InitialDelayTicks);
			Assert.That(visit.ScanOpportunity && visit.DrawRegular, Is.True);
			Assert.That(visit.State.NextRegularTick, Is.EqualTo(scheduler.InitialDelayTicks + scheduler.IntervalTicks));
			Assert.That(visit.TryRegular(0, scheduler.ChancePercent, true), Is.False);
			Assert.That(visit.TryRegular(0, scheduler.ChancePercent, false), Is.True);
		}

		sealed class LegacyProvider : IBotCaptureTransportProvider
		{
			public int Requests;

			public bool TryRequestCaptureRun(IBot bot, IReadOnlyList<Actor> passengers, IReadOnlyList<Actor> targets)
			{
				Requests++;
				return true;
			}

			public bool IsHandlingPassenger(Actor passenger) => false;

			public bool TryConsumeDelivered(Actor passenger, out Actor target)
			{
				target = null;
				return false;
			}
		}
	}
}
