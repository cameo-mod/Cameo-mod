#!/usr/bin/env python3
"""THE faction route map: which reference FACTIONS a Cameo faction may draw on — data first.

PRIOR ART: `explain_unit.HOME` maps a Cameo FAMILY (`ra2`, `td_ra1`, `ts`) to its home SOURCES,
and `assign_references` uses it as a tie-break flag inside the score — a preference, never a
filter, and three families wide. This maps a Cameo FACTION to a source's FACTION, and it is a
filter: `asianalliance` may see Generals Alpha `prc` and nothing else in that mod. The two are
composable, not duplicates — HOME still breaks contests among the rows routing admits — and
`explain_unit` is left alone because a fourth private copy of a source list is the exact failure
`reference_lineages.py` was consolidated to end.

⛔ WHY THIS EXISTS. Matching used to search all fifteen sources for any unit with a similar name or
shape, so an Asian Alliance militia could draw the Combined Arms Infiltrator. Maintainer ruling
2026-09-04, after reading that sheet: *"most of the references are bullshit... instead of trying to
match something completely unrelated we now try to map reference faction to our cameo factions."*
Routing removes the question rather than answering it better: an Asian Alliance unit may only see
Mental Omega China and Generals Alpha `prc`, so the Infiltrator was never a candidate at all.

⛔ DATA-ONLY, for the same reason as `reference_lineages.py`: three private copies of the
de-duplication rulings had drifted apart and one carried a live bug. One list, many consumers.
`assign_references.py` reads it; this file imports nothing of its own.

    python tools/balance/faction_routes.py              # the matrix, with measured row counts
    python tools/balance/faction_routes.py --check      # validation only, exit 1 on a problem

⚠ THE FACTION IDS HERE ARE THE TREE'S, NOT THE MATRIX DOCUMENT'S. `FACTION_REFERENCE_MATRIX.md`
was written with ids like `redalert2mod_asianalliance` and `tiberiandawn_gdi`; the mod's own
`InternalName`s — and every actor prefix in the ledger — are `asianalliance` and `td_gdi`. The
artifact wins. A route keyed on a name that does not exist routes nothing, silently, which is the
exact failure `reference_lineages` was consolidated to prevent, so `validate()` fails on one.

⚠ A ROUTE IS NOT A STAT. It decides only WHICH reference units a Cameo unit may be compared with,
i.e. where the unit sits in its class's distribution. The balance formula still prices it.
"""
from __future__ import annotations

import sys

if hasattr(sys.stdout, "reconfigure"):
    # Without this the validator CRASHES printing its own problem report on a
    # cp1252 console — the failure path was the one path never exercised.
    sys.stdout.reconfigure(encoding="utf-8")

# ── Cameo's own factions, as the mod declares them ────────────────────────────────────────────
# Longest-prefix wins: `ra2_allies` must beat `ra2`, `ts_gdi` must beat `ts`.
CAMEO_FACTIONS = (
    "td_gdi", "td_nod", "ra1_allies", "ra1_soviets", "ra2_allies", "ra2_soviets", "yuri",
    "ts_gdi", "ts_nod", "cabal", "forgotten",
    "asianalliance", "latinsyndicate", "steelconsortium", "futuretech", "naxis",
    "schwarzermond", "tkm", "japan",
    "ixian", "ordos", "harkonnen", "atreides", "corrino",
)

