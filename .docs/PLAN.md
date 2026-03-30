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

### 11. Page Identifiers — `object?` for Now, Custom Discriminated Union Later

`IScrollMetadataProvider` uses `object?` for `PreviousPage`, `NextPage`, `CurrentPage`. PHP uses `int|string|null` (int for offset pagination, string for cursor pagination). A custom `PageIdentifier` discriminated union with implicit conversions from `int` and `string` would be type-safe but is deferred to avoid over-engineering Phase 1. `string?` alone won't work because it changes JSON serialization (`2` vs `"2"`). The OneOf library is excluded (same transitive dependency concern as ErrorOr). **TODO:** Revisit in Phase 2 when `ScrollProp` is implemented.

### 12. Phase 1 Implementation Decisions (finalized during coding)

- **No `required` keyword on InertiaOptions** — conflicts with Options pattern (parameterless constructor needed). Use `[Required]` annotation + `ValidateOnStart()` instead.
- **All InertiaOptions properties use `{ get; set; }`** (not `init`) — Options binding and `.Configure()` delegates need mutable setters.
- **Flat InertiaOptions** (not nested classes) — properties prefixed: `SsrEnabled`, `SsrUrl`, etc. Simpler than nested `SsrOptions` class.
- **Interfaces are query-only** — fluent mutation methods (`Merge()`, `Once()`, `Defer()`) go on concrete types in Phase 2. Interfaces expose only what PropsResolver needs to inspect.
- **Contexts are sealed classes** (not records) — HttpContext doesn't have meaningful value equality.
- **Flat namespace `Inertia.AspNetCore`** for all types — `Interfaces/`, `Contexts/` subfolders are physical organization only, not namespace.
- **SsrUrl defaults to `"http://127.0.0.1:13714"`** (no `/render`) — gateway appends `/render` at call time, matching PHP behavior in `HttpGateway.php`.

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
| `ResolvesCallables` trait | Typed fields on each prop (`Func<T>?`, `Func<Task<T>>?`) — no shared helper, no reflection |
| Laravel events (`SsrRenderFailed`) | Structured `ILogger` events (no custom event bus) |
| `Macroable` | Not applicable; use extension methods |

---

## PHP Traits → C# Mapping (Concrete)

| PHP Trait | C# Approach | Used By |
|---|---|---|
| `ResolvesCallables` | Typed fields per prop type (`Func<T>?`, `Func<Task<T>>?`, direct invocation) | All prop types |
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

## InertiaOptions (Implemented)

```csharp
public sealed class InertiaOptions
{
    public const string Section = "Inertia";

    [Required] public string RootView { get; set; } = "~/Views/App.cshtml";
    public bool EncryptHistory { get; set; }
    public bool ExposeSharedPropKeys { get; set; } = true;

    // SSR
    public bool SsrEnabled { get; set; } = true;
    [Url] public string SsrUrl { get; set; } = "http://127.0.0.1:13714";
    public bool SsrEnsureBundleExists { get; set; } = true;
    public string? SsrBundle { get; set; }
    public bool SsrThrowOnError { get; set; }

    // Pages
    public bool EnsurePagesExist { get; set; }
    public string[] PagePaths { get; set; } = [];
    public string[] PageExtensions { get; set; } = ["js", "jsx", "svelte", "ts", "tsx", "vue"];
    public bool TestingEnsurePagesExist { get; set; } = true;

    // Serialization
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    // Middleware delegates (defaults use TypedResults)
    public Func<HttpContext, string>? VersionProvider { get; set; }
    public Func<HttpContext, string>? RootViewProvider { get; set; }
    public Func<HttpContext, IServiceProvider, IDictionary<string, object?>>? SharedPropsProvider { get; set; }
    public Func<HttpContext, IResult>? OnVersionChange { get; set; }
    public Func<HttpContext, IResult>? OnEmptyResponse { get; set; }
}
```

Registration: `ValidateDataAnnotations()` + `ValidateOnStart()`. All properties `{ get; set; }` for Options pattern compatibility.

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
├── Properties/
│   └── AssemblyInfo.cs                  # [assembly: InternalsVisibleTo("Inertia.Tests")]
├── IInertia.cs                          # Main factory interface (slimmed down)
├── InertiaFactory.cs                    # IInertia implementation (scoped)
├── InertiaResponse.cs                   # IActionResult + IResult
├── InertiaLocationResult.cs             # Location redirect (IActionResult + IResult)
├── InertiaPage.cs                       # Page object DTO for JSON serialization
├── Prop.cs                              # Static factory: Prop.Defer(), Prop.Always(), etc.
├── PropsResolver.cs                     # Prop resolution engine (internal sealed)
├── # CallableResolver.cs removed — prop types use typed fields, no reflection
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

