#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System.Diagnostics;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.CA.Traits
{
	partial class SquadManagerBotModuleCA
	{
		// Q1 (CONSOLIDATE-02, DEV only): one launch_source row per squad launch or state launch, path id L01-L20.
		// [Conditional]: every call site and its arguments are removed from a release build, so release adds no state,
		// RNG use or allocation. The row goes to the DEV team-event writer (DiagnosticTeamEvent; null in release).
		// squad: the launched squad, joined to its squad rows by DiagnosticSquadId; null for a team-level launch (L08, L20).
		[Conditional("CAMEO_DEV_LOGGING")]
		internal void LaunchSourceRecord(SquadCA squad, string path, string role, int units)
		{
			if (DiagnosticWriter == null)
				return;

			DiagnosticTeamEvent("launch", "source=" + path + " role=" + role + " units=" + units, squad == null ? null : DiagnosticSquadId(squad));
		}

		// Q1 fix-up (NIT 2): the exit of a staged Rush from its stage state is not a launch; the entry already has its L09/L10 launch row.
		[Conditional("CAMEO_DEV_LOGGING")]
		internal void StageExitRecord(SquadCA squad, string path, string role, int units)
		{
			if (DiagnosticWriter == null)
				return;

			DiagnosticTeamEvent("stage_exit", "source=" + path + " role=" + role + " units=" + units, squad == null ? null : DiagnosticSquadId(squad));
		}
	}
}
