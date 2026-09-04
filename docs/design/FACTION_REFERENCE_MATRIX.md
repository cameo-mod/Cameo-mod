# The faction reference matrix

> ⛔ **A PLAN, NOT AN IMPLEMENTATION.** Nothing is wired. `assign_references.py` still matches by
> name and shape, which is what produced the rejected scout sheet.

**Maintainer ruling 2026-09-04, after reviewing that sheet:** *"most of the references are
bullshit… what does the Asian Alliance Militia have to do with the Combined Arms Infiltrator? …
instead of trying to match something completely unrelated we now try to map reference faction to
our cameo factions."*

⭐ **The diagnosis is right and it explains every bad row.** Matching searched all 15 sources for
any unit with a similar name or shape. A Cameo RA2 Soviet conscript and a Mental Omega Latin
Confederation militia can then compete for the same reference. **Faction routing removes the
question instead of answering it better:** an Asian Alliance unit may only draw on Mental Omega
China, so *"Animal Alligator"* was never a candidate in the first place.

⚠ The one row the maintainer accepted proves the principle: `ra2_soviets_conscript` ← RV/CnCR/MO
**Conscript**, *"exactly the same unit"* — same faction, same role, same name.

---

## §1 — ⛔ What blocks this today

| blocker | detail |
|---|---|
| **No faction column anywhere** | Neither `ORIGINAL_UNITS_RAW.md` (DOC1) nor `ORIGINAL_UNITS_PEER_OPENRA.md` (DOC5) records a faction. Both need re-extracting. |
| **DTA has no unit rows at all** | Required for all four TD/RA1 factions. The maintainer is providing the INIs — **ask again tomorrow.** |
| **RA2 Reborn + Red Resurrection carry NO unit stats** | They exist only in `versus_raw.json` as warhead armour profiles. The ruled 1/6 weighting cannot include them yet — in practice RA2 is **1/4**, not 1/6. |
| **Mental Omega and CnC Reloaded have no faction data** | They are hand-typed tables with `Unit · kind · HP · Cost · Spd · Weapon · Dmg · Rld · Rng`. ⛔ **This is the big one — MO is the sole source for every invented faction.** |

### ⭐ But faction IS recoverable from the OpenRA clones

Measured over each clone's buildable roster, reading `Buildable.Queue` and `Prerequisites`:

| source | buildable | faction-tagged | factions found |
|---|--:|--:|---|
| **Romanov's Vengeance** | 832 | **831 (100%)** | allies · soviets · yuri · uk · france · usa · iraq · cuba · baku |
| **Shattered Paradise** | 317 | **317 (100%)** | gdi · nod · cabal · **mutant** · scrin |
| **Combined Arms** | 503 | 101 (20%) | gdi · nod · td · ra · soviet · allies · yuri · scrin — ⚠ its tokens mix faction with unit names (`~vehicles.mtnk`), so the extractor needs a token whitelist, not a regex |
| **OpenE2140** | — | — | **ucs · ed** |

⭐ **Shattered Paradise's `mutant` is Cameo's Forgotten** — a direct faction counterpart that name matching would never have found.

---

## §2 — The ruled tiers

### TD + RA1 — one third each

| Cameo faction | reference factions | weight |
|---|---|---|
| `tiberiandawn_gdi` | DTA **GDI** · Combined Arms **GDI** · Cameo | 1/3 each |
| `tiberiandawn_nod` | DTA **Nod** · Combined Arms **Nod** · Cameo | 1/3 each |
| `redalert_allies` | DTA **Allied** · Combined Arms **Allies/RA** · Cameo | 1/3 each |
| `redalert_soviets` | DTA **Soviet** · Combined Arms **Soviet** · Cameo | 1/3 each |

⛔ **Blocked on the DTA INIs.** Until they arrive these four factions have Combined Arms + Cameo
only, i.e. 1/2 each rather than 1/3.
⚠ `redalert_japan` is **not** in this tier — it is a Cameo invention (§3).

