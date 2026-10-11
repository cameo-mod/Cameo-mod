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
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// THE TARGET CENSUS + WEIGHT MODEL — mirror of <c>tools/balance/target_model.py</c>.
	/// The fantasy built into every divisor: which armors exist, how common each is,
	/// and how dense a blob stands. Everything here is derived from the resolved rules,
	/// not authored constants: change an actor's Armor and every derived number moves.
	/// </summary>
	public sealed class BotTargetModel
	{
		public const double ABlob = 9.0;
		public const double ASelf = 1.0;
		public const double BlobUptime = 0.30;
		public const int ReferenceHp = 200_000;

		public static readonly string[] Armors =
		{
			"None", "Flak", "Plate", "Heroic",
			"Scout", "Light", "Medium", "Heavy", "Superheavy",
			"Wood", "Steel", "Concrete",
			"Fighter", "Bomber", "Helicopter", "Spaceship",
		};

		/// <summary>Armor -> macro class (infantry / vehicle / building / air).</summary>
		public static readonly IReadOnlyDictionary<string, string> ArmorMacro =
			new Dictionary<string, string>(StringComparer.Ordinal)
			{
				["None"] = "INF", ["Flak"] = "INF", ["Plate"] = "INF", ["Heroic"] = "INF",
				["Scout"] = "VEH", ["Light"] = "VEH", ["Medium"] = "VEH", ["Heavy"] = "VEH", ["Superheavy"] = "VEH",
				["Wood"] = "BLD", ["Steel"] = "BLD", ["Concrete"] = "BLD",
				["Fighter"] = "AIR", ["Bomber"] = "AIR", ["Helicopter"] = "AIR", ["Spaceship"] = "AIR",
			};

		/// <summary>
		/// Units-per-cell² floor for one macro class. Every warhead's Versus row picks
		/// exactly one of these — THE floating point of §12.0c (the dense-vs-sparse
		/// trade-off each flat warhead declares).
		/// </summary>
		public static readonly IReadOnlyDictionary<string, double> Density =
			new Dictionary<string, double>(StringComparer.Ordinal)
			{
				["INF"] = 2.0, ["VEH"] = 0.33, ["BLD"] = 0.25, ["AIR"] = 0.20,
			};

		/// <summary>
		/// Share of fighting TIME spent against each macro class — the solo-vs-swarm
		/// knob; INF 0.35 / VEH 0.40 / BLD 0.15 / AIR 0.10.
		/// </summary>
		public static readonly IReadOnlyDictionary<string, double> Engagement =
			new Dictionary<string, double>(StringComparer.Ordinal)
			{
				["INF"] = 0.35, ["VEH"] = 0.40, ["BLD"] = 0.15, ["AIR"] = 0.10,
			};

		/// <summary>Pseudo rows the versus table admits that are not census armors.</summary>
		public static readonly string[] PseudoArmorRows = { "Shield" };

		/// <summary>The warhead types that apply damage (flat family + %-twins).</summary>
		public static readonly IReadOnlySet<string> DamageWarheadTypes = new HashSet<string>(StringComparer.Ordinal)
		{
			"AreaDamage", "SpreadDamage", "TargetDamage", "AreaDamagePercentage", "HealthPercentageDamage",
		};

		static readonly Regex GateToken = new("^[A-Za-z_][A-Za-z0-9_.]*", RegexOptions.Compiled);

		readonly MiniYamlMirrorRuleset rules;
		readonly Lazy<(Dictionary<string, int> Counts, Dictionary<string, List<long>> HpByMacro)> scanLazy;
		readonly Lazy<Dictionary<string, double>> armorWeightsLazy;
		readonly Lazy<double> shieldDamageShareLazy;
		readonly Lazy<Dictionary<string, long>> hpByMacroLazy;
		readonly Lazy<double> medianWeaponRangeLazy;
		readonly Dictionary<string, double> pseudoArmorMeans = new(StringComparer.Ordinal);

		public BotTargetModel(MiniYamlMirrorRuleset rules)
		{
			this.rules = rules;
			scanLazy = new Lazy<(Dictionary<string, int>, Dictionary<string, List<long>>)>(Scan);
			armorWeightsLazy = new Lazy<Dictionary<string, double>>(ComputeArmorWeights);
			shieldDamageShareLazy = new Lazy<double>(ComputeShieldDamageShare);
			hpByMacroLazy = new Lazy<Dictionary<string, long>>(() => scanLazy.Value.HpByMacro
				.ToDictionary(kv => kv.Key, kv => (long)Median(kv.Value), StringComparer.Ordinal));
			medianWeaponRangeLazy = new Lazy<double>(ComputeMedianWeaponRange);
		}

		static long? HpValue(string raw)
		{
			var f = BotFormula.PyFloat(raw);
			if (!f.HasValue)
				return null;
			return (long)f.Value;
		}

		(Dictionary<string, int> Counts, Dictionary<string, List<long>> HpByMacro) Scan()
		{
			var counts = Armors.ToDictionary(a => a, _ => 0, StringComparer.Ordinal);
			var buckets = Density.Keys.ToDictionary(k => k, _ => new List<long>(), StringComparer.Ordinal);
			foreach (var name in rules.Actors.Keys)
			{
				if (name.StartsWith('^') || name.StartsWith('$') || name.StartsWith('-') || name.Contains('.'))
					continue;
				var node = rules.Resolve(name);
				if (node == null)
					continue;
				string armor = null;
				foreach (var child in node.Children)
				{
					// First CANONICAL armor wins — an unrecognised Type must not hide
					// a real armor further down (target_model.py census loop).
					if (child.Key != "Armor" && !child.Key.StartsWith("Armor@", StringComparison.Ordinal))
						continue;
					var value = child.Get("Type");
					if (value != null && counts.ContainsKey(value.Trim()))
					{
						armor = value.Trim();
						break;
					}
				}

				if (armor == null)
					continue;
				counts[armor]++;
				var health = node.Child("Health");
				var hp = HpValue(health?.Get("HP"));
				if (hp.HasValue && hp.Value > 0)
					buckets[ArmorMacro[armor]].Add(hp.Value);
			}

			return (counts, buckets);
		}

		/// <summary>Actor counts per armor row (non-template units only).</summary>
		public IReadOnlyDictionary<string, int> ArmorCensus => scanLazy.Value.Counts;

		/// <summary>
		/// Armor weights: unit census weighted by engagement mix, one tick per armor
		/// (a one-unit armor is still a build target), Shield folded in by share of damage.
		/// </summary>
		public IReadOnlyDictionary<string, double> ArmorWeights => armorWeightsLazy.Value;

		Dictionary<string, double> ComputeArmorWeights()
		{
			// target_model.armor_weights: class totals are the ENGAGEMENT priors
			// themselves; the census only splits WITHIN each macro (with a +1
			// floor per canonical armor). Shield takes its measured share out of
			// the class rows so the total stays 1.0.
			var census = scanLazy.Value.Counts;
			var weightsByArmor = new Dictionary<string, double>(StringComparer.Ordinal);
			foreach (var kv in Engagement)
			{
				var members = Armors.Where(a => ArmorMacro[a] == kv.Key).ToList();
				var flooredTotal = members.Sum(a => census.TryGetValue(a, out var c) ? c + 1.0 : 1.0);
				foreach (var a in members)
					weightsByArmor[a] = flooredTotal > 0
						? kv.Value * ((census.TryGetValue(a, out var c) ? c : 0) + 1.0) / flooredTotal
						: 0.0;
			}

			var shieldShare = ShieldDamageShare;
			if (shieldShare > 0)
			{
				foreach (var armor in Armors)
					weightsByArmor[armor] *= 1.0 - shieldShare;
				weightsByArmor["Shield"] = shieldShare;
			}

			return weightsByArmor;
		}

		static bool ConditionIsGate(string cond)
		{
			if (string.IsNullOrEmpty(cond))
				return false;
			foreach (var term in Regex.Split(cond, @"&&|\|\|"))
			{
				var t = term.Trim().Trim('(', ')').Trim();
				if (t.Length > 0 && !t.StartsWith("!", StringComparison.Ordinal) && GateToken.IsMatch(t))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Share of all raw damage points absorbed by shields, after de-rating by each
		/// armor's own census (shield points x an armor weight are thicker than HP).
		/// </summary>
		public double ShieldDamageShare => shieldDamageShareLazy.Value;

		double ComputeShieldDamageShare()
		{
			double rawHealth = 0;
			double rawShield = 0;
			foreach (var name in rules.Actors.Keys)
			{
				if (name.StartsWith('^'))
					continue;
				var node = rules.Resolve(name);
				if (node == null)
					continue;
				double? hp = null;
				string armor = null;
				MiniYamlMirrorNode shielded = null;
				foreach (var c in node.Children)
				{
					var key = c.Key.Split('@')[0];
					if (key == "Health")
						hp = BotFormula.PyFloat((c.Get("HP") ?? "").Trim());
					if (armor == null && key == "Armor" && !ConditionIsGate(c.Get("RequiresCondition")))
						armor = (c.Get("Type") ?? "").Trim();
					if (shielded == null && key == "Shielded")
						shielded = c;
				}

				if (!hp.HasValue || hp.Value <= 0)
					continue;
				var vClass = armor != null && ArmorMacro.ContainsKey(armor)
					? PseudoArmorMean(armor)
					: 100.0;
				if (vClass == 0)
					vClass = 100.0;
				rawHealth += 100.0 * hp.Value / vClass;
				if (shielded == null)
					continue;
				var poolFlat = BotFormula.PyFloat((shielded.Get("MaxStrength") ?? "").Trim()) ?? 0;
				var poolPct = BotFormula.PyFloat((shielded.Get("MaxPercentageStrength") ?? "").Trim()) ?? 0;
				var init = (BotFormula.PyFloat((shielded.Get("InitialStrength") ?? "").Trim()) ?? 0)
					+ (BotFormula.PyFloat((shielded.Get("InitialPercentageStrength") ?? "").Trim()) ?? 0);
				if (poolFlat + poolPct <= 0 || init <= 0 || ConditionIsGate(shielded.Get("RequiresCondition")))
					continue;
				var pool = poolFlat + poolPct * hp.Value / 100;
				var vShield = PseudoArmorMean("Shield");
				if (vShield == 0)
					vShield = 100.0;
				rawShield += 100.0 * pool / vShield;
			}

			return rawShield + rawHealth > 0 ? rawShield / (rawShield + rawHealth) : 0.0;
		}

		/// <summary>Median live HP per macro class — the "typical target size" anchor.</summary>
		public IReadOnlyDictionary<string, long> HpByMacro => hpByMacroLazy.Value;

		static double Median(IReadOnlyList<long> values)
		{
			if (values.Count == 0)
				return 0.0;
			var sorted = values.OrderBy(v => v).ToArray();
			var n = sorted.Length;
			if (n % 2 != 0)
				return sorted[n / 2];
			return (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
		}

		/// <summary>Σ ENGAGEMENT[m] × median-hp[m] — a measured HP anchor (~4 700 today).</summary>
		public long ReferenceHpMeasured
		{
			get
			{
				var hp = HpByMacro;
				var total = Density.Keys.Sum(m => Engagement[m] * hp[m]);
				return (long)total;
			}
		}

		/// <summary>
		/// Does this template carry damage output ONLY at Damage: 0? Such templates
		/// exist to seed fields on real warheads; census skips them entirely.
		/// </summary>
		public static bool IsDamageInert(MiniYamlMirrorNode node)
		{
			var sawDamageWarhead = false;
			foreach (var child in node.Children)
			{
				if (!child.Key.StartsWith("Warhead@", StringComparison.Ordinal))
					continue;
				if (child.Value == null || !DamageWarheadTypes.Contains(child.Value))
					continue;
				var raw = child.Get("Damage");
				if (raw == null)
					return false;
				var f = BotFormula.PyFloat(raw);
				if (!f.HasValue || f.Value != 0)
					return false;
				sawDamageWarhead = true;
			}

			return sawDamageWarhead;
		}

		/// <summary>Does this template carry ONLY supplementary twins — never a standalone damage profile?</summary>
		public static bool IsSupplementaryTemplate(MiniYamlMirrorNode node)
		{
			var saw = false;
			foreach (var child in node.Children)
			{
				if (!child.Key.StartsWith("Warhead@", StringComparison.Ordinal))
					continue;
				if (child.Value == null || !DamageWarheadTypes.Contains(child.Value))
					continue;
				if (!child.Key.Contains("ExtraDamage", StringComparison.Ordinal) &&
					!child.Key.Contains("FriendlyFire", StringComparison.Ordinal))
					return false;
				saw = true;
			}

			return saw;
		}

		/// <summary>May this template speak for a weapon's CLASS? Neither a helper nor a twin may.</summary>
		public static bool IsClassBearing(MiniYamlMirrorNode node)
		{
			return !(IsDamageInert(node) || IsSupplementaryTemplate(node));
		}

		/// <summary>Templates that must never enter a weapon-class derivation — helpers and twins alike.</summary>
		public HashSet<string> NonClassTemplates()
		{
			return rules.Weapons
				.Where(kv => kv.Key.StartsWith('^') && !IsClassBearing(kv.Value))
				.Select(kv => kv.Key)
				.ToHashSet(StringComparer.Ordinal);
		}

		/// <summary>Names of every `^`-template IsDamageInert selects.</summary>
		public HashSet<string> DamageInertTemplates()
		{
			return rules.Weapons
				.Where(kv => kv.Key.StartsWith('^') && IsDamageInert(kv.Value))
				.Select(kv => kv.Key)
				.ToHashSet(StringComparer.Ordinal);
		}

		/// <summary>
		/// Mean Versus[row] across every MAIN damage warhead in the ruleset (flat
		/// damage only — a %-twin's Versus is a magnitude, not an armor multiplier).
		/// </summary>
		public double PseudoArmorMean(string row = "Shield")
		{
			lock (pseudoArmorMeans)
			{
				if (pseudoArmorMeans.TryGetValue(row, out var cached))
					return cached;
				var values = new List<double>();
				foreach (var node in rules.Weapons.Values)
				{
					if (IsDamageInert(node))
						continue;
					foreach (var child in node.Children)
					{
						if (!child.Key.StartsWith("Warhead@", StringComparison.Ordinal))
							continue;
						var wtype = child.Value ?? "";
						if (wtype.Contains("Percentage", StringComparison.Ordinal) ||
							child.Key.Contains("ExtraDamage", StringComparison.Ordinal) ||
							child.Key.Contains("FriendlyFire", StringComparison.Ordinal))
							continue;
						var damageRaw = child.Get("Damage");
						var damage = damageRaw == null ? 0.0 : BotFormula.PyFloat(damageRaw);
						if (!damage.HasValue || damage.Value == 0)
							continue;
						MiniYamlMirrorNode versus = null;
						foreach (var grand in child.Children)
							if (grand.Key == "Versus")
							{
								versus = grand;
								break;
							}

						if (versus == null)
							continue;
						foreach (var leaf in versus.Children)
						{
							if (leaf.Key != row)
								continue;
							var v = BotFormula.PyFloat(leaf.Value);
							if (v.HasValue)
								values.Add(v.Value);
							break;
						}
					}
				}

				var mean = values.Count == 0 ? 100.0 : values.Average();
				pseudoArmorMeans[row] = mean;
				return mean;
			}
		}

		/// <summary>Mean Versus["Shield"] across every main flat warhead — the pseudo armor anchor.</summary>
		public double ShieldVersusMean => PseudoArmorMean("Shield");

		/// <summary>What ONE point of shield strength is worth as HP.</summary>
		public double ShieldHpFactor
		{
			get
			{
				var mean = PseudoArmorMean("Shield");
				return mean > 0 ? 100.0 / mean : 1.0;
			}
		}

		/// <summary>HP plus the shield pool, converted to HP-equivalent.</summary>
		public double EffectiveHp(double hp, double shieldFlat = 0.0, double shieldPct = 0.0)
		{
			var pool = Math.Max(shieldFlat + shieldPct * hp / 100.0, 0.0);
			return hp + pool * ShieldHpFactor;
		}

		/// <summary>Census-weighted mean versus — the expected armor multiplier per impact.</summary>
		public double WeightedVersus(IReadOnlyDictionary<string, int?> versus, IReadOnlyDictionary<string, double> weights = null)
		{
			weights ??= ArmorWeights;
			var sum = 0.0;
			foreach (var kv in weights)
				sum += kv.Value * (versus != null && versus.TryGetValue(kv.Key, out var v) ? v.Value : 100.0) / 100.0;
			return sum;
		}

		/// <summary>
		/// The density one warhead's Versus row implies, census-weighted. A warhead
		/// strong versus INF hits dense blobs; strong versus AIR sees sparse sky.
		/// </summary>
		public double EffectiveDensity(IReadOnlyDictionary<string, int?> versus, IReadOnlyDictionary<string, double> weights = null)
		{
			weights ??= ArmorWeights;
			var num = 0.0;
			var den = 0.0;
			foreach (var armor in Armors)
			{
				var share = weights[armor] * (versus != null && versus.TryGetValue(armor, out var v) ? v.Value : 100.0) / 100.0;
				num += share * Density[ArmorMacro[armor]];
				den += share;
			}

			return den != 0 ? num / den : Density["VEH"];
		}

		/// <summary>
		/// Expected extra actors caught by one blob-hit. BLOB_UPTIME is the fraction
		/// of firing time spent on a group rather than the lead target; (covered - 1)
		/// because the shooter aimed at a real unit, not empty floor.
		/// </summary>
		public static double FootprintTargets(double footprintCells2, double density)
		{
			var covered = Math.Min(footprintCells2, ABlob);
			return Math.Max(density * BlobUptime * (covered - ASelf), 0.0);
		}

		/// <summary>Median authored weapon Range across the ruleset — the "typical range" anchor.</summary>
		public double MedianWeaponRange => medianWeaponRangeLazy.Value;

		double ComputeMedianWeaponRange()
		{
			var ranges = new List<long>();
			foreach (var name in rules.Weapons.Keys)
			{
				var node = rules.ResolveWeapon(name);
				if (node == null)
					continue;
				var raw = node.Get("Range");
				if (raw == null)
					continue;
				var range = (long)BotFormula.ParseWdist(raw);
				if (range > 0)
					ranges.Add(range);
			}

			if (ranges.Count == 0)
				return 6000.0;
			var sorted = ranges.OrderBy(v => v).ToArray();
			var n = sorted.Length;
			return n % 2 != 0 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0;
		}
	}
}
