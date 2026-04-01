# Feature-Parity Audit: Inertia.js v3 Protocol + inertia-laravel v3.0.1 vs inertia-dotnet

**Date**: 2026-03-31
**Audited by**: 8 specialist sub-agents (code-architect, code-reviewer, csharp-developer, security-auditor)
**Verified by**: dotnet-core-expert, csharp-developer, 3x code-explorer agents
**Cross-referenced against**: Inertia.js v3 protocol documentation (inertiajs.com/docs/v3/)
**Baseline**: 682 tests passing (610 Inertia.Tests + 72 Inertia.Testing.Tests)
**Current**: 788 tests passing (710 Inertia.Tests + 78 Inertia.Testing.Tests) — after Phase D

---

## Executive Summary

The audit identified **38 gaps** across 8 functional areas, plus **1 protocol extension**. Of these:
- **5 Critical** — block feature parity claim or cause runtime failures (**5 fixed** in Phases A+B)
- **19 Important** — significant missing features or API surface (**12 fixed** in Phases A+B+C+D)
- **14 Minor** — convenience gaps, documentation, or edge cases (**1 fixed** in Phase D)

2 original findings were removed as false positives during verification.
1 protocol-level feature (Precognition) identified — beyond adapter scope, tracked separately.

The most impactful remaining findings are:
1. **Testing `Scope()` and nested assertion methods missing** — no scoped assertions
2. **Testing `WhereNot`, `WhereType`, `WhereContains`, `HasAny` missing** — incomplete assertion methods
3. **Testing async `AssertInertia` overload missing** — no `Func<AssertableInertia, Task>` overload
4. **Component file existence check missing from `AssertableInertia`** — `shouldExist` not wired
5. **`FlushShared()` and `GetVersion()` not exposed on `IInertia`** — internal methods need public exposure

---

## Critical Gaps (5)

### ~~CRIT-01: CSR HTML format incompatible with Inertia.js v3~~ FIXED (Phase A)
- **Resolution**: Updated `InertiaAppTagHelper`, `InertiaResponse` fallback, and `AssertableInertia.ExtractDataPageFromHtml()` to emit/parse v3 `<script data-page="{id}" type="application/json">{json}</script><div id="{id}"></div>` format. Added `Id` validation. v1/v2 fallback parser guarded to only match JSON values.

### ~~CRIT-02: No default validation error sharing~~ FIXED (Phase B)
- **Resolution**: Added `Func<HttpContext, string?, IDictionary<string, object?>>? ValidationErrorProvider` delegate to `InertiaOptions`. The `string?` parameter receives the `X-Inertia-Error-Bag` header value. Middleware wraps the result in `Prop.Always<T>()` for lazy evaluation and partial-reload inclusion under the `"errors"` key.

### ~~CRIT-03: Exception handler status code set after response body~~ FIXED (Phase A)
- **Resolution**: Status code now set before `ExecuteAsync()`. `InertiaResponse.Execute()` guards against overwriting non-200 status. Added `HasStarted` guard (IMP-12), try/catch around delegate (IMP-11), and status code range validation (400-599 clamping).

### ~~CRIT-04: `IInertiaPropertyProvider` as `Render()` props silently broken~~ FIXED (Phase A)
- **Resolution**: Added `IInertiaPropertyProvider` switch case in both `InertiaFactory.Render()` and `InertiaExceptionResult.Render()` that wraps as `{ ["0"] = provider }` matching PHP's `[$props]`. Added input validation (`ArgumentException.ThrowIfNullOrWhiteSpace`) to `InertiaExceptionResult.Render()` and `.Redirect()`.

### ~~CRIT-05: No `shareOnce` middleware-level mechanism~~ FIXED (Phase B)
- **Resolution**: Added `Func<HttpContext, IServiceProvider, IDictionary<string, object?>>? SharedOncePropsProvider` delegate to `InertiaOptions`. Middleware iterates the dict: values implementing `IOnceable` are shared directly; other values/delegates are wrapped in `OnceProp<object?>`. Also added `ShareOnce<T>(string, Func<T>)` and async overload to `IInertia` (IMP-04).

---

## Important Gaps (19)

### ~~IMP-01: Missing fluent API on InertiaResponse: `With()`, `WithRootView()`, `Flash()`~~ FIXED (Phase B)
- **Resolution**: Added `With(string, object?)`, `With(IDictionary)`, `With(IInertiaPropertyProvider)`, `WithRootView(string)`, `Flash(string, object?)`, `Flash(IDictionary)` to `InertiaResponse`. All return `this` for fluent chaining. `Flash()` delegates via `Action<string, object?>` injected from `InertiaFactory`. Method named `WithRootView` (not `RootView`) to avoid C# name collision with internal property.

