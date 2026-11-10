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

// IMPL02 A6 telemetry (Frontrunner): the evidence an aggression rule held when a squad retreats, for the retreat_start row.
// Reached only from the switch-ON aggression paths (SquadManagerBotModuleCA.AggressionOn); the OFF arm never records any, so
// NoteRetreatStart finds none and its rows are unchanged.
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCA
	{
		static readonly ConditionalWeakTable<SquadCA, AggressionEvidenceCA> AggressionEvidenceTable = new();

		/// <summary>Evidence has one reader, NoteRetreatStart, which returns at once while diagnostics are off; so nothing is measured then
		/// (no extra ratio, ally scan or allocation on a switch-ON hold). Review A6-TELEMETRY-01 F1.</summary>
		bool AggressionEvidenceWanted => Info.StalemateDiagnosticsEnabled;

		/// <summary>The evidence recorded for this squad in the current tick, or null (never records, never allocates).</summary>
		internal AggressionEvidenceCA AggressionEvidenceFor(SquadCA squad) =>
			squad != null && AggressionEvidenceTable.TryGetValue(squad, out var evidence) && evidence.IsFresh(World.WorldTick) ? evidence : null;

		/// <summary>
		/// Record what the rule saw as the squad is about to retreat. `coalitionRatio` is the ratio that decided (with engaged allies where
		/// the rule counted them), `tradeDelta` is null for an idle squad. The squad-alone ratio is measured here, against the same enemies,
		/// so the row shows what the legacy engage call would have seen next to what the coalition saw.
		/// </summary>
		internal void AggressionRecordEvidence(SquadCA squad, AggressionBandCA band, double coalitionRatio, IReadOnlyList<Actor> enemies, long? tradeDelta)
		{
			if (!AggressionEvidenceWanted)
				return;

			var evidence = AggressionEvidenceTable.GetValue(squad, _ => new AggressionEvidenceCA());
			evidence.Tick = World.WorldTick;
			evidence.PredictedRatioMilli = AggressionEvidenceCA.ToMilli(AggressionRatio(squad, System.Array.Empty<Actor>(), enemies));
			evidence.CoalitionRatioMilli = AggressionEvidenceCA.ToMilli(coalitionRatio);
			evidence.Band = AggressionEvidenceCA.BandName(band);
			evidence.TradeDelta = tradeDelta;
			evidence.AlliesEngaged = AggressionAllyEngagedUnits(squad).Count;
			evidence.OwnFaction = Player.Faction?.InternalName;
			evidence.EnemyFaction = AggressionEnemyOf(squad)?.Faction?.InternalName;
		}

		/// <summary>A6 for a committed squad in a fight: the band is where the deciding ratio sat against the Flee and Engage percentages, the trade
		/// is the bot's own kill value since the commit minus the squad's own value lost (the same numbers AggressionWinningTrade reports).</summary>
		internal void AggressionRecordFightEvidence(SquadCA squad, double coalitionRatio, IReadOnlyList<Actor> enemies, long ownValueLost)
		{
			if (!AggressionEvidenceWanted)
				return;

			var band = AggressionPolicyCA.Band(coalitionRatio, AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), squad),
				AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionEngageRatioPct), squad));
			AggressionRecordEvidence(squad, band, coalitionRatio, enemies, AggressionKillsCost() - squad.AggressionCommitKills - ownValueLost);
		}
	}
}
