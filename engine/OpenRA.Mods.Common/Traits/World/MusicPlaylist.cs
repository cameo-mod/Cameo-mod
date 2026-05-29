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
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Trait for music handling. Attach this to the world actor.")]
	public class MusicPlaylistInfo : TraitInfo
	{
		[Desc("Music to play when the map starts.", "Plays the first song on the playlist when undefined.")]
		public readonly string StartingMusic = null;

		[Desc("Music to play when the game has been won.")]
		public readonly string VictoryMusic = null;

		[Desc("Music to play when the game has been lost.")]
		public readonly string DefeatMusic = null;

		[Desc("This track is played when no other music is playing.",
			"It cannot be paused, but can be overridden by selecting a new track.")]
		public readonly string BackgroundMusic = null;

		[Desc("Allow the background music to be muted by the player.")]
		public readonly bool AllowMuteBackgroundMusic = false;

		[Desc("Disable all world sounds (combat etc).")]
		public readonly bool DisableWorldSounds = false;

		public override object Create(ActorInitializer init) { return new MusicPlaylist(init.World, this); }
	}

	public class MusicPlaylist : INotifyActorDisposing, IGameOver, IPostWorldLoaded, INotifyGameLoaded
	{
		readonly MusicPlaylistInfo info;
		readonly World world;

		readonly MusicInfo[] random;
		readonly MusicInfo[] playlist;

		public readonly bool IsMusicInstalled;
		public readonly bool IsMusicAvailable;
		public readonly bool AllowMuteBackgroundMusic;

		public bool HasSongsForCurrentMode => FilterSongs(playlist).Length > 0;
		public bool IsBackgroundMusicMuted => AllowMuteBackgroundMusic && Game.Settings.Sound.MuteBackgroundMusic;

		public bool CurrentSongIsBackground { get; private set; }

		MusicInfo currentSong;
		MusicInfo currentBackgroundSong;

		public MusicPlaylist(World world, MusicPlaylistInfo info)
		{
			this.info = info;
			this.world = world;

			IsMusicInstalled = world.Map.Rules.InstalledMusic.Any();
			if (!IsMusicInstalled)
				return;

			playlist = world.Map.Rules.InstalledMusic
				.Where(a => !a.Value.Hidden)
				.Select(a => a.Value)
				.ToArray();

			random = playlist.Shuffle(Game.CosmeticRandom).ToArray();
			IsMusicAvailable = playlist.Length > 0;
			AllowMuteBackgroundMusic = info.AllowMuteBackgroundMusic;

			if (SongExists(info.BackgroundMusic))
			{
				currentSong = currentBackgroundSong = world.Map.Rules.Music[info.BackgroundMusic];
				CurrentSongIsBackground = true;
			}
			else
			{
				// Start playback with a random song
				currentSong = random.FirstOrDefault();
			}

			if (SongExists(info.StartingMusic))
			{
				currentSong = world.Map.Rules.Music[info.StartingMusic];
				CurrentSongIsBackground = false;
			}
			EnsureCurrentSongMatchesMode();
		}

		void IPostWorldLoaded.PostWorldLoaded(World world, WorldRenderer wr)
		{
			// Reset any bogus pre-existing state
			Game.Sound.DisableWorldSounds = info.DisableWorldSounds;

			if (!world.IsLoadingGameSave)
			{
				// If a track was already playing (e.g. selected in the menu or lobby)
				// and it is valid for the current mode, keep it rather than jumping
				// to a random pick from the constructor.
				var alreadyPlaying = Game.Sound.CurrentMusic;
				if (alreadyPlaying != null && !CurrentSongIsBackground)
				{
					var available = FilterSongsForPlaybackOrder();
					if (available.Contains(alreadyPlaying))
						currentSong = alreadyPlaying;
				}

				Play();
			}
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
			Play();
		}

		bool SongExists(string song)
		{
			return !string.IsNullOrEmpty(song)
				&& world.Map.Rules.Music.ContainsKey(song)
				&& world.Map.Rules.Music[song].Exists;
		}

		static bool SongExists(MusicInfo song)
		{
			return song != null && song.Exists;
		}

		public MusicInfo CurrentSong()
		{
			EnsureCurrentSongMatchesMode();
			return currentSong;
		}

		public MusicInfo[] AvailablePlaylist()
		{
			return FilterSongs(playlist);
		}

		void IGameOver.GameOver(World world)
		{
			if (world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Won)
			{
				if (SongExists(info.VictoryMusic))
				{
					currentBackgroundSong = world.Map.Rules.Music[info.VictoryMusic];
					Stop();
				}
			}
			else
			{
				// Most RTS treats observers losing the game,
				// no need for a special handling involving them here.
				if (SongExists(info.DefeatMusic))
				{
					currentBackgroundSong = world.Map.Rules.Music[info.DefeatMusic];
					Stop();
				}
			}
		}

		void Play()
		{
			EnsureCurrentSongMatchesMode();

			if (currentSong == null || !SongExists(currentSong) || (CurrentSongIsBackground && IsBackgroundMusicMuted))
				return;

			Game.Sound.PlayMusicThen(currentSong, PlayNextSong);
		}

		void PlayNextSong()
		{
			if (!CurrentSongIsBackground)
				currentSong = GetNextSong();

			Play();
		}

		public void Play(MusicInfo music)
		{
			if (music == null)
				return;

			currentSong = music;
			CurrentSongIsBackground = false;

			Play();
		}

		public void Play(MusicInfo music, Action onComplete)
		{
			if (music == null)
				return;

			currentSong = music;
			CurrentSongIsBackground = false;
			Game.Sound.PlayMusicThen(music, onComplete);
		}

		public void SetBackgroundMusic(MusicInfo music)
		{
			currentBackgroundSong = music;

			if (CurrentSongIsBackground)
				Stop();
		}

		public MusicInfo GetNextSong()
		{
			return GetSong(false);
		}

		public MusicInfo GetPrevSong()
		{
			return GetSong(true);
		}

		MusicInfo GetSong(bool reverse)
		{
			if (!IsMusicAvailable)
				return null;

			var songs = FilterSongsForPlaybackOrder();
			if (songs.Length == 0)
				return null;

			if (currentSong == null || !songs.Contains(currentSong))
				return reverse ? songs.LastOrDefault() : songs.FirstOrDefault();

			var sequence = reverse ? songs.Reverse() : songs.AsEnumerable();
			var next = sequence.SkipWhile(m => m != currentSong).Skip(1).FirstOrDefault();
			if (next == null)
				next = reverse ? songs.LastOrDefault() : songs.FirstOrDefault();

			return SongExists(next) ? next : null;
		}


		public void RefreshForPlaybackModeChange(bool resumePlayback)
		{
			if (!IsMusicAvailable || CurrentSongIsBackground)
				return;

			var ordered = FilterSongsForPlaybackOrder();
			if (ordered.Length == 0)
			{
				currentSong = null;
				Game.Sound.StopMusic();
				return;
			}

			if (currentSong == null || !ordered.Contains(currentSong))
			{
				currentSong = ordered.First();
				CurrentSongIsBackground = false;
			}

			if (resumePlayback && currentSong != null)
				Play();
		}

		void EnsureCurrentSongMatchesMode()
		{
			if (!IsMusicAvailable || CurrentSongIsBackground)
				return;

			var ordered = FilterSongsForPlaybackOrder();
			if (ordered.Length == 0)
			{
				currentSong = null;
				return;
			}

			if (currentSong == null || !ordered.Contains(currentSong))
			{
				currentSong = ordered.First();
				CurrentSongIsBackground = false;
			}
		}

		MusicInfo[] FilterSongsForPlaybackOrder()
		{
			return FilterSongs(Game.Settings.Sound.Shuffle ? random : playlist);
		}

		MusicInfo[] FilterSongs(IEnumerable<MusicInfo> songs)
		{
			var list = songs as IList<MusicInfo> ?? songs.ToList();
			if (list.Count == 0)
				return Array.Empty<MusicInfo>();

			switch (Game.Settings.Sound.MusicMode)
			{
				case MusicPlaybackMode.MixAll:
					return list.ToArray();
				case MusicPlaybackMode.OnlyOldschool:
					return list.Where(s => s.IsOldschool).ToArray();
				case MusicPlaybackMode.FactionSpecific:
					return FilterFactionSongs(list, DetermineFactionCategory());
				case MusicPlaybackMode.Custom:
					return FilterCustomSongs(list);
				case MusicPlaybackMode.Playlist:
					return FilterPlaylistSongs();
				default:
					return list.ToArray();
			}
		}

		MusicInfo[] FilterCustomSongs(IList<MusicInfo> songs)
		{
			var filters = Game.Settings.Sound.CustomMusicCategories ?? Array.Empty<string>();
			var normalized = filters
				.Select(f => MusicCategories.Normalize(f, null))
				.Where(f => !string.IsNullOrEmpty(f))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			if (normalized.Length == 0)
				return Array.Empty<MusicInfo>();

			return songs.Where(s => MatchesCustomCategory(s, normalized)).ToArray();
		}

		// Returns tracks in the order the playlist author defined them,
		// skipping any that aren't installed or are marked Hidden.
		MusicInfo[] FilterPlaylistSongs()
		{
			var tracks = Game.Settings.Sound.UserPlaylistTracks ?? Array.Empty<string>();

			// No playlist defined — fall back to full library respecting shuffle
			if (tracks.Length == 0)
				return Game.Settings.Sound.Shuffle ? random : playlist;

			var rules = world.Map.Rules;
			var result = tracks
				.Where(t => rules.Music.TryGetValue(t, out var info) && info.Exists && !info.Hidden)
				.Select(t => rules.Music[t])
				.ToArray();

			// All saved tracks missing — fall back to full library
			if (result.Length == 0)
				return Game.Settings.Sound.Shuffle ? random : playlist;

			// Apply shuffle to the user's playlist
			return Game.Settings.Sound.Shuffle
				? result.Shuffle(Game.CosmeticRandom).ToArray()
				: result;
		}

		bool MatchesCustomCategory(MusicInfo song, IReadOnlyCollection<string> normalizedCategories)
		{
			foreach (var category in normalizedCategories)
			{
				switch (category)
				{
					case MusicCategories.Generic:
						if (song.IsGeneric)
							return true;
						break;
					case MusicCategories.Oldschool:
						if (song.IsOldschool)
							return true;
						break;
					default:
						if (song.MatchesCategory(category))
							return true;
						break;
				}
			}

			return false;
		}

		MusicInfo[] FilterFactionSongs(IList<MusicInfo> songs, string category)
		{
			if (songs.Count == 0)
				return Array.Empty<MusicInfo>();

			if (string.IsNullOrEmpty(category))
				return songs.ToArray();

			if (category == MusicCategories.Generic)
			{
				var genericSongs = songs.Where(s => s.IsGeneric).ToArray();
				if (genericSongs.Length > 0)
					return genericSongs;

				var fallback = songs.Where(s => s.IsOldschool).ToArray();
				return fallback.Length > 0 ? fallback : songs.ToArray();
			}

			var matched = songs.Where(s => s.MatchesCategory(category)).ToArray();
			if (matched.Length > 0)
				return matched;

			return Array.Empty<MusicInfo>();
		}

		string DetermineFactionCategory()
		{
			if (Game.Settings.Sound.MusicMode != MusicPlaybackMode.FactionSpecific)
				return null;

			if (world?.LocalPlayer == null || !IsActivePlayer())
				return MusicCategories.Generic;

			var faction = world.LocalPlayer.Faction;
			if (faction == null)
				return MusicCategories.Generic;

			if (!string.IsNullOrEmpty(faction.Side))
				return MusicCategories.Normalize(faction.Side, MusicCategories.Generic);

			if (!string.IsNullOrEmpty(faction.InternalName))
				return MusicCategories.Normalize(faction.InternalName, MusicCategories.Generic);

			return MusicCategories.Generic;
		}

		bool IsActivePlayer()
		{
			var player = world.LocalPlayer;
			if (player == null)
				return false;

			if (world.IsReplay || player.NonCombatant || player.Spectating)
				return false;

			return true;
		}

		public void Stop()
		{
			currentSong = null;
			Game.Sound.StopMusic();

			if (currentBackgroundSong != null)
			{
				currentSong = currentBackgroundSong;
				CurrentSongIsBackground = true;

				if (!IsBackgroundMusicMuted)
					Play();
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (currentSong != null)
				Game.Sound.StopMusic();

			Game.Sound.DisableWorldSounds = false;
		}
	}
}
