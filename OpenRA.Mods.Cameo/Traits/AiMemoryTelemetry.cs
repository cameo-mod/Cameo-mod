#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Release-compatible no-op registration; memory telemetry is provided by the DEV mod.")]
	public sealed class AiMemoryTelemetryInfo : TraitInfo
	{
		[Desc("World ticks between memory samples.")]
		public readonly int SampleIntervalTicks = 250;

		public override object Create(ActorInitializer init) { return new AiMemoryTelemetry(this); }
	}

	// The release rules retain this registration; collection lives in the DEV assembly.
	public sealed class AiMemoryTelemetry
	{
		public AiMemoryTelemetry(AiMemoryTelemetryInfo info) { }
	}
}
