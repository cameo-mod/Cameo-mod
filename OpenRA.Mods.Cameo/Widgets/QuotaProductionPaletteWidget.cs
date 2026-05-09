#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets
{
	public class QuotaProductionPaletteWidget : ProductionPaletteCAWidget
	{
		SpriteFont quotaFont;

		[ObjectCreator.UseCtor]
		public QuotaProductionPaletteWidget(ModData modData, OrderManager orderManager, World world, WorldRenderer worldRenderer)
			: base(modData, orderManager, world, worldRenderer) { }

		public override void Initialize(WidgetArgs args)
		{
			base.Initialize(args);
			quotaFont = Game.Renderer.Fonts[OverlayFont];
		}

		public override void Draw()
		{
			base.Draw();

			var quotaManager = World.WorldActor.TraitOrDefault<QuotaProductionManager>();
			if (quotaManager == null || !quotaManager.Enabled || CurrentQueue == null)
				return;

			foreach (var icon in icons.Values)
			{
				var quota = quotaManager.GetQuota(CurrentQueue.Actor.ActorID, icon.Name);
				if (quota <= 0) continue;

				var alive = quotaManager.GetAliveCount(CurrentQueue.Actor.ActorID, icon.Name);
				var text = $"{alive}/{quota}";
				var textSize = quotaFont.Measure(text);
				var pos = icon.Pos + new float2(IconSize.X - textSize.X - 1, IconSize.Y - textSize.Y - 1);
				quotaFont.DrawTextWithContrast(text, pos, Color.Cyan, Color.Black, 1);
			}
		}
	}
}
