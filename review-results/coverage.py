from pathlib import Path
import re
r=Path(r'C:/cameo-wt/codex-context-providers')
s=(r/'OpenRA.Mods.CA/Traits/BotModules/IBotMapFeatures.cs').read_text()
map_fields=set(re.findall(r'"([a-z_0-9]+)"',re.findall(r'Array.AsReadOnly\(new\[\]\s*\{(.*?)\}\);',s,re.S)[0]))
g=(r/'OpenRA.Mods.Cameo/Traits/BotModules/PublicMapFeatureGraph.cs').read_text()
measured=set(re.findall(r'(?:Set|Distance|ResourceShare)\("([a-z_0-9]+)"',g))
print('Map fields currently calculated',len(measured),'missing adapters:',sorted(map_fields-measured))
