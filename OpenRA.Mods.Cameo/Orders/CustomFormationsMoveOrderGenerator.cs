using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Cameo.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Orders
{
	// Only ever instantiated while Game.Settings.Game.AttackMoveIsDefault is true (see
	// CustomFormationsCommandBarLogic.Toggle) - CustomFormationsAttackMoveOrderGenerator itself
	// stays completely untouched and is used as before when the setting is off, so the off state
	// never runs any of this class's code at all.
	//
	// This is a sibling of CustomFormationsAttackMoveOrderGenerator, not a subclass of it, mirroring
	// the engine's own AttackMoveOrderGenerator/MoveOrderGenerator split - deliberately duplicating
	// the marker-loading/cursor scaffolding (the same style CustomFormationsAttackMoveOrderGenerator
	// and CustomFormationsUnitOrderGenerator already duplicate between themselves) rather than
	// inheriting, so the off-path class needs no virtual hooks added for this to plug into.
	public enum ECustomFormationsMoveMode
	{
		None,
		Move,
		AssaultMove,
		Max
	}

	public class CustomFormationsMoveOrderGenerator : CustomFormationsOrderGeneratorBase
	{
		TraitPair<AttackMove>[] subjects;

		// Actors that can be plain-moved but don't have AttackMove (e.g. a harvester/MCV in a
		// mixed selection) - only used for the Move cursor preview when subjects is empty, since
		// order dispatch itself (TapOrder/GetCommandLineOrders) already goes through the full,
		// live OwningWorld.Selection.Actors regardless of this field.
		Actor[] moveSubjects;

		protected override MouseActionType ActionType => MouseActionType.ConfirmOrder;

		readonly MouseButton expectedButton;

		protected ECustomFormationsMoveMode CurrentMoveMode = ECustomFormationsMoveMode.None;

		protected PaletteReference MoveLineOrderTileMarkerPalette = null;
		protected ISpriteSequence MoveLineOrderTileMarkerSequence = null;
		protected Sprite MoveLineOrderTileMarkerSprite = null;
		protected Animation MoveLineOrderTileMarkerAnimation = null;
		protected bool bMoveLineOrderTileMarkerAnimates = false;

		protected PaletteReference MoveLineOrderMarkerPalette = null;
		protected ISpriteSequence MoveLineOrderMarkerSequence = null;
		protected Sprite MoveLineOrderMarkerSprite = null;
		protected Animation MoveLineOrderMarkerAnimation = null;
		protected bool bMoveLineOrderMarkerAnimates = false;

		protected PaletteReference AssaultMoveLineOrderTileMarkerPalette = null;
		protected ISpriteSequence AssaultMoveLineOrderTileMarkerSequence = null;
		protected Sprite AssaultMoveLineOrderTileMarkerSprite = null;
		protected Animation AssaultMoveLineOrderTileMarkerAnimation = null;
		protected bool bAssaultMoveLineOrderTileMarkerAnimates = false;

		protected PaletteReference AssaultMoveLineOrderMarkerPalette = null;
		protected ISpriteSequence AssaultMoveLineOrderMarkerSequence = null;
		protected Sprite AssaultMoveLineOrderMarkerSprite = null;
		protected Animation AssaultMoveLineOrderMarkerAnimation = null;
		protected bool bAssaultMoveLineOrderMarkerAnimates = false;

		public CustomFormationsMoveOrderGenerator(IEnumerable<Actor> subjects, MouseButton button)
		{
			expectedButton = button;

			var aliveSubjects = subjects.Where(a => !a.IsDead).ToArray();

			this.subjects = aliveSubjects
				.SelectMany(a => a.TraitsImplementing<AttackMove>()
					.Select(am => new TraitPair<AttackMove>(a, am)))
				.ToArray();

			moveSubjects = aliveSubjects
				.Where(a => a.Info.HasTraitInfo<IMoveInfo>())
				.ToArray();
		}

		// Alt always wins here too - matches the engine's AttackMoveOrderTargeter/
		// MoveOrderGenerator design, where ForceMove overrides everything else, including Ctrl -
		// but since this class only ever exists while the setting is on, there is no off-state
		// branch to guard here (unlike CustomFormationsAttackMoveOrderGenerator, which this
		// class never touches).
		static ECustomFormationsMoveMode ModeForModifiers(Modifiers modifiers)
		{
			if (modifiers.HasModifier(Modifiers.Ctrl) && !modifiers.HasModifier(Modifiers.Alt))
				return ECustomFormationsMoveMode.AssaultMove;

			return ECustomFormationsMoveMode.Move;
		}

		static string OrderNameForMode(ECustomFormationsMoveMode mode)
		{
			return mode == ECustomFormationsMoveMode.AssaultMove ? "AssaultMove" : "Move";
		}

		// AttackMoveInfo has no "plain move" cursor of its own - unlike AssaultMove, Move isn't
		// owned by a single trait, so look it up the same way Mobile/Aircraft's own targeters do,
		// falling back to the universal "move"/"move-blocked" cursor names.
		static string MoveCursorFor(Actor actor, bool blocked)
		{
			var mobile = actor.Info.TraitInfoOrDefault<MobileInfo>();
			if (mobile != null)
				return blocked ? mobile.BlockedCursor : mobile.Cursor;

			var aircraft = actor.Info.TraitInfoOrDefault<AircraftInfo>();
			if (aircraft != null)
				return blocked ? aircraft.BlockedCursor : aircraft.Cursor;

			return blocked ? "move-blocked" : "move";
		}

		public override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			var mode = ModeForModifiers(mi.Modifiers);

			if (bInitialized && (CurrentMode == ECustomFormationsMode.CommandLine))
			{
				ChangeMoveMode(mode);
			}

			if (mode == ECustomFormationsMoveMode.AssaultMove)
			{
				var subject = subjects.FirstOrDefault();
				if (subject.Actor != null)
				{
					var info = subject.Trait.Info;
					if (world.Map.Contains(cell))
					{
						var explored = subject.Actor.Owner.Shroud.IsExplored(cell);
						var cannotMove = subjects.FirstOrDefault(a => !a.Trait.Info.MoveIntoShroud).Trait;
						var blocked = !explored && cannotMove != null;
						return blocked ? cannotMove.Info.AssaultMoveBlockedCursor : info.AssaultMoveCursor;
					}

					return info.AssaultMoveBlockedCursor;
				}

				// No actor with AttackMove selected - AssaultMove has nothing to preview.
				return null;
			}

			var moveSubject = moveSubjects.FirstOrDefault();
			if (moveSubject != null)
			{
				var blocked = world.Map.Contains(cell) && !moveSubject.Owner.Shroud.IsExplored(cell);
				return MoveCursorFor(moveSubject, blocked);
			}

			return null;
		}

		protected override bool OnInitialize(WorldRenderer inWorldRenderer, World inWorld)
		{
			if (base.OnInitialize(inWorldRenderer, inWorld))
			{
				CustomFormationsModOptions modoptions = OwningWorld.LocalPlayer.PlayerActor.Trait<CustomFormationsModOptions>();

				bMoveLineOrderTileMarkerAnimates = modoptions.PlaysMoveOrderTileMarkerAnimation;
				MoveLineOrderTileMarkerPalette = OwningWorldRenderer.Palette(modoptions.MoveOrderTileMarkerPaletteName);
				MoveLineOrderTileMarkerSequence = OwningWorld.Map.Sequences.GetSequence(modoptions.MoveOrderTileMarkerImageName, modoptions.MoveOrderTileMarkerSequenceName);
				if (bMoveLineOrderTileMarkerAnimates)
				{
					MoveLineOrderTileMarkerAnimation = new Animation(OwningWorld, modoptions.MoveOrderTileMarkerImageName);
				}
				else
				{
					MoveLineOrderTileMarkerSprite = MoveLineOrderTileMarkerSequence.GetSprite(0);
				}

				bMoveLineOrderMarkerAnimates = modoptions.PlaysMoveOrderMarkerAnimation;
				MoveLineOrderMarkerPalette = OwningWorldRenderer.Palette(modoptions.MoveOrderMarkerPaletteName);
				MoveLineOrderMarkerSequence = OwningWorld.Map.Sequences.GetSequence(modoptions.MoveOrderMarkerImageName, modoptions.MoveOrderMarkerSequenceName);
				if (bMoveLineOrderMarkerAnimates)
				{
					MoveLineOrderMarkerAnimation = new Animation(OwningWorld, modoptions.MoveOrderMarkerImageName);
				}
				else
				{
					MoveLineOrderMarkerSprite = MoveLineOrderMarkerSequence.GetSprite(0);
				}

				bAssaultMoveLineOrderTileMarkerAnimates = modoptions.PlaysAssaultMoveOrderTileMarkerAnimation;
				AssaultMoveLineOrderTileMarkerPalette = OwningWorldRenderer.Palette(modoptions.AssaultMoveOrderTileMarkerPaletteName);
				AssaultMoveLineOrderTileMarkerSequence = OwningWorld.Map.Sequences.GetSequence(modoptions.AssaultMoveOrderTileMarkerImageName, modoptions.AssaultMoveOrderTileMarkerSequenceName);
				if (bAssaultMoveLineOrderTileMarkerAnimates)
				{
					AssaultMoveLineOrderTileMarkerAnimation = new Animation(OwningWorld, modoptions.AssaultMoveOrderTileMarkerImageName);
				}
				else
				{
					AssaultMoveLineOrderTileMarkerSprite = AssaultMoveLineOrderTileMarkerSequence.GetSprite(0);
				}

				bAssaultMoveLineOrderMarkerAnimates = modoptions.PlaysAssaultMoveOrderMarkerAnimation;
				AssaultMoveLineOrderMarkerPalette = OwningWorldRenderer.Palette(modoptions.AssaultMoveOrderMarkerPaletteName);
				AssaultMoveLineOrderMarkerSequence = OwningWorld.Map.Sequences.GetSequence(modoptions.AssaultMoveOrderMarkerImageName, modoptions.AssaultMoveOrderMarkerSequenceName);
				if (bAssaultMoveLineOrderMarkerAnimates)
				{
					AssaultMoveLineOrderMarkerAnimation = new Animation(OwningWorld, modoptions.AssaultMoveOrderMarkerImageName);
				}
				else
				{
					AssaultMoveLineOrderMarkerSprite = AssaultMoveLineOrderMarkerSequence.GetSprite(0);
				}

				ChangeMoveMode(ECustomFormationsMoveMode.Move);

				return true;
			}

			return false;
		}

		protected override IEnumerable<Order> OnLMBDown(CPos inCell, int2 inWorldPixel, MouseInput inMouseInput)
		{
			if (expectedButton == MouseButton.Left)
			{
				StartCommandLine(inCell);
			}

			yield break;
		}

		protected override IEnumerable<Order> OnLMBUp(CPos inCell, int2 inWorldPixel, MouseInput inMouseInput)
		{
			if (expectedButton != MouseButton.Left)
			{
				OwningWorld.CancelInputMode();
			}
			else
			{
				string orderName = OrderNameForMode(ModeForModifiers(inMouseInput.Modifiers));

				if (IsTap)
				{
					IEnumerable<Order> results = TapOrder(inCell, inWorldPixel, inMouseInput, orderName);
					foreach (Order o in results)
					{
						yield return o;
					}
				}
				else
				{
					IEnumerable<Order> orders = EndCommandLine(inCell, inWorldPixel, inMouseInput, orderName);
					foreach (Order o in orders)
					{
						yield return o;
					}
				}

				if (!inMouseInput.Modifiers.HasModifier(Modifiers.Shift))
				{
					OwningWorld.CancelInputMode();
				}
			}

			yield break;
		}

		protected override IEnumerable<Order> OnRMBDown(CPos inCell, int2 inWorldPixel, MouseInput inMouseInput)
		{
			if (expectedButton == MouseButton.Right)
			{
				StartCommandLine(inCell);
			}
			yield break;
		}

		protected override IEnumerable<Order> OnRMBUp(CPos inCell, int2 inWorldPixel, MouseInput inMouseInput)
		{
			if (expectedButton != MouseButton.Right)
			{
				OwningWorld.CancelInputMode();
			}
			else
			{
				string orderName = OrderNameForMode(ModeForModifiers(inMouseInput.Modifiers));

				if (IsTap)
				{
					IEnumerable<Order> results = TapOrder(inCell, inWorldPixel, inMouseInput, orderName);
					foreach (Order o in results)
					{
						yield return o;
					}
				}
				else
				{
					IEnumerable<Order> orders = EndCommandLine(inCell, inWorldPixel, inMouseInput, orderName);
					foreach (Order o in orders)
					{
						yield return o;
					}
				}

				if (!inMouseInput.Modifiers.HasModifier(Modifiers.Shift))
				{
					OwningWorld.CancelInputMode();
				}
			}
		}

		protected void ChangeMoveMode(ECustomFormationsMoveMode mode)
		{
			if (CurrentMoveMode == mode)
			{
				return;
			}

			CurrentMoveMode = mode;

			if (mode == ECustomFormationsMoveMode.AssaultMove)
			{
				LineOrderTileMarkerSequence = AssaultMoveLineOrderTileMarkerSequence;
				LineOrderTileMarkerSprite = AssaultMoveLineOrderTileMarkerSprite;
				LineOrderTileMarkerPalette = AssaultMoveLineOrderTileMarkerPalette;
				LineOrderTileMarkerAnimation = AssaultMoveLineOrderTileMarkerAnimation;
				bLineOrderTileMarkerAnimates = bAssaultMoveLineOrderTileMarkerAnimates;

				LineOrderMarkerSequence = AssaultMoveLineOrderMarkerSequence;
				LineOrderMarkerSprite = AssaultMoveLineOrderMarkerSprite;
				LineOrderMarkerPalette = AssaultMoveLineOrderMarkerPalette;
				LineOrderMarkerAnimation = AssaultMoveLineOrderMarkerAnimation;
				bLineOrderMarkerAnimates = bAssaultMoveLineOrderMarkerAnimates;
			}
			else
			{
				LineOrderTileMarkerSequence = MoveLineOrderTileMarkerSequence;
				LineOrderTileMarkerSprite = MoveLineOrderTileMarkerSprite;
				LineOrderTileMarkerPalette = MoveLineOrderTileMarkerPalette;
				LineOrderTileMarkerAnimation = MoveLineOrderTileMarkerAnimation;
				bLineOrderTileMarkerAnimates = bMoveLineOrderTileMarkerAnimates;

				LineOrderMarkerSequence = MoveLineOrderMarkerSequence;
				LineOrderMarkerSprite = MoveLineOrderMarkerSprite;
				LineOrderMarkerPalette = MoveLineOrderMarkerPalette;
				LineOrderMarkerAnimation = MoveLineOrderMarkerAnimation;
				bLineOrderMarkerAnimates = bMoveLineOrderMarkerAnimates;
			}

			if (bLineOrderTileMarkerAnimates && (CurrentMode == ECustomFormationsMode.CommandLine))
			{
				LineOrderTileMarkerAnimation.PlayRepeating(LineOrderTileMarkerSequence.Name);
			}

			if (bLineOrderMarkerAnimates && (CurrentMode == ECustomFormationsMode.CommandLine))
			{
				LineOrderMarkerAnimation.PlayRepeating(LineOrderMarkerSequence.Name);
			}

			LineOrderTileMarkerRenderableArray.Clear();
			LineOrderMarkerRenderableArray.Clear();
		}
	}
}
