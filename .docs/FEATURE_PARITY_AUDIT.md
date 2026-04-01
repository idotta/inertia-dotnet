# Feature-Parity Audit: Inertia.js v3 Protocol + inertia-laravel v3.0.1 vs inertia-dotnet

**Date**: 2026-03-31
**Audited by**: 8 specialist sub-agents (code-architect, code-reviewer, csharp-developer, security-auditor)
**Verified by**: dotnet-core-expert, csharp-developer, 3x code-explorer agents
**Cross-referenced against**: Inertia.js v3 protocol documentation (inertiajs.com/docs/v3/)
**Baseline**: 682 tests passing (610 Inertia.Tests + 72 Inertia.Testing.Tests)
**Current**: 919 tests passing (784 Inertia.Tests + 135 Inertia.Testing.Tests) — after Final Verification fixes

---

## Executive Summary

The audit identified **38 gaps** across 8 functional areas, plus **1 protocol extension**. Of these:
- **5 Critical** — block feature parity claim or cause runtime failures (**5 fixed** in Phases A+B)
- **19 Important** — significant missing features or API surface (**19 fixed** in Phases A+B+C+D+E+F)
- **14 Minor** — convenience gaps, documentation, or edge cases (**12 fixed** in Phases D+F)

2 original findings were removed as false positives during verification.
1 protocol-level feature (Precognition) identified — beyond adapter scope, tracked separately.

**All critical and important gaps are resolved.** Remaining:
1. **MIN-12** — No recursion into indexed arrays in PropsResolver (documented intentional divergence — will not fix)

**Final verification** (4 parallel code-reviewer agents, full side-by-side review) confirmed parity across all functional areas with 3 actionable findings:
- ~~**VER-01** [BUG]: Version mismatch URL omits `PathBase`~~ FIXED
- ~~**VER-02** [BEHAVIORAL]: Reflash on redirect keeps all TempData, not just Inertia flash~~ FIXED
- ~~**VER-03** [GAP]: `AssertInertiaFlashMissing` extension method missing~~ FIXED

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

### ~~IMP-13: Testing `AssertInertia` missing async overload~~ FIXED (Phase E)
- **Resolution**: Added `AssertInertia(Func<AssertableInertia, Task> asyncCallback)` overloads on both `HttpResponseMessage` and `Task<HttpResponseMessage>` extension targets. Callers can now use `async` lambdas with `await` for `ReloadAsync`, `LoadDeferredPropsAsync`, etc.

### ~~IMP-14: Component file existence check missing from AssertableInertia~~ FIXED (Phase E)
- **Resolution**: Added `bool? shouldExist = null` parameter to `Component()`. Added `PageExistenceConfig` record and static `Configure()` methods (including `InertiaOptions` overload) on `AssertableInertia`. When enabled, validates file exists by iterating `PagePaths × PageExtensions` with `File.Exists`. Uses `[ThreadStatic]` to prevent parallel test interference.

### ~~IMP-15: Testing `Scope()` and nested assertion methods missing~~ FIXED (Phase E)
- **Resolution**: Added `Scope(path, callback)`, `First(path, callback)`, `First(callback)`, `Each(path, callback)`, `Each(callback)`, and `Etc()` to `AssertableInertia`. Scoped instances use a private constructor with interaction tracking via `HashSet<string>`. At scope exit, `VerifyInteracted()` checks all top-level keys were touched unless `Etc()` was called. All existing assertion methods (`Has`, `Missing`, `Where`, `WhereNot`, `WhereType`, `WhereContains`, `HasAny`, `Prop`) now call `TrackInteraction()` — a no-op outside scopes.

### ~~IMP-16: Testing `WhereNot`, `WhereType`, `WhereContains`, `HasAny` missing~~ FIXED (Phase E)
- **Resolution**: Added `WhereNot(path, expected)`, `WhereType(path, expectedType)` (accepts "string"/"integer"/"number"/"boolean"/"array"/"object"/"null"), `WhereContains(path, expected)` (arrays and strings), and `HasAny(params paths)` to `AssertableInertia`.

