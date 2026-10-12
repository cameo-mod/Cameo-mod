from pathlib import Path
import json
p=Path('tools/audit/fog_honesty_manifest.json');j=json.loads(p.read_text(encoding='utf-8-sig'));j['OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs']=9;p.write_text(json.dumps(j,indent=2)+'\n',encoding='utf-8')
p=Path('tools/audit/fog_canary_manifest.json');j=json.loads(p.read_text(encoding='utf-8-sig'));m=j['OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs'];m['squad-desire-visible-targets']=1;m['squad-desire-integer-ratio']=1;j['OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs']=dict(sorted(m.items()));p.write_text(json.dumps(j,indent=2)+'\n',encoding='utf-8')
license=Path('OpenRA.Mods.CA/Traits/BotModules/Squads/SquadDesireEval.cs').read_text(encoding='utf-8-sig').split('using System;')[0]
for name in ['OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/IntegerCombatPredictor.cs','OpenRA.Mods.CA/Traits/BotModules/Squads/SquadDesireOrders.cs','OpenRA.Mods.CA/Traits/BotModules/Squads/SquadDesireMemory.cs','OpenRA.Mods.Cameo.Test/SquadDesireRegressionTest.cs']:
 p=Path(name);s=p.read_text(encoding='utf-8-sig');p.write_text(license+s,encoding='utf-8')
