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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;

namespace OpenRA.Mods.Cameo.Test
{
	/// <summary>
	/// CONSOLIDATE-02 Q6 (CONSOLIDATE_02_SQUAD_DESIGN.md 4.1 Step 3; row sq_target_pick under sq_pipeline_02): one target order per purpose (MAIN, HARASS, DEFENCE, LOCAL), pure,
	/// and the pickers that followed their own order (the squad FindNewTarget, the leader's pick, DF-4, the defence squads' target) now follow it. Fog honesty: an enemy the bot
	/// does not see is never picked.
	/// </summary>
	[TestFixture]
	public class Squad02Q6Test
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

		static TargetSeen Seen(uint id, long distSq, bool observed = true, bool armed = true, bool structure = false) => new(id, distSq, observed, armed, structure);

		static IEnumerable<List<T>> Permutations<T>(List<T> items)
		{
			if (items.Count <= 1)
			{
				yield return new List<T>(items);
				yield break;
			}

			for (var i = 0; i < items.Count; i++)
			{
				var rest = new List<T>(items);
				rest.RemoveAt(i);
				foreach (var tail in Permutations(rest))
				{
					tail.Insert(0, items[i]);
					yield return tail;
				}
			}
		}

		[Test]
		public void EveryPurposeHasItsOwnFixedOrder()
		{
			Assert.That(TargetPick.Order(TargetPurpose.Main), Is.EqualTo(new[] { TargetTier.Focus, TargetTier.Visible, TargetTier.Frozen, TargetTier.Hunt }),
				"MAIN: the finish focus, what is seen, what is remembered, one hunt cell");
			Assert.That(TargetPick.Order(TargetPurpose.Harass), Is.EqualTo(new[] { TargetTier.SharedHarass, TargetTier.HighValue, TargetTier.Visible, TargetTier.Frozen, TargetTier.Hunt }),
				"HARASS: the team's shared target first, then high value, then as MAIN");
			Assert.That(TargetPick.Order(TargetPurpose.Defence), Is.EqualTo(new[] { TargetTier.ArmedAttacker, TargetTier.Attacker }),
				"DEFENCE: the nearest armed attacker, then the one that reported the hit");
			Assert.That(TargetPick.Order(TargetPurpose.Local), Is.EqualTo(new[] { TargetTier.InRadius }), "LOCAL: inside the fight radius only");
		}

		[Test]
		public void AllowedTiersRestrictTheOrderAndKeepItsSequence()
		{
			// DF-4 (X14): a far raid asks for the team's shared target or a remembered building, in that order.
			Assert.That(TargetPick.Order(TargetPurpose.Harass, TargetTier.SharedHarass | TargetTier.Frozen), Is.EqualTo(new[] { TargetTier.SharedHarass, TargetTier.Frozen }));

			// Only a raid that passed its priority roll tries a high-value target.
			Assert.That(TargetPick.Order(TargetPurpose.Harass, TargetTier.All & ~TargetTier.HighValue),
				Is.EqualTo(new[] { TargetTier.SharedHarass, TargetTier.Visible, TargetTier.Frozen, TargetTier.Hunt }));

			// The leader hunts only late with the finish mode (or an all-in with nothing known).
			Assert.That(TargetPick.Order(TargetPurpose.Main, TargetTier.All & ~TargetTier.Hunt), Is.EqualTo(new[] { TargetTier.Focus, TargetTier.Visible, TargetTier.Frozen }));

			// A tier of another purpose is never added.
			Assert.That(TargetPick.Order(TargetPurpose.Defence, TargetTier.Focus), Is.Empty);
			Assert.That(TargetPick.Order(TargetPurpose.Local, TargetTier.None), Is.Empty);
			Assert.That(TargetPick.Order(TargetPurpose.Main, TargetTier.ArmedAttacker | TargetTier.InRadius | TargetTier.SharedHarass), Is.Empty);
		}

		[Test]
		public void ResolveStopsAtTheFirstTierThatResolves()
		{
			var tried = new List<TargetTier>();
			var hit = TargetPick.Resolve(TargetPick.Order(TargetPurpose.Main), t =>
			{
				tried.Add(t);
				return t == TargetTier.Frozen;
			});

			Assert.That(hit, Is.EqualTo(TargetTier.Frozen));
			Assert.That(tried, Is.EqualTo(new[] { TargetTier.Focus, TargetTier.Visible, TargetTier.Frozen }), "the hunt cell is never reached while a building is remembered");

			tried.Clear();
			var none = TargetPick.Resolve(TargetPick.Order(TargetPurpose.Harass), t =>
			{
				tried.Add(t);
				return false;
			});

			Assert.That(none, Is.Null);
			Assert.That(tried, Is.EqualTo(TargetPick.Order(TargetPurpose.Harass)), "every tier is offered once, in order");
			Assert.That(TargetPick.Resolve(Array.Empty<TargetTier>(), _ => true), Is.Null);
		}

