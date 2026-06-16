#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Widgets;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
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

		public override bool HandleMouseInput(MouseInput mi)
		{
			var quotaManager = World.LocalPlayer?.PlayerActor?.TraitOrDefault<QuotaProductionManager>();
			if (quotaManager == null || !quotaManager.Enabled || CurrentQueue == null)
				return base.HandleMouseInput(mi);

			if (mi.Event == MouseInputEvent.Move || mi.Event == MouseInputEvent.Scroll)
				return base.HandleMouseInput(mi);

			var icon = icons.Where(i => i.Key.Contains(mi.Location))
				.Select(i => i.Value).FirstOrDefault();

			if (icon == null)
				return false;

			if (mi.Event != MouseInputEvent.Down)
				return true;

			var buildable = CurrentQueue.BuildableItems().FirstOrDefault<ActorInfo>(a => a.Name == icon.Name);
			if (buildable == null)
			{
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickDisabledSound, null);
				return true;
			}

			if (!buildable.HasTraitInfo<MobileInfo>() && !buildable.HasTraitInfo<AircraftInfo>())
				return base.HandleMouseInput(mi);

			var count = mi.Modifiers.HasModifier(Modifiers.Shift) ? 5 : 1;

			if (mi.Button == MouseButton.Left)
			{
				quotaManager.AdjustQuota(icon.Name, count);
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				return true;
			}

			if (mi.Button == MouseButton.Right)
			{
				quotaManager.AdjustQuota(icon.Name, -count);
				Game.Sound.PlayNotification(World.Map.Rules, World.LocalPlayer, "Sounds", ClickSound, null);
				return true;
			}

			return base.HandleMouseInput(mi);
		}

		// Decoupled rendering: GetQuota/GetAliveCount read dictionaries the sim thread mutates
		// (ResolveOrder/Tick). Refresh this overlay snapshot under the world read lock; when the sim is
		// mid-tick, draw last frame's overlays instead of reading live state.
		readonly List<(float2 Pos, string Text)> cachedQuotaOverlays = [];

		public override void Draw()
		{
			base.Draw();

			if (Game.TryEnterWorldReadLock())
			{
				try
				{
					cachedQuotaOverlays.Clear();

					var quotaManager = World.LocalPlayer?.PlayerActor?.TraitOrDefault<QuotaProductionManager>();
					if (quotaManager != null && quotaManager.Enabled && CurrentQueue != null)
					{
						foreach (var icon in icons.Values)
						{
							var quota = quotaManager.GetQuota(icon.Name);
							if (quota <= 0) continue;

							var alive = quotaManager.GetAliveCount(icon.Name);
							var text = $"{alive}/{quota}";
							var textSize = quotaFont.Measure(text);
							cachedQuotaOverlays.Add((icon.Pos + new float2(IconSize.X - textSize.X - 1, IconSize.Y - textSize.Y - 1), text));
						}
					}
				}
				finally
				{
					Game.ExitWorldReadLock();
				}
			}

			foreach (var (pos, text) in cachedQuotaOverlays)
				quotaFont.DrawTextWithContrast(text, pos, Color.Cyan, Color.Black, 1);
		}
	}
}
