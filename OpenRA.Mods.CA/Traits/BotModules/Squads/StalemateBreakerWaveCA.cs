// BOTPASSIVE-FIX01 draft: deterministic seat-wave state machine, not wired into a manager.
// Design _06 §2, _08 §3 / N3 / N10, _09 LA / LB / LF / LG. No world access, no orders.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public enum StalemateWaveEndCA
	{
		None, Complete, MissingEvidence, IdentityChanged, Emergency, TargetLost, Casualties, NoProgress, Deadline,
		RallyFailed, RetargetFailed
	}

	/// <summary>Rally: before the rally end. Advancing: rally passed, before the arm tick. Armed: no-progress clock running.</summary>
	public enum StalemateWavePhaseCA
	{
		Rally, Advancing, Armed
	}

	/// <summary>Immutable captured member identity and original cost, in credits.</summary>
	public readonly record struct StalemateWaveMemberCA(uint ActorId, uint Generation, int InitialCost);

	/// <summary>
	/// Seat-owned wave accounting. This type does not observe the world or issue orders.
	/// A caller must supply only verified seat-legal observations and persist this state when enabled.
	/// </summary>
	public sealed class StalemateBreakerWaveCA
	{
		public const int SampleInterval = 75;
		public const int ProgressCells = 2;

		// Sorted by ActorId; the dictionary is lookup-only and never iterated for output.
		readonly StalemateWaveMemberCA[] roster;
		readonly Dictionary<(uint Id, uint Generation), int> members = new();
		readonly Dictionary<uint, uint> generationById = new();
		readonly HashSet<(uint Id, uint Generation)> dead = new();
		readonly HashSet<(int X, int Y)> leaderCells = new();

		public readonly int C;
		public readonly int E;
		public readonly int RallyDuration;
		public readonly int NoProgressDuration;
		public readonly int AbsoluteDuration;
		public readonly int RallyDeadline;
		public readonly int AbsoluteDeadline;
		public readonly int ArmTick;
		public readonly int InitialCount;
		public readonly long InitialValue;
		public readonly int N0;
		public readonly long V0;
		public readonly int RouteEstL;

		public StalemateWavePhaseCA Phase { get; private set; }
		public int LastProgressTick { get; private set; }
		public int BestL { get; private set; }
		public int LastSampleTick { get; private set; }
		public bool Arrived { get; private set; }
		public bool RetargetUsed { get; private set; }
		public uint LeaderId { get; private set; }
		public long ConfirmedDeadValue { get; private set; }
		public long DeadCost => ConfirmedDeadValue;
		public int RouteBestL { get; private set; }
		public int RouteActualCells => leaderCells.Count;
		public StalemateWaveEndCA End { get; private set; }

		public IReadOnlyList<StalemateWaveMemberCA> Members => roster;

		/// <summary>Number of confirmed-dead members (IMPL02 L1).</summary>
		public int DeadCount => dead.Count;

		/// <summary>
		/// Actor ids of the confirmed-dead members in ascending id order (IMPL02 L1): lets a manager whose own reported-dead set is
		/// empty (after a load or a personality handoff) derive it from the wave instead of re-reporting the same deaths.
		/// </summary>
		public IEnumerable<uint> DeadActorIds => roster.Where(m => dead.Contains((m.ActorId, m.Generation))).Select(m => m.ActorId);

		public StalemateBreakerWaveCA(int commitTick, IEnumerable<StalemateWaveMemberCA> initialMembers,
			StalemateBreakerPolicyCA.TimingCA timing, int n0, long v0, int routeEstL, uint leaderId)
		{
			if (commitTick < 0 || initialMembers == null || timing.EstimatedTicks < 0 || timing.EstimatedTicks > 18000
				|| timing.RallyDuration < 750 || timing.RallyDuration > 5000
				|| timing.NoProgressDuration < 1500 || timing.NoProgressDuration > 5000
				|| timing.AbsoluteDuration < 3000 || timing.AbsoluteDuration > 30000 || routeEstL < 0 || leaderId == 0)
				throw new ArgumentException("Invalid initial wave.");

			var list = new List<StalemateWaveMemberCA>();
			long value = 0;
			foreach (var member in initialMembers)
			{
				if (member.ActorId == 0 || member.Generation == 0 || member.InitialCost < 0
					|| generationById.ContainsKey(member.ActorId))
					throw new ArgumentException("Invalid or duplicate initial identity.");
				generationById.Add(member.ActorId, member.Generation);
				members.Add((member.ActorId, member.Generation), member.InitialCost);
				list.Add(member);
				value = checked(value + member.InitialCost);
			}

			roster = list.OrderBy(m => m.ActorId).ToArray();
			InitialCount = roster.Length;
			InitialValue = value;
			if (InitialCount == 0 || InitialValue <= 0)
				throw new ArgumentException("Empty or zero-value wave.");
			if (n0 < InitialCount || v0 < InitialValue)
				throw new ArgumentException("N0/V0 must include every committed member.");
			if (!generationById.ContainsKey(leaderId))
				throw new ArgumentException("The leader must be a committed member.");

			C = commitTick;
			E = timing.EstimatedTicks;
			RallyDuration = timing.RallyDuration;
			NoProgressDuration = timing.NoProgressDuration;
			AbsoluteDuration = timing.AbsoluteDuration;
			RallyDeadline = checked(C + RallyDuration);
			AbsoluteDeadline = checked(C + AbsoluteDuration);
			ArmTick = checked(C + SampleInterval * ((RallyDuration + SampleInterval - 1) / SampleInterval));

			// Reserve headroom so later grid arithmetic (sample + noProgress) cannot overflow.
			_ = checked(AbsoluteDeadline + NoProgressDuration + SampleInterval);
			N0 = n0;
			V0 = v0;
			RouteEstL = routeEstL;
			RouteBestL = routeEstL;
			BestL = routeEstL;
			LeaderId = leaderId;
			LastProgressTick = C;
			LastSampleTick = C;
			Phase = StalemateWavePhaseCA.Rally;
		}

		/// <summary>
		/// Compatibility overload for the earlier draft signature (StalemateSeatOwnerTest): the policy's minimum
		/// timing (E 0, rally 750, no-progress 1500, absolute 3000), N0/V0 = the committed roster, L = 0, and the
		/// lowest-ActorId member as leader. Internal (IMPL02 L3): only the test assembly calls it (InternalsVisibleTo).
		/// </summary>
		internal StalemateBreakerWaveCA(int commitTick, IEnumerable<StalemateWaveMemberCA> initialMembers)
			: this(commitTick, initialMembers?.ToArray(),
				new StalemateBreakerPolicyCA.TimingCA(0, StalemateBreakerPolicyCA.RallyTicks,
					StalemateBreakerPolicyCA.NoMovementTicks, StalemateBreakerPolicyCA.WaveTicks), true) { }

		StalemateBreakerWaveCA(int commitTick, StalemateWaveMemberCA[] initialMembers,
			StalemateBreakerPolicyCA.TimingCA timing, bool compatibility)
			: this(commitTick, initialMembers, timing, initialMembers?.Length ?? 0,
				initialMembers?.Aggregate(0L, (sum, m) => checked(sum + Math.Max(0, m.InitialCost))) ?? 0, 0,
				initialMembers == null || initialMembers.Length == 0 ? 0 : initialMembers.Min(m => m.ActorId)) { }

		/// <summary>Alias of C for the earlier draft API.</summary>
		public int StartTick => C;

		public static string WireName(StalemateWaveEndCA end)
		{
			switch (end)
			{
				case StalemateWaveEndCA.None: return "none";
				case StalemateWaveEndCA.Complete: return "complete";
				case StalemateWaveEndCA.MissingEvidence: return "missing_evidence";
				case StalemateWaveEndCA.IdentityChanged: return "identity_changed";
				case StalemateWaveEndCA.Emergency: return "emergency";
				case StalemateWaveEndCA.TargetLost: return "target_lost";
				case StalemateWaveEndCA.Casualties: return "casualties";
				case StalemateWaveEndCA.NoProgress: return "no_progress";
				case StalemateWaveEndCA.Deadline: return "deadline";
				case StalemateWaveEndCA.RallyFailed: return "rally_failed";
				case StalemateWaveEndCA.RetargetFailed: return "retarget_failed";
				default: throw new ArgumentOutOfRangeException(nameof(end));
			}
		}

		public bool IsActive => End == StalemateWaveEndCA.None;

		/// <summary>True iff tick lies on the sample grid C + 75k with k &gt;= 1.</summary>
		public bool IsSampleTick(int tick) => tick > C && ((long)tick - C) % SampleInterval == 0;

		/// <summary>An unverified identity or death immediately revokes the exemption.</summary>
		public bool RecordDeath(uint id, uint generation, bool verified)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			var key = (id, generation);
			if (!verified || !members.TryGetValue(key, out var cost))
			{
				End = StalemateWaveEndCA.IdentityChanged;
				return false;
			}

			if (dead.Add(key))
				ConfirmedDeadValue = checked(ConfirmedDeadValue + cost);
			if (ConfirmedDeadValue > (InitialValue - 1) / 2)
				End = StalemateWaveEndCA.Casualties;
			return true;
		}

		/// <summary>Arrival never reduces either original denominator and never ends commitment.</summary>
		public bool ObserveArrival(IEnumerable<(uint Id, uint Generation)> identities)
		{
			if (End != StalemateWaveEndCA.None || identities == null)
				return false;
			var arrived = new HashSet<(uint, uint)>();
			long value = 0;
			foreach (var key in identities)
			{
				if (!members.TryGetValue(key, out var cost) || dead.Contains(key) || !arrived.Add(key))
				{
					End = StalemateWaveEndCA.IdentityChanged;
					return false;
				}

				value = checked(value + cost);
			}

			Arrived |= StalemateBreakerPolicyCA.Arrived(InitialCount, InitialValue, arrived.Count, value);
			return Arrived;
		}

		/// <summary>
		/// Called at tick == RallyDeadline. Pass iff the assembled initial members reach 60 % of N0 by count
		/// AND 60 % of V0 by value. Otherwise the wave ends rally_failed.
		/// </summary>
		public bool RallyEnd(int tick, int assembledCount, long assembledValue)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (Phase != StalemateWavePhaseCA.Rally || tick != RallyDeadline || assembledCount < 0
				|| assembledCount > InitialCount || assembledValue < 0 || assembledValue > InitialValue)
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			if (assembledCount >= StalemateBreakerPolicyCA.RequiredUnits(N0)
				&& StalemateBreakerPolicyCA.MeetsValueShare(assembledValue, V0))
			{
				Phase = StalemateWavePhaseCA.Advancing;
				return true;
			}

			End = StalemateWaveEndCA.RallyFailed;
			return false;
		}

		/// <summary>Every tick: the absolute deadline from C, and a missed rally end.</summary>
		public bool CheckDeadlines(int tick)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (tick >= AbsoluteDeadline)
				End = StalemateWaveEndCA.Deadline;
			else if (Phase == StalemateWavePhaseCA.Rally && tick > RallyDeadline)
				End = StalemateWaveEndCA.MissingEvidence;
			return End == StalemateWaveEndCA.None;
		}

		/// <summary>
		/// Progress sample at t = C + 75k (k &gt;= 1), contiguous. remainingL is the leader's remaining engine-path
		/// length in cells, or null when the target is unreachable at this sample.
		/// </summary>
		public bool ObserveSample(int t, int? remainingL, bool damageOrKillObservedInWindow)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (!IsSampleTick(t) || (long)t != (long)LastSampleTick + SampleInterval
				|| (remainingL.HasValue && remainingL.Value < 0))
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			LastSampleTick = t;
			if (t >= AbsoluteDeadline)
			{
				End = StalemateWaveEndCA.Deadline;
				return false;
			}

			// A missed rally end is missing evidence. At a shared tick RallyEnd must be called before ObserveSample.
			if (Phase == StalemateWavePhaseCA.Rally && t >= RallyDeadline)
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			if (remainingL.HasValue && remainingL.Value < RouteBestL)
				RouteBestL = remainingL.Value;

			if (t < ArmTick)
				return true;

			if (t == ArmTick)
			{
				if (Phase != StalemateWavePhaseCA.Advancing || !remainingL.HasValue)
				{
					End = StalemateWaveEndCA.MissingEvidence;
					return false;
				}

				Phase = StalemateWavePhaseCA.Armed;
				BestL = remainingL.Value;
				LastProgressTick = ArmTick;
				return true;
			}

			if (Phase != StalemateWavePhaseCA.Armed)
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			if (!Arrived)
			{
				if (remainingL.HasValue && remainingL.Value <= (long)BestL - ProgressCells)
				{
					BestL = remainingL.Value;
					LastProgressTick = t;
				}
			}
			else if (damageOrKillObservedInWindow)
				LastProgressTick = t;

			if ((long)t - LastProgressTick >= NoProgressDuration)
				End = StalemateWaveEndCA.NoProgress;
			return End == StalemateWaveEndCA.None;
		}

		/// <summary>Telemetry only (route_actual_cells). An off-grid or ended observation is ignored, never fatal.</summary>
		public bool ObserveLeaderCell(int t, int x, int y)
		{
			if (End != StalemateWaveEndCA.None || t < C || ((long)t - C) % SampleInterval != 0)
				return false;
			leaderCells.Add((x, y));
			return true;
		}

		/// <summary>The new leader re-bases bestL; lastProgressTick does not change (no free time).</summary>
		public bool LeaderChange(int r, uint newLeaderId, int remainingL)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (r < LastSampleTick || remainingL < 0)
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			if (!generationById.TryGetValue(newLeaderId, out var generation) || dead.Contains((newLeaderId, generation)))
			{
				End = StalemateWaveEndCA.IdentityChanged;
				return false;
			}

			LeaderId = newLeaderId;
			BestL = remainingL;
			return true;
		}

		/// <summary>One retarget per wave, never to a target with a larger E. Deadlines and the clock are unchanged.</summary>
		public bool TryRetarget(int r, int newE, int remainingL)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (r < LastSampleTick || remainingL < 0)
			{
				End = StalemateWaveEndCA.MissingEvidence;
				return false;
			}

			if (RetargetUsed || newE < 0 || newE > E)
			{
				End = StalemateWaveEndCA.RetargetFailed;
				return false;
			}

			RetargetUsed = true;
			BestL = remainingL;
			Arrived = false;
			return true;
		}

		/// <summary>
		/// Apply hard abort conditions before granting any exemption. No-progress is evaluated only on the
		/// sample grid (ObserveSample), never here.
		/// </summary>
		public bool Continue(int tick, bool complete, bool identityValid, bool emergency,
			bool targetKnown, bool targetDestroyed)
		{
			if (End != StalemateWaveEndCA.None)
				return false;
			if (!complete || tick < LastProgressTick)
				End = StalemateWaveEndCA.MissingEvidence;
			else if (!identityValid)
				End = StalemateWaveEndCA.IdentityChanged;
			else if (emergency)
				End = StalemateWaveEndCA.Emergency;
			else if (!targetKnown)
				End = StalemateWaveEndCA.TargetLost;
			else if (tick >= AbsoluteDeadline)
				End = StalemateWaveEndCA.Deadline;
			else if (targetDestroyed)
				End = StalemateWaveEndCA.Complete;
			return End == StalemateWaveEndCA.None;
		}

		static string F(object v) => FieldSaver.FormatValue(v);

		public List<MiniYamlNode> Save()
		{
			var deadNodes = roster.Where(m => dead.Contains((m.ActorId, m.Generation)))
				.Select(m => new MiniYamlNode("Dead", F(new[] { m.ActorId, m.Generation }))).ToList();
			var cellNodes = leaderCells.OrderBy(c => c.X).ThenBy(c => c.Y)
				.Select(c => new MiniYamlNode("Cell", F(new[] { c.X, c.Y }))).ToList();
			return new List<MiniYamlNode>
			{
				new("C", F(C)),
				new("E", F(E)),
				new("RallyDuration", F(RallyDuration)),
				new("NoProgressDuration", F(NoProgressDuration)),
				new("AbsoluteDuration", F(AbsoluteDuration)),
				new("N0", F(N0)),
				new("V0", F(V0)),
				new("RouteEstL", F(RouteEstL)),
				new("RouteBestL", F(RouteBestL)),
				new("LeaderId", F(LeaderId)),
				new("Phase", F(Phase)),
				new("End", F(End)),
				new("LastProgressTick", F(LastProgressTick)),
				new("LastSampleTick", F(LastSampleTick)),
				new("BestL", F(BestL)),
				new("Arrived", F(Arrived)),
				new("RetargetUsed", F(RetargetUsed)),
				new("ConfirmedDeadValue", F(ConfirmedDeadValue)),
				new("Members", "", roster.Select(m => new MiniYamlNode("Member",
					F(new[] { (long)m.ActorId, m.Generation, m.InitialCost }))).ToList()),
				new("Dead", "", deadNodes),
				new("LeaderCells", "", cellNodes)
			};
		}

		/// <summary>Restores a saved wave exactly. Malformed or inconsistent data throws InvalidDataException.</summary>
		public static StalemateBreakerWaveCA Load(MiniYaml data)
		{
			if (data == null)
				throw new InvalidDataException("Missing wave state.");
			var nodes = data.ToDictionary();
			MiniYaml Node(string key) => nodes.TryGetValue(key, out var node) ? node
				: throw new InvalidDataException("Missing wave field " + key);
			T Get<T>(string key) => FieldLoader.GetValue<T>(key, Node(key).Value);

			var memberList = new List<StalemateWaveMemberCA>();
			foreach (var node in Node("Members").Nodes)
			{
				var parts = FieldLoader.GetValue<long[]>("Member", node.Value.Value);
				if (parts == null || parts.Length != 3 || parts[0] < 0 || parts[0] > uint.MaxValue
					|| parts[1] < 0 || parts[1] > uint.MaxValue || parts[2] < int.MinValue || parts[2] > int.MaxValue)
					throw new InvalidDataException("Malformed wave member.");
				memberList.Add(new StalemateWaveMemberCA((uint)parts[0], (uint)parts[1], (int)parts[2]));
			}

			var timing = new StalemateBreakerPolicyCA.TimingCA(Get<int>("E"), Get<int>("RallyDuration"),
				Get<int>("NoProgressDuration"), Get<int>("AbsoluteDuration"));
			StalemateBreakerWaveCA wave;
			try
			{
				wave = new StalemateBreakerWaveCA(Get<int>("C"), memberList, timing, Get<int>("N0"), Get<long>("V0"),
					Get<int>("RouteEstL"), memberList.Count > 0 ? memberList[0].ActorId : 0);
			}
			catch (Exception e) when (e is ArgumentException or OverflowException)
			{
				throw new InvalidDataException("Inconsistent wave state: " + e.Message);
			}

			foreach (var node in Node("Dead").Nodes)
			{
				var parts = FieldLoader.GetValue<uint[]>("Dead", node.Value.Value);
				if (parts == null || parts.Length != 2 || !wave.members.TryGetValue((parts[0], parts[1]), out var cost)
					|| !wave.dead.Add((parts[0], parts[1])))
					throw new InvalidDataException("Malformed dead identity.");
				wave.ConfirmedDeadValue = checked(wave.ConfirmedDeadValue + cost);
			}

			foreach (var node in Node("LeaderCells").Nodes)
			{
				var parts = FieldLoader.GetValue<int[]>("Cell", node.Value.Value);
				if (parts == null || parts.Length != 2 || !wave.leaderCells.Add((parts[0], parts[1])))
					throw new InvalidDataException("Malformed leader cell.");
			}

			var leader = Get<uint>("LeaderId");
			if (!wave.generationById.ContainsKey(leader))
				throw new InvalidDataException("Saved leader is not a member.");
			wave.LeaderId = leader;
			wave.RouteBestL = Get<int>("RouteBestL");
			wave.Phase = Get<StalemateWavePhaseCA>("Phase");
			wave.End = Get<StalemateWaveEndCA>("End");
			wave.LastProgressTick = Get<int>("LastProgressTick");
			wave.LastSampleTick = Get<int>("LastSampleTick");
			wave.BestL = Get<int>("BestL");
			wave.Arrived = Get<bool>("Arrived");
			wave.RetargetUsed = Get<bool>("RetargetUsed");
			if (Get<long>("ConfirmedDeadValue") != wave.ConfirmedDeadValue)
				throw new InvalidDataException("Dead value does not match the dead set.");
			if (!Enum.IsDefined(wave.Phase) || !Enum.IsDefined(wave.End) || wave.LastProgressTick < wave.C
				|| wave.LastSampleTick < wave.LastProgressTick || wave.BestL < 0 || wave.RouteBestL < 0
				|| ((long)wave.LastSampleTick - wave.C) % SampleInterval != 0
				|| ((long)wave.LastProgressTick - wave.C) % SampleInterval != 0)
				throw new InvalidDataException("Inconsistent wave progress state.");
			return wave;
		}
	}
}
