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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Mods.Cameo.Orders;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cameo.Widgets
{
	/// <summary>Contains all functions that are unit-specific.</summary>
	public class CustomFormationsCommandBarLogic : ChromeLogic
	{
		readonly World world;

		int selectionHash;
		Actor[] selectedActors = Array.Empty<Actor>();
		bool attackMoveDisabled = true;
		bool forceMoveDisabled = true;
		bool forceAttackDisabled = true;
		bool guardDisabled = true;
		bool scatterDisabled = true;
		bool stopDisabled = true;
		bool waypointModeDisabled = true;
		bool deployDisabled = true;

		int deployHighlighted;
		int scatterHighlighted;
		int stopHighlighted;

		//readonly Type AttackMoveOrderGeneratorType;

		TraitPair<IIssueDeployOrder>[] selectedDeploys = Array.Empty<TraitPair<IIssueDeployOrder>>();

		[ObjectCreator.UseCtor]
		public CustomFormationsCommandBarLogic(Widget widget, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;
			//world.ToggleInputMode
			//AttackMoveOrderGeneratorType = modData.ObjectCreator.FindType(modData.Manifest.DefaultOrderGenerator);

			var highlightOnButtonPress = false;
			if (logicArgs.TryGetValue("HighlightOnButtonPress", out var entry))
				highlightOnButtonPress = FieldLoader.GetValue<bool>("HighlightOnButtonPress", entry.Value);

			var attackMoveButton = widget.GetOrNull<ButtonWidget>("ATTACK_MOVE");
			if (attackMoveButton != null)
			{
				WidgetUtils.BindButtonIcon(attackMoveButton);

				attackMoveButton.IsDisabled = () => { UpdateStateIfNecessary(); return attackMoveDisabled; };
				attackMoveButton.IsHighlighted = () => world.OrderGenerator is CustomFormationsAttackMoveOrderGenerator;

				void Toggle(bool allowCancel)
				{
					if (attackMoveButton.IsHighlighted())
					{
						if (allowCancel)
							world.CancelInputMode();
					}
					else
						world.OrderGenerator = new CustomFormationsAttackMoveOrderGenerator(selectedActors, Game.Settings.Game.ResolveActionButton(MouseActionType.ConfirmOrder));
				}

				attackMoveButton.OnClick = () => UnderWorldLock(() => Toggle(true));
				attackMoveButton.OnKeyPress = _ => UnderWorldLock(() => Toggle(false));
			}

			var forceMoveButton = widget.GetOrNull<ButtonWidget>("FORCE_MOVE");
			if (forceMoveButton != null)
			{
				WidgetUtils.BindButtonIcon(forceMoveButton);

				forceMoveButton.IsDisabled = () => { UpdateStateIfNecessary(); return forceMoveDisabled; };
				forceMoveButton.IsHighlighted = () => !forceMoveButton.IsDisabled() && IsForceModifiersActive(Modifiers.Alt);
				forceMoveButton.OnClick = () => UnderWorldLock(() =>
				{
					if (forceMoveButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(world, Modifiers.Alt, true);
				});
			}

			var forceAttackButton = widget.GetOrNull<ButtonWidget>("FORCE_ATTACK");
			if (forceAttackButton != null)
			{
				WidgetUtils.BindButtonIcon(forceAttackButton);

				forceAttackButton.IsDisabled = () => { UpdateStateIfNecessary(); return forceAttackDisabled; };
				forceAttackButton.IsHighlighted = () => !forceAttackButton.IsDisabled() && IsForceModifiersActive(Modifiers.Ctrl)
					&& world.OrderGenerator is not CustomFormationsAttackMoveOrderGenerator;

				forceAttackButton.OnClick = () => UnderWorldLock(() =>
				{
					if (forceAttackButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(world, Modifiers.Ctrl, true);
				});
			}

			var guardButton = widget.GetOrNull<ButtonWidget>("GUARD");
			if (guardButton != null)
			{
				WidgetUtils.BindButtonIcon(guardButton);

				guardButton.IsDisabled = () => { UpdateStateIfNecessary(); return guardDisabled; };
				guardButton.IsHighlighted = () => world.OrderGenerator is GuardOrderGenerator;

				void Toggle(bool allowCancel)
				{
					if (guardButton.IsHighlighted())
					{
						if (allowCancel)
							world.CancelInputMode();
					}
					else
						world.OrderGenerator = new GuardOrderGenerator(world, selectedActors, "Guard", "guard");
				}

				guardButton.OnClick = () => UnderWorldLock(() => Toggle(true));
				guardButton.OnKeyPress = _ => UnderWorldLock(() => Toggle(false));
			}

			var scatterButton = widget.GetOrNull<ButtonWidget>("SCATTER");
			if (scatterButton != null)
			{
				WidgetUtils.BindButtonIcon(scatterButton);

				scatterButton.IsDisabled = () => { UpdateStateIfNecessary(); return scatterDisabled; };
				scatterButton.IsHighlighted = () => scatterHighlighted > 0;
				scatterButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						scatterHighlighted = 2;

					PerformKeyboardOrderOnSelection(a => new Order("Scatter", a, false));
				};

				scatterButton.OnKeyPress = ki => { scatterHighlighted = 2; scatterButton.OnClick(); };
			}

			var deployButton = widget.GetOrNull<ButtonWidget>("DEPLOY");
			if (deployButton != null)
			{
				WidgetUtils.BindButtonIcon(deployButton);

				deployButton.IsDisabled = () =>
				{
					UpdateStateIfNecessary();

					// Stage B (D-03): CanIssueDeployOrder dispatches a live trait on a selected actor the sim can
					// dispose; evaluate under a non-blocking world lock and keep last frame's result when mid-tick.
					if (!Game.TryEnterWorldReadLock())
						return deployDisabled;

					try
					{
						var queued = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
						deployDisabled = !selectedDeploys.Any(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued));
						return deployDisabled;
					}
					finally
					{
						Game.ExitWorldReadLock();
					}
				};

				deployButton.IsHighlighted = () => deployHighlighted > 0;
				deployButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						deployHighlighted = 2;

					var queued = Game.GetModifierKeys().HasModifier(Modifiers.Shift);
					PerformDeployOrderOnSelection(queued);
				};

				deployButton.OnKeyPress = ki => { deployHighlighted = 2; deployButton.OnClick(); };
			}

			var stopButton = widget.GetOrNull<ButtonWidget>("STOP");
			if (stopButton != null)
			{
				WidgetUtils.BindButtonIcon(stopButton);

				stopButton.IsDisabled = () => { UpdateStateIfNecessary(); return stopDisabled; };
				stopButton.IsHighlighted = () => stopHighlighted > 0;
				stopButton.OnClick = () =>
				{
					if (highlightOnButtonPress)
						stopHighlighted = 2;

					PerformKeyboardOrderOnSelection(a => new Order("Stop", a, false));
				};

				stopButton.OnKeyPress = ki => { stopHighlighted = 2; stopButton.OnClick(); };
			}

			var queueOrdersButton = widget.GetOrNull<ButtonWidget>("QUEUE_ORDERS");
			if (queueOrdersButton != null)
			{
				WidgetUtils.BindButtonIcon(queueOrdersButton);

				queueOrdersButton.IsDisabled = () => { UpdateStateIfNecessary(); return waypointModeDisabled; };
				queueOrdersButton.IsHighlighted = () => !queueOrdersButton.IsDisabled() && IsForceModifiersActive(Modifiers.Shift);
				queueOrdersButton.OnClick = () => UnderWorldLock(() =>
				{
					if (queueOrdersButton.IsHighlighted())
						world.CancelInputMode();
					else
						world.OrderGenerator = new ForceModifiersOrderGenerator(world, Modifiers.Shift, false);
				});
			}

			var keyOverrides = widget.GetOrNull<LogicKeyListenerWidget>("MODIFIER_OVERRIDES");
			if (keyOverrides != null)
			{
				var noShiftButtons = new[] { guardButton, deployButton, attackMoveButton };
				var keyUpButtons = new[] { guardButton, attackMoveButton };
				keyOverrides.AddHandler(e =>
				{
					// HACK: allow command buttons to be triggered if the shift (queue order modifier) key is held
					if (e.Modifiers.HasModifier(Modifiers.Shift))
					{
						var eNoShift = e;
						eNoShift.Modifiers &= ~Modifiers.Shift;

						foreach (var b in noShiftButtons)
						{
							// Button is not used by this mod
							if (b == null)
								continue;

							// Button is not valid for this event
							if (b.IsDisabled() || !b.Key.IsActivatedBy(eNoShift))
								continue;

							// Event is not valid for this button
							if (!(b.DisableKeyRepeat ^ e.IsRepeat) || (e.Event == KeyInputEvent.Up && !keyUpButtons.Contains(b)))
								continue;

							b.OnKeyPress(e);
							return true;
						}
					}

					// HACK: Attack move can be triggered if the ctrl (assault move modifier)
					// or shift (queue order modifier) keys are pressed, on both key down and key up
					var eNoMods = e;
					eNoMods.Modifiers &= ~(Modifiers.Ctrl | Modifiers.Shift);

					if (attackMoveButton != null && !attackMoveDisabled && attackMoveButton.Key.IsActivatedBy(eNoMods))
					{
						attackMoveButton.OnKeyPress(e);
						return true;
					}

					return false;
				});
			}
		}

		public override void Tick()
		{
			if (deployHighlighted > 0)
				deployHighlighted--;

			if (scatterHighlighted > 0)
				scatterHighlighted--;

			if (stopHighlighted > 0)
				stopHighlighted--;

			base.Tick();
		}

		bool IsForceModifiersActive(Modifiers modifiers)
		{
			if (world.OrderGenerator is ForceModifiersOrderGenerator fmog && fmog.Modifiers.HasFlag(modifiers))
				return true;

			return world.OrderGenerator is UnitOrderGenerator && Game.GetModifierKeys().HasFlag(modifiers);
		}

		static void UnderWorldLock(Action a)
		{
			// Stage B (D-03): run a one-shot command-bar callback under the blocking world lock so its OrderGenerator
			// swap / order issue can't race the sim thread's OrderGenerator.Tick. No-op when decoupling is off.
			Game.EnterWorldReadLock();
			try { a(); }
			finally { Game.ExitWorldReadLock(); }
		}

		void UpdateStateIfNecessary()
		{
			// Stage B (D-03): enumerates live Selection actors + their traits (TraitsImplementing hits CheckDestroyed
			// on a disposed actor). Runs per frame (button IsDisabled during unlocked Ui.Draw) and from one-shot order
			// callbacks. Guard with a non-blocking world lock: keep last frame's cached state when the sim is mid-tick.
			// One-shot callers hold the blocking world lock, so this re-enters it (Monitor re-entrant) and refreshes.
			if (!Game.TryEnterWorldReadLock())
				return;

			try
			{
			if (selectionHash == world.Selection.Hash)
				return;

			selectedActors = world.Selection.Actors
				.Where(a => a.Owner == world.LocalPlayer && a.IsInWorld && !a.IsDead)
				.ToArray();

			attackMoveDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<AttackMoveInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			guardDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<GuardInfo>() && a.Info.HasTraitInfo<AutoTargetInfo>());
			forceMoveDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<MobileInfo>() || a.Info.HasTraitInfo<AircraftInfo>());
			forceAttackDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<AttackBaseInfo>());
			scatterDisabled = !selectedActors.Any(a => a.Info.HasTraitInfo<IMoveInfo>());

			selectedDeploys = selectedActors
				.SelectMany(a => a.TraitsImplementing<IIssueDeployOrder>()
					.Select(d => new TraitPair<IIssueDeployOrder>(a, d)))
				.ToArray();

			var cbbInfos = selectedActors.Select(a => a.Info.TraitInfoOrDefault<CommandBarBlacklistInfo>()).ToArray();
			stopDisabled = !cbbInfos.Any(i => i == null || !i.DisableStop);
			waypointModeDisabled = !cbbInfos.Any(i => i == null || !i.DisableWaypointMode);

			selectionHash = world.Selection.Hash;
			}
			finally
			{
				Game.ExitWorldReadLock();
			}
		}

		void PerformKeyboardOrderOnSelection(Func<Actor, Order> f)
		{
			// Stage B (D-03): one-shot order issue under the blocking world lock (UpdateStateIfNecessary re-enters it).
			Game.EnterWorldReadLock();
			try
			{
				UpdateStateIfNecessary();

				var orders = selectedActors
					.Select(f)
					.ToArray();

				foreach (var o in orders)
					world.IssueOrder(o);

				orders.PlayVoiceForOrders();
			}
			finally
			{
				Game.ExitWorldReadLock();
			}
		}

		void PerformDeployOrderOnSelection(bool queued)
		{
			// Stage B (D-03): one-shot deploy issue under the blocking world lock (see above).
			Game.EnterWorldReadLock();
			try
			{
				UpdateStateIfNecessary();

				var orders = selectedDeploys
					.Where(pair => pair.Trait.CanIssueDeployOrder(pair.Actor, queued))
					.Select(d => d.Trait.IssueDeployOrder(d.Actor, queued))
					.Where(d => d != null)
					.ToArray();

				foreach (var o in orders)
					world.IssueOrder(o);

				orders.PlayVoiceForOrders();
			}
			finally
			{
				Game.ExitWorldReadLock();
			}
		}
	}
}
