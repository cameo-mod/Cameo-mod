using System.Collections.Generic;
using System.Linq;
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
		public void UnresolvedRemovedWithoutProvenCauseRemainsUnknown(BotQueueCancellationClass cls)
		{
			// An item that vanishes while its proof is still armed — no recorded cancel
			// intent, no proven placement, no event-local removal cause — is UNKNOWN.
			var t = Transition(BotQueueTransitionKind.Removed, cls: cls);
			Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.Unknown));
			Assert.That(AiEconomyQueueEvidence.Classify(in t, false), Is.EqualTo(EconomyQueueEvidenceKind.Unknown));
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out _), Is.False);
		}

		[TestCase(BotQueueCancellationClass.Destruction)]
		[TestCase(BotQueueCancellationClass.Elimination)]
		public void RemovedWithProvenEventLocalRemovalCauseObserves(BotQueueCancellationClass cls)
		{
			// A collapsing seat's queue items leave with a proven event-local removal
			// cause — the same evidence standard a Cancelled row carries. The removal is
			// accounted, not missing data, so the tail stays complete.
			var t = Transition(BotQueueTransitionKind.Removed, cls: cls);
			Assert.That(AiEconomyQueueEvidence.Classify(in t), Is.EqualTo(EconomyQueueEvidenceKind.Observation));
			Assert.That(AiEconomyQueueEvidence.Classify(in t, false), Is.EqualTo(EconomyQueueEvidenceKind.Observation));
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out _), Is.False);
		}

		[TestCase(BotQueueCancellationClass.None)]
		[TestCase(BotQueueCancellationClass.Production)]
		public void RemovedAfterResolvedEpisodeIsBookkeeping(BotQueueCancellationClass cls)
		{
			// The t10840 leaf case: a Ready item the module withdrew with recorded
			// CancelOrdered intent dequeues later as Removed. The outcome is already on
			// record — the dequeue adds nothing and must not poison the capture.
			var t = Transition(BotQueueTransitionKind.Removed, cls: cls);
			Assert.That(AiEconomyQueueEvidence.Classify(in t, true), Is.EqualTo(EconomyQueueEvidenceKind.Observation));
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
		public void CancellationQueueIndexResolvesUniqueProducerGroup()
		{
			var index = AiEconomyQueueEvidence.BuildCancellationQueueIndex(new[]
			{
				(17u, "Building", "Building"), (23u, "Vehicle", "Vehicle"), (31u, "Building", "Building"),
			}, AiEconomyHealthState.MaximumQueues);

			Assert.That(index, Is.Not.Null);
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(index, 17, "Building"),
				Is.EqualTo("17:Building:Building"));
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(index, 31, "Building"),
				Is.EqualTo("31:Building:Building"), "same group on different producers stays distinct");
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(index, 23, "Vehicle"),
				Is.EqualTo("23:Vehicle:Vehicle"));
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(index, 17, "Vehicle"), Is.Null,
				"a group the producer does not own cannot resolve");
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(index, 99, "Building"), Is.Null);
		}

		[Test]
		public void CancellationQueueIndexAmbiguityAndOverflowFailClosed()
		{
			var ambiguous = AiEconomyQueueEvidence.BuildCancellationQueueIndex(new[]
			{
				(17u, "Building", "Building"), (17u, "Building", "Building"),
			}, AiEconomyHealthState.MaximumQueues);
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(ambiguous, 17, "Building"), Is.Null,
				"two queue traits claiming one producer+group can never bind a canonical id");

			var overflow = AiEconomyQueueEvidence.BuildCancellationQueueIndex(
				System.Linq.Enumerable.Range(0, AiEconomyHealthState.MaximumQueues + 1)
					.Select(i => ((uint)i + 1, "Building", "Building")), AiEconomyHealthState.MaximumQueues);
			Assert.That(overflow, Is.Null, ">MaximumQueues own queues poisons the whole index");
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(overflow, 1, "Building"), Is.Null);
			Assert.That(AiEconomyQueueEvidence.ResolveCancellationQueueId(null, 1, "Building"), Is.Null);
		}

		[Test]
		public void EmitOffGridTerminalCoverageFixtures()
		{
			// A terminal observe at a tick not divisible by 50 still emits its own pulse and
			// end record; a transition callback on the same tick lands before them.
			var terminal = new List<string>
			{
				AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.Pulse(Identity(1, 50, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.Pulse(Identity(2, 100, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
			};
			var t = Transition(BotQueueTransitionKind.Cancelled, episode: 4);
			Assert.That(AiEconomyQueueEvidence.TryCancellation(in t, "17:Building:Building", out var evidence), Is.True);
			terminal.Add(AiEconomyHealthSchema.Cancel(Identity(3, 137, "cancel"), evidence));
			terminal.Add(AiEconomyHealthSchema.Pulse(Identity(4, 137, "pulse"), true, true,
				System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100));
			terminal.Add(AiEconomyHealthSchema.End(Identity(5, 137, "end"), true));
			AiEconomyHealthSchemaTest.WriteCliFixture("terminal", terminal);

			// The terminal watermark is only covered by a pulse within 50 ticks; a gap fails closed.
			AiEconomyHealthSchemaTest.WriteCliFixture("terminal-gap", new[] {
				AiEconomyHealthSchema.Pulse(Identity(0, 0, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.Pulse(Identity(1, 50, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.End(Identity(2, 137, "end"), true) });

			// Capture must start at world tick zero; a late first pulse is not coverage.
			AiEconomyHealthSchemaTest.WriteCliFixture("start-gap", new[] {
				AiEconomyHealthSchema.Pulse(Identity(0, 37, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.Pulse(Identity(1, 87, "pulse"), true, true,
					System.Array.Empty<EconomyQueueSnapshot>(), 5000, 0, 1000, 100),
				AiEconomyHealthSchema.End(Identity(2, 87, "end"), true) });
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
