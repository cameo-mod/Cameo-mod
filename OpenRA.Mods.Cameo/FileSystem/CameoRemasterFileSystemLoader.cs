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
using System.Collections.Immutable;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Cameo.FileSystem
{
	// Mounts the mod's normal packages exactly like DefaultFileSystem, then - only when the
	// player has opted into HD art AND owns the C&C Remastered Collection - additionally mounts
	// its HD texture packages from the owned install. With no opt-in or no install, this behaves
	// identically to DefaultFileSystem, so the game stays exactly as it is for everyone else.
	//
	// This does NOT subclass DefaultFileSystemLoader: the base Packages field uses
	// [FieldLoader.LoadUsing(nameof(LoadPackages))], and FieldLoader resolves that method on the
	// concrete type, so the loader function must live on this class.
	public class CameoRemasterFileSystemLoader : IFileSystemLoader
	{
		[FieldLoader.LoadUsing(nameof(LoadPackages))]
		public readonly ImmutableArray<KeyValuePair<string, string>> Packages = default;

		static object LoadPackages(MiniYaml yaml)
		{
			var packageNode = yaml.NodeWithKeyOrDefault(nameof(Packages));
			if (packageNode == null)
				return default(ImmutableArray<KeyValuePair<string, string>>);

			var packages = new List<KeyValuePair<string, string>>(packageNode.Value.Nodes.Length);
			foreach (var node in packageNode.Value.Nodes)
				packages.Add(KeyValuePair.Create(node.Key, node.Value.Value));

			return packages.ToImmutableArray();
		}

		public void Mount(Manifest manifest, OpenRA.FileSystem.FileSystem fileSystem, ObjectCreator objectCreator)
		{
			if (Packages != null)
				foreach (var kv in Packages)
					fileSystem.Mount(kv.Key, kv.Value);

			if (!RemasterContent.IsEnabled() || !RemasterContent.TryFindDataDir(out var dataDir))
				return;

			// dataDir is confirmed to exist by TryFindDataDir, so Folder won't create anything.
			var folder = new Folder(dataDir);
			foreach (var package in RemasterContent.TexturePackages)
			{
				if (!folder.Contains(package))
					continue;

				var meg = folder.OpenPackage(package, fileSystem);
				if (meg != null)
					fileSystem.Mount(meg);
			}
		}
	}
}
