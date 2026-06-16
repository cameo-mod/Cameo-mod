#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	// Must run AFTER ProductionTabsLogicCA (which also binds the production-type
	// buttons), so it is attached last on the PRODUCTION_TABS widget. It repurposes
	// the "Promotion" production-type button to open the promotion tree window
	// instead of selecting a palette tab.
	public class PromotionTreeButtonLogic : ChromeLogic
	{
		const string PromotionGroupName = "Promotion";
		const string CommanderTreeOverlayId = "COMMANDER_TREE_OVERLAY";

		readonly World world;

		[ObjectCreator.UseCtor]
		public PromotionTreeButtonLogic(Widget widget, World world)
		{
			this.world = world;

			// PRODUCTION_TYPES is not a child of this widget, so resolve it from the root.
			var typesContainer = Ui.Root.GetOrNull("PRODUCTION_TYPES");
			if (typesContainer == null)
				return;

			foreach (var child in typesContainer.Children)
			{
				if (child is ProductionTypeButtonWidget button &&
					string.Equals(button.ProductionGroup, PromotionGroupName, StringComparison.OrdinalIgnoreCase))
				{
					SetupPromotionButton(button);
					break;
				}
			}
		}

		void SetupPromotionButton(ProductionTypeButtonWidget button)
		{
			var queues = world?.LocalPlayer?.PlayerActor?
				.TraitsImplementing<ProductionQueue>()
				.Where(q => (q.Info.Group ?? q.Info.Type) == button.ProductionGroup)
				.ToArray() ?? Array.Empty<ProductionQueue>();

			button.IsDisabled = () => !queues.Any(q => q.Enabled);

			button.OnMouseDown = mi =>
			{
				if (mi.Button == MouseButton.Left && !button.IsDisabled())
					OpenCommanderTree();
			};
			button.OnMouseUp = _ => { };
			button.OnClick = () => { };
			button.OnKeyPress = e =>
			{
				if (e.Event != KeyInputEvent.Down || button.IsDisabled())
					return;

				OpenCommanderTree();
			};
			button.IsHighlighted = () =>
			{
				var current = Ui.CurrentWindow();
				return current != null && current.Id == CommanderTreeOverlayId;
			};

			var chromeName = button.ProductionGroup.ToLowerInvariant();
			var icon = button.GetOrNull<ImageWidget>("ICON");
			if (icon != null)
			{
				// Stage B (DecoupledRendering): AllQueued() is the live, sim-mutated queue list. Re-evaluate the
				// alert state only when the sim thread is not mid-tick; otherwise reuse the last value (this lambda
				// runs every frame, so it is at worst one frame stale).
				var cachedAlert = false;
				icon.GetImageName = () =>
				{
					if (button.IsDisabled())
						return chromeName + "-disabled";

					if (Game.TryEnterWorldReadLock())
					{
						try
						{
							cachedAlert = queues.Any(q => q.AllQueued().Any(i => i.Done));
						}
						finally
						{
							Game.ExitWorldReadLock();
						}
					}

					return cachedAlert ? chromeName + "-alert" : chromeName;
				};
			}
		}

		void OpenCommanderTree()
		{
			var current = Ui.CurrentWindow();
			if (current != null && current.Id == CommanderTreeOverlayId)
			{
				Ui.CloseWindow();
				return;
			}

			Game.OpenWindow(world, CommanderTreeOverlayId);
			Ui.ResetTooltips();
		}
	}
}
