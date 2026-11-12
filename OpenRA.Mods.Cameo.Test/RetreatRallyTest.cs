#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

// PLAYTEST-PORT (Opus ruling, 2026-11-10): the yaml-OFF guard assertions in this file were flipped to the ON values
// that the ported mods/cameo/ai/ai.yaml carries (switches armed on the six personalities; classic unchanged).
// Test names are unchanged. The fixture mcv_deploy_ai.yaml is the build-time copy of mods/cameo/ai/ai.yaml (csproj Link).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using Policy = OpenRA.Mods.CA.Traits.BotModules.Squads.RetreatRallyPolicyCA;

namespace OpenRA.Mods.Cameo.Test
{
	// A10 RETREAT-RALLY (design DRAFT02 section 7): pure tests of RetreatRallyPolicyCA (1-14) and source pins of the wiring (15-25). The squad
	// manager has no headless world fixture in this project, so the world side (collector, state, hooks) is covered by pins; the first
	// real exercise is the ON smoke.
	[TestFixture]
	public sealed class RetreatRallyTest
	{
		const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		static readonly Policy.Params Defaults = new(60, 40, 35, 25, 200, 1000, 800, 600, 100, 50, 450, 1000);

		static Policy.Candidate C(string owner = "a/", int id = 1, bool ally = false, bool idle = false, bool request = false,
			Policy.RallyNeed need = Policy.RallyNeed.Fight, long value = 3000, long strength = 4000, int d = 10,
			bool retreating = false, bool rallying = false, bool marked = false, bool cooling = false, bool black = false,
			bool breaker = false, bool self = false, bool other = false) =>
			new(owner, id, ally, idle, request, need, value, strength, d, retreating, rallying, marked, cooling, black, breaker, self, other);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Manager() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.RetreatRally.cs");

		static string State() => Source("mods/CA/Traits/BotModules/Squads/States/RetreatRallyStatesCA.cs");

		static string PolicySource() => Source("mods/CA/Traits/BotModules/Squads/RetreatRallyPolicyCA.cs");

		static string InfoSource() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCAInfo.RetreatRally.cs");

		static string GroundStates() => Source("mods/CA/Traits/BotModules/Squads/States/GroundStatesCA.cs");

		static string StateBase() => Source("mods/CA/Traits/BotModules/Squads/States/StateBaseCA.cs");

		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static int Count(string text, string needle) => Regex.Matches(text, Regex.Escape(needle)).Count;

		static int Score(Policy.Candidate c, long vr = 1000) => Policy.Score(c, vr, Defaults);

		static Policy.StepInput S(Policy.RallyPhase phase = Policy.RallyPhase.March, Policy.RallyAction action = Policy.RallyAction.Support, int since = 0, int support = 0,
			bool arrived = false, bool losing = false, bool invalid = false, bool ended = false, bool launched = false, int max = 900, int supportMax = 600) =>
			new(phase, action, since, support, arrived, losing, invalid, ended, launched, max, supportMax);

		// ---- 1. score ----

		[Test]
		public void ACloserDestinationScoresHigher()
		{
			Assert.That(Score(C(d: 10)), Is.EqualTo(933), "exact: (40 x 834 + 35 x 1000 + 25 x 1000) / 100");
			Assert.That(Score(C(d: 30)), Is.EqualTo(800), "(40 x 500 + 35 x 1000 + 25 x 1000) / 100");
			Assert.That(Score(C(d: 10)), Is.GreaterThan(Score(C(d: 30))));
		}

		[Test]
		public void AStrongerDestinationScoresHigherUpToTheCapAndFlatAbove()
		{
			int Of(long strength) => Score(C(strength: strength, need: Policy.RallyNeed.Idle));
			Assert.That(Of(100), Is.LessThan(Of(200)));
			Assert.That(Of(200), Is.LessThan(Of(1000)));
			Assert.That(Policy.StrengthTerm(2000, 1000, 200), Is.EqualTo(1000), "S = Vr x target / 100 is the cap");
			Assert.That(Policy.StrengthTerm(1999, 1000, 200), Is.EqualTo(999));
			Assert.That(Of(2000), Is.EqualTo(Of(4000)), "flat above the cap");
			Assert.That(Of(2000), Is.EqualTo(Of(2_000_000)));
			Assert.That(Policy.StrengthTerm(0, 1000, 200), Is.EqualTo(0));
		}

		[Test]
		public void TheNeedOrderIsFightDefendMarchIdle()
		{
			int Of(Policy.RallyNeed need) => Score(C(need: need));
			Assert.That(Of(Policy.RallyNeed.Fight), Is.GreaterThan(Of(Policy.RallyNeed.Defend)));
			Assert.That(Of(Policy.RallyNeed.Defend), Is.GreaterThan(Of(Policy.RallyNeed.March)));
			Assert.That(Of(Policy.RallyNeed.March), Is.GreaterThan(Of(Policy.RallyNeed.Idle)));
			Assert.That(Of(Policy.RallyNeed.Idle), Is.EqualTo(708), "(40 x 834 + 35 x 1000 + 25 x 100) / 100");
			Assert.That(Policy.NeedPoints(Policy.RallyNeed.Defend, Defaults), Is.EqualTo(800));
			Assert.That(Policy.NeedPoints(Policy.RallyNeed.March, Defaults), Is.EqualTo(600));
		}

		[Test]
		public void TheMaximumDistanceIsExcludedAndOneCellLessIsNot()
		{
			Assert.That(Policy.Exclusion(C(d: 60), Defaults), Is.EqualTo("distance"));
			Assert.That(Policy.Exclusion(C(d: 59), Defaults), Is.Null);
			Assert.That(Policy.Exclusion(C(d: 0), Defaults), Is.Null);
			Assert.That(Policy.DistanceTerm(0, 60), Is.EqualTo(1000));
			Assert.That(Policy.DistanceTerm(60, 60), Is.EqualTo(0));
			Assert.That(Policy.DistanceTerm(90, 60), Is.EqualTo(0), "never negative");
		}

		[Test]
		public void ARetreatingSquadOfNoValueIsAlwaysMatched()
		{
			Assert.That(Policy.StrengthTerm(0, 0, 200), Is.EqualTo(1000));
			Assert.That(Policy.StrengthTerm(5, 0, 200), Is.EqualTo(1000));
			Assert.That(Policy.StrengthTerm(5, 1000, 0), Is.EqualTo(1000), "a zero target percent cannot divide by zero");
		}

		[Test]
		public void LargeValuesDoNotOverflow()
		{
			// L3: Sstr in long. S_c x 100000 exceeds int for S_c above 21474.
			Assert.That(Policy.StrengthTerm(10_000_000, 1_000_000, 200), Is.EqualTo(1000));
			Assert.That(Policy.StrengthTerm(30_000, 1_000_000, 200), Is.EqualTo(15), "30000 x 100000 / (10^6 x 200) = 15, not an int-overflow garbage value");
			Assert.That(Policy.StrengthTerm(21_475, 1_000_000, 100), Is.EqualTo(21), "just above the int limit of the product");
			Assert.That(Score(C(strength: 30_000, need: Policy.RallyNeed.Idle), 1_000_000), Is.EqualTo((40 * 834 + 35 * 15 + 25 * 100) / 100));
			Assert.That(Score(C(strength: 10_000_000), 1_000_000), Is.EqualTo(933));
		}

		[Test]
		public void TheAllyPenaltyIsTakenOffAndTheScoreIsNeverBelowZeroOrAboveTheFull()
		{
			Assert.That(Score(C(ally: true)), Is.EqualTo(883));
			Assert.That(Policy.Score(C(ally: true, need: Policy.RallyNeed.Idle, strength: 0), 1000, Defaults with { AllyPenaltyPoints = 500 }), Is.EqualTo(0));
			Assert.That(Policy.Score(C(), 1000, Defaults with { NeedFightPoints = 5000, WeightDistance = 0, WeightStrength = 0 }), Is.EqualTo(1000));
		}

		// ---- 2. weights ----

