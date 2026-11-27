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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Dev
{
	/// <summary>
	/// LEARN-ALL-02 P1c-2: the pure half of the DEV per-game exposure writer. It turns one bot seat's context (LearnSeatContext, from LearnAllBotModule) and the signals the features
	/// handed over (IBotLearnSignals) into ONE JSON line in exactly the format tools/ai/fit_learn_all.py reads, and refuses what the fitter would refuse. No engine types, no I/O, no clock.
	/// Record = {"game_id","seq","seat","personality","own_faction","main_target","main_target_style","enemies":[{"faction","weight"}],"knobs":[{"knob","signal"}]}.
	/// Facts only a bot itself may know: public factions, never an enemy's personality, condition or true (hidden) faction. A Random or hidden faction is written as null.
	/// </summary>
	public static class LearnAllExposure
	{
		/// <summary>The most enemy players one entry may stand for (the fitter refuses a larger weight).</summary>
		public const int MaxEnemyWeight = 64;

		static readonly Regex KnobName = new(@"^[a-z0-9_.]+\z", RegexOptions.CultureInvariant);
		static readonly Regex SafeName = new(@"^[A-Za-z0-9_.\-]+\z", RegexOptions.CultureInvariant);

		/// <summary>The seat id the fitter and tools/ai/unit_effect_exposure.py agree on: seat_&lt;index in world.Players&gt;.</summary>
		public static string SeatId(int playerIndex) => "seat_" + playerIndex.ToString(CultureInfo.InvariantCulture);

		/// <summary>A knob id the fitter may store: lower-case letters, digits, '_' and '.', and no "__" (the snapshot key is personality__knob).</summary>
		public static bool ValidKnob(string knob) => !string.IsNullOrEmpty(knob) && knob.Length <= 96 && !knob.Contains("__", StringComparison.Ordinal) && KnobName.IsMatch(knob);

		/// <summary>A game id / style bucket the fitter accepts ([A-Za-z0-9_.-]+).</summary>
		public static bool ValidName(string name) => !string.IsNullOrEmpty(name) && name.Length <= 128 && SafeName.IsMatch(name);

		/// <summary>
		/// The valid signals of one seat, in the order given: an invalid knob id is dropped, a duplicate knob keeps the first, a value is clamped to [-1, 1] and NaN / infinity becomes null (no evidence).
		/// <paramref name="dropped"/> counts what was dropped.
		/// </summary>
		public static List<LearnSignal> Sanitize(IEnumerable<LearnSignal> signals, out int dropped)
		{
			dropped = 0;
			var result = new List<LearnSignal>();
			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (var s in signals)
			{
				if (!ValidKnob(s.Knob) || !seen.Add(s.Knob))
				{
					dropped++;
					continue;
				}

				double? value = s.Value is double v && !double.IsNaN(v) && !double.IsInfinity(v) ? Math.Max(-1.0, Math.Min(1.0, v)) : null;
				result.Add(new LearnSignal(s.Knob, value));
			}

			return result;
		}

		/// <summary>
		/// One record line, or null when the seat must not be written: no valid knob (nothing was armed), a personality the fitter would refuse (contains "__" or is not a safe name), or an
		/// invalid game id. The enemies are one entry per faction with weight = the number of enemy PLAYERS of that faction (the runtime reader counts one entry per player).
		/// </summary>
		public static string Record(string gameId, long seq, string seat, in LearnSeatContext ctx, IReadOnlyList<LearnSignal> signals)
		{
			if (!ValidName(gameId) || seq < 0 || string.IsNullOrEmpty(seat) || signals == null || signals.Count == 0)
				return null;

			var personality = string.IsNullOrEmpty(ctx.Personality) ? null : ctx.Personality;
			if (personality != null && (personality.Contains("__", StringComparison.Ordinal) || !ValidName(personality)))
				return null;

			var b = new StringBuilder(256);
			b.Append("{\"game_id\":").Append(Str(gameId)).Append(",\"seq\":").Append(seq.ToString(CultureInfo.InvariantCulture));
			b.Append(",\"seat\":").Append(Str(seat));
			b.Append(",\"personality\":").Append(NameOrNull(personality));
			b.Append(",\"own_faction\":").Append(NameOrNull(ctx.OwnFaction));
			b.Append(",\"main_target\":").Append(NameOrNull(ctx.MainTarget));
			b.Append(",\"main_target_style\":").Append(NameOrNull(ctx.MainTargetStyle));
			b.Append(",\"enemies\":[");

			var order = new List<string>();
			var counts = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var raw in ctx.Enemies ?? Array.Empty<string>())
			{
				// A Random or hidden pick is "" (public view): one player with no faction, written as null so the fitter counts the player but never steps a faction for it.
				var f = ValidName(raw) ? raw : "";
				if (!counts.TryGetValue(f, out var n))
					order.Add(f);

				counts[f] = Math.Min(MaxEnemyWeight, n + 1);
			}

			for (var i = 0; i < order.Count; i++)
			{
				if (i > 0)
					b.Append(',');

				b.Append("{\"faction\":").Append(NameOrNull(order[i])).Append(",\"weight\":").Append(counts[order[i]].ToString(CultureInfo.InvariantCulture)).Append('}');
			}

			b.Append("],\"knobs\":[");
			for (var i = 0; i < signals.Count; i++)
			{
				if (i > 0)
					b.Append(',');

				b.Append("{\"knob\":").Append(Str(signals[i].Knob)).Append(",\"signal\":");
				if (signals[i].Value is double v)
					b.Append(v.ToString("R", CultureInfo.InvariantCulture));
				else
					b.Append("null");

				b.Append('}');
			}

			return b.Append("]}").ToString();
		}

		static string NameOrNull(string name) => ValidName(name) ? Str(name) : "null";

		// Only validated names (no quote, backslash or control character possible) reach here.
		static string Str(string value) => "\"" + value + "\"";
	}
}
