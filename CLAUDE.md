# Book Ecommerce App

## Purpose

This project is a **learning exercise** for interview preparation. The author is learning a backend stack end-to-end by building a realistic full-stack application. Explanations, design choices, and code should favor **clarity and interview-relevant concepts** over clever shortcuts.

When suggesting an approach, briefly explain the "why" and, where it's useful, mention the language/framework concept or pattern being applied. Point out the kinds of follow-up questions an interviewer might ask.

## Current focus: Go (paused: ASP.NET Core)

The project originally targeted **ASP.NET Core** for a .NET interview and reached a complete admin RBAC + clean-architecture milestone (preserved under `server_asp/`). It is now **paused**.

The active learning track is **Go**, in `server_go/`, in preparation for a **new Go role**. Same product domain, same eventual API surface — different stack, different database (the Go backend gets its own Postgres schema; the existing EF migrations are not reused).

**Pacing for the Go track is deliberately slow** — one concept at a time, depth over speed, so each topic stays interview-defensible. Do not scaffold large pieces ahead of where the discussion is. Wait for explicit direction before adding the next layer.

The React `client/` continues to talk to the .NET backend until the Go backend reaches feature parity; cutover is a later decision.

## Domain

A marketplace for books with two primary flows:

1. **Sell** — users list books (new or used) for sale; other users buy them.
2. **Rent** — users rent a book for a defined period and must return it.

A **mock payment system** handles checkout. It simulates success/failure without hitting a real payment provider.

### Core concepts (initial sketch, subject to change)

- **User** — can act as seller, buyer, and/or renter.
- **Book listing** — a specific copy of a book offered by a user, marked as `ForSale` or `ForRent` (or both).
- **Order** — a purchase or a rental, with status (Pending, Paid, Shipped, Returned, Cancelled, etc.).
- **Rental** — an order subtype with a rental period and a return deadline.
- **Payment** — a mock record attached to an order; fields for amount, status, method, mock transaction id.

These are starting points. Refine them as we build.

## Tech stack

| Layer                | Choice                                                                  |
| -------------------- | ----------------------------------------------------------------------- |
| Backend (active)     | Go — framework / router / ORM TBD (decided incrementally as we learn)   |
| Backend (paused)     | .NET 10 (ASP.NET Core Web API) + ASP.NET Core Identity + JWT            |
| Database (Go)        | PostgreSQL — fresh schema, separate DB from the .NET one (TBD)          |
| Database (.NET)      | PostgreSQL via Npgsql + Entity Framework Core                           |
| Frontend             | React 19 + Vite + TypeScript (strict, `erasableSyntaxOnly`)             |
| Client state         | Zustand (with `persist` middleware, localStorage-backed)                |
| Server state         | TanStack Query v5 over an axios client                                  |
| Routing              | React Router v7 (`createBrowserRouter`)                                 |
| Forms                | react-hook-form + zod (+ `@hookform/resolvers`)                         |
| UI                   | shadcn/ui on Tailwind v4 (Nova preset, Radix primitives, Lucide)        |
| Toasts               | sonner                                                                  |
| Payments             | Mock — in-process service, no external provider                         |
| Orchestration        | Docker + docker-compose for db (backend + client containerization pending) |

## Project layout

```
book-ecommerce-app/
  client/                           # Vite + React + TS, feature-sliced (still talks to server_asp)
    src/
      app/                          # queryClient, route guards
      components/{ui,layout}/
      features/{auth,admin-users,admin-roles,permissions,buyer,seller,shared}/
      lib/                          # apiClient, queryKeys, types/, handleMutationError, utils
      stores/                       # Zustand stores (authStore)
      router.tsx
      main.tsx
  server_asp/                       # .NET 10 backend — paused, kept for reference
    BookEcom.sln
    BookEcom.Api/                   # composition root, controllers, Program.cs
    BookEcom.Application/           # services, DTOs, AppUser, IJwtTokenService
    BookEcom.Domain/                # entities, repo interfaces, Result<T>
    BookEcom.Infrastructure/        # AppDbContext, repos, migrations, JwtTokenService
  server_go/                        # Go backend — active learning track (currently empty)
  docker-compose.yml                # Postgres only; API/client containers pending
```

The `server_go/` layout will grow incrementally as we learn — module init, conventional `cmd/` + `internal/` split, etc. — rather than being scaffolded up front.