# ── The routes that are RULED AND AVAILABLE today ─────────────────────────────────────────────
# {cameo faction: ((source label, (faction tokens...)), ...)}
#
# ⚠ The source label must be the label AS THE DE-DUPLICATED POOL SEES IT. `Yuri's Revenge on
# OpenRA` and `OpenRA RA2 official` are collapsed into `Romanov's Vengeance` by
# `reference_lineages.RULED_LINEAGES` and are therefore NOT routable labels — routing to one
# reaches zero rows.
ROUTES = {
    # Tier 1 — TD and RA1, ruled 1/3 each. DTA is the missing third; see PENDING.
    "td_gdi":      (("Combined Arms", ("gdi",)), ("OpenRA Tiberian Dawn", ("gdi",))),
    "td_nod":      (("Combined Arms", ("nod",)), ("OpenRA Tiberian Dawn", ("nod",))),
    "ra1_allies":  (("Combined Arms", ("allies",)), ("OpenRA Red Alert", ("allies",))),
    "ra1_soviets": (("Combined Arms", ("soviet",)), ("OpenRA Red Alert", ("soviet",))),

    # Tier 2 — RA2, ruled 1/6 and achievable 1/4. RV and Valiant Shades are the two genuine
    # voices; MO and CnC Reloaded are ruled in and blocked on a faction column (PENDING).
    # ⚠ RV's Yuri faction is spelled `psicorps`, not `yuri`.
    "ra2_allies":  (("Romanov's Vengeance", ("allies",)), ("Valiant Shades", ("allies",))),
    "ra2_soviets": (("Romanov's Vengeance", ("soviets",)), ("Valiant Shades", ("soviets",))),
    "yuri":        (("Romanov's Vengeance", ("psicorps",)), ("Combined Arms", ("yuri",))),

    # Tier 3 — TS, ruled 1/4 each.
    "ts_gdi": (("Shattered Paradise", ("gdi",)), ("Crystallized Nexus", ("gdi",)),
               ("OpenRA Tiberian Sun", ("gdi",))),
    "ts_nod": (("Shattered Paradise", ("nod",)), ("Crystallized Nexus", ("nod",)),
               ("OpenRA Tiberian Sun", ("nod",))),
    # ⭐ Shattered Paradise's `mut` IS Cameo's Forgotten — a direct counterpart no name matcher
    # would have found. `cab` is likewise CABAL. Both still want a second game (see
    # OPEN_SECOND_GAME).
    "cabal":     (("Shattered Paradise", ("cab",)),),
    "forgotten": (("Shattered Paradise", ("mut",)),),

    # Tier 4 — the invented RA2 factions. Game A is Mental Omega for four of them and is blocked
    # on MO's missing faction column, so what is wired here is game B.
    "asianalliance":  (("Generals Alpha", ("prc",)),),
    "latinsyndicate": (("Generals Alpha", ("gla",)),),
    # ⭐ THE MIRROR-MERGE RULE (maintainer 2026-09-04): *"since they are nearly identical we just
    # regard them as one big faction with twice the units as reference"*. OpenHV's `sc` and `yi`
    # are near-mirrors — identical composition, swapped weapon flavour — so they are ONE voice of
    # 80 units rather than two voices or a coin toss. Two tokens on one source route is exactly
    # that: the source still offers at most one reference per Cameo unit.
    "steelconsortium": (("OpenHV", ("sc", "yi")),),
    "futuretech":      (("OpenE2140", ("ucs",)),),
    "naxis":           (("OpenE2140", ("ed",)),),

    # Tier 5 — Dune. Measured INDEPENDENT (16% agreement), so genuinely two games.
    "ordos":     (("OpenRA Dune 2000", ("ordos",)), ("OpenRA Dune II", ("ordos",))),
    "atreides":  (("OpenRA Dune 2000", ("atreides",)), ("OpenRA Dune II", ("atreides",))),
    "harkonnen": (("OpenRA Dune 2000", ("harkonnen",)), ("OpenRA Dune II", ("harkonnen",))),
}

