using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ObservedStyleQualificationTest
	{
		[Test]
		public void RepeatedGenerationCannotQualifyAndTargetSwitchResetsIt()
		{
			var gate = new ObservedStyleQualification();
			gate.Observe(1, 0, 2, 700, [800, 200]);
			gate.Observe(1, 75, 2, 700, [800, 200]);
			gate.Observe(2, 150, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(150), Is.EqualTo(-1));
			gate.Observe(3, 225, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(225), Is.Zero);
			Assert.That(gate.QualifiedClass(825), Is.EqualTo(-1));
			gate.Observe(3, 825, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(825), Is.EqualTo(-1));
			gate.Reset();
			Assert.That(gate.QualifiedClass(226), Is.EqualTo(-1));
		}

		[Test]
		public void ChallengerNeedsTwoFreshConfirmations()
		{
			var gate = new ObservedStyleQualification();
			gate.Observe(1, 0, 2, 700, [800, 200]);
			gate.Observe(2, 75, 2, 700, [800, 200]);
			gate.Observe(3, 150, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(150), Is.Zero);
			gate.Observe(4, 225, 2, 700, [200, 800]);
			Assert.That(gate.QualifiedClass(225), Is.EqualTo(-1));
			gate.Observe(5, 300, 2, 700, [200, 800]);
			Assert.That(gate.QualifiedClass(300), Is.EqualTo(1));
		}
	}
}