		[Test]
		public void AZeroWeightDropsItsTermAndTheNormalisationIsExact()
		{
			var c = C(d: 10, need: Policy.RallyNeed.Idle);
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightDistance = 0 }), Is.EqualTo((35 * 1000 + 25 * 100) / 60));
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightStrength = 0 }), Is.EqualTo((40 * 834 + 25 * 100) / 65));
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightNeed = 0 }), Is.EqualTo((40 * 834 + 35 * 1000) / 75));
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightDistance = 1, WeightStrength = 1, WeightNeed = 1 }), Is.EqualTo((834 + 1000 + 100) / 3));
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightDistance = 2, WeightStrength = 2, WeightNeed = 2 }), Is.EqualTo(Policy.Score(c, 1000, Defaults with { WeightDistance = 1, WeightStrength = 1, WeightNeed = 1 })), "scaling every weight changes nothing");
			Assert.That(Policy.Score(c, 1000, Defaults with { WeightDistance = 0, WeightStrength = 0, WeightNeed = 0 }), Is.EqualTo(0), "all zero is safe (and RangeError refuses it)");
		}

		[Test]
		public void AllZeroWeightsAreRefusedByTheRangeCheck()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(Policy.RangeError(info.RetreatRallyValue), Is.Null, "the shipped defaults");
			Assert.That(Policy.RangeError(k => k.StartsWith("AggTeamRallyWeight", StringComparison.Ordinal) ? 0 : info.RetreatRallyValue(k)), Does.Contain("not all be zero"));
			Assert.That(Policy.RangeError(k => k == "AggTeamRallyWeightDistance" || k == "AggTeamRallyWeightStrength" ? 0 : info.RetreatRallyValue(k)), Is.Null, "one weight is enough");
		}

		// ---- 3. ordering ----

		[Test]
		public void AnOwnDestinationBeatsAnAllyAtEqualInputsAndTiesGoByDistanceKeyAndId()
		{
			var own = C(owner: "z/", id: 9);
			var ally = C(owner: "a/", id: 1, ally: true);
			Assert.That(Policy.Choose(new[] { ally, own }, 1000, Defaults).Pick, Is.EqualTo(own), "the ally penalty ranks the own squad first");
			Assert.That(Policy.Choose(new[] { ally, own }, 1000, Defaults with { AllyPenaltyPoints = 0 }).Pick, Is.EqualTo(own), "at equal score own is still before ally, whatever the key");

			var near = C(owner: "b/", id: 1, d: 5, strength: 4000);
			var far = C(owner: "a/", id: 1, d: 6, strength: 4000);
			Assert.That(Policy.Choose(new[] { far, near }, 1000, Defaults with { WeightDistance = 0 }).Pick, Is.EqualTo(near), "equal score: the nearer one");
		}

		[Test]
		public void TheChoiceIsTheSameForEveryInputOrder()
		{
			var set = new[] { C("a/", 2), C("a/", 1), C("b/", 1), C("c/", 1, ally: true) };
			var expected = Policy.KeyOf(Policy.Choose(set, 1000, Defaults).Pick);
			Assert.That(expected, Is.EqualTo("a/#1"), "the lowest owner key, then the lowest squad id");
			foreach (var permutation in Permutations(set.ToList()))
				Assert.That(Policy.KeyOf(Policy.Choose(permutation, 1000, Defaults).Pick), Is.EqualTo(expected));

			var withoutFirst = set.Where(c => !(c.OwnerKey == "a/" && c.SquadId == 1)).ToList();
			Assert.That(Policy.KeyOf(Policy.Choose(withoutFirst, 1000, Defaults).Pick), Is.EqualTo("a/#2"));
			Assert.That(Policy.KeyOf(Policy.Choose(withoutFirst.Where(c => c.OwnerKey != "a/").ToList(), 1000, Defaults).Pick), Is.EqualTo("b/#1"));
		}

		static IEnumerable<List<T>> Permutations<T>(List<T> items)
		{
			if (items.Count <= 1)
			{
				yield return items.ToList();
				yield break;
			}

			for (var i = 0; i < items.Count; i++)
			{
				var rest = items.Where((_, j) => j != i).ToList();
				foreach (var tail in Permutations(rest))
				{
					tail.Insert(0, items[i]);
					yield return tail;
				}
			}
		}

		// ---- 4. exclusions ----

		[Test]
		public void EveryExclusionHasItsReasonInTheDocumentedOrder()
		{
			Assert.That(Policy.Exclusion(C(), Defaults), Is.Null);
			Assert.That(Policy.Exclusion(C(self: true), Defaults), Is.EqualTo("self"));
			Assert.That(Policy.Exclusion(C(breaker: true), Defaults), Is.EqualTo("breaker"));
			Assert.That(Policy.Exclusion(C(retreating: true), Defaults), Is.EqualTo("retreating"));
			Assert.That(Policy.Exclusion(C(rallying: true), Defaults), Is.EqualTo("rallying"));
			Assert.That(Policy.Exclusion(C(marked: true), Defaults), Is.EqualTo("marked"));
			Assert.That(Policy.Exclusion(C(cooling: true), Defaults), Is.EqualTo("cooldown"));
			Assert.That(Policy.Exclusion(C(black: true), Defaults), Is.EqualTo("blacklist"));
			Assert.That(Policy.Exclusion(C(idle: true, ally: true), Defaults), Is.EqualTo("idle_ally"));
			Assert.That(Policy.Exclusion(C(other: true), Defaults), Is.EqualTo("state"));
			Assert.That(Policy.Exclusion(C(value: 999), Defaults), Is.EqualTo("value"));
			Assert.That(Policy.Exclusion(C(value: 1000), Defaults), Is.Null, "the floor is inclusive");
			Assert.That(Policy.Exclusion(C(d: 60), Defaults), Is.EqualTo("distance"));
			Assert.That(Policy.Exclusion(C(self: true, marked: true), Defaults), Is.EqualTo("self"), "the first reason wins");
		}

		[Test]
		public void ADefenceRequestHasNoSquadValueFloorAndNoIdleExclusion()
		{
			Assert.That(Policy.Exclusion(C(request: true, ally: true, value: 0, need: Policy.RallyNeed.Defend), Defaults), Is.Null);
			Assert.That(Policy.Exclusion(C(request: true, ally: true, idle: true), Defaults), Is.Null, "a request is never an idle squad");
		}

		[Test]
		public void ChooseNeverPicksAnExcludedCandidateAndReportsTheNearestReasonWhenNoneIsLeft()
		{
			foreach (var bad in new[] { C(retreating: true), C(rallying: true), C(marked: true), C(cooling: true), C(black: true), C(idle: true, ally: true), C(value: 10), C(other: true) })
			{
				var good = C(owner: "z/", id: 5, d: 50, strength: 400);
				Assert.That(Policy.Choose(new[] { bad, good }, 1000, Defaults with { MinScore = 0 }).Pick, Is.EqualTo(good));
				Assert.That(Policy.Choose(new[] { bad }, 1000, Defaults).Found, Is.False);
			}

			Assert.That(Policy.Choose(new[] { C(marked: true) }, 1000, Defaults).Why, Is.EqualTo("marked"));
			Assert.That(Policy.Choose(new[] { C(idle: true, ally: true) }, 1000, Defaults).Why, Is.EqualTo("idle_ally"));
			Assert.That(Policy.Choose(new[] { C(cooling: true, d: 40), C(marked: true, d: 5) }, 1000, Defaults).Why, Is.EqualTo("marked"), "the nearest excluded candidate names the reason");
			Assert.That(Policy.Choose(new[] { C(self: true), C(breaker: true, id: 2) }, 1000, Defaults).Why, Is.EqualTo("none"), "the squad itself and the breaker are not a reason");
			Assert.That(Policy.Choose(Array.Empty<Policy.Candidate>(), 1000, Defaults).Why, Is.EqualTo("none"));
			Assert.That(Policy.Choose(null, 1000, Defaults).Why, Is.EqualTo("none"));
		}

		// ---- 5. MinScore ----

		[Test]
		public void TheBestScoreMustReachMinScoreInclusively()
		{
			var c = C(d: 10);
			Assert.That(Score(c), Is.EqualTo(933));
			var at = Policy.Choose(new[] { c }, 1000, Defaults with { MinScore = 933 });
			Assert.That(at.Found, Is.True, "inclusive");
			Assert.That(at.Score, Is.EqualTo(933));
			var above = Policy.Choose(new[] { c }, 1000, Defaults with { MinScore = 934 });
			Assert.That(above.Found, Is.False);
			Assert.That(above.Why, Is.EqualTo("min_score"));
			Assert.That(above.Best, Is.EqualTo(933), "the best score is reported");
			Assert.That(above.Considered, Is.EqualTo(1));
		}

		// ---- 6. guards G1 G2 G3 G8 ----

		[Test]
		public void G1TheJoinRatioIsInclusiveAndIsSkippedForAMergeAndWithoutEnemies()
		{
			Assert.That(Policy.Losing(1.0, 100, true, false), Is.False, "ratio = bar is not losing");
			Assert.That(Policy.Losing(0.99, 100, true, false), Is.True);
			Assert.That(Policy.Losing(0.0, 100, false, false), Is.False, "no visible enemy: skipped");
			Assert.That(Policy.Losing(0.0, 100, true, true), Is.False, "the merge destination has no enemy by definition");
			Assert.That(Policy.Losing(1.49, 150, true, false), Is.True);
			Assert.That(Policy.Losing(1.5, 150, true, false), Is.False);
		}

		[Test]
		public void G2ADestinationMustBeFarEnoughBehindTheSquadAwayFromThePursuers()
		{
			const int cell = Policy.CellUnits;
			Assert.That(Policy.BehindPursuers(true, 12 * cell, 10 * cell, 2), Is.False, "exactly the gain");
			Assert.That(Policy.BehindPursuers(true, 12 * cell - 1, 10 * cell, 2), Is.True);
			Assert.That(Policy.BehindPursuers(true, 1, 10 * cell, 2), Is.True);
			Assert.That(Policy.BehindPursuers(false, 1, 10 * cell, 2), Is.False, "no pursuer in sight: the rule is skipped");
			Assert.That(Policy.BehindPursuers(true, 10 * cell, 10 * cell, 0), Is.False, "gain 0: not nearer");
			Assert.That(Policy.BehindPursuers(true, 10 * cell - 1, 10 * cell, 0), Is.True);
		}

		[Test]
		public void TheWorldGuardsRejectRankedDestinationsUntilOnePassesWithABoundedNumberOfChecks()
		{
			var set = Enumerable.Range(1, 10).Select(i => C(owner: "a/", id: i, d: i)).ToList();
			var calls = new List<int>();
			var choice = Policy.Choose(set, 1000, Defaults, c => { calls.Add(c.SquadId); return c.SquadId < 3 ? (c.SquadId == 1 ? "pursuer" : "losing") : null; });
			Assert.That(choice.Found, Is.True);
			Assert.That(choice.Pick.SquadId, Is.EqualTo(3));
			Assert.That(calls, Is.EqualTo(new[] { 1, 2, 3 }), "ranked order");

			var all = Policy.Choose(set, 1000, Defaults, c => "pursuer");
			Assert.That(all.Found, Is.False);
			Assert.That(all.Why, Is.EqualTo("pursuer"));
			Assert.That(all.Best, Is.GreaterThan(0));

			var count = 0;
			Policy.Choose(set, 1000, Defaults, c => { count++; return "losing"; });
			Assert.That(count, Is.EqualTo(Policy.MaxGuardChecks));
			Assert.That(Policy.Choose(set, 1000, Defaults, c => c.SquadId == 1 ? "losing" : "pursuer").Why, Is.EqualTo("losing"), "the best destination's reason");
		}

		[Test]
		public void G3AndG8TheBaseAnswersFirst()
		{
			Assert.That(Policy.HomeBlock(false, false, false, false), Is.Null);
			Assert.That(Policy.HomeBlock(true, false, false, false), Is.EqualTo("recall"));
			Assert.That(Policy.HomeBlock(true, true, true, true), Is.EqualTo("recall"), "a recall names itself first");
			Assert.That(Policy.HomeBlock(false, true, false, false), Is.EqualTo("home"), "a squad that defends");
			Assert.That(Policy.HomeBlock(false, false, true, false), Is.EqualTo("home"), "an active defence plan");
			Assert.That(Policy.HomeBlock(false, false, false, true), Is.EqualTo("home"), "a home threat");
		}

		// ---- 7. G4 G5 G6 ----

		[Test]
		public void G4TheHopCapAndTheEpisodeReset()
		{
			Assert.That(Policy.HopsExhausted(0, -1, 0, 1, 1500), Is.False, "no rally yet");
			Assert.That(Policy.HopsExhausted(1, 100, 100, 1, 1500), Is.True, "one hop done, cap 1");
			Assert.That(Policy.HopsExhausted(1, 100, 100 + 1499, 1, 1500), Is.True);
			Assert.That(Policy.HopsExhausted(1, 100, 100 + 1500, 1, 1500), Is.False, "the episode resets after EpisodeResetTicks");
			Assert.That(Policy.HopsExhausted(0, -1, 5, 0, 1500), Is.True, "MaxHops 0 = base only");
			Assert.That(Policy.HopsExhausted(1, 100, 101, 2, 1500), Is.False);
			Assert.That(Policy.HopsExhausted(2, 100, 101, 2, 1500), Is.True);
			Assert.That(Policy.EffectiveHops(3, 100, 1600, 1500), Is.EqualTo(0));
			Assert.That(Policy.EffectiveHops(3, 100, 1599, 1500), Is.EqualTo(3));
			Assert.That(Policy.EffectiveHops(3, -1, 10, 1500), Is.EqualTo(0), "a never-rallied squad has no hops");
		}

		[Test]
		public void G5AndG6TheBlockEndsExactlyAtItsTick()
		{
			var until = Policy.BlockedUntil(100, 750);
			Assert.That(until, Is.EqualTo(850));
			Assert.That(Policy.Blocked(until, 849), Is.True);
			Assert.That(Policy.Blocked(until, 850), Is.False);
			Assert.That(Policy.Blocked(Policy.BlockedUntil(100, 0), 100), Is.False, "0 ticks blocks nothing");
			Assert.That(Policy.Blocked(Policy.BlockedUntil(100, -5), 100), Is.False);
			Assert.That(Policy.Blocked(0, 0), Is.False, "never blocked");
		}

		// ---- 8. G7 ----

		[Test]
		public void G7TheLockBlocksASwitchTheMarginDecidesAfterItAndAnInvalidDestinationAlwaysSwitches()
		{
			Assert.That(Policy.LockHeld(0, 249, 250), Is.True);
			Assert.That(Policy.LockHeld(0, 250, 250), Is.False);
			Assert.That(Policy.MaySwitch(0, 249, 250, 1000, 100, 25, false), Is.False, "locked: no switch however good");
			Assert.That(Policy.MaySwitch(0, 250, 250, 499, 400, 25, false), Is.False, "margin not reached (400 x 125 / 100 = 500)");
			Assert.That(Policy.MaySwitch(0, 250, 250, 500, 400, 25, false), Is.True, "margin reached exactly");
			Assert.That(Policy.MaySwitch(0, 250, 250, 400, 400, 0, false), Is.True, "margin 0: equal is enough");
			Assert.That(Policy.MaySwitch(0, 250, 250, 399, 400, 0, false), Is.False);
			Assert.That(Policy.MaySwitch(0, 10, 250, 0, 900, 25, true), Is.True, "an invalid destination is replaced at once, even locked");
			Assert.That(Policy.MaySwitch(0, 250, 0, 449, 450, 0, false), Is.False, "no lock: only a better score switches");
			Assert.That(Policy.RescoreDue(0, 99, 100), Is.False);
			Assert.That(Policy.RescoreDue(0, 100, 100), Is.True);
		}

		[Test]
		public void G7DuringTheLockTheOnlyExitIsDownwardThroughTheLosingRule()
		{
			// The squad may leave a locked destination only toward the base: the phase machine ends it on march_flee at any time (no lock term).
			var locked = Policy.Step(S(losing: true, since: 1));
			Assert.That(locked.End, Is.EqualTo(Policy.RallyEnd.AbortLosing));
			Assert.That(Policy.NextState(locked.End, locked.Phase), Is.EqualTo(Policy.RallyNext.FleeBaseOnly));
			Assert.That(Policy.MaySwitch(0, 1, 250, 1000, 1, 0, false), Is.False, "and never sideways");
		}

		// ---- 9. axis ----

		[Test]
		public void TheAxisCellLiesOnTheEnemyToOwnLineAtDepthOnTheEnemyFacingSide()
		{
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 6), Is.EqualTo((100, 94)), "positive depth: toward the enemy");
			Assert.That(Policy.AxisCell(100, 100, 100, 50, -6), Is.EqualTo((100, 106)), "negative depth: behind the base");
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 0), Is.EqualTo((100, 100)), "depth 0: the anchor");
			Assert.That(Policy.AxisCell(100, 100, 100, 100, 6), Is.EqualTo((100, 100)), "degenerate: enemy on the anchor");
			Assert.That(Policy.AxisCell(0, 0, 30, 40, 10), Is.EqualTo((6, 8)), "a diagonal axis");
			Assert.That(Policy.AxisCell(50, 50, 20, 50, 7), Is.EqualTo((43, 50)), "enemy to the west: the cell is west of the base");
		}

		[Test]
		public void TheAxisCellIsClampedAndSnapsToTheNearestReachableCellDownTheAxis()
		{
			(int, int) Clamp(int x, int y) => (Math.Max(0, x), Math.Max(0, y));
			Assert.That(Policy.AxisCell(2, 2, 2, 50, -6, Clamp), Is.EqualTo((2, 0)), "off the map edge");
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 6, null, (x, y) => y >= 98), Is.EqualTo((100, 99)), "depth 6, 3, 1: the first reachable");
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 6, null, (x, y) => true), Is.EqualTo((100, 94)));
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 6, null, (x, y) => (x, y) == (100, 100)), Is.EqualTo((100, 100)), "down to the anchor");
			Assert.That(Policy.AxisCell(100, 100, 100, 50, 6, null, (x, y) => false), Is.Null, "nothing reachable");
		}

		// ---- 10. phase machine ----

		[Test]
		public void TheMarchContinuesUntilItArrivesOrExpires()
		{
			var march = Policy.Step(S(since: 100));
			Assert.That(march, Is.EqualTo(new Policy.StepResult(Policy.RallyPhase.March, Policy.RallyAction.Support, Policy.RallyEnd.None)));
			Assert.That(Policy.Step(S(since: 899)).End, Is.EqualTo(Policy.RallyEnd.None));
			var expired = Policy.Step(S(since: 900));
			Assert.That(expired.End, Is.EqualTo(Policy.RallyEnd.Expired));
			Assert.That(expired.Phase, Is.EqualTo(Policy.RallyPhase.March));
			Assert.That(Policy.NextState(expired.End, expired.Phase), Is.EqualTo(Policy.RallyNext.FleeBaseOnly), "expiry in the march = the legacy flight");
			Assert.That(Policy.Step(S(since: 900, arrived: true)).End, Is.EqualTo(Policy.RallyEnd.None), "arriving on the last tick still arrives");
		}

		[Test]
		public void ArrivingAtAnArmyBeginsTheSupportAndSupportExpiresIntoIdle()
		{
			var arrive = Policy.Step(S(arrived: true));
			Assert.That(arrive, Is.EqualTo(new Policy.StepResult(Policy.RallyPhase.Support, Policy.RallyAction.Support, Policy.RallyEnd.None)));
			Assert.That(Policy.Step(S(Policy.RallyPhase.Support, support: 599)).End, Is.EqualTo(Policy.RallyEnd.None));
			var expired = Policy.Step(S(Policy.RallyPhase.Support, support: 600));
			Assert.That(expired.End, Is.EqualTo(Policy.RallyEnd.Expired));
			Assert.That(Policy.NextState(expired.End, expired.Phase), Is.EqualTo(Policy.RallyNext.Idle), "expiry in the support = Idle");
		}

		[Test]
		public void ASupportMaxOfZeroEndsOnArrival()
		{
			var arrive = Policy.Step(S(arrived: true, supportMax: 0));
			Assert.That(arrive.End, Is.EqualTo(Policy.RallyEnd.Expired));
			Assert.That(arrive.Phase, Is.EqualTo(Policy.RallyPhase.Support));
			Assert.That(Policy.NextState(arrive.End, arrive.Phase), Is.EqualTo(Policy.RallyNext.Idle));
			Assert.That(Policy.Step(S(arrived: false, supportMax: 0)).End, Is.EqualTo(Policy.RallyEnd.None), "only the arrival ends it");
		}

		[Test]
		public void ArrivingAtAnOwnIdleSquadMergesIntoThePool()
		{
			var merge = Policy.Step(S(action: Policy.RallyAction.Merge, arrived: true));
			Assert.That(merge.End, Is.EqualTo(Policy.RallyEnd.MergedPool));
			Assert.That(merge.Phase, Is.EqualTo(Policy.RallyPhase.Merge));
			Assert.That(Policy.NextState(merge.End, merge.Phase), Is.EqualTo(Policy.RallyNext.DismissPool));
			Assert.That(Policy.Step(S(action: Policy.RallyAction.Merge)).End, Is.EqualTo(Policy.RallyEnd.None), "still marching");
			Assert.That(Policy.Step(S(action: Policy.RallyAction.Merge, ended: true, arrived: true)).End, Is.EqualTo(Policy.RallyEnd.MergedPool), "an idle destination has no fight to end");
		}

		[Test]
		public void LosingAndAnInvalidDestinationAbortInEveryPhaseBeforeAnythingElse()
		{
			foreach (var phase in new[] { Policy.RallyPhase.March, Policy.RallyPhase.Support })
			{
				Assert.That(Policy.Step(S(phase, losing: true, arrived: true)).End, Is.EqualTo(Policy.RallyEnd.AbortLosing));
				Assert.That(Policy.Step(S(phase, invalid: true, arrived: true)).End, Is.EqualTo(Policy.RallyEnd.AbortLosing));
				Assert.That(Policy.Step(S(phase, losing: true, since: 5000, support: 5000)).End, Is.EqualTo(Policy.RallyEnd.AbortLosing), "not an expiry");
			}

			Assert.That(Policy.Step(S(action: Policy.RallyAction.Merge, invalid: true)).End, Is.EqualTo(Policy.RallyEnd.AbortLosing), "an own idle destination that is gone downgrades");
			Assert.That(Policy.Step(S(Policy.RallyPhase.End, losing: true)).End, Is.EqualTo(Policy.RallyEnd.None), "an ended rally stays ended");
		}

		[Test]
		public void ADestinationWhoseFightIsOverEndsTheSupportAsTargetGone()
		{
			Assert.That(Policy.Step(S(ended: true)).End, Is.EqualTo(Policy.RallyEnd.TargetGone));
			Assert.That(Policy.Step(S(Policy.RallyPhase.Support, ended: true)).End, Is.EqualTo(Policy.RallyEnd.TargetGone));
			Assert.That(Policy.NextState(Policy.RallyEnd.TargetGone, Policy.RallyPhase.Support), Is.EqualTo(Policy.RallyNext.Idle));
			Assert.That(Policy.NextState(Policy.RallyEnd.TargetGone, Policy.RallyPhase.March), Is.EqualTo(Policy.RallyNext.Idle));
		}

		// ---- 11. ping-pong (M2) ----

		[Test]
		public void IsMarkedBoundary()
		{
			Assert.That(Policy.IsMarked(-1, 1000, 400), Is.False, "never");
			Assert.That(Policy.IsMarked(100, 499, 400), Is.True);
			Assert.That(Policy.IsMarked(100, 500, 400), Is.False, "the window is exclusive");
			Assert.That(Policy.IsMarked(100, 100, 400), Is.True, "marked this very tick");
			Assert.That(Policy.IsMarked(0, 0, 400), Is.True, "tick 0 is a valid mark");
			Assert.That(Policy.IsMarked(100, 100, 0), Is.False);
		}

		[Test]
		public void TwoRetreatingSquadsNeverRallyOnEachOtherAndAChainIsImpossible()
		{
			// A retreats (marked) and B asks: A is not a destination, so B goes to the base. B retreating (marked) cannot be A's.
			var asBsView = Policy.Choose(new[] { C("a/", 1, marked: true) }, 1000, Defaults);
			Assert.That(asBsView.Found, Is.False);
			Assert.That(asBsView.Why, Is.EqualTo("marked"));
			Assert.That(asBsView.MarkedRejects, Is.EqualTo(1));

			// A rallies on B (A is now rallying and marked); C asks and sees A (rallying) and B (marked retreating): no chain.
			var cView = Policy.Choose(new[] { C("a/", 1, rallying: true, marked: true), C("b/", 1, marked: true) }, 1000, Defaults);
			Assert.That(cView.Found, Is.False);
			Assert.That(cView.MarkedRejects, Is.EqualTo(1), "A is excluded as rallying first, B as marked");

			// B retreats after A took it: B is marked, A rallies: they cannot swap.
			Assert.That(Policy.Choose(new[] { C("a/", 1, rallying: true) }, 1000, Defaults).Found, Is.False);
		}

		// ---- 12. A9 registry ----

		static IEnumerable<FieldInfo> RallyFields() => typeof(SquadManagerBotModuleCAInfo).GetFields(BindingFlags.Instance | BindingFlags.Public)
			.Where(f => f.Name.StartsWith("AggTeamRally", StringComparison.Ordinal) && f.FieldType == typeof(int));

		[Test]
		public void EveryAggTeamRallyFieldHasAKnownRangeThatEqualsItsDescAndAShippedDefaultInsideIt()
		{
			var fields = RallyFields().ToList();
			Assert.That(fields.Count, Is.EqualTo(Policy.Ranges.Length), "the registry table and the fields are the same set");
			Assert.That(fields.Select(f => f.Name), Is.EquivalentTo(Policy.Ranges.Select(r => r.Key)));
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var (key, min, max) in Policy.Ranges)
			{
				var field = typeof(SquadManagerBotModuleCAInfo).GetField(key);
				var m = Regex.Match(string.Join(" ", field.GetCustomAttribute<DescAttribute>().Lines), @"Range (-?\d+)-(\d+)");
				Assert.That(m.Success, Is.True, key);
				Assert.That((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)), Is.EqualTo((min, max)), key + " Desc range");
				Assert.That(AggressionParams.RangeOf(key), Is.EqualTo((min, max)), key + " resolver range");
				Assert.That((int)field.GetValue(info), Is.InRange(min, max), key + " default");
				Assert.That(AggressionParams.Resolve(info, key, "gdi", "nod"), Is.EqualTo((int)field.GetValue(info)), key + " resolves to its default with empty tables");
				Assert.That(info.RetreatRallyValue(key), Is.EqualTo((int)field.GetValue(info)), key);
			}

			Assert.Throws<ArgumentOutOfRangeException>(() => info.RetreatRallyValue("AggTeamRallyNoSuchRow"));
		}

		[Test]
		public void TheShippedDefaultsAreTheDraft02Table()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(info.RetreatRallyEnabled, Is.False);
			var expected = new Dictionary<string, int>
			{
				["AggTeamRallyMaxDistanceCells"] = 60, ["AggTeamRallyWeightDistance"] = 40, ["AggTeamRallyWeightStrength"] = 35, ["AggTeamRallyWeightNeed"] = 25,
				["AggTeamRallyStrengthTargetPercent"] = 200, ["AggTeamRallyNeedFightPoints"] = 1000, ["AggTeamRallyNeedDefendPoints"] = 800,
				["AggTeamRallyNeedMarchPoints"] = 600, ["AggTeamRallyNeedIdlePoints"] = 100, ["AggTeamRallyAllyPenaltyPoints"] = 50, ["AggTeamRallyMinScore"] = 450,
				["AggTeamRallyMinJoinRatioPercent"] = 100, ["AggTeamRallyDangerScanRadiusCells"] = 15, ["AggTeamRallyDestRadiusCells"] = 12,
				["AggTeamRallyAwayGainCells"] = 2, ["AggTeamRallyMinSquadValue"] = 1000, ["AggTeamRallyMinCommitTicks"] = 250, ["AggTeamRallyRescoreTicks"] = 100,
				["AggTeamRallySwitchMarginPercent"] = 25, ["AggTeamRallyMaxTicks"] = 900, ["AggTeamRallySupportMaxTicks"] = 600, ["AggTeamRallyCooldownTicks"] = 750,
				["AggTeamRallyRetreatMarkTicks"] = 400, ["AggTeamRallyMaxHops"] = 1, ["AggTeamRallyEpisodeResetTicks"] = 1500, ["AggTeamRallyBlacklistTicks"] = 3000,
				["AggTeamRallyFallbackDepthCells"] = 6, ["AggTeamRallyHumanAllies"] = 0,
			};
			Assert.That(expected.Count, Is.EqualTo(Policy.Ranges.Length));
			foreach (var (key, value) in expected)
				Assert.That(info.RetreatRallyValue(key), Is.EqualTo(value), key);
			Assert.That(Policy.Ranges.Single(r => r.Key == "AggTeamRallyFallbackDepthCells"), Is.EqualTo(("AggTeamRallyFallbackDepthCells", -20, 20)), "the depth sign is allowed");
			Assert.That(Policy.Ranges.Select(r => r.Key), Does.Contain("AggTeamRallyDangerScanRadiusCells"), "L2");
			Assert.That(Policy.Ranges.Select(r => r.Key), Does.Contain("AggTeamRallyRetreatMarkTicks"), "M2");
		}

		[Test]
		public void TheRangeCheckRejectsEveryValueOutsideItsRangeAndAcceptsBothEnds()
		{
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var (key, min, max) in Policy.Ranges)
			{
				Assert.That(Policy.RangeError(k => k == key ? min - 1 : info.RetreatRallyValue(k)), Does.Contain(key), key + " below");
				Assert.That(Policy.RangeError(k => k == key ? max + 1 : info.RetreatRallyValue(k)), Does.Contain(key), key + " above");
				Assert.That(Policy.RangeError(k => k == key ? min : info.RetreatRallyValue(k)), Is.Null.Or.Contain("not all be zero"), key + " min");
				Assert.That(Policy.RangeError(k => k == key ? max : info.RetreatRallyValue(k)), Is.Null, key + " max");
			}

			Assert.That(Policy.RangeError(k => k == "AggTeamRallyFallbackDepthCells" ? -20 : info.RetreatRallyValue(k)), Is.Null, "a negative depth is legal");
		}

		[Test]
		public void NoPersonalityArmsTheFlagOrAnyRallyRow()
		{
			var text = Fixture("mcv_deploy_ai.yaml");
			Assert.That(text, Does.Contain("RetreatRallyEnabled: true"), "only the switch group CH arms it (ported ON values)");
			Assert.That(text, Does.Not.Contain("AggTeamRally"), "v1 ships no personality rows");
			var nodes = MiniYaml.FromString(text, "ai").Single(n => n.Key == "Player").Value.Nodes;
			foreach (var n in nodes.Where(n => n.Key.StartsWith("SquadManagerBotModuleCA", StringComparison.Ordinal)))
				Assert.That(FieldLoader.Load<SquadManagerBotModuleCAInfo>(n.Value).RetreatRallyEnabled, Is.EqualTo(n.Key != "SquadManagerBotModuleCA@classic"), n.Key);
		}

		// ---- 13. M3 ----

		[Test]
		public void AnOwnIdleSquadIsMergedIntoAnAllyIdleOneIsExcludedAnEngagedAllyIsSupportedAndARequestIsSupported()
		{
			Assert.That(Policy.ActionFor(C(idle: true)), Is.EqualTo(Policy.RallyAction.Merge));
			Assert.That(Policy.Exclusion(C(idle: true, ally: true), Defaults), Is.EqualTo("idle_ally"));
			Assert.That(Policy.ActionFor(C(ally: true, need: Policy.RallyNeed.Fight)), Is.EqualTo(Policy.RallyAction.Support));
			Assert.That(Policy.ActionFor(C(request: true, ally: true, need: Policy.RallyNeed.Defend)), Is.EqualTo(Policy.RallyAction.Support), "a defence request is supported at its position");
			Assert.That(Policy.ActionFor(C(idle: true, request: true)), Is.EqualTo(Policy.RallyAction.Support));
			Assert.That(Policy.Exclusion(C(request: true, ally: true, need: Policy.RallyNeed.Defend), Defaults), Is.Null);
			Assert.That(Policy.Choose(new[] { C(idle: true, ally: true), C("b/", 2, ally: true, d: 40) }, 1000, Defaults with { MinScore = 0 }).Pick.SquadId, Is.EqualTo(2), "the allied idle squad never wins");
		}

		[Test]
		public void AnOwnIdleSquadThatLaunchedIsSupportedAndOneThatIsGoneDowngrades()
		{
			var launched = Policy.Step(S(action: Policy.RallyAction.Merge, launched: true, arrived: true));
			Assert.That(launched.Action, Is.EqualTo(Policy.RallyAction.Support));
			Assert.That(launched.Phase, Is.EqualTo(Policy.RallyPhase.Support));
			Assert.That(launched.End, Is.EqualTo(Policy.RallyEnd.None));
			Assert.That(Policy.Step(S(action: Policy.RallyAction.Merge, launched: true)).Action, Is.EqualTo(Policy.RallyAction.Support), "still marching, now toward an army");
			var gone = Policy.Step(S(action: Policy.RallyAction.Merge, invalid: true));
			Assert.That(Policy.NextState(gone.End, gone.Phase), Is.EqualTo(Policy.RallyNext.FleeBaseOnly));
		}

		[Test]
		public void AnOwnIdleSquadBeatsAFartherFightAtEqualFootingBecauseItEndsInThePool()
		{
			var idle = C("a/", 1, idle: true, need: Policy.RallyNeed.Idle, d: 10);
			Assert.That(Score(idle), Is.EqualTo(708));
			Assert.That(Policy.Choose(new[] { idle, C("b/", 2, d: 55) }, 1000, Defaults).Pick, Is.EqualTo(idle), "intended: a merge, not an idle squad beside an army (M3)");
		}

		// ---- 14. M4 ----

		[Test]
		public void EveryEndMapsToItsNextState()
		{
			foreach (var phase in new[] { Policy.RallyPhase.March, Policy.RallyPhase.Support, Policy.RallyPhase.Merge })
			{
				Assert.That(Policy.NextState(Policy.RallyEnd.MergedPool, phase), Is.EqualTo(Policy.RallyNext.DismissPool), "merged_pool " + phase);
				Assert.That(Policy.NextState(Policy.RallyEnd.TargetGone, phase), Is.EqualTo(Policy.RallyNext.Idle), "target_gone " + phase);
				Assert.That(Policy.NextState(Policy.RallyEnd.AbortLosing, phase), Is.EqualTo(Policy.RallyNext.FleeBaseOnly), "abort_losing " + phase);
				Assert.That(Policy.NextState(Policy.RallyEnd.Detached, phase), Is.EqualTo(Policy.RallyNext.None));
				Assert.That(Policy.NextState(Policy.RallyEnd.Dead, phase), Is.EqualTo(Policy.RallyNext.None));
				Assert.That(Policy.NextState(Policy.RallyEnd.None, phase), Is.EqualTo(Policy.RallyNext.None));
			}

			Assert.That(Policy.NextState(Policy.RallyEnd.Expired, Policy.RallyPhase.March), Is.EqualTo(Policy.RallyNext.FleeBaseOnly));
			Assert.That(Policy.NextState(Policy.RallyEnd.Expired, Policy.RallyPhase.Support), Is.EqualTo(Policy.RallyNext.Idle));
		}

		[Test]
		public void TheTelemetrySpellingOfEndsAndPhases()
		{
			Assert.That(new[] { Policy.RallyEnd.MergedPool, Policy.RallyEnd.TargetGone, Policy.RallyEnd.Expired, Policy.RallyEnd.AbortLosing, Policy.RallyEnd.Detached, Policy.RallyEnd.Dead }
				.Select(Policy.EndName), Is.EqualTo(new[] { "merged_pool", "target_gone", "expired", "abort_losing", "detached", "dead" }));
			Assert.That(Policy.PhaseName(Policy.RallyPhase.March), Is.EqualTo("march"));
			Assert.That(Policy.PhaseName(Policy.RallyPhase.Support), Is.EqualTo("support"));
			Assert.That(Policy.PhaseName(Policy.RallyPhase.Merge), Is.EqualTo("march"), "DRAFT02 section 6 lists march|support: a merge ends the march leg");
		}

		[Test]
		public void TheApproachPointStopsDestRadiusInFrontOfTheDestinationOnTheLineToTheSquad()
		{
			var dest = new WPos(0, 0, 0);
			var squad = new WPos(40 * 1024, 0, 0);
			Assert.That(Policy.ApproachPoint(squad, dest, 12), Is.EqualTo(new WPos(12 * 1024, 0, 0)));
			var inside = new WPos(5 * 1024, 0, 0);
			Assert.That(Policy.ApproachPoint(inside, dest, 12), Is.EqualTo(inside), "a squad inside the radius stays");
			Assert.That(Policy.Arrived(14 * 1024, 12), Is.True, "radius plus the 2-cell slack");
			Assert.That(Policy.Arrived(14 * 1024 + 1, 12), Is.False);
		}

		// ---- 15. H1 pin ----

		[Test]
		public void H1TheFleeStateAsksOnceBeforeTheLegacyFlightAndHandsOffWithoutDismissing()
		{
			var flee = Section(GroundStates(), "class GroundUnitsFleeStateCA", "class HarasserUnitsIdleStateCA");
			Assert.That(flee, Does.Contain("readonly bool baseOnly;"));
			Assert.That(flee, Does.Contain("bool handoff;"), "an instance field of the Flee state");
			Assert.That(flee, Does.Contain("public GroundUnitsFleeStateCA(bool baseOnly = false)"));
			var tick = Section(flee, "public void Tick(SquadCA owner)", "public void Deactivate(SquadCA owner)");
			var note = tick.IndexOf("owner.SquadManager.RetreatRallyNoteFlight(owner);", StringComparison.Ordinal);
			var ask = tick.IndexOf("if (!baseOnly && owner.SquadManager.TryRetreatRally(owner, \"flee_state\", out var plan))", StringComparison.Ordinal);
			var legacy = tick.IndexOf("GoToRandomOwnBuilding(owner, fleeing: true);", StringComparison.Ordinal);
			Assert.That(note, Is.GreaterThanOrEqualTo(0));
			Assert.That(ask, Is.GreaterThan(note), "the mark comes first, then the guarded ask");
			Assert.That(legacy, Is.GreaterThan(ask), "the legacy flight follows the ask");
			Assert.That(tick, Does.Contain("handoff = true;"));
			Assert.That(tick, Does.Contain("ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);"));
			Assert.That(tick, Does.Contain("ChangeState(owner, new GroundUnitsIdleStateCA(), true);"), "the legacy end is unchanged");
			var deactivate = flee.Substring(flee.IndexOf("public void Deactivate(SquadCA owner)", StringComparison.Ordinal));
			Assert.That(deactivate.IndexOf("if (handoff)", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0));
			Assert.That(deactivate.IndexOf("if (handoff)", StringComparison.Ordinal), Is.LessThan(deactivate.IndexOf("DismissSquad(owner)", StringComparison.Ordinal)), "ChangeState calls the old Deactivate first");
			Assert.That(Source("mods/CA/Traits/BotModules/Squads/SquadCA.cs"), Does.Not.Contain("RallyHandoff"), "L5: no field on the squad");
			Assert.That(Count(GroundStates(), "new GroundUnitsFleeStateCA()"), Is.EqualTo(7), "the 7 legacy entries are unchanged");
		}

		// ---- 16. H2 pin ----

		[Test]
		public void H2BothIdleFleeSitesMarkAndAskAfterTheRetreatStartRowAndBeforeTheLegacyRetreat()
		{
			var idle = Section(GroundStates(), "class GroundUnitsIdleStateCA", "class GroundUnitsStageStateCA");
			Assert.That(Count(idle, "RetreatRallyNoteFlight(owner);"), Is.EqualTo(2));
			Assert.That(Count(idle, "TryRetreatRally(owner, "), Is.EqualTo(2));
			var pos = 0;
			for (var i = 0; i < 2; i++)
			{
				var start = idle.IndexOf("NoteRetreatStart(owner, ", pos, StringComparison.Ordinal);
				var note = idle.IndexOf("RetreatRallyNoteFlight(owner);", start, StringComparison.Ordinal);
				var ask = idle.IndexOf("TryRetreatRally(owner, ", note, StringComparison.Ordinal);
				var legacy = idle.IndexOf("Retreat(owner, flee: true, rearm: true, repair: true);", ask, StringComparison.Ordinal);
				Assert.That(start, Is.GreaterThanOrEqualTo(pos));
				Assert.That(note, Is.GreaterThan(start));
				Assert.That(ask, Is.GreaterThan(note));
				Assert.That(legacy, Is.GreaterThan(ask));
				Assert.That(idle.Substring(ask, legacy - ask), Does.Contain("ChangeState(owner, new GroundUnitsRallyStateCA(plan), false);"));
				pos = legacy;
			}

			Assert.That(idle, Does.Contain("TryRetreatRally(owner, \"idle_engage\", out var plan)"));
			Assert.That(idle, Does.Contain("TryRetreatRally(owner, site, out var plan)"), "the agg-core site keeps its regroup-expired reason");
			Assert.That(Count(GroundStates(), "TryRetreatRally("), Is.EqualTo(3), "two idle sites and the Flee state");
			Assert.That(Count(GroundStates(), "RetreatRallyNoteFlight("), Is.EqualTo(3));
		}

		// ---- 17. H3 pin ----

		[Test]
		public void H3TheRallyStateCountsAsRetreatingForLivenessAndIsNotQuiet()
		{
			var text = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			Assert.That(text, Does.Contain("or AirFleeStateCA or StealthFleeStateCA or UnitsForProtectionFleeState or GroundUnitsRallyStateCA;"));
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs"),
				Does.Contain("internal static bool AggTeamIsQuietState(IState state) => state is GroundUnitsIdleStateCA;"), "unchanged");
			var rally = new GroundUnitsRallyStateCA(new RetreatRallyPlan());
			Assert.That(SquadManagerBotModuleCA.AggTeamIsQuietState(rally), Is.False, "A4, A5 and the A3 merge leave a rallying squad alone");
			Assert.That(SquadManagerBotModuleCA.AggTeamIsAttackState(rally), Is.False);
			Assert.That(AggressionPolicyCA.IsEngagedState(rally), Is.False, "a rallying squad is not an engaged ally");
			Assert.That(SquadManagerBotModuleCA.StalemateSquadQuiet(SquadCAType.Rush, rally, false), Is.False, "nor a quiet pool member of the breaker");
		}

		// ---- 18. H4 pin ----

		[Test]
		public void H4TheAxisCellIsReachedOnlyByAFleeingRushOrGuerrillaSquadAfterTheStagingBranch()
		{
			var text = StateBase();
			Assert.That(text, Does.Contain("protected static CPos HomeLocation(SquadCA squad, bool fleeing = false)"));
			Assert.That(text, Does.Contain("protected static void GoToRandomOwnBuilding(SquadCA squad, bool fleeing = false)"));
			var home = Section(text, "protected static CPos HomeLocation(", "protected static CPos RandomBuildingLocation(");
			var staging = home.IndexOf("StagingCellNear(from)", StringComparison.Ordinal);
			var axis = home.IndexOf("if (fleeing && (squad.Type == SquadCAType.Rush || squad.Type == SquadCAType.Guerrilla))", StringComparison.Ordinal);
			var random = home.IndexOf("return RandomBuildingLocation(squad);", StringComparison.Ordinal);
			Assert.That(staging, Is.GreaterThanOrEqualTo(0));
			Assert.That(axis, Is.GreaterThan(staging), "after the staging branch");
			Assert.That(random, Is.GreaterThan(axis), "before the legacy random building");
			Assert.That(home, Does.Contain("squad.SquadManager.RetreatRallyBaseCell(squad)"));
			Assert.That(Section(text, "protected static void GoToRandomOwnBuilding(", "// DESIGN 19.12").Replace("\n", " "), Does.Contain("HomeLocation(squad, fleeing)"));
			Assert.That(text, Does.Contain("var home = HomeLocation(squad, flee);"), "Retreat() passes its flee flag");
			Assert.That(Count(text, "HomeLocation(squad)"), Is.EqualTo(0), "no caller of the old shape is left in the state base");

			var ground = GroundStates();
			Assert.That(Count(ground, "GoToRandomOwnBuilding(owner, fleeing: true)"), Is.EqualTo(1), "only the Flee state");
			Assert.That(Count(ground, "GoToRandomOwnBuilding(owner);"), Is.EqualTo(2), "the Support and FireSupport idle states keep the default (L1)");
			Assert.That(Count(ground, "HomeLocation(owner)"), Is.EqualTo(1), "GroundStatesCA:1647 keeps the default");
			Assert.That(Count(ground, "fleeing: true"), Is.EqualTo(1));
			foreach (var file in new[] { "ProtectionStatesCA.cs", "StealthStatesCA.cs", "NavyStatesCA.cs" })
			{
				var other = Source("mods/CA/Traits/BotModules/Squads/States/" + file);
				Assert.That(other, Does.Not.Contain("fleeing:"), file);
				Assert.That(other, Does.Contain("GoToRandomOwnBuilding(owner);"), file);
			}
		}

		// ---- 19. OFF identity ----

		static SquadManagerBotModuleCA Shell(bool aggTeam, bool rally, bool breaker, bool aggression, bool predictor)
		{
			var info = new SquadManagerBotModuleCAInfo();
			void Set(string name, bool value) => typeof(SquadManagerBotModuleCAInfo).GetField(name).SetValue(info, value);
			Set(nameof(SquadManagerBotModuleCAInfo.AggTeamEnabled), aggTeam);
			Set(nameof(SquadManagerBotModuleCAInfo.RetreatRallyEnabled), rally);
			Set(nameof(SquadManagerBotModuleCAInfo.StalemateBreakerEnabled), breaker);
			Set(nameof(SquadManagerBotModuleCAInfo.AggressionEnabled), aggression);
			Set(nameof(SquadManagerBotModuleCAInfo.UseCombatPredictor), predictor);
			var m = (SquadManagerBotModuleCA)RuntimeHelpers.GetUninitializedObject(typeof(SquadManagerBotModuleCA));
			typeof(SquadManagerBotModuleCA).BaseType.GetField("Info", Any).SetValue(m, info);
			return m;
		}

		[Test]
		public void RetreatRallyOnNeedsTheAgentTeamFlagTheRallyFlagAndTheAggressionGate()
		{
			for (var mask = 0; mask < 32; mask++)
			{
				bool Bit(int i) => (mask & (1 << i)) != 0;
				var m = Shell(Bit(0), Bit(1), Bit(2), Bit(3), Bit(4));
				var expected = Bit(0) && Bit(1) && Bit(2) && Bit(3) && Bit(4);
				Assert.That(m.RetreatRallyOn, Is.EqualTo(expected), $"aggTeam={Bit(0)} rally={Bit(1)} breaker={Bit(2)} aggression={Bit(3)} predictor={Bit(4)}");
			}
		}

		[Test]
		public void OffTheEntryPointsReturnAtOnceTouchNothingAndDrawNoRandomNumber()
		{
			foreach (var m in new[] { Shell(false, false, false, false, false), Shell(true, false, true, true, true), Shell(false, true, true, true, true), Shell(true, true, false, true, true),
				Shell(true, true, true, false, true), Shell(true, true, true, true, false) })
			{
				// No world, player, bot or RNG exists on the shell: any read of them would throw.
				var args = new object[] { null, "flee_state", null };
				Assert.That(typeof(SquadManagerBotModuleCA).GetMethod("TryRetreatRally", Any).Invoke(m, args), Is.EqualTo(false));
				Assert.That(args[2], Is.Null);
				m.RetreatRallyNoteFlight(null);
				Assert.That(m.RetreatRallyBaseCell(null), Is.Null);
				Assert.That(m.RetreatRallyBaseCellPeek(null), Is.Null);
				m.RetreatRallyDetach(null);
				Assert.That(m.RetreatRallyMarchFlee(null, null), Is.False);
				Assert.That(m.RetreatRallyObserve(null, null, Policy.RallyPhase.March, Policy.RallyAction.Support), Is.EqualTo(default(RetreatRallyObservation)));
				Assert.That(m.RetreatRallyRescore(null, null, false), Is.Null);
				m.RetreatRallyArrive(null, null, Policy.RallyAction.Support, 0);
				m.RetreatRallyEnd(null, null, Policy.RallyEnd.Expired, Policy.RallyPhase.March);
				Assert.That(m.RetreatRallyParam("AggTeamRallyMinScore", null), Is.EqualTo(0));
				foreach (var name in new[] { "RallyPicks", "RallyBases", "RallyArrivals", "RallyEnds", "RallyDowngrades", "RallyMerges", "RallyHopBlocks", "RallyPingPongBlocks" })
					Assert.That((int)typeof(SquadManagerBotModuleCA).GetField(name, Any).GetValue(m), Is.EqualTo(0), name);
				Assert.That(typeof(SquadManagerBotModuleCA).GetField("retreatRallyNextId", Any).GetValue(m), Is.EqualTo(0), "no squad id was handed out");
			}
		}

		[Test]
		public void H19EveryEntryPointStartsWithTheGateAndTheNewFilesDrawNoRandomNumber()
		{
			var text = Manager();
			Assert.That(Section(text, "internal bool TryRetreatRally(", "string why = null;"), Does.Contain("plan = null;\n\t\t\tif (!RetreatRallyOn)\n\t\t\t\treturn false;"));
			Assert.That(Section(text, "internal void RetreatRallyNoteFlight(SquadCA squad)", "var now"), Does.Contain("{\n\t\t\tif (!RetreatRallyOn)\n\t\t\t\treturn;"));
			Assert.That(Section(text, "internal CPos? RetreatRallyBaseCell(SquadCA squad)", "var now"), Does.Contain("{\n\t\t\tif (!RetreatRallyOn)\n\t\t\t\treturn null;"));
			foreach (var file in new[] { text, State(), PolicySource(), InfoSource() })
			{
				Assert.That(file, Does.Not.Contain("BotRng"));
				Assert.That(file, Does.Not.Contain("MersenneTwister"));
				Assert.That(Regex.IsMatch(file, @"\bRandom\w*\b|\.Random\b|RandomOrDefault"), Is.False, "the module draws no random numbers");
				Assert.That(file, Does.Not.Contain("System.Random"));
			}

			Assert.That(text, Does.Not.Contain("RandomBuildingLocation"));
		}

		// ---- successor 02: LOW-1 (a diagnostic never changes a decision) and LOW-2 (detached is booked) ----

		[Test]
		public void TheRallyBaseRowReadsTheAxisWithoutWritingTheEpisodeCache()
		{
			var text = Manager();
			var ask = Section(text, "internal bool TryRetreatRally(", "static string RallyNeedName(");
			Assert.That(ask, Does.Contain("var axis = RetreatRallyBaseCellPeek(squad);"), "the telemetry row uses the read-only variant");
			Assert.That(Regex.IsMatch(ask, @"RetreatRallyBaseCell\("), Is.False, "the decision path never calls the cache-writing variant: CE on and CE off leave the same cache state");
			var peek = Section(text, "internal CPos? RetreatRallyBaseCellPeek(SquadCA squad)", "CPos? RallyAxisCompute(SquadCA squad)");
			Assert.That(peek, Does.Not.Contain(".Axis ="));
			Assert.That(peek, Does.Not.Contain(".AxisTick ="));
			Assert.That(peek, Does.Not.Contain("RallyEpisodes.GetValue"), "TryGetValue only: no episode is created either");
			Assert.That(peek, Does.Contain("RallyEpisodes.TryGetValue(squad, out var episode)"));
			Assert.That(peek, Does.Contain("return RallyAxisCompute(squad);"));
			var compute = Section(text, "CPos? RallyAxisCompute(SquadCA squad)", "/// <summary>Where the enemy is, for the axis");
			Assert.That(compute, Does.Not.Contain("episode"));
			Assert.That(compute, Does.Not.Contain("RallyEpisodes"));
			Assert.That(compute, Does.Not.Contain("Tick ="));
			var cached = Section(text, "internal CPos? RetreatRallyBaseCell(SquadCA squad)", "internal CPos? RetreatRallyBaseCellPeek(");
			Assert.That(cached, Does.Contain("var cell = RallyAxisCompute(squad);"), "the flee path and the row share one computation");
			Assert.That(Count(cached, "episode.Axis = cell;"), Is.EqualTo(1));
			Assert.That(Count(text, "episode.Axis = "), Is.EqualTo(2), "one cache write in the flee path and the episode reset to null");
			Assert.That(Count(text, "episode.AxisTick = "), Is.EqualTo(2));
		}

		[Test]
		public void AnOpenRallyIsBookedAsDetachedWhenTheSquadIsDismissedFromOutside()
		{
			var manager = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.cs");
			var dismiss = Section(manager, "public void DismissSquad(SquadCA squad)", "var leases = BotUnitLeases.Of(Player);");
			Assert.That(dismiss, Does.Contain("{\n\t\t\tRetreatRallyDetach(squad);\n\t\t\tDiagnosticSquadEvent(squad, \"disband\");"), "first, before the units are cleared");
			Assert.That(Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs"), Does.Contain("DismissSquad(s);"), "the detachment stands a squad down through DismissSquad, not through its state");
			var text = Manager();
			var detach = Section(text, "internal void RetreatRallyDetach(SquadCA squad)", "/// <summary>Arrival at the destination:");
			Assert.That(detach, Does.Contain("if (!RetreatRallyOn)\n\t\t\t\treturn;"), "OFF: the first line returns");
			Assert.That(detach, Does.Contain("squad?.FuzzyStateMachine?.CurrentState is GroundUnitsRallyStateCA rally"));
			Assert.That(detach, Does.Contain("rally.Deactivate(squad);"));
			var state = State();
			var deactivate = state.Substring(state.IndexOf("public void Deactivate(SquadCA owner)", StringComparison.Ordinal));
			Assert.That(deactivate, Does.Contain("if (ended)\n\t\t\t\treturn;\n\n\t\t\tended = true;\n\t\t\towner.SquadManager.RetreatRallyEnd(owner, plan, Policy.RallyEnd.Detached, phase);"), "an ended rally (merged_pool dismisses through here) is not booked twice");
			Assert.That(Policy.NextState(Policy.RallyEnd.Detached, Policy.RallyPhase.Support), Is.EqualTo(Policy.RallyNext.None));
			Assert.That(Policy.EndName(Policy.RallyEnd.Detached), Is.EqualTo("detached"));
			var end = Section(text, "internal void RetreatRallyEnd(", "// ---- the fallback base cell ----");
			Assert.That(end, Does.Contain("RallyCooldowns.GetValue(owner, _ => new StrongBox<int>()).Value = until;"), "a detached rally still cools the squad and its destination");
			Assert.That(end.IndexOf("RallyEnds++;", StringComparison.Ordinal), Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void TheFleeStateWithDefaultsIsTheOldBodyAndTheStateBaseDefaultsAreTheOldBehaviour()
		{
			var flee = new GroundUnitsFleeStateCA();
			Assert.That(typeof(GroundUnitsFleeStateCA).GetField("baseOnly", Any).GetValue(flee), Is.EqualTo(false));
			Assert.That(typeof(GroundUnitsFleeStateCA).GetField("handoff", Any).GetValue(flee), Is.EqualTo(false));
			Assert.That(typeof(GroundUnitsFleeStateCA).GetField("baseOnly", Any).GetValue(new GroundUnitsFleeStateCA(true)), Is.EqualTo(true));
			Assert.That(typeof(GroundUnitsFleeStateCA).GetConstructor(new[] { typeof(bool) }).GetParameters()[0].HasDefaultValue, Is.True);
			Assert.That(typeof(SquadCA).GetField("RetreatSinceTick", Any).GetValue(RuntimeHelpers.GetUninitializedObject(typeof(SquadCA))), Is.EqualTo(0),
				"(an uninitialized shell holds 0; the field initializer gives -1 on a real squad)");
			Assert.That(Source("mods/CA/Traits/BotModules/Squads/SquadCA.cs"), Does.Contain("internal int RetreatSinceTick = -1;"));
		}

		// ---- 20. allied reading ----

		[Test]
		public void TheCollectorReadsOwnAndAlliedBotManagersInProcessAndNeverTheWorldOrTheEnemy()
		{
			var text = Manager();
			var collect = Section(text, "RallyScan RallyCollect(", "static bool RallyBlacklisted(");
			Assert.That(collect, Does.Contain("if (pl != Player && !(pl.IsBot && Player.IsAlliedWith(pl)))"));
			Assert.That(collect, Does.Contain("pl.PlayerActor.TraitsImplementing<SquadManagerBotModuleCA>()"));
			Assert.That(collect, Does.Contain("if (m.IsTraitDisabled)"));
			Assert.That(collect, Does.Contain("foreach (var s in m.Squads)"));
			Assert.That(collect, Does.Contain("RallyIsGroundType(s) || !s.IsValid"));
			Assert.That(text, Does.Not.Contain("PlayerRelationship.Enemy"));
			Assert.That(text, Does.Not.Contain("World.Actors"), "no world-wide actor scan: candidates come from the squad lists");
			Assert.That(collect, Does.Not.Contain("FindActorsInCircle"), "the candidate list itself is never a spatial actor scan");
			Assert.That(text, Does.Contain("(a.Owner.IsBot || humanAllies)"), "human units only behind AggTeamRallyHumanAllies");
			Assert.That(text, Does.Contain("RallyP(nameof(SquadManagerBotModuleCAInfo.AggTeamRallyHumanAllies), squad) != 0"));
			Assert.That(text, Does.Contain("IsPreferredObservedEnemyUnit(a)"), "enemies only as observed");
			Assert.That(text, Does.Contain("TeamBlackboard.CollectBroadcasts(Player)"), "ally defence requests come from the blackboard");
			Assert.That(collect, Does.Contain("b.RequestsDefence && b.DefendPosition != WPos.Zero"));
			Assert.That(collect, Does.Contain("s.Type == SquadCAType.Rush"), "an own idle destination is a Rush squad (the A3 mechanism)");
		}

		// ---- 21. M1 gate ----

		[Test]
		public void M1TheGateNamesTheThreeFlagsAndEveryAggressionCallIsBehindIt()
		{
			var text = Manager();
			Assert.That(text, Does.Contain("internal bool RetreatRallyOn => Info.AggTeamEnabled && Info.RetreatRallyEnabled && AggressionOn && !IsTraitDisabled;"));
			// Every method of the new partial that calls an agg-core member (AggressionRatio, AggressionFriends, AggressionParam, AggressionCommit) starts with the gate.
			var methods = Regex.Matches(text, @"\n\t\t(?:internal |static )?[A-Za-z<>?\.\(\), ]+ (?<name>\w+)\((?<args>[^)]*)\)\n\t\t\{\n(?<body>(?:.|\n)*?)\n\t\t\}\n");
			var checkedMethods = 0;
			foreach (Match m in methods)
			{
				var body = m.Groups["body"].Value;
				if (!Regex.IsMatch(body, @"\bAggression(Ratio|Friends|Param|Commit|HoldsInFight|AllyEngagedUnits)\("))
					continue;

				checkedMethods++;
				var firstStatement = body.TrimStart('\t', '\n').Split('\n')[0];
				var gate = body.IndexOf("if (!RetreatRallyOn)", StringComparison.Ordinal);
				var call = Regex.Match(body, @"\bAggression(Ratio|Friends|Param|Commit|HoldsInFight|AllyEngagedUnits)\(").Index;
				Assert.That(gate, Is.GreaterThanOrEqualTo(0), m.Groups["name"].Value + " has no gate: " + firstStatement);
				Assert.That(gate, Is.LessThan(call), m.Groups["name"].Value + " calls an agg-core member before the gate");
			}

			Assert.That(checkedMethods, Is.GreaterThanOrEqualTo(4), "march_flee, join, observe/arrive, commit");
			Assert.That(Section(text, "string RallyWorldGuard(", "var rc = scan.ByKey"), Does.Contain("if (!RetreatRallyOn)\n\t\t\t\treturn null;"), "the world guard is gated too");
			Assert.That(State(), Does.Not.Contain("Aggression"), "the state reaches agg-core only through the manager's gated members");
		}

		[Test]
		public void M1TheSwitchGroupNeedsCgAndCfIsARestraintAndSetsOnlyTheFlag()
		{
			var yaml = Fixture("increment_switches.yaml");
			Assert.That(yaml, Does.Contain("\n  CH_retreat_rally: [CG_agg_team, CF_agg_core] "));
			Assert.That(yaml, Does.Contain("\n  CH_retreat_rally: restraint "));
			Assert.That(yaml, Does.Contain("\n  CH_retreat_rally: "));
			var groups = yaml.Substring(yaml.IndexOf("\ngroups:", StringComparison.Ordinal));
			var group = groups.Substring(groups.IndexOf("\n  CH_retreat_rally:", StringComparison.Ordinal)).TrimEnd();

			// STALL-FIX-01: CH is no longer the last group — cut the slice at the next group header.
			var next = Regex.Match(group.Substring(1), @"\n  \w+:");
			if (next.Success)
				group = group.Substring(0, next.Index + 1);

			Assert.That(group, Does.Contain("SquadManagerBotModuleCA:\n      RetreatRallyEnabled: true"));
			Assert.That(Count(group, "true"), Is.EqualTo(1).Or.GreaterThanOrEqualTo(1));
			Assert.That(Regex.Matches(group, @"\n      \w+:").Count, Is.EqualTo(1), "sets exactly one field");
			Assert.That(group, Does.Not.Contain("MasterAiBotModule"));
		}

		// ---- 22. Q4 ----

		[Test]
		public void Q4TheSquadStateMachineIsSkippedUnderSquadDesireSoA10IsInertThere()
		{
			var text = Source("mods/CA/Traits/BotModules/Squads/SquadCA.cs");
			const string Message = "A10 is inert under BP_squad_desire: revisit if BP is armed";
			Assert.That(text, Does.Contain("var desire = SquadManager.Info.UseSquadDesire && SquadDesireOrders.Supports(Type)\n\t\t\t\t\t? SquadManager.SquadDesire : null;"), Message);
			Assert.That(text, Does.Contain("if (desire != null)\n\t\t\t\t\t(desireController ??= new SquadDesireController<Actor>()).Tick(CreateDesireExecution(), desire);\n\t\t\t\telse\n\t\t\t\t\tFuzzyStateMachine.Update(this);"), Message);
			Assert.That(Source("mods/CA/Traits/BotModules/Squads/SquadDesireOrders.cs"), Does.Not.Contain("RetreatRally"), "no SquadDesireOrders edit");
		}

		// ---- 23. telemetry ----

		[Test]
		public void TheEventNamesAreLiteralsEmittedOnceEachBehindTheDiagnosticsFlagWithTheDestinationCell()
		{
			var text = Manager();
			foreach (var name in new[] { "rally_pick", "rally_base", "rally_arrive", "rally_switch", "rally_end" })
			{
				Assert.That(Count(text, "\"" + name + "\""), Is.EqualTo(1), name);
				foreach (var other in new[] { State(), PolicySource(), GroundStates() })
					Assert.That(other, Does.Not.Contain("\"" + name + "\""), name);
			}

			foreach (Match m in Regex.Matches(text, @"DiagnosticSquadEventAt\("))
			{
				var before = text.Substring(Math.Max(0, m.Index - 160), Math.Min(160, m.Index));
				Assert.That(before, Does.Contain("Info.StalemateDiagnosticsEnabled"), "every emit is behind the telemetry flag");
			}

			Assert.That(Count(text, "DiagnosticSquadEventAt("), Is.EqualTo(5));
			var stalemate = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate.cs");
			Assert.That(stalemate, Does.Contain("void DiagnosticSquadEventAt(SquadCA squad, string evt, string reason, CPos? cell) => DiagnosticSquadEventWith(squad, evt, reason, null, cell);"));
			Assert.That(stalemate, Does.Contain("var cell = cellOverride ?? (squad.Target.Type == TargetType.Invalid ? (CPos?)null : World.Map.CellContaining(squad.Target.CenterPosition));"));
			Assert.That(stalemate, Does.Contain("void DiagnosticSquadEvent(SquadCA squad, string evt, string reason = null) => DiagnosticSquadEventWith(squad, evt, reason, null);"), "the existing overload is unchanged");
		}

		[Test]
		public void ThePayloadKeysAreExactlyTheDocumentedOnes()
		{
			var text = Manager();
			Assert.That(text, Does.Contain("dest={plan.DestName};score={choice.Score};d={choice.Pick.DistanceCells};str={choice.Pick.Strength};need={RallyNeedName(choice.Pick.Need)};n={choice.Considered};site={site}"));
			Assert.That(text, Does.Contain("why={why};best={best};axis="));
			Assert.That(text, Does.Contain("dest={plan.DestName};ratio={ratioMilli}"));
			Assert.That(text, Does.Contain("from={plan.Key};to={next.Key};gain={choice.Score - oldScore}"));
			Assert.That(text, Does.Contain("end={Policy.EndName(end)};phase={Policy.PhaseName(phase)};ticks={now - plan.StartTick};size0={plan.Size0};size1={owner.Units.Count};v0={plan.V0};v1={RallyUnitValue(owner)}"));
			Assert.That(text, Does.Contain("c.DefenceRequest ? \"defreq\" : c.Ally ? \"ally:\" + rc.PlayerKey : c.Idle ? \"own_idle\" : \"own\""));
			Assert.That(text, Does.Contain("Fight => \"fight\"").Or.Contain("Policy.RallyNeed.Fight => \"fight\""));
			Assert.That(Count(text, "FormattableString.Invariant"), Is.EqualTo(1), "one invariant formatter for every payload");
		}

		// ---- 24. orders ----

		[Test]
		public void TheMarchLegIsAMoveAndTheSupportLegAnAttackMoveBothThroughQueueDeduped()
		{
			var text = State();
			Assert.That(text, Does.Contain("QueueDeduped(owner, \"AttackMove\", SquadOrderKey.ForCell(\"AttackMove\", cell), Target.FromCell(owner.World, cell), members);"));
			Assert.That(text, Does.Contain("QueueDeduped(owner, \"Move\", SquadOrderKey.ForCell(\"Move\", cell), Target.FromCell(owner.World, cell), members);"));
			Assert.That(Count(text, "QueueDeduped("), Is.EqualTo(2));
			Assert.That(text, Does.Not.Contain("new Order("), "no order outside the dedup helper");
			Assert.That(text, Does.Not.Contain("QueueOrder("));
			var order = Section(text, "void Order(SquadCA owner, WPos destination)", "void End(SquadCA owner");
			Assert.That(order.IndexOf("if (support)", StringComparison.Ordinal), Is.LessThan(order.IndexOf("\"AttackMove\"", StringComparison.Ordinal)));
			Assert.That(order.IndexOf("\"AttackMove\"", StringComparison.Ordinal), Is.LessThan(order.IndexOf("else", order.IndexOf("\"AttackMove\"", StringComparison.Ordinal), StringComparison.Ordinal)));
			Assert.That(order, Does.Contain("Policy.ApproachPoint(owner.CenterPosition, destination,"), "the march stops in front of the destination");
			Assert.That(order, Does.Contain("var point = support ? destination"), "the support attack-moves onto the target itself");
		}

		// ---- 25. L6 and M4 ----

		[Test]
		public void L6TheMarchTickChecksTheFleeRatioAndM4EveryLosingEndRunsTheLegacyFlightBaseOnly()
		{
			var manager = Manager();
			var flee = Section(manager, "internal bool RetreatRallyMarchFlee(", "bool RallyJoinLosing(");
			Assert.That(flee, Does.Contain("AggressionRatio(owner, AggressionFriends(owner, null), enemies)"));
			Assert.That(flee, Does.Contain("AggressionParam(nameof(SquadManagerBotModuleCAInfo.AggressionFleeRatioPct), owner)"));
			Assert.That(flee, Does.Contain("return Policy.MarchFlee(ratio, AggressionParam("), "the ratio decides, not a constant");
			Assert.That(flee, Does.Contain("plan.PursuerIds.Contains(a.ActorID)"), "the pursuers the squad is leaving do not count");
			var observe = Section(manager, "internal RetreatRallyObservation RetreatRallyObserve(", "internal RetreatRallyPlan RetreatRallyRescore(");
			Assert.That(observe, Does.Contain("RetreatRallyMarchFlee(owner, plan)"));
			Assert.That(observe, Does.Contain("RallyJoinLosing(owner, destPos, out ratioMilli)"), "G1 again at arrival");
			var state = State();
			Assert.That(state, Does.Contain("new GroundUnitsFleeStateCA(true), false);"));
			Assert.That(state, Does.Contain("case Policy.RallyNext.FleeBaseOnly:"));
			Assert.That(state, Does.Contain("case Policy.RallyNext.DismissPool:\n\t\t\t\t\tmanager.DismissSquad(owner);"), "Merge dismisses into the pool");
			Assert.That(state, Does.Contain("case Policy.RallyNext.Idle:"));
			Assert.That(state, Does.Contain("manager.RetreatRallyObserve(owner, plan, phase, action)"));
			Assert.That(Count(state, "new GroundUnitsFleeStateCA("), Is.EqualTo(1));
			Assert.That(PolicySource(), Does.Contain("RallyEnd.AbortLosing => RallyNext.FleeBaseOnly"));
			Assert.That(PolicySource(), Does.Contain("RallyEnd.Expired => phaseAtEnd == RallyPhase.March ? RallyNext.FleeBaseOnly : RallyNext.Idle"));
		}

		[Test]
		public void TheRallyStateKeepsTheSquadAndDoesNotEndTwice()
		{
			var state = State();
			Assert.That(state, Does.Contain("class GroundUnitsRallyStateCA : StateBaseCA, IState"));
			var end = Section(state, "void End(SquadCA owner", "// The state is replaced from outside");
			Assert.That(end.IndexOf("ended = true;", StringComparison.Ordinal), Is.LessThan(end.IndexOf("manager.RetreatRallyEnd(", StringComparison.Ordinal)));
			Assert.That(end.IndexOf("manager.RetreatRallyEnd(", StringComparison.Ordinal), Is.LessThan(end.IndexOf("switch (Policy.NextState(end, atPhase))", StringComparison.Ordinal)), "the row and the cooldown come before the state change");
			var deactivate = state.Substring(state.IndexOf("public void Deactivate(SquadCA owner)", StringComparison.Ordinal));
			Assert.That(deactivate, Does.Contain("if (ended)\n\t\t\t\treturn;"));
			Assert.That(deactivate, Does.Not.Contain("DismissSquad"), "the rally state's own Deactivate never dismisses the squad");
			Assert.That(state, Does.Contain("manager.RetreatRallyArrive(owner, plan, step.Action, observed.RatioMilli);"));
		}

		[Test]
		public void TheRallyCountersAndTablesExist()
		{
			var text = Manager();
			foreach (var name in new[] { "RallyPicks", "RallyBases", "RallyArrivals", "RallyEnds", "RallyDowngrades", "RallyMerges", "RallyHopBlocks", "RallyPingPongBlocks" })
				Assert.That(text, Does.Contain("internal int " + name + ";"), name);
			foreach (var table in new[] { "RallyCooldowns", "RallyIds", "RallyEpisodes", "RallyBlacklists" })
				Assert.That(text, Does.Contain("static readonly ConditionalWeakTable<SquadCA, "), table);
			Assert.That(text, Does.Contain("RallyPingPongBlocks += choice.MarkedRejects;"));
			Assert.That(text, Does.Contain("RallyHopBlocks++;"));
		}

		[Test]
		public void TheRetreatMarkIsSetAtEveryFlightTickAndOneDecisionIsTakenPerEpisode()
		{
			var text = Manager();
			var note = Section(text, "internal void RetreatRallyNoteFlight(SquadCA squad)", "internal bool TryRetreatRally(");
			Assert.That(note, Does.Contain("squad.RetreatSinceTick = now;"));
			Assert.That(note, Does.Contain("Policy.IsMarked(squad.RetreatSinceTick, now,"), "the previous mark decides whether this is a new episode");
			Assert.That(note.IndexOf("Policy.IsMarked(", StringComparison.Ordinal), Is.LessThan(note.IndexOf("squad.RetreatSinceTick = now;", StringComparison.Ordinal)), "read before it is overwritten");
			var ask = Section(text, "internal bool TryRetreatRally(", "static string RallyNeedName(");
			Assert.That(ask, Does.Contain("if (episode.FlightDecisionTick >= 0)\n\t\t\t\treturn false;"));
			Assert.That(ask, Does.Contain("episode.Hops = Policy.EffectiveHops("));
			Assert.That(ask, Does.Contain("+ 1;\n\t\t\tepisode.LastRallyTick = now;\n\t\t\tRallyPicks++;"), "the pick ages the episode (hops reset, cooldown start)");
			Assert.That(ask, Does.Contain("Policy.HomeBlock(defence.RecallAll, AggTeamSquadDefends(squad), defence.IsActive,"));
			Assert.That(ask, Does.Contain("ReferenceEquals(squad, stalemateSquad)"));
			Assert.That(ask, Does.Contain("!RallyIsGroundType(squad)"));
		}

		[Test]
		public void TheCooldownAndTheBlacklistAreWrittenAtTheEnd()
		{
			var text = Manager();
			var end = Section(text, "internal void RetreatRallyEnd(", "// ---- the fallback base cell ----");
			Assert.That(end, Does.Contain("RallyCooldowns.GetValue(owner, _ => new StrongBox<int>()).Value = until;"));
			Assert.That(end, Does.Contain("RallyCooldowns.GetValue(plan.DestSquad, _ => new StrongBox<int>()).Value = until;"), "the destination is cooled too");
			Assert.That(end, Does.Contain("end == Policy.RallyEnd.AbortLosing || (end == Policy.RallyEnd.Expired && phase == Policy.RallyPhase.March)"), "only a lost destination is blacklisted");
			Assert.That(end, Does.Contain("AggTeamRallyBlacklistTicks"));
			Assert.That(end, Does.Contain("AggTeamRallyCooldownTicks"));
		}
	}
}
