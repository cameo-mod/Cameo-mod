#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class RecoverySpendOwnerBotModuleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) => new RecoverySpendOwnerBotModule(this);
	}

	// Persistence/ownership skeleton only: admission and receipt integration are
	// intentionally absent until the shared engine receipt is available.
	public class RecoverySpendOwnerBotModule : ConditionalTrait<RecoverySpendOwnerBotModuleInfo>,
		IBotRecoverySpendOwner, IGameSaveTraitData, INotifyOtherProduction
	{
		const string StateKey = "RecoverySpendState";
		readonly RecoverySpendLedgerCA ledger = new();
		OpenRA.Player player;
		RecoveryEssentialPlannerCA planner;
		bool hasPersistentState;
		bool faulted;

		public RecoverySpendOwnerBotModule(RecoverySpendOwnerBotModuleInfo info)
			: base(info) { }

		RecoverySpendLedgerCA IBotRecoverySpendOwner.Ledger => ledger;
		bool IBotRecoverySpendOwner.Faulted => faulted;

		void INotifyOtherProduction.UnitProducedByOther(Actor self, Actor producer, Actor produced, string productionType, TypeDictionary init)
		{
			// Check ownership before inspecting any actor details. Replayed history
			// is followed by the authoritative saved owner snapshot.
			if (faulted || self.World.IsLoadingGameSave || produced == null || produced.Owner != self.Owner)
				return;

			ledger.NoteProduced(produced.Info.Name, produced.ActorID, self.World.WorldTick);
		}

		protected override void Created(Actor self)
		{
			player = self.Owner;
			base.Created(self);
		}

		(ProductionQueue Queue, ActorInfo Item, RecoverySpendPolicyCA.EssentialLane Lane)?
			IBotRecoverySpendOwner.SelectEssential(int minimumExcessPower)
		{
			if (IsTraitDisabled || faulted || player == null)
				return null;

			var baseBuilder = player.PlayerActor.TraitsImplementing<BaseBuilderBotModuleCA>().FirstEnabledTraitOrDefault();
			if (baseBuilder == null)
				return null;

			planner ??= new RecoveryEssentialPlannerCA(player, baseBuilder.Info);
			return planner.Select(ledger.OutstandingEntries(), minimumExcessPower);
		}

		List<MiniYamlNode> IGameSaveTraitData.IssueTraitData(Actor self)
		{
			if (IsTraitDisabled && !hasPersistentState && !ledger.HasState)
				return null;

			// A fault never serializes a fresh empty ledger over unresolved coverage.
			if (faulted)
				throw new InvalidOperationException("Cannot save an invalid recovery spend state.");

			hasPersistentState = true;
			var bytes = Encoding.UTF8.GetBytes(ledger.SaveText());
			return [new MiniYamlNode(StateKey, Convert.ToBase64String(bytes))];
		}

		void IGameSaveTraitData.ResolveTraitData(Actor self, MiniYaml data)
		{
			if (self.World.IsReplay)
				return;

			var node = data.NodeWithKeyOrDefault(StateKey);
			if (node == null)
				return;

			hasPersistentState = true;
			try
			{
				var text = Encoding.UTF8.GetString(Convert.FromBase64String(node.Value.Value));
				faulted = !ledger.RestoreText(text);
			}
			catch (FormatException)
			{
				faulted = true;
			}
		}
	}
}