### ~~IMP-02: No URL resolver delegate~~ FIXED (Phase B)
- **Resolution**: Added `Func<HttpContext, string>? UrlResolver` to `InertiaOptions`. Passed through `InertiaFactory` to `InertiaResponse`. When set, overrides default `PathBase + Path + QueryString` URL construction.

### ~~IMP-03: Non-zero-parameter delegates silently pass through~~ FIXED (Phase A)
- **Resolution**: Added `InvalidOperationException` for all unresolvable delegates (params > 0 OR void return) in 3 PropsResolver locations: `ResolveCallableAsync`, `UnpackDotProps`, `EnsurePathIsTraversable`. Error messages include declaring type, method name, parameter count, and return type.

### ~~IMP-04: No `ShareOnce()` convenience method~~ FIXED (Phase B, with CRIT-05)
- **Resolution**: Added `ShareOnce<T>(string, Func<T>)` and `ShareOnce<T>(string, Func<Task<T>>)` to `IInertia` and `InertiaFactory`. Creates `OnceProp<T>` and shares it.

### ~~IMP-05: No per-key `GetShared(key)` accessor~~ FIXED (Phase C)
- **Resolution**: Added `object? GetShared(string key, object? defaultValue = null)` to `IInertia` and `InertiaFactory`. Supports dot-notation traversal of nested `IDictionary<string, object?>` values (e.g., `GetShared("user.profile.name")`). Returns `defaultValue` when key or intermediate path is missing.

### ~~IMP-06: `IsPrefetch()` missing `Sec-Purpose` header (Firefox)~~ FIXED (Phase B)
- **Resolution**: Added `|| req.Headers["Sec-Purpose"].FirstOrDefault() == "prefetch"` to `IsPrefetch()` in middleware.

### ~~IMP-07: No static SSR path exclusion via config~~ FIXED (Phase C)
- **Resolution**: Added `string[]? SsrExcludePaths` to `InertiaOptions`. Middleware applies static exclusions to scoped `SsrState` via `GetService<SsrState>()` (null-safe). Supports exact match and trailing wildcard (e.g., `"/api/*"`). Additive with per-request `WithoutSsr()`.

### ~~IMP-08: No Vite hot reload detection for SSR~~ FIXED (Phase D)
- **Resolution**: Added `Func<string?>? HotFileResolver` delegate to `InertiaOptions`. When it returns non-null, `HttpSsrGateway.DispatchAsync` uses `{hotUrl}/__inertia_ssr` instead of `{SsrUrl}/render` and skips bundle existence checks (matching PHP behavior). `IsHealthyAsync` always uses the production URL.

### ~~IMP-09: `DeriveStatusCode` only handles `BadHttpRequestException`~~ FIXED (Phase D)
- **Resolution**: `DeriveStatusCode` now uses pattern-matching switch: `BadHttpRequestException` → `InertiaHttpException` → `HttpRequestException.StatusCode` → 500 fallback. Added `InertiaHttpException` sealed class as C# equivalent of PHP's `abort()` (e.g., `throw new InertiaHttpException(403)`).

### ~~IMP-10: No public `HttpRequest.IsInertia()` extension method~~ FIXED (Phase C)
- **Resolution**: Added `InertiaHttpRequestExtensions.IsInertia(this HttpRequest)` public extension method. Middleware's `IsInertiaRequest` refactored to delegate to it.

### ~~IMP-11: Exception handler delegate invoked without try/catch~~ FIXED (Phase A, with CRIT-03)
- **Resolution**: Delegate invocation wrapped in try/catch; returns `false` on exception to let default handler take over.

### ~~IMP-12: No `HasStarted` check in exception handler~~ FIXED (Phase A, with CRIT-03)
- **Resolution**: Added `HasStarted` guard at start of `TryHandleAsync`; returns `false` if response already started.

### IMP-13: Testing `AssertInertia` missing async overload
- **C#**: `InertiaTestExtensions.AssertInertia(Action<AssertableInertia>)` takes sync `Action<>` but `ReloadAsync`, `LoadDeferredPropsAsync` return `Task`. No `Func<AssertableInertia, Task>` overload exists.
- **Impact**: Callers wanting async assertions must block synchronously or write `async void` lambdas. Not a silent correctness bug (compiler emits CS4014 warning), but a usability gap.
- **Fix**: Add `AssertInertia(Func<AssertableInertia, Task> asyncCallback)` overload.

### IMP-14: Component file existence check missing from AssertableInertia
- **PHP**: `AssertableInertia.php:102-115` — `component(value, shouldExist: true)` validates file on disk
- **C#**: `Component(string expected)` — asserts name only, no `shouldExist` parameter
- **Note**: `InertiaOptions.TestingEnsurePagesExist` already exists (default `true`) with `PagePaths`/`PageExtensions`, but is never wired into `AssertableInertia`.
- **Fix**: Add `bool? shouldExist = null` parameter; wire `InertiaOptions` into `AssertableInertia` via delegate or `IServiceProvider`.

