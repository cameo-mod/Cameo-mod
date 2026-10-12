from pathlib import Path
p=Path('OpenRA.Mods.CA/Traits/Attack/AttackGarrisonedSP.cs');license=p.read_text(encoding='utf-8-sig').split('using System;')[0];p.write_text(license+'''namespace OpenRA.Mods.CA.Traits
{
	/// <summary>One-release alias of independent canonical engine fire stations.</summary>
	public class AttackGarrisonedSPInfo : OpenRA.Mods.AS.Traits.AttackGarrisonedInfo
	{
		// Accepted only while loading unmigrated legacy YAML; independent targeting is unconditional.
		public readonly bool PerPassengerTargeting = true;
		public override void RulesetLoaded(Ruleset rules, ActorInfo actor)
		{
			Log.Write("debug", $"{actor.Name}: AttackGarrisonedSP is deprecated; use AttackGarrisoned.");
			base.RulesetLoaded(rules, actor);
		}
	}
}
''',encoding='utf-8')
