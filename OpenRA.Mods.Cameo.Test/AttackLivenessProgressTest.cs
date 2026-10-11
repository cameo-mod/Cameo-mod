using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AttackLivenessProgressTest
	{
		static AttackLivenessEvalCA Eligible()
		{
			var state = new AttackLivenessEvalCA();
			state.Observe(10, true, true, true, true, 1000, 1000, 7500, 0, AttackSoftRestraintCA.None);
			return state;
		}

		[Test]
		public void AcceptedReceiptPreservesTheIntervalThatWasReset()
		{
			var state = Eligible();
			Assert.That(state.ObserveOffensiveActivity(20, 1, 3, true, true, true, true, out var receipt), Is.True);
			Assert.That(receipt, Is.EqualTo(new AttackLivenessProgressCA(20, 1, 3, 1, 10, 7510)));
			Assert.That(state.DeadlineTick, Is.EqualTo(-1));
		}

		[TestCase(false, true, true, true, 3, 1)]
		[TestCase(true, false, true, true, 3, 1)]
		[TestCase(true, true, false, true, 3, 1)]
		[TestCase(true, true, true, false, 3, 1)]
		[TestCase(true, true, true, true, 0, 1)]
		[TestCase(true, true, true, true, 3, 0)]
		public void RejectedEvidenceCannotProduceAProgressReceipt(bool owned, bool actual,
			bool legal, bool complete, int members, long wave)
		{
			var state = Eligible();
			Assert.That(state.ObserveOffensiveActivity(20, wave, members, owned, actual, legal, complete,
				out var receipt), Is.False);
			Assert.That(receipt, Is.EqualTo(default(AttackLivenessProgressCA)));
			Assert.That(state.DeadlineTick, Is.EqualTo(7510));
			Assert.That(state.ObservedOffensiveCount, Is.Zero);
		}

		[Test]
		public void RepeatedOrOlderWavesDoNotLogOrResetANewInterval()
		{
			var state = Eligible();
			Assert.That(state.ObserveOffensiveActivity(20, 2, 3, true, true, true, true, out _), Is.True);
			state.Observe(30, true, true, true, true, 1000, 1000, 7500, 0, AttackSoftRestraintCA.None);
			foreach (var wave in new long[] { 2, 1 })
			{
				Assert.That(state.ObserveOffensiveActivity(40, wave, 3, true, true, true, true,
					out var receipt), Is.False);
				Assert.That(receipt, Is.EqualTo(default(AttackLivenessProgressCA)));
			}
			Assert.That(state.DeadlineTick, Is.EqualTo(7530));
			Assert.That(state.ObserveOffensiveActivity(50, 3, 2, true, true, true, true, out var next), Is.True);
			Assert.That(next, Is.EqualTo(new AttackLivenessProgressCA(50, 3, 2, 2, 30, 7530)));
		}
	}
}