# ── Unblocked 2026-09-05 by the INI extraction ────────────────────────────────────────────────
# Every route below was PENDING on data that is now on disk and extracted to
# `docs/reference/ini_corpus.json`. Faction names are the sources' OWN `Owner=` country names,
# verified against the corpus — not the names the PENDING entries guessed at, several of which
# did not exist (see the note under PENDING).
#
# ⚠ Mental Omega ships SUB-FACTION countries, not the three sides. The maintainer assigned each
# sub-faction its own Cameo destination (2026-09-05), which covers all twelve MO countries:
#     Europeans->ra2_allies  USSR->ra2_soviets  PsiCorps+Headquaters->yuri  Chinese->asianalliance
#     Latin->latinsyndicate  Guild1/2/3->steelconsortium  ScorpionCell->tkm
#     UnitedStates->futuretech  Pacific->japan
INI_ROUTES = {
    "td_gdi":          (("DTA Classic", ("GDI",)),),
    "td_nod":          (("DTA Classic", ("Nod",)),),
    "ra1_allies":      (("DTA Classic", ("Allies",)),),
    "ra1_soviets":     (("DTA Classic", ("Soviet",)),),
    "ra2_allies":      (("Mental Omega", ("Europeans",)),
                        ("CnC Reloaded", ("AlliesCountry",))),
    "ra2_soviets":     (("Mental Omega", ("USSR",)),
                        ("CnC Reloaded", ("SovietCountry",))),
    "yuri":            (("Mental Omega", ("PsiCorps", "Headquaters")),
                        ("CnC Reloaded", ("YuriCountry",))),
    "ts_gdi":          (("CnC Reloaded", ("GDICountry",)),),
    "ts_nod":          (("CnC Reloaded", ("NodCountry",)),),
    "asianalliance":   (("Mental Omega", ("Chinese",)),
                        ("Rise of the East", ("China",))),
    "latinsyndicate":  (("Mental Omega", ("Latin",)),),
    "steelconsortium": (("Mental Omega", ("Guild1", "Guild2", "Guild3")),),
    "tkm":             (("Mental Omega", ("ScorpionCell",)),
                        ("Rise of the East", ("Iraq",))),
    "futuretech":      (("Mental Omega", ("UnitedStates",)),),
    "japan":           (("Mental Omega", ("Pacific",)),),
}

for _f, _r in INI_ROUTES.items():
    ROUTES[_f] = tuple(ROUTES.get(_f, ())) + _r

# ⛔ CASE-FOLD THE TOKENS, ONCE, HERE. `peer_factions()` lowercases the corpus column so that
# DOC5's `nod` and an INI mod's `NodCountry` are comparable at all — but the routes above are
# written in each source's OWN casing, because that is what a human verifies against the rules
# file. Comparing the two directly matched nothing: every one of the 15 INI routes resolved to
# ZERO rows in `allows()` while `--check` reported the token as missing and listed it, lowercased,
# in the very same line ("has no faction token 'Chinese' (has: chinese, ...)"). The declarations
# keep their readable casing; the comparison is normalised.
ROUTES = {_f: tuple((_src, tuple(_t.lower() for _t in _toks)) for _src, _toks in _r)
          for _f, _r in ROUTES.items()}

