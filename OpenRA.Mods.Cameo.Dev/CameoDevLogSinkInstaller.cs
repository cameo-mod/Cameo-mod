#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>The development-only endpoint installed behind the resident hub.</summary>
	public sealed class CameoDevLogSink : ICameoLogSink
	{
		public bool Enabled => true;
	}

	[TraitLocation(SystemActors.World)]
	[Desc("Installs the development logging sink on the world actor.")]
	public sealed class CameoDevLogSinkInstallerInfo : TraitInfo, Requires<CameoLogHubInfo>
	{
		public override object Create(ActorInitializer init) => new CameoDevLogSinkInstaller();
	}

	public sealed class CameoDevLogSinkInstaller : INotifyCreated
	{
		void INotifyCreated.Created(Actor self)
		{
			self.Trait<CameoLogHub>().Install(new CameoDevLogSink());
		}
	}
}
