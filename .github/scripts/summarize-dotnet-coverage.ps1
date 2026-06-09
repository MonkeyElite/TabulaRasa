param(
  [string]$CoverageRoot = ".",
  [string]$BadgePath = ".github/badges/coverage.svg",
  [string]$SummaryPath = "coverage-summary.md",
  [double]$MinimumLineCoverage = 60
)

$ErrorActionPreference = "Stop"

function Get-CoverageColor {
  param([double]$Percentage)

  if ($Percentage -ge 90) { return "#4c1" }
  if ($Percentage -ge 75) { return "#97ca00" }
  if ($Percentage -ge 60) { return "#dfb317" }
  return "#e05d44"
}

function Get-BadgeSvg {
  param(
    [string]$Label,
    [string]$Message,
    [string]$Color
  )

  $labelWidth = 76
  $messageWidth = [Math]::Max(52, ($Message.Length * 7) + 12)
  $totalWidth = $labelWidth + $messageWidth
  $messageX = $labelWidth + ($messageWidth / 2)

  return @"
<svg xmlns="http://www.w3.org/2000/svg" width="$totalWidth" height="20" role="img" aria-label="${Label}: $Message">
  <title>${Label}: $Message</title>
  <linearGradient id="s" x2="0" y2="100%">
    <stop offset="0" stop-color="#bbb" stop-opacity=".1"/>
    <stop offset="1" stop-opacity=".1"/>
  </linearGradient>
  <clipPath id="r">
    <rect width="$totalWidth" height="20" rx="3" fill="#fff"/>
  </clipPath>
  <g clip-path="url(#r)">
    <rect width="$labelWidth" height="20" fill="#555"/>
    <rect x="$labelWidth" width="$messageWidth" height="20" fill="$Color"/>
    <rect width="$totalWidth" height="20" fill="url(#s)"/>
  </g>
  <g fill="#fff" text-anchor="middle" font-family="Verdana,Geneva,DejaVu Sans,sans-serif" text-rendering="geometricPrecision" font-size="110">
    <text aria-hidden="true" x="380" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)" textLength="660">coverage</text>
    <text x="380" y="140" transform="scale(.1)" textLength="660">coverage</text>
    <text aria-hidden="true" x="$($messageX * 10)" y="150" fill="#010101" fill-opacity=".3" transform="scale(.1)">$Message</text>
    <text x="$($messageX * 10)" y="140" transform="scale(.1)">$Message</text>
  </g>
</svg>
"@
}

$coverageFiles = Get-ChildItem -Path $CoverageRoot -Filter "coverage.cobertura.xml" -Recurse -File

if ($coverageFiles.Count -eq 0) {
  throw "No coverage.cobertura.xml files were found under '$CoverageRoot'."
}

$totalLinesCovered = 0
$totalLinesValid = 0
$totalBranchesCovered = 0
$totalBranchesValid = 0

foreach ($coverageFile in $coverageFiles) {
  [xml]$coverage = Get-Content -Path $coverageFile.FullName
  $root = $coverage.coverage

  $totalLinesCovered += [int]$root."lines-covered"
  $totalLinesValid += [int]$root."lines-valid"
  $totalBranchesCovered += [int]$root."branches-covered"
  $totalBranchesValid += [int]$root."branches-valid"
}

if ($totalLinesValid -eq 0) {
  throw "Coverage files were found, but no coverable lines were reported."
}

$lineCoverage = [Math]::Round(($totalLinesCovered / $totalLinesValid) * 100, 2)
$branchCoverage = if ($totalBranchesValid -gt 0) {
  [Math]::Round(($totalBranchesCovered / $totalBranchesValid) * 100, 2)
} else {
  0
}

$coverageMessage = "$lineCoverage%"
$coverageColor = Get-CoverageColor -Percentage $lineCoverage

$summary = @"
## .NET Code Coverage

| Metric | Covered | Total | Coverage |
| --- | ---: | ---: | ---: |
| Lines | $totalLinesCovered | $totalLinesValid | $lineCoverage% |
| Branches | $totalBranchesCovered | $totalBranchesValid | $branchCoverage% |

Minimum required line coverage: $MinimumLineCoverage%.
"@

$summaryDirectory = Split-Path -Parent $SummaryPath
if ($summaryDirectory) {
  New-Item -ItemType Directory -Force -Path $summaryDirectory | Out-Null
}
$summary | Set-Content -Path $SummaryPath -Encoding UTF8

if ($env:GITHUB_STEP_SUMMARY) {
  $summary | Add-Content -Path $env:GITHUB_STEP_SUMMARY -Encoding UTF8
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $BadgePath) | Out-Null
Get-BadgeSvg -Label "coverage" -Message $coverageMessage -Color $coverageColor |
  Set-Content -Path $BadgePath -Encoding UTF8

Write-Host "Line coverage: $lineCoverage% ($totalLinesCovered/$totalLinesValid)"
Write-Host "Branch coverage: $branchCoverage% ($totalBranchesCovered/$totalBranchesValid)"

if ($lineCoverage -lt $MinimumLineCoverage) {
  throw "Line coverage $lineCoverage% is below the required $MinimumLineCoverage%."
}
