#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// One enemy economy sighting as this bot has OBSERVED it (RAID-HARV-01 design 02 R1): a harvester seen with its cell and tick, or a refinery building from a valid previously observed snapshot. Nothing here is a read of a hidden actor:
	/// the provider's memory holds visible sightings only (units) and frozen buildings that are neither Hidden nor Shrouded.
	/// </summary>
	public readonly struct BotObservedEnemy
	{
		public BotObservedEnemy(uint actorId, CPos location, int lastSeenTick, bool harvester, bool refinery, OpenRA.Player owner)
		{
			ActorId = actorId;
			Location = location;
			LastSeenTick = lastSeenTick;
			Harvester = harvester;
			Refinery = refinery;
			Owner = owner;
		}

		public uint ActorId { get; }
		public CPos Location { get; }
		public int LastSeenTick { get; }
		public bool Harvester { get; }
		public bool Refinery { get; }
		public OpenRA.Player Owner { get; }
	}

	/// <summary>
	/// The fog-honest read of the enemy economy this bot has seen: remembered harvester sightings (visible-only, with their last cell and tick) and refineries from a valid frozen snapshot (Hidden and Shrouded skipped, always). Implemented by the
	/// master-AI module over its observation table; consumed by the squad manager, which must not name that class (the assemblies stay apart, as for <see cref="IBotRegionThreatProvider"/>). A disabled or absent provider reports nothing.
	/// </summary>
	public interface IBotObservedEnemyProvider
	{
		IEnumerable<BotObservedEnemy> ObservedEnemyEconomy();
	}
}
