#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// IMPL02-AGG-TEAM (A4): one allied bot's attack intent on the team blackboard. An immutable, own-side value: where a
	/// ground assault squad is attacking, the rendezvous point on its approach, and when it passes / arrives. Allied idle
	/// squads that are in range join it (same target, rendezvous, start together). Never an enumeration of enemy actors.
	/// </summary>
	public sealed class TeamAttackIntent
	{
		/// <summary>The centre of the publisher squad's target.</summary>
		public readonly WPos Target;

		/// <summary>A point on the publisher's approach, short of <see cref="Target"/>, where a joiner assembles.</summary>
		public readonly WPos Rendezvous;

		/// <summary>World tick the publisher's squad is expected to pass <see cref="Rendezvous"/>.</summary>
		public readonly int RendezvousTick;

		/// <summary>World tick the publisher's squad is expected at <see cref="Target"/>.</summary>
		public readonly int EtaTick;

		/// <summary>World tick the intent was computed (staleness is judged from it).</summary>
		public readonly int PublishTick;

		/// <summary>Combined cost of the publisher squad.</summary>
		public readonly long Value;

		/// <summary>ActorID of the target when it is a live actor, else 0 (a joiner re-resolves by position).</summary>
		public readonly uint TargetActorId;

		public TeamAttackIntent(WPos target, WPos rendezvous, int rendezvousTick, int etaTick, int publishTick, long value, uint targetActorId)
		{
			Target = target;
			Rendezvous = rendezvous;
			RendezvousTick = rendezvousTick;
			EtaTick = etaTick;
			PublishTick = publishTick;
			Value = value;
			TargetActorId = targetActorId;
		}
	}

	/// <summary>
	/// IMPL02-AGG-TEAM (A4) publish seam: the squad manager answers its current attack intent, or null. Null whenever the
	/// AggTeam flag is off, so the broadcast of an OFF seat is byte-identical to before.
	/// </summary>
	public interface IBotAttackIntentSource
	{
		/// <summary>The bot's best live attack intent, or null when none or the flag is off.</summary>
		TeamAttackIntent AttackIntent { get; }
	}
}
