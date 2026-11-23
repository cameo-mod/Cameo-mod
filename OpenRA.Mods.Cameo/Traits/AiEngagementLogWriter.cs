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
using System.Text;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Appends the record-only AI engagement log (AI_ARCHITECTURE 12.30 EL-0, DESIGN 19.13): one line per closed fight and one",
		"per army posture sample.")]
	public class AiEngagementLogWriterInfo : TraitInfo
	{
		public readonly string FileName = "cameo-ai-engagements.jsonl";

		public override object Create(ActorInitializer init) { return new AiEngagementLogWriter(this); }
	}

	// Resident engagement coordinator: Active and the match-end callbacks feed score state.
	public class AiEngagementLogWriter : IWorldLoaded, IGameOver
	{
		readonly List<Action<int>> flushers = new();
		OpenRA.Mods.CA.Traits.ICameoLogSink sink;
		bool ended;
		public bool Active { get; private set; }
		public string FallbackGameUid { get; private set; }
		public AiEngagementLogWriter(AiEngagementLogWriterInfo info) { }
		public void RegisterFlush(Action<int> flush) => flushers.Add(flush);
		public void Append(string line)
		{
			if (Active && !ended && sink?.Enabled == true)
				sink.EngagementLine(line);
		}
		void IWorldLoaded.WorldLoaded(World world, WorldRenderer worldRenderer)
		{
			Active = AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost);
			if (Active)
				FallbackGameUid = Guid.NewGuid().ToString("N");
			sink = world.WorldActor.TraitOrDefault<OpenRA.Mods.CA.Traits.CameoLogHub>()?.Sink;
		}
		void IGameOver.GameOver(World world)
		{
			if (ended || !Active)
				return;
			foreach (var flush in flushers)
				flush(world.WorldTick);
			ended = true;
			if (sink?.Enabled == true)
				sink.EngagementEnd(world.WorldTick);
		}
	}
}