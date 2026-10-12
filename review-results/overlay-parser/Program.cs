using System; using System.Linq; using OpenRA; using OpenRA.FileSystem;
var manifest = new Manifest("cameo", new Folder(args[0]));
if (!manifest.Rules.Contains("campaign_ai|ai/ai.yaml") || manifest.Rules.Contains("cameo|ai/ai.yaml")) throw new Exception("wrong AI routing");
if (manifest.Package.GetStream("ContentPacks/Outpost2/content.yaml") == null) throw new Exception("include missing");
Console.WriteLine("PINNED_ENGINE_MANIFEST_PARSE_PASS rules=" + manifest.Rules.Length);
