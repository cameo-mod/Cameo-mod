# AI harvester gate

Two `hard` bots, each starting with a construction yard, two power buildings, a
refinery, a vehicle factory and 10000 credits:

* **TkmBot** (`tkm`): its refinery is in `RefineryTypes`, and its harvester only
  reaches `HarvesterTypes` through the harvester role (AI_ARCHITECTURE.md §2.8).
* **AtreidesBot** (`atreides`, Dune 2000): neither its refinery nor its yard is
  listed, so `BaseBuilderBotModuleCA` keeps unit production paused. It needs the
  refinery and conyard roles, and it records that gap until they land.

A refinery gives harvesters away (`FreeActor`: TKM 1, Atreides 2 with the carryall
delivery), and the base builder adds refineries, so the raw count proves nothing.
The script reports `extra = harvesters - free x refineries`, the harvesters a bot
actually built, at ticks 1500, 3000, 4500 and 6000.

Run `python tools/tests/ai_harvester_gate.py [--min TkmBot=3]`. It asserts exit
code 0 and no new `exception-*.log`, then reads the samples from `lua.log`. Runs
vary: 2026-09-27, 4 paired runs, TKM built +1/+1 without the role and +5/+6 with it.
