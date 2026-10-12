from pathlib import Path
r=Path(r'C:/cameo-wt/codex-context-providers')
p=r/'docs/AGENT_WORKSPACE.md'
p.write_text(p.read_text().replace('\n\n| **gpt-6.1-sol**', '\n| **gpt-6.1-sol**'))
p=r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs'
p.write_text(p.read_text().replace('using System.Collections.Generic;\nusing System.Collections;', 'using System.Collections;\nusing System.Collections.Generic;'))
