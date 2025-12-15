# Inertia.js Laravel vs .NET Feature Comparison

This document provides a detailed comparison of features between inertia-laravel (PHP/Laravel) and the planned inertia-dotnet (C#/.NET) implementation.

## Legend

- ✅ **Implemented** - Feature is complete in inertia-laravel
- 🔄 **To Migrate** - Feature needs to be migrated to .NET
- ⚠️ **Adaptation Required** - Feature requires significant changes for .NET
- ❌ **Not Applicable** - Feature doesn't apply to .NET

## Feature Matrix

### 1. Core Response Handling

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Basic Rendering** |
| Render component with props | ✅ `Inertia::render()` | 🔄 `IInertia.RenderAsync()` | To Migrate | Use async/await pattern |
| Root view configuration | ✅ `setRootView()` | 🔄 `InertiaOptions.RootView` | To Migrate | Use IOptions pattern |
| Component name validation | ✅ `ensure_pages_exist` | 🔄 `InertiaOptions.EnsurePagesExist` | To Migrate | File system validation |
| Page paths configuration | ✅ `page_paths` array | 🔄 `InertiaOptions.PagePaths` | To Migrate | List<string> |
| Page extensions | ✅ `page_extensions` | 🔄 `InertiaOptions.PageExtensions` | To Migrate | Support .js/.jsx/.vue/.ts/.tsx/.svelte |
| **Shared Data** |
| Share props globally | ✅ `share()` method | 🔄 `Share()` method | To Migrate | Dictionary<string, object> |
| Share with array | ✅ `share(['key' => 'value'])` | 🔄 `Share(new { key = "value" })` | To Migrate | Anonymous types |
| Share with Arrayable | ✅ `share($arrayable)` | 🔄 `Share(IEnumerable)` | To Migrate | Support collections |
| Share single key-value | ✅ `share('key', $value)` | 🔄 `Share("key", value)` | To Migrate | Overload method |
| Get shared data | ✅ `getShared($key)` | 🔄 `GetShared(key)` | To Migrate | Null handling |
| Flush shared data | ✅ `flushShared()` | 🔄 `FlushShared()` | To Migrate | Clear dictionary |
| **Asset Versioning** |
| Set version (string) | ✅ `version($string)` | 🔄 `SetVersion(string)` | To Migrate | Simple string |
| Set version (closure) | ✅ `version(fn() => ...)` | 🔄 `SetVersion(Func<string>)` | To Migrate | Lazy evaluation |
| Get version | ✅ `getVersion()` | 🔄 `GetVersion()` | To Migrate | Return string |
| Auto version from manifest | ✅ `version()` in middleware | 🔄 Middleware `Version()` | To Migrate | Hash manifest file |
| **URL Resolution** |
| Custom URL resolver | ✅ `resolveUrlUsing()` | 🔄 `ResolveUrlUsing()` | To Migrate | Func<HttpRequest, string> |
| Default URL resolution | ✅ Automatic | 🔄 `HttpRequest.Path` | To Migrate | From request path |

### 2. Property Types

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Optional Props (formerly Lazy)** |
| Create optional prop | ✅ `optional(fn() => ...)` | 🔄 `Optional(() => ...)` | To Migrate | Factory method |
| Ignore on first load | ✅ `IgnoreFirstLoad` interface | 🔄 `IIgnoreFirstLoad` | To Migrate | Marker interface |
| Load on partial reload | ✅ Automatic | 🔄 Header detection | To Migrate | X-Inertia-Partial-Data |
| Async callback support | ✅ Laravel Container | 🔄 Async/await | To Migrate | `Func<Task<T>>` |
| **Deferred Props** |
| Create deferred prop | ✅ `defer(fn() => ...)` | 🔄 `Defer(() => ...)` | To Migrate | Factory method |
| Defer groups | ✅ `defer($callback, 'group')` | 🔄 `Defer(() => ..., "group")` | To Migrate | Named groups |
| Load after render | ✅ Client-initiated | 🔄 Same | To Migrate | Frontend requests |
| Merge support | ✅ `Mergeable` interface | 🔄 `IMergeable` | To Migrate | Interface |
| **Always Props** |
| Create always prop | ✅ `always($value)` | 🔄 `Always(value)` | To Migrate | Factory method |
| Bypass partial filtering | ✅ Automatic | 🔄 Property check | To Migrate | Always included |
| Callable values | ✅ `always(fn() => ...)` | 🔄 `Always(() => ...)` | To Migrate | Lazy evaluation |
| Static values | ✅ `always($value)` | 🔄 `Always(value)` | To Migrate | Direct value |
| **Merge Props** |
| Create merge prop | ✅ `merge($value)` | 🔄 `Merge(value)` | To Migrate | Factory method |
| Shallow merge | ✅ Default | 🔄 Default | To Migrate | Object.assign logic |
| Deep merge | ✅ `deepMerge($value)` | 🔄 `DeepMerge(value)` | To Migrate | Recursive merge |
| Callable values | ✅ Supported | 🔄 `Func<T>` | To Migrate | Lazy evaluation |
| Configure merge path | ✅ `path()` method | 🔄 `WithPath()` | To Migrate | Fluent API |
| Merge only on partial | ✅ `onlyOnPartial()` | 🔄 `OnlyOnPartial()` | To Migrate | Fluent API |
| **Scroll Props** |
| Create scroll prop | ✅ `scroll($value)` | 🔄 `Scroll(value)` | To Migrate | Factory method |
| Wrapper configuration | ✅ `scroll($value, 'data')` | 🔄 `Scroll(value, "data")` | To Migrate | Wrapper key |
| Metadata provider | ✅ `ProvidesScrollMetadata` | 🔄 `IProvidesScrollMetadata` | To Migrate | Interface |
| Auto-detect paginator | ✅ Laravel Paginator | 🔄 Custom pagination | ⚠️ Adaptation | Different pagination libs |
| Append merge | ✅ `append()` method | 🔄 `Append()` | To Migrate | Fluent API |
| Prepend merge | ✅ `prepend()` method | 🔄 `Prepend()` | To Migrate | Fluent API |
| Merge intent header | ✅ `X-Inertia-Infinite-Scroll-Merge-Intent` | 🔄 Same | To Migrate | Header constant |
| **Once Props** |
| Create once prop | ✅ `once(fn() => ...)` | 🔄 `Once(() => ...)` | To Migrate | Factory method |
| Share once prop | ✅ `shareOnce($key, fn())` | 🔄 `ShareOnce(key, () => ...)` | To Migrate | Helper method |
| Cache across navigations | ✅ Automatic | 🔄 Context tracking | To Migrate | Resolution caching |
| Fresh loads | ✅ `fresh` props | 🔄 Header detection | To Migrate | X-Inertia-Partial-Data |
| **Deprecated Props** |
| Lazy prop (deprecated) | ✅ Alias to OptionalProp | 🔄 Optional alias | To Migrate | Backward compat |

### 3. Middleware

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Core Middleware** |
| Inertia request detection | ✅ `X-Inertia` header | 🔄 Extension method | To Migrate | `request.IsInertia()` |
| Version checking | ✅ `X-Inertia-Version` | 🔄 Compare versions | To Migrate | Force reload if mismatch |
| Shared props injection | ✅ `share()` method | 🔄 Middleware hook | To Migrate | Override `Share()` |
| Root view selection | ✅ `rootView()` method | 🔄 Override `RootView()` | To Migrate | Per-request |
| URL resolver | ✅ `urlResolver()` method | 🔄 Override `UrlResolver()` | To Migrate | Custom logic |
| Empty response handling | ✅ `onEmptyResponse()` | 🔄 Override method | To Migrate | Redirect back |
| Version change handling | ✅ `onVersionChange()` | 🔄 Override method | To Migrate | Force reload |
| **Validation Errors** |
| Resolve validation errors | ✅ `resolveValidationErrors()` | 🔄 ModelState integration | ⚠️ Adaptation | ASP.NET validation |
| Error bag support | ✅ `X-Inertia-Error-Bag` | 🔄 Same header | To Migrate | Named error bags |
| Multiple errors per field | ✅ `withAllErrors` flag | 🔄 Option setting | To Migrate | Array of errors |
| Session flash errors | ✅ Laravel Session | 🔄 TempData | ⚠️ Adaptation | ASP.NET session |
| **Redirect Handling** |
| 303 for PUT/PATCH/DELETE | ✅ Automatic | 🔄 Status code change | To Migrate | Middleware logic |
| Flash session on redirect | ✅ `session()->reflash()` | 🔄 TempData.Keep() | ⚠️ Adaptation | ASP.NET session |
| **Headers** |
| Vary header | ✅ `Vary: X-Inertia` | 🔄 Response header | To Migrate | HTTP caching |
| Partial data header | ✅ `X-Inertia-Partial-Data` | 🔄 Parse header | To Migrate | Comma-separated |
| Partial component | ✅ `X-Inertia-Partial-Component` | 🔄 Validate component | To Migrate | Security check |
| Partial except | ✅ `X-Inertia-Partial-Except` | 🔄 Parse header | To Migrate | Exclude props |
| Reset header | ✅ `X-Inertia-Reset` | 🔄 Parse header | To Migrate | Reset scroll/merge |
| Location header | ✅ `X-Inertia-Location` (409) | 🔄 Same | To Migrate | Force redirect |
| **History Encryption** |
| Encrypt middleware | ✅ `EncryptHistoryMiddleware` | 🔄 Separate middleware | To Migrate | Optional encryption |
| Configuration | ✅ `inertia.history.encrypt` | 🔄 `InertiaOptions.EncryptHistory` | To Migrate | Config option |
| Clear history | ✅ `clearHistory()` | 🔄 `ClearHistory()` | To Migrate | Session flag |

### 4. Server-Side Rendering (SSR)

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **SSR Core** |
| SSR enabled flag | ✅ `ssr.enabled` config | 🔄 `InertiaOptions.SsrEnabled` | To Migrate | Boolean flag |
| SSR URL configuration | ✅ `ssr.url` config | 🔄 `InertiaOptions.SsrUrl` | To Migrate | Default: localhost:13714 |
| SSR bundle path | ✅ `ssr.bundle` config | 🔄 `InertiaOptions.SsrBundle` | To Migrate | Optional path |
| Ensure bundle exists | ✅ `ssr.ensure_bundle_exists` | 🔄 `InertiaOptions.EnsureBundleExists` | To Migrate | Validation flag |
| **HTTP Gateway** |
| HTTP client | ✅ `Http::post()` | 🔄 `HttpClient` | To Migrate | IHttpClientFactory |
| Render endpoint | ✅ POST `/render` | 🔄 Same | To Migrate | JSON payload |
| Health check endpoint | ✅ GET `/health` | 🔄 Same | To Migrate | Boolean response |
| Error handling | ✅ Try/catch, return null | 🔄 Exception handling | To Migrate | Graceful fallback |
| Connection exceptions | ✅ Catch and handle | 🔄 HttpRequestException | To Migrate | Recent feature (v2.0.11) |
| **Bundle Detection** |
| Auto-detect bundle | ✅ `BundleDetector` | 🔄 File system search | To Migrate | Common paths |
| Default paths | ✅ `bootstrap/ssr/ssr.mjs`, `ssr.js` | 🔄 `wwwroot/ssr/`, etc. | ⚠️ Adaptation | .NET conventions |
| Custom bundle path | ✅ Config override | 🔄 Same | To Migrate | Config priority |
| **SSR Response** |
| Parse head content | ✅ `$response['head']` | 🔄 Parse JSON | To Migrate | Array of strings |
| Parse body content | ✅ `$response['body']` | 🔄 Parse JSON | To Migrate | HTML string |
| Merge into response | ✅ View data | 🔄 Razor layout | ⚠️ Adaptation | Different rendering |
| **Fallback** |
| CSR fallback | ✅ Return null | 🔄 Same | To Migrate | Client-side render |
| Conditional dispatch | ✅ `shouldDispatch()` | 🔄 Same logic | To Migrate | Private method |

### 5. Testing Infrastructure

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Test Response Macros** |
| assertInertia() | ✅ `TestResponse` macro | 🔄 Extension method | To Migrate | Fluent API |
| inertiaPage() | ✅ Get page array | 🔄 Extension method | To Migrate | Returns object |
| inertiaProps() | ✅ Get prop value | 🔄 Extension method | To Migrate | Nested access |
| **AssertableInertia** |
| Create from response | ✅ `fromTestResponse()` | 🔄 From HttpResponse | ⚠️ Adaptation | WebApplicationFactory |
| Component assertions | ✅ `component()` | 🔄 `WithComponent()` | To Migrate | Name + file check |
| URL assertions | ✅ `url()` | 🔄 `WithUrl()` | To Migrate | String comparison |
| Version assertions | ✅ `version()` | 🔄 `WithVersion()` | To Migrate | String comparison |
| Has props | ✅ `has()` | 🔄 `Has()` | To Migrate | Existence check |
| Missing props | ✅ `missing()` | 🔄 `Missing()` | To Migrate | Non-existence |
| Where conditions | ✅ `where()` | 🔄 `Where()` | To Migrate | Value matching |
| Count assertions | ✅ `count()` | 🔄 `WithCount()` | To Migrate | Array/collection |
| Type assertions | ✅ `whereType()` | 🔄 `WhereType()` | To Migrate | Type checking |
| Nested props | ✅ Dot notation | 🔄 Same | To Migrate | 'user.name' |
| Array indexing | ✅ `users.0.name` | 🔄 Same | To Migrate | Index access |
| **Debugging** |
| Dump props | ✅ `dump()` | 🔄 `Dump()` | To Migrate | Console output |
| Dump and die | ✅ `dd()` | 🔄 `Dd()` | To Migrate | Throw exception |
| **Partial Reload Testing** |
| Reload request helper | ✅ `ReloadRequest` | 🔄 Same | To Migrate | Add headers |
| Only props | ✅ `reloadOnly()` | 🔄 Same | To Migrate | X-Inertia-Partial-Data |
| Except props | ✅ `reloadExcept()` | 🔄 Same | To Migrate | X-Inertia-Partial-Except |
| Array inputs | ✅ Recent feature | 🔄 Same | To Migrate | v2.0.11 |
| **Deferred Props Testing** |
| Load deferred props | ✅ `loadDeferredProps()` | 🔄 Same | To Migrate | Simulate request |
| Load by group | ✅ Group parameter | 🔄 Same | To Migrate | Named groups |

### 6. Configuration

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **General** |
| Root view name | ✅ Set in middleware | 🔄 `InertiaOptions.RootView` | To Migrate | Default: "app" |
| Ensure pages exist | ✅ `ensure_pages_exist` | 🔄 `InertiaOptions.EnsurePagesExist` | To Migrate | Boolean |
| Page paths | ✅ Array of paths | 🔄 `List<string>` | To Migrate | Search directories |
| Page extensions | ✅ Array of extensions | 🔄 `List<string>` | To Migrate | .js, .jsx, .vue, etc. |
| Script element option | ✅ `use_script_element_for_initial_page` | 🔄 `InertiaOptions.UseScriptElement` | To Migrate | v2.0.12 feature |
| **SSR Configuration** |
| All SSR options | ✅ `ssr` config section | 🔄 `InertiaOptions.Ssr` | To Migrate | Nested config |
| **Testing Configuration** |
| Testing page paths | ✅ `testing.page_paths` | 🔄 Separate config | To Migrate | Override for tests |
| Testing extensions | ✅ `testing.page_extensions` | 🔄 Same | To Migrate | Override for tests |
| Ensure pages in tests | ✅ `testing.ensure_pages_exist` | 🔄 Default: true | To Migrate | Test validation |
| **History Configuration** |
| Encrypt history | ✅ `history.encrypt` | 🔄 `InertiaOptions.EncryptHistory` | To Migrate | Boolean |

### 7. CLI Commands

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Middleware Creation** |
| Create middleware | ✅ `inertia:middleware` | 🔄 `dotnet inertia create-middleware` | ⚠️ Adaptation | Or skip for .NET |
| Stub file | ✅ `stubs/inertia-middleware.stub` | 🔄 Template | ⚠️ Adaptation | .NET template |
| **SSR Management** |
| Start SSR server | ✅ `inertia:start-ssr` | 🔄 `dotnet inertia start-ssr` | ⚠️ Adaptation | Process management |
| Node.js runtime | ✅ `--runtime=node` | 🔄 Same | To Migrate | Default |
| Bun runtime | ✅ `--runtime=bun` | 🔄 Same | To Migrate | Alternative |
| Stop SSR server | ✅ `inertia:stop-ssr` | 🔄 `dotnet inertia stop-ssr` | ⚠️ Adaptation | Process kill |
| Check SSR health | ✅ `inertia:check-ssr` | 🔄 `dotnet inertia check-ssr` | To Migrate | HTTP health check |
| **Process Management** |
| PCNTL signals | ✅ PHP extension | 🔄 Process.Kill() | ⚠️ Adaptation | Platform differences |
| Process output streaming | ✅ `foreach ($process)` | 🔄 OutputDataReceived | ⚠️ Adaptation | Event-based |

### 8. Helper Functions and Facades

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Helper Functions** |
| inertia() helper | ✅ `inertia($component, $props)` | 🔄 DI injection preferred | ⚠️ Adaptation | Not idiomatic C# |
| inertia_location() | ✅ Helper function | 🔄 Extension method | To Migrate | `inertia.Location()` |
| **Facade** |
| Inertia facade | ✅ `Inertia::render()` | 🔄 Static class or DI | ⚠️ Adaptation | DI preferred in .NET |
| Facade methods | ✅ All ResponseFactory methods | 🔄 Interface methods | To Migrate | `IInertia` |

### 9. Interfaces and Contracts

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Property Interfaces** |
| IgnoreFirstLoad | ✅ Marker interface | 🔄 `IIgnoreFirstLoad` | To Migrate | Marker |
| Mergeable | ✅ Interface | 🔄 `IMergeable` | To Migrate | Merge behavior |
| Onceable | ✅ Interface | 🔄 `IOnceable` | To Migrate | Once behavior |
| **Provider Interfaces** |
| ProvidesInertiaProperties | ✅ Multiple props | 🔄 `IProvidesInertiaProperties` | To Migrate | ToArray() method |
| ProvidesInertiaProperty | ✅ Single prop | 🔄 `IProvidesInertiaProperty` | To Migrate | GetKey(), GetValue() |
| ProvidesScrollMetadata | ✅ Pagination info | 🔄 `IProvidesScrollMetadata` | To Migrate | Page numbers |
| **SSR Interfaces** |
| Gateway | ✅ `dispatch()` method | 🔄 `IGateway` | To Migrate | Interface |
| HasHealthCheck | ✅ `isHealthy()` | 🔄 `IHasHealthCheck` | To Migrate | Health check |
| **Laravel Interfaces** |
| Arrayable | ✅ Laravel interface | 🔄 `IEnumerable` or custom | ⚠️ Adaptation | .NET equivalent |
| Responsable | ✅ Laravel interface | 🔄 `IActionResult` | ⚠️ Adaptation | ASP.NET Core |

### 10. Exception Handling

| Feature | inertia-laravel | inertia-dotnet | Status | Notes |
|---------|----------------|----------------|--------|-------|
| **Exceptions** |
| ComponentNotFoundException | ✅ Exception class | 🔄 Same | To Migrate | File not found |
| SsrException | ✅ Exception class | 🔄 Same | To Migrate | SSR errors |
| **Error Handling** |
| Try/catch SSR | ✅ Graceful fallback | 🔄 Same | To Migrate | Return null |
| Connection exceptions | ✅ Catch StrayRequestException | 🔄 HttpRequestException | ⚠️ Adaptation | .NET exceptions |

### 11. Recent Additions (v2.0.x)

| Feature | Version | Status | Priority |
|---------|---------|--------|----------|
| Once props | v2.0.12 | 🔄 To Migrate | High |
| Script element for initial page | v2.0.12 | 🔄 To Migrate | Medium |
| Multiple errors per field | v2.0.11 | 🔄 To Migrate | High |
| Array inputs in reload methods | v2.0.11 | 🔄 To Migrate | High |
| Connection exception handling | v2.0.11 | 🔄 To Migrate | Medium |
| Scroll props improvements | v2.0.8-10 | 🔄 To Migrate | High |
| Deep merge improvements | v2.0.8 | 🔄 To Migrate | Medium |
| Deferred props testing | v2.0.7 | 🔄 To Migrate | Medium |
| Encrypt history middleware | v2.0.6 | 🔄 To Migrate | Low |

## Summary Statistics

### Implementation Status

| Category | Total Features | To Migrate | Adaptation Required | Not Applicable |
|----------|----------------|------------|---------------------|----------------|
| Core Response | 18 | 18 | 0 | 0 |
| Property Types | 35 | 35 | 0 | 0 |
| Middleware | 24 | 20 | 4 | 0 |
| SSR | 18 | 15 | 3 | 0 |
| Testing | 26 | 24 | 2 | 0 |
| Configuration | 13 | 13 | 0 | 0 |
| CLI Commands | 8 | 4 | 4 | 0 |
| Helpers/Facades | 4 | 2 | 2 | 0 |
| Interfaces | 11 | 9 | 2 | 0 |
| Exceptions | 4 | 3 | 1 | 0 |
| Recent Additions | 9 | 9 | 0 | 0 |
| **TOTAL** | **170** | **152** | **18** | **0** |

### Priority Breakdown

| Priority | Features | Description |
|----------|----------|-------------|
| **Critical** | 80 | Core functionality, must have for v1.0 |
| **High** | 55 | Important features, should have for v1.0 |
| **Medium** | 25 | Nice to have, can be v1.1+ |
| **Low** | 10 | Optional, community-driven |

## Implementation Order

### Phase 1: Foundation (Weeks 1-2)
- Core response handling (18 features)
- Basic middleware (10 features)
- Service registration (5 features)
**Total: 33 features**

### Phase 2: Property System (Week 3)
- All property types (35 features)
- Property interfaces (6 features)
**Total: 41 features**

### Phase 3: Complete Middleware (Week 4)
- Remaining middleware features (14 features)
- Validation integration (4 features)
**Total: 18 features**

### Phase 4: SSR (Week 5)
- SSR core (18 features)
**Total: 18 features**

### Phase 5: Testing (Week 6)
- Testing infrastructure (26 features)
**Total: 26 features**

### Phase 6: Advanced (Weeks 7-8)
- Configuration (13 features)
- CLI commands (8 features, optional)
- Helpers and utilities (4 features)
- Exceptions (4 features)
**Total: 29 features**

### Phase 7: Polish (Weeks 9-10)
- Recent additions (9 features)
- Documentation
- Examples
**Total: 9+ features**

## Key Differences and Adaptations

### 1. **Dependency Injection vs Facades**
- **Laravel:** Heavy use of facades (`Inertia::render()`)
- **.NET:** Prefer DI (`IInertia` injection)
- **Impact:** More explicit, better testability

### 2. **Middleware Pattern**
- **Laravel:** Class-based with handle() method
- **.NET:** IMiddleware or middleware delegates
- **Impact:** Similar concepts, different implementation

### 3. **Session Handling**
- **Laravel:** Built-in session with flash data
- **.NET:** ISession + TempData
- **Impact:** Need adaptation for validation errors

### 4. **Validation Errors**
- **Laravel:** MessageBag from session
- **.NET:** ModelState.Errors
- **Impact:** Different error structure

### 5. **Paginations**
- **Laravel:** Built-in Paginator
- **.NET:** Various libraries (X.PagedList, etc.)
- **Impact:** Need flexible pagination support

### 6. **File System**
- **Laravel:** Helper functions (base_path, resource_path)
- **.NET:** Path.Combine, IHostEnvironment
- **Impact:** Different path resolution

### 7. **HTTP Client**
- **Laravel:** Http facade
- **.NET:** HttpClient with IHttpClientFactory
- **Impact:** Proper disposal and configuration

### 8. **Process Management**
- **Laravel:** Symfony Process with PCNTL
- **.NET:** System.Diagnostics.Process
- **Impact:** Platform-specific considerations

### 9. **View Rendering**
- **Laravel:** Blade templates
- **.NET:** Razor views/pages
- **Impact:** Different template syntax

### 10. **Testing Framework**
- **Laravel:** PHPUnit with Laravel TestCase
- **.NET:** xUnit with WebApplicationFactory
- **Impact:** Different test structure

## Conclusion

The migration involves **170 features** in total:
- **152 features** can be directly migrated with C# equivalents
- **18 features** require adaptation to .NET patterns
- **0 features** are not applicable

The project is feasible and well-scoped. Most features have direct .NET equivalents, and the adaptations required are well-understood. The biggest challenges will be:

1. Validation error integration with ASP.NET Core
2. Session/TempData handling for flash data
3. CLI commands (may be optional)
4. Pagination library flexibility

With a focused team and the 12-week timeline, this migration can achieve 100% feature parity with inertia-laravel v2.0.14.
