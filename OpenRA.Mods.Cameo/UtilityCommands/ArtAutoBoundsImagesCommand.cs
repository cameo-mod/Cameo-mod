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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.UtilityCommands
{
	// Lists the body-sprite images whose actors rely on AUTO mouse/selection bounds (Interactable/Selectable
	// with empty Bounds AND empty Polygon, plus a WithSpriteBody). Those auto-bounds are the union of the body
	// sequence's sprites, so trimming transparent borders off those images would shrink the clickable area.
	// A sprite-trim pass must SKIP every image printed here.
	sealed class ArtAutoBoundsImagesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--auto-bounds-images";

		bool IUtilityCommand.ValidateArguments(string[] args) => true;

		[Desc("Print (one per line) the body images of actors that fall back to AUTO mouse bounds, " +
			"so a sprite-trim pass can skip them and not shrink their clickable area.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var modData = Game.ModData = utility.ModData;
			var rules = modData.DefaultRules;
			var factions = rules.Actors[SystemActors.World].TraitInfos<FactionInfo>().Select(f => f.InternalName).ToArray();

			var protectedImages = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			var actorCount = 0;

			foreach (var actorInfo in rules.Actors)
			{
				if (actorInfo.Key.StartsWith(ActorInfo.AbstractActorPrefix))
					continue;

				List<InteractableInfo> interactables;
				try { interactables = actorInfo.Value.TraitInfos<InteractableInfo>().ToList(); }
				catch { continue; }
				if (interactables.Count == 0)
					continue;

				// Relies on auto bounds only when no explicit Bounds AND no explicit Polygon.
				var autoBounds = interactables.Any(ii => ii.Bounds.IsDefaultOrEmpty && ii.Polygon.IsDefaultOrEmpty);
				if (!autoBounds)
					continue;

				// Auto mouse bounds in this tree are supplied by WithSpriteBody (boundsAnimation on info.Sequence).
				if (!actorInfo.Value.TraitInfos<WithSpriteBodyInfo>().Any())
					continue;

				var render = actorInfo.Value.TraitInfos<RenderSpritesInfo>().FirstOrDefault();
				if (render == null)
					continue;

				actorCount++;
				protectedImages.Add(render.GetImage(actorInfo.Value, null).ToLowerInvariant());
				foreach (var faction in factions)
					protectedImages.Add(render.GetImage(actorInfo.Value, faction).ToLowerInvariant());
			}

			Console.Error.WriteLine($"# auto-bounds actors: {actorCount}; protected images: {protectedImages.Count}");
			foreach (var image in protectedImages)
				Console.WriteLine(image);
		}
	}
}