		[Test]
		public void ARaidTakesTheTeamTargetBeforeAnythingItSees()
		{
			// X13/X14: the shared harass target is the first thing a raid asks, so a far raid and a home raid end on the same one.
			var tried = new List<TargetTier>();
			TargetPick.Resolve(TargetPick.Order(TargetPurpose.Harass, TargetTier.All & ~TargetTier.HighValue), t =>
			{
				tried.Add(t);
				return t == TargetTier.SharedHarass;
			});

			Assert.That(tried, Is.EqualTo(new[] { TargetTier.SharedHarass }));
		}

		[Test]
		public void AnEnemyTheBotDoesNotSeeIsNeverPicked()
		{
			// Fog honesty: the nearest enemy (id 1) and a nearer structure (id 5) are not observed; the pick is the nearest one that is.
			var seen = new[] { Seen(1, 100, observed: false), Seen(2, 900), Seen(3, 400, observed: false), Seen(5, 10, observed: false, structure: true), Seen(4, 1600) };

			Assert.That(seen[TargetPick.Nearest(seen)].Id, Is.EqualTo(2u));
			Assert.That(seen[TargetPick.Nearest(seen, structuresFirst: true)].Id, Is.EqualTo(2u), "an unseen structure does not win the structures-first rule");
			Assert.That(seen[TargetPick.Nearest(seen, armedFirst: true)].Id, Is.EqualTo(2u));

			var unseen = new[] { Seen(1, 1, observed: false), Seen(2, 2, observed: false) };
			Assert.That(TargetPick.Nearest(unseen), Is.EqualTo(-1), "nothing seen = no pick");
			Assert.That(TargetPick.Nearest(Array.Empty<TargetSeen>()), Is.EqualTo(-1));
		}

		[Test]
		public void TheNearestPickIsATotalOrder()
		{
			// Plain: nearest, then the lowest id.
			var tie = new[] { Seen(9, 100), Seen(4, 100), Seen(7, 100) };
			Assert.That(tie[TargetPick.Nearest(tie)].Id, Is.EqualTo(4u));

			// Structures first (artillery): a far structure beats a near unit.
			var mixed = new[] { Seen(1, 10, structure: false), Seen(2, 1000, structure: true) };
			Assert.That(mixed[TargetPick.Nearest(mixed)].Id, Is.EqualTo(1u));
			Assert.That(mixed[TargetPick.Nearest(mixed, structuresFirst: true)].Id, Is.EqualTo(2u));

			// Armed first (defence): an unarmed harvester that hit first no longer draws the guard from a nearer-or-farther armed attacker.
			var guard = new[] { Seen(1, 10, armed: false), Seen(2, 5000, armed: true) };
			Assert.That(guard[TargetPick.Nearest(guard)].Id, Is.EqualTo(1u));
			Assert.That(guard[TargetPick.Nearest(guard, armedFirst: true)].Id, Is.EqualTo(2u));

			// With both rules the structure rule comes first, then armed, then the distance.
			var both = new[] { Seen(1, 10, armed: true, structure: false), Seen(2, 900, armed: false, structure: true), Seen(3, 800, armed: true, structure: true) };
			Assert.That(both[TargetPick.Nearest(both, structuresFirst: true, armedFirst: true)].Id, Is.EqualTo(3u));

			// The same candidates in any list order give the same pick (every client agrees).
			foreach (var order in Permutations(new List<TargetSeen> { Seen(8, 300), Seen(3, 300), Seen(6, 120, observed: false), Seen(5, 700, armed: false) }))
			{
				var pick = order[TargetPick.Nearest(order)];
				Assert.That(pick.Id, Is.EqualTo(3u));
			}
		}

		[Test]
		public void ArtilleryPrefersBuildingsAndTheFightRadiusIsAHardEdge()
		{
			Assert.That(TargetPick.PrefersStructures(true, 0, 5), Is.True, "the global preference");
			Assert.That(TargetPick.PrefersStructures(false, 2, 4), Is.True, "half artillery");
			Assert.That(TargetPick.PrefersStructures(false, 3, 4), Is.True);
			Assert.That(TargetPick.PrefersStructures(false, 1, 4), Is.False);
			Assert.That(TargetPick.PrefersStructures(false, 1, 1), Is.True);
			Assert.That(TargetPick.PrefersStructures(false, 0, 0), Is.False, "an empty squad prefers nothing");

			Assert.That(TargetPick.WithinFight(100, 100), Is.True);
			Assert.That(TargetPick.WithinFight(101, 100), Is.False, "LOCAL sees nothing outside the fight radius");
		}

