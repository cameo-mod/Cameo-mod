#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Globalization;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>Owner-produced claim admission identity. Default/sequence zero = UNKNOWN.
	/// Identity includes participant, provider and provider instance; never a tick or target.</summary>
	public readonly record struct BotClaimEpisode(string ParticipantId, string ProviderId, int ProviderInstance, long Sequence)
	{
		public bool IsKnown => !string.IsNullOrWhiteSpace(ParticipantId) && !string.IsNullOrWhiteSpace(ProviderId)
			&& ProviderInstance >= 0 && Sequence > 0;
		public bool BelongsTo(string participantId) => IsKnown && ParticipantId == participantId;
		public string OwnerId => !IsKnown ? null
			: ParticipantId.Length.ToString(CultureInfo.InvariantCulture) + ":" + ParticipantId
				+ ProviderId.Length.ToString(CultureInfo.InvariantCulture) + ":" + ProviderId
				+ ":" + ProviderInstance.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Record-only per-owner counter. Admit is called once when a new claim is accepted,
	/// not when the request is read/refreshed. No RNG, actor mutation or static shared state.</summary>
	public sealed class BotClaimEpisodeCounter
	{
		readonly BotClaimEpisode owner;
		long sequence;
		bool invalid;

		public BotClaimEpisodeCounter(string participantId, string providerId, int providerInstance)
		{
			invalid = string.IsNullOrWhiteSpace(participantId) || string.IsNullOrWhiteSpace(providerId) || providerInstance < 0;
			owner = new BotClaimEpisode(participantId, providerId, providerInstance, 0);
		}

		public BotClaimEpisode Admit()
		{
			if (invalid || sequence == long.MaxValue)
				return default;
			return owner with { Sequence = ++sequence };
		}

		// A legacy save restores no admission history. Refuse identity instead of recycling IDs.
		public void Invalidate() => invalid = true;
	}

	/// <summary>
	/// A standing guard job - e.g. an MCV driving to an expansion site or an outpost that needs
	/// a screen. The publisher re-emits the request while the job is live (short ExpiresTick);
	/// the squad manager releases the escort when no live request covers it any more. `Value`
	/// uses the same army_value scale as situation/army telemetry.
	/// </summary>
	public readonly record struct BotProtectionRequest(CPos Location, int Value, int ExpiresTick,
		BotClaimEpisode Episode = default);

	/// <summary>Published by modules that need a guard somewhere (expansion MCVs, outposts); read by the squad manager.</summary>
	public interface IBotProtectionRequestProvider
	{
		IReadOnlyList<BotProtectionRequest> ProtectionRequests { get; }
	}
}
