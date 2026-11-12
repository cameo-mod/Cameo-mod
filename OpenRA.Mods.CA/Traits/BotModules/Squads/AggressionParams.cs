// IMPL02 A9 (Opus ruling D1912510): the layered parameter resolver. effective = Global x Personality x OwnFaction x Matchup, clamped to the
// documented range. The Info field holds Global x Personality as shipped for the instance; three optional tables (all default empty = 1.0)
// hold the learnable multipliers in permille. Pure and deterministic: no world access, no RNG.
//
// Table keys (lower-case faction ids = Player.Faction.InternalName):
//   AggMultPersonality: "<param>"                      e.g. AggTeamReserveSharePercent: 1200
//   AggMultFaction:     "<own>/<param>"                e.g. gdi/AggTeamReserveSharePercent: 800
//   AggMultMatchup:     "<own>><enemy>/<param>", then "*><enemy>/<param>", then "<own>>*/<param>" (first hit wins; skipped without an enemy faction)
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.CA.Traits.BotModules.Squads
{
	public static class AggressionParams
	{
		/// <summary>1.0 in permille.</summary>
		public const int OnePermille = 1000;

		/// <summary>A single multiplier is clamped to this range (0.1 .. 5.0) so one table row cannot zero or explode a parameter.</summary>
		public const int MinMultiplierPermille = 100;
		public const int MaxMultiplierPermille = 5000;

		static readonly Regex RangeText = new(@"Range (-?\d+)-(\d+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		static readonly ConcurrentDictionary<string, (FieldInfo Field, int Min, int Max)> Registry = new();

		static readonly ConditionalWeakTable<SquadManagerBotModuleCAInfo, ConcurrentDictionary<(string Name, string Own, string Enemy), int>> Cache = new();

		/// <summary>The pure layering: base x personality x faction x matchup (permille each, each clamped), rounded to nearest, then clamped to [min, max].</summary>
		public static int Apply(int baseValue, int min, int max, int personalityPermille, int factionPermille, int matchupPermille)
		{
			long numerator = (long)baseValue * Clamp(personalityPermille) * Clamp(factionPermille) * Clamp(matchupPermille);
			const long Denominator = (long)OnePermille * OnePermille * OnePermille;
			var rounded = numerator >= 0 ? (numerator + Denominator / 2) / Denominator : -((-numerator + Denominator / 2) / Denominator);
			return (int)Math.Min(max, Math.Max(min, rounded));
		}

		static int Clamp(int permille) => Math.Min(MaxMultiplierPermille, Math.Max(MinMultiplierPermille, permille));

		/// <summary>The documented range of a parameter. Throws for an unknown name, a non-int field or a Desc without `Range a-b`.</summary>
		public static (int Min, int Max) RangeOf(string name)
		{
			var e = Entry(name);
			return (e.Min, e.Max);
		}

		static (FieldInfo Field, int Min, int Max) Entry(string name) => Registry.GetOrAdd(name ?? "", n =>
		{
			var field = typeof(SquadManagerBotModuleCAInfo).GetField(n, BindingFlags.Instance | BindingFlags.Public);
			if (field == null || field.FieldType != typeof(int))
				throw new ArgumentException("A9: '" + n + "' is not an int parameter of SquadManagerBotModuleCAInfo");
			var desc = field.GetCustomAttribute<DescAttribute>();
			var m = RangeText.Match(desc == null ? "" : string.Join(" ", desc.Lines));
			if (!m.Success)
				throw new ArgumentException("A9: '" + n + "' documents no `Range a-b` in its Desc");
			return (field, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value));
		});

		static string Norm(string faction) => string.IsNullOrEmpty(faction) ? "" : faction.ToLowerInvariant();

		static int Lookup(IReadOnlyDictionary<string, int> table, string key) =>
			table != null && table.TryGetValue(key, out var v) ? v : OnePermille;

		static int FactionMultiplier(SquadManagerBotModuleCAInfo info, string name, string own) =>
			own.Length > 0 ? Lookup(info.AggMultFaction, own + "/" + name) : OnePermille;

		static int MatchupMultiplier(SquadManagerBotModuleCAInfo info, string name, string own, string enemy)
		{
			if (enemy.Length == 0 || info.AggMultMatchup == null)
				return OnePermille;
			var exact = own.Length > 0 && info.AggMultMatchup.TryGetValue(own + ">" + enemy + "/" + name, out var x) ? x : (int?)null;
			var anyOwn = info.AggMultMatchup.TryGetValue("*>" + enemy + "/" + name, out var y) ? y : (int?)null;
			var anyEnemy = own.Length > 0 && info.AggMultMatchup.TryGetValue(own + ">*/" + name, out var z) ? z : (int?)null;
			return exact ?? anyOwn ?? anyEnemy ?? OnePermille;
		}

		/// <summary>The effective value of <paramref name="name"/> for this seat's faction against <paramref name="enemyFaction"/> (null or empty skips the matchup layer). The tactical, per-engagement form.</summary>
		public static int Resolve(SquadManagerBotModuleCAInfo info, string name, string ownFaction, string enemyFaction)
		{
			var e = Entry(name);
			var own = Norm(ownFaction);
			var enemy = Norm(enemyFaction);
			var cache = Cache.GetValue(info, _ => new ConcurrentDictionary<(string, string, string), int>());
			return cache.GetOrAdd((name, own, enemy), _ =>
				Apply((int)e.Field.GetValue(info), e.Min, e.Max, Lookup(info.AggMultPersonality, name),
					FactionMultiplier(info, name, own), MatchupMultiplier(info, name, own, enemy)));
		}

		/// <summary>One enemy seat for the strategic form: its faction and its weight (observed army value plus a floor; non-positive weights are ignored).</summary>
		public readonly record struct EnemySeat(string Faction, long Weight);

		/// <summary>
		/// The strategic, aggregate form (Opus A9 add): a weighted geometric mean over the enemy seats of M(own coalition, f_e), where the own coalition
		/// is the geometric mean over the own and allied factions c of F(c) x Matchup(c, f_e). With no enemy seat only the coalition's faction layer
		/// applies. All multipliers at 1.0 give exactly 1.0. Used by the A5 reserve and the A7 finishing push; tactical calls use <see cref="Resolve"/>.
		/// </summary>
		public static int ResolveStrategic(SquadManagerBotModuleCAInfo info, string name, IReadOnlyList<string> ownCoalitionFactions, IReadOnlyList<EnemySeat> enemySeats)
		{
			var e = Entry(name);
			var coalition = new List<string>();
			if (ownCoalitionFactions != null)
				foreach (var f in ownCoalitionFactions)
					if (!string.IsNullOrEmpty(f))
						coalition.Add(Norm(f));
			if (coalition.Count == 0)
				coalition.Add("");

			var seats = new List<EnemySeat>();
			if (enemySeats != null)
				foreach (var s in enemySeats)
					if (s.Weight > 0 && !string.IsNullOrEmpty(s.Faction))
						seats.Add(new EnemySeat(Norm(s.Faction), s.Weight));

			double logSum = 0;
			if (seats.Count == 0)
			{
				foreach (var c in coalition)
					logSum += Math.Log(Clamp(FactionMultiplier(info, name, c)) / (double)OnePermille);
				logSum /= coalition.Count;
			}
			else
			{
				double total = 0;
				foreach (var seat in seats)
					total += seat.Weight;
				foreach (var seat in seats)
				{
					double inner = 0;
					foreach (var c in coalition)
						inner += Math.Log(Clamp(FactionMultiplier(info, name, c)) / (double)OnePermille)
							+ Math.Log(Clamp(MatchupMultiplier(info, name, c, seat.Faction)) / (double)OnePermille);
					logSum += seat.Weight / total * (inner / coalition.Count);
				}
			}

			var aggregatePermille = (int)Math.Round(OnePermille * Math.Exp(logSum));
			return Apply((int)e.Field.GetValue(info), e.Min, e.Max, Lookup(info.AggMultPersonality, name), aggregatePermille, OnePermille);
		}
	}
}