# ── Ruled, but the source is not in the corpus yet ────────────────────────────────────────────
# These are NOT speculation: each is a maintainer ruling whose data is missing. Listed so the
# weighting a faction actually gets today is visible against the weighting it was ruled.
PENDING = {
    # ── RESOLVED 2026-09-05. Fifteen entries left this table when the INI corpus landed; the
    # routes they became are in INI_ROUTES above. Two of the old entries were WRONG about the
    # world, not merely blocked, and both are recorded here so the mistake is not repeated:
    #
    #   * `cabal` and `forgotten` were routed to CnC Reloaded. **CnC Reloaded has no CABAL and no
    #     Forgotten country.** Its full Owner= list is GDICountry, NodCountry, AlliesCountry,
    #     SovietCountry, YuriCountry, RobotCountry (+ variants) and the vanilla nation slots.
    #     Their real source is Shattered Paradise's `cab` / `mut`.
    #   * `tkm` was routed to "Rise of the East / GLA". **RotE has no GLA country.** Ruled
    #     2026-09-05: TKM takes Mental Omega ScorpionCell + Rise of the East Iraq.
    #
    # The blocker on the rest was never "no faction column" — every INI source carries `Owner=`.
    # It was that nobody had read the files. See docs/design/REFERENCE_EXTRACTION_PLAN.md §3.5.
    # (cabal and forgotten are NOT pending — they are already ROUTED to Shattered Paradise
    # `cab` / `mut` above. What they wanted was a SECOND game, and CnC Reloaded cannot be it:
    # it has no CABAL and no Forgotten country. They stay in OPEN_SECOND_GAME.)

    # ⭐ THE DUNE TIER IS THE THINNEST IN THE CORPUS AND THE MAINTAINER IS FIXING IT (2026-09-04):
    # *"The dune factions will need the OpenRA dune x emperor battle for dune x some different
    # dune mods I have here I need to share with you tomorrow and of course also the spice wars
    # game!"* Measured, the need is real — `ordos` has 25 Cameo units against SEVEN routed
    # reference rows, and its whole exchange rate rests on 4 pairs.
    #
    # ⏸ DEFERRED BY RULING R10 (2026-09-05): the C&C family is built first and the Dune, Warcraft
    # and StarCraft factions come later from their OWN pools. Safe because ZERO of the 22 classes
    # in use are deferred-only. ⛔ But R11 makes these release-blocking for Cameo 1.0: every
    # faction must ship on the new formula WITH reference data.
    "ordos":     (("Emperor: Battle for Dune", "Ordos", "mod not supplied"),
                  ("Dune: Spice Wars", "Ordos", "game not supplied"),),
    "atreides":  (("Emperor: Battle for Dune", "Atreides", "mod not supplied"),
                  ("Dune: Spice Wars", "Atreides", "game not supplied"),),
    "harkonnen": (("Emperor: Battle for Dune", "Harkonnen", "mod not supplied"),
                  ("Dune: Spice Wars", "Harkonnen", "game not supplied"),),
    # ⭐ EMPEROR UNBLOCKS THE TWO DUNE FACTIONS THAT HAVE NO SOURCE AT ALL. It fields the sub-houses
    # the earlier survey found nowhere: House Ix, and the Imperial/Sardaukar house for Corrino.
    # Both were listed as permanently formula-only on the strength of "not on disk" — which was a
    # claim about the corpus, not about the world.
    "ixian":   (("Emperor: Battle for Dune", "Ix", "mod not supplied"),),
    "corrino": (("Emperor: Battle for Dune", "Imperial / Sardaukar", "mod not supplied"),),
}


# ── No route at all: FORMULA-ONLY, by ruling ──────────────────────────────────────────────────
# *"They should be formula-only from a grounded class anchor"* — the ruling already made for units
# with no reference, rather than forcing a bad match, which is what produced the rejected sheet.
# ── Sources whose `Owner=` is too broad to use directly ───────────────────────────────────────
# MEASURED 2026-09-05, mean owners per costed unit: DTA Classic 1.3, Mental Omega 4.4, Rise of
# the East 5.0, **CnC Reloaded 11.6 (median 13 of ~23 countries)**. At that breadth a faction
# roster stops describing the faction: CnCR's `GDICountry` (453 units) and `NodCountry` (450)
# SHARE 346 of them, so a naive GDI reference is 76% identical to the Nod one.
#
# Maintainer ruling 2026-09-05: for these sources a faction's reference roster is the units it
# owns that the OPPOSING routed factions of the same source do not — 107 GDI-only, 104 Nod-only.
# Smaller, but discriminating, which is the entire point of a reference. The shared pool
# describes the MOD, not any faction inside it.
#
# ⚠ RE-MEASURED 2026-09-06 once the corpus was wired into `reference_distribution` — the first
# time the overlap could be measured where it actually MATTERS, between the CAMEO factions a
# source feeds rather than between its own countries. Worst pair overlap (Jaccard) per source:
#
#     Mental Omega     97%   asianalliance / latinsyndicate     mean 5.8 owners
#     CnC Reloaded     81%   ra2_soviets / yuri                 mean 11.8
#     OpenRA Tib.Dawn  45%   td_gdi / td_nod                    mean 1.5
#     Rise of the East 36%   asianalliance / tkm                mean 5.3
#     DTA Classic      33%   td_gdi / td_nod                    mean 1.6
#     everything else  <=13%
#
# ⭐ THE 33-45% BAND IS NOT ROT — it is what a C&C roster honestly looks like: OpenRA Tiberian
# Dawn and DTA give both sides the same harvester, MCV and power plant. The pathology is the top
# two, and MENTAL OMEGA IS WORSE THAN THE CASE THAT ESTABLISHED THE RULE: at 97%, Cameo's Asian
# Alliance and Latin Syndicate would have received the same reference roster with 3% to tell them
# apart. MO is therefore added under the SAME 2026-09-05 ruling, not a new one.
# Rise of the East stays a full voice: 36% sits inside the honest band despite its 5.3 mean.
EXCLUSIVE_ONLY = {
    "CnC Reloaded": "median unit owned by 13 of ~23 countries; GDICountry/NodCountry share 76%; "
                    "81% of the ra2_soviets roster is also the yuri roster",
    "Mental Omega": "ships sub-faction countries and gives most units to all of them; 97% of the "
                    "asianalliance roster is also the latinsyndicate roster",
}

