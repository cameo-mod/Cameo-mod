#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available under the terms of the GNU General Public License.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>
	/// One immutable, ordinally ordered snapshot of building prerequisite metadata.
	/// The capture-unlock and tech-depth maps are different projections of this
	/// same rules traversal; faction filtering remains specific to tech depth.
	/// </summary>
	internal sealed class BaseBuilderRuleMetadataCA
	{
		internal readonly record struct Provider(string Prerequisite, ImmutableArray<string> Factions);
		internal readonly record struct Building(string Name, ImmutableArray<string> Prerequisites, ImmutableArray<Provider> Providers);

		readonly ImmutableArray<Building> buildings;

		BaseBuilderRuleMetadataCA(ImmutableArray<Building> buildings)
		{
			this.buildings = buildings;
		}

		internal static BaseBuilderRuleMetadataCA Capture(Ruleset rules)
		{
			var buildings = ImmutableArray.CreateBuilder<Building>();
			foreach (var info in rules.Actors.Values.OrderBy(a => a.Name, StringComparer.Ordinal))
			{
				if (!info.HasTraitInfo<BuildingInfo>())
					continue;

				var prerequisites = ImmutableArray.CreateBuilder<string>();
				foreach (var buildable in info.TraitInfos<BuildableInfo>())
					prerequisites.AddRange(buildable.Prerequisites);

				var providers = ImmutableArray.CreateBuilder<Provider>();
				foreach (var provider in info.TraitInfos<ProvidesPrerequisiteInfo>())
					providers.Add(new Provider(provider.Prerequisite ?? info.Name, provider.Factions.ToImmutableArray()));

				buildings.Add(new Building(info.Name, prerequisites.ToImmutable(), providers.ToImmutable()));
			}

			return new BaseBuilderRuleMetadataCA(buildings.ToImmutable());
		}

		internal static BaseBuilderRuleMetadataCA FromRecords(IEnumerable<Building> records) =>
			new(records.OrderBy(a => a.Name, StringComparer.Ordinal).ToImmutableArray());

		internal IReadOnlyList<string> ActorNames => buildings.Select(b => b.Name).ToArray();

		internal CaptureUnlockMapsCA CaptureMaps()
		{
			var prereqsOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			var providesOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			foreach (var building in buildings)
			{
				prereqsOf[building.Name] = building.Prerequisites;
				providesOf[building.Name] = building.Providers.Select(p => p.Prerequisite).ToArray();
			}

			return CaptureUnlockMapsCA.Build(prereqsOf, providesOf);
		}

		internal IReadOnlyDictionary<string, int> CoreDepths(string faction, IReadOnlyCollection<string> constructionYardTypes)
		{
			var prereqsOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			var providersOf = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			foreach (var building in buildings)
			{
				prereqsOf[building.Name] = building.Prerequisites;
				foreach (var provider in building.Providers)
				{
					if (provider.Factions.Length > 0 && !provider.Factions.Any(f => string.Equals(f, faction, StringComparison.OrdinalIgnoreCase)))
						continue;

					var key = (provider.Prerequisite ?? building.Name).ToLowerInvariant();
					if (!providersOf.TryGetValue(key, out var list))
						providersOf[key] = list = new List<string>();

					((List<string>)list).Add(building.Name);
				}
			}

			return TechDepthCA.Compute(prereqsOf, providersOf, constructionYardTypes);
		}
	}
}
