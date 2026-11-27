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
	// FIRST-COPY-DUE-01 row (RULES_AUDIT_04 A5, user D3). Owner: Sonnet.
	// UnitFirstCopyDue off = the unit roll keeps its weight-only first-copy boost (no due clock, no forced pick); the old behaviour exactly.
	public static partial class AblationIds
	{
		public const string UnitFirstCopyDue = "ub_first_copy_due";
	}
}
