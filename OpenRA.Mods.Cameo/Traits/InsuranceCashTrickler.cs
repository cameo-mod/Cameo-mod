using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Traits
{
	public sealed class InsuranceCashTricklerInfo : CashTricklerInfo
	{
		public override object Create(ActorInitializer init) => new InsuranceCashTrickler(this);
	}

	public sealed class InsuranceCashTrickler : CashTrickler
	{
		readonly bool fake;
		public InsuranceCashTrickler(InsuranceCashTricklerInfo info) : base(info) { fake = info.Fake; }
		public override void ModifyCash(Actor self, int amount)
		{
			var resources = self.Owner.PlayerActor.Trait<PlayerResources>();
			var before = (long)resources.Cash + resources.Resources;
			base.ModifyCash(self, amount); // Preserve modifiers, refund, storage, XP and UI semantics.
			if (!fake && amount > 0)
				InsuranceTelemetry.Payout(self, "legacy_secondaryinsurance", amount, before,
					(long)resources.Cash + resources.Resources);
		}
	}
}
