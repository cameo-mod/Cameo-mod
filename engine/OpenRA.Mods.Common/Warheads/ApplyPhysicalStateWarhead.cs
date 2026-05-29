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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Apply changes to a named PhysicalState on hit actors.")]
	public class ApplyPhysicalStateWarhead : Warhead
	{
		[FieldLoader.Require]
		[Desc("Name of the PhysicalState to modify.")]
		public readonly string PhysicalStateName = null;

		[Desc("Amount to add to the PhysicalState. Can be negative to subtract.")]
		public readonly int Amount = 0;

		[Desc("Apply firepower damage modifiers to the PhysicalState change.")]
		public readonly bool ApplyFirepowerModifiers = true;

		[Desc("Targets apply damage modifiers to the PhysicalState change.")]
		public readonly bool ApplyDamageModifiers = true;

		[Desc("Affects actors in a radius. Leave at 0 to only affect the direct target.")]
		public readonly WDist Range = WDist.Zero;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			if (target.Type == TargetType.Invalid || Amount == 0)
				return;

			var firedBy = args.SourceActor;
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			var change = Amount;
			if (ApplyFirepowerModifiers && args != null && args.DamageModifiers.Length > 0)
				change = Util.ApplyPercentageModifiers(Amount, args.DamageModifiers);

			foreach (var actor in actors)
			{
				if (!IsValidAgainst(actor, firedBy))
					continue;

				var physicalState = actor.TraitsImplementing<PhysicalState>()
					.FirstOrDefault(ps => ps.Name == PhysicalStateName);

				physicalState?.ApplyChange(change, firedBy, ApplyDamageModifiers);
			}
		}
	}
}
