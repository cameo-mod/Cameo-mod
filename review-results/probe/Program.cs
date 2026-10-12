using System.Reflection;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Mods.CA.Traits;
class Space : IOccupySpace { public CPos TopLeft => new CPos(8,8); public WPos CenterPosition => new WPos(0,0,0); public (CPos, SubCell)[] OccupiedCells() => []; }
class Program {
static void Set(object o,string n,object v) { for(var t=o.GetType(); t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly); if(f!=null){f.SetValue(o,v);return;}} throw new Exception(n); }
static object Blank(Type t)=>RuntimeHelpers.GetUninitializedObject(t);
static void Main(){
var module=(BaseBuilderBotModuleCA)Blank(typeof(BaseBuilderBotModuleCA));
var info=new BaseBuilderBotModuleCAInfo(); Set(info,"ConstructionYardTypes",new[]{"yard"}.ToFrozenSet()); Set(module,"Info",info);
var world=(World)Blank(typeof(World)); Set(module,"world",world);
var mcv=(Actor)Blank(typeof(Actor)); Set(mcv,"ActorID",(uint)7);
var yard=(Actor)Blank(typeof(Actor)); Set(yard,"Info",new ActorInfo("yard")); Set(yard,"<OccupiesSpace>k__BackingField",new Space()); mcv.ReplacedByActor=yard;
var demand=new ExpansionDemand(mcv);
var demands=new Dictionary<Actor,ExpansionDemand>{{mcv,demand}}; Set(module,"ExpansionDemands",demands);
Set(module,"RequestedRefineries",new Dictionary<Actor,(CPos,CPos)>{{mcv,(new CPos(8,8),new CPos(9,9))}});
var bf=module.GetType().GetField("builders",BindingFlags.NonPublic|BindingFlags.Instance);var bt=bf.FieldType.GetElementType();var builders=Array.CreateInstance(bt,1);var builder=Blank(bt);builders.SetValue(builder,0);bf.SetValue(module,builders);
var sweep=module.GetType().GetMethod("SweepExpansionDemands",BindingFlags.NonPublic|BindingFlags.Instance);
foreach(var tick in new[]{100,101,200,1000}) {Set(world,"<WorldTick>k__BackingField",tick);Set(builder,"WaitTicks",50);sweep.Invoke(module,new object[]{null}); Console.WriteLine($"tick={tick} deployed={demand.Deployed} expires={demand.ExpiresTick} demands={demands.Count} wait={bt.GetField("WaitTicks").GetValue(builder)}");}
Set(yard,"<Disposed>k__BackingField",true); Set(world,"<WorldTick>k__BackingField",1001); sweep.Invoke(module,new object[]{null}); Console.WriteLine($"Disposed deployed yard: expires={demand.ExpiresTick} demands={demands.Count}");
demand.BindRefinery("refinery",yard,1000); Console.WriteLine($"Same producer/name maps arbitrary queue items to demand: {ReferenceEquals(module.DemandForQueuedItem("refinery",yard),demand)}");
var reverse=new[]{new WPos(10240,0,0),new WPos(5120,0,0),new WPos(0,0,0)};long distance=0;var previous=new WPos(0,0,0);foreach(var p in reverse){distance+=(p-previous).Length;previous=p;}Console.WriteLine($"Returned target->source straight path: actualDistance=10240 measuredDistance={distance} travelAtSpeed100={BaseBuilderQueueEvalCA.ExpansionTravelTicks(distance,100)} expected=102");
}
}



