#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Resident boundary for an optional development log sink.")]
	public sealed class CameoLogHubInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new CameoLogHub();
	}

	public sealed class CameoLogHub
	{
		public ICameoLogSink Sink { get; private set; } = NullCameoLogSink.Instance;

		/// <summary>Bind the optional development sink; simulation code only reads its Enabled flag.</summary>
		public void Install(ICameoLogSink sink)
		{
			ArgumentNullException.ThrowIfNull(sink);
			if (Sink != NullCameoLogSink.Instance)
				throw new InvalidOperationException("A development log sink is already installed.");

			Sink = sink;
		}
	}
}
