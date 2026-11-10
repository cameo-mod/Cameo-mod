// IMPL02 MCV-SAFE-01: pure policy tests (zones, forward-contain, route/abort/loss rules) and source pins for the world wiring
// (the planner has no headless world fixture; the first real exercise is the ON smoke).
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class McvSafeTest
	{
		static CPos C(int x, int y) => new(x, y);

		static McvThreatZone Z(int x, int y, int r, int expiry = -1) => new(C(x, y), r, expiry);

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static string Planner() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs");

		static string Safe() => Source("mods/Cameo/Traits/BotModules/ExpansionPlannerBotModule.McvSafe.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		// ---- zones ----

		[Test]
		public void TheZoneRadiusIsWeaponRangePlusArtilleryPlusMarginAndNeverNegative()
		{
			Assert.That(McvSafePolicyCA.ZoneRadius(8, 10, 4), Is.EqualTo(22));
			Assert.That(McvSafePolicyCA.ZoneRadius(0, 10, 4), Is.EqualTo(14), "an unarmed producer still reaches artillery distance");
			Assert.That(McvSafePolicyCA.ZoneRadius(-3, -1, -2), Is.EqualTo(0));
		}

		[Test]
		public void AZoneIncludesItsEdgeAndGrowsByTheExtraCells()
		{
			var zones = new[] { Z(50, 50, 10) };
			Assert.That(McvSafePolicyCA.InZone(C(60, 50), zones, 0), Is.True, "radius is inclusive");
			Assert.That(McvSafePolicyCA.InZone(C(61, 50), zones, 0), Is.False);
			Assert.That(McvSafePolicyCA.InZone(C(63, 50), zones, 0, 3), Is.True, "grown by the abort margin");
			Assert.That(McvSafePolicyCA.InZone(C(64, 50), zones, 0, 3), Is.False);
			Assert.That(McvSafePolicyCA.InZone(C(50, 50), null, 0), Is.False);
		}

		[Test]
		public void AnExpiredZoneNoLongerExcludesAndAPermanentOneNeverExpires()
		{
			var zones = new[] { Z(50, 50, 10, 1000), Z(100, 100, 5) };
			Assert.That(McvSafePolicyCA.InZone(C(50, 50), zones, 999), Is.True);
			Assert.That(McvSafePolicyCA.InZone(C(50, 50), zones, 1000), Is.False, "expires at the expiry tick");
			Assert.That(McvSafePolicyCA.InZone(C(100, 100), zones, 99999), Is.True, "negative expiry is permanent");
		}

		[Test]
		public void TheEdgeDistanceIsMeasuredToTheNearestLiveZone()
		{
			var zones = new[] { Z(50, 50, 10), Z(90, 50, 10, 500) };
			Assert.That(McvSafePolicyCA.EdgeDistanceCells(C(65, 50), zones, 0), Is.EqualTo(5));
			Assert.That(McvSafePolicyCA.EdgeDistanceCells(C(75, 50), zones, 0), Is.EqualTo(5), "the second zone is nearer (edge at 80)");
			Assert.That(McvSafePolicyCA.EdgeDistanceCells(C(75, 50), zones, 600), Is.EqualTo(15), "the second zone expired");
			Assert.That(McvSafePolicyCA.EdgeDistanceCells(C(52, 50), zones, 0), Is.EqualTo(0), "inside counts as on the edge");
			Assert.That(McvSafePolicyCA.EdgeDistanceCells(C(5, 5), Array.Empty<McvThreatZone>(), 0), Is.EqualTo(int.MaxValue));
		}

		// ---- scoring ----

		[Test]
		public void TheForwardBonusIsLargestAtTheZoneEdgeAndFallsToNothingAtTheBand()
		{
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(0, 14, 40), Is.EqualTo(1400));
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(7, 14, 40), Is.EqualTo(1200));
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(14, 14, 40), Is.EqualTo(1000));
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(15, 14, 40), Is.EqualTo(1000), "beyond the band");
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(int.MaxValue, 14, 40), Is.EqualTo(1000), "no zone known");
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(3, 0, 40), Is.EqualTo(1000), "zero band");
			Assert.That(McvSafePolicyCA.ForwardFactorPermille(3, 14, 0), Is.EqualTo(1000), "zero bonus");
		}

		[Test]
		public void AFullFieldScoresTheBonusAndASmallOneDoesNot()
		{
			Assert.That(McvSafePolicyCA.FullFieldFactorPermille(75, 100, 75, 25), Is.EqualTo(1250), "at the threshold");
			Assert.That(McvSafePolicyCA.FullFieldFactorPermille(74, 100, 75, 25), Is.EqualTo(1000));
			Assert.That(McvSafePolicyCA.FullFieldFactorPermille(100, 0, 75, 25), Is.EqualTo(1000), "no field known");
			Assert.That(McvSafePolicyCA.FullFieldFactorPermille(100, 100, 75, 0), Is.EqualTo(1000), "zero bonus");
		}

		[Test]
		public void ASafeFullFieldBeatsAnEnemyAdjacentOneOnceTheZoneIsExcluded()
		{
			// the user game: a 100-cell field next to an enemy spawn versus a 62-cell safe field. The zone removes the first; scoring is then value-based.
			var zones = new[] { Z(127, 28, 30) };
			Assert.That(McvSafePolicyCA.InZone(C(127, 20), zones, 0), Is.True, "field 5 is 8 cells from the spawn");
			Assert.That(McvSafePolicyCA.InZone(C(56, 15), zones, 0), Is.False);
		}

		// ---- route, abort, loss ----

		[Test]
		public void ARouteThatReEntersAZoneIsUnsafeButOneLeavingTheStartZoneIsFine()
		{
			var zones = new[] { Z(50, 50, 10) };
			Assert.That(McvSafePolicyCA.PathCrossesZone(new[] { C(45, 50), C(55, 50), C(70, 50) }, zones, 0), Is.False, "leading cells inside the zone are tolerated");
			Assert.That(McvSafePolicyCA.PathCrossesZone(new[] { C(30, 50), C(40, 50), C(50, 50), C(70, 50) }, zones, 0), Is.True);
			Assert.That(McvSafePolicyCA.PathCrossesZone(new[] { C(30, 50), C(35, 50) }, zones, 0), Is.False);
			Assert.That(McvSafePolicyCA.PathCrossesZone(null, zones, 0), Is.False);
			Assert.That(McvSafePolicyCA.PathCrossesZone(Array.Empty<CPos>(), zones, 0), Is.False);
		}

		[Test]
		public void AZoneCellCostsTheConfiguredPenaltyAndOtherCellsNothing()
		{
			var zones = new[] { Z(50, 50, 10) };
			Assert.That(McvSafePolicyCA.PathCost(C(50, 50), zones, 0, 600), Is.EqualTo(600));
			Assert.That(McvSafePolicyCA.PathCost(C(80, 50), zones, 0, 600), Is.EqualTo(0));
			Assert.That(McvSafePolicyCA.PathCost(C(50, 50), zones, 0, -5), Is.EqualTo(0), "never a negative cost");
		}

		[Test]
		public void AnMcvAbortsInsideAGrownZoneOrNearAVisibleThreatOnly()
		{
			var zones = new[] { Z(50, 50, 10) };
			Assert.That(McvSafePolicyCA.ShouldAbort(C(63, 50), zones, 0, 3, -1, 8), Is.True, "inside the zone grown by the margin");
			Assert.That(McvSafePolicyCA.ShouldAbort(C(64, 50), zones, 0, 3, -1, 8), Is.False);
			Assert.That(McvSafePolicyCA.ShouldAbort(C(90, 90), zones, 0, 3, 8, 8), Is.True, "a visible armed enemy exactly at the threat distance");
			Assert.That(McvSafePolicyCA.ShouldAbort(C(90, 90), zones, 0, 3, 9, 8), Is.False);
			Assert.That(McvSafePolicyCA.ShouldAbort(C(90, 90), zones, 0, 3, -1, 8), Is.False, "-1 = none seen");
		}

		[Test]
		public void WaypointsAreEvenlySpacedAndEndOnTheLastCell()
		{
			var path = Enumerable.Range(1, 20).Select(i => C(i, 0)).ToList();
			var wp = McvSafePolicyCA.Waypoints(path, 4);
			Assert.That(wp, Is.EqualTo(new[] { C(5, 0), C(10, 0), C(15, 0), C(20, 0) }));
			Assert.That(McvSafePolicyCA.Waypoints(path, 1), Is.EqualTo(new[] { C(20, 0) }));
			Assert.That(McvSafePolicyCA.Waypoints(path.Take(2).ToList(), 4), Is.EqualTo(new[] { C(1, 0), C(2, 0) }), "never more waypoints than cells");
			Assert.That(McvSafePolicyCA.Waypoints(null, 4), Is.Empty);
			Assert.That(McvSafePolicyCA.Waypoints(path, 0), Is.Empty);
		}

		[Test]
		public void AnMcvThatVanishedBesideAnOwnYardWasDeployedAndOtherwiseLost()
		{
			Assert.That(McvSafePolicyCA.WasLost(C(60, 60), new[] { C(63, 62) }, 8), Is.False, "a yard stands within 8 cells");
			Assert.That(McvSafePolicyCA.WasLost(C(60, 60), new[] { C(80, 80) }, 8), Is.True);
			Assert.That(McvSafePolicyCA.WasLost(C(60, 60), Array.Empty<CPos>(), 8), Is.True);
			Assert.That(McvSafePolicyCA.WasLost(C(60, 60), null, 8), Is.True);
			Assert.That(McvSafePolicyCA.WasLost(C(60, 60), new[] { C(68, 60) }, 8), Is.False, "inclusive");
		}

		// ---- parameters and wiring ----

		[Test]
		public void EveryMcvSafeParameterIsANamedRangedField()
		{
			var fields = typeof(SquadManagerBotModuleCAInfo).GetFields().Where(f => f.Name.StartsWith("AggTeamMcv", StringComparison.Ordinal)).ToList();
			Assert.That(fields.Count, Is.GreaterThanOrEqualTo(19));
			var info = new SquadManagerBotModuleCAInfo();
			foreach (var f in fields)
			{
				var (min, max) = AggressionParams.RangeOf(f.Name);
				var value = (int)f.GetValue(info);
				Assert.That(value, Is.InRange(min, max), f.Name);
			}
		}

		[Test]
		public void TheStrategicDefaultsAreTheSafetyDefaultsWithNoTables()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryCells), "gdi", "nod"), Is.EqualTo(10));
			Assert.That(AggressionParams.Resolve(info, nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnZoneCells), "gdi", null), Is.EqualTo(30));
		}

		[Test]
		public void TheSwitchSeamIsOffUnlessAggTeamIsEnabledAndTheParamsGoThroughTheResolver()
		{
			var wiring = Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.AggTeam.cs");
			Assert.That(wiring, Does.Contain("bool IBotMcvSafetyPolicy.McvSafeEnabled => Info.AggTeamEnabled && !IsTraitDisabled;"));
			Assert.That(wiring, Does.Contain("int IBotMcvSafetyPolicy.McvSafeParam(string name, Player enemy) => AggP(name, enemy);"));
		}

		[Test]
		public void ThePlannerHooksAreAllBehindTheContextAndTheFallbackCoversBothNullReturns()
		{
			var site = Section(Planner(), "CPos? IBotMcvExpansionSiteProvider.McvExpansionSite(Actor mcv)", "bool ScalingDeployCellExists(");
			foreach (var guard in new[]
			{
				"var safe = McvSafeBegin();",
				"if (safe != null && (rejected.Contains(f.Index) || !McvSafeSiteAllowed(safe, f)))",
				"weight = f => (baseWeight?.Invoke(f) ?? 1.0) * McvSafeSiteFactor(safe, f);",
				"attempt < (safe != null ? 3 : 2)",
				"return safe != null ? McvSafeFallback(safe, mcv) : null;",
				"if (safe != null && !McvSafeRoute(safe, mcv, s.Center, true, out safeRoute))",
				"McvSafeIssue(mcv, s.Index, safeRoute);",
			})
				Assert.That(site, Does.Contain(guard), guard);

			Assert.That(System.Text.RegularExpressions.Regex.Matches(site, Regex("return safe != null ? McvSafeFallback(safe, mcv) : null;")).Count, Is.EqualTo(2));
			Assert.That(site.IndexOf("McvSafeRoute(safe, mcv, s.Center", StringComparison.Ordinal), Is.LessThan(site.IndexOf("TrackMcvHandout(", StringComparison.Ordinal)),
				"a refused route must not count as a hand-out");
			Assert.That(site.IndexOf("TrackMcvHandout(", StringComparison.Ordinal), Is.LessThan(site.IndexOf("McvSafeIssue(", StringComparison.Ordinal)),
				"orders are issued only for an accepted hand-out");
			Assert.That(Planner(), Does.Contain("public partial class ExpansionPlannerBotModule"));
			Assert.That(Section(Planner(), "void IBotTick.BotTick(IBot bot)", "void Replan(IBot bot)"), Does.Contain("McvSafeTick(bot);"));
		}

		static string Regex(string literal) => System.Text.RegularExpressions.Regex.Escape(literal);

		[Test]
		public void TheContextIsNullWithoutAnEnabledPolicyAndTheGuardClearsItsStateThen()
		{
			var safe = Safe();
			Assert.That(Section(safe, "McvSafeContext McvSafeBegin()", "int McvWeaponRangeCells("), Does.Contain("if (policy == null)\n\t\t\t\treturn null;").And.Contain("FirstOrDefault(t => t.McvSafeEnabled)"));
			var tick = Section(safe, "void McvSafeTick(IBot bot)", "void McvSafeVanished(");
			Assert.That(tick, Does.Contain("t => t.McvSafeEnabled"));
			Assert.That(tick, Does.Contain("mcvTracks.Clear();"));
			Assert.That(tick, Does.Contain("new Order(\"Stop\", mcv, false)"));
			Assert.That(tick, Does.Contain("parkedUntil[track.Field] = now + Info.ParkTicks;"));
		}

		[Test]
		public void TheZonesComeFromEnemyMemoryOnlyAndSpawnZonesStopOnceExplored()
		{
			var zones = Section(Safe(), "IReadOnlyList<McvThreatZone> McvZones(", "bool McvSafeSiteAllowed(");
			foreach (var guard in new[]
			{
				"player.FrozenActorLayer", "player.RelationshipWith(fa.Owner) != PlayerRelationship.Enemy", "fa.Info.HasTraitInfo<BuildingInfo>()",
				"McvSafePolicyCA.ZoneRadius(armed ? McvWeaponRangeCells(fa.Info) : 0, artillery, margin)",
				"!a.CanBeViewedByPlayer(player)", "range >= artilleryMin",
				"player.Shroud.IsExplored(start)", "mcvLossZones.RemoveAll(", "d.Value.Value == \"mpspawn\"", "p == player || player.IsAlliedWith(p)",
				"ArmyStagingEval.EnemySpawnCandidates(mcvStarts, homes, policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvSpawnMatchCells)))",
			})
				Assert.That(zones, Does.Contain(guard), guard);
			Assert.That(zones, Does.Not.Contain("Spectating"), "the actual enemy assignment is not read (Opus spawn ruling)");
			Assert.That(zones, Does.Not.Contain("p.HomeLocation, spawn"), "no zone is put on a player's home");
		}

		[Test]
		public void AZoneThatAlreadyHeldTheTripStartNeverAbortsTheTripButANewOneDoes()
		{
			var baseLoss = Z(50, 50, 16);
			var other = Z(120, 50, 10);
			var zones = new[] { baseLoss, other };
			var trip = McvSafePolicyCA.ZonesNotContainingStart(zones, C(52, 50), 0, 3);
			Assert.That(trip, Is.EqualTo(new[] { other }), "the zone around the start is left out");
			Assert.That(McvSafePolicyCA.ShouldAbort(C(60, 50), trip, 0, 3, -1, 8), Is.False, "still inside the start zone: no abort");
			Assert.That(McvSafePolicyCA.ShouldAbort(C(112, 50), trip, 0, 3, -1, 8), Is.True, "entering a new zone from outside aborts");
			Assert.That(McvSafePolicyCA.ShouldAbort(C(60, 50), trip, 0, 3, 5, 8), Is.True, "a visible armed enemy still aborts");
			Assert.That(McvSafePolicyCA.ZonesNotContainingStart(zones, C(50 + 16 + 3, 50), 0, 3), Is.EqualTo(new[] { other }), "the margin counts: a start on the grown edge is inside");
			Assert.That(McvSafePolicyCA.ZonesNotContainingStart(zones, C(50 + 16 + 4, 50), 0, 3), Is.EqualTo(zones), "one cell further out the zone counts");
			Assert.That(McvSafePolicyCA.ZonesNotContainingStart(null, C(0, 0), 0, 3), Is.Empty);
			var expired = Z(50, 50, 16, 10);
			Assert.That(McvSafePolicyCA.ZonesNotContainingStart(new[] { expired }, C(52, 50), 20, 3), Is.EqualTo(new[] { expired }), "an expired zone never contained the start");
		}

		[Test]
		public void TheGuardJudgesEachTripAgainstTheZonesOutsideItsStart()
		{
			var tick = Section(Safe(), "void McvSafeTick(IBot bot)", "void McvSafeVanished(");
			foreach (var guard in new[]
			{
				"track.TripStart = null;", "track.TripStart ??= mcv.Location;", "McvSafePolicyCA.ZonesNotContainingStart(zones, track.TripStart.Value, now, abortMargin)",
				"McvSafePolicyCA.ShouldAbort(mcv.Location, tripZones, now, abortMargin, threat, threatCells)",
			})
				Assert.That(tick, Does.Contain(guard), guard);
			Assert.That(tick.IndexOf("track.TripStart = null;", StringComparison.Ordinal), Is.LessThan(tick.IndexOf("track.TripStart ??=", StringComparison.Ordinal)),
				"an idle MCV starts a new trip next time");
		}

		[Test]
		public void TheTelemetryTellsAKilledUnclaimedMcvOnlyWithTheCgPolicyOn()
		{
			var text = Source("mods/Cameo/Traits/AiPlayerTelemetry.cs");
			var prune = Section(text, "void PruneEvents(int tick)", "void ITick.Tick(Actor self)");
			foreach (var guard in new[]
			{
				"e.Tick < horizon && e.Cause == \"killed\" && McvSafetyOn(e.Owner)", "EmitEvent(seat, \"mcv_lost\", e.ActorName, e.Cell, cause: e.Cause,",
			})
				Assert.That(prune, Does.Contain(guard), guard);
			Assert.That(prune.IndexOf("EmitEvent(seat, \"mcv_lost\"", StringComparison.Ordinal), Is.LessThan(prune.IndexOf("mcvRemoved.RemoveAll(e => e.Tick < horizon);", StringComparison.Ordinal)),
				"the unclaimed entries are reported before they are dropped");
			Assert.That(text, Does.Contain("ActorName = actor.Info.Name, ActorId = actor.ActorID, Cause = LossCause(actor)"));
			Assert.That(text, Does.Contain("TraitsImplementing<OpenRA.Mods.CA.Traits.BotModules.IBotMcvSafetyPolicy>().Any(t => t.McvSafeEnabled)"));
		}

		[Test]
		public void ALostMcvIsToldFromADeployByAnOwnYardBesideItAndAParkedFieldIsNotRetried()
		{
			var lost = Section(Safe(), "void McvSafeVanished(", "\n\t}\n}");
			foreach (var guard in new[]
			{
				"!track.Actor.IsDead", "McvSafePolicyCA.WasLost(track.LastCell, yards,", "mcvLossZones.Add(new McvThreatZone(track.LastCell,",
				"parkedUntil[track.Field] = now + Info.ParkTicks;", "a.Info.HasTraitInfo<BaseBuildingInfo>() || a.Info.HasTraitInfo<RefineryInfo>()",
			})
				Assert.That(lost, Does.Contain(guard), guard);
		}

		[Test]
		public void ADeployMatchTakesTheNewestSameOwnerEntryInsideTheWindowAndTheCells()
		{
			var entries = new System.Collections.Generic.List<(string Owner, CPos Cell, int Tick)>
			{
				("a", C(10, 10), 100), ("b", C(10, 10), 105), ("a", C(11, 11), 106), ("a", C(30, 30), 107),
			};
			Assert.That(DeployMatchMath.Find(entries, "a", C(10, 10), 108, 10, 3), Is.EqualTo(2), "newest same-owner entry within 3 cells");
			Assert.That(DeployMatchMath.Find(entries, "b", C(10, 10), 108, 10, 3), Is.EqualTo(1), "only the same owner");
			Assert.That(DeployMatchMath.Find(entries, "a", C(10, 10), 111, 10, 3), Is.EqualTo(2), "tick 106 is 5 ticks back; 100 would be 11");
			Assert.That(DeployMatchMath.Find(entries, "a", C(10, 10), 117, 10, 3), Is.EqualTo(-1), "all outside the window");
			Assert.That(DeployMatchMath.Find(entries, "a", C(15, 10), 108, 10, 3), Is.EqualTo(-1), "4 cells from the nearest entry");
			Assert.That(DeployMatchMath.Find(entries, "a", C(14, 10), 108, 10, 3), Is.EqualTo(2), "exactly 3 cells is inside");
			Assert.That(DeployMatchMath.Find(new System.Collections.Generic.List<(string, CPos, int)>(), "a", C(0, 0), 0, 10, 3), Is.EqualTo(-1));
		}

		[Test]
		public void TheTelemetryMatchesADeployWhenTheYardIsAddedBeforeTheMcvIsRemoved()
		{
			var text = Source("mods/Cameo/Traits/AiPlayerTelemetry.cs");
			Assert.That(text, Does.Contain("pendingDeployAdds"));
			Assert.That(Section(text, "void ActorRemoved(Actor actor)", "bool TryMatchDeploy("), Does.Contain("if (TryMatchPendingDeploy(actor.Owner, actor.Location, world.WorldTick, out var deployedYard))"));
		}

		// ---- TIER-01 A9: McvSafe joins the A9-5 gate. The one real threshold is a named ranged parameter; the unit conversions are named constants ----

		const string ArtilleryScans = nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryZoneScans);

		[Test]
		public void TheArtilleryZoneLifetimeIsANamedRangedParameterWhoseDefaultIsTodaysThreeScans()
		{
			var info = new SquadManagerBotModuleCAInfo();
			Assert.That(info.AggTeamMcvArtilleryZoneScans, Is.EqualTo(3), "the literal `interval * 3` it replaced");
			Assert.That(AggressionParams.RangeOf(ArtilleryScans), Is.EqualTo((1, 20)));
			Assert.That(AggressionParams.Resolve(info, ArtilleryScans, null, null), Is.EqualTo(3), "empty tables resolve to the default");
			Assert.That(AggressionParams.Resolve(info, ArtilleryScans, "gdi", "nod"), Is.EqualTo(3));
		}

		[Test]
		public void TheArtilleryZoneLifetimeIsLearnableThroughTheLayeredResolverAndClampedToItsRange()
		{
			var info = new SquadManagerBotModuleCAInfo();
			info.AggMultPersonality[ArtilleryScans] = 2000;
			Assert.That(AggressionParams.Resolve(info, ArtilleryScans, "gdi", "nod"), Is.EqualTo(6), "3 x 2.0");

			var wide = new SquadManagerBotModuleCAInfo();
			typeof(SquadManagerBotModuleCAInfo).GetField(ArtilleryScans).SetValue(wide, 10);
			wide.AggMultPersonality[ArtilleryScans] = 5000;
			Assert.That(AggressionParams.Resolve(wide, ArtilleryScans, "gdi", "nod"), Is.EqualTo(20), "10 x 5.0 = 50 is clamped to the Range maximum");
		}

		[Test]
		public void TheArtilleryZoneIsBuiltFromTheParameterAndNoRawLiteralRemainsInTheMcvSafeSources()
		{
			var safe = Safe();
			var zones = Section(safe, "IReadOnlyList<McvThreatZone> McvZones(", "bool McvSafeSiteAllowed(");
			Assert.That(zones, Does.Contain("var artilleryScans = policy.McvSafeParam(nameof(SquadManagerBotModuleCAInfo.AggTeamMcvArtilleryZoneScans));"));
			Assert.That(zones, Does.Contain("now + interval * artilleryScans"));
			Assert.That(zones, Does.Not.Contain("interval * 3"));
			Assert.That(safe, Does.Contain("MaxRange.Length + AggTeamPolicyCA.CellUnits - 1) / AggTeamPolicyCA.CellUnits"));
			Assert.That(safe, Does.Contain("return forward / McvSafePolicyCA.PermilleUnit * full / McvSafePolicyCA.PermilleUnit;"));
			Assert.That(safe, Does.Not.Contain("+ 1023").And.Not.Contain("/ 1024").And.Not.Contain("/ 1000.0"));
			var policy = Source("mods/CA/Traits/BotModules/Squads/McvSafePolicyCA.cs");
			Assert.That(policy, Does.Not.Contain("* 10 ").And.Not.Contain("* 10;"), "the percent-to-permille factor is PermillePerPercent");
		}

		[Test]
		public void TheNamedConversionsGiveExactlyTheResultsOfTheLiteralsTheyReplaced()
		{
			Assert.That(McvSafePolicyCA.PermillePerPercent, Is.EqualTo(10));
			Assert.That(McvSafePolicyCA.PermilleUnit, Is.EqualTo(1000.0));
			Assert.That(AggTeamPolicyCA.CellUnits, Is.EqualTo(1024));

			var bad = 0;
			for (var band = 1; band <= 60; band += 3)
				for (var edge = 0; edge <= band + 1; edge++)
					for (var bonus = 0; bonus <= 300; bonus += 7)
					{
						var old = edge > band || bonus <= 0 ? 1000 : 1000 + bonus * 10 * (band - edge) / band;
						if (McvSafePolicyCA.ForwardFactorPermille(edge, band, bonus) != old)
							bad++;
					}

			for (var max = 0; max <= 120; max += 5)
				for (var value = 0; value <= 130; value += 3)
					for (var full = 0; full <= 100; full += 5)
						for (var bonus = 0; bonus <= 300; bonus += 25)
						{
							var old = max <= 0 || bonus <= 0 ? 1000 : (long)value * 100 >= (long)max * full ? 1000 + bonus * 10 : 1000;
							if (McvSafePolicyCA.FullFieldFactorPermille(value, max, full, bonus) != old)
								bad++;
						}

			Assert.That(bad, Is.EqualTo(0), "cells where the named-constant form differs from the old literal form");
		}
	}
}
