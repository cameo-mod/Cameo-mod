## Resume

Task remains paused at the maintainer's instruction while AB-CAMPAIGN-TOOLING is active. Current evidence SHA available at pause: `703d1e826` (campaign tooling branch; MEM source/evidence is the fleet's `DEFECT_2026-10-10_pt7_all_memory.md`). Resume with source/evidence-only analysis unless an explicit maintainer ruling authorizes a bounded reproduction. Do not launch a game or clear `ABORT_PT7` without that ruling. Recheck exclusive lane, free RAM and OpenRA process state first; never run the full `tools/tests` directory in one process.

## Known evidence and limits

The prior watchdog killed PID 8164 at 8,209 MB private memory on the all-arm 45k 6v6 run. The preserved result tree lacked the killed all-arm invocation receipt and memory time series. The all-arm run co-armed 60 switches, so it cannot isolate a switch or subsystem. Writer buffers inspected in the pinned source had local caps, which does not identify total process memory. No causal source attribution or mitigation is supported yet.

## Open gates

- Maintainer's PT7 no-relaunch ruling and `ABORT_PT7` remain binding absent explicit superseding approval.
- A useful diagnostic needs the exact killed run's engine/source/binary provenance and per-process memory timeline or a narrowly scoped approved repro with a stricter stop threshold.
- Current owner switched to higher-priority AB campaign tooling; no MEM investigation or runtime was performed in this turn.
