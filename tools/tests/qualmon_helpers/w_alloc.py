"""W6/W8 helper: bounded committed-memory allocation held for a fixed window.

Touches every page so private bytes actually register (not just reserved).
Hard-bounded by --commit-mb (default 64 MiB); anything above ~10 GiB is
refused outright so a miswritten case cannot starve the host.

Used only by w_driver.py under separately admitted JIT stages:
  * W6 (owned): run inside the monitored tree, commit ~8.7 GiB -> monitor must
    stop at MAX_OWNED_PRIVATE_BYTES and clean up the tree.
  * W8 (unowned): run OUTSIDE the job as the --sentinel process; monitor must
    never terminate it even while owned work is stopped.

Stdlib only; writes a ready marker with pid + committed bytes.
"""

import argparse
import json
import os
import pathlib
import sys
import time

HARD_CAP_MB = 10 * 1024


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--commit-mb", type=int, required=True)
	parser.add_argument("--seconds", type=float, default=30.0)
	parser.add_argument("--ready-file")
	parser.add_argument("--work-dir", default=None)
	args = parser.parse_args()

	commit_mb = int(args.commit_mb)
	if commit_mb < 1 or commit_mb > HARD_CAP_MB:
		return 2  # refuse unreasonable sizes — never silently allocate

	blocks = []
	block = 64 * 1024 * 1024
	committed = 0
	try:
		for _ in range((commit_mb * 1024 * 1024 + block - 1) // block):
			chunk = bytearray(block)
			# Touch every page so PrivateCommit registers the memory.
			for off in range(0, block, 4096):
				chunk[off] = 1
			blocks.append(chunk)
			committed += len(chunk)
	except MemoryError:
		committed = sum(len(b) for b in blocks)

	if args.ready_file:
		pathlib.Path(args.ready_file).write_text(json.dumps({
			"pid": os.getpid(), "ppid": os.getppid(),
			"committed_bytes": committed,
			"requested_mb": commit_mb,
		}, indent=2), encoding="utf-8")

	time.sleep(max(0.0, args.seconds))
	del blocks
	return 0


if __name__ == "__main__":
	sys.exit(main())
