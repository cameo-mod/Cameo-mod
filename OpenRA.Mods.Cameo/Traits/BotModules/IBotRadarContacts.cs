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

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// One anonymous ranged-GPS contact (RADAR-DOTS design note): exactly what a
	/// human watching the radar dots could write down — WHERE the dot is, WHEN it
	/// was last seen, HOW FAST it moved, WHAT icon class it showed and WHOSE
	/// colour it wore. Deliberately no unit type, no health, no ActorID: the icon
	/// is all the information the renderer gives the human, and contacts are the
	/// bot-side equivalent of that channel. Anything richer would be a fog leak.
	/// </summary>
	public sealed class BotRadarContact
	{
		/// <summary>The cell the dot last occupied.</summary>
		public CPos Cell;

		/// <summary>The world tick the dot was last observed.</summary>
		public int Tick;

		/// <summary>Fixed-point drift between consecutive sightings, cells x1000 per tick
		/// (the situation log's vx_per_kilotick convention). 0 until the contact has
		/// been seen at two distinct ticks.</summary>
		public int VXPerKilotick, VYPerKilotick;

		/// <summary>The gpsdot icon the human saw — the dot's Sequence name
		/// (Infantry, Vehicle, Ship, Plane, Helicopter or a semantic building icon).</summary>
		public string Class = "";

		/// <summary>The effective owner whose colour the dot wore — equivalent to the
		/// owner colouring the human reads, nothing more.</summary>
		public OpenRA.Player Owner;
	}

	/// <summary>
	/// The bot-side radar screen: the set of enemy contacts a provider computes
	/// from the SAME predicates the render effect applies for a human viewer
	/// (CameoRangedGpsDotEffect.ShouldRenderDot). Live contacts only — a dot that
	/// leaves every provider disc drops out exactly like it stops rendering.
	/// No orders, no decisions: phase A publishes; consumers arrive separately.
	/// </summary>
	public interface IBotRadarContacts
	{
		/// <summary>The contacts visible this refresh, ordered deterministically.
		/// Empty while no own/allied provider is enabled or the module is off.</summary>
		IReadOnlyList<BotRadarContact> Contacts { get; }
	}
}
