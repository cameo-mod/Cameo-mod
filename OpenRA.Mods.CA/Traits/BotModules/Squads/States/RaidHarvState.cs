#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>
	/// RAID-HARV-01 P2b: the state of a squad that is on a harvester raid. It holds the raid's run record and delegates every tick to the squad manager's raid step (the logic and its fog rules live in SquadManagerBotModuleCA.RaidHarvRun.cs).
	/// The squad enters it from an idle state through the raid's pick and leaves it only through the raid's release (sequence bump, non-queued Stop, then the normal idle state).
	/// </summary>
	class RaidHarvState : GroundStateBaseCA, IState
	{
		internal readonly RaidRun Run;

		public RaidHarvState(RaidRun run)
		{
			Run = run;
		}

		public void Activate(SquadCA owner) { }

		public void Tick(SquadCA owner)
		{
			if (!owner.IsValid)
				return;

			owner.SquadManager.RaidTick(owner, Run, () => HomeLocation(owner));
		}

		// Left by anything other than the raid's own release (a dismissal, another module's order): the run ends and issues nothing more.
		public void Deactivate(SquadCA owner) => Run.Intent.Release(null, null);
	}
}
