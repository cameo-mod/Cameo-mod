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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	// AI_ARCHITECTURE.md §12.4 (CA-3): unit and target roles derived from the
	// ruleset — never listed by actor id. Extends BotTargetTags (6g) the same
	// way: load-time derivation only, usable for every faction.
	//
	// Report-only at this stage: the maps are logged to bot-roles.log for review
	// and exposed on the trait; production reads them behind RoleMix (§12.5).
	public class BotUnitRolesInfo : TraitInfo, IRulesetLoaded
	{
		public override object Create(ActorInitializer init) { return new BotUnitRoles(this); }

		// role -> member actor names (resolved once per ruleset).
		public IReadOnlyDictionary<string, HashSet<string>> RoleMembers { get; private set; } =
			new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		// actor name -> roles.
		public IReadOnlyDictionary<string, HashSet<string>> ActorRoles { get; private set; } =
			new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		void IRulesetLoaded<ActorInfo>.RulesetLoaded(Ruleset rules, ActorInfo info)
		{
			ActorRoles = BotUnitRoles.BuildRoleMap(rules);

			var members = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
			foreach (var kv in ActorRoles)
				foreach (var role in kv.Value)
				{
					if (!members.TryGetValue(role, out var set))
						members[role] = set = new HashSet<string>(StringComparer.Ordinal);
					set.Add(kv.Key);
				}

			RoleMembers = members;

			Log.AddChannel("bot-roles", "bot-roles.log");
			foreach (var role in members.Keys.OrderBy(r => r, StringComparer.Ordinal))
				Log.Write("bot-roles",
					$"{info.Name} BotUnitRoles role `{role}`: {members[role].Count} members: {string.Join(", ", members[role].OrderBy(m => m, StringComparer.Ordinal))}");
		}
	}

	public class BotUnitRoles
	{
		public const string Frontline = "frontline";
		public const string Skirmisher = "skirmisher";
		public const string AntiInfantry = "anti_infantry";
		public const string AntiArmour = "anti_armour";
		public const string Artillery = "artillery";
		public const string AntiAir = "anti_air";
		public const string Scout = "scout";
		public const string Support = "support";
		public const string Transport = "transport";
		public const string Gunship = "gunship";
		public const string Fighter = "fighter";
		public const string Bomber = "bomber";
		public const string AirTransport = "air_transport";

		// Target tags (§12.4): buildings worth striking, not units to produce.
		public const string Power = "power";
		public const string Defence = "defence";
		public const string Tech = "tech";

		// Primary-role precedence for counting a unit toward exactly one role:
		// specialists claim first so `frontline` remains the residual tank mass.
		public static readonly string[] PrimaryRoleOrder =
		{
			Support, Transport, AirTransport, Scout, Artillery, AntiAir, Bomber, Fighter, Gunship,
			AntiInfantry, AntiArmour, Skirmisher, Frontline
		};

		// A weapon counts toward an anti-* role when it deals at least this much
		// (percent) against the armor class and more than against the other class.
		const int SpecializedVersus = 110;

		static readonly BitSet<TargetableType> AirTargets = new("Air");
		static readonly BitSet<TargetableType> GroundTargets = new("Ground");
		static readonly BitSet<TargetableType> HealRepairTargets = new("Heal", "Repair");
		static readonly BitSet<TargetableType> InfantryTargets = new("Infantry");
		static readonly BitSet<TargetableType> VehicleTargets = new("Vehicle", "Ship", "Structure");

		public readonly BotUnitRolesInfo Info;

		public BotUnitRoles(BotUnitRolesInfo info) { Info = info; }

		public IReadOnlyDictionary<string, HashSet<string>> RoleMembers => Info.RoleMembers;
		public IReadOnlyDictionary<string, HashSet<string>> ActorRoles => Info.ActorRoles;

		sealed class Facts
		{
			public bool Mobile, Aircraft, Building, Attacks, Cargo, Reveals, Repairs;
			public int Health, Speed, Cost;
			public int MaxRange;
			public bool CanHitAir, CanHitGround, CanHeal;
			public int VersusInfantry, VersusVehicle;
			public bool VTOL;
			public int PowerAmount;
			public bool HitsInfantryType, CantHitInfantry, HitsVehicleType, CantHitVehicle;
			public bool GivesTech;
		}

		public static IReadOnlyDictionary<string, HashSet<string>> BuildRoleMap(Ruleset rules)
		{
			var actors = rules.Actors.Values
				.Where(a => !a.Name.StartsWith('^'))
				.ToList();

			// Armour classes are pooled from the production-queue categories:
			// what "infantry" or "vehicle" means is read out of the ruleset, never typed.
			var infantryArmors = ArmorsOfQueue(actors, "Infantry");
			var vehicleArmors = ArmorsOfQueue(actors, "Vehicle");

			var facts = actors.ToDictionary(a => a.Name, a => Collect(a, rules, infantryArmors, vehicleArmors), StringComparer.Ordinal);

			// Faction-independent percentile bands over the mobile combat pool.
			var mobileCombat = facts.Values.Where(f => f.Mobile && f.Attacks && !f.Aircraft && !f.Building).ToList();
			var hpCut = Percentile(mobileCombat.Select(f => f.Health), 60);
			var speedCut = Percentile(mobileCombat.Select(f => f.Speed), 75);
			var costCut = Percentile(mobileCombat.Select(f => f.Cost), 50);

			var roles = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
			foreach (var a in actors)
			{
				var f = facts[a.Name];
				var set = new HashSet<string>(StringComparer.Ordinal);
				roles[a.Name] = set;

				if (f.Building)
				{
					if (f.PowerAmount > 0)
						set.Add(Power);
					if (f.Attacks)
						set.Add(Defence);
					if (f.GivesTech)
						set.Add(Tech);
					continue;
				}

				if (!f.Mobile && !f.Aircraft)
					continue;

				if (f.Cargo)
					set.Add(f.Aircraft ? AirTransport : Transport);

				if (f.Repairs || f.CanHeal)
					set.Add(Support);

				if (!f.Attacks)
				{
					if (f.Mobile && f.Reveals && f.Speed >= speedCut && f.Cost <= costCut)
						set.Add(Scout);
					continue;
				}

				if (f.Aircraft)
				{
					if (f.VTOL && f.CanHitGround)
						set.Add(Gunship);
					if (f.CanHitAir)
						set.Add(Fighter);
					if (!f.VTOL && !f.CanHitAir && f.CanHitGround)
						set.Add(Bomber);
					continue;
				}

				// Mobile ground combat from here on.
				var ground = f.Mobile && !f.Aircraft;
				if (!ground)
					continue;

				if (f.MaxRange >= BotTargetTags.ArtilleryMinRange)
					set.Add(Artillery);
				if (f.CanHitAir)
					set.Add(AntiAir);
				if (f.Health >= hpCut && f.MaxRange < BotTargetTags.ArtilleryMinRange)
					set.Add(Frontline);
				else if (f.Speed >= speedCut)
					set.Add(Skirmisher);
				if (f.Reveals && f.Speed >= speedCut && f.Cost <= costCut)
					set.Add(Scout);

				// Anti-* specialisations, two complementary signals:
				// (a) the pooled Versus profile (fires where armour classes differ per unit class);
				// (b) explicit type targeting — a weapon that declares Infantry but not Vehicle is
				//     anti-infantry, and `InvalidTargets: Infantry` marks an anti-armour weapon.
				// Shared armour ladders can leave (a) empty without making (b) wrong.
				if (f.VersusInfantry >= SpecializedVersus && f.VersusInfantry > f.VersusVehicle
					|| f.HitsInfantryType && (f.CantHitVehicle || !f.HitsVehicleType))
					set.Add(AntiInfantry);
				if (f.VersusVehicle >= SpecializedVersus && f.VersusVehicle > f.VersusInfantry
					|| f.HitsVehicleType && (f.CantHitInfantry || !f.HitsInfantryType))
					set.Add(AntiArmour);
			}

			return roles;
		}

		static HashSet<string> ArmorsOfQueue(List<ActorInfo> actors, string queue)
		{
			var set = new HashSet<string>(StringComparer.Ordinal);
			foreach (var a in actors)
			{
				if (!a.TraitInfos<BuildableInfo>().Any(b => b.Queue != null &&
					b.Queue.Any(q => q.StartsWith(queue, StringComparison.OrdinalIgnoreCase))))
					continue;

				var armor = a.TraitInfos<ArmorInfo>().FirstOrDefault(x => x.InstanceName == null)?.Type;
				if (armor != null)
					set.Add(armor);
			}

			return set;
		}

		// A building earns the `tech` target tag when it provides a prerequisite whose
		// name carries the tech marker — faction-agnostic across every ContentPack.
		// An unset Prerequisite defaults to the actor's own name, like the trait does.
		static bool GivesTechPrerequisite(ActorInfo a)
		{
			return a.TraitInfos<ProvidesPrerequisiteInfo>().Any(p =>
				(p.Prerequisite ?? a.Name).IndexOf("tech", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		static Facts Collect(ActorInfo a, Ruleset rules, HashSet<string> infantryArmors, HashSet<string> vehicleArmors)
		{
			var f = new Facts
			{
				Mobile = a.HasTraitInfo<MobileInfo>(),
				Aircraft = a.HasTraitInfo<AircraftInfo>(),
				Building = a.HasTraitInfo<BuildingInfo>(),
				Attacks = a.HasTraitInfo<AttackBaseInfo>(),
				Cargo = a.HasTraitInfo<CargoInfo>(),
				Reveals = a.HasTraitInfo<RevealsShroudInfo>(),
				Repairs = a.HasTraitInfo<RepairsUnitsInfo>(),
				VTOL = a.TraitInfos<AircraftInfo>().FirstOrDefault()?.VTOL ?? false,
				Health = a.TraitInfos<HealthInfo>().Select(h => h.HP).DefaultIfEmpty().Max(),
				Speed = a.TraitInfos<MobileInfo>().Select(m => m.Speed).DefaultIfEmpty().Max(),
				Cost = a.TraitInfos<ValuedInfo>().Select(v => v.Cost).DefaultIfEmpty().Min(),
				PowerAmount = a.TraitInfos<PowerInfo>().Sum(p => p.Amount),
				GivesTech = GivesTechPrerequisite(a)
			};

			// Same evaluation as BotRoleSets.WeaponTargets: EnabledByDefault is not
			// trustworthy at ruleset-load time, so conditions are evaluated directly.
			foreach (var armament in a.TraitInfos<ArmamentInfo>()
				.Where(x => x.RequiresCondition == null || x.RequiresCondition.Evaluate(VariableExpression.NoVariables)))
			{
				if (armament.Weapon == null || !rules.Weapons.TryGetValue(armament.Weapon.ToLowerInvariant(), out var weapon))
					continue;

				if (weapon.ValidTargets.Overlaps(AirTargets))
					f.CanHitAir = true;
				if (weapon.ValidTargets.Overlaps(GroundTargets))
					f.CanHitGround = true;
				if (weapon.ValidTargets.Overlaps(HealRepairTargets))
					f.CanHeal = true;
				if (weapon.ValidTargets.Overlaps(InfantryTargets))
					f.HitsInfantryType = true;
				if (weapon.ValidTargets.Overlaps(VehicleTargets))
					f.HitsVehicleType = true;
				if (weapon.InvalidTargets.Overlaps(InfantryTargets))
					f.CantHitInfantry = true;
				if (weapon.InvalidTargets.Overlaps(VehicleTargets))
					f.CantHitVehicle = true;
				if (weapon.Range.Length > f.MaxRange)
					f.MaxRange = weapon.Range.Length;

				// The scoring profile AdaptiveCounterProduction uses: the highest-damage
				// DamageWarhead's Versus, read against each pooled armour class.
				var main = weapon.Warheads.OfType<DamageWarhead>().Where(d => d.Damage > 0)
					.OrderByDescending(d => d.Damage).FirstOrDefault();
				if (main == null)
					continue;

				f.VersusInfantry = Math.Max(f.VersusInfantry, BestVersus(main, infantryArmors));
				f.VersusVehicle = Math.Max(f.VersusVehicle, BestVersus(main, vehicleArmors));
			}

			return f;
		}

		static int BestVersus(DamageWarhead warhead, HashSet<string> armors)
		{
			var best = 0;
			foreach (var armor in armors)
				best = Math.Max(best, warhead.Versus.GetValueOrDefault(armor, 100));
			return best;
		}

		static int Percentile(IEnumerable<int> values, int percentile)
		{
			var sorted = values.Where(v => v > 0).OrderBy(v => v).ToArray();
			return sorted.Length == 0 ? int.MaxValue : sorted[(sorted.Length - 1) * percentile / 100];
		}
	}
}
