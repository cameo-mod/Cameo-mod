# Offline raw serializer sizing

This standalone .NET10 harness calls the existing compiled internal `AiEconomyHealthSchema.Raw` and `AcceptedResourceEvidence` methods through reflection. It creates no World, mounts no trait and runs no game. Supply a DLL built from an identified clean logger commit, record that source commit separately, and retain the emitted assembly SHA-256. The harness checks the assembly bytes remain unchanged during measurement. It refuses to overwrite a receipt.

```powershell
dotnet run --project tools/ai/raw-size-fixture/RawSizeFixture.csproj -c Release -- <OpenRA.Mods.Cameo.dll> <new-receipt.json>
```

Declared workloads are two players across45001 ticks, one queue observation/player/50 ticks, and one or three accepted-resource records/player/tick. A second one-delivery workload stresses JSON escaping with64-character identifiers. These are explicit synthetic envelopes, not actual or maximum supported engine event rates. Queue evidence uses the frozen raw record shape and is not authoritative cancellation/placement evidence.

The harness counts actual UTF-8 serialized bytes plus one newline, records and maximum line length. It checks those against the frozen logger's128MiB,200000-record and64KiB-line limits; it does not exercise the writer or disk cost again. Existing accepted writer tests cover overflow behavior. A workload exceeding any bound cannot support complete capture: runtime must yield UNKNOWN rather than silently drop evidence. Changes to serializer or limits require a new exact-head measurement/review.

Measurement on compiled clean logger3741fe1c46947ada382a6680b6b43b1cf99b8711 (assembly SHA a6a4a0620227f7edb184011a7e48aabd6267717af53f464b412e37e5acd19e1e):

| Synthetic raw workload | Bytes | Records | Maximum line incl newline | Fits frozen bounds |
| --- | ---: | ---: | ---: | --- |
| One delivery/player/tick | 20,340,758 | 91,804 | 325 | Yes |
| One delivery, escaped identifiers | 89,616,738 | 91,804 | 1,060 | Yes |
| Three deliveries/player/tick | 59,969,006 | 271,808 | 326 | No: record count |

Health and summary are not regenerated here. The prior approved health-only fixture measured38,046,762 bytes; combining that separate declared health envelope with these raw envelopes gives58,387,520 /127,663,500 /98,015,768 bytes respectively, excluding all summaries, terminal additions and other artifacts. These sums are a planning calculation, not an actual shared capture. Summary and combined campaign bytes remain unknown. All lines fit the writer limit and the analyzer1MiB line ceiling for these workloads only.

This does not approve runtime event coverage, CPU/allocation/disk performance, arbitrary roster capacity, terminal causality, mounting, dual-gate integration or campaign launch. Frozen main logger branch remains unchanged.
