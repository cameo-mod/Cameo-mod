#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ENGINEER-CAP-10PCT (maintainer 2026-10-12): how many capturable targets the bot knows of and
	/// has not captured yet — neutral tech buildings plus PriorityCapturableActorTypes enemy
	/// structures, each fog-fairly seen. The engineer owner reports the raw count; the unit builder
	/// clamps it into the capturer cap as the extra allowance (+1 per known target).
	/// </summary>
	public interface IBotCapturerTargetAllowance
	{
		int CapturerTargetAllowance { get; }
	}
}