### IMP-15: Testing `Scope()` and nested assertion methods missing
- **PHP**: `scope()`, `first()`, `each()`, `etc()` from AssertableJson
- **C#**: None of these exist — no nested scoping, no interaction checking
- **Fix**: Implement `Scope()`, `First()`, `Each()`, `Etc()` with interaction tracking

### IMP-16: Testing `WhereNot`, `WhereType`, `WhereContains`, `HasAny` missing
- **PHP**: Full set of AssertableJson assertion methods
- **C#**: Only `Has`, `HasAll`, `Missing`, `MissingAll`, `Where` exist
- **Fix**: Add missing assertion methods

### ~~IMP-17: `SsrRenderFailed` event notification missing~~ FIXED (Phase D)
- **Resolution**: Added `Action<SsrRenderFailedContext>? OnSsrRenderFailed` delegate to `InertiaOptions`. `SsrRenderFailedContext` sealed class carries all 7 PHP fields (Page, Error, ErrorType, Hint, BrowserApi, Stack, SourceLocation) plus `Exception?`. Callback invoked before `ILogger` warning and optional throw. Callback exceptions are caught and logged at `LogError` level to protect CSR fallback.

### IMP-18: `FlushShared()` not exposed on `IInertia` interface
- **PHP**: `Inertia::flushShared()` is public on the `ResponseFactory`
- **C#**: `InertiaFactory.FlushShared()` exists (`InertiaFactory.cs:186`) but is `internal`
- **Impact**: Consumers cannot reset shared state in test setup or between scopes
- **Fix**: Add `void FlushShared()` to `IInertia` interface; change visibility from `internal` to `public` on the implementation

### IMP-19: `GetVersion()` not exposed on `IInertia` interface
- **PHP**: `Inertia::getVersion()` is public on the `ResponseFactory`
- **C#**: `InertiaFactory.GetVersion()` exists (`InertiaFactory.cs:196`) but is `internal`
- **Impact**: Consumers cannot inspect the resolved asset version for debugging or conditional logic
- **Fix**: Add `string GetVersion()` to `IInertia` interface; change visibility from `internal` to `public`

---

## Minor Gaps (14)

| ID | Area | Issue |
|----|------|-------|
| MIN-01 | Core | `ClearHistory()`/`PreserveFragment()` don't persist across redirects via TempData |
| MIN-02 | Core | `Share()` doesn't accept `object` for property flattening |
| MIN-03 | Props | ScrollProp missing 2 `new` fluent overloads for `Append/Prepend(IDictionary)` |
| MIN-04 | Props | ScrollProp missing constructor/factory overloads for metadata factory callback |
| MIN-05 | Props | `Until(DateTimeOffset)` overload missing on OnceInfo |
| MIN-06 | Props | `Func<IServiceProvider, T>` overloads documented in plan but never implemented |
| MIN-07 | Middleware | Version mismatch only reflashes Inertia flash, not all TempData |
| MIN-08 | Middleware | Empty response redirect uses `Referer` only (not session-stored URL) |
| MIN-09 | SSR | Missing `bootstrap/ssr/` default bundle detection paths |
| MIN-10 | SSR | Full-URL pattern matching absent from SSR path exclusion |
| ~~MIN-11~~ | SSR | ~~`BrowserApi` and `Stack` fields missing from `SsrException`~~ FIXED (Phase D): Added `BrowserApi` and `Stack` properties to `SsrException`. `ParseError` now extracts `browserApi` and `stack` from SSR error JSON. |
| MIN-12 | Props | No recursion into indexed arrays in PropsResolver (documented intentional divergence) |
| MIN-13 | Testing | `AssertInertiaFlash` on redirect responses (TempData inspection) missing |
| MIN-15 | Core | No `Back()` convenience method on `IInertia` — PHP has `Inertia::back($status, $headers, $fallback)` for redirect to previous URL; consumers can use `Request.Headers.Referer` directly |

---

## Protocol-Level Features (Beyond inertia-laravel Scope)

Features documented in the Inertia.js v3 protocol specification that are NOT implemented by inertia-laravel but could be provided by our adapter or ecosystem packages.

### PROTO-01: Precognition Support
- **Protocol**: Request headers `Precognition: true`, `Precognition-Validate-Only: field1,field2`
- **Response**: Headers `Precognition: true`, `Precognition-Success: true`, `Vary: Precognition`; status 204 on success, 422 with validation errors on failure
- **Behavior**: Validate form fields without processing the submission — enables real-time field-by-field validation as users fill out forms
- **Laravel**: Handled by separate `laravel/precognition` package, NOT part of inertia-laravel
- **Recommendation**: Optional middleware. Could be:
  - (a) Separate `PrecognitionMiddleware` in `Inertia.AspNetCore` (simplest for users)
  - (b) Separate NuGet package `Inertia.AspNetCore.Precognition` (cleaner separation)
