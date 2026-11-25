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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ub_capturer_strict (Opus 11-25, V08E): the capturer cap counted ANY unit with a Captures trait, so ordinary rifle infantry (garrison and driver-kill captures) were capped as capturers and the
	/// infantry queue sat idle. Strict = a unit is a capturer only when one of its Captures traits can capture a BUILDING class (CapturerCaptureTypes: building, defense, tech, the Capturable.Types
	/// of enemy buildings). Garrison, driver-kill and vehicle/husk captures never count. Pure, so it is testable without rules.
	/// </summary>
	public static class CapturerClass
	{
		public static bool IsStrict(IEnumerable<BitSet<CaptureType>> captureTypesOfTheTraits, BitSet<CaptureType> buildingTypes)
		{
			foreach (var types in captureTypesOfTheTraits)
				if (types.Overlaps(buildingTypes))
					return true;

			return false;
		}
	}
}