		[Test]
		public void RaidsPickAsHarassAndEveryOtherSquadAsMain()
		{
			Assert.That(SquadManagerBotModuleCA.TargetPurposeOf(SquadCAType.Harass), Is.EqualTo(TargetPurpose.Harass));
			Assert.That(SquadManagerBotModuleCA.TargetPurposeOf(SquadCAType.Guerrilla), Is.EqualTo(TargetPurpose.Harass));
			Assert.That(SquadManagerBotModuleCA.TargetPurposeOf(SquadCAType.Rush), Is.EqualTo(TargetPurpose.Main));
			Assert.That(SquadManagerBotModuleCA.TargetPurposeOf(SquadCAType.Artillery), Is.EqualTo(TargetPurpose.Main));
			Assert.That(SquadManagerBotModuleCA.TargetPurposeOf(SquadCAType.Protection), Is.EqualTo(TargetPurpose.Main));
		}

		[Test]
		public void ThePickerIsPureAndTheRowIsRegisteredUnderTheMasterSwitch()
		{
			var pure = RepoFile(Bots + "Squads/TargetPick.cs");
			Assert.That(pure, Does.Not.Contain("World"), "no world access");
			Assert.That(pure, Does.Not.Contain("BotRng"), "no RNG");
			Assert.That(pure, Does.Not.Contain("Random"));
			Assert.That(pure, Does.Not.Contain("Actor "), "no actor is read; the caller offers what it observes");

			Assert.That(AblationIds.SqTargetPick, Is.EqualTo("sq_target_pick"));
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			Assert.That(wiring, Does.Contain("AblationLatch ablateSqTargetPick;"));
			Assert.That(wiring, Does.Contain("internal bool ArmyTargetPickOn => ArmyPipelineOn && !ablateSqTargetPick.Off(Player, AblationIds.SqTargetPick);"));
		}

