#region Copyright & License Information
/*
 * Cameo mod
 */
#endregion

using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class StarportBatchStatusLogic : ChromeLogic
	{
		readonly World world;
		readonly Widget paletteContainer;
		readonly ProductionPaletteWidget palette;
		readonly Widget statusBackground;
		readonly LabelWidget status;

		[ObjectCreator.UseCtor]
		public StarportBatchStatusLogic(Widget widget, World world)
		{
			this.world = world;
			paletteContainer = widget.Get("PALETTE");
			palette = widget.Get<ProductionPaletteWidget>("PRODUCTION_PALETTE");
			statusBackground = widget.Get("STARPORT_BATCH_STATUS_BACKGROUND");
			status = widget.Get<LabelWidget>("STARPORT_BATCH_STATUS");
			status.GetText = StatusText;
		}

		public override void Tick()
		{
			var visible = palette.CurrentQueue is StarportBatchProductionQueue;
			statusBackground.Visible = visible;
			status.Visible = visible;
			paletteContainer.Bounds.Y = visible ? 44 : 24;
		}

		string StatusText()
		{
			if (palette.CurrentQueue is not StarportBatchProductionQueue queue)
				return "";

			var parts = new System.Collections.Generic.List<string>();
			if (queue.CollectingCount > 0)
				parts.Add($"COLLECT {queue.CollectingCount}/{queue.MaxBatchSize} " +
					WidgetUtils.FormatTime(queue.CollectionRemaining, world.Timestep));

			if (queue.PendingBatchCount > 0)
			{
				var dispatch = queue.DispatchRemaining > 0
					? WidgetUtils.FormatTime(queue.DispatchRemaining, world.Timestep)
					: "READY";
				parts.Add($"WAIT {queue.PendingBatchCount} ({dispatch})");
			}

			if (queue.ActiveBatchCount > 0)
				parts.Add($"UNLOAD {queue.ActiveBatchCount}");

			return parts.Count == 0 ? "STARPORT READY" : string.Join(" | ", parts);
		}
	}
}
