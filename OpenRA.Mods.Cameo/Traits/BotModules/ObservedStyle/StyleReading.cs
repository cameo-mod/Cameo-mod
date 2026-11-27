#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// B4 OBSERVED-STYLE phase d (DESIGN_OBSERVED_STYLE_02 section 1, "Output"): one enemy's observed-style reading. Six posteriors in permille (sum 1000, each 5..995,
	/// the order of <see cref="StyleNames"/>), the confidence in permille and the tick it was made. A reading is a stable object the classifier updates in place (zero
	/// allocation per update); a consumer treats it as read-only. <see cref="Prior"/> is the shared cold-start reading (uniform, confidence 0).
	/// </summary>
	public sealed class StyleReading
	{
		public const int Classes = 6;

		/// <summary>The six observable clusters' human-readable names, fixed (DESIGN_OBSERVED_STYLE_02 section 3d). Not learned from labels.</summary>
		public static readonly string[] StyleNames = { "rush", "tech", "turtle", "expansion", "steamroller", "guerrilla" };

		/// <summary>The uniform cold-start posterior in permille (sums to 1000).</summary>
		internal static readonly int[] UniformPermille = { 167, 167, 167, 167, 166, 166 };

		public static readonly StyleReading Prior = new();

		readonly int[] posterior = new int[Classes];

		internal StyleReading()
		{
			UniformPermille.CopyTo(posterior, 0);
		}

		public ReadOnlySpan<int> PosteriorPermille => posterior;

		/// <summary>0..1000. 0 means "knows nothing": the learner must use the three-value result.</summary>
		public int ConfidencePermille { get; private set; }

		public int Tick { get; private set; }

		internal void Set(ReadOnlySpan<int> permille, int confidencePermille, int tick)
		{
			permille.Slice(0, Classes).CopyTo(posterior);
			ConfidencePermille = confidencePermille;
			Tick = tick;
		}
	}

	/// <summary>
	/// B4 OBSERVED-STYLE phase d: the classifier's public face (DESIGN_OBSERVED_STYLE_02 section 1). Other modules (counter demand later, the learner in phase e) read the
	/// style of an enemy through this and never run a second classifier. The <see cref="OpenRA.Player"/> is only a key to look the reading up: it is never dereferenced
	/// for state. Never null: an enemy that was not classified (or any enemy in a release build) gets <see cref="StyleReading.Prior"/>, confidence 0.
	/// </summary>
	public interface IBotObservedStyle
	{
		StyleReading Reading(OpenRA.Player enemy);
	}

	/// <summary>
	/// The enable constant (DESIGN_OBSERVED_STYLE_02 section 4 and ADD2 section 1a). While it is false every release and playtest build uses the three-value learner result
	/// bit for bit (confidence 0 branch). It flips to true only in the commit that records BOTH passing gates (bot gate ADD1 and human-transfer gate ADD2/ADD3), never earlier.
	/// </summary>
	public static class ObservedStyleSettings
	{
		public static readonly bool Enabled = false;
	}
}
