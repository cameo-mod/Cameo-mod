using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class StalemateSeatOwnerTest
	{
		static StalemateBreakerWaveCA Wave() => new(100, new[] { new StalemateWaveMemberCA(1, 1, 100) });

		[Test]
		public void HandoffPreservesTheExactWaveAndBlocksOrdersUntilAdoption()
		{
			var seat = new StalemateSeatOwnerCA();
			var wave = Wave();
			Assert.That(seat.TryStart("rush", wave), Is.True);
			Assert.That(seat.TryStart("turtle", Wave()), Is.False);
			Assert.That(seat.TryBeginHandoff("rush", "turtle", out var token), Is.True);
			Assert.That(seat.MayIssueOrders("rush"), Is.False);
			Assert.That(seat.MayIssueOrders("turtle"), Is.False);
			Assert.That(seat.FinishHandoff(token, "turtle", true, false), Is.True);
			Assert.That(seat.Wave, Is.SameAs(wave));
			Assert.That(seat.Wave.StartTick, Is.EqualTo(100));
			Assert.That(seat.MayIssueOrders("rush"), Is.False);
			Assert.That(seat.MayIssueOrders("turtle"), Is.True);
		}

		[Test]
		public void FailedTransferKeepsBothSidesSilentUntilRollbackIsConfirmed()
		{
			var seat = new StalemateSeatOwnerCA();
			seat.TryStart("rush", Wave());
			seat.TryBeginHandoff("rush", "turtle", out var token);
			Assert.That(seat.FinishHandoff(token, "turtle", false, false), Is.False);
			Assert.That(seat.MayIssueOrders("rush"), Is.False);
			Assert.That(seat.FinishHandoff(token + 1, "turtle", false, true), Is.False);
			Assert.That(seat.FinishHandoff(token, "turtle", false, true), Is.False);
			Assert.That(seat.MayIssueOrders("rush"), Is.True);
		}
	}
}
