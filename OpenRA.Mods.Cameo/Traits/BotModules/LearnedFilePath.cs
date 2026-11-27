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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Cameo.Traits.BotModules
{
	/// <summary>
	/// Resolves a mod-relative learned-data path for <see cref="OpenRA.FileSystem.IReadOnlyFileSystem"/>.
	/// A bare path like <c>ai/learned/x.yaml</c> can never resolve: the file system indexes only the
	/// top-level <c>Folder.Contents</c> names, so nested paths must be addressed inside the cameo
	/// package explicitly (<c>cameo|ai/learned/x.yaml</c>), where <c>Contains</c>/<c>GetStream</c>
	/// combine the path on disk. An already package-qualified path is passed through.
	/// </summary>
	internal static class LearnedFilePath
	{
		public static string Resolve(string file)
		{
			return file.Contains('|') ? file : $"cameo|{file}";
		}
	}

	/// <summary>
	/// CONSOL-DUP-04: the shared steps of the one-time learned-YAML load (path qualification, existence check, open, MiniYaml parse). It owns NO failure policy:
	/// nothing is caught and nothing is logged here. Each caller keeps its own (BotLearnedPriors: missing = neutral + debug log, errors propagate;
	/// EngagementPriorsBotModule: missing = state "none", open/parse errors caught into state "error"); the two schema parsers stay separate.
	/// </summary>
	internal static class LearnedYaml
	{
		/// <summary>The package-qualified path when the learned file exists, else null. The existence check is outside any caller's try (as before).</summary>
		public static string ExistingPath(OpenRA.FileSystem.IReadOnlyFileSystem fs, string file)
		{
			var path = LearnedFilePath.Resolve(file);
			return fs.Exists(path) ? path : null;
		}

		/// <summary>Opens and parses a learned file found by <see cref="ExistingPath"/>; every open or parse exception propagates to the caller.
		/// The parse is finished inside the open stream (ToList), as the callers always consumed it inside their using.</summary>
		public static List<MiniYamlNode> Parse(OpenRA.FileSystem.IReadOnlyFileSystem fs, string path, string file)
		{
			using (var stream = fs.Open(path))
				return MiniYaml.FromStream(stream, file).ToList();
		}
	}
}
