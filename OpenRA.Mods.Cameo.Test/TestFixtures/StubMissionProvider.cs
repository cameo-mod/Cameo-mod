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
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Test.TestFixtures
{
	/// <summary>
	/// Publish-only mission provider: hands a fixed mission list to the consumer
	/// under test and ignores MissionTaken callbacks.
	/// </summary>
	public sealed class StubMissionProvider : IBotMissionProvider
	{
		public IReadOnlyList<BotMission> Missions { get; set; } = Array.Empty<BotMission>();
		public void MissionTaken(BotMission mission) { }
	}
}
