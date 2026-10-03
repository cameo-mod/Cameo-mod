"""tune_build_order: scoring, significance, bounded coordinate step, opening bandit, round trip, proposals."""
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "ai"))
import tune_build_order as tbo  # noqa: E402

BASE = "bo__rush__ra1_allies__base"
UP = "bo__rush__ra1_allies__tempo__up100"


def row(arm, score, win=1.0, opening="eco", game="g", personality="rush", faction="ra1_allies", enemy="ra1_soviets"):
    return {"game_uid": game, "arm": arm, "personality": personality, "faction": faction, "enemy_faction": enemy,
            "opening": opening, "knobs": {k: 1000 for k in tbo.KNOBS}, "score": score, "win": win}


def arm_rows(arm, scores, **kw):
    return [row(arm, s, game=f"{arm}{i}", **kw) for i, s in enumerate(scores)]


def test_score_is_win_plus_margin_plus_speed_only_for_wins():
    win, flag = tbo.score_match("won", 9000, 1000, 27000)
    assert flag == 1.0
    assert abs(win - (1.0 + 0.8 + 0.25 * 0.5)) < 1e-9
    loss, flag = tbo.score_match("lost", 1000, 9000, 27000)
    assert flag == 0.0
    assert abs(loss - (-0.8)) < 1e-9, "a loss gets the margin but no win and no speed bonus"
    assert tbo.score_match("won", 0, 0, 100000)[0] == 1.0, "no value traded = margin 0; a slow win has no speed bonus"


def test_undecided_and_too_short_matches_carry_no_signal():
    assert tbo.score_match("undecided", 1, 1, 30000) is None
    assert tbo.score_match("won", 1, 1, tbo.MIN_DURATION_TICKS - 1) is None


def test_welch_z_signs_and_zero_variance():
    a, b = tbo.Stats(), tbo.Stats()
    for x in (1.0, 1.2, 0.8, 1.1, 0.9):
        a.add(x)
    for x in (0.2, 0.0, 0.4, 0.1, 0.3):
        b.add(x)
    assert tbo.welch_z(a, b) > 5
    assert tbo.welch_z(b, a) < -5
    c, d = tbo.Stats(), tbo.Stats()
    for _ in range(3):
        c.add(1.0)
        d.add(1.0)
    assert tbo.welch_z(c, d) == 0.0


def good_rows(n=25):
    rows = arm_rows(BASE, [0.0 + 0.1 * (i % 3) for i in range(n)], win=0.0)
    rows += arm_rows(UP, [1.0 + 0.1 * (i % 3) for i in range(n)])
    return rows


def test_a_significant_arm_is_accepted_and_the_multiplier_is_bounded_by_the_step():
    learned = tbo.empty_learned()
    changes = tbo.write_update(good_rows(), learned)
    assert [(d["knob"], d["new"]) for d in changes["knobs"]] == [("tempo", 1100)]
    assert learned["knobs"][("rush", "ra1_allies")]["tempo"] == 1100


def test_too_few_matches_changes_nothing_even_when_the_gap_is_huge():
    learned = tbo.empty_learned()
    changes = tbo.write_update(good_rows(5), learned)
    assert changes["knobs"] == [] and learned["knobs"] == {}
    assert learned["processed_knobs"] == [], "undecided games stay available for the next write"


def test_an_insignificant_or_negative_arm_is_rejected_but_its_games_are_consumed():
    rows = arm_rows(BASE, [0.5, 0.6, 0.4, 0.5] * 6) + arm_rows(UP, [0.5, 0.4, 0.6, 0.5] * 6)
    learned = tbo.empty_learned()
    assert tbo.write_update(rows, learned)["knobs"] == []
    assert len(learned["processed_knobs"]) == len(rows)
    worse = arm_rows(BASE, [1.0, 1.1, 0.9] * 8) + arm_rows(UP, [0.1, 0.2, 0.0] * 8)
    assert tbo.write_update(worse, tbo.empty_learned())["knobs"] == []


