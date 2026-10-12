from pathlib import Path
r=Path(r'C:/cameo-wt/codex-rereview-econ-a')
p=r/'review-results/probe'; p.mkdir(exist_ok=True)
old=Path(r'C:/cameo-wt/codex-review-econ-a/review-results/probe/probe.csproj').read_text().replace('codex-review-econ-a\\engine','codex-rereview-econ-a\\engine')
(p/'probe.csproj').write_text(old)
(p/'Program.cs').write_text('''using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Primitives;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Common.Traits;
class Program {
static object Blank(Type t)=>RuntimeHelpers.GetUninitializedObject(t);
static T Blank<T>()=>(T)Blank(typeof(T));
static void Set(object o,string n,object v) { for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,v);return;}}throw new Exception(n); }
static void Main(){
var world=Blank<World>(); Set(world,"TraitDict",Activator.CreateInstance(typeof(World).Assembly.GetType("OpenRA.TraitDictionary")!,true));
var map=Blank<Map>(); Set(map,"Grid",new MapGrid(new MiniYaml(""))); Set(map,"<AllCells>k__BackingField",new CellRegion(MapGridType.Rectangular,new CPos(0,0),new CPos(15,15))); Set(world,"<Map>k__BackingField",map);
var player=Blank<Player>(); var playerActor=Blank<Actor>(); Set(playerActor,"World",world);Set(playerActor,"ActorID",(uint)1);Set(player,"PlayerActor",playerActor);
var blocker=Blank<Actor>();Set(blocker,"World",world);Set(blocker,"ActorID",(uint)2);Set(blocker,"Info",new ActorInfo("passable_frozen_building"));Set(blocker,"crushables",Array.Empty<ICrushable>());
blocker.AddTrait(new DoesNotBlock(null,new DoesNotBlockInfo()));
var frozen=Blank<FrozenActor>();Set(frozen,"actor",blocker);Set(frozen,"<Owner>k__BackingField",player);Set(frozen,"<Visible>k__BackingField",true);Set(frozen,"Footprint",new[]{new PPos(1,1)});
var partition=new SpatiallyPartitioned<FrozenActor>(16,16,10);partition.Add(frozen,new Rectangle(1,1,1,1));
var layer=Blank<FrozenActorLayer>();Set(layer,"partitionedFrozenActors",partition);playerActor.AddTrait(layer);
var locomotor=Blank<Locomotor>();Set(locomotor,"Info",new LocomotorInfo());var mobile=Blank<Mobile>();Set(mobile,"<Locomotor>k__BackingField",locomotor);
var module=Blank<BaseBuilderBotModuleCA>();Set(module,"world",world);Set(module,"player",player);var mcv=Blank<Actor>();
var method=typeof(BaseBuilderBotModuleCA).GetMethod("FrozenBlockedCells",BindingFlags.Instance|BindingFlags.NonPublic)!;
int Count()=>((HashSet<CPos>?)method.Invoke(module,new object[]{mcv,mobile}))?.Count??0;
Console.WriteLine($"same frozen snapshot: valid={frozen.IsValid}, visible={frozen.Visible}, hidden={frozen.Hidden}, footprint={frozen.Footprint.Length}");
Console.WriteLine($"live passable backing actor: blocked_cells={Count()}");Set(blocker,"<Disposed>k__BackingField",true);
Console.WriteLine($"hidden disposal, frozen snapshot unchanged: valid={frozen.IsValid}, visible={frozen.Visible}, footprint={frozen.Footprint.Length}, blocked_cells={Count()}");
}
}
''')
