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
using System.Globalization;
using System.Linq;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class StatisticsWindowLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public StatisticsWindowLogic(Widget widget, World world)
		{
			var summary = CameoStatistics.LoadSummary();
			var stats = summary.Factions;

			// Selectable, non-random factions, with their localized display names. These define
			// the per-faction rows (unplayed factions appear with zeroed totals so the player can
			// see which factions they have yet to play).
			var factions = world.WorldActor.Info.TraitInfos<FactionInfo>()
				.Where(f => f.Selectable && !IsRandom(f))
				.OrderBy(ResolveName, StringComparer.CurrentCultureIgnoreCase)
				.ToList();

			// Global totals, summed across every recorded faction.
			var gamesPlayed = stats.Values.Sum(s => s.GamesPlayed);
			var gamesWon = stats.Values.Sum(s => s.GamesWon);
			var gamesLost = stats.Values.Sum(s => s.GamesLost);
			var unitsKilled = stats.Values.Sum(s => (long)s.UnitsKilled);
			var unitsLost = stats.Values.Sum(s => (long)s.UnitsLost);
			var buildingsDestroyed = stats.Values.Sum(s => (long)s.BuildingsKilled);
			var buildingsLost = stats.Values.Sum(s => (long)s.BuildingsLost);
			var resourcesEarned = stats.Values.Sum(s => s.ResourcesEarned);
			var resourcesSpent = stats.Values.Sum(s => s.ResourcesSpent);
			var enemyAssetsDestroyed = stats.Values.Sum(s => s.EnemyAssetsDestroyed);
			var assetsOwned = stats.Values.Sum(s => s.AssetsOwned);
			var factionsPlayed = stats.Count(kv => kv.Value.GamesPlayed > 0);

			SetText(widget, "GAMES_PLAYED_VALUE", Number(gamesPlayed));
			SetText(widget, "GAMES_WON_VALUE", Number(gamesWon));
			SetText(widget, "GAMES_LOST_VALUE", Number(gamesLost));
			SetText(widget, "FACTIONS_PLAYED_VALUE", $"{Number(factionsPlayed)} / {Number(factions.Count)}");
			SetText(widget, "UNITS_KILLED_VALUE", Number(unitsKilled));
			SetText(widget, "UNITS_LOST_VALUE", Number(unitsLost));
			SetText(widget, "BUILDINGS_DESTROYED_VALUE", Number(buildingsDestroyed));
			SetText(widget, "BUILDINGS_LOST_VALUE", Number(buildingsLost));
			SetText(widget, "RESOURCES_COLLECTED_VALUE", Metric(resourcesEarned));
			SetText(widget, "RESOURCES_SPENT_VALUE", Metric(resourcesSpent));
			SetText(widget, "MEDIAN_GAME_LENGTH_VALUE", FormatGameLength(summary.OverallGameLength.MedianMilliseconds));
			SetText(widget, "AVERAGE_GAME_LENGTH_VALUE", FormatGameLength(summary.OverallGameLength.AverageMilliseconds));
			SetText(widget, "ENEMY_ASSETS_DESTROYED_VALUE", Metric(enemyAssetsDestroyed));
			SetText(widget, "ASSETS_OWNED_VALUE", Metric(assetsOwned));

			var maps = widget.GetOrNull<ScrollPanelWidget>("MAP_LIST");
			var mapTemplate = maps?.GetOrNull<ScrollItemWidget>("MAP_TEMPLATE");
			if (maps != null && mapTemplate != null)
			{
				maps.RemoveChildren();
				foreach (var map in summary.TopMaps)
				{
					var item = ScrollItemWidget.Setup(mapTemplate, () => false, () => { });
					SetText(item, "MAP", map.Title);
					SetText(item, "MAP_GAMES", Number(map.Games));
					maps.AddChild(item);
				}
			}

			var selectedFaction = factions.FirstOrDefault(f =>
				stats.TryGetValue(f.InternalName, out var s) && s.GamesPlayed > 0) ?? factions.FirstOrDefault();

			void UpdateFactionDetails()
			{
				if (selectedFaction == null)
					return;

				stats.TryGetValue(selectedFaction.InternalName, out var factionStats);
				summary.FactionGameLengths.TryGetValue(selectedFaction.InternalName, out var gameLength);
				factionStats ??= new FactionStatistics();
				gameLength ??= new GameLengthStatistics(0, 0, 0);

				SetText(widget, "FACTION_NAME", ResolveName(selectedFaction));
				SetText(widget, "FACTION_GAMES_PLAYED_VALUE", Number(factionStats.GamesPlayed));
				SetText(widget, "FACTION_GAMES_WON_VALUE", Number(factionStats.GamesWon));
				SetText(widget, "FACTION_GAMES_LOST_VALUE", Number(factionStats.GamesLost));
				SetText(widget, "FACTION_UNITS_KILLED_VALUE", Number(factionStats.UnitsKilled));
				SetText(widget, "FACTION_UNITS_LOST_VALUE", Number(factionStats.UnitsLost));
				SetText(widget, "FACTION_BUILDINGS_DESTROYED_VALUE", Number(factionStats.BuildingsKilled));
				SetText(widget, "FACTION_BUILDINGS_LOST_VALUE", Number(factionStats.BuildingsLost));
				SetText(widget, "FACTION_MEDIAN_GAME_LENGTH_VALUE", FormatGameLength(gameLength.MedianMilliseconds));
				SetText(widget, "FACTION_AVERAGE_GAME_LENGTH_VALUE", FormatGameLength(gameLength.AverageMilliseconds));
				SetText(widget, "FACTION_ENEMY_ASSETS_DESTROYED_VALUE", Metric(factionStats.EnemyAssetsDestroyed));
				SetText(widget, "FACTION_ASSETS_OWNED_VALUE", Metric(factionStats.AssetsOwned));
			}

			var factionList = widget.GetOrNull<ScrollPanelWidget>("FACTION_LIST");
			var factionTemplate = factionList?.GetOrNull<ScrollItemWidget>("FACTION_TEMPLATE");
			if (factionList != null && factionTemplate != null)
			{
				factionList.RemoveChildren();
				foreach (var faction in factions)
				{
					var item = ScrollItemWidget.Setup(factionTemplate,
						() => selectedFaction == faction,
						() =>
						{
							selectedFaction = faction;
							UpdateFactionDetails();
						});
					SetText(item, "FACTION", ResolveName(faction));
					factionList.AddChild(item);
				}
			}

			UpdateFactionDetails();

			var closeButton = widget.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			if (closeButton != null)
				closeButton.OnClick = Ui.CloseWindow;
		}

		static string Number(long value)
		{
			return value.ToString("N0", CultureInfo.CurrentCulture);
		}

		internal static string Metric(long value)
		{
			var suffixes = new[] { "", "K", "M", "B", "T" };
			var scaled = (double)value;
			var suffix = 0;
			while (Math.Abs(scaled) >= 1000 && suffix < suffixes.Length - 1)
			{
				scaled /= 1000;
				suffix++;
			}

			if (suffix == 0)
				return Number(value);

			var rounded = Math.Round(scaled, 1, MidpointRounding.AwayFromZero);
			if (Math.Abs(rounded) >= 1000 && suffix < suffixes.Length - 1)
			{
				rounded /= 1000;
				suffix++;
			}

			return rounded.ToString("0.#", CultureInfo.CurrentCulture) + suffixes[suffix];
		}

		internal static string FormatGameLength(double milliseconds)
		{
			if (milliseconds <= 0)
				return "—";

			var duration = TimeSpan.FromMilliseconds(milliseconds);
			return duration.TotalHours >= 1
				? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
				: $"{(int)duration.TotalMinutes}:{duration.Seconds:00}";
		}

		static bool IsRandom(FactionInfo f)
		{
			return string.Equals(f.InternalName, "Random", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(f.Side, "Random", StringComparison.OrdinalIgnoreCase);
		}

		static string ResolveName(FactionInfo f)
		{
			var name = f.Name;
			if (!string.IsNullOrEmpty(name) && FluentProvider.TryGetMessage(name, out var localized))
				return localized;

			return string.IsNullOrEmpty(name) ? f.InternalName : name;
		}

		static void SetText(Widget parent, string id, string value)
		{
			var label = parent.GetOrNull<LabelWidget>(id);
			if (label != null)
				label.GetText = () => value;
		}
	}
}
