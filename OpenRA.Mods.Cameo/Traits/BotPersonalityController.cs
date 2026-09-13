#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class BotPersonalityControllerInfo : ConditionalTraitInfo
	{
		public readonly string[] Conditions =
		{
			"personality-rush",
			"personality-turtle",
			"personality-tech",
			"personality-expansion",
			"personality-steamroller"
		};

		public readonly string PersonalityPrefix = "personality-";

		public override object Create(ActorInitializer init) { return new BotPersonalityController(init.Self, this); }
	}

	public class BotPersonalityController : ConditionalTrait<BotPersonalityControllerInfo>, IResolveOrder
	{
		int personalityToken = Actor.InvalidConditionToken;

		public string CurrentPersonality { get; private set; } = "";

		public BotPersonalityController(Actor self, BotPersonalityControllerInfo info)
			: base(info) { }

		protected override void TraitEnabled(Actor self)
		{
			var condition = Info.Conditions.Random(self.World.SharedRandom);
			personalityToken = self.GrantCondition(condition);
			CurrentPersonality = PersonalityName(condition, Info.PersonalityPrefix);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (personalityToken != Actor.InvalidConditionToken)
				personalityToken = self.RevokeCondition(personalityToken);

			CurrentPersonality = "";
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || order.OrderString != "SetBotPersonality")
				return;

			var condition = Info.Conditions.FirstOrDefault(c => PersonalityName(c, Info.PersonalityPrefix) == order.TargetString);
			if (condition == null || PersonalityName(condition, Info.PersonalityPrefix) == CurrentPersonality)
				return;

			self.World.AddFrameEndTask(_ =>
			{
				if (IsTraitDisabled || PersonalityName(condition, Info.PersonalityPrefix) == CurrentPersonality)
					return;

				if (personalityToken != Actor.InvalidConditionToken)
					personalityToken = self.RevokeCondition(personalityToken);

				personalityToken = self.GrantCondition(condition);
				CurrentPersonality = PersonalityName(condition, Info.PersonalityPrefix);
			});
		}

		internal static string PersonalityName(string condition, string prefix)
		{
			return condition.StartsWith(prefix, StringComparison.Ordinal) ? condition[prefix.Length..] : "";
		}
	}
}
