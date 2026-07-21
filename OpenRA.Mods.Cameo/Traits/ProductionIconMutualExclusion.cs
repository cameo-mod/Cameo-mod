#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using OpenRA;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Groups production icons that should be highlighted together when one mutually exclusive item is hovered.")]
	public class ProductionIconMutualExclusionInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly string Group = null;

		public readonly Color OutlineColor = Color.OrangeRed;

		public override object Create(ActorInitializer init) { return new ProductionIconMutualExclusion(); }
	}

	public class ProductionIconMutualExclusion { }
}
