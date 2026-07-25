# Balance Synthesis Plan — mods → Cameo (master capture)

**Status:** living plan, captured 2026-07-25 from a long maintainer strategy session. This is the
**durable record** of the whole "extract-the-mods → synthesize → fix Cameo balance" program, so
work can resume from a fresh session with nothing lost. Read alongside:
`ORIGINAL_UNIT_STATS.md` (the extracted reference data + reference map), `FORMULA_V2.md` (the
class-baseline formula), `ARMOR_SYSTEM.md` + `mods/cameo/weapons/weapons.yaml` (weapon/warhead
templates), `DESIGN.md` (binding laws — the §7–§10 rules below must be promoted there).

---

## 0. Why we did all this (the goal)

Cameo has real balance problems: **extreme values** — some units are so tanky they can't be
killed, some deal so much damage nothing survives, some whole classes (e.g. **MBTs**) deal *too
little* damage while others deal too much; and units with a wide warhead mix have **no weakness**
(good against everything). The old Cameo balance had good *ideas*, executed poorly.

**The fix:** deeply analyse the best C&C / crossover mods, extract their stats, **normalize** them
to a common scale, **synthesize** the multiple mods + the original games + the old Cameo balance
into coherent targets, and run those through our algorithmic formula. We are NOT plug-and-playing
mod numbers — we normalize, reason, and extrapolate them into Cameo's own system.

---

## 1. Source library — the mods & how to read each (paths + extraction)

