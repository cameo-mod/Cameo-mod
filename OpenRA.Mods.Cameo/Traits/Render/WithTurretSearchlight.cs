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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	public enum SearchlightShape
	{
		Beam,
		Cone
	}

	[Desc("Registers a decorative GlowRenderer beam from a turret's current facing.")]
	public class WithTurretSearchlightInfo : ConditionalTraitInfo
	{
		[Desc("Turreted 'Turret' key to follow.")]
		public readonly string Turret = "primary";

		[Desc("Searchlight source offset relative to the turret orientation. X is lateral to the beam, Y forward, Z up.")]
		public readonly WVec Offset = new(0, 0, 128);

		[Desc("Derive the source height from the turret weapon's muzzle instead of Offset.Z (falls back to Offset.Z if the actor has no armament).")]
		public readonly bool UseWeaponHeight = true;

		[Desc("Fallback length of the searchlight beam when no matching armament range is available.")]
		public readonly WDist Range = WDist.FromCells(6);

		[Desc("Speed at which the searchlight turns toward the turret's aim direction.")]
		public readonly WAngle TurnSpeed = new(24);

		[Desc("Color of the registered glow.")]
		public readonly Color Color = Color.FromArgb(170, Color.LightYellow);

		[Desc("Scales GlowRenderer radius and brightness for this beam.")]
		public readonly float GlowScale = 0.75f;

		[Desc("Extra brightness multiplier, independent of glow radius.")]
		public readonly float Intensity = 1.0f;

		[Desc("Searchlight rendering shape. Cone tapers narrow-to-wide with an endpoint pool; Beam is a uniform-width beam.")]
		public readonly SearchlightShape Shape = SearchlightShape.Cone;

		[Desc("Glow radius scale at the Shape=Cone source (narrow end, at the turret).")]
		public readonly float ConeStartScale = 0.4f;

		[Desc("Glow radius scale at the Shape=Cone target (wide end, the far pool).")]
		public readonly float ConeEndScale = 1.6f;

		[Desc("Extra brightness ramped toward the Shape=Cone wide end to form the light pool.")]
		public readonly float EndpointBoost = 1.2f;

		[Desc("Lobby option id that enables weather searchlights.")]
		public readonly string WeatherOption = "weather";

		[Desc("Lobby option value that enables weather searchlights.")]
		public readonly string WeatherValue = "weather";

		[Desc("Disable the searchlight while the owning player is in Low or Critical power.")]
		public readonly bool DisableOnLowPower = true;

		[Desc("Maximum random idle sweep nudge (clockwise or counter-clockwise) applied while idle.")]
		public readonly WAngle IdleScanRange = WAngle.FromDegrees(45);

		[Desc("Minimum delay (in ticks) between idle sweep nudges.")]
		public readonly int IdleScanMinDelay = 75;

		[Desc("Maximum delay (in ticks) between idle sweep nudges.")]
		public readonly int IdleScanMaxDelay = 250;

		[Desc("Ticks after last engaging an enemy before idle sweeping resumes.")]
		public readonly int IdleScanCooldown = 200;

		[Desc("Turn speed used to slew toward the idle sweep target (gentler than tracking TurnSpeed; default 3x slower).")]
		public readonly WAngle IdleTurnSpeed = new(4);

		[Desc("Damage states where the searchlight flickers.")]
		public readonly DamageState FlickerDamageStates = DamageState.Heavy | DamageState.Critical;

		[Desc("How many render frames each flicker sample lasts.")]
		public readonly int FlickerSampleFrames = 3;

		[Desc("Chance per flicker sample that the searchlight turns fully off.")]
		public readonly int FlickerOffChance = 12;

		[Desc("Chance per flicker sample that the searchlight dims instead of rendering normally.")]
		public readonly int FlickerDimChance = 28;

		[Desc("Brightness multiplier used for dim flicker samples.")]
		public readonly float FlickerDimIntensity = 0.45f;

		public override object Create(ActorInitializer init) { return new WithTurretSearchlight(init.Self, this); }
	}

	public class WithTurretSearchlight : ConditionalTrait<WithTurretSearchlightInfo>, ITick, IRender, INotifyOwnerChanged
	{
		Turreted turret;
		AttackFollow attack;
		Armament[] armaments;
		Armament muzzleArmament;
		Barrel muzzleBarrel;
		PowerManager playerPower;
		bool weatherEnabled;
		WAngle searchlightYaw;
		bool initializedYaw;

		// Idle sweep state (cosmetic only — driven by Game.CosmeticRandom, never the synced RNG).
		int lastEngagedTick;
		int nextScanTick;
		WAngle idleTargetYaw;
		bool idleTargetInit;

		public WithTurretSearchlight(Actor self, WithTurretSearchlightInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			base.Created(self);

			turret = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == Info.Turret);
			attack = self.TraitsImplementing<AttackFollow>()
				.FirstOrDefault(a => a.Info is AttackTurretedInfo ati && ati.Turrets.Contains(Info.Turret));
			armaments = self.TraitsImplementing<Armament>().Where(a => a.Info.Turret == Info.Turret).ToArray();

			// Cache the first barrel-bearing armament so the source height can track the real muzzle.
			muzzleArmament = armaments.FirstOrDefault(a => a.Barrels.Length > 0);
			muzzleBarrel = muzzleArmament?.Barrels[0];
			playerPower = self.Owner.PlayerActor.TraitOrDefault<PowerManager>();

			// Treat freshly-built towers as already idle (no cooldown) so they sweep without a 60s wait.
			lastEngagedTick = self.World.WorldTick - Info.IdleScanCooldown;
			ScheduleNextScan(self);

			var selected = self.World.LobbyInfo.GlobalSettings.OptionOrDefault(Info.WeatherOption, "none");
			weatherEnabled = selected == Info.WeatherValue;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !weatherEnabled || !HasSearchlightPower() || turret == null)
				return;

			var target = CurrentAttackTarget(self);
			var engaged = target.IsValidFor(self);
			if (engaged)
				lastEngagedTick = self.World.WorldTick;

			var desiredYaw = DesiredSearchlightYaw(self, target, engaged);
			if (!initializedYaw)
			{
				searchlightYaw = desiredYaw;
				initializedYaw = true;
				return;
			}

			var turnSpeed = engaged ? Info.TurnSpeed : Info.IdleTurnSpeed;
			searchlightYaw = Common.Util.TickFacing(searchlightYaw, desiredYaw, turnSpeed);
		}

		WAngle DesiredSearchlightYaw(Actor self, Target target, bool engaged)
		{
			if (engaged)
			{
				var pivot = self.CenterPosition + turret.Position(self);
				var targetOffset = attack.GetTargetPosition(pivot, target) - pivot;
				if (targetOffset.HorizontalLengthSquared > 0)
				{
					idleTargetInit = false;
					return targetOffset.Yaw;
				}
			}

			var holdYaw = initializedYaw ? searchlightYaw : turret.WorldOrientation.Yaw;

			// Hold the last facing during the post-engagement cooldown; only sweep once truly idle.
			if (self.World.WorldTick - lastEngagedTick < Info.IdleScanCooldown)
			{
				idleTargetInit = false;
				return holdYaw;
			}

			if (!idleTargetInit)
			{
				idleTargetYaw = holdYaw;
				idleTargetInit = true;
			}

			if (self.World.WorldTick >= nextScanTick)
			{
				var magnitude = 1 + Game.CosmeticRandom.Next(Info.IdleScanRange.Angle);
				var sign = Game.CosmeticRandom.Next(2) == 0 ? -1 : 1;
				idleTargetYaw += new WAngle(sign * magnitude);
				ScheduleNextScan(self);
			}

			return idleTargetYaw;
		}

		bool HasSearchlightPower()
		{
			return !Info.DisableOnLowPower || playerPower == null || playerPower.PowerState == PowerState.Normal;
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, OpenRA.Player oldOwner, OpenRA.Player newOwner)
		{
			playerPower = newOwner.PlayerActor.TraitOrDefault<PowerManager>();
		}

		void ScheduleNextScan(Actor self)
		{
			var max = System.Math.Max(Info.IdleScanMinDelay + 1, Info.IdleScanMaxDelay);
			nextScanTick = self.World.WorldTick + Game.CosmeticRandom.Next(Info.IdleScanMinDelay, max);
		}

		Target CurrentAttackTarget(Actor self)
		{
			if (attack == null || !attack.IsAiming)
				return Target.Invalid;

			return attack.RequestedTarget.IsValidFor(self) ? attack.RequestedTarget : attack.OpportunityTarget;
		}

		int MaxSearchlightLength()
		{
			if (armaments == null || armaments.Length == 0)
				return Info.Range.Length;

			var enabledArmaments = armaments.Where(a => !a.IsTraitDisabled && !a.IsTraitPaused).ToArray();
			var ranges = enabledArmaments.Length > 0 ? enabledArmaments : armaments;
			return ranges.Select(a => a.MaxRange().Length).DefaultIfEmpty(Info.Range.Length).Max();
		}

		WPos SearchlightTarget(Actor self, WPos source, WRot orientation)
		{
			var maxLength = MaxSearchlightLength();
			var target = CurrentAttackTarget(self);
			if (!target.IsValidFor(self))
				return source + new WVec(0, -maxLength, 0).Rotate(orientation);

			var targetOffset = attack.GetTargetPosition(source, target) - source;
			var targetHorizontalLength = targetOffset.HorizontalLength;
			if (targetHorizontalLength == 0)
				return source + targetOffset;

			var horizontalLength = System.Math.Min(targetHorizontalLength, maxLength);
			var horizontalTarget = new WVec(0, -horizontalLength, 0).Rotate(orientation);

			// Weapons range against aircraft is horizontal, but the light should aim at aircraft
			// height rather than the ground projection under the target.
			var z = (int)((long)targetOffset.Z * horizontalLength / targetHorizontalLength);
			return source + new WVec(horizontalTarget.X, horizontalTarget.Y, z);
		}

		// Screen-px margin for glow bleed beyond the bare source/target segment.
		const int GlowScreenMargin = 128;

		IEnumerable<IRenderable> IRender.Render(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || !weatherEnabled || !HasSearchlightPower() || turret == null)
				yield break;

			// The screen-space glow geometry breaks down at the extreme zoom-out that only observers can
			// reach (below the player zoom floor, where WorldDownscaleFactor climbs above 1), producing
			// oversized, detached beams. Skip the cosmetic searchlight there — no player in a match sees
			// this zoom level, so there is nothing to fix visually, only to hide.
			if (wr.Viewport.Zoom < wr.Viewport.MinZoom)
				yield break;

			var orientation = WRot.FromYaw(searchlightYaw);
			var source = self.CenterPosition + turret.Position(self) + Info.Offset.Rotate(orientation);

			// Take the source height from the real weapon muzzle when available (horizontal stays
			// centered/swept; using the full muzzle position would orbit with the unmoved turret head).
			if (Info.UseWeaponHeight && muzzleArmament != null)
				source = new WPos(source.X, source.Y, self.CenterPosition.Z + muzzleArmament.MuzzleOffset(self, muzzleBarrel).Z);

			var target = SearchlightTarget(self, source, orientation);

			// Per-frame, direction-aware cull: skip registering a glow whose cone falls entirely
			// off-screen (it would cost a post-process slot with no visible pixels). The square
			// ScreenBounds keeps the actor alive long enough for this test to run while near the edge.
			if (!ConeIntersectsViewport(wr, source, target))
				yield break;

			var flickerIntensity = FlickerIntensity(self);
			if (flickerIntensity <= 0f)
				yield break;

			yield return EmitSearchlight(source, target, flickerIntensity);
		}

		float FlickerIntensity(Actor self)
		{
			if (!Info.FlickerDamageStates.HasFlag(self.GetDamageState()))
				return 1f;

			var sampleFrames = System.Math.Max(1, Info.FlickerSampleFrames);
			var sample = Game.RenderFrame / sampleFrames;
			var roll = FlickerHash(self.ActorID, sample) % 100;
			if (roll < Info.FlickerOffChance)
				return 0f;

			if (roll < Info.FlickerOffChance + Info.FlickerDimChance)
				return Info.FlickerDimIntensity;

			return 1f;
		}

		static int FlickerHash(uint actorId, int sample)
		{
			unchecked
			{
				var x = (int)actorId * 1103515245 + sample * 12345;
				x ^= x >> 16;
				x *= 0x45d9f3b;
				x ^= x >> 16;
				return x & 0x7fffffff;
			}
		}

		static bool ConeIntersectsViewport(WorldRenderer wr, WPos source, WPos target)
		{
			var s = wr.ScreenPxPosition(source);
			var t = wr.ScreenPxPosition(target);
			var left = System.Math.Min(s.X, t.X) - GlowScreenMargin;
			var right = System.Math.Max(s.X, t.X) + GlowScreenMargin;
			var top = System.Math.Min(s.Y, t.Y) - GlowScreenMargin;
			var bottom = System.Math.Max(s.Y, t.Y) + GlowScreenMargin;

			var vp = wr.Viewport;
			return right >= vp.TopLeft.X && left <= vp.BottomRight.X
				&& bottom >= vp.TopLeft.Y && top <= vp.BottomRight.Y;
		}

		IRenderable EmitSearchlight(WPos source, WPos target, float intensityScale)
		{
			// Cone tapers the glow radius from narrow (turret) to wide (far pool) in a single
			// GlowRenderer registration; Beam keeps a uniform radius with no endpoint pool.
			if (Info.Shape == SearchlightShape.Cone)
				return new TurretSearchlightRenderable(new[]
				{
					new SearchlightGlow(source, target, Info.Color, Info.ConeStartScale, Info.ConeEndScale, Info.Intensity * intensityScale, Info.EndpointBoost)
				});

			return new TurretSearchlightRenderable(new[]
			{
				new SearchlightGlow(source, target, Info.Color, Info.GlowScale, Info.GlowScale, Info.Intensity * intensityScale, 0f)
			});
		}

		IEnumerable<Rectangle> IRender.ScreenBounds(Actor self, WorldRenderer wr)
		{
			if (IsTraitDisabled || !weatherEnabled || turret == null)
				yield break;

			// Direction-agnostic square covering the cone's full reach in any sweep direction, so the
			// ScreenMap keeps this actor renderable while any part of the cone could be on-screen.
			// (The ScreenMap caches a static building's bounds, so this must not depend on facing.)
			var center = self.CenterPosition + turret.Position(self);
			var maxLength = MaxSearchlightLength();
			var c = wr.ScreenPxPosition(center);
			var ex = wr.ScreenPxPosition(center + new WVec(maxLength, 0, 0)) - c;
			var ey = wr.ScreenPxPosition(center + new WVec(0, maxLength, 0)) - c;
			var r = System.Math.Max(
				System.Math.Max(System.Math.Abs(ex.X), System.Math.Abs(ex.Y)),
				System.Math.Max(System.Math.Abs(ey.X), System.Math.Abs(ey.Y))) + GlowScreenMargin;

			yield return new Rectangle(c.X - r, c.Y - r, 2 * r, 2 * r);
		}
	}

	readonly struct SearchlightGlow
	{
		public readonly WPos Source;
		public readonly WPos Target;
		public readonly Color Color;
		public readonly float GlowScale;
		public readonly float GlowScaleEnd;
		public readonly float Intensity;
		public readonly float EndpointBoost;

		public SearchlightGlow(WPos source, WPos target, Color color, float glowScale, float glowScaleEnd, float intensity, float endpointBoost)
		{
			Source = source;
			Target = target;
			Color = color;
			GlowScale = glowScale;
			GlowScaleEnd = glowScaleEnd;
			Intensity = intensity;
			EndpointBoost = endpointBoost;
		}
	}

	sealed class TurretSearchlightRenderable : IRenderable, IFinalizedRenderable
	{
		readonly SearchlightGlow[] glows;

		public TurretSearchlightRenderable(IEnumerable<SearchlightGlow> glows)
		{
			this.glows = glows.ToArray();
		}

		public WPos Pos => glows.Length > 0 ? glows[0].Source : WPos.Zero;
		public int ZOffset => 0;
		public bool IsDecoration => true;

		public IRenderable WithZOffset(int newOffset) { return this; }
		public IRenderable OffsetBy(in WVec vec)
		{
			var offset = vec;
			return new TurretSearchlightRenderable(glows.Select(g =>
				new SearchlightGlow(g.Source + offset, g.Target + offset, g.Color, g.GlowScale, g.GlowScaleEnd, g.Intensity, g.EndpointBoost)));
		}

		public IRenderable AsDecoration() { return this; }
		public IFinalizedRenderable PrepareRender(WorldRenderer wr) { return this; }

		public void Render(WorldRenderer wr)
		{
			var glowRenderer = wr.World.WorldActor.TraitOrDefault<GlowRenderer>();
			if (glowRenderer == null)
				return;

			foreach (var glow in glows)
				glowRenderer.RegisterGlow(glow.Source, glow.Target, glow.Color, glow.GlowScale,
					intensity: glow.Intensity, scaleEnd: glow.GlowScaleEnd, endpointBoost: glow.EndpointBoost);
		}

		public void RenderDebugGeometry(WorldRenderer wr) { }
		public Rectangle ScreenBounds(WorldRenderer wr) { return Rectangle.Empty; }
	}
}
