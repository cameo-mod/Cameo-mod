#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// MODE-BIAS-01 (user ruling 2026-11-25): the four mode bias profiles (essentials, techchain, antirush, opening) of the staged planner. Each is an existing
	// building list plus a milli multiplier on the BuildingFractions weight of those types, active only while the list's own trigger holds, never a gate. The
	// snapshot carries one BiasMilli function (null while no armed profile's trigger holds, so the roll stays the old uniform shuffle); the composition and the
	// roll are ModeBias (pure). The stages S3/S4 and the essentials hold are unchanged; the profiles only ADD the weight. Every trigger here is a side-effect-free query.
	partial class BaseBuilderQueueManagerCA
	{
		Func<string, int> ModeBiasFor(List<ActorInfo> buildable, BaseBuildSnapshot s)
		{
			if (!(baseBuilder.MbEssentialsArmed || baseBuilder.MbTechChainArmed || baseBuilder.MbAntiRushArmed || baseBuilder.MbOpeningArmed))
				return null;

			var profiles = new List<(string Id, Func<string, bool> Member)>(4);
			var b = baseBuilder.Info;

			// essentials: the same trigger as the low-cash hold (EssentialsOn, a tier line, the bank under it); the types are the ones EssentialsLowCashCA.Holds lets through.
			var line = TierLine;
			if (baseBuilder.MbEssentialsArmed && EssentialsOn && line > 0 && s.Bank < line)
				profiles.Add((ModeBias.Essentials, name => IsLowCashEssential(s, name, line)));

			// techchain: a NEW trigger (no source stage copies it; ruling "tech stage -> tech chain", Opus relay of the user design 11-25, and the tech chain is never
			// blocked, designer rule 11-22): a tech step is open, i.e. a TechChainTypes type is buildable now with no copy owned or in flight (CountOf 0). The members are
			// those open steps only, so a built step stops being weighted at once (S3's TechCenter role counts the whole list and only during the opening).
			if (baseBuilder.MbTechChainArmed && b.TechChainTypes.Count > 0)
			{
				var openSteps = new HashSet<string>(buildable.Where(a => b.TechChainTypes.Contains(a.Name) && BaseBuildPlanCA.CountOf(s, a.Name) == 0).Select(a => a.Name), StringComparer.Ordinal);
				if (openSteps.Count > 0)
					profiles.Add((ModeBias.TechChain, openSteps.Contains));
			}

			// antirush: the S4 condition (the S4 set switch, then AntiRushCA.Wants); the types are the ground defences. Without S4 armed there is no anti-rush trigger.
			if (baseBuilder.MbAntiRushArmed && AntiRushSetOn && AntiRushWantedNow())
				profiles.Add((ModeBias.AntiRush, name => world.Map.Rules.Actors.TryGetValue(name, out var info) && IsGroundDefence(info)));

			// opening: the S3 condition (OpeningActive); the types are those of the next open role of the standard order.
			if (baseBuilder.MbOpeningArmed && s.OpeningActive)
			{
				var types = OpeningBiasTypes(buildable, s);
				if (types != null)
					profiles.Add((ModeBias.Opening, types.Contains));
			}

			if (profiles.Count == 0)
				return null;

			var values = baseBuilder.ModeBiasValues;
			return name =>
			{
				var ids = new List<string>(profiles.Count);
				foreach (var (id, member) in profiles)
					if (member(name))
						ids.Add(id);

				return values.Compose(ids);
			};
		}

		// The essentials types: not held by the low-cash gate, i.e. a first copy, a tech-chain type, a refinery, a power plant while power is short, or a passive-income
		// generator (the planner/demand exemption is false here: it describes a start, not a type).
		bool IsLowCashEssential(BaseBuildSnapshot s, string name, int line)
		{
			if (!world.Map.Rules.Actors.TryGetValue(name, out var info))
				return false;

			var b = baseBuilder.Info;
			return !EssentialsLowCashCA.Holds(true, s.Bank, line, BaseBuildPlanCA.CountOf(s, name) == 0, TechChainExempt(name), b.RefineryTypes.Contains(name),
				b.PowerTypes.Contains(name) && playerPower != null && playerPower.ExcessPower < minimumExcessPower,
				info.HasTraitInfo<CashTricklerInfo>(), false);
		}

		// The same wanting test as PipelineAntiRushStage (production standing, fewer ground defences owned or queued than the tier's set size).
		bool AntiRushWantedNow()
		{
			if (playerBuildings == null)
				return false;

			var b = baseBuilder.Info;
			var have = playerBuildings.Count(a => IsGroundDefence(a.Info));
			foreach (var kv in baseBuilder.BuildingsBeingProduced)
				if (world.Map.Rules.Actors.TryGetValue(kv.Key, out var queued) && IsGroundDefence(queued))
					have += kv.Value;

			var size = AntiRushCA.SetSize(botLimits != null ? botLimits.Info.ArmyShareTargetPermille : -1, b.AntiRushBaseMin, b.AntiRushBaseMax);
			return AntiRushCA.Wants(playerBuildings.Any(a => b.ProductionTypes.Contains(a.Info.Name)), have, size);
		}

		// The buildable types of the next open role of the opening order (S3's role to types mapping and counts, without S3's picks: the wanted-refinery rung and the
		// stuck ledger have side effects, so a role is open here when its count is short and it has a buildable type). Null = no open role.
		HashSet<string> OpeningBiasTypes(List<ActorInfo> buildable, BaseBuildSnapshot s)
		{
			var b = baseBuilder.Info;
			var airRole = new HashSet<string>(b.ProductionTypes.Except(b.BarracksTypes).Except(b.VehiclesFactoryTypes).Except(b.NavalProductionTypes));
			foreach (var slot in s.OpeningOrder)
			{
				IEnumerable<string> roleTypes;
				int have;
				switch (slot.Role)
				{
					case OpeningRole.Power:
						roleTypes = b.PowerTypes;
						have = OwnedOrQueuedCount(b.PowerTypes);
						break;
					case OpeningRole.Barracks:
						roleTypes = b.BarracksTypes;
						have = OwnedOrQueuedCount(b.BarracksTypes);
						break;
					case OpeningRole.Refinery:
						roleTypes = b.RefineryTypes;
						have = OwnedOrQueuedCount(b.RefineryTypes);
						break;
					case OpeningRole.WarFactory:
						roleTypes = b.VehiclesFactoryTypes;
						have = OwnedOrQueuedCount(b.VehiclesFactoryTypes);
						break;
					case OpeningRole.Radar:
						roleTypes = GetRadarProducibles(buildable).Select(a => a.Name);
						have = OwnedRadarProviders();
						break;
					case OpeningRole.Airfield:
						roleTypes = airRole;
						have = OwnedOrQueuedCount(airRole);
						break;
					case OpeningRole.TechCenter:
						roleTypes = b.TechChainTypes;
						have = OwnedOrQueuedCount(b.TechChainTypes);
						break;
					default:
						roleTypes = buildable.Where(a => a.HasTraitInfo<CashTricklerInfo>()).Select(a => a.Name);
						have = playerBuildings.Count(a => a.Info.HasTraitInfo<CashTricklerInfo>())
							+ baseBuilder.BuildingsBeingProduced.Count(kv => kv.Value > 0 && world.Map.Rules.Actors.TryGetValue(kv.Key, out var info) && info.HasTraitInfo<CashTricklerInfo>());
						break;
				}

				if (have >= slot.Required)
					continue;

				var types = new HashSet<string>(roleTypes);
				types.IntersectWith(s.Buildable);
				if (types.Count > 0)
					return types;
			}

			return null;
		}
	}
}
