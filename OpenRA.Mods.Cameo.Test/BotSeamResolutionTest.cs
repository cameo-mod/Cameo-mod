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
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class BotSeamResolutionTest
	{
		// A provider trait is constructed disabled (RequiresCondition grants later), so a consumer
		// that caches `FirstEnabledTraitOrDefault()` at Created/TraitEnabled caches null forever.
		// The seam contract is: cache the candidate ARRAY, resolve enabled at each use — then a
		// provider that enables after the consumer's construction is still found.
		sealed class StubMainTargetProvider : IBotMainTargetProvider, IDisabledTrait
		{
			public bool IsTraitDisabled { get; set; }
			public Player MainTarget => null;
		}

		sealed class StubEnemyCompositionProvider : IBotEnemyCompositionProvider, IDisabledTrait
		{
			public bool IsTraitDisabled { get; set; }
			public bool TryGetEnemyComposition(out IReadOnlyDictionary<string, int> valueByActorType)
			{
				valueByActorType = null;
				return false;
			}
		}

		[Test]
		public void LateEnabledMainTargetProviderIsSeen()
		{
			var provider = new StubMainTargetProvider { IsTraitDisabled = true };
			var candidates = new IBotMainTargetProvider[] { provider };

			// The cached-at-enable-time read fails while the provider is still disabled.
			Assert.That(candidates.FirstEnabledTraitOrDefault(), Is.Null);

			provider.IsTraitDisabled = false;
			Assert.That(candidates.FirstEnabledTraitOrDefault(), Is.SameAs(provider));
		}

		[Test]
		public void LateEnabledCompositionProviderIsSeen()
		{
			var provider = new StubEnemyCompositionProvider { IsTraitDisabled = true };
			var candidates = new IBotEnemyCompositionProvider[] { provider };

			Assert.That(candidates.FirstEnabledTraitOrDefault(), Is.Null);

			provider.IsTraitDisabled = false;
			Assert.That(candidates.FirstEnabledTraitOrDefault(), Is.SameAs(provider));
		}

		[Test]
		public void DisabledProviderStaysSkipped()
		{
			var disabled = new StubMainTargetProvider { IsTraitDisabled = true };
			var enabled = new StubMainTargetProvider { IsTraitDisabled = false };
			var candidates = new IBotMainTargetProvider[] { disabled, enabled };

			Assert.That(candidates.FirstEnabledTraitOrDefault(), Is.SameAs(enabled));
		}
	}
}
