#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ENG-T (maintainer 2026-09-30): an infiltration RUN — one transport (APC, transport helicopter) carries 1-5 capturers
	/// into the enemy base along a route around the enemy army and defences and visits several buildings: at each it drops
	/// ONE capturer right next to the building, who starts capturing at once, and drives on to the next. A transport that
	/// can only unload everyone at once (the stock "Unload" order; the engine's UnloadCargo activity supports a single
	/// passenger only when issued as such) drops them all at the first stop, and each capturer then runs to its OWN
	/// building. The contract is neutral (CA) so the engineer owner (Cameo) never depends on a transport implementation;
	/// the intended provider is Fransbot's capture-transport service (FB1/FB2, DESIGN §22: one transport system).
	/// The provider owns every passenger of an accepted run until that passenger is delivered or the run aborts.
	/// </summary>
	public interface IBotCaptureTransportProvider
	{
		/// <summary>
		/// Carry `passengers[i]` to `targets[i]` (same length, 1-5; targets in visiting order). False when it cannot now
		/// (no craft, no safe route): the caller sends them on foot.
		/// </summary>
		bool TryRequestCaptureRun(IBot bot, IReadOnlyList<Actor> passengers, IReadOnlyList<Actor> targets);

		/// <summary>
		/// ENGDROP: side-effect-free readiness. True when an EXISTING craft could start this run now against these targets and the
		/// hard exclusion constraints. No order, no production request, no lease, no mission, no random number. A provider that does not
		/// implement it answers false (fail closed): an opportunity drop never queues a transport.
		/// </summary>
		bool CanStartCaptureRun(IReadOnlyList<Actor> passengers, IReadOnlyList<Actor> targets, DropConstraints constraints) => false;

		/// <summary>
		/// ENGDROP: the run request with hard exclusion constraints. Commit revalidates a stale readiness answer. queueProduction false (Trigger A) = existing craft or a refusal that
		/// leaves nothing behind; true (a successful Trigger B roll) may request a transport on an appropriate no-craft refusal.
		/// runIdentity names this run's generation for <see cref="RequestAbort"/> and is null on refusal.
		/// </summary>
		bool TryRequestConstrainedCaptureRun(IBot bot, IReadOnlyList<Actor> passengers, IReadOnlyList<Actor> targets,
			DropConstraints constraints, bool queueProduction, out string runIdentity)
		{
			runIdentity = null;
			return false;
		}

		/// <summary>
		/// ENGDROP: ask the provider to abort a run it owns (the owner saw the evidence contradicted). Idempotent: only the current
		/// owner and generation of the passenger's run is honoured, a second request or a stale identity returns false. The provider
		/// takes its existing safe retreat path, then hands the passengers back once.
		/// </summary>
		bool RequestAbort(IBot bot, string runIdentity, Actor passenger, string reason) => false;

		/// <summary>True while the passenger waits for, rides in or is being unloaded from a transport.</summary>
		bool IsHandlingPassenger(Actor passenger);

		/// <summary>True exactly once after the passenger was dropped for its target; the caller then orders the capture.</summary>
		bool TryConsumeDelivered(Actor passenger, out Actor target);
	}
}
