using System;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// The first armed player trait owns this object; disabled personalities retain it.
	// No static/team state and no randomness. All armed managers use one wave sequence.
	public sealed class M13PlayerStateCA
	{
		public AttackLivenessEvalCA Liveness { get; } = new();
		public TeamResponseBudgetCA Responses { get; } = new();
		public long LastWaveId { get; private set; }
		public bool EvidenceComplete { get; private set; } = true;
		public long BaseDangerUntilTick { get; private set; } = -1;
		public void NoteBaseDamage(int tick, int reviewTicks)
		{
			if (tick < 0 || reviewTicks <= 0)
				throw new ArgumentOutOfRangeException(nameof(tick));
			BaseDangerUntilTick = Math.Max(BaseDangerUntilTick, (long)tick + reviewTicks);
		}

		// Without restored interval and membership evidence, do not silently renew
		// a deadline or recycle a response slot. An adapter must supply both later.
		public void Invalidate() => EvidenceComplete = false;

		public long AllocateWave()
		{
			if (LastWaveId == long.MaxValue)
				throw new InvalidOperationException("M13 wave identity exhausted.");
			return ++LastWaveId;
		}
	}
}
