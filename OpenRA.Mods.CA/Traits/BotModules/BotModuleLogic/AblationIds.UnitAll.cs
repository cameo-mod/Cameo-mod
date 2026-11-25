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

namespace OpenRA.Mods.CA.Traits
{
	// V08 rows (Opus 11-25).
	public static partial class AblationIds
	{
		// UNIT-ALL-01: the unit pick is one fraction roll over every listed UnitsToBuild type the queue can build, with a personality bias, an observed-enemy counter term and a
		// floor (every listed type keeps a chance; a type never built is picked first). Row off = the old mix layers.
		public const string UnitRoll = "ub_unit_roll";

		// UNIT-ALL-01 mode bias profiles (the folded layers; each multiplies the weight while its existing trigger holds, never gates). Same order as UnitRoll.ProfileNames.
		public const string UnitRollComposition = "ub_roll_composition";
		public const string UnitRollCounter = "ub_roll_counter";
		public const string UnitRollWaveMix = "ub_roll_wavemix";
		public const string UnitRollRoleDeficit = "ub_roll_roledeficit";
		public const string UnitRollOpening = "ub_roll_opening";
		public const string UnitRollLearned = "ub_roll_learned";

		public static readonly string[] UnitRollProfileRows =
		[
			UnitRollComposition, UnitRollCounter, UnitRollWaveMix, UnitRollRoleDeficit, UnitRollOpening, UnitRollLearned,
		];
	}
}
