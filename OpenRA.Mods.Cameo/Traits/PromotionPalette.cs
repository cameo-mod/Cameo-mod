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

using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[Desc("Controls promotion tree layout hints such as fixed columns and groupings.")]
	public class PromotionPaletteInfo : TraitInfo<PromotionPalette>
	{
		[Desc("Zero-based column index for the promotion icon. Leave negative to use automatic layout.")]
		public readonly int Column = -1;

		[Desc("Group identifier used to draw a shared frame around related promotions.")]
		public readonly string Group;
	}

	public sealed class PromotionPalette
	{
	}
}
