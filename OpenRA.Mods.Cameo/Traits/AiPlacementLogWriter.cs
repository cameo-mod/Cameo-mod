#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Appends the record-only AI placement / build-order log (AI_ARCHITECTURE 12.24 FE-0, 12.25 BO-0): one line per building a bot places.")]
	public class AiPlacementLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-placements.jsonl";

		public override object Create(ActorInitializer init) { return new AiPlacementLogWriter(this); }
	}

	/// <summary>
	/// REF-1 telemetry follow-up (lead ruling A, 2026-10-04): the diff verdict for one tracked refinery
	/// between scans. An owner change is a capture — a loss for the old owner and an acquisition for the
	/// new one; leaving the world is a loss whose cause is the death flag (a sold actor is disposed
	/// alive). Pure so the cause mapping is unit-tested.
	/// </summary>
	internal readonly struct RefineryTransition
	{
		public readonly string LostCause;
		public readonly bool Acquired;

		public RefineryTransition(string lostCause, bool acquired)
		{
			LostCause = lostCause;
			Acquired = acquired;
		}
	}

	internal static class RefineryLifecycle
	{
		public static RefineryTransition Classify(bool ownerChanged, bool inWorld, bool isDead)
		{
			if (inWorld)
				return ownerChanged ? new RefineryTransition("captured", true) : new RefineryTransition(null, false);

			return new RefineryTransition(isDead ? "killed" : "sold", false);
		}
	}

	// The world-side refinery set the tracker diffs — owner and the last seen location, with the
	// actor kept so the death flag and the definition are still readable after it leaves the world.
	// File scope (not nested): the module-map generator splits a class's body at the next `class`
	// keyword, and a nested class would truncate the writer's scanned trait lookups.

	// Release rules keep configuration; the observer lives in the DEV assembly.
	public sealed class AiPlacementLogWriter
	{
		public AiPlacementLogWriter(AiPlacementLogWriterInfo info) { }
	}
}