- **Priority**: Future enhancement — no server adapter bundles this; not blocking parity claim
- **Decision**: Track as post-v3 enhancement

---

## Removed Findings (2 false positives)

| Original ID | Issue | Why Removed |
|-------------|-------|-------------|
| CRIT-06 | Async `AssertInertia` silently swallows | Reframed: async methods return `Task`, compiler prevents silent fire-and-forget. Replaced by IMP-13 (missing async overload). |
| MIN-14 | Extra `X-Inertia: true` on `ReloadRequest` | `X-Inertia: true` is correct — partial requests ARE Inertia requests and require this header for middleware recognition. |

---

## Not Applicable

### Laravel-Specific (~11 items)

These are intentionally not ported — they are Laravel ecosystem idioms with C# equivalents:
- Facade pattern (`Inertia::render()`) → DI + `IInertia`
- Blade directives → Tag Helpers
- Route/Request/Redirect macros → Extension methods / DI
- Artisan commands → No CLI planned
- `Macroable` trait → Delegate-based extension
- `Arrayable` / `Responsable` support → C# has no equivalents
- `App::call()` DI in callables → Zero-parameter `Func<T>` design
- Mix manifest versioning → Manual `VersionProvider`
- Config publishing → `appsettings.json` + Options pattern
- `GuzzleHttp\Promise` support → `Task<T>` natively
- No enum component name overload — C# developers call `.ToString()` (PHP idiom, not needed)

### Framework-Level (ASP.NET Core handles)

These are mentioned in the Inertia.js protocol docs but handled by ASP.NET Core framework middleware, not the Inertia adapter:
- CSRF Protection → `AntiforgeryMiddleware` / `[ValidateAntiForgeryToken]`
- Method Spoofing (`_method`) → `HttpMethodOverrideMiddleware`
- Session Management → ASP.NET Core session middleware
- Authentication → ASP.NET Core Identity / external auth providers
- File Upload handling → Model binding handles `multipart/form-data` natively
- Cookie encryption → ASP.NET Core Data Protection

### Separate Package Concerns

Features that exist in the Inertia ecosystem as standalone packages, not part of any server adapter:
- Precognition → See PROTO-01 above

---

## Recommended Fix Priority

### ~~Phase A: Protocol Correctness (blocks v3 compatibility)~~ COMPLETE
1. ~~**CRIT-01** — CSR HTML format (3 code paths + test parser)~~
2. ~~**CRIT-03 + IMP-11 + IMP-12** — Exception handler: status code ordering + try/catch + HasStarted guard~~
3. ~~**CRIT-04** — `IInertiaPropertyProvider` as `Render()` props~~
4. ~~**IMP-03** — Non-zero-parameter AND void delegates must throw~~

### ~~Phase B: Core Feature Gaps (blocks feature parity claim)~~ COMPLETE
5. ~~**CRIT-02** — Validation error sharing pipeline~~
6. ~~**CRIT-05 + IMP-04** — `shareOnce` middleware mechanism + convenience method~~
7. ~~**IMP-01** — Fluent API on InertiaResponse (`With`, `WithRootView`, `Flash`)~~
8. ~~**IMP-02** — URL resolver delegate~~
9. ~~**IMP-06** — `Sec-Purpose` prefetch header~~

### ~~Phase C: Developer Experience~~ COMPLETE
10. ~~**IMP-10** — Public `IsInertia()` extension method~~
11. ~~**IMP-05** — Per-key `GetShared(key)` accessor~~
12. ~~**IMP-07** — Static SSR path exclusion config~~

### ~~Phase D: SSR & Error Handling~~ COMPLETE
13. ~~**IMP-08** — Vite hot reload detection~~
14. ~~**IMP-09** — Broader status code derivation~~
15. ~~**IMP-17** — SSR failure event notification~~

### Phase E: Testing Package
16. **IMP-13** — Async `AssertInertia` overload
17. **IMP-14** — Wire `TestingEnsurePagesExist` into `AssertableInertia`
18. **IMP-15** — `Scope()`, `First()`, `Each()`, `Etc()`
19. **IMP-16** — Missing assertion methods

### Phase F: API Surface & Minor Polish
20. **IMP-18** — Expose `FlushShared()` on `IInertia` (trivial: interface addition + visibility change)
21. **IMP-19** — Expose `GetVersion()` on `IInertia` (trivial: interface addition + visibility change)
22. **MIN-15** — `Back()` convenience method
23-34. All remaining MIN-* items
