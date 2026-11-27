#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License as
 * published by the Free Software Foundation, version 3 or later.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>REF-LEARN-01: convert a learned per-mille multiplier to a bounded integer.</summary>
	public static class RefineryLearn
	{
		public static int Resolve(IBotLearnedKnobs provider, string key, int prior, int min, int max)
		{
			if (provider == null || !provider.TryResolvePermille(key, out var factor))
				return Math.Clamp(prior, min, max);

			var scaled = ((long)prior * Math.Max(0, factor) + 500) / 1000;
			return (int)Math.Clamp(scaled, min, max);
		}

		// The fallback keeps the prior five-cell gap below the strict spacing.
		public static int RelaxedSpacing(int strict) => Math.Max(1, strict - 5);

		public static bool RequireTwoVerdicts(bool armed, bool previous) => !armed && previous;

		public static int NearestZone(CPos cell, IReadOnlyList<CPos> anchors)
		{
			var best = -1;
			var distance = long.MaxValue;
			for (var z = 0; z < anchors.Count; z++)
			{
				var d = RefineryDemand.DistanceSq(cell, anchors[z]);
				if (d < distance)
				{
					best = z;
					distance = d;
				}
			}

			return best;
		}
	}
}
