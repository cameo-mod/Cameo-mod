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
}