### ~~IMP-17: `SsrRenderFailed` event notification missing~~ FIXED (Phase D)
- **Resolution**: Added `Action<SsrRenderFailedContext>? OnSsrRenderFailed` delegate to `InertiaOptions`. `SsrRenderFailedContext` sealed class carries all 7 PHP fields (Page, Error, ErrorType, Hint, BrowserApi, Stack, SourceLocation) plus `Exception?`. Callback invoked before `ILogger` warning and optional throw. Callback exceptions are caught and logged at `LogError` level to protect CSR fallback.

### ~~IMP-18: `FlushShared()` not exposed on `IInertia` interface~~ FIXED (Phase F)
- **Resolution**: Added `void FlushShared()` to `IInertia` interface. Changed visibility from `internal` to `public` on `InertiaFactory`.

### ~~IMP-19: `GetVersion()` not exposed on `IInertia` interface~~ FIXED (Phase F)
- **Resolution**: Added `string GetVersion()` to `IInertia` interface. Changed visibility from `internal` to `public` on `InertiaFactory`.

---

## Minor Gaps (14)

| ID | Area | Issue |
|----|------|-------|
| ~~MIN-01~~ | Core | ~~`ClearHistory()`/`PreserveFragment()` don't persist across redirects via TempData~~ FIXED (Phase F): Both methods now write to TempData. `Render()` resolves via `TryGetValue` (read-and-consume) matching PHP's `session()->pull()`. |
| ~~MIN-02~~ | Core | ~~`Share()` doesn't accept `object` for property flattening~~ FIXED (Phase F): Added `Share(object)` overload with runtime type dispatch (`IDictionary`, `IInertiaPropertyProvider`, `string` guard, default reflection). |
| ~~MIN-03~~ | Props | ~~ScrollProp missing 2 `new` fluent overloads for `Append/Prepend(IDictionary)`~~ FIXED (Phase F): Added `Append(IDictionary<string, string>)` and `Prepend(IDictionary<string, string>)` covariant shadows. |
| ~~MIN-04~~ | Props | ~~ScrollProp missing constructor/factory overloads for metadata factory callback~~ FIXED (Phase F): Added async metadata factory constructor + 2 `Prop.Scroll<T>` factory overloads. |
| ~~MIN-05~~ | Props | ~~`Until(DateTimeOffset)` overload missing on OnceInfo~~ FIXED (Phase F): Added `Until(DateTimeOffset)` to `OnceInfo` + fluent methods on `OptionalProp`, `OnceProp`, `DeferProp`, `MergeProp`. |
| ~~MIN-06~~ | Props | ~~`Func<IServiceProvider, T>` overloads documented in plan but never implemented~~ FIXED (Phase F): Added `IServiceResolvableProp` interface, service callback fields/constructors on all 6 prop types, `PropsResolver` service resolution branch, and 12 `Prop.*` factory overloads. |
| ~~MIN-07~~ | Middleware | ~~Version mismatch only reflashes Inertia flash, not all TempData~~ FIXED (Phase F): Added `ReflashAllTempData()` using `tempData.Keep()` (no-arg keeps all keys). Replaced both `ReflashFlashData` call sites. |
| ~~MIN-08~~ | Middleware | ~~Empty response redirect uses `Referer` only~~ FIXED (Phase F): Changed no-Referer fallback from 204 No Content to `302 → /`. |
| ~~MIN-09~~ | SSR | ~~Missing `bootstrap/ssr/` default bundle detection paths~~ FIXED (Phase F): Prepended 4 `wwwroot/ssr/` paths to `DefaultPaths` (higher priority than `wwwroot/js/`). |
| ~~MIN-10~~ | SSR | ~~Full-URL pattern matching absent from SSR path exclusion~~ FIXED (Phase F): Added `NormalizePattern()` that extracts `Uri.AbsolutePath` from full-URL patterns. |
| ~~MIN-11~~ | SSR | ~~`BrowserApi` and `Stack` fields missing from `SsrException`~~ FIXED (Phase D): Added `BrowserApi` and `Stack` properties to `SsrException`. `ParseError` now extracts `browserApi` and `stack` from SSR error JSON. |
| MIN-12 | Props | No recursion into indexed arrays in PropsResolver (documented intentional divergence — will not fix) |
| ~~MIN-13~~ | Testing | ~~`AssertInertiaFlash` on redirect responses missing~~ FIXED (Phase F): Added `AssertInertiaFlash(key, httpClient)` and `AssertInertiaFlash(key, expected, httpClient)` extensions that follow the redirect and assert flash on the resulting page. |
| ~~MIN-15~~ | Core | ~~No `Back()` convenience method~~ FIXED (Phase F): Added `InertiaBackResult : IActionResult, IResult` with configurable status code. `IInertia.Back()` resolves URL from Referer with fallback. |

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
- `App::call()` DI in callables → `Func<IServiceProvider, T>` overloads (Phase F)
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

