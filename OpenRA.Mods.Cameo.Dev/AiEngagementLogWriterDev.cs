using System;
using System.Diagnostics;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Traits.BotModules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Development-only engagement file output.")]
	public sealed class AiEngagementLogWriterDevInfo : TraitInfo, Requires<AiEngagementLogWriterInfo>
	{
		public override object Create(ActorInitializer init) => new AiEngagementLogWriterDev(init.Self.Info.TraitInfo<AiEngagementLogWriterInfo>());
	}

	public sealed class AiEngagementLogWriterDev : IWorldLoaded, ITick
	{
		readonly AiEngagementLogWriterInfo info;
		readonly StringBuilder lines = new();
		AiLogFileAppender appender;
		bool active;
		bool written;
		bool flushing;
		int nextAttemptTick;
		TimeSpan enqueueElapsed;
		int enqueueChars;

		public AiEngagementLogWriterDev(AiEngagementLogWriterInfo info) { this.info = info; }

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			active = world.WorldActor.Trait<CameoLogHub>().Sink.Enabled &&
				AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost);
			if (active)
			{
				appender = new AiLogFileAppender(info.FileName, nameof(AiEngagementLogWriterDev));
				foreach (var player in world.Players)
					player.PlayerActor.TraitOrDefault<EngagementLogBotModule>()?.PrewarmBalanceMetadataForDevelopmentLog();
			}
		}

		public void Append(string line)
		{
			if (active && !written)
				lines.Append(line);
		}

		public void End(int tick)
		{
			if (written || !active || flushing)
				return;
			flushing = true;
			var whole = Stopwatch.StartNew();
			enqueueElapsed = TimeSpan.Zero;
			enqueueChars = 0;
			TryAppend(tick);
			AiGameOverProbe.Report(nameof(AiEngagementLogWriterDev), whole.Elapsed, enqueueElapsed, enqueueChars);
		}

		void ITick.Tick(Actor self)
		{
			if (!written && flushing && self.World.WorldTick >= nextAttemptTick)
				TryAppend(self.World.WorldTick);
		}

		void TryAppend(int tick)
		{
			if (lines.Length == 0)
			{
				written = true;
				return;
			}
			// The engagement batch is ended once (EngagementEnd, end of game), so every append here is a final record.
			var text = lines.ToString();
			var enqueue = Stopwatch.StartNew();
			var result = appender.TryAppend(text, tick, out nextAttemptTick, finalRecord: true);
			enqueueElapsed += enqueue.Elapsed;
			enqueueChars = text.Length;
			if (result != AiLogAppendResult.RetryableFailure || appender.IsTerminal)
				written = true;
		}
	}
}
