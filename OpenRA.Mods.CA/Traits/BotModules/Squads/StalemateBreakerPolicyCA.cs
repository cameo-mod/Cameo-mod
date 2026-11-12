// BOTPASSIVE-FIX01 draft policy. No world-facing caller is wired yet.
// This file alone does not enable a feature or authorize a game.
using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>Pure, deterministic admission and stop predicates for a concentrated wave.</summary>
	public static class StalemateBreakerPolicyCA
	{
		public const int StartTick = 5000;
		public const int NoProgressTicks = 6000;
		public const int MinimumUnits = 8;
		public const int MinimumValue = 6000;
		public const int CooldownTicks = 5000;
		public const int RallyTicks = 750;
		public const int NoMovementTicks = 1500;
		public const int WaveTicks = 3000;
		public const int MinimumPercent = 60;

		public readonly record struct RouteCA(int EdgeCount, int MinimumSpeed);
		public readonly record struct TimingCA(int EstimatedTicks, int RallyDuration, int NoProgressDuration, int AbsoluteDuration);

		/// <summary>Fail closed for incomplete or unsupported routes; never clamp an oversized travel estimate.</summary>
		public static bool TryTiming(IEnumerable<RouteCA> routes, out TimingCA timing)
		{
			timing = default;
			if (routes == null)
				return false;
			var count = 0;
			long estimate = 0;
			foreach (var route in routes)
			{
				if (++count > 256 || route.EdgeCount < 0 || route.EdgeCount > 2048 || route.MinimumSpeed <= 0)
					return false;
				var distance = 1536L * route.EdgeCount;
				var ticks = (distance + route.MinimumSpeed - 1) / route.MinimumSpeed;
				estimate = Math.Max(estimate, ticks);
			}

			if (count == 0 || estimate > 18000)
				return false;
			var rally = (int)Math.Min(5000, Math.Max(750, (estimate + 2) / 3));
			var noProgress = (int)Math.Min(5000, Math.Max(1500, (estimate + 1) / 2));
			var absolute = (int)Math.Min(30000, Math.Max(3000, rally + 2 * estimate));
			timing = new TimingCA((int)estimate, rally, noProgress, absolute);
			return true;
		}

		public static int RequiredUnits(int eligible)
		{
			if (eligible < 0)
				throw new ArgumentOutOfRangeException(nameof(eligible));
			return (int)(((long)eligible * MinimumPercent + 99) / 100);
		}

		/// <summary>Design _06 §3: every force-trigger input is clamped to [0, 2^28 - 1], so 9 x cap^2 &lt; 2^63 is exact in int64.</summary>
		public const long ForceInputCap = (1L << 28) - 1;

		/// <summary>Fresh-seen window for denL (design _06 §3, Opus ruling 6FBC1BCD).</summary>
		public const int SeenFreshTicks = 750;

		public static long ClampForceInput(long value) => value < 0 ? 0 : value > ForceInputCap ? ForceInputCap : value;

		/// <summary>
		/// No-kill clock (design _04): elapsed from max(5000, last verified enemy CY/production kill) >= 6000, earliest 11000.
		/// Unknown provenance (lastConfirmedKillTick outside [-1, now]) is never a trigger.
		/// </summary>
		public static bool ClockTrigger(bool enabled, int now, int lastConfirmedKillTick)
		{
			if (!enabled || now < StartTick || lastConfirmedKillTick < -1 || lastConfirmedKillTick > now)
				return false;
			return (long)now - Math.Max(StartTick, lastConfirmedKillTick) >= NoProgressTicks;
		}

		/// <summary>
		/// Binding force trigger (design _06 §3, _07 M2): sqrt(rLocal x rTeam) >= 1.5 as 4 x numL x numT >= 9 x denL x denT.
		/// rLocal = numL / denL for the owner of the candidate being tried; rTeam = numT / denT.
		/// A zero (unknown, stale or empty) denominator never fires.
		/// </summary>
		public static bool ForceTrigger(bool enabled, long numL, long denL, long numT, long denT)
		{
			if (!enabled)
				return false;
			numL = ClampForceInput(numL);
			denL = ClampForceInput(denL);
			numT = ClampForceInput(numT);
			denT = ClampForceInput(denT);
			if (denL == 0 || denT == 0)
				return false;
			return checked(4 * numL * numT) >= checked(9 * denL * denT);
		}

		/// <summary>Telemetry only: floor(1000 x sqrt(rLocal x rTeam)) on the clamped inputs, or -1 when a denominator is zero.</summary>
		public static long ScoreMilli(long numL, long denL, long numT, long denT)
		{
			numL = ClampForceInput(numL);
			denL = ClampForceInput(denL);
			numT = ClampForceInput(numT);
			denT = ClampForceInput(denT);
			if (denL == 0 || denT == 0)
				return -1;
			var q = System.Numerics.BigInteger.Divide(
				System.Numerics.BigInteger.Multiply(1000000, System.Numerics.BigInteger.Multiply(numL, numT)),
				System.Numerics.BigInteger.Multiply(denL, denT));
			return (long)ISqrt(q);
		}

		static System.Numerics.BigInteger ISqrt(System.Numerics.BigInteger n)
		{
			if (n.Sign <= 0)
				return 0;
			var x = (System.Numerics.BigInteger)Math.Sqrt((double)n);
			while (x * x > n)
				x--;
			while ((x + 1) * (x + 1) <= n)
				x++;
			return x;
		}

		public static bool Admit(bool enabled, bool targetValid, bool homeEmergency, int eligibleUnits,
			int reachableUnits, long eligibleValue, long reachableValue)
		{
			if (!enabled || !targetValid || homeEmergency || eligibleUnits < MinimumUnits || reachableUnits < 0
				|| reachableUnits > eligibleUnits || eligibleValue < MinimumValue || reachableValue < 0
				|| reachableValue > eligibleValue)
				return false;
			return reachableUnits >= RequiredUnits(eligibleUnits) && MeetsValueShare(reachableValue, eligibleValue);
		}

		// Compute ceil(3 * denominator / 5) without overflowing long.
		public static bool MeetsValueShare(long numerator, long denominator) =>
			denominator > 0 && numerator >= 0 && numerator <= denominator
				&& numerator >= (denominator / 5 * 3) + ((denominator % 5 * 3 + 4) / 5);

		/// <summary>Arrival uses the immutable commit roster, not the shrinking live roster.</summary>
		public static bool Arrived(int committedCount, long committedValue, int arrivedCount, long arrivedValue) =>
			committedCount > 0 && arrivedCount >= 0 && arrivedCount <= committedCount
				&& arrivedCount >= RequiredUnits(committedCount) && MeetsValueShare(arrivedValue, committedValue);

		/// <summary>Return false as soon as the temporary predictor exemption is no longer justified.</summary>
		public static bool DeferRetreat(bool enabled, bool evidenceComplete, bool targetValid, bool targetDestroyed,
			bool homeEmergency, int now, int waveStart, int lastProgress, long startingValue, long confirmedLossValue,
			int noProgressDuration, int absoluteDuration)
		{
			if (!enabled || !evidenceComplete || !targetValid || targetDestroyed || homeEmergency || waveStart < 0
				|| lastProgress < waveStart || now < lastProgress || startingValue <= 0 || confirmedLossValue < 0
				|| confirmedLossValue >= startingValue || noProgressDuration < 1500 || noProgressDuration > 5000
				|| absoluteDuration < 3000 || absoluteDuration > 30000)
				return false;
			return (long)now - waveStart < absoluteDuration && (long)now - lastProgress < noProgressDuration
				&& confirmedLossValue <= (startingValue - 1) / 2;
		}

		/// <summary>Clamp only downwards; preserve existing lower launch bars.</summary>
		public static int SteamrollerValueBar(bool enabled, int existing) => enabled ? Math.Min(existing, 15000) : existing;

		public static int SteamrollerOverflow(bool enabled, int existing) => enabled ? Math.Min(existing, 24) : existing;

		/// <summary>
		/// IMPL01-HOTFIX03 (IMPL02-A): whether the members of a squad of this type enter the breaker pool. Rush and Guerrilla squads
		/// always count (their members are drafted only while the squad is quiet); every other type counts only while its squad is quiet (SquadManagerBotModuleCA.StalemateSquadQuiet decides per type),
		/// so a busy member of a Protection, Harass, Artillery, Support, FireSupport or Stealth squad never enters it.
		/// </summary>
		public static bool PoolContributes(SquadCAType type, bool squadQuiet) =>
			type is SquadCAType.Rush or SquadCAType.Guerrilla || squadQuiet;

		/// <summary>
		/// IMPL01-HOTFIX03 (IMPL02-B): the home condition that blocks admission and ends a live wave is a visible home threat or the
		/// home latch. A loss-only Emergency (no threat, no latch) no longer blocks the breaker.
		/// </summary>
		public static bool HomeBlocksBreaker(bool homeThreat, bool homeLatch) => homeThreat || homeLatch;

		/// <summary>The immediate main-base emergency preempts the personality dwell.</summary>
		public static bool MaySwitchPersonality(bool enabled, bool mainBaseEmergency, int now, int lastApplied) =>
			!enabled || mainBaseEmergency || (lastApplied >= 0 && now >= lastApplied && (long)now - lastApplied >= 3000);
	}
}
