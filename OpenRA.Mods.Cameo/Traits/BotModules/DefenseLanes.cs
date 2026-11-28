#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// DEF-LANES-01 P1 (user 11-28): the per-lane attack memory of a base. A lane is a bearing bucket from the base centre to the origin of a hit on an
	/// own building. Each lane holds a weight in permille of extra defence budget: it rises per attack and per damage taken, and decays while the lane
	/// is quiet. World-free (tested); fed only by damage events, so nothing here reads hidden state or scans the world.
	/// </summary>
	public sealed class DefenseLanes
	{
		/// <summary>Damage points per strength step (one step adds the per-strength gain).</summary>
		public const int DamagePerStep = 10000;

		readonly int[] weight;
		readonly int[] lastAttackTick;
		readonly int[] damageCarry;
		int lastDecayTick;

		public DefenseLanes(int buckets)
		{
			buckets = Math.Max(1, buckets);
			weight = new int[buckets];
			lastAttackTick = new int[buckets];
			damageCarry = new int[buckets];
			for (var i = 0; i < buckets; i++)
				lastAttackTick[i] = int.MinValue / 2;
		}

		public int Buckets => weight.Length;

		public int Weight(int bucket) => weight[bucket];

		/// <summary>The bearing bucket of the offset (dx, dy); bucket 0 is centred on +X and the buckets run toward +Y. A zero offset is bucket 0.</summary>
		public static int Bucket(int dx, int dy, int buckets)
		{
			if (buckets <= 1 || (dx == 0 && dy == 0))
				return 0;

			var turn = Math.Atan2(dy, dx) / (2 * Math.PI); // -0.5 .. 0.5
			var b = (int)Math.Floor(turn * buckets + 0.5);
			return ((b % buckets) + buckets) % buckets;
		}

		/// <summary>The offset, <paramref name="radius"/> cells long, of the centre bearing of a bucket.</summary>
		public static (int X, int Y) Direction(int bucket, int buckets, int radius)
		{
			var a = 2 * Math.PI * bucket / Math.Max(1, buckets);
			return ((int)Math.Round(Math.Cos(a) * radius), (int)Math.Round(Math.Sin(a) * radius));
		}

		/// <summary>Applies the decay due up to <paramref name="tick"/>: every full interval takes <paramref name="decayPermille"/> off each lane.</summary>
		public void Decay(int tick, int decayPermille, int intervalTicks)
		{
			if (intervalTicks <= 0 || decayPermille <= 0)
			{
				lastDecayTick = tick;
				return;
			}

			var steps = (tick - lastDecayTick) / intervalTicks;
			if (steps <= 0)
				return;

			lastDecayTick += steps * intervalTicks;
			var keep = Math.Clamp(1000 - decayPermille, 0, 1000);
			for (var i = 0; i < weight.Length; i++)
				for (var s = 0; s < steps && weight[i] > 0; s++)
					weight[i] = weight[i] * keep / 1000;
		}

		/// <summary>
		/// One hit on an own building from the lane. A hit after a quiet window of <paramref name="attackWindowTicks"/> is a new attack and adds
		/// <paramref name="gainPerAttack"/>; every <see cref="DamagePerStep"/> damage points add <paramref name="gainPerStrength"/>. A lane holds at most
		/// <paramref name="maxWeight"/>.
		/// </summary>
		public void Hit(int bucket, int tick, int damage, int gainPerAttack, int gainPerStrength, int attackWindowTicks, int maxWeight)
		{
			if (bucket < 0 || bucket >= weight.Length)
				return;

			long w = weight[bucket];
			if (tick - lastAttackTick[bucket] >= attackWindowTicks)
				w += gainPerAttack;

			lastAttackTick[bucket] = tick;
			if (damage > 0)
			{
				long carry = damageCarry[bucket] + (long)damage;
				w += carry / DamagePerStep * gainPerStrength;
				damageCarry[bucket] = (int)(carry % DamagePerStep);
			}

			weight[bucket] = (int)Math.Clamp(w, 0, Math.Max(0, maxWeight));
		}

		/// <summary>The lane with the most weight (the lowest bucket on a tie); -1 when every lane is at 0.</summary>
		public int Heaviest()
		{
			var best = -1;
			var bestWeight = 0;
			for (var i = 0; i < weight.Length; i++)
				if (weight[i] > bestWeight)
				{
					bestWeight = weight[i];
					best = i;
				}

			return best;
		}

		public int Total()
		{
			long t = 0;
			foreach (var w in weight)
				t += w;

			return (int)Math.Min(int.MaxValue, t);
		}

		/// <summary>The extra defence budget in permille: 0 with no lane weight, else the base bump plus the summed weight, capped.</summary>
		public static int ExtraPermille(int totalWeight, int baseBump, int maxExtra)
		{
			if (totalWeight <= 0)
				return 0;

			return (int)Math.Clamp((long)baseBump + totalWeight, 0, Math.Max(0, maxExtra));
		}

		/// <summary>P2: the team budget. The own extra plus the ally share of the hardest-hit ally's extra, capped.</summary>
		public static int TeamExtra(int ownExtra, int allyExtra, int allySharePercent, int maxExtra)
		{
			return (int)Math.Clamp(ownExtra + (long)Math.Max(0, allyExtra) * Math.Max(0, allySharePercent) / 100, 0, Math.Max(0, maxExtra));
		}

		/// <summary>P2: an ally's lane takes the placement when its weight, after the ally share, is above our own heaviest lane.</summary>
		public static bool AllyLaneWins(int ownWeight, int allyWeight, int allySharePercent)
		{
			return (long)allyWeight * Math.Max(0, allySharePercent) / 100 > ownWeight;
		}

		/// <summary>The L1/L2 split: true when the next defence belongs on the lane (the lane share is below its target).</summary>
		public static bool LaneTurn(int lanePlaced, int coveragePlaced, int laneSharePercent)
		{
			return lanePlaced * 100 < laneSharePercent * (lanePlaced + coveragePlaced + 1);
		}
	}
}