## Conventions

### Backend — `server_asp/` (.NET Core, paused)

These conventions describe the paused .NET backend. They are preserved as reference; new work happens in `server_go/`.


- Target **.NET 10** (LTS, released Nov 2025).
- **Async all the way** for I/O: `async`/`await`, `CancellationToken` on controller/service methods.
- Use **DTOs** at API boundaries; do not expose EF entities directly.
- Prefer **constructor injection** (primary constructors where they shorten things).
- **Migrations** via EF Core CLI (`dotnet ef migrations add <Name>`). Never edit schema by hand.
- Standard REST: plural resource names (`/api/books`, `/api/users`), proper verbs and status codes.
- Input validation via data annotations for now (FluentValidation decision pending).
- **Error format TODO** — CLAUDE.md wants RFC 7807 `ProblemDetails` but controllers currently return ad-hoc `{ error: "..." }`. The client has an `ApiProblem` parser ready; server retrofit is a pending follow-up (see `project_api_review_2026-04-22.md`).
- Optimistic concurrency on user + role mutations via `ConcurrencyStamp`. 409 on mismatch.

### Backend — `server_go/` (Go, active)

Conventions will be added here **as we adopt them**, not before. Empty by design — the Go track is being built one teachable concept at a time, and writing aspirational rules ahead of practice would defeat the learning goal.

Likely early decisions to make together (do not pre-commit):

