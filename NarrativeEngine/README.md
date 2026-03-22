# Narrative Engine AI

An AI-powered creative writing assistant platform. Writers receive real-time AI suggestions when they hit a block, visualize the emotional arc of each chapter, automatically detect character consistency contradictions, and stream background music matched to the scene's mood.

Designed as a C# backend portfolio project that naturally demonstrates real-world engineering skills: streaming, parallel processing, RAG, caching, resilience, and observability.

---

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10 |
| Backend | ASP.NET Core, SignalR, EF Core 9 |
| Database | PostgreSQL + pgvector (Supabase) |
| AI | Claude API (Anthropic), Mubert API |
| Resilience | Polly (Retry, Circuit Breaker) |
| Observability | Serilog (JSON structured logging) |
| API Docs | Swashbuckle (OpenAPI / Swagger UI) |
| Testing | xUnit, FluentAssertions, FsCheck, Bogus, AutoFixture, Testcontainers |
| Frontend | React (Vite) + TypeScript *(planned)* |
| CI/CD | GitHub Actions + Docker + Railway *(planned)* |

---

## Architecture

```
NarrativeEngine/
├── src/
│   ├── NarrativeEngine.Domain/          # Entities, interfaces, enums, constants
│   ├── NarrativeEngine.Infrastructure/  # EF Core, Polly, Serilog
│   ├── NarrativeEngine.Api/             # Controllers, services, middleware
│   └── NarrativeEngine.Common/          # IClock, SystemClock (shared utilities)
└── tests/
    ├── NarrativeEngine.Tests.Unit/       # Unit tests (including property-based)
    └── NarrativeEngine.Tests.Integration/# Integration tests (Testcontainers)
```

Clean Architecture with three layers. `Domain` has no external dependencies. `Infrastructure` encapsulates EF Core, Polly, and other implementation details. `Api` is the HTTP entry point and service orchestrator.

---

## Implementation Status

### Completed (Tasks 1–6)

#### Infrastructure & Common Setup

- **Solution structure**: Domain / Infrastructure / Api / Common — four-project separation
- **AppDbContext**: PostgreSQL + pgvector, EF Core Fluent API, global query filter for soft delete (`e => !e.IsDeleted`)
- **Soft delete interception**: `SaveChangesAsync` override converts `EntityState.Deleted` into the `ISoftDeletable` pattern, automatically setting `IsDeleted`, `DeletedAt`, and `DeletedBy`
- **ICurrentUserAccessor**: Reads the JWT `sub` claim from the HTTP context to provide the current user ID; used to populate `DeletedBy` for audit tracking
- **Serilog**: JSON structured logging, configuration-driven via `appsettings.json`
- **Swagger / OpenAPI**: JWT Bearer authentication scheme included
- **Global exception middleware**: Standard error response shape — `error.code` / `error.message` / `error.traceId`
- **Health check**: `GET /health`
- **Rate limiting**:
  - `user-id` policy: JWT `sub` claim-based, 60 req/min
  - `auth-ip` policy: IP-based, 10 req/min (applied to `/api/auth/login` and `/api/auth/register`)
- **Caching**: `AddDistributedMemoryCache()` + `AddMemoryCache()` with defined cache key constants (`emotion:chapter:{id}`, `music:bgm:{tag}`, `context:summary:{id}`, `lock:emotion:{id}`)
- **ServiceType constants**: `muse`, `consistency`, `embedding`, `emotion`

#### Resilience

- **Polly Retry**: Exponential backoff × 3 (1s → 2s → 4s), `Retry-After` header respected for 429 responses
- **Circuit Breaker**: Reacts to network exceptions, 429, and 5xx; opens for 30 seconds after 5 consecutive failures
- **LoggingBehavior**: Logs a Warning when an external API call exceeds 5 seconds

#### Auth Domain (Tasks 3–6)

**Entities**

| Entity | Key Fields |
|--------|------------|
| `User` | Id, Email, PasswordHash, Role (UserRole), CreatedAt/By, ModifiedAt/By, IsDeleted, DeletedAt/By |
| `RefreshToken` | Id, UserId, TokenHash, ExpiresAt, IsRevoked, RevokedAt, ReplacedByTokenHash, RevokeReason |
| `UserSettings` | Id, UserId, MuseEnabled, MuseTrigger, EmotionAnalysisAuto, BgmEnabled |

**Database configuration**

