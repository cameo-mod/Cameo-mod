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
using OpenRA.Mods.Cameo.Widgets;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class CommanderTreeWindowLogic : ChromeLogic
	{
		readonly World world;
		readonly LabelWidget pointsValue;
		readonly LabelWidget rankValue;
		readonly LabelWidget progressValue;
		readonly ButtonWidget closeButton;
		readonly ColorBlockWidget dismissArea;
		readonly LogicKeyListenerWidget hotkeys;
		readonly CommanderTreeWidget commanderTree;
		readonly SliderWidget horizontalScroll;

		[ObjectCreator.UseCtor]
		public CommanderTreeWindowLogic(Widget widget, World world)
		{
			this.world = world;

			var titleLabel = widget.GetOrNull<LabelWidget>("TITLE");
			var pointsLabel = widget.GetOrNull<LabelWidget>("POINTS_LABEL");
			var rankLabel = widget.GetOrNull<LabelWidget>("RANK_LABEL");
			var progressLabel = widget.GetOrNull<LabelWidget>("PROGRESS_LABEL");

			pointsValue = widget.GetOrNull<LabelWidget>("POINTS_VALUE");
			rankValue = widget.GetOrNull<LabelWidget>("RANK_VALUE");
			progressValue = widget.GetOrNull<LabelWidget>("PROGRESS_VALUE");
			closeButton = widget.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			dismissArea = widget.GetOrNull<ColorBlockWidget>("DISMISS_AREA");
			hotkeys = widget.GetOrNull<LogicKeyListenerWidget>("HOTKEYS");
			commanderTree = widget.GetOrNull<CommanderTreeWidget>("COMMANDER_TREE");
			horizontalScroll = widget.GetOrNull<SliderWidget>("TREE_HSCROLL");

			if (titleLabel != null)
				titleLabel.GetText = () => GetLocalizedString("commander-tree.title", "Promotions");

			if (rankLabel != null)
				rankLabel.GetText = () => GetLocalizedString("promotion-counter.rank", "Rank");

			if (pointsLabel != null)
				pointsLabel.GetText = () => GetLocalizedString("commander-tree.points-label", "Available Points");

			if (progressLabel != null)
				progressLabel.GetText = () => GetLocalizedString("promotion-counter.progress", "Progress");

			if (closeButton != null)
			{
				closeButton.OnClick = Close;
				closeButton.GetTooltipText = () => GetLocalizedString("commander-tree.close", "Close");
			}

			if (dismissArea != null)
			{
				dismissArea.OnMouseDown = mi =>
				{
					if (mi.Button != MouseButton.None)
						Close();
				};
				dismissArea.OnMouseUp = _ => { };
			}

			if (hotkeys != null)
				hotkeys.AddHandler(e =>
				{
					if (e.Event == KeyInputEvent.Down && !e.IsRepeat && e.Key == Keycode.ESCAPE)
						Close();

					return true;
				});

			if (pointsValue != null)
				pointsValue.GetText = () => GetCommanderPoints().ToStringInvariant();

			if (rankValue != null)
				rankValue.GetText = GetCommanderRank;

			if (progressValue != null)
				progressValue.GetText = GetCommanderProgress;

			if (horizontalScroll != null)
			{
				horizontalScroll.Visible = false;
				horizontalScroll.Disabled = true;
				horizontalScroll.GetValue = () => commanderTree?.HorizontalScrollFraction ?? 0f;
				horizontalScroll.OnChange += value =>
				{
					commanderTree?.SetHorizontalScrollFraction(value);
				};
			}
		}

		ISync GetPromotionsTrait()
		{
			var player = world.LocalPlayer;
			if (player == null)
				return null;

			return player.PlayerActor
				.TraitsImplementing<ISync>()
				.FirstOrDefault(t => string.Equals(t.GetType().Name, "PlayerPromotions", StringComparison.OrdinalIgnoreCase));
		}

		int GetCommanderPoints()
		{
			var promotionsTrait = GetPromotionsTrait();
			if (promotionsTrait != null)
			{
				var pointsField = promotionsTrait.GetType().GetField("Points");
				if (pointsField != null && pointsField.FieldType == typeof(int))
					return (int)pointsField.GetValue(promotionsTrait);
			}

			return 0;
		}

		string GetCommanderRank()
		{
			var promotionsTrait = GetPromotionsTrait();
			if (promotionsTrait != null)
			{
				var rankField = promotionsTrait.GetType().GetField("currentRank");
				if (rankField != null && rankField.FieldType == typeof(string))
				{
					var value = rankField.GetValue(promotionsTrait) as string;
					if (!string.IsNullOrEmpty(value))
					{
						if (FluentProvider.TryGetMessage(value, out var localized))
							return localized;

						return value;
					}
				}
			}

			return "-";
		}

		string GetCommanderProgress()
		{
			var promotionsTrait = GetPromotionsTrait();
			if (promotionsTrait == null)
				return "-";

			var traitType = promotionsTrait.GetType();
			var isMaxLevelField = traitType.GetField("IsMaxLevel");
			var nextLevelField = traitType.GetField("nextLevelXpRequired");
			var experienceProperty = traitType.GetProperty("Experience");

			var isMaxLevel = isMaxLevelField != null && isMaxLevelField.FieldType == typeof(bool) && (bool)isMaxLevelField.GetValue(promotionsTrait);
			if (isMaxLevel)
				return FluentProvider.GetMessage("promotion-counter.progress-max");

			var experience = 0;
			if (experienceProperty != null && experienceProperty.PropertyType == typeof(int))
				experience = (int)experienceProperty.GetValue(promotionsTrait);

			var nextRequirement = 0;
			if (nextLevelField != null && nextLevelField.FieldType == typeof(int))
				nextRequirement = (int)nextLevelField.GetValue(promotionsTrait);

			if (nextRequirement <= 0)
				return experience.ToStringInvariant();

			return experience.ToStringInvariant() + "/" + nextRequirement.ToStringInvariant();
		}

		void Close()
		{
			Ui.CloseWindow();
		}

		public override void Tick()
		{
			base.Tick();

			if (commanderTree == null || horizontalScroll == null)
				return;

			var needsScroll = commanderTree.HasHorizontalOverflow;
			horizontalScroll.Visible = needsScroll;
			horizontalScroll.Disabled = !needsScroll;

			if (!needsScroll && commanderTree.HorizontalScrollFraction > 0f)
				commanderTree.SetHorizontalScrollFraction(0f);
		}

		string GetLocalizedString(string key, string fallback)
		{
			return FluentProvider.TryGetMessage(key, out var message) ? message : fallback;
		}
	}
}
