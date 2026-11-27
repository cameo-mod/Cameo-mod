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
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// RAID-HARV-01 P3: tools/tests/raid_harv_signals_golden.json is hand-derived from <see cref="RaidHarvSignals"/> and replayed here against the nine C# signal functions, and by
	/// tools/tests/test_extract_raid_harv_signals.py against the python extractor (tools/ai/extract_raid_harv_signals.py), so the DEV extractor and the signal definitions cannot drift apart.
	/// </summary>
	[TestFixture]
	public sealed class RaidHarvSignalsGoldenTest
	{
		static JsonElement Golden() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "raid_harv_signals_golden.json"))).RootElement;

		static int[] Ints(JsonElement e) => e.EnumerateArray().Select(x => x.GetInt32()).ToArray();

		static bool[] Bools(JsonElement e) => e.EnumerateArray().Select(x => x.GetBoolean()).ToArray();

		static RaidSignal Eval(string key, JsonElement i) => key switch
		{
			"lost_recon" => RaidHarvSignals.LostRecon(i.GetProperty("current").GetInt32(), Ints(i.GetProperty("reacquire")), i.GetProperty("exhausted").GetInt32()),
			"move_refresh" => RaidHarvSignals.MoveRefresh(Ints(i.GetProperty("drifts"))),
			"req_margin" => RaidHarvSignals.ReqMargin(i.GetProperty("results").EnumerateArray().Select(r => new RaidResult(r[0].GetBoolean(), r[1].GetInt32())).ToList()),
			"recon_radius" => RaidHarvSignals.ReconRadius(i.GetProperty("current").GetInt32(), Ints(i.GetProperty("distances")), i.GetProperty("exhausted_far_seen").GetInt32()),
			"incident_loss" => RaidHarvSignals.IncidentLoss(i.GetProperty("retreats").EnumerateArray().Select(r => new RetreatOutcome(r[0].GetBoolean(), r[1].GetBoolean())).ToList()),
			"incident_window" => RaidHarvSignals.IncidentWindow(i.GetProperty("current").GetInt32(), Ints(i.GetProperty("spans"))),
			"incident_decay" => RaidHarvSignals.IncidentDecay(i.GetProperty("cells").EnumerateArray().Select(r => new DecayOutcome(r[0].GetBoolean(), r[1].GetBoolean())).ToList()),
			"waypoint_spacing" => RaidHarvSignals.WaypointSpacing(Bools(i.GetProperty("legs"))),
			"defence_radius" => RaidHarvSignals.DefenceRadius(Bools(i.GetProperty("outside"))),
			_ => throw new ArgumentException("unknown signal " + key),
		};

		static RaidSignal Expect(JsonElement e) => e.ValueKind == JsonValueKind.Null ? RaidSignal.None : new RaidSignal(true, e.GetInt32());

		[Test]
		public void TheDeclaredConstantsAndTheKeyOrderMatchThePythonExtractor()
		{
			var c = Golden().GetProperty("constants");
			Assert.That(RaidHarvKeys.ErrorClipPermille, Is.EqualTo(c.GetProperty("error_clip").GetInt32()));
			Assert.That(RaidHarvKeys.CategoricalPermille, Is.EqualTo(c.GetProperty("categorical").GetInt32()));
			Assert.That(RaidHarvSignals.RefreshSettleTicks, Is.EqualTo(c.GetProperty("refresh_settle_ticks").GetInt32()));
			Assert.That(RaidHarvSignals.LateDriftCells, Is.EqualTo(c.GetProperty("late_drift_cells").GetInt32()));
			Assert.That(RaidHarvSignals.WastedDriftCells, Is.EqualTo(c.GetProperty("wasted_drift_cells").GetInt32()));
			Assert.That(RaidHarvKeys.All.Select(k => k.Id).ToArray(), Is.EqualTo(Golden().GetProperty("keys").EnumerateArray().Select(k => k.GetString()).ToArray()));
		}

		[Test]
		public void EverySignalVectorMatchesThePythonMirror()
		{
			var seen = 0;
			foreach (var signal in Golden().GetProperty("signals").EnumerateObject())
				foreach (var v in signal.Value.EnumerateArray())
				{
					seen++;
					Assert.That(Eval(signal.Name, v.GetProperty("in")), Is.EqualTo(Expect(v.GetProperty("expect"))), signal.Name + ": " + v.GetProperty("name").GetString());
				}

			Assert.That(seen, Is.GreaterThan(60), "the golden file carries the vectors of all nine keys");
		}

		[Test]
		public void EveryKeyHasANoSampleVectorAndASampleVector()
		{
			foreach (var signal in Golden().GetProperty("signals").EnumerateObject())
			{
				var expects = signal.Value.EnumerateArray().Select(v => Expect(v.GetProperty("expect"))).ToList();
				Assert.That(expects.Any(e => !e.HasSample), Is.True, signal.Name);
				Assert.That(expects.Any(e => e.HasSample), Is.True, signal.Name);
			}
		}
	}
}
