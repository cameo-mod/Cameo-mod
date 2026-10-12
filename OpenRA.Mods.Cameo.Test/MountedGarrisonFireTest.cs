using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OpenRA.GameRules;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public sealed class MountedGarrisonFireTest
	{
		static readonly MiniYamlMirrorRuleset Mounted = MiniYamlMirrorRuleset.FromRepo(FindRoot());
		static string FindRoot()
		{
			for (var d = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); d != null; d = d.Parent)
				if (File.Exists(Path.Combine(d.FullName, "mods/cameo/mod.yaml"))) return d.FullName;
			throw new InvalidOperationException("Mounted Cameo rules required.");
		}
		static MiniYaml Yaml(MiniYamlMirrorNode n) => new(n.Value,
			n.Children.Select(c => new MiniYamlNode(c.Key, Yaml(c))).ToArray());
		static T Load<T>(MiniYamlMirrorNode n) where T : new()
		{
			var info = new T(); FieldLoader.Load(info, Yaml(n)); return info;
		}
		static void Set(object info, string field, object value) => info.GetType().GetField(field,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(info, value);
		sealed class FacingMarkerInfo : TraitInfo, IFacingInfo
		{
			public WAngle GetInitialFacing() => WAngle.Zero;
			public override object Create(ActorInitializer init) => new object();
		}
		sealed class MountedFixture
		{
			public readonly Actor Host, Rifle, Infantry;
			public readonly AttackGarrisoned Attack;
			public readonly OpenRA.Test.AttackGarrisonedTest.Recorder Recorder;
			public MountedFixture(string host, string infantry)
			{
				var shell = new OpenRA.Test.AttackGarrisonedTest.Fixture();
				var actors = shell.World.Map.Rules.Actors.ToDictionary(p => p.Key, p => p.Value);
				var weapons = shell.World.Map.Rules.Weapons.ToDictionary(p => p.Key, p => p.Value);
				Configure(actors, weapons, host, infantry);
				var rules = new Ruleset(actors, weapons, null, null, null, null, null, null);
				typeof(Map).GetProperty("Rules").SetValue(shell.World.Map, rules);
				Host = shell.Create("host", shell.Own, WPos.Zero);
				Rifle = shell.Create("rifle", shell.Own, WPos.Zero);
				Infantry = shell.Create("infantry", shell.Enemy, new WPos(0, -2048, 0));
				var index = (OpenRA.Test.AttackGarrisonedTest.SpatialIndex)(object)shell.World.ActorMap;
				index.Targets.Clear(); index.Targets.Add(Infantry);
				Attack = Host.Trait<AttackGarrisoned>();
				Recorder = Host.Trait<OpenRA.Test.AttackGarrisonedTest.Recorder>();
			}
			public void Enter()
			{
				if (Host.TraitOrDefault<Cargo>() is Cargo cargo) cargo.Load(Host, Rifle);
				else Host.Trait<OpenRA.Mods.AS.Traits.Garrisonable>().Load(Host, Rifle);
			}
			public void Tick()
			{
				((ITick)Attack).Tick(Host);
				foreach (var arm in Rifle.TraitsImplementing<Armament>()) ((ITick)arm).Tick(Rifle);
			}
		}

		// Mounted combat traits and weapon ranges/cadence/target policy are real.
		// The engine fixture supplies the asset-free world/position shell. Projectile,
		// sound and muzzle assets are omitted; actual armament fire is recorded.
		static void Configure(Dictionary<string, ActorInfo> actors, Dictionary<string, WeaponInfo> weapons,
			string hostName, string infantryName)
		{
			var profile = Mounted.Resolve(infantryName);
			var traits = actors["rifle"].TraitInfos<TraitInfo>().Where(t =>
				!(t is ArmamentInfo or AutoTargetInfo or AutoTargetPriorityInfo or AttackBaseInfo
				or PassengerInfo or OpenRA.Mods.AS.Traits.GarrisonerInfo)).ToList();
			traits.Add(Load<AutoTargetInfo>(profile.Child("AutoTarget")));
			traits.AddRange(profile.ChildrenNamed("AutoTargetPriority").Select(Load<AutoTargetPriorityInfo>));
			var passenger = Load<PassengerInfo>(profile.Child("Passenger"));
			// Cargo derives weight one from the omitted infantry renderer in production.
			Assert.That(profile.ChildrenNamed("WithInfantryBody"), Is.Not.Empty);
			if (passenger.Weight == 0) Set(passenger, "Weight", 1);
			traits.Add(passenger);
			traits.Add(Load<OpenRA.Mods.AS.Traits.GarrisonerInfo>(profile.Child("Garrisoner")));
			traits.Add(new AttackFrontalInfo()); traits.Add(new FacingMarkerInfo());
			foreach (var node in profile.ChildrenNamed("Armament"))
			{
				var arm = Load<ArmamentInfo>(node); Set(arm, "MuzzleSequence", null); traits.Add(arm);
				var authored = Mounted.ResolveWeapon(arm.Weapon);
				var fields = authored.Children.Where(c => c.Key != "Projectile" && !c.Key.StartsWith("Warhead")
					&& !c.Key.StartsWith("Inherits")).Select(c => new MiniYamlNode(c.Key, Yaml(c))).ToArray();
				var weapon = new WeaponInfo(new MiniYaml(null, fields));
				Set(weapon, "Report", ImmutableArray<string>.Empty);
				weapons[arm.Weapon.ToLowerInvariant()] = weapon;
			}
			actors["rifle"] = new ActorInfo(infantryName, traits.ToArray());
			var targetTraits = actors["infantry"].TraitInfos<TraitInfo>().Where(t => !(t is TargetableInfo)).ToList();
			targetTraits.Add(Load<TargetableInfo>(profile.Child("Targetable")));
			actors["infantry"] = new ActorInfo("infantry", targetTraits.ToArray());
			var hostProfile = Mounted.Resolve(hostName);
			var hostTraits = actors["host"].TraitInfos<TraitInfo>().Where(t =>
				!(t is AttackGarrisonedInfo or CargoInfo or OpenRA.Mods.AS.Traits.GarrisonableInfo or AutoTargetInfo)).ToList();
			hostTraits.Add(Load<AttackGarrisonedInfo>(hostProfile.Child("AttackGarrisoned")));
			hostTraits.Add(Load<AutoTargetInfo>(hostProfile.Child("AutoTarget")));
			if (hostProfile.Child("Cargo") is MiniYamlMirrorNode cargoNode)
			{
				var cargo = Load<CargoInfo>(cargoNode); Set(cargo, "InitialUnits", ImmutableArray<string>.Empty); hostTraits.Add(cargo);
			}
			else hostTraits.Add(Load<OpenRA.Mods.AS.Traits.GarrisonableInfo>(hostProfile.Child("Garrisonable")));
			actors["host"] = new ActorInfo(hostName, hostTraits.ToArray());
		}
		static void AssertFires(string host, string infantry, bool explicitAttack, WPos position)
		{
			var f = new MountedFixture(host, infantry);
			f.Host.Trait<AutoTarget>().SetStance(f.Host, UnitStance.Defend);
			f.Rifle.Trait<AutoTarget>().SetStance(f.Rifle, UnitStance.Defend);
			f.Infantry.Trait<IPositionable>().SetCenterPosition(f.Infantry, position); f.Enter();
			Assert.That(f.Rifle.TraitsImplementing<Armament>().Any(a => a.Info.Name == "garrisoned"), Is.True);
			if (explicitAttack)
				f.Attack.GetAttackActivity(f.Host, AttackSource.Default, Target.FromActor(f.Infantry), false, false).TickOuter(f.Host);
			for (var i = 0; i < 30; i++) f.Tick();
			Assert.That(f.Recorder.Events, Is.Not.Empty, $"{host}/{infantry} {position} explicit={explicitAttack}");
		}
		[TestCase("^GarrisonableBuilding", "td_gdi_minigunner", false)]
		[TestCase("^GarrisonableBuilding", "td_gdi_minigunner", true)]
		[TestCase("ra1_allies_pillbox", "ra1_allies_machinegunner", false)]
		[TestCase("ra1_allies_pillbox", "ra1_allies_machinegunner", true)]
		public void OccupiedMountedProfileFires(string host, string infantry, bool explicitAttack) =>
			AssertFires(host, infantry, explicitAttack, new WPos(0, -2048, 0));
		[TestCase("ra1_allies_pillbox", false)]
		[TestCase("ra1_allies_pillbox", true)]
		[TestCase("ra1_allies_camopillbox", false)]
		[TestCase("ra1_allies_camopillbox", true)]
		public void SingleOccupantPillboxCoversEveryDirection(string host, bool explicitAttack)
		{
			foreach (var position in new[] { new WPos(0, -2048, 0), new WPos(2048, 0, 0),
				new WPos(0, 2048, 0), new WPos(-2048, 0, 0) })
				AssertFires(host, "ra1_allies_machinegunner", explicitAttack, position);
		}
	}
}