# {source: every faction token that source routes to ANY Cameo faction} — the rivals an
# EXCLUSIVE_ONLY unit must NOT also belong to. Derived from ROUTES so it cannot drift.
ROUTED_TOKENS = {}
for _f, _r in ROUTES.items():
    for _src, _toks in _r:
        ROUTED_TOKENS.setdefault(_src, set()).update(_toks)


# ── What "exclusive" MEANS depends on how the source groups its countries ─────────────────────
# ⭐ MEASURED 2026-09-06 by signature — the distinct sets of routed countries that own a unit.
# The two EXCLUSIVE_ONLY sources are built completely differently and one rule cannot serve both:
#
#   CnC Reloaded  has real per-faction pools — nodcountry 86 units, gdicountry 61, sovietcountry
#                 42, alliescountry 41, yuricountry 32 — under a 284-unit universal pool. Country
#                 exclusivity is the right cut and yields 32-86 discriminating units per faction.
#
#   Mental Omega  has almost NO per-country pool: 3 to 7 units each, out of ~233 owned. What it
#                 actually models is the SIDE, and the signature says so exactly —
#                     108  all twelve countries          (the mod's common pool)
#                      61  the nine non-Foehn            (Allied + Soviet + Epsilon)
#                      60  chinese, latin, ussr          SOVIET
#                      58  europeans, pacific, us        ALLIED
#                      49  guild1, guild2, guild3        FOEHN
#                      41  headquaters, psicorps, sc     EPSILON
#                 Country exclusivity here is not strict, it is EMPTY: it cut `japan` to 6 units
#                 and `yuri` to 9. The nine Cameo factions routed to MO countries are drawing on
#                 a source that only distinguishes four sides.
#
# So the cut is declared as a PARTITION of each source's routed tokens, and a unit is admitted
# only when every routed country owning it falls inside ONE cell of that partition. Absent a
# declaration the cells are the per-Cameo-faction token sets, which is country exclusivity and
# also keeps the maintainer's multi-token routes intact (guild1/2/3 -> steelconsortium is one
# cell, so a unit owned by all three is not "shared").
EXCLUSIVITY_GROUPS = {
    "Mental Omega": (
        ("europeans", "pacific", "unitedstates"),          # Allied
        ("chinese", "latin", "ussr"),                      # Soviet
        ("headquaters", "psicorps", "scorpioncell"),       # Epsilon
        ("guild1", "guild2", "guild3"),                    # Foehn
    ),
}


def exclusivity_cells(src):
    """The partition of `src`'s routed tokens that defines "not shared" for that source."""
    declared = EXCLUSIVITY_GROUPS.get(src)
    if declared:
        return [frozenset(g) for g in declared]
    cells = []
    for _, routes in ROUTES.items():
        toks = {t for s_, ts in routes if s_ == src for t in ts}
        if toks:
            cells.append(frozenset(toks))
    return cells


def is_shared(row, src):
    """True when this row's routed owners straddle more than one cell — it describes the MOD."""
    owned = peer_factions(row) & ROUTED_TOKENS.get(src, set())
    return not any(owned <= cell for cell in exclusivity_cells(src))

