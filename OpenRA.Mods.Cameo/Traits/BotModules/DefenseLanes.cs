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
using System.Text.Json;

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

		// Defences the team placed on each lane (by the owner or by an ally) while the lane holds weight; a lane that goes cold starts again at 0.
		readonly int[] placed;
		int lastDecayTick;

		public DefenseLanes(int buckets)
		{
			buckets = Math.Max(1, buckets);
			weight = new int[buckets];
			lastAttackTick = new int[buckets];
			damageCarry = new int[buckets];
			placed = new int[buckets];
			for (var i = 0; i < buckets; i++)
				lastAttackTick[i] = int.MinValue / 2;
		}

		public int Buckets => weight.Length;

		public int Weight(int bucket) => weight[bucket];

		public int Placed(int bucket) => placed[bucket];

		/// <summary>One defence was placed on the lane (by the owner or by an ally).</summary>
		public void NotePlaced(int bucket)
		{
			if (bucket >= 0 && bucket < placed.Length && weight[bucket] > 0)
				placed[bucket]++;
		}

		/// <summary>
		/// The team spread: the weight of a lane as an ally sees it, lowered by the defences already placed there. Each placed defence adds
		/// <paramref name="spreadPermille"/> to the divisor (500 = the weight is divided by 1.5, 2, 2.5 ...). 0 = no spread.
		/// </summary>
		public int EffectiveWeight(int bucket, int spreadPermille)
		{
			return (int)((long)weight[bucket] * 1000 / (1000 + (long)placed[bucket] * Math.Max(0, spreadPermille)));
		}

		/// <summary>The bearing bucket of the offset (dx, dy); bucket 0 is centred on +X and the buckets run toward +Y. A zero offset is bucket 0.</summary>
		public static int Bucket(int dx, int dy, int buckets)
		{
			if (buckets <= 1 || (dx == 0 && dy == 0))
				return 0;

			var turn = Math.Atan2(dy, dx) / (2 * Math.PI); // -0.5 .. 0.5
			var b = (int)Math.Floor(turn * buckets + 0.5);
			return ((b % buckets) + buckets) % buckets;
		}

		/// <summary>
		/// The lane of one hit. Observed only: the bearing of the attacker when the bot can see it, else the bearing of the building that was hit
		/// (offsets from the base centre). -1 when neither gives a bearing (an unseen attacker hitting the building on the base centre).
		/// </summary>
		public static int OriginBucket(bool attackerSeen, int attackerDx, int attackerDy, int buildingDx, int buildingDy, int buckets)
		{
			if (attackerSeen)
				return Bucket(attackerDx, attackerDy, buckets);

			return buildingDx == 0 && buildingDy == 0 ? -1 : Bucket(buildingDx, buildingDy, buckets);
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
			{
				for (var s = 0; s < steps && weight[i] > 0; s++)
					weight[i] = weight[i] * keep / 1000;

				if (weight[i] <= 0)
					placed[i] = 0;
			}
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
			if (weight[bucket] <= 0)
				placed[bucket] = 0;
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

		/// <summary>The whole memory with the split of its episode, as plain values for a save.</summary>
		public SavedLanes Save(LaneSplit split)
		{
			return new SavedLanes(SavedLanes.CurrentVersion, (int[])weight.Clone(), (int[])lastAttackTick.Clone(), (int[])damageCarry.Clone(),
				(int[])placed.Clone(), lastDecayTick, split.Hot, split.LanePlaced, split.CoveragePlaced);
		}

		/// <summary>Restores a save made with the same bucket count. False (and nothing changed) for any other shape.</summary>
		public bool Restore(SavedLanes state, LaneSplit split)
		{
			if (state == null || state.Weight.Length != weight.Length || state.LastAttack.Length != weight.Length || state.Carry.Length != weight.Length
				|| state.Placed.Length != weight.Length)
				return false;

			for (var i = 0; i < weight.Length; i++)
			{
				weight[i] = Math.Max(0, state.Weight[i]);
				lastAttackTick[i] = state.LastAttack[i];
				damageCarry[i] = Math.Clamp(state.Carry[i], 0, DamagePerStep - 1);
				placed[i] = weight[i] > 0 ? Math.Max(0, state.Placed[i]) : 0;
			}

			lastDecayTick = state.LastDecay;
			split.Restore(state.Hot, state.LanePlaced, state.CoveragePlaced);
			return true;
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

	/// <summary>
	/// DEF-LANES-01: the coverage/lane split of one episode. An episode starts when a lane turns hot after a quiet time; its counts start at 0,
	/// so an old episode's placements never owe or pre-pay the split of a new one. World-free (tested).
	/// </summary>
	public sealed class LaneSplit
	{
		bool hot;

		public bool Hot => hot;
		public int LanePlaced { get; private set; }
		public int CoveragePlaced { get; private set; }

		/// <summary>Reports whether any lane holds weight now; cold to hot starts a new episode.</summary>
		public void Observe(bool nowHot)
		{
			if (nowHot && !hot)
			{
				LanePlaced = 0;
				CoveragePlaced = 0;
			}

			hot = nowHot;
		}

		public bool LaneTurn(int laneSharePercent) => DefenseLanes.LaneTurn(LanePlaced, CoveragePlaced, laneSharePercent);

		public void Restore(bool wasHot, int lanePlaced, int coveragePlaced)
		{
			hot = wasHot;
			LanePlaced = Math.Max(0, lanePlaced);
			CoveragePlaced = Math.Max(0, coveragePlaced);
		}

		public void NotePlacement(bool onLane)
		{
			if (onLane)
				LanePlaced++;
			else
				CoveragePlaced++;
		}
	}

	/// <summary>One bot's lane memory in a save game. Plain ints so the record round-trips through JSON.</summary>
	public sealed record SavedLanes(int Version, int[] Weight, int[] LastAttack, int[] Carry, int[] Placed, int LastDecay, bool Hot, int LanePlaced,
		int CoveragePlaced)
	{
		// Version 2: the per-lane placed counts joined. Key and version changed with it; the V1 shape never left a branch.
		public const int CurrentVersion = 2;
		public const string SaveKey = "DefenseLanesV2";

		/// <summary>The save text: base64 of the JSON.</summary>
		public static string Encode(SavedLanes state) => Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(state));

		/// <summary>A damaged, foreign or other-version text decodes to null, never throws.</summary>
		public static SavedLanes Decode(string text)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			try
			{
				var state = JsonSerializer.Deserialize<SavedLanes>(Convert.FromBase64String(text));
				if (state == null || state.Version != CurrentVersion || state.Weight == null || state.LastAttack == null || state.Carry == null || state.Placed == null)
					return null;

				return state;
			}
			catch (Exception e) when (e is FormatException or JsonException or NotSupportedException)
			{
				return null;
			}
		}
	}
}
