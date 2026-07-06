# My Work Tracker

A personal work-tracking dashboard built with **.NET Aspire** that monitors PRs, work items, epics, agents, skills, and plugins across your repositories.

## Features

- **Repo Onboarding** — Add GitHub or Azure DevOps repos to track
- **PR Monitor** — See all PRs assigned to you, grouped by status
- **Work Item Board** — Kanban-style board (Backlog → In Progress → In Review → Done)
- **Epic Tracker** — Onboard epics and track progress toward deliverables
- **"What's Next?" Engine** — AI-powered prioritization based on deadlines, dependencies, and staleness
- **Agent/Skill/Plugin Registry** — Track your Copilot agents, skills, and plugins

## Architecture

```
MyWorkTracker.AppHost          — Aspire orchestrator
├── MyWorkTracker.Web          — Blazor frontend (dashboard + board)
├── MyWorkTracker.ApiService   — ASP.NET Core Web API
├── MyWorkTracker.Worker       — Background sync service
├── PostgreSQL                 — Persistent storage
└── Redis                      — Caching layer
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Aspire containers)
- A GitHub personal access token (for API access)

### Run Locally

```bash
cd src/MyWorkTracker.AppHost
dotnet run
```

This launches the Aspire dashboard at `https://localhost:15888` with all services orchestrated.

### Configuration

Set your GitHub token via user secrets:

```bash
cd src/MyWorkTracker.Worker
dotnet user-secrets set "GitHub:Token" "your-pat-here"
```

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/repos` | List tracked repos |
| POST | `/api/repos` | Onboard a new repo |
| GET | `/api/prs` | List open PRs assigned to you |
| GET | `/api/workitems/board` | Get Kanban board view |
| GET | `/api/epics/{id}/next` | Get next priority item for an epic |
| GET | `/api/epics/{id}/prioritized` | Get all items ranked by priority |
| GET | `/api/agents` | List tracked agents/skills/plugins |
| POST | `/api/agents` | Register a new agent/skill/plugin |

## Prioritization Algorithm

Items are scored by:
1. **Priority level** (Critical > High > Medium > Low)
2. **Deadline proximity** (overdue items score highest)
3. **In-progress boost** (finish what you started)
4. **Dependency awareness** (unblocked items preferred)
5. **Staleness** (neglected items get attention)

## License

MIT