def test_the_learned_multiplier_never_leaves_its_bounds():
    learned = tbo.empty_learned()
    learned["knobs"][("rush", "ra1_allies")] = {"tempo": tbo.LEARN_MAX}
    changes = tbo.write_update(good_rows(), learned)
    assert changes["knobs"] == [], "already at the cap: the step is clamped to no change"
    learned = tbo.empty_learned()
    learned["knobs"][("rush", "ra1_allies")] = {"tempo": 1200}
    tbo.write_update(good_rows(), learned)
    assert learned["knobs"][("rush", "ra1_allies")]["tempo"] == tbo.LEARN_MAX


def test_only_the_best_significant_arm_per_group_is_taken():
    rows = good_rows()
    rows += arm_rows("bo__rush__ra1_allies__greed__up100", [0.6 + 0.1 * (i % 3) for i in range(25)])
    learned = tbo.empty_learned()
    changes = tbo.write_update(rows, learned)
    assert [d["knob"] for d in changes["knobs"]] == ["tempo"]
    assert "greed" not in learned["knobs"][("rush", "ra1_allies")]


def test_a_down_arm_lowers_the_multiplier():
    dn = "bo__rush__ra1_allies__defence__dn100"
    rows = arm_rows(BASE, [0.0 + 0.1 * (i % 3) for i in range(25)], win=0.0) + arm_rows(dn, [1.0 + 0.1 * (i % 3) for i in range(25)])
    learned = tbo.empty_learned()
    tbo.write_update(rows, learned)
    assert learned["knobs"][("rush", "ra1_allies")]["defence"] == 900


def test_opening_posteriors_update_only_with_enough_matches_and_only_once_per_game():
    rows = [row(BASE, 1.0, win=1.0 if i % 4 else 0.0, opening="eco", game=f"e{i}") for i in range(24)]
    rows += [row(BASE, 0.0, win=0.0, opening="fast_tech", game=f"f{i}") for i in range(3)]
    learned = tbo.empty_learned()
    tbo.update_openings(learned, rows)
    post = learned["openings"][("rush", "ra1_allies", "ra1_soviets")]
    assert post["eco"] == [1 + 18, 1 + 6]
    assert post["fast_tech"] == [1, 4]
    again = tbo.update_openings(learned, rows)
    assert again == [] and learned["openings"][("rush", "ra1_allies", "ra1_soviets")]["eco"] == [19, 7], "a processed game counts once"

    few = tbo.empty_learned()
    assert tbo.update_openings(few, rows[:5]) == [] and few["openings"] == {}


def test_the_posterior_total_is_capped_by_halving():
    learned = tbo.empty_learned()
    learned["openings"][("rush", "ra1_allies", "ra1_soviets")] = {"eco": [150, 49]}
    rows = [row(BASE, 1.0, win=1.0, game=f"c{i}") for i in range(25)]
    tbo.update_openings(learned, rows)
    alpha, beta = learned["openings"][("rush", "ra1_allies", "ra1_soviets")]["eco"]
    assert alpha + beta <= tbo.POSTERIOR_CAP + 1 and alpha > beta


def test_learned_file_round_trips_and_stays_readable_by_the_game():
    learned = tbo.empty_learned()
    learned["knobs"][("rush", "ra1_allies")] = {"tempo": 1100, "defence": 900}
    learned["knobs"][("any", "any")] = {"greed": 1050}
    learned["openings"][("rush", "ra1_allies", "ra1_soviets")] = {"eco": [7, 3], "fast_tech": [2, 9]}
    learned["processed_knobs"] = ["abc", "def"]
    text = tbo.format_learned(learned)
    assert text.splitlines()[4] == "BotBuildOrderKnobs:", "the root node the game's parser looks for"
    assert "\tKnobs@rush__ra1_allies:\n\t\ttempo: 1100\n\t\tdefence: 900" in text
    assert "\tOpenings@rush__ra1_allies__vs__ra1_soviets:\n\t\teco: 7 3\n\t\tfast_tech: 2 9" in text
    back = tbo.parse_learned(text)
    assert back == {**learned, "processed_knobs": ["abc", "def"], "processed_openings": []}
    assert tbo.parse_learned(tbo.format_learned(tbo.empty_learned())) == tbo.empty_learned()


