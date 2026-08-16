#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Adds the synchronized resource regrowth rate dropdown to the lobby.")]
	public class ResourceRegrowthInfo : TraitInfo, ILobbyOptions
	{
		const string OptionId = "resourceregrowth";

		[FluentReference]
		public readonly string DropdownLabel = "dropdown_resource_regrowth.label";

		[FluentReference]
		public readonly string DropdownDescription = "dropdown_resource_regrowth.description";

		public readonly bool DropdownLocked = false;
		public readonly bool DropdownVisible = true;
		public readonly int DropdownDisplayOrder = 22;

		static readonly Dictionary<string, string> Values = new()
		{
			{ "off", "dropdown_resource_regrowth.choice-off" },
			{ "half", "dropdown_resource_regrowth.choice-half" },
			{ "normal", "dropdown_resource_regrowth.choice-normal" },
			{ "twice", "dropdown_resource_regrowth.choice-twice" },
			{ "4x", "dropdown_resource_regrowth.choice-4x" }
		};

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyOption(map, OptionId, DropdownLabel, DropdownDescription,
				DropdownVisible, DropdownDisplayOrder, Values, "normal", DropdownLocked);
		}

		public override object Create(ActorInitializer init) { return new ResourceRegrowth(); }

		public static int RateUnits(World world)
		{
			return world.LobbyInfo.GlobalSettings.OptionOrDefault(OptionId, "normal") switch
			{
				"off" => 0,
				"half" => 1,
				"twice" => 4,
				"4x" => 8,
				_ => 2
			};
		}
	}

	public class ResourceRegrowth { }

	[Desc("Spreads a resource at the lobby-selected resource regrowth rate.")]
	public class LobbyScaledSeedsResourceInfo : ConditionalTraitInfo
	{
		public readonly int Interval = 75;
		public readonly string ResourceType = "Tiberium";
		public readonly int MaxRange = 100;

		public override object Create(ActorInitializer init) { return new LobbyScaledSeedsResource(init.Self, this); }
	}

	public class LobbyScaledSeedsResource : ConditionalTrait<LobbyScaledSeedsResourceInfo>, ITick, ISeedableResource
	{
		readonly LobbyScaledSeedsResourceInfo info;
		readonly IResourceLayer resourceLayer;
		readonly int threshold;
		readonly int rateUnits;

		[VerifySync]
		int progress;

		public LobbyScaledSeedsResource(Actor self, LobbyScaledSeedsResourceInfo info)
			: base(info)
		{
			this.info = info;
			resourceLayer = self.World.WorldActor.Trait<IResourceLayer>();
			threshold = info.Interval * 2;
			rateUnits = ResourceRegrowthInfo.RateUnits(self.World);
			progress = threshold - rateUnits;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || rateUnits == 0)
				return;

			progress += rateUnits;
			if (progress < threshold)
				return;

			progress -= threshold;
			Seed(self);
		}

		public void Seed(Actor self)
		{
			var cell = Util.RandomWalk(self.Location, self.World.SharedRandom)
				.Take(info.MaxRange)
				.SkipWhile(p => resourceLayer.GetResource(p).Type == info.ResourceType && !resourceLayer.CanAddResource(info.ResourceType, p))
				.Cast<CPos?>().FirstOrDefault();

			if (cell != null && resourceLayer.CanAddResource(info.ResourceType, cell.Value))
				resourceLayer.AddResource(info.ResourceType, cell.Value);
		}
	}
}