UNROUTED = {
    # ⭐ `tkm` and `japan` LEFT this table on 2026-09-05 — both are routed now. TKM takes Mental
    # Omega ScorpionCell + Rise of the East Iraq (its old note said "Rise of the East is not
    # supplied"; it is, and it has no `gla` country — Iraq is the ruled analogue). Japan takes
    # Mental Omega Pacific Front, which is a closer archetype than the RA3 Empire its note asked
    # for and which no mod in the corpus provides.
    "schwarzermond": "Earth 2150 Lunar Corporation is the documented inspiration and is not on "
                     "disk. OpenE2140 is Earth 2140. ⏰ The maintainer raised Foehn Revolt as a "
                     "possible partial source (2026-09-05) — not yet ruled.",
    "ixian": "House Ix is Emperor-only. ⏰ UNBLOCKS when the maintainer supplies Emperor: Battle "
             "for Dune — see PENDING.",
    "corrino": "House Corrino appears only as a Dune II/D2K campaign side, with no buildable "
               "roster in the extracted corpus. ⏰ UNBLOCKS with Emperor's Imperial/Sardaukar "
               "house — see PENDING.",
}


# ── Ruled-open: a route exists but the matrix marks the second game as unchosen ───────────────
# Reported, never silently satisfied. A faction here still routes on what it has.
OPEN_SECOND_GAME = {
    "cabal": "Shattered Paradise `cab` only — a second game is unchosen.",
    "forgotten": "Shattered Paradise `mut` only — a second game is unchosen.",
    "futuretech": "OpenE2140 `ucs` only. Combined Arms `scrin` is RESERVED for the upcoming Cameo "
                  "Scrin faction and must not be spent here.",
    "naxis": "OpenE2140 `ed` only — and `ed` may mirror `ucs`, which is FutureTech's. ⛔ Measured "
             "worse than thin: `ed`'s four infantry rows are Androids A01-A04 at HP 28/28/28/20 "
             "and speed 50/50/50/50, so the source cannot place a single Naxis infantryman.",
    "ordos": "OpenRA D2K + Dune II is two games but only 7 routed rows. ⏰ Emperor and Spice Wars "
             "are ruled in and pending — see PENDING.",
    "atreides": "As `ordos`.",
    "harkonnen": "As `ordos`.",
    "asianalliance": "Generals Alpha `prc` only until Mental Omega gains a faction column.",
    "latinsyndicate": "Generals Alpha `gla` only until Mental Omega gains a faction column.",
    "steelconsortium": "OpenHV (merged) only until Mental Omega gains a faction column.",
}

# Cameo factions that exist in the mod but have no C&C-lineage counterpart anywhere in the corpus,
# and are not expected to: the crossover rosters. They are formula-only and that is not a gap.
NO_COUNTERPART_EXPECTED = ("wc2_humans", "wc2_orcs", "terran", "zerg", "protoss")

_BY_LENGTH = tuple(sorted(CAMEO_FACTIONS, key=len, reverse=True))


def faction_of(actor_id):
    """The Cameo faction an actor belongs to, by longest declared prefix. None if unknown.

    ⚠ Longest-first matters: `ra2_allies_gi` must not resolve to `ra2`, and there is no `ra2`
    faction to resolve to — `ra2_*` actors that are neither allies nor soviets are the RA2 SHARED
    pack and legitimately have no faction.
    """
    if not actor_id:
        return None
    for fac in _BY_LENGTH:
        if actor_id == fac or actor_id.startswith(fac + "_"):
            return fac
    return None


def routes_for(faction):
    """((source, frozenset(tokens)), ...) — empty when the faction is formula-only."""
    return tuple((src, frozenset(toks)) for src, toks in ROUTES.get(faction, ()))


def routed_sources(faction):
    return frozenset(src for src, _ in ROUTES.get(faction, ()))


def peer_factions(row):
    """The faction tokens on one peer row. The column is `/`-separated; `—` means untagged."""
    raw = (row.get("faction") or "").strip()
    if not raw or raw in {"—", "-", "?"}:
        return frozenset()
    return frozenset(t.strip().lower() for t in raw.split("/") if t.strip())


