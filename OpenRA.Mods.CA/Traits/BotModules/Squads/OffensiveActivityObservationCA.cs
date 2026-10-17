using System;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	// Reads current activity only, never NextActivity or an order request. One sample per
	// owned actor retained by the caller; callbacks must reject hidden/illegal targets.
	internal sealed class OffensiveActivityObservationCA
	{
		Activity previous;
		WPos previousPosition;
		int previousTick = -1;

		public bool Observe(int tick, Activity current, Actor self, WPos position, bool ownWaveMember,
			bool idle, Func<Target, bool> legalWaveTarget)
		{
			if (tick < 0 || tick < previousTick)
				throw new ArgumentOutOfRangeException(nameof(tick));
			var old = previous;
			var oldPosition = previousPosition;
			var oldTick = previousTick;
			previous = current;
			previousPosition = position;
			previousTick = tick;
			if (!ownWaveMember || idle || current == null || current.State != ActivityState.Active)
				return false;

			// AttackMove can have a ReturnToBase child. Its active leaf must actually be
			// moving/attacking; generic Move (including retreat/staging/transport) is excluded.
			var leaf = current;
			for (var depth = 0; leaf.ChildActivity != null; depth++)
			{
				if (depth >= 16 || leaf is ReturnToBase || leaf.ChildActivity.State != ActivityState.Active)
					return false;
				leaf = leaf.ChildActivity;
			}
			if (leaf is ReturnToBase)
				return false;
			var attack = leaf.ActivityType == ActivityType.Attack;
			var move = (current is AttackMoveActivity || current.ActivityType == ActivityType.Attack)
				&& leaf.ActivityType == ActivityType.Move;
			if (!attack && !move)
				return false;

			var targetCount = 0;
			foreach (var target in leaf.GetTargets(self))
			{
				if (++targetCount > 256)
					return false;
				if (Matches(target))
					return true;
			}
			// Some attack activities expose their actor only through public target lines.
			// Never call AttackMove target lines: those construct a hypothetical move.
			if (attack)
				foreach (var node in leaf.TargetLineNodes(self))
				{
					if (++targetCount > 256)
						return false;
					if (Matches(node.Target))
						return true;
				}
			return false;

			bool Matches(Target target)
			{
				if (target.Type == TargetType.Invalid || !legalWaveTarget(target))
					return false;
				if (attack)
					return true;
				// A stuck move, target drift or duplicate tick is not progress. Compare both
				// own positions against the SAME currently visible target position.
				return ReferenceEquals(current, old) && tick > oldTick && position != oldPosition
					&& (position - target.CenterPosition).HorizontalLengthSquared
						< (oldPosition - target.CenterPosition).HorizontalLengthSquared;
			}
		}
	}
}
