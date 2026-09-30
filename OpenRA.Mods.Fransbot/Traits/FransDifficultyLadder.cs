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

namespace OpenRA.Mods.Fransbot.Traits
{
	/// <summary>
	/// DESIGN §19.1 / AI_MASTER_PLAN §5: every harvested module runs on every
	/// <c>genericbot</c> difficulty and only its strength changes, interpolated on
	/// one equal-step line between the easiest and the cameogod value. This is the
	/// <c>DynamicBotInsurance</c> pattern factored for the Frans modules: declare
	/// <c>MinX</c> (value at easiest) / <c>MaxX</c> (value at cameogod) and resolve
	/// the effective value once from the owner's <see cref="Player.BotType"/>.
	/// <c>classic</c> stays outside the ladder (the A/B reference) and never arms
	/// these modules; the fransbot donor type plays at hard strength.
	/// </summary>
	public static class FransDifficultyLadder
	{
		// Ordered easiest to hardest — identical to BotLimits@* and the
		// audit's DIFFICULTIES; keep in sync (audit_ai_personalities).
		static readonly string[] Difficulties =
		{
			"easiest", "veryeasy", "easy", "medium", "hard",
			"veryhard", "brutal", "challenger", "unbeatable", "cameogod",
		};

		// Bot types that play AS a ladder difficulty. `fransbot` is the hidden donor
		// stack (ai/fransbot.yaml) and `classic` the A/B reference — both anchor at hard.
		internal static readonly (string BotType, string AsDifficulty)[] Aliases =
		{
			("fransbot", "hard"),
			("classic", "hard"),
		};

		public static int RankOf(Player owner)
		{
			if (owner == null || !owner.IsBot)
				return -1;

			var botType = owner.BotType;
			if (string.IsNullOrEmpty(botType))
				return -1;

			foreach (var (type, alias) in Aliases)
				if (string.Equals(botType, type, StringComparison.OrdinalIgnoreCase))
				{
					botType = alias;
					break;
				}

			return Array.FindIndex(Difficulties,
				d => string.Equals(d, botType, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>Linear interpolation on the ladder: rank 0 → easiestValue, last rank → cameogodValue.</summary>
		public static int Scale(int easiestValue, int cameogodValue, int rank)
		{
			var steps = Difficulties.Length - 1;
			if (steps <= 0)
				return easiestValue;

			rank = Math.Clamp(rank, 0, steps);
			return easiestValue + (cameogodValue - easiestValue) * rank / steps;
		}

		/// <summary>
		/// Resolve a module strength value: ladder-scaled for ranked bots,
		/// <paramref name="fallback"/> for humans/unknown types (classic is aliased,
		/// so only genuinely unranked owners hit the fallback).
		/// </summary>
		public static int Resolve(int easiestValue, int cameogodValue, int fallback, Player owner)
		{
			var rank = RankOf(owner);
			return rank < 0 ? fallback : Scale(easiestValue, cameogodValue, rank);
		}
	}
}
