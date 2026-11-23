#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Globalization;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	// WAVE-MIN-01 (USER rule, 2026-11-23): the attack-wave launch gate. Only CreateAttackForce (moving out to attack the enemy) asks it;
	// base defence (ProtectOwn), DEFEND-ALLY responses, harvester retaliation, scouts and ENGDROP form their squads elsewhere and never do.
	public partial class SquadManagerBotModuleCA
	{
		AblationLatch ablateWaveMin;

		/// <summary>WAVE-MIN-01 is on for this bot: the Info switch and the DEV ablation row (row off = the old launch path).</summary>
		bool WaveMinActive => Info.WaveMinEnabled && !ablateWaveMin.Off(Player, AblationIds.WaveMin);

		(int Units, int Value) WaveMinBars()
		{
			var limits = botLimits?.Info;
			return (WaveMinCA.Effective(limits?.WaveMinUnits ?? -1, Info.WaveMinUnits),
				WaveMinCA.Effective(limits?.WaveMinValue ?? -1, Info.WaveMinValue));
		}

		/// <summary>True when the idle pool is below the tier's wave minimum, so the wave stays at home (the pool keeps growing meanwhile).</summary>
		bool WaveBelowMinimum()
		{
			if (!WaveMinActive)
				return false;

			var (minUnits, minValue) = WaveMinBars();
			if (!WaveMinCA.Limits(minUnits, minValue))
				return false;

			long value = 0;
			foreach (var u in unitsHangingAroundTheBase)
				value += UnitValue(u.Actor);

			return !WaveMinCA.Passes(unitsHangingAroundTheBase.Count, value, minUnits, minValue);
		}

		/// <summary>DEV launch record: pool units, summed cost, the bars and the roles. Null (nothing built) unless the DEV log is on.</summary>
		string WaveLaunchSummary()
		{
			if (!(WaveMinActive || WaveMixActive) || !DiagnosticSinkEnabled)
				return null;

			var (minUnits, minValue) = WaveMinActive ? WaveMinBars() : (0, 0);
			var roles = UnitRoles;
			var summary = WaveMinCA.RoleSummary(unitsHangingAroundTheBase.Select(u => roles?.PrimaryRoleOf(u.Actor.Info.Name)));
			return string.Create(CultureInfo.InvariantCulture,
				$"units={unitsHangingAroundTheBase.Count};value={unitsHangingAroundTheBase.Sum(u => (long)UnitValue(u.Actor))};min={minUnits}/{minValue};roles={summary}");
		}
	}
}
