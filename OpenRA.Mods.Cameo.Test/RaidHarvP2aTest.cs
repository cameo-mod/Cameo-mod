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
using System.Text.RegularExpressions;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// RAID-HARV-01 P2a (delta 02 sections 2 and 3, Opus P2 nits): the squad manager's side WITHOUT call sites. The team-level raid fields (route-ready offers, one open proposal), the proposal freshness, the observed-enemy seam, and source pins for the
	/// fog rules: the R2 resolver is the only GetActorById of the raid module and tests visibility first; the route is selected with terrain-only blocking and an explored-only cost; the leader writes the joint assignment in one place and never
	/// dereferences the proposed id; the master module's provider applies the Hidden/Shrouded skip on every read.
	/// </summary>
	[TestFixture]
	public class RaidHarvP2aTest
	{
		const string Bots = "OpenRA.Mods.CA/Traits/BotModules/";

		static string RepoFile(string relative)
		{
			var dir = new DirectoryInfo(AppContext.BaseDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "mod.config")))
				dir = dir.Parent;

			Assert.That(dir, Is.Not.Null, "repository root (mod.config) not found above " + AppContext.BaseDirectory);
			return File.ReadAllText(Path.Combine(dir.FullName, relative)).Replace("\r\n", "\n");
		}

		static string Between(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text[start..end];
		}

		static int Count(string text, string term) => text.Split(new[] { term }, StringSplitOptions.None).Length - 1;

		static string Flat(string text) => Regex.Replace(text, @"\s+", " ");

		[Test]
		public void TheTeamKeepsTheLatestRouteReadyOfferPerSeatAndHandsOutCopies()
		{
			var team = new TeamLeadTeam();
			Assert.That(team.RaidRoutes(), Is.Empty);
			Assert.That(team.RaidProposal, Is.Null);

			team.PublishRaidRoute(new RaidContribution(1, 77, 600, 3, 200, 100));
			team.PublishRaidRoute(new RaidContribution(2, 77, 400, 2, 220, 100));
			team.PublishRaidRoute(new RaidContribution(1, 88, 650, 3, 210, 150)); // seat 1 moves to another target: its previous offer is replaced
			var routes = team.RaidRoutes();
			Assert.That(routes.Count, Is.EqualTo(2));
			Assert.That(routes.Single(r => r.SeatKey == 1).TargetId, Is.EqualTo(88u));
			Assert.That(RaidHarvGroup.RoutedValue(routes, 77, 160, 500, 2), Is.EqualTo(400), "only seat 2 still offers target 77");

			// A retract removes the seat's offer; the list handed out is a copy.
			routes.Clear();
			Assert.That(team.RaidRoutes().Count, Is.EqualTo(2), "mutating the copy changes nothing");
			team.RetractRaidRoute(2);
			Assert.That(team.RaidRoutes().Select(r => r.SeatKey), Is.EqualTo(new[] { 1 }));
			team.RetractRaidRoute(99);
			Assert.That(team.RaidRoutes().Count, Is.EqualTo(1), "retracting an unknown seat does nothing");
		}

		[Test]
		public void AProposalIsFreshOnlyWhenItExistsIsNotFromTheFutureAndIsNotOld()
		{
			var proposal = new RaidProposal(3, 77, 900, new CPos(10, 12), 480, 500);
			Assert.That(RaidHarvGroup.ProposalFresh(null, 600, 300), Is.False);
			Assert.That(RaidHarvGroup.ProposalFresh(proposal, 500, 300), Is.True);
			Assert.That(RaidHarvGroup.ProposalFresh(proposal, 800, 300), Is.True, "exactly at the limit");
			Assert.That(RaidHarvGroup.ProposalFresh(proposal, 801, 300), Is.False, "stale");
			Assert.That(RaidHarvGroup.ProposalFresh(proposal, 400, 300), Is.False, "from the future");

			// The proposal carries ids and cells as data; the record itself holds no actor reference.
			Assert.That(typeof(RaidProposal).GetProperties().Select(p => p.PropertyType.Name), Does.Not.Contain("Actor"));
			Assert.That(typeof(RaidContribution).GetProperties().Select(p => p.PropertyType.Name), Does.Not.Contain("Actor"));
		}

		[Test]
		public void TheObservedEnemyRecordHoldsDataOnlyAndTheSeamHasOneReadOnlyMethod()
		{
			var seen = new BotObservedEnemy(5, new CPos(3, 4), 1200, true, false, null);
			Assert.That(seen.ActorId, Is.EqualTo(5u));
			Assert.That(seen.Location, Is.EqualTo(new CPos(3, 4)));
			Assert.That(seen.LastSeenTick, Is.EqualTo(1200));
			Assert.That(seen.Harvester, Is.True);
			Assert.That(seen.Refinery, Is.False);
			Assert.That(typeof(BotObservedEnemy).GetProperties().Select(p => p.PropertyType.Name), Does.Not.Contain("Actor"), "no actor reference crosses the seam");
			Assert.That(typeof(IBotObservedEnemyProvider).GetMethods().Select(m => m.Name), Is.EqualTo(new[] { "ObservedEnemyEconomy" }));
		}

		[Test]
		public void TheMasterModuleProviderIsVisibleOnlyForHarvestersAndAppliesTheHiddenShroudedSkipOnEveryRead()
		{
			var text = RepoFile("OpenRA.Mods.Cameo/Traits/BotModules/BotSituation.cs");
			Assert.That(text, Does.Contain("IBotRememberedDefenceProvider, IBotObservedEnemyProvider, IBotSiegeFailureMemory,"));
			var method = Between(text, "IEnumerable<BotObservedEnemy> IBotObservedEnemyProvider.ObservedEnemyEconomy()", "// CN3 detector seam");

			// Units come from the table's visible-only records; no walk over the world's actors.
			Assert.That(method, Does.Contain("fogMemory.Remembered(enemy)"));
			Assert.That(method, Does.Contain("if (seen.Harvester && !seen.Building)"));
			Assert.That(method, Does.Not.Contain("World.Actors"));
			Assert.That(method, Does.Not.Contain("ActorsHavingTrait"));

			// A refinery counts from a valid frozen snapshot that is neither Hidden nor Shrouded (always on, not tied to the master module's switch) or while the building is in view.
			Assert.That(method, Does.Contain("!BotFogMemory.FrozenUnrememberable(frozen.Hidden, frozen.Shrouded, true)"));
			Assert.That(method, Does.Contain("frozen != null && frozen.IsValid"));
			Assert.That(method, Does.Contain("live != null && live.CanBeViewedByPlayer(player) && live.IsInWorld && !live.IsDead"), "visibility before every other read of the live actor");
			Assert.That(method, Does.Contain("if (IsTraitDisabled || fogMemory == null)"));
			Assert.That(method, Does.Not.Contain("SkipHiddenShroudedFrozen"), "the skip does not depend on the master module's switch");
		}

		[Test]
		public void TheRaidModuleResolvesEveryActorIdThroughOneVisibilityResolverAndNeverEnumeratesTheWorld()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");

			// R2: one GetActorById, inside the resolver, and the visibility test runs before anything of the actor is read.
			Assert.That(Count(text, "GetActorById("), Is.EqualTo(1));
			var resolver = Between(text, "internal Actor RaidTargetResolve(uint id)", "List<BotObservedEnemy> RaidObservedEconomy()");
			Assert.That(resolver, Does.Contain("World.GetActorById(id)"));
			Assert.That(resolver, Does.Contain("if (actor == null || !actor.CanBeViewedByPlayer(Player) || !actor.IsInWorld)"));
			Assert.That(resolver.IndexOf("CanBeViewedByPlayer", StringComparison.Ordinal), Is.LessThan(resolver.IndexOf("actor.Owner", StringComparison.Ordinal)), "visibility first");
			Assert.That(resolver.IndexOf("CanBeViewedByPlayer", StringComparison.Ordinal), Is.LessThan(resolver.IndexOf("actor.IsDead", StringComparison.Ordinal)));

			// No world enumeration without a visibility test in front of the read.
			Assert.That(text, Does.Not.Contain("World.Actors"));
			Assert.That(text, Does.Not.Contain("ActorsHavingTrait"));
			Assert.That(text, Does.Not.Contain("FrozenActorLayer"), "the frozen layer is read by the provider, with its Hidden/Shrouded skip");
			var defence = Between(text, "internal long RaidRequired(CPos cell, int targetValue)", "internal RaidRoutePlan RaidPlanRoute(");
			Assert.That(defence, Does.Contain("if (!a.CanBeViewedByPlayer(Player) || !a.IsInWorld)"), "visibility is the first read of a circle result");
			Assert.That(defence.IndexOf("CanBeViewedByPlayer", StringComparison.Ordinal), Is.LessThan(defence.IndexOf("a.Owner", StringComparison.Ordinal)), "the circle query result is filtered by visibility first");
			Assert.That(defence.IndexOf("CanBeViewedByPlayer", StringComparison.Ordinal), Is.LessThan(defence.IndexOf("Cost", StringComparison.Ordinal)));
		}

		[Test]
		public void TheRouteIsSelectedWithTerrainOnlyBlockingAndAnExploredOnlyCostAndTheScoutAimSearchesNoPath()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");
			Assert.That(Count(text, "FindPathToTargetCells("), Is.EqualTo(1), "one route search");
			Assert.That(Flat(text), Does.Contain("new[] { target }, BlockedByActor.None, RaidRoute.ExploredOnlyCost(Explored, PathGraph.PathCostForInvalidPath));"));
			foreach (var banned in new[] { "BlockedByActor.Stationary", "BlockedByActor.Immovable", "BlockedByActor.All", "FindPathToTargetCell(", "FindPathToTargetCellByPredicate", "PathExistsForLocomotor" })
				Assert.That(text, Does.Not.Contain(banned), banned);

			// The explored verifier guards the produced route, and the scout aim is a flood over explored, terrain-legal cells (no search through unknown terrain).
			Assert.That(text, Does.Contain("if (!RaidRoute.AllExplored(travel, Explored))"));
			Assert.That(text, Does.Contain("RaidRoute.FloodExplored(lead.Location, c => Explored(c) && Legal(c), 400)"));
			Assert.That(text, Does.Contain("RaidRoute.ScoutAim(flood, target, Explored, map.Contains, 4)"));

			// The risk gate runs at every waypoint with the package value and reads this raid's own incident memory.
			Assert.That(text, Does.Contain("if (!PassesRiskGate(wp, packageValue) || raidIncidents.Near(wp, now, RaidValue(RaidKey.IncidentDecay), 2))"));
		}

		[Test]
		public void OnlyTheLeaderWritesTheJointAssignmentAndNeverDereferencesTheProposedId()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");

			// One writer of the shared harass target in the raid module: the leader's pass.
			Assert.That(Count(text, "team.HarassTargetId = "), Is.EqualTo(1));
			var leader = Between(text, "void RaidLeaderPass(TeamLeadTeam team, int now)", "// ---- the scout request");
			Assert.That(leader, Does.Contain("team.HarassTargetId = proposal.TargetId;"));
			Assert.That(leader, Does.Contain("if (!RaidHarvOn || !teamLeadIsLeader)"));
			Assert.That(leader, Does.Contain("RaidHarvGroup.LeaderApproves(teamLeadIsLeader, true, routed, proposal.Req)"));
			Assert.That(leader, Does.Not.Contain("GetActorById"), "the leader compares ids and values only");
			Assert.That(leader, Does.Not.Contain("RaidTargetResolve"), "a proposed id is not resolved by the leader; each member resolves it through R2");
			Assert.That(leader, Does.Not.Contain(".Location"));
			Assert.That(leader, Does.Not.Contain("CenterPosition"));

			// The seat side publishes data only: an offer and a proposal, never the assignment.
			var publish = Between(text, "void RaidPublishRoute(", "/// The team leader's pass");
			Assert.That(publish, Does.Not.Contain("HarassTargetId"));
			Assert.That(publish, Does.Contain("team.PublishRaidRoute(new RaidContribution(RaidSeatKey, targetId, value, units, etaTicks, now));"));
			Assert.That(publish, Does.Contain("team.RaidProposal = new RaidProposal(RaidSeatKey, targetId, req, lastSeenCell, lastSeenTick, now);"));

			// Routed value uses the join minimum as an ACTOR count and the harass freshness window.
			Assert.That(leader, Does.Contain("RaidHarvGroup.RoutedValue(team.RaidRoutes(), proposal.TargetId, now, Info.TeamLeadHarassTicks, Info.HarassMinUnits)"));

			// A live shared target is never replaced by an approval.
			Assert.That(leader, Does.Contain("if (!TeamLeadCA.HarassLive(now, team.HarassTick, Info.TeamLeadHarassTicks) || team.HarassTargetId == 0)"));
		}

		[Test]
		public void TheActiveAllyPredicateAndTheSwitchAndTheValueSourceAreWiredToTheP1Classes()
		{
			var text = RepoFile(Bots + "SquadManagerBotModuleCA.RaidHarv.cs");
			Assert.That(text, Does.Contain("RaidHarvGroup.ActiveAlly(p == Player, Player.RelationshipWith(p) == PlayerRelationship.Ally, p.NonCombatant, p.WinState != WinState.Undefined)"));
			Assert.That(text, Does.Contain("internal bool RaidHarvOn => Info.RaidHarvEnabled && !ablateRaidHarv.Off(Player, AblationIds.RaidHarv);"));
			Assert.That(text, Does.Contain("(raidKeys ??= RaidKeyPriors.FromInfo(Info)).Read(key)"), "P2a reads the priors; the learned snapshot is the P3 wiring");
			Assert.That(text, Does.Contain("public partial class SquadManagerBotModuleCA : IBotScoutInterestProvider"));
			Assert.That(text, Does.Contain("IReadOnlyList<CPos> IBotScoutInterestProvider.ScoutInterestCells"));

			// P2a has no call sites: the existing launch, pick and team-lead files still do not mention the raid (P2b adds the hooks and replaces this pin).
			foreach (var file in new[] { "SquadManagerBotModuleCA.TargetPick.cs", "SquadManagerBotModuleCA.HarassCap.cs", "SquadManagerBotModuleCA.TeamLead.cs", "SquadManagerBotModuleCA.cs", "SquadManagerBotModuleCA.EnemyScan.cs" })
				Assert.That(RepoFile(Bots + file), Does.Not.Contain("RaidHarv"), file);
		}
	}
}
