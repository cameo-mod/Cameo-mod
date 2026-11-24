#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	// TEAM-LEAD-01 (USER rule, 2026-11-23). Every value is a named yaml parameter; none is read while TeamLeadEnabled is false.
	// It builds on the AggTeam intent and target code, so it also needs AggTeamEnabled.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("TEAM-LEAD-01 master flag (release switch; the DEV row is tl_team_lead). On: each team of bots elects one leader by score; followers launch only on its order and the whole team gathers at one rally point, then pushes one target together. Off = every bot plays as before. Needs AggTeamEnabled.")]
		public readonly bool TeamLeadEnabled = false;

		[Desc("Ticks between team checks (election, order upkeep, published pools). The only periodic work of the feature. Range 100-300.")]
		public readonly int TeamLeadCheckTicks = 100;

		[Desc("A challenger takes the leader post only with this many percent more score than the leader. Range 5-50.")]
		public readonly int TeamLeadLeadPercent = 15;

		[Desc("A new leader keeps the post at least this many ticks (a dead leader is replaced at once). Range 500-6000.")]
		public readonly int TeamLeadHoldTicks = 1500;

		[Desc("The gathering never pushes earlier than this many ticks after the order. Range 100-900.")]
		public readonly int TeamLeadGatherMinTicks = 300;

		[Desc("The gathering pushes at the latest this many ticks after the order, whoever is still on the way. Range 600-3000.")]
		public readonly int TeamLeadGatherMaxTicks = 1500;

		[Desc("Percent of the committed army value that must stand at the rally point for an early push. Range 50-100.")]
		public readonly int TeamLeadArrivePercent = 70;

		[Desc("A unit counts as at the rally point within this many cells of it. Range 4-16.")]
		public readonly int TeamLeadArriveCells = 8;

		[Desc("Cells the rally point lies ahead of the leader's wave toward the target when the army staging has no plan. Range 4-24.")]
		public readonly int TeamLeadRallyForwardCells = 12;

		[Desc("A bot contributes a wave to an order only with at least this many idle units. Range 1-10.")]
		public readonly int TeamLeadJoinMinUnits = 3;

		[Desc("The leader forms the team wave with at least this many idle units of its own (the launch bars measure the whole team's pool). Range 1-10.")]
		public readonly int TeamLeadLeaderMinUnits = 1;

		[Desc("A leader that issued no order for this many ticks (counted from its election, the last order and the first attack tick) hands the post to the seat with the largest idle pool, so a leader that cannot form a wave never stalls the team. Range 1500-9000.")]
		public readonly int TeamLeadStallTicks = 4500;

		[Desc("A follower may still form and send a wave this many ticks after the push. Range 0-1200.")]
		public readonly int TeamLeadLateJoinTicks = 600;

		[Desc("An order that has not pushed after this many ticks is cancelled. Range 1500-6000.")]
		public readonly int TeamLeadOrderMaxTicks = 3000;

		[Desc("The joint guerrilla/harass target of the team is kept this many ticks (or until it dies), then the next squad without a target publishes a new one. Range 600-3000.")]
		public readonly int TeamLeadHarassTicks = 1500;

		[Desc("A published idle-pool count older than this many ticks is ignored in the combined blob. Range 200-600.")]
		public readonly int TeamLeadPoolMaxAgeTicks = 300;

		[Desc("TEAM-LEAD-02 (release switch; the DEV row is tl_leader_cadence). On: the leader publishes the team push from its own team check, no formed leader squad needed, whenever the combined blob (every member's idle army, quiet squads included) reaches the WAVE-MIN tier, at least as often as the old per-bot attack cadence (the MinimumAttackForceDelay reset, late-escalated) and at the latest every TeamLeadFallbackTicks; every member forms its wave for the open order at once, and a push whose target cannot be resolved attack-moves to the order's position instead of standing down. Off = the TEAM-LEAD-01 behaviour. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadCadenceEnabled = false;

		[Desc("TEAM-LEAD-02: the team has no order for this many ticks with a ready blob = the leader publishes now, whatever its own cadence says. Range 600-3000.")]
		public readonly int TeamLeadFallbackTicks = 1500;

		[Desc("TEAM-LEAD-02 (release switch; the DEV row is tl_finish_weakest). On, from LateEscalationStartTick: the team's push target (and the joint harass target) is the WEAKEST known enemy seat - the fewest known buildings, from what the bot sees and remembers - and it stays until that seat is dead. Off = the nearest known enemy as before. Needs TeamLeadEnabled.")]
		public readonly bool TeamLeadFinishWeakestEnabled = false;

		[Desc("TEAM-LEAD-02: a finish focus held this long without killing the seat (unreachable) rotates to the next weakest seat (and may come back to this one after the next such period). Range 6000-30000.")]
		public readonly int TeamLeadFocusMaxTicks = 12000;
	}
}
