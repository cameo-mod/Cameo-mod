#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// LEARN-P6 (SPEC 2026-10-05 §11): the pure math of the squad desire system — urgency mapping,
	/// the per-stance leaky integrator, the capped personality bias and the hysteresis + min-dwell
	/// pick. Integer/fixed-point only (thousandths): MP-deterministic and World-free for tests.
	/// Stance index order equals <see cref="SquadDesireStance"/>.
	/// </summary>
	public static class SquadDesireEval
	{
		public const int Scale = 1000;
		public const int StanceCount = 6;
		public const int MaxRatioMilli = 4000;

		// Per-personality urgency bias, [attack, defend, retreat, regroup, harass, reinforce],
		// thousandths before the cap. A missing/unknown name is the zero vector — no bias.
		static readonly int[,] BiasTable =
		{
			/* rush        */ { 150, -100, -100, -50, 50, 0 },
			/* turtle      */ { -100, 150, 75, 0, -100, 50 },
			/* tech        */ { -75, 75, 0, 50, -50, 50 },
			/* expansion   */ { -50, 50, 0, 25, -50, 75 },
			/* steamroller */ { 100, -50, -100, -150, -75, -50 },
			/* guerrilla   */ { 25, -75, -50, 25, 150, -25 },
		};

		static readonly string[] BiasNames = { "rush", "turtle", "tech", "expansion", "steamroller", "guerrilla" };

		static int ClampMilli(int v) => Math.Max(0, Math.Min(Scale, v));

		/// <summary>
		/// The six stance urgencies from the packaged facts, each clamped to 0..1000:
		/// attack follows the predicted ratio and a live target; defend follows enemies seen at home;
		/// retreat follows a losing call or damage; regroup follows scatter; harass is the raider
		/// appetite for a weakly-held target; reinforce rises when outgunned but not hopeless.
		/// </summary>
		public static void Urgencies(in SquadDesireSignals s, int[] dst)
		{
			var own = Math.Max(1, s.OwnValue);
			var dom = (int)Math.Min(MaxRatioMilli, s.SeenEnemyValue * (long)Scale / own);
			var baseDom = (int)Math.Min(MaxRatioMilli, s.BaseEnemyValue * (long)Scale / own);

			dst[(int)SquadDesireStance.Attack] =
				ClampMilli(s.PredictedRatioMilli * 2 / 3 + (s.HasTarget ? 150 : -150) - baseDom / 4);
			dst[(int)SquadDesireStance.Defend] =
				s.BaseEnemyValue > 0 ? ClampMilli(150 + baseDom * 3 / 4) : 0;
			dst[(int)SquadDesireStance.Retreat] =
				ClampMilli((Scale - s.PredictedRatioMilli) / 2 + (Scale - s.HealthMilli) / 3);
			dst[(int)SquadDesireStance.Regroup] =
				ClampMilli(s.ScatterMilli);
			dst[(int)SquadDesireStance.Harass] =
				ClampMilli((s.HarassCapable ? 400 : 50) + (s.HasTarget ? 100 : 0)
					+ Math.Max(0, Scale - dom) / 2 - (s.HarassCapable ? 0 : 100));
			dst[(int)SquadDesireStance.Reinforce] =
				ClampMilli(dom / 2 - 200 + baseDom / 4);
		}

		/// <summary>One leaky-integrator step in thousandths: d += ratePermille·(u − d)/1000.</summary>
		public static int Step(int d, int u, int ratePermille)
		{
			return d + ratePermille * (u - d) / Scale;
		}

		/// <summary>
		/// Adds the personality's per-stance bias to the urgencies, each component clamped to
		/// ±capMilli first — the cap keeps temperament from ever drowning clearly stronger evidence.
		/// </summary>
		public static void AddBias(int[] urgencies, string personality, int capMilli)
		{
			var row = -1;
			for (var i = 0; i < BiasNames.Length; i++)
				if (BiasNames[i] == personality)
				{
					row = i;
					break;
				}

			if (row < 0)
				return;

			var cap = Math.Abs(capMilli);
			for (var s = 0; s < StanceCount; s++)
				urgencies[s] = ClampMilli(urgencies[s] + Math.Max(-cap, Math.Min(cap, BiasTable[row, s])));
		}

		/// <summary>
		/// Hysteresis + dwell pick: a challenger must beat the incumbent by more than marginMilli,
		/// and only after dwellOk is true. current &lt; 0 bootstraps to the plain argmax; ties keep
		/// the incumbent, then the lowest stance index — identical order on every client.
		/// </summary>
		public static int Pick(int[] desires, int current, int marginMilli, bool dwellOk)
		{
			var best = 0;
			for (var i = 1; i < StanceCount; i++)
				if (desires[i] > desires[best])
					best = i;

			if (current < 0 || current >= StanceCount || best == current)
				return best;

			if (!dwellOk || desires[best] <= desires[current] + marginMilli)
				return current;

			return best;
		}
	}
}
