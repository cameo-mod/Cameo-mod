#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// Ports of the scalar helpers in <c>tools/balance/formula.py</c>: Int32/bool/WDist
	/// parsing, burst-delay cadence math, charge-up records, and the default-condition
	/// evaluator. All arithmetic mirrors the Python (and therefore the C# engine)
	/// semantics exactly, including truncate-toward-zero division.
	/// </summary>
	public static class BotFormula
	{
		public const double EngineDefaultReloadDelay = 1.0;
		public const int EngineDefaultBurst = 1;
		public const double EngineDefaultBurstDelay = 5.0;
		public const double EngineDefaultRange = 0.0;
		public const double ChargeUpPriceMultiplier = 0.75;
		public const double ChargeAnchorShare = 1.0 / 3.0;

		static readonly Regex Int32Text = new("^[+-]?[0-9]+$", RegexOptions.Compiled);
		static readonly Regex WdistText = new(@"^([+-]?[0-9]+)\s*(?:c\s*([+-]?[0-9]+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex CondToken = new("[A-Za-z_][A-Za-z0-9_.\\-]*", RegexOptions.Compiled);
		static readonly HashSet<string> CondKeep = new(StringComparer.Ordinal) { "and", "or", "not", "True", "False" };

		/// <summary>Traits that make an actor pay for a charge-up (formula.py CHARGE_UP_TRAITS).</summary>
		public static readonly IReadOnlySet<string> ChargeUpTraits = new HashSet<string>(StringComparer.Ordinal)
		{
			"AttackCharged", "AttackTurretedCharged", "AttackFrontalCharged", "AttackCharges", "AttackTesla",
		};

		/// <summary>Retired by W16 — kept as an empty set so older callers keep working.</summary>
		public static readonly IReadOnlySet<string> ChargeUpExcludedTraits = new HashSet<string>(StringComparer.Ordinal);

		/// <summary>Parse one C# Int32 field without accepting fractional coercion. Null input returns <paramref name="defaultValue"/>.</summary>
		public static int? ParseInt32(string raw, string field = "value", int? defaultValue = null)
		{
			if (raw == null)
				return defaultValue;
			var text = raw.Trim();
			if (!Int32Text.IsMatch(text))
				throw new InvalidDataException($"{field} must be an Int32");
			if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
				value < int.MinValue || value > int.MaxValue)
				throw new InvalidDataException($"{field} is outside the Int32 range");
			return (int)value;
		}

		/// <summary>Parse one C# bool field (only true/false, case-insensitive).</summary>
		public static bool? ParseBool(string raw, string field = "value", bool? defaultValue = null)
		{
			if (raw == null)
				return defaultValue;
			var text = raw.Trim();
			if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
				return true;
			if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
				return false;
			throw new InvalidDataException($"{field} must be true or false");
		}

		internal static int UncheckedInt32(long value) => unchecked((int)value);

		/// <summary>C# integer division: truncate toward zero, including signed values.</summary>
		public static long TruncDiv(long numerator, long denominator)
		{
			if (denominator == 0)
				throw new DivideByZeroException("runtime geometry divides by a zero-width range step");
			return numerator / denominator;
		}

		/// <summary>C# integer division on Int128 intermediates.</summary>
		public static Int128 TruncDiv(Int128 numerator, Int128 denominator)
		{
			if (denominator == 0)
				throw new DivideByZeroException("percentage denominator must be non-zero");
			return numerator / denominator;
		}

		/// <summary>Python floor division (<c>//</c>) on ints.</summary>
		public static long FloorDiv(long numerator, long denominator)
		{
			if (denominator == 0)
				throw new DivideByZeroException("integer division by zero");
			var q = numerator / denominator;
			var r = numerator % denominator;
			if (r != 0 && ((r < 0) != (denominator < 0)))
				q--;
			return q;
		}

		/// <summary>
		/// Parse OpenRA's integer or cell-relative WDist notation (<c>40c0</c> = 40960).
		/// With <paramref name="allowDistribution"/> a comma list reduces to its integer mean.
		/// </summary>
		public static int ParseWdist(string raw, bool allowDistribution = false)
		{
			var text = (raw ?? "").Trim();
			if (text.Contains(','))
			{
				if (!allowDistribution)
					throw new InvalidDataException("scalar WDist cannot contain a distribution");
				var parts = text.Split(',');
				if (parts.Any(part => part.Trim().Length == 0))
					throw new InvalidDataException("WDist distribution contains an empty value");
				var sum = parts.Sum(part => (long)ParseWdist(part));
				return (int)FloorDiv(sum, parts.Length);
			}

			var match = WdistText.Match(text);
			if (!match.Success)
				throw new InvalidDataException($"invalid WDist: {raw}");
			var first = ParseInt32(match.Groups[1].Value, "WDist component").Value;
			var remainder = match.Groups[2];
			if (!remainder.Success)
				return first;
			var subcell = ParseInt32(remainder.Value, "WDist subcell component").Value;
			if (first < 0)
				subcell = -subcell;
			return UncheckedInt32(1024L * first + subcell);
		}

		/// <summary>Safe scalar WDist: returns <paramref name="defaultValue"/> on blank or unparseable input.</summary>
		public static int? WdistValue(string raw, int? defaultValue = null)
		{
			if (raw == null || raw.Trim().Length == 0)
				return defaultValue;
			try
			{
				return ParseWdist(raw);
			}
			catch (Exception e) when (e is InvalidDataException or FormatException or OverflowException)
			{
				return defaultValue;
			}
		}

		/// <summary>Python <c>float(str(raw).strip())</c>: null input parses as "None" → null.</summary>
		public static double? PyFloat(string raw)
		{
			var text = (raw ?? "None").Trim();
			if (text.Length == 0)
				return null;
			switch (text.ToLowerInvariant())
			{
				case "inf":
				case "+inf":
				case "infinity":
				case "+infinity":
					return double.PositiveInfinity;
				case "-inf":
				case "-infinity":
					return double.NegativeInfinity;
				case "nan":
				case "+nan":
				case "-nan":
					return double.NaN;
			}

			if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
				return value;

			// Python float() accepts underscores between digits ("1_000").
			if (text.Contains('_'))
			{
				var squashed = Regex.Replace(text, "(?<=\\d)_(?=\\d)", "");
				if (double.TryParse(squashed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
					return value;
			}

			return null;
		}

		/// <summary>First number in a raw ledger value ("15, 15" -> 15.0), else null. A node reads its Value.</summary>
		public static double? Fnum(object raw)
		{
			if (raw is MiniYamlMirrorNode node)
				raw = node.Value;
			var s = raw?.ToString();
			if (s == null)
				return null;
			return PyFloat(s.Split(',')[0].Trim());
		}

		/// <summary>Parse the engine's integer BurstDelays field without truncation; null when unusable.</summary>
		public static int[] BurstDelayValues(object raw)
		{
			if (raw is MiniYamlMirrorNode node)
				raw = node.Value;
			var s = raw?.ToString();
			if (s == null || s.Trim().Length == 0)
				return null;
			var values = new List<int>();
			foreach (var part in s.Split(','))
			{
				var number = PyFloat(part.Trim());
				if (!number.HasValue || !double.IsFinite(number.Value) || number.Value != Math.Floor(number.Value))
					return null;
				if (number.Value < int.MinValue || number.Value > int.MaxValue)
					return null;
				values.Add((int)number.Value);
			}

			return values.Count > 0 ? values.ToArray() : null;
		}

		/// <summary>Total inter-shot delay in one engine burst.</summary>
		public static double BurstDelaySum(int burst = 1, object burstDelays = null)
		{
			var gaps = Math.Max((burst != 0 ? burst : 1) - 1, 0);
			if (gaps == 0)
				return 0.0;
			var values = BurstDelayValues(burstDelays);
			if (values == null || values.Length == 0)
				values = new[] { (int)EngineDefaultBurstDelay };
			if (values.Length == 1)
				return (long)values[0] * gaps;
			return values.Take(gaps).Sum(v => (long)v);
		}

		/// <summary>Effective ticks per full burst cycle, including every inter-shot delay.</summary>
		public static double EffReload(double reloadDelay, int burst = 1, object burstDelays = null)
		{
			return reloadDelay + BurstDelaySum(burst, burstDelays);
		}

		/// <summary>Fraction of an attack cycle the actor spends winding up; 0 when unknown.</summary>
		public static double ChargeShare(double? ticks, double? cycle)
		{
			if (!ticks.HasValue || !cycle.HasValue || ticks.Value <= 0 || cycle.Value <= 0)
				return 0.0;
			return ticks.Value / (ticks.Value + cycle.Value);
		}

		/// <summary>
		/// (cycle ticks, shots per cycle) for a trait that OVERRIDES the weapon's reload
		/// (AttackTesla); null for the ChargeLevel family, whose gun keeps its own reload.
		/// </summary>
		public static (double Cycle, int Shots)? ChargeAttackCycle(BotChargeUp charge, double? weaponReload)
		{
			if (charge == null)
				return null;
			var reload = charge.CycleReload;
			if (!reload.HasValue || reload.Value == 0)
				return null;
			var burst = charge.Burst.HasValue && charge.Burst.Value != 0 ? (int)charge.Burst.Value : 1;
			var windUp = charge.Ticks ?? 0.0;
			var chargeDelay = charge.ChargeDelay;
			if (!chargeDelay.HasValue || !weaponReload.HasValue || weaponReload.Value == 0)
				return (EffReload(reload.Value, burst, weaponReload) + windUp, burst);
			double gap;
			if (weaponReload.Value > chargeDelay.Value)
				gap = weaponReload.Value + windUp;   // charge-per-shot
			else
				gap = chargeDelay.Value;             // charge-once
			return ((burst - 1) * gap + reload.Value + windUp, burst);
		}

		/// <summary>Price discount for a charging actor, proportional to its real charge burden (W16).</summary>
		public static double ChargePriceMultiplier(BotChargeUp charge, double? reloadFallback = null)
		{
			if (charge == null)
				return 1.0;
			var trait = charge.Trait;
			var ticks = charge.Ticks;
			var own = ChargeAttackCycle(charge, reloadFallback);
			var cycle = own?.Cycle ?? reloadFallback;
			if (string.IsNullOrEmpty(trait) || !ChargeUpTraits.Contains(trait.Split('@')[0]))
				return 1.0;
			var share = ChargeShare(ticks, cycle);
			if (share <= 0.0)
				return ChargeUpPriceMultiplier;
			var scaled = 1.0 - (1.0 - ChargeUpPriceMultiplier) * (share / ChargeAnchorShare);
			return Math.Min(1.0, Math.Max(ChargeUpPriceMultiplier, scaled));
		}

		/// <summary>
		/// Is this OpenRA condition expression true for a unit as built? Every named
		/// condition evaluates false (negated forms therefore true); unparseable input
		/// evaluates false — mirror of <c>formula.condition_holds_by_default</c>.
		/// </summary>
		public static bool ConditionHoldsByDefault(string expr)
		{
			if (expr == null)
				return true;
			var src = expr.Trim();
			if (src.Length == 0)
				return true;

			// Order matters: `!=` must survive the `!` -> `not` rewrite.
			src = src.Replace("!=", "\0NE\0")
				.Replace("&&", " and ")
				.Replace("||", " or ")
				.Replace("!", " not ")
				.Replace("\0NE\0", "!=");

			// Every condition name is 0/False. Keep Python keywords the rewrite produced.
			src = CondToken.Replace(src, m => CondKeep.Contains(m.Value) ? m.Value : "0");
			try
			{
				return EvalCondition(src) != 0.0;
			}
			catch (FormatException)
			{
				return false;
			}
			catch (DivideByZeroException)
			{
				return false;
			}
			catch (OverflowException)
			{
				return false;
			}
		}

		enum CondKind { Number, Word, LParen, RParen, Eq, Ne, Lt, Le, Gt, Ge, Plus, Minus, Mul, Div, Pow }

		static double EvalCondition(string src)
		{
			var tokens = new List<(CondKind Kind, double Value, string Text)>();
			var i = 0;
			while (i < src.Length)
			{
				var ch = src[i];
				if (char.IsWhiteSpace(ch))
				{
					i++;
					continue;
				}

				if (char.IsDigit(ch) || (ch == '.' && i + 1 < src.Length && char.IsDigit(src[i + 1])))
				{
					var m = Regex.Match(src.Substring(i), @"^\d+(?:\.\d*)?(?:[eE][+-]?\d+)?|^\.\d+(?:[eE][+-]?\d+)?");
					if (!m.Success || !double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var num))
						throw new FormatException($"bad number in '{src}'");
					tokens.Add((CondKind.Number, num, m.Value));
					i += m.Length;
					continue;
				}

				if (char.IsLetter(ch) || ch == '_')
				{
					var start = i;
					while (i < src.Length && (char.IsLetterOrDigit(src[i]) || src[i] == '_' || src[i] == '.'))
						i++;
					tokens.Add((CondKind.Word, 0.0, src.Substring(start, i - start)));
					continue;
				}

				switch (ch)
				{
					case '(': tokens.Add((CondKind.LParen, 0, "(")); i++; break;
					case ')': tokens.Add((CondKind.RParen, 0, ")")); i++; break;
					case '+': tokens.Add((CondKind.Plus, 0, "+")); i++; break;
					case '-': tokens.Add((CondKind.Minus, 0, "-")); i++; break;
					case '*':
						if (i + 1 < src.Length && src[i + 1] == '*')
						{
							tokens.Add((CondKind.Pow, 0, "**"));
							i += 2;
						}
						else
						{
							tokens.Add((CondKind.Mul, 0, "*"));
							i++;
						}

						break;
					case '/': tokens.Add((CondKind.Div, 0, "/")); i++; break;
					case '=':
						if (i + 1 >= src.Length || src[i + 1] != '=')
							throw new FormatException($"bare '=' in '{src}'");
						tokens.Add((CondKind.Eq, 0, "=="));
						i += 2;
						break;
					case '!':
						if (i + 1 >= src.Length || src[i + 1] != '=')
							throw new FormatException($"bare '!' in '{src}'");
						tokens.Add((CondKind.Ne, 0, "!="));
						i += 2;
						break;
					case '<':
						if (i + 1 < src.Length && src[i + 1] == '=')
						{
							tokens.Add((CondKind.Le, 0, "<="));
							i += 2;
						}
						else
						{
							tokens.Add((CondKind.Lt, 0, "<"));
							i++;
						}

						break;
					case '>':
						if (i + 1 < src.Length && src[i + 1] == '=')
						{
							tokens.Add((CondKind.Ge, 0, ">="));
							i += 2;
						}
						else
						{
							tokens.Add((CondKind.Gt, 0, ">"));
							i++;
						}

						break;
					default:
						throw new FormatException($"unexpected '{ch}' in '{src}'");
				}
			}

			var pos = 0;
			var result = ParseOr(tokens, ref pos);
			if (pos != tokens.Count)
				throw new FormatException($"trailing tokens in '{src}'");
			return result;
		}

		static bool IsWord(List<(CondKind Kind, double Value, string Text)> tokens, int pos, string word)
		{
			return pos < tokens.Count && tokens[pos].Kind == CondKind.Word && tokens[pos].Text == word;
		}

		static double ParseOr(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParseAnd(tokens, ref pos);
			while (IsWord(tokens, pos, "or"))
			{
				pos++;
				var right = ParseAnd(tokens, ref pos);
				left = left != 0 ? left : right;   // Python `a or b` returns the operand, not a bool
			}

			return left;
		}

		static double ParseAnd(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParseNot(tokens, ref pos);
			while (IsWord(tokens, pos, "and"))
			{
				pos++;
				var right = ParseNot(tokens, ref pos);
				left = left != 0 ? right : left;   // Python `a and b` returns the operand
			}

			return left;
		}

		static double ParseNot(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			if (IsWord(tokens, pos, "not"))
			{
				pos++;
				return ParseNot(tokens, ref pos) != 0 ? 0.0 : 1.0;
			}

			return ParseComparison(tokens, ref pos);
		}

		static double ParseComparison(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParseAdditive(tokens, ref pos);
			var any = false;
			var all = true;
			while (pos < tokens.Count && tokens[pos].Kind is CondKind.Eq or CondKind.Ne or CondKind.Lt or CondKind.Le or CondKind.Gt or CondKind.Ge)
			{
				var op = tokens[pos].Kind;
				pos++;
				var right = ParseAdditive(tokens, ref pos);
				any = true;
				var hold = op switch
				{
					CondKind.Eq => left == right,
					CondKind.Ne => left != right,
					CondKind.Lt => left < right,
					CondKind.Le => left <= right,
					CondKind.Gt => left > right,
					CondKind.Ge => left >= right,
					_ => false,
				};
				if (!hold)
					all = false;
				left = right;   // chained comparisons: left operand of the next op is this right side
			}

			return any ? (all ? 1.0 : 0.0) : left;
		}

		static double ParseAdditive(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParseMultiplicative(tokens, ref pos);
			while (pos < tokens.Count && tokens[pos].Kind is CondKind.Plus or CondKind.Minus)
			{
				var op = tokens[pos].Kind;
				pos++;
				var right = ParseMultiplicative(tokens, ref pos);
				left = op == CondKind.Plus ? left + right : left - right;
			}

			return left;
		}

		static double ParseMultiplicative(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParseUnary(tokens, ref pos);
			while (pos < tokens.Count && tokens[pos].Kind is CondKind.Mul or CondKind.Div)
			{
				var op = tokens[pos].Kind;
				pos++;
				var right = ParseUnary(tokens, ref pos);
				if (op == CondKind.Mul)
					left *= right;
				else
				{
					if (right == 0)
						throw new DivideByZeroException();
					left /= right;
				}
			}

			return left;
		}

		static double ParseUnary(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			if (pos < tokens.Count && tokens[pos].Kind == CondKind.Minus)
			{
				pos++;
				return -ParseUnary(tokens, ref pos);
			}

			if (pos < tokens.Count && tokens[pos].Kind == CondKind.Plus)
			{
				pos++;
				return ParseUnary(tokens, ref pos);
			}

			return ParsePower(tokens, ref pos);
		}

		static double ParsePower(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			var left = ParsePrimary(tokens, ref pos);
			if (pos < tokens.Count && tokens[pos].Kind == CondKind.Pow)
			{
				pos++;
				var right = ParseUnary(tokens, ref pos);   // right-assoc; unary on the exponent
				return Math.Pow(left, right);
			}

			return left;
		}

		static double ParsePrimary(List<(CondKind Kind, double Value, string Text)> tokens, ref int pos)
		{
			if (pos >= tokens.Count)
				throw new FormatException("unexpected end of expression");
			var token = tokens[pos];
			switch (token.Kind)
			{
				case CondKind.Number:
					pos++;
					return token.Value;
				case CondKind.Word:
					if (token.Text == "True")
					{
						pos++;
						return 1.0;
					}

					if (token.Text == "False")
					{
						pos++;
						return 0.0;
					}

					throw new FormatException($"unexpected identifier '{token.Text}'");
				case CondKind.LParen:
				{
					pos++;
					var inner = ParseOr(tokens, ref pos);
					if (pos >= tokens.Count || tokens[pos].Kind != CondKind.RParen)
						throw new FormatException("unbalanced '('");
					pos++;
					return inner;
				}

				default:
					throw new FormatException($"unexpected '{token.Text}'");
			}
		}
	}

	/// <summary>A heaviness / anchor configuration the engine would reject at RulesetLoaded.</summary>
	public sealed class BotHeavinessException : Exception
	{
		public BotHeavinessException(string message)
			: base(message) { }
	}

	/// <summary>
	/// Shared runtime model for DESIGN §12.0i continuous heaviness — mirror of
	/// <c>tools/balance/effective_heaviness.py</c> (and of the engine's
	/// AreaDamageWarhead.cs / HeavinessBell.cs behaviour it documents).
	/// </summary>
	public static class BotHeaviness
	{
		public const int Disabled = -1;
		public const int HeavinessMax = 2000;
		public const string ModeLegacy = "Legacy";
		public const string ModeShared = "SharedVersus";
		public const double BellLo = 1.0 / 1.5;
		public const double BellSigma = 0.75;
		public const double HeroicDivisor = 200.0;

		static readonly string[] HeavinessModes = { ModeLegacy, ModeShared };

		/// <summary>HeavinessBell.cs — one global 13-slot scale, 0..2, step 1/6.</summary>
		static readonly string[][] BellAxisOrder =
		{
			new[] { "Scout" }, new[] { "None" }, new[] { "Fighter" }, new[] { "Light" },
			new[] { "Wood" }, new[] { "Bomber" }, new[] { "Medium", "Flak", "Steel" },
			new[] { "Helicopter" }, new[] { "Concrete" }, new[] { "Heavy" },
			new[] { "Spaceship" }, new[] { "Plate" }, new[] { "Superheavy" },
		};

		public static readonly IReadOnlyDictionary<string, double> BellAxis = BuildBellAxis();

		static Dictionary<string, double> BuildBellAxis()
		{
			var axis = new Dictionary<string, double>(StringComparer.Ordinal);
			for (var i = 0; i < BellAxisOrder.Length; i++)
				foreach (var armor in BellAxisOrder[i])
					axis[armor] = i * 2.0 / (BellAxisOrder.Length - 1);
			return axis;
		}

		/// <summary>Armor rows derived geometrically from parents (DESIGN §12.0l), in compute order.</summary>
		public static readonly IReadOnlyList<(string Name, string[] Parents)> GeoDerived = new (string, string[])[]
		{
			("FlyingInfantry", new[] { "Scout", "Flak", "Helicopter" }),
			("CyborgLight", new[] { "None", "Light" }),
			("CyborgMedium", new[] { "Flak", "Medium" }),
			("CyborgHeavy", new[] { "Plate", "Heavy" }),
			("CyborgHeroic", new[] { "Heroic", "Superheavy" }),
			("AntiAirInfantry", new[] { "None", "Flak" }),
			("AntiAirVehicle", new[] { "Light", "Medium" }),
			("AntiAirBuilding", new[] { "Concrete", "Steel" }),
			("ShipLight", new[] { "Light", "Wood" }),
			("ShipMedium", new[] { "Medium", "Concrete" }),
			("ShipHeavy", new[] { "Heavy", "Steel" }),
			("ShipSuperheavy", new[] { "Superheavy", "Steel" }),
			("AntiAirShip", new[] { "ShipLight", "ShipMedium" }),
			("SubmarineLight", new[] { "ShipMedium", "Heavy" }),
			("SubmarineHeavy", new[] { "ShipHeavy", "Superheavy" }),
		};

		public static readonly IReadOnlySet<string> DerivedArmors =
			new HashSet<string>(GeoDerived.Select(g => g.Name).Append("Heroic"), StringComparer.Ordinal);

		public static readonly IReadOnlySet<string> NonArmorRows = new HashSet<string>(StringComparer.Ordinal)
		{
			"Shield", "HAZMAT", "COMPOSITE", "BLAST", "REFLECTOR", "ARMOR",
		};

		/// <summary>Ladders, lightest to heaviest, for the rank-restore step.</summary>
		static readonly string[][] Ladders =
		{
			new[] { "None", "Flak", "Plate", "Heroic" },
			new[] { "Scout", "Light", "Medium", "Heavy", "Superheavy" },
			new[] { "Wood", "Steel", "Concrete" },
			new[] { "Fighter", "Bomber", "Helicopter", "Spaceship" },
		};

		/// <summary>Read and canonicalise HeavinessMode (Legacy default); unknown values fail clear.</summary>
		public static string HeavinessModeOf(MiniYamlMirrorNode node)
		{
			var raw = node.Get("HeavinessMode");
			if (raw == null || raw.Trim().Length == 0)
				return ModeLegacy;
			var value = raw.Trim();
			var head = value.TrimStart('-');
			if (head.Length > 0 && head.All(char.IsDigit))
			{
				var index = int.Parse(value, CultureInfo.InvariantCulture);
				if (index >= 0 && index < HeavinessModes.Length)
					return HeavinessModes[index];
				throw new BotHeavinessException(
					$"Unknown HeavinessMode {raw}: numeric value outside {HeavinessModes.Length} defined modes " +
					"(Enum.IsDefined rejects it at rules load on the C# side).");
			}

			foreach (var known in HeavinessModes)
				if (string.Equals(value, known, StringComparison.OrdinalIgnoreCase))
					return known;
			throw new BotHeavinessException(
				$"Unknown HeavinessMode {raw}: must be one of {string.Join(", ", HeavinessModes)} " +
				$"(by name, or by their {HeavinessModes.Length}-value numeric index).");
		}

		/// <summary>Read Heaviness from an AreaDamage warhead node (-1 when omitted).</summary>
		public static int HeavinessOf(MiniYamlMirrorNode node)
		{
			var value = BotFormula.ParseInt32(node.Get("Heaviness"), "Heaviness", Disabled) ?? Disabled;
			if (value < Disabled || value > HeavinessMax)
				throw new BotHeavinessException(
					$"Heaviness must be -1 (disabled) or 0..{HeavinessMax} (thousandths of h), got {value}");
			return value;
		}

		/// <summary>Validate the (mode, Heaviness, percentage tables) triple exactly as the C# RulesetLoaded does.</summary>
		public static (string Mode, int Heaviness) HeavinessProfileConfig(
			MiniYamlMirrorNode node,
			IReadOnlyDictionary<string, int?> light,
			IReadOnlyDictionary<string, int?> medium,
			IReadOnlyDictionary<string, int?> heavy,
			bool subclassTwin = false)
		{
			var mode = HeavinessModeOf(node);
			var heaviness = HeavinessOf(node);
			if (mode == ModeShared)
			{
				if (subclassTwin)
					throw new BotHeavinessException(
						"AreaDamagePercentage does not support the SharedVersus heaviness mode: " +
						"only the Legacy mode (the default) is available here.");
				if (heaviness < 0)
					throw new BotHeavinessException(
						"HeavinessMode SharedVersus requires an active Heaviness (0..2000); " +
						"omitting Heaviness disables heaviness entirely.");
				if (medium.Count > 0 || light.Count > 0 || heavy.Count > 0)
					throw new BotHeavinessException(
						"HeavinessMode SharedVersus rejects PercentageVersus and the " +
						"PercentageVersusLight/Heavy endpoints: the percentage half follows the " +
						"SAME belled table as the flat half.");
				return (mode, heaviness);
			}

			ValidateAnchors(heaviness, light, medium, heavy, subclassTwin);
			return (mode, heaviness);
		}

		/// <summary>The shared profile's nonnegative-input contract + load-time overflow check.</summary>
		public static void ValidateSharedNumeric(string mode, int heaviness,
			IReadOnlyDictionary<string, int?> versus, int? damage = null, int? scale = null)
		{
			if (mode != ModeShared)
				return;
			if (damage.HasValue && damage.Value < 0)
				throw new BotHeavinessException(
					$"HeavinessMode SharedVersus rejects a negative Damage ({damage.Value}); the approved conversion is nonnegative.");
			if (scale.HasValue && scale.Value < 0)
				throw new BotHeavinessException(
					$"HeavinessMode SharedVersus rejects a negative PercentageScale ({scale.Value}); the approved conversion is nonnegative.");
			var shield = versus != null && versus.TryGetValue("Shield", out var s) ? s : 0;
			if (shield < 0)
				throw new BotHeavinessException(
					$"HeavinessMode SharedVersus rejects a negative Shield coefficient ({shield}); " +
					"the approved (2000 + h) / 2000 scaling is nonnegative.");
			if (damage.HasValue && scale.HasValue)
			{
				var (growthNum, growthDen) = BotPercentageDamage.SharedGrowth(heaviness);
				var denominator = (Int128)200_000 * growthDen;
				var rounded = ((Int128)damage.Value * scale.Value * growthNum + denominator / 2) / denominator;
				if (rounded > int.MaxValue)
					throw new OverflowException(
						$"HeavinessMode SharedVersus: percentage units (Damage {damage.Value} x " +
						$"PercentageScale {scale.Value} x Heaviness {heaviness}) exceed Int32.");
			}
		}

		/// <summary>The shared profile's single Shield scaling: (2000 + h) / 2000, half-up.</summary>
		public static int ShieldCoefficient(int value, int heaviness)
		{
			return (int)BotFormula.FloorDiv((long)value * (2000 + heaviness) + 1000, 2000);
		}

		/// <summary>The shared profile's single effective table: flat Versus belled once, Shield scaled once.</summary>
		public static Dictionary<string, int?> SharedVersusProfile(IReadOnlyDictionary<string, int?> versus, int heaviness)
		{
			var belled = BellTransform(versus, heaviness / 1000.0, true);
			var output = new Dictionary<string, int?>(belled, StringComparer.Ordinal);
			if (output.TryGetValue("Shield", out var s) && s.HasValue)
				output["Shield"] = ShieldCoefficient(s.Value, heaviness);
			return output;
		}

		/// <summary>Mirror the C# anchor validation (same order, same conditions).</summary>
		public static void ValidateAnchors(int heaviness,
			IReadOnlyDictionary<string, int?> light,
			IReadOnlyDictionary<string, int?> medium,
			IReadOnlyDictionary<string, int?> heavy,
			bool subclassTwin = false)
		{
			var hasBands = light.Count > 0 || heavy.Count > 0;
			if (subclassTwin && hasBands)
				throw new BotHeavinessException(
					"AreaDamagePercentage cannot set PercentageVersusLight/Heavy: these endpoints " +
					"parameterise the folded percentage half, which this warhead forbids entirely.");
			if (heaviness < 0 && hasBands)
				throw new BotHeavinessException(
					"PercentageVersusLight/Heavy endpoints require an active Heaviness (0..2000); " +
					"omitting Heaviness disables heaviness entirely.");
			if (!hasBands)
				return;
			if (light.Count == 0 || heavy.Count == 0)
				throw new BotHeavinessException("PercentageVersusLight and PercentageVersusHeavy must both be set.");
			if (medium.Count == 0)
				throw new BotHeavinessException(
					"PercentageVersusLight/Heavy require an explicit PercentageVersus (the h=1 medium anchor).");
			if (!SameKeys(light, medium) || !SameKeys(heavy, medium))
				throw new BotHeavinessException("PercentageVersusLight/Heavy keys must match PercentageVersus exactly.");
			foreach (var (name, anchor) in new (string, IReadOnlyDictionary<string, int?>)[]
			{
				("PercentageVersus", medium), ("PercentageVersusLight", light), ("PercentageVersusHeavy", heavy),
			})
			{
				foreach (var kv in anchor)
					if (!kv.Value.HasValue || kv.Value.Value < 0)
						throw new BotHeavinessException(
							$"{name} anchor values must be non-negative ({kv.Key}: {kv.Value}).");
			}
		}

		static bool SameKeys(IReadOnlyDictionary<string, int?> a, IReadOnlyDictionary<string, int?> b)
		{
			return a.Count == b.Count && a.Keys.All(b.ContainsKey);
		}

		/// <summary>Integer half-to-even rounding, truncating toward zero on non-ties.</summary>
		public static int RoundHalfEven(long numerator, long denominator)
		{
			if (denominator == 0)
				throw new DivideByZeroException("round_half_even denominator must be non-zero");
			var negative = (numerator < 0) != (denominator < 0);
			var magnitude = Math.Abs(numerator);
			var denom = Math.Abs(denominator);
			var quotient = magnitude / denom;
			var remainder = magnitude % denom;
			var twice = 2 * remainder;
			if (twice > denom || (twice == denom && quotient % 2 != 0))
				quotient++;
			return (int)(negative ? -quotient : quotient);
		}

		/// <summary>One bounded radius scale shared with the C# (ScaledRadiusLength).</summary>
		public static int ScaleLength(int length, int heaviness)
		{
			if (heaviness < 0)
				return length;
			var scale = (heaviness / 1000.0 + 2.0) / 3.0;
			var scaled = length * scale;
			if (scaled > int.MaxValue || scaled < int.MinValue)
				throw new BotHeavinessException(
					$"Scaled radius {scaled:f} (authored {length} x {scale:f} at h={heaviness / 1000.0}) overflows Int32.");
			return (int)scaled;
		}

		/// <summary>Piecewise per-armor interpolation between the three anchor tables (ties-even).</summary>
		public static Dictionary<string, int?> InterpolatePercentageBands(
			IReadOnlyDictionary<string, int?> light,
			IReadOnlyDictionary<string, int?> medium,
			IReadOnlyDictionary<string, int?> heavy,
			int heaviness)
		{
			var interpolated = new Dictionary<string, int?>(StringComparer.Ordinal);
			foreach (var kv in medium)
			{
				long product;
				if (heaviness <= 1000)
					product = light[kv.Key].Value * (1000L - heaviness) + kv.Value.Value * heaviness;
				else
					product = kv.Value.Value * (long)(HeavinessMax - heaviness) + heavy[kv.Key].Value * (heaviness - 1000L);
				interpolated[kv.Key] = RoundHalfEven(product, 1000);
			}

			return interpolated;
		}

		static double? CentreOfMass(Dictionary<string, double> values)
		{
			var total = 0.0;
			var weighted = 0.0;
			foreach (var kv in values)
				if (kv.Value > 0 && BellAxis.TryGetValue(kv.Key, out var x))
				{
					total += kv.Value;
					weighted += x * kv.Value;
				}

			return total != 0 ? weighted / total : null;
		}

		static double GeometricMean(IEnumerable<double> values)
		{
			var sum = 0.0;
			var count = 0;
			foreach (var v in values)
				if (v > 0)
				{
					sum += Math.Log(v);
					count++;
				}

			return count == 0 ? 0.0 : Math.Exp(sum / count);
		}

		/// <summary>Exact mirror of HeavinessBell.Transform (float math, int output).</summary>
		public static Dictionary<string, int?> BellTransform(
			IReadOnlyDictionary<string, int?> table, double h, bool mainTable = false)
		{
			var values = table.ToDictionary(kv => kv.Key, kv => (double)kv.Value.Value, StringComparer.Ordinal);
			var live = values.Where(kv => !NonArmorRows.Contains(kv.Key)).Select(kv => kv.Value).ToList();
			if (live.Count == 0 || live.Max() <= live.Min())
				return table.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);

			var tiltable = values
				.Where(kv => BellAxis.ContainsKey(kv.Key) && !DerivedArmors.Contains(kv.Key))
				.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
			var com = CentreOfMass(tiltable);
			if (com.HasValue)
			{
				var mu = (h + com.Value) / 2.0;
				var belled = tiltable.ToDictionary(
					kv => kv.Key,
					kv => kv.Value * (BellLo + (1.0 - BellLo) *
						Math.Exp(-((BellAxis[kv.Key] - mu) * (BellAxis[kv.Key] - mu)) / (2.0 * BellSigma * BellSigma))),
					StringComparer.Ordinal);
				var before = GeometricMean(tiltable.Values);
				var after = GeometricMean(belled.Values);
				if (before > 0 && after > 0)
				{
					var factor = before / after;
					foreach (var key in belled.Keys.ToList())
						belled[key] *= factor;
				}

				foreach (var ladder in Ladders)
				{
					var rungs = ladder.Where(belled.ContainsKey).ToList();
					if (rungs.Count < 2)
						continue;
					var order = Enumerable.Range(0, rungs.Count)
						.OrderBy(i => -values[rungs[i]])
						.ThenBy(i => i)
						.ToList();
					var ranked = rungs.Select(r => belled[r]).OrderByDescending(x => x).ToList();
					for (var slot = 0; slot < order.Count; slot++)
						values[rungs[order[slot]]] = ranked[slot];
				}
			}

			var result = values.ToDictionary(
				kv => kv.Key,
				kv => (int?)Math.Round(kv.Value, 0, MidpointRounding.ToEven),
				StringComparer.Ordinal);
			if (mainTable && result.ContainsKey("Heroic") && result.ContainsKey("Plate") && result.ContainsKey("Scout"))
			{
				var body = result
					.Where(kv => !NonArmorRows.Contains(kv.Key) && !DerivedArmors.Contains(kv.Key))
					.Select(kv => kv.Value.Value)
					.ToList();
				if (body.Count > 0 && body.Min() == body.Max())
					result["Heroic"] = body[0];
				else
					result["Heroic"] = (int)Math.Round(
						result["Plate"].Value * result["Scout"].Value / HeroicDivisor,
						0, MidpointRounding.ToEven);
			}

			foreach (var (name, parents) in GeoDerived)
			{
				if (!result.ContainsKey(name) || parents.Any(p => !result.ContainsKey(p)))
					continue;
				var product = 1.0;
				foreach (var p in parents)
					product *= Math.Max(result[p].Value, 0);
				result[name] = (int)Math.Round(Math.Pow(product, 1.0 / parents.Length), 0, MidpointRounding.ToEven);
			}

			return result;
		}

		/// <summary>Effective main Versus table: verbatim when disabled, bell once when active.</summary>
		public static Dictionary<string, int?> VersusProfile(IReadOnlyDictionary<string, int?> versus, int heaviness)
		{
			if (heaviness < 0)
				return versus.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
			return BellTransform(versus, heaviness / 1000.0, true);
		}

		/// <summary>Effective percentage-half table, mirroring the C# branch exactly.</summary>
		public static Dictionary<string, int?> PercentageProfile(
			IReadOnlyDictionary<string, int?> versus,
			IReadOnlyDictionary<string, int?> percentageVersus,
			IReadOnlyDictionary<string, int?> light,
			IReadOnlyDictionary<string, int?> heavy,
			int heaviness)
		{
			ValidateAnchors(heaviness, light, percentageVersus, heavy);
			if (heaviness < 0)
				return (percentageVersus.Count > 0 ? percentageVersus : versus)
					.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
			var h = heaviness / 1000.0;
			if (light.Count > 0 || heavy.Count > 0)
				return BellTransform(InterpolatePercentageBands(light, percentageVersus, heavy, heaviness), h);
			if (percentageVersus.Count > 0)
				return BellTransform(percentageVersus, h);
			return BellTransform(versus, h);
		}
	}

	/// <summary>
	/// Shared runtime model for percentage-of-max-health weapon damage — mirror of
	/// <c>tools/balance/percentage_damage.py</c>. Authored fields and final damage
	/// remain Int32, intermediates use Int64/Int128 exactly like the C# warheads.
	/// </summary>
	public static class BotPercentageDamage
	{
		public const string PctFolded = "pct_folded";
		public const string PctStandalone = "pct_standalone";
		public const int FoldedScaleDenominator = 200_000;
		public const int FoldedRoundingBias = FoldedScaleDenominator / 2;
		public const int FoldedDefaultDenominator = 10_000;
		public const int StandaloneDefaultDenominator = 100;
		public const int DefaultPercentageSpread = 50;
		public const long SharedScaleDenominator = 200_000L * 2000;

		public static readonly IReadOnlySet<string> StandaloneTypes = new HashSet<string>(StringComparer.Ordinal)
		{
			"AreaDamagePercentage", "HealthPercentageDamage",
		};

		/// <summary>One runtime percentage-damage application on a weapon (percentage_damage.py dict shape).</summary>
		public sealed class PctApplication
		{
			public string Kind { get; init; }
			public string Tag { get; init; }
			public MiniYamlMirrorNode Node { get; init; }
			public int Damage { get; init; }
			public int Scale { get; init; }
			public int Denominator { get; init; }
			public int Heaviness { get; init; }
			public string Mode { get; init; }
			public double ContinuousUnits { get; init; }
			public int RuntimeUnits { get; init; }
			public double ContinuousHp { get; init; }
			public int RuntimeHp { get; init; }
			public double RoundingHp { get; init; }
			public IReadOnlyDictionary<string, int?> Versus { get; init; }
			public int? PercentageSpread { get; init; }
		}

		/// <summary>Return one armor table from a MiniYAML warhead node.</summary>
		public static Dictionary<string, int?> VersusTable(MiniYamlMirrorNode node, string field = "Versus")
		{
			var block = node.Child(field);
			var output = new Dictionary<string, int?>(StringComparer.Ordinal);
			if (block == null)
				return output;
			foreach (var child in block.Children)
				output[child.Key] = BotFormula.ParseInt32(child.Value, $"{field}.{child.Key}");
			return output;
		}

		internal static int VersusValue(IReadOnlyDictionary<string, int?> table, string key, int defaultValue)
		{
			return table != null && table.TryGetValue(key, out var v) ? v.Value : defaultValue;
		}

		/// <summary>Validate an engine Int32 result after wide intermediate arithmetic.</summary>
		public static int RuntimeInt32(long value)
		{
			if (value < int.MinValue || value > int.MaxValue)
				throw new OverflowException("percentage damage exceeds the runtime Int32 result");
			return (int)value;
		}

		public static int RuntimeInt32(Int128 value)
		{
			if (value < int.MinValue || value > int.MaxValue)
				throw new OverflowException("percentage damage exceeds the runtime Int32 result");
			return (int)value;
		}

		/// <summary>Continuous and engine-rounded percentage units for one folded hit.</summary>
		public static (double Continuous, int Rounded) FoldedUnits(int damage, int scale)
		{
			var continuous = (long)damage * scale / (double)FoldedScaleDenominator;
			var numerator = (long)damage * scale + FoldedRoundingBias;
			var rounded = RuntimeInt32(BotFormula.TruncDiv(numerator, FoldedScaleDenominator));
			return (continuous, rounded);
		}

		/// <summary>§12.0j growth curve: (4000 + h) / 5000 up to h=1000, (3000 + h) / 4000 above.</summary>
		public static (int Num, int Den) SharedGrowth(int heaviness)
		{
			return heaviness <= 1000 ? (4000 + heaviness, 5000) : (3000 + heaviness, 4000);
		}

		/// <summary>THE SHARED-PROFILE conversion: one rounded combined fraction with the growth factor inside.</summary>
		public static (double Continuous, int Rounded) SharedFoldedUnits(int damage, int scale, int heaviness)
		{
			var (growthNum, growthDen) = SharedGrowth(heaviness);
			var denominator = (Int128)200_000 * growthDen;
			var continuous = (double)((Int128)damage * scale * growthNum) / (double)denominator;
			var numerator = (Int128)damage * scale * growthNum + denominator / 2;
			var rounded = RuntimeInt32(numerator / denominator);
			return (continuous, rounded);
		}

		/// <summary>Neutral-armor HP damage with the current C# wide-intermediate path.</summary>
		public static int RuntimePercentageHp(double referenceHp, long units, long denominator)
		{
			var hp = (long)referenceHp;
			var afterUnits = RuntimeInt32(BotFormula.TruncDiv(hp * units, 100));
			return RuntimeInt32(BotFormula.TruncDiv((long)afterUnits * 100, denominator));
		}

		/// <summary>Return every positive runtime percentage application on a weapon.</summary>
		public static List<PctApplication> PercentageApplications(MiniYamlMirrorNode resolved, double referenceHp)
		{
			var output = new List<PctApplication>();
			foreach (var node in resolved.Children)
			{
				if (!node.Key.StartsWith("Warhead", StringComparison.Ordinal))
					continue;
				var tag = node.Key.Contains('@') ? node.Key.Split('@', 2)[1] : node.Key;

				if (node.Value == "AreaDamage")
				{
					// Heaviness / mode / anchor validation mirrors the C# RulesetLoaded
					// throws: they fire regardless of Damage or PercentageScale.
					var lightVersus = VersusTable(node, "PercentageVersusLight");
					var heavyVersus = VersusTable(node, "PercentageVersusHeavy");
					var pctVersus = VersusTable(node, "PercentageVersus");
					var (mode, heaviness) = BotHeaviness.HeavinessProfileConfig(
						node, lightVersus, pctVersus, heavyVersus);
					var damage = BotFormula.ParseInt32(node.Get("Damage"), $"{tag}.Damage");
					var scale = BotFormula.ParseInt32(node.Get("PercentageScale"), $"{tag}.PercentageScale", 0) ?? 0;
					BotHeaviness.ValidateSharedNumeric(mode, heaviness, VersusTable(node), damage, scale);
					var denominator = BotFormula.ParseInt32(
						node.Get("PercentageDenominator"), $"{tag}.PercentageDenominator", FoldedDefaultDenominator)
						?? FoldedDefaultDenominator;
					var percentageSpread = BotFormula.ParseInt32(
						node.Get("PercentageSpread"), $"{tag}.PercentageSpread", DefaultPercentageSpread)
						?? DefaultPercentageSpread;
					if (denominator <= 0)
						throw new InvalidDataException($"{tag}: AreaDamage PercentageDenominator must be positive");
					if (damage == null || damage <= 0 || scale <= 0)
						continue;

					double continuousUnits;
					int runtimeUnits;
					IReadOnlyDictionary<string, int?> effectivePct;
					if (mode == BotHeaviness.ModeShared)
					{
						(continuousUnits, runtimeUnits) = SharedFoldedUnits(damage.Value, scale, heaviness);
						effectivePct = BotHeaviness.SharedVersusProfile(VersusTable(node), heaviness);
					}
					else
					{
						(continuousUnits, runtimeUnits) = FoldedUnits(damage.Value, scale);
						effectivePct = BotHeaviness.PercentageProfile(
							VersusTable(node), pctVersus, lightVersus, heavyVersus, heaviness);
					}

					if (runtimeUnits <= 0 && continuousUnits <= 0)
						continue;
					var continuousHp = referenceHp * continuousUnits / denominator;
					var runtimeHp = RuntimePercentageHp(referenceHp, runtimeUnits, denominator);
					output.Add(new PctApplication
					{
						Kind = PctFolded,
						Tag = tag,
						Node = node,
						Damage = damage.Value,
						Scale = scale,
						Denominator = denominator,
						Heaviness = heaviness,
						Mode = mode,
						ContinuousUnits = continuousUnits,
						RuntimeUnits = runtimeUnits,
						ContinuousHp = continuousHp,
						RuntimeHp = runtimeHp,
						RoundingHp = runtimeHp - continuousHp,
						Versus = effectivePct,
						PercentageSpread = percentageSpread,
					});
					continue;
				}

				if (node.Value == null || !StandaloneTypes.Contains(node.Value))
					continue;

				int standaloneDenominator;
				if (node.Value == "AreaDamagePercentage")
				{
					// This class inherits the field even though its standalone hit does
					// not use it; FieldLoader still requires valid Int32 syntax.
					BotFormula.ParseInt32(node.Get("PercentageSpread"), $"{tag}.PercentageSpread", DefaultPercentageSpread);
					standaloneDenominator = BotFormula.ParseInt32(
						node.Get("PercentageDenominator"), $"{tag}.PercentageDenominator", StandaloneDefaultDenominator)
						?? StandaloneDefaultDenominator;
					if (standaloneDenominator <= 0)
						throw new InvalidDataException($"{tag}: AreaDamagePercentage PercentageDenominator must be positive");
					var standaloneScale = BotFormula.ParseInt32(
						node.Get("PercentageScale"), $"{tag}.PercentageScale", 0) ?? 0;
					if (standaloneScale > 0)
						throw new InvalidDataException($"{tag}: AreaDamagePercentage cannot also set PercentageScale");
				}
				else
					standaloneDenominator = StandaloneDefaultDenominator;

				int standaloneHeaviness;
				string standaloneMode;
				if (node.Value == "AreaDamagePercentage")
				{
					var light = VersusTable(node, "PercentageVersusLight");
					var heavy = VersusTable(node, "PercentageVersusHeavy");
					(_, standaloneHeaviness) = BotHeaviness.HeavinessProfileConfig(
						node, light, VersusTable(node, "PercentageVersus"), heavy, subclassTwin: true);
					standaloneMode = BotHeaviness.ModeLegacy;
				}
				else
				{
					standaloneHeaviness = BotHeaviness.Disabled;
					standaloneMode = BotHeaviness.ModeLegacy;
				}

				var standaloneDamage = BotFormula.ParseInt32(node.Get("Damage"), $"{tag}.Damage");
				if (standaloneDamage == null || standaloneDamage <= 0)
					continue;
				var hpEquiv = referenceHp * standaloneDamage.Value / standaloneDenominator;
				var runtimeHpStandalone = RuntimePercentageHp(referenceHp, standaloneDamage.Value, standaloneDenominator);
				output.Add(new PctApplication
				{
					Kind = PctStandalone,
					Tag = tag,
					Node = node,
					Damage = standaloneDamage.Value,
					Denominator = standaloneDenominator,
					Heaviness = standaloneHeaviness,
					Mode = standaloneMode,
					ContinuousUnits = standaloneDamage.Value,
					RuntimeUnits = standaloneDamage.Value,
					ContinuousHp = hpEquiv,
					RuntimeHp = runtimeHpStandalone,
					RoundingHp = runtimeHpStandalone - hpEquiv,
					Versus = node.Value == "AreaDamagePercentage"
						? BotHeaviness.VersusProfile(VersusTable(node), standaloneHeaviness)
						: VersusTable(node),
					PercentageSpread = null,
				});
			}

			return output;
		}

		static double FalloffAt(IReadOnlyList<double> falloff, IReadOnlyList<double> radii, double radius)
		{
			if (radius <= radii[0])
				return falloff[0];
			for (var index = 0; index < falloff.Count - 1; index++)
			{
				var inner = radii[index];
				var outer = radii[index + 1];
				if (radius <= outer)
				{
					if (outer <= inner)
						return falloff[index];
					var position = (radius - inner) / (outer - inner);
					return falloff[index] + (falloff[index + 1] - falloff[index]) * position;
				}
			}

			return 0.0;
		}

		/// <summary>Clip a main falloff curve at the folded percentage radius.</summary>
		public static (double[] Falloff, double[] Radii) ClipFalloff(
			IEnumerable<double> falloff, IEnumerable<double> radii, int? percentageSpread)
		{
			var spread = percentageSpread ?? DefaultPercentageSpread;
			var rs = radii.Select(v => (double)v).ToArray();
			if (rs.Length == 0)
				return (falloff.Select(v => (double)v).ToArray(), rs);
			var outer = (long)rs[rs.Length - 1];
			var cutoff = Math.Min(outer, Math.Max(0, BotFormula.FloorDiv(outer * spread, 100)));
			return ClipFalloffAtRadius(falloff, rs, cutoff);
		}

		/// <summary>Clip a falloff curve at one absolute runtime victim radius.</summary>
		public static (double[] Falloff, double[] Radii) ClipFalloffAtRadius(
			IEnumerable<double> falloff, IEnumerable<double> radii, double cutoff)
		{
			var fo = falloff.Select(v => (double)v).ToArray();
			var rs = radii.Select(v => (double)v).ToArray();
			if (fo.Length < 2 || fo.Length != rs.Length)
				return (fo, rs);
			cutoff = Math.Min(rs[rs.Length - 1], Math.Max(0.0, cutoff));
			if (cutoff >= rs[rs.Length - 1])
				return (fo, rs);
			if (cutoff <= rs[0])
				return (new[] { fo[0], 0.0 }, new[] { rs[0], rs[0] });
			var clippedFo = new List<double>();
			var clippedRs = new List<double>();
			for (var i = 0; i < fo.Length; i++)
			{
				if (rs[i] < cutoff)
				{
					clippedFo.Add(fo[i]);
					clippedRs.Add(rs[i]);
				}
				else
					break;
			}

			clippedFo.Add(FalloffAt(fo, rs, cutoff));
			clippedRs.Add(cutoff);
			return (clippedFo.ToArray(), clippedRs.ToArray());
		}
	}

	/// <summary>
	/// CPython <c>random.Random(int)</c> port: MT19937 seeded by
	/// <c>init_by_array</c> of the seed's absolute 32-bit words, producing the same
	/// <c>random()</c> 53-bit doubles. Needed bit-for-bit because the scatter-PDF
	/// histogram is a fixed-seed artifact the parity test pins.
	/// </summary>
	sealed class BotCpythonRandom
	{
		const int N = 624;
		const int M = 397;
		const uint MatrixA = 0x9908B0DFu;
		const uint UpperMask = 0x80000000u;
		const uint LowerMask = 0x7FFFFFFFu;

		readonly uint[] mt = new uint[N];
		int index;

		public BotCpythonRandom(int seed)
		{
			var magnitude = seed < 0 ? (ulong)(-(long)seed) : (ulong)seed;
			var words = new List<uint>();
			do
			{
				words.Add((uint)(magnitude & 0xFFFFFFFFu));
				magnitude >>= 32;
			}
			while (magnitude != 0);

			InitByArray(words.ToArray());
		}

		void InitGenRand(uint s)
		{
			mt[0] = s;
			for (var i = 1; i < N; i++)
				mt[i] = 1812433253u * (mt[i - 1] ^ (mt[i - 1] >> 30)) + (uint)i;
			index = N;
		}

		void InitByArray(uint[] key)
		{
			InitGenRand(19650218u);
			var i = 1;
			var j = 0;
			for (var k = Math.Max(N, key.Length); k > 0; k--)
			{
				mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1664525u)) + key[j] + (uint)j;
				i++;
				j++;
				if (i >= N)
				{
					mt[0] = mt[N - 1];
					i = 1;
				}

				if (j >= key.Length)
					j = 0;
			}

			for (var k = N - 1; k > 0; k--)
			{
				mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1566083941u)) - (uint)i;
				i++;
				if (i >= N)
				{
					mt[0] = mt[N - 1];
					i = 1;
				}
			}

			mt[0] = 0x80000000u;
		}

		uint GenRandUInt32()
		{
			if (index >= N)
			{
				int kk;
				for (kk = 0; kk < N - M; kk++)
				{
					var y = (mt[kk] & UpperMask) | (mt[kk + 1] & LowerMask);
					mt[kk] = mt[kk + M] ^ (y >> 1) ^ ((y & 1) != 0 ? MatrixA : 0u);
				}

				for (; kk < N - 1; kk++)
				{
					var y = (mt[kk] & UpperMask) | (mt[kk + 1] & LowerMask);
					mt[kk] = mt[kk + (M - N)] ^ (y >> 1) ^ ((y & 1) != 0 ? MatrixA : 0u);
				}

				var yLast = (mt[N - 1] & UpperMask) | (mt[0] & LowerMask);
				mt[N - 1] = mt[M - 1] ^ (yLast >> 1) ^ ((yLast & 1) != 0 ? MatrixA : 0u);
				index = 0;
			}

			var r = mt[index++];
			r ^= r >> 11;
			r ^= (r << 7) & 0x9D2C5680u;
			r ^= (r << 15) & 0xEFC60000u;
			r ^= r >> 18;
			return r;
		}

		/// <summary>CPython random(): (a * 2^26 + b) / 2^53 where a/b are 27/26 top bits.</summary>
		public double NextDouble()
		{
			var a = GenRandUInt32() >> 5;
			var b = GenRandUInt32() >> 6;
			return (a * 67108864.0 + b) * (1.0 / 9007199254740992.0);
		}

		/// <summary>CPython uniform(a, b): a + (b - a) * random().</summary>
		public double Uniform(double a, double b)
		{
			return a + (b - a) * NextDouble();
		}
	}

	/// <summary>One flat-damage warhead picked up by <see cref="BotEffectiveDamage.FlatDamageWarheads"/>.</summary>
	public sealed class BotFlatWarhead
	{
		public BotFlatWarhead(string tag, string type, int baseDamage, MiniYamlMirrorNode node)
		{
			Tag = tag;
			Type = type;
			Base = baseDamage;
			Node = node;
		}

		public string Tag { get; }
		public string Type { get; }
		public int Base { get; }
		public MiniYamlMirrorNode Node { get; }

		/// <summary>Tuple-shape access: (tag, warhead type, authored Damage, node).</summary>
		public void Deconstruct(out string tag, out string wtype, out int baseDamage, out MiniYamlMirrorNode node)
		{
			tag = Tag;
			wtype = Type;
			baseDamage = Base;
			node = Node;
		}
	}

	/// <summary>The (effective, base_total, footprint, avg_reliability, sigma) tuple of effective_damage().</summary>
	public sealed class BotEdResult
	{
		public BotEdResult(double effective, double baseTotal, double footprintTotal, double avgReliability, double sigma)
		{
			Effective = effective;
			BaseTotal = baseTotal;
			FootprintTotal = footprintTotal;
			AvgReliability = avgReliability;
			Sigma = sigma;
		}

		public double Effective { get; }
		public double BaseTotal { get; }
		public double FootprintTotal { get; }
		public double AvgReliability { get; }
		public double Sigma { get; }
	}

	/// <summary>
	/// READ-ONLY area-integrated damage metric per weapon — mirror of
	/// <c>tools/balance/effective_damage.py</c>, including the fixed-seed scatter
	/// PDF (CPython MT19937 port verified bit-for-bit against the Python table).
	/// </summary>
	public static class BotEffectiveDamage
	{
		public const double Cell = 1024.0;
		public const double SwarmW = 0.25;
		public const double Lead = 0.20;
		public const double TargetSpeed = 100;
		public const double SpeedCap = 10000;
		public const int DefaultAreaSpread = 43;
		public const int PointTargetRadius = 100;
		public const int BulletDefaultSpeed = 17;
		public const int AreaBeamDefaultDuration = 10;
		public const int AreaBeamDefaultDamageInterval = 3;
		public const int LaserZapDefaultDuration = 10;
		public const int LaserZapDefaultDamageDuration = 1;
		public const int LaserZapDefaultDamageInterval = 1;
		public const int SpriteAthenaDefaultExplosionInterval = 3;
		public const int SpriteAthenaDefaultPierceTicks = 0;
		public const int SpriteAthenaDefaultStayTicks = 8;
		public const int LightningZapDefaultDuration = 3;
		public const int LightningZapDefaultDamageDuration = 1;
		public const int ScatterBinCount = 256;
		public const int ScatterSamples = 400000;
		public const int ScatterSeed = 20260811;

		public static readonly int[] DefaultAreaFalloff = { 100, 37, 14, 5, 0 };

		public static readonly IReadOnlySet<string> InstantProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"InstantHit", "LaserZap", "Railgun", "InstantHitLine", "InstantHitAS",
			"SupportPowerInstantExplode", "InstantExplode", "LightningZap", "RadBeam",
			"KKNDLaser", "LaserZapCA", "InstantHitWithFakeBullets",
		};

		public static readonly IReadOnlySet<string> InstantScatterProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"InstantHit", "InstantHitWithFakeBullets", "Railgun", "InstantHitLine", "InstantHitAS",
		};

		public static readonly IReadOnlySet<string> TrackedZapProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"LaserZap", "LaserZapCA",
		};

		public static readonly IReadOnlySet<string> UnmodeledTrajectoryProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"GravityBomb", "NukeLaunch",
		};

		public static readonly IReadOnlyDictionary<string, int> MovingProjectileDefaultSpeeds =
			new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["Bullet"] = BulletDefaultSpeed,
				["ScaledBullet"] = BulletDefaultSpeed,
				["Missile"] = 384,
				["AreaBeam"] = 128,
				["SpriteAthenaLaser"] = 90,
				["LinearPulse"] = 6 * 1024,
			};

		public static readonly IReadOnlySet<string> InaccuracyProjectiles = new HashSet<string>(
			InstantScatterProjectiles.Concat(TrackedZapProjectiles)
				.Concat(new[] { "Bullet", "ScaledBullet", "Missile", "AreaBeam", "LinearPulse" }),
			StringComparer.Ordinal);

		public static readonly IReadOnlySet<string> CenterTargetActorProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"InstantHit", "InstantHitWithFakeBullets",
		};

		public static readonly IReadOnlySet<string> LineWidthActorProjectiles = new HashSet<string>(StringComparer.Ordinal)
		{
			"SpriteRailgun", "SmokeParticleRailgun",
		};

		public static readonly IReadOnlySet<string> LinearPulseActorImpacts = new HashSet<string>(StringComparer.Ordinal)
		{
			"rectangle", "cone", "trapezoid",
		};

		// The 256-bin radial density of the engine's two-axis triangular scatter —
		// built once, verified against the Python table the fixtures pin.
		static readonly double[] ScatterDensities = BuildScatterPdf();

		/// <summary>The 256 normalised radial-density bins of the engine's scatter.</summary>
		public static IReadOnlyList<double> ScatterPdf => ScatterDensities;

		/// <summary>Width of one scatter-PDF bin: sqrt(2) / 256.</summary>
		public static double ScatterBinWidth => Math.Sqrt(2.0) / ScatterBinCount;

		static bool Truthy(string raw, string field = "boolean")
		{
			return BotFormula.ParseBool(raw, field, false) ?? false;
		}

		static double[] BuildScatterPdf()
		{
			var rng = new BotCpythonRandom(ScatterSeed);
			var hi = Math.Sqrt(2.0);
			var hist = new double[ScatterBinCount];
			for (var i = 0; i < ScatterSamples; i++)
			{
				var x = (rng.Uniform(-1, 1) + rng.Uniform(-1, 1)) / 2;
				var y = (rng.Uniform(-1, 1) + rng.Uniform(-1, 1)) / 2;
				var b = (int)(Math.Sqrt(x * x + y * y) / hi * ScatterBinCount);
				if (b < ScatterBinCount)
					hist[b] += 1.0;
			}

			var total = hist.Sum();
			var width = hi / ScatterBinCount;
			for (var i = 0; i < ScatterBinCount; i++)
				hist[i] = hist[i] / total / width;
			return hist;
		}

		internal static double ScatterDensityAt(double t)
		{
			var hi = Math.Sqrt(2.0);
			if (t < 0 || t >= hi)
				return 0.0;
			return ScatterDensities[(int)(t / hi * ScatterBinCount)];
		}

		/// <summary>Whether the projectile invokes DamageWarhead's direct-Actor path.</summary>
		public static bool DirectActorImpact(MiniYamlMirrorNode resolved)
		{
			var projectile = resolved.Child("Projectile");
			var projectileType = projectile?.Value;
			if (projectileType == "AreaBeam")
				return true;
			if (projectileType == "Railgun")
				return Truthy(resolved.Get("Projectile", "DamageActorsInLine"), "Railgun.DamageActorsInLine");
			if (projectileType != null && LineWidthActorProjectiles.Contains(projectileType))
			{
				var rawWidth = resolved.Get("Projectile", "LineWidth");
				return rawWidth != null && BotFormula.ParseWdist(rawWidth) > 0;
			}

			if (projectileType == "LinearPulse")
			{
				var impactType = (resolved.Get("Projectile", "ImpactType") ?? "StandardImpact").Trim().ToLowerInvariant();
				return LinearPulseActorImpacts.Contains(impactType);
			}

			return projectileType != null && CenterTargetActorProjectiles.Contains(projectileType) &&
				Truthy(resolved.Get("TargetActorCenter"), "Weapon.TargetActorCenter");
		}

		/// <summary>Expected warhead applications made by one weapon fire.</summary>
		public static double ProjectileImpactMultiplier(MiniYamlMirrorNode resolved)
		{
			var projectile = resolved.Child("Projectile");
			var projectileType = projectile?.Value;
			if (projectileType == "SpriteAthenaLaser")
			{
				var nominal = ProjectileNominalImpactCount(resolved);
				return nominal == 0 ? 0.0 : 1.0;
			}

			if (projectileType != null && TrackedZapProjectiles.Contains(projectileType))
			{
				var duration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "Duration"), $"{projectileType}.Duration", LaserZapDefaultDuration)
					?? LaserZapDefaultDuration;
				var damageDuration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "DamageDuration"), $"{projectileType}.DamageDuration", LaserZapDefaultDamageDuration)
					?? LaserZapDefaultDamageDuration;
				var interval = BotFormula.ParseInt32(
					resolved.Get("Projectile", "DamageInterval"), $"{projectileType}.DamageInterval", LaserZapDefaultDamageInterval)
					?? LaserZapDefaultDamageInterval;
				var activeTicks = Math.Max(Math.Min(damageDuration, Math.Max(duration, 1)), 0);
				if (interval <= 0)
					return activeTicks;
				return (double)((activeTicks + interval - 1) / interval);
			}

			if (projectileType == "LightningZap")
			{
				var duration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "Duration"), "LightningZap.Duration", LightningZapDefaultDuration)
					?? LightningZapDefaultDuration;
				var damageDuration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "DamageDuration"), "LightningZap.DamageDuration", LightningZapDefaultDamageDuration)
					?? LightningZapDefaultDamageDuration;
				return Math.Max(Math.Min(damageDuration, duration), 0);
			}

			if (projectileType != "AreaBeam")
				return 1.0;

			var beamDuration = BotFormula.ParseInt32(
				resolved.Get("Projectile", "Duration"), "AreaBeam.Duration", AreaBeamDefaultDuration)
				?? AreaBeamDefaultDuration;
			var beamInterval = BotFormula.ParseInt32(
				resolved.Get("Projectile", "DamageInterval"), "AreaBeam.DamageInterval", AreaBeamDefaultDamageInterval)
				?? AreaBeamDefaultDamageInterval;
			if (beamDuration <= 0 || beamInterval <= 0)
				throw new InvalidDataException(
					$"AreaBeam cadence must be positive (Duration={beamDuration}, DamageInterval={beamInterval})");
			return beamDuration / (double)beamInterval;
		}

		/// <summary>Max-range total impacts for corridor projectiles not reducible to one-target K.</summary>
		public static int? ProjectileNominalImpactCount(MiniYamlMirrorNode resolved)
		{
			var projectile = resolved.Child("Projectile");
			if (projectile?.Value != "SpriteAthenaLaser")
				return null;
			var rangeRaw = resolved.Get("Range");
			var weaponRange = rangeRaw != null ? (long)BotFormula.ParseWdist(rangeRaw) : 0;
			var speedRaw = resolved.Get("Projectile", "Speed");
			var speed = speedRaw != null ? (long)BotFormula.ParseWdist(speedRaw) : MovingProjectileDefaultSpeeds["SpriteAthenaLaser"];
			var interval = Math.Max(BotFormula.ParseInt32(
				resolved.Get("Projectile", "ExplosionInterval"), "SpriteAthenaLaser.ExplosionInterval",
				SpriteAthenaDefaultExplosionInterval) ?? SpriteAthenaDefaultExplosionInterval, 1);
			var pierce = BotFormula.ParseInt32(
				resolved.Get("Projectile", "PierceTicks"), "SpriteAthenaLaser.PierceTicks", SpriteAthenaDefaultPierceTicks)
				?? SpriteAthenaDefaultPierceTicks;
			var stay = BotFormula.ParseInt32(
				resolved.Get("Projectile", "StayTicks"), "SpriteAthenaLaser.StayTicks", SpriteAthenaDefaultStayTicks)
				?? SpriteAthenaDefaultStayTicks;
			var flight = Math.Max(weaponRange / Math.Max(speed, 1), 1);
			var finalTick = Math.Max(flight + pierce + stay + 1, 1);
			return (int)(finalTick / interval);
		}

		/// <summary>Machine-readable gaps that make a derived weapon value provisional.</summary>
		public static List<string> ModelLimitations(MiniYamlMirrorNode resolved)
		{
			var projectile = resolved.Child("Projectile");
			var projectileType = projectile?.Value;
			var limitations = new List<string>();
			if (resolved.Children.Any(c => c.Key.StartsWith("Warhead@", StringComparison.Ordinal) && c.Value == "FireShrapnel"))
				limitations.Add("unmodeled_secondary_payload:FireShrapnel");
			if (projectileType == "AreaBeam")
				limitations.Add("unmodeled_projectile_geometry:AreaBeam");
			else if (projectileType == "Railgun" &&
				Truthy(resolved.Get("Projectile", "DamageActorsInLine"), "Railgun.DamageActorsInLine"))
				limitations.Add("unmodeled_projectile_geometry:Railgun.line");
			else if (projectileType != null && LineWidthActorProjectiles.Contains(projectileType))
			{
				var rawWidth = resolved.Get("Projectile", "LineWidth");
				if (rawWidth != null && BotFormula.ParseWdist(rawWidth) > 0)
					limitations.Add($"unmodeled_projectile_geometry:{projectileType}.line");
			}
			else if (projectileType == "LinearPulse")
			{
				var impactType = (resolved.Get("Projectile", "ImpactType") ?? "StandardImpact").Trim();
				if (LinearPulseActorImpacts.Contains(impactType.ToLowerInvariant()))
					limitations.Add($"unmodeled_projectile_geometry:LinearPulse.{impactType}");
			}
			else if (projectileType == "SpriteAthenaLaser")
			{
				limitations.Add("unmodeled_projectile_cadence:SpriteAthenaLaser");
				limitations.Add("unmodeled_projectile_geometry:SpriteAthenaLaser");
			}
			else if (projectileType != null && UnmodeledTrajectoryProjectiles.Contains(projectileType))
				limitations.Add($"unmodeled_projectile_trajectory:{projectileType}");

			if (projectileType == "Missile")
			{
				var probability = BotFormula.ParseInt32(
					resolved.Get("Projectile", "LockOnProbability"), "Missile.LockOnProbability", 100) ?? 100;
				if (probability >= 0 && probability < 99)
					limitations.Add("unmodeled_projectile_lock_on:Missile");
			}

			if (projectileType != null && TrackedZapProjectiles.Contains(projectileType))
			{
				var duration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "Duration"), $"{projectileType}.Duration", LaserZapDefaultDuration)
					?? LaserZapDefaultDuration;
				var damageDuration = BotFormula.ParseInt32(
					resolved.Get("Projectile", "DamageDuration"), $"{projectileType}.DamageDuration", LaserZapDefaultDamageDuration)
					?? LaserZapDefaultDamageDuration;
				var hitAnim = resolved.Get("Projectile", "HitAnim");
				if (!string.IsNullOrWhiteSpace(hitAnim) && damageDuration > Math.Max(duration, 1))
					limitations.Add($"unmodeled_projectile_hitanim_lifetime:{projectileType}");
			}

			if (projectileType == "AreaBeam")
			{
				var multiplier = ProjectileImpactMultiplier(resolved);
				if (multiplier != Math.Floor(multiplier))
					limitations.Add("phase_averaged_projectile_cadence:AreaBeam");
			}

			var speedRaw = resolved.Get("Projectile", "Speed");
			if (projectileType is "Bullet" or "ScaledBullet" && speedRaw != null && speedRaw.Contains(','))
				limitations.Add($"approximated_projectile_speed_distribution:{projectileType}");
			return limitations;
		}

		/// <summary>Parse one comma list of Int32 values, skipping empty entries.</summary>
		public static int[] ParseInts(string s)
		{
			return (s ?? "None").Split(',')
				.Where(x => x.Trim().Length != 0)
				.Select(x => BotFormula.ParseInt32(x.Trim(), "integer list value").Value)
				.ToArray();
		}

		/// <summary>C# integer division: truncate toward zero, including signed values.</summary>
		public static long CSharpDiv(long numerator, long denominator)
		{
			return BotFormula.TruncDiv(numerator, denominator);
		}

		static bool FieldPresent(MiniYamlMirrorNode node, string key)
		{
			return node.Child(key) != null;
		}

		/// <summary>
		/// (falloff percents, radii in WDist, live) — mirrors AreaDamageWarhead /
		/// SpreadDamageWarhead exactly, including the single-Range footgun.
		/// </summary>
		public static (int[] Falloff, int[] Radii, bool Live) FalloffAndRadii(MiniYamlMirrorNode node, int? spread = null)
		{
			var falloffPresent = FieldPresent(node, "Falloff");
			var foRaw = node.Get("Falloff");
			var fo = falloffPresent ? ParseInts(foRaw) : DefaultAreaFalloff;
			if (fo.Length == 0)
				throw new InvalidDataException("area warhead Falloff cannot be empty");
			int spreadValue;
			if (spread.HasValue)
				spreadValue = spread.Value;
			else
			{
				var spreadRaw = node.Get("Spread");
				spreadValue = spreadRaw == null ? DefaultAreaSpread : BotFormula.ParseWdist(spreadRaw);
			}

			var rangePresent = FieldPresent(node, "Range");
			int[] radii;
			if (rangePresent)
			{
				var rng = node.Get("Range");
				radii = (rng ?? "").Split(',')
					.Where(x => x.Trim().Length != 0)
					.Select(x => BotFormula.ParseWdist(x.Trim()))
					.ToArray();
				if (radii.Length == 0)
					throw new InvalidDataException("area warhead Range cannot be empty");
				if (radii.Length != 1 && radii.Length != fo.Length)
					throw new InvalidDataException("area warhead Range length must be one or equal Falloff length");
				for (var i = 0; i + 1 < radii.Length; i++)
					if (radii[i] > radii[i + 1])
						throw new InvalidDataException("area warhead Range values must be nondecreasing");
			}
			else
				radii = Enumerable.Range(0, fo.Length).Select(i => i * spreadValue).ToArray();

			if (node.Value == "AreaDamage" || node.Value == "AreaDamagePercentage")
			{
				var heaviness = BotHeaviness.HeavinessOf(node);
				if (heaviness >= 0)
				{
					spreadValue = BotHeaviness.ScaleLength(spreadValue, heaviness);
					if (rangePresent)
					{
						var scaled = radii.Select(r => BotHeaviness.ScaleLength(r, heaviness)).ToArray();
						if (scaled.Length >= 2 && radii[0] < radii[1] && scaled[0] == scaled[1] && scaled[0] > 0)
							throw new BotHeavinessException(
								$"Scaled Range front collapsed to a positive duplicate " +
								$"(authored {radii[0]} / {radii[1]}, scaled {scaled[0]} / {scaled[1]}).");
						radii = scaled;
					}
					else
						radii = Enumerable.Range(0, fo.Length).Select(i => i * spreadValue).ToArray();
				}
			}

			return (fo, radii, fo.Length >= 2 && radii.Length >= 2);
		}

		/// <summary>Area/SpreadDamage GetDamageFalloff, including inward extrapolation.</summary>
		public static long RuntimeFalloff(IReadOnlyList<int> fo, IReadOnlyList<int> radii, double distance)
		{
			if (fo.Count < 2 || radii.Count < 2)
				return 0;
			var d = Math.Max((long)distance, 0);
			var inner = (long)radii[0];
			for (var i = 1; i < radii.Count; i++)
			{
				var outer = (long)radii[i];
				if (outer > d)
					return fo[i - 1] + CSharpDiv((fo[i] - (long)fo[i - 1]) * (d - inner), outer - inner);
				inner = outer;
			}

			return 0;
		}

		static (double Intercept, double Slope) FalloffLine(IReadOnlyList<int> fo, IReadOnlyList<int> radii, double distance)
		{
			if (fo.Count < 2 || radii.Count < 2)
				return (0.0, 0.0);
			var inner = (double)radii[0];
			for (var i = 1; i < radii.Count; i++)
			{
				var outer = (double)radii[i];
				if (outer > distance)
				{
					var span = outer - inner;
					if (span == 0)
						throw new DivideByZeroException("runtime geometry divides by a zero-width range step");
					var slope = (fo[i] - (double)fo[i - 1]) / span / 100.0;
					var intercept = fo[i - 1] / 100.0 - slope * inner;
					return (intercept, slope);
				}

				inner = outer;
			}

			return (0.0, 0.0);
		}

		/// <summary>2*pi*INT F(r)r dr over runtime falloff, starting at impact distance zero.</summary>
		public static double FootprintCells2(IReadOnlyList<int> fo, IReadOnlyList<int> radii, long? cutoff = null)
		{
			if (fo.Count < 2 || radii.Count < 2)
				return 0.0;
			var limit = cutoff.HasValue ? Math.Min(radii[radii.Count - 1], cutoff.Value) : (long)radii[radii.Count - 1];
			if (limit <= 0)
				return 0.0;
			var boundaries = new SortedSet<double> { 0.0, limit };
			for (var i = 1; i < radii.Count; i++)
				if (radii[i] > 0 && radii[i] < limit)
					boundaries.Add(radii[i]);
			var list = boundaries.ToList();
			var total = 0.0;
			for (var i = 0; i + 1 < list.Count; i++)
			{
				var a = list[i];
				var b = list[i + 1];
				if (b <= a)
					continue;
				var (intercept, slope) = FalloffLine(fo, radii, (a + b) / 2.0);
				total += intercept * (b * b - a * a) / 2.0 + slope * (b * b * b - a * a * a) / 3.0;
			}

			return 2 * Math.PI * total / (Cell * Cell);
		}

		/// <summary>Uniform positional catch radius for TargetDamage and its subclasses.</summary>
		public static (int Radius, bool Live) TargetDamageRadius(MiniYamlMirrorNode node)
		{
			var spreadRaw = node.Get("Spread");
			var spread = spreadRaw == null ? 0 : BotFormula.ParseWdist(spreadRaw);
			return (spread, spread > 0);
		}

		public static double UniformFootprintCells2(long radius)
		{
			return Math.PI * Math.Max(radius, 0) * Math.Max(radius, 0) / (Cell * Cell);
		}

		/// <summary>AreaDamage's validated C# per-tick modifiers.</summary>
		public static int[] AreaTickModifiers(MiniYamlMirrorNode node)
		{
			var ticks = BotFormula.ParseInt32(node.Get("Ticks"), "AreaDamage.Ticks", 1) ?? 1;
			var weightsPresent = FieldPresent(node, "TickDamage");
			var weights = weightsPresent ? ParseInts(node.Get("TickDamage") ?? "") : null;
			if (weights != null && weights.Length != ticks)
				throw new InvalidDataException("TickDamage length must equal Ticks");
			if (ticks <= 0)
				return Array.Empty<int>();
			var total = weights?.Sum(w => (long)w) ?? 0;
			if (weights != null && total > 0)
				return weights.Select(w => (int)CSharpDiv(100L * w, total)).ToArray();
			return Enumerable.Repeat((int)CSharpDiv(100, ticks), ticks).ToArray();
		}

		/// <summary>Mirror ruleset-time area geometry validation for every resolved node.</summary>
		public static void ValidateDamageWarheads(MiniYamlMirrorNode resolved)
		{
			foreach (var node in resolved.Children)
			{
				if (!node.Key.StartsWith("Warhead", StringComparison.Ordinal))
					continue;
				if (node.Value != "SpreadDamage" && node.Value != "AreaDamage" && node.Value != "AreaDamagePercentage")
					continue;
				FalloffAndRadii(node);
				if (node.Value == "AreaDamage" || node.Value == "AreaDamagePercentage")
				{
					AreaTickModifiers(node);
					var light = BotPercentageDamage.VersusTable(node, "PercentageVersusLight");
					var medium = BotPercentageDamage.VersusTable(node, "PercentageVersus");
					var heavy = BotPercentageDamage.VersusTable(node, "PercentageVersusHeavy");
					var (mode, heaviness) = BotHeaviness.HeavinessProfileConfig(
						node, light, medium, heavy, subclassTwin: node.Value == "AreaDamagePercentage");
					var scale = BotFormula.ParseInt32(node.Get("PercentageScale"), "Warhead.PercentageScale", 0) ?? 0;
					BotHeaviness.ValidateSharedNumeric(
						mode, heaviness, medium,
						BotFormula.ParseInt32(node.Get("Damage"), "Warhead.Damage"),
						scale != 0 ? scale : null);
				}
			}
		}

		/// <summary>(weight, reliability, footprint) for every runtime AreaDamage tick.</summary>
		public static List<(double Weight, double Reliability, double Footprint)> AreaGeometrySamples(
			MiniYamlMirrorNode node, IReadOnlyList<int> fo, IReadOnlyList<int> radii, double sigma, long radiusScale = 100)
		{
			var modifiers = AreaTickModifiers(node);
			var ticks = modifiers.Length;
			var samples = new List<(double, double, double)>();
			if (ticks == 0)
				return samples;
			var finalOuter = (long)radii[radii.Count - 1];
			var maxRadiusRaw = node.Get("MaxRadius");
			var minRadiusRaw = node.Get("MinRadius");
			var authoredMaxRadius = maxRadiusRaw == null ? 0 : BotFormula.ParseWdist(maxRadiusRaw);
			var authoredMinRadius = minRadiusRaw == null ? 0 : BotFormula.ParseWdist(minRadiusRaw);
			long maxRadius = authoredMaxRadius;
			long minRadius = authoredMinRadius;
			if (node.Value == "AreaDamage" || node.Value == "AreaDamagePercentage")
			{
				var heaviness = BotHeaviness.HeavinessOf(node);
				if (heaviness >= 0)
				{
					minRadius = BotHeaviness.ScaleLength((int)minRadius, heaviness);
					maxRadius = BotHeaviness.ScaleLength((int)maxRadius, heaviness);
				}
			}

			for (var tick = 0; tick < modifiers.Length; tick++)
			{
				var outer = finalOuter;
				if (authoredMaxRadius > 0 && ticks > 1)
					outer = minRadius + CSharpDiv((maxRadius - minRadius) * (tick + 1), ticks);
				var scaledOuter = CSharpDiv(outer * radiusScale, 100);
				var cutoff = Math.Min(outer, scaledOuter);
				samples.Add((modifiers[tick] / 100.0, Reliability(fo, radii, sigma, cutoff), FootprintCells2(fo, radii, cutoff)));
			}

			return samples;
		}

		/// <summary>
		/// Expected falloff at the impact point over the engine's scatter (POINT target):
		/// E[F(R)] where R is the miss distance; cutoff models an AreaDamage radius gate.
		/// </summary>
		public static double Reliability(IReadOnlyList<int> fo, IReadOnlyList<int> radii, double sigma, long? cutoff = null)
		{
			if (sigma <= 0)
			{
				if (cutoff.HasValue && cutoff.Value < 0)
					return 0.0;
				return RuntimeFalloff(fo, radii, 0) / 100.0;
			}

			const int n = 400;
			var hi = Math.Sqrt(2.0);
			var acc = 0.0;
			var weight = 0.0;
			var step = hi / n;
			for (var i = 0; i < n; i++)
			{
				var t = (i + 0.5) * step;
				var w = ScatterDensityAt(t) * step;
				var distance = t * sigma;
				if (!cutoff.HasValue || distance <= cutoff.Value)
					acc += RuntimeFalloff(fo, radii, distance) / 100.0 * w;
				weight += w;
			}

			return weight != 0 ? acc / weight : 1.0;
		}

		/// <summary>Probability a point impact lands inside TargetDamage's closed disc.</summary>
		public static double UniformReliability(long radius, double sigma)
		{
			if (radius <= 0)
				return 0.0;
			if (sigma <= 0)
				return 1.0;
			const int n = 400;
			var hi = Math.Sqrt(2.0);
			var step = hi / n;
			var caught = 0.0;
			var weight = 0.0;
			for (var i = 0; i < n; i++)
			{
				var t = (i + 0.5) * step;
				var w = ScatterDensityAt(t) * step;
				if (t * sigma <= radius)
					caught += w;
				weight += w;
			}

			return weight != 0 ? caught / weight : 0.0;
		}

		/// <summary>A warhead's Damage as an int, or null when the field is not numeric.</summary>
		public static int? DamageValue(string raw)
		{
			try
			{
				return BotFormula.ParseInt32(raw, "Damage");
			}
			catch (InvalidDataException)
			{
				if (BotFormula.PyFloat(raw) == null)
					return null;
				throw;
			}
		}

		/// <summary>raw as a float, or null when it is not numeric.</summary>
		public static double? Number(string raw)
		{
			return BotFormula.PyFloat(raw);
		}

		/// <summary>Main + extra-damage flat warheads (excludes %-twins, FriendlyFire, EMP, effects).</summary>
		public static List<BotFlatWarhead> FlatDamageWarheads(MiniYamlMirrorNode resolved)
		{
			var output = new List<BotFlatWarhead>();
			foreach (var c in resolved.Children)
			{
				if (!c.Key.StartsWith("Warhead@", StringComparison.Ordinal))
					continue;
				var tag = c.Key.Split('@', 2)[1];
				if (tag.Contains("FriendlyFire", StringComparison.Ordinal))
					continue;
				if (c.Value != "AreaDamage" && c.Value != "SpreadDamage" && c.Value != "TargetDamage")
					continue;
				var baseDamage = DamageValue(c.Get("Damage"));
				if (baseDamage == null || baseDamage <= 0)
					continue;
				output.Add(new BotFlatWarhead(tag, c.Value, baseDamage.Value, c));
			}

			return output;
		}

		/// <summary>(zero travel drift, sigma), with runtime projectile defaults and scatter.</summary>
		public static (bool Instant, double Sigma) WeaponReliabilityCtx(MiniYamlMirrorNode resolved)
		{
			var proj = resolved.Child("Projectile");
			var ptype = proj?.Value;
			var rngRaw = resolved.Get("Range");
			var rng = rngRaw != null ? (long)BotFormula.ParseWdist(rngRaw) : 0;
			if (ptype != null && UnmodeledTrajectoryProjectiles.Contains(ptype))
				return (false, 0.0);

			var speedRaw = resolved.Get("Projectile", "Speed");
			var inaccRaw = ptype != null && InaccuracyProjectiles.Contains(ptype)
				? resolved.Get("Projectile", "Inaccuracy")
				: null;

			double InaccuracyAtRange(double baseInacc)
			{
				var kind = (resolved.Get("Projectile", "InaccuracyType") ?? "Maximum").Trim().ToLowerInvariant();
				return kind == "percellincrement" ? CSharpDiv((long)baseInacc * rng, 1024) : baseInacc;
			}

			var inacc = inaccRaw != null ? (double)BotFormula.ParseWdist(inaccRaw) : 0.0;
			if (ptype != null && InstantProjectiles.Contains(ptype))
			{
				if (CenterTargetActorProjectiles.Contains(ptype) && DirectActorImpact(resolved))
					inacc = 0;
				else if (TrackedZapProjectiles.Contains(ptype))
				{
					var rawTrack = resolved.Get("Projectile", "TrackTarget");
					if (BotFormula.ParseBool(rawTrack, $"{ptype}.TrackTarget", true) ?? true)
						inacc = 0;
				}
				else if (!InstantScatterProjectiles.Contains(ptype))
					inacc = 0;

				return (true, InaccuracyAtRange(inacc));
			}

			if (ptype == "AreaBeam")
			{
				var rawTrack = resolved.Get("Projectile", "TrackTarget");
				if (BotFormula.ParseBool(rawTrack, "AreaBeam.TrackTarget", false) ?? false)
					return (false, 0.0);
			}

			if (ptype == "Missile")
			{
				var probability = BotFormula.ParseInt32(
					resolved.Get("Projectile", "LockOnProbability"), "Missile.LockOnProbability", 100) ?? 100;
				var lockRaw = resolved.Get("Projectile", "LockOnInaccuracy");
				var lockInacc = lockRaw == null ? -1 : BotFormula.ParseWdist(lockRaw);
				if (probability >= 99 && lockInacc >= 0)
					inacc = lockInacc;
			}

			double speed;
			if (ptype != null && MovingProjectileDefaultSpeeds.TryGetValue(ptype, out var defaultSpeed))
			{
				var scaledSentinel = false;
				if (speedRaw != null && speedRaw.Contains(',') && (ptype == "Bullet" || ptype == "ScaledBullet"))
				{
					var values = speedRaw.Split(',')
						.Where(x => x.Trim().Length != 0)
						.Select(x => BotFormula.ParseWdist(x.Trim()))
						.ToArray();
					if (values.Length >= 2)
					{
						if (values[1] < values[0])
							throw new InvalidDataException("Bullet Speed range must be nondecreasing");
						speed = values[0] == values[1] ? values[0] : (values[0] + values[1] - 1) / 2.0;
					}
					else
						speed = values[0];
				}
				else
				{
					speed = speedRaw != null ? BotFormula.ParseWdist(speedRaw) : defaultSpeed;
					scaledSentinel = ptype == "ScaledBullet" && speed == BulletDefaultSpeed;
				}

				if (ptype == "ScaledBullet" && rng > 0)
				{
					var speedPct = BotFormula.ParseInt32(
						resolved.Get("Projectile", "ProjectileSpeedPercentage"),
						"ScaledBullet.ProjectileSpeedPercentage", 0) ?? 0;
					var inaccPct = BotFormula.ParseInt32(
						resolved.Get("Projectile", "InaccuracyPercentage"),
						"ScaledBullet.InaccuracyPercentage", 0) ?? 0;
					if (speedPct > 0 && scaledSentinel)
						speed = CSharpDiv(rng * speedPct, 100);
					if (inaccPct > 0 && inacc == 0)
						inacc = CSharpDiv(rng * inaccPct, 100);
				}
			}
			else
			{
				if (speedRaw == null)
					return (true, InaccuracyAtRange(inacc));
				speed = BotFormula.ParseWdist(speedRaw);
			}

			inacc = InaccuracyAtRange(inacc);
			var effSpeed = Math.Max(Math.Min(speed, SpeedCap), 1.0);
			var drift = rng != 0 ? Lead * TargetSpeed * rng / effSpeed : 0.0;
			return (false, inacc + drift);
		}

		/// <summary>(effective, base_total, footprint_total, avg_reliability, sigma) or null.</summary>
		public static BotEdResult Evaluate(MiniYamlMirrorNode resolved)
		{
			ValidateDamageWarheads(resolved);
			BotPercentageDamage.PercentageApplications(resolved, 1);   // validation pass over percentage fields
			var whs = FlatDamageWarheads(resolved);
			if (whs.Count == 0)
				return null;
			var (_, sigma) = WeaponReliabilityCtx(resolved);
			var isDirectActor = DirectActorImpact(resolved);
			var eff = 0.0;
			var baseTotal = 0.0;
			var footTotal = 0.0;
			var relWeighted = 0.0;
			foreach (var w in whs)
			{
				int[] fo = null;
				int[] radii = null;
				var live = true;
				if (w.Type == "AreaDamage" || w.Type == "SpreadDamage")
				{
					(fo, radii, live) = FalloffAndRadii(w.Node);
					if (w.Type == "AreaDamage")
						AreaTickModifiers(w.Node);
				}

				if (isDirectActor)
				{
					var directRel = Reliability(DirectFalloff, DirectRadii, sigma);
					eff += w.Base * directRel;
					baseTotal += w.Base;
					relWeighted += directRel * w.Base;
					continue;
				}

				long radius = 0;
				if (w.Type == "TargetDamage")
					(radius, live) = TargetDamageRadius(w.Node);
				if (!live)
				{
					baseTotal += w.Base;
					continue;
				}

				double fp;
				double rel;
				if (w.Type == "TargetDamage")
				{
					fp = UniformFootprintCells2(radius);
					rel = UniformReliability(radius, sigma);
				}
				else if (w.Type == "AreaDamage")
				{
					var samples = AreaGeometrySamples(w.Node, fo, radii, sigma);
					rel = samples.Sum(s => s.Weight * s.Reliability);
					fp = samples.Sum(s => s.Weight * s.Footprint);
				}
				else
				{
					fp = FootprintCells2(fo, radii);
					rel = Reliability(fo, radii, sigma);
				}

				eff += w.Base * (rel + SwarmW * fp);
				baseTotal += w.Base;
				footTotal += fp;
				relWeighted += rel * w.Base;
			}

			var impactMultiplier = ProjectileImpactMultiplier(resolved);
			eff *= impactMultiplier;
			footTotal *= impactMultiplier;
			var avgRel = baseTotal != 0 ? relWeighted / baseTotal : 0.0;
			return new BotEdResult(eff, baseTotal, footTotal, avgRel, sigma);
		}

		static readonly int[] DirectFalloff = { 100, 0 };
		static readonly int[] DirectRadii = { 0, PointTargetRadius };

		// ---- BotFormula forwarding shims (the sibling weapon-model file addresses
		// these helpers on BotEffectiveDamage, matching the Python module layout where
		// effective_damage re-exports the formula namespace). ----

		/// <summary><see cref="BotFormula.ParseInt32"/>.</summary>
		public static int? ParseInt32(string raw, string field = "value", int? defaultValue = null)
		{
			return BotFormula.ParseInt32(raw, field, defaultValue);
		}

		/// <summary><see cref="BotFormula.ParseBool"/>.</summary>
		public static bool? ParseBool(string raw, string field = "value", bool? defaultValue = null)
		{
			return BotFormula.ParseBool(raw, field, defaultValue);
		}

		/// <summary><see cref="BotFormula.ParseWdist"/>.</summary>
		public static int ParseWdist(string raw, bool allowDistribution = false)
		{
			return BotFormula.ParseWdist(raw, allowDistribution);
		}

		/// <summary><see cref="BotFormula.Fnum"/>.</summary>
		public static double? Fnum(object raw)
		{
			return BotFormula.Fnum(raw);
		}

		/// <summary><see cref="BotFormula.EffReload"/>.</summary>
		public static double EffReload(double reloadDelay, int burst = 1, object burstDelays = null)
		{
			return BotFormula.EffReload(reloadDelay, burst, burstDelays);
		}

		/// <summary><see cref="Evaluate"/> as a nullable tuple — the derived-metrics call shape.</summary>
		public static (double Effective, double BaseTotal, double FootTotal, double AvgRel, double Sigma)?
			EffectiveDamageTuple(MiniYamlMirrorNode resolved)
		{
			var result = Evaluate(resolved);
			if (result == null)
				return null;
			return (result.Effective, result.BaseTotal, result.FootprintTotal, result.AvgReliability, result.Sigma);
		}
	}
}
