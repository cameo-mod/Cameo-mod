#region Copyright & License Information
/*
 * Copyright (c) OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("MC1 (docs/design/AI_MISSION_CARDS.md): appends every bot mission-card transition to a record-only JSONL",
		"archive, one line per transition, schema `mission-card/1`. The game never reads it back (DESIGN §21.1).",
		"Host only, regular games only, like the match and situation logs.")]
	public class AiMissionLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-missions.jsonl";

		[Desc("Ticks between flushes, so a match killed by the batch harness keeps most of its lines.")]
		public readonly int FlushIntervalTicks = 1500;

		[Desc("Most lines kept in memory between flushes; further lines are counted and dropped.")]
		public readonly int MaxPendingLines = 5000;

		public override object Create(ActorInitializer init) { return new AiMissionLogWriter(this); }
	}

	// The release rules retain this configuration; logging lives in the DEV assembly.
	public sealed class AiMissionLogWriter
	{
		public AiMissionLogWriter(AiMissionLogWriterInfo info) { }
	}
}
