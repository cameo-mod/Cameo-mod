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
	/// Hotspot #8 extraction (2026-10-04, NOTE_2026-10-04_nova_hotspot8_tree): the pure
	/// decision pieces of SquadManagerBotModuleCA.PrepositionDefenceTick — channel
	/// precedence, rally cell, hold expiry, the emergency ring, and the shared
	/// coalition-election responder match. Every member is behaviour-preserving; the
	/// impure parts (provider scans, broadcast collection, drafts, orders, mission
	/// records) stay in the tick.
	/// </summary>
	public enum PrepositionChannelCA
	{
		None,
		Threat,
		Request,
		DefendAnswer,
		AssistAnswer
	}

	public static class PrepositionDecisionEvalCA
	{
		/// <summary>The precedence ladder: a real incoming attack outranks an escort
		/// request, which outranks an ally defend answer, which outranks an assist.</summary>
		public static PrepositionChannelCA SelectChannel(bool hasThreat, bool hasRequest, bool hasDefend, bool hasAssist)
		{
			if (hasThreat)
				return PrepositionChannelCA.Threat;
			if (hasRequest)
				return PrepositionChannelCA.Request;
			if (hasDefend)
				return PrepositionChannelCA.DefendAnswer;
			if (hasAssist)
				return PrepositionChannelCA.AssistAnswer;
			return PrepositionChannelCA.None;
		}

		/// <summary>Escorts rally at the guarded point; threats rally at the nearest own
		/// defensive building inside the search ring (caller supplies it, or the threat
		/// cell itself when none is near).</summary>
		public static CPos RallyFor(BotProtectionRequest? request, CPos nearestDefenceOrTarget)
		{
			return request?.Location ?? nearestDefenceOrTarget;
		}

		/// <summary>Request path: the publisher's expiry, bounded by the rolling refresh
		/// window (10 ProtectIntervals). Threat path: the attack's ETA plus the same grace.</summary>
		public static int HoldUntilTick(BotProtectionRequest? request, BotPredictedThreat? threat, int nowTick, int protectInterval)
		{
			return request.HasValue
				? System.Math.Min(request.Value.ExpiresTick, nowTick + protectInterval * 10)
				: nowTick + (threat?.EtaTicks ?? 0) + protectInterval * 10;
		}

		/// <summary>The doorstep ring: a rally inside MaxBaseRadius of the base center is an
		/// emergency (full pool); outside it keeps the defend reserve (CA-2).</summary>
		public static bool IsEmergencyRally(CPos rally, CPos baseCenter, int maxBaseRadiusCells)
		{
			return (rally - baseCenter).LengthSquared <= (long)maxBaseRadiusCells * maxBaseRadiusCells;
		}

		/// <summary>A coalition election assignment is mine when it names me, or when the
		/// pre-id broadcast fallback marks my ClientIndex.</summary>
		public static bool IsElectedResponder(string responderId, int responderClientIndex, string myId, int myClientIndex)
		{
			return responderId == myId || (responderId == null && responderClientIndex == myClientIndex);
		}
	}
}
