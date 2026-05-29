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
using OpenRA;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ModContentPromptLogic : ChromeLogic
	{
		[FluentReference]
		const string Continue = "button-continue";

		[ObjectCreator.UseCtor]
		public ModContentPromptLogic(ModData modData, Widget widget, ModContent content, Action continueLoading)
		{
			var continueMessage = FluentProvider.GetMessage(Continue);

			var panel = widget.Get("CONTENT_PROMPT_PANEL");
			var headerLabel = panel.Get<LabelWidget>("HEADER_LABEL");
			headerLabel.IncreaseHeightToFitCurrentText();
			var headerHeight = headerLabel.Bounds.Height;

			panel.Bounds.Height += headerHeight;
			panel.Bounds.Y -= headerHeight / 2;

			var advancedButton = panel.Get<ButtonWidget>("ADVANCED_BUTTON");
			advancedButton.Bounds.Y += headerHeight;
			advancedButton.OnClick = () =>
			{
				ModContentInstallerLogic.SetPendingCancelAction(continueLoading);

				Ui.OpenWindow("CONTENT_PANEL", new WidgetArgs
				{
					{ "content", content },
				});
			};

			var quickButton = panel.Get<ButtonWidget>("QUICK_BUTTON");
			quickButton.IsVisible = () => !string.IsNullOrEmpty(content.QuickDownload);
			quickButton.Bounds.Y += headerHeight;
			quickButton.OnClick = () =>
			{
				var downloadYaml = MiniYaml.Load(modData.DefaultFileSystem, content.Downloads, null);
				var download = downloadYaml.FirstOrDefault(n => n.Key == content.QuickDownload);
				if (download == null)
					throw new InvalidOperationException($"Mod QuickDownload `{content.QuickDownload}` definition not found.");

				Ui.OpenWindow("PACKAGE_DOWNLOAD_PANEL", new WidgetArgs
				{
					{ "download", new ModContent.ModDownload(download.Value) },
					{ "onSuccess", continueLoading }
				});
			};

			var continueButton = panel.Get<ButtonWidget>("QUIT_BUTTON");
			continueButton.Bounds.Y += headerHeight;
			continueButton.OnClick = () =>
			{
				Game.SkipContentPrompt = true;
				Ui.CloseWindow();
				continueLoading();
			};
			continueButton.Key = modData.Hotkeys["escape"];
			continueButton.GetText = () => continueMessage;

			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}
}
