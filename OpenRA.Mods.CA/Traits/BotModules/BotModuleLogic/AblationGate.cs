#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ABLATE-01 hook, release-safe shared half (ABLATE_01_PLAN_03 section 2). The DEV assembly provides an
	/// implementation (the trait that reads the synced lobby option); a release build has none, so
	/// <see cref="AblationGate.Off"/> is a constant false with no option parsing and no #if at any call site.
	/// </summary>
	public interface IAblationProvider
	{
		/// <summary>True when the registry id (or a row that needs it) is switched OFF for this owner's team in this DEV game.</summary>
		bool IsOff(Player owner, string id);
	}

	/// <summary>Registry ids of the PROD-IDLE G1 rows (tools/ablate/ablate_groups.yaml). Inert constants in release.</summary>
	public static class AblationIds
	{
		public const string FairProducer = "pi_fair_producer";
		public const string ArmyRatio = "pi_army_ratio";
		public const string Overflow = "pi_overflow";
		public const string Lean = "pi_lean";
		public const string DemandCapturers = "pi_demand_capturers";
		public const string RoleMix = "pi_role_mix";
		public const string DerivedWeights = "pi_derived_weights";
		public const string QueueDepth = "pi_queue_depth";
		public const string Recovery = "pi_recovery";
	}

	public static class AblationGate
	{
		/// <summary>
		/// Whether the feature <paramref name="id"/> is ablated (OFF) for <paramref name="owner"/>. Absent provider
		/// (every release build, every DEV game without the option) = false = the full bundle runs. Callers cache the
		/// answer once (<see cref="AblationLatch"/>): the lobby option cannot change during a game.
		/// </summary>
		public static bool Off(Player owner, string id)
		{
			if (owner == null || string.IsNullOrEmpty(id) || owner.World?.WorldActor == null)
				return false;

			var provider = owner.World.WorldActor.TraitsImplementing<IAblationProvider>().FirstOrDefault();
			return provider != null && provider.IsOff(owner, id);
		}
	}

	/// <summary>Lazily resolved, cached <see cref="AblationGate.Off"/> answer for one (owner, id); no per-tick cost after the first call.</summary>
	public struct AblationLatch
	{
		byte state;

		public bool Off(Player owner, string id)
		{
			// An answer for no owner is not an answer: never cache it, so a call made before the owner is
			// known cannot pin the row ON for the whole game.
			if (state == 0 && owner != null)
				state = AblationGate.Off(owner, id) ? (byte)2 : (byte)1;

			return state == 2;
		}
	}
}
