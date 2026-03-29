# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

A .NET adapter for Inertia.js, porting [inertia-laravel](https://github.com/inertiajs/inertia-laravel) v3.0.1 to ASP.NET Core 10 / C# 14. The reference PHP implementation is tracked as a git submodule at `inertia-laravel/`.

## Build & Test Commands

```bash
dotnet build                                    # Build entire solution
dotnet test                                     # Run all tests
dotnet test tests/Inertia.Tests                 # Run library tests only
dotnet test tests/Inertia.Testing.Tests         # Run testing package tests only
dotnet test --filter "FullyQualifiedName~InertiaOptionsTests"  # Single test class
dotnet test --filter "FullyQualifiedName~InertiaOptionsTests.Defaults.RootView_DefaultsToAppCshtml"  # Single test
dotnet format                                   # Format code
```

Verify after each change: `dotnet format && dotnet build && dotnet test` — zero warnings, zero errors, all tests pass.

## Solution Structure

Two NuGet packages and two test projects, all targeting `net10.0`:

| Project | Path | Purpose |
|---------|------|---------|
| `Inertia.AspNetCore` | `src/Inertia.AspNetCore/` | Main library |
| `Inertia.Testing` | `src/Inertia.Testing/` | Test assertions for consumers |
| `Inertia.Tests` | `tests/Inertia.Tests/` | Tests for the main library |
| `Inertia.Testing.Tests` | `tests/Inertia.Testing.Tests/` | Tests for the testing package |

Solution file: `inertia-dotnet.slnx` (XML-based solution format).

## Architecture

### PHP-to-C# Port Strategy

The implementation plan lives at `.docs/PLAN.md`. Each C# type maps to a specific PHP counterpart in the `inertia-laravel/` submodule. When implementing or modifying features, always cross-reference the PHP source to ensure behavioral parity.

### Key Design Decisions

- **Flat namespace**: All types use `namespace Inertia.AspNetCore` regardless of subfolder (`Interfaces/`, `Contexts/`, `Props/`, etc.)
- **No ErrorOr/Result pattern**: This is a library, not application code. Use exceptions for programmer errors, nullable returns for optional results (e.g., `SsrResponse?`)
- **Static `Prop` class**: Factory methods (`Prop.Defer()`, `Prop.Always()`, `Prop.Optional()`) instead of methods on `IInertia` interface
- **Generic prop types**: `DeferProp<T>`, `OptionalProp<T>`, `AlwaysProp<T>` etc. with non-generic base interfaces (`IDeferrable`, `IMergeable`, `IOnceable`)
- **Dual result types**: `InertiaResponse` and `InertiaLocationResult` implement both `IActionResult` (MVC) and `IResult` (minimal APIs)
- **Middleware delegates over subclassing**: Customization via `Func<>` delegates on `InertiaOptions`, not virtual methods on a base class
- **`IOptions<InertiaOptions>`**: Not `IOptionsSnapshot` — startup config that doesn't change at runtime. Registered with `ValidateDataAnnotations()` + `ValidateOnStart()`
- **Session via TempData + HttpContext.Items**: Not raw `ISession`. TempData for flash data (survives one redirect), HttpContext.Items for current-request-only data
- **System.Text.Json only**: No Newtonsoft.Json dependency. Polymorphic serialization with runtime types

### DI Lifetimes

| Service | Lifetime | Reason |
|---------|----------|--------|
| `IInertia` / `InertiaFactory` | Scoped | Per-request shared props state |
| `InertiaMiddleware` | Transient | IMiddleware, resolved per request |
| `ISsrGateway` / `HttpSsrGateway` | Singleton | Stateless, uses IHttpClientFactory |
| `SsrState` | Scoped | Per-request SSR dispatch cache |
| `IOptions<InertiaOptions>` | Singleton | Startup config |

`PropsResolver` is **not** in DI — created per-response inside `InertiaResponse`, receives `IServiceProvider` from `HttpContext.RequestServices`.

### Prop Type Hierarchy

```
AlwaysProp<T>      (standalone)
OptionalProp<T>    : IIgnoreFirstLoad, IOnceable  [contains OnceInfo]
OnceProp<T>        : IOnceable  [contains OnceInfo]
MergeablePropBase  (abstract) : IMergeable
  ├── MergeProp<T> : IOnceable  [contains OnceInfo]
  ├── DeferProp<T> : IDeferrable, IIgnoreFirstLoad, IOnceable  [contains DeferInfo, OnceInfo]
  └── ScrollProp<T>: IDeferrable  [contains DeferInfo]
```

### Implementation Phases

The project follows a phased plan (see `.docs/PLAN.md`). Current status:
- **Phase 1** (constants, options, interfaces, contexts) — complete
- **Phase 2** (property types + trait compositions) — complete
- **Phase 3** (response factory + response) — complete
- **Phase 4+** (PropsResolver, middleware, SSR, DI registration, Tag Helpers, testing package) — not started

Subsequent phases build incrementally — check the plan for current status before starting work.

## Test Conventions

- **Framework**: xUnit v3 with `[Fact]` attributes (no `[Theory]` unless data-driven)
- **Assertions**: FluentAssertions 8.x (`value.Should().Be(...)`)
- **Mocking**: NSubstitute (`Substitute.For<IFoo>()`)
- **Structure**: Nested classes group tests by concern (e.g., `InertiaOptionsTests.Defaults`, `InertiaOptionsTests.Validation`)
- **Naming**: `MethodOrProperty_Scenario_ExpectedResult` (e.g., `RootView_DefaultsToAppCshtml`)
- **Integration tests**: Use `Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory`
- **InternalsVisibleTo**: `Inertia.Tests` can access internal types in `Inertia.AspNetCore`

## Code Conventions

- All properties on `InertiaOptions` use `{ get; set; }` (not `init`) for Options pattern compatibility
- No `required` keyword on Options classes (conflicts with parameterless constructor requirement)
- Contexts are sealed classes, not records (no meaningful value equality with HttpContext)
- Interfaces are query-only — fluent mutation methods go on concrete types
- IOnceable properties use explicit interface implementation on prop types (access via cast: `((IOnceable)prop).ShouldResolveOnce`)
- Concrete prop types shadow `MergeablePropBase` fluent methods with `new` for covariant return types (e.g., `public new DeferProp<T> Merge()`)
- Prop types use typed fields (`T? _value`, `Func<T>? _syncCallback`, `Func<Task<T>>? _asyncCallback`) — no reflection, no `object` boxing of callbacks
- Prop types expose only `ResolveAsync()` — no sync `Resolve()`. PropsResolver (Phase 4) is async, so this is the only resolution path
- `InertiaPage.DefaultJsonOptions` includes `RuntimeTypeJsonConverter` for polymorphic `object?` serialization
- `InertiaFactory` is `internal sealed` — consumers interact via `IInertia` interface
- Initial page load writes minimal `<div id="app" data-page='...'>` HTML — full Razor view rendering deferred to Phase 7
- XML doc comments on all public API surface
