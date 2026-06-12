# TabulaRasa

[![CI](https://github.com/MonkeyElite/TabulaRasa/actions/workflows/ci.yml/badge.svg)](https://github.com/MonkeyElite/TabulaRasa/actions/workflows/ci.yml)
![Coverage](.github/badges/coverage.svg)

TabulaRasa is a modular life-simulation project built around a .NET simulation engine and a Next.js web interface. Agents move through a grid-based world, react to needs, plan routes, execute actions, and expose state through an API and editable web UI.

The CI/CD pipeline and maintainability report is available as a Word document: [docs/CI-CD-Pipeline-Report.docx](docs/CI-CD-Pipeline-Report.docx).

## Requirements

- [.NET SDK 10.0.x](https://dotnet.microsoft.com/download)
- [Node.js 20 LTS or newer](https://nodejs.org/)
- npm, included with Node.js
- Git

Optional but useful:

- Visual Studio, Visual Studio Code, or JetBrains Rider for .NET development
- A modern browser for the web interface

## Install

Clone the repository and restore the .NET solution:

```powershell
git clone https://github.com/MonkeyElite/TabulaRasa.git
cd TabulaRasa
dotnet restore .\TabulaRasa\TabulaRasa.sln
```

Install the web dependencies:

```powershell
cd .\TabulaRasa\TabulaRasa.Web
npm ci
```

## Run the Application

Start the ASP.NET API in one terminal:

```powershell
dotnet run --project .\TabulaRasa\TabulaRasa.Api\TabulaRasa.Api.csproj
```

The API listens on `http://localhost:5088` when using the repository launch profile.

Start the Next.js web app in a second terminal:

```powershell
cd .\TabulaRasa\TabulaRasa.Web
npm run dev
```

Open `http://localhost:3000`. The web app proxies `/api/simulations` requests to `http://localhost:5088/api` by default.

To point the web app at a different API URL, set `TABULARASA_API_URL` before starting Next.js:

```powershell
$env:TABULARASA_API_URL = "http://localhost:5088/api"
npm run dev
```

## Run Tests

Run the .NET unit test suite:

```powershell
dotnet test .\TabulaRasa\TabulaRasa.sln
```

Run the frontend Vitest suite:

```powershell
cd .\TabulaRasa\TabulaRasa.Web
npm test
```

`npm test` runs Vitest with V8 coverage enabled. To run the frontend tests without coverage, use:

```powershell
npm run test:unit
```

Build the frontend production bundle:

```powershell
npm run build
```

Run the same .NET coverage collection used by CI:

```powershell
dotnet test .\TabulaRasa\TabulaRasa.sln --configuration Release --collect:"XPlat Code Coverage" --logger "trx" --results-directory .\TestResults
powershell -ExecutionPolicy Bypass -File .\.github\scripts\summarize-dotnet-coverage.ps1 -CoverageRoot .\TestResults -MinimumLineCoverage 60 -BadgePath .\.github\badges\coverage.svg -SummaryPath .\coverage-summary.md
```

## Current Coverage

The latest local coverage measurements are:

| Area | Metric | Covered | Total | Coverage |
| --- | --- | ---: | ---: | ---: |
| .NET | Lines | 9198 | 11508 | 79.93% |
| .NET | Branches | 2136 | 3260 | 65.52% |
| Web | Lines | 943 | 2987 | 31.57% |
| Web | Branches | 177 | 320 | 55.31% |

CI enforces a minimum .NET line coverage threshold of `60%`. The README coverage badge currently reflects the .NET line coverage gate; frontend coverage is generated in CI as a separate Vitest artifact and workflow summary section.

## Project Layout

| Path | Purpose |
| --- | --- |
| `TabulaRasa/TabulaRasa.Abstractions` | Shared contracts for agents, entities, spatial data, time, and execution context |
| `TabulaRasa/TabulaRasa.World` | World state, entities, resources, grid, navigation, and mutation services |
| `TabulaRasa/TabulaRasa.Agents` | Agent models, needs, and default decision-making behavior |
| `TabulaRasa/TabulaRasa.Simulation` | Simulation engine, systems, actions, movement, tasks, jobs, memory, and goals |
| `TabulaRasa/TabulaRasa.Api` | ASP.NET API for simulation sessions and snapshots |
| `TabulaRasa/TabulaRasa.Web` | Next.js UI for inspecting and controlling simulations |
| `TabulaRasa/TabulaRasa.UnitTests` | xUnit test suite for backend simulation and API behavior |
| `.github/workflows/ci.yml` | GitHub Actions workflow for restore, build, tests, coverage, and artifacts |

## CI

GitHub Actions runs on pushes and pull requests targeting `main` and `develop`. The current workflow restores the .NET solution, builds in `Release`, runs xUnit tests with Coverlet coverage, installs frontend dependencies with `npm ci`, runs Vitest with V8 coverage through `npm test`, builds the Next.js app with `npm run build`, uploads test and coverage artifacts, and refreshes the README coverage badge on trusted branch pushes.
