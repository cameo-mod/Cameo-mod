using System.Collections.Generic;
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
		internal static EconomyQueueEvidenceKind Classify(in BotQueueTransition transition,
			bool terminalEpisodeResolved = false)
		{
			if (transition.Kind == BotQueueTransitionKind.Removed)
				// A Removed behind the episode's recorded resolution (cancel intent, proven
				// cancellation, proven placement) is the dequeue of an outcome already on
				// record, and an event-local proven removal cause — Destruction,
				// Elimination, Captured (owner changed on a live producer) or
				// Unbuildable (item absent from its own live queue's BuildableItems,
				// the engine's own eviction predicate) — is accounted evidence, not
				// missing data. Only an unresolved Removed with no proven cause is an
				// unproven disappearance and stays UNKNOWN.
				return terminalEpisodeResolved
					|| transition.CancellationClass is BotQueueCancellationClass.Destruction
						or BotQueueCancellationClass.Elimination
						or BotQueueCancellationClass.Captured
						or BotQueueCancellationClass.Unbuildable
					? EconomyQueueEvidenceKind.Observation
					: EconomyQueueEvidenceKind.Unknown;
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
			BotQueueCancellationClass.Captured => "captured",
			BotQueueCancellationClass.Unbuildable => "unbuildable",
			_ => null
		};

		// Amortized canonical queue resolution: one bounded own-queue enumeration per world
		// tick instead of one scan per proven cancellation. The fail-closed contract is
		// unchanged — more than maximumQueues own queues poisons the index (null), and a
		// missing or ambiguous (producer, group) pair resolves to null.
		internal static Dictionary<(uint Producer, string Group), string> BuildCancellationQueueIndex(
			IEnumerable<(uint Producer, string QueueType, string Group)> ownQueues, int maximumQueues)
		{
			var index = new Dictionary<(uint, string), string>();
			var count = 0;
			foreach (var (producer, queueType, group) in ownQueues)
			{
				if (++count > maximumQueues)
					return null;
				var key = (producer, group);
				index[key] = index.ContainsKey(key) ? null : $"{producer}:{queueType}:{group}";
			}
			return index;
		}

		internal static string ResolveCancellationQueueId(
			IReadOnlyDictionary<(uint Producer, string Group), string> index, uint producer, string group) =>
			index != null && index.TryGetValue((producer, group), out var id) ? id : null;
	}
}
