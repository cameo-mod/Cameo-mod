from pathlib import Path
p=Path('OpenRA.Mods.Cameo.Test/AreaDamageWarheadTest.cs');s=p.read_text(encoding='utf-8');s=s.replace('using NUnit.Framework;', 'using System.Linq;\nusing NUnit.Framework;\nusing OpenRA.GameRules;\nusing OpenRA.Mods.Common.Traits;\nusing OpenRA.Traits;');pos=s.index('\t\t[TestCase(2010')
s=s[:pos]+'''		sealed class TargetingProbe : AreaDamageWarhead
		{
			public int ActualVersus(Actor victim, HitShape shape) => DamageVersus(victim, shape, default);
		}
		[TestCase(-1)]
		[TestCase(0)]
		[TestCase(2000)]
		public void AutomaticTargetScoreUsesEffectiveDamageVersus(int heaviness)
		{
			var fixture = new OpenRA.Test.AttackGarrisonedTest.Fixture(dynamicPriority: true);
			var info = FieldLoader.Load<TargetingProbe>(new MiniYaml("", new[]
			{
				new MiniYamlNode("Heaviness", new MiniYaml(heaviness.ToString())),
				new MiniYamlNode("Versus", new MiniYaml("", new[]
				{
					new MiniYamlNode("Heavy", new MiniYaml("120")),
				})),
			}));
			((IRulesetLoaded<WeaponInfo>)info).RulesetLoaded(null, null);
			var shape = fixture.Tank.TraitsImplementing<HitShape>().Single();
			Assert.That(info.TargetingVersus(fixture.Tank, shape).CompareTo(
				new WeaponTargetScore(info.ActualVersus(fixture.Tank, shape), 1)), Is.Zero);
		}

'''+s[pos:];p.write_text(s,encoding='utf-8')
