# Feature-Parity Audit: inertia-laravel v3.0.1 vs inertia-dotnet

**Date**: 2026-03-30
**Audited by**: 8 specialist sub-agents (code-architect, code-reviewer, csharp-developer, security-auditor)
**Verified by**: dotnet-core-expert, csharp-developer, 3x code-explorer agents
**Baseline**: 682 tests passing (610 Inertia.Tests + 72 Inertia.Testing.Tests)
**Current**: 708 tests passing (630 Inertia.Tests + 78 Inertia.Testing.Tests) — after Phase A

---

## Executive Summary

The audit identified **35 gaps** across 8 functional areas. Of these:
- **5 Critical** — block feature parity claim or cause runtime failures (**3 fixed** in Phase A)
- **17 Important** — significant missing features or API surface (**2 fixed** in Phase A)
- **13 Minor** — convenience gaps, documentation, or edge cases

2 original findings were removed as false positives during verification.

The most impactful remaining findings are:
1. **No default validation error sharing** — the foundational `errors` prop is entirely absent
2. **No `shareOnce` middleware-level mechanism** — cannot register middleware-level once-props
3. **Missing fluent API on InertiaResponse** — `With()`, `RootView()`, `Flash()` absent
4. **No URL resolver delegate** — custom URL resolution not possible
5. **No Vite hot reload detection for SSR** — SSR dispatch hits wrong URL during dev

---

## Critical Gaps (5)

### ~~CRIT-01: CSR HTML format incompatible with Inertia.js v3~~ FIXED (Phase A)
- **Resolution**: Updated `InertiaAppTagHelper`, `InertiaResponse` fallback, and `AssertableInertia.ExtractDataPageFromHtml()` to emit/parse v3 `<script data-page="{id}" type="application/json">{json}</script><div id="{id}"></div>` format. Added `Id` validation. v1/v2 fallback parser guarded to only match JSON values.

### CRIT-02: No default validation error sharing
- **PHP**: `Middleware.php:68-73` — always shares `errors` as `Inertia::always($this->resolveValidationErrors($request))`
- **C#**: Entirely absent. `SharedPropsProvider` is null by default. `X-Inertia-Error-Bag` header defined but never consumed.
- **Impact**: Every Inertia form example relies on `errors` prop. Without it, form validation is broken out of the box.
- **Fix**: Add `Func<HttpContext, IDictionary<string, object?>>? ValidationErrorProvider` delegate on `InertiaOptions`. Must be PRG-aware: read flashed validation errors from TempData on redirected GET (ModelState is empty after redirect).

### ~~CRIT-03: Exception handler status code set after response body~~ FIXED (Phase A)
- **Resolution**: Status code now set before `ExecuteAsync()`. `InertiaResponse.Execute()` guards against overwriting non-200 status. Added `HasStarted` guard (IMP-12), try/catch around delegate (IMP-11), and status code range validation (400-599 clamping).

### ~~CRIT-04: `IInertiaPropertyProvider` as `Render()` props silently broken~~ FIXED (Phase A)
- **Resolution**: Added `IInertiaPropertyProvider` switch case in both `InertiaFactory.Render()` and `InertiaExceptionResult.Render()` that wraps as `{ ["0"] = provider }` matching PHP's `[$props]`. Added input validation (`ArgumentException.ThrowIfNullOrWhiteSpace`) to `InertiaExceptionResult.Render()` and `.Redirect()`.

### CRIT-05: No `shareOnce` middleware-level mechanism
- **PHP**: `Middleware.php:80-124` — `shareOnce()` override point + wiring in `handle()`
- **C#**: No `SharedOncePropsProvider` delegate, no middleware-level once-prop wiring
- **Impact**: Cannot register middleware-level once-props (permissions, user settings). The `onceProps` response field cannot be populated via config.
- **Fix**: Add `SharedOncePropsProvider` delegate to `InertiaOptions`, wire in middleware.

---

## Important Gaps (17)

### IMP-01: Missing fluent API on InertiaResponse: `With()`, `RootView()`, `Flash()`
- **PHP**: `Response.php:124-178` — `with()`, `rootView()`, `flash()` for fluent chaining after `render()`
- **C#**: All props/rootView/flash must be set before `Render()` call. Only `WithViewData()` is fluent on `InertiaResponse`.
- **Fix**: Add `With(key, value)`, `RootView(string)`, `Flash(key, value)` to `InertiaResponse`. Since `WithViewData()` already mutates in place, follow the same pattern for consistency.

### IMP-02: No URL resolver delegate
- **PHP**: `ResponseFactory.php:150-163` — `resolveUrlUsing()` for custom URL resolution
- **C#**: URL always built from `PathBase + Path + QueryString`, no override point
- **Fix**: Add `Func<HttpContext, string>? UrlResolver` to `InertiaOptions`

### ~~IMP-03: Non-zero-parameter delegates silently pass through~~ FIXED (Phase A)
- **Resolution**: Added `InvalidOperationException` for all unresolvable delegates (params > 0 OR void return) in 3 PropsResolver locations: `ResolveCallableAsync`, `UnpackDotProps`, `EnsurePathIsTraversable`. Error messages include declaring type, method name, parameter count, and return type.

### IMP-04: No `ShareOnce()` convenience method
- **PHP**: `ResponseFactory.php:277-280` — `shareOnce(key, callable)`
- **C#**: Must manually `Share(key, Prop.Once<T>(callback))`
- **Fix**: Add `ShareOnce<T>(string key, Func<T> callback)` to `IInertia`

