#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// DEF-RATIO-PERS-01 (rulebook amendment 3, user 11-27): the RATIO-2X factor per personality, in permille (2000 = 2.0). The yaml values are the priors.
	/// The learned knob <c>defence.ratio</c> replaces a prior through IBotLearnedKnobs.TryResolvePermille once LA02 lands; that seam is not on the v08h-j2
	/// base, so until then <see cref="Resolve"/> is called with <c>hasLearned</c> false and the prior is used. Off (the switch or the row
	/// def_ratio_personality) = 2000 for every personality. Only the factor moves here: the waiver, the AF2 cap, the per-type caps and the FLOAT exemption
	/// are separate rules and are not touched by this class.
	/// </summary>
	public static class DefenceRatioPersonality
	{
		/// <summary>The code default (Classic and every other personality, and the switch off): 2.0.</summary>
		public const int CodeDefault = 2000;

		/// <summary>The accepted parameter domain of the factor (0.75 .. 3.0).</summary>
		public const int Min = 750;
		public const int Max = 3000;

		// The personality strings the bot reports (IBotCurrentPersonality). The code spells the guerilla personality "guerrilla".
		public const string Rush = "rush";
		public const string Guerrilla = "guerrilla";
		public const string Steamroller = "steamroller";
		public const string Expansion = "expansion";
		public const string Tech = "tech";
		public const string Turtle = "turtle";

		/// <summary>The prior of a personality. An unknown personality (and Classic) reads the code default.</summary>
		public static int PriorFor(string personality, int rush, int guerrilla, int steamroller, int expansion, int tech, int turtle) => personality switch
		{
			Rush => rush,
			Guerrilla => guerrilla,
			Steamroller => steamroller,
			Expansion => expansion,
			Tech => tech,
			Turtle => turtle,
			_ => CodeDefault,
		};

		/// <summary>Switch off = 2000. On: the learned value when one is held, else the prior; clamped to 750..3000.</summary>
		public static int Resolve(bool armed, int prior, bool hasLearned, int learnedPermille)
		{
			if (!armed)
				return CodeDefault;

			return Math.Clamp(hasLearned ? learnedPermille : prior, Min, Max);
		}
	}
}
