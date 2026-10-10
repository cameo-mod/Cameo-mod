using System.Globalization;
using OpenRA.Mods.CA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	internal enum EconomyQueueEvidenceKind { Observation, ProvenPlacement, ProvenCancellation, Unknown }
	internal readonly record struct EconomyCancellationEvidence(string QueueId, string ItemId, string Item,
		string Reason, bool PlayerActive, bool ProducerLive, string CancellationClass);

	// Consumes the independently accepted terminal seam; never promotes order intent or disappearance.
	internal static class AiEconomyQueueEvidence
	{
		internal static EconomyQueueEvidenceKind Classify(in BotQueueTransition transition)
		{
			if (transition.Kind == BotQueueTransitionKind.Removed)
				return EconomyQueueEvidenceKind.Unknown;
			if (transition.Kind is not (BotQueueTransitionKind.Placed or BotQueueTransitionKind.Cancelled))
				return EconomyQueueEvidenceKind.Observation;
			if (transition.ProducerActorId == 0 || transition.EpisodeId == 0 || transition.ProducerLive == null
				|| string.IsNullOrEmpty(transition.ItemId) || string.IsNullOrEmpty(transition.Queue))
				return EconomyQueueEvidenceKind.Unknown;
			if (transition.Kind == BotQueueTransitionKind.Placed)
				return EconomyQueueEvidenceKind.ProvenPlacement;
			return CancellationClass(transition.CancellationClass) == null
				? EconomyQueueEvidenceKind.Unknown : EconomyQueueEvidenceKind.ProvenCancellation;
		}

		internal static bool TryCancellation(in BotQueueTransition transition, string queueId,
			out EconomyCancellationEvidence evidence)
		{
			evidence = default;
			if (Classify(in transition) != EconomyQueueEvidenceKind.ProvenCancellation || string.IsNullOrEmpty(queueId))
				return false;
			evidence = new(queueId,
				"seam:" + transition.Category + ":" + transition.EpisodeId.ToString(CultureInfo.InvariantCulture),
				transition.ItemId, transition.Reason.ToString(), transition.PlayerActive,
				transition.ProducerLive.Value, CancellationClass(transition.CancellationClass));
			return true;
		}

		static string CancellationClass(BotQueueCancellationClass value) => value switch
		{
			BotQueueCancellationClass.Production => "production",
			BotQueueCancellationClass.Destruction => "destruction",
			BotQueueCancellationClass.Elimination => "elimination",
			_ => null
		};
	}
}
