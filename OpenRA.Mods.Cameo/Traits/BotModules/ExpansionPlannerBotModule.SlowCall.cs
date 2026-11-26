#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// PERF-UB-PICK-01 item 4 (Opus 11-26, DEV only): a slow-call guard on the planner's BotTick. V08G had one 5682 ms call on Multi9 at tick 40198
	/// with no debug line. Every BotTick call is timed; a call of SlowCallMs or more writes one debug.log line (EXP-SLOW-01) that splits the call into
	/// its phases (the time from the previous recorded mark, or the call start, to each mark; a phase the call never reached is absent; rest = after the last mark).
	/// Record only: no value here is read by a decision, and a release build has no fields and no calls (every helper is [Conditional]).
	/// </summary>
	public partial class ExpansionPlannerBotModule
	{
		// Mark order = the phase index. The first two are BotTick's, the rest Replan's.
		const int SlowMarkMcvSafe = 0;
		const int SlowMarkCoverage = 1;
		const int SlowMarkBuildables = 2;
		const int SlowMarkActors = 3;
		const int SlowMarkFields = 4;
		const int SlowMarkTarget = 5;
		const int SlowMarkClaims = 6;
		const int SlowMarkMcv = 7;

#if CAMEO_DEV_LOGGING
		const double SlowCallMs = 250.0;
		static readonly string[] SlowMarkNames = { "mcvsafe", "coverage", "buildables", "actors", "fields", "target", "claims", "mcv" };
		readonly long[] slowMarks = new long[8];
		long slowStart;
		bool slowReplan;
#endif

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void SlowCallBegin()
		{
#if CAMEO_DEV_LOGGING
			System.Array.Clear(slowMarks, 0, slowMarks.Length);
			slowReplan = false;
			slowStart = System.Diagnostics.Stopwatch.GetTimestamp();
#endif
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void SlowCallMark(int mark)
		{
#if CAMEO_DEV_LOGGING
			slowMarks[mark] = System.Diagnostics.Stopwatch.GetTimestamp();
			if (mark >= SlowMarkBuildables)
				slowReplan = true;
#endif
		}

		[System.Diagnostics.Conditional("CAMEO_DEV_LOGGING")]
		void SlowCallEnd()
		{
#if CAMEO_DEV_LOGGING
			var end = System.Diagnostics.Stopwatch.GetTimestamp();
			double Ms(long from, long to) => (to - from) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
			var total = Ms(slowStart, end);
			if (total < SlowCallMs)
				return;

			var parts = new System.Text.StringBuilder();
			var last = slowStart;
			for (var i = 0; i < slowMarks.Length; i++)
			{
				if (slowMarks[i] == 0)
					continue;

				parts.Append($" {SlowMarkNames[i]}=").Append(Ms(last, slowMarks[i]).ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
				last = slowMarks[i];
			}

			parts.Append(" rest=").Append(Ms(last, end).ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
			var actors = 0;
			foreach (var a in world.Actors)
				if (a.Owner == player)
					actors++;

			Log.Write("debug", $"EXP-SLOW-01 player={player.ClientIndex} tick={world.WorldTick} ms={total.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} replan={slowReplan}{parts} " +
				$"scores={LastScores?.Count ?? -1} own_actors={actors}");
#endif
		}
	}
}