### RA2 — one sixth each, ruled; one quarter, achievable

| Cameo faction | ruled sources | usable today |
|---|---|---|
| `redalert2_allies` | RV · MO · CnCR · RA2 Reborn · Red Resurrection · Cameo | RV **allies** · MO · CnCR · Cameo |
| `redalert2_soviets` | same six | RV **soviets** · MO · CnCR · Cameo |
| `redalert2_yuri` | same six | RV **yuri** · MO · CnCR · Cameo |

⚠ RV alone carries faction tags; MO and CnCR would contribute unrouted until they gain a faction
column.

### TS — one quarter each

| Cameo faction | reference factions |
|---|---|
| `tiberiansun_gdi` | CnCR · Shattered Paradise **gdi** · Crystallized Nexus · Cameo |
| `tiberiansun_nod` | CnCR · Shattered Paradise **nod** · Crystallized Nexus · Cameo |
| `tiberiansun_cabal` | CnCR · Shattered Paradise **cabal** · Crystallized Nexus · Cameo |
| `tiberiansun_forgotten` | CnCR · Shattered Paradise **mutant** ⭐ · Crystallized Nexus · Cameo |

---

## §3 — The invented factions: research

⭐ **The inspiration map already exists** — `BALANCE_SYNTHESIS.md` §3, written 2026-07-25. The work
is connecting each documented inspiration to a mod actually on disk.

| Cameo faction | documented inspiration | reference faction | on disk? |
|---|---|---|---|
| `redalert2mod_syndicate` (Latin Syndicate) | **MO Latin Confederation** — *"basically the same"* | MO **Latin Confederation** | ⚠ MO has no faction column |
| `redalert2mod_asianalliance` | **Generals China + MO China** + CA China refs | MO **China** · Generals Alpha **China** | ⚠ same; GA mod id unresolved |
| `redalert2mod_consortium` (Steel Consortium) | **MO Foehn Revolt** + Earth 2150 LC | MO **Foehn** | ⚠ same |
| `redalert2mod_futuretech` | Earth 2140/50 **UCS** + RA3 FutureTech | ⭐ **OpenE2140 `ucs`** | ⭐ **YES — on disk and faction-tagged** |
| `redalert2mod_schwarzermond` | Iron Sky + **Earth 2150 Lunar Corporation** | ⚠ Earth **2150** is not on disk; OpenE2140 is Earth **2140** (`ucs`/`ed`) | partial |
| `redalert2mod_naxis` | WW2 parody + Iron Sky | ⛔ no counterpart found in any source | **no** |
| `redalert2mod_tkm` | not in §3 | ⛔ unidentified — needs a maintainer answer | **no** |
| `redalert_japan` | RA3 Empire + WW2 Japan + Touhou | ⛔ no RA3 mod in the corpus | **no** |

### ⭐ The single best find

**FutureTech ← OpenE2140's `ucs`.** `BALANCE_SYNTHESIS` §3 names Earth 2140's UCS as its
inspiration, and OpenE2140 is cloned, resolving, and faction-tagged with exactly `ucs`. That is a
documented intent meeting available data — the only invented faction that can be routed today.

### ⚠ Three factions have no reference at all

`naxis`, `tkm` and `redalert_japan` have no counterpart in any of the 15 sources. **They should be
formula-only from a grounded class anchor** — the ruling already made for units with no reference,
rather than forcing a bad match, which is exactly what produced the rejected sheet.

---

## §4 — How auto-detection would then work

The goal: *"Asian Alliance is MO China, so their Lynx is the equivalent of the Qiling."*

With faction routing the search space collapses from 2,878 rows to one faction's roster — perhaps
30 units — and within it the existing cascade is enough:

1. **route** — `asianalliance_lynxtank` may only see MO China
2. **filter by type** — vehicles only
3. **filter by class** — MBT
4. **rank by the cascade** — name, then tier, then role/shape, then cost

