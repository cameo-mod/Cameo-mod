#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>What an idle ground squad does with the coalition ratio it predicts at its target (IMPL02 A2).</summary>
	public enum AggressionBandCA { Flee, Regroup, Attack }

	/// <summary>
	/// IMPL02-AGG-CORE (AGGRESSION_PLAN_01 A1-A3 and the A4 no-abandonment part). Pure decisions only: the module and the
	/// ground states feed them world values. Every caller is gated by SquadManagerBotModuleCA.AggressionOn, which is false
	/// whenever the switch-ON flag StalemateBreakerEnabled is false, so the OFF arm never reaches this class.
	/// </summary>
	public static class AggressionPolicyCA
	{
		/// <summary>A9 registry: every IMPL02-AGG-CORE threshold, its yaml key and its documented inclusive range. A later tuner
		/// may move a value anywhere inside its range; RulesetLoaded rejects a value outside it.</summary>
		public static readonly (string Key, int Min, int Max)[] Ranges =
		{
			("AggressionEngageRatioPct", 1, 300),
			("AggressionFleeRatioPct", 0, 300),
			("AggressionCoalitionCells", 1, 64),
			("AggressionAllyEngagedCells", 1, 64),
			("AggressionRegroupMarginCells", 0, 32),
			("AggressionRegroupMaxTicks", 0, 6000),
			("AggressionMinCommitTicks", 0, 3000),
			("AggressionCommitBreakLossPct", 0, 100),
			("AggressionMinDispatchUnits", 0, 20),
			("AggressionMinDispatchValue", 0, 20000),
		};

		/// <summary>The first range violation as a message, or null. The flee ratio must also not exceed the engage ratio.</summary>
		public static string RangeError(Func<string, int> value)
		{
			foreach (var (key, min, max) in Ranges)
			{
				var v = value(key);
				if (v < min || v > max)
					return $"{key} = {v} is outside its documented range {min}..{max}.";
			}

			if (value("AggressionFleeRatioPct") > value("AggressionEngageRatioPct"))
				return "AggressionFleeRatioPct must not exceed AggressionEngageRatioPct.";

			return null;
		}

		/// <summary>The ON gate: the breaker switch (the A/B ON arm), the aggression kill switch and the combat predictor.</summary>
		public static bool Enabled(bool stalemateBreakerEnabled, bool aggressionEnabled, bool useCombatPredictor) =>
			stalemateBreakerEnabled && aggressionEnabled && useCombatPredictor;

		/// <summary>A2: flee only below FleeRatio, attack at or above EngageRatio, regroup forward in between.
		/// A FleeRatio above the EngageRatio is treated as equal to it, so a bad yaml pair cannot open an inverted band.</summary>
		public static AggressionBandCA Band(double ratio, int fleeRatioPct, int engageRatioPct)
		{
			var pct = ratio * 100;
			var flee = Math.Min(fleeRatioPct, engageRatioPct);
			if (pct < flee)
				return AggressionBandCA.Flee;

			return pct >= engageRatioPct ? AggressionBandCA.Attack : AggressionBandCA.Regroup;
		}

		/// <summary>
		/// FREEZE10 DIAG-FIX-01 (idle path, CG): an idle squad whose own coalition is in the Flee band is re-judged on the ratio that also
		/// counts the allied squads fighting beside it (the in-fight A4 rule). A ratio at or above FleeRatio turns the flight into the
		/// forward Regroup - never straight into an attack, the engaged allies only keep it from running. Other bands, and a ratio that
		/// is still below FleeRatio, are unchanged.
		/// </summary>
		public static AggressionBandCA IdleBandWithAllies(AggressionBandCA band, double ratioWithAllies, int fleeRatioPct, int engageRatioPct) =>
			band == AggressionBandCA.Flee && Band(ratioWithAllies, fleeRatioPct, engageRatioPct) != AggressionBandCA.Flee ? AggressionBandCA.Regroup : band;

		/// <summary>A3: a squad below the dispatch floor (unit count or value) is tiny. A tiny squad never runs the flee loop
		/// and never regroups forward: it attacks only when the coalition band is Attack, otherwise it holds and grows.</summary>
		public static bool IsTiny(int units, long value, int minDispatchUnits, int minDispatchValue) =>
			units < minDispatchUnits || value < minDispatchValue;

		/// <summary>The idle decision. Tiny squads map Flee and Regroup to Hold (null); a veto turns Attack into Regroup
		/// (Hold for a tiny squad) and never into a flight. A7 finishing push (Sonnet's AggTeamFinishActive): every squad
		/// attacks unless its coalition is decisively out-numbered (the Flee band) - no regroup, no hold, no veto.</summary>
		public static AggressionBandCA? IdleDecision(AggressionBandCA band, bool tiny, bool vetoed, bool finish = false)
		{
			if (finish)
				return band == AggressionBandCA.Flee ? AggressionBandCA.Flee : AggressionBandCA.Attack;

			if (band == AggressionBandCA.Attack && vetoed)
				band = AggressionBandCA.Regroup;

			if (tiny && band != AggressionBandCA.Attack)
				return null;

			return band;
		}

		/// <summary>A4: a squad is engaged while it is in the ground fight states (attack, guerrilla hit). Marching, staging,
		/// idle and fleeing squads are not engaged.</summary>
		internal static bool IsEngagedState(IState state) => state is GroundUnitsAttackState or GuerrillaUnitsHitState;

		/// <summary>Why a committed squad that the legacy rule wants to pull out stays in the fight (A2, A4); None = it may flee.
		/// The trade (enemy value killed against own value lost) is not a reason: it is report-only (Opus S6 (i), IsWinningTrade).</summary>
		public enum HoldReason { None, CommitWindow, Coalition, AllyEngaged }

		/// <summary>
		/// A2/A4 for a committed squad whose legacy ShouldFlee fired, in this order:
		/// - a decisive loss flees first (Opus ruling b, review C1): None when neither the coalition ratio (own squad plus own and
		///   allied-bot units nearby) nor the ratio that also counts engaged allied squads reaches FleeRatio, whatever the commit
		///   window or the trade say;
		/// - in a fight, inside the MinCommitTicks window with less than CommitBreakLossPct of the committed value lost: CommitWindow;
		/// - the coalition ratio at or above FleeRatio: Coalition;
		/// - otherwise the engaged-ally ratio, which is then at or above FleeRatio: AllyEngaged.
		/// On the march (onMarch, review C3) the window is not a reason: only Coalition and AllyEngaged, which weigh the observed
		/// defences at the target, cancel a remembered-defence verdict. As the decisive-loss rule comes first, the window never
		/// turns a flight into a hold by itself; it names the hold. The ON rule only ever cancels a legacy flight, never starts one.
		/// </summary>
		public static HoldReason HoldsInFight(int ticksSinceCommit, int minCommitTicks, long committedValue, long ownValueLost,
			int commitBreakLossPct, double coalitionRatio, double allyEngagedRatio, int fleeRatioPct, bool onMarch)
		{
			if (coalitionRatio * 100 < fleeRatioPct && allyEngagedRatio * 100 < fleeRatioPct)
				return HoldReason.None;

			if (!onMarch && ticksSinceCommit >= 0 && ticksSinceCommit < minCommitTicks && !CommitBroken(committedValue, ownValueLost, commitBreakLossPct))
				return HoldReason.CommitWindow;

			if (coalitionRatio * 100 >= fleeRatioPct)
				return HoldReason.Coalition;

			return HoldReason.AllyEngaged;
		}

		/// <summary>Report-only (Opus S6 (i)), for A6 telemetry: a squad wins its trade when the enemy value killed since the
		/// commit exceeds its own committed value lost. An even trade is not winning. It never holds a squad in.</summary>
		public static bool IsWinningTrade(long enemyValueKilled, long ownValueLost) => enemyValueKilled > ownValueLost;

		/// <summary>The commit window breaks once the squad has lost CommitBreakLossPct of its committed value (or everything
		/// it committed was worthless, which cannot hold a window open).</summary>
		public static bool CommitBroken(long committedValue, long ownValueLost, int commitBreakLossPct) =>
			committedValue <= 0 || ownValueLost * 100 >= committedValue * commitBreakLossPct;

		/// <summary>
		/// A2 forward regroup point: on the line from the target to the squad, StandoffLength from the target. A squad farther
		/// out moves forward to it (never home); a squad already inside the stand-off pulls back to it, a short step.
		/// The result is integer WPos math, so it is deterministic across platforms.
		/// </summary>
		public static WPos RegroupPoint(WPos squad, WPos target, int standoffLength)
		{
			var delta = squad - target;
			var length = delta.HorizontalLength;
			if (length <= 0)
				return squad;

			return new WPos(
				target.X + (int)((long)delta.X * standoffLength / length),
				target.Y + (int)((long)delta.Y * standoffLength / length),
				squad.Z);
		}
	}
}
