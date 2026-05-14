#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class CameoGameplaySettingsLogic : ChromeLogic
	{
		readonly WorldRenderer worldRenderer;
		[FluentReference]
		const string AutoSaveIntervalOptions = "auto-save-interval.options";

		[FluentReference]
		const string AutoSaveIntervalDisabled = "auto-save-interval.disabled";

		[FluentReference]
		const string AutoSaveIntervalMinuteOptions = "auto-save-interval.minute-options";

		[FluentReference]
		const string AutoSaveMaxFileNumber = "auto-save-max-file-number";

		readonly int[] autoSaveSeconds = [0, 10, 30, 45, 60, 120, 180, 300, 600];
		readonly int[] autoSaveFileNumbers = [3, 5, 10, 20, 50, 100];

		[ObjectCreator.UseCtor]
		public CameoGameplaySettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");
			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			var autoSaveIntervalDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_INTERVAL_DROP_DOWN");
			autoSaveIntervalDropDown.OnClick = () => ShowAutoSaveIntervalDropdown(autoSaveIntervalDropDown, autoSaveSeconds);
			autoSaveIntervalDropDown.GetText = () => GetMessageForAutoSaveInterval(Game.Settings.SinglePlayerSettings.AutoSaveInterval);

			var autoSaveNoDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_FILE_NUMBER_DROP_DOWN");
			autoSaveNoDropDown.OnMouseDown = _ => ShowAutoSaveFileNumberDropdown(autoSaveNoDropDown, autoSaveFileNumbers);
			autoSaveNoDropDown.GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", Game.Settings.SinglePlayerSettings.AutoSaveMaxFileCount);
			autoSaveNoDropDown.IsDisabled = () => Game.Settings.SinglePlayerSettings.AutoSaveInterval <= 0;

			var isMultiplayer = worldRenderer?.World != null &&
				worldRenderer.World.Players.Count(p => !p.IsBot && p.Playable) > 1;

			var quotaCheckbox = panel.Get<CheckboxWidget>("QUOTA_MODE_CHECKBOX");
			quotaCheckbox.IsChecked = () => !isMultiplayer && Game.Settings.SinglePlayerSettings.QuotaModeEnabled;
			quotaCheckbox.IsDisabled = () => isMultiplayer;
			quotaCheckbox.OnClick = () =>
			{
				if (isMultiplayer) return;
				Game.Settings.SinglePlayerSettings.QuotaModeEnabled ^= true;
				Game.Settings.Save();

				var quotaManager = worldRenderer?.World?.WorldActor?.TraitOrDefault<QuotaProductionManager>();
				if (quotaManager != null)
					quotaManager.Enabled = Game.Settings.SinglePlayerSettings.QuotaModeEnabled;
			};

			return () => false;
		}

		Action ResetPanel(Widget panel)
		{
			return () => { };
		}

		void ShowAutoSaveIntervalDropdown(DropDownButtonWidget dropdown, int[] options)
		{
			var gsp = Game.Settings.SinglePlayerSettings;

			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gsp.AutoSaveInterval == o,
					() => { gsp.AutoSaveInterval = o; Game.Settings.Save(); });

				item.Get<LabelWidget>("LABEL").GetText = () => GetMessageForAutoSaveInterval(o);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		void ShowAutoSaveFileNumberDropdown(DropDownButtonWidget dropdown, int[] options)
		{
			var gsp = Game.Settings.SinglePlayerSettings;

			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gsp.AutoSaveMaxFileCount == o,
					() => { gsp.AutoSaveMaxFileCount = o; Game.Settings.Save(); });

				item.Get<LabelWidget>("LABEL").GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", o);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		static string GetMessageForAutoSaveInterval(int value) =>
			value switch
			{
				0 => FluentProvider.GetMessage(AutoSaveIntervalDisabled),
				< 60 => FluentProvider.GetMessage(AutoSaveIntervalOptions, "seconds", value),
				_ => FluentProvider.GetMessage(AutoSaveIntervalMinuteOptions, "minutes", value / 60)
			};
	}
}
