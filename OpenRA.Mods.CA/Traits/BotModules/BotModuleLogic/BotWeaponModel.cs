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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// C# port of tools/balance/weapon_efficiency.py (analyse + context factors)
	/// and tools/balance/extract_stats.py (derived_metrics field emission).
	/// One record per weapon — the same numbers docs/balance/derived/*.json carry.
	/// </summary>
	public sealed class BotWeaponModel
	{
		public string Weapon;
		public bool IsModelled;
		public double? EffectiveDamage;
		public double? DamageTotal;
		public double? Footprint;
		public double? Reliability;
		public double? Sigma;
		public double? K;
		public double? KContext;
		public double? KFlat;
		public double? KFlatContext;
		public double? PctAbsolute;
		public double? PctAbsoluteContext;
		public double? FoldedRounding;
		public double? FoldedRoundingContext;
		public double? AvgVersus;
		public double? FactorTargets;
		public double? FactorRange;
		public double? FactorDeadzone;
		public double? Overkill;
		public double? EffectivePerShot;
		public double? EffReload;
		public double? EffectiveDps;
		public double? DpsFloor;
		public double? ProjectileImpactMultiplier;
		public int? NominalProjectileImpacts;

		/// <summary>Per-armor decompositions of the census-weighted aggregate: the flat/folded share
		/// coefficient (k), the standalone percentage absolute, and the folded rounding term, each
		/// evaluated with the target's own armor key instead of the weighted-versus average. Density
		/// (splash footprint) terms stay census-level — the crowd around a target is armor-mixed.</summary>
		public IReadOnlyDictionary<string, double> KByArmor;
		public IReadOnlyDictionary<string, double> PctByArmor;
		public IReadOnlyDictionary<string, double> FoldedByArmor;
		public IReadOnlyList<string> ModelLimitations = Array.Empty<string>();
		public bool Provisional => ModelLimitations.Count > 0;

		// Weapon cadence kept for the bot-facing DamagePerTick helper.
		public int Burst = 1;
		public double? WeaponReload;
		public string BurstDelays;

		public double EffReloadOrDefault()
		{
			return EffReload ?? BotEffectiveDamage.EffReload(1.0, Burst, BurstDelays);
		}

		/// <summary>Effective damage per tick for the bot predictor.
		/// Uncharged: EffectivePerShot * Burst / EffReload (= fixture effective_dps).
		/// ChargeLevel family: the charge delays the same weapon cycle.
		/// AttackTesla (cycle_reload set): the trait's own cycle replaces it.</summary>
		public double DamagePerTick(BotChargeUp charge, int weaponBurst)
		{
			return DamagePerTick(charge, weaponBurst, null);
		}

		/// <summary>Same, with the versus term evaluated against a specific target armor
		/// (the live-stats path: the target's armour is observed, not census-weighted).</summary>
		public double DamagePerTick(BotChargeUp charge, int weaponBurst, string armor)
		{
			if (!IsModelled || !EffectivePerShot.HasValue)
				return 0;
			var shot = EffectivePerShotAgainst(armor) ?? EffectivePerShot.Value;
			var effReload = EffReloadOrDefault();
			if (charge == null)
				return effReload != 0 ? shot * weaponBurst / effReload : 0;
			var windUp = charge.Ticks ?? 0;
			if (!charge.CycleReload.HasValue)
				return effReload + windUp != 0 ? shot * weaponBurst / (effReload + windUp) : 0;
			var cycle = ChargeAttackCycle(charge, WeaponReload);
			if (!cycle.HasValue)
				return effReload + windUp != 0 ? shot * weaponBurst / (effReload + windUp) : 0;
			return cycle.Value.Cycle != 0 ? shot * cycle.Value.Shots / cycle.Value.Cycle : 0;
		}

		/// <summary>EffectivePerShot recomputed for a specific target armor key —
		/// every part's profiled Versus table read at that armor; density terms and
		/// context factors unchanged. Null/missing armor falls back to the census value.</summary>
		public double? EffectivePerShotAgainst(string armor)
		{
			if (!IsModelled || !EffectivePerShot.HasValue || !DamageTotal.HasValue)
				return null;
			if (armor == null || KByArmor == null || !KByArmor.TryGetValue(armor, out var ka))
				return EffectivePerShot;
			var ctx = (FactorTargets ?? 1.0) * (FactorRange ?? 1.0) * (FactorDeadzone ?? 1.0);
			var pa = PctByArmor != null && PctByArmor.TryGetValue(armor, out var p) ? p : 0.0;
			var fa = FoldedByArmor != null && FoldedByArmor.TryGetValue(armor, out var f) ? f : 0.0;
			return DamageTotal.Value * ka * ctx + (pa + fa) * ctx;
		}

		/// <summary>formula.charge_attack_cycle — (cycle, shots) only for traits that
		/// override the weapon's reload (AttackTesla); null for the ChargeLevel family.</summary>
		public static (double Cycle, int Shots)? ChargeAttackCycle(BotChargeUp charge, double? weaponReload)
		{
			if (charge == null || !charge.CycleReload.HasValue || charge.CycleReload.Value == 0)
				return null;
			var burst = (int)(charge.Burst ?? 1);
			var windUp = charge.Ticks ?? 0;
			var cycleReload = charge.CycleReload.Value;
			if (charge.ChargeDelay == null || !weaponReload.HasValue || weaponReload.Value == 0)
				return (BotEffectiveDamage.EffReload(cycleReload, burst, weaponReload) + windUp, burst);
			var gap = weaponReload.Value > charge.ChargeDelay.Value
				? weaponReload.Value + windUp
				: charge.ChargeDelay.Value;
			return ((burst - 1) * gap + cycleReload + windUp, burst);
		}

		// ------------------------------------------------------------- //
		// weapon_efficiency.analyse
		// ------------------------------------------------------------- //

		public const double TargetsFloor = 0.5;
		public const double RangeWeight = 0.25;
		public const double RangeLo = 0.75;
		public const double RangeHi = 1.50;
		public const double DeadzoneWeight = 1.0;
		const string ChipSuffix = "_ExtraDamage";

		sealed class Part
		{
			public string Tag;
			public double Share;
			public double Versus;
			public double Rel;
			public double Secondary;
			public double Footprint;
			public double RoundingShare;
			public string Kind;
			public IReadOnlyDictionary<string, int?> Vs;
		}

		sealed class AnalysisResult
		{
			public double? K;
			public double? KContext;
			public double KFlat;
			public double KFlatContext;
			public double PctAbsolute;
			public double PctAbsoluteContext;
			public double FoldedRounding;
			public double FoldedRoundingContext;
			public double FlatTotal;
			public double DamageTotal;
			public double Overkill;
			public double Sigma;
			public double Effective;
			public double ImpactMultiplier;
			public int? NominalImpacts;
			public List<string> Limitations;
			public List<Part> Parts;
			public double FactorTargets;
			public double FactorRange;
			public double FactorDeadzone;
			public Dictionary<string, double> KByArmor;
			public Dictionary<string, double> PctByArmor;
			public Dictionary<string, double> FoldedByArmor;
		}

		static double TargetsFactor(MiniYamlMirrorNode resolved)
		{
			var raw = resolved.Get("ValidTargets");
			if (raw == null || raw.Trim().Length == 0)
				return 1.0;
			var tokens = new HashSet<string>(StringComparer.Ordinal);
			foreach (var t in raw.Split(','))
			{
				var s = t.Trim().ToLowerInvariant();
				if (s.Length > 0)
					tokens.Add(s);
			}

			var ground = tokens.Contains("ground") || tokens.Contains("water") ||
				tokens.Contains("ship") || tokens.Contains("trees") || tokens.Contains("wall");
			var air = tokens.Contains("air");
			if (!ground && !air)
				return 1.0;
			var share = 0.0;
			if (ground)
				share += BotTargetModel.Engagement["INF"] + BotTargetModel.Engagement["VEH"] + BotTargetModel.Engagement["BLD"];
			if (air)
				share += BotTargetModel.Engagement["AIR"];
			return TargetsFloor + (1.0 - TargetsFloor) * share;
		}

		static double RangeFactor(MiniYamlMirrorNode resolved, double median)
		{
			var raw = resolved.Get("Range");
			if (raw == null || raw.Trim().Length == 0)
				return 1.0;
			var rng = BotEffectiveDamage.ParseWdist(raw);
			if (rng <= 0 || median <= 0)
				return 1.0;
			return Math.Min(Math.Max(1.0 + RangeWeight * (rng / median - 1.0), RangeLo), RangeHi);
		}

		static double DeadzoneFactor(MiniYamlMirrorNode resolved)
		{
			var rawMin = resolved.Get("MinRange");
			var rawMax = resolved.Get("Range");
			if (rawMin == null || rawMax == null)
				return 1.0;
			var lo = BotEffectiveDamage.ParseWdist(rawMin);
			var hi = BotEffectiveDamage.ParseWdist(rawMax);
			if (lo <= 0 || hi <= 0 || lo >= hi)
				return 1.0;
			var ratio = (double)lo / hi;
			return 1.0 - DeadzoneWeight * ratio * ratio;
		}

		static double OverkillFactor(double perShot, double targetHp)
		{
			if (perShot <= 0 || targetHp <= 0)
				return 1.0;
			var shots = Math.Ceiling(targetHp / perShot);
			return targetHp / (shots * perShot);
		}

		static (double Rel, double Secondary, double Footprint) AreaGeometryTerms(
			MiniYamlMirrorNode node, int[] fo, int[] radii, double density, double sigma, int radiusScale = 100)
		{
			double relTotal = 0, secondaryTotal = 0, footprintTotal = 0;
			foreach (var (weight, reliability, footprint) in BotEffectiveDamage.AreaGeometrySamples(node, fo, radii, sigma, radiusScale))
			{
				relTotal += weight * reliability;
				secondaryTotal += weight * BotTargetModel.FootprintTargets(footprint, density);
				footprintTotal += weight * footprint;
			}

			return (relTotal, secondaryTotal, footprintTotal);
		}

		static (double Versus, double Rel, double Secondary, double Footprint, IReadOnlyDictionary<string, int?> Vs) WarheadTerms(
			MiniYamlMirrorNode node, string wtype, double sigma, bool isDirectActor, BotTargetModel target)
		{
			var vs = BotPercentageDamage.VersusTable(node);
			if (wtype == "AreaDamage")
			{
				var heaviness = BotHeaviness.HeavinessOf(node);
				var mode = BotHeaviness.HeavinessModeOf(node);
				BotHeaviness.ValidateSharedNumeric(
					mode, heaviness, vs,
					BotEffectiveDamage.ParseInt32(node.Get("Damage"), "Warhead.Damage"),
					BotEffectiveDamage.ParseInt32(node.Get("PercentageScale"), "Warhead.PercentageScale", 0));
				vs = mode == BotHeaviness.ModeShared
					? BotHeaviness.SharedVersusProfile(vs, heaviness)
					: BotHeaviness.VersusProfile(vs, heaviness);
			}

			var versus = target.WeightedVersus(vs);
			var density = target.EffectiveDensity(vs);
			int[] fo = null, radii = null;
			var live = true;
			if (wtype == "AreaDamage" || wtype == "SpreadDamage")
			{
				(fo, radii, live) = BotEffectiveDamage.FalloffAndRadii(node);
				if (wtype == "AreaDamage")
					BotEffectiveDamage.AreaTickModifiers(node);
			}

			if (isDirectActor)
			{
				var rel = BotEffectiveDamage.Reliability(new[] { 100, 0 }, new[] { 0, BotEffectiveDamage.PointTargetRadius }, sigma);
				return (versus, rel, 0.0, 0.0, vs);
			}

			var radius = 0;
			if (wtype == "TargetDamage")
				(radius, live) = BotEffectiveDamage.TargetDamageRadius(node);
			if (!live)
				return (versus, 0.0, 0.0, 0.0, vs);
			if (wtype == "TargetDamage")
			{
				var footprint = BotEffectiveDamage.UniformFootprintCells2(radius);
				var rel = BotEffectiveDamage.UniformReliability(radius, sigma);
				return (versus, rel, BotTargetModel.FootprintTargets(footprint, density), footprint, vs);
			}

			if (wtype == "AreaDamage")
			{
				var (rel2, secondary2, footprint2) = AreaGeometryTerms(node, fo, radii, density, sigma);
				return (versus, rel2, secondary2, footprint2, vs);
			}

			var fp = BotEffectiveDamage.FootprintCells2(fo, radii);
			var rl = BotEffectiveDamage.Reliability(fo, radii, sigma);
			return (versus, rl, BotTargetModel.FootprintTargets(fp, density), fp, vs);
		}

		static (double Versus, double Rel, double Secondary, double Footprint) PercentageTerms(
			BotPercentageDamage.PctApplication app, double sigma, bool isDirectActor, BotTargetModel target)
		{
			var node = app.Node;
			int[] fo = null, radii = null;
			var live = true;
			if (node.Value == "AreaDamagePercentage" || app.Kind == BotPercentageDamage.PctFolded)
			{
				(fo, radii, live) = BotEffectiveDamage.FalloffAndRadii(node);
				BotEffectiveDamage.AreaTickModifiers(node);
			}

			if (isDirectActor)
			{
				var rel = BotEffectiveDamage.Reliability(new[] { 100, 0 }, new[] { 0, BotEffectiveDamage.PointTargetRadius }, sigma);
				return (target.WeightedVersus(app.Versus), rel, 0.0, 0.0);
			}

			if (node.Value == "HealthPercentageDamage")
			{
				var vs = app.Versus;
				var versus = target.WeightedVersus(vs);
				var density = target.EffectiveDensity(vs);
				var (radius, live2) = BotEffectiveDamage.TargetDamageRadius(node);
				if (live2)
				{
					var footprint = BotEffectiveDamage.UniformFootprintCells2(radius);
					var reliability = BotEffectiveDamage.UniformReliability(radius, sigma);
					return (versus, reliability, BotTargetModel.FootprintTargets(footprint, density), footprint);
				}

				return (versus, 0.0, 0.0, 0.0);
			}

			var vs2 = app.Versus;
			var versus2 = target.WeightedVersus(vs2);
			var density2 = target.EffectiveDensity(vs2);
			if (!live)
				return (versus2, 0.0, 0.0, 0.0);
			var radiusScale = app.Kind == BotPercentageDamage.PctFolded
				? app.PercentageSpread ?? BotPercentageDamage.DefaultPercentageSpread
				: 100;
			var (rel3, secondary3, footprint3) = AreaGeometryTerms(node, fo, radii, density2, sigma, radiusScale);
			return (versus2, rel3, secondary3, footprint3);
		}

		static AnalysisResult Analyse(MiniYamlMirrorNode resolved, double damageTotalArg, BotTargetModel target, double medianRange)
		{
			BotEffectiveDamage.ValidateDamageWarheads(resolved);
			var whs = BotEffectiveDamage.FlatDamageWarheads(resolved);
			var refHp = (double)BotTargetModel.ReferenceHp;
			var (_, sigma) = BotEffectiveDamage.WeaponReliabilityCtx(resolved);
			var isDirectActor = BotEffectiveDamage.DirectActorImpact(resolved);
			var applications = BotPercentageDamage.PercentageApplications(resolved, refHp);
			if (whs.Count == 0 && applications.Count == 0)
				return null;
			var impactMultiplier = BotEffectiveDamage.ProjectileImpactMultiplier(resolved);
			var nominalImpacts = BotEffectiveDamage.ProjectileNominalImpactCount(resolved);
			var limitations = BotEffectiveDamage.ModelLimitations(resolved);
			var parts = new List<Part>();
			double flatTotal = 0;
			foreach (var (_, _, baseDmg, _) in whs)
				flatTotal += baseDmg;
			var shareTotal = flatTotal != 0 ? flatTotal : 1.0;
			var damageTotal = damageTotalArg;
			foreach (var (tag, wtype, baseDmg, node) in whs)
			{
				var (versus, rel, secondary, footprint, vs) = WarheadTerms(node, wtype, sigma, isDirectActor, target);
				parts.Add(new Part
				{
					Tag = tag, Share = baseDmg / shareTotal, Versus = versus, Rel = rel,
					Secondary = secondary, Footprint = footprint, RoundingShare = 0.0, Vs = vs,
					Kind = tag.EndsWith("ExtraDamage", StringComparison.Ordinal) || tag.Contains(ChipSuffix) ? "chip" : "flat",
				});
			}

			foreach (var app in applications)
			{
				var (versus, rel, secondary, footprint) = PercentageTerms(app, sigma, isDirectActor, target);
				var roundingHp = app.RoundingHp;
				if (app.Kind == BotPercentageDamage.PctFolded && damageTotal != flatTotal)
				{
					var modeledDamage = (int)Math.Round(app.Damage * damageTotal / shareTotal, MidpointRounding.ToEven);
					double continuousUnits;
					int runtimeUnits;
					if (app.Mode == BotHeaviness.ModeShared)
						(continuousUnits, runtimeUnits) = BotPercentageDamage.SharedFoldedUnits(modeledDamage, app.Scale, app.Heaviness);
					else
						(continuousUnits, runtimeUnits) = BotPercentageDamage.FoldedUnits(modeledDamage, app.Scale);
					var denominator = app.Denominator;
					var continuousHp = refHp * continuousUnits / denominator;
					var runtimeHp = BotPercentageDamage.RuntimePercentageHp(refHp, runtimeUnits, denominator);
					roundingHp = runtimeHp - continuousHp;
				}

				var applicationHp = app.Kind == BotPercentageDamage.PctFolded ? app.ContinuousHp : app.RuntimeHp;
				parts.Add(new Part
				{
					Tag = app.Tag, Share = applicationHp / shareTotal, Versus = versus, Rel = rel,
					Secondary = secondary, Footprint = footprint, Kind = app.Kind, Vs = app.Versus,
					RoundingShare = roundingHp / shareTotal,
				});
			}

			double Contrib(Part p) => p.Share * p.Versus * (p.Rel + p.Secondary);
			var kFlat = 0.0;
			foreach (var p in parts)
				if (p.Kind == "flat" || p.Kind == "chip" || p.Kind == BotPercentageDamage.PctFolded)
					kFlat += Contrib(p);
			kFlat *= impactMultiplier;
			var pctAbsolute = 0.0;
			foreach (var p in parts)
				if (p.Kind == BotPercentageDamage.PctStandalone)
					pctAbsolute += Contrib(p);
			pctAbsolute *= shareTotal * impactMultiplier;
			var foldedRounding = 0.0;
			foreach (var p in parts)
				if (p.Kind == BotPercentageDamage.PctFolded)
					foldedRounding += p.RoundingShare * p.Versus * (p.Rel + p.Secondary);
			foldedRounding *= shareTotal * impactMultiplier;
			double? k = damageTotal > 0 ? kFlat + (pctAbsolute + foldedRounding) / damageTotal : (double?)null;
			var factorTargets = TargetsFactor(resolved);
			var factorRange = RangeFactor(resolved, medianRange);
			var factorDeadzone = DeadzoneFactor(resolved);
			var ctx = factorTargets * factorRange * factorDeadzone;
			var effectiveUncontextual = damageTotal * kFlat + pctAbsolute + foldedRounding;
			var effective = damageTotal * kFlat * ctx + pctAbsolute * ctx + foldedRounding * ctx;
			var overkill = OverkillFactor(effectiveUncontextual, refHp);

			// Per-armor decompositions for the live path: the same sums with each part's own
			// profiled Versus table keyed by armor instead of the census-weighted average.
			var armorKeys = new HashSet<string>(StringComparer.Ordinal);
			foreach (var a in BotTargetModel.Armors)
				armorKeys.Add(a);
			armorKeys.Add("Shield");
			foreach (var p in parts)
				if (p.Vs != null)
					foreach (var k2 in p.Vs.Keys)
						armorKeys.Add(k2);

			var kByArmor = new Dictionary<string, double>(StringComparer.Ordinal);
			var pctByArmor = new Dictionary<string, double>(StringComparer.Ordinal);
			var foldByArmor = new Dictionary<string, double>(StringComparer.Ordinal);
			foreach (var armor in armorKeys)
			{
				double ka = 0, pa = 0, fa = 0;
				foreach (var p in parts)
				{
					var v = p.Vs != null && p.Vs.TryGetValue(armor, out var vv) && vv.HasValue ? vv.Value : 100;
					var term = v / 100.0 * (p.Rel + p.Secondary);
					if (p.Kind == "flat" || p.Kind == "chip" || p.Kind == BotPercentageDamage.PctFolded)
						ka += p.Share * term;
					if (p.Kind == BotPercentageDamage.PctStandalone)
						pa += p.Share * term;
					if (p.Kind == BotPercentageDamage.PctFolded)
						fa += p.RoundingShare * term;
				}

				kByArmor[armor] = ka * impactMultiplier;
				pctByArmor[armor] = pa * shareTotal * impactMultiplier;
				foldByArmor[armor] = fa * shareTotal * impactMultiplier;
			}

			return new AnalysisResult
			{
				K = k, KContext = k.HasValue ? k.Value * ctx : (double?)null,
				KFlat = kFlat, KFlatContext = kFlat * ctx,
				PctAbsolute = pctAbsolute, PctAbsoluteContext = pctAbsolute * ctx,
				FoldedRounding = foldedRounding, FoldedRoundingContext = foldedRounding * ctx,
				FlatTotal = flatTotal, DamageTotal = damageTotal, Overkill = overkill,
				Sigma = sigma, Effective = effective, ImpactMultiplier = impactMultiplier,
				NominalImpacts = nominalImpacts, Limitations = limitations, Parts = parts,
				FactorTargets = factorTargets, FactorRange = factorRange, FactorDeadzone = factorDeadzone,
				KByArmor = kByArmor, PctByArmor = pctByArmor, FoldedByArmor = foldByArmor,
			};
		}

		/// <summary>extract_stats.derived_metrics — one record per resolved weapon.
		/// Returns null only when the weapon is unresolvable; an unmodelled weapon
		/// returns a record with IsModelled=false (and ModelLimitations).</summary>
		public static BotWeaponModel Compute(MiniYamlMirrorRuleset rs, BotTargetModel target, double medianRange, string weaponName)
		{
			var resolved = rs.ResolveWeapon(weaponName);
			if (resolved == null)
				return null;
			var model = new BotWeaponModel { Weapon = weaponName };
			var ed = BotEffectiveDamage.EffectiveDamageTuple(resolved);
			if (ed.HasValue)
			{
				model.EffectiveDamage = ed.Value.Effective;
				model.DamageTotal = ed.Value.BaseTotal;
				model.Footprint = ed.Value.FootTotal;
				model.Reliability = ed.Value.AvgRel;
				model.Sigma = ed.Value.Sigma;
			}

			var damageTotal = ed?.BaseTotal ?? 0.0;
			var res = Analyse(resolved, damageTotal, target, medianRange);
			if (res != null)
			{
				model.IsModelled = true;
				var flatParts = new List<Part>();
				foreach (var p in res.Parts)
					if (p.Kind == "flat" || p.Kind == "chip")
						flatParts.Add(p);
				var shares = 0.0;
				foreach (var p in flatParts)
					shares += p.Share;
				model.K = res.K;
				model.KContext = res.KContext;
				model.KFlat = res.KFlat;
				model.KFlatContext = res.KFlatContext;
				model.PctAbsolute = res.PctAbsolute;
				model.PctAbsoluteContext = res.PctAbsoluteContext;
				if (Math.Abs(res.FoldedRoundingContext) >= 0.005)
				{
					model.FoldedRounding = res.FoldedRounding;
					model.FoldedRoundingContext = res.FoldedRoundingContext;
				}

				if (shares > 0)
				{
					var av = 0.0;
					foreach (var p in flatParts)
						av += p.Share * p.Versus;
					model.AvgVersus = av / shares;
				}

				model.FactorTargets = res.FactorTargets;
				model.FactorRange = res.FactorRange;
				model.FactorDeadzone = res.FactorDeadzone;
				model.Overkill = res.Overkill;
				model.KByArmor = res.KByArmor;
				model.PctByArmor = res.PctByArmor;
				model.FoldedByArmor = res.FoldedByArmor;
				if (Math.Abs(res.ImpactMultiplier - 1.0) > 1e-9)
					model.ProjectileImpactMultiplier = res.ImpactMultiplier;
				model.NominalProjectileImpacts = res.NominalImpacts;
				model.ModelLimitations = res.Limitations;
				model.EffectivePerShot = res.Effective;

				var burstF = BotEffectiveDamage.Fnum(resolved.Get("Burst"));
				var burst = burstF.HasValue && burstF.Value != 0 ? (int)burstF.Value : 1;
				model.Burst = burst;
				var reloadDelay = BotEffectiveDamage.Fnum(resolved.Get("ReloadDelay"));
				model.WeaponReload = reloadDelay;
				model.BurstDelays = resolved.Get("BurstDelays");
				if (reloadDelay.HasValue && reloadDelay.Value != 0)
				{
					var eff = BotEffectiveDamage.EffReload(reloadDelay.Value, burst, model.BurstDelays);
					model.EffReload = eff;
					model.EffectiveDps = res.Effective * burst / eff;
					if (res.PctAbsoluteContext > 0)
						model.DpsFloor = res.PctAbsoluteContext * burst / eff;
				}
			}
			else
			{
				model.ModelLimitations = BotEffectiveDamage.ModelLimitations(resolved);
			}

			return model;
		}
	}

	/// <summary>extract_stats.charge_up — one actor's charge-up attack trait record.</summary>
	public sealed class BotChargeUp
	{
		public string Trait;
		public double? Ticks;
		public double? CycleReload;
		public double? Burst;
		public double? ChargeDelay;

		static readonly HashSet<string> ChargeUpTraits = new(StringComparer.Ordinal)
		{
			"AttackCharged", "AttackTurretedCharged", "AttackFrontalCharged",
			"AttackCharges", "AttackTesla"
		};

		sealed class ChargeSpec
		{
			public (string Field, double Default)? Charge;
			public (string Field, double Default)? Rate;
			public (string Field, double Default)? CycleReload;
			public (string Field, double Default)? BurstField;
			public (string Field, double Default)? ChargeDelay;
		}

		static readonly Dictionary<string, ChargeSpec> ChargeFields = new(StringComparer.Ordinal)
		{
			{
				"AttackTesla", new ChargeSpec
				{
					Charge = ("InitialChargeDelay", 22),
					CycleReload = ("ReloadDelay", 120),
					BurstField = ("MaxCharges", 1),
					ChargeDelay = ("ChargeDelay", 3),
				}
			},
			{ "AttackCharges", new ChargeSpec { Charge = ("ChargeLevel", 25), Rate = ("ChargeRate", 1) } },
			{ "AttackCharged", new ChargeSpec { Charge = ("ChargeLevel", 25), Rate = ("ChargeRate", 1) } },
			{ "AttackFrontalCharged", new ChargeSpec { Charge = ("ChargeLevel", 25), Rate = ("ChargeRate", 1) } },
			{ "AttackTurretedCharged", new ChargeSpec { Charge = ("ChargeLevel", 25), Rate = ("ChargeRate", 1) } },
		};

		/// <summary>Mean of MIN and MAX of a comma list (charge ranges), else default.</summary>
		static double ChargeScalar(string raw, double defaultValue)
		{
			var parts = new List<double>();
			foreach (var piece in (raw ?? "").Split(','))
			{
				var p = piece.Trim();
				if (p.Length == 0)
					continue;
				if (!double.TryParse(p, out var v))
					return defaultValue;
				parts.Add(v);
			}

			if (parts.Count == 0)
				return defaultValue;
			var min = double.MaxValue;
			var max = double.MinValue;
			foreach (var v in parts)
			{
				if (v < min)
					min = v;
				if (v > max)
					max = v;
			}

			return (min + max) / 2.0;
		}

		static double Num(MiniYamlMirrorNode node, (string Field, double Default)? pair, double? fallback = null)
		{
			if (!pair.HasValue)
				return fallback ?? 0;
			var (field, defaultValue) = pair.Value;
			var n = node.Child(field);
			if (n == null || string.IsNullOrEmpty(n.Value))
				return defaultValue;
			return ChargeScalar(n.Value, defaultValue);
		}

		/// <summary>The actor's charge-up record, or null when it carries none.
		/// Mirrors extract_stats.charge_up: skips traits whose RequiresCondition does
		/// not hold by default, rounds every emitted value to two decimals.</summary>
		public static BotChargeUp For(MiniYamlMirrorNode resolved)
		{
			foreach (var c in resolved.Children)
			{
				var at = c.Key.IndexOf('@');
				var baseName = at >= 0 ? c.Key.Substring(0, at) : c.Key;
				if (!ChargeUpTraits.Contains(baseName))
					continue;
				if (!BotFormula.ConditionHoldsByDefault(c.Get("RequiresCondition")))
					continue;
				var rec = new BotChargeUp { Trait = baseName };
				if (!ChargeFields.TryGetValue(baseName, out var spec))
					return rec;
				var ticks = Num(c, spec.Charge);
				var rate = Num(c, spec.Rate, 1);
				if (rate == 0)
					rate = 1;
				rec.Ticks = Math.Round(ticks / rate, 2, MidpointRounding.ToEven);
				var cycleReload = Num(c, spec.CycleReload);
				if (cycleReload != 0)
				{
					rec.CycleReload = Math.Round(cycleReload, 2, MidpointRounding.ToEven);
					var burst = Num(c, spec.BurstField, 1);
					rec.Burst = burst != 0 ? (int)burst : 1;
					if (spec.ChargeDelay.HasValue)
						rec.ChargeDelay = Math.Round(Num(c, spec.ChargeDelay), 2, MidpointRounding.ToEven);
				}

				return rec;
			}

			return null;
		}
	}
}
