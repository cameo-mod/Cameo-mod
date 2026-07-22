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
			var stats = CameoStatistics.Load();

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
			var enemiesKilled = stats.Values.Sum(s => (long)s.UnitsKilled);
			var buildingsDestroyed = stats.Values.Sum(s => (long)s.BuildingsKilled);
			var resourcesEarned = stats.Values.Sum(s => s.ResourcesEarned);
			var resourcesSpent = stats.Values.Sum(s => s.ResourcesSpent);
			var factionsPlayed = stats.Count(kv => kv.Value.GamesPlayed > 0);

			SetText(widget, "GAMES_PLAYED_VALUE", Number(gamesPlayed));
			SetText(widget, "GAMES_WON_VALUE", Number(gamesWon));
			SetText(widget, "GAMES_LOST_VALUE", Number(gamesLost));
			SetText(widget, "FACTIONS_PLAYED_VALUE", $"{Number(factionsPlayed)} / {Number(factions.Count)}");
			SetText(widget, "ENEMIES_KILLED_VALUE", Number(enemiesKilled));
			SetText(widget, "BUILDINGS_DESTROYED_VALUE", Number(buildingsDestroyed));
			SetText(widget, "RESOURCES_COLLECTED_VALUE", Metric(resourcesEarned));
			SetText(widget, "RESOURCES_SPENT_VALUE", Metric(resourcesSpent));

			var list = widget.GetOrNull<ScrollPanelWidget>("FACTION_LIST");
			var template = list?.GetOrNull<ScrollItemWidget>("ROW_TEMPLATE");
			if (list != null && template != null)
			{
				list.RemoveChildren();
				foreach (var faction in factions)
				{
					stats.TryGetValue(faction.InternalName, out var s);

					var name = ResolveName(faction);
					var games = Number(s?.GamesPlayed ?? 0);
					var wins = Number(s?.GamesWon ?? 0);
					var kills = Number(s?.UnitsKilled ?? 0);
					var destroyed = Number(s?.BuildingsKilled ?? 0);

					var item = ScrollItemWidget.Setup(template, () => false, () => { });
					SetText(item, "NAME", name);
					SetText(item, "GAMES", games);
					SetText(item, "WINS", wins);
					SetText(item, "KILLS", kills);
					SetText(item, "DESTROYED", destroyed);
					list.AddChild(item);
				}
			}

			var closeButton = widget.GetOrNull<ButtonWidget>("CLOSE_BUTTON");
			if (closeButton != null)
				closeButton.OnClick = Ui.CloseWindow;
		}

		static string Number(long value)
		{
			return value.ToString("N0", CultureInfo.CurrentCulture);
		}

		static string Metric(long value)
		{
			var magnitude = Math.Abs((double)value);
			if (magnitude >= 1_000_000_000_000)
				return (value / 1_000_000_000_000d).ToString("0.#", CultureInfo.CurrentCulture) + "T";
			if (magnitude >= 1_000_000_000)
				return (value / 1_000_000_000d).ToString("0.#", CultureInfo.CurrentCulture) + "B";
			if (magnitude >= 1_000_000)
				return (value / 1_000_000d).ToString("0.#", CultureInfo.CurrentCulture) + "M";
			if (magnitude >= 1_000)
				return (value / 1_000d).ToString("0.#", CultureInfo.CurrentCulture) + "K";

			return Number(value);
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
