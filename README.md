# Book Ecommerce App

A full-stack marketplace for books, built as a hands-on **backend learning project for interview preparation**. The eventual product: users can **sell** books (new or used), **rent** books for a defined period, and check out via a mock payment gateway.

> **Active track (2026-05-03):** **Go** backend in `server_go/` — preparing for a new Go role. Built incrementally, one concept at a time.
>
> **Paused track:** the original **ASP.NET Core** backend in `server_asp/` (was a .NET interview prep project; reached a complete admin RBAC + clean-architecture milestone before the pivot). The React `client/` still talks to `server_asp` until the Go backend reaches parity.

---

## Status (2026-04-25 — .NET track, paused)

### Working today

- **Auth** — register / login / `/users/me` over JWT bearer
- **Admin RBAC** — full CRUD for users, roles, permissions
  - Per-user role assignment + per-user direct-permission grants
  - SuperAdmin guard rails (role can't be deleted/renamed; last SuperAdmin can't be removed; users can't delete themselves)
  - Optimistic concurrency via `ConcurrencyStamp` — 409 on conflict, atomic `ExecuteUpdate` for the conflict-prone flows
- **Books** — basic CRUD (no listing/marketplace flow yet)
- **React admin panel** — login/register, role-based landing, admin Users and Roles screens with permission matrices, shared 409 handling, key-remount pattern for edit forms
- **Clean architecture** — four-project split, dependency arrows pointing inward (Phase 7 + Phase 5 complete — Application has zero `using BookEcom.Infrastructure` imports)

### Not yet built

- **Policy-based authorization** — admin endpoints currently use bare `[Authorize]`, so any authenticated user can hit them. Pre-ship blocker.
- **ProblemDetails (RFC 7807)** — error responses are still ad-hoc `{ error: "..." }`. Client `ApiProblem` parser ready and waiting.
- **Refresh tokens** — single-token model only; no `/auth/refresh`, no password reset, no email confirmation
- **Marketplace flows** — book listings, browse/search, orders, rental lifecycle, late-fee handling
- **Mock payment gateway** — strategy interface planned, real impl pending
- **Tests** — xUnit + WebApplicationFactory + Testcontainers pencilled in
- **Dockerized API/client** — currently only Postgres runs in compose
- **CI pipeline**

---

## Tech Stack

| Layer                | Choice                                                                  |
| -------------------- | ----------------------------------------------------------------------- |
| Backend (active)     | **Go** — framework / router / DB layer chosen incrementally as we learn |
| Backend (paused)     | .NET 10 (ASP.NET Core Web API) + ASP.NET Core Identity + JWT bearer     |
| Database (Go)        | PostgreSQL — fresh schema, separate DB from the .NET one                |
| Database (.NET)      | PostgreSQL via Npgsql + Entity Framework Core                           |
| Frontend             | React 19 + Vite + TypeScript (strict, `erasableSyntaxOnly`)             |
| Client state         | Zustand (with `persist` middleware, localStorage-backed)                |
| Server state         | TanStack Query v5 over an axios client                                  |
| Routing              | React Router v7 (`createBrowserRouter`)                                 |
| Forms                | react-hook-form + zod (`@hookform/resolvers`)                           |
| UI                   | shadcn/ui on Tailwind v4 (Nova preset, Radix primitives, Lucide)        |
| Toasts               | sonner                                                                  |
| API docs (.NET)      | OpenAPI + Scalar                                                        |
| Orchestration        | Docker + docker-compose (Postgres only — API/client containers TBD)     |

---

## Architecture

### Layer boundaries

After Phase 5, the four-project graph has all dependency arrows pointing inward:

```
Api  →  Application  ←  Infrastructure
              │                │
              └→   Domain   ←──┘
```

- **`BookEcom.Domain`** — entities (`Book`, `Permission`, `RolePermission`, `UserPermission`), business rules, persistence abstractions (`I*Repository`, `IUnitOfWork`, `IAppTransaction`), value-object-style outcomes (`Result<T>`, `Error`), and Identity-free projection types (`RoleSummary`, `UserSnapshot`). Zero framework package references.
- **`BookEcom.Application`** — use-case services (`BookService`, `UserManagementService`, `RoleManagementService`, `PermissionService`, `AuthService`), DTOs (`*Request` / `*Response`), `AppUser : IdentityUser<int>`, `IJwtTokenService` interface, `JwtOptions`. Depends only on Domain.
- **`BookEcom.Infrastructure`** — `AppDbContext`, repository implementations, migrations, `JwtTokenService` implementation, `IdentitySeeder`. Depends on Application + Domain.
- **`BookEcom.Api`** — controllers, `Program.cs` DI wiring, `ResultExtensions` translating `Result<T>` → `ActionResult`. Composition root.

Application's `.csproj` has only one project reference: `BookEcom.Domain`. Infrastructure references both Application (for `AppUser`, `IJwtTokenService`, `JwtOptions`) and Domain. The arrow flip satisfies SOLID's **D** at the project-graph level — high-level (Application) and low-level (Infrastructure) both depend on abstractions owned upstream.

### SOLID

| Principle                 | How it shows up                                                                                                                          |
| ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| **Single Responsibility** | Controllers handle HTTP only; services orchestrate use cases; repos own persistence; entities own invariants.                            |
| **Open / Closed**         | New persistence backend? Write a new `IBookRepository` impl, swap one DI registration. Service code untouched.                           |
| **Liskov Substitution**   | Repos return entities or pure-domain projections — never `IQueryable` or EF types — so any conforming implementation substitutes cleanly. |
| **Interface Segregation** | One repo per aggregate root with use-case-named methods (`FindForUpdateAsync`, `ClearPermissionsAsync`), not a generic `IRepository<T>`. |
| **Dependency Inversion**  | Application defines what it needs; Infrastructure implements. Project references enforce this at compile time.                           |

### Design patterns

- **Repository + Unit of Work** — one `I*Repository` per aggregate root + `IUnitOfWork` for the transaction boundary. Interfaces in Domain, EF implementations in Infrastructure.
- **Dependency Injection** — ASP.NET Core's built-in container; services registered `Scoped` to share the request-scoped `DbContext`.
- **DTO at the boundary** — separate request/response shapes from entities. HTTP-side validation via data annotations (`[Required]`, `[StringLength]`); domain invariants enforced separately in entity factories (`Book.Create`).
- **Result pattern** — services return `Result<T>` instead of throwing for expected failures. A single `ResultExtensions.ToActionResult()` at the controller boundary maps to HTTP status codes (200/201/204/400/404/409 etc.).
- **Optimistic concurrency** — `ConcurrencyStamp` field on user/role aggregates; mutations use atomic `ExecuteUpdateAsync` with an expected-stamp WHERE clause, surfacing conflicts as 409.
- **Domain-vs-persistence entity** — `User` (domain concept) is represented by `AppUser : IdentityUser<int>` (persistence/Identity). `Role` (domain concept) is `IdentityRole<int>` plus our `RolePermission` join. Read paths return pure-domain projections (`UserSnapshot`, `RoleSummary`).
- **Strategy** *(planned)* — real vs. mock payment gateway behind a common interface.

---

## Project Structure

```
book-ecommerce-app/
├── client/                                 # React + Vite + TypeScript (feature-sliced)
│   └── src/
│       ├── app/                            # queryClient, route guards
│       ├── components/{ui,layout}/
│       ├── features/{auth,admin-users,admin-roles,permissions,buyer,seller,shared}/
│       ├── lib/                            # apiClient, queryKeys, handleMutationError, types
│       ├── stores/                         # Zustand (authStore)
│       └── router.tsx
├── server_asp/                             # .NET 10 backend — paused, kept for reference
│   ├── BookEcom.sln
│   ├── BookEcom.Api/                       # composition root, controllers, Program.cs
│   ├── BookEcom.Application/               # services, DTOs, AppUser, IJwtTokenService
│   ├── BookEcom.Domain/                    # entities, repo interfaces, Result<T>
│   └── BookEcom.Infrastructure/            # AppDbContext, repos, migrations, JwtTokenService
├── server_go/                              # Go backend — active learning track (currently empty)
├── docker-compose.yml                      # Postgres (API/client containers planned)
├── .env.example
├── CLAUDE.md                               # canonical conventions & decisions
└── README.md
```

---

## Getting Started

### Prerequisites

- [Go 1.23+](https://go.dev/dl/) — for the active `server_go/` track (once it has code)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — for the paused `server_asp/` backend
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

PostgreSQL runs inside docker-compose — no separate install needed.

### Run (paused .NET backend — current source of API for the React client)

```bash
# 1. Start Postgres
cp .env.example .env
docker compose up -d

# 2. Apply migrations
dotnet ef database update \
    --project server_asp/BookEcom.Infrastructure \
    --startup-project server_asp/BookEcom.Api

# 3. Run the API (separate terminal)
cd server_asp/BookEcom.Api
dotnet watch run

# 4. Run the client (separate terminal)
cd client
npm install
npm run dev
```

### Run (active Go backend)

Not yet runnable — `server_go/` is empty. Setup commands will be added here as the module takes shape.

| Service             | URL                                   |
| ------------------- | ------------------------------------- |
| API                 | http://localhost:5027                 |
| Scalar API explorer | http://localhost:5027/scalar/v1       |
| OpenAPI JSON        | http://localhost:5027/openapi/v1.json |
| React app           | http://localhost:5173                 |
| PostgreSQL          | localhost:**5433** (host port — container listens on 5432) |

### Seeded SuperAdmin

The first run seeds a default admin you can log in with:

```
admin@bookecom.local / Admin!ChangeMe1
```

The seeder logs a warning every boot until the password is rotated.

---

## API Documentation

- **Scalar interactive UI:** http://localhost:5027/scalar/v1
- **OpenAPI JSON spec:** http://localhost:5027/openapi/v1.json

Both are mounted only in the Development environment.

---

## Database & Migrations

EF Core migrations are the source of truth for the **.NET backend's** schema. The `DbContext` lives in `BookEcom.Infrastructure` (separate from the startup project), so every `dotnet ef` invocation needs both `--project` and `--startup-project`:

```bash
# create a new migration
dotnet ef migrations add <Name> \
    --project server_asp/BookEcom.Infrastructure \
    --startup-project server_asp/BookEcom.Api

# apply pending migrations
dotnet ef database update \
    --project server_asp/BookEcom.Infrastructure \
    --startup-project server_asp/BookEcom.Api

# remove the most recent (un-applied) migration
dotnet ef migrations remove \
    --project server_asp/BookEcom.Infrastructure \
    --startup-project server_asp/BookEcom.Api
```

The Go backend will use its **own fresh schema** in a separate database — no shared tables with the .NET backend. Migration tooling (likely `golang-migrate` or `goose`) will be picked once we have a first table to migrate.

Conventions:

- Money: `decimal` in .NET / `numeric` in Postgres — never floating-point
- Timestamps: `timestamp with time zone`, UTC only
- Table and column names: `snake_case` via Npgsql naming conventions
- `RolePermission` / `UserPermission` link to Identity tables via explicit FK config; no Identity nav properties on Domain entities (keeps Domain framework-free)

---

## Configuration

Connection strings, JWT options, and CORS origins are bound from configuration:

- **Development:** `server_asp/BookEcom.Api/appsettings.Development.json` (intentional dev placeholders — change before any non-throwaway environment)
- **Production / Docker:** environment variables (`ConnectionStrings__Default`, `Jwt__SigningKey`, etc.)

Frontend reads `VITE_API_BASE_URL` from `client/.env`.

---

## Coding Conventions

- **Async all the way** for I/O; `CancellationToken` propagated from controllers through services to repositories
- **Nullable reference types enabled** across all projects
- **DTOs at the API boundary** — EF entities never leak through controllers
- **TypeScript enums forbidden** in client code (`erasableSyntaxOnly` is on) — use `as const` objects + derived union types
- **Server state is React Query, not `useEffect` + `fetch`** — every server read is a `useQuery`, every write a `useMutation`
- **Client state is Zustand, not Context**
- **LF line endings**, enforced via `.gitattributes`

See [`CLAUDE.md`](./CLAUDE.md) for the full project conventions and architectural decisions, including the reasoning behind each compromise (e.g., why `AppUser` lives in Application rather than Domain, why `RolePermission` deliberately omits its `Role` navigation property).

---

## Roadmap

Roughly in priority order — see `CLAUDE.md` "Open questions" for the live list.

1. **Policy-based authorization** — `[HasPermission(...)]` attribute + permission claims in JWT (pre-ship blocker)
2. **RFC 7807 ProblemDetails** — global exception handler → consistent error shape (client parser is already ready)
3. **Refresh tokens + email confirmation + password reset** — pulls in the full auth UX
4. **Marketplace flows** — listings, browse/search, orders, rental lifecycle
5. **Mock payment gateway** — strategy interface, success/failure simulation
6. **Tests** — xUnit + WebApplicationFactory + Testcontainers
7. **Dockerized API + client + CI pipeline**
