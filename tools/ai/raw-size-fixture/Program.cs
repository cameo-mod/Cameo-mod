using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Standalone serializer measurement. Never creates a World or loads a mod.
if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: RawSizeFixture <frozen OpenRA.Mods.Cameo.dll> <new receipt.json>");
    return 2;
}

var dll = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var assemblyDigest = SHA256.HashData(File.ReadAllBytes(dll));
if (File.Exists(output))
{
    Console.Error.WriteLine("Receipt exists; choose a new path.");
    return 2;
}

AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var dependency = Path.Combine(Path.GetDirectoryName(dll)!, name.Name + ".dll");
    return File.Exists(dependency) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(dependency) : null;
};
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
var schema = assembly.GetType("OpenRA.Mods.Cameo.Traits.AiEconomyHealthSchema", throwOnError: true)!;
var flags = BindingFlags.Static | BindingFlags.NonPublic;
var raw = schema.GetMethod("Raw", flags) ?? throw new InvalidOperationException("Raw serializer unavailable");
var aggregateType = assembly.GetType("OpenRA.Mods.Cameo.Traits.EconomyAcceptedTick", throwOnError: true)!;
var instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
var add = aggregateType.GetMethod("Add", instanceFlags)!;
var take = aggregateType.GetMethod("TryTakeBefore", instanceFlags)!;
var results = new List<object>();
foreach (var (label, perTick, stress) in new[]
{
    ("two-player-one-delivery-per-tick", 1, false),
    ("two-player-escaped-identifiers", 1, true),
    ("two-player-three-deliveries-per-tick", 3, false),
    ("two-player-three-deliveries-escaped-identifiers", 3, true)
})
{
    var sequence = new int[2];
    var aggregates = Enumerable.Range(0, 2).Select(_ => Activator.CreateInstance(aggregateType, nonPublic: true)!).ToArray();
    long creditedTotal = 0;
    long bytes = 0;
    long records = 0;
    var maximumLine = 0;
    var token = stress ? new string('<', 64) : "ore";
    var game = stress ? new string('"', 64) : "raw-sizing";
    for (var tick = 0; tick <= 45000; tick++)
    {
        for (var player = 0; player < 2; player++)
        {
            var playerName = "hard-" + player;
            for (var n = 0; n < perTick; n++)
            {
                if (!(bool)add.Invoke(aggregates[player], new object[] { tick, (uint)(17 + player), token, 100, 100 })!)
                    throw new InvalidOperationException("Aggregate rejected a delivery");
            }

            // Drain after all callbacks for the synthetic tick, as terminal/advance does.
            var takeArgs = new object?[] { tick + 1, false, 0, null };
            if (!(bool)take.Invoke(aggregates[player], takeArgs)!)
                throw new InvalidOperationException("Aggregate omitted a tick");
            Count("resource-accepted-tick", takeArgs[3]!);

            if (tick % 50 == 0)
                Count("queue-observation", new
                {
                    item = stress ? new string('<', 64) : "ra1_soviets_orerefinery",
                    queue = "Building", producer = (uint)(17 + player), episode = (uint)(tick + 1),
                    kind = "Ready", reason = "None", player_active = true, producer_live = true,
                    cancellation_class = "None", category = "Building"
                });

            void Count(string kind, object evidence)
            {
                var json = (string)raw.Invoke(null,
                    new object[] { sequence[player]++, tick, game, playerName, kind, evidence })!;
                using var parsed = JsonDocument.Parse(json);
                if (parsed.RootElement.GetProperty("tick").GetInt32() != tick)
                    throw new InvalidOperationException("Serializer tick mismatch");
                if (kind == "resource-accepted-tick")
                {
                    var rollup = parsed.RootElement.GetProperty("evidence");
                    if (rollup.GetProperty("event_count").GetInt64() != perTick)
                        throw new InvalidOperationException("Aggregate event count mismatch");
                    creditedTotal = checked(creditedTotal + rollup.GetProperty("credited_value").GetInt64());
                }
                var lineBytes = checked(Encoding.UTF8.GetByteCount(json) + 1);
                bytes = checked(bytes + lineBytes);
                maximumLine = Math.Max(maximumLine, lineBytes);
                records++;
            }
        }
    }

    if (creditedTotal != 2L * 45001 * perTick * 100 || records != 91804)
        throw new InvalidOperationException("Aggregate bound/total mismatch");
    var fits = bytes <= 128L * 1024 * 1024 && records < 200000 && maximumLine <= 65536;
    if (!fits)
        throw new InvalidOperationException("Declared aggregate envelope exceeds writer bounds");
    results.Add(new
    {
        label, players = 2, ticks = 45001, accepted_events_per_player_tick = perTick,
        queue_events_per_player = 901, escaped_identifier_chars = stress ? 64 : 0,
        raw_bytes = bytes, raw_records = records, total_credited_value = creditedTotal,
        raw_max_line_bytes_including_newline = maximumLine,
        declared_envelope_fits_writer_bounds = fits,
        overflow_capture_contract = "UNKNOWN; never silently drop or claim complete",
        health_bytes = (long?)null, summary_bytes = (long?)null, combined_campaign_bytes = (long?)null
    });
}

if (!assemblyDigest.AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(dll))))
    throw new InvalidOperationException("Assembly changed during measurement");
var receipt = new
{
    schema = 1,
    scope = "actual frozen compiled EconomyAcceptedTick accumulator and Raw serializer; declared synthetic workloads only",
    assembly_path = dll, assembly_sha256 = Convert.ToHexString(assemblyDigest).ToLowerInvariant(),
    runtime_cost_measured = false, disk_cost_measured = false, actual_event_coverage_measured = false,
    adoption_approved = false, results
};
var jsonReceipt = JsonSerializer.Serialize(receipt, new JsonSerializerOptions { WriteIndented = true });
using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None))
using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
    writer.Write(jsonReceipt);
Console.WriteLine(jsonReceipt);
return 0;
