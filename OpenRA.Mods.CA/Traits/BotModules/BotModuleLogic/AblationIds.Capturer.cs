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
	// ub_capturer_strict row. Owner: Sonnet. Off = the old rule: any unit with a Captures trait is a capturer for the cap and the armed-infantry denominator.
	public static partial class AblationIds
	{
		public const string CapturerStrict = "ub_capturer_strict";

		// ub_capturer_workers row (MILITIA-CAP-01). Owner: Sonnet. Off = a Harvester-trait unit with a building Captures trait counts as a capturer for the cap, as today.
		public const string CapturerWorkers = "ub_capturer_workers";

		// ub_unitmix_capturer row. Owner: Sonnet. Off = the unit-mix census classifies a capturer by the raw Captures trait (garrison infantry fall in None).
		public const string UnitMixCapturer = "ub_unitmix_capturer";
	}
}
