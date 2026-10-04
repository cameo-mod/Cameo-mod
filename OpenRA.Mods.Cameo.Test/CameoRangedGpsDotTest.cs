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

using NUnit.Framework;
using OpenRA.Mods.Cameo.Effects;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class CameoRangedGpsDotTest
	{
		// RADAR-ALLY: the engine's provider check (RangedGpsDotEffect.cs:87) counted
		// only providers owned by the viewer, so allied radars never shared their
		// GPS discs. The cameo gate counts allied providers in range — the same
		// ally semantics the classic GpsWatcher already had.

		static bool Render(bool traitDisabled = false, bool anyWatcherGranted = true,
			bool frozenPortraitVisible = false, bool actorLooksAllied = false,
			bool visibilityModifierDenies = false, bool shroudBlocks = false,
			bool providerInRange = true, bool alreadyVisible = false)
		{
			return CameoRangedGpsDotEffect.ShouldRenderDot(traitDisabled, anyWatcherGranted,
				frozenPortraitVisible, actorLooksAllied, visibilityModifierDenies,
				shroudBlocks, providerInRange, alreadyVisible);
		}

		[Test]
		public void OwnProviderInRangeShowsTheDot()
		{
			Assert.That(CameoRangedGpsDotEffect.ProviderCountsForViewer(
				sameOwner: true, allied: false, providerDead: false), Is.True);
			Assert.That(Render(), Is.True);
		}

		[Test]
		public void AlliedProviderInRangeShowsTheDot()
		{
			// The fix: an ally's live provider grants the dot exactly like an own one.
			Assert.That(CameoRangedGpsDotEffect.ProviderCountsForViewer(
				sameOwner: false, allied: true, providerDead: false), Is.True);
			Assert.That(Render(), Is.True);
		}

		[Test]
		public void EnemyProviderInRangeGrantsNothing()
		{
			Assert.That(CameoRangedGpsDotEffect.ProviderCountsForViewer(
				sameOwner: false, allied: false, providerDead: false), Is.False);
			Assert.That(Render(providerInRange: false), Is.False);
		}

		[Test]
		public void DeadProviderGrantsNothingWhetherOwnOrAllied()
		{
			Assert.That(CameoRangedGpsDotEffect.ProviderCountsForViewer(
				sameOwner: true, allied: false, providerDead: true), Is.False);
			Assert.That(CameoRangedGpsDotEffect.ProviderCountsForViewer(
				sameOwner: false, allied: true, providerDead: true), Is.False);
			Assert.That(Render(providerInRange: false), Is.False);
		}

		[Test]
		public void DisabledDotTraitHidesTheIndicator()
		{
			Assert.That(Render(traitDisabled: true), Is.False);
		}

		[Test]
		public void NoWatcherHidesTheIndicator()
		{
			Assert.That(Render(anyWatcherGranted: false), Is.False);
		}

		[Test]
		public void FrozenPortraitHidesTheIndicator()
		{
			Assert.That(Render(frozenPortraitVisible: true), Is.False);
		}

		[Test]
		public void AlliedLookingActorHidesTheIndicator()
		{
			Assert.That(Render(actorLooksAllied: true), Is.False);
		}

		[Test]
		public void VisibilityModifierDenialHidesTheIndicator()
		{
			Assert.That(Render(visibilityModifierDenies: true), Is.False);
		}

		[Test]
		public void UnexploredShroudHidesTheIndicator()
		{
			Assert.That(Render(shroudBlocks: true), Is.False);
		}

		[Test]
		public void AlreadyVisibleActorShowsNoDot()
		{
			Assert.That(Render(alreadyVisible: true), Is.False);
		}
	}
}