tests/Inertia.Tests/
├── InertiaHeaderNamesTests.cs
├── InertiaSessionKeysTests.cs
├── InertiaOptionsTests.cs
├── ComponentNotFoundExceptionTests.cs
├── Contexts/
│   ├── RenderContextTests.cs
│   └── PropertyContextTests.cs
└── Interfaces/
    └── InterfaceContractTests.cs

tests/Inertia.Testing.Tests/             # Tests for the testing package
```

**Removed from original:** `SsrRenderFailed.cs`, `SsrException.cs` (use structured `ILogger` events instead)

**Added:** `Prop.cs`, `CallableResolver.cs`, `InertiaLocationResult.cs`, `MergeablePropBase.cs`, `DeferInfo.cs`, `OnceInfo.cs`, `Properties/AssemblyInfo.cs`

---

## Critical Infrastructure Items

### Validation Error Pipeline

Laravel auto-redirects with errors in session. ASP.NET Core has no equivalent. We need:

1. **`InertiaValidationFilter`** (endpoint filter) — catches `ModelState` failures, stores errors in TempData, returns 302 redirect back
2. **In `InertiaMiddleware`** — reads validation errors from TempData and shares them as `Prop.Always(() => errors)` on the next request

### Polymorphic JSON Serialization

`Dictionary<string, object?>` props serialize as `{}` with System.Text.Json when using declared type. Solution: serialize each value with `JsonSerializer.Serialize(value, value.GetType(), options)` or use `JsonSerializer.SerializeToNode()`.

### InternalsVisibleTo ✅

`[assembly: InternalsVisibleTo("Inertia.Tests")]` added to `Properties/AssemblyInfo.cs`.

### SSR Gateway State Fix

Per-request path exclusions (`WithoutSsr()`) flow through scoped `SsrState`, not the singleton `HttpSsrGateway`. Global exclusions go in `InertiaOptions`.

### HttpSsrGateway Resilience

Use `IHttpClientFactory` with `AddStandardResilienceHandler()` per dotnet-recommended guidelines.

---

## Implementation Phases

### Phase 0: Project Setup ✅ (Already Done)

Branch `v3` exists, submodule at v3.0.1, directory structure created, `dotnet build` succeeds.

### Phase 1: Constants, Options, Interfaces, Contexts ✅

**Files:** `InertiaHeaderNames.cs`, `InertiaSessionKeys.cs`, `InertiaOptions.cs`, all 7 interfaces, `RenderContext.cs`, `PropertyContext.cs`, `ComponentNotFoundException.cs`, `Properties/AssemblyInfo.cs`

- 15 source files + 7 test files (81 tests, all passing)
- InertiaOptions: sealed, flat properties, `[Required]` on RootView, `[Url]` on SsrUrl, all `{ get; set; }`
- Interfaces: query-only (no mutation methods), flat `Inertia.AspNetCore` namespace
- Contexts: sealed classes with constructor validation, not records
- `[assembly: InternalsVisibleTo("Inertia.Tests")]` in AssemblyInfo

### Phase 2: Property Types + Trait Compositions ✅

**Files:** `Prop.cs`, `MergeablePropBase.cs`, `DeferInfo.cs`, `OnceInfo.cs`, all `Props/*.cs`

- 11 source files + 10 test files (181 new tests, cumulative 262)
- Generic prop types: `DeferProp<T>`, `OptionalProp<T>`, `AlwaysProp<T>`, `MergeProp<T>`, `OnceProp<T>`, `ScrollProp<T>`
- `Prop` static factory with overloads for `T value`, `Func<T>`, `Func<Task<T>>` + `DeepMerge` convenience
- Composition objects: `DeferInfo` (record struct), `OnceInfo` (sealed class), `MergeablePropBase` (abstract)
- Prop types use typed fields (`T? _value`, `Func<T>? _syncCallback`, `Func<Task<T>>? _asyncCallback`) — no reflection, no `object` boxing
- Only `ResolveAsync()` — no sync `Resolve()` (eliminates bug vector of calling sync on async prop)
- `CallableResolver` removed — each prop type handles its own resolution via typed invocation
- `ScrollMetadata` default `IScrollMetadataProvider` implementation
- Fluent APIs on concrete types with `new` shadowing for covariant returns
- IOnceable properties use explicit interface implementation

### Phase 3: Response Factory + Response ✅

**Files:** `IInertia.cs`, `InertiaFactory.cs`, `InertiaResponse.cs`, `InertiaLocationResult.cs`, `InertiaPage.cs`

- 5 source files + 5 test files (100 new tests, cumulative 362)
- `InertiaResponse : IActionResult, IResult` — JSON for Inertia requests, minimal HTML for initial loads (full Razor rendering deferred to Phase 7)
- `InertiaLocationResult : IActionResult, IResult` — 409 + X-Inertia-Location for Inertia requests, 302 for standard
- `InertiaFactory` (internal sealed, scoped) implements `IInertia` with per-request shared state, flash via TempData, anonymous object reflection
- `InertiaPage` DTO with conditional JSON serialization and `RuntimeTypeJsonConverter` for polymorphic `object?` values
- Prop resolution is minimal (shared+page merge only) — full resolution deferred to Phase 4 (PropsResolver)

### Phase 4: PropsResolver (Most Complex)

**File:** `PropsResolver.cs` (~400-500 lines)

- Port of 681-line PHP `PropsResolver.php`
- Receives `IServiceProvider` from `HttpContext.RequestServices`
- Key methods: `Resolve()`, `ResolveProps()` (recursive), `ResolveValue()`, `ShouldIncludeInPartialResponse()`, `ExcludeFromInitialResponse()`, `CollectMetadata()`, `BuildMetadata()`
- Handles: partial filtering (only/except), initial load exclusions, metadata collection (deferred/merge/scroll/once), dot-notation unpacking, nested prop type unwrapping
- Defensive assertion: no `Func<>` types leak past resolution (debug builds)
- Comprehensive unit tests (most critical test coverage)

### Phase 5: Middleware + Validation Pipeline ✅

**Files:** `Middleware/InertiaMiddleware.cs`, `Middleware/EncryptHistoryMiddleware.cs`

- 2 source files + 2 test files (34 new tests, cumulative 456)
- `InertiaMiddleware` (internal sealed, `IMiddleware`) — version check (short-circuit before `next()` on mismatch), `Vary: X-Inertia` header (via `OnStarting` + explicit set), 302→303 for PUT/PATCH/DELETE, empty response handling (redirect to Referer, fallback 204), fragment redirect (409 + `X-Inertia-Redirect`)
- `EncryptHistoryMiddleware` (internal sealed, `IMiddleware`) — calls `IInertia.EncryptHistory()` before downstream
- Configuration delegates from `InertiaOptions` (not virtual methods): `VersionProvider`, `SharedPropsProvider`, `RootViewProvider`, `OnVersionChange`, `OnEmptyResponse`
- Validation error sharing is consumer responsibility via `SharedPropsProvider` (no auto-resolution — ASP.NET Core lacks Laravel's `ViewErrorBag`)
- Flash data reflashing on redirects and version mismatches via TempData
- Version check happens BEFORE `next()` (eagerly evaluated, avoids response buffering)

### Phase 6: SSR ✅

- 7 source files + 6 test files (41 new tests, cumulative 506)
- `ISsrGateway` interface with `DispatchAsync` (returns `SsrResponse?`) and `IsHealthyAsync`
- `HttpSsrGateway` (internal sealed, Singleton) — `IHttpClientFactory` named client, POSTs page JSON to SSR server, parses `{head: string[], body: string}` response, structured `ILogger` warnings on failure, `SsrException` when `SsrThrowOnError` enabled
- `SsrResponse` sealed record (`Head`, `Body`)
- `SsrState` (internal sealed, Scoped) — per-request dispatch cache (gateway called once), path exclusions with exact/wildcard matching
- `SsrBundleDetector` (internal sealed, Singleton) — checks custom `SsrBundle` path then default `wwwroot/js/` paths, testable via internal `Func<string, bool>` constructor
- `SsrErrorType` enum + `SsrErrorTypeParser` — maps wire strings (`"browser-api"`, `"connection"`, etc.) to enum values
- `SsrException` — `Component`, `ErrorType`, `Hint`, `SourceLocation` properties, `Create()` factory
- `IInertia.WithoutSsr(params string[] paths)` — delegates to scoped `SsrState.ExcludePaths()`
- `InertiaFactory` adds nullable `SsrState?` constructor parameter (non-breaking for existing tests)
- `InertiaResponse.Execute()` stores page on `SsrState` for Tag Helper dispatch (Phase 7)
- Path exclusions on `SsrState` (scoped), not the Singleton gateway — per-request isolation
- No Vite hot module, no Laravel events — `ILogger` for error reporting

### Phase 7: DI Registration + Tag Helpers + View Rendering ✅

- 7 source files + 5 test files (31 new tests, cumulative 537)
- `AddInertia(Action<InertiaOptions>?)` — registers all services with `ValidateDataAnnotations() + ValidateOnStart()`, `BindConfiguration("Inertia")`, `TryAdd*` for idempotent registration, named HttpClient `"InertiaSSR"`
- `UseInertia()` / `UseInertiaEncryptHistory()` — middleware pipeline extensions
- `MapInertia(pattern, component, props)` — shorthand endpoint registration (like Laravel `Route::inertia()`), returns `RouteHandlerBuilder` for chaining
- `InertiaAppTagHelper` (`<inertia-app>`) — renders SSR body or CSR fallback div with `data-page` attribute. Resolves `SsrState` from `RequestServices` (internal type, not constructor-injectable). Checks path exclusions before SSR dispatch
- `InertiaHeadTagHelper` (`<inertia-head>`) — renders SSR head content or child content fallback (slot)
- `InertiaViewRenderer` (internal sealed, Scoped) — renders Razor views using `IRazorViewEngine`. Resolved from `RequestServices` in `InertiaResponse.Execute()` for initial page loads
- `InertiaResponse` modified — delegates to `InertiaViewRenderer.RenderAsync()` for initial page loads, falls back to inline HTML when DI not configured (unit test compatibility)
- Tag Helpers use `[ViewContextAttribute]` + `[HtmlAttributeNotBound]` for `HttpContext` access
- No csproj changes — all required types are in the `Microsoft.AspNetCore.App` framework reference

### Phase 8: Testing Package ✅

**Files:** `Inertia.Testing/*`

- `AssertableInertia` (public sealed) — fluent assertions on component, props (with dot-notation navigation), URL, version, flash, deferred props. Factory methods: `FromJson()` (public) and `FromResponseAsync()` (internal). Prop assertions: `Has()`, `Missing()`, `HasAll()`, `MissingAll()`, `Where()` (value comparison + callback). Flash assertions: `HasFlash()`, `MissingFlash()`. Reload methods: `ReloadAsync()`, `ReloadOnlyAsync()`, `ReloadExceptAsync()`, `LoadDeferredPropsAsync()`. Data access: `Prop()`, `Prop<T>()`, `ToPage()`, `GetComponent()`, `GetUrl()`, `GetVersion()`, `GetProps()`, `GetDeferredProps()`
- `InertiaTestExtensions` (public static) — `HttpResponseMessage.AssertInertia(Action<AssertableInertia>, HttpClient?)`, `.InertiaPage()`, `.InertiaProps(string?)` + `Task<HttpResponseMessage>` overloads
- `ReloadRequest` (internal sealed) — builds partial reload HTTP requests with correct Inertia headers (`X-Inertia`, `X-Inertia-Version`, `X-Inertia-Partial-Component`, `X-Inertia-Partial-Data`, `X-Inertia-Partial-Except`)
- Uses `System.Text.Json.JsonDocument` + `JsonElement.Clone()` for JSON parsing without `IDisposable` on `AssertableInertia`
- `Xunit.Assert` for test failure messages (compatible with xunit v3 test runners)
- Supports HTML response fallback via `data-page` attribute extraction

### Phase 9: Port PHP Tests ✅

- 4 new test files + additions to existing middleware tests (73 new tests, cumulative 610)
- `InertiaResponseIntegrationTests` — end-to-end response JSON testing: deferred/merge/once props metadata, URL handling (trailing slash, query params, PathBase), history flags, shared props merging
- `InertiaFactoryIntegrationTests` — factory→response→JSON pipeline: dot-notation unpacking, OnceProp sharing, shared props metadata tracking, flash data, IInertiaPropertyProvider expansion, component validation (EnsurePagesExist), prop type integration (defer, merge, scroll)
- `HistoryIntegrationTests` — EncryptHistory/ClearHistory/PreserveFragment effect on rendered JSON, global config vs per-request override
- `OncePropEdgeCaseTests` — Fresh() overrides except-once-props header, once props on partial requests
- Source changes: `InertiaResponse.GetUrl()` includes `PathBase`, `InertiaFactory.Render()` validates component existence when `EnsurePagesExist` is enabled

### Phase 10: Exception Handling ✅

- 3 new source files + 3 new test files (16 new tests, cumulative 682)
- `InertiaExceptionContext` (public sealed) — data carrier with `Exception`, `HttpContext`, `StatusCode`
- `InertiaExceptionResult` (public sealed) — builder pattern with static factory methods: `Render(component, props)`, `Redirect(url)`, fluent `.WithSharedData()`, `.RootView(rootView)`
- `InertiaExceptionHandler` (internal sealed, `IExceptionHandler`) — invoked by `app.UseExceptionHandler()`, delegates to `InertiaOptions.ExceptionHandler` callback, renders Inertia component with preserved status code, supports shared data inclusion and custom root view
- `InertiaOptions.ExceptionHandler` — `Func<InertiaExceptionContext, InertiaExceptionResult?>?` delegate, return null to fall through to default handler
- Registered as `IExceptionHandler` in `AddInertia()`

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
