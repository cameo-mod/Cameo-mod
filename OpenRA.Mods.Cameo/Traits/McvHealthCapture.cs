using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Opt-in read-only MCV observations; unsupported order/hold/transform proof remains UNKNOWN. Do not mount by default.")]
	public sealed class McvHealthCaptureInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new McvHealthCapture();
	}

	public sealed class McvHealthCapture { }
}
