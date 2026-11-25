// PERF-F1 (PERF_SCAN_BURST_01 section 5): one enemy scan per bot visit instead of one per squad call.
// FindClosestEnemy (3 overloads that scan the world) and FindHighValueTarget (2) each ran World.Actors.Where(predicate).ToList()
// over every actor, then re-filtered the result. Within one IBotTick call nothing a squad does changes the world (orders only
// queue), so the owner-independent steps give the same answer on every call of the pass: the world filter, the fog filter, the
// main-target preference and the visible subset. They are built once, on first use, and dropped when the tick ends.
// Same list content in the same order (World.Actors order), so every pick, tie-break and BotRng draw is unchanged.
// Shared lists are never modified in place: the risk gate filters into a new list (it used RemoveAll on the list it owned).
// Switch: Info.UseSharedEnemyScan (default true), ablation id pf_enemy_scan (DEV only). Off = the old per-call scan.
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.CA.Traits
{
	public partial class SquadManagerBotModuleCAInfo
	{
		[Desc("PERF-F1: build the enemy candidate lists once per bot tick and share them between squads (same lists, same order, same picks). False = every FindClosestEnemy / FindHighValueTarget call scans the world itself, as before.")]
		public readonly bool UseSharedEnemyScan = true;

		[Desc("PERF-F1 DEV check (compiled into DEV logging builds only): every shared list served from the cache is compared with a fresh scan and a PERF-F1 MISMATCH line is logged on a difference. Costs a scan per call, so keep it off in perf runs.")]
		public readonly bool VerifySharedEnemyScan = false;
	}

	public partial class SquadManagerBotModuleCA
	{
		sealed class EnemyScanPass
		{
			// IsPreferredEnemyUnit over World.Actors, World.Actors order.
			public List<Actor> Base;

			// FilterHiddenUnderFog(Base) then PreferOwned(main target); valid for PreferredFor.
			public List<Actor> Preferred;
			public Player PreferredFor;

			// Preferred restricted to IsNotHiddenUnit.
			public List<Actor> Visible;

			// High value targets after the fog filter and PreferOwned; valid for HighValueFor.
			public List<Actor> HighValue;
			public Player HighValueFor;

			public void Reset()
			{
				Base = Preferred = Visible = HighValue = null;
				PreferredFor = HighValueFor = null;
			}
		}

		readonly EnemyScanPass enemyScanStorage = new();

		// Non-null only inside IBotTick.BotTick while the switch is on. Calls made outside a tick (none today) scan afresh.
		EnemyScanPass enemyScan;
		AblationLatch ablateEnemyScan;

		bool EnemyScanShared => Info.UseSharedEnemyScan && !ablateEnemyScan.Off(Player, AblationIds.EnemyScan);

		void BeginEnemyScanPass()
		{
			if (!EnemyScanShared)
				return;

			enemyScanStorage.Reset();
			enemyScan = enemyScanStorage;
		}

		void EndEnemyScanPass()
		{
			enemyScan = null;
			enemyScanStorage.Reset();
		}

		// The old sequence, in one place: world filter, fog filter, main-target preference.
		List<Actor> EnemyCandidatesFresh(Player mainTarget)
		{
			var units = World.Actors.Where(IsPreferredEnemyUnit).ToList();
			units = FilterHiddenUnderFog(units, FoggedScans, IsNotHiddenUnit);
			return PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
		}

		/// <summary>The candidates every FindClosestEnemy starts from, before the squad's own tag preference. Read only: the
		/// returned list can be the shared one, so callers must not modify it.</summary>
		List<Actor> EnemyCandidates(Player mainTarget)
		{
			var scan = enemyScan;
			if (scan == null)
				return EnemyCandidatesFresh(mainTarget);

			if (scan.Preferred != null && scan.PreferredFor == mainTarget)
			{
#if CAMEO_DEV_LOGGING
				if (Info.VerifySharedEnemyScan)
					VerifyEnemyScan(scan.Preferred, EnemyCandidatesFresh(mainTarget), "candidates");
#endif
				return scan.Preferred;
			}

			scan.Base ??= World.Actors.Where(IsPreferredEnemyUnit).ToList();
			var fogged = FilterHiddenUnderFog(scan.Base, FoggedScans, IsNotHiddenUnit);
			scan.Preferred = PreferOwned(fogged, mainTarget == null ? null : a => a.Owner == mainTarget);
			scan.PreferredFor = mainTarget;
			scan.Visible = null;
			return scan.Preferred;
		}

		/// <summary>The enemies of one player: the same list the old `World.Actors.Where(owner == player &amp;&amp; IsPreferredEnemyUnit)` gave.
		/// A new list, so the caller may modify it.</summary>
		List<Actor> EnemyUnitsOf(Player targetPlayer)
		{
			var scan = enemyScan;
			if (scan == null)
				return World.Actors.Where(a => a.Owner == targetPlayer && IsPreferredEnemyUnit(a)).ToList();

			scan.Base ??= World.Actors.Where(IsPreferredEnemyUnit).ToList();
			return scan.Base.Where(a => a.Owner == targetPlayer).ToList();
		}

		/// <summary>The visible subset of <paramref name="units"/>; cached when <paramref name="units"/> is the shared candidate list.</summary>
		List<Actor> VisibleOfCandidates(List<Actor> units)
		{
			var scan = enemyScan;
			if (scan != null && ReferenceEquals(units, scan.Preferred))
			{
#if CAMEO_DEV_LOGGING
				if (Info.VerifySharedEnemyScan && scan.Visible != null)
					VerifyEnemyScan(scan.Visible, units.Where(IsNotHiddenUnit).ToList(), "visible");
#endif
				return scan.Visible ??= units.Where(IsNotHiddenUnit).ToList();
			}

			return units.Where(IsNotHiddenUnit).ToList();
		}

		/// <summary>High value targets after the fog filter and the main-target preference. Read only, like <see cref="EnemyCandidates"/>.</summary>
		List<Actor> HighValueCandidates(Player mainTarget)
		{
			var scan = enemyScan;
			if (scan != null && scan.HighValue != null && scan.HighValueFor == mainTarget)
			{
#if CAMEO_DEV_LOGGING
				if (Info.VerifySharedEnemyScan)
					VerifyEnemyScan(scan.HighValue, HighValueCandidatesFresh(mainTarget), "high-value");
#endif
				return scan.HighValue;
			}

			var units = HighValueCandidatesFresh(mainTarget);
			if (scan != null)
			{
				scan.HighValue = units;
				scan.HighValueFor = mainTarget;
			}

			return units;
		}

		List<Actor> HighValueCandidatesFresh(Player mainTarget)
		{
			var units = World.Actors.Where(IsHighValueTarget).ToList();
			if (FoggedScans)
				units = units.Where(IsNotHiddenUnit).ToList();

			return PreferOwned(units, mainTarget == null ? null : a => a.Owner == mainTarget);
		}

		/// <summary>The risk gate as a new list, never in place (the input can be a shared list). Same predicate calls, same order,
		/// same survivors as the old RemoveAll.</summary>
		List<Actor> WithoutRiskyTargets(List<Actor> units, int attackerValue)
		{
			var kept = new List<Actor>(units.Count);
			foreach (var u in units)
				if (PassesRiskGate(u.Location, attackerValue))
					kept.Add(u);

			return kept;
		}

#if CAMEO_DEV_LOGGING
		// Call sites test Info.VerifySharedEnemyScan BEFORE building the fresh list: the argument is a full scan.
		void VerifyEnemyScan(List<Actor> served, List<Actor> fresh, string what)
		{
			if (served.SequenceEqual(fresh))
				return;

			Log.Write("debug", $"PERF-F1 MISMATCH ({Player.ClientIndex}): {what} served {served.Count} actors, a fresh scan gives {fresh.Count} (tick {World.WorldTick})");
		}
#endif
	}
}
