#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Squad desire provider (LEARN-P6, SPEC 2026-10-05 §11/A4, AI_ARCHITECTURE): WHAT a squad wants — one of six stances",
		"(attack, defend, retreat, regroup, harass, reinforce). Per squad a leaky integrator per stance over the urgency the",
		"squad manager packages as SquadDesireSignals, plus a capped personality bias; a stance switches only when the best",
		"desire beats the current by the margin AND MinDwellTicks elapsed. FIXED POINT: all thousandths, integer math only.",
		"A PURE PROVIDER: it publishes IBotSquadDesire and issues NO orders — the squad states consume the stance in place of",
		"the binary attack/retreat branch (§19.3: only this module computes desirability).")]
	public class SquadDesireBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Leaky-integrator rate per squad eval, in thousandths (d += rate·(urgency − d)/1000). 100 = 10% per eval.")]
		public readonly int LeakPermille = 100;

		[Desc("A challenger's desire must beat the current stance's by this many thousandths to switch.")]
		public readonly int HysteresisMilli = 150;

		[Desc("A stance must hold at least this many world ticks before it can be replaced.")]
		public readonly int MinDwellTicks = 100;

		[Desc("The personality bias per stance is clamped to ±this many thousandths of urgency.")]
		public readonly int BiasCapMilli = 200;

		[Desc("Ticks between sweeps that forget squads no longer consulted.")]
		public readonly int PruneIntervalTicks = 500;

		[Desc("A squad not evaluated for this many ticks is forgotten (dead or re-formed).")]
		public readonly int PruneAfterTicks = 2500;

		public override object Create(ActorInitializer init) { return new SquadDesireBotModule(init.Self, this); }
	}

	public class SquadDesireBotModule : ConditionalTrait<SquadDesireBotModuleInfo>, IBotSquadDesire, IBotTick
	{
		sealed class DesireState
		{
			public readonly int[] Desires = new int[SquadDesireEval.StanceCount];
			public int Current = -1;
			public int LastSwitchTick;
			public int LastSeenTick;
			public bool Primed;
		}

		readonly World world;
		readonly OpenRA.Player player;
		readonly Dictionary<SquadCA, DesireState> states = new();
		readonly int[] urgencies = new int[SquadDesireEval.StanceCount];
		readonly List<SquadCA> pruneScratch = new();

		string personality;
		int personalityTick = -1;
		int nextPruneTick;

		public SquadDesireBotModule(Actor self, SquadDesireBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		string Personality
		{
			get
			{
				if (personalityTick != world.WorldTick)
				{
					personalityTick = world.WorldTick;
					personality = player.PlayerActor.TraitOrDefault<BotPersonalityController>()?.CurrentPersonality ?? "";
				}

				return personality;
			}
		}

		public SquadDesireStance StanceFor(SquadCA squad, in SquadDesireSignals signals)
		{
			if (!states.TryGetValue(squad, out var st))
				states[squad] = st = new DesireState();

			SquadDesireEval.Urgencies(in signals, urgencies);
			SquadDesireEval.AddBias(urgencies, Personality, Info.BiasCapMilli);

			for (var i = 0; i < SquadDesireEval.StanceCount; i++)
				st.Desires[i] = st.Primed
					? SquadDesireEval.Step(st.Desires[i], urgencies[i], Math.Max(0, Info.LeakPermille))
					: urgencies[i];

			st.Primed = true;
			st.LastSeenTick = signals.WorldTick;

			var next = SquadDesireEval.Pick(st.Desires, st.Current, Math.Max(0, Info.HysteresisMilli),
				signals.WorldTick - st.LastSwitchTick >= Math.Max(0, Info.MinDwellTicks));
			if (next != st.Current)
			{
				st.Current = next;
				st.LastSwitchTick = signals.WorldTick;
			}

			return (SquadDesireStance)st.Current;
		}

		void IBotTick.BotTick(IBot bot)
		{
			if (IsTraitDisabled || world.WorldTick < nextPruneTick)
				return;

			nextPruneTick = world.WorldTick + Math.Max(1, Info.PruneIntervalTicks);
			var cutoff = world.WorldTick - Math.Max(0, Info.PruneAfterTicks);
			foreach (var kv in states)
				if (kv.Value.LastSeenTick < cutoff)
					pruneScratch.Add(kv.Key);

			foreach (var dead in pruneScratch)
				states.Remove(dead);
			pruneScratch.Clear();
		}
	}
}
