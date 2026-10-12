from pathlib import Path
p=Path(r'C:/cameo-wt/codex-rereview-econ-a/review-results/probe/Program.cs')
s=p.read_text().replace('using OpenRA.Mods.Common.Traits;', 'using OpenRA.Mods.Common.Traits;\nusing OpenRA.Mods.Common.Activities;\nusing System.Collections.Frozen;')
needle='Console.WriteLine($"hidden disposal, frozen snapshot unchanged: valid={frozen.IsValid}, visible={frozen.Visible}, footprint={frozen.Footprint.Length}, blocked_cells={Count()}");'
s=s.replace(needle, needle+'''
// Actual demand sweep: one-shot deployment / cleanup, then targetless non-idle redirection.
var info=new BaseBuilderBotModuleCAInfo();Set(info,"ConstructionYardTypes",new[]{"yard"}.ToFrozenSet());Set(module,"Info",info);
var travelers=new Dictionary<Actor,ExpansionDemand>();var requests=new Dictionary<Actor,(CPos,CPos)>();Set(module,"ExpansionDemands",travelers);Set(module,"RequestedRefineries",requests);
var bf=typeof(BaseBuilderBotModuleCA).GetField("builders",BindingFlags.Instance|BindingFlags.NonPublic)!;var bt=bf.FieldType.GetElementType()!;var builders=Array.CreateInstance(bt,1);var builder=Blank(bt);builders.SetValue(builder,0);bf.SetValue(module,builders);
var sweep=typeof(BaseBuilderBotModuleCA).GetMethod("SweepExpansionDemands",BindingFlags.Instance|BindingFlags.NonPublic)!;
var traveler=Blank<Actor>();Set(traveler,"World",world);Set(traveler,"ActorID",(uint)7);Set(traveler,"<Owner>k__BackingField",player);Set(traveler,"<IsInWorld>k__BackingField",true);
var yard=Blank<Actor>();Set(yard,"World",world);Set(yard,"ActorID",(uint)8);Set(yard,"Info",new ActorInfo("yard"));Set(yard,"<Owner>k__BackingField",player);Set(yard,"<IsInWorld>k__BackingField",true);Set(yard,"<OccupiesSpace>k__BackingField",new Space());traveler.ReplacedByActor=yard;
var demand=new ExpansionDemand(traveler);travelers[traveler]=demand;requests[traveler]=(new CPos(8,8),new CPos(9,9));
foreach(var tick in new[]{100,101,200,1000}) {Set(world,"<WorldTick>k__BackingField",tick);Set(builder,"WaitTicks",50);sweep.Invoke(module,new object[]{null});Console.WriteLine($"deploy tick={tick} expiry={demand.ExpiresTick} demand_count={travelers.Count} wait={bt.GetField("WaitTicks").GetValue(builder)}");}
traveler.ReplacedByActor=null;traveler.AddTrait(mobile);Set(traveler,"currentActivity",new WaitFor(()=>false));
demand=new ExpansionDemand(traveler){ExpiresTick=200,NextEtaTick=int.MaxValue,ConyardLoc=new CPos(8,8)};travelers[traveler]=demand;requests[traveler]=(new CPos(8,8),new CPos(9,9));
foreach(var tick in new[]{100,200,1000}) {Set(world,"<WorldTick>k__BackingField",tick);sweep.Invoke(module,new object[]{null});Console.WriteLine($"targetless WaitFor tick={tick} idle={traveler.IsIdle} expiry={demand.ExpiresTick} demand_count={travelers.Count}");}
''')
s='class Space : IOccupySpace { public CPos TopLeft=>new(8,8); public WPos CenterPosition=>new(0,0,0); public (CPos,SubCell)[] OccupiedCells()=>[]; }\n'.join(s.rsplit('class Program {',1)) if False else s.replace('class Program {','class Space : IOccupySpace { public CPos TopLeft=>new(8,8); public WPos CenterPosition=>new(0,0,0); public (CPos,SubCell)[] OccupiedCells()=>[]; }\nclass Program {')
p.write_text(s)
