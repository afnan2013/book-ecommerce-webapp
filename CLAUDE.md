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

| Layer        | Choice                                           |
| ------------ | ------------------------------------------------ |
| Frontend     | React                                            |
| Backend      | .NET Core (ASP.NET Core Web API)                 |
| Database     | PostgreSQL                                       |
| ORM          | Entity Framework Core (Npgsql provider)          |
| Payments     | Mock — in-process service, no external provider  |
| Orchestration| Docker + docker-compose for db, server, client   |

## Project layout (target)

```
book-ecommerce-app/
  client/              # React app
  server/              # .NET Core solution
    BookEcom.Api/      # ASP.NET Core Web API
    BookEcom.Domain/   # Entities, value objects, domain rules
    BookEcom.Infrastructure/ # EF Core, DbContext, repositories, external integrations
    BookEcom.Application/    # Use cases / services / DTOs (optional, if we go clean-ish)
    BookEcom.Tests/          # Unit / integration tests
  docker-compose.yml   # db + server + client
  .env.example         # sample env vars (never commit real secrets)
  CLAUDE.md
  README.md
```

The split between `Domain`/`Application`/`Infrastructure`/`Api` is a common clean-architecture layout that interviewers often ask about. We can start simpler and refactor into it once the concepts land.

## Conventions

### Backend (.NET Core)
- Target **.NET 10** (current LTS, released Nov 2025, supported through Nov 2028).
- **Async all the way** for I/O: `async`/`await`, `CancellationToken` on controller/service methods.
- Use **DTOs** at API boundaries; do not expose EF entities directly.
- Prefer **constructor injection**; avoid service locators.
- **Migrations** via EF Core CLI (`dotnet ef migrations add <Name>`). Never edit the database schema by hand.
- Follow standard REST: plural resource names (`/api/books`, `/api/orders`), proper HTTP verbs and status codes.
- Validate input with data annotations or FluentValidation — pick one and be consistent.
- Return `ProblemDetails` for errors (RFC 7807) — interviewers like seeing this.

### Frontend (React)
- Keep it simple at first: functional components, hooks, fetch/axios.
- Defer state-management library decisions (Redux / Zustand / React Query) until we actually feel the pain.
- Talk to the API through a single `api` client module so base URL / auth is configured in one place.

### Database (PostgreSQL)
- Schema owned by EF Core migrations.
- Use `snake_case` table/column names via Npgsql naming conventions (interviewers often ask about this).
- Money values: store as `decimal` (EF) / `numeric` (Postgres) — never `float`/`double`.
- Timestamps: `timestamp with time zone` (UTC only).

### Docker
- Each service has its own `Dockerfile` (multi-stage build for .NET and the React app).
- `docker-compose.yml` wires up: `db` (Postgres), `server` (.NET API), `client` (React).
- Use a named volume for Postgres data so restarts don't wipe the db.
- Secrets/config via `.env` + env vars, never hardcoded.

## Learning emphasis

Since this is interview prep, when we implement a feature please surface the **teachable moments** briefly:

- **DI & service lifetimes** (Singleton / Scoped / Transient) — when and why.
- **Middleware pipeline** and request lifecycle.
- **EF Core**: change tracking, lazy vs eager loading, `AsNoTracking`, N+1 problems, migrations.
- **Async/await**: `ConfigureAwait`, deadlocks, `CancellationToken` propagation.
- **Authentication/Authorization**: JWT, cookie auth, policy-based authorization.
- **Error handling**: exception middleware, `ProblemDetails`.
- **Testing**: xUnit, Moq/NSubstitute, integration tests with `WebApplicationFactory` and Testcontainers.
- **Clean architecture / DDD-lite**: separating Domain / Application / Infrastructure / API.
- **REST vs RPC**, idempotency, optimistic concurrency.

We don't need to cover all of this up front — call them out as they come up naturally in the code.

## Working agreement

- This document is a **living spec**. Update it as decisions are made and features land.
- Prefer small, end-to-end vertical slices (e.g., "list a book" working across db → API → UI) over building each layer in isolation.
- Keep a short changelog at the bottom of this file when major decisions change.

## Open questions / to decide

- Authentication approach (JWT with refresh tokens? cookie sessions? simple for v1?)
- Whether to include an `Application` layer from day one or fold it into the API project
- Frontend tooling: Vite vs Create React App (Vite is the modern default)
- Test strategy: how much integration coverage with Testcontainers
- Shipping/logistics model for sales — simulate or ignore for v1?
- Late-return policy for rentals — fee calculation rules?

## Changelog

- **2026-04-20** — Project initialized. Stack chosen: React + .NET 10 + PostgreSQL + Docker. Mock payment system. Primary goal: .NET Core interview prep.
