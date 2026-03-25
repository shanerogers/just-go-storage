# just-go-storage

A .NET Aspire application that syncs and stores data from the JustGo API. It orchestrates a PostgreSQL database and the JustGo API service using the Aspire AppHost.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (or Podman) — required for the PostgreSQL container
- .NET Aspire workload and CLI

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/shanerogers/just-go-storage.git
cd just-go-storage
```

### 2. Install the Aspire CLI

Install the .NET Aspire workload and standalone CLI tool:

```bash
dotnet workload install aspire
dotnet tool install --global aspire
```

### 3. Set the JustGo API key secret

The AppHost requires a `justgo-apikey` user secret. Set it against the AppHost project:

```bash
dotnet user-secrets set "justgo-apikey" "<your-justgo-api-key>" --project JustGo/JustGo.AppHost.csproj
```

### 4. Run the inner dev loop (F5)

Start all resources using the Aspire CLI from the repo root:

```bash
aspire run --project JustGo/JustGo.AppHost.csproj
```

Or use `dotnet run` directly:

```bash
dotnet run --project JustGo/JustGo.AppHost.csproj
```

This starts:
- **PostgreSQL** — persistent container with a DbGate UI
- **JustGo.Api** — the API service with health and job dashboards

The Aspire dashboard will open automatically and show links to all running resources, including the **Job Dashboard** (`/quartz`) and **Health Dashboard** (`/health-ui`).

Alternatively, open the solution in Visual Studio or VS Code and press **F5** to launch the `JustGo.AppHost` project.
