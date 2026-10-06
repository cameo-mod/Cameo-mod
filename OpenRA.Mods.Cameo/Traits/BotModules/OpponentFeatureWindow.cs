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
		public long ArmyValue { get; }
		public long RoleValueTotal { get; }

		public OpponentFeatureCensus(int tick, int lastVisibleTick, int observedActorSamples,
			int coveragePermille, int rememberedEconomyValue, int rememberedProductionValue,
			IEnumerable<int> rememberedRoleValues)
		{
			var roles = rememberedRoleValues.Take(8).ToArray();
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
			RoleValueTotal = roles.Sum(v => (long)v);
			ArmyValue = RoleValueTotal;
		}
	}

	/// <summary>Capped 750-tick census history. Hidden units are never removed here because they left sight.</summary>
	public sealed class OpponentFeatureWindow
	{
		public const int MinimumActorSamples = 8;
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
			var evidence = Math.Min(1000, ContextFeatureEncoding.Ratio(ContextFeatureEncoding.Saturate(actorSamples), MinimumActorSamples));
			var recency = Math.Max(0, 1000 - ContextFeatureEncoding.Ratio(age, MacroWindow));
			var confidence = ContextFeatureEncoding.Ratio((long)evidence * last.CoveragePermille, 1000, 1);
			confidence = ContextFeatureEncoding.Ratio((long)confidence * recency, 1000, 1);
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
				var midpoint = ((long)recent[0].Tick + recent[^1].Tick) / 2;
				var firstHalf = recent.Where(s => s.Tick <= midpoint).ToArray();
				var secondHalf = recent.Where(s => s.Tick > midpoint).ToArray();
				if (firstHalf.Length >= 2 && secondHalf.Length >= 2 &&
					firstHalf[^1].Tick - firstHalf[0].Tick >= SampleInterval &&
					secondHalf[^1].Tick - secondHalf[0].Tick >= SampleInterval)
				{
					var firstRate = ContextFeatureEncoding.SlopeRatio(firstHalf.Select(s => (s.Tick, s.ArmyValue)).ToArray());
					var secondRate = ContextFeatureEncoding.SlopeRatio(secondHalf.Select(s => (s.Tick, s.ArmyValue)).ToArray());
					var span = ((long)secondHalf[0].Tick + secondHalf[^1].Tick - firstHalf[0].Tick - firstHalf[^1].Tick) / 2;
					Set("army_value_acceleration", ContextFeatureEncoding.Saturate((secondRate.Numerator * firstRate.Denominator - firstRate.Numerator * secondRate.Denominator) * 1000 /
						(firstRate.Denominator * secondRate.Denominator * span)));
				}
				var macro = samples.Where(s => tick - s.Tick <= MacroWindow).ToArray();
				Set("visible_econ_growth_per_1000t", ContextFeatureEncoding.Slope(macro.Select(s => (s.Tick, s.RememberedEconomyValue)).ToArray()));
				Set("visible_production_growth_per_1000t", ContextFeatureEncoding.Slope(macro.Select(s => (s.Tick, s.RememberedProductionValue)).ToArray()));
				Set("role_entropy_permille", ContextFeatureEncoding.Entropy(last.RememberedRoleValues));
				Set("military_to_econ_permille", ContextFeatureEncoding.Ratio(last.RoleValueTotal, last.RememberedEconomyValue));
				for (var i = 0; i < RoleFields.Length; i++)
					Set(RoleFields[i], ContextFeatureEncoding.Ratio(last.RememberedRoleValues[i], last.RoleValueTotal));
				Set("air_share_permille", ContextFeatureEncoding.Ratio(last.RememberedRoleValues[2], last.RoleValueTotal));
				Set("naval_share_permille", ContextFeatureEncoding.Ratio(last.RememberedRoleValues[3], last.RoleValueTotal));
			}

			// Response, micro and style metrics need their real visible event denominators.
			// A census cannot turn an unobserved response, command, expansion or bank into measured zero.
			return cached = new(ContextFeatureKind.Opponent, fields);
		}
	}
}
