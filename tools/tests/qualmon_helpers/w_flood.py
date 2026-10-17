"""D8-real helper: bounded stdout flood.

Writes --mb megabytes to stdout as fast as possible, then exits. The monitor
redirects inner stdout to inner_stdout.log, so this exercises the append-only
output path (file evidence hashing), not a live pipe. CAP_MB = 1 — the
coverage matrix bounds helper stdout at <=1 MiB; larger floods are refused.
"""

import argparse
import sys

CAP_MB = 1


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--mb", type=int, required=True)
	args = parser.parse_args()
	if not 1 <= args.mb <= CAP_MB:  # bounded args enforced — refuse, no clamp
		return 2
	mb = args.mb
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
