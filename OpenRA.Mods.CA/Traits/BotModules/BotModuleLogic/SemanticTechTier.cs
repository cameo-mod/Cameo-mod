using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public static partial class AblationIds
	{
		public const string SemanticTechTiers = "bb_semantic_tech_tiers";
	}

	public readonly record struct SemanticTechRole(int Tier, bool Superweapon)
	{
		// Strategic-only structures are ranked after the tiered progression.
		public int FirstCopyOrder => Tier == 0 && Superweapon ? 4 : Tier;
	}

	public static class SemanticTechTier
	{
		public static SemanticTechRole Classify(BaseBuilderBotModuleCAInfo info, ActorInfo actor)
		{
			if (!actor.HasTraitInfo<BuildingInfo>())
				return default;

			var name = actor.Name;
			var strategic = info.SemanticSuperweaponTypes.Contains(name);
			if (info.SemanticTechCentreTypes.Contains(name))
				return new SemanticTechRole(3, strategic);

			if (info.SemanticRadarTypes.Contains(name))
				return new SemanticTechRole(2, strategic);

			return new SemanticTechRole(strategic ? 0 : 1, strategic);
		}
	}
}
