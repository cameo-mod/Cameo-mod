"""W8-adjacent helper: bounded committed-memory allocation held for a window.

Touches every page so private bytes actually register (not just reserved).
HARD_CAP_MB = 1 GiB — the coverage matrix's "no 8-GiB allocations" bound makes
the former ~8.7 GiB owned-pressure case inadmissible; that scenario is
descoped to a controlled external rig. What remains is a bounded UNOWNED
bystander for W7/W8 sentinel duty: run OUTSIDE the job so the monitor must
never terminate it even while owned work is stopped.

Stdlib only; writes a ready marker with pid + committed bytes.
"""

import argparse
import json
import os
import pathlib
import sys
import time

HARD_CAP_MB = 1024  # <=1 GiB: the matrix forbids 8-GiB-scale allocations


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
	if not 0.0 <= args.seconds <= 90.0:  # sentinel lifetime bound
		return 2

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
