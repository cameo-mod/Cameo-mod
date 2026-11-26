param(
	[Parameter(Mandatory)][string]$EconomyCsv,
	[Parameter(Mandatory)][string]$RefTierCsv,
	[Parameter(Mandatory)][string]$RadarPlacementReport
)

$ErrorActionPreference = 'Stop'
$economy = Import-Csv -LiteralPath $EconomyCsv
$tier = Import-Csv -LiteralPath $RefTierCsv
$radar = @{}
foreach ($line in Get-Content -LiteralPath $RadarPlacementReport) {
	if ($line -match '^\|\s*(seat_\d+)\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|\s*(\d+|-)\s*\|') {
		$seat = $Matches[1]
		if ($radar.ContainsKey($seat)) { throw "Duplicate radar row for $seat" }
		$radar[$seat] = @{ Faction = $Matches[2].Trim(); Personality = $Matches[3].Trim(); Tick = $Matches[4] }
	}
}

foreach ($seat in 'seat_9', 'seat_12') {
	$ruleRow = @($tier | Where-Object seat -eq $seat)
	$buckets = @($economy | Where-Object seat -eq $seat)
	if ($ruleRow.Count -ne 1 -or $buckets.Count -eq 0 -or !$radar.ContainsKey($seat)) {
		throw "Missing or ambiguous input for $seat"
	}

	$identity = $radar[$seat]
	if ($identity.Faction -ne $ruleRow[0].faction -or $identity.Personality -ne $ruleRow[0].personality) {
		throw "Seat identity differs between tier and radar sources for $seat"
	}

	$seenBuckets = @{}
	foreach ($bucket in $buckets) {
		if ($bucket.faction -ne $identity.Faction -or $seenBuckets.ContainsKey($bucket.bucket)) {
			throw "Wrong faction or duplicate economy bucket for $seat at $($bucket.bucket)"
		}

		$seenBuckets[$bucket.bucket] = $true
		[pscustomobject]@{
			seat = $seat
			faction = $identity.Faction
			personality = $identity.Personality
			bucket = $bucket.bucket
			refineries = $bucket.ref
			harvesters = $bucket.harv
			cash_lt300_sample_share = $bucket.cash_lt300
			idle_share = $bucket.idle_share
			internal_radar_standing_tick = $ruleRow[0].radar_standing
			radar_placed = $identity.Tick -ne '-'
			first_radar_placement_tick = if ($identity.Tick -ne '-') { [int]$identity.Tick } else { $null }
			ref_tier_live_end = $ruleRow[0].live_end
		}
	}
}
