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
	public enum RatioVerdict
	{
		/// <summary>No usable measure (no provider, no snapshot, or a stale one): the rule never holds.</summary>
		Inert,

		/// <summary>The ratio is satisfied: mobile army value >= 2 x (static defence value + this defence).</summary>
		Admit,

		/// <summary>Below the ratio, but the first copy of this defence type (Opus ruling O7): admitted, counted as exempt_first.</summary>
		ExemptFirst,

		/// <summary>Below the ratio and not a first copy: the FLOAT exemption decides (section 3 of ARMY_FIRST_02_DESIGN_04).</summary>
		NeedsFloat,

		/// <summary>Below the ratio, the unit side cannot absorb the money and the bank is floating: admitted, counted as exempt_float.</summary>
		ExemptFloat,

		/// <summary>Below the ratio with no exemption: the start waits (never cancelled); the sweep retries.</summary>
		Hold,
	}

	/// <summary>
	/// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_02 s2, _03, _04): the user's rule "mobile army is always 2x of static defences", pure and
	/// world-free. A defence START is admitted only if <c>M &gt;= 2 x (S + cost)</c> (M = live mobile army value, S = static defence
	/// value including queued and READY-held defences and the starts recorded in the last <see cref="RecordTicks"/> ticks), all in
	/// Valued cost. It is a priority rule, never a veto: nothing is cancelled, sold or removed, a held start is retried next sweep.
	/// Exemptions, first match wins (see <see cref="Stage1"/>): first copy of a defence type (O7), and FLOAT (unit side saturated AND
	/// the bank is floating, the bar being the tier's existing bank-saturation threshold, no new value). The factor 2 is a constant:
	/// no tier scaling and no yaml key. No RNG, no orders, no world access.
	/// </summary>
	public sealed class MobileStaticRatioCA
	{
		/// <summary>The ratio: mobile >= Ratio x static. Fixed at every tier (ladder values: none).</summary>
		public const int Ratio = 2;

		/// <summary>A recorded start counts toward S for this many ticks (order-latency cover and the same-sweep guard).</summary>
		public const int RecordTicks = 3;

		/// <summary>A situation snapshot older than this reads as "no measure": the rule is inert, never a hold.</summary>
		public const int SnapshotMaxAgeTicks = 300;

		/// <summary>Storage leg of FLOAT: stored resources at or above this percent of the storage capacity (money above the cap is wasted).</summary>
		public const int StoragePct = 90;

		readonly List<(int Tick, long Cost)> recorded = new();
		readonly HashSet<string> started = new(StringComparer.Ordinal);

		/// <summary>True when the bot has ever started a defence of this type through the gate (the first-copy ledger).</summary>
		public bool EverStarted(string type) => type != null && started.Contains(type);

		/// <summary>
		/// First copy of a defence type (Opus ruling O7): none owned or queued now AND never started by this bot. The caller passes
		/// <paramref name="ownedOrQueued"/> from the live building list and the defence queues, so a loaded game or a pre-existing
		/// building also counts as "has a copy".
		/// </summary>
		public bool IsFirstCopy(string type, bool ownedOrQueued) => !ownedOrQueued && !EverStarted(type);

		/// <summary>Record an admitted defence start: its cost counts toward S for <see cref="RecordTicks"/> ticks and its type is no longer a first copy.</summary>
		public void Record(int now, string type, long cost)
		{
			recorded.Add((now, Math.Max(0, cost)));
			if (type != null)
				started.Add(type);
		}

		/// <summary>Sum of the recorded start costs still inside the window at <paramref name="now"/>; expired and future entries are dropped.</summary>
		public long Recorded(int now)
		{
			long sum = 0;
			for (var i = recorded.Count - 1; i >= 0; i--)
			{
				var age = now - recorded[i].Tick;
				if (age < 0 || age >= RecordTicks)
					recorded.RemoveAt(i);
				else
					sum += recorded[i].Cost;
			}

			return sum;
		}

		/// <summary>The measure is usable: a snapshot exists and is not older than <see cref="SnapshotMaxAgeTicks"/> (a snapshot from the future reads as unusable).</summary>
		public static bool MeasureUsable(bool hasSnapshot, int snapshotTick, int now)
		{
			var age = now - snapshotTick;
			return hasSnapshot && age >= 0 && age <= SnapshotMaxAgeTicks;
		}

		/// <summary>
		/// The ratio test: <c>mobile &gt;= 2 x (staticTotal + cost)</c>. Negative inputs clamp to 0. Saturating: a value too large to
		/// multiply is never admitted (it cannot arise from real costs; the guard keeps the function total).
		/// </summary>
		public static bool Admits(long mobile, long staticTotal, long cost)
		{
			var need = Math.Max(0, staticTotal);
			var add = Math.Max(0, cost);
			if (need > long.MaxValue - add)
				return false;

			need += add;
			if (need > long.MaxValue / Ratio)
				return false;

			return mobile >= Ratio * need;
		}

		/// <summary>
		/// Money above the storage cap is wasted: stored resources at or above <see cref="StoragePct"/> percent of a positive capacity.
		/// </summary>
		public static bool StorageNearlyFull(long resources, long resourceCapacity) =>
			resourceCapacity > 0 && resources * 100 >= resourceCapacity * StoragePct;

		/// <summary>
		/// FLOAT leg (b) (ARMY_FIRST_02_DESIGN_04 s3): the storage is nearly full, OR the bank has reached the tier's bank-saturation
		/// bar (<see cref="ProductionSaturationCA.Saturated"/>: bar > 0 and bank >= bar; bar 0 = the saturation switch off or classic,
		/// only the storage leg then applies). Condition (a), the unit side being saturated, is tested separately by the caller.
		/// </summary>
		public static bool Floating(long resources, long resourceCapacity, long cashAndResources, int bankSaturationBar) =>
			StorageNearlyFull(resources, resourceCapacity) || ProductionSaturationCA.Saturated(cashAndResources, bankSaturationBar);

		/// <summary>
		/// Decision stages 1-3 (cheap, no unit-side scan): no measure = Inert; ratio satisfied = Admit (also for a first copy, so an
		/// exempt start is always a start BELOW the ratio); first copy of the type = ExemptFirst; otherwise NeedsFloat, and the caller
		/// completes with <see cref="Float"/> only then (the unit-side saturation is computed lazily).
		/// </summary>
		public static RatioVerdict Stage1(bool measured, long mobile, long staticTotal, long cost, bool firstCopyOfType)
		{
			if (!measured)
				return RatioVerdict.Inert;

			if (Admits(mobile, staticTotal, cost))
				return RatioVerdict.Admit;

			return firstCopyOfType ? RatioVerdict.ExemptFirst : RatioVerdict.NeedsFloat;
		}

		/// <summary>Stage 4: FLOAT needs BOTH the unit side saturated (a) and the bank floating (b); otherwise the start is held.</summary>
		public static RatioVerdict Float(bool unitSideSaturated, bool floating) =>
			unitSideSaturated && floating ? RatioVerdict.ExemptFloat : RatioVerdict.Hold;

		/// <summary>Stage 4 for a caller that has already evaluated both legs (the base builder's lazy FLOAT test): floating = exempt, else hold.</summary>
		public static RatioVerdict Float(bool floatNow) => floatNow ? RatioVerdict.ExemptFloat : RatioVerdict.Hold;

		/// <summary>The whole decision for callers and tests that have every input at hand.</summary>
		public static RatioVerdict Decide(bool measured, long mobile, long staticTotal, long cost, bool firstCopyOfType,
			bool unitSideSaturated, bool floating)
		{
			var v = Stage1(measured, mobile, staticTotal, cost, firstCopyOfType);
			return v == RatioVerdict.NeedsFloat ? Float(unitSideSaturated, floating) : v;
		}

		/// <summary>The start waits this sweep (the only verdict that holds).</summary>
		public static bool Holds(RatioVerdict verdict) => verdict == RatioVerdict.Hold;
	}
}
