#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The parsed tier-1 delivery grid (`BotEngagementPriors` root of
	/// `ai/learned/engagement_priors.yaml`, TIER1_FITTER_SPEC §"file format"): `DeliveryArmour@<tag>__x__<armour>`
	/// cells in permille, each with a `PriorPct@` twin recording the resolved Versus percent the cell was fitted
	/// on. Staleness is per cell (ORDERS F1(b)): the consumer recomputes the current resolved prior — the weapon's
	/// own Versus row — and reverts only cells whose percent moved. `LedgerHash` is offline provenance only; no
	/// global fingerprint gates the file.
	/// </summary>
	public sealed class DeliveryPriors
	{
		public const int Neutral = 1000;

		readonly Dictionary<string, (int Milli, int PriorPct)> cells = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> defenceFire = new(StringComparer.Ordinal);

		public string LedgerHash { get; private set; }
		public int IntoDefences { get; private set; } = Neutral;
		public int AttritionExponent { get; private set; } = Neutral;
		public int CellCount => cells.Count;

		/// <summary>Cell correction for (delivery, armour): reverts to neutral when the weapon's resolved Versus
		/// percent for that armour moved since the fit — the per-cell staleness check of TIER1_FITTER_SPEC F1(b).</summary>
		public int CorrectionMilli(BotWeaponProfile weapon, string targetArmor)
		{
			var armor = targetArmor ?? "-";
			if (!cells.TryGetValue(weapon.Delivery + "|" + armor, out var cell))
				return Neutral;

			var current = armor == "-" ? 100 : weapon.Versus.GetValueOrDefault(armor, 100);
			return cell.PriorPct == current ? cell.Milli : Neutral;
		}

		public int DefenceFireMilli(BotWeaponProfile weapon) =>
			weapon.Delivery != null && defenceFire.TryGetValue(weapon.Delivery, out var v) ? v : Neutral;

		public static DeliveryPriors Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var priors = new DeliveryPriors();
			var root = nodes.FirstOrDefault(n => n.Key == "BotEngagementPriors");
			if (root == null)
				return priors;

			foreach (var node in root.Value.Nodes)
			{
				if (node.Key == "LedgerHash")
					priors.LedgerHash = node.Value.Value;
				else if (node.Key == "IntoDefencesMilli"
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idm))
					priors.IntoDefences = idm;
				else if (node.Key == "AttritionExponentMilli"
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var aem))
					priors.AttritionExponent = aem;
				else if (node.Key.StartsWith("DeliveryArmour@", StringComparison.Ordinal)
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cell))
				{
					var key = CellKey(node.Key["DeliveryArmour@".Length..]);
					if (key != null)
						priors.cells[key] = (cell, priors.cells.TryGetValue(key, out var old) ? old.PriorPct : -1);
				}
				else if (node.Key.StartsWith("PriorPct@", StringComparison.Ordinal)
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pct))
				{
					var key = CellKey(node.Key["PriorPct@".Length..]);
					if (key != null)
						priors.cells[key] = (priors.cells.TryGetValue(key, out var old) ? old.Milli : Neutral, pct);
				}
				else if (node.Key.StartsWith("DefenceState@", StringComparison.Ordinal)
					&& int.TryParse(node.Value.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ds))
					priors.defenceFire[node.Key["DefenceState@".Length..]] = ds;
			}

			return priors;
		}

		// "DeliveryArmour@<tag>__x__<armour>" → "<tag>|<armour>" — one key space for cells and their PriorPct twins.
		static string CellKey(string raw)
		{
			var sep = raw.IndexOf("__x__", StringComparison.Ordinal);
			return sep > 0 ? raw[..sep] + "|" + raw[(sep + 5)..] : null;
		}
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("Tier-1 delivery priors (TIER1_FITTER_SPEC, DESIGN 19.13, AI_ARCHITECTURE 12.34 phase B): the fitter's",
		"delivery×armour correction grid multiplied into the veto's damage assembly, with per-cell staleness against",
		"the resolved Versus prior. genericbot && tier1_priors only — classic never sees the provider.")]
	public class DeliveryPriorsBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Mod-relative path of the tier-1 fitted delivery grid; missing file = neutral priors.")]
		public readonly string PriorsFile = "ai/learned/engagement_priors.yaml";

		[Desc("Bounds on any single cell correction, in permille — the fitter already clamps; this is the in-match belt.")]
		public readonly int MinCorrectionMilli = 500;
		public readonly int MaxCorrectionMilli = 2000;

		public override object Create(ActorInitializer init) { return new DeliveryPriorsBotModule(init.Self, this); }
	}

	public class DeliveryPriorsBotModule : ConditionalTrait<DeliveryPriorsBotModuleInfo>, IBotTick, IBotEngagementPriors
	{
		readonly OpenRA.Player player;
		DeliveryPriors priors = new();
		bool loaded;

		public DeliveryPriorsBotModule(Actor self, DeliveryPriorsBotModuleInfo info)
			: base(info)
		{
			player = self.Owner;
		}

		public string PriorsState { get; private set; }

		void IBotTick.BotTick(IBot bot)
		{
			if (loaded)
				return;

			loaded = true;
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.PriorsFile))
			{
				PriorsState = "delivery:none";
				return;
			}

			try
			{
				using (var stream = fs.Open(Info.PriorsFile))
					priors = DeliveryPriors.Parse(MiniYaml.FromStream(stream, Info.PriorsFile));

				PriorsState = $"delivery:fitted:{priors.CellCount}";
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: {priors.CellCount} delivery cells, " +
					$"exponent {priors.AttritionExponent}‰, into-defences {priors.IntoDefences}‰ (ledger {priors.LedgerHash ?? "?"})");
			}
			catch (Exception e)
			{
				priors = new DeliveryPriors();
				PriorsState = "delivery:error";
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: error ({e.Message}), neutral");
			}
		}

		// The unit-level pair correction is this file's coarser sibling's axis — neutral here.
		public int CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target) => DeliveryPriors.Neutral;

		public int CorrectionMilli(BotWeaponProfile weapon, string targetArmor) =>
			IsTraitDisabled ? DeliveryPriors.Neutral
				: Math.Clamp(priors.CorrectionMilli(weapon, targetArmor), Info.MinCorrectionMilli, Info.MaxCorrectionMilli);

		public int IntoDefencesMilli(BotWeaponProfile weapon) =>
			IsTraitDisabled ? DeliveryPriors.Neutral : priors.IntoDefences;

		public int DefenceFireMilli(BotWeaponProfile weapon) =>
			IsTraitDisabled ? DeliveryPriors.Neutral
				: Math.Clamp(priors.DefenceFireMilli(weapon), Info.MinCorrectionMilli, Info.MaxCorrectionMilli);

		public int AttritionExponentMilli => IsTraitDisabled ? 1000 : priors.AttritionExponent;
	}
}
