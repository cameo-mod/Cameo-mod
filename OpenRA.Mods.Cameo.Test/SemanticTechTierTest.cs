using System.Collections.Frozen;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class SemanticTechTierTest
	{
		[Test]
		public void MilestonesAndStrategicFlagAreIndependent()
		{
			var info = new BaseBuilderBotModuleCAInfo();
			typeof(BaseBuilderBotModuleCAInfo).GetField(nameof(info.SemanticRadarTypes)).SetValue(info, new[] { "outpost" }.ToFrozenSet());
			typeof(BaseBuilderBotModuleCAInfo).GetField(nameof(info.SemanticTechCentreTypes)).SetValue(info, new[] { "lab" }.ToFrozenSet());
			typeof(BaseBuilderBotModuleCAInfo).GetField(nameof(info.SemanticSuperweaponTypes)).SetValue(info, new[] { "lab", "silo" }.ToFrozenSet());

			Assert.That(info.SemanticTechTiers, Is.False);
			Assert.That(SemanticTechTier.Classify(info, new ActorInfo("yard", new BuildingInfo())), Is.EqualTo(new SemanticTechRole(1, false)));
			Assert.That(SemanticTechTier.Classify(info, new ActorInfo("outpost", new BuildingInfo())), Is.EqualTo(new SemanticTechRole(2, false)));
			Assert.That(SemanticTechTier.Classify(info, new ActorInfo("lab", new BuildingInfo())), Is.EqualTo(new SemanticTechRole(3, true)));
			Assert.That(SemanticTechTier.Classify(info, new ActorInfo("silo", new BuildingInfo())), Is.EqualTo(new SemanticTechRole(0, true)));
			Assert.That(SemanticTechTier.Classify(info, new ActorInfo("lab")), Is.EqualTo(default(SemanticTechRole)));
		}
	}
}
