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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// FOG-MINELAYER-01 (Opus 11-26 BINDING no-omniscience): the pure decisions of ObservedMinelayerBotModule.
	/// Every input is a fact the bot may know: a live actor in sight, a frozen-layer record, or our own actor.
	/// </summary>
	public static class ObservedMine
	{
		/// <summary>
		/// A live enemy actor the bot SEES may be the target of the first mine field: an enemy, alive, not a husk, with target types
		/// that are not empty and not in the ignored set (the engine's IsPreferredEnemyUnit, with the in-sight test first).
		/// </summary>
		public static bool PreferredVisibleEnemy(bool inSight, bool enemyOwner, bool dead, bool husk, bool targetTypesEmpty, bool targetTypesIgnored) =>
			inSight && enemyOwner && !dead && !husk && !targetTypesEmpty && !targetTypesIgnored;

		/// <summary>
		/// A remembered enemy building (frozen-layer record) is a fallback target when no preferred enemy is in sight: the record is valid, its ghost is shown
		/// (the spot is fogged now), it is not hidden by a visibility modifier, its owner is an enemy, and it is a building that is not a husk. A record whose footprint
		/// lies wholly in shroud (<c>FrozenActor.Shrouded</c>: no footprint cell explored) is not a memory of the place and never counts (the FOG-FROZEN-01 rule).
		/// </summary>
		public static bool RememberedCandidate(bool valid, bool ghostShown, bool hidden, bool enemyOwner, bool building, bool husk, bool shrouded) =>
			valid && ghostShown && !hidden && !shrouded && enemyOwner && building && !husk;

		/// <summary>A remembered enemy record counts for the nearby-enemy test of a mine site when it is valid, shown as a ghost, not hidden, not wholly in shroud and owned by an enemy.</summary>
		public static bool RememberedEnemyNearby(bool valid, bool ghostShown, bool hidden, bool enemyOwner, bool shrouded) =>
			valid && ghostShown && !hidden && !shrouded && enemyOwner;

		/// <summary>
		/// An attack on our actor starts the conflict-site logic when its attacker is an enemy and, if the attacker is in sight, also passes the preferred-unit test.
		/// A hidden attacker is judged by its owner relationship only (its type, state and place are not known to us).
		/// </summary>
		public static bool AttackCounts(bool attackerInSight, bool attackerEnemy, bool preferredWhenSeen) =>
			attackerEnemy && (!attackerInSight || preferredWhenSeen);

		/// <summary>The attacker's own cell is the conflict site only when the attacker is in sight and the victim's target types ask for the enemy location; otherwise the victim's cell.</summary>
		public static bool UsesAttackerCell(bool attackerInSight, bool victimWantsEnemyLocation) =>
			attackerInSight && victimWantsEnemyLocation;

		/// <summary>An ordered minelayer made no progress for stallTicks (its cell is unchanged since lastProgressTick).</summary>
		public static bool Stalled(int now, int lastProgressTick, int stallTicks) => now - lastProgressTick >= stallTicks;

		/// <summary>A parked site is skipped until the tick it is parked to.</summary>
		public static bool ParkedActive(int now, int parkedUntil) => now < parkedUntil;

		/// <summary>The tick a site is parked to after its failures-th stall: a linear back-off, and for the rest of the game once maxAttempts stalls were reached.</summary>
		public static int ParkUntil(int now, int parkTicks, int failures, int maxAttempts) =>
			failures >= maxAttempts ? int.MaxValue : now + parkTicks * failures;
	}
}
