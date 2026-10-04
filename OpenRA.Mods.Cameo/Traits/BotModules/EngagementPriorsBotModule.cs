#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
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
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// The tier-1 fitted priors (tools/ai/fit_engagement_priors.py), one schema per the 2026-10-03
	/// contract ruling (F1): a `BotEngagementPriors` root with `DeliveryArmour@&lt;tag&gt;__x__&lt;armor&gt;`
	/// cells (residuals on the resolved Versus prior, clamped to the fitter's own bound), per-delivery
	/// `DefenceState@&lt;tag&gt;` factors and a global `IntoDefencesMilli`. Stat-normalized keys — warhead
	/// tags and armour classes, no unit ids — so the file survives roster churn.
	///
	/// Per-cell staleness (F1-b): the fitter also writes `PriorPct@&lt;tag&gt;__x__&lt;armor&gt;` = the resolved
	/// Versus percent the cell was fitted on. The game recomputes the same resolved prior from
	/// `^Warhead_&lt;tag&gt;`'s Versus table; a cell whose prior moved since the fit reverts to neutral —
	/// a rebalance invalidates exactly the cells that shifted instead of the whole file. A cell with no
	/// PriorPct row is treated as unfitted and stays neutral. `LedgerHash` remains offline provenance
	/// only — the ledgers live outside the mounted mod paths and cannot be verified in-match.
	/// </summary>
	public sealed class BotEngagementPriors
	{
		public const int Neutral = 1000;

		readonly Dictionary<string, int> factors = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> fittedPriors = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> defenceFactors = new(StringComparer.Ordinal);
		readonly HashSet<string> staleCells = new(StringComparer.Ordinal);

		/// <summary>Residual into static defences (permille) applied when the TARGET is a building. Neutral = 1000.</summary>
		public int IntoDefencesPermille { get; private set; } = Neutral;

		public int FactorCount => factors.Count;
		public int StaleCount => staleCells.Count;
		public string LedgerHash { get; private set; }
		public int? AttritionExponentMilli { get; private set; }

		public static BotEngagementPriors Parse(IEnumerable<MiniYamlNode> nodes)
		{
			var priors = new BotEngagementPriors();
			var root = nodes.FirstOrDefault(n => n.Key == "BotEngagementPriors");
			if (root == null)
				return priors;

			foreach (var node in root.Value.Nodes)
			{
				var v = node.Value.Value;
				if (node.Key == "LedgerHash")
					priors.LedgerHash = v;
				else if (node.Key == "AttritionExponentMilli" && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ae))
					priors.AttritionExponentMilli = ae;
				else if (node.Key == "IntoDefencesMilli" && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
					priors.IntoDefencesPermille = id;
				else if (node.Key.StartsWith("DeliveryArmour@", StringComparison.Ordinal) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var f))
				{
					var cell = CellKey(node.Key["DeliveryArmour@".Length..]);
					if (cell != null)
						priors.factors[cell] = f;
				}
				else if (node.Key.StartsWith("PriorPct@", StringComparison.Ordinal) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pp))
				{
					var cell = CellKey(node.Key["PriorPct@".Length..]);
					if (cell != null)
						priors.fittedPriors[cell] = pp;
				}
				else if (node.Key.StartsWith("DefenceState@", StringComparison.Ordinal) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ds))
					priors.defenceFactors[node.Key["DefenceState@".Length..]] = ds;
			}

			return priors;
		}

		static string CellKey(string raw)
		{
			// `<delivery>__x__<armor>` — both sides are stat-normalized names, never ids.
			var sep = raw.IndexOf("__x__", StringComparison.Ordinal);
			return sep <= 0 ? null : raw[..sep] + "|" + raw[(sep + 5)..];
		}

		/// <summary>Fitted correction in thousandths for (delivery tag, armour); Neutral for an unknown cell
		/// or a cell whose resolved prior moved since the fit (per-cell staleness, F1-b).</summary>
		public int FactorPermille(string delivery, string armor)
		{
			if (delivery == null || armor == null || !factors.TryGetValue(delivery + "|" + armor, out var v))
				return Neutral;

			// Per-cell staleness: the fitter's baseline prior must exist and still equal the resolved
			// Versus percent for this cell — else the fit rode a stat that has since moved.
			if (!fittedPriors.TryGetValue(delivery + "|" + armor, out var fitted))
				return Neutral;

			var current = BotUnitProfiles.ResolvedTagVersus(delivery);
			var now = current != null && current.TryGetValue(armor, out var pct) ? pct : 100;
			if (now != fitted)
			{
				staleCells.Add(delivery + "|" + armor);
				return Neutral;
			}

			return v;
		}

		/// <summary>Per-delivery static-defence effectiveness in thousandths; Neutral when the delivery
		/// was never fitted (these rows carry no PriorPct — the scalar is trusted as committed).</summary>
		public int DefenceFactorPermille(string delivery) =>
			delivery != null && defenceFactors.TryGetValue(delivery, out var v) ? v : Neutral;
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("The combat veto's tier-1 prior source (AI_ARCHITECTURE §12.31, contract ruling F1): serves the",
		"delivery×armour residual table from ai/learned/engagement_priors.yaml on IBotEngagementPriors",
		"(thousandths, bounded 500–2000 like the fit). Frozen at match start; a missing file, a missing cell",
		"or a stale PriorPct row means 1000 — the pure BotCombatPredictor numbers.",
		"genericbot && tier1_priors only — classic never sees the provider.")]
	public class EngagementPriorsBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Mod-relative path of the priors file written by tools/ai/fit_engagement_priors.py --write.")]
		public readonly string PriorsFile = "ai/learned/engagement_priors.yaml";

		[Desc("Lowest correction (thousandths) a fitted prior may give an attacker's damage.")]
		public readonly int MinCorrectionMilli = 500;

		[Desc("Highest correction (thousandths) a fitted prior may give an attacker's damage.")]
		public readonly int MaxCorrectionMilli = 2000;

		public override object Create(ActorInitializer init) { return new EngagementPriorsBotModule(init.Self, this); }
	}

	public class EngagementPriorsBotModule : ConditionalTrait<EngagementPriorsBotModuleInfo>, IBotTick, IBotEngagementPriors
	{
		readonly OpenRA.Player player;
		BotEngagementPriors priors = new();
		bool loaded;

		public EngagementPriorsBotModule(Actor self, EngagementPriorsBotModuleInfo info)
			: base(info)
		{
			player = self.Owner;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (loaded)
				return;

			loaded = true;
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.PriorsFile))
			{
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: none ({Info.PriorsFile} missing), neutral");
				return;
			}

			using (var stream = fs.Open(Info.PriorsFile))
				priors = BotEngagementPriors.Parse(MiniYaml.FromStream(stream, Info.PriorsFile));

			Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: {priors.FactorCount} cells, into-defences {priors.IntoDefencesPermille}‰" +
				$"{(priors.LedgerHash != null ? $", ledger {priors.LedgerHash} (provenance only — unverifiable in-match)" : "")}");
		}

		int IBotEngagementPriors.CorrectionMilli(BotUnitProfile attacker, BotUnitProfile target)
		{
			if (IsTraitDisabled || !loaded || attacker == null || target == null)
				return BotEngagementPriors.Neutral;

			// The fitted cells are per (delivery tag, armour): the attacker's dominant weapon against this
			// target supplies the delivery key — the same scoring DamagePerTickAgainst applies per weapon.
			var delivery = DominantDelivery(attacker, target);
			var f = (long)priors.FactorPermille(delivery, target.Armor);
			if (attacker.IsBuilding)
				f = f * priors.DefenceFactorPermille(delivery) / BotEngagementPriors.Neutral;
			if (target.IsBuilding)
				f = f * priors.IntoDefencesPermille / BotEngagementPriors.Neutral;

			return (int)Math.Clamp(f, Info.MinCorrectionMilli, Info.MaxCorrectionMilli);
		}

		// The weapon that would do most of the damage against this target decides the delivery cell —
		// the per-pair correction cannot see inside DamagePerTickAgainst's weapon sum either way.
		static string DominantDelivery(BotUnitProfile attacker, BotUnitProfile target)
		{
			var best = -1.0;
			string delivery = null;
			foreach (var w in attacker.Weapons)
			{
				if (!w.CanTarget(target.TargetTypes))
					continue;

				var score = w.DamagePerTick * (target.Armor == null ? 100 : w.Versus.GetValueOrDefault(target.Armor, 100));
				if (score > best)
				{
					best = score;
					delivery = w.Delivery;
				}
			}

			return delivery;
		}
	}
}
