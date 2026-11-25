#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// CONSOLIDATE-02 Q9 (design 4.1 Step 5, row sq_states_micro under sq_pipeline_02, needs the one launcher and the target pick): the squad state machines are micro only. For a MAIN squad of
	// the team (TeamLeadTeamSquad: a Rush squad that is no response, breaker or m13 squad) the order is the only launcher. Its Idle state holds (never a pick of its own), a lost target takes
	// only an enemy in its scan radius (never a far target of its own pick), and with none the squad reports the target done and stands idle; Flee reports Fled. A raid (HARASS) keeps its own
	// quorum launch. The state classes only ask these methods; every rule is here, behind the row. Reach: a squad in AttackMove/Attack is no quiet wave squad (TeamLeadWaveSquad is true only
	// for the Idle state), so it always updates and the lost-target hooks run; a pushed blob and a bounded squad keep their older rule and only report done through the same call.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateSqStatesMicro;

		/// <summary>Q9 (row sq_states_micro): the one launcher and the target pick are on, and the row is not ablated.</summary>
		internal bool ArmyStatesMicroOn => ArmyLaunchArbiterOn && ArmyTargetPickOn && !ablateSqStatesMicro.Off(Player, AblationIds.SqStatesMicro);

		/// <summary>
		/// The Idle state of this squad holds: no launch from Idle (L09-L11 are not launchers any more); the team order sends it. Defence in depth: under the one launcher a quiet Rush squad
		/// never reaches Update (before the first team check it waits, after it TeamLeadPrepare/ArmyPrepare joins or dismisses it, which is what TeamLeadWaveSquad's quiet-state test is for);
		/// only a defending squad (TeamLeadWaveSquad false) can tick in Idle, and it does not launch either.
		/// </summary>
		internal bool ArmyIdleHolds(SquadCA squad) => ArmyStatesMicroOn && TeamLeadTeamSquad(squad);

		/// <summary>
		/// The lost target of this squad is answered by a LOCAL pick only: a team MAIN squad that neither follows its order (TeamLeadKeepsGoing) nor is already bounded to its fight
		/// (TeamLeadBoundsRetarget, which does the same) would otherwise pick the nearest enemy anywhere or a far target of its own.
		/// </summary>
		internal bool ArmyLocalOnly(SquadCA squad) => ArmyStatesMicroOn && TeamLeadTeamSquad(squad) && !TeamLeadKeepsGoing(squad) && !TeamLeadBoundsRetarget(squad);

		/// <summary>The LOCAL pick: the closest enemy the bot sees or remembers inside the squad's scan radius around its first unit. False = none (the squad is done).</summary>
		internal bool ArmyLocalPick(SquadCA squad)
		{
			if (squad.Units.Count == 0)
				return false;

			var near = FindClosestEnemy(squad.Units[0].Actor, WDist.FromCells(Info.AttackScanRadius), squad);
			if (near == null)
				return false;

			squad.TargetActor = near;
			return true;
		}

		/// <summary>
		/// Q9 report to the plan (DEV record): the squad's target is gone and it stands idle until the order sends it again. Called at every hand-off of a team MAIN squad to Idle on a lost target:
		/// the local-only rule, the bounded retarget (gt_bounded_retarget) and the pushed blob's go-on (TeamLeadKeepsGoing), so the record is complete whichever older rule ends the fight.
		/// </summary>
		internal void ArmyNoteTargetDone(SquadCA squad)
		{
			if (ArmyStatesMicroOn && TeamLeadTeamSquad(squad) && DiagnosticWriter != null)
				DiagnosticTeamEvent("army_target_done", "units=" + squad.Units.Count + " type=" + squad.Type, DiagnosticSquadId(squad));
		}

		/// <summary>Q9 report to the plan (DEV record): the squad's flight ended; its units go back to the pool (MAIN) through the dismissal.</summary>
		internal void ArmyNoteFled(SquadCA squad)
		{
			if (ArmyStatesMicroOn && DiagnosticWriter != null)
				DiagnosticTeamEvent("army_fled", "units=" + squad.Units.Count + " type=" + squad.Type, DiagnosticSquadId(squad));
		}
	}
}
