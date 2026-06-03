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
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Widgets;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class CommanderTreeWindowLogic : ChromeLogic
	{
		const int HeaderHeight = 148;
		const int FooterHeight = 24;
		const int SidePadding = 16;
		const int MinWindowWidth = 260;
		const int MinWindowHeight = 350;

		readonly World world;
		readonly Widget overlay;
		readonly Widget windowWidget;
		readonly LabelWidget pointsValue;
		readonly LabelWidget rankValue;
		readonly LabelWidget progressValue;
		readonly ButtonWidget closeButton;
		readonly ColorBlockWidget dismissArea;
		readonly LogicKeyListenerWidget hotkeys;
		readonly CommanderTreeWidget commanderTree;
		readonly Widget hscrollContainer;
		readonly DragHandleWidget hscrollThumb;
		readonly DragHandleWidget dragHandle;
		readonly ScrollPanelWidget treeScrollPanel;
		readonly Widget windowBgWidget;
		readonly Widget headerStatsWidget;
		readonly Widget treeTooltipWidget;
		readonly Widget insigniaFrameWidget;

		bool isDragging;
		int2 dragStartMouse;
		int2 dragStartWindowPos;
		bool hasBeenDragged;
		int lastContentWidth;
		int lastContentHeight;

		bool isDraggingHScroll;
		int hscrollDragStartX;
		float hscrollDragStartFraction;

		[ObjectCreator.UseCtor]
		public CommanderTreeWindowLogic(Widget widget, World world)
		{
			this.world = world;
			overlay = widget;

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
			hscrollContainer = widget.GetOrNull<Widget>("TREE_HSCROLL");
			hscrollThumb = widget.GetOrNull<DragHandleWidget>("HSCROLL_THUMB");
			dragHandle = widget.GetOrNull<DragHandleWidget>("DRAG_HANDLE");
			treeScrollPanel = widget.GetOrNull<ScrollPanelWidget>("TREE_SCROLL");
			windowWidget = widget.GetOrNull<Widget>("COMMANDER_TREE_WINDOW");
			windowBgWidget = widget.GetOrNull<Widget>("WINDOW_BG");
			headerStatsWidget = widget.GetOrNull<Widget>("HEADER_STATS");
			treeTooltipWidget = widget.GetOrNull<Widget>("COMMANDER_TREE_TOOLTIP");
			insigniaFrameWidget = widget.GetOrNull<Widget>("FACTION_INSIGNIA_FRAME");

			if (insigniaFrameWidget != null)
				insigniaFrameWidget.Visible = HasFactionInsignia();

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
				dismissArea.OnMouseDown = _ => { };
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

			if (hscrollThumb != null && hscrollContainer != null)
			{
				hscrollThumb.OnMouseDown = mi =>
				{
					isDraggingHScroll = true;
					hscrollDragStartX = mi.Location.X;
					hscrollDragStartFraction = commanderTree?.HorizontalScrollFraction ?? 0f;
				};

				hscrollThumb.OnMouseMove = mi =>
				{
					if (!isDraggingHScroll || commanderTree == null)
						return;
					var thumbW = GetHScrollThumbWidth();
					var trackAvail = hscrollContainer.Bounds.Width - thumbW;
					if (trackAvail <= 0)
						return;
					var delta = mi.Location.X - hscrollDragStartX;
					commanderTree.SetHorizontalScrollFraction(Math.Clamp(hscrollDragStartFraction + delta / (float)trackAvail, 0f, 1f));
				};

				hscrollThumb.OnMouseUp = _ => isDraggingHScroll = false;
			}

			if (dragHandle != null && windowWidget != null)
			{
				dragHandle.OnMouseDown = mi =>
				{
					isDragging = true;
					dragStartMouse = mi.Location;
					dragStartWindowPos = new int2(windowWidget.Bounds.X, windowWidget.Bounds.Y);
				};

				dragHandle.OnMouseMove = mi =>
				{
					if (!isDragging)
						return;

					hasBeenDragged = true;
					var delta = mi.Location - dragStartMouse;
					var screenW = overlay.Bounds.Width;
					var screenH = overlay.Bounds.Height;
					var newX = Math.Clamp(dragStartWindowPos.X + delta.X, 0, Math.Max(0, screenW - windowWidget.Bounds.Width));
					var newY = Math.Clamp(dragStartWindowPos.Y + delta.Y, 0, Math.Max(0, screenH - windowWidget.Bounds.Height));
					var b = windowWidget.Bounds;
					b.X = newX;
					b.Y = newY;
					windowWidget.Bounds = b;
				};

				dragHandle.OnMouseUp = _ => isDragging = false;
			}

		}

		bool HasFactionInsignia()
		{
			var player = world.LocalPlayer;
			if (player == null || player.Spectating)
				return false;

			// Mirror AddFactionSuffixLogic: the frame's child ImageWidget resolves its
			// collection to "sidebar-<faction>" (honoring the FactionSuffix-* metric remap).
			var faction = player.Faction.InternalName;
			if (ChromeMetrics.TryGet("FactionSuffix-" + faction, out string mapped))
				faction = mapped;

			// Only show the frame when that collection actually defines an `insignia` region.
			// ImageWidget.Draw uses the throwing ChromeProvider.GetImage, so a missing region
			// would otherwise crash the Promotions window on open.
			return ChromeProvider.TryGetImage("sidebar-" + faction, "insignia") != null;
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

			if (commanderTree != null && windowWidget != null)
			{
				var cw = commanderTree.ContentWidth;
				var ch = commanderTree.ContentHeight;
				if (cw > 0 && ch > 0 && (cw != lastContentWidth || ch != lastContentHeight))
				{
					lastContentWidth = cw;
					lastContentHeight = ch;
					AutoSizeWindow(cw, ch);
				}
			}

			if (commanderTree == null || hscrollContainer == null)
				return;

			var needsScroll = commanderTree.HasHorizontalOverflow;
			hscrollContainer.Visible = needsScroll;

			if (!needsScroll && commanderTree.HorizontalScrollFraction > 0f)
				commanderTree.SetHorizontalScrollFraction(0f);
			else if (needsScroll)
				UpdateHScrollThumb();
		}

		void AutoSizeWindow(int treeContentWidth, int treeContentHeight)
		{
			if (windowWidget == null)
				return;

			var screenW = overlay.Bounds.Width;
			var screenH = overlay.Bounds.Height;
			var maxW = screenW * 9 / 10;
			var maxH = screenH * 9 / 10;

			var targetW = Math.Clamp(treeContentWidth + SidePadding, MinWindowWidth, maxW);
			var targetH = Math.Clamp(treeContentHeight + HeaderHeight + FooterHeight, MinWindowHeight, maxH);

			var current = windowWidget.Bounds;
			if (current.Width == targetW && current.Height == targetH)
				return;

			int newX, newY;
			if (hasBeenDragged)
			{
				newX = Math.Clamp(current.X, 0, Math.Max(0, screenW - targetW));
				newY = Math.Clamp(current.Y, 0, Math.Max(0, screenH - targetH));
			}
			else
			{
				newX = Math.Clamp(screenW * 82 / 100 - targetW, 0, Math.Max(0, screenW - targetW));
				newY = Math.Clamp(screenH / 5, 0, Math.Max(0, screenH - targetH));
			}

			var b = windowWidget.Bounds;
			b.X = newX;
			b.Y = newY;
			b.Width = targetW;
			b.Height = targetH;
			windowWidget.Bounds = b;

			ResizeChildren(targetW, targetH);
		}

		void ResizeChildren(int w, int h)
		{
			SetSize(windowBgWidget, w, h);

			if (dragHandle != null)
			{
				var db = dragHandle.Bounds;
				db.Width = w;
				dragHandle.Bounds = db;
			}

			if (headerStatsWidget != null)
			{
				var hb = headerStatsWidget.Bounds;
				hb.Width = w - SidePadding;
				headerStatsWidget.Bounds = hb;
			}

			if (closeButton != null)
			{
				var cb = closeButton.Bounds;
				cb.X = w - cb.Width - 4;
				closeButton.Bounds = cb;
			}

			if (treeScrollPanel != null)
			{
				var sb = treeScrollPanel.Bounds;
				sb.Width = w - SidePadding;
				sb.Height = h - (HeaderHeight + FooterHeight);
				treeScrollPanel.Bounds = sb;
			}

			if (hscrollContainer != null)
			{
				var hb = hscrollContainer.Bounds;
				hb.Y = h - FooterHeight;
				hb.Width = w - SidePadding;
				hscrollContainer.Bounds = hb;
			}

			SetSize(treeTooltipWidget, w, h);
		}

		static void SetSize(Widget w, int width, int height)
		{
			if (w == null)
				return;
			var b = w.Bounds;
			b.Width = width;
			b.Height = height;
			w.Bounds = b;
		}

		int GetHScrollThumbWidth()
		{
			var contentW = commanderTree?.ContentWidth ?? 0;
			var barW = hscrollContainer?.Bounds.Width ?? 0;
			if (contentW <= 0 || barW <= 0)
				return 40;
			var viewportW = treeScrollPanel?.Bounds.Width ?? barW;
			var fraction = Math.Clamp((float)viewportW / contentW, 0.05f, 1f);
			return Math.Max(24, (int)(fraction * barW));
		}

		void UpdateHScrollThumb()
		{
			if (hscrollThumb == null || hscrollContainer == null)
				return;
			var barW = hscrollContainer.Bounds.Width;
			var thumbW = GetHScrollThumbWidth();
			var scrollFraction = commanderTree?.HorizontalScrollFraction ?? 0f;
			var thumbX = (int)(scrollFraction * Math.Max(0, barW - thumbW));
			var b = hscrollThumb.Bounds;
			b.X = thumbX;
			b.Width = thumbW;
			hscrollThumb.Bounds = b;
		}

		string GetLocalizedString(string key, string fallback)
		{
			return FluentProvider.TryGetMessage(key, out var message) ? message : fallback;
		}
	}
}
