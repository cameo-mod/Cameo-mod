#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>CONSOLIDATE-02 Step 1: the one job of every unit of a seat (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1).</summary>
	public enum ArmyRole : byte
	{
		/// <summary>The seat's share of the team blob: the pool, the quiet Rush squads, the gathering and the pushed squads.</summary>
		Main,

		/// <summary>A raid squad inside the harass cap (by value) and at least the minimum raid.</summary>
		Harass,

		/// <summary>Artillery, fire support and support: they follow the seat's main squad.</summary>
		Attached,

		/// <summary>The home guard (Protection squad); sized by the defend arbiter (Q3).</summary>
		DefenceHome,

		/// <summary>Air, naval and stealth keep their own domains; they count in the totals only.</summary>
		Other
	}

	/// <summary>What a squad is, for the role rules.</summary>
	public enum ArmySquadKind : byte { Rush, Raid, Protection, Attached, Domain }

	/// <summary>Where a squad stands right now (the defend arbiter reads the same classes).</summary>
	public enum ArmyHolder : byte
	{
		/// <summary>The idle pool (no squad).</summary>
		Pool,

		/// <summary>A squad in the Idle state: it has not moved out.</summary>
		Quiet,

		/// <summary>A squad gathering for the open team order.</summary>
		Gathering,

		/// <summary>A team squad that has pushed (marching or fighting for the order).</summary>
		Pushed,

		/// <summary>Any other state: staging, marching, fighting, fleeing, raiding.</summary>
		Busy
	}

	/// <summary>One squad of the snapshot. <see cref="Key"/> is the caller's index; the plan answers per key.</summary>
	public readonly record struct ArmyPlanSquad(int Key, ArmySquadKind Kind, ArmyHolder Holder, int Units, long Value);

	/// <summary>The result of one Step 1 pass.</summary>
	public sealed class ArmyPlan
	{
		public static readonly ArmyPlan Empty = new(new Dictionary<int, ArmyRole>(), -1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		public ArmyPlan(Dictionary<int, ArmyRole> roles, int mainSquadKey, long combatValue, long harassCapValue,
			int mainUnits, long mainValue, int harassUnits, long harassValue, int attachedUnits, long attachedValue,
			int defenceUnits, long defenceValue, int otherUnits, int demotedSmall, int demotedOverCap)
		{
			Roles = roles;
			MainSquadKey = mainSquadKey;
			CombatValue = combatValue;
			HarassCapValue = harassCapValue;
			MainUnits = mainUnits;
			MainValue = mainValue;
			HarassUnits = harassUnits;
			HarassValue = harassValue;
			AttachedUnits = attachedUnits;
			AttachedValue = attachedValue;
			DefenceUnits = defenceUnits;
			DefenceValue = defenceValue;
			OtherUnits = otherUnits;
			DemotedSmall = demotedSmall;
			DemotedOverCap = demotedOverCap;
		}

		/// <summary>The role of every squad of the snapshot, by its key. Pool units are always <see cref="ArmyRole.Main"/>.</summary>
		public IReadOnlyDictionary<int, ArmyRole> Roles { get; }

		/// <summary>The seat's main squad (the parent of every attached squad); -1 = none (the pool only).</summary>
		public int MainSquadKey { get; }

		/// <summary>The ground combat value the harass cap is measured on: pool + Rush + raid + home guard.</summary>
		public long CombatValue { get; }

		/// <summary>The most raid value the cap allows (long.MaxValue = no cap).</summary>
		public long HarassCapValue { get; }

		public int MainUnits { get; }
		public long MainValue { get; }
		public int HarassUnits { get; }
		public long HarassValue { get; }
		public int AttachedUnits { get; }
		public long AttachedValue { get; }
		public int DefenceUnits { get; }
		public long DefenceValue { get; }
		public int OtherUnits { get; }

		/// <summary>Raid squads below the minimum raid: they count as MAIN.</summary>
		public int DemotedSmall { get; }

		/// <summary>Raid squads that do not fit the value cap: they count as MAIN.</summary>
		public int DemotedOverCap { get; }

		public ArmyRole RoleOf(int key) => Roles.TryGetValue(key, out var role) ? role : ArmyRole.Main;
	}

	/// <summary>
	/// CONSOLIDATE-02 Q2 (row sq_roles under the master switch sq_pipeline_02): the pure Step 1 of the army plan. No world access, no RNG, deterministic
	/// (ordinal sorts on the caller's keys). The squad manager builds the snapshot once per ArmyPlanTicks and acts on the answers; nothing here issues an order.
	/// </summary>
	public static class ArmyPlanCA
	{
		/// <summary>
		/// Step 1. Every squad gets exactly one role:
		/// - domain squads (air, naval, stealth) are Other; artillery, fire support and support are Attached; the home guard is DefenceHome;
		/// - a raid squad is Harass when it has at least <paramref name="minRaidUnits"/> units and fits the cap of <paramref name="harassCapPercent"/> percent of the
		///   ground combat VALUE (Opus D5); the busy raids (already out) are placed first, then the bigger ones, then the lower key. A raid that is too small or does not
		///   fit counts as Main;
		/// - every Rush squad and the pool are Main.
		/// The main squad is the Main Rush squad of the team order (gathering or pushed) with the most value, else the most valuable Main Rush squad; ties by key.
		/// </summary>
		public static ArmyPlan Assign(IReadOnlyList<ArmyPlanSquad> squads, int poolUnits, long poolValue, int harassCapPercent, int minRaidUnits)
		{
			squads ??= Array.Empty<ArmyPlanSquad>();
			var roles = new Dictionary<int, ArmyRole>(squads.Count);
			long combat = Math.Max(0, poolValue);
			foreach (var s in squads)
				if (s.Kind is ArmySquadKind.Rush or ArmySquadKind.Raid or ArmySquadKind.Protection)
					combat += Math.Max(0, s.Value);

			var cap = HarassCap(combat, harassCapPercent);
			int mainUnits = Math.Max(0, poolUnits), harassUnits = 0, attachedUnits = 0, defenceUnits = 0, otherUnits = 0, small = 0, overCap = 0;
			long mainValue = Math.Max(0, poolValue), harassValue = 0, attachedValue = 0, defenceValue = 0;

			var raids = new List<ArmyPlanSquad>();
			foreach (var s in squads)
			{
				switch (s.Kind)
				{
					case ArmySquadKind.Domain:
						roles[s.Key] = ArmyRole.Other;
						otherUnits += s.Units;
						break;
					case ArmySquadKind.Attached:
						roles[s.Key] = ArmyRole.Attached;
						attachedUnits += s.Units;
						attachedValue += s.Value;
						break;
					case ArmySquadKind.Protection:
						roles[s.Key] = ArmyRole.DefenceHome;
						defenceUnits += s.Units;
						defenceValue += s.Value;
						break;
					case ArmySquadKind.Raid:
						raids.Add(s);
						break;
					default:
						roles[s.Key] = ArmyRole.Main;
						mainUnits += s.Units;
						mainValue += s.Value;
						break;
				}
			}

			raids.Sort((a, b) =>
			{
				var busy = (b.Holder == ArmyHolder.Busy).CompareTo(a.Holder == ArmyHolder.Busy);
				if (busy != 0)
					return busy;
				var value = b.Value.CompareTo(a.Value);
				return value != 0 ? value : a.Key.CompareTo(b.Key);
			});

			foreach (var r in raids)
			{
				var role = ArmyRole.Main;
				if (r.Units < Math.Max(1, minRaidUnits))
					small++;
				else if (harassValue + Math.Max(0, r.Value) <= cap)
					role = ArmyRole.Harass;
				else
					overCap++;

				roles[r.Key] = role;
				if (role == ArmyRole.Harass)
				{
					harassUnits += r.Units;
					harassValue += Math.Max(0, r.Value);
				}
				else
				{
					mainUnits += r.Units;
					mainValue += Math.Max(0, r.Value);
				}
			}

			var main = -1;
			var mainInOrder = false;
			long mainBest = -1;
			foreach (var s in squads)
			{
				if (s.Kind != ArmySquadKind.Rush || s.Units <= 0)
					continue;

				var inOrder = s.Holder is ArmyHolder.Gathering or ArmyHolder.Pushed;
				var better = main < 0 || (inOrder && !mainInOrder) || (inOrder == mainInOrder && (s.Value > mainBest || (s.Value == mainBest && s.Key < main)));
				if (!better)
					continue;

				main = s.Key;
				mainInOrder = inOrder;
				mainBest = s.Value;
			}

			return new ArmyPlan(roles, main, combat, cap, mainUnits, mainValue, harassUnits, harassValue, attachedUnits, attachedValue,
				defenceUnits, defenceValue, otherUnits, small, overCap);
		}

		/// <summary>The raid value the cap allows: <paramref name="capPercent"/> percent of the combat value, rounded down. 100 or more = no cap; 0 or less = no raids.</summary>
		public static long HarassCap(long combatValue, int capPercent) =>
			capPercent >= 100 ? long.MaxValue : Math.Max(0, combatValue) * Math.Max(0, capPercent) / 100;

		/// <summary>
		/// One more raider is allowed by VALUE (Opus D5). <paramref name="totalValue"/> is the seat's ground combat value with the candidate; <paramref name="raidValue"/>
		/// and <paramref name="raiders"/> are the raid squads without it. While the raiders are fewer than <paramref name="minRaid"/> (a first squad being formed) the
		/// cap must also hold that many units of the candidate's value, or no raider is taken at all (a raid that can never reach the minimum only holds units back).
		/// </summary>
		public static bool HarassAllowsByValue(long raidValue, int raiders, long totalValue, int candidateValue, int capPercent, int minRaid)
		{
			if (capPercent >= 100)
				return true;

			var cap = HarassCap(totalValue, capPercent);
			var candidate = Math.Max(0, candidateValue);
			if (Math.Max(0, raidValue) + candidate > cap)
				return false;

			return raiders >= minRaid || (long)Math.Max(0, minRaid) * candidate <= cap;
		}

		/// <summary>
		/// An attached squad keeps its parent when that parent is a valid Main squad with at least half the units of the seat's main squad; otherwise it is
		/// re-parented to the main squad (never to "the newest squad", which may be a one-unit follower: design L18).
		/// </summary>
		public static bool AttachedKeepsParent(bool parentValid, bool parentIsMain, int parentUnits, int mainUnits) =>
			parentValid && parentIsMain && (long)parentUnits * 2 >= mainUnits;
	}
}
