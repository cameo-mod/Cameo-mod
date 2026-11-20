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
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// STAGGER_DESIGN_06 7.1 (Opus condition): CombatAnalysis weights from a deferred snapshot equal today's synchronous weights.
	[TestFixture]
	public sealed class CombatAnalysisDealtStepTest
	{
		const int MinDamage = 3;
		const float Weight = 5f;
		const int Interval = 25;

		// Today's inline logic (CombatAnalysisBotModule INotifyAppliedDamage before STAGGER-06), kept verbatim as the reference.
		sealed class Reference
		{
			public float Score;
			public int? Next;

			public void Hit(int tick, int damage)
			{
				if (damage < MinDamage)
					return;

				if (Next.HasValue && tick < Next.Value)
					return;

				Score = Math.Min(100f, Score + Weight);
				Next = tick + Math.Max(1, Interval);
			}
		}

		sealed class Step
		{
			public float Score;
			public int? Next;

			public void Hit(int tick, int damage)
			{
				var r = CombatAnalysisBotModule.DealtStep(Score, Next, tick, damage, MinDamage, Weight, Interval);
				if (!r.Applied)
					return;

				Score = r.Score;
				Next = r.NextTick;
			}
		}

		static List<(int Tick, int Damage)> Hits(int seed, int count)
		{
			var rng = new Random(seed);
			var hits = new List<(int, int)>();
			var tick = 0;
			for (var i = 0; i < count; i++)
			{
				tick += rng.Next(0, 9);
				hits.Add((tick, rng.Next(0, 12)));
			}

			return hits;
		}

		[Test]
		public void BelowTheDamageThresholdNothingHappens()
		{
			var r = CombatAnalysisBotModule.DealtStep(10f, null, 100, MinDamage - 1, MinDamage, Weight, Interval);
			Assert.That(r.Applied, Is.False);
			Assert.That(r.Score, Is.EqualTo(10f));
		}

		[Test]
		public void TheFirstHitAppliesAndTheThrottleRunsFromTheEventTick()
		{
			var r = CombatAnalysisBotModule.DealtStep(0f, null, 100, 10, MinDamage, Weight, Interval);
			Assert.That(r.Applied, Is.True);
			Assert.That(r.Score, Is.EqualTo(5f));
			Assert.That(r.NextTick, Is.EqualTo(125));

			Assert.That(CombatAnalysisBotModule.DealtStep(5f, 125, 124, 10, MinDamage, Weight, Interval).Applied, Is.False);
			Assert.That(CombatAnalysisBotModule.DealtStep(5f, 125, 125, 10, MinDamage, Weight, Interval).Applied, Is.True);
		}

		[Test]
		public void TheScoreIsCappedAtOneHundredAndAZeroIntervalStillMovesOneTick()
		{
			var r = CombatAnalysisBotModule.DealtStep(98f, null, 7, 10, MinDamage, Weight, 0);
			Assert.That(r.Score, Is.EqualTo(100f));
			Assert.That(r.NextTick, Is.EqualTo(8));
		}

		[Test]
		public void AtNOneTheStepMatchesTodaysRuleOverManyRandomHitSequences()
		{
			for (var seed = 1; seed <= 40; seed++)
			{
				var reference = new Reference();
				var step = new Step();
				foreach (var (tick, damage) in Hits(seed, 400))
				{
					reference.Hit(tick, damage);
					step.Hit(tick, damage);
					Assert.That(step.Score, Is.EqualTo(reference.Score), $"seed {seed} tick {tick}");
					Assert.That(step.Next, Is.EqualTo(reference.Next), $"seed {seed} tick {tick}");
				}
			}
		}

		[Test]
		public void DeliveringTheSnapshotsLaterInFifoOrderGivesTheSameWeightsAsDeliveringThemNow()
		{
			// The deferred path replays the same (event tick, damage) pairs in arrival order at later world ticks;
			// the step has no clock input, so the result must be identical to the synchronous one.
			for (var seed = 1; seed <= 40; seed++)
			{
				var hits = Hits(seed, 400);
				var now = new Step();
				foreach (var (tick, damage) in hits)
					now.Hit(tick, damage);

				var later = new Step();
				var queue = new Queue<(int, int)>(hits);
				while (queue.Count > 0)
				{
					var (tick, damage) = queue.Dequeue();
					later.Hit(tick, damage);
				}

				Assert.That(later.Score, Is.EqualTo(now.Score), $"seed {seed}");
				Assert.That(later.Next, Is.EqualTo(now.Next), $"seed {seed}");
			}
		}

		[Test]
		public void TheSnapshotKeepsEveryFieldThroughTheSavedForm()
		{
			var ev = new DamageDealtEvent
			{
				Seq = 9,
				ArrivalTick = 777,
				Recipient = new ModuleKey("Multi2", "CombatAnalysisBotModule@0"),
				EventTick = 770,
				DamagedActorId = 4242,
				VictimOwner = "Multi5",
				VictimType = "harv",
				AttackerActorId = 17,
				AttackerOwner = "Multi1",
				AttackerType = "e1",
				Damage = 63,
				LocationX = 31,
				LocationY = 87,
				Dead = true,
			};

			var back = (DamageDealtEvent)BotEvent.Load(ev.Save().Value);
			Assert.That(back, Is.Not.Null);
			Assert.That(back.Recipient, Is.EqualTo(ev.Recipient));
			Assert.That(back.EventTick, Is.EqualTo(770));
			Assert.That(back.DamagedActorId, Is.EqualTo(4242u));
			Assert.That(back.VictimOwner, Is.EqualTo("Multi5"));
			Assert.That(back.VictimType, Is.EqualTo("harv"));
			Assert.That(back.AttackerActorId, Is.EqualTo(17u));
			Assert.That(back.AttackerOwner, Is.EqualTo("Multi1"));
			Assert.That(back.AttackerType, Is.EqualTo("e1"));
			Assert.That(back.Damage, Is.EqualTo(63));
			Assert.That(back.LocationX, Is.EqualTo(31));
			Assert.That(back.LocationY, Is.EqualTo(87));
			Assert.That(back.Dead, Is.True);
			Assert.That(back.ActorIds, Is.Empty, "a snapshot holds no live reference to resolve after a load");
		}
	}
}