def test_current_multiplier_follows_the_games_lookup_chain():
    learned = tbo.empty_learned()
    learned["knobs"][("rush", "any")] = {"tempo": 1050}
    learned["knobs"][("rush", "family_ra1")] = {"tempo": 1080}
    assert tbo.current_multiplier(learned, "rush", "ra1_allies", "tempo") == 1080
    learned["knobs"][("rush", "ra1_allies")] = {"tempo": 1120}
    assert tbo.current_multiplier(learned, "rush", "ra1_allies", "tempo") == 1120
    assert tbo.current_multiplier(learned, "turtle", "ra1_allies", "tempo") == 1000


def test_next_experiment_walks_the_knobs_skips_bounded_steps_and_ignores_expansion():
    learned = tbo.empty_learned()
    exp = tbo.next_experiment([], learned, "rush", "ra1_allies")
    assert (exp["knob"], exp["direction"], exp["old"], exp["new"]) == ("tempo", "up", 1000, 1100)
    assert exp["arm"] == UP and exp["base_arm"] == BASE

    rows = arm_rows(BASE, [0.5] * 25) + arm_rows(UP, [0.5] * 25)
    exp = tbo.next_experiment(rows, learned, "rush", "ra1_allies")
    assert (exp["knob"], exp["direction"]) == ("tempo", "dn")
    rows += arm_rows("bo__rush__ra1_allies__tempo__dn100", [0.5] * 25)

    learned["knobs"][("rush", "ra1_allies")] = {"tempo": tbo.LEARN_MAX}
    exp = tbo.next_experiment(rows, learned, "rush", "ra1_allies")
    assert (exp["knob"], exp["direction"]) == ("greed", "up"), "the capped up step and the measured down step are skipped"

    done = []
    for knob in tbo.TUNABLE:
        for d in ("up", "dn"):
            done += arm_rows(f"bo__rush__ra1_allies__{knob}__{d}100", [0.5] * 25)
    done += arm_rows(BASE, [0.5] * 25)
    assert tbo.next_experiment(done, tbo.empty_learned(), "rush", "ra1_allies") is None
    assert all(e != "expansion" for e in tbo.TUNABLE)


def test_experiment_files_differ_by_exactly_the_one_knob_and_the_spec_names_each_arm():
    learned = tbo.empty_learned()
    learned["knobs"][("rush", "ra1_allies")] = {"greed": 950}
    exp = tbo.next_experiment([], learned, "rush", "ra1_allies")
    files = tbo.experiment_files(learned, "rush", "ra1_allies", exp)
    assert set(files) == {BASE, UP}
    assert "tempo" not in files[BASE]
    assert "tempo: 1100" in files[UP] and "greed: 950" in files[UP] and "greed: 950" in files[BASE]
    spec = tbo.switches_text([BASE, UP])
    assert f"  {UP}:\n    BuildOrderKnobsBotModule:\n      UseLearnedBuildOrder: true\n      LearnedFile: ai/learned/{UP}.yaml" in spec


