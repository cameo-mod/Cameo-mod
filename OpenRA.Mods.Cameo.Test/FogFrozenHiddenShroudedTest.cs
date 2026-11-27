// FOG-FROZEN-01: BotFogMemory must not remember frozen actors that are Hidden (a visibility modifier hides the backing actor)
// or Shrouded (no footprint cell explored yet). Pure rule tests plus source pins for both loops (per-owner walk and snapshot).
using System;
using System.IO;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;
using OpenRA.Mods.Cameo.Traits.BotModules;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class FogFrozenHiddenShroudedTest
	{
		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		[Test]
		public void NeverExploredFrozenActorIsNotRemembered()
		{
			Assert.That(BotFogMemory.FrozenUnrememberable(hidden: false, shrouded: true, skipHiddenShrouded: true), Is.True);
		}

		[Test]
		public void HiddenFrozenActorIsNotRemembered()
		{
			Assert.That(BotFogMemory.FrozenUnrememberable(hidden: true, shrouded: false, skipHiddenShrouded: true), Is.True);
		}

		[Test]
		public void ExploredThenFoggedFrozenActorIsStillRemembered()
		{
			Assert.That(BotFogMemory.FrozenUnrememberable(hidden: false, shrouded: false, skipHiddenShrouded: true), Is.False);
		}

		[Test]
		public void SwitchOffKeepsTheOldWalk()
		{
			Assert.That(BotFogMemory.FrozenUnrememberable(hidden: true, shrouded: false, skipHiddenShrouded: false), Is.False);
			Assert.That(BotFogMemory.FrozenUnrememberable(hidden: false, shrouded: true, skipHiddenShrouded: false), Is.False);
		}

		[Test]
		public void SwitchIsOnByDefault()
		{
			Assert.That(new MasterAiBotModuleInfo().SkipHiddenShroudedFrozen, Is.True);
		}

		[Test]
		public void TheSkipHasItsOwnAblationRow()
		{
			Assert.That(AblationIds.FogFrozenSkip, Is.EqualTo("fog_frozen_skip"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain("fog_frozen_skip"));
		}

		[Test]
		public void SourcePinBothLoopsApplyTheRuleAndTheSnapshotKeepsItsValidityFilter()
		{
			var fog = Source("mods/Cameo/Traits/BotModules/BotFogMemory.cs");
			Assert.That(fog.Split("FrozenUnrememberable(fa.Hidden, fa.Shrouded, skipFrozen)").Length - 1, Is.EqualTo(2));
			Assert.That(fog.Split("var skipFrozen = info.SkipHiddenShroudedFrozen && !ablateFrozenSkip.Off(viewer, AblationIds.FogFrozenSkip);").Length - 1, Is.EqualTo(2));
			Assert.That(fog, Does.Contain("if (!fa.IsValid)"));
			Assert.That(fog, Does.Contain("if (!fa.IsValid || fa.Owner != enemy || table.ContainsKey(fa.ID))"));
		}
	}
}
