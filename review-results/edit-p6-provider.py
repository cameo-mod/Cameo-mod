from pathlib import Path
p=Path('OpenRA.Mods.Cameo/Traits/BotModules/SquadDesireBotModule.cs')
s=p.read_text(encoding='utf-8-sig')
a=s.index('\t\tsealed class DesireState'); b=s.index('\t\treadonly World world;',a)
s=s[:a]+s[b:]
s=s.replace('Dictionary<SquadCA, DesireState>', 'Dictionary<SquadCA, SquadDesireMemory>')
s=s.replace('\t\treadonly int[] urgencies = new int[SquadDesireEval.StanceCount];\n','')
a=s.index('\t\t\tif (!states.TryGetValue'); b=s.index('\n\t\tvoid IBotTick.BotTick',a)
s=s[:a]+'''\t\t\tif (!states.TryGetValue(squad, out var state))
				states[squad] = state = new SquadDesireMemory();
			return state.Evaluate(in signals, Personality, Info.LeakPermille,
				Info.HysteresisMilli, Info.MinDwellTicks, Info.BiasCapMilli);
		}
'''+s[b:]
p.write_text(s,encoding='utf-8')
p=Path('OpenRA.Mods.CA/Traits/BotModules/IBotSquadDesire.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('one per squad per eval; the P6 consumer maps every non-Attack pick onto the classic\n\t/// not-engage path, so this list is the decision vocabulary, not a state machine.', 'one per squad per eval. The ground squad controller executes six distinct routes;\n\t/// an independent integer combat guard blocks unsafe attacks immediately.')
s=s.replace('The shared combat predictor\'s own/enemy ratio x1000', 'The integer rules-derived combat predictor\'s own/enemy ratio x1000')
p.write_text(s,encoding='utf-8')
