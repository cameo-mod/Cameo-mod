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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// RATIO-2X-01 (ARMY_FIRST_02_DESIGN_02 s2.2) read seam: the situation owner publishes the live mobile army value of its latest
	/// snapshot, and the classification of a defence by actor info, so the base builder's 2x gate measures with exactly the
	/// definitions of the situation log and the audit (<c>own.army_value</c>, <c>own.defence_value</c>). It adds no scan: the value
	/// is the one <c>Rebuild</c> already computes every <c>SnapshotInterval</c> ticks. Read-only, no orders, no RNG.
	/// </summary>
	public interface IBotMobileStaticValues
	{
		/// <summary>A snapshot exists (the provider is enabled and has run its first rebuild).</summary>
		bool HasSnapshot { get; }

		/// <summary>The world tick the snapshot was taken at.</summary>
		int SnapshotTick { get; }

		/// <summary>Valued cost of the own live combat units at the snapshot (attack trait, not a building, not a harvester).</summary>
		long MobileValue { get; }

		/// <summary>
		/// The info-level twin of the situation's <c>IsDefence</c>: a building that has an attack trait or a defence target type.
		/// Conditions are not evaluated (an info has none): a conditionally targetable defence type counts as a defence.
		/// </summary>
		bool IsStaticDefence(ActorInfo info);
	}
}
