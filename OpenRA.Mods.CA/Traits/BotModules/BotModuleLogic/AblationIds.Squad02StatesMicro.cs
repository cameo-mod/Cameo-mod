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
	public static partial class AblationIds
	{
		// Q9 Step 5 (states micro only): the Idle state of a team's MAIN squad holds (the order launches it, never its own pick); a lost target takes a LOCAL enemy only, else the squad
		// reports it done and stands idle; Flee reports Fled. Row off = the old state code.
		public const string SqStatesMicro = "sq_states_micro";
	}
}
