using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Cameo.Test
{
	[TestFixture]
	public class McvDeployCellMountTest
	{
		static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
			"tools", "tests", "fixtures", name)).Replace("\r\n", "\n");

		[Test]
		public void OriginalInstanceIsByteIdenticalExceptForAddedSearchCondition()
		{
			var original = Fixture("mcv_deploy_cell_legacy_block.yaml");
			var expected = original.Replace("\tMcvExpansionManagerBotModule:\n",
				"\tMcvExpansionManagerBotModule:\n\t\tSkipUnreachableDeployCellsCondition: genericbot\n");
			Assert.That(Fixture("mcv_deploy_ai.yaml"), Does.Contain(expected));
		}

		[Test]
		public void RealYamlArmsOnlyGenericAndRetainsClassicDefaults()
		{
			var nodes = MiniYaml.FromString(Fixture("mcv_deploy_ai.yaml"), "ai").Single(n => n.Key == "Player").Value.Nodes;
			Assert.That(nodes.Count(n => n.Key.Split('@')[0] == "McvExpansionManagerBotModule"), Is.EqualTo(1));
			var info = FieldLoader.Load<McvExpansionManagerBotModuleInfo>(nodes.Single(n => n.Key == "McvExpansionManagerBotModule").Value);
			Assert.That(info.RequiresCondition.Evaluate(new Dictionary<string, int> { ["classicbot"] = 1 }), Is.True);
			Assert.That(info.RequiresCondition.Evaluate(new Dictionary<string, int> { ["genericbot"] = 1 }), Is.True);
			Assert.That(info.SkipUnreachableDeployCellsCondition.Evaluate(new Dictionary<string, int> { ["classicbot"] = 1 }), Is.False);
			Assert.That(info.SkipUnreachableDeployCellsCondition.Evaluate(new Dictionary<string, int> { ["genericbot"] = 1 }), Is.True);
		}
	}
}
