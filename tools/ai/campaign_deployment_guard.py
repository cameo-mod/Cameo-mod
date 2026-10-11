"""Shared, fail-closed parser for map-local per-seat deployment evidence."""
import pathlib
import re

DEPLOY_RE = re.compile(r"AB_CAMPAIGN_DEPLOY seat=([A-Za-z0-9_.-]+) tick=(\d+) type=([A-Za-z0-9_.-]+)")
NO_DEPLOY_RE = re.compile(r"AB_CAMPAIGN_NO_DEPLOY seat=([A-Za-z0-9_.-]+) tick=(\d+)")
CHECK_RE = re.compile(r"AB_CAMPAIGN_DEPLOYMENT_CHECK tick=(\d+) seat_count=(\d+) expected_count=(\d+)")
WORLD_TICK_RE = re.compile(r"\[WT\s+(\d+)\]")
VERIFIED_TYPES = frozenset(("td_gdi_constructionyard", "td_nod_constructionyard",
	"ra1_allies_constructionyard", "ra1_soviets_constructionyard"))

# A sampling exception is only a benign exit race when no stop condition has
# already been selected. Whitelisting normal states prevents a late sample
# exception from rehabilitating any safety stop (or another failure reason).
_SAFETY_STOPS = frozenset(("MEMORY_KILL", "LOW_FREE_RAM", "STALL", "WALL_TIMEOUT", "INVALID_NO_DEPLOY"))


def sampling_failure_is_exit_race(stop_reason: str | None, *, process_exited: bool,
		valid_sample_count: int) -> bool:
	"""Whether a failed sample can be attributed to a just-exited process.

	The caller must re-check process liveness after the sampling failure. A prior
	valid sample is mandatory: an unmeasured process still fails closed. Existing
	safety stops and non-normal errors are never cleared.
	"""
	return (process_exited and valid_sample_count > 0
		and stop_reason in (None, "PROCESS_EXIT")
		and stop_reason not in _SAFETY_STOPS)


def parse(text: str) -> dict:
	checks = CHECK_RE.findall(text)
	return {"check_tick": int(checks[-1][0]) if checks else None,
		"reported_seat_count": int(checks[-1][1]) if checks else None,
		"expected_seat_count": int(checks[-1][2]) if checks else None,
		"seats": {name: {"tick": int(tick), "type": kind} for name, tick, kind in DEPLOY_RE.findall(text)},
		"missing": [name for name, _ in NO_DEPLOY_RE.findall(text)]}


def merge(current: dict, incoming: dict) -> dict:
	current["seats"].update(incoming["seats"])
	current["missing"].extend(incoming["missing"])
	for key in ("check_tick", "reported_seat_count", "expected_seat_count"):
		if incoming.get(key) is not None: current[key] = incoming[key]
	return current


def valid(evidence: dict, expected_seats: list[str]) -> bool:
	seats = evidence.get("seats", {})
	return (evidence.get("check_tick") == 100
		and evidence.get("reported_seat_count") == len(expected_seats)
		and evidence.get("expected_seat_count") == len(expected_seats)
		and not evidence.get("missing")
		and set(seats) == set(expected_seats)
		and all(type(seats[s].get("tick")) is int and 0 <= seats[s]["tick"] <= 100
			and seats[s].get("type") in VERIFIED_TYPES for s in expected_seats))


def latest_world_tick(path: pathlib.Path) -> int | None:
	path = pathlib.Path(path)
	if not path.is_file(): return None
	with path.open("rb") as stream:
		stream.seek(max(0, path.stat().st_size - 65536))
		text = stream.read().decode("utf-8", errors="replace")
	ticks = WORLD_TICK_RE.findall(text)
	return max((int(tick) for tick in ticks), default=None)


def highest_world_tick(deployment: dict, stall_log: pathlib.Path | None = None, extra_ticks=()) -> int | None:
	"""Highest trustworthy observed world tick. Combines deployment check/seat
	marker ticks, [WT N] clock lines in the stall log and caller-supplied sample
	ticks (e.g. actor samples) so a stale or missing clock can never mask a
	known-advanced game (sol review finding 1)."""
	ticks = [t for t in (deployment.get("check_tick"), *(extra_ticks or ())) if type(t) is int]
	for seat in deployment.get("seats", {}).values():
		if isinstance(seat, dict) and type(seat.get("tick")) is int:
			ticks.append(seat["tick"])
	clock_tick = latest_world_tick(stall_log) if stall_log is not None else None
	if clock_tick is not None:
		ticks.append(clock_tick)
	return max(ticks, default=None)


# Engine Log.FlushInterval is five seconds. Allow two intervals for transport,
# never for simulation deployment: valid() still requires every yard by tick 100.
FLUSH_WAIT_SECONDS = 10.0


class MarkerReader:
	"""Keep split log lines until complete; never interpret a partial terminal check."""
	def __init__(self):
		self.offset = 0
		self.pending = ""

	def read(self, path: pathlib.Path) -> str:
		if not path.is_file(): return ""
		with path.open("r", encoding="utf-8", errors="replace") as stream:
			stream.seek(self.offset)
			self.pending += stream.read()
			self.offset = stream.tell()
		end = self.pending.rfind("\n") + 1
		complete, self.pending = self.pending[:end], self.pending[end:]
		return complete


class DeploymentDeadline:
	"""Fixed transport grace after an independent clock crosses tick 100.

	Explicit terminal negative evidence aborts immediately. Missing evidence waits
	at most two engine flush intervals; observations cannot renew that deadline.
	The existing startup wall bound also remains absolute.
	"""
	def __init__(self, started: float, wall_timeout: float):
		self.started = started
		self.wall_timeout = wall_timeout
		self.tick_100_seen_at = None

	def should_abort(self, evidence: dict, expected_seats: list[str], world_tick: int | None, now: float) -> bool:
		evidence["verified"] = valid(evidence, expected_seats)
		if evidence["verified"]: return False
		if evidence.get("missing") or evidence.get("check_tick") is not None: return True
		if world_tick is not None and world_tick >= 100 and self.tick_100_seen_at is None:
			self.tick_100_seen_at = now
		return (now - self.started >= self.wall_timeout
			or (self.tick_100_seen_at is not None and now - self.tick_100_seen_at >= FLUSH_WAIT_SECONDS))
