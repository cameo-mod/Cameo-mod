// FREEZE10 DIAG-FIX-02 (Sonnet; Opus order on Workhorse MEDIUM-1 of DIAG-FIX-01): the enemy-half refusal drops a suspected enemy start once this seat has
// explored it and seen no enemy building within SpawnMatchCells of it (an unused start on a map with more starts than players). Occupied and
// unexplored starts stay enemy starts. The rule is a pure function (McvSafePolicyCA.PruneEmptyExploredStarts) composed with the existing
// NearerToEnemyStart refusal, so the behaviour is tested directly; the world wiring (shroud + frozen-actor layer, CG only) is pinned by source.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class DiagFix02Test
	{
		static CPos C(int x, int y) => new(x, y);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Safe() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.McvSafe.cs");

		static string PolicySource() => Source("mods/CA/Traits/BotModules/Squads/McvSafePolicyCA.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		// A four-start map with two players: this seat's start (the own base), the enemy's start, and two unused ones.
		static readonly CPos OwnBase = C(10, 10);
		static readonly CPos OccupiedStart = C(90, 10);
		static readonly CPos EmptyStart = C(10, 60);
		static readonly CPos UnexploredStart = C(90, 90);
		static readonly CPos[] Candidates = { OccupiedStart, EmptyStart, UnexploredStart };

		static bool Explored(CPos c) => c != UnexploredStart;

		static readonly CPos[] SeenEnemyBuildings = { C(93, 12), C(95, 8) };

		static List<CPos> Starts(IReadOnlyList<CPos> seen = null, Func<CPos, bool> explored = null, int match = 8) =>
			McvSafePolicyCA.PruneEmptyExploredStarts(Candidates, explored ?? Explored, seen ?? SeenEnemyBuildings, match);

		static bool Refused(CPos field, IReadOnlyList<CPos> starts) => McvSafePolicyCA.NearerToEnemyStart(field, starts, new[] { OwnBase });

		// ---- behaviour: more starts than players ----

		[Test]
		public void AnExploredEmptyStartIsDroppedOccupiedAndUnexploredStartsStay()
		{
			Assert.That(Starts(), Is.EqualTo(new[] { OccupiedStart, UnexploredStart }));
		}

		[Test]
		public void AFieldNearAnExploredEmptyStartIsAllowedAndWasRefusedBeforeTheFix()
		{
			var field = C(12, 58);
			Assert.That(Refused(field, Candidates), Is.True, "before DIAG-FIX-02 the explored empty start still counted: refused all game");
			Assert.That(Refused(field, Starts()), Is.False, "after: the empty start is not an enemy start");
		}

		[Test]
		public void AFieldNearAnOccupiedStartIsStillRefused()
		{
			Assert.That(Refused(C(88, 12), Starts()), Is.True);
		}

		[Test]
		public void AFieldNearAnUnexploredStartIsStillRefused()
		{
			Assert.That(Refused(C(88, 88), Starts()), Is.True);
		}

		[Test]
		public void WithNoStartKnownNothingIsRefusedAsBefore()
		{
			var none = McvSafePolicyCA.PruneEmptyExploredStarts(Array.Empty<CPos>(), Explored, SeenEnemyBuildings, 8);
			Assert.That(none, Is.Empty);
			Assert.That(Refused(C(12, 58), none), Is.False);
			Assert.That(Refused(C(88, 12), none), Is.False);
			Assert.That(McvSafePolicyCA.PruneEmptyExploredStarts(null, Explored, SeenEnemyBuildings, 8), Is.Empty);
		}

		[Test]
		public void WhenEveryStartIsExploredAndEmptyNothingIsRefused()
		{
			var starts = Starts(Array.Empty<CPos>(), _ => true);
			Assert.That(starts, Is.Empty);
			Assert.That(Refused(C(88, 12), starts), Is.False);
			Assert.That(Refused(C(88, 88), starts), Is.False);
		}

		[Test]
		public void OccupiedMeansAnEnemyBuildingWithinSpawnMatchCellsInclusiveBySquaredIntegerDistance()
		{
			// (6,0) from the start: 6^2 = 36 <= 64 occupied; exactly 8: 64 <= 64 occupied (the EnemySpawnCandidates rule); 9: 81 > 64 empty.
			Assert.That(Starts(new[] { EmptyStart + new CVec(8, 0) }), Does.Contain(EmptyStart));
			Assert.That(Starts(new[] { EmptyStart + new CVec(9, 0) }), Does.Not.Contain(EmptyStart));
			Assert.That(Starts(new[] { EmptyStart + new CVec(6, 5) }), Does.Contain(EmptyStart), "diagonal 36 + 25 = 61 <= 64: occupied");
			Assert.That(Starts(new[] { EmptyStart + new CVec(6, 6) }), Does.Not.Contain(EmptyStart), "diagonal 36 + 36 = 72 > 64: empty");
		}

		[Test]
		public void ABuildingThatOnlyNearsAnotherStartDoesNotOccupyThisOne()
		{
			Assert.That(Starts(new[] { OccupiedStart }), Does.Not.Contain(EmptyStart));
			Assert.That(Starts(new[] { OccupiedStart }), Does.Contain(OccupiedStart));
		}

		[Test]
		public void TheMatchRadiusIsTheOneGivenAndNeverNegative()
		{
			var seen = new[] { EmptyStart + new CVec(3, 0) };
			Assert.That(Starts(seen, match: 2), Does.Not.Contain(EmptyStart));
			Assert.That(Starts(seen, match: 3), Does.Contain(EmptyStart));
			Assert.That(Starts(seen, match: -5), Does.Not.Contain(EmptyStart));
		}

		[Test]
		public void AnUnexploredStartStaysEvenWhenNoBuildingIsSeenAndTheOrderIsKept()
		{
			var result = McvSafePolicyCA.PruneEmptyExploredStarts(new[] { C(5, 5), C(70, 70), C(30, 30) }, c => c == C(70, 70), Array.Empty<CPos>(), 8);
			Assert.That(result, Is.EqualTo(new[] { C(5, 5), C(30, 30) }));
		}

		[Test]
		public void NoExploredPredicateOrNoBuildingListDegradesWithoutThrowing()
		{
			Assert.That(McvSafePolicyCA.PruneEmptyExploredStarts(Candidates, null, SeenEnemyBuildings, 8), Is.EqualTo(Candidates), "nothing is known to be explored: all stay");
			Assert.That(McvSafePolicyCA.PruneEmptyExploredStarts(Candidates, Explored, null, 8), Is.EqualTo(new[] { UnexploredStart }), "explored and no building seen: empty");
		}

		[Test]
		public void TheRadiusSquareIsComputedInLongSoAHugeRadiusDoesNotOverflow()
		{
			// int.MaxValue^2 overflows int (and long-free code would wrap negative and match nothing); in long it is simply "everything is near".
			Assert.That(McvSafePolicyCA.PruneEmptyExploredStarts(new[] { C(100, 100) }, _ => true, new[] { C(0, 0) }, int.MaxValue), Is.EqualTo(new[] { C(100, 100) }));
		}

		// ---- wiring pins (no headless world fixture) ----

		[Test]
		public void TheEnemyStartListIsPrunedFromThePublicShroudAndFrozenActorsOnlyAndTheSuffixCountsThePrunedList()
		{
			var half = Section(Safe(), "bool McvSafeSiteInEnemyHalf(", "string McvSafeZoneNote(");
			foreach (var guard in new[]
			{
				"bool Explored(CPos c) => world.Map.Contains(c) && player.Shroud.IsExplored(c);",
				"ctx.EnemyStarts = McvSafePolicyCA.PruneEmptyExploredStarts(candidates, Explored,",
				"candidates.Any(Explored) ? McvSeenEnemyBuildingCells() : new List<CPos>(), matchCells);",
				"player.FrozenActorLayer", "player.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy", "fa.Info.HasTraitInfo<BuildingInfo>()",
				"({ctx.EnemyStarts.Count} starts,",
			})
				Assert.That(half, Does.Contain(guard), guard);

			Assert.That(half, Does.Not.Contain("Rand").And.Not.Contain("double").And.Not.Contain("Math.Sqrt"), "no RNG, integer math only");
			Assert.That(half, Does.Not.Contain("Spectating").And.Not.Contain("PlayerName").And.Not.Contain("ClientIndex ==").And.Not.Contain("Faction"), "no hidden state or identity");
			Assert.That(half.IndexOf("McvSafePolicyCA.PruneEmptyExploredStarts(", StringComparison.Ordinal),
				Is.LessThan(half.IndexOf("McvSafePolicyCA.NearerToEnemyStart(", StringComparison.Ordinal)), "pruned before the comparison");
			Assert.That(Section(Safe(), "string McvSafeZoneNote(", "bool McvSafeAbortHome(").Split(new[] { "\n\t\t/// <summary>" }, StringSplitOptions.None)[0],
				Does.Contain("starts {ctx.EnemyStarts?.Count ?? -1}"), "the EX-3 'starts S' count is the pruned list");
		}

		[Test]
		public void ThePruneRunsOnlyInsideTheCgContextAndTheRuleStaysPureAndIntegerOnly()
		{
			var all = Safe() + "\n" + Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");
			Assert.That(System.Text.RegularExpressions.Regex.Matches(all, System.Text.RegularExpressions.Regex.Escape("PruneEmptyExploredStarts(")).Count, Is.EqualTo(1),
				"one call, inside McvSafeSiteInEnemyHalf, whose only call site is the CG-guarded Eligible line (DiagFix01Test)");
			var rule = Section(PolicySource(), "public static List<CPos> PruneEmptyExploredStarts(", "/// <summary>An MCV that disappeared");
			Assert.That(rule, Does.Not.Contain("Rand").And.Not.Contain("double").And.Not.Contain("Math.Sqrt").And.Not.Contain("float"));
			Assert.That(rule, Does.Contain("DistanceSquared(s, b) <= r * r"));
		}
	}
}
