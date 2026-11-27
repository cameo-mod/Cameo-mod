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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>The knob names a learned estimate is stored under. One name = one owner (the consumer that reads it).</summary>
	public static class LearnAllKnobs
	{
		/// <summary>The wave-mix role value (ARMY-MIX-01): how much a missing role is worth in the current match-up. Read through IBotMixLearning.TryRoleValues.</summary>
		public static string WaveRole(string role) => "wave_role." + role;
	}

	[TraitLocation(SystemActors.Player)]
	[Desc("LEARN-ALL-02 P1b: the release-side reader of the accepted learned snapshot (LearnedFile). It provides IBotMixLearning from the four learned axes",
		"(global, own faction, enemy faction, observed style) of the bot's own personality, falling back to the global table, and reads the file once at match start.",
		"It never fits, never writes and never changes a decision on its own: a role it does not know is neutral. Off (the default) = no provider = the classic path.")]
	public class LearnAllBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Mod-relative path of the accepted snapshot written by tools/ai/fit_learn_all.py. A bare path resolves inside the cameo package; 'package|path' is honoured.")]
		public readonly string LearnedFile = "ai/learned/learn_all.yaml";

		[Desc("Ticks between refreshes of the enemy faction list (player-level information only).")]
		public readonly int RefreshTicks = 250;

		public override object Create(ActorInitializer init) { return new LearnAllBotModule(init.Self, this); }
	}

	public class LearnAllBotModule : ConditionalTrait<LearnAllBotModuleInfo>, IBotMixLearning, IBotLearnedKnobs
	{
		readonly World world;
		readonly OpenRA.Player player;
		IBotCurrentPersonality[] personalityProviders;
		IBotObservedMainTargetStyle[] styleProviders;
		IBotMainTargetProvider[] mainTargetProviders;
		string lastMainTarget = "";
		string lastMainStyle;
		LearnTable table;
		string[] enemies = Array.Empty<string>();
		string ownFaction = "";
		int refreshedAt = int.MinValue;
		AblationLatch ablateLearnAll;

		public LearnAllBotModule(Actor self, LearnAllBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		bool IBotMixLearning.TryRoleValues(string role, out MixRoleValues values)
		{
			values = default;
			if (IsTraitDisabled || role == null || ablateLearnAll.Off(player, AblationIds.LearnAll))
				return false;

			table ??= Load();
			Refresh();
			var personality = Personality();
			var axes = table.Find(personality, LearnAllKnobs.WaveRole(role));
			if (axes == null)
				return false;

			var (mainTarget, mainStyle) = ReadMainTarget();
			var enemyEff = axes.EnemyEff(enemies, mainTarget);

			// S_obs: the observed style of the main target owner when a classifier provides one and the table has learned it; unseen, unknown or no provider = 0 = the exact
			// three-axis result (null never fabricates a style, and nothing here reads an enemy's personality or true label).
			var styleValue = mainStyle != null && axes.Style.TryGetValue(mainStyle, out var learnedStyle) ? learnedStyle / 1000.0 : 0.0;
			values = new MixRoleValues(axes.Global / 1000.0, axes.Own(ownFaction) / 1000.0, enemyEff / 1000.0, styleValue);
			return true;
		}

		/// <summary>
		/// The generic read seam (IBotLearnedKnobs): the decision value of any stored knob in this match-up, in per-mille. False when the module is off or ablated, the knob is empty or
		/// not in the snapshot: the consumer then uses its prior. The unseen observed style (B4) resolves as the exact three-axis result.
		/// </summary>
		bool IBotLearnedKnobs.TryResolvePermille(string knob, out int permille)
		{
			permille = LearnAllMath.One;
			if (IsTraitDisabled || string.IsNullOrEmpty(knob) || ablateLearnAll.Off(player, AblationIds.LearnAll))
				return false;

			table ??= Load();
			Refresh();
			var personality = Personality();
			var (mainTarget, mainStyle) = ReadMainTarget();
			return TryResolve(table, personality, knob, ownFaction, enemies, mainTarget, mainStyle, out permille);
		}

		/// <summary>
		/// The seat's learning context for the DEV exposure writer: the same player-level facts the decision path reads (own faction, one public faction per enemy PLAYER, the authoritative main target
		/// and its observed style, the current personality). Works whether or not the module is enabled (training is independent of the read switch). The main target is the LAST authoritative one
		/// of the game (it may have died by the end); "" = never had one. Never an enemy's personality or true faction.
		/// </summary>
		public LearnSeatContext SeatContext()
		{
			// Called by the DEV writer at game over only: the match is decided, so refreshing the decision cache here cannot change any decision.
			ObserveMainTarget();
			Refresh();
			return new LearnSeatContext(ownFaction, enemies, lastMainTarget, lastMainStyle, Personality());
		}

		/// <summary>
		/// Record-only look at the authoritative main target and its observed style for the DEV writer's tick. It reads the live providers through ReadMainTarget (the same read the decision
		/// path makes, which caches nothing) and touches NONE of the decision-path state (no refresh phase, no cached faction or enemies), so a game with the writer on decides exactly as one with it off.
		/// </summary>
		public void ObserveMainTarget()
		{
			var (target, style) = ReadMainTarget();
			if (target.Length == 0)
				return;

			lastMainTarget = target;
			lastMainStyle = style;
		}

		(string Faction, string Style) ReadMainTarget()
		{
			mainTargetProviders ??= player.PlayerActor.TraitsImplementing<IBotMainTargetProvider>().ToArray();
			styleProviders ??= player.PlayerActor.TraitsImplementing<IBotObservedMainTargetStyle>().ToArray();
			var styleProvider = styleProviders.FirstEnabledTraitOrDefault();
			return MainTargetContext(mainTargetProviders, BotFactionView.PublicFactionOf, styleProvider);
		}

		internal static (string Faction, string Style) MainTargetContext(IBotMainTargetProvider[] providers,
			Func<OpenRA.Player, string> publicFaction, IBotObservedMainTargetStyle styleProvider)
		{
			foreach (var provider in providers)
				if (provider.MainTarget is OpenRA.Player target)
					return (publicFaction(target) ?? "", styleProvider?.ObservedStyleOf(target));

			return ("", null);
		}

		// The providers are resolved once (the player actor's traits never change): the reads run per knob per decision, so no enumeration allocates per call (Workhorse F3).
		string Personality() => (personalityProviders ??= player.PlayerActor.TraitsImplementing<IBotCurrentPersonality>().ToArray()).FirstEnabledTraitOrDefault()?.CurrentPersonality;

		/// <summary>The pure half of the read seam: false for an empty table argument or a knob the table does not hold, else exactly LearnTable.Resolve (style unseen).</summary>
		public static bool TryResolve(LearnTable learned, string personality, string knob, string ownFaction, IReadOnlyList<string> enemyFactions, string mainTargetFaction, out int permille) =>
			TryResolve(learned, personality, knob, ownFaction, enemyFactions, mainTargetFaction, null, out permille);

		/// <summary>The same with the observed-style bucket of the main target owner: null or a bucket the table never learned = unseen = the exact three-axis result.</summary>
		public static bool TryResolve(LearnTable learned, string personality, string knob, string ownFaction, IReadOnlyList<string> enemyFactions, string mainTargetFaction, string observedStyle, out int permille)
		{
			permille = LearnAllMath.One;
			if (learned == null || string.IsNullOrEmpty(knob) || learned.Find(personality, knob) == null)
				return false;

			permille = learned.Resolve(personality, knob, ownFaction, enemyFactions, mainTargetFaction, observedStyle);
			return true;
		}

		LearnTable Load()
		{
			var fs = Game.ModData.DefaultFileSystem;
			var path = LearnedFilePath.Resolve(Info.LearnedFile);
			if (!fs.Exists(path))
				return new LearnTable();

			using (var stream = fs.Open(path))
				return LearnAllSnapshot.Parse(MiniYaml.FromStream(stream, Info.LearnedFile));
		}

		// Player-level information only: the lobby (public) factions of the enemy players and of the main target, never actors.
		void Refresh()
		{
			var tick = world.WorldTick;
			if (refreshedAt != int.MinValue && tick - refreshedAt < Math.Max(1, Info.RefreshTicks))
				return;

			refreshedAt = tick;
			ownFaction = player.Faction?.InternalName ?? "";
			enemies = world.Players
				.Where(p => p != player && !p.NonCombatant && !p.Spectating && player.RelationshipWith(p) == PlayerRelationship.Enemy)
				.Select(BotFactionView.PublicFactionOf)
				.ToArray();
		}
	}
}


namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>The player-level context of one bot seat that a learning exposure record carries. Factions are public names ("" = hidden or unpicked); no actor, no enemy personality.</summary>
	public readonly struct LearnSeatContext
	{
		public readonly string OwnFaction;
		public readonly IReadOnlyList<string> Enemies;
		public readonly string MainTarget;
		public readonly string MainTargetStyle;
		public readonly string Personality;

		public LearnSeatContext(string ownFaction, IReadOnlyList<string> enemies, string mainTarget, string mainTargetStyle, string personality)
		{
			OwnFaction = ownFaction;
			Enemies = enemies;
			MainTarget = mainTarget;
			MainTargetStyle = mainTargetStyle;
			Personality = personality;
		}
	}
}
