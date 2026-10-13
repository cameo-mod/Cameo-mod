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
	/// <summary>
	/// One observed transition of a shadow frontier outpost (CB_frontier_observe, P3a —
	/// DESIGN_2026-10-12_protected_frontier §2/§6). <see cref="Cell"/> is the field's frozen
	/// first-seen centre — the outpost's identity (integrator ruling O1). <see cref="Stage"/> is
	/// the stage the record transitions INTO; <see cref="Reason"/> carries the transition cause —
	/// an entry reason ("claim_wanted"), a retire reason ("stall", "freeze_timeout") or a
	/// decline reason ("record_cap_full", "max_active", "watch_overflow"). <see cref="Episode"/>
	/// counts the record's own backoff episodes; <see cref="EscortSeq"/> is always 0 at P3a —
	/// the M13 episode counter is n ≥ 1, so 0 unambiguously reads "no escort episode exists".
	/// </summary>
	public readonly record struct FrontierStageRow(CPos Cell, string Stage, int CoverValue, int AtRiskValue, string Reason, int Episode, long EscortSeq);

	/// <summary>
	/// Record-only observer of the expansion planner's read-only shadow of the protected-frontier
	/// stage machine (CB_frontier_observe, P3a). Implemented by a world trait in OpenRA.Mods.Cameo
	/// (the player telemetry log); this assembly must not reference it by name.
	/// The observer must never feed a decision: the planner ignores everything about it, so an
	/// absent observer leaves behaviour bit-identical.
	/// </summary>
	public interface IBotFrontierStageObserver
	{
		/// <summary>
		/// The shadow stage machine recorded <paramref name="row"/> for <paramref name="owner"/> at
		/// <paramref name="tick"/> — a stage entry, a retire, or a censor decline.
		/// </summary>
		void FrontierStageChanged(Player owner, int tick, in FrontierStageRow row);
	}
}