### ~~Phase E: Testing Package~~ COMPLETE
16. ~~**IMP-13** — Async `AssertInertia` overload~~
17. ~~**IMP-14** — Wire `TestingEnsurePagesExist` into `AssertableInertia`~~
18. ~~**IMP-15** — `Scope()`, `First()`, `Each()`, `Etc()`~~
19. ~~**IMP-16** — Missing assertion methods~~

### ~~Phase F: API Surface & Minor Polish~~ COMPLETE
20. ~~**IMP-18** — Expose `FlushShared()` on `IInertia`~~
21. ~~**IMP-19** — Expose `GetVersion()` on `IInertia`~~
22. ~~**MIN-15** — `Back()` convenience method~~
23-34. ~~All remaining MIN-* items (MIN-01–MIN-10, MIN-13)~~

---

## Final Verification (2026-03-31)

Complete side-by-side review of ALL PHP source files against ALL C# source files by 4 parallel code-reviewer agents, each covering a functional slice: (1) API & Response Lifecycle, (2) Prop Types & PropsResolver, (3) SSR, Config & DI, (4) Testing & Supporting Types.

### Verified Areas (confirmed parity)

- **ResponseFactory ↔ IInertia/InertiaFactory**: All 20+ public methods have C# equivalents
- **Response ↔ InertiaResponse**: Page object JSON structure matches (all fields, camelCase, conditional inclusion)
- **Middleware**: Version check, 302→303 conversion, fragment redirect, prefetch detection, validation errors, Vary header, encrypt history
- **All 6 prop types**: AlwaysProp, OptionalProp, OnceProp, DeferProp, MergeProp, ScrollProp — fluent APIs, interfaces, resolution behavior all match
- **PropsResolver**: Partial filtering (only/except with bidirectional prefix matching), initial load exclusion, AlwaysProp bypass, once-prop exclusion, reset, metadata collection (all 8 categories), dot-notation unpacking, prop-type unwrap after resolution
- **SSR**: Dispatch URLs (production + hot mode), health check, bundle detection, error parsing (all 6 fields), SsrRenderFailed context, SsrState dispatch caching
- **Config**: All 11 PHP config keys have C# InertiaOptions equivalents with matching defaults
- **DI lifetimes**: Correct (singleton for stateless, scoped for per-request)
- **Headers & session keys**: All constants map 1-to-1 with identical string values
- **Testing**: All AssertableInertia assertion methods (component, url, version, has, missing, where, whereNot, whereType, whereContains, hasAny, scope, first, each, etc, flash), ReloadRequest headers, test extensions
- **Supporting types**: PropertyContext, RenderContext, provider interfaces, ScrollMetadata, ComponentNotFoundException, Tag Helpers

### New Findings

#### ~~VER-01: Version mismatch URL omits PathBase~~ FIXED
- **Resolution**: Changed `HandleVersionChange` default to include `PathBase` in URL: `$"{ctx.Request.PathBase}{ctx.Request.Path}{ctx.Request.QueryString}"`. Added test `InvokeAsync_VersionMismatch_OnGet_IncludesPathBaseInLocation`.

