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

	/// <summary>
	/// Group-name registry for the ABLATE-01 hook (mirror of tools/ablate/ablate_groups.yaml). Inert constants in release.
	/// One id per independently ablatable feature or merge group. Rules: lowercase `[a-z0-9_]`, prefixed by the owning area
	/// (`pi_` PROD-IDLE G1, add your own, e.g. `cn_` CN waves, `bn_` BOT-NEXT, `ed_` ENGDROP), no comma, `@`, `;` or space (the
	/// lobby value is a comma list with an optional `@team<k>` suffix). The class is partial so each owner adds a
	/// constants-only file of its own (`AblationIds.<Area>.cs`) and nobody edits another owner's file. With the DEV half
	/// (ABLATE-01, not in this commit) an id that is in the lobby value but missing from the registry yaml aborts the DEV
	/// start (fail closed), so register every id in both places.
	/// </summary>
	public static partial class AblationIds
	{
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