### IMP-05: No per-key `GetShared(key)` accessor
- **PHP**: `ResponseFactory.php:114-121` — `getShared(?string $key, $default)` with dot-notation lookup
- **C#**: Only internal `GetShared()` returning all shared props; not exposed on `IInertia`
- **Fix**: Add `object? GetShared(string key, object? defaultValue = null)` with dot-notation traversal

### IMP-06: `IsPrefetch()` missing `Sec-Purpose` header (Firefox)
- **PHP**: Checks both `Purpose: prefetch` and `Sec-Purpose: prefetch`
- **C#**: Only checks `Purpose: prefetch`
- **Fix**: Add `Sec-Purpose` header check (one-line fix)

### IMP-07: No static SSR path exclusion via config
- **PHP**: `Middleware.php:39` — `$withoutSsr = []` property on middleware
- **C#**: Must call `IInertia.WithoutSsr()` per-request in action code
- **Fix**: Add `string[]? SsrExcludePaths` to `InertiaOptions`, wire in middleware

### IMP-08: No Vite hot reload detection for SSR
- **PHP**: `HttpGateway.php:36-44` — detects `public/hot` file, uses hot URL + `/__inertia_ssr` for SSR dispatch
- **C#**: Always uses `{SsrUrl}/render` regardless of dev/prod mode
- **Impact**: During local development with Vite hot reload, SSR dispatch hits wrong URL. Graceful CSR fallback exists, so app still works without SSR.
- **Fix**: Add opt-in `Func<string?>? HotFileResolver` delegate on `InertiaOptions` for Vite dev mode detection.

### IMP-09: `DeriveStatusCode` only handles `BadHttpRequestException`
- **PHP**: Uses response status code (covers all HTTP status codes)
- **C#**: Only `BadHttpRequestException.StatusCode`, everything else → 500
- **Fix**: Also check `HttpRequestException.StatusCode` (.NET 5+). Consider adding `InertiaHttpException` for `abort()` equivalent.

### IMP-10: No public `HttpRequest.IsInertia()` extension method
- **PHP**: `$request->inertia()` macro registered in ServiceProvider
- **C#**: Internal `IsInertiaRequest` check only in middleware (private static)
- **Fix**: Add `public static bool IsInertia(this HttpRequest request)` extension method

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

### IMP-17: `SsrRenderFailed` event notification missing
- **PHP**: Dispatches rich `SsrRenderFailed` event through Laravel event system
- **C#**: Only logs via `ILogger`, no consumer-accessible notification
- **Fix**: Add event delegate or `ISsrEventHandler` interface

---

## Minor Gaps (13)

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
| MIN-11 | SSR | `BrowserApi` and `Stack` fields missing from `SsrException` |
| MIN-12 | Props | No recursion into indexed arrays in PropsResolver (documented intentional divergence) |
| MIN-13 | Testing | `AssertInertiaFlash` on redirect responses (TempData inspection) missing |

---

## Removed Findings (2 false positives)

| Original ID | Issue | Why Removed |
|-------------|-------|-------------|
| CRIT-06 | Async `AssertInertia` silently swallows | Reframed: async methods return `Task`, compiler prevents silent fire-and-forget. Replaced by IMP-13 (missing async overload). |
| MIN-14 | Extra `X-Inertia: true` on `ReloadRequest` | `X-Inertia: true` is correct — partial requests ARE Inertia requests and require this header for middleware recognition. |

---

## Not Applicable (Laravel-specific, ~20 items)

These are intentionally not ported:
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

---

## Recommended Fix Priority

### ~~Phase A: Protocol Correctness (blocks v3 compatibility)~~ COMPLETE
1. ~~**CRIT-01** — CSR HTML format (3 code paths + test parser)~~
2. ~~**CRIT-03 + IMP-11 + IMP-12** — Exception handler: status code ordering + try/catch + HasStarted guard~~
3. ~~**CRIT-04** — `IInertiaPropertyProvider` as `Render()` props~~
4. ~~**IMP-03** — Non-zero-parameter AND void delegates must throw~~

### Phase B: Core Feature Gaps (blocks feature parity claim)
5. **CRIT-02** — Validation error sharing pipeline (PRG-aware, TempData-based)
6. **CRIT-05 + IMP-04** — `shareOnce` middleware mechanism + convenience method
7. **IMP-01** — Fluent API on InertiaResponse (`With`, `RootView`, `Flash`)
8. **IMP-02** — URL resolver delegate
9. **IMP-06** — `Sec-Purpose` prefetch header (one-line fix)

### Phase C: Developer Experience
10. **IMP-10** — Public `IsInertia()` extension method
11. **IMP-05** — Per-key `GetShared(key)` accessor
12. **IMP-07** — Static SSR path exclusion config

### Phase D: SSR & Error Handling
13. **IMP-08** — Vite hot reload detection
14. **IMP-09** — Broader status code derivation
15. **IMP-17** — SSR failure event notification

### Phase E: Testing Package
16. **IMP-13** — Async `AssertInertia` overload
17. **IMP-14** — Wire `TestingEnsurePagesExist` into `AssertableInertia`
18. **IMP-15** — `Scope()`, `First()`, `Each()`, `Etc()`
19. **IMP-16** — Missing assertion methods

### Phase F: Minor Polish
20-32. All MIN-* items
