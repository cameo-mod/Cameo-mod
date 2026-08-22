#region Copyright & License Information
/*
 * Ported to Cameo from OpenRA Combined Arms (github.com/Inq8/CAmod), which is
 * free software under the GNU General Public License. See COPYING.
 *
 * Cameo changes: namespace OpenRA.Mods.CA.* -> OpenRA.Mods.Cameo.*.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets.Logic
{
	public class ReplayControlBarLogicCameo : ChromeLogic
	{
		// ⭐ ONE TABLE, ONE LOOP. The CA original repeats an identical eight-line block per
		// speed, so adding a step there means editing the enum, the dictionary AND the body.
		// Here a speed is one row.
		//
		// ⚠ THE NUMBER IS A TIMESTEP MULTIPLIER, SO SMALLER IS FASTER. It scales
		// `world.Timestep`, which is the milliseconds per tick — 2x playback is 0.5, not 2.
		// Getting this backwards silently makes every button do the opposite.
		//
		// Only four replay hotkeys exist engine-side (Slow/Regular/Fast/Max), so the four new
		// intermediate steps are mouse-only. `ReplaySpeedFast` stays on 2x — its long-standing
		// meaning — rather than moving to 1.33x.
		static readonly (string Button, float Timestep, string Label)[] Speeds =
		{
			("BUTTON_SLOW",     2f,       "0.5x"),
			("BUTTON_REGULAR",  1f,       "1x"),
			("BUTTON_SPEED125", 0.8f,     "1.25x"),
			("BUTTON_SPEED133", 0.75f,    "1.33x"),
			("BUTTON_SPEED150", 1f / 1.5f, "1.5x"),
			("BUTTON_FAST",     0.5f,     "2x"),
			("BUTTON_FASTER",   0.2f,     "5x"),
			("BUTTON_MAXIMUM",  0.001f,   "MAX"),
		};

		[ObjectCreator.UseCtor]
		public ReplayControlBarLogicCameo(Widget widget, World world, OrderManager orderManager)
		{
			if (world.IsReplay)
			{
				var container = widget.Get("REPLAY_PLAYER");
				var connection = (ReplayConnection)orderManager.Connection;
				var replayNetTicks = connection.TickCount;

				var background = widget.Parent.GetOrNull("OBSERVER_CONTROL_BG");
				if (background != null)
					background.Bounds.Height += container.Bounds.Height;

				container.Visible = true;
				// index into Speeds; 1 is the 1x row
				var speed = 1;
				var originalTimestep = world.Timestep;

				// In the event the replay goes out of sync, it becomes no longer usable. For polish we permanently pause the world.
				bool IsWidgetDisabled() => orderManager.IsOutOfSync || orderManager.NetFrameNumber >= replayNetTicks;

				var pauseButton = widget.Get<ButtonWidget>("BUTTON_PAUSE");
				pauseButton.IsVisible = () => world.ReplayTimestep != 0 && !IsWidgetDisabled();
				pauseButton.OnClick = () => world.ReplayTimestep = 0;

				var playButton = widget.Get<ButtonWidget>("BUTTON_PLAY");
				playButton.IsVisible = () => world.ReplayTimestep == 0 || IsWidgetDisabled();
				playButton.OnClick = () => world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * Speeds[speed].Timestep);
				playButton.IsDisabled = IsWidgetDisabled;

				for (var i = 0; i < Speeds.Length; i++)
				{
					var index = i;                          // capture per iteration, not the loop var
					var button = widget.GetOrNull<ButtonWidget>(Speeds[i].Button);
					if (button == null)
						continue;                           // a chrome that omits a step still works

					button.IsHighlighted = () => speed == index;
					button.IsDisabled = IsWidgetDisabled;
					button.OnClick = () =>
					{
						speed = index;
						if (world.ReplayTimestep != 0)
							world.ReplayTimestep = (int)Math.Ceiling(originalTimestep * Speeds[index].Timestep);
					};
				}
			}
		}
	}
}
