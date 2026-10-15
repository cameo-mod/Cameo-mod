"""D8/W-adjacent helper: bounded stdout flood.

Writes --mb megabytes to stdout as fast as possible, then exits. The monitor
redirects inner stdout to inner_stdout.log, so this exercises the append-only
output path (large file evidence hashing), not a live pipe. Hard cap 256 MiB.
"""

import argparse
import os
import sys

CAP_MB = 256


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--mb", type=int, required=True)
	args = parser.parse_args()
	mb = max(1, min(args.mb, CAP_MB))
	chunk = b"qualmon-flood " * 4096  # ~56 KiB of repetitive bytes
	target = mb * 1024 * 1024
	out = sys.stdout.buffer
	sent = 0
	while sent < target:
		n = min(len(chunk), target - sent)
		out.write(chunk[:n])
		sent += n
	out.flush()
	return 0


if __name__ == "__main__":
	sys.exit(main())
