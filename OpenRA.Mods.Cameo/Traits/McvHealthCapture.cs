using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Opt-in read-only MCV observations; unsupported order/hold/transform proof remains UNKNOWN. Do not mount by default.")]
	public sealed class McvHealthCaptureInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) => new McvHealthCapture();
	}

	public sealed class McvHealthCapture : IWorldLoaded, ITick, IGameOver, INotifyActorDisposing
	{
		sealed class Participant
		{
			internal OpenRA.Player Player;
			internal readonly McvHealthState State = new();
			internal readonly List<McvHealthObservation> Observations = new(McvHealthState.MaximumActors + 1);
			internal readonly Dictionary<string, (string Role, string Target)> Types = new(StringComparer.Ordinal);
			internal readonly Dictionary<string, string> RoleMap = new(StringComparer.Ordinal);
			internal IBotUnitLeases Leases;
			internal bool Supported;
			internal int Seq;
			internal int LastPulse = -1;
		}

		readonly Dictionary<OpenRA.Player, Participant> participants = [];
		AiEconomyHealthLogWriter writer;
		string uid;
		bool ended;
		bool playerOverflow;

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer renderer)
		{
			if (!AiMatchLogWriter.Eligible(world.Type, world.IsReplay, world.IsLoadingGameSave, Game.IsHost))
				return;
			uid = world.LobbyInfo.GlobalSettings.GameUid;
			try
			{
				var directory = Path.Combine(Platform.SupportDir, "Logs");
				Directory.CreateDirectory(directory);
				writer = new AiEconomyHealthLogWriter(Path.Combine(directory, "cameo-ai-mcv-health.jsonl"));
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Log.Write("debug", "MCV health capture unavailable: " + e.GetType().Name);
				return;
			}
			foreach (var player in world.Players)
			{
				if (!AiMatchLogWriter.IsLoggableBot(player)) continue;
				if (participants.Count >= 64) { playerOverflow = true; continue; }
				var p = new Participant { Player = player, Leases = BotUnitLeases.Of(player) };
				var infos = player.PlayerActor.Info.TraitInfos<McvExpansionManagerBotModuleInfo>().ToArray();
				var types = infos.SelectMany(i => i.McvTypes).Distinct(StringComparer.Ordinal).ToArray();
				p.Supported = infos.Length > 0 && types.Length > 0 && types.Length <= 128;
				foreach (var type in types.Take(128))
				{
					var role = "unknown";
					string target = null;
					if (world.Map.Rules.Actors.TryGetValue(type, out var actorInfo))
					{
						var transforms = actorInfo.TraitInfos<TransformsInfo>().ToArray();
						if (transforms.Length == 1)
						{
							target = transforms[0].IntoActor;
							if (target != null && world.Map.Rules.Actors.TryGetValue(target, out var into))
								role = McvHealthState.ResolveRole(true, infos.Any(i => i.ConstructionMcvTypes.Contains(type)),
									true, infos.Any(i => i.ConstructionYardTypes.Contains(target)),
									into.HasTraitInfo<RefineryInfo>(), into.HasTraitInfo<BuildingInfo>());
						}
					}
					p.Types[type] = (role, target);
					p.RoleMap[type] = role;
					p.Supported &= role != "unknown";
				}
				participants.Add(player, p);
			}
			Observe(world, "start");
		}

		void Observe(World world, string kind = null)
		{
			if (writer == null || ended) return;
			foreach (var p in participants.Values) p.Observations.Clear();
			// One world enumeration; facts are read only after selecting a participating owner.
			foreach (var actor in world.Actors)
			{
				if (!participants.TryGetValue(actor.Owner, out var p) || !p.Types.TryGetValue(actor.Info.Name, out var type)
					|| p.Observations.Count > McvHealthState.MaximumActors) continue;
				var activity = actor.IsIdle ? "idle" : actor.CurrentActivity is Transform ? "deploying"
					: actor.CurrentActivity is Move ? "mobile" : "busy";
				p.Observations.Add(new(actor.ActorID, actor.Info.Name, type.Role, type.Target,
					actor.Location.X, actor.Location.Y, !actor.IsDead && !actor.Disposed, actor.IsInWorld,
					activity, p.Leases?.LeaseOf(actor)?.Owner));
			}
			foreach (var p in participants.Values)
			{
				var changed = p.State.Observe(world.WorldTick, p.Observations);
				var pulse = world.WorldTick % 50 == 0 && p.LastPulse != world.WorldTick;
				if (kind == null && !changed && !pulse) continue;
				var rowKind = kind ?? (pulse ? "pulse" : "transition");
				if (pulse || kind == "start" || kind == "end") p.LastPulse = world.WorldTick;
				writer.TryWrite(McvHealthSchema.Row(uid, p.Player.InternalName, p.Seq++, world.WorldTick, rowKind,
					p.Player.WinState == WinState.Undefined, p.Supported,
					p.State.Complete && writer.Complete && !playerOverflow && !string.IsNullOrEmpty(uid),
					p.State.Snapshots, p.RoleMap));
			}
		}

		void ITick.Tick(Actor self) => Observe(self.World);
		void IGameOver.GameOver(World world)
		{
			if (ended) return;
			Observe(world, "end");
			ended = true;
			writer?.Dispose();
		}
		void INotifyActorDisposing.Disposing(Actor self)
		{
			ended = true;
			writer?.Dispose();
		}
	}
}
