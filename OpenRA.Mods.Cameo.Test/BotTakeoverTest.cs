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
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotTakeoverTest
	{
		static readonly TakeoverLastPlayerPolicy[] AllPolicies = Enum.GetValues<TakeoverLastPlayerPolicy>();

		static TakeoverSeat Seat(string name, int team, WinState state = WinState.Undefined, bool combatant = true)
		{
			return new TakeoverSeat(name, team, state, combatant);
		}

		static Order OrderWithString(string orderString)
		{
			var order = (Order)RuntimeHelpers.GetUninitializedObject(typeof(Order));
			OrderString(order) = orderString;
			return order;
		}

		[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "OrderString")]
		static extern ref string OrderString(Order order);

		// ---------------------------------------------------------------------
		// R3: more than two teams alive -> always take over.
		// ---------------------------------------------------------------------

		[Test]
		public void MoreThanTwoTeamsAlwaysTakesOver()
		{
			foreach (var trigger in new[] { TakeoverTrigger.Disconnect, TakeoverTrigger.Surrender })
			foreach (var teammate in new[] { true, false })
			foreach (var policy in AllPolicies)
				Assert.That(BotTakeoverTracker.Decide(3, teammate, trigger, policy),
					Is.EqualTo(TakeoverDecision.Takeover),
					$"teams=3 teammate={teammate} trigger={trigger} policy={policy}");
		}

		// ---------------------------------------------------------------------
		// R4 (2026-10-04): <= 2 teams -> takeover unless the seat is the last
		// undefeated player of ANY kind on its team.
		// ---------------------------------------------------------------------

		[Test]
		public void TwoTeamsWithUndefeatedTeammateTakesOver()
		{
			foreach (var trigger in new[] { TakeoverTrigger.Disconnect, TakeoverTrigger.Surrender })
				Assert.That(BotTakeoverTracker.Decide(2, true, trigger, TakeoverLastPlayerPolicy.Defeat),
					Is.EqualTo(TakeoverDecision.Takeover), trigger.ToString());
		}

		[Test]
		public void LastPlayerSurrenderDefeats()
		{
			Assert.That(BotTakeoverTracker.Decide(2, false, TakeoverTrigger.Surrender, TakeoverLastPlayerPolicy.Defeat),
				Is.EqualTo(TakeoverDecision.Defeat));
			Assert.That(BotTakeoverTracker.Decide(1, false, TakeoverTrigger.Surrender, TakeoverLastPlayerPolicy.Takeover),
				Is.EqualTo(TakeoverDecision.Defeat), "surrender ignores the disconnect policy");
		}

		[Test]
		public void DisconnectIsAlwaysTakeoverUnderTheDefaultPolicy()
		{
			// Maintainer 2026-10-04: a disconnect is ALWAYS a takeover — last player or not,
			// 1v1 included. Only a last-player surrender still defeats.
			foreach (var teams in new[] { 1, 2 })
				Assert.That(BotTakeoverTracker.Decide(teams, false, TakeoverTrigger.Disconnect,
						TakeoverLastPlayerPolicy.Takeover),
					Is.EqualTo(TakeoverDecision.Takeover), $"teams={teams} last player disconnect");
		}

		[Test]
		public void LastPlayerDisconnectFollowsTheSinglePolicy()
		{
			// The policy point stays selectable for the maintainer; Takeover is the default.
			Assert.That(BotTakeoverTracker.Decide(2, false, TakeoverTrigger.Disconnect, TakeoverLastPlayerPolicy.Idle),
				Is.EqualTo(TakeoverDecision.Idle));
			Assert.That(BotTakeoverTracker.Decide(2, false, TakeoverTrigger.Disconnect, TakeoverLastPlayerPolicy.Defeat),
				Is.EqualTo(TakeoverDecision.Defeat));
		}

		// ---------------------------------------------------------------------
		// R2: teams-alive counting.
		// ---------------------------------------------------------------------

		[Test]
		public void TeamsAliveCountsDistinctTeamsAndSoloSeats()
		{
			var seats = new[]
			{
				Seat("a1", 1), Seat("a2", 1),                  // one team with two seats
				Seat("b1", 2, WinState.Lost),                  // defeated team doesn't count
				Seat("ffa", 0),                                // no-team player = team of one
				Seat("ref", 3, WinState.Undefined, false)      // noncombatant excluded
			};

			Assert.That(BotTakeoverTracker.CountTeamsAlive(seats), Is.EqualTo(2));
		}

		[Test]
		public void AiOnlyTeamCountsAsAlive()
		{
			// R5: a team whose seats are all AI (lobby or takeover) is still a live team
			// for counting; nothing auto-defeats it.
			var seats = new[] { Seat("bot1", 1), Seat("bot2", 1), Seat("human", 2) };
			Assert.That(BotTakeoverTracker.CountTeamsAlive(seats), Is.EqualTo(2));
		}

		[Test]
		public void UndefeatedTeammateOfAnyKindCounts()
		{
			var seats = new[] { Seat("human1", 1), Seat("bot1", 1), Seat("enemy", 2) };
			var subject = Seat("human1", 1);

			Assert.That(BotTakeoverTracker.HasUndefeatedTeammate(seats, subject), Is.True,
				"an undefeated AI teammate keeps the seat out of the last-player branch");
			Assert.That(BotTakeoverTracker.HasUndefeatedTeammate(seats, Seat("solo", 0)), Is.False,
				"a no-team player is a team of one");
			Assert.That(BotTakeoverTracker.HasUndefeatedTeammate(
				new[] { Seat("a", 1), Seat("b", 1, WinState.Lost) }, Seat("a", 1)), Is.False,
				"a defeated teammate does not count");
		}

		// ---------------------------------------------------------------------
		// Order authority.
		// ---------------------------------------------------------------------

		[Test]
		public void ValidatorMatrix()
		{
			var human = new Session.Client { Index = 1, Slot = "a" };
			var bot = new Session.Client { Index = 2, Slot = "b", Bot = "hard", BotControllerClientIndex = 1 };

			// Stock rules preserved.
			Assert.That(CameoValidateOrder.OrderIsValid(1, human, 1, false, -1, true), Is.True, "owner client");
			Assert.That(CameoValidateOrder.OrderIsValid(9, human, 1, false, -1, true), Is.False, "foreign client");
			Assert.That(CameoValidateOrder.OrderIsValid(1, null, 1, false, -1, true), Is.False, "missing session row");
			Assert.That(CameoValidateOrder.OrderIsValid(1, bot, 2, false, -1, true), Is.True, "bot controller");
			Assert.That(CameoValidateOrder.OrderIsValid(9, bot, 2, false, -1, true), Is.False, "non-controller bot order");
			Assert.That(CameoValidateOrder.OrderIsValid(1, human, 1, false, -1, false), Is.False, "AcceptsOrder still gates");

			// Takeover seat: only the CURRENT elected controller, whatever the lobby rows say.
			Assert.That(CameoValidateOrder.OrderIsValid(5, null, 1, true, 5, true), Is.True, "elected controller");
			Assert.That(CameoValidateOrder.OrderIsValid(5, human, 1, true, 5, true), Is.True,
				"controller passes even when the surrendered human's row still exists");
			Assert.That(CameoValidateOrder.OrderIsValid(1, human, 1, true, 5, true), Is.False,
				"the surrendered human's own orders are rejected");
			Assert.That(CameoValidateOrder.OrderIsValid(4, null, 1, true, 5, true), Is.False,
				"a stale controller is rejected after re-election");
			Assert.That(CameoValidateOrder.OrderIsValid(5, null, 1, true, 5, false), Is.False,
				"AcceptsOrder still gates the controller");
		}

		[Test]
		public void ControllerElectionIsDeterministic()
		{
			var connected = new[] { 7, 2, 9, 4 };
			var first = BotTakeoverTracker.ElectController(connected);
			Assert.That(first, Is.EqualTo(2));
			Assert.That(BotTakeoverTracker.ElectController(connected.Reverse()), Is.EqualTo(first),
				"iteration order does not change the electee");

			var second = BotTakeoverTracker.ElectController(connected.Where(id => id != 2));
			Assert.That(second, Is.EqualTo(4), "the electee re-elects to the next lowest index");
			Assert.That(BotTakeoverTracker.ElectController(Enumerable.Empty<int>()), Is.EqualTo(-1));
		}

		// ---------------------------------------------------------------------
		// Surrender intercept: IResolveOrder dispatch must reach CameoMissionObjectives'
		// re-listed implementation, once, without touching base's ForceDefeat.
		// ---------------------------------------------------------------------

		sealed class ProbeCameoMissionObjectives : CameoMissionObjectives
		{
			public int TakeoverCalls;

			public ProbeCameoMissionObjectives(OpenRA.Player player, CameoMissionObjectivesInfo info)
				: base(player, info) { }

			internal override bool TakeoverSurrender(Actor self)
			{
				TakeoverCalls++;
				return true;
			}
		}

		[Test]
		public void SurrenderDispatchHitsTheCameoImplementation()
		{
			var player = (OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var probe = new ProbeCameoMissionObjectives(player, new CameoMissionObjectivesInfo());

			// If IResolveOrder had bound to MissionObjectives' implicit implementation, the
			// probe would never run: this asserts the re-listed interface dispatch works.
			((IResolveOrder)probe).ResolveOrder(null, OrderWithString("Surrender"));
			Assert.That(probe.TakeoverCalls, Is.EqualTo(1), "the surrender was resolved exactly once via the cameo intercept");
		}

		[Test]
		public void OtherOrdersNeverReachTheIntercept()
		{
			var player = (OpenRA.Player)RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var probe = new ProbeCameoMissionObjectives(player, new CameoMissionObjectivesInfo());
			((IResolveOrder)probe).ResolveOrder(null, OrderWithString("Move"));
			Assert.That(probe.TakeoverCalls, Is.EqualTo(0));
		}

		// ---------------------------------------------------------------------
		// boss_review T1-T4 corrections (2026-10-04).
		// ---------------------------------------------------------------------

		[Test]
		public void LobbyTeamsComeFromClientsNotMapReferences()
		{
			// T2: the engine builds players from the unchanged map reference (all map
			// teams 0) and applies lobby teams only via SetupPlayerMasks. The snapshot
			// must read the client row for EVERY slot-bound player, bots included.
			var lobby = new Session();
			lobby.Clients.Add(new Session.Client { Index = 1, Slot = "human_slot", Team = 1 });
			lobby.Clients.Add(new Session.Client { Index = 2, Slot = "bot_a", Bot = "hard", Team = 2 });
			lobby.Clients.Add(new Session.Client { Index = 3, Slot = "bot_b", Bot = "hard", Team = 2 });
			lobby.Clients.Add(new Session.Client { Index = 4 });                 // spectator: no slot

			Assert.That(BotTakeoverTracker.LobbyTeamOf(lobby, "human_slot", 0), Is.EqualTo(1));
			Assert.That(BotTakeoverTracker.LobbyTeamOf(lobby, "bot_a", 0), Is.EqualTo(2));
			Assert.That(BotTakeoverTracker.LobbyTeamOf(lobby, "bot_b", 0), Is.EqualTo(2));
			Assert.That(BotTakeoverTracker.LobbyTeamOf(lobby, "map_side_player", 7), Is.EqualTo(7),
				"players with no lobby client keep their map team");
		}

		[Test]
		public void HumanVsTwoBotTeamIsTwoTeamsLastSurrenderDefeats()
		{
			// T2 regression: human on team 1 vs two lobby bots on team 2 is TWO teams.
			// Reading map teams would give three solo seats -> a wrongful "3-team" takeover.
			var seats = new[] { Seat("human", 1), Seat("bot_a", 2), Seat("bot_b", 2) };

			Assert.That(BotTakeoverTracker.CountTeamsAlive(seats), Is.EqualTo(2));
			Assert.That(BotTakeoverTracker.HasUndefeatedTeammate(seats, Seat("human", 1)), Is.False);
			Assert.That(
				BotTakeoverTracker.Decide(
					BotTakeoverTracker.CountTeamsAlive(seats), false, TakeoverTrigger.Surrender,
					TakeoverLastPlayerPolicy.Takeover),
				Is.EqualTo(TakeoverDecision.Defeat),
				"the last human's surrender defeats — a lobby bot team is not a third team");
		}

		[Test]
		public void ResolvedBotConditionsIncludeLobbyGrants()
		{
			// T3: a real hard bot would carry inc3_frans_services (ai.yaml GrantConditionOnBotOwner
			// names hard); the takeover seat resolves the same set or the Frans services never wake.
			var grant = new GrantConditionOnBotOwnerInfo();
			FieldLoader.LoadFieldOrProperty(grant, "Condition", "inc3_frans_services");
			FieldLoader.LoadFieldOrProperty(grant, "Bots", "hard,fransbot");

			var resolved = BotTakeoverTracker.ResolvedBotConditions(
				new[] { "genericbot", "hardbot" }, new[] { grant }, "hard");
			Assert.That(resolved, Is.EquivalentTo(new[] { "genericbot", "hardbot", "inc3_frans_services" }));

			var otherType = BotTakeoverTracker.ResolvedBotConditions(
				new[] { "genericbot" }, new[] { grant }, "easy");
			Assert.That(otherType, Is.EquivalentTo(new[] { "genericbot" }),
				"a bot type the grant does not name gets no extra condition");
		}

		[Test]
		public void OpenSeatClosesOnResolveDisconnectOrTakeover()
		{
			// T1: a bound human seat keeps the match-log capture open only while it
			// can still convert; none of the closed states can reopen.
			Assert.That(BotTakeoverTracker.SeatStillOpen(WinState.Undefined, false, true), Is.True);
			Assert.That(BotTakeoverTracker.SeatStillOpen(WinState.Lost, false, true), Is.False, "defeated");
			Assert.That(BotTakeoverTracker.SeatStillOpen(WinState.Undefined, true, true), Is.False, "already a bot");
			Assert.That(BotTakeoverTracker.SeatStillOpen(WinState.Undefined, false, false), Is.False,
				"disconnected without takeover cannot convert later");
		}

		[Test]
		public void CaptureWaitsForLoggableResolutionAndOpenSeats()
		{
			// T1: lobby bots resolving early must not close the record while a human seat
			// can still convert — its later takeover record would never be written.
			Assert.That(AiMatchLogWriter.CaptureReady(new[] { WinState.Lost, WinState.Won }, true), Is.False,
				"resolved lobby bots + open seat: keep waiting");
			Assert.That(AiMatchLogWriter.CaptureReady(new[] { WinState.Lost, WinState.Won }, false), Is.True);
			Assert.That(AiMatchLogWriter.CaptureReady(new[] { WinState.Undefined }, false), Is.False,
				"an unresolved logged seat still waits");
			Assert.That(AiMatchLogWriter.CaptureReady(new WinState[0], false), Is.True,
				"no logged seats and none pending: close");
			Assert.That(AiMatchLogWriter.CaptureReady(new WinState[0], true), Is.False,
				"a human-only match with possible takeovers stays open");
		}

		[Test]
		public void SpectatorNeverControlsButCountsAsConnected()
		{
			// T4: connectivity includes spectators (a spectator admin is visible for log
			// ownership) while electability stays bound-playable only.
			var connected = new HashSet<int> { 0, 2, 3 };           // 0 = spectator admin, 2/3 bound players
			var bound = new HashSet<int> { 2, 3 };
			var electee = BotTakeoverTracker.ElectController(
				BotTakeoverTracker.ElectableClients(connected, bound, new HashSet<int>()));

			Assert.That(electee, Is.EqualTo(2), "the spectator admin is never elected controller");
			Assert.That(connected.Contains(0), Is.True, "the spectator admin still reads as connected");

			var afterTwo = BotTakeoverTracker.ElectController(
				BotTakeoverTracker.ElectableClients(connected, bound, new HashSet<int> { 2 }));
			Assert.That(afterTwo, Is.EqualTo(3), "a taken-over bound client leaves the electable set");
		}
	}
}