- **Module layout** — single binary under `cmd/api/`, business logic under `internal/` (Go's enforced visibility boundary).
- **Router / web framework** — `net/http` + `chi` vs. Echo/Gin/Fiber; pick the one whose tradeoffs the user can articulate.
- **DB driver / query layer** — `database/sql` + `pgx` vs. `sqlc` vs. an ORM like GORM/Ent; revisit after first endpoint.
- **Migrations** — `golang-migrate` or `goose`; pick once we have a first table.
- **Config** — env vars via `os.Getenv` to start; introduce `viper` only if it earns its weight.
- **Auth** — JWT via `golang-jwt`; password hashing via `bcrypt`. Identity-equivalent (users, roles, permissions) is rolled by hand.

### Frontend (React)

- **Server state is React Query, not `useEffect` + `fetch`.** Every server read is a `useQuery`; every write is a `useMutation`. Cache invalidation drives re-fetches.
- **Client state is Zustand, not Context.** One store per concern (currently just `authStore`). Persisted with the `persist` middleware where needed. Don't reach for Context or RTK.
- **HTTP via a single axios instance** (`lib/apiClient.ts`) with a request interceptor that attaches the JWT and a response interceptor that clears the auth store on 401 + normalizes `application/problem+json` bodies into a typed `ApiProblem`.
- **Forms: react-hook-form + zod.** zod schemas double as TypeScript types via `z.infer`. Don't hand-roll `useState` per field.
- **UI: shadcn/ui components in `src/components/ui/`.** These are code we own — edit them freely. Don't introduce MUI or Chakra.
- **Feature folders** own `api.ts` (typed axios calls), `hooks.ts` (React Query wrappers), `schemas.ts` (zod), `pages/`. Cross-feature imports go through the feature root, not into its `pages/`.
- **409 / optimistic concurrency UX:** use the shared `lib/handleMutationError.ts` helper — it toasts and invalidates. For edit surfaces that hydrate from a query, use `key={data.concurrencyStamp}` on the edit sections so a successful save or invalidate-driven refetch cleanly remounts local form state (no `useEffect` sync — see `feedback_client_patterns.md` in memory).
- **TypeScript enums are forbidden** (`erasableSyntaxOnly: true` in tsconfig). Use `as const` objects + a derived union type.
- **JWT in localStorage** via Zustand persist. Refresh-token flow is a later phase — until then, short access-token lifetime is the mitigation.

### Database (PostgreSQL)

- Schema owned by EF Core migrations.
- `snake_case` table/column names via Npgsql naming conventions.
- Money values: `decimal` (EF) / `numeric` (Postgres) — never `float`/`double`.
- Timestamps: `timestamp with time zone` (UTC only).

### Docker

- Postgres runs via `docker-compose.yml` today.
- API and client Dockerfiles are a later task — when added, multi-stage build for .NET and the React app, Postgres named volume to survive restarts, secrets via `.env` + env vars.

## Learning emphasis

Since this is interview prep, when we implement a feature surface the **teachable moments** briefly. Call these out as they come up naturally — don't lecture pre-emptively.

### Go (active)

Topics likely to come up as we build, in roughly the order they tend to bite:

- **Packages, modules, imports** — `go mod init`, internal/external visibility, the `internal/` directory rule.
- **Goroutines, channels, `context.Context`** — concurrency model; what's the equivalent of cancellation tokens / async/await.
- **Interfaces (structural / implicit satisfaction)** — the biggest mental shift from C#/Java/TS; small interfaces, accept-interfaces-return-structs.
- **Error handling** — explicit `error` returns, `errors.Is` / `errors.As`, sentinel vs typed vs wrapped errors; no exceptions.
- **Slices, maps, pointers vs values** — when `*T` vs `T`, slice header semantics, map zero-value gotchas.
- **`net/http` request lifecycle** — handlers, middleware as `http.Handler` wrappers, why this is much simpler than ASP.NET's pipeline.
- **Project structure** — `cmd/`, `internal/`, `pkg/` conventions; idiomatic Go is flatter than clean-architecture .NET.
- **Database access** — `database/sql` vs `pgx` vs `sqlc`; `Tx` for transactions; struct scanning patterns.
- **Auth** — JWT signing/validation, password hashing, middleware chains for auth + RBAC.
- **Testing** — table-driven tests, `httptest`, integration tests against real Postgres (testcontainers-go).
- **Build/deploy** — single static binary, cross-compilation, why this changes the deployment story vs .NET.

### .NET (paused — for reference)

- DI & service lifetimes (Singleton / Scoped / Transient).
- Middleware pipeline and request lifecycle.
- EF Core: change tracking, eager vs lazy loading, `AsNoTracking`, N+1, migrations, `ExecuteDeleteAsync`.
- Async/await: `ConfigureAwait`, deadlocks, `CancellationToken` propagation.
- AuthN/AuthZ: JWT, cookie auth, policy-based authorization, `ConcurrencyStamp` vs `SecurityStamp`.
- Error handling: exception middleware, `ProblemDetails`.
- Testing: xUnit, Moq/NSubstitute, `WebApplicationFactory` + Testcontainers.
- Clean architecture / DDD-lite: Domain / Application / Infrastructure / API split.
- REST vs RPC, idempotency, optimistic concurrency.

## Working agreement

- This document is a **living spec**. Update it as decisions are made and features land.
- Prefer small, end-to-end vertical slices.
- Detailed state (what's shipped, what's pending, known issues) lives in the author's Claude memory under this project — `MEMORY.md` indexes them. CLAUDE.md is conventions + shape; memory is the punch list.
- Short changelog at the bottom of this file when major decisions change.

## Open questions / to decide

- **Pagination strategy** for `GET /api/users` and future large lists.
- **Refresh tokens** — `RefreshTokens` table + `POST /api/auth/refresh`. Also enables password reset + email confirmation flows. Needed for the SecurityStamp-based session invalidation already wired into `SetRoles` to actually take effect.
- **Test strategy** — xUnit + WebApplicationFactory + Testcontainers pencilled in; not yet started.
- **Shipping/logistics model** for sales — simulate or ignore for v1.
- **Late-return policy** for rentals — fee calculation rules.

## Changelog

- **2026-05-03** — **Pivot: ASP.NET Core paused, Go track started.** Renamed `server/` → `server_asp/` (preserved end-of-Phase-8B state), created empty `server_go/`. Motivation: a new Go role to interview for. Scope: same product domain, new fresh Postgres schema for Go (no schema reuse from EF migrations), React `client/` continues to talk to `server_asp` until Go reaches parity. Pacing rule for the Go track: one concept at a time, no scaffolding ahead of the discussion. `package.json` scripts updated: `server:asp` and `server:go` (the latter assumes a future `cmd/api` entry point and will fail until that exists — placeholder).

- **2026-04-26** — Phase 6 (RFC 7807 ProblemDetails) shipped (one commit `ebe42de`). `ResultExtensions.ToFailureResult` now emits `application/problem+json` for every domain failure (404/409/400/403/401/500); domain validation `Details` ride along as a top-level `errors` extension. New `GlobalExceptionHandler : IExceptionHandler` is the safety net for anything that escapes a controller without becoming a `Result` — sanitises in non-Development, leaks exception type + stack trace as extensions in Development. `AddProblemDetails()` also normalises framework-side errors (auth 401/403, model-binding 400s) onto the same envelope. Client `ApiError` parser was already RFC 7807-aware so the switch is transparent. Closes review finding #3.
- **2026-04-26** — `UserType.Admin → UserType.Employee` rename + default-role-on-creation for all user types (one commit `605de32`). UserType becomes a coarse classification (Buyer / Seller / Employee); roles carry the fine-grained capability (SuperAdmin / Moderator / SupportAdmin / per-tenant grants). Internal staff are now Employees, admin-created via `POST /api/users` with the new auto-assigned `Employee` baseline role; admins layer SuperAdmin/etc. on top via `PUT /api/users/{id}/roles`. `AuthService.RegisterAsync` and `UserManagementService.CreateAsync` both auto-assign the corresponding default role. `IdentitySeeder.EnsureRegistrationRolesAsync` ensures Buyer/Seller/Employee roles exist on every boot (permissions seeded on first creation only — admins customise from there). DB has nothing to migrate (numeric values preserved). Five client files renamed in lockstep. Closes review finding #5.
- **2026-04-26** — Phase 8B Step 6 shipped (four commits): policy-based authorization end-to-end. *8B.6.1*: `IPermissionService.GetEffectivePermissionsAsync` computes union of role + direct permissions (SuperAdmin shortcut emits `PermissionNames.All`); `IJwtTokenService.CreateAccessToken` emits `role` and `perm` claims at login. *8B.6.2*: `PermissionRequirement` + `PermissionAuthorizationHandler` + `HasPermissionAttribute` + `PermissionPolicyProvider` in `BookEcom.Application/Auth/Authorization/` — dynamic policy resolution so adding a new permission to `PermissionNames.cs` needs zero DI plumbing. *8B.6.3*: every admin endpoint (Users, Roles, Permissions) and every Books endpoint now declares `[HasPermission(...)]` per action. `Users.Me` keeps bare `[Authorize]` (Pattern 1 from `project_rbac.md`). The #1 pre-ship blocker is closed — a Buyer attempting `PUT /api/users/{id}/roles` now gets 403. Newly-visible consequence: fresh Buyers/Sellers without a default role get 403 on `/api/books` too — "default role on register" is the next item.
- **2026-04-25** — Phase 5 shipped end-to-end (six commits). *5a.1–5a.5*: introduced `IUnitOfWork` + per-aggregate repository abstractions in `BookEcom.Domain.Abstractions` (`IBookRepository`, `IPermissionRepository`, `IRoleRepository`, `IUserRepository`); every Application service dropped `AppDbContext` and now goes through these. Read paths use untracked domain projections (`RoleSummary`, `UserSnapshot`) so Domain stays Identity-free. Concurrency-stamp bumps switched from change-tracker mutation + `DbUpdateConcurrencyException` catch to atomic `ExecuteUpdate` returning a bool. *5b*: relocated `AppUser`, `IJwtTokenService`, and `JwtOptions` from Infrastructure to `BookEcom.Application.Auth` and flipped the project arrow — Infrastructure now references Application. Result: Application has zero `using BookEcom.Infrastructure` and zero project references to Infrastructure. The dependency graph finally matches the canonical Onion / Clean Architecture shape — `Api → Application ← Infrastructure`, both pointing inward to `Domain`. SOLID's `D` is satisfied at the project-graph level. Resolves `project_decisions.md` items #1 (AppDbContext in services) and #6 (no `CancellationToken` in repo/service layer).
- **2026-04-23** — Client rewritten (feature-sliced React app) and admin panel shipped end-to-end: Users + Roles CRUD, role/direct-permission attachment, user create/delete. Server gained `POST /api/users` + `DELETE /api/users/{id}`. Stack finalized: Zustand + React Query + axios + shadcn/ui + Tailwind v4 + react-hook-form + zod + sonner.
- **2026-04-22** — Phase 8B landed: Permissions catalog (code-defined `PermissionNames`), role+user permission entities, EF configs extracted to `Data/Configurations/`, `IdentitySeeder` rewritten (4-step idempotent), full admin endpoints for Roles and Users (attachment + concurrency). Review identified authz gap as the #1 pre-ship item.
- **2026-04-20** — Project initialized. Stack chosen: React + .NET 10 + PostgreSQL + Docker. Mock payment system. Primary goal: .NET Core interview prep.