def test_load_matches_reads_logs_groups_by_arm_and_scores(tmp_path):
    logs = tmp_path / "bo__rush__ra1_allies__base_3" / "Logs"
    logs.mkdir(parents=True)

    def match(uid, name, outcome, killed, lost):
        return {"game_uid": uid, "duration_ticks": 27000, "allies": [], "opponents": [{"faction": "ra1_soviets"}],
                "player": {"name": name, "faction": "ra1_allies", "outcome": outcome, "personality": "rush"},
                "stats": {"kills_cost": killed, "deaths_cost": lost}}

    def situation(uid, name, tick, opening):
        bo = {"personality": "rush", "opening": opening, **{f"base_{k}": 1000 + i for i, k in enumerate(tbo.KNOBS)}}
        return {"game_uid": uid, "player": name, "tick": tick, "own": {"build_order": bo}}

    (logs / "cameo-ai-matches.jsonl").write_text("\n".join(json.dumps(r) for r in [
        match("g1", "Multi0", "won", 9000, 1000), match("g2", "Multi0", "undecided", 1, 1), match("g3", "Multi0", "lost", 1000, 9000)]),
        encoding="utf-8")
    (logs / "cameo-ai-situations.jsonl").write_text("\n".join(json.dumps(r) for r in [
        situation("g1", "Multi0", 100, ""), situation("g1", "Multi0", 900, "eco"), situation("g3", "Multi0", 900, "fast_tech")]),
        encoding="utf-8")
    rows = tbo.load_matches([tmp_path / "bo__rush__ra1_allies__base_3"])
    assert [(r["arm"], r["opening"], round(r["win"])) for r in rows] == [(BASE, "eco", 1), (BASE, "fast_tech", 0)]
    assert rows[0]["knobs"]["tempo"] == 1000 and rows[0]["knobs"]["support"] == 1007
    assert tbo.report(rows, tbo.empty_learned()).startswith("2 scored matches")


def cell_row(arm, score, spawn, i, **kw):
    r = row(arm, score, game=f"{arm}{spawn}_{i}", **kw)
    r.update({"map": "m1", "spawn": spawn})
    return r


def correlated_rows(n_per_cell=15, gain=0.05):
    """Two spawn cells with a huge cell effect (+-2) and a tiny, consistent arm gain: pairing cancels the cell effect."""
    rows = []
    for spawn, offset in ((0, 2.0), (1, -2.0)):
        for i in range(n_per_cell):
            noise = 0.01 * (i % 3)
            rows.append(cell_row(BASE, offset + noise, spawn, i))
            rows.append(cell_row(UP, offset + noise + gain + 0.002 * (i % 2), spawn, i))
    return rows


def test_paired_beats_welch_on_correlated_data():
    rows = correlated_rows()
    [d] = tbo.decisions(rows, tbo.empty_learned())
    assert d["test"] == "paired" and d["n_pairs"] == 30
    assert d["significant"], "paired z is huge"
    welch = tbo.welch_z(tbo.arm_stats(rows)[("rush", "ra1_allies", UP)], tbo.arm_stats(rows)[("rush", "ra1_allies", BASE)])
    assert welch < 1.96 < d["z"], "the cell effect swamps the unpaired test"


def test_unpairable_data_falls_back_to_welch_and_says_so():
    [d] = tbo.decisions(good_rows(), tbo.empty_learned())  # rows carry no map/spawn but share the empty cell -> still pairs by order
    assert d["test"] in ("paired", "welch")
    rows = [dict(r, spawn=i % 2, map=f"m{i}") for i, r in enumerate(good_rows())]  # every row in its own cell: no shared key
    [d] = tbo.decisions(rows, tbo.empty_learned())
    assert d["test"] == "welch" and d["n_pairs"] == 0


def test_holm_critical_values_and_m1_reproduces_196():
    assert tbo.holm_critical_z(1) == [1.96]
    crits = tbo.holm_critical_z(4)
    assert crits[0] > 2.4 and crits[0] > crits[1] > crits[2] > crits[3] == 1.96


