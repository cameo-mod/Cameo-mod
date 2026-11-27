using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>Qualifies deterministic classifier readings for one uninterrupted target tenure.</summary>
	internal sealed class ObservedStyleQualification
	{
		const int MinimumGenerations = 3;
		const int MinimumSpan = 150;
		const int MaxEvidenceAge = 600;
		int firstTick;
		int lastGeneration = -1;
		int lastFreshTick;
		int generations;
		int incumbent = -1;
		int challenger = -1;
		int confirmations;

		public void Reset()
		{
			lastGeneration = -1;
			generations = 0;
			incumbent = -1;
			challenger = -1;
			confirmations = 0;
		}

		public int QualifiedClass(int tick) => generations > 0 && tick - lastFreshTick < MaxEvidenceAge ? incumbent : -1;

		public void Observe(int generation, int tick, int featureFamilies, int confidence, ReadOnlySpan<int> posterior)
		{
			if (generation <= lastGeneration)
				return;

			if (generations > 0 && tick - lastFreshTick >= MaxEvidenceAge)
				Reset();

			lastGeneration = generation;
			if (featureFamilies < 2 || posterior.Length < 2)
			{
				challenger = -1;
				confirmations = 0;
				return;
			}

			if (generations == 0)
				firstTick = tick;

			generations = Math.Min(MinimumGenerations, generations + 1);
			lastFreshTick = tick;

			var best = -1;
			var runnerUp = -1;
			for (var i = 0; i < posterior.Length; i++)
			{
				if (best < 0 || posterior[i] > posterior[best])
				{
					runnerUp = best;
					best = i;
				}
				else if (runnerUp < 0 || posterior[i] > posterior[runnerUp])
					runnerUp = i;
			}

			if (incumbent >= 0)
			{
				var rival = best == incumbent ? runnerUp : best;
				// A qualified challenger goes through its own confirmation before replacing the incumbent.
				if (confidence < 500 || posterior[incumbent] - posterior[rival] < 75)
					incumbent = -1;
			}

			if (best == incumbent || confidence < 650 || posterior[best] - posterior[runnerUp] < 150)
			{
				challenger = -1;
				confirmations = 0;
				return;
			}

			confirmations = best == challenger ? Math.Min(2, confirmations + 1) : 1;
			challenger = best;
			if (confirmations >= 2 && generations >= MinimumGenerations && tick - firstTick >= MinimumSpan)
				incumbent = challenger;
		}
	}
}
