# Campaign map package inventory

Source pin: `964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (`origin/master`). Package SHA is computed over the complete `.oramap` bytes returned by `git show <pin>:<path>`. Counts are independently parsed from archived `map.yaml`: playable `PlayerReference@MultiN` seats and `ActorN: mpspawn` entries.

| Spec map | Repository file | SHA-256 | Playable Multi seats | mpspawn actors | Required seats | Result |
|---|---|---|---:|---:|---:|---|
| A Nuclear Winter | `mods/cameo/maps/_ra_a-nuclear-winter.oramap` | `191efefe3043acb2fd504b2e2e5c974a49f6498f8e016c1380159c9f7efc1c4a` | 2 | 2 | 2 | present |
| Satan's Clutch | `mods/cameo/maps/SatansClutch.oramap` | `1bd3bee0bbb38c95015126ba689496c4284c2f4e8f8b6573711a9fdadcbf16cb` | 2 | 2 | 2 | present |
| Red Spice | `mods/cameo/maps/Red_Spice_2v2_BI-4.4.oramap` | `882f865b4e556f93324bbd1abb31397bff30b651489652fa151c031cf02cb65a` | 4 | 4 | 4 | present |
| Terra Cotta | `mods/cameo/maps/Terracotta-ratls2.oramap` | `c1a66fba029ccde5998fbe57b563f87569425ace82617e42e6e42e3fcc9e740d` | 4 | 4 | 4 | present |
| Back to Basics | `mods/cameo/maps/back-to-basics.oramap` | `ddd071d131895ecb59be66b7d1924b023731c52a04ee01052fa9fc384dd15e6b` | 6 | 6 | 6 | present |
| Winter's End (Rich) | `mods/cameo/maps/winters-end-rich.oramap` | `a29eea3fef3087d5544c428d2d07d59f4430806c0b23db7eeb605dde7013c94e` | 6 | 6 | 6 | present |
| Great Sahara 2 | `mods/cameo/maps/Great_Sahara_3.oramap` | `8b5c089c7cd2f8209819afa04436d6fa15849e125823ddc16bf5e996cd280ddc` | 8 | 8 | 8 | present |
| Ice Cold | `mods/cameo/maps/ice_cold.oramap` | `e097fcca4fa40f5878b4fb7d965529728dbbd34c2a14b8825e1bec0aea6da98b` | 8 | 8 | 8 | present |

All eight files exist at the exact source pin, their package hashes match the planner's previous asset pins, and each has exactly the required number of playable references and spawn actors. This is a capacity check only; it does not establish symmetric spawn pairs, playable faction support, or engine acceptance. Those remain runtime/preflight gates.