def test_holm_rejects_a_lone_z2_among_four_candidates_but_accepts_it_for_one(monkeypatch):
    def fake(z_by_arm):
        zs = iter(z_by_arm)
        monkeypatch.setattr(tbo, "paired_z", lambda a, b, n: (next(zs), 30))
        rows = []
        for knob in ("tempo", "greed", "production", "tech")[:len(z_by_arm)]:
            arm = f"bo__rush__ra1_allies__{knob}__up100"
            rows += arm_rows(arm, [0.5] * 25)
        rows += arm_rows(BASE, [0.4] * 25)
        return tbo.decisions(rows, tbo.empty_learned())

    four = fake([2.0, 0.1, 0.2, 0.3])
    assert not any(d["significant"] for d in four)
    one = fake([2.0])
    assert one[0]["significant"] and one[0]["z_crit_used"] == 1.96


def test_perturbed_arm_matches_do_not_move_the_opening_posterior_but_are_consumed():
    perturbed = [row(UP, 1.0, win=1.0, opening="eco", game=f"p{i}") for i in range(25)]
    learned = tbo.empty_learned()
    assert tbo.update_openings(learned, perturbed) == [] and learned["openings"] == {}
    assert len(set(learned["processed_openings"])) == 25, "skipped games are recorded so they are not reconsidered"
    base = [row(BASE, 1.0, win=1.0, opening="eco", game=f"b{i}") for i in range(25)]
    assert tbo.update_openings(learned, perturbed + base) != []
    assert learned["openings"][("rush", "ra1_allies", "ra1_soviets")]["eco"] == [26, 1]


# ---------------------------------------------------------------- tier 4: SPSA (TIER4_SPSA_SPEC.md section 9)

import math  # noqa: E402

P_, F_ = "rush", "ra1_allies"


def spsa_rows(learned, step, n_per_arm, score_of, noise=None, knob_override=None):
    """Fabricate n_per_arm measured rows per arm for one SPSA step, scored by score_of(knob vector milli)."""
    exp = tbo.next_spsa_experiment([], learned, P_, F_, n_per_arm)
    assert exp["step"] == step
    rows = []
    for side in ("plus", "minus"):
        vec = {k: (knob_override or exp["values"])[k][side] for k in tbo.TUNABLE}
        for i in range(n_per_arm):
            eps = noise(side, i) if noise else 0.0
            r = row(exp[f"{side}_arm"], score_of(vec) + eps, game=f"s{step}{side}{i}", personality=P_, faction=F_)
            r["knobs"] = {**{k: 1000 for k in tbo.KNOBS}, **vec}
            rows.append(r)
    return exp, rows


def quadratic(theta_star):
    def j(vec):
        return -sum((math.log(vec[k]) - math.log(theta_star[k])) ** 2 for k in tbo.TUNABLE)
    return j


def test_spsa_converges_on_a_synthetic_quadratic():
    theta_star = {"tempo": 1100, "greed": 1150, "production": 1050, "tech": 1180,
                  "defence": 920, "power_margin": 880, "support": 1080}

    def noise(side, i):
        return 0.11 * (math.sin(i * 1.3) if side == "plus" else math.cos(i * 2.1))

    learned = tbo.empty_learned()
    steps = 60
    for k in range(steps):
        _, rows = spsa_rows(learned, k, tbo.MIN_MATCHES, quadratic(theta_star), noise)
        tbo.write_spsa_update(rows, learned, tbo.MIN_MATCHES)
    final = learned["knobs"][(P_, F_)]
    assert learned["spsa_steps"][(P_, F_)] == steps
    for knob, target in theta_star.items():
        assert abs(final.get(knob, 1000) - target) <= 200, f"{knob}: {final.get(knob)} vs {target}"
    dist = math.sqrt(sum((math.log(final.get(k, 1000)) - math.log(t)) ** 2 for k, t in theta_star.items()))
    assert dist < 0.20, f"the committed fixture lands ~0.105 at sigma 0.16, got {dist:.3f}"
    assert all(tbo.LEARN_MIN <= v <= tbo.LEARN_MAX for v in final.values())


