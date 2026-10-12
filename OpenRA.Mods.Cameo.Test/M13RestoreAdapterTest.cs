using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class M13RestoreAdapterTest
	{
		static AttackLivenessEvalCA Waiting()
		{
			var clock = new AttackLivenessEvalCA();
			clock.Observe(100, true, true, true, true, 1000, 1000, 7500, 150, AttackSoftRestraintCA.TeamResponse);
			return clock;
		}

		static TeamResponseBudgetCA ClaimedBudget()
		{
			var budget = new TeamResponseBudgetCA();
			Assert.That(budget.TryAcquire(200, "m13:0", "2:1:p:1:5", 1500, true), Is.True);
			return budget;
		}

		static M13PlayerSnapshotCA PlayerSnapshot(
			AttackLivenessSnapshotCA liveness = null,
			TeamResponseBudgetSnapshotCA budget = null,
			long lastWaveId = 12, long baseDangerUntil = -1, string identity = "Multi1") =>
			new(1, identity, lastWaveId, baseDangerUntil,
				liveness ?? Waiting().Capture(),
				budget ?? new TeamResponseBudgetCA().Capture(identity));

		static M13MembershipSnapshotCA Membership(
			long maxSquadWave = 0, int intentFlags = 0, long intentWave = 0,
			long pendingWave = 0, long lastIssuedWave = 0, long lastWaveId = 12,
			int observedNoWave = 0, int corrupt = 0,
			int responseFlags = 0, int responseSquads = 0,
			string activeKey = null, string owner = null,
			string claimant = null, string key = null) =>
			new(maxSquadWave, intentFlags, intentWave, pendingWave, lastIssuedWave,
				lastWaveId, observedNoWave, corrupt, responseFlags, responseSquads,
				activeKey, owner, claimant, key);

		[Test]
		public void PlayerRecordRoundTripsIdentityCounterBudgetAndClock()
		{
			var state = new M13PlayerStateCA();
			var budget = ClaimedBudget().Capture("Multi1");
			var pending = Waiting();
			pending.NoteDispatchIntent(7);
			var snapshot = PlayerSnapshot(pending.Capture(), budget, lastWaveId: 9, baseDangerUntil: 400);
			Assert.That(state.TryRestore(8000, "Multi1", snapshot, 50), Is.True);
			Assert.That(state.RestoreSeen, Is.True);
			Assert.That(state.EvidenceComplete, Is.True);
			Assert.That(state.LastWaveId, Is.EqualTo(9));
			Assert.That(state.BaseDangerUntilTick, Is.EqualTo(400));
			Assert.That(state.AllocateWave(), Is.EqualTo(10));
			Assert.That(state.Responses.ActiveKey, Is.EqualTo("2:1:p:1:5"));
			Assert.That(state.Responses.Owner, Is.EqualTo("m13:0"));
			Assert.That(state.Responses.ExpiresTick, Is.EqualTo(1700));
			var clock = state.Liveness.Capture();
			Assert.That(clock.PendingWave, Is.EqualTo(7));
			Assert.That(clock.DeadlineTick, Is.EqualTo(7600));
			Assert.That(state.TryRestore(8000, "Multi1", snapshot, 50), Is.False);
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		[TestCase(4)]
		[TestCase(5)]
		[TestCase(6)]
		public void MissingOrCorruptPlayerRecordLeavesEvidenceUnknown(int fault)
		{
			var valid = PlayerSnapshot();
			var corrupt = fault switch
			{
				0 => null,
				1 => valid with { Version = 2 },
				2 => valid with { PlayerIdentity = "Multi9" },
				3 => valid with { LastWaveId = -1 },
				4 => valid with { BaseDangerUntilTick = long.MaxValue },
				5 => valid with { LastWaveId = 0, Liveness = valid.Liveness with { LastIssuedWave = 4, PendingWave = 0, LastObservedWave = 0 } },
				_ => valid with { Liveness = valid.Liveness with { Phase = (AttackLivenessPhaseCA)99 } },
			};
			var state = new M13PlayerStateCA();
			Assert.That(state.TryRestore(8000, "Multi1", corrupt, 50), Is.False);
			Assert.That(state.RestoreSeen, Is.True);
			Assert.That(state.EvidenceComplete, Is.False);
			Assert.That(state.LastWaveId, Is.EqualTo(0));
		}

		[Test]
		public void CorruptBudgetTransportPoisonsWholePlayerEvidence()
		{
			var state = new M13PlayerStateCA();
			var bad = new TeamResponseBudgetSnapshotCA(9, "Multi1", 200, "k", "m13:0", 200, 400, []);
			var snapshot = PlayerSnapshot(budget: bad);
			Assert.That(state.TryRestore(8000, "Multi1", snapshot, 50), Is.False);
			Assert.That(state.EvidenceComplete, Is.False);
			// A half-restored clock stays inert behind the evidence gate.
			Assert.That(state.Liveness.ObserveOffensiveActivity(8001, 1, 2, true, true, true,
				state.EvidenceComplete), Is.False);
		}

		[Test]
		public void DeadPendingIntentResolvesAsDispatchFailureOnce()
		{
			var pending = Waiting();
			pending.NoteDispatchIntent(7);
			var snapshot = PlayerSnapshot(pending.Capture());
			var state = new M13PlayerStateCA();
			Assert.That(state.TryRestore(8000, "Multi1", snapshot, 50), Is.True);
			// The intent squad did not survive the save; its wave resolves as failed.
			var verdict = M13RestoreMembershipCA.Evaluate(Membership(
				pendingWave: 7, lastIssuedWave: 7, lastWaveId: state.LastWaveId));
			Assert.That(verdict, Is.EqualTo(M13MembershipVerdictCA.DeadPendingIntent));
			Assert.That(state.Liveness.NoteDispatchFailed(7), Is.True);
			Assert.That(M13RestoreMembershipCA.Evaluate(Membership(
				lastIssuedWave: 7, lastWaveId: state.LastWaveId)),
				Is.EqualTo(M13MembershipVerdictCA.Consistent));
		}

		[Test]
		public void ConsistentMembershipAcceptsWavesIntentAndResponse()
		{
			var pending = Waiting();
			pending.NoteDispatchIntent(7);
			var snapshot = pending.Capture();
			Assert.That(M13RestoreMembershipCA.Evaluate(Membership(
				maxSquadWave: 9, intentFlags: 1, intentWave: 7,
				pendingWave: snapshot.PendingWave, lastIssuedWave: 7,
				lastWaveId: 9, responseFlags: 1, responseSquads: 1,
				activeKey: "2:1:p:1:5", owner: "m13:1", claimant: "m13:1", key: "2:1:p:1:5")),
				Is.EqualTo(M13MembershipVerdictCA.Consistent));
		}

		[TestCase(0)]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		[TestCase(4)]
		[TestCase(5)]
		[TestCase(6)]
		[TestCase(7)]
		[TestCase(8)]
		[TestCase(9)]
		public void InconsistentMembershipNeverEnablesEvidence(int fault)
		{
			var snapshot = fault switch
			{
				0 => Membership(maxSquadWave: 13),
				1 => Membership(intentFlags: 2, intentWave: 7, pendingWave: 7, lastIssuedWave: 7),
				2 => Membership(intentFlags: 1, intentWave: 9, pendingWave: 7, lastIssuedWave: 9),
				3 => Membership(intentFlags: 1, intentWave: 7),
				4 => Membership(observedNoWave: 1),
				5 => Membership(corrupt: 1),
				6 => Membership(responseFlags: 1),
				7 => Membership(activeKey: "k", owner: "m13:0", responseFlags: 1),
				8 => Membership(activeKey: "k", owner: "m13:0", claimant: "m13:1",
					responseFlags: 1, responseSquads: 1, key: "k"),
				_ => Membership(activeKey: "k", owner: "m13:0", claimant: "m13:0",
					responseFlags: 1, responseSquads: 1, key: "other"),
			};
			Assert.That(M13RestoreMembershipCA.Evaluate(snapshot),
				Is.EqualTo(M13MembershipVerdictCA.Inconsistent));
		}
	}
}
