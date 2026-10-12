from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
h=(r/'OpenRA.Mods.Cameo.Test/ZoneTopologyTest.cs').read_text().split('using ')[0]
(r/'OpenRA.Mods.Cameo/Traits/BotModules/OpponentFeatureWindow.cs').write_text(h+'''using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Anonymous census supplied by the observed-side buffer, never by a truth-state reader.</summary>
	public sealed class OpponentFeatureCensus
	{
		public int Tick { get; }
		public int LastVisibleTick { get; }
		public int ObservedActorSamples { get; }
		public int CoveragePermille { get; }
		public int RememberedEconomyValue { get; }
		public int RememberedProductionValue { get; }
		public IReadOnlyList<int> RememberedRoleValues { get; }
		public int ArmyValue { get; }

		public OpponentFeatureCensus(int tick, int lastVisibleTick, int observedActorSamples,
			int coveragePermille, int rememberedEconomyValue, int rememberedProductionValue,
			IEnumerable<int> rememberedRoleValues)
		{
			var roles = rememberedRoleValues.ToArray();
			if (tick < 0 || lastVisibleTick < 0 || lastVisibleTick > tick || observedActorSamples < 0 ||
				coveragePermille < 0 || coveragePermille > 1000 || rememberedEconomyValue < 0 ||
				rememberedProductionValue < 0 || roles.Length != 7 || roles.Any(v => v < 0))
				throw new ArgumentException("Invalid observed census.");
			Tick = tick;
			LastVisibleTick = lastVisibleTick;
			ObservedActorSamples = observedActorSamples;
			CoveragePermille = coveragePermille;
			RememberedEconomyValue = rememberedEconomyValue;
			RememberedProductionValue = rememberedProductionValue;
			RememberedRoleValues = Array.AsReadOnly(roles);
			ArmyValue = ContextFeatureEncoding.Saturate(roles.Aggregate(System.Numerics.BigInteger.Zero, (a, b) => a + b));
		}
	}

	/// <summary>Capped 750-tick census history. Hidden units are never removed here because they left sight.</summary>
	public sealed class OpponentFeatureWindow
	{
		public const int SampleInterval = 750;
		public const int UpdateInterval = 250;
		public const int MicroWindow = 3000;
		public const int ArmyWindow = 7500;
		public const int MacroWindow = 15000;
		public const int MaximumSamples = MacroWindow / SampleInterval + 1;
		static readonly string[] RoleFields =
		[
			"role_share_infantry", "role_share_vehicle", "role_share_air", "role_share_naval",
			"role_share_defence", "role_share_artillery", "role_share_other"
		];
		readonly List<OpponentFeatureCensus> samples = [];
		int lastUpdate = -1;
		ContextFeatureVector cached = new(ContextFeatureKind.Opponent);
		public int SampleCount => samples.Count;

		public bool Add(OpponentFeatureCensus sample)
		{
			ArgumentNullException.ThrowIfNull(sample);
			if (samples.Count != 0)
			{
				var elapsed = sample.Tick - samples[^1].Tick;
				if (elapsed < 0)
					throw new ArgumentException("Census ticks must be monotonic.", nameof(sample));
				if (elapsed < SampleInterval)
					return false;
			}

			samples.Add(sample);
			samples.RemoveAll(s => sample.Tick - s.Tick > MacroWindow);
			while (samples.Count > MaximumSamples)
				samples.RemoveAt(0);
			return true;
		}

		public ContextFeatureVector Features(int tick)
		{
			if (tick < 0 || (samples.Count != 0 && tick < samples[^1].Tick) || tick < lastUpdate)
				throw new ArgumentOutOfRangeException(nameof(tick));
			if (lastUpdate >= 0 && tick - lastUpdate < UpdateInterval)
				return cached;
			lastUpdate = tick;
			var fields = new SortedDictionary<string, ContextFeatureMeasurement>(StringComparer.Ordinal);
			if (samples.Count == 0)
				return cached = new(ContextFeatureKind.Opponent);

			var last = samples[^1];
			var age = tick - last.LastVisibleTick;
			var actorSamples = samples.Where(s => tick - s.Tick <= MacroWindow)
				.Aggregate(System.Numerics.BigInteger.Zero, (a, b) => a + b.ObservedActorSamples);
			var evidence = Math.Min(1000, ContextFeatureEncoding.Ratio(ContextFeatureEncoding.Saturate(actorSamples), 8));
			var recency = Math.Max(0, 1000 - ContextFeatureEncoding.Ratio(age, MacroWindow));
			var confidence = ContextFeatureEncoding.Ratio((long)evidence * last.CoveragePermille, 1000);
			confidence = ContextFeatureEncoding.Ratio((long)confidence * recency, 1000);
			fields.Add("seen_actor_samples", ContextFeatureMeasurement.Observed(ContextFeatureEncoding.Saturate(actorSamples)));
			fields.Add("observation_age_ticks", ContextFeatureMeasurement.Observed(age));
			fields.Add("coverage_permille", ContextFeatureMeasurement.Observed(last.CoveragePermille));
			fields.Add("confidence_permille", ContextFeatureMeasurement.Observed(confidence));
			var recent = samples.Where(s => tick - s.Tick <= ArmyWindow).ToArray();
			// A 750-tick minimum spans observations; two samples at the same time cannot satisfy it.
			if (recent.Length >= 2 && recent[^1].Tick - recent[0].Tick >= SampleInterval && confidence > 0)
			{
				void Set(string name, int value) => fields.Add(name, ContextFeatureMeasurement.Observed(value, confidence));
				Set("army_value_slope", ContextFeatureEncoding.Slope(recent.Select(s => (s.Tick, s.ArmyValue)).ToArray()));
				var macro = samples.Where(s => tick - s.Tick <= MacroWindow).ToArray();
				Set("visible_econ_growth_per_1000t", ContextFeatureEncoding.Slope(macro.Select(s => (s.Tick, s.RememberedEconomyValue)).ToArray()));
				Set("visible_production_growth_per_1000t", ContextFeatureEncoding.Slope(macro.Select(s => (s.Tick, s.RememberedProductionValue)).ToArray()));
				Set("military_to_econ_permille", ContextFeatureEncoding.Ratio(last.ArmyValue, last.RememberedEconomyValue));
				for (var i = 0; i < RoleFields.Length; i++)
					Set(RoleFields[i], ContextFeatureEncoding.Ratio(last.RememberedRoleValues[i], last.ArmyValue));
				Set("air_share_permille", ContextFeatureEncoding.Ratio(last.RememberedRoleValues[2], last.ArmyValue));
				Set("naval_share_permille", ContextFeatureEncoding.Ratio(last.RememberedRoleValues[3], last.ArmyValue));
			}

			// Response, micro and style metrics need their real visible event denominators.
			// A census cannot turn an unobserved response, command, expansion or bank into measured zero.
			return cached = new(ContextFeatureKind.Opponent, fields);
		}
	}
}
''')
