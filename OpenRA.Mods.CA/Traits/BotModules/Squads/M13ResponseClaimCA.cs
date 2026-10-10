using System.Globalization;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// Record-only bridge from a published claim to the existing protection path.
	// A missing/stale election cannot invent an episode from a tick or location.
	public static class M13ResponseClaimCA
	{
		public static BotProtectionRequest Synthesize(CPos rally, int value, int expiresTick,
			string participant, BotClaimEpisode published, BotClaimEpisode elected = default,
			bool electionRequired = false)
		{
			var episode = published.BelongsTo(participant)
				&& (!electionRequired || elected == published) ? published : default;
			return new BotProtectionRequest(rally, value, expiresTick, episode);
		}

		// The response consumer uses a stable owner+admission+kind key, never rally
		// or expiry. This does not itself admit a response or allocate any units.
		public static bool TryKey(PrepositionChannelCA channel, BotProtectionRequest request,
			string participant, out string key)
		{
			key = null;
			if (channel is not (PrepositionChannelCA.Request or PrepositionChannelCA.DefendAnswer or PrepositionChannelCA.AssistAnswer)
				|| !request.Episode.BelongsTo(participant))
				return false;
			key = ((int)channel).ToString(CultureInfo.InvariantCulture) + ":"
				+ request.Episode.OwnerId + ":" + request.Episode.Sequence.ToString(CultureInfo.InvariantCulture);
			return true;
		}
	}
}
