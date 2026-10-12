using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRA;
using OpenRA.Support;
using OpenRA.Mods.CA.Traits.BotModules;
using OpenRA.Mods.Cameo.Traits.BotModules;
class Program {
static void Set(object o,string n,object v){for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(n,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);if(f!=null){f.SetValue(o,v);return;}}throw new Exception(n);}
static BotFightThresholds Parse(string s)=>BotFightThresholds.Parse(MiniYaml.FromString(s,"probe"));
static void Main(){
var sparse=Parse("BotFightLearning:\n\tSchema: 999\n\tEvidence@td_gdi: 1\n\tRetreatRatioPct@td_gdi: 90\n"); Console.WriteLine($"Schema999/evidence1 accepted: {sparse.RetreatRatioPct("td_gdi","td_nod",50)}");
var family=Parse("BotFightLearning:\n\tRetreatRatioPct@family_td: 65\n");Console.WriteLine($"Fitter-emitted family ignored: {family.RetreatRatioPct("td_gdi","td_nod",50)}");
var m=(FightThresholdsBotModule)RuntimeHelpers.GetUninitializedObject(typeof(FightThresholdsBotModule));Set(m,"Info",new FightThresholdsBotModuleInfo());Set(m,"thresholds",new BotFightThresholds());Set(m,"loaded",true);var p=(IBotFightThresholds)m;
Console.WriteLine($"Empty learned data retreat fallback0: {p.RetreatRatioPct("td_gdi","td_nod",0)} (expected0)");Console.WriteLine($"Empty learned data engage fallback400: {p.EngageMarginPct("td_gdi","td_nod",400)} (expected400)");
Console.WriteLine($"Empty learned data normal fallback: {p.RetreatRatioPct("td_gdi","td_nod",50)}");
}
}