		[Test]
		public void TheSquadPickFollowsTheOrderOfItsPurposeAndTheHighValueRollStaysWithTheCaller()
		{
			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			var pick = Between(wiring, "internal bool PickSquadTarget(", "// The finish focus' nearest known building");

			Assert.That(pick, Does.Contain("var tier = TargetPick.Resolve(TargetPick.Order(purpose, allowed), t => t switch"));
			Assert.That(pick, Does.Contain("TargetTier.Focus => TargetFocusFor(squad),"));
			Assert.That(pick, Does.Contain("TargetTier.SharedHarass => TargetSharedHarassFor(squad),"));
			Assert.That(pick, Does.Contain("TargetTier.HighValue => TargetHighValueFor(squad, from, risk),"));
			Assert.That(pick, Does.Contain("TargetTier.Visible => TargetVisibleFor(squad, source, risk, structures),"));
			Assert.That(pick, Does.Contain("TargetTier.Frozen => TargetFrozenFor(squad, from, risk, frozenNeedsFog),"));
			Assert.That(wiring, Does.Contain("if (needsFog && !FoggedScans)\n\t\t\t\treturn false;"), "a state pick keeps the fog guard of the old 6d fallback; DF-4 passes false and never had it");
			Assert.That(pick, Does.Contain("TargetTier.Hunt => TargetHuntFor(squad, source),"));
			Assert.That(pick, Does.Contain("if (!tryHighValue)\n\t\t\t\tallowed &= ~TargetTier.HighValue;"), "a high-value target only after the caller's roll");
			Assert.That(pick, Does.Contain("var risk = riskGated ? SquadValueOf(squad) : -1;"));
			Assert.That(pick, Does.Contain("(tier == TargetTier.HighValue || tier == TargetTier.Visible) && squad.TargetActor != null)\n\t\t\t\tHarassPublish(squad.TargetActor);"),
				"a raid's own pick becomes the team's target; a shared pick is never re-published");
			Assert.That(pick, Does.Contain("#if CAMEO_DEV_LOGGING\n\t\t\tDiagnosticSquadEvent(squad, \"target_pick\", reason: purpose + \":\" + tier);\n#endif"), "the tier tag is DEV only");

			// A squad only READS the finish focus the leader refreshes each team check; refreshing it from a squad would move the leader's target to the building nearest to that squad.
			var focus = Between(wiring, "bool TargetFocusFor(", "bool TargetSharedHarassFor(");
			Assert.That(focus, Does.Contain("if (team == null || !TeamLeadFinishWeakestOn || !team.FocusHasTarget || team.FocusTargetId == 0)"));
			Assert.That(focus, Does.Contain("var id = team.FocusTargetId;"));
			Assert.That(focus, Does.Not.Contain("TeamLeadFocusTarget("), "no refresh from a squad");
			Assert.That(focus, Does.Contain("var frozen = Player.FrozenActorLayer?.FromID(id);"), "a building not seen now is the remembered one");

			// The tiers read what the old pickers read.
			Assert.That(wiring, Does.Contain("var target = risk >= 0 ? FindClosestEnemy(source, risk, squad, structures) : FindClosestEnemy(source, squad, structures);"));
			Assert.That(wiring, Does.Contain("var target = risk >= 0 ? FindHighValueTarget(from, risk) : FindHighValueTarget(from);"));
			Assert.That(wiring, Does.Contain("var frozen = FindFrozenEnemyTarget(from, risk, squad);"));
			Assert.That(wiring, Does.Contain("if (!FinishModeActive || !Stalemate01CA.Hunts(squad.Type) || TeamLeadHuntsViaOrder(squad))"), "the squad hunt is the old one: not while the order hunts for the team");
			Assert.That(wiring, Does.Contain("return shared != null && !shared.IsDead && shared.IsInWorld && IsPreferredObservedEnemyUnit(shared) ? shared : null;"), "the shared target is taken only if seen");

			// The roll is drawn by the state from the same stream, and only for a raid.
			var states = Between(RepoFile(Bots + "Squads/States/GroundStatesCA.cs"), "protected bool FindNewTarget(", "class GroundUnitsIdleStateCA");
			Assert.That(states, Does.Contain("if (owner.SquadManager.ArmyTargetPickOn)"));
			Assert.That(states, Does.Contain("var tryHighValue = highValueCheck && SquadManagerBotModuleCA.TargetPurposeOf(owner.Type) == TargetPurpose.Harass"));
			Assert.That(states, Does.Contain("&& owner.SquadManager.Info.HighValueTargetPriority > BotRng.For(owner.Bot, nameof(GroundStateBaseCA)).Next(0, 100);"));
			Assert.That(states, Does.Contain("return owner.SquadManager.PickSquadTarget(owner, tryHighValue, riskCheck);"));
			Assert.That(states.IndexOf("ArmyTargetPickOn", StringComparison.Ordinal), Is.LessThan(states.IndexOf("if (highValueCheck)", StringComparison.Ordinal)), "row off falls through to the old body");
			Assert.That(states, Does.Contain("var highValueTargetRoll = BotRng.For(owner.Bot, nameof(GroundStateBaseCA)).Next(0, 100);"), "the old body is untouched");
			Assert.That(states, Does.Contain("var closestEnemy = FindClosestEnemy(owner, riskCheck);"));
		}

