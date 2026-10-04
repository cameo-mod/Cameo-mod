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
	/// Per-cell carry-over (F1-b → PRIORS-CARRY): the fitter also writes `PriorPct@&lt;tag&gt;__x__&lt;armor&gt;` =
	/// the resolved Versus percent the cell was fitted on. The game recomputes the same resolved prior
	/// from `^Warhead_&lt;tag&gt;`'s Versus table; a cell whose prior moved since the fit keeps its residual
	/// decayed by `exp(-|ln(now/fitted)| / StalenessTauMilli)` — a rebalance shrinks exactly the cells
	/// that shifted, in proportion to how far, instead of zeroing them (a residual is relative, so it
	/// survives). A cell with no PriorPct row is treated as unfitted and stays neutral; a cell whose
	/// delivery row disappeared outright goes neutral. `LedgerHash` remains offline provenance
	/// only — the ledgers live outside the mounted mod paths and cannot be verified in-match.
	/// </summary>
	public sealed class BotEngagementPriors
	{
		public const int Neutral = 1000;

		// The fitter's per-cell bound (MIN_MILLI/MAX_MILLI in fit_engagement_priors.py) and the
		// MinCorrectionMilli/MaxCorrectionMilli defaults — a scaled cell must stay inside it too.
		const int MinFittedMilli = 500;
		const int MaxFittedMilli = 2000;

		readonly Dictionary<string, int> factors = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> fittedPriors = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> defenceFactors = new(StringComparer.Ordinal);
		readonly Dictionary<string, int> carriedDecayMilli = new(StringComparer.Ordinal);

		int? intoDefences;

		/// <summary>Residual into static defences (permille) applied when the TARGET is a building. Neutral = 1000
		/// (absent row = unfitted, never the global scale).</summary>
		public int IntoDefencesPermille => intoDefences == null ? Neutral : Scale(intoDefences.Value);

		/// <summary>Optional global obs/exp scale the fitted factors are relative to (permille; default 1000 =
		/// absolute factors). When the fitter emits it, every lookup multiplies it back in so relative factors
		/// reproduce measured performance; absent keeps Schema-1 absolute semantics.</summary>
		public int GlobalScaleMilli { get; private set; } = Neutral;

		/// <summary>Carry-over horizon for a moved cell (permille of the |ln(now/fitted)| exponent;
		/// default 350: a 10% Versus move keeps ~76% of the residual, a 2x redesign ~14%). The fitter
		/// may write it next to its cells; absent keeps the default.</summary>
		public int StalenessTauMilli { get; private set; } = 350;

		public int FactorCount => factors.Count;
		public int CarriedCount => carriedDecayMilli.Count;
		public int MeanDecayMilli =>
			carriedDecayMilli.Count == 0 ? Neutral : carriedDecayMilli.Values.Sum() / carriedDecayMilli.Count;
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
					priors.intoDefences = id;
				else if (node.Key == "GlobalScaleMilli" && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var gs))
					priors.GlobalScaleMilli = gs;
				else if (node.Key == "StalenessTauMilli" && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var st) && st > 0)
					priors.StalenessTauMilli = st;
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

		/// <summary>Fitted correction in thousandths for (delivery tag, armour); Neutral for an unknown cell.
		/// A cell whose resolved prior moved since the fit carries its residual over, decayed by
		/// exp(-|ln(now/fitted)| / tau) (PRIORS-CARRY): a residual is relative, so it survives a
		/// rebalance with confidence shrinking as the stat moved. Only a delivery whose Versus row
		/// disappeared outright — or a degenerate non-positive prior — still goes fully neutral.</summary>
		public int FactorPermille(string delivery, string armor)
		{
			if (delivery == null || armor == null || !factors.TryGetValue(delivery + "|" + armor, out var v))
				return Neutral;

			// The fitter's baseline prior must exist for this cell — a DeliveryArmour row with no
			// PriorPct was never staked to a prior and stays neutral.
			if (!fittedPriors.TryGetValue(delivery + "|" + armor, out var fitted))
				return Neutral;

			var current = BotUnitProfiles.ResolvedTagVersus(delivery);
			if (current == null && BotUnitProfiles.VersusTableLoaded)
				return Neutral;

			// An unresolved cell reads as the Versus-default 100 — under tests/headless the whole
			// warhead map is unloaded, so a missing table is "unverifiable", not "gone". When the
			// map IS loaded a null row means the delivery class truly disappeared (neutral above).
			var now = current != null && current.TryGetValue(armor, out var pct) ? pct : 100;
			if (now != fitted)
			{
				if (fitted <= 0 || now <= 0)
					return Neutral;

				var decayMilli = (int)(Math.Exp(-Math.Abs(Math.Log((double)now / fitted)) * Neutral / StalenessTauMilli) * Neutral);
				carriedDecayMilli[delivery + "|" + armor] = decayMilli;
				v = Neutral + (int)((v - Neutral) * (long)decayMilli / Neutral);
			}

			return Scale(v);
		}

		/// <summary>Per-delivery static-defence effectiveness in thousandths; Neutral when the delivery
		/// was never fitted (these rows carry no PriorPct — the scalar is trusted as committed).</summary>
		public int DefenceFactorPermille(string delivery) =>
			delivery != null && defenceFactors.TryGetValue(delivery, out var v) ? Scale(v) : Neutral;

		// Fitted values come back absolute: the optional global obs/exp scale the fitter wrote them
		// relative to is multiplied back in. Neutral stays Neutral — a stale or missing cell reads
		// the pure predictor, not the corrected global level.
		// Re-clamp after GlobalScaleMilli: a cell fitted at the bound (e.g. 2000) scaled by g > 1000
		// would otherwise sit outside the documented [500, 2000] cell range.
		int Scale(int fittedMilli) => (int)Math.Clamp((long)fittedMilli * GlobalScaleMilli / Neutral, MinFittedMilli, MaxFittedMilli);
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("The combat veto's tier-1 prior source (AI_ARCHITECTURE §12.31, contract ruling F1): serves the",
		"delivery×armour residual table from ai/learned/engagement_priors.yaml on IBotEngagementPriors",
		"(thousandths, bounded 500–2000 like the fit). Frozen at match start; a missing file, a missing cell",
		"or a vanished Versus row means 1000 — the pure BotCombatPredictor numbers — while a moved PriorPct",
		"carries its residual over decayed (PRIORS-CARRY).",
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

		string priorsState; // "none" | "error" | "fitted" — CarriedCount/MeanDecayMilli are read live so the match record shows end-of-match carry-over
		public string PriorsState =>
			priorsState == "fitted" ? $"fitted:{priors.FactorCount}/carried:{priors.CarriedCount}/decay:{priors.MeanDecayMilli}" : priorsState;

		void IBotTick.BotTick(IBot bot)
		{
			if (loaded)
				return;

			loaded = true;
			var fs = Game.ModData.DefaultFileSystem;
			if (!fs.Exists(Info.PriorsFile))
			{
				priorsState = "none";
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: none ({Info.PriorsFile} missing), neutral");
				return;
			}

			try
			{
				using (var stream = fs.Open(Info.PriorsFile))
					priors = BotEngagementPriors.Parse(MiniYaml.FromStream(stream, Info.PriorsFile));

				priorsState = "fitted";
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: {priors.FactorCount} cells, into-defences {priors.IntoDefencesPermille}‰" +
					$"{(priors.LedgerHash != null ? $", ledger {priors.LedgerHash} (provenance only — unverifiable in-match)" : "")}");
			}
			catch (Exception e)
			{
				priors = new BotEngagementPriors();
				priorsState = "error";
				Log.Write("debug", $"AI {player.InternalName}: TIER1 priors: error ({e.Message}), neutral");
			}
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

		// The fitter's attrition exponent warps the predicted ratio (CombatVetoEval applies ratio^alpha);
		// absent or disabled stays the pure square law. Same bounds as a correction: alpha in [0.5, 2.0].
		int IBotEngagementPriors.AttritionExponentMilli =>
			IsTraitDisabled || priors.AttritionExponentMilli == null ? 1000
				: Math.Clamp(priors.AttritionExponentMilli.Value, Info.MinCorrectionMilli, Info.MaxCorrectionMilli);

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
