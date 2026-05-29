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
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MusicPlayerLogic : ChromeLogic
	{
		// ── Fluent keys ────────────────────────────────────────────────────
		[FluentReference] const string SoundMuted = "label-sound-muted";
		[FluentReference] const string NoSongPlaying = "label-no-song-playing";
		[FluentReference] const string SavePresetTitle = "dialog-save-preset.title";
		[FluentReference] const string SavePresetPrompt = "dialog-save-preset.prompt";
		[FluentReference] const string SavePresetConfirm = "dialog-save-preset.confirm";
		[FluentReference] const string OverwritePresetTitle = "dialog-overwrite-preset.title";
		[FluentReference] const string OverwritePresetPrompt = "dialog-overwrite-preset.prompt";
		[FluentReference] const string OverwritePresetConfirm = "dialog-overwrite-preset.confirm";
		[FluentReference] const string DeletePresetTitle = "dialog-delete-preset.title";
		[FluentReference] const string DeletePresetPrompt = "dialog-delete-preset.prompt";
		[FluentReference] const string DeletePresetConfirm = "dialog-delete-preset.confirm";
		[FluentReference] const string LoadPresetDefault = "button-load-preset";

		// ── Panel resize constants ─────────────────────────────────────────
		const int PlaylistPanelWidth = 780;
		const int PlaylistPanelHeight = 700;

		// ── Core ───────────────────────────────────────────────────────────
		readonly Widget panel;
		readonly ModData modData;
		readonly World world;
		readonly MusicPlaylist musicPlaylist;
		MusicInfo currentSong = null;
		bool noSongsForMode;

		// ── Single-panel widgets ───────────────────────────────────────────
		readonly ScrollPanelWidget musicList;
		readonly ScrollItemWidget itemTemplate;
		readonly Widget customFilterPanel;
		readonly Widget labelContainer;
		readonly Widget buttonsPanel;
		readonly LabelWidget timeLabelWidget;
		readonly LabelWidget titleLabelWidget;
		readonly CheckboxWidget shuffleWidget;
		readonly CheckboxWidget repeatWidget;
		readonly Widget noMusicContainer;
		readonly LabelWidget muteLabelWidget;
		readonly Widget modePanel;
		readonly ButtonWidget backButtonWidget;
		readonly LabelWidget noSongsLabelWidget;

		// ── Custom filter layout shift ─────────────────────────────────────
		readonly List<(Widget Widget, int BaseY, int BaseHeight, bool StretchHeight, bool ShiftWithCustom)> layoutShiftTargets = new();
		readonly List<(Widget Widget, int BaseY)> customRaiseTargets = new();
		readonly List<(Widget Widget, int BaseY)> customControlTargets = new();

		const int CustomDownOffset = 0;
		const int CustomUpOffset = 0;
		const int CustomLowerOffset = 0;
		const int CustomControlDownOffset = 130;

		readonly bool allowPanelResize;
		readonly int panelCenterY;
		readonly int basePanelHeight;
		readonly int basePanelWidth;
		int baseRepeatY;
		int baseTitleY;
		int baseTimeY;
		readonly int expandedPanelHeight;
		readonly int collapsedPanelHeight;
		readonly int customLayoutOffset;
		bool showCustomFilters;

		// ── Playlist panel shift ───────────────────────────────────────────
		// Widgets that shift down when panel expands to Playlist height.
		// Only top-level widgets — NOT children of other shifted widgets.
		readonly List<(Widget Widget, int BaseY, int BaseX)> playlistShiftTargets = new();
		bool showPlaylistLayout;

		static readonly StringComparer CustomCategoryComparer = StringComparer.OrdinalIgnoreCase;

		static readonly (string CheckboxId, string Category)[] CustomFilterOptions =
		[
			("CUSTOM_FILTER_ALLIES", "allies"),
			("CUSTOM_FILTER_SOVIET", "soviet"),
			("CUSTOM_FILTER_CHINA", "china"),
			("CUSTOM_FILTER_GDI", "gdi"),
			("CUSTOM_FILTER_NOD", "nod"),
			("CUSTOM_FILTER_SCRIN", "scrin"),
			("CUSTOM_FILTER_GENERIC", MusicCategories.Generic),
			("CUSTOM_FILTER_OLDSCHOOL", MusicCategories.Oldschool)
		];

		// ── Dual-panel widgets ─────────────────────────────────────────────
		readonly Widget dualPanelContent;
		readonly Widget presetRow;
		readonly ScrollPanelWidget availableList;
		readonly ScrollItemWidget availableTemplate;
		readonly ScrollPanelWidget playlistTrackList;
		readonly ScrollItemWidget playlistTrackTemplate;
		readonly LabelWidget availableHeaderWidget;
		readonly LabelWidget playlistHeaderWidget;

		MusicInfo selectedAvailableTrack;
		MusicInfo selectedPlaylistTrack;
		readonly List<string> userPlaylistKeys;
		readonly MusicInfo[] allInstalledTracks;

		// ── Preset storage ─────────────────────────────────────────────────
		static readonly string UserPresetsFilename = "music-presets.yaml";
		string UserPresetsPath => Path.Combine(Platform.SupportDir, UserPresetsFilename);
		readonly Dictionary<string, PlaylistDefinition> userPresets;
		string currentPresetKey;

		[ObjectCreator.UseCtor]
		public MusicPlayerLogic(Widget widget, World world, ModData modData, Action onExit)
		{
			panel = widget;
			this.world = world;
			this.modData = modData;
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			// ── Dual-panel + preset init ──────────────────────────────────
			allInstalledTracks = world.Map.Rules.InstalledMusic
				.Where(m => !m.Value.Hidden)
				.Select(m => m.Value)
				.OrderBy(m => m.Title)
				.ToArray();

			userPlaylistKeys = new List<string>(
				Game.Settings.Sound.UserPlaylistTracks ?? Array.Empty<string>());

			// Auto-switch to Playlist mode if the user has a real custom playlist
			var hasCustomPlaylist = userPlaylistKeys.Count > 0 &&
				!(userPlaylistKeys.Count == 1 && userPlaylistKeys[0] == "menu");
			if (hasCustomPlaylist && Game.Settings.Sound.MusicMode == MusicPlaybackMode.MixAll)
				Game.Settings.Sound.MusicMode = MusicPlaybackMode.Playlist;

			userPresets = LoadUserPresets();

			dualPanelContent = panel.GetOrNull<Widget>("DUAL_PANEL_CONTENT");
			presetRow = panel.GetOrNull<Widget>("PRESET_ROW");
			availableList = panel.GetOrNull<ScrollPanelWidget>("AVAILABLE_LIST");
			availableTemplate = availableList?.Get<ScrollItemWidget>("AVAILABLE_TEMPLATE");
			playlistTrackList = panel.GetOrNull<ScrollPanelWidget>("PLAYLIST_TRACK_LIST");
			playlistTrackTemplate = playlistTrackList?.Get<ScrollItemWidget>("PLAYLIST_TRACK_TEMPLATE");
			availableHeaderWidget = panel.GetOrNull<LabelWidget>("AVAILABLE_HEADER");
			playlistHeaderWidget = panel.GetOrNull<LabelWidget>("PLAYLIST_TRACK_HEADER");

			// ── Single-panel init ─────────────────────────────────────────
			musicList = panel.Get<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = musicList.Get<ScrollItemWidget>("MUSIC_TEMPLATE");
			customFilterPanel = panel.GetOrNull<Widget>("CUSTOM_FILTER_PANEL");
			labelContainer = panel.GetOrNull<Widget>("LABEL_CONTAINER");
			buttonsPanel = panel.GetOrNull<Widget>("BUTTONS");
			timeLabelWidget = panel.GetOrNull<LabelWidget>("TIME_LABEL");
			titleLabelWidget = panel.GetOrNull<LabelWidget>("TITLE_LABEL");
			shuffleWidget = panel.Get<CheckboxWidget>("SHUFFLE");
			repeatWidget = panel.Get<CheckboxWidget>("REPEAT");
			noMusicContainer = panel.GetOrNull<Widget>("NO_MUSIC_LABEL");
			muteLabelWidget = panel.GetOrNull<LabelWidget>("MUTE_LABEL");
			modePanel = panel.GetOrNull<Widget>("MODE_PANEL");
			backButtonWidget = panel.GetOrNull<ButtonWidget>("BACK_BUTTON");
			noSongsLabelWidget = panel.GetOrNull<LabelWidget>("NO_MATCHING_SONGS");

			basePanelHeight = panel.Bounds.Height;
			basePanelWidth = panel.Bounds.Width;
			baseRepeatY = repeatWidget?.Bounds.Y ?? 0;
			baseTitleY = titleLabelWidget?.Bounds.Y ?? 0;
			baseTimeY = timeLabelWidget?.Bounds.Y ?? 0;
			panelCenterY = panel.Bounds.Y + basePanelHeight / 2;
			allowPanelResize = panel.Id == "MUSIC_PANEL";

			customLayoutOffset = customFilterPanel != null
				? customFilterPanel.Bounds.Height + CustomDownOffset + CustomLowerOffset : 0;
			expandedPanelHeight = basePanelHeight + (customFilterPanel != null ? customLayoutOffset : 0);
			collapsedPanelHeight = basePanelHeight;

			// ── Visibility ────────────────────────────────────────────────
			bool IsPlaylistMode() => Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist;

			if (customFilterPanel != null)
				customFilterPanel.IsVisible = () => showCustomFilters && !IsPlaylistMode();

			if (presetRow != null)
				presetRow.IsVisible = IsPlaylistMode;

			if (dualPanelContent != null)
				dualPanelContent.IsVisible = IsPlaylistMode;

			// Hide the "Playlist Mode" label when not in Playlist mode
			if (modePanel != null)
			{
				var modeLabel = modePanel.GetOrNull<LabelWidget>("MODE_LABEL");
				if (modeLabel != null)
					modeLabel.IsVisible = IsPlaylistMode;
			}

			if (labelContainer != null)
			{
				var orig = labelContainer.IsVisible;
				labelContainer.IsVisible = () => !IsPlaylistMode() && (orig == null || orig());
			}

			if (musicList != null)
			{
				var orig = musicList.IsVisible;
				musicList.IsVisible = () => !IsPlaylistMode() && (orig == null || orig());
			}

			if (noSongsLabelWidget != null)
				noSongsLabelWidget.IsVisible = () => noSongsForMode && !IsPlaylistMode();

			// ── Custom filter layout targets ──────────────────────────────
			RegisterLayoutTarget(labelContainer, shiftWithCustom: true);
			RegisterLayoutTarget(musicList, shiftWithCustom: true);
			RegisterLayoutTarget(noSongsLabelWidget, shiftWithCustom: true);
			RegisterLayoutTarget(titleLabelWidget, shiftWithCustom: true);
			RegisterLayoutTarget(buttonsPanel);
			RegisterLayoutTarget(timeLabelWidget);
			RegisterLayoutTarget(shuffleWidget);
			RegisterLayoutTarget(repeatWidget);
			RegisterLayoutTarget(noMusicContainer);
			RegisterLayoutTarget(muteLabelWidget);
			RegisterCustomRaiseTarget(modePanel);
			RegisterLayoutTarget(backButtonWidget);
			RegisterCustomControlTarget(buttonsPanel);
			RegisterCustomControlTarget(timeLabelWidget);
			RegisterCustomControlTarget(shuffleWidget);
			RegisterCustomControlTarget(repeatWidget);
			RegisterCustomControlTarget(noMusicContainer);
			RegisterCustomControlTarget(muteLabelWidget);
			RegisterCustomControlTarget(backButtonWidget);

			// ── Playlist panel shift targets (top-level only, not children of BUTTONS) ──
			RegisterPlaylistShiftTarget(buttonsPanel);
			RegisterPlaylistShiftTarget(shuffleWidget);
			RegisterPlaylistShiftTarget(noMusicContainer);
			RegisterPlaylistShiftTarget(muteLabelWidget);
			RegisterPlaylistShiftTarget(backButtonWidget);

			// ── Mode / filter / preset / dual-panel ───────────────────────
			ConfigureModeButton(panel, "MUSIC_MODE_MIX", MusicPlaybackMode.MixAll);
			ConfigureModeButton(panel, "MUSIC_MODE_OLD", MusicPlaybackMode.OnlyOldschool);
			ConfigureModeButton(panel, "MUSIC_MODE_FACTION", MusicPlaybackMode.FactionSpecific);
			ConfigureModeButton(panel, "MUSIC_MODE_CUSTOM", MusicPlaybackMode.Custom);
			ConfigureModeButton(panel, "MUSIC_MODE_PLAYLIST", MusicPlaybackMode.Playlist);

			ConfigureCustomFilters();
			ConfigurePresetRow();
			ConfigureDualPanel();

			// ── Playback controls ─────────────────────────────────────────
			bool NoMusic() => !musicPlaylist.IsMusicAvailable ||
				musicPlaylist.CurrentSongIsBackground || currentSong == null;

			if (noMusicContainer != null)
				noMusicContainer.IsVisible = () => !musicPlaylist.IsMusicAvailable;

			if (musicPlaylist.IsMusicAvailable && muteLabelWidget != null)
				muteLabelWidget.GetText = () =>
					Game.Settings.Sound.Mute ? FluentProvider.GetMessage(SoundMuted) : "";

			var playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
			playButton.IsDisabled = NoMusic;
			playButton.IsVisible = () => !Game.Sound.MusicPlaying;

			var pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Game.Sound.PauseMusic;
			pauseButton.IsDisabled = NoMusic;
			pauseButton.IsVisible = () => Game.Sound.MusicPlaying;

			var stopButton = panel.Get<ButtonWidget>("BUTTON_STOP");
			stopButton.OnClick = musicPlaylist.Stop;
			stopButton.IsDisabled = NoMusic;

			var nextButton = panel.Get<ButtonWidget>("BUTTON_NEXT");
			nextButton.OnClick = () => { currentSong = musicPlaylist.GetNextSong(); Play(); };
			nextButton.IsDisabled = NoMusic;

			var prevButton = panel.Get<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => { currentSong = musicPlaylist.GetPrevSong(); Play(); };
			prevButton.IsDisabled = NoMusic;

			shuffleWidget.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleWidget.OnClick = () => Game.Settings.Sound.Shuffle ^= true;
			shuffleWidget.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;

			repeatWidget.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatWidget.OnClick = () => Game.Sound.SetMusicLooped(!Game.Settings.Sound.Repeat);
			repeatWidget.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;

			if (timeLabelWidget != null)
				timeLabelWidget.GetText = () =>
				{
					if (currentSong == null || musicPlaylist.CurrentSongIsBackground) return "";
					var seek = Game.Sound.MusicSeekPosition;
					return $"{(int)seek / 60:D2}:{(int)seek % 60:D2} / " +
					       $"{currentSong.Length / 60:D2}:{currentSong.Length % 60:D2}";
				};

			var noSongPlaying = FluentProvider.GetMessage(NoSongPlaying);
			if (titleLabelWidget != null)
				titleLabelWidget.GetText = () =>
					currentSong != null ? currentSong.Title : noSongPlaying;

			var musicSlider = panel.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => Game.Sound.MusicVolume = x;
			musicSlider.Value = Game.Sound.MusicVolume;

			var songWatcher = widget.GetOrNull<LogicTickerWidget>("SONG_WATCHER");
			if (songWatcher != null)
				songWatcher.OnTick = () =>
				{
					if (musicPlaylist.CurrentSongIsBackground && currentSong != null)
						currentSong = null;

					if (Game.Sound.CurrentMusic == null ||
						currentSong == Game.Sound.CurrentMusic ||
						musicPlaylist.CurrentSongIsBackground)
						return;

					currentSong = Game.Sound.CurrentMusic;
				};

			if (backButtonWidget != null)
				backButtonWidget.OnClick = () =>
				{
					Game.Settings.Save();
					Ui.CloseWindow();
					onExit();
				};

			UpdateCustomLayout(forceRefresh: true);
			UpdatePlaylistLayout(forceRefresh: true);
			BuildMusicTable();
			BuildDualPanelLists();
		}

		// ── Single-panel ───────────────────────────────────────────────────
		public void BuildMusicTable()
		{
			if (!musicPlaylist.IsMusicAvailable) return;

			var music = musicPlaylist.AvailablePlaylist();
			noSongsForMode = musicPlaylist.IsMusicAvailable && music.Length == 0;
			currentSong = musicPlaylist.CurrentSong();
			musicList.RemoveChildren();

			foreach (var song in music)
			{
				var s = song;
				var item = ScrollItemWidget.Setup(
					s.Filename, itemTemplate,
					() => currentSong == s,
					() => { currentSong = s; Play(); },
					() => { });

				WidgetUtils.TruncateLabelToTooltip(item.Get<LabelWithTooltipWidget>("TITLE"), s.Title);
				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(s);
				musicList.AddChild(item);
			}

			if (currentSong != null && !musicPlaylist.CurrentSongIsBackground)
				musicList.ScrollToItem(currentSong.Filename);
		}

		// ── Dual-panel ─────────────────────────────────────────────────────
		void BuildDualPanelLists()
		{
			BuildAvailableList();
			BuildPlaylistTrackList();
		}

		void BuildAvailableList()
		{
			if (availableList == null || availableTemplate == null) return;

			if (availableHeaderWidget != null)
				availableHeaderWidget.GetText = () =>
					FormatDualHeader("Available Tracks", allInstalledTracks);

			availableList.RemoveChildren();

			// Item and label widths based on current scroll panel width
			var listW = availableList.Bounds.Width;
			var itemW = listW - 27;
			var labelW = itemW - 50;

			foreach (var track in allInstalledTracks)
			{
				if (userPlaylistKeys.Contains(track.Key)) continue;

				var t = track;
				var item = ScrollItemWidget.Setup(
					"avail_" + t.Key, availableTemplate,
					() => selectedAvailableTrack == t,
					() => selectedAvailableTrack = t,
					() => { selectedAvailableTrack = t; currentSong = t; PlayDirect(); });

				var ib = item.Bounds;
				item.Bounds = new WidgetBounds(ib.X, ib.Y, itemW, ib.Height);

				var titleLabel = item.Get<LabelWithTooltipWidget>("TITLE");
				var tb = titleLabel.Bounds;
				titleLabel.Bounds = new WidgetBounds(tb.X, tb.Y, labelW, tb.Height);

				var lengthLabel = item.Get<LabelWidget>("LENGTH");
				var lb = lengthLabel.Bounds;
				lengthLabel.Bounds = new WidgetBounds(itemW - 60, lb.Y, lb.Width, lb.Height);

				WidgetUtils.TruncateLabelToTooltip(titleLabel, t.Title);
				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(t);
				availableList.AddChild(item);
			}
		}

		void BuildPlaylistTrackList()
		{
			if (playlistTrackList == null || playlistTrackTemplate == null) return;

			if (playlistHeaderWidget != null)
				playlistHeaderWidget.GetText = () =>
					FormatDualHeader("Playlist", GetCurrentPlaylistTracks());

			playlistTrackList.RemoveChildren();

			var listW = playlistTrackList.Bounds.Width;
			var itemW = listW - 27;
			var labelW = itemW - 50;

			foreach (var track in GetCurrentPlaylistTracks())
			{
				var t = track;
				var item = ScrollItemWidget.Setup(
					"pl_" + t.Key, playlistTrackTemplate,
					() => currentSong == t || selectedPlaylistTrack == t,
					() => selectedPlaylistTrack = t,
					() => { selectedPlaylistTrack = t; currentSong = t; PlayDirect(); });

				var ib = item.Bounds;
				item.Bounds = new WidgetBounds(ib.X, ib.Y, itemW, ib.Height);

				var titleLabel = item.Get<LabelWithTooltipWidget>("TITLE");
				var tb = titleLabel.Bounds;
				titleLabel.Bounds = new WidgetBounds(tb.X, tb.Y, labelW, tb.Height);

				var lengthLabel = item.Get<LabelWidget>("LENGTH");
				var lb = lengthLabel.Bounds;
				lengthLabel.Bounds = new WidgetBounds(itemW - 60, lb.Y, lb.Width, lb.Height);

				WidgetUtils.TruncateLabelToTooltip(titleLabel, t.Title);
				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(t);
				playlistTrackList.AddChild(item);
			}
		}

		MusicInfo[] GetCurrentPlaylistTracks()
		{
			var rules = world.Map.Rules;
			return userPlaylistKeys
				.Where(k => rules.Music.TryGetValue(k, out var info) && info.Exists && !info.Hidden)
				.Select(k => rules.Music[k])
				.ToArray();
		}

		void SaveUserPlaylist()
		{
			Game.Settings.Sound.UserPlaylistTracks = userPlaylistKeys.ToArray();

			if (Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist)
				musicPlaylist.RefreshForPlaybackModeChange(
					Game.Sound.MusicPlaying && !musicPlaylist.CurrentSongIsBackground);
		}

		static string FormatDualHeader(string prefix, IReadOnlyCollection<MusicInfo> tracks)
		{
			var s = tracks.Sum(t => t.Length);
			var h = s / 3600; var m = (s % 3600) / 60; var sec = s % 60;
			var time = h > 0 ? $"{h}:{m:D2}:{sec:D2}" : $"{m:D2}:{sec:D2}";
			return $"{prefix} ({tracks.Count}) - {time}";
		}

		void ConfigureDualPanel()
		{
			if (dualPanelContent == null) return;

			var addTrack = panel.GetOrNull<ButtonWidget>("ADD_TRACK");
			if (addTrack != null)
				addTrack.OnClick = () =>
				{
					if (selectedAvailableTrack == null ||
						userPlaylistKeys.Contains(selectedAvailableTrack.Key))
						return;

					var addedKey = selectedAvailableTrack.Key;
					var filtered = allInstalledTracks
						.Where(t => !userPlaylistKeys.Contains(t.Key)).ToList();
					var idx = filtered.FindIndex(t => t.Key == addedKey);
					var anchor = idx >= 0 && idx + 1 < filtered.Count
						? filtered[idx + 1].Key : (idx > 0 ? filtered[idx - 1].Key : null);

					userPlaylistKeys.Add(addedKey);
					selectedAvailableTrack = null;
					SaveUserPlaylist();
					BuildDualPanelLists();

					if (anchor != null)
						availableList?.ScrollToItem("avail_" + anchor);
				};

			var addAll = panel.GetOrNull<ButtonWidget>("ADD_ALL");
			if (addAll != null)
				addAll.OnClick = () =>
				{
					foreach (var t in allInstalledTracks)
						if (!userPlaylistKeys.Contains(t.Key))
							userPlaylistKeys.Add(t.Key);

					selectedAvailableTrack = null;
					SaveUserPlaylist();
					BuildDualPanelLists();
				};

			var removeTrack = panel.GetOrNull<ButtonWidget>("REMOVE_TRACK");
			if (removeTrack != null)
				removeTrack.OnClick = () =>
				{
					if (selectedPlaylistTrack == null) return;

					if (currentSong == selectedPlaylistTrack)
						musicPlaylist.Stop();

					var removedKey = selectedPlaylistTrack.Key;
					var plTracks = GetCurrentPlaylistTracks().ToList();
					var idx = plTracks.FindIndex(t => t.Key == removedKey);
					var anchor = idx >= 0 && idx + 1 < plTracks.Count
						? plTracks[idx + 1].Key : (idx > 0 ? plTracks[idx - 1].Key : null);

					userPlaylistKeys.Remove(removedKey);
					selectedPlaylistTrack = null;
					SaveUserPlaylist();
					BuildDualPanelLists();

					if (anchor != null)
						playlistTrackList?.ScrollToItem("pl_" + anchor);
				};

			var removeAll = panel.GetOrNull<ButtonWidget>("REMOVE_ALL");
			if (removeAll != null)
				removeAll.OnClick = () =>
				{
					if (Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist)
						musicPlaylist.Stop();

					userPlaylistKeys.Clear();
					selectedPlaylistTrack = null;
					SaveUserPlaylist();
					BuildDualPanelLists();
				};
		}

		// ── Preset management ──────────────────────────────────────────────
		Dictionary<string, PlaylistDefinition> LoadUserPresets()
		{
			var result = new Dictionary<string, PlaylistDefinition>();
			try
			{
				if (!File.Exists(UserPresetsPath)) return result;
				foreach (var node in MiniYaml.FromFile(UserPresetsPath, false))
					result[node.Key] = new PlaylistDefinition(node.Key, node.Value);
			}
			catch (Exception e) { Log.Write("debug", $"Failed to load music presets: {e.Message}"); }
			return result;
		}

		void WriteUserPresets()
		{
			try
			{
				var sb = new StringBuilder();
				foreach (var (key, def) in userPresets)
				{
					sb.AppendLine($"{key}:");
					sb.AppendLine($"\t{def.DisplayName}");
					sb.AppendLine("\tTracks:");
					foreach (var track in def.Tracks)
						sb.AppendLine($"\t\t{track}");
				}
				File.WriteAllText(UserPresetsPath, sb.ToString());
			}
			catch (Exception e) { Log.Write("debug", $"Failed to save music presets: {e.Message}"); }
		}

		static string MakePresetKey(string displayName, IEnumerable<string> existingKeys)
		{
			var sanitized = new string(
				displayName.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray()).Trim('_');
			if (string.IsNullOrEmpty(sanitized)) sanitized = "preset";
			var existing = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);
			var key = sanitized; var i = 2;
			while (existing.Contains(key)) key = sanitized + "_" + i++;
			return key;
		}

		void LoadPreset(string key, bool isUserPreset)
		{
			PlaylistDefinition def = null;
			if (isUserPreset) userPresets.TryGetValue(key, out def);
			else world.Map.Rules.Playlists.TryGetValue(key, out def);
			if (def == null) return;

			var rules = world.Map.Rules;
			userPlaylistKeys.Clear();
			userPlaylistKeys.AddRange(def.Tracks
				.Where(t => rules.Music.TryGetValue(t, out var info) && info.Exists && !info.Hidden));

			SaveUserPlaylist();
			BuildDualPanelLists();
		}

		void ConfigurePresetRow()
		{
			var loadDropdown = panel.GetOrNull<DropDownButtonWidget>("LOAD_PRESET");
			if (loadDropdown != null)
			{
				loadDropdown.GetText = () =>
				{
					if (currentPresetKey != null)
					{
						if (userPresets.TryGetValue(currentPresetKey, out var def)) return def.DisplayName;
						if (world.Map.Rules.Playlists.TryGetValue(currentPresetKey, out def)) return def.DisplayName;
					}
					return FluentProvider.GetMessage(LoadPresetDefault);
				};

				loadDropdown.OnMouseDown = _ =>
				{
					var items = new List<(string Key, string Label, bool IsUser)>();
					foreach (var kvp in world.Map.Rules.Playlists)
						items.Add((kvp.Key, kvp.Value.DisplayName, false));
					foreach (var kvp in userPresets)
						items.Add((kvp.Key, "• " + kvp.Value.DisplayName, true));
					if (items.Count == 0) return;

					ScrollItemWidget SetupItem(
						(string Key, string Label, bool IsUser) entry,
						ScrollItemWidget template)
					{
						var item = ScrollItemWidget.Setup(entry.Key, template,
							() => currentPresetKey == entry.Key,
							() => { currentPresetKey = entry.Key; LoadPreset(entry.Key, entry.IsUser); },
							() => { });
						item.Get<LabelWidget>("LABEL").GetText = () => entry.Label;
						return item;
					}

					loadDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 200, items, SetupItem);
				};
			}

			var saveButton = panel.GetOrNull<ButtonWidget>("SAVE_PRESET");
			if (saveButton != null)
			{
				saveButton.IsDisabled = () => userPlaylistKeys.Count == 0;
				saveButton.OnClick = () =>
				{
					ConfirmationDialogs.TextInputPrompt(
						modData, SavePresetTitle, SavePresetPrompt, initialText: "",
						onAccept: name =>
						{
							// Check if a user preset with this display name already exists
							var existingEntry = userPresets
								.FirstOrDefault(kv => string.Equals(
									kv.Value.DisplayName, name,
									StringComparison.OrdinalIgnoreCase));

							// Also check mod presets
							var modConflict = world.Map.Rules.Playlists
								.Any(kv => string.Equals(
									kv.Value.DisplayName, name,
									StringComparison.OrdinalIgnoreCase));

							if (existingEntry.Key != null)
							{
								// User preset exists — ask to overwrite
								var keyToOverwrite = existingEntry.Key;
								ConfirmationDialogs.ButtonPrompt(modData,
									title: OverwritePresetTitle,
									text: OverwritePresetPrompt,
									textArguments: ["preset", name],
									onConfirm: () =>
									{
										userPresets[keyToOverwrite] = new PlaylistDefinition(
											name, userPlaylistKeys.ToArray());
										currentPresetKey = keyToOverwrite;
										WriteUserPresets();
									},
									confirmText: OverwritePresetConfirm,
									onCancel: () => { });
							}
							else if (modConflict)
							{
								// Mod preset with same name — disallow, save as a new distinct name
								var allKeys = world.Map.Rules.Playlists.Keys.Concat(userPresets.Keys);
								var allNames = world.Map.Rules.Playlists.Values
									.Select(d => d.DisplayName)
									.Concat(userPresets.Values.Select(d => d.DisplayName));
								var key = MakePresetKey(name, allKeys);
								userPresets[key] = new PlaylistDefinition(name, userPlaylistKeys.ToArray());
								currentPresetKey = key;
								WriteUserPresets();
							}
							else
							{
								// No conflict — save normally
								var allKeys = world.Map.Rules.Playlists.Keys.Concat(userPresets.Keys);
								var key = MakePresetKey(name, allKeys);
								userPresets[key] = new PlaylistDefinition(name, userPlaylistKeys.ToArray());
								currentPresetKey = key;
								WriteUserPresets();
							}
						},
						acceptText: SavePresetConfirm,
						inputValidator: name => !string.IsNullOrWhiteSpace(name));
				};
			}

			var deleteButton = panel.GetOrNull<ButtonWidget>("DELETE_PRESET");
			if (deleteButton != null)
			{
				deleteButton.IsDisabled = () =>
					currentPresetKey == null || !userPresets.ContainsKey(currentPresetKey);

				deleteButton.OnClick = () =>
				{
					if (currentPresetKey == null ||
						!userPresets.TryGetValue(currentPresetKey, out var def)) return;

					var keyToDelete = currentPresetKey;
					ConfirmationDialogs.ButtonPrompt(modData,
						title: DeletePresetTitle,
						text: DeletePresetPrompt,
						textArguments: ["preset", def.DisplayName],
						onConfirm: () =>
						{
							userPresets.Remove(keyToDelete);
							if (currentPresetKey == keyToDelete) currentPresetKey = null;
							WriteUserPresets();
						},
						confirmText: DeletePresetConfirm,
						onCancel: () => { });
				};
			}
		}

		// ── Mode buttons ───────────────────────────────────────────────────
			void ConfigureModeButton(Widget panel, string id, MusicPlaybackMode mode)
		{
			var button = panel.GetOrNull<ButtonWidget>(id);
			if (button == null) return;

			button.IsHighlighted = () => Game.Settings.Sound.MusicMode == mode;
			button.OnClick = () =>
			{
				// In lobby mode, Playlist button opens the full MUSIC_PANEL as a modal overlay
				if (mode == MusicPlaybackMode.Playlist && panel.Id != "MUSIC_PANEL")
				{
					Game.Settings.Sound.MusicMode = MusicPlaybackMode.Playlist;
					Ui.OpenWindow("MUSIC_PANEL", new WidgetArgs
					{
						{ "world", world },
						{ "onExit", (Action)(() => { }) }
					});
					return;
				}

				if (Game.Settings.Sound.MusicMode == mode) return;

				var resume = Game.Sound.MusicPlaying && !musicPlaylist.CurrentSongIsBackground;
				var wasPlaylist = Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist;
				Game.Settings.Sound.MusicMode = mode;
				UpdatePlaylistLayout(forceRefresh: false);
				UpdateCustomLayout(forceRefresh: wasPlaylist);
				RefreshPlaylistAfterFilterChange(resume);
			};
		}

		void ConfigureCustomFilters()
		{
			if (customFilterPanel == null) return;
			foreach (var (checkboxId, category) in CustomFilterOptions)
			{
				var checkbox = customFilterPanel.GetOrNull<CheckboxWidget>(checkboxId);
				if (checkbox == null) continue;
				checkbox.IsChecked = () => IsCustomCategoryEnabled(category);
				checkbox.OnClick = () => ToggleCustomCategory(category);
			}
		}

		// ── Layout: Custom filter expansion ───────────────────────────────
		void RegisterLayoutTarget(Widget widget, bool stretchHeight = false, bool shiftWithCustom = false)
		{
			if (widget == null) return;
			layoutShiftTargets.Add((widget, widget.Bounds.Y, widget.Bounds.Height, stretchHeight, shiftWithCustom));
		}

		void RegisterCustomRaiseTarget(Widget widget)
		{
			if (widget == null) return;
			customRaiseTargets.Add((widget, widget.Bounds.Y));
		}

		void RegisterCustomControlTarget(Widget widget)
		{
			if (widget == null) return;
			customControlTargets.Add((widget, widget.Bounds.Y));
		}

		void UpdateCustomLayout(bool forceRefresh)
		{
			// Playlist mode handles its own layout — don't interfere
			if (Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist) return;
			if (customFilterPanel == null) return;

			var shouldShow = Game.Settings.Sound.MusicMode == MusicPlaybackMode.Custom;
			if (!forceRefresh && shouldShow == showCustomFilters) return;

			showCustomFilters = shouldShow;
			var height = showCustomFilters ? expandedPanelHeight : collapsedPanelHeight;
			var offset = showCustomFilters ? customLayoutOffset : 0;

			if (allowPanelResize)
				panel.Bounds = new WidgetBounds(
					panel.Bounds.X, panelCenterY - height / 2,
					panel.Bounds.Width, height);

			foreach (var (widget, baseY, baseHeight, stretchHeight, shiftWithCustom) in layoutShiftTargets)
			{
				if (widget == null) continue;
				var b = widget.Bounds;
				var h = stretchHeight && allowPanelResize
					? baseHeight + (showCustomFilters ? customLayoutOffset : 0) : baseHeight;
				var y = showCustomFilters && allowPanelResize && shiftWithCustom ? baseY + offset : baseY;
				widget.Bounds = new WidgetBounds(b.X, y, b.Width, h);
			}

			foreach (var (widget, baseY) in customRaiseTargets)
			{
				if (widget == null) continue;
				var b = widget.Bounds;
				widget.Bounds = new WidgetBounds(b.X,
					showCustomFilters ? baseY - CustomUpOffset : baseY, b.Width, b.Height);
			}

			foreach (var (widget, baseY) in customControlTargets)
			{
				if (widget == null) continue;
				var b = widget.Bounds;
				widget.Bounds = new WidgetBounds(b.X,
					showCustomFilters ? baseY + CustomControlDownOffset : baseY, b.Width, b.Height);
			}
		}

		// ── Layout: Playlist panel expansion ──────────────────────────────
		void RegisterPlaylistShiftTarget(Widget widget)
		{
			if (widget == null) return;
			playlistShiftTargets.Add((widget, widget.Bounds.Y, widget.Bounds.X));
		}

		void UpdatePlaylistLayout(bool forceRefresh)
		{
			if (!allowPanelResize) return;

			var shouldShow = Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist;
			if (!forceRefresh && shouldShow == showPlaylistLayout) return;

			showPlaylistLayout = shouldShow;

			var newWidth = shouldShow ? PlaylistPanelWidth : basePanelWidth;
			var newHeight = shouldShow ? PlaylistPanelHeight : basePanelHeight;
			var centeredX = (Game.Renderer.Resolution.Width - newWidth) / 2;
			var centeredY = panelCenterY - newHeight / 2;

			panel.Bounds = new WidgetBounds(centeredX, centeredY, newWidth, newHeight);

			var inner = newWidth - 40;
			var half = (inner - 10) / 2;
			var half2 = half + 10;
			var third = (inner - 20) / 3;

			// ── Full-width containers ─────────────────────────────────────
			ResizeTo(modePanel, inner);
			ResizeTo(labelContainer, inner);
			ResizeTo(musicList, inner);
			ResizeTo(customFilterPanel, inner);
			ResizeTo(presetRow, inner);
			ResizeTo(dualPanelContent, inner);

			// ── Mode buttons inside MODE_PANEL ────────────────────────────
			if (modePanel != null)
			{
				foreach (var btn in modePanel.Children.OfType<ButtonWidget>())
				{
					var b = btn.Bounds;
					if (btn.Id == "MUSIC_MODE_PLAYLIST")
						btn.Bounds = new WidgetBounds(0, b.Y, inner, b.Height);
					else if (b.X == 0)
						btn.Bounds = new WidgetBounds(0, b.Y, half, b.Height);
					else
						btn.Bounds = new WidgetBounds(half2, b.Y, half, b.Height);
				}
			}

			// ── Custom filter checkboxes inside CUSTOM_FILTER_PANEL ───────
			if (customFilterPanel != null)
			{
				foreach (var cb in customFilterPanel.Children.OfType<CheckboxWidget>())
				{
					var b = cb.Bounds;
					if (b.X == 0)
						cb.Bounds = new WidgetBounds(0, b.Y, half, b.Height);
					else
						cb.Bounds = new WidgetBounds(half2, b.Y, half, b.Height);
				}
			}

			// ── Preset row buttons ────────────────────────────────────────
			if (presetRow != null)
			{
				var load = presetRow.GetOrNull<DropDownButtonWidget>("LOAD_PRESET");
				var save = presetRow.GetOrNull<ButtonWidget>("SAVE_PRESET");
				var del = presetRow.GetOrNull<ButtonWidget>("DELETE_PRESET");

				if (load != null) { var b = load.Bounds; load.Bounds = new WidgetBounds(0, b.Y, third, b.Height); }
				if (save != null) { var b = save.Bounds; save.Bounds = new WidgetBounds(third + 10, b.Y, third, b.Height); }
				if (del != null) { var b = del.Bounds; del.Bounds = new WidgetBounds(2 * (third + 10), b.Y, third, b.Height); }
			}

			// ── Dual panel content ────────────────────────────────────────
			if (dualPanelContent != null)
			{
				var btnHalfW = half / 2 - 5;
				var btnHalfX2 = half / 2 + 5;

				ResizeWithX(availableHeaderWidget, 0, half);
				ResizeWithX(availableList, 0, half);

				var addTrack = dualPanelContent.GetOrNull<ButtonWidget>("ADD_TRACK");
				var addAll = dualPanelContent.GetOrNull<ButtonWidget>("ADD_ALL");
				if (addTrack != null) { var b = addTrack.Bounds; addTrack.Bounds = new WidgetBounds(0, b.Y, btnHalfW, b.Height); }
				if (addAll != null) { var b = addAll.Bounds; addAll.Bounds = new WidgetBounds(btnHalfX2, b.Y, btnHalfW, b.Height); }

				ResizeWithX(playlistHeaderWidget, half2, half);
				ResizeWithX(playlistTrackList, half2, half);

				var remTrack = dualPanelContent.GetOrNull<ButtonWidget>("REMOVE_TRACK");
				var remAll = dualPanelContent.GetOrNull<ButtonWidget>("REMOVE_ALL");
				if (remTrack != null) { var b = remTrack.Bounds; remTrack.Bounds = new WidgetBounds(half2, b.Y, btnHalfW, b.Height); }
				if (remAll != null) { var b = remAll.Bounds; remAll.Bounds = new WidgetBounds(half2 + btnHalfX2, b.Y, btnHalfW, b.Height); }
			}

			// ── BUTTONS container + slider ────────────────────────────────
			ResizeTo(buttonsPanel, inner);
			var slider = buttonsPanel?.GetOrNull<SliderWidget>("MUSIC_SLIDER");
			if (slider != null)
			{
				var b = slider.Bounds;
				slider.Bounds = new WidgetBounds(b.X, b.Y, inner - b.X, b.Height);
			}

			// ── Bottom control Y shift + Back button re-center ────────────
			var yShift = shouldShow ? (PlaylistPanelHeight - basePanelHeight) : 0;

			// ── Title and time labels — update width/X/Y so centering works ─
			if (titleLabelWidget != null)
				titleLabelWidget.Bounds = new WidgetBounds(20, baseTitleY + yShift, newWidth - 40, titleLabelWidget.Bounds.Height);

			if (timeLabelWidget != null)
				timeLabelWidget.Bounds = new WidgetBounds((newWidth - 140) / 2, baseTimeY + yShift, 140, timeLabelWidget.Bounds.Height);

			// ── Repeat checkbox (right-aligned + Y shift) ─────────────────
			if (repeatWidget != null)
			{
				var b = repeatWidget.Bounds;
				repeatWidget.Bounds = new WidgetBounds(
					newWidth - 15 - b.Width,
					baseRepeatY + yShift,
					b.Width, b.Height);
			}

			foreach (var (widget, baseY, baseX) in playlistShiftTargets)
			{
				if (widget == null) continue;
				var b = widget.Bounds;
				var newX = widget == backButtonWidget ? (newWidth - b.Width) / 2 : baseX;
				widget.Bounds = new WidgetBounds(newX, baseY + yShift, b.Width, b.Height);
			}
		}

		static void ResizeTo(Widget widget, int newWidth)
		{
			if (widget == null) return;
			var b = widget.Bounds;
			widget.Bounds = new WidgetBounds(b.X, b.Y, newWidth, b.Height);
		}

		static void ResizeWithX(Widget widget, int newX, int newWidth)
		{
			if (widget == null) return;
			var b = widget.Bounds;
			widget.Bounds = new WidgetBounds(newX, b.Y, newWidth, b.Height);
		}

		// ── Playback ───────────────────────────────────────────────────────
		void Play()
		{
			if (currentSong == null) return;

			if (Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist)
				playlistTrackList?.ScrollToItem("pl_" + currentSong.Key);
			else
				musicList.ScrollToItem(currentSong.Filename);

			musicPlaylist.Play(currentSong);
		}

		void PlayDirect()
		{
			if (currentSong == null) return;
			musicPlaylist.Play(currentSong);
		}

		void ToggleCustomCategory(string category)
		{
			var normalized = NormalizeCategory(category);
			if (string.IsNullOrEmpty(normalized)) return;

			var selection = Game.Settings.Sound.CustomMusicCategories?.ToList() ?? new List<string>();
			var idx = selection.FindIndex(c => CustomCategoryComparer.Equals(c, normalized));
			if (idx >= 0) selection.RemoveAt(idx); else selection.Add(normalized);

			Game.Settings.Sound.CustomMusicCategories = selection.ToArray();
			RefreshPlaylistAfterFilterChange(
				Game.Sound.MusicPlaying && !musicPlaylist.CurrentSongIsBackground);
		}

		static bool IsCustomCategoryEnabled(string category)
		{
			var normalized = NormalizeCategory(category);
			if (string.IsNullOrEmpty(normalized)) return false;
			return (Game.Settings.Sound.CustomMusicCategories ?? Array.Empty<string>())
				.Any(c => CustomCategoryComparer.Equals(c, normalized));
		}

		static string NormalizeCategory(string category)
			=> MusicCategories.Normalize(category, null);

		void RefreshPlaylistAfterFilterChange(bool resume)
		{
			musicPlaylist.RefreshForPlaybackModeChange(resume);
			BuildMusicTable();
			if (Game.Settings.Sound.MusicMode == MusicPlaybackMode.Playlist)
				BuildPlaylistTrackList();
			if (resume) Play();
		}

		static string SongLengthLabel(MusicInfo song)
			=> $"{song.Length / 60:D1}:{song.Length % 60:D2}";
	}
}
