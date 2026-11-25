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

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// NO-CANCEL-01 (BS-03 commit 10, designs _17 and _17B): the pure decisions that keep a bot from removing a building that is in production.
	/// A removal for a choice reason becomes a hold or a finish; only a READY item with proof that no legal cell exists may still be removed
	/// (a physical impossibility with a full refund, not a choice). Row off = every function answers the legacy way, so the six pre-existing
	/// sites stay byte-identical; the callers pass the live state, nothing here reads the world. Each call site keeps its existing condition and
	/// adds this guard with <c>&amp;&amp;</c>: the guard can only withhold a removal.
	/// </summary>
	public static class NoCancelGatesCA
	{
		/// <summary>
		/// Whether a site may remove its item. Row off = true (today). Row on = only a READY item with an unplaceable proof
		/// (a live exhaustion certificate, the failure budget, or the relocation latch on a READY entry).
		/// </summary>
		public static bool MayCancel(bool rowOn, bool itemReady, bool unplaceableProof) =>
			!rowOn || (itemReady && unplaceableProof);

		/// <summary>
		/// The demand unwind (site 1): the verdict already carries the certificate proof, so only the readiness is added. A verdict of
		/// <see cref="DemandCancelDisposition.Issued"/> becomes <see cref="DemandCancelDisposition.ProtectionHold"/> while the entry is still
		/// in progress (row on); every other verdict passes through unchanged. The existing callers read a hold as "the item stays queued
		/// and the queue layer's placement sweep owns it", which is exactly "finish, never remove".
		/// </summary>
		public static DemandCancelDisposition DemandCancelGate(DemandCancelDisposition verdict, bool rowOn, bool entryDone) =>
			verdict == DemandCancelDisposition.Issued && !MayCancel(rowOn, entryDone, true)
				? DemandCancelDisposition.ProtectionHold
				: verdict;
	}
}
