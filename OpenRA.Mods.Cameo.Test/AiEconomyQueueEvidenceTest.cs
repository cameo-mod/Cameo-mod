using System.Collections.Generic;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class AiEconomyQueueEvidenceTest
	{
		static BotQueueTransition Transition(BotQueueTransitionKind kind, uint episode = 1,
			bool active = true, bool? live = true, BotQueueCancellationClass cls = BotQueueCancellationClass.Production,
			uint producer = 17) => new(10, "refinery", "Building", BuildingType.Refinery,
				producer, episode, kind, BotQueueTransitionReason.DemandCancel, active, live, cls);

		[TestCase(BotQueueTransitionKind.PlacementOrdered)]
		[TestCase(BotQueueTransitionKind.CancelOrdered)]
		[TestCase(BotQueueTransitionKind.Queued)]
		[TestCase(BotQueueTransitionKind.Ready)]
		public void IntentAndNonTerminalObservationsCannotSerializeCancellation(BotQueueTransitionKind kind)
		{
			var t = Transition(kind);
			Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.Observation));
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out _), Is.False);
		}

		[TestCase(BotQueueCancellationClass.None)]
		[TestCase(BotQueueCancellationClass.Production)]
		[TestCase(BotQueueCancellationClass.Destruction)]
		[TestCase(BotQueueCancellationClass.Elimination)]
		public void RemovedWithAnyRequestAttributionRemainsUnknown(BotQueueCancellationClass cls)
		{
			var t = Transition(BotQueueTransitionKind.Removed, cls: cls);
			Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.Unknown));
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out _), Is.False);
		}

		[Test]
		public void ProvenPlacementDoesNotInventAnUnsupportedHealthRecord()
		{
			var t = Transition(BotQueueTransitionKind.Placed, cls: BotQueueCancellationClass.None);
			Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.ProvenPlacement));
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out _), Is.False);
		}

		[Test]
		public void MissingLivenessEpisodeProducerOrClassCannotProveCancellation()
		{
			var missing = new[] { Transition(BotQueueTransitionKind.Cancelled, live: null),
				Transition(BotQueueTransitionKind.Cancelled, episode: 0),
				Transition(BotQueueTransitionKind.Cancelled, producer: 0),
				Transition(BotQueueTransitionKind.Cancelled, cls: BotQueueCancellationClass.None) };
			foreach (var t in missing)
				Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.Unknown));
			var valid = Transition(BotQueueTransitionKind.Cancelled);
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in valid, null, out _), Is.False,
				"missing or ambiguous runtime queue resolution cannot enter canonical evidence");
		}

		[Test]
		public void EventLocalInactiveAndDestructionStateIsPreservedRatherThanPulseInferred()
		{
			var t = Transition(BotQueueTransitionKind.Cancelled, active: false, live: false,
				cls: BotQueueCancellationClass.Destruction);
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out var evidence), Is.True);
			using var row = JsonDocument.Parse(AiEconomyHealthSchema.Cancel(Identity(1, 10, "cancel"), evidence));
			Assert.That(row.RootElement.GetProperty("player_active").GetBoolean(), Is.False);
			Assert.That(row.RootElement.GetProperty("producer_live").GetBoolean(), Is.False);
			Assert.That(row.RootElement.GetProperty("cancellation_class").GetString(), Is.EqualTo("destruction"));
		}

		static EconomyHealthIdentity Identity(int seq, int tick, string kind) =>
			new(seq, tick, kind, "roundtrip", "hard", "map", "td_nod", "hard", true, 0);

		[TestCase("cancel-active", true, true, BotQueueCancellationClass.Production, false)]
		[TestCase("cancel-inactive", false, true, BotQueueCancellationClass.Elimination, false)]
		[TestCase("cancel-destroyed", true, false, BotQueueCancellationClass.Destruction, false)]
		[TestCase("cancel-duplicate", true, true, BotQueueCancellationClass.Production, true)]
		public void EmitPinnedCancellationCliFixtures(string name, bool active, bool live,
			BotQueueCancellationClass cls, bool duplicate)
		{
			var rows = new List<string> { AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"),
				true, true, System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 0) };
			var ids = new HashSet<string>();
			for (uint i = 1; i <= 3; i++)
			{
				var t = Transition(BotQueueTransitionKind.Cancelled, duplicate ? 1 : i, active, live, cls);
				Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out var evidence), Is.True);
				ids.Add(evidence.ItemId);
				rows.Add(AiEconomyHealthSchema.Cancel(Identity(rows.Count, (int)i * 10, "cancel"), evidence));
			}
			Assert.That(ids.Count, Is.EqualTo(duplicate ? 1 : 3), "same-name requeued episodes are distinct");
			rows.Add(AiEconomyHealthSchema.Pulse(Identity(rows.Count, 50, "pulse"), active, true,
				System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 0));
			rows.Add(AiEconomyHealthSchema.End(Identity(rows.Count, 50, "end"), true));
			AiEconomyHealthSchemaTest.WriteCliFixture(name, rows);
		}

		[Test]
		public void UnclassifiedRemovalPermanentlyMarksCoverageUnknown()
		{
			var state = new AiEconomyHealthState();
			var t = Transition(BotQueueTransitionKind.Removed);
			if (AiEconomyQueueEvidence.Classify(in t) == EconomyQueueEvidenceKind.Unknown)
				state.MarkIncomplete();
			state.Observe(0, System.Array.Empty<EconomyQueueObservation>());
			state.Observe(1, System.Array.Empty<EconomyQueueObservation>());
			Assert.That(state.Complete, Is.False);
			AiEconomyHealthSchemaTest.WriteCliFixture("removed-unknown", new[] {
				AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 0),
				AiEconomyHealthSchema.End(Identity(1, 0, "end"), state.Complete) });
		}
	}
}
