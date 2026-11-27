#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using NUnit.Framework;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class ObservedStyleQualificationTest
	{
		[Test]
		public void DisguiseProjectionUsesTheApparentOwnerAndType()
		{
			var enemy = (OpenRA.Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var neutral = (OpenRA.Player)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(OpenRA.Player));
			var apparent = (ActorInfo)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ActorInfo));
			Assert.That(ObservedStyleBotModule.EffectiveAppearance(enemy, enemy, apparent), Is.SameAs(apparent));
			Assert.That(ObservedStyleBotModule.EffectiveAppearance(enemy, neutral, apparent), Is.Null);
			Assert.That(ObservedStyleBotModule.EffectiveAppearance(enemy, enemy, null), Is.Null);

			var common = new FogStyleActor { TypeName = "apparent-tank", ActorId = 42, Vehicle = true, Value = 700 };
			var first = new FogStyleActor[1];
			var second = new FogStyleActor[1];
			Assert.That(FogStyleAdapter.Copy([new ObservedActor { Harvester = true, Value = 2000, StyleAppearance = common }], first), Is.EqualTo(1));
			Assert.That(FogStyleAdapter.Copy([new ObservedActor { Aircraft = true, Value = 500, StyleAppearance = common }], second), Is.EqualTo(1));
			Assert.That(first[0], Is.EqualTo(second[0]));
		}

		[Test]
		public void RepeatedGenerationCannotQualifyAndTargetSwitchResetsIt()
		{
			var gate = new ObservedStyleQualification();
			gate.Observe(1, 0, 2, 700, [800, 200]);
			gate.Observe(1, 75, 2, 700, [800, 200]);
			gate.Observe(2, 150, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(150), Is.EqualTo(-1));
			gate.Observe(3, 225, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(225), Is.Zero);
			Assert.That(gate.QualifiedClass(825), Is.EqualTo(-1));
			gate.Observe(3, 825, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(825), Is.EqualTo(-1));
			gate.Reset();
			Assert.That(gate.QualifiedClass(226), Is.EqualTo(-1));
		}

		[Test]
		public void ChallengerNeedsTwoFreshConfirmations()
		{
			var gate = new ObservedStyleQualification();
			gate.Observe(1, 0, 2, 700, [800, 200]);
			gate.Observe(2, 75, 2, 700, [800, 200]);
			gate.Observe(3, 150, 2, 700, [800, 200]);
			Assert.That(gate.QualifiedClass(150), Is.Zero);
			gate.Observe(4, 225, 2, 700, [200, 800]);
			Assert.That(gate.QualifiedClass(225), Is.EqualTo(-1));
			gate.Observe(5, 300, 2, 700, [200, 800]);
			Assert.That(gate.QualifiedClass(300), Is.EqualTo(1));
		}
	}
}