#### ~~VER-02: Reflash on redirect keeps ALL TempData~~ FIXED
- **Resolution**: Split into `ReflashAllTempData()` (version mismatch — keeps all keys via `tempData.Keep()`) and `ReflashInertiaTempData()` (redirects — selectively keeps only `FlashData`, `ClearHistory`, `PreserveFragment` keys). Updated redirect path to use selective method.

#### ~~VER-03: `AssertInertiaFlashMissing` extension missing~~ FIXED
- **Resolution**: Added `AssertInertiaFlashMissing(key, httpClient)` + Task overload to `InertiaTestExtensions`. Follows redirect, parses Inertia page, calls `MissingFlash(key)`.

#### VER-04: `Once()` missing `as`/`until` shorthand parameters [CONVENIENCE]
- **Confidence**: 90%
- **PHP**: `->once(as: 'key', until: 3600)` single-call convenience
- **C#**: Requires chaining `.Once().As("key").Until(3600)` — functionally equivalent, but different API shape
- **Fix**: Add `Once(bool value = true, string? key = null, TimeSpan? until = null)` overload to `OnceInfo` + prop types

#### VER-05: `Share()` with dot-notation keys stores flat key [BEHAVIORAL]
- **Confidence**: 82%
- **PHP**: `Arr::set($props, 'user.name', value)` creates nested `['user' => ['name' => value]]`
- **C#**: `_sharedProps["user.name"] = value` stores literal key
- **Impact**: Low — consumers typically use `Share(new { User = ... })` not dot-notation keys

#### VER-06: SSR path exclusion only supports trailing `/*` wildcards [LIMITATION]
- **Confidence**: 90%
- **PHP**: Uses `fnmatch()`-style glob matching via `Str::is()` — supports `*/admin`, `api/*/users`
- **C#**: Only handles `pattern/*` suffix and exact match
- **Impact**: Low — common patterns (`/admin/*`) work; exotic mid-path wildcards don't

### Remaining API Surface Gaps (Low Priority)

| ID | Gap | Confidence | Notes |
|----|-----|-----------|-------|
| VER-07 | No per-request `ResolveUrlUsing()` on `IInertia` | 83% | `InertiaOptions.UrlResolver` covers startup config; per-request override not exposed |
| VER-08 | `Location()` doesn't accept redirect result objects | 80% | Callers in .NET have URL strings directly |
| VER-09 | `Back()` missing `headers` parameter | 80% | PHP `$headers` param rarely used |
| VER-10 | `ScrollMetadata.FromPaginator()` has no equivalent | 82% | No .NET standard paginator type; acceptable divergence |

### Accepted Divergences (from verification)

| ID | Issue | Rationale |
|----|-------|-----------|
| VER-04 | `Once()` missing `as`/`until` shorthand params | C# idiomatic chaining: `.Once().As("key").Until(3600)` — dismissed |
| VER-05 | `Share("user.name", val)` stores flat key (PHP `Arr::set` creates nested) | C# idiom is `Share(new { User = new { Name = val } })` via `Share(object)` overload |
| VER-06 | SSR path exclusion only supports trailing `/*` wildcards | Common patterns work; mid-path/leading wildcards are exotic and undocumented |
| VER-07 | No per-request `ResolveUrlUsing()` on `IInertia` | `InertiaOptions.UrlResolver` delegate covers startup config |
| VER-08 | `Location()` doesn't accept redirect result objects | .NET callers have URL strings directly |
| VER-09 | `Back()` missing `headers` parameter | Rarely used in PHP; callers can set headers on the response directly |
| VER-10 | No `ScrollMetadata.FromPaginator()` | No .NET standard paginator type — manual construction required |

### Verdict

**Feature parity is confirmed.** All 3 actionable findings (VER-01, VER-02, VER-03) have been fixed. The remaining items (VER-04 through VER-10) are intentional .NET idiom divergences or low-impact convenience gaps that do not affect the parity claim.
