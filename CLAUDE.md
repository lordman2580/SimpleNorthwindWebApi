# CLAUDE.md

Guidance for AI assistants working in this repository.

## Project

**Simple Northwind WebApi** — a .NET 8 + SQL Server backend (no frontend). Employee password login (JWT), orders + order_details CRUD, customers CRUD, full API audit logging. Inventory is decremented on order and restored on cancel; oversell is rejected; paid-off orders cannot be cancelled.

Guiding principle: **keep clean and simple** — no MediatR, no event-driven, no over-engineering.

## Architecture (Clean Architecture)

```
src/
  SimpleNorthwind.Domain          # entities, Result<T>, domain errors — ZERO dependencies
  SimpleNorthwind.Application     # DTOs (one responsibility per file), repo/service/UoW interfaces, services, FluentValidation
  SimpleNorthwind.Infrastructure  # Dapper repos, UnitOfWork, JWT, password hashing, AES secret protector, date converter, DI
  SimpleNorthwind.WebApi          # Controllers, Program.cs, filters, middleware, appsettings
  SimpleNorthwind.Migrator        # standalone console: runs embedded .sql migrations + seed; NO project references
tests/
  *.UnitTests / *.Architecture.Tests / *.E2E.Tests
```

Dependency direction: `WebApi → Application → Domain`, `WebApi → Infrastructure → Application/Domain`. `Domain` depends on nothing. `Migrator` is independent.

## Commands

```powershell
# build (TreatWarningsAsErrors — must stay zero-warning)
dotnet build SimpleNorthwind.sln

# run migrator (creates DB SimpleNorthwind + applies all migrations, idempotent)
dotnet run --project src/SimpleNorthwind.Migrator

# run API (Swagger at /swagger)
dotnet run --project src/SimpleNorthwind.WebApi

# tests (must actually pass — never report green without running)
dotnet test SimpleNorthwind.sln
```

SDK is pinned to `net8.0` via `global.json` (machine also has 9.0.300). Common build props live in `Directory.Build.props`.

## Conventions (easy to get wrong)

- **Dates**: store **UTC** (`datetime2`); API **output** is converted to the **caller's local timezone** (`X-Time-Zone` header; falls back to `App:DefaultTimeZone` = `Asia/Taipei`, NOT UTC), **input** parsed as client-local then converted back to UTC. Format `yyyy-MM-dd HH:mm:ss`. DTO datetime fields do **not** carry a `Utc` suffix.
- **Dapper + SQL Server**: a single connection cannot run commands in parallel — never `Task.WhenAll` multiple queries on one UoW; `await` sequentially. All SQL parameterized.
- **Unit of Work / Repository**: repositories use `uow.Connection` + `uow.Transaction`. UoW/Repo/Service are Scoped.
- **Errors**: business failures use `Result`/`Result<T>` → mapped to `ProblemDetails`; only system faults throw.
- **async**: no `.Result`/`.Wait()`; thread `CancellationToken` from controller to Dapper; `ConfigureAwait(false)` in library layers.
- **Secrets**: reversible secrets (connection string, `Jwt:Secret`) use AES-256-GCM with an `enc:` prefix, decrypted at startup via `PostConfigure`. Dev key = gitignored `secret.decryption.key`; prod key = env `APP_SECRET_KEY`. Passwords use `PasswordHasher<Employee>` (PBKDF2). Dev plaintext secrets live in User Secrets — never in the repo.
- **Config**: `appsettings.json` (non-sensitive) + `appsettings.{Development,Production}.json`. No secrets in committed files.
- **DTOs**: one responsibility per file (CRUD request/response of one entity may share a file).

## Plan / design docs

Full SA/SD plan lives in Obsidian (not in this repo): `00-總覽` … `15-Checkpoint-P7`. Checkpoints P0–P7 drive implementation; tick `- [x]` as steps complete.
