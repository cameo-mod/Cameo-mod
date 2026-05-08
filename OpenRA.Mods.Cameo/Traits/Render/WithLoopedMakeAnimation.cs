#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.Render
{
	[Desc("Replaces the sprite during construction by looping the make animation for a fixed duration,",
		"then playing a one-shot finish animation before becoming functional.",
		"Use instead of WithMakeAnimation on Zerg buildings to simulate StarCraft-style long build animations.")]
	public class WithLoopedMakeAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence to loop during construction.")]
		public readonly string LoopSequence = "make-loop";

		[SequenceReference]
		[Desc("Sequence to play once when the loop timer expires, before the building becomes functional.")]
		public readonly string FinishSequence = "make-finish";

		[Desc("Duration of the looping part of the construction animation in milliseconds at default game speed.")]
		public readonly int Duration = 10000;

		[GrantedConditionReference]
		[Desc("Condition granted while the construction animation is playing.")]
		public readonly string Condition = "build-incomplete";

		[Desc("Apply to sprite bodies with these names.")]
		public readonly string[] BodyNames = ["body"];

		public override object Create(ActorInitializer init) { return new WithLoopedMakeAnimation(init, this); }
	}

	public class WithLoopedMakeAnimation : INotifyCreated, ITick
	{
		readonly WithLoopedMakeAnimationInfo info;
		readonly bool skipMakeAnimation;

		WithSpriteBody[] wsbs;
		int token = Actor.InvalidConditionToken;
		int remainingTicks;
		bool looping;

		public WithLoopedMakeAnimation(ActorInitializer init, WithLoopedMakeAnimationInfo info)
		{
			this.info = info;
			skipMakeAnimation = init.Contains<SkipMakeAnimsInit>(info);
		}

		void INotifyCreated.Created(Actor self)
		{
			wsbs = self.TraitsImplementing<WithSpriteBody>()
				.Where(w => info.BodyNames.Contains(w.Info.Name))
				.ToArray();

			if (skipMakeAnimation)
				return;

			// Convert milliseconds to ticks (40ms per tick at default speed).
			remainingTicks = info.Duration / 40;
			looping = true;

			token = self.GrantCondition(info.Condition);

			// Defer by one frame so the condition system can evaluate RequiresCondition
			// on any WithSpriteBody traits before we try to play the animation.
			// (Conditional WSBs start with IsTraitDisabled = true until the first UpdateEnabled fires.)
			self.World.AddFrameEndTask(_ => PlayLoop(self));
		}

		void PlayLoop(Actor self)
		{
			var wsb = wsbs.FirstEnabledConditionalTraitOrDefault();
			if (wsb == null)
				return;

			wsb.PlayCustomAnimation(self, info.LoopSequence, () =>
			{
				if (looping)
					PlayLoop(self);
				else
					PlayFinish(self);
			});
		}

		void PlayFinish(Actor self)
		{
			var wsb = wsbs.FirstEnabledConditionalTraitOrDefault();
			if (wsb == null)
			{
				RevokeToken(self);
				return;
			}

			wsb.PlayCustomAnimation(self, info.FinishSequence, () =>
			{
				self.World.AddFrameEndTask(_ => RevokeToken(self));
			});
		}

		void RevokeToken(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		void ITick.Tick(Actor self)
		{
			if (!looping)
				return;

			if (--remainingTicks > 0)
				return;

			// Timer expired — let the current loop iteration finish, then transition to PlayFinish.
			looping = false;
		}
	}
}
