#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING. If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.CA
{
	/// <summary>
	/// STAGGER_DESIGN_06 section 8: the per-bot clock. A gated bot acts once every N ticks, so a module that counts
	/// calls would stretch its period N-fold. The visit clock gives the tick of the current visit and the number of
	/// ticks since the previous one. An ungated bot (or no scheduler) sees {WorldTick, 1, true}.
	/// Lives in OpenRA.Mods.CA so CA modules can read it without a reference to OpenRA.Mods.Cameo.
	/// </summary>
	public interface IBotVisitClock
	{
		(int VisitTick, int Elapsed, bool InVisit) Clock { get; }
	}

	public static class BotVisitClock
	{
		public static (int VisitTick, int Elapsed, bool InVisit) Of(IBot bot)
		{
			if (bot is IBotVisitClock c)
				return c.Clock;

			return (bot.Player.World.WorldTick, 1, true);
		}
	}

	/// <summary>
	/// Class P (period, STAGGER_DESIGN_06 9.2/9.3, rulings R1/R2): "run every K ticks". An absolute <c>Due</c> world
	/// tick; on a visit v >= Due the routine runs once and Due advances by whole periods past v (missed deadlines are
	/// coalesced and counted). With a visit every tick the firing ticks equal the old call-counted forms.
	/// </summary>
	public struct BotPeriod
	{
		public int Due;
		public int Period;
		public int Coalesced;

		// Call-counted forms (F1-F3) only count calls the module actually received: a module that was disabled for a
		// while (RunModules does not tick it) froze its old counter, so the gap is added back to Due (Sol: disabled-gap aging).
		int lastSeen;
		bool counted;

		BotPeriod(int due, int period, int seen = 0, bool counted = false)
		{
			Due = due;
			Period = Math.Max(1, period);
			Coalesced = 0;
			lastSeen = seen;
			this.counted = counted;
		}

		/// <summary>F1 pre-decrement <c>if (--x > 0) return; x = K;</c>. r = registration tick, x0 = counter then.</summary>
		public static BotPeriod F1(int r, int x0, int k) => new(r + Math.Max(1, x0), Math.Max(1, k), r, true);

		/// <summary>F2 post-decrement <c>if (x-- > 0) return; x = K;</c> fires every K + 1 calls.</summary>
		public static BotPeriod F2(int r, int x0, int k) => new(r + Math.Max(1, x0 + 1), Math.Max(1, k + 1), r, true);

		/// <summary>F3 pre-increment to a threshold <c>if (++c &lt; K) return; c = 0;</c>.</summary>
		public static BotPeriod F3(int r, int c0, int k) => new(r + Math.Max(1, k - c0), Math.Max(1, k), r, true);

		/// <summary>F4 residue <c>WorldTick % K == res</c>: the first tick t >= r + 1 with t mod K == res.</summary>
		public static BotPeriod F4(int r, int k, int res)
		{
			var kk = Math.Max(1, k);
			if (res < 0 || res >= kk)
				throw new ArgumentOutOfRangeException(nameof(res), res, $"F4 residue must be in [0, {kk}).");

			var t = r + 1;
			var m = ((t - res) % kk + kk) % kk;
			return new BotPeriod(m == 0 ? t : t + (kk - m), kk);
		}

		/// <summary>An absolute first deadline (<c>WorldTick >= next</c> forms): due0 = max(r + 1, next0).</summary>
		public static BotPeriod Abs(int r, int next0, int k) => new(Math.Max(r + 1, next0), Math.Max(1, k));

		/// <summary>
		/// F4 residue form <c>WorldTick % K == res</c> (R4): true when ANY tick in the visit's elapsed window
		/// [v - elapsed + 1, v] has the residue. Stateless, so a module that was disabled for a while does not fire a
		/// stale deadline on re-enable; at N = 1 (elapsed 1) it is exactly the old test.
		/// </summary>
		public static bool ResidueHit(int visitTick, int elapsed, int k, int res)
		{
			var kk = Math.Max(1, k);
			if (res < 0 || res >= kk)
				throw new ArgumentOutOfRangeException(nameof(res), res, $"Residue must be in [0, {kk}).");

			var last = visitTick - (((visitTick - res) % kk) + kk) % kk;
			return last >= visitTick - Math.Max(1, elapsed) + 1;
		}

		/// <summary>Restore form A (absolute due) or form R (remaining ticks at the load tick).</summary>
		public static BotPeriod FromRemaining(int loadTick, int remaining, int k) => new(loadTick + Math.Max(1, remaining), Math.Max(1, k));

		/// <summary>
		/// Carry for a plain int due field (<c>if (WorldTick >= next) { next = WorldTick + K; run }</c> forms). A due of 0 or
		/// less means "never armed": the first visit fires and the next deadline is v + K (identical to the old form at N = 1,
		/// where due0 = max(r + 1, next0)); afterwards due advances by whole periods past v (coalesced).
		/// </summary>
		public static bool CarryDue(ref int due, int visitTick, int k)
		{
			var kk = Math.Max(1, k);
			if (due <= 0)
				due = visitTick;

			if (visitTick < due)
				return false;

			due += ((visitTick - due) / kk + 1) * kk;
			return true;
		}

		/// <summary>
		/// Seed a plain due field for <see cref="CarryDue"/> from a constructor value: a seed already at or before the first visit
		/// becomes 0 (unarmed: fire at the first visit, next v + K, as the old <c>tick >= next</c> form does), a seed still in the
		/// future is kept. Without it CarryDue would reload s + m K from a stale positive seed (s 37, K 150, v 100: 187, not 250).
		/// </summary>
		public static int SeedDue(int seed, int worldTick) => seed <= worldTick ? 0 : seed;

		/// <summary>11.2 save form R: remaining = due - saveTick (may be zero or negative when overdue between visits).</summary>
		public int SavedRemaining(int saveTick) => Due - saveTick;

		/// <summary>
		/// Ticks between the last call this timer received and saveTick (0 for forms that are not call-counted). A counted timer
		/// ages by the calls it missed while its module was disabled; that age since lastSeen is part of the state, so a save in
		/// the middle of a disabled gap must carry it (RR-SAVE-01), or the restored timer forgets the gap so far and fires early.
		/// </summary>
		public int SeenAge(int saveTick) => counted ? Math.Max(0, saveTick - lastSeen) : 0;

		/// <summary>
		/// 11.2 load: TimerFormat 1 restores due = loadTick + saved exactly; a legacy number is a fresh x0 at r = loadTick (F1).
		/// seenAge is the saved <see cref="SeenAge"/>: lastSeen = loadTick - seenAge restores the exact pre-save last-seen tick.
		/// A save without it (age 0, as written before RR-SAVE-01) keeps lastSeen = loadTick.
		/// </summary>
		public static BotPeriod Restore(int loadTick, int saved, int k, bool newFormat, int seenAge = 0) =>
			newFormat ? new BotPeriod(loadTick + saved, k, loadTick - Math.Max(0, seenAge), true) : F1(loadTick, saved, k);

		public const int TimerFormatVersion = 1;

		/// <summary>A module that finds a newer TimerFormat than it knows refuses the load, naming itself (11.2).</summary>
		public static bool ReadTimerFormat(string module, string formatText)
		{
			if (formatText == null)
				return false;

			var v = int.Parse(formatText, System.Globalization.CultureInfo.InvariantCulture);
			if (v > TimerFormatVersion)
				throw new InvalidOperationException($"{module} save TimerFormat {v} is newer than the supported {TimerFormatVersion}.");

			return v >= 1;
		}

		public int Remaining(int visitTick) => Math.Max(0, Due - visitTick);

		/// <summary>The visit clock overload: also accounts for visits this module did not receive (disabled gap).</summary>
		public bool TryFire((int VisitTick, int Elapsed, bool InVisit) clock) => TryFire(clock.VisitTick, clock.Elapsed);

		public bool TryFire(int visitTick, int elapsed)
		{
			if (counted)
			{
				var previousVisit = visitTick - Math.Max(1, elapsed);
				if (previousVisit > lastSeen)
					Due += previousVisit - lastSeen;

				lastSeen = visitTick;
			}

			return TryFire(visitTick);
		}

		/// <summary>True when the routine is due at visit v; advances the deadline past v.</summary>
		public bool TryFire(int visitTick)
		{
			if (Period <= 0)
				throw new InvalidOperationException("BotPeriod used before it was armed (default struct has no period).");

			if (visitTick < Due)
				return false;

			var periods = (visitTick - Due) / Period + 1;
			Coalesced += periods - 1;
			Due += periods * Period;
			return true;
		}

		/// <summary>R2 freeze: on a visit where the old code would not have counted, the clock stops for Elapsed ticks.</summary>
		public void Freeze(int elapsed) => Due += Math.Max(0, elapsed);
	}

	/// <summary>
	/// Class C (cooldown, 9.2, R1): "after event E do not do X for K ticks". RELOAD kept: <c>Start(v, K)</c> at the
	/// visit that handles E; K is rounded up to the next visit by the <c>v >= Due</c> test. No catch-up.
	/// </summary>
	public struct BotCooldown
	{
		public int Due;

		public bool Ready(int visitTick) => visitTick >= Due;

		public void Start(int visitTick, int k) => Due = visitTick + k;

		/// <summary>R2 freeze for a cooldown counted only on some calls.</summary>
		public void Freeze(int elapsed) => Due += Math.Max(0, elapsed);

		public int Remaining(int visitTick) => Math.Max(0, Due - visitTick);

		public static BotCooldown FromRemaining(int loadTick, int remaining) => new() { Due = loadTick + Math.Max(0, remaining) };
	}
}
