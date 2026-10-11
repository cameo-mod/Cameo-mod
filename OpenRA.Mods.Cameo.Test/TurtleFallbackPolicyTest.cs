using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class TurtleFallbackPolicyTest
	{
		static readonly PersonalityChoice Fallback = new("turtle", PersonalityCandidateReason.TerminalPressure);
		[TestCase("rush")]
		[TestCase("steamroller")]
		[TestCase("guerrilla")]
		public void PressureAndChangedTierCannotRenewOriginalExpiry(string incumbent)
		{
			var policy = new TurtleFallbackPolicy();
			Assert.That(policy.Select(100, true, Fallback, incumbent, true, false, 1000).Personality, Is.EqualTo(incumbent));
			Assert.That(policy.Select(1099, true, Fallback, incumbent, true, false, 9000).ExpiresTick, Is.EqualTo(1100));
			var expired = policy.Select(1100, true, Fallback, incumbent, true, false, 9000);
			Assert.That(expired.Personality, Is.EqualTo("turtle"));
			Assert.That(expired.Reason, Is.EqualTo("Expired"));
			Assert.That(policy.Select(1101, true, Fallback, incumbent, true, false, 9000), Is.EqualTo(expired));
		}

		[TestCase("rush")]
		[TestCase("turtle")]
		[TestCase("steamroller")]
		[TestCase("guerrilla")]
		[TestCase("expansion")]
		[TestCase("tech")]
		public void OffReturnsExactlyTheOriginalCandidateWithoutTrackingTime(string candidate)
		{
			var policy = new TurtleFallbackPolicy();
			var choice = new PersonalityChoice(candidate, PersonalityCandidateReason.TerminalPressure);
			Assert.That(policy.Select(100, false, choice, "rush", true, false, 1000).Personality, Is.EqualTo(candidate));
			Assert.That(policy.Select(0, false, choice, "rush", true, true, 1000).Personality, Is.EqualTo(candidate));
		}

		[Test]
		public void LocalDangerBypassesHoldWithoutRenewingIt()
		{
			var policy = new TurtleFallbackPolicy();
			policy.Select(100, true, Fallback, "rush", true, false, 1000);
			var danger = policy.Select(200, true, Fallback, "rush", true, true, 1000);
			Assert.That(danger.Personality, Is.EqualTo("turtle"));
			Assert.That(danger.Reason, Is.EqualTo("BaseDanger"));
			Assert.That(policy.Select(300, true, Fallback, "rush", true, false, 1000).ExpiresTick, Is.EqualTo(1100));
		}

		[Test]
		public void IneligibleIncumbentAndUnprovenSavedStateCannotCreateAHold()
		{
			var policy = new TurtleFallbackPolicy();
			Assert.That(policy.Select(100, true, Fallback, "tech", false, false, 1000).Reason, Is.EqualTo("Ineligible"));
			Assert.That(policy.Select(200, true, Fallback, "rush", true, false, 0).Reason, Is.EqualTo("Unsupported"));
			policy.Invalidate();
			Assert.That(policy.Select(300, true, Fallback, "rush", true, false, 1000).Personality, Is.EqualTo("turtle"));
			Assert.That(policy.Select(400, true, Fallback, "rush", true, false, 1000).Reason, Is.EqualTo("Unsupported"));
		}

		[Test]
		public void GenuineCandidatesAndLegacyEmergencyKeepTheirPrecedence()
		{
			var info = new MasterAiBotModuleInfo();
			var enemy = new EnemyProfile { Alive = true, NearestCells = 1, ArmyValue = 100000 };
			var available = new[] { "turtle", "rush", "steamroller", "guerrilla", "tech", "expansion" };
			var terminal = MasterAiBotModule.CandidatePersonalityChoice(BotUrgency.Pressured, enemy, 1000,
				new[] { enemy }, "rush", available, info);
			Assert.That(terminal, Is.EqualTo(Fallback));
			Assert.That(MasterAiBotModule.CandidatePersonality(BotUrgency.Pressured, enemy, 1000,
				new[] { enemy }, "rush", available, info), Is.EqualTo(terminal.Personality));
			var emergency = MasterAiBotModule.CandidatePersonalityChoice(BotUrgency.Emergency, enemy, 1000,
				new[] { enemy }, "rush", available, info);
			Assert.That(emergency.Reason, Is.EqualTo(PersonalityCandidateReason.LegacyEmergency));
			var policy = new TurtleFallbackPolicy();
			Assert.That(policy.Select(100, true, emergency, "rush", true, false, 1000).Personality, Is.EqualTo("turtle"));
			enemy.DefenceCount = info.FortifiedDefenceCount;
			var siege = MasterAiBotModule.CandidatePersonalityChoice(BotUrgency.Pressured, enemy,
				info.SteamrollerMinArmyValue, new[] { enemy }, "rush", available, info);
			Assert.That(siege.Personality, Is.EqualTo("steamroller"));
			Assert.That(policy.Select(200, true, siege, "rush", true, false, 1000).Personality, Is.EqualTo("steamroller"));
		}

		[Test]
		public void PlayersHaveIndependentEpisodesAndOnlyAGenuineOtherReasonRearms()
		{
			var first = new TurtleFallbackPolicy();
			var second = new TurtleFallbackPolicy();
			first.Select(100, true, Fallback, "rush", true, false, 1000);
			second.Select(200, true, Fallback, "rush", true, false, 1000);
			Assert.That(first.Select(1100, true, Fallback, "rush", true, false, 1000).Reason, Is.EqualTo("Expired"));
			Assert.That(second.Select(1100, true, Fallback, "rush", true, false, 1000).Reason, Is.EqualTo("Held"));
			first.Select(1101, true, new("rush", PersonalityCandidateReason.Candidate), "rush", true, false, 1000);
			Assert.That(first.Select(1102, true, Fallback, "rush", true, false, 1000).ExpiresTick, Is.EqualTo(2102));
		}
	}
}
