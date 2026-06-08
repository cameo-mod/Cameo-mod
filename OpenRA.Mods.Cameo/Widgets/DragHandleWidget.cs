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

using System;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets
{
	public class DragHandleWidget : ColorBlockWidget
	{
		public Action<MouseInput> OnMouseMove = _ => { };

		[ObjectCreator.UseCtor]
		public DragHandleWidget(ModData modData)
			: base(modData) { }

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Down)
			{
				TakeMouseFocus(mi);
				OnMouseDown(mi);
				return true;
			}

			if (mi.Event == MouseInputEvent.Move)
			{
				if (HasMouseFocus)
					OnMouseMove(mi);
				return HasMouseFocus;
			}

			if (mi.Event == MouseInputEvent.Up)
			{
				OnMouseUp(mi);
				YieldMouseFocus(mi);
				return true;
			}

			return false;
		}
	}
}
