#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	public class FirePortSP
	{
		public WVec Offset;
		public WAngle Yaw;
		public WAngle Cone;
	}

	[Desc("Cargo can fire their weapons out of fire ports with per-passenger independent targeting.")]
	public class AttackGarrisonedSPInfo : AttackFollowInfo, IRulesetLoaded
	{
		[FieldLoader.Require]
		[Desc("Fire port offsets in local coordinates.")]
		public readonly WVec[] PortOffsets = null;

		[Desc("Fire port yaw angles. If empty, ports have no facing constraint.")]
		public readonly WAngle[] PortYaws = null;

		[Desc("Fire port yaw cone angle. If empty, ports have no facing constraint.")]
		public readonly WAngle[] PortCones = null;

		[PaletteReference]
		public readonly string MuzzlePalette = "effect";

		[Desc("If true, passengers independently acquire and fire at targets of opportunity.")]
		public readonly bool PerPassengerTargeting = true;

		public override object Create(ActorInitializer init) { return new AttackGarrisonedSP(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (PortOffsets == null || PortOffsets.Length == 0)
				throw new YamlException("PortOffsets must have at least one entry.");

			if (PortYaws != null && PortYaws.Length != PortOffsets.Length)
				throw new YamlException("PortYaws must define an angle for each port.");

			if (PortCones != null && PortCones.Length != PortOffsets.Length)
				throw new YamlException("PortCones must define an angle for each port.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class AttackGarrisonedSP : AttackFollow, INotifyPassengerEntered, INotifyPassengerExited, INotifyGarrisonerEntered, INotifyGarrisonerExited, IRender, ITick
	{
		public new readonly AttackGarrisonedSPInfo Info;
		INotifyAttack[] notifyAttacks;
		readonly Lazy<BodyOrientation> coords;
		readonly List<Actor> passengers = [];
		readonly List<Armament> armaments = [];
		readonly Dictionary<Actor, IFacing> paxFacing = [];
		readonly Dictionary<Actor, IPositionable> paxPos = [];
		readonly Dictionary<Actor, RenderSprites> paxRender = [];
		readonly Dictionary<Actor, AutoTarget> paxAutoTarget = [];
		readonly List<AnimationWithOffset> muzzles = [];
		readonly FirePortSP[] ports;

		public AttackGarrisonedSP(Actor self, AttackGarrisonedSPInfo info)
			: base(self, info)
		{
			Info = info;
			coords = Exts.Lazy(self.Trait<BodyOrientation>);

			if (info.PortYaws != null && info.PortYaws.Length > 0)
			{
				ports = new FirePortSP[info.PortOffsets.Length];
				for (var i = 0; i < info.PortOffsets.Length; i++)
				{
					ports[i] = new FirePortSP
					{
						Offset = info.PortOffsets[i],
						Yaw = info.PortYaws[i],
						Cone = info.PortCones != null ? info.PortCones[i] : WAngle.FromDegrees(360)
					};
				}
			}
			else
			{
				ports = new FirePortSP[info.PortOffsets.Length];
				for (var i = 0; i < info.PortOffsets.Length; i++)
				{
					ports[i] = new FirePortSP
					{
						Offset = info.PortOffsets[i],
						Yaw = WAngle.Zero,
						Cone = WAngle.FromDegrees(360)
					};
				}
			}
		}

		protected override void Created(Actor self)
		{
			notifyAttacks = self.TraitsImplementing<INotifyAttack>().ToArray();
			base.Created(self);
		}

		protected override Func<IEnumerable<Armament>> InitializeGetArmaments(Actor self)
		{
			return () => armaments;
		}

		void OnActorEntered(Actor self, Actor actor)
		{
			passengers.Add(actor);
			paxFacing.Add(actor, actor.Trait<IFacing>());
			paxPos.Add(actor, actor.Trait<IPositionable>());
			paxRender.Add(actor, actor.Trait<RenderSprites>());
			paxAutoTarget.Add(actor, actor.TraitOrDefault<AutoTarget>());

			foreach (var a in actor.TraitsImplementing<Armament>())
			{
				if (Info.Armaments.Contains(a.Info.Name))
				{
					a.AddNotifyAttacks(self, notifyAttacks);
					armaments.Add(a);
				}
			}
		}

		void OnActorExited(Actor self, Actor actor)
		{
			passengers.Remove(actor);
			paxFacing.Remove(actor);
			paxPos.Remove(actor);
			paxRender.Remove(actor);
			paxAutoTarget.Remove(actor);

			foreach (var a in armaments.ToList())
			{
				if (a.Actor == actor)
				{
					a.RemoveNotifyAttacks(notifyAttacks);
					armaments.Remove(a);
				}
			}
		}

		void INotifyPassengerEntered.OnPassengerEntered(Actor self, Actor passenger)
		{
			OnActorEntered(self, passenger);
		}

		void INotifyPassengerExited.OnPassengerExited(Actor self, Actor passenger)
		{
			OnActorExited(self, passenger);
		}

		void INotifyGarrisonerEntered.OnGarrisonerEntered(Actor self, Actor garrisoner)
		{
			OnActorEntered(self, garrisoner);
		}

		void INotifyGarrisonerExited.OnGarrisonerExited(Actor self, Actor garrisoner)
		{
			OnActorExited(self, garrisoner);
		}

		FirePortSP SelectFirePort(Actor self, WAngle targetYaw)
		{
			var bodyYaw = facing != null ? facing.Facing : WAngle.Zero;
			var indices = Enumerable.Range(0, ports.Length).Shuffle(self.World.SharedRandom);
			foreach (var i in indices)
			{
				var yaw = bodyYaw + ports[i].Yaw;
				var leftTurn = (yaw - targetYaw).Angle;
				var rightTurn = (targetYaw - yaw).Angle;
				if (Math.Min(leftTurn, rightTurn) <= ports[i].Cone.Angle)
					return ports[i];
			}

			return null;
		}

		WVec PortOffset(Actor self, FirePortSP p)
		{
			var bodyOrientation = coords.Value.QuantizeOrientation(self.Orientation);
			return coords.Value.LocalToWorld(p.Offset.Rotate(bodyOrientation));
		}

		public override void DoAttack(Actor self, in Target target)
		{
			if (!CanAttack(self, target))
				return;

			var pos = self.CenterPosition;
			var targetedPosition = GetTargetPosition(pos, target);
			var targetYaw = (targetedPosition - pos).Yaw;

			foreach (var a in armaments)
			{
				if (a.IsTraitDisabled)
					continue;

				var port = SelectFirePort(self, targetYaw);
				if (port == null)
					return;

				paxFacing[a.Actor].Facing = targetYaw;
				paxPos[a.Actor].SetCenterPosition(a.Actor, pos + PortOffset(self, port));

				if (!a.CheckFire(a.Actor, facing, target))
					continue;

				if (a.Info.MuzzleSequence != null)
				{
					var muzzleAnim = new Animation(self.World, paxRender[a.Actor].GetImage(a.Actor), () => targetYaw);
					var sequence = a.Info.MuzzleSequence;
					var muzzleFlash = new AnimationWithOffset(muzzleAnim,
						() => PortOffset(self, port),
						() => false,
						p => RenderUtils.ZOffsetFromCenter(self, p, 1024));

					muzzles.Add(muzzleFlash);
					muzzleAnim.PlayThen(sequence, () => muzzles.Remove(muzzleFlash));
				}
			}
		}

		void DoPerPassengerAttack(Actor self)
		{
			if (!Info.PerPassengerTargeting)
				return;

			var pos = self.CenterPosition;

			foreach (var a in armaments)
			{
				if (a.IsTraitDisabled || a.IsTraitPaused)
					continue;

				if (paxAutoTarget.TryGetValue(a.Actor, out var autoTarget) && autoTarget != null && !autoTarget.IsTraitDisabled)
				{
					if (autoTarget.Stance < UnitStance.Defend)
						continue;

					var paxTarget = autoTarget.ScanForTarget(self, false, false);
					if (!paxTarget.IsValidFor(self))
						continue;

					var targetedPosition = GetTargetPosition(pos, paxTarget);
					var targetYaw = (targetedPosition - pos).Yaw;

					var port = SelectFirePort(self, targetYaw);
					if (port == null)
						continue;

					paxFacing[a.Actor].Facing = targetYaw;
					paxPos[a.Actor].SetCenterPosition(a.Actor, pos + PortOffset(self, port));

					if (!a.CheckFire(a.Actor, facing, paxTarget))
						continue;

					if (a.Info.MuzzleSequence != null)
					{
						var muzzleAnim = new Animation(self.World, paxRender[a.Actor].GetImage(a.Actor), () => targetYaw);
						var sequence = a.Info.MuzzleSequence;
						var muzzleFlash = new AnimationWithOffset(muzzleAnim,
							() => PortOffset(self, port),
							() => false,
							p => RenderUtils.ZOffsetFromCenter(self, p, 1024));

						muzzles.Add(muzzleFlash);
						muzzleAnim.PlayThen(sequence, () => muzzles.Remove(muzzleFlash));
					}

					foreach (var npa in notifyAttacks)
						npa.Attacking(self, paxTarget, a, null);
				}
			}
		}

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			var pal = wr.Palette(Info.MuzzlePalette);

			foreach (var m in muzzles)
				foreach (var r in m.Render(self, pal))
					yield return r;
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			yield break;
		}

		protected override void Tick(Actor self)
		{
			base.Tick(self);

			foreach (var m in muzzles.ToArray())
				m.Animation.Tick();

			if (IsTraitDisabled || IsTraitPaused)
				return;

			if (!RequestedTarget.IsValidFor(self) && !OpportunityTarget.IsValidFor(self))
				DoPerPassengerAttack(self);
		}
	}
}