def test_spsa_zero_noise_approaches_the_target_monotonically():
    theta_star = {"tempo": 1100, "greed": 1150, "production": 1050, "tech": 1180,
                  "defence": 920, "power_margin": 880, "support": 1080}
    j = quadratic(theta_star)
    learned = tbo.empty_learned()

    def dist():
        f = learned["knobs"][(P_, F_)]
        return math.sqrt(sum((math.log(f.get(k, 1000)) - math.log(t)) ** 2 for k, t in theta_star.items()))

    for k in range(60):
        _, rows = spsa_rows(learned, k, tbo.MIN_MATCHES, j)
        tbo.write_spsa_update(rows, learned, tbo.MIN_MATCHES)
    assert dist() < 0.10, "zero noise proves the update direction, not just damping (fixture: ~0.058)"
    for k in range(60, 250):
        _, rows = spsa_rows(learned, k, tbo.MIN_MATCHES, j)
        tbo.write_spsa_update(rows, learned, tbo.MIN_MATCHES)
    assert dist() < 0.05


def test_spsa_perturbation_is_deterministic_signed_and_step_dependent():
    a = tbo.spsa_delta(P_, F_, 0)
    assert a == tbo.spsa_delta(P_, F_, 0), "same (personality, faction, k) -> same vector on any machine"
    assert len(a) == len(tbo.TUNABLE) and all(v in (1, -1) for v in a)
    vectors = {tuple(tbo.spsa_delta(P_, F_, k)) for k in range(8)}
    assert len(vectors) > 1, "different steps change the perturbation"
    scopes = {tuple(tbo.spsa_delta(p, f, 0)) for p, f in ((P_, F_), ("turtle", F_), (P_, "td_gdi"))}
    assert len(scopes) > 1, "different scopes get different vectors"


def test_spsa_two_arm_proposal_naming_files_and_switch_output():
    learned = tbo.empty_learned()
    exp = tbo.next_spsa_experiment([], learned, P_, F_)
    assert (exp["plus_arm"], exp["minus_arm"]) == ("bo__rush__ra1_allies__spsa__k0__plus",
                                                 "bo__rush__ra1_allies__spsa__k0__minus")
    files = tbo.spsa_experiment_files(learned, P_, F_, exp)
    assert set(files) == {exp["plus_arm"], exp["minus_arm"]}
    for knob, d in exp["delta"].items():
        pv, mv = exp["values"][knob]["plus"], exp["values"][knob]["minus"]
        assert (pv - 1000) * d >= 0 or pv == tbo.LEARN_MAX, "the plus arm moves each knob along its delta sign"
        assert (mv - 1000) * d <= 0 or mv == tbo.LEARN_MIN, "the minus arm moves opposite"
        assert f"{knob}: {pv}" in files[exp["plus_arm"]] and f"{knob}: {mv}" in files[exp["minus_arm"]]
    assert "# spsa step 0 plus" in files[exp["plus_arm"]]
    spec = tbo.switches_text([exp["plus_arm"], exp["minus_arm"]])
    assert spec.count("BuildOrderKnobsBotModule") == 2, "one existing-Seam group per arm - no new switch letter (R4)"
    assert f"      LearnedFile: ai/learned/{exp['minus_arm']}.yaml" in spec


def test_spsa_measured_step_waits_for_write_and_k_advances_exactly_once():
    learned = tbo.empty_learned()
    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: 0.0)
    assert tbo.next_spsa_experiment(rows, learned, P_, F_) is None, "a measured pair is not re-proposed"
    tbo.write_spsa_update(rows, learned)
    assert learned["spsa_steps"][(P_, F_)] == 1
    n_processed = len(learned["processed_knobs"])
    changes = tbo.write_spsa_update(rows, learned)
    assert changes["spsa"] == [] and learned["spsa_steps"][(P_, F_)] == 1
    assert len(learned["processed_knobs"]) == n_processed, "consumed games are not reprocessed"
    assert tbo.next_spsa_experiment([], learned, P_, F_)["step"] == 1


