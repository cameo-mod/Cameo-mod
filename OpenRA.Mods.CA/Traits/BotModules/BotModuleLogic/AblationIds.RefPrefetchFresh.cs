#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// REF-PREFETCH-FRESH.
	public static partial class AblationIds
	{
		// Relaxed refinery sites promoted before READY expire at the READY flip, and a strict site outranks a relaxed one (Info RefPrefetchFresh). Off = as before.
		public const string RefPrefetchFresh = "ref_prefetch_fresh";
	}
}
