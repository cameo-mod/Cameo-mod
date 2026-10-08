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
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class WeaponModelParityTest
	{
		// Fixture seam (docs/design/PREDICTOR_EFFECTIVE_DAMAGE_MODEL.md §4):
		//   MiniYamlMirror.LoadManifest(repoRoot) -> .Rules / .Weapons file lists
		//   BotWeaponModelTable.FromFiles(weaponsYamlPaths, rulesYamlPaths) -> table
		//   table.Compute(weaponName) -> BotWeaponModel record; table.ChargeUpFor(actorName)
		//   table.Target -> BotTargetModel census/invariants;
		//   BotEffectiveDamage.ScatterPdf/ScatterBinWidth -> the 256-bin pdf.
		// BotWeaponModel members are read through Func<BotWeaponModel, double?> so the
		// lambdas bind unchanged whether the port uses double, double?, float or int.

		string repoRoot;
		BotWeaponModelTable table;

		[OneTimeSetUp]
		public void BuildModelTable()
		{
			repoRoot = FindRepoRoot();

			var manifest = MiniYamlMirror.LoadManifest(repoRoot);
			Assert.That(manifest, Is.Not.Null, "MiniYamlMirror.LoadManifest returned null");
			Assert.That(manifest.Weapons, Is.Not.Null, "manifest.Weapons is null");
			Assert.That(manifest.Weapons, Is.Not.Empty, "manifest.Weapons is empty");
			Assert.That(manifest.Rules, Is.Not.Null, "manifest.Rules is null");
			Assert.That(manifest.Rules, Is.Not.Empty, "manifest.Rules is empty");

			table = BotWeaponModelTable.FromFiles(manifest.Weapons, manifest.Rules);
			Assert.That(table, Is.Not.Null, "BotWeaponModelTable.FromFiles returned null");
			Assert.That(table.Target, Is.Not.Null, "BotWeaponModelTable.Target is null");
		}

		static string FindRepoRoot()
		{
			var bases = new[]
			{
				AppDomain.CurrentDomain.BaseDirectory,
				TestContext.CurrentContext.TestDirectory
			};

			foreach (var start in bases)
			{
				if (string.IsNullOrEmpty(start))
					continue;

				var dir = new DirectoryInfo(start);
				for (var i = 0; i < 12 && dir != null; i++, dir = dir.Parent)
				{
					if (File.Exists(Path.Combine(dir.FullName, "mods", "cameo", "mod.yaml")))
						return dir.FullName;
				}
			}

			Assert.Fail(
				"WeaponModelParityTest: could not locate the repo root " +
				$"(a directory containing mods/cameo/mod.yaml) within 12 levels of {bases[0]}");
			return null; // unreachable — Assert.Fail throws
		}

		static bool Near(double actual, double expected, double absTol, double relTol)
		{
			if (double.IsNaN(actual) || double.IsNaN(expected))
				return double.IsNaN(actual) && double.IsNaN(expected);

			return Math.Abs(actual - expected) <= Math.Max(absTol, relTol * Math.Abs(expected));
		}

		// Fixture field name -> model accessor, tolerances and emission class.
		// Tolerances: fixtures round to 4dp (reliability/footprint/k*/avg_versus/factor_*/
		// overkill) or 2dp (sigma/effective_damage/pct_*/folded_*/eff_reload/effective_dps/
		// dps_floor); abs tolerance leaves headroom over the half-ulp rounding bound plus
		// small port drift, rel 1e-3 covers large magnitudes. Conditional = the field may
		// legitimately be absent from a fixture row (spec §3 emission rules); an absent
		// conditional field never fails, an absent unconditional field fails when the
		// model computes a nonzero value.
		static readonly (string Name, Func<BotWeaponModel, double?> Get, double Abs, double Rel, bool Conditional)[] Fields =
		{
			("effective_per_shot", m => m.EffectivePerShot, 0.05, 1e-3, false),
			("effective_damage", m => m.EffectiveDamage, 0.006, 1e-3, false),
			("damage_total", m => m.DamageTotal, 0.5, 1e-9, false),
			("reliability", m => m.Reliability, 5e-4, 1e-3, false),
			("footprint", m => m.Footprint, 5e-4, 1e-3, false),
			("sigma", m => m.Sigma, 0.006, 1e-3, false),
			("k", m => m.K, 5e-4, 1e-3, true),                    // omitted when null (k <= 0)
			("k_context", m => m.KContext, 5e-4, 1e-3, true),    // omitted when null
			("k_flat", m => m.KFlat, 5e-4, 1e-3, false),
			("k_flat_context", m => m.KFlatContext, 5e-4, 1e-3, false),
			("avg_versus", m => m.AvgVersus, 5e-4, 1e-3, false),
			("factor_targets", m => m.FactorTargets, 5e-4, 1e-3, false),
			("factor_range", m => m.FactorRange, 5e-4, 1e-3, false),
			("factor_deadzone", m => m.FactorDeadzone, 5e-4, 1e-3, false),
			("overkill", m => m.Overkill, 5e-4, 1e-3, false),
			("pct_absolute", m => m.PctAbsolute, 0.006, 1e-3, false),
			("pct_absolute_context", m => m.PctAbsoluteContext, 0.006, 1e-3, false),
			("folded_rounding", m => m.FoldedRounding, 0.006, 1e-3, true),          // emitted only when >= 0.005
			("folded_rounding_context", m => m.FoldedRoundingContext, 0.006, 1e-3, true),
			("eff_reload", m => m.EffReload, 0.006, 1e-3, true),
			("effective_dps", m => m.EffectiveDps, 0.006, 1e-3, true),
			("dps_floor", m => m.DpsFloor, 0.006, 1e-3, true),
			("projectile_impact_multiplier", m => m.ProjectileImpactMultiplier, 5e-4, 1e-3, true), // emitted only when != 1
		};

		[Test]
		public void TargetModelInvariantsMatchFixture()
		{
			var modelPath = Path.Combine(repoRoot, "docs", "balance", "derived", "_model.json");
			Assert.That(File.Exists(modelPath), Is.True, $"fixture missing: {modelPath}");

			using var doc = JsonDocument.Parse(File.ReadAllText(modelPath));
			var targetModel = doc.RootElement.GetProperty("target_model");

			// armor_census: exact per-armor integer match, no missing/extra rows.
			var census = targetModel.GetProperty("armor_census");
			var expected = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var kv in census.EnumerateObject())
				expected[kv.Name] = kv.Value.GetInt32();

			var actual = table.Target.ArmorCensus;
			Assert.That(actual, Is.Not.Null, "BotTargetModel.ArmorCensus is null");
			Assert.That(actual.Count, Is.EqualTo(expected.Count),
				$"armor_census size differs; model has [{string.Join(", ", actual.Keys.OrderBy(k => k, StringComparer.Ordinal))}]");
			foreach (var kv in expected)
			{
				Assert.That(actual.TryGetValue(kv.Key, out var count), Is.True,
					$"armor_census missing armor {kv.Key}");
				Assert.That(count, Is.EqualTo(kv.Value), $"armor_census[{kv.Key}]");
			}

			Assert.That(table.Target.ShieldVersusMean,
				Is.EqualTo(targetModel.GetProperty("shield_versus_mean").GetDouble()).Within(0.5),
				"shield_versus_mean");
			Assert.That(table.Target.ShieldHpFactor,
				Is.EqualTo(targetModel.GetProperty("shield_hp_factor").GetDouble()).Within(0.005),
				"shield_hp_factor");
			Assert.That(table.Target.MedianWeaponRange,
				Is.EqualTo(doc.RootElement.GetProperty("weapon_efficiency").GetProperty("median_weapon_range").GetDouble()).Within(1),
				"median_weapon_range");
			Assert.That(Convert.ToDouble(table.Target.ReferenceHpMeasured),
				Is.EqualTo(targetModel.GetProperty("reference_hp_measured").GetDouble()).Within(5),
				"reference_hp_measured");
		}

		[Test]
		public void DerivedWeaponMetricsMatchFixture()
		{
			var derivedDir = Path.Combine(repoRoot, "docs", "balance", "derived");
			Assert.That(Directory.Exists(derivedDir), Is.True, $"fixture dir missing: {derivedDir}");

			var failures = new List<string>();
			var weaponModels = new Dictionary<string, BotWeaponModel>(StringComparer.Ordinal);
			var weaponErrors = new Dictionary<string, string>(StringComparer.Ordinal);
			var weapons = new HashSet<string>(StringComparer.Ordinal);
			var rows = 0;
			var unmodelled = 0;
			var skippedNoWeapon = 0;

			foreach (var file in Directory.GetFiles(derivedDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
			{
				var fileName = Path.GetFileName(file);
				if (fileName.StartsWith("_", StringComparison.Ordinal))
					continue;

				using var doc = JsonDocument.Parse(File.ReadAllText(file));
				if (!doc.RootElement.TryGetProperty("sections", out var sections) ||
					sections.ValueKind != JsonValueKind.Object)
					continue;

				foreach (var section in sections.EnumerateObject())
				{
					if (section.Value.ValueKind != JsonValueKind.Object)
						continue;

					foreach (var actor in section.Value.EnumerateObject())
					{
						if (actor.Value.ValueKind != JsonValueKind.Object ||
							!actor.Value.TryGetProperty("armaments", out var armaments) ||
							armaments.ValueKind != JsonValueKind.Array)
							continue;

						foreach (var arm in armaments.EnumerateArray())
						{
							if (arm.ValueKind != JsonValueKind.Object)
								continue;

							var slot = arm.TryGetProperty("slot", out var s) && s.ValueKind == JsonValueKind.String
								? s.GetString()
								: "?";
							var weapon = arm.TryGetProperty("weapon", out var w) && w.ValueKind == JsonValueKind.String
								? w.GetString()
								: null;
							if (weapon == null)
							{
								skippedNoWeapon++;
								continue;
							}

							rows++;
							weapons.Add(weapon);
							var id = $"{fileName}|{actor.Name}|{slot}|{weapon}";

							// Resolve the model once per weapon; a throw or a null is a
							// divergence (spec §6: "a weapon that raises in either
							// implementation = FAIL").
							BotWeaponModel m;
							string error = null;
							if (weaponErrors.TryGetValue(weapon, out var cachedError))
							{
								m = null;
								error = cachedError;
							}
							else if (!weaponModels.TryGetValue(weapon, out m))
							{
								try
								{
									m = table.Compute(weapon);
								}
								catch (Exception e)
								{
									m = null;
									error = $"table.Compute threw {e.GetType().Name}: {e.Message}";
								}

								if (m is null)
									weaponErrors[weapon] = error ??= "table.Compute returned null";
								else
									weaponModels[weapon] = m;
							}

							if (error != null)
							{
								failures.Add($"{id}: {error}");
								continue;
							}

							// Unmodelled weapon rows: the fixture omits effective_per_shot.
							// Every observed case also carries model_status "provisional" —
							// skip the numeric compare (Compute above already proved no
							// crash). Any other status is an anomaly worth flagging.
							var hasEffectivePerShot =
								arm.TryGetProperty("effective_per_shot", out var eps) &&
								eps.ValueKind == JsonValueKind.Number;
							if (!hasEffectivePerShot)
							{
								unmodelled++;
								var status = arm.TryGetProperty("model_status", out var st) &&
									st.ValueKind == JsonValueKind.String
										? st.GetString()
										: null;
								if (status != "provisional")
									failures.Add($"{id}: no effective_per_shot and model_status={status ?? "absent"}");
								continue;
							}

							foreach (var f in Fields)
							{
								var present =
									arm.TryGetProperty(f.Name, out var el) &&
									el.ValueKind == JsonValueKind.Number;
								var modelValue = f.Get(m);
								if (present)
								{
									var fixtureValue = el.GetDouble();
									if (modelValue is null)
										failures.Add($"{id}|{f.Name}: fixture {fixtureValue:G17} vs model null");
									else if (!Near(modelValue.Value, fixtureValue, f.Abs, f.Rel))
										failures.Add($"{id}|{f.Name}: fixture {fixtureValue:G17} vs model {modelValue.Value:G17}");
								}
								else if (!f.Conditional && modelValue is not null && Math.Abs(modelValue.Value) > 1e-9)
								{
									failures.Add($"{id}|{f.Name}: fixture absent vs model {modelValue.Value:G17}");
								}
							}
						}
					}
				}
			}

			Assert.That(rows, Is.GreaterThan(0), "no armament rows found in docs/balance/derived/*.json");
			TestContext.Out.WriteLine(
				$"{rows} rows, {weapons.Count} weapons, {unmodelled} unmodelled, " +
				$"{skippedNoWeapon} no-weapon, {failures.Count} failures");
			Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(50)));
		}

		[Test]
		public void ChargeUpRecordsMatchFixture()
		{
			var balanceDir = Path.Combine(repoRoot, "docs", "balance");
			Assert.That(Directory.Exists(balanceDir), Is.True, $"fixture dir missing: {balanceDir}");

			var failures = new List<string>();
			var rows = 0;

			// Top-level faction files only — derived/ is a subdirectory, non-faction
			// json files are skipped by the "sections" shape check.
			foreach (var file in Directory.GetFiles(balanceDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
			{
				JsonDocument doc;
				try
				{
					doc = JsonDocument.Parse(File.ReadAllText(file));
				}
				catch (JsonException)
				{
					continue;
				}

				using (doc)
				{
					if (!doc.RootElement.TryGetProperty("sections", out var sections) ||
						sections.ValueKind != JsonValueKind.Object)
						continue;

					foreach (var section in sections.EnumerateObject())
					{
						if (section.Value.ValueKind != JsonValueKind.Object)
							continue;

						foreach (var actor in section.Value.EnumerateObject())
						{
							// charge_up lives at ACTOR level (sibling of armaments).
							if (actor.Value.ValueKind != JsonValueKind.Object ||
								!actor.Value.TryGetProperty("charge_up", out var cu) ||
								cu.ValueKind != JsonValueKind.Object)
								continue;

							rows++;
							var actorName = actor.Name;
							var id = $"{Path.GetFileName(file)}|{actorName}|charge_up";

							var c = CallOrNull(() => table.ChargeUpFor(actorName), out var chargeError);
							if (chargeError != null)
							{
								failures.Add($"{id}: ChargeUpFor {chargeError}");
								continue;
							}

							if (cu.TryGetProperty("v", out var v) && v.ValueKind == JsonValueKind.String)
							{
								var expected = v.GetString();
								var actual = c.Trait?.ToString();
								if (!string.Equals(actual, expected, StringComparison.Ordinal))
									failures.Add($"{id}|v: fixture {expected} vs model {actual}");
							}

							CheckCharge(cu, "ticks", c.Ticks, failures, id);
							CheckCharge(cu, "cycle_reload", c.CycleReload, failures, id);
							CheckCharge(cu, "burst", c.Burst, failures, id);
							CheckCharge(cu, "charge_delay", c.ChargeDelay, failures, id);
						}
					}
				}
			}

			Assert.That(rows, Is.GreaterThan(0), "no charge_up records found in docs/balance/*.json");
			TestContext.Out.WriteLine($"{rows} charge_up rows, {failures.Count} failures");
			Assert.That(failures, Is.Empty, string.Join("\n", failures.Take(50)));
		}

		// Invoke a model getter; a throw or a null result is a divergence, returned
		// through 'error' so the caller can collect it like any other mismatch.
		// Generic so the charge record type stays inferred (member names pinned).
		static T CallOrNull<T>(Func<T> f, out string error)
		{
			try
			{
				var r = f();
				error = r is null ? "returned null" : null;
				return r;
			}
			catch (Exception e)
			{
				error = $"threw {e.GetType().Name}: {e.Message}";
				return default;
			}
		}

		static void CheckCharge(JsonElement cu, string name, double? actual, List<string> failures, string id, double tol = 0.01)
		{
			var present = cu.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number;
			if (present)
			{
				var expected = el.GetDouble();
				if (actual is null)
					failures.Add($"{id}|{name}: fixture {expected:G17} vs model null");
				else if (Math.Abs(actual.Value - expected) > tol)
					failures.Add($"{id}|{name}: fixture {expected:G17} vs model {actual.Value:G17}");
			}
			else if (actual is not null && Math.Abs(actual.Value) > tol)
			{
				// Fixture omits the field; a nonzero model value is a divergence.
				failures.Add($"{id}|{name}: fixture absent vs model {actual.Value:G17}");
			}
		}

		[Test]
		public void ScatterPdfMatchesPythonTable()
		{
			// Seam: BotEffectiveDamage.ScatterPdf (IReadOnlyList<double> bin
			// densities) + BotEffectiveDamage.ScatterBinWidth.
			var pdf = BotEffectiveDamage.ScatterPdf;
			Assert.That(pdf, Is.Not.Null, "BotEffectiveDamage.ScatterPdf is null");
			Assert.That(pdf.Count, Is.EqualTo(256), "scatter pdf bin count");

			var width = BotEffectiveDamage.ScatterBinWidth;
			Assert.That(width, Is.EqualTo(Math.Sqrt(2.0) / 256).Within(1e-12), "scatter bin width");
			Assert.That(pdf.Sum() * width, Is.EqualTo(1.0).Within(1e-6), "scatter pdf total probability");

			// Pinned Python bins from tools/balance/effective_damage.py
			// _build_radial_pdf(bins=256, samples=400_000) under random.Random(20260811).
			// A faithful MT19937/CPython-random port reproduces these bit-for-bit.
			Assert.That(pdf[0], Is.EqualTo(0.02172232031805074).Within(1e-9));
			Assert.That(pdf[64], Is.EqualTo(1.335470151220161).Within(1e-9));
			Assert.That(pdf[128], Is.EqualTo(1.1205096897394506).Within(1e-9));
			Assert.That(pdf[192], Is.EqualTo(0.13847979202757346).Within(1e-9));
			Assert.That(pdf[255], Is.EqualTo(0.0).Within(1e-9));
		}
	}
}
