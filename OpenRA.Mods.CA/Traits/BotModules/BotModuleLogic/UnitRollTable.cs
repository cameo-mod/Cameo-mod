#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// UNIT-ALL-01 parameter table (user 11-25, LEARN-MULT-01 later): every number the unit roll multiplies lives here and nowhere else: the base fraction of each listed type, the personality
	/// bias (early and late), the ramp, the mode-profile strengths (the demand multipliers) and the floor. The yaml fills it; a learner reads any value and overrides it per player (one
	/// table per module instance) under a stable key, without touching the yaml or the roll. Keys: fraction.UNIT, bias.PERSONALITY.ROLE, biaslate.PERSONALITY.ROLE,
	/// strength.PROFILE, floor, ramp. An override replaces the yaml value; clearing it restores the yaml value.
	/// </summary>
	public sealed class UnitRollTable
	{
		public const string FloorKey = "floor";
		public const string RampKey = "ramp";

		readonly IReadOnlyDictionary<string, int> fractions;
		readonly IReadOnlyDictionary<string, int> bias;
		readonly IReadOnlyDictionary<string, int> biasLate;
		readonly IReadOnlyDictionary<string, int> strengths;
		readonly int floorPct;
		readonly int rampTicks;
		readonly Dictionary<string, int> overrides = new(StringComparer.Ordinal);

		public UnitRollTable(IReadOnlyDictionary<string, int> fractions, IReadOnlyDictionary<string, int> bias, IReadOnlyDictionary<string, int> biasLate,
			IReadOnlyDictionary<string, int> strengths, int floorPct, int rampTicks)
		{
			this.fractions = fractions;
			this.bias = bias;
			this.biasLate = biasLate;
			this.strengths = strengths;
			this.floorPct = floorPct;
			this.rampTicks = rampTicks;
		}

		public static string FractionKey(string unit) => "fraction." + unit;
		public static string BiasKey(string personality, string role) => "bias." + personality + "." + role;
		public static string BiasLateKey(string personality, string role) => "biaslate." + personality + "." + role;
		public static string StrengthKey(string profile) => "strength." + profile;

		/// <summary>The learner's overrides, by key (read only).</summary>
		public IReadOnlyDictionary<string, int> Overrides => overrides;

		public void Override(string key, int value) => overrides[key] = value;

		public void ClearOverride(string key) => overrides.Remove(key);

		public void ClearOverrides() => overrides.Clear();

		/// <summary>The base fraction of a listed type (0 = not listed).</summary>
		public int Fraction(string unit) => Read(FractionKey(unit), fractions, unit, out var v) ? v : 0;

		/// <summary>The strength percent of a mode bias profile (0 = off).</summary>
		public int Strength(string profile) => Read(StrengthKey(profile), strengths, profile, out var v) ? Math.Max(0, v) : 0;

		public int FloorPct => overrides.TryGetValue(FloorKey, out var v) ? v : floorPct;

		public int RampTicks => overrides.TryGetValue(RampKey, out var v) ? v : rampTicks;

		/// <summary>Percent bias of a role for a personality at a tick (same rule as UnitRoll.RoleBiasPct); 100 = neutral.</summary>
		public int BiasPct(string personality, string role, int tick)
		{
			if (string.IsNullOrEmpty(personality) || string.IsNullOrEmpty(role))
				return 100;

			var key = personality + "." + role;
			var early = Read(BiasKey(personality, role), bias, key, out var e) ? e : 100;
			var ramp = RampTicks;
			if (ramp <= 0 || !Read(BiasLateKey(personality, role), biasLate, key, out var late))
				return early;

			var t = Math.Clamp(tick, 0, ramp);
			return (int)(early + (long)(late - early) * t / ramp);
		}

		bool Read(string overrideKey, IReadOnlyDictionary<string, int> yaml, string yamlKey, out int value)
		{
			if (overrides.TryGetValue(overrideKey, out value))
				return true;

			value = 0;
			return yaml != null && yaml.TryGetValue(yamlKey, out value);
		}
	}
}
