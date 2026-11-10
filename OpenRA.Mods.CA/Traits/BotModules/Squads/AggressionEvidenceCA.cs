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
	/// <summary>
	/// IMPL02 A6 telemetry (AGGRESSION_PLAN_01 section 3 A6, ADDENDUM_01): the numbers an aggression rule held at the moment a squad
	/// retreated. Written by SquadManagerBotModuleCA.AggressionRecordEvidence only on the switch-ON aggression paths, read once by
	/// NoteRetreatStart in the same tick and put on the retreat_start row; the OFF arm never creates one, so its rows keep their bytes.
	/// Pure data and pure conversions: no world access, no RNG.
	/// </summary>
	public sealed class AggressionEvidenceCA
	{
		/// <summary>A ratio is capped on the wire: a squad with no armed enemy in sight has an infinite ratio and JSON has no infinity.</summary>
		public const int RatioMilliCap = 1000000;

		/// <summary>World tick the numbers were measured at. Evidence of any other tick is stale and is never logged.</summary>
		public int Tick = -1;

		/// <summary>The squad alone against the enemies the rule looked at (what the legacy engage call would have used), in thousandths.</summary>
		public int PredictedRatioMilli;

		/// <summary>The squad plus its coalition (counting engaged allies where the rule counted them) against the same enemies, in thousandths.</summary>
		public int CoalitionRatioMilli;

		/// <summary>flee, regroup or attack: where CoalitionRatioMilli sat against the Flee and Engage percentages.</summary>
		public string Band;

		/// <summary>Enemy value killed minus own value lost since the squad's commit; null for a squad that was not committed. Positive = winning the trade.</summary>
		public long? TradeDelta;

		/// <summary>Armed units of engaged own or allied-bot ground squads within AggressionAllyEngagedCells of the squad; 0 = nobody was fighting nearby.</summary>
		public int AlliesEngaged;

		/// <summary>Faction ids (Player.Faction.InternalName) of the retreating seat and of the seat its squad was engaging (null without a target).</summary>
		public string OwnFaction;
		public string EnemyFaction;

		public bool IsFresh(int tick) => Tick >= 0 && Tick == tick;

		/// <summary>A ratio as thousandths, rounded to nearest, never negative and never above RatioMilliCap (NaN reads as 0).</summary>
		public static int ToMilli(double ratio) =>
			double.IsNaN(ratio) ? 0 : (int)Math.Max(0, Math.Min(RatioMilliCap, Math.Round(ratio * 1000)));

		public static string BandName(AggressionBandCA band) => band switch
		{
			AggressionBandCA.Flee => "flee",
			AggressionBandCA.Regroup => "regroup",
			AggressionBandCA.Attack => "attack",
			_ => throw new ArgumentOutOfRangeException(nameof(band), band, "not an aggression band")
		};
	}
}
