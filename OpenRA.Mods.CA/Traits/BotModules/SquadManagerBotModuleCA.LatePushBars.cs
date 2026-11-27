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
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	// LATEPUSH-01 P1 (rows lp_age_min and lp_commit; design 01 D1 and D2, design 02). V08G (12 seats, 51216 ticks): 85 percent of every seat's army sat in the home guard, the blob the leader counts stayed below WAVE-MIN and one team
	// order was published in the whole game. Two rows:
	//  - lp_age_min: the leader's ordinary team bar is the WAVE-MIN bar x (1 + t/30000) x the map context x the ONE learned multiplier L, never above LatePushCapPercent of what the team can send (E_team) and never below WAVE-MIN.
	//    An open all-in keeps WAVE-MIN (the age-scaled bar is for ordinary pushes). Member-side floors (squad launch, keeps-going, pincer, reinforce, stalemate) stay on WAVE-MIN.
	//  - lp_commit: the blob counts a share f of the eligible army (free units plus the guard units above the P7 floor) instead of the free units plus a fixed lend share, and the guard lends exactly what that share needs.
	//    It needs the guard floor (the one reserve, design 02 R-D): with the floor off the row is inert and a DEV line says so once.
	// No scan, no RNG: the eligible figure rides on the blob the seat already computes at every team check; the bars are computed once per publish pass.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateLpAgeMin;
		AblationLatch ablateLpCommit;
		LatePushTable latePushTable;
		bool latePushInertLogged;
		int latePushFreeUnits;
		long latePushFreeValue;
		int latePushSpareUnits;
		long latePushSpareValue;
		int latePushBarLogTick = -1000;
		int latePushCommitLogTick = -1000;

		/// <summary>lp_age_min: the one launcher, WAVE-MIN, the Info switch and the DEV row.</summary>
		internal bool LatePushAgeMinOn => ArmyLaunchArbiterOn && WaveMinActive && Info.LatePushAgeMinEnabled && !ablateLpAgeMin.Off(Player, AblationIds.LpAgeMin);

		// lp_commit as configured (guard-lend on, the Info switch, the DEV row). It acts only with the guard floor on (design 02 R-D).
		bool LatePushCommitRequested => ArmyGuardLendOn && Info.LatePushCommitEnabled && !ablateLpCommit.Off(Player, AblationIds.LpCommit);

		/// <summary>lp_commit: requested and the guard floor on (the floor is the one reserve; without it the row is inert).</summary>
		internal bool LatePushCommitOn => LatePushCommitRequested && GuardFloorOn;

		// Either row needs the seat's eligible army (the cap of the bar and the base of the share).
		// LATEPUSH-01 P2: the all-in publish needs the eligible figure as well.
		bool LatePushOn => LatePushAgeMinOn || LatePushCommitOn || LatePushAllInPublishOn;

		/// <summary>The learnable numbers of the late push of this seat (prior = the yaml): the learner overrides them under the LatePushTable keys.</summary>
		internal LatePushTable LatePushParameters => latePushTable ??= new LatePushTable(Info.LatePushPersonalityPercent, Info.LatePushMapRefCells, Info.LatePushArmyCapPermille);

		// TeamLeadBlob, before the guard's lend is added: the seat's free army (pool plus quiet wave squads) and the guard's spare cleared. Also the one place the inert state of lp_commit is said (DEV only, once).
		void LatePushBlobBegin(int freeUnits, long freeValue)
		{
			if (LatePushCommitRequested && !GuardFloorOn && !latePushInertLogged && DiagnosticWriter != null)
			{
				latePushInertLogged = true;
				DiagnosticTeamEvent("lp_commit", "inert: guard floor off");
			}

			if (!LatePushOn)
				return;

			latePushFreeUnits = freeUnits;
			latePushFreeValue = freeValue;
			latePushSpareUnits = 0;
			latePushSpareValue = 0;
		}

		// TeamLeadBlob, after the guard's lend: publish what the seat could send (free units plus the guard units above the floor) next to its blob.
		void LatePushBlobEnd()
		{
			if (!LatePushOn || teamLeadTeam == null)
				return;

			teamLeadTeam.PublishEligible(teamLeadKey, LatePush.Eligible(latePushFreeUnits, latePushSpareUnits), latePushFreeValue + latePushSpareValue, World.WorldTick);
		}

		// ArmyGuardPick, after the ordinary lend share is known and before the guard floor narrows it. Records the guard units above the floor (the guard's share of the eligible army) and, with lp_commit on and no all-in open,
		// replaces the ordinary share with what the commit fraction needs. The result still goes through GuardFloorLend (the floor and the join minimum stay the one lend contract).
		int LatePushLend(SquadCA guard, int ordinaryLend)
		{
			if (!LatePushOn)
				return ordinaryLend;

			var spare = LatePushGuardSpare(guard, ordinaryLend);
			latePushSpareUnits = spare;
			latePushSpareValue = 0;
			for (var i = 0; i < spare; i++)
				latePushSpareValue += UnitValue(armyGuardLend[i].Actor);

			// LATEPUSH-01 P2 (row lp_all_in_publish): an open all-in claims every guard unit above the floor (the lend contract below still narrows it).
			if (LatePushAllInPublishOn && TeamLeadOpenOrder() is { AllIn: true })
				return LatePushAllInClaim(spare);

			if (!LatePushCommitOn || TeamLeadOpenOrder() is { AllIn: true })
				return ordinaryLend;

			var free = LatePushFreeUnits();
			var f = LatePushCommitNow(out var ratio);
			var plan = Math.Min(LatePush.LendPlan(f, free, spare), armyGuardLend.Count);
			var now = World.WorldTick;
			if (DiagnosticWriter != null && now - latePushCommitLogTick >= 250)
			{
				latePushCommitLogTick = now;
				DiagnosticTeamEvent("lp_commit", "tick=" + now + " f=" + f + " r=" + ratio + " free=" + free + " guard=" + guard.Units.Count + " joinable=" + armyGuardLend.Count + " spare=" + spare
					+ " ordinary=" + ordinaryLend + " plan=" + plan);
			}

			return plan;
		}

		// The guard units a commit could use: the joinable guard units above the floor. With the floor off the ordinary lend share is the most the guard gives (the old behaviour), so nothing is invented.
		int LatePushGuardSpare(SquadCA guard, int ordinaryLend)
		{
			if (!GuardFloorOn)
				return Math.Clamp(ordinaryLend, 0, armyGuardLend.Count);

			var allIn = TeamLeadOpenOrder() is { AllIn: true };
			var floor = GuardFloor.FloorUnits(guard.Units.Count, GuardFloorParameters.FloorPct, allIn, CoreUnderAttackNow);
			return Math.Clamp(guard.Units.Count - floor, 0, armyGuardLend.Count);
		}

		// The seat's free army in units, the same definition as the first part of TeamLeadBlob (the joinable pool plus the units of the quiet wave squads). The join pass asks it too, when the blob is not at hand.
		int LatePushFreeUnits()
		{
			var units = 0;
			if (TeamLeadBarJoinableOn)
			{
				foreach (var u in unitsHangingAroundTheBase)
					if (TeamLeadJoinable(u))
						units++;
			}
			else
				units = unitsHangingAroundTheBase.Count;

			foreach (var s in Squads)
				if (TeamLeadWaveSquad(s))
					units += s.Units.Count;

			return units;
		}

		// The share of the eligible army one order commits now (percent), and the observed army ratio it used (100 = unknown, time only). The ratio is the leader's last AheadScan, shared on the team.
		int LatePushCommitNow(out int ratio)
		{
			var now = World.WorldTick;
			ratio = teamLeadTeam?.AheadRatio(now, Math.Max(Info.TeamLeadPoolMaxAgeTicks, 4 * Info.ArmyAheadScanTicks)) ?? 100;
			return LatePush.CommitPercent(now, Info.LatePushAgeTicks, Info.LatePushCommitBasePercent, Info.LatePushCommitCapPercent, ratio, Info.ArmyAheadArmyPercent, LatePushParameters.CommitPercent);
		}

		// AheadScan (the leader's, once per ArmyAheadScanTicks): the observed own/enemy army ratio for the seats' commit fraction; 100 while the enemy army is unknown (never a guess from hidden state).
		void LatePushNoteRatio(AheadView v, int now)
		{
			if (teamLeadTeam == null)
				return;

			var enemy = v.EnemyArmy + v.EnemyDefence;
			teamLeadTeam.NoteAheadRatio(v.ArmyKnown && v.FactsKnown && enemy > 0 ? (int)Math.Clamp(100L * v.OwnArmy / enemy, 0, 1000) : 100, now);
		}

		// K_map in percent: the distance from the home position to the leader's LAST picked target (the target is picked after the ready test, so the bar cannot use the one about to be picked) over the learned
		// reference distance, clamped. No pick yet = 100 (neutral). cells = the distance used (-1 = none).
		int LatePushMapPercent(TeamLeadTeam team, out int cells)
		{
			cells = -1;
			if (team == null || team.LastTargetTick < 0)
				return 100;

			cells = (team.LastTarget - AggTeamHomePos()).HorizontalLength / WDist.FromCells(1).Length;
			return LatePush.MapPercent(cells, LatePushParameters.MapRefCells);
		}

		/// <summary>
		/// The bars of the leader's ordinary team push (ready test, rally promotion, nearly-won scan): the WAVE-MIN bars with lp_age_min off, an open all-in or no limits; otherwise the age-scaled bars capped by
		/// the team's eligible army and floored at WAVE-MIN. Called once per publish pass.
		/// </summary>
		(int Units, int Value) LatePushBars(TeamLeadTeam team, int now, bool allIn)
		{
			var bars = WaveMinBars();
			if (allIn || team == null || !LatePushAgeMinOn || !WaveMinCA.Limits(bars.Units, bars.Value))
				return bars;

			var k = LatePushMapPercent(team, out var cells);
			var l = LatePushParameters.LearnedPercent;
			var mu = LatePush.Bar(bars.Units, now, Info.LatePushAgeTicks, k, l);
			var mv = LatePush.Bar(bars.Value, now, Info.LatePushAgeTicks, k, l);
			var eligible = team.Eligible(now, Info.TeamLeadPoolMaxAgeTicks);
			// LATEPUSH-01 P2 (row lp_army_cap): the cap is the learned share of the eligible army (prior 800 permille); with the row off it is the fixed P1 percent, the same number as before.
			var cap = LatePushArmyCapNow();
			var units = LatePush.EffectiveBarPermille(mu, bars.Units, cap, eligible.Units);
			var value = LatePush.EffectiveBarPermille(mv, bars.Value, cap, eligible.Value);
			if (DiagnosticWriter != null && now - latePushBarLogTick >= 500)
			{
				latePushBarLogTick = now;
				DiagnosticTeamEvent("lp_age_min", "tick=" + now + " base=" + bars.Units + "/" + bars.Value + " age=" + LatePush.AgePermille(now, Info.LatePushAgeTicks) + " k=" + k + " kcells=" + cells
					+ " l=" + l + " m=" + mu + "/" + mv + " elig=" + eligible.Units + "/" + eligible.Value + " cap=" + cap + " bar=" + units + "/" + value);
			}

			return (units, value);
		}
	}
}
