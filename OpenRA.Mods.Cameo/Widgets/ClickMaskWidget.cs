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

using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets
{
	// A full-screen colour fill that also swallows every mouse event, so widgets sitting
	// behind it (e.g. the main menu) cannot be clicked while a modal window is open. The
	// main menu is loaded directly into the UI root rather than the modal window stack, so
	// OpenWindow alone does not hide it; this mask provides the missing modal backstop.
	public class ClickMaskWidget : ColorBlockWidget
	{
		[ObjectCreator.UseCtor]
		public ClickMaskWidget(ModData modData)
			: base(modData)
		{
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				TakeMouseFocus(mi);
				OnMouseDown(mi);
				return true;
			}

			if (mi.Event == MouseInputEvent.Up)
			{
				OnMouseUp(mi);
				YieldMouseFocus(mi);
				return true;
			}

			return true;
		}
	}
}
