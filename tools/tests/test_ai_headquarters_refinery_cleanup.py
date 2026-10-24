"""Active-rule coverage and source guard checks; not an in-game simulation.

The sell-cleanup guard in BaseBuilderBotModuleCA.SellUselessRefinery consults
Info.ConstructionYardTypes AFTER BotRoleSets.RulesetLoaded has unioned every
applied role's members into it (targets `BaseBuilderBotModuleCA.ConstructionYardTypes`,
unsuffixed = every instance). The protected set a test must assert is therefore
the EFFECTIVE set: authored `ConstructionYardTypes` across all
BaseBuilderBotModuleCA slots, unioned with the members of every applied role
whose Targets name that field — mirroring BotRoleSetsInfo.ResolveMembers.
zerg_lair/zerg_hive are upgrade actors (no BaseBuilding, BuildingAddons queue)
so they cannot derive `conyard`; the authored pair covers exactly them.
"""
import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
import miniyaml
from miniyaml import Ruleset

# Engine-side base-class walk (BotRoleSets TraitTypeNames) in miniature:
# the only trait subclass relevant to the DeriveHas types in play.
TRAIT_NAME_EXPANSION = {
    "Building": {"Building", "D2kBuilding"},
}


def csv(value):
    return [x.strip() for x in (value or "").split(",") if x.strip()]


def trait_match(stems, trait_name):
    return bool(stems & TRAIT_NAME_EXPANSION.get(trait_name, {trait_name}))


def role_members(rules, role, derives, derive_has, derive_not, exclude):
    """Mirror of ResolveMembers for one role: explicit BotRoles entries always
    count; a role derives members only when named in DeriveHas/DeriveHasField —
    an explicit-only role does not pass every buildable actor through empty
    specs. Derived members need Buildable+Queue (DeriveOnlyBuildable), every
    DeriveHas trait type, no DeriveNot trait type, and no Exclude entry."""
    members = set()
    for name in rules.actors:
        if name.startswith("^"):
            continue
        try:
            actor = rules.resolve(name)
        except Exception:
            continue
        if actor is None:
            continue
        stems = {c.key.split("@", 1)[0] for c in actor.children
                 if not c.key.startswith("-")}
        declared = {r for c in actor.children
                    if c.key.split("@", 1)[0] == "BotRoles"
                    for r in csv(c.get("Roles"))}
        if role in declared:
            members.add(name)
            continue
        if not derives or name in exclude:
            continue
        queue = next((c.get("Queue") for c in actor.children
                      if c.key.split("@", 1)[0] == "Buildable"), None)
        if not (queue or "").strip():
            continue
        if all(trait_match(stems, t) for t in derive_has) and \
                not any(trait_match(stems, t) for t in derive_not):
            members.add(name)
    return members


class HeadquartersRefineryCleanupTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Ruleset(ROOT)
        player = cls.rules.resolve("Player")

        cls.protected = set()
        for module in player.children:
            if module.key.split("@", 1)[0] == "BaseBuilderBotModuleCA":
                cls.protected.update(csv(module.get("ConstructionYardTypes")))

        brs = next((n for n in player.children
                    if n.key.split("@", 1)[0] == "BotRoleSets"), None)
        cls.role_sets_present = brs is not None
        if brs is None:
            return

        def section(name):
            node = brs.child(name)
            return {c.key: (c.value or "") for c in node.children} if node else {}

        derive_has = section("DeriveHas")
        derive_not = section("DeriveNot")
        derive_has_field = section("DeriveHasField")
        derive_not_field = section("DeriveNotField")
        exclude = {r: set(csv(v)) for r, v in section("Exclude").items()}
        targets = {r: csv(v) for r, v in section("Targets").items()}
        applied = set(csv(brs.get("Apply")))
        only_buildable = (brs.get("DeriveOnlyBuildable") or "true").strip().lower() != "false"

        for role, target_fields in targets.items():
            if role not in applied:
                continue
            hits_conyard = any(
                t.rsplit(".", 1)[0].split("@", 1)[0] == "BaseBuilderBotModuleCA"
                and t.endswith(".ConstructionYardTypes")
                for t in target_fields)
            if not hits_conyard:
                continue
            if role in derive_has_field or role in derive_not_field:
                raise RuntimeError(
                    f"applied role `{role}` feeds ConstructionYardTypes via "
                    "DeriveHasField/DeriveNotField, which this test cannot "
                    "mirror without C# field declarations — update the mirror")
            if not only_buildable:
                raise RuntimeError(
                    "DeriveOnlyBuildable is false — extend the test mirror")
            cls.protected.update(role_members(
                cls.rules, role, role in derive_has,
                csv(derive_has.get(role)), csv(derive_not.get(role)),
                exclude.get(role, set())))

    def test_all_starcraft_and_warcraft_headquarters_are_protected(self):
        for name in ("protoss_nexus", "terran_commandcenter", "zerg_hatchery",
                     "wc2_humans_townhall", "wc2_orcs_greathall"):
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertIsNotNone(actor)
                self.assertTrue(actor.children_named("Refinery"))
                self.assertIn(name, self.protected)

    def test_zerg_upgrade_headquarters_are_protected(self):
        # zerg_lair/zerg_hive cannot derive `conyard` (no BaseBuilding trait);
        # they must stay in the authored list or protection is lost.
        for name in ("zerg_lair", "zerg_hive"):
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertIsNotNone(actor)
                self.assertIn(name, self.protected)

    def test_every_hq_class_refinery_is_protected(self):
        """Any actor carrying both Refinery and BaseBuilding is a
        construction-yard headquarters — all must be in the effective set,
        not just the faction list hardcoded above."""
        hq_refineries = []
        for name in self.rules.actors:
            if name.startswith("^"):
                continue
            actor = self.rules.resolve(name)
            if actor is None:
                continue
            stems = {c.key.split("@", 1)[0] for c in actor.children
                     if not c.key.startswith("-")}
            if "Refinery" in stems and "BaseBuilding" in stems:
                hq_refineries.append(name)
        self.assertTrue(hq_refineries)
        for name in hq_refineries:
            with self.subTest(actor=name):
                self.assertIn(name, self.protected)

    def test_role_sets_wiring_feeds_the_guard(self):
        # The guard's coverage comes from an applied role targeting
        # BaseBuilderBotModuleCA.ConstructionYardTypes; if `conyard` leaves
        # Apply or the target is renamed, the protected set silently shrinks
        # to the authored pair and the HQ assertions above must still fail.
        self.assertTrue(self.role_sets_present,
                        "Player has no BotRoleSets trait")
        self.assertIn("protoss_nexus", self.protected)

    def test_normal_refineries_remain_unprotected(self):
        for name in ("protoss_assimilator", "terran_refinery", "zerg_extractor",
                     "ixian_refineryixian", "ra1_soviets_orerefinery",
                     "wc2_humans_elvenlumbermill", "wc2_orcs_trolllumbermill"):
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertIsNotNone(actor)
                self.assertTrue(actor.children_named("Refinery"))
                self.assertNotIn(name, self.protected)

    def test_cleanup_guard_precedes_both_sell_paths_without_changing_count(self):
        source = (ROOT / "OpenRA.Mods.CA/Traits/BotModules/BaseBuilderBotModuleCA.cs").read_text()
        cleanup = source.split("void SellUselessRefinery(IBot bot)", 1)[1].split(
            "List<MiniYamlNode>", 1)[0]
        self.assertIn("world.ActorsHavingTrait<Refinery>().Where(a => a.Owner == player).ToArray()", cleanup)
        guard = "if (Info.ConstructionYardTypes.Contains(refineries[i].Info.Name))"
        self.assertIn(guard, cleanup)
        # The diagnostic string contains braces, so inspect through the next loop.
        guard_body = cleanup.split(guard, 1)[1].split("for (var j =", 1)[0]
        self.assertIn("continue;", guard_body)
        self.assertLess(cleanup.index("if (refineries.Length <="), cleanup.index(guard))
        self.assertLess(cleanup.index("for (var i ="), cleanup.index(guard))
        self.assertLess(cleanup.index(guard), cleanup.index("for (var j ="))
        self.assertLess(cleanup.index(guard), cleanup.index("if (ResourceMapModule != null"))
        self.assertEqual(2, cleanup.count('new Order("Sell"'))


if __name__ == "__main__":
    unittest.main()
