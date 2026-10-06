#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// ECON-A (SPEC_2026-10-05_econ_logistics Part A): one expansion's future building demand,
	/// published the tick <see cref="IBotSuggestRefineryProduction.RequestLocation"/> commits a deploy
	/// target. The base builder schedules the refinery (then the strongest affordable defence) so
	/// each reaches Ready ~= <see cref="EtaTick"/>, holds it at the head of its production queue, and
	/// places it the tick the MCV's transform completes (<see cref="Deployed"/>). The
	/// RequestedRefineries entry keyed on <see cref="Mcv"/> is the demand's refinery record; this
	/// object carries the extra metadata (ETA, defence pick, queue bindings, expiry) around it.
	/// </summary>
	public sealed class ExpansionDemand
	{
		/// <summary>The actor driving to the site. Re-keyed forward across a Transform (a relocating
		/// conyard continues its journey as an MCV) until the replacement is a construction yard.</summary>
		public Actor Mcv;

		/// <summary>The committed deploy target — RequestLocation's conyardLocation, refreshed on re-post.</summary>
		public CPos ConyardLoc;

		/// <summary>The field the outpost refinery serves — RequestLocation's refineryLocation.</summary>
		public CPos ResourceLoc;

		/// <summary>
		/// The strongest affordable defence actor name chosen when the ETA was last computed
		/// (sticky once picked; recomputed while null), or null when nothing is eligible — the
		/// refinery demand proceeds regardless.
		/// </summary>
		public string DefenceType;

		/// <summary>Estimated deploy tick: now + travel path length / nominal speed + deploy allowance.</summary>
		public int EtaTick;

		/// <summary>Liveness window — refreshed every module tick while the MCV is en route.</summary>
		public int ExpiresTick;

		/// <summary>Next tick the ETA gets recomputed from the MCV's live path (it may detour).</summary>
		public int NextEtaTick;

		/// <summary>Set when the MCV's Transform completed — the new conyard exists and holds place.</summary>
		public bool Deployed;

		/// <summary>The deployed conyard's top-left cell (falls back to ConyardLoc when unknown).</summary>
		public CPos DeployedYardLoc;

		// Queue bindings — the item name this demand queued (or adopted) on a given producer.
		// Bind before the StartProduction order resolves; the sweep gives the order a grace window
		// before deciding a still-absent item was never queued.
		public string RefineryItem;
		public Actor RefineryProducer;
		public int RefineryQueuedTick;

		public string DefenceItem;
		public Actor DefenceProducer;
		public int DefenceQueuedTick;

		public ExpansionDemand(Actor mcv)
		{
			Mcv = mcv;
		}

		public void BindRefinery(string item, Actor producer, int now)
		{
			RefineryItem = item;
			RefineryProducer = producer;
			RefineryQueuedTick = now;
		}

		public void BindDefence(string item, Actor producer, int now)
		{
			DefenceItem = item;
			DefenceProducer = producer;
			DefenceQueuedTick = now;
		}

		public void UnbindRefinery()
		{
			RefineryItem = null;
			RefineryProducer = null;
		}

		public void UnbindDefence()
		{
			DefenceItem = null;
			DefenceProducer = null;
		}

		/// <summary>The queue-bound side of the demand is finished (placed or unwound).</summary>
		public bool Unbound => RefineryItem == null && DefenceItem == null;
	}
}
