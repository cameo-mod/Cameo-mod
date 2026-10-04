#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Hotspot #1 extraction: the pure decision branches of MasterAiBotModule.Rebuild.
	/// Plain values in (plus EnemyProfile/RegionMemory data tables the caller already
	/// built), a decision out — no World, no Actor, no trait lookup. Every function is
	/// a byte-for-byte move of the branch it replaces; Rebuild keeps the world-gathering,
	/// the state mutation and the order queueing.
	/// </summary>
	public static class MasterAiEval
	{
		/// <summary>Urgency for this snapshot: Emergency latches (CheckEmergency owns its
		/// lifecycle between snapshots); else Pressured when any enemy profile carries
		/// pressure OR the remembered enemy army outweighs ours past PressuredArmyRatio
		/// percent; else Normal.</summary>
		public static BotUrgency ClassifyUrgency(BotUrgency current, bool anyPressure,
			int enemyArmy, int ownArmy, int pressuredArmyRatio)
		{
			if (current == BotUrgency.Emergency)
				return BotUrgency.Emergency;

			return anyPressure || (enemyArmy > 0 && (long)ownArmy * 100 < (long)enemyArmy * pressuredArmyRatio)
				? BotUrgency.Pressured : BotUrgency.Normal;
		}

		/// <summary>TC-3 BE (§12.18): the coalition directive's MainTarget adopted only when
		/// the flag is on, a shared target exists and it is still one of this bot's own
		/// attackable candidates. Null keeps the locally chosen target.</summary>
		public static EnemyProfile CoalitionBiasTarget(bool useBias, OpenRA.Player coalitionMainTarget,
			IEnumerable<EnemyProfile> candidates)
		{
			if (!useBias || coalitionMainTarget == null)
				return null;

			return candidates.FirstOrDefault(p => p.Player == coalitionMainTarget);
		}

		/// <summary>§4.3 nemesis override: a live nemesis whose score clears the override
		/// weight retargets regardless of the decision interval and MinimumHoldTicks —
		/// but only when its profile is still alive and reached. Null keeps the incumbent.</summary>
		public static EnemyProfile NemesisOverrideTarget(OpenRA.Player nemesis, float nemesisScore,
			int overrideWeight, OpenRA.Player incumbent, IEnumerable<EnemyProfile> profiles)
		{
			if (nemesis == null || nemesis == incumbent || nemesisScore < overrideWeight)
				return null;

			return profiles.FirstOrDefault(p => p.Alive && p.NearestCells >= 0 && p.Player == nemesis);
		}

		/// <summary>Once-per-emergency personality override (§12.14): fires on the first
		/// Emergency snapshot while EmergencyKeepsPersonality is off and the transition
		/// has not already been handled.</summary>
		public static bool EmergencyTransition(BotUrgency urgency, bool handled, bool keepsPersonality)
		{
			return urgency == BotUrgency.Emergency && !handled && !keepsPersonality;
		}

		/// <summary>The demand set ResolveDemands must honour: the controller's live active
		/// set when it holds anything, else this bot's last issued set (so a demand the
		/// controller already dropped still times out instead of resurrecting).</summary>
		public static IReadOnlyCollection<string> HeldDemands(IReadOnlyCollection<string> active,
			IReadOnlyCollection<string> lastIssued)
		{
			return active.Count > 0 ? active : lastIssued;
		}

		/// <summary>Queue a SetBotCounterDemand order only when a controller is present to
		/// receive it AND the resolved set differs from what is already active.</summary>
		public static bool ShouldIssueDemandOrder(bool hasController, IReadOnlySet<string> active,
			IEnumerable<string> resolved)
		{
			return hasController && !active.SetEquals(resolved);
		}

		/// <summary>§12.14 guerrilla inputs: over the union of every enemy's region table,
		/// count regions whose intel is fresh (seen within the scout staleness horizon —
		/// an empty look still counts) and regions with any remembered enemy presence.
		/// Per-cell loops short-circuit once both flags are set.</summary>
		public static (int Fresh, int Presence) CountRegionIntel(
			IReadOnlyDictionary<OpenRA.Player, RegionMemory.Region[]> byEnemy,
			int cellCount, int tick, int staleAfterTicks)
		{
			var fresh = 0;
			var presence = 0;
			for (var i = 0; i < cellCount; i++)
			{
				var isFresh = false;
				var hasPresence = false;
				foreach (var enemyRegions in byEnemy.Values)
				{
					if (i >= enemyRegions.Length)
						continue;

					var r = enemyRegions[i];
					if (r == null)
						continue;

					if (!isFresh && r.EverSeen && tick - r.LastSeenTick <= staleAfterTicks)
						isFresh = true;
					if (!hasPresence && r.ArmyValue + r.DefenceValue + r.EconomyValue > 0)
						hasPresence = true;
					if (isFresh && hasPresence)
						break;
				}

				fresh += isFresh ? 1 : 0;
				presence += hasPresence ? 1 : 0;
			}

			return (fresh, presence);
		}

		/// <summary>Ledger delta with sentinel handling: a negative previous or current
		/// read (no ledger last snapshot / no ledger now) is a window of zero, never a
		/// spike or a negative.</summary>
		public static long LedgerWindowDelta(long previous, long current)
		{
			return previous < 0 || current < 0 ? 0 : Math.Max(0, current - previous);
		}

		/// <summary>Cumulative-counter delta, floored at zero (a counter that reset or a
		/// stale read never produces a negative window).</summary>
		public static int WindowDelta(int previous, int current)
		{
			return Math.Max(0, current - previous);
		}

		/// <inheritdoc cref="WindowDelta(int,int)"/>
		public static long WindowDelta(long previous, long current)
		{
			return Math.Max(0L, current - previous);
		}

		/// <summary>Window → per-game-minute rate; the first snapshot (no previous tick)
		/// contributes zero rather than a one-tick extrapolation.</summary>
		public static long PerGameMin(long window, int deltaTicks, long ticksPerMinute)
		{
			return deltaTicks > 0 ? window * ticksPerMinute / deltaTicks : 0;
		}

		/// <summary>Situation hint: budget share for defence by urgency (80 emergency /
		/// 55 pressured / 30 normal — all already inside the 0-100 clamp).</summary>
		public static int DefenceFractionHint(BotUrgency urgency)
		{
			return urgency == BotUrgency.Emergency ? 80 : urgency == BotUrgency.Pressured ? 55 : 30;
		}

		/// <summary>Situation hint: expansion appetite — 60 only on a Normal snapshot with
		/// an army worth projecting, else 20.</summary>
		public static int ExpansionAppetiteHint(BotUrgency urgency, int ownArmy)
		{
			return urgency == BotUrgency.Normal && ownArmy > 0 ? 60 : 20;
		}

		/// <summary>TC-1 (§12.17): whether this bot asks the team for defence this
		/// snapshot — Pressured or worse.</summary>
		public static bool RequestsDefence(BotUrgency urgency)
		{
			return urgency >= BotUrgency.Pressured;
		}
	}
}