def test_spsa_under_sampled_pairs_do_not_write_or_advance_k():
    learned = tbo.empty_learned()
    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES - 1, lambda v: 1.0)
    changes = tbo.write_spsa_update(rows, learned)
    assert changes["spsa"] == [] and learned["spsa_steps"].get((P_, F_)) is None
    assert learned["knobs"] == {} and learned["processed_knobs"] == [], "the pair waits for more matches"


def test_spsa_significantly_negative_z_still_updates_toward_the_minus_arm():
    """R1 regression: when the minus arm wins, the update follows the measured direction - it is not dropped."""
    learned = tbo.empty_learned()
    probe = tbo.next_spsa_experiment([], learned, P_, F_)
    knob = next(k for k in tbo.TUNABLE if probe["delta"][k] == 1)  # plus arm raises it -> -score makes minus win
    exp, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: -v[knob])
    [d] = tbo.write_spsa_update(rows, learned)["spsa"]
    assert d["z"] < -tbo.Z_CRIT and d["step_scale"] == tbo.SPSA_SCALE_FULL, "negative z is directional evidence"
    moved = {m["knob"]: (m["old"], m["new"]) for m in d["moves"]}
    old, new = moved[knob]
    minus = exp["values"][knob]["minus"]
    assert (new - old) * (minus - old) > 0, "the step moves toward the winning minus side (may overshoot to the bound)"
    for m in d["moves"]:  # every moved knob goes toward its minus-arm side (delta sign handled by signed eff)
        tgt = exp["values"][m["knob"]]["minus"]
        assert (m["new"] - m["old"]) * (tgt - m["old"]) > 0
    assert learned["spsa_steps"][(P_, F_)] == 1


def test_spsa_weak_evidence_damps_but_still_updates():
    learned = tbo.empty_learned()

    def big_noise(side, i):  # paired diff noise ~1.0 swamps the tiny signal -> |z| small
        return (0.5 if i % 2 else -0.5) * (1 if side == "plus" else -1)

    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: 0.001 * (v["tempo"] - 1000), big_noise)
    [d] = tbo.write_spsa_update(rows, learned)["spsa"]
    assert abs(d["z"]) < d["z_crit_used"] and d["step_scale"] == tbo.SPSA_SCALE_WEAK
    assert learned["spsa_steps"][(P_, F_)] == 1, "a damped step still consumes its measured pair"


def test_spsa_never_leaves_the_bounds_and_masks_clamped_deltas():
    learned = tbo.empty_learned()
    learned["knobs"][(P_, F_)] = {k: tbo.LEARN_MAX for k in tbo.TUNABLE}
    vals = tbo.spsa_arm_values(learned, P_, F_, 0)
    for k in tbo.TUNABLE:
        assert tbo.LEARN_MIN <= min(vals[k]["minus"], vals[k]["plus"])
        assert max(vals[k]["minus"], vals[k]["plus"]) <= tbo.LEARN_MAX
    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: sum(v.values()))
    tbo.write_spsa_update(rows, learned)
    assert all(tbo.LEARN_MIN <= v <= tbo.LEARN_MAX for v in learned["knobs"][(P_, F_)].values())

    learned = tbo.empty_learned()
    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: 0.0,
                        knob_override={k: {"plus": 1249, "minus": 1251} for k in tbo.TUNABLE})
    [d] = tbo.write_spsa_update(rows, learned)["spsa"]
    assert set(d["masked"]) == set(tbo.TUNABLE) and d["moves"] == []
    assert learned["spsa_steps"][(P_, F_)] == 1, "a fully-masked pair still consumed its matches"


