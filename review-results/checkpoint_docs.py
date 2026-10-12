from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'OpenRA.Mods.Cameo.Test/ContextFeaturesTest.cs'
s=p.read_text();i=s.rfind('\t}\n}')
s=s[:i]+'''		[Test]
		public void ArmyRateDoesNotFlattenWhenRoleTotalsExceedInt32()
		{
			var window = new OpponentFeatureWindow();
			window.Add(new(0, 0, 4, 1000, 100, 100, new[] { int.MaxValue, int.MaxValue, 0, 0, 0, 0, 0 }));
			window.Add(new(750, 750, 4, 1000, 100, 100, new[] { int.MaxValue, int.MaxValue, 750, 0, 0, 0, 0 }));
			Assert.That(window.Features(750).Fields["army_value_slope"].Value, Is.EqualTo(1000));
		}
''' +s[i:]
p.write_text(s)
p=r/'tools/ai/audit_context_features.py'
s=p.read_text()
start=s.index('SCHEMAS = {');end=s.index('\n}\n',start)+2
# Format the closed vocabulary for review instead of a generated several-kilobyte line.
import ast
schemas=ast.literal_eval(s[start+len('SCHEMAS = '):end])
text='SCHEMAS = {\n'
for key,fields in schemas.items():
    text+=f'    {key!r}: (\n'+''.join(f'        {field!r},\n' for field in fields)+'    ),\n'
text+='}'
p.write_text(s[:start]+text+s[end:])
p=r/'docs/AGENT_WORKSPACE.md'
s=p.read_text()
row='| **gpt-6.1-sol** | GPT-6.1 Sol, Team slot 01a11068 | `codex/context-providers` | new context schemas, pure public-map graph/window/encoder, context audit and focused tests; BX telemetry adapter after reviewed P0 lands | ACTIVE 2026-10-06; local core checkpoint, final runtime gates pending |\n'
marker='\n**Signing rule'
s=s.replace(marker,'\n'+row+marker)
p.write_text(s)
