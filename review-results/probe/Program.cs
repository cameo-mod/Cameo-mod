using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Activities;
using System.Collections.Frozen;
class Space : IOccupySpace { public CPos TopLeft=>new(8,8); public WPos CenterPosition=>new(0,0,0); public (CPos,SubCell)[] OccupiedCells()=>[]; }
class Program {
static object Blank(Type t)=>RuntimeHelpers.GetUninitializedObject(t);
static T Blank<T>()=>(T)Blank(typeof(T));
static void Set(object o,string n,object v) { for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,v);return;}}throw new Exception(n); }
static void Main(){
var world=Blank<World>(); Set(world,"TraitDict",Activator.CreateInstance(typeof(World).Assembly.GetType("OpenRA.TraitDictionary")!,true));
var map=Blank<Map>(); Set(map,"Grid",new MapGrid(new MiniYaml(""))); Set(map,"<AllCells>k__BackingField",new CellRegion(MapGridType.Rectangular,new CPos(0,0),new CPos(15,15))); Set(world,"Map",map);
var player=Blank<Player>(); var playerActor=Blank<Actor>(); Set(playerActor,"World",world);Set(playerActor,"ActorID",(uint)1);Set(player,"PlayerActor",playerActor);
var blocker=Blank<Actor>();Set(blocker,"World",world);Set(blocker,"ActorID",(uint)2);Set(blocker,"Info",new ActorInfo("passable_frozen_building"));Set(blocker,"crushables",Array.Empty<ICrushable>());
var enemy=Blank<Player>();Set(blocker,"<Owner>k__BackingField",enemy);blocker.AddTrait(new DoesNotBlock(null,new DoesNotBlockInfo()));
var frozen=Blank<FrozenActor>();Set(frozen,"actor",blocker);Set(frozen,"<Owner>k__BackingField",enemy);Set(frozen,"<Visible>k__BackingField",true);Set(frozen,"Footprint",new[]{new PPos(1,1)});
var partition=new SpatiallyPartitioned<FrozenActor>(16,16,10);partition.Add(frozen,new Rectangle(1,1,1,1));
var layer=Blank<FrozenActorLayer>();Set(layer,"partitionedFrozenActors",partition);playerActor.AddTrait(layer);
var locomotor=Blank<Locomotor>();Set(locomotor,"Info",new LocomotorInfo());var mobile=Blank<Mobile>();Set(mobile,"<Locomotor>k__BackingField",locomotor);
var module=Blank<BaseBuilderBotModuleCA>();Set(module,"world",world);Set(module,"player",player);var mcv=Blank<Actor>();
var method=typeof(BaseBuilderBotModuleCA).GetMethod("FrozenBlockedCells",BindingFlags.Instance|BindingFlags.NonPublic)!;
int Count()=>((HashSet<CPos>)method.Invoke(module,new object[]{mcv,mobile}))?.Count??0;
Console.WriteLine($"same frozen snapshot: valid={frozen.IsValid}, visible={frozen.Visible}, hidden={frozen.Hidden}, footprint={frozen.Footprint.Length}");
Console.WriteLine($"live passable backing actor: blocked_cells={Count()}");Set(blocker,"<Disposed>k__BackingField",true);
Console.WriteLine($"hidden disposal, frozen snapshot unchanged: valid={frozen.IsValid}, visible={frozen.Visible}, footprint={frozen.Footprint.Length}, blocked_cells={Count()}");
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

}
}

