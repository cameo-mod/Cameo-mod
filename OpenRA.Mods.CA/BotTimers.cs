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

		BotPeriod(int due, int period)
		{
			Due = due;
			Period = Math.Max(1, period);
			Coalesced = 0;
		}

		/// <summary>F1 pre-decrement <c>if (--x > 0) return; x = K;</c>. r = registration tick, x0 = counter then.</summary>
		public static BotPeriod F1(int r, int x0, int k) => new(r + Math.Max(1, x0), Math.Max(1, k));

		/// <summary>F2 post-decrement <c>if (x-- > 0) return; x = K;</c> fires every K + 1 calls.</summary>
		public static BotPeriod F2(int r, int x0, int k) => new(r + Math.Max(1, x0 + 1), Math.Max(1, k + 1));

		/// <summary>F3 pre-increment to a threshold <c>if (++c &lt; K) return; c = 0;</c>.</summary>
		public static BotPeriod F3(int r, int c0, int k) => new(r + Math.Max(1, k - c0), Math.Max(1, k));

		/// <summary>F4 residue <c>WorldTick % K == res</c>: the first tick t >= r + 1 with t mod K == res.</summary>
		public static BotPeriod F4(int r, int k, int res)
		{
			var kk = Math.Max(1, k);
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
			var last = visitTick - (((visitTick - res) % kk) + kk) % kk;
			return last >= visitTick - Math.Max(1, elapsed) + 1;
		}

		/// <summary>Restore form A (absolute due) or form R (remaining ticks at the load tick).</summary>
		public static BotPeriod FromRemaining(int loadTick, int remaining, int k) => new(loadTick + Math.Max(1, remaining), Math.Max(1, k));

		public int Remaining(int visitTick) => Math.Max(0, Due - visitTick);

		/// <summary>True when the routine is due at visit v; advances the deadline past v.</summary>
		public bool TryFire(int visitTick)
		{
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
