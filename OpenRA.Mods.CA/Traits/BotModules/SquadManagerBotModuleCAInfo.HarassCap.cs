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
	// HARASS-CAP-01 (USER P0, V02 RCA 2026-11-24). Every switch defaults to false (@classic keeps it); the six personalities set them in ai.yaml.
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("HARASS-CAP-01 (release switch; the DEV row is tl_harass_cap). On, ALL game: the guerrilla and harass squads of a seat together hold at most HarassCapPercent of its combat units; " +
			"a unit beyond the cap goes to the idle pool (the team blob) instead of a raid squad. Off = as before.")]
		public readonly bool HarassCapEnabled = false;

		[Desc("HARASS-CAP-01: the cap, in percent of the seat's combat units (the guerrilla personality sets 40). Range 0-100.")]
		public readonly int HarassCapPercent = 25;

		[Desc("HARASS-CAP-01 (release switch; the DEV row is tl_harass_min). On, ALL game: a quiet guerrilla/harass squad launches only with at least HarassMinUnits. " +
			"A smaller one holds; a guerrilla squad merges into a bigger quiet one with room; after HarassMinHoldTicks it goes to the idle pool (the blob). New guerrillas fill the largest open squad first. Off = as before.")]
		public readonly bool HarassMinEnabled = false;

		[Desc("HARASS-CAP-01: the smallest raid that launches. Range 1-10.")]
		public readonly int HarassMinUnits = 3;

		[Desc("HARASS-CAP-01: a quiet raid squad below HarassMinUnits holds this many ticks, then its units go to the pool. Range 300-6000.")]
		public readonly int HarassMinHoldTicks = 1500;

		[Desc("HARASS-CAP-01 (release switch; the DEV row is tl_raider_recall). On, from LateEscalationStartTick: the quiet or approaching guerrilla/harass squads of the team count into the blob and join the open team order; " +
			"a squad already in a fight finishes it. Needs TeamLeadEnabled. Off = the raiders keep raiding.")]
		public readonly bool TeamLeadRaiderRecallEnabled = false;
	}
}
