#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class CameoGameplaySettingsLogic : ChromeLogic
	{
		readonly WorldRenderer worldRenderer;
		readonly AutoSaveSettings autoSaveSettings;
		readonly CameoSettings cameoSettings;

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
			autoSaveSettings = Game.ModData.GetSettings<AutoSaveSettings>();
			cameoSettings = Game.ModData.GetSettings<CameoSettings>();
			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");
			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			var autoSaveIntervalDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_INTERVAL_DROP_DOWN");
			autoSaveIntervalDropDown.OnClick = () => ShowAutoSaveIntervalDropdown(autoSaveIntervalDropDown, autoSaveSeconds);
			autoSaveIntervalDropDown.GetText = () => GetMessageForAutoSaveInterval(autoSaveSettings.AutoSaveInterval);

			var autoSaveNoDropDown = panel.Get<DropDownButtonWidget>("AUTO_SAVE_FILE_NUMBER_DROP_DOWN");
			autoSaveNoDropDown.OnMouseDown = _ => ShowAutoSaveFileNumberDropdown(autoSaveNoDropDown, autoSaveFileNumbers);
			autoSaveNoDropDown.GetText = () => FluentProvider.GetMessage(AutoSaveMaxFileNumber, "saves", autoSaveSettings.AutoSaveMaxFileCount);
			autoSaveNoDropDown.IsDisabled = () => autoSaveSettings.AutoSaveInterval <= 0;

			var quotaCheckbox = panel.Get<CheckboxWidget>("QUOTA_MODE_CHECKBOX");
			quotaCheckbox.IsChecked = () => cameoSettings.QuotaModeEnabled;
			quotaCheckbox.OnClick = () =>
			{
				cameoSettings.QuotaModeEnabled ^= true;
				cameoSettings.Save();

				var quotaManager = worldRenderer?.World?.LocalPlayer?.PlayerActor?.TraitOrDefault<QuotaProductionManager>();
				quotaManager?.SetEnabled(cameoSettings.QuotaModeEnabled);
			};

			var buildingCountsCheckbox = panel.Get<CheckboxWidget>("BUILDING_COUNTS_CHECKBOX");
			buildingCountsCheckbox.IsChecked = () => cameoSettings.ShowBuildingCounts;
			buildingCountsCheckbox.OnClick = () =>
			{
				cameoSettings.ShowBuildingCounts ^= true;
				cameoSettings.Save();
			};

			return () => false;
		}

		Action ResetPanel(Widget panel)
		{
			return () => { };
		}

		void ShowAutoSaveIntervalDropdown(DropDownButtonWidget dropdown, int[] options)
		{
			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => autoSaveSettings.AutoSaveInterval == o,
					() => { autoSaveSettings.AutoSaveInterval = o; autoSaveSettings.Save(); });

				item.Get<LabelWidget>("LABEL").GetText = () => GetMessageForAutoSaveInterval(o);
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options, SetupItem);
		}

		void ShowAutoSaveFileNumberDropdown(DropDownButtonWidget dropdown, int[] options)
		{
			ScrollItemWidget SetupItem(int o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => autoSaveSettings.AutoSaveMaxFileCount == o,
					() => { autoSaveSettings.AutoSaveMaxFileCount = o; autoSaveSettings.Save(); });

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
