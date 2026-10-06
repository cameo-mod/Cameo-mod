#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.CA.Traits.BotModuleLogic;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.CA.Traits.BotModules.Squads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	[TraitLocation(SystemActors.Player)]
	[Desc("Combat veto (DESIGN 19.13 tier 2, AI_ARCHITECTURE 12.31): consulted where a squad commits an attack or a retreat.",
		"Blocks an attack whose predicted trade on the SEEN forces (including remembered static defences) falls below the",
		"engage line, and a retreat the pursuit outruns. Every veto is a mission-card event so the engagement report can",
		"score vetoed vs non-vetoed attacks.",
		"A PURE ADVISOR: it issues NO orders and learns nothing — the same BotCombatPredictor every consumer already uses,",
		"with the tier-1 priors multiplied in when a provider offers them. No enabled provider = today's behaviour,",
		"bit-identical.",
		"",
		"FIXED POINT: thresholds are whole percents, corrections thousandths.")]
	public class CombatVetoBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Below this predicted ratio (own/enemy x 100) an uncommitted engage is vetoed.")]
		public readonly int VetoEngageRatioPct = 50;

		[Desc("An already-committed squad is vetoed only below this lower line (hysteresis: enter >= engage, exit < abort).")]
		public readonly int VetoAbortRatioPct = 35;

		[Desc("Below this predicted ratio a wave launch is vetoed (remembered defences at the target, parity-floored at own value).")]
		public readonly int VetoLaunchRatioPct = 60;

		[Desc("The pursuit outruns the force when its mean speed reaches this percent of the force's mean.")]
		public readonly int VetoFleeSpeedMarginPct = 100;

		[Desc("A squad's cached verdict is recomputed at most every this many ticks.")]
		public readonly int VetoCacheTicks = 25;

		[Desc("Remembered armed defences within this many cells of the fight/target join the enemy side.")]
		public readonly int DefenceIncludeCells = 12;

		[Desc("The same squad+kind veto writes at most one mission card per this many ticks.")]
		public readonly int VetoCardTicks = 250;

		[Desc("BM_live_combat_model: run the veto's predictor on the balance pipeline's",
			"effective-damage model instead of the classic main-warhead DPS. False = classic, bit-identical.")]
		public readonly bool UseEffectiveDamageModel = false;

		public override object Create(ActorInitializer init) => new CombatVetoBotModule(init, this);
	}

	public class CombatVetoBotModule : ConditionalTrait<CombatVetoBotModuleInfo>, IBotCombatVeto
	{
		sealed class CachedVerdict
		{
			public int Tick;
			public bool Vetoed;
			public string Reason;
		}

		readonly OpenRA.Player player;
		readonly World world;
		readonly Dictionary<(SquadCA Squad, string Kind), CachedVerdict> cache = new();
		readonly Dictionary<string, int> lastCard = new();

		IBotEngagementPriors[] priors;
		IBotFightThresholds[] fightThresholds;
		IBotRememberedDefenceProvider[] defenceProviders;
		IBotRegionThreatProvider[] regionThreats;

		public CombatVetoBotModule(ActorInitializer init, CombatVetoBotModuleInfo info)
			: base(info)
		{
			player = init.Self.Owner;
			world = init.Self.World;
		}

		public bool VetoEngage(SquadCA squad, IReadOnlyList<Actor> enemies, bool alreadyCommitted, out string reason)
		{
			reason = null;
			if (IsTraitDisabled)
				return false;

			var key = (squad, "engage");
			if (cache.TryGetValue(key, out var cached) && world.WorldTick - cached.Tick < Info.VetoCacheTicks)
			{
				reason = cached.Reason;
				return Report(squad, "engage", cached.Vetoed, null);
			}

			var rules = world.Map.Rules;
			var own = ForceOf(squad);
			var foes = enemies
				.Where(e => e != null && !e.IsDead && e.IsInWorld && e.Info.HasTraitInfo<AttackBaseInfo>())
				.GroupBy(e => e.Info)
				.SelectMany(g => Info.UseEffectiveDamageModel
					? g.Select(e => (BotUnitProfiles.Get(e, player, true), 1))
					: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
				.ToList();

			AddDefencesNear(foes, CentroidOf(enemies));

			var ratioPct = (int)(CombatVetoEval.Predict(own, foes, Priors(), Info.UseEffectiveDamageModel).Ratio * 100);
			var thresholds = FightThresholds(enemies);
			var vetoed = CombatVetoEval.EngageVetoed(ratioPct, alreadyCommitted, thresholds.Engage, thresholds.Abort);
			cache[key] = new CachedVerdict { Tick = world.WorldTick, Vetoed = vetoed, Reason = vetoed ? BotMissionReasons.Outmatched : null };
			reason = vetoed ? BotMissionReasons.Outmatched : null;
			return Report(squad, "engage", vetoed, null);
		}

		public bool VetoLaunch(IReadOnlyList<Actor> force, CPos targetCell, out string reason)
		{
			reason = null;
			if (IsTraitDisabled)
				return false;

			var rules = world.Map.Rules;
			var own = force
				.Where(a => a != null && !a.IsDead && a.IsInWorld)
				.GroupBy(a => a.Info)
				.SelectMany(g => Info.UseEffectiveDamageModel
					? g.Select(a => (BotUnitProfiles.Get(a, player, true), 1))
					: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
				.ToList();

			var foes = new List<(BotUnitProfile Unit, int Count)>();
			var defenceValue = AddDefencesNear(foes, targetCell);
			regionThreats ??= player.PlayerActor.TraitsImplementing<IBotRegionThreatProvider>().ToArray();
			var threatValue = regionThreats.MergedThreatAt(targetCell);

			// CP §2.3 parity floor: assume the enemy is at least as strong as we are unless proven otherwise — a loss
			// must be proven, never assumed. Below own value the remembered evidence predicts a mirror (ratio ~1).
			var ownValue = own.Sum(u => (long)u.Item1.Cost * u.Item2);
			if (defenceValue + threatValue < ownValue)
				foes = own;

			var ratioPct = (int)(CombatVetoEval.Predict(own, foes, Priors(), Info.UseEffectiveDamageModel).Ratio * 100);
			var vetoed = ratioPct < Info.VetoLaunchRatioPct;
			reason = vetoed ? BotMissionReasons.Outmatched : null;
			return Report(null, "launch", vetoed, targetCell);
		}

		public bool VetoFlee(SquadCA squad, IReadOnlyList<Actor> pursuers, out string reason)
		{
			reason = null;
			if (IsTraitDisabled)
				return false;

			var key = (squad, "flee");
			if (cache.TryGetValue(key, out var cached) && world.WorldTick - cached.Tick < Info.VetoCacheTicks)
			{
				reason = cached.Reason;
				return Report(squad, "flee", cached.Vetoed, null);
			}

			var rules = world.Map.Rules;
			var own = ForceOf(squad);
			var foes = pursuers
				.Where(e => e != null && !e.IsDead && e.IsInWorld)
				.GroupBy(e => e.Info)
				.SelectMany(g => Info.UseEffectiveDamageModel
					? g.Select(e => (BotUnitProfiles.Get(e, player, true), 1))
					: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
				.ToList();

			var vetoed = CombatVetoEval.CannotOutrun(own, foes, Info.VetoFleeSpeedMarginPct);
			cache[key] = new CachedVerdict { Tick = world.WorldTick, Vetoed = vetoed, Reason = vetoed ? "x_no_outrun" : null };
			reason = vetoed ? "x_no_outrun" : null;
			return Report(squad, "flee", vetoed, null);
		}

		IBotEngagementPriors Priors()
		{
			priors ??= player.PlayerActor.TraitsImplementing<IBotEngagementPriors>().ToArray();
			return priors.FirstEnabledTraitOrDefault();
		}

		(int Engage, int Abort) FightThresholds(IEnumerable<Actor> enemies)
		{
			fightThresholds ??= player.PlayerActor.TraitsImplementing<IBotFightThresholds>().ToArray();
			var provider = fightThresholds.FirstEnabledTraitOrDefault();
			if (provider == null)
				return (Info.VetoEngageRatioPct, Info.VetoAbortRatioPct);
			var enemy = enemies.Where(a => a?.Owner?.Faction != null && player.RelationshipWith(a.Owner) == PlayerRelationship.Enemy)
				.Select(a => a.Owner.Faction.InternalName).Where(f => !string.IsNullOrEmpty(f))
				.OrderBy(f => f, System.StringComparer.Ordinal).FirstOrDefault() ?? "";
			var engage = provider.RetreatRatioPct(player.Faction.InternalName, enemy, Info.VetoEngageRatioPct);
			var abort = Info.VetoAbortRatioPct * engage / System.Math.Max(1, Info.VetoEngageRatioPct);
			return (engage, System.Math.Min(engage, abort));
		}

		List<(BotUnitProfile Unit, int Count)> ForceOf(SquadCA squad)
		{
			var rules = world.Map.Rules;
			return squad.Units
				.Where(u => u.Actor != null && !u.Actor.IsDead && u.Actor.IsInWorld)
				.GroupBy(u => u.Actor.Info)
				.SelectMany(g => Info.UseEffectiveDamageModel
					? g.Select(u => (BotUnitProfiles.Get(u.Actor, player, true), 1))
					: new[] { (BotUnitProfiles.Get(rules, g.Key, false), g.Count()) })
				.ToList();
		}

		/// <summary>Adds the remembered armed defences within DefenceIncludeCells of centre to the foe list;
		/// returns their total value. Empty providers mean nothing observed, never "no defences".</summary>
		int AddDefencesNear(List<(BotUnitProfile Unit, int Count)> foes, CPos centre)
		{
			defenceProviders ??= player.PlayerActor.TraitsImplementing<IBotRememberedDefenceProvider>().ToArray();
			var rules = world.Map.Rules;
			var includeSq = (long)Info.DefenceIncludeCells * Info.DefenceIncludeCells;
			var total = 0;
			foreach (var g in defenceProviders
				.SelectMany(p => p.RememberedDefences())
				.Where(d => d.Observed != null && (d.Enemy == null || player.RelationshipWith(d.Enemy) == PlayerRelationship.Enemy))
				.Where(d => (d.Cell - centre).LengthSquared <= includeSq)
				.GroupBy(d => d.Observed))
			{
				total += g.Sum(d => d.Value);
				foes.Add((BotUnitProfiles.Get(rules, g.Key, Info.UseEffectiveDamageModel), g.Count()));
			}

			return total;
		}

		static CPos CentroidOf(IReadOnlyList<Actor> actors)
		{
			long x = 0, y = 0;
			var n = 0;
			foreach (var a in actors)
			{
				if (a == null || a.IsDead || !a.IsInWorld)
					continue;

				x += a.Location.X;
				y += a.Location.Y;
				n++;
			}

			return n == 0 ? CPos.Zero : new CPos((int)(x / n), (int)(y / n));
		}

		/// <summary>Rate-limited veto card: one per squad+kind per VetoCardTicks; every veto is a Denied event
		/// on a veto:<kind> mission id (the one contract — EL scores vetoed vs non-vetoed attacks off these).</summary>
		bool Report(SquadCA squad, string kind, bool vetoed, CPos? targetCell)
		{
			if (!vetoed)
				return false;

			var cardKey = $"{kind}:{squad?.GetHashCode() ?? 0}";
			if (lastCard.TryGetValue(cardKey, out var last) && world.WorldTick - last < Info.VetoCardTicks)
				return true;

			lastCard[cardKey] = world.WorldTick;
			var firstUnit = squad?.Units.FirstOrDefault(u => u.Actor != null && !u.Actor.IsDead && u.Actor.IsInWorld && u.Actor.OccupiesSpace != null)?.Actor;
			BotMissionLog.Write(new BotMissionRecord
			{
				Player = player,
				MissionId = $"veto:{kind}:{world.WorldTick}",
				Event = BotMissionEvent.Denied,
				Reason = kind == "flee" ? "x_no_outrun" : BotMissionReasons.Outmatched,
				Executor = "CombatVeto",
				MissionType = kind == "flee" ? "retreat" : "attack",
				TargetCell = targetCell ?? firstUnit?.Location,
				Units = squad?.Units.Count ?? 0
			});
			return true;
		}
	}
}
