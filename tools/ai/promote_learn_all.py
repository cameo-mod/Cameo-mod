#!/usr/bin/env python3
"""LEARN-ALL-02 P1c-2: snapshot PROMOTION. Moves a fitted candidate snapshot into the ACCEPTED one (the file the release reads) after the gates below.

The DEV fitter (fit_learn_all.py --snapshot <work file>) steps a working snapshot after every DEV game. The release build reads only the accepted file
(mods/cameo/ai/learned/learn_all.yaml). Promotion is the one explicit step between them. Nothing here learns, resets or edits a value: it only decides whether the
candidate may replace the accepted file, and replaces it atomically.

Gates (each failure leaves the accepted file untouched; exit code in brackets):
  [2] the candidate (or an existing accepted file) is not a valid snapshot (parse_text_strict: version, ranges, no duplicates, LastSeq == max Games seq, no orphan Applied keys);
  [3] the candidate would LOSE history: every game, applied key and knob of the accepted snapshot must still be in the candidate, and LastSeq may not go down (never reset);
  [4] nothing new: the candidate is byte-identical to the accepted file, or holds no knob at all (an empty snapshot never replaces a learned one).
A missing accepted file is the one first-use case (the candidate becomes the accepted snapshot). The prior accepted file is kept next to the candidate as
learn_all.accepted.before.<LastSeq>.yaml (never overwritten) and the decision is appended to learn_all_promote.log next to the candidate.

Usage: python tools/ai/promote_learn_all.py --candidate <work>/learn_all.yaml [--accepted mods/cameo/ai/learned/learn_all.yaml] [--dry-run]
"""
import argparse
import datetime
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fit_learn_all as f  # noqa: E402


def read(path):
    with open(path, "r", encoding="utf-8") as handle:
        return handle.read()


def history_loss(old, new):
    """Returns a message when `new` drops anything `old` holds, else None."""
    if new.last_seq is None or (old.last_seq is not None and new.last_seq < old.last_seq):
        return "LastSeq goes down (%s -> %s)" % (old.last_seq, new.last_seq)
    for game, seq in old.games.items():
        if new.games.get(game) != seq:
            return "game %s (seq %s) is missing or changed" % (game, seq)
    missing = sorted(old.applied - new.applied)
    if missing:
        return "applied key %s is missing" % missing[0]
    for personality, knobs in old.personalities.items():
        for knob in knobs:
            if knob not in new.personalities.get(personality, {}):
                return "knob %s of %s is missing" % (knob, personality)
    return None


def knob_count(table):
    return sum(len(k) for k in table.personalities.values())


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--candidate", required=True)
    ap.add_argument("--accepted", default=f.DEFAULT_SNAPSHOT)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args(argv)

    candidate_path = os.path.normpath(args.candidate)
    accepted_path = os.path.normpath(args.accepted)
    folder = os.path.dirname(os.path.abspath(candidate_path))
    log_path = os.path.join(folder, "learn_all_promote.log")

    def log(line):
        with open(log_path, "a", encoding="utf-8", newline="\n") as handle:
            handle.write("%s\t%s\n" % (datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"), line))

    def refuse(code, message):
        print(message, file=sys.stderr)
        log("REFUSED exit=%d %s" % (code, message))
        return code

    try:
        new_text = read(candidate_path)
        new = f.parse_text_strict(new_text)
    except (OSError, f.SnapshotError) as exc:
        return refuse(2, "refused: the candidate %s is not a valid snapshot (%s)" % (candidate_path, exc))

    old_text, old = None, None
    if os.path.exists(accepted_path):
        try:
            old_text = read(accepted_path)
            old = f.parse_text_strict(old_text)
        except (OSError, f.SnapshotError) as exc:
            return refuse(2, "refused: the accepted snapshot %s is not valid (%s); it was left untouched" % (accepted_path, exc))

    if knob_count(new) == 0:
        return refuse(4, "refused: the candidate holds no knob (an empty snapshot never replaces a learned one)")
    if old_text is not None and old_text == new_text:
        return refuse(4, "refused: the candidate is identical to the accepted snapshot (nothing new)")
    if old is not None:
        loss = history_loss(old, new)
        if loss is not None:
            return refuse(3, "refused: the candidate would lose accepted history (%s)" % loss)

    if args.dry_run:
        print("would promote %s -> %s (LastSeq %s, %d knobs)" % (candidate_path, accepted_path, new.last_seq, knob_count(new)))
        return 0

    if old_text is not None:
        backup = os.path.join(folder, "learn_all.accepted.before.%s.yaml" % old.last_seq)
        if os.path.exists(backup):
            return refuse(3, "refused: %s already exists (a backup is never overwritten)" % backup)
        with open(backup, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(old_text)
    tmp = accepted_path + ".tmp"
    try:
        with open(tmp, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(new_text)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(tmp, accepted_path)
    except OSError as exc:
        return refuse(2, "refused: the accepted snapshot could not be written (%s); it was left untouched" % exc)
    log("PROMOTED lastseq=%s knobs=%d accepted=%s" % (new.last_seq, knob_count(new), accepted_path))
    print("promoted: LastSeq %s, %d knobs -> %s" % (new.last_seq, knob_count(new), accepted_path))
    return 0


if __name__ == "__main__":
    sys.exit(main())