def test_spsa_and_coordinate_arms_share_the_holm_family():
    learned = tbo.empty_learned()
    _, rows = spsa_rows(learned, 0, tbo.MIN_MATCHES, lambda v: -v["tempo"])
    rows += arm_rows(BASE, [0.5] * tbo.MIN_MATCHES)
    rows += arm_rows(UP, [0.5] * tbo.MIN_MATCHES)  # a coordinate arm with z ~ 0 sharing the scope
    spsa_decs = tbo.spsa_decisions(rows, learned)
    coord_decs = tbo.decisions(rows, learned)
    tbo.combined_verdicts(coord_decs, spsa_decs)
    [d] = [x for x in spsa_decs if x["enough"]]
    assert d["z_crit_used"] > tbo.Z_CRIT, "family = coord arms + spsa pair widens the critical z"


def test_spsa_composite_objective_and_load_matches_el_join(tmp_path):
    r = row("a", 1.0)
    r["el_mean_milli"] = 600.0
    assert abs(tbo.composite_score(r) - (1.0 + tbo.EL_WEIGHT * 0.6)) < 1e-9
    assert tbo.composite_score(row("a", 1.0)) == 1.0, "no engagement data -> neutral term"

    logs = tmp_path / "bo__rush__ra1_allies__base_1" / "Logs"
    logs.mkdir(parents=True)
    match = {"game_uid": "g1", "duration_ticks": 27000, "allies": [], "opponents": [{"faction": "ra1_soviets"}],
             "player": {"name": "Multi0", "faction": "ra1_allies", "outcome": "won", "personality": "rush"},
             "stats": {"kills_cost": 9000, "deaths_cost": 1000}}
    situation = {"game_uid": "g1", "player": "Multi0", "tick": 900,
                 "own": {"build_order": {"personality": "rush", "opening": "eco"}}}
    engagements = [
        {"game_uid": "g1", "player": "Multi0", "score": {"total_milli": 400}},
        {"game_uid": "g1", "player": "Multi0", "score": {"total_milli": 200}},
        {"game_uid": "g1", "player": "Multi0", "score": {"total_milli": 900}, "skirmish": True},
    ]
    (logs / "cameo-ai-matches.jsonl").write_text(json.dumps(match), encoding="utf-8")
    (logs / "cameo-ai-situations.jsonl").write_text(json.dumps(situation), encoding="utf-8")
    (logs / "cameo-ai-engagements.jsonl").write_text("\n".join(json.dumps(e) for e in engagements), encoding="utf-8")
    [r] = tbo.load_matches([tmp_path / "bo__rush__ra1_allies__base_1"])
    assert r["el_n"] == 2 and abs(r["el_mean_milli"] - 300) < 1e-9, "skirmish records are excluded"
    assert abs(tbo.composite_score(r) - (r["score"] + tbo.EL_WEIGHT * 0.3)) < 1e-9


def test_spsa_steps_round_trip_and_are_invisible_to_the_game_parser():
    learned = tbo.empty_learned()
    learned["spsa_steps"][(P_, F_)] = 3
    text = tbo.format_learned(learned)
    assert "\tSpsaSteps:\n\t\tStep@rush__ra1_allies: 3\n" in text
    assert tbo.parse_learned(text)["spsa_steps"] == {(P_, F_): 3}
    # BuildOrderLearned.Parse reads only Knobs@*/Openings@* children of the root
    # (BuildOrderKnobsEval.cs:361-403, no else clause): SpsaSteps is ignored by the game (R6).
    assert tbo.parse_learned(tbo.format_learned(tbo.empty_learned())) == tbo.empty_learned()


def test_spsa_trajectory_is_deterministic_end_to_end():
    def run():
        learned = tbo.empty_learned()
        for k in range(5):
            _, rows = spsa_rows(learned, k, tbo.MIN_MATCHES, lambda v: -v["tempo"],
                                lambda s, i: 0.1 * (i % 3 - 1))
            tbo.write_spsa_update(rows, learned)
        return learned["knobs"], learned["spsa_steps"]

    assert run() == run()
