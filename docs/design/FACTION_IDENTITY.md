# Faction Identity & Balance Bias

**Purpose.** This is the binding reference for *how each faction's units differ within a
class*. The per-class formula (`FORMULA_V2.md`) fixes each unit's **cost** and holds
**Δ≤1**; this document decides the *direction* of the leftover uniqueness budget so
differentiation is **lore-directed**, not arbitrary. Source-game research is cited; the
lore compendium is `../FACTIONS.md` (this doc is the balance interpretation of it).

## Core principle

Within a class, cost is pinned and price must stay Δ≤1. Because the price is fixed,
raising one stat forces another down — so each faction spends its budget on the stat
its source-game identity leans into, and pays for it in the opposite stat.

**Primary axis (confirmed across every source game): HP ↔ Speed.**
- **Brute / Heavy / Turtle** factions → **+HP, +Damage, −Speed** (tanky, hard-hitting, slow; in the original games also more expensive / fewer units — here that becomes stat weight since cost is pinned).
- **Rush / Mobile / Stealth / Swarm** factions → **+Speed, −HP** (fast, fragile; cheaper/more numerous in-game → here they trade raw durability for reach/speed).

**Secondary axes:**
- **Tech / Power** factions → **+Damage, +Range** (elite units; Protoss/Steel-Consortium "shields" read as +HP).
- **Special mechanic** (not a stat bias — a per-unit trait, priced via the special-K multiplier): stealth, mind-control, self-heal, decoys, transforming, cyborg turn-rate, area-denial.

**Exception — mirror factions (Warcraft 2).** Humans and Orcs are *stat-mirrored* in the
source game; they differ by **abilities/spells**, not base stats. WC2 counterpart units
(Footman/Grunt, Knight/Ogre, …) should stay near-identical in HP/Speed/Damage and
differ only in their special (Human = heal/defensive, Orc = Bloodlust/aggressive).

## Faction bias table

Legend: **HP** / **SPD** / **DMG** / **RNG** columns are the lean (＋ high, − low, ・ neutral).
"Special" is the signature mechanic (priced separately, not a stat lean).

### Tiberium (C&C: Tiberian Dawn / Tiberian Sun)

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| GDI TD | Brute Force / Heavy Armor | ＋ | − | ＋ | ・ | Air power, strong defense; Mammoth/Orca/Commando. Expensive, few, slow. |
| Nod TD | Rush / Stealth / Guerrilla | − | ＋ | ・ | ・ | Stealth, cheap & numerous, better AA, nuke; harassment. |
| GDI TS | Turtle / Positional | ＋ | − | ＋ | ＋ | Titans (mechs), Juggernaut long-range artillery, drop pods. |
| Nod TS | Stealth / Hit & Run | − | ＋ | ・ | ・ | Tick Tanks (burrow), Mobile Stealth Generators, cyborgs, chemical. |
| Forgotten | Adaptive / Promotion | ＋ | − | ・ | ・ | **Self-heal from Tiberium**; scavenged mixed units; mutants. |
| CABAL | Tech Rush / Promotion | ＋ | − | ＋ | ・ | **Cyborg = vehicle turn-rate (speed ×5)**; AI, promotion tech. |

### Red Alert 1 (+ Cameo's Japan)

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Allies RA1 | Tech / Mobility / Naval | − | ＋ | ・ | ＋ | Faster-weaker tanks; naval (Cruiser long-range); Chronosphere. Weak ground defense. |
| Soviets RA1 | Brute Force | ＋ | − | ＋ | ・ | Durable/slow/expensive vehicles; Tesla; Iron Curtain; heavy air. |
| Japan RA1 | Rush / Mobility | − | ＋ | ・ | ・ | **Samurai melee (one-slash)**, imperial discipline, speed/flexibility. |

### Red Alert 2 / Yuri

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Allies RA2 | Mobility / Garrison | − | ＋ | ・ | ＋ | Hit-and-run, micro, air force, versatile tech, GI garrison. |
| Soviets RA2 | Brute Force / Tank Rush | ＋ | − | ＋ | ・ | Heavy defense, macro; tanks, flak, Desolator. |
| Yuri | Mind Control | ・ | ＋ | ＋ | ・ | **Mind control**; cheap-but-strong psychic Initiates; Brutes = melee anti-tank; Gattling ramp-up. |

### Dune (Dune 2000 / Emperor)

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Ordos | Mobility / Stealth | − | ＋ | − | ・ | Fastest, less punch; Deviator (mind control); Raider trikes; stealth. |
| Ixians | Turtle / Tech Rush | ・ | ＋ | ・ | ・ | **Holographic decoys** (Projector), stealth Infiltrator, hover/tech; fascist technocrat. |
| Atreides | Balanced | ・ | ・ | ・ | ・ | Middle-of-road; Sonic Tank; quick+powerful (reference-like). |
| Harkonnen | Power / Heavy Armor | ＋ | − | ＋ | ・ | Power at expense of speed; Devastator; Death Hand. |

