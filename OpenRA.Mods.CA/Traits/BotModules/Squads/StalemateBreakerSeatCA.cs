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

// BOTPASSIVE-FIX01 IMPL01: the per-seat breaker state that decisions read, persisted when the switch is ON
// (design _06 §1.4/§2, _08 §3, _09 LB). Allocated only when SquadManagerBotModuleCAInfo.StalemateBreakerEnabled is true.
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	/// <summary>One kept group of a live wave: locomotor, representative, frozen tc_g, centroid at C, route at C and members.</summary>
	public sealed class StalemateWaveGroupCA
	{
		public string Locomotor = "";
		public uint RepId;
		public int TcX, TcY;
		public int CenX, CenY;
		public int L, S;
		public List<uint> Members = new();
	}

	/// <summary>A seen enemy actor in the seat's own fresh-sighting cache (denL and den_defence_fresh), seat-legal by construction.</summary>
	public readonly record struct StalemateSightingCA(int OwnerIndex, long Value, int Tick, bool Defence, int X, int Y);

	public sealed class StalemateBreakerSeatCA
	{
		public int LastKillTick = -1;
		public int NextAttemptTick;
		public int CooldownUntil;
		public long WaveCounter;
		public StalemateExclusionSetCA Exclusion = new();

		// Live wave (null between waves).
		public StalemateBreakerWaveCA Wave;
		public long WaveId;
		public uint TargetId;
		public int TargetOwnerIndex = -1;
		public int TargetX, TargetY;
		public int TargetLastHp = -1;
		public int RallyX, RallyY;
		public bool RetreatDeferred;
		public int WindowDeaths;
		public List<StalemateWaveGroupCA> Groups = new();

		// Fresh-sighting cache and the observed-destruction watch (both read by decisions, so both persist).
		public readonly SortedDictionary<uint, StalemateSightingCA> Sightings = new();
		public readonly SortedDictionary<uint, (int X, int Y)> KillWatch = new();

		/// <summary>Design _02 §target validation: the last tick each enemy CY/production building was seen by this seat.
		/// A remembered (frozen) candidate qualifies only while this is at most RememberedFreshTicks old.</summary>
		public readonly SortedDictionary<uint, int> TargetSeen = new();

		public const int RememberedFreshTicks = 1500;

		public bool RememberedFresh(uint actorId, int now) =>
			TargetSeen.TryGetValue(actorId, out var seen) && now >= seen && now - seen <= RememberedFreshTicks;

		/// <summary>Design _06 §3 denL: the owner's combat units seen within 750 ticks.</summary>
		public long DenL(int ownerIndex, int now) => Sightings.Values
			.Where(s => !s.Defence && s.OwnerIndex == ownerIndex && now - s.Tick <= StalemateBreakerCA.SeenFreshTicks)
			.Sum(s => s.Value);

		/// <summary>Design _08 §2 den_defence_fresh (telemetry only): the owner's defences seen within 750 ticks and 20 cells of raw.</summary>
		public long DenDefenceFresh(int ownerIndex, int rawX, int rawY, int now)
		{
			var r2 = (long)StalemateBreakerCA.DefenceRadiusCells * StalemateBreakerCA.DefenceRadiusCells;
			return Sightings.Values.Where(s => s.Defence && s.OwnerIndex == ownerIndex
					&& now - s.Tick <= StalemateBreakerCA.SeenFreshTicks
					&& ((long)(s.X - rawX) * (s.X - rawX)) + ((long)(s.Y - rawY) * (s.Y - rawY)) <= r2)
				.Sum(s => s.Value);
		}

		public void PruneSightings(int now)
		{
			foreach (var id in Sightings.Where(kv => now - kv.Value.Tick > StalemateBreakerCA.SeenFreshTicks).Select(kv => kv.Key).ToList())
				Sightings.Remove(id);
			foreach (var id in TargetSeen.Where(kv => now - kv.Value > RememberedFreshTicks).Select(kv => kv.Key).ToList())
				TargetSeen.Remove(id);
		}

		public void ClearWave()
		{
			Wave = null;
			TargetId = 0;
			TargetOwnerIndex = -1;
			TargetLastHp = -1;
			RetreatDeferred = false;
			WindowDeaths = 0;
			Groups.Clear();
		}

		static string F(object v) => FieldSaver.FormatValue(v);

		public List<MiniYamlNode> Save()
		{
			var nodes = new List<MiniYamlNode>
			{
				new("LastKillTick", F(LastKillTick)),
				new("NextAttemptTick", F(NextAttemptTick)),
				new("CooldownUntil", F(CooldownUntil)),
				new("WaveCounter", F(WaveCounter)),
				new("Exclusion", "", Exclusion.Save()),
				new("Sightings", "", Sightings.Select(kv => new MiniYamlNode(F(kv.Key),
					F(new long[] { kv.Value.OwnerIndex, kv.Value.Value, kv.Value.Tick, kv.Value.Defence ? 1 : 0, kv.Value.X, kv.Value.Y }))).ToList()),
				new("KillWatch", "", KillWatch.Select(kv => new MiniYamlNode(F(kv.Key), F(new[] { kv.Value.X, kv.Value.Y }))).ToList()),
				new("TargetSeen", "", TargetSeen.Select(kv => new MiniYamlNode(F(kv.Key), F(kv.Value))).ToList())
			};

			if (Wave != null)
			{
				nodes.Add(new MiniYamlNode("Wave", "", Wave.Save()));
				nodes.Add(new MiniYamlNode("WaveId", F(WaveId)));
				nodes.Add(new MiniYamlNode("Target", F(new long[] { TargetId, TargetOwnerIndex, TargetX, TargetY, TargetLastHp })));
				nodes.Add(new MiniYamlNode("Rally", F(new[] { RallyX, RallyY })));
				nodes.Add(new MiniYamlNode("RetreatDeferred", F(RetreatDeferred)));
				nodes.Add(new MiniYamlNode("WindowDeaths", F(WindowDeaths)));
				nodes.Add(new MiniYamlNode("Groups", "", Groups.Select((g, i) => new MiniYamlNode(F(i), "", new List<MiniYamlNode>
				{
					new("Locomotor", g.Locomotor),
					new("Route", F(new long[] { g.RepId, g.TcX, g.TcY, g.CenX, g.CenY, g.L, g.S })),
					new("Members", F(g.Members.ToArray()))
				})).ToList()));
			}

			return nodes;
		}

		public static StalemateBreakerSeatCA Load(MiniYaml data)
		{
			var seat = new StalemateBreakerSeatCA();
			if (data == null)
				return seat;
			var nodes = data.ToDictionary();
			MiniYaml Node(string key) => nodes.TryGetValue(key, out var node) ? node : throw new InvalidDataException("Missing breaker field " + key);
			T Get<T>(string key) => FieldLoader.GetValue<T>(key, Node(key).Value);

			seat.LastKillTick = Get<int>("LastKillTick");
			seat.NextAttemptTick = Get<int>("NextAttemptTick");
			seat.CooldownUntil = Get<int>("CooldownUntil");
			seat.WaveCounter = Get<long>("WaveCounter");
			seat.Exclusion = StalemateExclusionSetCA.Load(Node("Exclusion"));
			foreach (var n in Node("Sightings").Nodes)
			{
				var p = FieldLoader.GetValue<long[]>("Sighting", n.Value.Value);
				if (p == null || p.Length != 6)
					throw new InvalidDataException("Malformed sighting.");
				seat.Sightings[FieldLoader.GetValue<uint>("SightingId", n.Key)] =
					new StalemateSightingCA((int)p[0], p[1], (int)p[2], p[3] != 0, (int)p[4], (int)p[5]);
			}

			foreach (var n in Node("KillWatch").Nodes)
			{
				var p = FieldLoader.GetValue<int[]>("KillWatch", n.Value.Value);
				if (p == null || p.Length != 2)
					throw new InvalidDataException("Malformed kill watch.");
				seat.KillWatch[FieldLoader.GetValue<uint>("KillWatchId", n.Key)] = (p[0], p[1]);
			}

			foreach (var n in Node("TargetSeen").Nodes)
				seat.TargetSeen[FieldLoader.GetValue<uint>("TargetSeenId", n.Key)] = FieldLoader.GetValue<int>("TargetSeen", n.Value.Value);

			if (!nodes.ContainsKey("Wave"))
				return seat;

			seat.Wave = StalemateBreakerWaveCA.Load(Node("Wave"));
			seat.WaveId = Get<long>("WaveId");
			var t = Get<long[]>("Target");
			if (t == null || t.Length != 5)
				throw new InvalidDataException("Malformed breaker target.");
			seat.TargetId = (uint)t[0];
			seat.TargetOwnerIndex = (int)t[1];
			seat.TargetX = (int)t[2];
			seat.TargetY = (int)t[3];
			seat.TargetLastHp = (int)t[4];
			var r = Get<int[]>("Rally");
			if (r == null || r.Length != 2)
				throw new InvalidDataException("Malformed rally cell.");
			seat.RallyX = r[0];
			seat.RallyY = r[1];
			seat.RetreatDeferred = Get<bool>("RetreatDeferred");
			seat.WindowDeaths = Get<int>("WindowDeaths");
			foreach (var n in Node("Groups").Nodes.OrderBy(n => FieldLoader.GetValue<int>("GroupIndex", n.Key)))
			{
				var g = n.Value.ToDictionary();
				var route = FieldLoader.GetValue<long[]>("Route", g["Route"].Value);
				if (route == null || route.Length != 7)
					throw new InvalidDataException("Malformed wave group.");
				seat.Groups.Add(new StalemateWaveGroupCA
				{
					Locomotor = g["Locomotor"].Value ?? "",
					RepId = (uint)route[0], TcX = (int)route[1], TcY = (int)route[2], CenX = (int)route[3], CenY = (int)route[4],
					L = (int)route[5], S = (int)route[6],
					Members = FieldLoader.GetValue<uint[]>("Members", g["Members"].Value).ToList()
				});
			}

			return seat;
		}
	}
}
