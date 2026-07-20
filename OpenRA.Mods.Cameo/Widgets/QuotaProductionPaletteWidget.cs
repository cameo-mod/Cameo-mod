#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

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
		public readonly string MainStructureProductionGroup = "Building";
		public readonly Color StructureCountColor = Color.Lime;
		public readonly int2 StructureCountMargin = new(5, 4);

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

		public override void Draw()
		{
			base.Draw();

			var localPlayer = World.LocalPlayer;
			if (localPlayer == null || CurrentQueue == null)
				return;

			if (CurrentQueue.Info.Group == MainStructureProductionGroup)
			{
				var structureCounts = World.ActorsHavingTrait<Building>()
					.Where(a => a.Owner == localPlayer && !a.IsDead)
					.GroupBy(a => a.Info.Name)
					.ToDictionary(g => g.Key, g => g.Count());

				foreach (var icon in icons.Values)
				{
					if (!structureCounts.TryGetValue(icon.Name, out var count))
						continue;

					var text = count.ToStringInvariant();
					var textSize = quotaFont.Measure(text);
					var pos = icon.Pos + new float2(
						IconSize.X - textSize.X - StructureCountMargin.X,
						IconSize.Y - textSize.Y - StructureCountMargin.Y);
					quotaFont.DrawTextWithContrast(text, pos, StructureCountColor, Color.Black, 1);
				}
			}

			var quotaManager = localPlayer.PlayerActor.TraitOrDefault<QuotaProductionManager>();
			if (quotaManager == null || !quotaManager.Enabled)
				return;

			foreach (var icon in icons.Values)
			{
				var quota = quotaManager.GetQuota(icon.Name);
				if (quota <= 0)
					continue;

				var alive = quotaManager.GetAliveCount(icon.Name);
				var text = $"{alive}/{quota}";
				var textSize = quotaFont.Measure(text);
				var pos = icon.Pos + new float2(IconSize.X - textSize.X - 1, IconSize.Y - textSize.Y - 1);
				quotaFont.DrawTextWithContrast(text, pos, Color.Cyan, Color.Black, 1);
			}
		}
	}
}