| Source | Game(s) | Engine | Local path | Extraction | Status |
|---|---|---|---|---|---|
| original TD | Tiberian Dawn | Westwood | Nyerguds gist | `parse_ini.py` | ✅ HP/Cost/Speed |
| original RA1(+Aftermath) | Red Alert 1 | Westwood | unitstatistics | web | ✅ HP |
| original TS+FS | Tiberian Sun | Westwood | `C:\Users\AedisToru\Downloads\Rules.ini` | `parse_ini.py` | ✅ |
| original RA2+YR | Red Alert 2 | Westwood | `Downloads\RA2inis`, `YRinis` | `parse_ini.py` | ✅ |
| StarCraft / Warcraft 2 | — | Blizzard | unitstatistics | web | ✅ |
| **DTA** | TD+RA1 crossover | **TS engine ×10** | `G:\...\DTA\DTA Release\INI\Rules.ini` + `Enhance.ini` | `parse_ini.py`, `compare_ini.py` | ✅ Classic+Enhanced |
| **Combined Arms** | TD/RA1/RA2/Yuri/**Scrin** | **OpenRA** | `Downloads\CAmod-master\mods\ca\rules\` | `extract_openra.py` | ✅ HP/Cost/Speed (need weapons) |
| **Shattered Paradise** | GDI/Nod/CABAL/**Scrin**/Mutant | **OpenRA** | `Downloads\Shattered-Paradise-SDK-bleed\mods\sp\rules\` | `extract_openra.py` | ✅ HP (need weapons) |
| **Mental Omega 3.3.6** | RA2 (Allied/Soviet/Epsilon/Foehn) | Ares/YR | `expandmo99.mix` entry `0xe8df0937` (content-scan) | `extract_all.py`→`parse_ini.py` | ✅ HP/Cost/Speed (need weapons) |
| **CnC Reloaded 2.7.0** | **RA2 + TS combined** | Ares/YR | `Downloads\CnCReloaded-2.7.0\Tools\Map Editor\rulesmd.ini` (loose, full) | `parse_ini.py` | ⏳ TODO |
| **Romanov's Vengeance** | **RA2 remake** | **OpenRA** | `Downloads\Romanovs-Vengeance-master\mods\rv\rules\` + `\weapons\` | `extract_openra.py` | ⏳ TODO |
| Dune II / Dune 2000 / Emperor | Dune | Westwood | TBD (need INIs) | — | ⏳ TODO |
| Outpost 2 | — | Sierra | TBD | — | ⏳ TODO |
| SC2 / Cosmonarchy / WC3 | — | Blizzard | web | web | ⏳ identity-only |

**Scratchpad tools** (session scratchpad): `parse_ini.py` (INI→Strength/Cost/Speed/TechLevel;
digit + prefixed keys), `compare_ini.py` (Classic-vs-Enhanced diff), `extract_openra.py`
(OpenRA yaml→HP/Cost/Speed), `mix_extract.py` (unencrypted-mix extract by filename hash),
`extract_all.py` (dump ALL mix entries, keep INIs — bypasses the RA2 hash; **how MO was
recovered**). Blowfish-encrypted mixes (flags `0x30000`, e.g. `ra2md.mix`) still need XCC Mixer.

---

## 2. The synthesis map — which sources feed which Cameo faction

**★ CROSS-REFERENCE PRINCIPLE (maintainer 2026-07-25):** synthesize from **ALL** source material,
including cross-references. A unit that appears in several mods pools **every** appearance — e.g.
the **Apocalypse tank** is in vanilla RA2 **+ MO + Romanov's + CnC Reloaded + Combined Arms**, so
all five feed its synthesis, even though CA "mainly" targets TD/RA1. Each mod's cross-references
influence the other factions too; never restrict a mod to only its "primary" factions.

| Cameo faction group | Sources to synthesize (all pooled) |
|---|---|
| **RA2** — Allies / Soviets / Yuri | original RA2+YR **+ Mental Omega + Romanov's Vengeance + CnC Reloaded** (+ CA / SP cross-refs) |
| **TS** — GDI / Nod / Forgotten / CABAL | original TS+FS **+ Shattered Paradise + CnC Reloaded** (+ CA / MO cross-refs) |
| **TD + RA1** — GDI / Nod / Allies / Soviets / Japan | original TD+RA1 **+ DTA + Combined Arms** (+ cross-refs) |
| **Dune** — Ordos / Ixian (+ parked Atreides/Harkonnen) | **all Dune games**: Dune II + Dune 2000 + Emperor (Ixian ← Emperor House Ix) + any Dune-mod stats |
| **StarCraft / Warcraft** | original + SC2 / Cosmonarchy (SC) / WC3 (WC) — identity |
| **RA2 modded factions** | see §3 |

### 3. RA2-modded factions — specific inspirations
| Cameo faction | Inspiration sources |
|---|---|
| **Steel Consortium** | **MO Foehn Revolt** (durable/tanky — confirmed: Foehn Giantsbane 750 = tankiest infantry in MO) + Earth 2150 LC |
| **Latin Syndicate** | **MO Latin Confederation** (Soviet subfaction — black-market Soviet surplus, explosives, guerrilla; "basically the same") |
| **Asian Alliance** | **Generals China + MO China** (Soviet subfaction) **+ Combined Arms** (China refs) — mass horde |
| **Naxis** | WW2 parody + Iron Sky refs |
| **Schwarzer Mond** | Iron Sky (moon-Nazis) + **Earth 2150 Lunar Corporation** (anti-grav / hover / energy) |
| **FutureTech** | robotic (Earth 2140/50 **UCS** + RA3 FutureTech + CA/MO high-tech) |
| **Japan RA1** | **RA3 Empire of the Rising Sun + WW2 Japan + TOUHOU + misc** — a funny mix (transforming mecha, Rocket Angels, Touhou characters/bullet-hell flavour, imperial WW2) |

---

## 4. Extraction format — capture the FULL balance-spreadsheet stat set

**Directive:** extract every reference unit with the **same stats we put into the Cameo balance
spreadsheet**, so the reference is directly usable. Per unit:
- **HP** (Strength), **Cost**, **Speed**, **Tech tier**, **Armor type**
- Per weapon: **Damage per shot**, **Burst**, **BurstDelays**, **ReloadDelay**, **Range**,
  **MinRange**, **Warhead(s)**, and each warhead's **Versus values** (vs each armor type) + spread
- Then: **normalize** to that mod's basic rifleman and **convert to our system**.

**Tooling gap:** `parse_ini.py` / `extract_openra.py` currently grab **HP/Cost/Speed/Tech only**.
**EXTEND them to weapons** — the Westwood INIs have `[Weapon]`/`[Warhead]` sections with
`Damage`/`ROF`/`Range`/`Burst`/`Verses=`; OpenRA has `Armament:`/`Weapons:` + `weapons/*.yaml`
with `Warhead@…: Versus:`. This is required before we can synthesize the DPS/versus side (§8).

---

## 5. The methodology — normalize → synthesize → formula (the HOW)

The hard question the maintainer posed: we can't plug-and-play mod numbers (different scales), so
**how** do we combine mods + old Cameo + our formula? The method:

1. **Extract** every mod's FULL stats (§4), **normalized to that mod's basic rifleman** → a common
   *relative* scale (ratios, not raw numbers).
2. **Synthesize a target relative-profile** per Cameo class/faction by pooling the relevant sources
   (§2, weighted by relevance) **+ old Cameo Layer-2** (current stats — keep what works) **+ the
   original game**. Reason/extrapolate to a coherent relative target; fill gaps by role-analogy.
3. **Map** the relative target onto **Cameo's absolute scale** (basic rifle anchor = **20000 HP**).
4. **Set the CLASS ANCHORS** from these synthesized targets. *This is where per-class multipliers
   get baked in* (§7) — the anchor **is** the baseline actor.
5. **Run the formula** (`FORMULA_V2`, Δ≤1, 5-stat uniqueness) to price + **spread units within the
   class** around the synthesized anchor.
6. Result: the **formula still does the pricing/spreading** (keeps Cameo's system); the **mods set
   the anchors** (grounds the previously-arbitrary anchor picks in real, synthesized data). We
   never write a mod's HP into a unit — we write synthesized-normalized targets into the *anchors*.

**Formula may need changes:** anchor **ratios** should come from synthesized mod data (tighter than
today — see §6); the **DPS side** must be reworked around the weapon-template binding (§8).

---

## 6. The spread-width decision (maintainer question + honest recommendation)

**Question:** Cameo's spreads are wider than the mods (Cameo close-combat 2.5× / commando **4.0×**
the rifle, vs CA flame 1.8× / hero 2.2×). Is that a *problem*, or is it *necessary* because we have
so many units that must all be unique? Large spreads = unique but maybe too extreme + hard to
balance. Where's the middle ground?

**Honest recommendation:**
- The extreme spread is **likely a real cause of the "unkillable / unstoppable" complaint** — but
  the every-unit-unique law does **NOT** actually require it.
- **Uniqueness is cheap; extremes come from the ANCHORS, not from uniqueness.** Within a class,
  ~30 faction variants at small steps (e.g. 1000-HP steps on a 20000 base) span only ~2.5× —
  modest. The 16× total range (support anchor 5000 → commando 80000) comes from the **class-anchor
  ratios**, not from making units unique.
- **Uniqueness ≠ unique HP.** A unit can be unique via a *different* one of the 5 stats (speed,
  range, damage/shot, reload). Spread uniqueness across all five → HP can stay in a sane band.
- **Middle ground:** tighten the **class-anchor HP band** to roughly the mod-normalized range
  (**≈0.4× … ≈3.5×** the basic rifleman; the mods top out ~3.3–3.7×). Compress the total class
  band from ~16× toward **~8–9×**. Keep **within-class steps small**. This preserves uniqueness AND
  removes the extremes. Concretely: pull commando 4.0×→~3.5×, re-check support 0.25× (too frail?),
  and re-derive every class anchor from the §5 synthesis instead of arbitrary picks.

---

## 7. Baseline-only balancing — bake out per-class multipliers

**Directive:** stop balancing with per-class **multipliers**; balance **baseline actors only**.
- `mods/cameo/rules/.../defaults.yaml` currently gives class templates per-class
  `FirepowerMultiplier` / `DamageMultiplier` (extra firepower + damage-resistance). **Bake these
  into the baseline actor stats** so the baseline is WYSIWYG. Changing a baseline stat then reruns
  the whole class rebalance and updates every member.
- **KEEP (the only allowed global multipliers):** the global **50% firepower reduction** (ironically
  named *"global buff"*) **+ 150% damage multiplier**. Reason: our fixed **2000-damage-step + 1%
  percentage** rule made total damage too high, so we cut 50% then added 150% back — but the 150%
  applies **only to units + defenses**, so **regular buildings are exempt and stay much more
  durable** than anything else. This asymmetry is intentional and stays.
- Everything else = resolved into baseline actors.

---

## 8. Weapon / warhead rework — STRICT class ↔ weapon binding

**The core problem:** mixing many warheads on one unit **averages** their versus-profiles → the
unit becomes **good against everything with no weakness**. Offenders: **StarCraft Ghost/Specter**
(anti-inf + anti-air + anti-tank all at once), **WC2 axethrowers / archers** and their upgrades
(**High Elven Archer, Head Hunter** — insane damage to everything, too much range, unstoppable),
mismatched mixes like **TD Nod Light Tank Mk2** (cannon + chemical, but the tank has no chemical
lore connection).

**The law (promote to DESIGN.md):**
- **Every unit class is bound to a specific weapon class/type.** MBTs → medium cannon; scouts →
  small arms; etc. A fixed connection unit-class ↔ weapon-class.
- **Warhead MIXING is only allowed via UPGRADES** (lore-justified, balance-costed) or **well-marked
  special-case units** (must be explicitly flagged as exceptions so we know they're intentional).
- **Every unit gets a STRENGTH and a WEAKNESS** — no unit may damage everything on its own.
- **Grow the warhead library:** make **more templates per role/situation** so units don't have to
  mix. E.g. **SmallArmsAP** (upgraded SmallArms, more vs tanks); dedicated anti-air, anti-armor,
  anti-structure warheads — each **good vs SOME armor types, BAD vs others** (real trade-offs).
- **Good mixing (allowed, thematic + from upgrade):** RA1 Soviet *Incendiary Bullets* (adds a light
  flame warhead to bullets); **Chemical Attack Bike** (upgrades Recon Bike: missile + chemical),
  **Chemical Stealth Tank** (upgrades Stealth Tank), **Chemical SSM** (replaces the SSM's fire
  warheads with chemical). These *replace/augment* along a lore line, from an upgrade.
- **Versus system:** the fixed **descending-order versus values** (spread + percentage; only the
  *order of armor types* is swapped per warhead) is fine **per warhead** — the failure is *mixing*,
  which averages the profile away. **One primary warhead per unit** keeps its weakness intact;
  upgrades add costed exceptions.

**Work:** audit `weapons.yaml` (all current templates + their versus-profiles), compare to the
mods' weapon/versus data (§4 extension), define the **unit-class ↔ weapon-class binding matrix**,
expand the warhead library, and remove the wild mixes (converting real ones to upgrade-gated).

---

## 9. Anti-Air capability — class-gated (promote to DESIGN.md)

Which classes may hit air (everything else = ground-only):
- **Infantry:** ONLY **Special Forces, Archers, Rocket Infantry**.
- **Vehicles:** **Support-Vehicle class** (APCs + dedicated AA — *consider splitting transport-APC
  vs dedicated-AA*, different roles), **Scout vehicles** (all have AA — they fill the AA role), and
  **high-tech tanks** with *limited* AA (unit-dependent — e.g. Mammoth / Apocalypse AA rockets;
  not every MBT).
- **Aircraft:** **all helicopters + planes** get AA (dogfights), and big **spaceships**. EXCEPT
  **flying artillery** and some support weapons (**Cryocopter beam**) = **anti-ground only** (too
  strong as AA, and slow projectiles don't make sense against air).

---

## 10. The rock-paper-scissors counter mandate

The end goal is a **real multi-layered rock-paper-scissors** system: **every role clearly counters
some roles while being countered by others**, expressed through clear roles + armor types + weapon
versus values. This is the giant unifying task that all the extracted data feeds. Class↔weapon
binding (§8), AA gating (§9), tightened spreads (§6), and baseline-only balancing (§7) are the
mechanisms; the mod synthesis (§2–§5) supplies the reference numbers.

---

## 11. Sequencing & open questions

**Sequence:** (a) finish extraction — CnC Reloaded + Romanov's Vengeance, then Dune + Outpost 2,
extend tooling to **weapons/versus** (§4); (b) build **normalized full reference tables** per mod
per faction (§5.1); (c) synthesize per-class/faction targets → **re-derive class anchors** (§5–§6);
(d) rework the **weapon/warhead library + class binding** (§8) and **AA gating** (§9); (e) **bake
out multipliers** (§7); (f) rerun the formula per class → apply. All under the RPS goal (§10).

**Open questions:** does the formula itself need reshaping to hit synthesized targets, or just
better anchors? How exactly to weight each source in the synthesis? Where to draw each class's
spread band precisely? — resolve with the data in hand, class by class.
