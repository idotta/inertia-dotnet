# Sample App Design: "Velocity" -- Team Project Tracker

## Context

The inertia-dotnet library has completed all 10 implementation phases covering 50+ features. We need a sample app that serves as both a **demonstration** (showing consumers how to use every feature) and a **validation tool** (smoke-testing the full feature surface). No sample app exists yet -- the README references planned samples that haven't been created.

## Recommendation: "Velocity"

A lightweight **team project tracker** where teams manage projects, tasks, members, and an activity feed. The domain naturally produces the data shapes, pagination patterns, and authentication concerns that exercise every library feature without contrivance.

### Why This Domain

| Need | Natural Fit |
|------|------------|
| Paginated lists (ScrollProp, Merge, Append/Prepend) | Task lists, activity feeds |
| Deferred expensive data (DeferProp, groups) | Dashboard analytics, charts |
| Always-available auth (AlwaysProp, Share) | Current user, permissions |
| On-demand detailed data (OptionalProp) | Audit logs, export configs |
| One-time config (OnceProp + .Until()) | Feature flags, server info |
| Form submissions (redirects, flash, validation) | Create/edit tasks, members |
| External redirects (Location) | GitHub links, docs |
| Admin vs. regular views (RootViewProvider) | Settings page |
| Error states (ExceptionHandler) | 404 task, 500 server error |
| Mixed routing (MVC + minimal API) | CRUD controllers + quick endpoints |

### Pages & Feature Coverage

**Dashboard** (`DashboardController`)
- `Prop.Always()` for auth user
- `Prop.Defer()` with groups ("stats" group: analytics + charts)
- `Prop.Once()` with `.Until(30min)` for server metadata
- `Prop.Merge()` for activity feed
- `.WithViewData()` for Razor view data
- Plain anonymous object props

**Projects Index** (`ProjectsController`)
- `Prop.Optional()` for stats loaded on demand
- `Prop.DeepMerge()` for nested project data
- Partial reloads via search/filter

**Project Show**
- `IInertiaPropertyProvider` (AuthPropertyProvider shared via controller)
- `EncryptHistory()` for sensitive project data
- `PreserveFragment()` for tab navigation (#tasks, #members)
- `Prop.Defer()` for task list
- `Prop.Merge()` with `.Once().As()` for comments

**Project Create/Edit**
- Flash data for success messages
- Validation error pipeline (ModelState errors)
- 302-to-303 redirect conversion (POST/PUT)
- Flash reflashing on redirects

**Tasks Index** (`TasksController`)
- `Prop.Scroll()` for infinite scroll
- `IScrollMetadataProvider` (PaginationMetadataProvider)

**Task Show**
- `Prop.Optional()` with `.Once().As().Until()` chain (audit log)
- `Prop.Defer()` with `.Merge().Append().MatchOn()` (related tasks)
- `Prop.Defer()` with `.Once().Fresh()` (attachments)
- `Prop.Once()` with `.As()` (task config)
- `Prop.Merge()` with `.Prepend()` (comments)

**Members Index** (`MembersController`)
- `Share()` with dictionary overload
- `Prop.DeepMerge()` for nested roles/permissions
- `Prop.Defer()` for invite stats

**Settings** (`SettingsController`)
- `ClearHistory()` after password change
- `EncryptHistory()` for all settings pages
- `WithoutSsr()` for sensitive content
- Alternate Razor root view via `RootViewProvider`

**Minimal API Endpoints** (`Program.cs`)
- `MapInertia("/about", "About")` shorthand
- `Location()` for external GitHub redirect
- `inertia.Render()` returning `IResult` (notifications page)

**Error Handling** (`Program.cs`)
- `InertiaExceptionResult.Render()` for 404s with `.WithSharedData()`
- `InertiaExceptionResult.Redirect()` for 500s

### Program.cs Configuration

Exercises all `InertiaOptions` delegates:
- `VersionProvider` -- manifest file hash
- `SharedPropsProvider` -- app name, CSRF token
- `RootViewProvider` -- Admin.cshtml for /settings, App.cshtml default
- `OnVersionChange` -- custom 409 response
- `OnEmptyResponse` -- 204 No Content
- `ExceptionHandler` -- 404 render, 500 redirect
- `EnsurePagesExist` -- dev mode page validation
- `JsonSerializerOptions` -- custom serialization
- `SsrEnabled`, `SsrUrl`, `SsrEnsureBundleExists`

Middleware: `UseInertiaEncryptHistory()` + `UseInertia()` + `UseExceptionHandler()`

### Project Structure

```
samples/
  Velocity/
    Velocity.csproj              # net10.0, references Inertia.AspNetCore
    Program.cs                   # DI, middleware, minimal API endpoints
    Controllers/                 # MVC controllers (5)
    Models/                      # EF Core InMemory entities (4)
    Providers/                   # AuthPropertyProvider, PaginationMetadataProvider
    Views/                       # App.cshtml, Admin.cshtml, _ViewImports.cshtml
    ClientApp/src/Pages/         # React + TypeScript frontend
  Velocity.Tests/
    Velocity.Tests.csproj        # Integration tests with AssertableInertia
```

### Key Design Decisions

1. **EF Core InMemory** -- zero external deps, seeded on startup, `dotnet run` just works
2. **Two Razor root views** -- App.cshtml (default) + Admin.cshtml (settings) to exercise `RootViewProvider`
3. **MVC for CRUD + minimal API for reads** -- demonstrates `InertiaResponse` as both `IActionResult` and `IResult`
4. **React + TypeScript frontend** -- most popular Inertia.js framework (Vite + @inertiajs/react)
5. **Test project alongside sample** -- validates the app AND demonstrates `Inertia.Testing` usage
6. **Location**: Inside this repo at `samples/Velocity/`, project-references the library

### Implementation Sequence

1. Scaffold project, add to solution
2. Models + InMemory DB + seed data
3. Program.cs with full configuration
4. Providers (AuthPropertyProvider, PaginationMetadataProvider)
5. Razor views with tag helpers
6. Controllers (5) exercising all features
7. React frontend (Vite + @inertiajs/react)
8. Integration tests using AssertableInertia
9. Validation run

### Verification

- `dotnet build` -- compiles cleanly
- `dotnet test` in Velocity.Tests -- all integration tests pass
- `dotnet run` -- app starts, pages render (CSR mode without SSR server)
- Each feature has at least one test asserting its behavior via `AssertableInertia`
