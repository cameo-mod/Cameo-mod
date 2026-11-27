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
	/// strength.PROFILE, floor, ramp, firstboost|firstramp|firstcap, corefocus.share|count|max|bank (R2 only), techlean.top|bank|start (R4 admission only; start = the ramp unless overridden). An override replaces the yaml value; clearing it restores the yaml value.
	/// </summary>
	public sealed class UnitRollTable
	{
		public const string FloorKey = "floor";
		public const string RampKey = "ramp";
		public const string FirstBoostKey = "firstboost";
		public const string FirstRampKey = "firstramp";
		public const string FirstCapKey = "firstcap";
		public const string FirstDueKey = "firstdue";
		public const string FirstDueCashKey = "firstduecash";

		// CONSOLIDATE-03 P5 a2 (addendum 1, user ruling 11-26): the tech-up demand multipliers become named inputs of the table. corefocus.* is read by the roll's weights step (R2, the core lift) ONLY;
		// techlean.* by the admission step (R4, the lean predicate) ONLY. The values are the yaml (Hans) values = the PRIOR; a learner override (LEARN-MULT-01, DEV only) replaces them; a release reads the accepted snapshot.
		public const string CoreShareKey = "corefocus.share";
		public const string CoreCountKey = "corefocus.count";
		public const string CoreMaxKey = "corefocus.max";
		public const string CoreBankKey = "corefocus.bank";
		public const string TechTopKey = "techlean.top";
		public const string TechBankKey = "techlean.bank";

		// CONSOLIDATE-03 P5 (b) (addendum 3, row ub_ramp_split as a table key, no switch): the tick from which the tech lean may admit a late pick, apart from the ramp of the personality bias. With no override it IS the ramp
		// (the same number, read through the property so a ramp override still moves it: behaviour equal to before the split); a learner (LEARN-MULT-01, DEV only) may set it by itself. Read by R4 only.
		public const string TechStartKey = "techlean.start";

		/// <summary>Every value, yaml or learned override, is clamped to [0, MaxValue]: the table never holds a negative or runaway number.</summary>
		public const int MaxValue = 1_000_000;

		readonly IReadOnlyDictionary<string, int> fractions;
		readonly IReadOnlyDictionary<string, int> bias;
		readonly IReadOnlyDictionary<string, int> biasLate;
		readonly IReadOnlyDictionary<string, int> strengths;
		readonly int floorPct;
		readonly int rampTicks;
		readonly int firstBoost;
		readonly int firstRampTicks;
		readonly int firstCap;
		readonly int coreShare;
		readonly int coreCount;
		readonly int coreMax;
		readonly int coreBank;
		readonly int techTop;
		readonly int techBank;
		readonly int firstDueTicks;
		readonly int firstDueCash;
		readonly Dictionary<string, int> overrides = new(StringComparer.Ordinal);

		public UnitRollTable(IReadOnlyDictionary<string, int> fractions, IReadOnlyDictionary<string, int> bias, IReadOnlyDictionary<string, int> biasLate,
			IReadOnlyDictionary<string, int> strengths, int floorPct, int rampTicks, int firstBoost = 8, int firstRampTicks = 3000, int firstCap = 64,
			int coreShare = 0, int coreCount = 3, int coreMax = 800, int coreBank = 30, int techTop = 0, int techBank = 30, int firstDueTicks = 3000, int firstDueCash = 0)
		{
			this.coreShare = coreShare;
			this.coreCount = coreCount;
			this.coreMax = coreMax;
			this.coreBank = coreBank;
			this.techTop = techTop;
			this.techBank = techBank;
			this.firstDueTicks = firstDueTicks;
			this.firstDueCash = firstDueCash;
			this.fractions = fractions;
			this.bias = bias;
			this.biasLate = biasLate;
			this.strengths = strengths;
			this.floorPct = floorPct;
			this.rampTicks = rampTicks;
			this.firstBoost = firstBoost;
			this.firstRampTicks = firstRampTicks;
			this.firstCap = firstCap;
		}

		static int Clamp(int v) => Math.Clamp(v, 0, MaxValue);

		public static string FractionKey(string unit) => "fraction." + unit;
		public static string BiasKey(string personality, string role) => "bias." + personality + "." + role;
		public static string BiasLateKey(string personality, string role) => "biaslate." + personality + "." + role;
		public static string StrengthKey(string profile) => "strength." + profile;

		/// <summary>The learner's overrides, by key (read only).</summary>
		public IReadOnlyDictionary<string, int> Overrides => overrides;

		public void Override(string key, int value) => overrides[key] = Clamp(value);

		public void ClearOverride(string key) => overrides.Remove(key);

		public void ClearOverrides() => overrides.Clear();

		/// <summary>The base fraction of a listed type (0 = not listed).</summary>
		public int Fraction(string unit) => Read(FractionKey(unit), fractions, unit, out var v) ? Clamp(v) : 0;

		/// <summary>The strength percent of a mode bias profile (0 = off).</summary>
		public int Strength(string profile) => Read(StrengthKey(profile), strengths, profile, out var v) ? Clamp(v) : 0;

		public int FloorPct => Clamp(overrides.TryGetValue(FloorKey, out var v) ? v : floorPct);

		public int RampTicks => Clamp(overrides.TryGetValue(RampKey, out var v) ? v : rampTicks);

		/// <summary>The first-build weight multiplier of a buildable, never-built type (UnitRoll.FirstBoost): the base value, the ticks per doubling and the cap. Learnable like every other number.</summary>
		public int FirstBoost => Clamp(overrides.TryGetValue(FirstBoostKey, out var v) ? v : firstBoost);

		public int FirstRampTicks => Clamp(overrides.TryGetValue(FirstRampKey, out var v) ? v : firstRampTicks);

		public int FirstCap => Clamp(overrides.TryGetValue(FirstCapKey, out var v) ? v : firstCap);

		/// <summary>The core lift's inputs (R2 only): the target share (percent of the weight of the types issued once), how many types form the core, the multiplier cap (percent), the bank share the core must be affordable with. Prior = the yaml values.</summary>
		public int CoreFocusSharePct => Clamp(overrides.TryGetValue(CoreShareKey, out var v) ? v : coreShare);

		public int CoreFocusCount => Clamp(overrides.TryGetValue(CoreCountKey, out var v) ? v : coreCount);

		public int CoreFocusMaxPct => Clamp(overrides.TryGetValue(CoreMaxKey, out var v) ? v : coreMax);

		public int CoreFocusBankPct => Clamp(overrides.TryGetValue(CoreBankKey, out var v) ? v : coreBank);

		/// <summary>The tech lean's inputs (R4 only, the admission predicate): the top share of the pool's dearest price a pick must reach, and the bank share it must be covered by. Prior = the yaml values.</summary>
		public int TechLeanTopPct => Clamp(overrides.TryGetValue(TechTopKey, out var v) ? v : techTop);

		public int TechLeanBankPct => Clamp(overrides.TryGetValue(TechBankKey, out var v) ? v : techBank);

		/// <summary>The tick from which the tech lean admits (R4 only). Prior = the ramp tick (the same number); a learner override of techlean.start separates the two.</summary>
		public int TechLeanStartTicks => Clamp(overrides.TryGetValue(TechStartKey, out var v) ? v : RampTicks);

		/// <summary>FIRST-COPY-DUE-01 (learned, prior 3000 = two minutes at 25 ticks per second): the credited ticks after which a never-ordered type is due. 0 = no deadline.</summary>
		public int FirstDueTicks => Clamp(overrides.TryGetValue(FirstDueKey, out var v) ? v : firstDueTicks);

		/// <summary>FIRST-COPY-DUE-01 (learned, prior 0): the bank at or below which the player counts as having no cash (the due clock pauses only then AND without income).</summary>
		public int FirstDueCash => Clamp(overrides.TryGetValue(FirstDueCashKey, out var v) ? v : firstDueCash);

		/// <summary>Percent bias of a role for a personality at a tick (same rule as UnitRoll.RoleBiasPct); 100 = neutral.</summary>
		public int BiasPct(string personality, string role, int tick)
		{
			if (string.IsNullOrEmpty(personality) || string.IsNullOrEmpty(role))
				return 100;

			var key = personality + "." + role;
			var early = Read(BiasKey(personality, role), bias, key, out var e) ? Clamp(e) : 100;
			var ramp = RampTicks;
			if (ramp <= 0 || !Read(BiasLateKey(personality, role), biasLate, key, out var late))
				return early;

			late = Clamp(late);
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
