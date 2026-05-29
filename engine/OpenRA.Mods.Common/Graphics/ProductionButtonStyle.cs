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

namespace OpenRA.Mods.Common.Graphics
{
	public enum ProductionButtonStyle
	{
		// Default RA2-style bevel with a subtle highlight and dark left edge
		Ra2,

		// Classic RA/RA1 beveled edge
		Ra1,

		// TD inspired bevel using a warmer highlight tone
		Td,

		// TS-inspired cooler edge highlight
		Ts,

		// Fallback to the legacy bar-only rendering
		None,
	}
}

