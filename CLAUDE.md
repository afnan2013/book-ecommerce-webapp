# Book Ecommerce App

## Purpose

This project is a **learning exercise** to prepare for an upcoming **.NET Core interview**. The author is learning .NET Core by building a realistic full-stack application end-to-end. Explanations, design choices, and code should favor **clarity and interview-relevant concepts** over clever shortcuts.

When suggesting an approach, briefly explain the "why" and, where it's useful, mention the .NET concept or pattern being applied (e.g., DI, middleware, EF Core conventions, DTO vs entity, async/await pitfalls). Point out the kinds of follow-up questions an interviewer might ask.

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

| Layer         | Choice                                                             |
| ------------- | ------------------------------------------------------------------ |
| Backend       | .NET 10 (ASP.NET Core Web API) + ASP.NET Core Identity + JWT       |
| Database      | PostgreSQL via Npgsql + Entity Framework Core                      |
| Frontend      | React 19 + Vite + TypeScript (strict, `erasableSyntaxOnly`)        |
| Client state  | Zustand (with `persist` middleware, localStorage-backed)           |
| Server state  | TanStack Query v5 over an axios client                             |
| Routing       | React Router v7 (`createBrowserRouter`)                            |
| Forms         | react-hook-form + zod (+ `@hookform/resolvers`)                    |
| UI            | shadcn/ui on Tailwind v4 (Nova preset, Radix primitives, Lucide)   |
| Toasts        | sonner                                                             |
| Payments      | Mock — in-process service, no external provider                    |
| Orchestration | Docker + docker-compose for db (server/client containerization pending) |

## Project layout

### Actual today

```
book-ecommerce-app/
  client/                           # Vite + React + TS, feature-sliced
    src/
      app/                          # queryClient, route guards
      components/
        ui/                         # shadcn primitives
        layout/                     # Public/Admin/Dashboard layouts
      features/                     # feature folders: api.ts + hooks.ts + schemas.ts + pages/
        {auth, admin-users, admin-roles, permissions, buyer, seller, shared}/
      lib/                          # apiClient, queryKeys, types/, handleMutationError, utils
      stores/                       # Zustand stores (authStore)
      router.tsx
      main.tsx
  server/
    BookEcom.Api/                   # single project today; see "Target" below
      Auth/                         # AppUser, UserType, JwtTokenService, PermissionNames, RoleNames
      Controllers/
      Data/
        Configurations/             # IEntityTypeConfiguration<T> per entity
        Seed/IdentitySeeder.cs
      Dtos/
      Entities/
      Migrations/
  docker-compose.yml                # Postgres only; API/client containers pending
```

### Target (clean-architecture refactor, deferred)

```
server/
  BookEcom.Api/                     # ASP.NET Core Web API — composition root
  BookEcom.Domain/                  # Entities, value objects, domain rules
  BookEcom.Infrastructure/          # EF Core, DbContext, repositories, external integrations
  BookEcom.Application/             # Use cases / services / DTOs
  BookEcom.Tests/                   # xUnit + WebApplicationFactory + Testcontainers
```

Partial progress: entities are already in `Entities/` and EF configs are extracted to `Data/Configurations/` — the domain-layer extraction will be mostly mechanical when prioritized.

## Conventions

### Backend (.NET Core)

- Target **.NET 10** (LTS, released Nov 2025).
- **Async all the way** for I/O: `async`/`await`, `CancellationToken` on controller/service methods.
- Use **DTOs** at API boundaries; do not expose EF entities directly.
- Prefer **constructor injection** (primary constructors where they shorten things).
- **Migrations** via EF Core CLI (`dotnet ef migrations add <Name>`). Never edit schema by hand.
- Standard REST: plural resource names (`/api/books`, `/api/users`), proper verbs and status codes.
- Input validation via data annotations for now (FluentValidation decision pending).
- **Error format TODO** — CLAUDE.md wants RFC 7807 `ProblemDetails` but controllers currently return ad-hoc `{ error: "..." }`. The client has an `ApiProblem` parser ready; server retrofit is a pending follow-up (see `project_api_review_2026-04-22.md`).
- Optimistic concurrency on user + role mutations via `ConcurrencyStamp`. 409 on mismatch.

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

Since this is interview prep, when we implement a feature surface the **teachable moments** briefly:

- **DI & service lifetimes** (Singleton / Scoped / Transient) — when and why.
- **Middleware pipeline** and request lifecycle.
- **EF Core**: change tracking, lazy vs eager loading, `AsNoTracking`, N+1 problems, migrations, `ExecuteDeleteAsync` vs change tracker.
- **Async/await**: `ConfigureAwait`, deadlocks, `CancellationToken` propagation.
- **Authentication/Authorization**: JWT, cookie auth, policy-based authorization, `ConcurrencyStamp` vs `SecurityStamp`.
- **Error handling**: exception middleware, `ProblemDetails`.
- **Testing**: xUnit, Moq/NSubstitute, integration tests with `WebApplicationFactory` and Testcontainers.
- **Clean architecture / DDD-lite**: separating Domain / Application / Infrastructure / API.
- **REST vs RPC**, idempotency, optimistic concurrency.

Call these out as they come up naturally.

## Working agreement

- This document is a **living spec**. Update it as decisions are made and features land.
- Prefer small, end-to-end vertical slices.
- Detailed state (what's shipped, what's pending, known issues) lives in the author's Claude memory under this project — `MEMORY.md` indexes them. CLAUDE.md is conventions + shape; memory is the punch list.
- Short changelog at the bottom of this file when major decisions change.

## Open questions / to decide

- **Pre-ship blocker**: policy-based authorization. Admin endpoints currently use bare `[Authorize]` — any authenticated user can hit them. Needs `PermissionRequirement` + `[HasPermission("...")]` + `perm` claims in the JWT.
- **ProblemDetails retrofit** on the server (pairs naturally with the pending Phase 6 global error handler).
- **Default role on register** — new Buyers/Sellers currently land with zero roles/permissions. Auto-assign by UserType, or require admin to assign first?
- **Pagination strategy** for `GET /api/users` and future large lists.
- **Refresh tokens** — `RefreshTokens` table + `POST /api/auth/refresh`. Also enables password reset + email confirmation flows. Needed for the SecurityStamp-based session invalidation already wired into `SetRoles` to actually take effect.
- **Clean-architecture split** (Domain/Application/Infrastructure projects) — still deferred; unblocks repository pattern.
- **Test strategy** — xUnit + WebApplicationFactory + Testcontainers pencilled in; not yet started.
- **Shipping/logistics model** for sales — simulate or ignore for v1.
- **Late-return policy** for rentals — fee calculation rules.

## Changelog

- **2026-04-23** — Client rewritten (feature-sliced React app) and admin panel shipped end-to-end: Users + Roles CRUD, role/direct-permission attachment, user create/delete. Server gained `POST /api/users` + `DELETE /api/users/{id}`. Stack finalized: Zustand + React Query + axios + shadcn/ui + Tailwind v4 + react-hook-form + zod + sonner.
- **2026-04-22** — Phase 8B landed: Permissions catalog (code-defined `PermissionNames`), role+user permission entities, EF configs extracted to `Data/Configurations/`, `IdentitySeeder` rewritten (4-step idempotent), full admin endpoints for Roles and Users (attachment + concurrency). Review identified authz gap as the #1 pre-ship item.
- **2026-04-20** — Project initialized. Stack chosen: React + .NET 10 + PostgreSQL + Docker. Mock payment system. Primary goal: .NET Core interview prep.