MO China fields one MBT. The Qiling falls out **without needing a name match at all**, which is
precisely why the Lynx/Qiling and Rusher/Jaguar pairs are undiscoverable by today's matcher.

⚠ Where a faction fields two units of one class, the cascade still decides, and the MO wiki is the
tie-breaker the maintainer named for unclear roles.

---

## §5 — What has to happen, in order

1. ⛔ **Add a faction column to the peer extractor** and re-extract DOC5. RV and SP give 100%
   immediately; Combined Arms needs a token whitelist.
2. ⛔ **Get faction data for Mental Omega and CnC Reloaded.** They are hand tables. Without this
   every invented faction stays unroutable — **the biggest single blocker in this document.**
3. **Ask for the DTA INIs** (promised for 2026-09-05) and extract a full roster.
4. **Confirm the faction map** in §3, especially `tkm` and the three with no counterpart.
5. **Then** rewrite matching to route by faction, and regenerate the scout sheet to compare.

⚠ Until step 5, **no class should be signed on reference evidence.** The scout sheet stands
rejected.

---

# PART II — THE MATRIX (2026-09-04)

**Ruling:** *"Always have at least 2 reference factions from different games for each cameo
faction!"* — mirrors or not, no measurement first. Applied below.

⚠ **THE MIRROR PROBLEM IS WHY.** Two reference factions from the SAME game may be mirrors of each
other, and extracting both then yields no extra information — every Cameo unit would land on stats
identical to its counterpart. `OpenE2140`'s `ucs` and `ed` look exactly like this: both are named
*Mobile Air Base · Mobile Refinery · Mobile Defense Tower*. **A second GAME, not a second faction,
is what guarantees uniqueness.**

⚠ And the reference decides only **where a unit sits in its class's distribution.** The balance
formula still prices it. These are inputs to placement, not final stats.

## §6 — Every faction now available

| source | factions |
|---|---|
| Generals Alpha | **`prc`** (China) · **`gla`** (Global Liberation Army) · `usa` |
| Romanov's Vengeance | `allies` · `soviets` · `psicorps` · `bakupact` + 27 subfactions |
| Combined Arms | `gdi` · `nod` · `allies` · `soviet` · `yuri` · `scrin` · `talon` · `shadow` |
| Shattered Paradise | `gdi` · `nod` · `cab` · **`mut`** · `scr` |
| Crystallized Nexus | `gdi` · `nod` · `gdf` · `steel` · `zocom` |
| OpenE2140 | **`ucs`** · `ed` |
| OpenRA TD / TS / RA / D2K | `gdi` · `nod` / `gdi` · `nod` / `allies` · `soviet` / `atreides` · `harkonnen` · `ordos` |
| Valiant Shades | `allies` · `soviets` |
| OpenHV | `yi` · `sc` — ⚠ unidentified, Polish-named sci-fi, no obvious Cameo counterpart |

⛔ **A near-miss worth recording: Crystallized Nexus `steel` is STEEL TALONS**, a GDI division
(Titan, Wolverine, Juggernaut) — **not** Steel Consortium. `gdf` and `zocom` are likewise GDI
branches, and Combined Arms' `talon` is the same Steel Talons. Name similarity nearly produced
exactly the class of error this whole redesign exists to remove.

## §7 — The matrix

⭐ = confident · ⚠ = proposed, wants your ruling · ⛔ = no second game exists

### Tier 1 — TD and RA1 (ruled 1/3 each)

| Cameo faction | game A | game B | game C | status |
|---|---|---|---|---|
| `tiberiandawn_gdi` | **DTA** gdi | Combined Arms `gdi` | OpenRA TD `gdi` | ⭐ 3 games |
| `tiberiandawn_nod` | **DTA** nod | Combined Arms `nod` | OpenRA TD `nod` | ⭐ |
| `redalert_allies` | **DTA** allied | Combined Arms `allies` | OpenRA RA `allies` | ⭐ |
| `redalert_soviets` | **DTA** soviet | Combined Arms `soviet` | OpenRA RA `soviet` | ⭐ |

