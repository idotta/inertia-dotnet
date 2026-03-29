# Inertia.js .NET Adapter v3 — Implementation Plan

## Context

We're building a C# port of inertia-laravel v3.0.1 for ASP.NET Core 10 / C# 14. The repository has a clean slate: project scaffolding exists (4 `.csproj` files, directory structure, submodule at v3.0.1) but zero C# implementation.

This plan incorporates findings from four specialist reviews (C# architecture, API design, .NET Core infrastructure, code review) and aligns with the `dotnet-recommended` skill guidelines.

---

## Architecture: 2 Packages

| Package | Purpose | Target |
|---------|---------|--------|
| `Inertia.AspNetCore` | Main library: factory, response, props, middleware, SSR, Tag Helpers | `net10.0` / C# 14 |
| `Inertia.Testing` | Test assertions, reload simulation | `net10.0` / C# 14 |

---

## Key Design Decisions

### 1. ErrorOr — DO NOT USE

ErrorOr is for **application code**, not libraries. Adding it creates a transitive dependency every consumer inherits. The PHP source uses exceptions for programmer errors and nullable returns for optional results. We follow the same:

- `SsrResponse?` for SSR gateway (nullable = failure, logged via `ILogger`)
- `ComponentNotFoundException` for invalid component names
- `ArgumentException` / `InvalidOperationException` for misconfiguration
- No result pattern anywhere in the library

### 2. FluentAssertions — KEEP

The test project already has FluentAssertions 8.9.0. Version 8.x has clean Apache 2.0 license. Keep it for test readability. NSubstitute (already present) over Moq is also correct.

### 3. Prop Factory — Static `Prop` Class (not on IInertia)

Extract prop factory methods from `IInertia` to a static `Prop` class:

- Removes 7+ methods from the interface
- Enables prop creation without DI injection
- Follows .NET conventions (`TimeSpan.FromSeconds()`, `Task.FromResult()`)

```csharp
// Consumer code:
inertia.Render("Users/Index", new {
    users = Prop.Defer<List<User>>(() => GetUsersAsync()),
    auth = Prop.Always<AuthData>(GetAuth()),
    filters = Prop.Optional<Filters>(() => GetFilters())
});
```

### 4. Generic Prop Types

All callback-bearing props are generic: `DeferProp<T>`, `OptionalProp<T>`, `AlwaysProp<T>`, etc. This preserves type information, reduces boxing, and enables better IDE support. Non-generic base interfaces (`IDeferrable`, `IMergeable`, etc.) with generic concrete types.

### 5. InertiaResponse — Dual IActionResult + IResult

`InertiaResponse` implements **both** `IActionResult` and `IResult` so it works in MVC controllers AND minimal API endpoints without consumers doing anything special.

### 6. Middleware — IMiddleware + Configuration Delegates Only (no subclassing)

Override points (`Version`, `Share`, `RootView`, etc.) are `Func<>` delegates on `InertiaOptions`, not virtual methods on a base class. This is the idiomatic ASP.NET Core approach. `IMiddleware` is used (not conventional middleware) because the middleware needs scoped `IInertia`. No base class for subclassing — delegates cover all customization.

```csharp
builder.Services.AddInertia(options =>
{
    options.RootView = "~/Views/App.cshtml";
    options.VersionProvider = ctx => GetManifestHash();
    options.SharedPropsProvider = (ctx, sp) =>
        new Dictionary<string, object?>
        {
            ["auth"] = GetAuth(ctx),
            ["errors"] = GetErrors(ctx)
        };
});
```

### 7. Session Strategy — TempData + HttpContext.Items (not raw ISession)

| PHP Concept | .NET Mechanism |
|---|---|
| `session()->now()` (current request only) | `HttpContext.Items` |
| `session()->flash()` (survives one redirect) | `ITempDataDictionary` |
| Clear history / preserve fragment flags | `ITempDataDictionary` |
| Validation errors | Dedicated filter + TempData pipeline |
| Once props | Client-side (v3 uses `X-Inertia-Except-Once-Props` header) |

### 8. TypedResults Usage

`TypedResults` (per dotnet-recommended) applies to standard HTTP responses, not to our custom result types:

- `InertiaResponse` / `InertiaLocationResult` — custom `IResult + IActionResult`, writes directly to `HttpContext.Response`. Does NOT use TypedResults.
- Middleware delegate defaults (`OnVersionChange`, `OnEmptyResponse`) — use `TypedResults.Conflict()`, `TypedResults.NoContent()`, etc.
- `MapInertia()` endpoint — wraps `InertiaResponse` (custom result), not TypedResults.
- Middleware short-circuits (302→303 redirect, 409 version mismatch) — use TypedResults for standard responses.

### 9. JSON Serialization — System.Text.Json Only

- No Newtonsoft.Json dependency
- Shared `JsonSerializerOptions` instance with `CamelCase` naming policy
- Expose `InertiaOptions.JsonSerializerOptions` for consumer customization
- Polymorphic serialization: serialize each prop value with its runtime type, not declared `object` type (avoids `{}` serialization of anonymous types stored as `object`)

### 10. IOptions<T> (not IOptionsSnapshot<T>)

Inertia config is startup configuration that doesn't change at runtime. Use `IOptions<InertiaOptions>` with `ValidateDataAnnotations()` + `ValidateOnStart()`.

---

## PHP-to-C# Mapping

| PHP Concept | C# Equivalent |
|---|---|
| `ResponseFactory` (singleton via Facade) | `IInertia` interface + `InertiaFactory` (scoped) |
| `Response` (Responsable) | `InertiaResponse` implementing `IActionResult` + `IResult` |
| `PropsResolver` | `PropsResolver` (internal sealed, instantiated per response) |
| `Middleware` (abstract base) | `InertiaMiddleware` (`IMiddleware`) + delegates on `InertiaOptions` |
| PHP closures / `App::call()` | `Func<T>` / `Func<Task<T>>` / `Func<IServiceProvider, T>` |
| `Inertia` Facade | Direct DI injection of `IInertia` |
| `config/inertia.php` | `InertiaOptions` via `IOptions<InertiaOptions>` |
| Blade components | Razor Tag Helpers (`<inertia-app>`, `<inertia-head>`) |
| Session (`session()`) | `TempData` + `HttpContext.Items` (see decision #7) |
| `MergesProps` trait | `abstract class MergeablePropBase : IMergeable` (inheritance) |
| `DefersProps` trait | `record struct DeferInfo` (composition) |
| `ResolvesOnce` trait | `class OnceInfo` (composition, mutable for fluent API) |
| `ResolvesCallables` trait | `internal static class CallableResolver` (shared helper) |
| Laravel events (`SsrRenderFailed`) | Structured `ILogger` events (no custom event bus) |
| `Macroable` | Not applicable; use extension methods |

---

## PHP Traits → C# Mapping (Concrete)

| PHP Trait | C# Approach | Used By |
|---|---|---|
| `ResolvesCallables` | `internal static class CallableResolver` (shared helper) | All prop types, PropsResolver |
| `MergesProps` | `abstract class MergeablePropBase : IMergeable` (inheritance) | MergeProp, DeferProp, ScrollProp |
| `DefersProps` | `record struct DeferInfo` (composition) | DeferProp, ScrollProp |
| `ResolvesOnce` | `class OnceInfo` (composition, mutable for fluent API) | OptionalProp, OnceProp, MergeProp, DeferProp |

Class hierarchy:

```
AlwaysProp<T> (standalone)
OptionalProp<T> : IIgnoreFirstLoad, IOnceable  [contains OnceInfo]
OnceProp<T> : IOnceable  [contains OnceInfo]
MergeablePropBase (abstract) : IMergeable
  ├── MergeProp<T> : IOnceable  [contains OnceInfo]
  ├── DeferProp<T> : IDeferrable, IIgnoreFirstLoad, IOnceable  [contains DeferInfo, OnceInfo]
  └── ScrollProp<T> : IDeferrable  [contains DeferInfo]
```

---

## IInertia Interface

```csharp
public interface IInertia
{
    // Response production
    InertiaResponse Render(string component, object? props = null);
    InertiaResponse Render(string component, IDictionary<string, object?> props);
    InertiaLocationResult Location(string url);

    // Per-request shared state
    void Share(string key, object? value);
    void Share(IDictionary<string, object?> props);
    void Share(IInertiaPropertyProvider provider);

    // Flash data
    void Flash(string key, object? value);
    void Flash(IDictionary<string, object?> data);
    IDictionary<string, object?> GetFlashed();

    // Per-request flags
    void ClearHistory();
    void PreserveFragment();
    void EncryptHistory(bool encrypt = true);
}
```

**Removed from original:** `ShareOnce()` (use `Share("key", Prop.Once(...))`), `WithoutSsr()` (moved to per-request `SsrState`), all prop factory methods (moved to static `Prop` class).

**Internal-only on InertiaFactory:** `GetShared()`, `FlushShared()`, `SetVersion()`, `SetRootView()`.

---

## InertiaOptions (with Middleware Delegates)

```csharp
public class InertiaOptions
{
    public const string Section = "Inertia";

    [Required]
    public required string RootView { get; init; } = "~/Views/App.cshtml";

    public bool EncryptHistory { get; init; }
    public bool SsrEnabled { get; init; }

    [Url]
    public string SsrUrl { get; init; } = "http://127.0.0.1:13714/render";

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    // Middleware delegate overrides (instead of subclassing)
    // Defaults use TypedResults for standard HTTP responses
    public Func<HttpContext, string>? VersionProvider { get; set; }
    public Func<HttpContext, string>? RootViewProvider { get; set; }
    public Func<HttpContext, IServiceProvider, IDictionary<string, object?>>? SharedPropsProvider { get; set; }
    public Func<HttpContext, IResult>? OnVersionChange { get; set; }   // default: TypedResults.Conflict() + X-Inertia-Location
    public Func<HttpContext, IResult>? OnEmptyResponse { get; set; }   // default: TypedResults.NoContent()
}
```

Registration: `ValidateDataAnnotations()` + `ValidateOnStart()`.

---

## DI Lifetime Map

| Service | Lifetime | Why |
|---|---|---|
| `IInertia` / `InertiaFactory` | **Scoped** | Per-request shared props state |
| `InertiaMiddleware` | **Transient** | IMiddleware, resolved per request |
| `ISsrGateway` / `HttpSsrGateway` | **Singleton** | Stateless, uses IHttpClientFactory |
| `SsrState` | **Scoped** | Per-request SSR dispatch cache + path exclusions |
| `SsrBundleDetector` | **Singleton** | Stateless file checks |
| `IOptions<InertiaOptions>` | **Singleton** | Startup config, no runtime reload |

**PropsResolver** — NOT in DI. Created per-response inside `InertiaResponse.ExecuteResultAsync()`. Receives `IServiceProvider` from `HttpContext.RequestServices`.

**IHttpContextAccessor** — registered by `AddInertia()` (needed by `InertiaFactory`).

---

## File Structure

```
src/Inertia.AspNetCore/
├── Inertia.AspNetCore.csproj
├── IInertia.cs                          # Main factory interface (slimmed down)
├── InertiaFactory.cs                    # IInertia implementation (scoped)
├── InertiaResponse.cs                   # IActionResult + IResult
├── InertiaLocationResult.cs             # Location redirect (IActionResult + IResult)
├── InertiaPage.cs                       # Page object DTO for JSON serialization
├── Prop.cs                              # Static factory: Prop.Defer(), Prop.Always(), etc.
├── PropsResolver.cs                     # Prop resolution engine (internal sealed)
├── CallableResolver.cs                  # Static helper for Func<T> resolution (internal)
├── InertiaHeaderNames.cs                # Header constants
├── InertiaSessionKeys.cs                # Session/TempData key constants
├── InertiaOptions.cs                    # Configuration + middleware delegates
├── ComponentNotFoundException.cs
├── Props/
│   ├── AlwaysProp.cs                    # Standalone (no base class)
│   ├── DeferProp.cs                     # : MergeablePropBase, IDeferrable, IIgnoreFirstLoad, IOnceable
│   ├── MergeProp.cs                     # : MergeablePropBase, IOnceable
│   ├── MergeablePropBase.cs             # Abstract base (from MergesProps trait)
│   ├── OnceProp.cs                      # Standalone, IOnceable
│   ├── OptionalProp.cs                  # : IIgnoreFirstLoad, IOnceable
│   ├── ScrollProp.cs                    # : MergeablePropBase, IDeferrable
│   ├── ScrollMetadata.cs
│   ├── DeferInfo.cs                     # Composition value object (from DefersProps trait)
│   └── OnceInfo.cs                      # Composition value object (from ResolvesOnce trait)
├── Interfaces/
│   ├── IIgnoreFirstLoad.cs
│   ├── IDeferrable.cs
│   ├── IMergeable.cs
│   ├── IOnceable.cs
│   ├── IInertiaPropertyProvider.cs      # Renamed from IProvidesInertiaProperties
│   ├── IInertiaPropertyValueProvider.cs # Renamed from IProvidesInertiaProperty
│   └── IScrollMetadataProvider.cs       # Renamed from IProvidesScrollMetadata
├── Contexts/
│   ├── RenderContext.cs
│   └── PropertyContext.cs
├── Middleware/
│   ├── InertiaMiddleware.cs             # IMiddleware implementation
│   └── EncryptHistoryMiddleware.cs
├── Ssr/
│   ├── ISsrGateway.cs
│   ├── HttpSsrGateway.cs               # Singleton, uses IHttpClientFactory + resilience
│   ├── SsrResponse.cs
│   ├── SsrState.cs                      # Scoped (per-request SSR state + path exclusions)
│   ├── SsrBundleDetector.cs             # Singleton
│   └── SsrErrorType.cs                  # Enum
├── TagHelpers/
│   ├── InertiaAppTagHelper.cs
│   └── InertiaHeadTagHelper.cs
├── Extensions/
│   ├── InertiaServiceCollectionExtensions.cs  # AddInertia()
│   ├── InertiaApplicationBuilderExtensions.cs # UseInertia()
│   └── InertiaEndpointExtensions.cs           # MapInertia() -> RouteHandlerBuilder
└── ExceptionHandling/
    └── ExceptionResponse.cs             # Phase 10 (optional/later)

src/Inertia.Testing/
├── Inertia.Testing.csproj
├── AssertableInertia.cs
├── InertiaTestExtensions.cs
└── ReloadRequest.cs

tests/Inertia.Tests/                     # Unit + integration tests
tests/Inertia.Testing.Tests/             # Tests for the testing package
```

**Removed from original:** `SsrRenderFailed.cs`, `SsrException.cs` (use structured `ILogger` events instead)

**Added:** `Prop.cs`, `CallableResolver.cs`, `InertiaLocationResult.cs`, `MergeablePropBase.cs`, `DeferInfo.cs`, `OnceInfo.cs`

---

## Critical Infrastructure Items

### Validation Error Pipeline

Laravel auto-redirects with errors in session. ASP.NET Core has no equivalent. We need:

1. **`InertiaValidationFilter`** (endpoint filter) — catches `ModelState` failures, stores errors in TempData, returns 302 redirect back
2. **In `InertiaMiddleware`** — reads validation errors from TempData and shares them as `Prop.Always(() => errors)` on the next request

### Polymorphic JSON Serialization

`Dictionary<string, object?>` props serialize as `{}` with System.Text.Json when using declared type. Solution: serialize each value with `JsonSerializer.Serialize(value, value.GetType(), options)` or use `JsonSerializer.SerializeToNode()`.

### InternalsVisibleTo

Add `[assembly: InternalsVisibleTo("Inertia.Tests")]` to `Inertia.AspNetCore` so `PropsResolver` (internal) can be unit tested.

### SSR Gateway State Fix

Per-request path exclusions (`WithoutSsr()`) flow through scoped `SsrState`, not the singleton `HttpSsrGateway`. Global exclusions go in `InertiaOptions`.

### HttpSsrGateway Resilience

Use `IHttpClientFactory` with `AddStandardResilienceHandler()` per dotnet-recommended guidelines.

---

## Implementation Phases

### Phase 0: Project Setup ✅ (Already Done)

Branch `v3` exists, submodule at v3.0.1, directory structure created, `dotnet build` succeeds.

### Phase 1: Constants, Options, Interfaces, Contexts

**Files:** `InertiaHeaderNames.cs`, `InertiaSessionKeys.cs`, `InertiaOptions.cs`, all interfaces, `RenderContext.cs`, `PropertyContext.cs`, `ComponentNotFoundException.cs`

- Direct translations from PHP
- InertiaOptions with `ValidateDataAnnotations()` + `ValidateOnStart()`
- Add `InternalsVisibleTo` to AssemblyInfo

### Phase 2: Property Types + Trait Compositions

**Files:** `Prop.cs`, `CallableResolver.cs`, `MergeablePropBase.cs`, `DeferInfo.cs`, `OnceInfo.cs`, all `Props/*.cs`

- Generic prop types: `DeferProp<T>`, `OptionalProp<T>`, `AlwaysProp<T>`, etc.
- `Prop` static factory with overloads for `Func<T>`, `Func<Task<T>>`
- Non-generic base interfaces (`IDeferrable`, `IMergeable`, etc.) with generic concrete types
- Unit tests for each prop type

### Phase 3: Response Factory + Response

**Files:** `IInertia.cs`, `InertiaFactory.cs`, `InertiaResponse.cs`, `InertiaLocationResult.cs`, `InertiaPage.cs`

- `InertiaResponse : IActionResult, IResult`
- `InertiaLocationResult : IActionResult, IResult`
- `Render()` with `object?` props (handles anonymous types + dictionaries)
- `WithViewData()` fluent method on `InertiaResponse`
- JSON serialization with runtime-type polymorphism

### Phase 4: PropsResolver (Most Complex)

**File:** `PropsResolver.cs` (~400-500 lines)

- Port of 681-line PHP `PropsResolver.php`
- Receives `IServiceProvider` from `HttpContext.RequestServices`
- Key methods: `Resolve()`, `ResolveProps()` (recursive), `ResolveValue()`, `ShouldIncludeInPartialResponse()`, `ExcludeFromInitialResponse()`, `CollectMetadata()`, `BuildMetadata()`
- Handles: partial filtering (only/except), initial load exclusions, metadata collection (deferred/merge/scroll/once), dot-notation unpacking, nested prop type unwrapping
- Defensive assertion: no `Func<>` types leak past resolution (debug builds)
- Comprehensive unit tests (most critical test coverage)

### Phase 5: Middleware + Validation Pipeline

**Files:** `InertiaMiddleware.cs`, `EncryptHistoryMiddleware.cs`

- `IMiddleware` implementation
- Configuration delegates from `InertiaOptions` (not virtual methods)
- Version checking, 302→303, `Vary: X-Inertia`, fragment redirect
- Validation error sharing via TempData
- Integration tests via WebApplicationFactory

### Phase 6: SSR

**Files:** `ISsrGateway.cs`, `HttpSsrGateway.cs`, `SsrResponse.cs`, `SsrState.cs`, `SsrBundleDetector.cs`, `SsrErrorType.cs`

- `IHttpClientFactory` with `AddStandardResilienceHandler()`
- `SsrState` (scoped) holds per-request path exclusions + dispatch cache
- Gateway returns `SsrResponse?` (nullable on failure, logged via `ILogger`)

### Phase 7: DI Registration + Tag Helpers

**Files:** Extensions, Tag Helpers

- `AddInertia(Action<InertiaOptions>?)` with `ValidateOnStart()`
- `UseInertia()` — explicit pipeline registration (not auto-registered)
- `MapInertia()` → returns `RouteHandlerBuilder`
- `<inertia-app>` / `<inertia-head>` Tag Helpers (not Razor Components)
- Registers `IHttpContextAccessor`

### Phase 8: Testing Package

**Files:** `Inertia.Testing/*`

- `AssertableInertia` — fluent assertions on component, props, URL, deferred, merge
- `InertiaTestExtensions` — `HttpResponseMessage.AssertInertia(Action<AssertableInertia>)`
- `ReloadRequest` — builds partial reload requests with correct headers

### Phase 9: Tests

Port key tests from inertia-laravel, focusing on:

- PropsResolver (1066-line PHP test suite → comprehensive coverage)
- Middleware behavior (integration tests)
- SSR fallback
- End-to-end rendering

### Phase 10: Exception Handling (Optional/Later)

- `ExceptionResponse.cs` adapted for ASP.NET Core `IExceptionHandler`

---

## Verification

After each phase:

1. `dotnet format` — no formatting issues
2. `dotnet build` — zero warnings, zero errors
3. `dotnet test` — all tests pass

Key behavioral tests per phase:

- **Phase 2:** Property types resolve correctly, metadata is collected
- **Phase 3-4:** `InertiaResponse` returns correct JSON for Inertia requests and renders view for initial loads; prop resolution handles partials, deferred, once, merge
- **Phase 5:** Middleware detects Inertia requests, handles version mismatch (409), converts 302→303, shares validation errors
- **Phase 6:** SSR gateway dispatches and falls back to CSR on failure
- **Phase 7:** `AddInertia()` registers all services correctly
- **Phase 8:** `AssertableInertia` can assert on component, props, URL, deferred props

End-to-end: a minimal test app in `Inertia.Tests` via `WebApplicationFactory` that renders an Inertia response, handles partial reloads, and demonstrates shared props.

---

## Critical Reference Files (inertia-laravel v3.0.1)

| PHP File | Lines | C# Target | Complexity |
|----------|-------|-----------|-----------|
| `src/PropsResolver.php` | 681 | `PropsResolver.cs` | **Highest** |
| `src/ResponseFactory.php` | 436 | `InertiaFactory.cs` | High |
| `src/Response.php` | 284 | `InertiaResponse.cs` | Medium |
| `src/Middleware.php` | 248 | `InertiaMiddleware.cs` | Medium |
| `src/MergesProps.php` | 163 | `MergeablePropBase.cs` | Medium |
| `src/ResolvesOnce.php` | 117 | `OnceInfo.cs` | Medium |
| `src/ScrollProp.php` | 143 | `ScrollProp.cs` | Medium |
| `src/ExceptionResponse.php` | 165 | `ExceptionResponse.cs` | Medium |
| `src/Ssr/HttpGateway.php` | 175 | `HttpSsrGateway.cs` | Medium |
| `src/Testing/AssertableInertia.php` | 304 | `AssertableInertia.cs` | Medium |
