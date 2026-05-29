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
using System.Linq;

namespace OpenRA.GameRules
{
	/// <summary>
	/// Represents a named, curated playlist defined in YAML.
	/// Loaded from the mod's Playlists: manifest entry (e.g. audio/playlists.yaml).
	/// </summary>
	public class PlaylistDefinition
	{
		/// <summary>The human-readable display name shown in the jukebox UI.</summary>
		public readonly string DisplayName;

		/// <summary>
		/// Ordered list of track keys (matching keys in music.yaml).
		/// Tracks that don't exist in the installed music are silently skipped at runtime.
		/// </summary>
		public readonly string[] Tracks;

		/// <summary>Load from YAML (mod playlists.yaml or user-playlists.yaml).</summary>
		public PlaylistDefinition(string key, MiniYaml value)
		{
			DisplayName = !string.IsNullOrWhiteSpace(value.Value) ? value.Value : key;

			var nd = value.ToDictionary();
			if (nd.TryGetValue("Tracks", out var tracksYaml))
				Tracks = tracksYaml.Nodes.Select(n => n.Key).ToArray();
			else
				Tracks = Array.Empty<string>();
		}

		/// <summary>Create a user preset from name + track list.</summary>
		public PlaylistDefinition(string displayName, string[] tracks)
		{
			DisplayName = displayName;
			Tracks = tracks ?? Array.Empty<string>();
		}
	}
}
