#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License,
 * either version 3 of the License, or (at your option) any later version.
 * For more information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	// ESSENTIAL-PASS-01 (Opus 11-24): the only building-pause provider on the tiers (ArmyFirstBotModule@af02) lets a first copy through its own vote, but the base
	// builder's IsArmyFirstEssential was false for an ordinary first copy and for a cash trickler, so the projected-share vote (General class) and the
	// mobile-spend reserve (TickQueue) could still hold them. The first copy of every building type and every trickler is now an essential.
	[TestFixture]
	public sealed class EssentialPass01Test
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void AFirstCopyAndATricklerAreEssentialButAnExtraCopyIsNot()
		{
			Assert.That(EssentialPassCA.Essential(true, false, false), Is.True, "first copy of an eligible kind");
			Assert.That(EssentialPassCA.Essential(true, true, false), Is.False, "second copy");
			Assert.That(EssentialPassCA.Essential(false, false, false), Is.False, "wall/defence/fake/proxy kind");
			Assert.That(EssentialPassCA.Essential(false, true, true), Is.True, "a trickler up to its limit");
		}

		[Test]
		public void AnEssentialIsNeverHeldByTheShareOrReserveVotesWhileAnExtraCopyIs()
		{
			var share = new ArmyShareCA();
			share.Refresh(1800, 0, 18600, 0);
			Assert.That(ArmyFirstEval.ShareHolds(false, RecoverySpendPolicyCA.Admission.General, share, 1000, 625), Is.True, "V02: the extra is held");
			Assert.That(ArmyFirstEval.ShareHolds(true, RecoverySpendPolicyCA.Admission.OpeningOrEssential, share, 1000, 625), Is.False);
			Assert.That(ArmyFirstEval.ReserveHolds(true, RecoverySpendPolicyCA.Admission.OpeningOrEssential, 500, 0, 1000, 2000), Is.False);
			Assert.That(BaseBuilderQueueEvalCA.MobileSpendBlocks(900, 800, 700, true), Is.False, "an essential keeps the old floor under the mobile reserve");
		}

		[Test]
		public void TheRuleSitsInIsArmyFirstEssentialAheadOfTheFirstFactoryRuleAndReusesTheKindCache()
		{
			var q = Source("mods/CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs");
			var rule = q.IndexOf("EssentialPassCA.Essential(FirstCopyKind(actorInfo), OwnedOrQueued(actorInfo), actorInfo.HasTraitInfo<CashTricklerInfo>())", StringComparison.Ordinal);
			Assert.That(rule, Is.GreaterThan(q.IndexOf("bool IsArmyFirstEssential(ActorInfo actorInfo)", StringComparison.Ordinal)));
			Assert.That(rule, Is.LessThan(q.IndexOf("// One queued-but-unplaced factory is enough", StringComparison.Ordinal)));
			Assert.That(q, Does.Contain("bool EssentialPassOn => baseBuilder.EssentialPassArmed && !ablateEssentialPass.Off(player, AblationIds.Af2EssentialPass);"));
			Assert.That(q, Does.Contain("bool UnlistedOnceKind(ActorInfo info) =>\n\t\t\t!(baseBuilder.Info.BuildingFractions != null && baseBuilder.Info.BuildingFractions.ContainsKey(info.Name)) && FirstCopyKind(info);"));
		}

		[Test]
		public void TheRowIsRegisteredAndOnlyGenericbotArmsIt()
		{
			Assert.That(Source("mods/CA/Traits/BotModules/BotModuleLogic/AblationIds.ArmyFirst02.cs"), Does.Contain("Af2EssentialPass = \"af2_essential_pass\""));
			var player = MiniYaml.FromString(Source("packaging/ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			var armed = player.Where(n => n.Key.StartsWith("BaseBuilderBotModuleCA@", StringComparison.Ordinal)
				&& n.Value.Nodes.Any(x => x.Key == "EssentialPassFirstCopies" && x.Value.Value == "true")).ToArray();
			Assert.That(armed.Length, Is.EqualTo(1));
			Assert.That(armed[0].Key, Is.EqualTo("BaseBuilderBotModuleCA@generic"));
			Assert.That(armed[0].Value.Nodes.Single(x => x.Key == "SwitchCondition").Value.Value, Is.EqualTo("genericbot"));
		}
	}
}
