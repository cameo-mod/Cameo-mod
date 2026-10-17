# Deployment guard flush-lag fix

Base master e285d5ab0601b582a69ef0f23f06da5313a1e716; engine remains 4f69df005f. No engine, strategy, YAML, simulation, manifest pins or retry policy changed. Manifest hashes must be regenerated after landing.

Confirmed preserved false negative: C:/cameo-wt/land-campaign/results/map-acceptance-20261011-v3/acceptance_receipt.json rejects 2v2_red_gdi_allies|1337|0 at wall 52.2s with an empty deployment object. The cell's preserved Support/Logs/lua.log has Multi1/Multi3 deployed at tick35, Multi0/Multi2 at tick41 and DEPLOYMENT_CHECK tick100 seat_count4 expected_count4, no NO_DEPLOY marker. Debug tail reaches WT500. Engine OpenRA.Game/Support/Log.cs uses an asynchronous writer, AutoFlush=false and a five-second flush timer. Independent debug visibility is not proof that the Lua evidence has flushed. The preserved capture remains rejected/incomplete; this fix does not rescore it or retry it.

All three consumers (acceptance, campaign runner, A5) now use one shared fixed transport deadline: observing any trustworthy tick>=100 starts a ten-second monotonic wait, covering two engine flush intervals. Further clock samples or missing logs cannot renew it. The existing startup wall bound remains absolute. Valid evidence still requires the exact expected seats, verified construction-yard types, yard ticks<=100 and the tick100 terminal check. Explicit missing-seat or invalid terminal-check evidence aborts immediately. Truly missing logs abort when the grace expires. Memory, host-RAM, identity and general timeout monitoring continue during the wait; no blocking sleep or retry was added.

The shared reader retains incomplete lines so a partially flushed check cannot be parsed as a complete negative verdict. Campaign monitoring also reads deployments when actor/deployment streams are the same file. Normal-exit tail reconciliation consumes shutdown-flushed markers; it never clears a recorded safety/INVALID_NO_DEPLOY stop or rehabilitates a killed cell.

Offline regressions cover five-second delayed Lua delivery after debug WT500, fixed missing-marker expiry despite later/missing clock samples, explicit missing seats, late tick101 yards, wrong seat set, incomplete check lines and the unchanged startup timeout. The monitor-level delayed-flush case exercises the same actor/Lua stream. Existing missing-marker tests now advance fake wall time rather than freezing their clocks.

Validation: five focused unittest modules (deployment_guard, campaign_runner, map_engine_acceptance, a5_memory_parity_probe, ab_campaign_pilot): 75 run, 74 passed, one existing skip; diff-check clean. No full tools/tests, engine build, boot or game launch. Runtime effectiveness remains unmeasured; exact-SHA review and manifest regeneration precede a newly reserved run.

Preserved input SHA256: lua.log 750ae159c078078208ddc692fe399e8226b2ff256cc7ac69edbf2dcd394291c4; acceptance_receipt.json 8f4a4bdf82d8fd2ae1754d38a99dd1f59ca17ea9ed209fd6e1c27ff6098d5fd6.