⛔ **DTA pending — ask the maintainer 2026-09-05.** Until then each has two games, which still
satisfies the rule.

### Tier 2 — RA2 (ruled 1/6, achievable 1/4)

| Cameo faction | game A | game B | game C | status |
|---|---|---|---|---|
| `redalert2_allies` | Romanov's Vengeance `allies` | Valiant Shades `allies` | MO / CnCR (unrouted) | ⭐ 2 games |
| `redalert2_soviets` | Romanov's Vengeance `soviets` | Valiant Shades `soviets` | MO / CnCR | ⭐ |
| `redalert2_yuri` | Romanov's Vengeance `psicorps` | Combined Arms `yuri` | MO / CnCR | ⭐ |

⚠ RV and *Yuri's Revenge on OpenRA* are ONE lineage and already collapsed — they do not count as
two games. Valiant Shades and Combined Arms are the genuine second voices.

### Tier 3 — TS (ruled 1/4 each)

| Cameo faction | game A | game B | game C | status |
|---|---|---|---|---|
| `tiberiansun_gdi` | Shattered Paradise `gdi` | Crystallized Nexus `gdi` | OpenRA TS `gdi` | ⭐ 3 games |
| `tiberiansun_nod` | Shattered Paradise `nod` | Crystallized Nexus `nod` | OpenRA TS `nod` | ⭐ |
| `tiberiansun_cabal` | Shattered Paradise `cab` | ⚠ Combined Arms `scrin`? | — | ⚠ needs a 2nd |
| `tiberiansun_forgotten` | ⭐ Shattered Paradise `mut` | ⚠ Crystallized Nexus `gdf`? | — | ⚠ needs a 2nd |

### Tier 4 — the invented RA2 factions

| Cameo faction | game A | game B | status |
|---|---|---|---|
| `redalert2mod_asianalliance` | MO **China** | ⭐ **Generals Alpha `prc`** | ⭐ exactly as ruled |
| `redalert2mod_syndicate` | MO **Latin Confederation** | ⭐ **Generals Alpha `gla`** | ⭐ GLA is guerrilla / black-market / explosives — the LC archetype |
| `redalert2mod_futuretech` | ⭐ **OpenE2140 `ucs`** | ⚠ Combined Arms `scrin`? | ⚠ needs a 2nd |
| `redalert2mod_naxis` | ⚠ OpenE2140 `ed` | ⛔ none | ⛔ and `ed` may mirror `ucs` |
| `redalert2mod_consortium` | MO **Foehn Revolt** | ⛔ none — `steel` is Steel Talons | ⛔ |
| `redalert2mod_schwarzermond` | ⛔ Earth **2150** LC not on disk | ⛔ none | ⛔ |
| `redalert2mod_tkm` | ⛔ unidentified | ⛔ | ⛔ |
| `redalert_japan` | ⛔ no RA3 mod in the corpus | ⛔ | ⛔ |

### Tier 5 — Dune

| Cameo faction | game A | game B | status |
|---|---|---|---|
| `d2k_ordos` | OpenRA D2K `ordos` | OpenRA Dune II `ordos` | ⭐ measured INDEPENDENT (16% agreement) — genuinely two games |
| `d2k_atreides` · `d2k_harkonnen` | same pair | | ⭐ |
| `d2k_ixian` | ⛔ House Ix is Emperor-only, not on disk | ⛔ | ⛔ |

## §8 — What is still open

1. ⛔ **MO and CnC Reloaded have no faction column** — hand tables. Every Tier-4 mapping above
   depends on it. **Biggest blocker.**
2. ⛔ **DTA** — ask 2026-09-05.
3. ⚠ **Five factions have no second game**: `consortium`, `schwarzermond`, `naxis`, `tkm`,
   `redalert_japan`, plus `d2k_ixian`. They are formula-only until one is found.
4. ⚠ **Three need a second choosing**: `tiberiansun_cabal`, `tiberiansun_forgotten`,
   `redalert2mod_futuretech`.
