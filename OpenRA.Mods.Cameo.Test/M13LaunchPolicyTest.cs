using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class M13LaunchPolicyTest
	{
		[TestCase(36, 1000, 1)]
		[TestCase(36, 1000, 250)]
		[TestCase(36, 1000, 1000)]
		[TestCase(36, 1000, 12000)]
		[TestCase(0, 1000, 12000)]
		[TestCase(int.MaxValue, 1000, 1000)]
		public void SwitchOffRetainsExactLegacyScaledValve(int configured, int basis, int target)
		{
			var old = System.Math.Max(1, (int)((long)configured * target / basis));
			Assert.That(M13LaunchPolicyCA.MaxIdleUnits(false, configured, basis, target), Is.EqualTo(old));
		}

		[TestCase(1)]
		[TestCase(250)]
		[TestCase(1000)]
		[TestCase(12000)]
		public void ArmedValveCannotGrowOrShrinkWithEnemyScale(int target)
		{
			var limit = M13LaunchPolicyCA.MaxIdleUnits(true, 36, 1000, target);
			Assert.That(limit, Is.EqualTo(36));
			Assert.That(AttackLivenessEvalCA.AbsoluteOverflow(35, limit), Is.False);
			Assert.That(AttackLivenessEvalCA.AbsoluteOverflow(36, limit), Is.True);
		}
	}
}
