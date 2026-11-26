#region Copyright & License Information
/**
 * Copyright (c) The OpenRA Combined Arms Developers (see CREDITS).
 * This file is part of OpenRA Combined Arms, which is free software.
 * It is made available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of the License,
 * or (at your option) any later version. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.CA.Traits;
using OpenRA.Mods.Cameo.Dev;

namespace OpenRA.Mods.Cameo.Test
{
	// REF-PREFETCH-FRESH (row ref_prefetch_fresh, Sol V08F_CUMREV_SOL_01 finding 2, Opus V08G): a relaxed refinery site promoted before READY expires at the READY flip, and a strict
	// site outranks a relaxed one. The list step is tested purely, the search wiring by pins, the arm by yaml.
	[TestFixture]
	public sealed class RefPrefetchFreshTest
	{
		const string Dir = "mods/CA/Traits/BotModules/BotModuleLogic/";

		static string Source(string file) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source", file)).Replace("\r\n", "\n");

		static MiniYamlNode PlayerNode(string key) =>
			MiniYaml.FromString(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tools", "tests", "fixtures", "mcv_deploy_ai.yaml")).Replace("\r\n", "\n"), "ai")
				.Single(n => n.Key == "Player").Value.Nodes.Single(n => n.Key == key);

		static string FieldOrNull(MiniYamlNode node, string key) => node.Value.Nodes.SingleOrDefault(n => n.Key == key)?.Value.Value?.Trim();

		sealed class C
		{
			public CPos Cell;
			public RefinerySiteScore Score;
			public bool Relaxed;
		}

		static C Site(int x, RefinerySiteScore score, bool relaxed = false) => new() { Cell = new CPos(x, 0), Score = score, Relaxed = relaxed };

		static void Insert(List<C> list, C c) => RefinerySiteSearchLogic.InsertBest(list, c, x => x.Score, x => x.Cell);

		[Test]
		public void TakeRelaxedRemovesEveryRelaxedMemberAndKeepsStrictOnesAndTheIssuedOrder()
		{
			var best = new List<C>();
			var relaxedA = Site(1, new RefinerySiteScore(0, 0, 2, 4, 1, 0), true);
			var strict = Site(2, new RefinerySiteScore(0, 1, 0, 9, 2, 0));
			var relaxedB = Site(3, new RefinerySiteScore(0, 2, 3, 16, 3, 0), true);
			foreach (var c in new[] { relaxedA, strict, relaxedB })
				Insert(best, c);

			var taken = RefinerySiteSearchLogic.TakeRelaxed(best, x => x.Relaxed, x => ReferenceEquals(x, relaxedB));
			Assert.That(taken, Is.EqualTo(new[] { relaxedA }));
			Assert.That(best, Is.EqualTo(new[] { strict, relaxedB }), "a strict member and the relaxed site an order was issued for stay, in order");

			Assert.That(RefinerySiteSearchLogic.TakeRelaxed(best, x => x.Relaxed, _ => false), Is.EqualTo(new[] { relaxedB }));
			Assert.That(best, Is.EqualTo(new[] { strict }));
			Assert.That(RefinerySiteSearchLogic.TakeRelaxed(best, x => x.Relaxed, _ => false), Is.Empty, "nothing relaxed left: no change");
			Assert.That(RefinerySiteSearchLogic.TakeRelaxed(new List<C>(), x => x.Relaxed, _ => false), Is.Empty);
		}

		// Sol's pin: a prefetched relaxed site, then a newly valid strict one before READY. Off, the class-0 relaxed claim site outranks the class-1 strict widened site
		// (the score compares the class first). On, the strict one is alone in the list and the relaxed one waits.
		[Test]
		public void AStrictSiteFoundAfterAPrefetchedRelaxedOneWinsOnlyWhenTheRowIsOn()
		{
			var relaxed = Site(1, new RefinerySiteScore(0, 0, 2, 4, 1, 0), true);
			var strict = Site(2, new RefinerySiteScore(1, 0, 0, 0, 2, 0));

			// off: as before
			var off = new List<C>();
			Insert(off, relaxed);
			Insert(off, strict);
			Assert.That(off[0], Is.SameAs(relaxed), "the stale class-0 relaxed claim outranks the fresh class-1 strict site");

			// on: the strict offer evicts the relaxed member first (EvictRelaxedForStrict), the relaxed one goes back to its own list
			var on = new List<C>();
			var waiting = new List<C>();
			Insert(on, relaxed);
			foreach (var r in RefinerySiteSearchLogic.TakeRelaxed(on, x => x.Relaxed, _ => false))
				Insert(waiting, r);

			Insert(on, strict);
			Assert.That(on, Is.EqualTo(new[] { strict }));
			Assert.That(waiting, Is.EqualTo(new[] { relaxed }));
		}

		[Test]
		public void TheSearchWiringExpiresAtTheReadyFlipAndEvictsOnAStrictOffer()
		{
			var search = Source(Dir + "RefinerySiteSearch.cs");

			Assert.That(search, Does.Contain("Dev(e, now, \"ready\", $\"tR={e.ReadyTick} hold={(e.DemandHold ? 1 : 0)}\");\n\t\t\t\t\tExpirePrefetchedRelaxed(e, now);"), "the expiry runs at the READY flip");
			Assert.That(search, Does.Contain("if (!spacingFallback)\n\t\t\t\tEvictRelaxedForStrict(e, cell, now);\n\n\t\t\tif (spacingFallback)"), "a strict offer evicts before it is inserted; the relaxed branch is unchanged");
			Assert.That(search, Does.Contain("if (!baseBuilder.RefPrefetchFreshArmed)\n\t\t\t\treturn;"));
			Assert.That(search, Does.Contain("if (!baseBuilder.RefPrefetchFreshArmed || e.Best.Count == 0)\n\t\t\t\treturn;"));
			Assert.That(search, Does.Contain("e.InO3R = false;\n\t\t\te.Anchors = null;\n\t\t\twatchStale = true;\n\t\t\tDev(e, now, \"relaxed_expired\""), "the pass restarts exactly as at a pass end");
			Assert.That(search, Does.Contain("Dev(e, now, \"relaxed_outranked\""));
			Assert.That(search, Does.Contain("RefinerySiteSearchLogic.TakeRelaxed(e.Best, x => x.SpacingFallback, x => ReferenceEquals(x, e.Outstanding))"), "an issued order is never taken back");

			// the certificate path itself is unchanged
			Assert.That(search, Does.Contain("if (RefineryDemand.RelaxedGate(true, e.Best.Count, e.Relaxed.Count) == RefineryDemand.RelaxedStep.Promote)"));
		}

		[Test]
		public void OnlyTheGenericBaseBuilderArmsItAndTheRowIsRegisteredAndOffByDefault()
		{
			var bb = Source("mods/CA/Traits/BotModules/BaseBuilderBotModuleCA.cs");
			Assert.That(bb, Does.Contain("public bool RefPrefetchFreshArmed => Info.RefPrefetchFresh && switchesActive && !ablateRefPrefetchFresh.Off(player, AblationIds.RefPrefetchFresh);"));

			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@generic"), "RefPrefetchFresh"), Is.EqualTo("true"));
			Assert.That(FieldOrNull(PlayerNode("BaseBuilderBotModuleCA@classic"), "RefPrefetchFresh"), Is.Null);
			Assert.That(AblationIds.RefPrefetchFresh, Is.EqualTo("ref_prefetch_fresh"));
			Assert.That(AblationSpec.RegistryIds(), Does.Contain(AblationIds.RefPrefetchFresh));
			Assert.That(new BaseBuilderBotModuleCAInfo().RefPrefetchFresh, Is.False);
		}
	}
}
