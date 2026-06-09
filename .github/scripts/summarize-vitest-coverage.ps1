param(
  [string]$CoverageSummaryPath = "TabulaRasa/TabulaRasa.Web/coverage/coverage-summary.json",
  [string]$SummaryPath = "frontend-coverage-summary.md"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $CoverageSummaryPath)) {
  throw "Vitest coverage summary was not found at '$CoverageSummaryPath'."
}

$coverage = Get-Content -LiteralPath $CoverageSummaryPath -Raw | ConvertFrom-Json
$total = $coverage.total

$summary = @"
## Frontend Code Coverage

| Metric | Covered | Total | Coverage |
| --- | ---: | ---: | ---: |
| Lines | $($total.lines.covered) | $($total.lines.total) | $($total.lines.pct)% |
| Statements | $($total.statements.covered) | $($total.statements.total) | $($total.statements.pct)% |
| Functions | $($total.functions.covered) | $($total.functions.total) | $($total.functions.pct)% |
| Branches | $($total.branches.covered) | $($total.branches.total) | $($total.branches.pct)% |
"@

$summaryDirectory = Split-Path -Parent $SummaryPath
if ($summaryDirectory) {
  New-Item -ItemType Directory -Force -Path $summaryDirectory | Out-Null
}

$summary | Set-Content -Path $SummaryPath -Encoding UTF8

if ($env:GITHUB_STEP_SUMMARY) {
  $summary | Add-Content -Path $env:GITHUB_STEP_SUMMARY -Encoding UTF8
}

Write-Host "Frontend line coverage: $($total.lines.pct)% ($($total.lines.covered)/$($total.lines.total))"
Write-Host "Frontend branch coverage: $($total.branches.pct)% ($($total.branches.covered)/$($total.branches.total))"
