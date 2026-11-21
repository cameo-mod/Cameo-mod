#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public enum DropExclusionKind { Ground, AntiAir }

	/// <summary>One hard exclusion disc: a remembered or observed threat plus its reach and margin (world units).</summary>
	public readonly record struct DropExclusion(WPos Center, int RadiusLength, DropExclusionKind Kind);

	/// <summary>
	/// ENGDROP hard exclusion geometry for one capture run, an immutable ordered snapshot of the owner's knowledge (scouted memory): anti-air discs bar aircraft
	/// (their points and flight segments), ground discs (remembered static range plus margin, observed or remembered ground forces)
	/// bar every landing and target point. A rejection is a refusal: there is no permissive fallback.
	/// </summary>
	public sealed class DropConstraints
	{
		public static readonly DropConstraints None = new([], 0);

		public IReadOnlyList<DropExclusion> Zones { get; }

		/// <summary>The world tick the snapshot was taken.</summary>
		public int ObservedTick { get; }

		public DropConstraints(IEnumerable<DropExclusion> zones, int observedTick)
		{
			Zones = zones
				.OrderBy(z => (int)z.Kind).ThenBy(z => z.Center.X).ThenBy(z => z.Center.Y).ThenBy(z => z.RadiusLength)
				.ToArray();
			ObservedTick = observedTick;
		}

		static bool Applies(DropExclusion zone, bool air) => zone.Kind == DropExclusionKind.Ground || air;

		/// <summary>True when the point lies inside a zone that applies to this kind of craft.</summary>
		public bool RejectsPoint(WPos point, bool air)
		{
			foreach (var zone in Zones)
				if (Applies(zone, air) && DistanceSq(point, zone.Center) <= (long)zone.RadiusLength * zone.RadiusLength)
					return true;

			return false;
		}

		/// <summary>True when an AIRCRAFT's straight flight from a to b crosses an anti-air or ground zone. Ground craft never reject on a segment
		/// here (their route is the provider's own path search); this is the air corridor test.</summary>
		public bool RejectsSegment(WPos a, WPos b, bool air)
		{
			if (!air)
				return false;

			foreach (var zone in Zones)
				if (zone.Kind == DropExclusionKind.AntiAir && SegmentWithin(a, b, zone.Center, zone.RadiusLength))
					return true;

			return false;
		}

		static long DistanceSq(WPos a, WPos b)
		{
			long dx = a.X - b.X;
			long dy = a.Y - b.Y;
			return dx * dx + dy * dy;
		}

		/// <summary>Whether the closest point of segment a-b to p is within radius (horizontal plane).</summary>
		public static bool SegmentWithin(WPos a, WPos b, WPos p, int radius)
		{
			long abx = b.X - a.X;
			long aby = b.Y - a.Y;
			var len2 = abx * abx + aby * aby;
			long r2 = (long)radius * radius;
			if (len2 == 0)
				return DistanceSq(p, a) <= r2;

			long apx = p.X - a.X;
			long apy = p.Y - a.Y;
			var dot = apx * abx + apy * aby;
			if (dot <= 0)
				return DistanceSq(p, a) <= r2;

			if (dot >= len2)
				return DistanceSq(p, b) <= r2;

			// distance^2 = cross^2 / len2; compared as cross^2 <= r2 * len2 in double (no long overflow).
			var cross = (double)(apx * aby - apy * abx);
			return cross * cross <= (double)r2 * len2;
		}
	}
}