		[Test]
		public void TheLeaderPickIsTheMainOrderAndTheOldPickStaysWithItOff()
		{
			var lead = Between(RepoFile(Bots + "SquadManagerBotModuleCA.TeamLead.cs"), "bool TeamLeadPickTarget(", "// No risk gate on the leader's pick");
			Assert.That(lead, Does.Contain("if (ArmyTargetPickOn)\n\t\t\t\treturn TeamLeadPickMain(from, now, out target, out actorId);"));
			Assert.That(lead.IndexOf("ArmyTargetPickOn", StringComparison.Ordinal), Is.LessThan(lead.IndexOf("if (TeamLeadFocusTarget(from, now, out target, out actorId))", StringComparison.Ordinal)),
				"row off = the old body below the guard");
			Assert.That(lead, Does.Contain("var mainTarget = EffectiveMainTarget();"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			var main = Between(wiring, "bool TeamLeadPickMain(", "/// A defence squad's target (DEFENCE)");
			Assert.That(main, Does.Contain("var hunts = (TeamLeadHuntViaOrderOn && FinishModeActive) || teamLeadAllInPicking || StalemateScoutHunts;"), "the same hunt rule as before");
			Assert.That(main, Does.Contain("var allowed = hunts ? TargetTier.All : TargetTier.All & ~TargetTier.Hunt;"));
			Assert.That(main, Does.Contain("TargetPick.Resolve(TargetPick.Order(TargetPurpose.Main, allowed), t =>"));
			Assert.That(main, Does.Contain("return TeamLeadFocusTarget(from, now, out picked, out pickedId);"));
			Assert.That(main, Does.Contain("var seen = mainTarget != null ? FindClosestEnemy(World.Map.CellContaining(from), TeamLeadOpenRisk, mainTarget) : null;"), "no risk gate on the leader's pick");
			Assert.That(main, Does.Contain("if (!FoggedScans || FindFrozenEnemyTarget(from, -1) is not { } frozen)"));
			Assert.That(main, Does.Contain("var huntSource = teamLeadAllInPicking || StalemateScoutHunts ? TeamLeadHuntSample()"), "an all-in hunts from a ground unit");
		}

		[Test]
		public void Df4AndTheDefenceSquadsFollowTheirOrderAndTheOldTargetStaysWithItOff()
		{
			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");

			var df4 = Between(sm, "void ReactWithFastSquads(", "Player EffectiveMainTarget()");
			Assert.That(df4, Does.Contain("if (ArmyTargetPickOn)\n\t\t\t\t\tPickSquadTarget(sq, false, true, TargetTier.SharedHarass | TargetTier.Frozen, frozenNeedsFog: false);\n\t\t\t\telse\n\t\t\t\t{"),
				"X14: the shared harass target, else the remembered building");
			Assert.That(df4, Does.Contain("var target = FindFrozenEnemyTarget(leader.CenterPosition, SquadValueOf(sq), sq);"), "row off = the old DF-4 pick");

			var own = Between(sm, "var defenceTarget = ArmyTargetPickOn", "// LC5: reachable from INotifyDamage");
			Assert.That(own, Does.Contain("? PickDefenceTarget(protectTarget, victim != null && victim.IsInWorld ? victim.CenterPosition : attacker != null ? attacker.CenterPosition : World.Map.CenterOfCell(initialBaseCenter))"));
			Assert.That(own, Does.Contain(": protectTarget;"), "row off = the reporter, as before");
			Assert.That(own, Does.Contain("if (defenceTarget != null)\n\t\t\t\t\tprotectSq.TargetActor = defenceTarget;"));

			var ally = RepoFile(Bots + "SquadManagerBotModuleCA.DefendAlly.cs");
			Assert.That(ally, Does.Contain("var defenceTarget = ArmyTargetPickOn ? PickDefenceTarget(attacker, friendlyPos) : attacker;"));

			var wiring = RepoFile(Bots + "SquadManagerBotModuleCA.TargetPick.cs");
			var defence = wiring[wiring.IndexOf("internal Actor PickDefenceTarget(", StringComparison.Ordinal)..];
			Assert.That(defence, Does.Contain("IsPreferredEnemyUnit(a) && a.Info.HasTraitInfo<AttackBaseInfo>() && !a.Info.HasTraitInfo<HuskInfo>()"), "armed enemies only");
			Assert.That(defence, Does.Contain("&& !a.Info.HasTraitInfo<BuildingInfo>())"), "an enemy structure is not an attacker");
			Assert.That(defence, Does.Contain("new TargetSeen(a.ActorID, (a.CenterPosition - anchor).HorizontalLengthSquared, TargetObserved(a), true, false)"), "observed only, nearest to what is defended");
			Assert.That(defence, Does.Contain("var i = TargetPick.Nearest(seen);"));
			Assert.That(wiring, Does.Contain("bool TargetObserved(Actor a) => !FoggedScans || IsNotHiddenUnit(a);"));
		}

		[Test]
		public void TheStructurePreferenceReachesBothClosestEnemyPicksAndDefaultsOff()
		{
			var sm = RepoFile(Bots + "SquadManagerBotModuleCA.cs");
			Assert.That(sm, Does.Contain("internal Actor FindClosestEnemy(Actor sourceActor, SquadCA owner = null, bool preferStructures = false)"));
			Assert.That(sm, Does.Contain("if (Info.PreferBuildingTargets || preferStructures)"));
			Assert.That(sm, Does.Contain("internal Actor FindClosestEnemy(Actor sourceActor, int attackerValue, SquadCA owner = null, bool preferStructures = false)"));

			var risk = Between(sm, "internal Actor FindClosestEnemy(Actor sourceActor, int attackerValue, SquadCA owner = null, bool preferStructures = false)", "// Fogged scans require a currently visible actor");
			Assert.That(risk, Does.Contain("if (preferStructures)\n\t\t\t{\n\t\t\t\tvar visibleBuildings = visible.Where(IsPreferredEnemyBuilding).ToList();"), "only the observed candidates are re-ordered");
			Assert.That(risk, Does.Not.Contain("Info.PreferBuildingTargets"), "the risk-gated pick takes the preference only when the caller asks");
		}
	}
}
