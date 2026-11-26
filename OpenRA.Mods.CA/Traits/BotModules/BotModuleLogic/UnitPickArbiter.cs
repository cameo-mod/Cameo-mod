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
using System.Globalization;
using System.Text;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// CONSOLIDATE-03 P5 (unit pick), step (a): the decision record and the DEV equivalence trace of the one unit pick (design 02 4c19e5f9 + addenda 1 and 2).
	/// Pure text and integer math: no game state, no random draw. The module reads its own state (the pool, the Quantize units, the RNG's draw counter) and hands the values to these formatters;
	/// the trace rows of an old and a new build must be equal (same seed, same switches), so a change of a bound, a bin, a refused redraw or an extra draw shows as a diff.
	/// Rule names follow the arbiter order of design 02 section 3: R1 legal set (A1, A2), R2 weights, R3 the draw, R4 post-draw gates and the one admission list (paygo, lean, full).
	/// </summary>
	public static class UnitPickArbiter
	{
		public const string RuleEmptyPool = "R1_empty";
		public const string RuleDraw = "R3_draw";
		public const string RuleGate = "R4_gate";
		public const string RuleAdmitPaygo = "R4_paygo";
		public const string RuleAdmitLean = "R4_lean";
		public const string RuleAdmitFull = "R4_full";
		public const string RuleNoPick = "R3_none";

		/// <summary>The decision of one roll: the picked index, the integer units and the final weights the draw used, the draw bound, the deciding rule.</summary>
		public readonly record struct PickDecision(int Pick, int[] Units, double[] Weights, int Bound, string Rule);

		/// <summary>
		/// R2, one candidate's bias in the FIXED order (design 02 section 3): the personality bias times the role-mix deficit, the learned prior, the wave-mix, the composition and the opening term, each applied
		/// only when present (null = the profile does not apply to this candidate). Same multiply-and-clamp steps, same order as the roll always had (UnitRoll.MulPct).
		/// </summary>
		public static int ComposeBias(int baseBias, int? deficit, int? learned, int? waveMix, int? composition, int? opening)
		{
			var bias = baseBias;
			if (deficit.HasValue)
				bias = UnitRoll.MulPct(bias, deficit.Value);

			if (learned.HasValue)
				bias = UnitRoll.MulPct(bias, learned.Value);

			if (waveMix.HasValue)
				bias = UnitRoll.MulPct(bias, waveMix.Value);

			if (composition.HasValue)
				bias = UnitRoll.MulPct(bias, composition.Value);

			if (opening.HasValue)
				bias = UnitRoll.MulPct(bias, opening.Value);

			return bias;
		}

		/// <summary>
		/// R2 + R3 of the unit pick: the weights (fraction x bias x counter x first boost, then the mean floor), then the core lift as the LAST step on the weights (null = the row is off), then the integer
		/// units (UnitRoll.Quantize: relative floor, every candidate owns a drawable interval), then the ONE draw (<paramref name="draw"/> is the module's own RNG stream, called exactly once with the
		/// bound) and the pick. No other random call, no game state.
		/// </summary>
		public static PickDecision Decide(IReadOnlyList<int> fractions, IReadOnlyList<int> biases, IReadOnlyList<int> counterPcts, int floorPct, IReadOnlyList<double> boosts,
			Func<double[], double[]> coreLift, Func<int, int> draw)
		{
			var weights = UnitRoll.Weights(fractions, biases, counterPcts, floorPct, boosts);
			if (coreLift != null)
				weights = coreLift(weights);

			var units = UnitRoll.Quantize(weights); // integer units, sum <= 2^30, relative floor: every candidate owns a drawable interval
			var bound = UnitRoll.Sum(units);
			var pick = UnitRoll.Pick(units, draw(bound));
			return new PickDecision(pick, units, weights, bound, RuleDraw);
		}

		public const string ResultAccept = "accept";
		public const string ResultRefuse = "refuse";
		public const string ResultNone = "none";

		/// <summary>One draw of the roll (Addendum 2 gaps A and B: the quantized integer bins, the hidden state the pick reads or writes).</summary>
		public readonly record struct DrawTrace(
			int Ordinal, int Fill, int Attempt, uint Producer, string Queue, int PoolCount, string Bins, long Bound,
			int Raw, int RngTotal, string Pick, bool First, int PoolMaxBefore, int PoolMax, int BankLeft, string Excluded, string Roles, int AvailCount, string AvailDigest, string AvailNew, string Rule);

		/// <summary>One empty roll: no candidate left after the legal set (design 02 R1).</summary>
		public readonly record struct EmptyTrace(int Fill, int Attempt, uint Producer, string Queue, string Excluded, string Roles, int RngTotal, string Rule);

		/// <summary>The outcome after a draw (Addendum 2 gap C: batch cost, headroom, gate cost): accept (an order started) or refuse (a post-draw gate said no) with the deciding rule.</summary>
		public readonly record struct FillTrace(
			int Fill, int Attempt, uint Producer, string Queue, string Result, string Rule, string Why, string Name, int Held,
			int GateCost, int FullCost, int BatchBank, int BatchReserved, int BatchBankLeft, string Headroom, bool First, int OrderSeq);

		/// <summary>The cap headroom the batch holds for a type ("na" while no cap entry or not yet counted).</summary>
		public static string HeadroomText(IReadOnlyDictionary<string, int> headroom, string name) =>
			name != null && headroom.TryGetValue(name, out var h) ? h.ToString(CultureInfo.InvariantCulture) : "na";

		/// <summary>One fill (a queue pass) ended: the RNG state, the order count.</summary>
		public readonly record struct FillEndTrace(int Fill, uint Producer, int Orders, int RngTotal, int RngLast);

		/// <summary>The ordered (name, integer unit) list the draw used, "name:units|name:units"; lossless (Addendum 2 gap B).</summary>
		public static string Bins(IReadOnlyList<string> names, IReadOnlyList<int> units)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < names.Count; i++)
			{
				if (i > 0)
					sb.Append('|');

				sb.Append(names[i]).Append(':').Append(units[i].ToString(CultureInfo.InvariantCulture));
			}

			return sb.ToString();
		}

		/// <summary>FNV-1a of a canonical text as 8 hex digits (the same salt hash the bot RNG uses).</summary>
		public static string Digest(string text) => BotRng.Fnv1a(text ?? string.Empty).ToString("x8", CultureInfo.InvariantCulture);

		/// <summary>"name:tick|name:tick" in ordinal name order: the first-sighting ticks (rollAvailableSince, an add-only table: a write is a new name).</summary>
		public static string Avail(IReadOnlyDictionary<string, int> since)
		{
			var names = new List<string>(since.Keys);
			names.Sort(StringComparer.Ordinal);
			var sb = new StringBuilder();
			foreach (var name in names)
			{
				if (sb.Length > 0)
					sb.Append('|');

				sb.Append(name).Append(':').Append(since[name].ToString(CultureInfo.InvariantCulture));
			}

			return sb.ToString();
		}

		public static string DrawDetail(in DrawTrace d) => string.Create(CultureInfo.InvariantCulture,
			$"ord={d.Ordinal};fill={d.Fill};att={d.Attempt};prod={d.Producer};n={d.PoolCount};bins={d.Bins};bound={d.Bound};raw={d.Raw};tot={d.RngTotal};first={(d.First ? 1 : 0)};pmb={d.PoolMaxBefore};pm={d.PoolMax};bank={d.BankLeft};excl={d.Excluded};roles={d.Roles};avail={d.AvailCount}/{d.AvailDigest};availnew={d.AvailNew};rule={d.Rule}");

		public static string EmptyDetail(in EmptyTrace e) => string.Create(CultureInfo.InvariantCulture,
			$"fill={e.Fill};att={e.Attempt};prod={e.Producer};excl={e.Excluded};roles={e.Roles};tot={e.RngTotal};rule={e.Rule}");

		public static string FillDetail(in FillTrace f) => string.Create(CultureInfo.InvariantCulture,
			$"fill={f.Fill};att={f.Attempt};prod={f.Producer};res={f.Result};rule={f.Rule};why={f.Why};held={f.Held};gate={f.GateCost};full={f.FullCost};bank={f.BatchBank};reserved={f.BatchReserved};left={f.BatchBankLeft};headroom={f.Headroom};first={(f.First ? 1 : 0)};order={f.OrderSeq}");

		public static string FillEndDetail(in FillEndTrace e) => string.Create(CultureInfo.InvariantCulture,
			$"fill={e.Fill};prod={e.Producer};orders={e.Orders};tot={e.RngTotal};last={e.RngLast}");

		/// <summary>The "admission" rule name of an order that was accepted, in the old subreason order: pay-as-you-go first build, then the tech lean, then the full price.</summary>
		public static string AdmitRule(bool paygo, bool leaned) => paygo ? RuleAdmitPaygo : leaned ? RuleAdmitLean : RuleAdmitFull;
	}
}
