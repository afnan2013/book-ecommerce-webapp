# Book Ecommerce App

A full-stack marketplace for books. Users can **sell** books (new or used) and **rent** books for a defined period with returns. A **mock payment gateway** simulates checkout without touching a real provider.

The codebase is deliberately structured around **SOLID principles** and proven design patterns so that business rules stay independent of frameworks, databases, and external services.

---

## Features

- Browse, search, and purchase books
- List books for sale (new or used condition)
- Rent a book for a defined period; return it before the deadline
- Mock payment gateway simulating both success and failure paths
- Unified user accounts — a single user can be seller, buyer, and renter

## Tech Stack

| Layer          | Technology                                      |
| -------------- | ----------------------------------------------- |
| Frontend       | React + Vite                                    |
| Backend        | ASP.NET Core Web API on .NET 10                 |
| Database       | PostgreSQL                                      |
| ORM            | Entity Framework Core (Npgsql provider)         |
| API Docs       | OpenAPI + Scalar                                |
| Payments       | Mock in-process gateway                         |
| Infrastructure | Docker + docker-compose                         |

---

## Architecture

### SOLID

| Principle                   | How it shows up                                                                                         |
| --------------------------- | ------------------------------------------------------------------------------------------------------- |
| **Single Responsibility**   | Controllers handle HTTP only. Persistence, validation, and business logic live elsewhere.               |
| **Open / Closed**           | Services extend through interfaces, not modification — a new payment method plugs in as a new adapter.  |
| **Liskov Substitution**     | Repository and gateway implementations honor their interface contracts; real and mock doubles swap.     |
| **Interface Segregation**   | Prefer narrow interfaces (`IBookReader`, `IBookWriter`) over one fat `IBookService`.                    |
| **Dependency Inversion**    | High-level code depends on abstractions (`IBookRepository`, `IPaymentGateway`); adapters live at edges. |

### Design patterns

- **Repository** — persistence abstracted from business logic
- **Dependency Injection** — ASP.NET Core's built-in container wires everything at startup
- **DTO** — API request/response shapes separated from persistence entities
- **Strategy** — real vs. mock payment gateway hidden behind one interface, selected via DI registration
- **Options** — typed configuration binding (`IOptions<T>`) for connection strings, JWT secrets, fee rules, etc.
- **Unit of Work** — EF Core's `DbContext` scoped per request, commits change-tracked writes atomically

### Layer boundaries (Clean Architecture)

```
┌────────────────────────────────────────────────────┐
│                 BookEcom.Api                       │   HTTP, controllers, DI wiring
│                      │                             │
│              BookEcom.Application                  │   use cases, DTOs, interfaces
│                      │                             │
│                BookEcom.Domain                     │   entities, value objects, business rules
│                                                    │
│  BookEcom.Infrastructure  →  Application + Domain  │   EF Core, Postgres, mock payment gateway
│                                                    │
│         BookEcom.Tests  →  all layers              │
└────────────────────────────────────────────────────┘
```

Dependencies point **inward**. `Domain` has zero framework dependencies. `Api` references `Infrastructure` only at the composition root (for DI registration) — controllers never see EF Core directly.

---

## Project Structure

```
book-ecommerce-app/
├── client/                         # React app (Vite)
├── server/
│   ├── BookEcom.Api/               # HTTP layer, controllers, DI wiring
│   ├── BookEcom.Application/       # Use cases, DTOs, service interfaces
│   ├── BookEcom.Domain/            # Entities, value objects, business rules
│   ├── BookEcom.Infrastructure/    # EF Core, Postgres, mock payment gateway
│   └── BookEcom.Tests/             # Unit + integration tests
├── docker-compose.yml              # db + server + client
├── .env.example                    # sample environment variables
├── .gitignore
├── .gitattributes
└── README.md
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

PostgreSQL runs inside the docker-compose stack — no separate install needed.

### Run everything with Docker

```bash
cp .env.example .env          # edit values if needed
docker compose up -d
```

| Service              | URL                                  |
| -------------------- | ------------------------------------ |
| API                  | http://localhost:5027                |
| Scalar API explorer  | http://localhost:5027/scalar/v1      |
| React app            | http://localhost:5173                |
| PostgreSQL           | localhost:5432                       |

### Run pieces individually

Backend with hot reload:

```bash
cd server/BookEcom.Api
dotnet watch run
```

Frontend:

```bash
cd client
npm install
npm run dev
```

PostgreSQL only (handy when running the API directly on your host):

```bash
docker compose up -d db
```

---

## API Documentation

- **Scalar interactive UI:** http://localhost:5027/scalar/v1
- **OpenAPI JSON spec:** http://localhost:5027/openapi/v1.json

Both are mounted only in the Development environment.

---

## Database & Migrations

EF Core migrations are the source of truth for the schema.

```bash
# create a new migration
dotnet ef migrations add <Name> \
    --project server/BookEcom.Infrastructure \
    --startup-project server/BookEcom.Api

# apply pending migrations
dotnet ef database update \
    --project server/BookEcom.Infrastructure \
    --startup-project server/BookEcom.Api
```

Conventions:

- Money stored as `decimal` in .NET / `numeric` in Postgres — never floating-point
- Timestamps stored as `timestamp with time zone`, UTC only
- Table and column names in `snake_case` via Npgsql naming conventions

---

## Testing

```bash
dotnet test
```

Unit tests cover Domain and Application layers; integration tests run against a real Postgres container to catch mapping and migration bugs that mocks would hide.

---

## Configuration

All environment-specific values (connection strings, JWT secrets, payment-mock toggles, CORS origins) are read from `.env` in dev and from real environment variables in production. See `.env.example` for the full list. Secrets never belong in `appsettings.json`.

---

## Coding Conventions

- **Async all the way** for I/O; `CancellationToken` propagated from controllers to data access
- **Nullable reference types enabled** — non-nullable where possible, `?` explicit at boundaries
- **DTOs at the API boundary** — EF entities never leak through controllers
- **`ProblemDetails` (RFC 7807)** for all error responses
- **LF line endings**, enforced via `.gitattributes`

---

## Current Status

- [x] Web API scaffold (.NET 10, ASP.NET Core controllers)
- [x] In-memory `BooksController` with full CRUD
- [x] OpenAPI spec + Scalar UI
- [ ] Clean-architecture project split (Domain / Application / Infrastructure)
- [ ] PostgreSQL + EF Core migrations
- [ ] Authentication & authorization
- [ ] Rental lifecycle (checkout, return, late-fee handling)
- [ ] Mock payment gateway
- [ ] Dockerized multi-service compose
- [ ] React frontend
- [ ] Unit + integration tests
- [ ] CI pipeline
