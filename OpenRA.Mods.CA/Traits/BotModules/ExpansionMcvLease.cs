using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.CA.Traits
{
	/// <summary>Prebuild owns building demand; the expansion manager owns traveller orders.</summary>
	public static class ExpansionMcvLease
	{
		public const string IssuerOwner = nameof(McvExpansionManagerBotModule);
		const string DemandOwner = nameof(BaseBuilderBotModuleCA);

		public static bool Acquire(IBotUnitLeases leases, Actor mcv, int durationTicks)
		{
			if (leases == null)
				return true;

			var held = leases.LeaseOf(mcv);
			if (held is BotLease lease)
			{
				// Transfer is unconditional in the registry: check both owner and purpose first.
				// An emergency or any unrelated lease is never ours to hand over.
				if (lease.Purpose != BotLeasePurpose.McvExpansion)
					return false;
				if (lease.Owner == DemandOwner)
					return leases.Transfer(mcv, IssuerOwner, BotLeasePurpose.McvExpansion, durationTicks);
				if (lease.Owner != IssuerOwner)
					return false;
			}

			return leases.TryClaim(mcv, IssuerOwner, BotLeasePurpose.McvExpansion, durationTicks);
		}

		public static void Release(IBotUnitLeases leases, Actor mcv)
		{
			if (leases?.LeaseOf(mcv) is BotLease lease && lease.Purpose == BotLeasePurpose.McvExpansion
				&& (lease.Owner == IssuerOwner || lease.Owner == DemandOwner))
				leases.Release(mcv, lease.Owner);
		}
	}
}
