#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public enum AirstrikeMission { Attack, Guard }

	public class AirstrikePowerASInfo : DirectionalSupportPowerInfo
	{
		[FieldLoader.Require]
		public readonly Dictionary<int, string> UnitTypes = [];

		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircrafts arrive.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have left the area.")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		public readonly AirstrikeMission Mission = AirstrikeMission.Attack;

		public readonly Dictionary<int, int> GuardDurations = [];

		[Desc("Condition to grant after reaching the target area.")]
		public readonly Dictionary<int, string> GuardingConditions = [];

		public override object Create(ActorInitializer init) { return new AirstrikePowerAS(init.Self, this); }
	}

	public class AirstrikePowerAS : DirectionalSupportPower
	{
		readonly AirstrikePowerASInfo info;

		public AirstrikePowerAS(Actor self, AirstrikePowerASInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
			{
				Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, info);
			}
			else
				base.SelectTarget(self, order, manager);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendAirstrike(self, order.Target.CenterPosition, facing);
		}

		public void SendAirstrike(Actor self, WPos target, WAngle? facing = null)
		{
			var level = GetLevel();
			if (level == 0)
				return;

			var info = Info as AirstrikePowerASInfo;
			if (!facing.HasValue)
				facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

			var unitType = info.UnitTypes.First(ut => ut.Key == level).Value;
			var altitude = self.World.Map.Rules.Actors[unitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var attackRotation = WRot.FromYaw(facing.Value);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
			target += new WVec(0, 0, altitude);

			var startPos = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				var aircrafts = new HashSet<Actor>();

				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
					var height = self.World.Map.DistanceAboveTerrain(target + spawnOffset);

					var a = w.CreateActor(unitType,
					[
						new CenterPositionInit(startPos + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(facing.Value),
						new SpectreTargetPositionInit(target - new WVec(WDist.Zero, WDist.Zero, height)),
					]);

					delta = new WVec(WDist.Zero, info.BeaconDistanceOffset, WDist.Zero).Rotate(attackRotation);

					if (info.Mission == AirstrikeMission.Attack)
					{
						a.QueueActivity(
							new FlyAttack(a, AttackSource.Default, Target.FromPos(target + spawnOffset - new WVec(WDist.Zero, WDist.Zero, height)), true, Color.OrangeRed));
					}
					else
					{
						a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
						a.QueueActivity(new AttackMoveActivity(a, () => new FlyIdle(a, info.GuardDurations.First(ut => ut.Key == level).Value, false)));
					}

					a.QueueActivity(new FlyOffMap(a));
					a.QueueActivity(new RemoveSelf());

					aircrafts.Add(a);
				}

				var effect = new AirstrikePowerASEffect(self.World, self.Owner, target, aircrafts, this, info);
				self.World.Add(effect);
			});
		}
	}
}
