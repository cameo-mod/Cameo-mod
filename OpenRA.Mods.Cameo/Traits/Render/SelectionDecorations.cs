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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	[Desc("Cameo's selection decorations. Identical to the Common trait of the same name",
		"except that the selection bars are drawn in W21 LAYER ORDER — shield, integrity,",
		"armor plating, health, then everything else — instead of health first with the",
		"extras pushed underneath it.",
		"",
		"⚠ THIS SHADOWS `OpenRA.Mods.Common.Traits.Render.SelectionDecorations` ON PURPOSE.",
		"ObjectCreator.FindType returns the FIRST assembly in mod.yaml's Assemblies list",
		"that has a type of the requested name, and the order there is",
		"AS, CA, Cameo, Cnc, D2k, Common — so Cameo wins and every one of the ~400 live",
		"`SelectionDecorations:` nodes gets this version with no yaml change at all.",
		"Cameo already does this for ColorPickerColorShift and PlayerColorShift.",
		"The alternative was renaming the trait at ~400 sites across ~98 files, most of",
		"them owned by other agents.")]
	public class SelectionDecorationsInfo : Common.Traits.Render.SelectionDecorationsInfo
	{
		[Desc("Draw the layer bars above the health bar, outermost first. Set false to get",
			"Common's original order back (health on top, every extra bar beneath it).",
			"This field also PROVES the shadowing works: Common's trait has no such field,",
			"so if yaml naming it loads without an 'unknown field' error, the type the game",
			"resolved is this one.")]
		public readonly bool LayerBarOrder = true;

		public override object Create(ActorInitializer init) { return new SelectionDecorations(init.Self, this); }
	}

	public class SelectionDecorations : Common.Traits.Render.SelectionDecorations
	{
		readonly SelectionDecorationsInfo info;
		readonly Interactable interactable;

		public SelectionDecorations(Actor self, SelectionDecorationsInfo info)
			: base(self, info)
		{
			this.info = info;

			// The base keeps its own copy privately, so resolve our own rather than
			// reaching into it.
			interactable = self.Trait<Interactable>();
		}

		protected override IEnumerable<IRenderable> RenderSelectionBars(Actor self, WorldRenderer wr, bool displayHealth, bool displayExtra)
		{
			if (!info.LayerBarOrder)
			{
				foreach (var r in base.RenderSelectionBars(self, wr, displayHealth, displayExtra))
					yield return r;

				yield break;
			}

			// Same two guards as the base: non-selectable actors draw nothing, and neither
			// does an actor whose player has both bar kinds switched off.
			if (interactable is not Selectable || (!displayHealth && !displayExtra))
				yield break;

			yield return new LayeredSelectionBarsRenderable(self, interactable.DecorationBounds(self, wr), displayHealth, displayExtra);
		}
	}
}