- `UserRole` enum: `User` (default) / `Admin`
- `ux_users_email_active`: Partial unique index — `UNIQUE (email) WHERE is_deleted = false`
- EF Core migration applied

**Services**

- **JwtTokenGenerator**: ES256 (ECDSA P-256) asymmetric key signing via `ECDsa.ImportFromPem`
- **AuthService**: PBKDF2 password hashing (`IPasswordHasher<User>`), access token issuance, automatic `UserSettings` initialization on account creation
- **Refresh Token Rotation**: DB transaction + `SELECT FOR UPDATE` concurrency guard; reuse detection triggers full token family revocation; records `RevokedAt`, `ReplacedByTokenHash`, `RevokeReason`
- **UserLifecycleService**:
  - `SoftDeleteUserAsync` — single transaction, cascading soft delete via the `SaveChangesAsync` interceptor
  - `PurgeDeletedDataAsync` — `ExecuteDeleteAsync` bypasses the ChangeTracker for true hard delete

**API Endpoints**

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/auth/register` | User registration |
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh` | Renew access token |
| POST | `/api/auth/logout` | Logout (token revocation) |

- Refresh token delivered as an HttpOnly cookie (SameSite=Strict, Secure)
- JWT Bearer middleware: ES256 public key validation, ClockSkew=Zero, SignalR `access_token` query string support, automatic `X-User-Id` header injection

**Tests**

- Unit tests: `AuthService` requirements 1.1–1.15 (xUnit + NSubstitute)
- Property-based tests: valid registration, short password rejection, no plaintext password storage, Refresh Token Rotation (FsCheck.Xunit + Bogus + AutoFixture)
- Integration tests: concurrent refresh token request de-duplication (Testcontainers + PostgreSQL)
- Shared fixtures: `TestClock`, `SharedDatabaseFixture`

---

### Planned (Tasks 7–39)

| Domain | Tasks | Scope |
|--------|-------|-------|
| Project | 7–10 | Project / Chapter / Character entities, CRUD services, APIs, pagination, ownership validation |
| Settings | 11–13 | UserSettings service & API, per-user isolation |
| Muse | 14–16 | Claude API SSE → SignalR live streaming, ContextCompressor, TokenUsageLog |
| Emotion | 17–20 | SemaphoreSlim(5) parallel emotion analysis, IMemoryCache locking, cache invalidation |
| RAG | 21–24 | pgvector cosine similarity search, CharacterEmbedding, ConsistencyReport, HNSW index |
| Music | 25–28 | Mubert API BGM matching, EmotionTag → Mood mapping, fallback MP3 assets |
| CI/CD | 29 | Docker multi-stage build, GitHub Actions pipeline, Railway deployment |
| Frontend | 30–39 | React + TypeScript, SignalR hook, MuseContext, Chart.js emotion waveform, BGM player |

---

## Local Development

### Prerequisites

- .NET 10 SDK
- Docker (PostgreSQL + pgvector)

### Start the database

```bash
docker compose up -d
```

### Configure secrets

```bash
cd src/NarrativeEngine.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=narrative_engine;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:PrivateKeyPem" "<ES256 PEM private key>"
dotnet user-secrets set "Jwt:PublicKeyPem" "<ES256 PEM public key>"
```

### Apply migrations and run

```bash
dotnet ef database update --project src/NarrativeEngine.Infrastructure --startup-project src/NarrativeEngine.Api
dotnet run --project src/NarrativeEngine.Api
```

Swagger UI: `https://localhost:{port}/swagger`
Health check: `https://localhost:{port}/health`

### Run tests

```bash
# Unit tests
dotnet test tests/NarrativeEngine.Tests.Unit

# Integration tests (Docker required — Testcontainers spins up PostgreSQL automatically)
dotnet test tests/NarrativeEngine.Tests.Integration
```

---

## MVP Engineering Goals

| Capability | Implementation | Status |
|------------|----------------|--------|
| Streaming | Claude API SSE → SignalR real-time relay | Planned |
| Parallel processing | SemaphoreSlim-based emotion analysis | Planned |
| RAG | pgvector cosine similarity + project_id metadata filtering | Planned |
| Caching | IDistributedCache + SemaphoreSlim distributed lock | Foundation complete |
| Resilience | Polly Exponential Backoff Retry + Circuit Breaker | Complete |
| Observability | Serilog JSON structured logging | Complete |
| Authentication | ES256 JWT + Refresh Token Rotation + Soft Delete | Complete |
