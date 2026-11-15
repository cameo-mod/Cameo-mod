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
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Release-compatible no-op registration; capture is provided by the DEV mod.")]
	public sealed class AiEconomyHealthCaptureInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new AiEconomyHealthCapture();
	}

	// The release rules retain this registration; capture lives in the DEV assembly.
	public sealed class AiEconomyHealthCapture { }

}
