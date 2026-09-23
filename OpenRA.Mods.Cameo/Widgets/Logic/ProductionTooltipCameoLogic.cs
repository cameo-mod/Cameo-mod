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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	// The versus block, armour type, coloured Strong/Weak lines and the attribute line follow Combined
	// Arms' ProductionTooltipLogicCA, with one difference that is the point of the port: CA writes those
	// lines BY HAND per unit, Cameo DERIVES them from the resolved warheads (VersusSummary), so they
	// cannot drift from the balance. Maintainer ruling 2026-09-23 (ROADMAP). Every widget added for it
	// is optional, so other chrome that reuses this logic keeps working.
	public class ProductionTooltipCameoLogic : ChromeLogic
	{
		[FluentReference("prequisites")]
		const string Requires = "label-requires";

		[FluentReference]
		const string VersusHeader = "label-versus-header";

		[FluentReference]
		const string ArmorPiercing = "label-versus-armor-piercing";

		[FluentReference]
		const string CannotAttackCell = "label-versus-cannot-attack";

		[FluentReference("armors")]
		const string StrongLine = "label-versus-strong";

		[FluentReference("armors")]
		const string WeakLine = "label-versus-weak";

		[FluentReference("ladders")]
		const string CannotLine = "label-versus-cannot";

		[FluentReference("targets")]
		const string TargetsLine = "label-versus-targets";

		[FluentReference]
		const string AttributeDetector = "label-attribute-detector";

		[FluentReference]
		const string AttributeStealth = "label-attribute-stealth";

		[FluentReference("count")]
		const string AttributeTransport = "label-attribute-transport";

		[FluentReference]
		const string AttributeHarvester = "label-attribute-harvester";

		[FluentReference]
		const string AttributeShielded = "label-attribute-shielded";

		[FluentReference]
		const string LadderInfantry = "label-versus-infantry";

		[FluentReference]
		const string LadderVehicles = "label-versus-vehicles";

		[FluentReference]
		const string LadderBuildings = "label-versus-buildings";

		[FluentReference]
		const string LadderAircraft = "label-versus-aircraft";

		[FluentReference]
		const string TargetGround = "label-target-ground";

		[FluentReference]
		const string TargetWater = "label-target-water";

		[FluentReference]
		const string TargetUnderwater = "label-target-underwater";

		[FluentReference]
		const string TargetAir = "label-target-air";

		static readonly Dictionary<VersusSummary.LadderKind, string> LadderNames = new()
		{
			{ VersusSummary.LadderKind.Infantry, LadderInfantry },
			{ VersusSummary.LadderKind.Vehicles, LadderVehicles },
			{ VersusSummary.LadderKind.Buildings, LadderBuildings },
			{ VersusSummary.LadderKind.Aircraft, LadderAircraft },
		};

		static readonly Dictionary<string, string> TargetNames = new()
		{
			{ "Ground", TargetGround },
			{ "Water", TargetWater },
			{ "Underwater", TargetUnderwater },
			{ "Air", TargetAir },
			{ "Infantry", LadderInfantry },
			{ "Vehicle", LadderVehicles },
			{ "Structure", LadderBuildings },
		};

		const int MaxLeftWidth = 330;

		[ObjectCreator.UseCtor]
		public ProductionTooltipCameoLogic(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;
			var pm = player.PlayerActor.TraitOrDefault<PowerManager>();
			var pr = player.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null &&
				BuildableInfo.GetTraitForQueue(getTooltipIcon().Actor, getTooltipIcon().ProductionQueue?.Info.Type).ShowTooltip;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var extrasLabel = widget.Get<LabelWidget>("EXTRAS");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var armorTypeLabel = widget.GetOrNull<LabelWidget>("ARMORTYPE");
			var armorTypeIcon = widget.GetOrNull<ImageWidget>("ARMORTYPE_ICON");
			var versusContainer = widget.GetOrNull<ContainerWidget>("VERSUS");
			var versusTemplate = widget.GetOrNull<LabelWidget>("VERSUS_CELL");
			var strengthsLabel = widget.GetOrNull<LabelWidget>("STRENGTHS");
			var weaknessesLabel = widget.GetOrNull<LabelWidget>("WEAKNESSES");
			var attributesLabel = widget.GetOrNull<LabelWidget>("ATTRIBUTES");
			if (versusTemplate != null)
				versusTemplate.Visible = false;

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var extrasFont = Game.Renderer.Fonts[extrasLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var cellFont = versusTemplate != null ? Game.Renderer.Fonts[versusTemplate.Font] : descFont;
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));

			ActorInfo lastActor = null;
			var lastHotkey = Hotkey.Invalid;
			var lastPowerState = pm?.PowerState ?? PowerState.Normal;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;
			var requiresLabelY = requiresLabel.Bounds.Y;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();

				var actor = tooltipIcon?.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey?.GetValue() ?? Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey && (pm == null || pm.PowerState == lastPowerState))
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip != null ? FluentProvider.GetMessage(tooltip.Name) : actor.Name;
				var buildable = BuildableInfo.GetTraitForQueue(actor, tooltipIcon.ProductionQueue?.Info.Type);

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				nameLabel.GetText = () => name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = $"({hotkey.DisplayString()})";

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.GetText = () => hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites
					.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith('~') && !s.StartsWith('!'))
					.ToList();

				var requiresSize = int2.Zero;
				if (prereqs.Count > 0)
				{
					var requiresText = FluentProvider.GetMessage(Requires, "prerequisites", prereqs.JoinWith(", "));
					requiresLabel.GetText = () => requiresText;
					requiresSize = requiresFont.Measure(requiresText);
					requiresLabel.Visible = true;
					requiresLabel.Bounds.Y = requiresLabelY;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height;
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var powerSize = new int2(0, 0);
				var power = 0;
				if (pm != null)
				{
					power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
					var powerText = power.ToString(NumberFormatInfo.CurrentInfo);
					powerLabel.GetText = () => powerText;
					powerLabel.GetColor = () => (pm.PowerProvided - pm.PowerDrained >= -power || power > 0)
						? Color.White : Color.Red;
					powerLabel.Visible = power != 0;
					powerIcon.Visible = power != 0;
					powerSize = font.Measure(powerText);
				}

				var buildTime = tooltipIcon.ProductionQueue?.GetBuildTime(actor, buildable) ?? 0;
				var timeModifier = pm != null && pm.PowerState != PowerState.Normal ? tooltipIcon.ProductionQueue.Info.LowPowerModifier : 100;

				var timeText = formatBuildTime.Update(buildTime * timeModifier / 100);
				timeLabel.GetText = () => timeText;
				timeLabel.TextColor =
					(pm != null && pm.PowerState != PowerState.Normal && tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100)
						? Color.Red
						: Color.White;
				var timeSize = font.Measure(timeText);

				costLabel.IsVisible = () => cost != 0;
				costIcon.IsVisible = () => cost != 0;
				var costText = cost.ToString(NumberFormatInfo.CurrentInfo);
				costLabel.GetText = () => costText;
				costLabel.GetColor = () => pr.GetCashAndResources() >= cost ? Color.White : Color.Red;
				var costSize = font.Measure(costText);

				// The unit's own armour, under the time/power column (CA's GetArmorTypeLabel, Cameo's ladders).
				var armorTypeSize = int2.Zero;
				if (armorTypeLabel != null && armorTypeIcon != null)
				{
					var (armorText, armorColor) = ArmorType(actor);
					armorTypeLabel.GetText = () => armorText;
					armorTypeLabel.GetColor = () => armorColor;
					armorTypeLabel.Visible = armorTypeIcon.Visible = armorText != "";
					armorTypeSize = armorText != "" ? font.Measure(armorText) : int2.Zero;
					var below = power != 0 ? powerIcon : timeIcon;
					armorTypeIcon.Bounds.Y = below.Bounds.Bottom + 4;
					armorTypeLabel.Bounds.Y = powerLabel.Bounds.Y - powerIcon.Bounds.Y + armorTypeIcon.Bounds.Y;
				}

				var tooltipExtras = actor.TraitInfos<TooltipExtrasInfo>();
				extrasLabel.Text = string.Join("\n", tooltipExtras.Select(extra => FluentProvider.GetMessage(extra.Description)));
				var extraSize = new int2(0, 0);

				if (extrasLabel.Text != "")
				{
					extraSize = extrasFont.Measure(extrasLabel.Text);
					extrasLabel.Visible = true;
					descLabel.Bounds.Y += extraSize.Y;
					requiresLabel.Bounds.Y += extraSize.Y;
				}

				var summary = versusContainer != null && versusTemplate != null ? VersusSummary.For(actor, mapRules) : null;
				var derived = summary != null && summary.HasWeapons;

				var desc = string.IsNullOrEmpty(buildable.Description) ? "" : FluentProvider.GetMessage(buildable.Description);

				// DESIGN §7's hand-written "Strong vs / Weak vs" lines are replaced by the derived ones below.
				if (derived)
					desc = StripHandWrittenVersus(desc);

				descLabel.GetText = () => desc;
				var descSize = descFont.Measure(desc);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y + descLabelPadding;

				var bottom = descLabel.Bounds.Y + descSize.Y;
				var versusWidth = 0;
				if (summary != null)
				{
					versusContainer.RemoveChildren();
					if (derived)
						(bottom, versusWidth) = LayoutVersus(summary, versusContainer, versusTemplate, cellFont, descLabel.Bounds.X, bottom + 4);
				}

				var extraLines = new List<(LabelWidget Label, string Text)>();
				if (derived)
				{
					var strong = summary.Strong().Select(ArmorName).ToList();
					var weak = summary.Weak().Select(ArmorName).ToList();
					var cannot = summary.CannotAttack().Select(k => FluentProvider.GetMessage(LadderNames[k])).ToList();

					var strongText = strong.Count > 0 ? FluentProvider.GetMessage(StrongLine, "armors", strong.JoinWith(", ")) : "";
					var weakLines = new List<string>();
					if (weak.Count > 0)
						weakLines.Add(FluentProvider.GetMessage(WeakLine, "armors", weak.JoinWith(", ")));
					if (cannot.Count > 0)
						weakLines.Add(FluentProvider.GetMessage(CannotLine, "ladders", cannot.JoinWith(", ")));

					extraLines.Add((strengthsLabel, strongText));
					extraLines.Add((weaknessesLabel, weakLines.JoinWith("\n")));
				}

				extraLines.Add((attributesLabel, Attributes(actor).JoinWith("\n")));

				var extraWidth = 0;
				foreach (var (label, text) in extraLines)
				{
					if (label == null)
						continue;

					var wrapped = WrapText(text, MaxLeftWidth, descFont);
					label.GetText = () => wrapped;
					label.Visible = wrapped != "";
					if (!label.Visible)
						continue;

					var size = descFont.Measure(wrapped);
					label.Bounds.Y = bottom + 2;
					label.Bounds.Height = size.Y;
					bottom = label.Bounds.Y + size.Y;
					extraWidth = Math.Max(extraWidth, size.X);
				}

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X, extraSize.X, versusWidth, extraWidth }.Aggregate(Math.Max);
				var rightWidth = new[] { powerSize.X, timeSize.X, costSize.X, armorTypeSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				if (armorTypeIcon != null && armorTypeLabel != null)
				{
					armorTypeIcon.Bounds.X = timeIcon.Bounds.X;
					armorTypeLabel.Bounds.X = timeLabel.Bounds.X;
				}

				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = Math.Max(descLabel.Bounds.Bottom, bottom + descLabelPadding) + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightBottom = armorTypeIcon != null && armorTypeIcon.Visible ? armorTypeIcon.Bounds.Bottom
					: powerLabel.Visible ? powerIcon.Bounds.Bottom : timeIcon.Bounds.Bottom;
				var rightHeight = rightBottom + costIcon.Bounds.Top;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
				if (pm != null)
					lastPowerState = pm.PowerState;
			};
		}

		// One row per armour ladder: the ladder name, then one coloured cell per rung ("Heavy 150%"),
		// wrapping inside MaxLeftWidth. A header line carries the targets and, for an anti-heavy weapon,
		// the Armor Piercing tag.
		static (int Bottom, int Width) LayoutVersus(VersusSummary summary, ContainerWidget container, LabelWidget template,
			SpriteFont cellFont, int x, int y)
		{
			var lineHeight = cellFont.Measure("Ag").Y + 2;
			var gap = cellFont.Measure("  ").X;
			var width = 0;

			LabelWidget Add(string text, Color color, int cx, int cy)
			{
				var label = template.Clone();
				label.Visible = true;
				label.GetText = () => text;
				label.GetColor = () => color;
				var size = cellFont.Measure(text);
				label.Bounds = new WidgetBounds(cx, cy, size.X, lineHeight);
				container.AddChild(label);
				width = Math.Max(width, cx + size.X - x);
				return label;
			}

			var header = FluentProvider.GetMessage(VersusHeader);
			var headerLabel = Add(header, Color.FromArgb(0xFF, 0xDD, 0x88), x, y);
			var cursor = headerLabel.Bounds.Right + gap;
			if (summary.ArmorPiercing)
			{
				var tag = Add("[" + FluentProvider.GetMessage(ArmorPiercing) + "]", Color.FromArgb(0xFF, 0xAA, 0x22), cursor, y);
				cursor = tag.Bounds.Right + gap;
			}

			if (summary.Targets.Length > 0)
			{
				var targets = summary.Targets.Select(t => FluentProvider.GetMessage(TargetNames[t])).JoinWith(", ");
				Add(FluentProvider.GetMessage(TargetsLine, "targets", targets), Color.FromArgb(0xAA, 0xCC, 0xEE), cursor, y);
			}

			y += lineHeight;
			var nameColumn = VersusSummary.Ladders.Select(l => cellFont.Measure(FluentProvider.GetMessage(LadderNames[l.Kind])).X).Max() + gap;
			foreach (var row in summary.Rows)
			{
				Add(FluentProvider.GetMessage(LadderNames[row.Kind]), Color.FromArgb(0xBB, 0xBB, 0xBB), x, y);
				var cx = x + nameColumn;
				if (!row.CanAttack)
				{
					Add(FluentProvider.GetMessage(CannotAttackCell), Color.FromArgb(0x88, 0x88, 0x88), cx, y);
					y += lineHeight;
					continue;
				}

				foreach (var (armor, percent) in row.Rungs)
				{
					var text = $"{ArmorName(armor)} {percent}%";
					var w = cellFont.Measure(text).X;
					if (cx + w - x > MaxLeftWidth && cx > x + nameColumn)
					{
						cx = x + nameColumn;
						y += lineHeight;
					}

					var cell = Add(text, VersusSummary.ColorFor(percent), cx, y);
					cx = cell.Bounds.Right + gap;
				}

				y += lineHeight;
			}

			return (y, width);
		}

		static string ArmorName(string armor)
		{
			return FluentProvider.GetMessage("label-armor-class." + armor);
		}

		static (string Text, Color Color) ArmorType(ActorInfo actor)
		{
			var armors = actor.TraitInfos<ArmorInfo>().Where(a => a.EnabledByDefault).Select(a => a.Type).ToList();
			var main = VersusSummary.Ladders.SelectMany(l => l.Rungs.Select((r, i) => (l.Kind, Rung: r, Heavy: i >= l.Rungs.Length - 2)))
				.FirstOrDefault(r => armors.Contains(r.Rung));
			if (main.Rung == null)
				return ("", Color.White);

			var text = ArmorName(main.Rung);
			if (armors.Contains("Shield"))
				text += " + " + ArmorName("Shield");

			var color = main.Kind switch
			{
				VersusSummary.LadderKind.Infantry => Color.ForestGreen,
				VersusSummary.LadderKind.Vehicles => main.Heavy ? Color.Firebrick : Color.MediumPurple,
				VersusSummary.LadderKind.Buildings => Color.Peru,
				_ => Color.SkyBlue,
			};

			return (text, color);
		}

		// Derived facts only — a trait gated on a condition is not an innate ability (DESIGN §7).
		static IEnumerable<string> Attributes(ActorInfo actor)
		{
			if (actor.TraitInfos<DetectCloakedInfo>().Any(t => t.EnabledByDefault))
				yield return FluentProvider.GetMessage(AttributeDetector);

			if (actor.TraitInfos<CloakInfo>().Any(t => t.EnabledByDefault))
				yield return FluentProvider.GetMessage(AttributeStealth);

			var cargo = actor.TraitInfos<CargoInfo>().Where(t => t.EnabledByDefault).Sum(t => t.MaxWeight);
			if (cargo > 0)
				yield return FluentProvider.GetMessage(AttributeTransport, "count", cargo);

			if (actor.HasTraitInfo<HarvesterInfo>())
				yield return FluentProvider.GetMessage(AttributeHarvester);

			if (actor.TraitInfos<ArmorInfo>().Any(a => a.EnabledByDefault && a.Type == "Shield"))
				yield return FluentProvider.GetMessage(AttributeShielded);
		}

		static string StripHandWrittenVersus(string desc)
		{
			var lines = desc.Split('\n').Where(line =>
			{
				var t = line.Trim().TrimStart('•', '-', '*', ' ');
				return !t.StartsWith("Strong vs", StringComparison.OrdinalIgnoreCase)
					&& !t.StartsWith("Weak vs", StringComparison.OrdinalIgnoreCase);
			});

			return string.Join("\n", lines).TrimEnd('\n', ' ');
		}

		static string WrapText(string text, int maxWidth, SpriteFont font)
		{
			if (string.IsNullOrEmpty(text))
				return "";

			var output = new List<string>();
			foreach (var paragraph in text.Split('\n'))
			{
				var line = "";
				foreach (var word in paragraph.Split(' '))
				{
					var candidate = line.Length == 0 ? word : line + " " + word;
					if (line.Length > 0 && font.Measure(candidate).X > maxWidth)
					{
						output.Add(line);
						line = "  " + word;
					}
					else
						line = candidate;
				}

				output.Add(line);
			}

			return string.Join("\n", output);
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return FluentProvider.GetMessage(actorTooltip.Name);
			}

			return a;
		}
	}
}
