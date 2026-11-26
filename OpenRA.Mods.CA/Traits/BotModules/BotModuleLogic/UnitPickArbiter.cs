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

		public const string ResultAccept = "accept";
		public const string ResultRefuse = "refuse";
		public const string ResultNone = "none";

		/// <summary>One draw of the roll (Addendum 2 gaps A and B: the quantized integer bins, the hidden state the pick reads or writes).</summary>
		public readonly record struct DrawTrace(
			int Ordinal, int Fill, int Attempt, uint Producer, string Queue, int PoolCount, string Bins, long Bound,
			int Raw, int RngTotal, string Pick, bool First, int PoolMax, int BankLeft, string Excluded, string Roles, int AvailCount, string AvailDigest, string Rule);

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
		public readonly record struct FillEndTrace(int Fill, uint Producer, string Queue, int Orders, int RngTotal, int RngLast);

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
			$"ord={d.Ordinal};fill={d.Fill};att={d.Attempt};prod={d.Producer};n={d.PoolCount};bins={d.Bins};bound={d.Bound};raw={d.Raw};tot={d.RngTotal};first={(d.First ? 1 : 0)};pm={d.PoolMax};bank={d.BankLeft};excl={d.Excluded};roles={d.Roles};avail={d.AvailCount}/{d.AvailDigest};rule={d.Rule}");

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
