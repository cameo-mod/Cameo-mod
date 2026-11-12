#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// BOTPASSIVE F3/F4 state of the master module (design _06 §4-§5.4, _07 L2, _08 M3/N4/N5).
	/// Allocated only when MasterAiBotModuleInfo.StalemateUrgencyEnabled is true, so the switch-OFF path
	/// keeps its allocations, save bytes and decisions unchanged.
	/// </summary>
	public sealed class StalemateUrgencyState
	{
		public const int HomeRadiusCells = 20;

		public StalemateHomePressureCA HomeLatch = new();

		/// <summary>HomeThreat at the latest snapshot.</summary>
		public bool HomeThreat;

		/// <summary>The switch-OFF CheckEmergency state, tracked in parallel for urgency_legacy only.</summary>
		public bool LegacyEmergency;

		/// <summary>The switch-OFF urgency at the latest snapshot (telemetry only).</summary>
		public BotUrgency LegacyUrgency;

		public long OwnArmy, AlliedArmy, EnemyArmy;

		/// <summary>DEFENCE-DETACH inputs at the latest snapshot (derived each snapshot, not saved, like the army values).</summary>
		public long HomeThreatValue, HomeDefenceValue;

		/// <summary>Design _03 §1 dwell: the tick of the last personality switch this module applied. The initial
		/// personality counts as applied at tick 0 (first manager enable).</summary>
		public int LastAppliedSwitchTick;

		/// <summary>Own production buildings and CYs alive at the previous emergency check: id -> cell, is-CY.</summary>
		public readonly SortedDictionary<uint, (int X, int Y, bool Cy)> Production = new();

		/// <summary>Anchors alive at the previous emergency check (CYs + production; the start cell when none): id (0 = start cell) and cell.</summary>
		public readonly List<(uint Id, int X, int Y)> PrevAnchors = new();

		public readonly Queue<int> HomeProductionLossTicks = new();

		readonly Dictionary<ActorInfo, int> radiusByType = new();

		/// <summary>Design _08 N5: 20 cells, or the type's longest weapon range + 2 when larger. Public ruleset data.</summary>
		public int RadiusFor(ActorInfo info)
		{
			if (info == null)
				return HomeRadiusCells;
			if (radiusByType.TryGetValue(info, out var r))
				return r;
			var range = info.TraitInfos<OpenRA.Mods.Common.Traits.ArmamentInfo>()
				.Select(a => a.WeaponInfo?.Range.Length ?? 0).DefaultIfEmpty(0).Max();
			r = MasterAiEval.HomeThreatRadiusCells(range);
			radiusByType[info] = r;
			return r;
		}

		/// <summary>
		/// One emergency-check step of the home production/CY loss window (design _06 §5.4, _08 M3).
		/// live: own production buildings and CYs now. A lost one counts if it was a CY, or lay within HomeRadiusCells
		/// of another anchor alive at the previous check. Returns whether the 750-tick window holds a home loss.
		/// HOTFIX02: a vanished id counts only in a step whose own DeathsCost rose (deathsIncreased). A transform (MCV deploy,
		/// CY undeploy) or a sale disposes the actor without a death, so it is not a loss.
		/// </summary>
		public bool StepHomeProductionLoss(int tick, IReadOnlyDictionary<uint, (int X, int Y, bool Cy)> live,
			(int X, int Y) startCell, int lossWindowTicks, bool deathsIncreased)
		{
			foreach (var (id, rec) in Production)
			{
				if (!deathsIncreased || live.ContainsKey(id))
					continue;
				var others = PrevAnchors.Where(a => a.Id != id).Select(a => (a.X, a.Y)).ToList();
				if (rec.Cy || MasterAiEval.WithinAnyAnchor(rec.X, rec.Y, others, HomeRadiusCells))
					HomeProductionLossTicks.Enqueue(tick);
			}

			Production.Clear();
			PrevAnchors.Clear();
			foreach (var (id, rec) in live.OrderBy(kv => kv.Key))
			{
				Production[id] = rec;
				PrevAnchors.Add((id, rec.X, rec.Y));
			}

			if (PrevAnchors.Count == 0)
				PrevAnchors.Add((0, startCell.X, startCell.Y));

			while (HomeProductionLossTicks.Count > 0 && tick - HomeProductionLossTicks.Peek() > lossWindowTicks)
				HomeProductionLossTicks.Dequeue();

			return HomeProductionLossTicks.Count > 0;
		}

		/// <summary>Anchor cells now (design _08 M3): own CYs and production buildings, else the start cell.</summary>
		public static List<(int X, int Y)> Anchors(IEnumerable<(int X, int Y)> cyAndProduction, (int X, int Y) startCell)
		{
			var anchors = cyAndProduction.ToList();
			if (anchors.Count == 0)
				anchors.Add(startCell);
			return anchors;
		}

		/// <summary>Restore every saved field from a loaded state (the owner's reference stays readonly). The radius cache is rebuilt lazily.</summary>
		public void CopyFrom(StalemateUrgencyState loaded)
		{
			HomeLatch = loaded.HomeLatch;
			HomeThreat = loaded.HomeThreat;
			LegacyEmergency = loaded.LegacyEmergency;
			LegacyUrgency = loaded.LegacyUrgency;
			LastAppliedSwitchTick = loaded.LastAppliedSwitchTick;
			Production.Clear();
			foreach (var kv in loaded.Production)
				Production[kv.Key] = kv.Value;
			PrevAnchors.Clear();
			PrevAnchors.AddRange(loaded.PrevAnchors);
			HomeProductionLossTicks.Clear();
			foreach (var t in loaded.HomeProductionLossTicks)
				HomeProductionLossTicks.Enqueue(t);
		}

		public List<MiniYamlNode> Save()
		{
			return new List<MiniYamlNode>
			{
				new("HomeLatch", "", HomeLatch.Save()),
				new("HomeThreat", FieldSaver.FormatValue(HomeThreat)),
				new("LegacyEmergency", FieldSaver.FormatValue(LegacyEmergency)),
				new("LegacyUrgency", FieldSaver.FormatValue(LegacyUrgency)),
				new("LastAppliedSwitchTick", FieldSaver.FormatValue(LastAppliedSwitchTick)),
				new("Production", "", Production.Select(kv => new MiniYamlNode(
					FieldSaver.FormatValue(kv.Key), FormattableString.Invariant($"{kv.Value.X},{kv.Value.Y},{(kv.Value.Cy ? 1 : 0)}"))).ToList()),
				new("PrevAnchors", "", PrevAnchors.Select((a, i) => new MiniYamlNode(
					FieldSaver.FormatValue(i), FormattableString.Invariant($"{a.Id},{a.X},{a.Y}"))).ToList()),
				new("HomeProductionLossTicks", FieldSaver.FormatValue(HomeProductionLossTicks.ToArray()))
			};
		}

		public static StalemateUrgencyState Load(MiniYaml data)
		{
			var state = new StalemateUrgencyState();
			if (data == null)
				return state;
			var nodes = data.ToDictionary();
			if (nodes.TryGetValue("HomeLatch", out var latch))
				state.HomeLatch = StalemateHomePressureCA.Load(latch);
			if (nodes.TryGetValue("HomeThreat", out var threat))
				state.HomeThreat = FieldLoader.GetValue<bool>("HomeThreat", threat.Value);
			if (nodes.TryGetValue("LegacyEmergency", out var legacyEmergency))
				state.LegacyEmergency = FieldLoader.GetValue<bool>("LegacyEmergency", legacyEmergency.Value);
			if (nodes.TryGetValue("LegacyUrgency", out var legacyUrgency))
				state.LegacyUrgency = FieldLoader.GetValue<BotUrgency>("LegacyUrgency", legacyUrgency.Value);
			if (nodes.TryGetValue("LastAppliedSwitchTick", out var lastSwitch))
				state.LastAppliedSwitchTick = FieldLoader.GetValue<int>("LastAppliedSwitchTick", lastSwitch.Value);
			if (nodes.TryGetValue("Production", out var production))
				foreach (var node in production.Nodes)
				{
					var p = node.Value.Value.Split(',');
					state.Production[FieldLoader.GetValue<uint>("Production", node.Key)] =
						(Int(p[0]), Int(p[1]), p[2] == "1");
				}

			if (nodes.TryGetValue("PrevAnchors", out var anchors))
				foreach (var node in anchors.Nodes.OrderBy(n => Int(n.Key)))
				{
					var p = node.Value.Value.Split(',');
					state.PrevAnchors.Add((uint.Parse(p[0], NumberStyles.None, CultureInfo.InvariantCulture), Int(p[1]), Int(p[2])));
				}

			if (nodes.TryGetValue("HomeProductionLossTicks", out var losses))
				foreach (var t in FieldLoader.GetValue<int[]>("HomeProductionLossTicks", losses.Value))
					state.HomeProductionLossTicks.Enqueue(t);
			return state;
		}

		static int Int(string s) => int.Parse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
	}
}
