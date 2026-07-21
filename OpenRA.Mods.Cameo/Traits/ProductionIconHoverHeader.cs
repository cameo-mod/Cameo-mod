#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using OpenRA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Displays a short header over this actor's production icon using its live pointer position.")]
	public class ProductionIconHoverHeaderInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly string Text = null;

		public override object Create(ActorInitializer init) { return new ProductionIconHoverHeader(); }
	}

	public class ProductionIconHoverHeader { }
}
