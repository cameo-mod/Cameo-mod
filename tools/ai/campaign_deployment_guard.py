"""Shared, fail-closed parser for map-local per-seat deployment evidence."""
import pathlib
import re

DEPLOY_RE = re.compile(r"AB_CAMPAIGN_DEPLOY seat=([A-Za-z0-9_.-]+) tick=(\d+) type=([A-Za-z0-9_.-]+)")
NO_DEPLOY_RE = re.compile(r"AB_CAMPAIGN_NO_DEPLOY seat=([A-Za-z0-9_.-]+) tick=(\d+)")
CHECK_RE = re.compile(r"AB_CAMPAIGN_DEPLOYMENT_CHECK tick=(\d+) seat_count=(\d+) expected_count=(\d+)")
WORLD_TICK_RE = re.compile(r"\[WT\s+(\d+)\]")
VERIFIED_TYPES = frozenset(("td_gdi_constructionyard", "td_nod_constructionyard",
	"ra1_allies_constructionyard", "ra1_soviets_constructionyard"))


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
