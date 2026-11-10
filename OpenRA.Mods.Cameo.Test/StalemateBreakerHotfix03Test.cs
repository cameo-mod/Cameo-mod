#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	// IMPL01-HOTFIX03 (IMPL02-A and IMPL02-B), switch-ON path only (the breaker never runs with CD_stalemate_breaker off).
	// A: the breaker pool admits the idle members of every squad type, not only Rush and Guerrilla (FREEZE6 ON smoke:
	//    pool_too_small was 56% of the attempts after tick 15k).
	// B: the breaker is blocked, and a live wave ended, only by a visible home threat or the home latch, not by any Emergency'
	//    (home_emergency was 29% of the attempts and one live wave was ended after 89 ticks).
	// The world-facing module has no unit fixture in this project, so the decisions live in pure predicates (tested on state) and the
	// module call sites are pinned in the source text.
	[TestFixture]
	public class StalemateBreakerHotfix03Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file));

		static string StalemateSource() => Source("mods/CA/Traits/BotModules/SquadManagerBotModuleCA.Stalemate.cs");

		static string Section(string text, string from, string to)
		{
			var start = text.IndexOf(from, StringComparison.Ordinal);
			Assert.That(start, Is.GreaterThanOrEqualTo(0), from);
			var end = text.IndexOf(to, start + from.Length, StringComparison.Ordinal);
			Assert.That(end, Is.GreaterThan(start), to);
			return text.Substring(start, end - start);
		}

		static readonly SquadCAType[] OtherTypes = Enum.GetValues<SquadCAType>()
			.Where(t => t is not (SquadCAType.Rush or SquadCAType.Guerrilla)).ToArray();

		// ---- IMPL02-A: pool membership ----

		[Test]
		public void APoolPredicateAdmitsAQuietSquadOfAnyOtherType()
		{
			// The pool predicate itself: any type whose squad is quiet contributes (what "quiet" means per type is AQuietIsDecidedPerType...).
			Assert.That(OtherTypes, Does.Contain(SquadCAType.Protection));
			Assert.That(OtherTypes, Does.Contain(SquadCAType.Artillery));
			foreach (var type in OtherTypes)
				Assert.That(StalemateBreakerPolicyCA.PoolContributes(type, true), Is.True, type.ToString());
		}

		[Test]
		public void APoolPredicateRejectsABusySquadOfAnyOtherType()
		{
			foreach (var type in OtherTypes)
				Assert.That(StalemateBreakerPolicyCA.PoolContributes(type, false), Is.False, type.ToString());
		}

		[Test]
		public void ARushAndGuerrillaSquadsStillCountWhetherQuietOrNot()
		{
			// Unchanged FREEZE7 behaviour: their members count in N0/V0 and are drafted only while the squad is quiet (Transferable).
			foreach (var quiet in new[] { true, false })
			{
				Assert.That(StalemateBreakerPolicyCA.PoolContributes(SquadCAType.Rush, quiet), Is.True);
				Assert.That(StalemateBreakerPolicyCA.PoolContributes(SquadCAType.Guerrilla, quiet), Is.True);
			}
		}

		[Test]
		public void APoolSizeAndAdmissionBarsAreUnchanged()
		{
			Assert.That(StalemateBreakerPolicyCA.MinimumUnits, Is.EqualTo(8));
			Assert.That(StalemateBreakerPolicyCA.MinimumValue, Is.EqualTo(6000));
			Assert.That(StalemateBreakerPolicyCA.CooldownTicks, Is.EqualTo(5000));
			Assert.That(StalemateBreakerCA.AttemptInterval, Is.EqualTo(150));
		}

		[Test]
		public void ATheLivePoolUsesThePerTypeQuietPredicateAndTheTelemetryPoolKeepsItsLegacyFilter()
		{
			var src = StalemateSource();
			var pool = Section(src, "StalematePool(IBotUnitLeases leases)", "StalematePoolSize()");
			Assert.That(pool, Does.Contain("StalemateSquadQuiet(s.Type, s.FuzzyStateMachine?.CurrentState, s.Parent != null && s.Parent.IsValid)"));
			Assert.That(pool, Does.Contain("StalemateBreakerPolicyCA.PoolContributes(s.Type, quiet)"));
			Assert.That(pool, Does.Contain("ReferenceEquals(s, stalemateSquad)"), "the live breaker squad is still never a source");
			Assert.That(pool, Does.Contain("byId[u.Actor.ActorID] = (u.Actor, UnitValue(u.Actor), s, quiet)"), "Transferable stays the quiet flag");

			// M2 (Workhorse review): the read-only pool also feeds pool_count/pool_value on the OFF arm through the CE diagnostics.
			var readOnly = Section(src, "StalemateEligiblePoolReadOnly(IBotUnitLeases leases)", "// ---- candidates");
			Assert.That(readOnly, Does.Contain("if (s.Type is SquadCAType.Rush or SquadCAType.Guerrilla)"));
			Assert.That(readOnly, Does.Not.Contain("PoolContributes"));
			Assert.That(readOnly, Does.Not.Contain("StalemateSquadQuiet"));
		}

		[Test]
		public void AQuietIsDecidedPerTypeOverTheRealStateClasses()
		{
			static bool Quiet(SquadCAType type, IState state, bool parent = false) => SquadManagerBotModuleCA.StalemateSquadQuiet(type, state, parent);

			// Rush and Guerrilla: unchanged, GroundUnitsIdleStateCA only.
			foreach (var type in new[] { SquadCAType.Rush, SquadCAType.Guerrilla })
			{
				Assert.That(Quiet(type, new GroundUnitsIdleStateCA()), Is.True, type.ToString());
				Assert.That(Quiet(type, new GroundUnitsAttackMoveStateCA()), Is.False, type.ToString());
				Assert.That(Quiet(type, null), Is.False, type.ToString());
			}

			// A turtle- or steamroller-instance assault squad is an ordinary Rush squad: an idle one is quiet, a marching one is not.
			Assert.That(Quiet(SquadCAType.Harass, new HarasserUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.Harass, new GroundUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.Harass, new GroundUnitsAttackMoveStateCA()), Is.False);

			// Escorts: quiet only in their idle state and only with no valid parent assault.
			Assert.That(Quiet(SquadCAType.Artillery, new ArtilleryUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.Artillery, new ArtilleryUnitsIdleStateCA(), parent: true), Is.False);
			Assert.That(Quiet(SquadCAType.Artillery, new GroundUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.Support, new SupportUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.Support, new SupportUnitsIdleStateCA(), parent: true), Is.False);
			Assert.That(Quiet(SquadCAType.FireSupport, new FireSupportUnitsIdleStateCA()), Is.True);
			Assert.That(Quiet(SquadCAType.FireSupport, new FireSupportUnitsIdleStateCA(), parent: true), Is.False);

			// Never quiet: Protection and Stealth idle classes act on the next tick; air, naval and the rest are not ground pool sources.
			Assert.That(Quiet(SquadCAType.Protection, new UnitsForProtectionIdleState()), Is.False);
			Assert.That(Quiet(SquadCAType.Stealth, new StealthUnitsIdleStateCA()), Is.False);
			Assert.That(Quiet(SquadCAType.Naval, new NavyUnitsIdleState()), Is.False);
			Assert.That(Quiet(SquadCAType.Air, new AirIdleStateCA()), Is.False);
		}

		[Test]
		public void ATheInitialStateOfEverySquadTypeMatchesTheQuietRule()
		{
			// The M1 defect: the quiet test was a GroundUnitsIdleStateCA check, and only Rush and Guerrilla start there. Pin the real
			// constructor mapping (SquadCA.cs) so a changed initial state is caught: type -> state class the rule expects.
			var ctor = Section(Source("mods/CA/Traits/BotModules/Squads/SquadCA.cs"), "switch (type)", "// The production factory");
			string[][] expected =
			{
				new[] { "Guerrilla", "GroundUnitsIdleStateCA" }, new[] { "Rush", "GroundUnitsIdleStateCA" }, new[] { "Harass", "HarasserUnitsIdleStateCA" },
				new[] { "Artillery", "ArtilleryUnitsIdleStateCA" }, new[] { "Support", "SupportUnitsIdleStateCA" },
				new[] { "FireSupport", "FireSupportUnitsIdleStateCA" }, new[] { "Protection", "UnitsForProtectionIdleState" },
				new[] { "Stealth", "StealthUnitsIdleStateCA" }
			};
			foreach (var e in expected)
			{
				var c = ctor.IndexOf("case SquadCAType." + e[0] + ":", StringComparison.Ordinal);
				Assert.That(c, Is.GreaterThanOrEqualTo(0), e[0]);
				var line = ctor.Substring(c, ctor.IndexOf("break;", c, StringComparison.Ordinal) - c);
				Assert.That(line, Does.Contain("new " + e[1] + "()"), e[0]);
			}

			// With those initial states the types that start out quiet (parentless) are exactly the ones the rule expects.
			Assert.That(SquadManagerBotModuleCA.StalemateSquadQuiet(SquadCAType.Rush, new GroundUnitsIdleStateCA(), false), Is.True);
			Assert.That(SquadManagerBotModuleCA.StalemateSquadQuiet(SquadCAType.Harass, new HarasserUnitsIdleStateCA(), false), Is.True);
			Assert.That(SquadManagerBotModuleCA.StalemateSquadQuiet(SquadCAType.Protection, new UnitsForProtectionIdleState(), false), Is.False);
			Assert.That(SquadManagerBotModuleCA.StalemateSquadQuiet(SquadCAType.Stealth, new StealthUnitsIdleStateCA(), false), Is.False);
		}

		// ---- IMPL02-B: the home block ----

		[Test]
		public void BEmergencyWithoutHomeThreatOrLatchAdmits()
		{
			// Emergency' by a home production loss alone, or by the loss branch with nothing at home, is an Emergency but not a home block.
			Assert.That(MasterAiEval.StalemateEmergency(true, false, false, false), Is.True);
			Assert.That(MasterAiEval.StalemateEmergency(false, true, false, false), Is.False, "loss branch alone is not an emergency");
			Assert.That(StalemateBreakerPolicyCA.HomeBlocksBreaker(false, false), Is.False);
		}

		[Test]
		public void BHomeThreatBlocks()
		{
			Assert.That(StalemateBreakerPolicyCA.HomeBlocksBreaker(true, false), Is.True);
		}

		[Test]
		public void BHomeLatchBlocks()
		{
			Assert.That(StalemateBreakerPolicyCA.HomeBlocksBreaker(false, true), Is.True);
			Assert.That(StalemateBreakerPolicyCA.HomeBlocksBreaker(true, true), Is.True);
		}

		[Test]
		public void BALiveWaveSurvivesAnEmergencyWithoutThreatAndEndsOnAThreat()
		{
			var timing = new StalemateBreakerPolicyCA.TimingCA(1000, 750, 1500, 30000);
			StalemateBreakerWaveCA NewWave() => new(0, Enumerable.Range(1, 10).Select(i => new StalemateWaveMemberCA((uint)i, 1, 100)).ToArray(),
				timing, 10, 1000, 1000, 1);

			var calm = NewWave();
			Assert.That(calm.Continue(10, true, true, StalemateBreakerPolicyCA.HomeBlocksBreaker(false, false), true, false), Is.True);
			Assert.That(calm.End, Is.EqualTo(StalemateWaveEndCA.None));

			var hot = NewWave();
			Assert.That(hot.Continue(10, true, true, StalemateBreakerPolicyCA.HomeBlocksBreaker(true, false), true, false), Is.False);
			Assert.That(hot.End, Is.EqualTo(StalemateWaveEndCA.Emergency));

			var latched = NewWave();
			Assert.That(latched.Continue(10, true, true, StalemateBreakerPolicyCA.HomeBlocksBreaker(false, true), true, false), Is.False);
			Assert.That(latched.End, Is.EqualTo(StalemateWaveEndCA.Emergency));
		}

		[Test]
		public void BRetreatDeferralFollowsTheSameHomePredicate()
		{
			static bool Defer(bool homeBlocks) => StalemateBreakerPolicyCA.DeferRetreat(true, true, true, false, homeBlocks,
				now: 2000, waveStart: 1000, lastProgress: 1900, startingValue: 1000, confirmedLossValue: 0,
				noProgressDuration: 1500, absoluteDuration: 3000);

			Assert.That(Defer(StalemateBreakerPolicyCA.HomeBlocksBreaker(false, false)), Is.True);
			Assert.That(Defer(StalemateBreakerPolicyCA.HomeBlocksBreaker(true, false)), Is.False);
		}

		[Test]
		public void BEveryModuleHomeCheckUsesTheOneThreatOrLatchPredicate()
		{
			var src = StalemateSource();
			Assert.That(src, Does.Not.Contain("StalemateHomeEmergency"), "no module decision reads the any-Emergency flag any more");
			var helper = Section(src, "static bool StalemateHomeBlocks(", "static string UrgencyName");
			Assert.That(helper, Does.Contain("provider == null"), "fail closed without a provider");
			Assert.That(helper, Does.Contain("StalemateBreakerPolicyCA.HomeBlocksBreaker(provider.StalemateHomeThreat, provider.StalemateHomeLatch)"));

			var admission = Section(src, "void StalemateTryAdmission(", "void StalemateRunAdmission(");
			// FREEZE9 CF seam: the three sites read StalemateBlocked, which keeps this predicate with CG off (FreezeNineSeamsTest).
			Assert.That(admission, Does.Contain("else if (StalemateBlocked(provider))"));
			Assert.That(src, Does.Contain("wave.Continue(t, provider != null, true, StalemateBlocked(provider), status.Known, status.Destroyed)"));
			Assert.That(src, Does.Contain("StalemateBlocked(provider), t, wave.C, wave.LastProgressTick"));
			Assert.That(src, Does.Contain(": StalemateHomeBlocks(provider);"));
		}

		[Test]
		public void BTheHoldReasonKeepsItsTelemetryName()
		{
			// Row checkers and the run sheets key on "home_emergency"; only the predicate changed, not the schema.
			Assert.That(Section(StalemateSource(), "void StalemateTryAdmission(", "void StalemateRunAdmission("),
				Does.Contain("earlyHold = \"home_emergency\""));
		}
	}
}