def allows(faction, row):
    """May a Cameo unit of `faction` use this peer row as a reference?

    ⛔ AN UNTAGGED ROW IS NOT ADMITTED. Half the corpus carries no faction, and admitting those
    "just in case" reinstates exactly the cross-faction matching the ruling removed — an untagged
    Combined Arms row would be visible to every Cameo faction at once. A route is a claim about
    identity; a missing tag is the absence of one.
    """
    for src, toks in ROUTES.get(faction, ()):
        if row.get("source") != src:
            continue
        mine = peer_factions(row) & frozenset(toks)
        if not mine:
            continue
        # ⛔ THE EXCLUSIVITY RULE, ENFORCED HERE AND NOT ONLY DECLARED. `EXCLUSIVE_ONLY` was
        # honoured by `faction_profile.py` and ignored by this function, so the ruling shaped the
        # faction PROFILES while the reference ROSTERS — the thing units are actually priced
        # against — kept the shared pool. A unit owned by several of a source's routed factions
        # describes the mod, not any faction in it, so it is admitted to none of them.
        # ⚠ RIVALS ARE THE OTHER *CAMEO* FACTIONS, NOT THE OTHER SOURCE COUNTRIES. Counting
        # source countries cancels the maintainer's own multi-token routes: PsiCorps AND
        # Headquaters are both `yuri`, Guild1/2/3 are all `steelconsortium`, and a unit owned by
        # two of them is not shared with anybody — it is the SAME Cameo faction twice. Measured
        # first: the country-counting version cut `japan` from 233 rows to 6 and `yuri` to 59.
        if src in EXCLUSIVE_ONLY and is_shared(row, src):
            continue
        return True
    return False


def validate(rows):
    """Check every ruled route against the corpus. Returns a list of problem strings.

    ⚠ THE POINT OF THIS FUNCTION. `reference_lineages` was consolidated because a ruled label
    (`RA2/YR`) did not match the pool's label (`RA2/YR (raw INI)`) and the ruling silently did
    nothing for weeks. A route is the same shape of claim and fails the same way.
    """
    problems = []
    labels = {r.get("source") for r in rows}
    by_source = {}
    for r in rows:
        by_source.setdefault(r.get("source"), set()).update(peer_factions(r))
    for faction, routes in sorted(ROUTES.items()):
        if faction not in CAMEO_FACTIONS:
            problems.append(f"{faction}: not a declared Cameo faction")
        for src, toks in routes:
            if src not in labels:
                problems.append(f"{faction}: source {src!r} is not in the de-duplicated corpus "
                                f"(collapsed lineage, or a label typo)")
                continue
            for tok in toks:
                if tok not in by_source.get(src, set()):
                    problems.append(f"{faction}: {src!r} has no faction token {tok!r} "
                                    f"(has: {', '.join(sorted(by_source.get(src, ()))) or '—'})")
    for faction in sorted(set(UNROUTED) & set(ROUTES)):
        problems.append(f"{faction}: listed BOTH as routed and as formula-only")
    return problems


def _main():
    import argparse
    import pathlib
    import sys

    sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
    import reference_distribution as rd    # noqa: E402  (CLI only — the module stays data-only)

    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--check", action="store_true", help="validation only; exit 1 on a problem")
    args = ap.parse_args()

    rows = rd.peer_rows()
    problems = validate(rows)
    if not args.check:
        print(f"corpus: {len(rows)} peer rows from {len({r['source'] for r in rows})} sources\n")
        print(f"  {'cameo faction':<18}{'reference source':<26}{'tokens':<16}{'rows':>6}")
        for faction in sorted(ROUTES):
            for src, toks in ROUTES[faction]:
                n = sum(1 for r in rows
                        if r.get("source") == src and (peer_factions(r) & frozenset(toks)))
                flag = "  ⛔ ZERO" if not n else ""
                print(f"  {faction:<18}{src[:25]:<26}{'/'.join(toks)[:15]:<16}{n:>6}{flag}")
        print(f"\nrouted factions   : {len(ROUTES)} of {len(CAMEO_FACTIONS)} declared")
        print(f"formula-only      : {', '.join(sorted(UNROUTED)) or '—'}")
        print(f"second game OPEN  : {', '.join(sorted(OPEN_SECOND_GAME)) or '—'}")
        pend = sum(len(v) for v in PENDING.values())
        print(f"ruled but PENDING : {pend} routes across {len(PENDING)} factions "
              f"(missing source data)")
    if problems:
        print("\n⛔ PROBLEMS")
        for p in problems:
            print(f"  {p}")
        return 1
    print("\n✅ every ruled route resolves against the corpus")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