### StarCraft

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Terran | Turtle / Positional | ・ | ・ | ・ | ＋ | Mobile & adaptable; hold positions; siege/range; "regular" (near-reference). |
| Protoss | Tech / Power Units | ＋ | − | ＋ | ＋ | **Shields = +HP**; expensive, few, powerful; splash; late-game. |
| Zerg | Swarm / Map Control | − | ＋ | − | − | Cheap, numerous, fast, fragile; vulnerable to AoE; weak in long fights. |

### Warcraft 2 — MIRROR factions (differ by ability, not stats)

| Faction | Playstyle | Stat rule | Special |
|---|---|---|---|
| Humans WC2 | Balanced / Magic | **Mirror the Orc counterpart's HP/SPD/DMG** | Healing (Paladin/Mage), defensive, methodical. |
| Orcs WC2 | Aggressive / Dark Magic | **Mirror the Human counterpart's HP/SPD/DMG** | Bloodlust + Raise Dead, aggressive/offensive. |

### Outpost 2

| Faction | Playstyle | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Eden | Colony / Tech Rush | ＋ | − | ＋ | ・ | Better tech, brute-force military, defense-oriented. |
| Plymouth | Colony / Area Denial | − | ＋ | ・ | ・ | Speed & trickery, stolen/improvised tech, Action Bomb, area denial. |

### RA2-Mod factions (Cameo-original / obscure — from `FACTIONS.md`; deeper source research pending)

| Faction | Playstyle (FACTIONS.md) | HP | SPD | DMG | RNG | Special / signature |
|---|---|:-:|:-:|:-:|:-:|---|
| Asian Alliance | High-Tech / Mass Infantry | ・ | ＋ | ・ | ・ | Cheap, numerous infantry; high-tech late. |
| Steel Consortium | Tech Rush / Shields | ＋ | − | ＋ | ・ | **Shields = +HP**; expensive, powerful, robotic. |
| Latin Syndicate | Attrition / Stolen Tech | − | ＋ | ・ | ・ | Cheap, expendable, suicide/terror, stolen tech. |
| Naxis | Turtle / Heavy Armor | ＋＋ | −− | ＋ | ・ | Very tanky, slow; undead revival superweapon. WW2-German themed. |
| Schwarzer Mond | Timing Attack | ・ | ・ | ＋ | ・ | Burst / mid-game power spike; lunar theme. |
| FutureTech | Tech Rush / Robotics | ＋ | − | ＋ | ＋ | **Droids = vehicle turn-rate (speed ×5)**; robotic, high-tech. |
| TKM | Modular (WIP) | ・ | ・ | ・ | ・ | Modular units — flagged WIP; treat as neutral until designed. |

## How this feeds the converter (implementation note)

The converter (`propose_class_rebalance.py`) currently makes stats unique by arbitrary
nudging. The next step is to bias the uniqueness step by the acting unit's faction lean
above: e.g. in the scout class, GDI/Soviet/Naxis scouts take the high-HP/low-speed end
of the spread, Nod/Ordos/Zerg scouts the high-speed/low-HP end — all still Δ≤1, all still
5-stat unique. Mirror factions (WC2) keep their counterpart's stats and differ only in
the special. This keeps every unit unique **and** lore-true.

## Sources

- C&C TD: [GameReplays beginner guide](https://www.gamereplays.org/cnctiberiandawnremastered/portals.php?show=page&name=beginners_guide_tiberian_dawn), [Neo Encyclopedia](https://neoencyclopedia.fandom.com/wiki/Factions_of_Command_%26_Conquer)
- C&C TS: [Hardcore Gaming 101](https://www.hardcoregaming101.net/command-and-conquer-tiberian-sun/), [TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/CommandAndConquerTiberianSun)
- RA1: [TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/CommandAndConquerRedAlert), [CNC Wiki](https://cnc.fandom.com/wiki/Command_%26_Conquer:_Red_Alert)
- RA2/Yuri: [XWIS comparison](https://xwis.net/forums/index.php/topic/174385-sovs-and-allies-the-official-comparison/), [Yuri faction (CNC Wiki)](https://cnc.fandom.com/wiki/Yuri_(faction)), [Yuri's Revenge (Wikipedia)](https://en.wikipedia.org/wiki/Command_%26_Conquer:_Yuri's_Revenge)
- Dune: [Dune 2000 Q&A (GameSpot)](https://www.gamespot.com/articles/dune-2000-qanda/1100-2463781/), [Emperor: Battle for Dune (Dune Wiki)](https://dune.fandom.com/wiki/Emperor:_Battle_for_Dune)
- StarCraft: [Gameplay of StarCraft (Wiki)](https://starcraft.fandom.com/wiki/Gameplay_of_StarCraft), [TechEdvocate deep dive](https://www.thetechedvocate.org/the-three-races-of-starcraft-a-deep-dive-into-terrans-zerg-and-protoss/)
- Warcraft 2: [Wayward Strategy — Acute Asymmetry](https://waywardstrategy.com/2020/12/01/the-beauty-of-acute-asymmetry-in-warcraft-orcs-and-humans/), [Battle.net Orcs vs Humans](http://classic.battle.net/war2/ovh/index.shtml)
- Outpost 2: [Cola Powered Gamer review](https://colapoweredgamer.wordpress.com/2019/03/16/review-outpost-2-divided-destiny/), [Liquisearch gameplay](https://www.liquisearch.com/outpost_2/gameplay)
