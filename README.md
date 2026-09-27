# Work Item Tracker

A small full-stack app for tracking work items: create, search, filter, paginate, and advance
items through `Todo -> InProgress -> Done`.

- **Backend**: ASP.NET Core 8 Web API, EF Core, SQLite. Layered architecture (Domain / Application
  / Infrastructure / Api).
- **Frontend**: Angular 17 (standalone components), RxJS.

## Project layout

```
WorkItemTracker.sln
src/
  WorkItemTracker.Domain          Entity + status state machine + domain exceptions
  WorkItemTracker.Application     DTOs, service interfaces, WorkItemService (orchestration)
  WorkItemTracker.Infrastructure  EF Core DbContext, SQLite, repository
  WorkItemTracker.Api             Controllers, exception-handling middleware, Program.cs
tests/
  WorkItemTracker.UnitTests           Domain + Application unit tests (no DB, no HTTP)
  WorkItemTracker.IntegrationTests    Full HTTP pipeline against a real (temp) SQLite file
client/                            Angular app
```

## Running the backend

```bash
cd src/WorkItemTracker.Api
dotnet run
```

- Serves on `http://localhost:5209` (see `Properties/launchSettings.json`).
- On startup it calls `Database.EnsureCreated()`, so `workitems.db` (SQLite) is created
  automatically on first run in the API project's working directory. Data persists across
  restarts because it's a real file, not an in-memory store.
- Swagger UI is available at `/swagger` in Development.

Run the tests:

```bash
dotnet test
```

## Running the frontend

```bash
cd client
npm install
npm start        # ng serve, http://localhost:4200
npm test         # unit tests (Karma/Jasmine) — requires Chrome/Chromium available locally
```

The API's CORS policy allows `http://localhost:4200` by default (see `Program.cs`).

## API summary

| Method | Route                          | Notes |
|--------|---------------------------------|-------|
| POST   | `/api/work-items`               | Creates an item in `Todo`. 400 on missing/too-long title. |
| GET    | `/api/work-items`                | `?search=&status=&page=&pageSize=`. |
| GET    | `/api/work-items/{id}`           | 404 if missing. (Not required by the spec, but used by the POST response's `Location` header and by an integration test proving persistence.) |
| PATCH  | `/api/work-items/{id}/status`     | Body `{ "status": "InProgress" }`. 404 if missing, 409 on an illegal transition. |

Status values are serialized as strings: `Todo`, `InProgress`, `Done`.

## Design notes

- **The transition rule lives on the `WorkItem` entity**, not in the service or controller. This
  was the main architectural call: it means `Todo -> InProgress -> Done` (and nothing else) is
  enforced no matter what calls into it, and it's testable with zero mocking
  (see `WorkItemTests.cs`).
- **A global exception-handling middleware** maps `DomainValidationException -> 400`,
  `NotFoundException -> 404`, `InvalidTransitionException -> 409`, so controllers stay free of
  try/catch and status-code plumbing.
- **Angular's search never shows stale results** because the search/filter/pagination stream is
  piped through `switchMap`: a newer request cancels whatever was still in flight. This is
  asserted directly in `work-item-list.component.spec.ts` (the test checks the older HTTP request
  is actually `cancelled`, not just ignored).
- Loading / empty / error states are handled explicitly in `WorkItemListComponent` and its
  template, rather than only the happy path.

## Assumptions (spec left these unspecified)

- **Auth**: none, per the stated scope.
- **Schema management**: `Database.EnsureCreated()` is used instead of EF Core Migrations, to
  keep setup to a single `dotnet run` for this assessment. A real project would use
  `dotnet ef migrations add/update` instead so schema changes are versioned.
- **Page size**: defaults to 10 on the frontend; the API accepts `pageSize` up to a max of 100
  and clamps out-of-range values instead of rejecting them.
- **Search**: case-insensitive substring match against `Title` only (not `Description`).
- **Sort order**: newest first (`CreatedAt` descending) — the spec didn't specify a sort order.
- **`GET /api/work-items/{id}`**: added beyond the required endpoints, purely so `POST` can
  return a spec-compliant `Location` header and so persistence could be verified end-to-end in
  an integration test without reaching back into the DB directly.
- **CORS**: restricted to `localhost:4200`/`https://localhost:4200` (the Angular dev server),
  since no deployment target was specified.
- **Concurrency**: no optimistic concurrency token; last write wins. A 409 is only returned for
  an illegal state transition, not for a race between two concurrent valid updates.
- **Angular environments**: `environment.ts` is imported directly; this scaffold doesn't wire up
  `fileReplacements` for a prod API URL swap, since only one deployment target exists here.

## What's unfinished / would do next with more time

- No `dotnet build`/`dotnet test` verification was possible in the environment this was written
  in (no .NET SDK, no NuGet access) — the Angular side was fully installed, built, and tested
  (11/11 passing), but please run `dotnet test` on your machine as the first sanity check.
- Would add EF Core Migrations instead of `EnsureCreated()` for a real deployment.
- Would add e2e coverage (e.g. Playwright) for the full create → filter → advance flow.
- Would add a small `docker-compose.yml` for one-command startup of both apps.
