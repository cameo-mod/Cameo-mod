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
using System.Linq;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// ALLIN-TRIGGER-01 (USER 2026-11-25, memory designer-dynamic-all-in): the team all-in is DYNAMIC. The leader reads the OBSERVED state of the game (its own side's kill and loss counters, the
	// scoreboard, the enemy sightings of the master module, an own superweapon) and marks its order all-in when the team is ahead; it ends that all-in again when the ratio drops below about 1.0.
	// The old inputs (cap, idle army, nearly won, the stalemate signal) stay as they are (TeamLeadAllInWhy), and the late tick (Q8) stays as the fallback of a game that never gets decided.
	public partial class SquadManagerBotModuleCA
	{
		// What one scan saw. Filled by AheadScan, read by AheadWhy and AheadDeescalate.
		sealed class AheadView
		{
			public bool Scanned, FactsKnown, ArmyKnown, SuperReady, AllyAttacked, HomeThreat;
			public long Kills, Losses, BattleKills, BattleLosses, ExchangeKills, ExchangeLosses, OwnArmy, EnemyArmy, EnemyDefence;
			public int Defeated, TeamSeats, EnemySeats, LivingEnemies, EnemyBuildings, YardDead, ProductionDead, EconomyDead;

			public void Reset()
			{
				FactsKnown = ArmyKnown = SuperReady = AllyAttacked = HomeThreat = false;
				Kills = Losses = BattleKills = BattleLosses = ExchangeKills = ExchangeLosses = OwnArmy = EnemyArmy = EnemyDefence = 0;
				Defeated = TeamSeats = EnemySeats = LivingEnemies = EnemyBuildings = YardDead = ProductionDead = EconomyDead = 0;
			}
		}

		// The rows: slots 0-8 are the triggers in AheadTrigger order, then the family and the de-escalation.
		static readonly string[] AheadRows =
		{
			AblationIds.SqAheadYard, AblationIds.SqAheadProduction, AblationIds.SqAheadEconomy, AblationIds.SqAheadDefeat, AblationIds.SqAheadTrade,
			AblationIds.SqAheadBattle, AblationIds.SqAheadArmy, AblationIds.SqAheadCounter, AblationIds.SqAheadSuper, AblationIds.SqAhead, AblationIds.SqAheadDeescalate
		};

		const int AheadFamilySlot = 9;
		const int AheadDeescalateSlot = 10;

		readonly AblationLatch[] ablateAhead = new AblationLatch[11];
		readonly AheadView aheadView = new();
		readonly AheadLedger aheadLedger = new();
		readonly AheadWindow aheadWindow = new();
		IBotAheadFactsProvider[] aheadProviders;
		int aheadScanTick = -1, aheadDefeatsHandled, aheadLastFired = -1;

		bool AheadRowOn(int slot) => !ablateAhead[slot].Off(Player, AheadRows[slot]);

		/// <summary>The dynamic all-in is in force: the one launcher is on, the yaml family switch, and its DEV row.</summary>
		internal bool ArmyAheadOn => ArmyLaunchArbiterOn && Info.ArmyAheadEnabled && AheadRowOn(AheadFamilySlot);

		bool AheadTriggerOn(AheadTrigger trigger) => AheadRowOn((int)trigger) && trigger switch
		{
			AheadTrigger.Yard => Info.ArmyAheadYardEnabled,
			AheadTrigger.Production => Info.ArmyAheadProductionEnabled,
			AheadTrigger.Economy => Info.ArmyAheadEconomyEnabled,
			AheadTrigger.Defeat => Info.ArmyAheadDefeatEnabled,
			AheadTrigger.Trade => Info.ArmyAheadTradeEnabled,
			AheadTrigger.Battle => Info.ArmyAheadBattleEnabled,
			AheadTrigger.Army => Info.ArmyAheadArmyEnabled,
			AheadTrigger.Counter => Info.ArmyAheadCounterEnabled,
			AheadTrigger.Super => Info.ArmyAheadSuperEnabled,
			_ => false
		};

		// The master module's observed enemy facts (null: none, or its sightings are not fog-honest: the army, defence and structure triggers stay silent).
		IBotAheadFactsProvider AheadProvider =>
			(aheadProviders ??= Player.PlayerActor.TraitsImplementing<IBotAheadFactsProvider>().ToArray()).FirstOrDefault(p => p.AheadFactsEnabled);

		// Once per ArmyAheadScanTicks: the team's kill and loss counters (the own side's PlayerStatistics, allies included), the scoreboard, the enemy facts, an own superweapon.
		void AheadScan(int now)
		{
			if (!AllInCA.ScanDue(now, aheadScanTick, Info.ArmyAheadScanTicks))
				return;

			aheadScanTick = now;
			var v = aheadView;
			v.Reset();
			long kills = 0, losses = 0;
			foreach (var p in World.Players)
			{
				if (p.NonCombatant)
					continue;

				if (p == Player || Player.IsAlliedWith(p))
				{
					if (p.WinState == WinState.Undefined)
						v.TeamSeats++;

					var stats = p.PlayerActor.TraitOrDefault<PlayerStatistics>();
					if (stats != null)
					{
						kills += stats.KillsCost;
						losses += stats.DeathsCost;
					}
				}
				else if (Player.RelationshipWith(p) == PlayerRelationship.Enemy)
				{
					if (p.WinState == WinState.Lost)
						v.Defeated++;
					else if (p.WinState == WinState.Undefined)
						v.EnemySeats++;
				}
			}

			v.Kills = kills;
			v.Losses = losses;
			aheadWindow.Add(now, kills, losses, Math.Max(Info.ArmyAheadBattleWindowTicks, Info.ArmyAheadDeescalateWindowTicks));
			(v.BattleKills, v.BattleLosses) = aheadWindow.Delta(now, Info.ArmyAheadBattleWindowTicks);
			(v.ExchangeKills, v.ExchangeLosses) = aheadWindow.Delta(now, Info.ArmyAheadDeescalateWindowTicks);

			// The same own and allied army numbers the AggTeam finish reads (the master module's latest snapshot).
			var urgency = StalemateUrgency;
			if (urgency != null)
			{
				v.ArmyKnown = true;
				v.OwnArmy = urgency.StalemateOwnArmy + urgency.StalemateAlliedArmy;
				v.HomeThreat = urgency.StalemateHomeThreat;
			}

			var enemies = AheadProvider?.AheadEnemies;
			if (enemies != null && enemies.Count > 0)
			{
				v.FactsKnown = true;
				for (var i = 0; i < enemies.Count; i++)
				{
					var f = enemies[i];
					v.LivingEnemies++;
					v.EnemyArmy += f.ArmyValue;
					v.EnemyDefence += f.DefenceValue;
					v.EnemyBuildings += f.Buildings;
					aheadLedger.Note(f.Seat, f.Yards, f.Production, f.Refineries);
					if (aheadLedger.YardDead(f.Seat, f.Yards, f.Mcvs))
						v.YardDead++;
					if (aheadLedger.ProductionDead(f.Seat, f.Production))
						v.ProductionDead++;
					if (aheadLedger.EconomyDead(f.Seat, f.Refineries, f.Harvesters))
						v.EconomyDead++;
				}
			}

			v.AllyAttacked = TeamBlackboard.Collect(Player).DefendRequests > 0;
			v.SuperReady = AheadSuperReady();
			v.Scanned = true;
		}

		// An own support power is ready and charges for at least ArmyAheadSuperMinChargeTicks (a superweapon, not a minor power).
		bool AheadSuperReady()
		{
			var powers = Player.PlayerActor.TraitOrDefault<SupportPowerManager>()?.Powers;
			if (powers == null)
				return false;

			foreach (var power in powers.Values)
				if (power.Ready && power.Info != null && power.Info.ChargeInterval >= Info.ArmyAheadSuperMinChargeTicks)
					return true;

			return false;
		}

		// The team is behind at <pct> percent: its army is below that share of the enemy army it knows, or the exchange of the last window goes against it.
		bool AheadBehind(AheadView v, int pct) =>
			(v.ArmyKnown && v.FactsKnown && AllInAhead.Behind(v.OwnArmy, v.EnemyArmy, pct))
			|| AllInAhead.TradeBehind(v.ExchangeKills, v.ExchangeLosses, Info.ArmyAheadDeescalateMinLoss, pct);

		// The ratio the DEV rows show for the end of an all-in: the army ratio when both armies are known, else the exchange of the window.
		static long AheadObservedRatio(AheadView v) =>
			v.ArmyKnown && v.FactsKnown && v.EnemyArmy > 0 ? AllInAhead.RatioPct(v.OwnArmy, v.EnemyArmy) : AllInAhead.RatioPct(v.ExchangeKills, v.ExchangeLosses);

		// The ratio of a trigger that fires (percent; -1 = it does not fire now).
		long AheadFired(AheadTrigger trigger, AheadView v) => trigger switch
		{
			AheadTrigger.Yard => v.FactsKnown && AllInAhead.Majority(v.YardDead, v.LivingEnemies, Info.ArmyAheadMajorityPercent)
				? AllInAhead.RatioPct(v.YardDead, v.LivingEnemies) : -1,
			AheadTrigger.Production => v.FactsKnown && AllInAhead.Majority(v.ProductionDead, v.LivingEnemies, Info.ArmyAheadMajorityPercent)
				? AllInAhead.RatioPct(v.ProductionDead, v.LivingEnemies) : -1,
			AheadTrigger.Economy => v.FactsKnown && AllInAhead.Majority(v.EconomyDead, v.LivingEnemies, Info.ArmyAheadMajorityPercent)
				? AllInAhead.RatioPct(v.EconomyDead, v.LivingEnemies) : -1,
			AheadTrigger.Defeat => AllInAhead.Defeat(v.Defeated, aheadDefeatsHandled, v.TeamSeats, v.EnemySeats, v.OwnArmy, v.EnemyArmy, Info.ArmyAheadDefeatArmyPercent)
				? AllInAhead.RatioPct(v.TeamSeats, v.EnemySeats) : -1,
			AheadTrigger.Trade => AllInAhead.Trade(v.Kills, v.Losses, Info.ArmyAheadTradeMinKills, Info.ArmyAheadTradePercent)
				? AllInAhead.RatioPct(v.Kills, v.Losses) : -1,
			AheadTrigger.Battle => AllInAhead.Battle(v.BattleKills, v.BattleLosses, Info.ArmyAheadBattleMinKills, Info.ArmyAheadBattlePercent, v.OwnArmy)
				? AllInAhead.RatioPct(v.BattleKills, v.BattleLosses) : -1,
			AheadTrigger.Army => v.ArmyKnown && v.FactsKnown && AllInAhead.Army(v.OwnArmy, v.EnemyArmy, v.EnemyDefence, Info.ArmyAheadArmyPercent)
				? AllInAhead.RatioPct(v.OwnArmy, v.EnemyArmy + v.EnemyDefence) : -1,
			AheadTrigger.Counter => v.ArmyKnown && v.FactsKnown && AllInAhead.Counter(v.AllyAttacked, v.HomeThreat, v.OwnArmy, v.EnemyDefence, v.EnemyBuildings, Info.ArmyAheadCounterPercent)
				? AllInAhead.RatioPct(v.OwnArmy, v.EnemyDefence) : -1,
			AheadTrigger.Super => v.SuperReady ? AheadObservedRatio(v) : -1,
			_ => -1
		};

		/// <summary>
		/// Leader only (from TeamLeadAllInWhy, when no older input fired): the ahead trigger that fires now (an AllInAhead.Names entry), or null. Never before ArmyAheadStartTick, never while the team is
		/// behind (ArmyAheadResumePercent). The DEV row all_in_ahead (trigger, ratio, tick) is written when a trigger starts to fire.
		/// </summary>
		string AheadWhy(int now)
		{
			if (!ArmyAheadOn || now < Info.ArmyAheadStartTick)
				return null;

			AheadScan(now);
			var v = aheadView;
			if (!v.Scanned || AheadBehind(v, Info.ArmyAheadResumePercent))
			{
				aheadLastFired = -1;
				return null;
			}

			for (var i = 0; i <= (int)AheadTrigger.Super; i++)
			{
				var trigger = (AheadTrigger)i;
				if (!AheadTriggerOn(trigger))
					continue;

				var ratio = AheadFired(trigger, v);
				if (ratio < 0)
					continue;

				// A defeat is answered once: the next one fires again.
				if (trigger == AheadTrigger.Defeat)
					aheadDefeatsHandled = v.Defeated;

				if (aheadLastFired != i)
				{
					aheadLastFired = i;
					AheadRecord("all_in_ahead", AllInAhead.Why(trigger), ratio, now);
				}

				return AllInAhead.Why(trigger);
			}

			aheadLastFired = -1;
			return null;
		}

		/// <summary>
		/// Leader only (from TeamLeadAllInWhy, before its early return): an open all-in order that an ahead trigger started ends (its members stop sending every unit; the pushed squads fight on as an
		/// ordinary order) when the team is behind at ArmyAheadDeescalatePercent. Every defeat that happens while an all-in order is open counts as answered by it.
		/// </summary>
		void AheadDeescalate(int now)
		{
			if (!ArmyAheadOn || TeamLeadOpenOrder() is not { AllIn: true } open)
				return;

			AheadScan(now);
			var v = aheadView;
			if (!v.Scanned)
				return;

			aheadDefeatsHandled = Math.Max(aheadDefeatsHandled, v.Defeated);
			if (!AllInAhead.IsAheadWhy(open.AllInWhy) || !Info.ArmyAheadDeescalateEnabled || !AheadRowOn(AheadDeescalateSlot) || !AheadBehind(v, Info.ArmyAheadDeescalatePercent))
				return;

			AheadRecord("all_in_ahead_end", open.AllInWhy, AheadObservedRatio(v), now);
			open.ClearAllIn();
			aheadLastFired = -1;
		}

		// DEV record (the writer is null in a release build): the trigger, the observed ratio in percent and the tick.
		void AheadRecord(string evt, string trigger, long ratio, int now)
		{
			if (DiagnosticWriter == null)
				return;

			var v = aheadView;
			DiagnosticTeamEvent(evt, "trigger=" + trigger + " ratio=" + ratio + " tick=" + now + " kills=" + v.Kills + " losses=" + v.Losses + " own=" + v.OwnArmy + " enemy=" + v.EnemyArmy
				+ " def=" + v.EnemyDefence + " defeated=" + v.Defeated + " seat=" + teamLeadKey);
		}
	}
}
