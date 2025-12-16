# Inertia.js Laravel to .NET Migration Plan

## Executive Summary

This document provides a comprehensive plan to migrate the entire [inertia-laravel](https://github.com/inertiajs/inertia-laravel) PHP/Laravel adapter to C#/.NET, creating a feature-complete inertia-dotnet implementation.

**Current inertia-laravel version analyzed:** v2.0.14 (commit: 7240b646eee71e583f9c7ba62efe351f9b3e1c15)

## Overview

The inertia-laravel adapter provides server-side functionality for building modern single-page applications using Inertia.js. The adapter handles:
- Rendering Inertia responses with component and props
- Managing shared data across requests
- Asset versioning for cache busting
- Server-side rendering (SSR) integration
- Property types for lazy loading, deferred loading, and merging
- Testing utilities for asserting Inertia responses
- Middleware for handling Inertia requests
- CLI commands for SSR management

## Architecture Analysis

### Core Components (46 PHP files analyzed)

#### 1. **Response Management** (7 files)
- `ResponseFactory.php` (313 lines) - Factory for creating Inertia responses
- `Response.php` (421 lines) - Inertia response implementation
- `Inertia.php` (70 lines) - Facade for ResponseFactory
- `Controller.php` (26 lines) - Basic Inertia controller
- `Directive.php` (107 lines) - Blade directives (@inertia, @inertiaHead)
- `ServiceProvider.php` (148 lines) - Service provider registration
- `helpers.php` (37 lines) - Helper functions

#### 2. **Property Types** (10 files)
- `LazyProp.php` (36 lines) - Deprecated in Laravel, not implemented in .NET (use OptionalProp)
- `OptionalProp.php` (39 lines) - Properties loaded only when requested
- `DeferProp.php` (59 lines) - Properties loaded asynchronously after page render
- `AlwaysProp.php` (39 lines) - Properties always included in responses
- `MergeProp.php` (42 lines) - Properties merged with client-side data
- `ScrollProp.php` (130 lines) - Paginated properties with merge support
- `OnceProp.php` (36 lines) - Properties cached and reused across navigations
- `Mergeable.php` (12 lines) - Interface for mergeable properties
- `MergesProps.php` (83 lines) - Trait for merge functionality
- `ResolvesOnce.php` (41 lines) - Trait for once resolution

#### 3. **Supporting Interfaces** (6 files)
- `IgnoreFirstLoad.php` (9 lines) - Marker interface for lazy/optional props
- `Onceable.php` (9 lines) - Interface for once props
- `ProvidesInertiaProperties.php` (13 lines) - Interface for property providers
- `ProvidesInertiaProperty.php` (15 lines) - Single property provider interface
- `ProvidesScrollMetadata.php` (19 lines) - Interface for scroll metadata
- `ScrollMetadata.php` (94 lines) - Scroll metadata implementation

#### 4. **Middleware** (3 files)
- `Middleware.php` (196 lines) - Core Inertia middleware
- `EncryptHistoryMiddleware.php` (52 lines) - Encrypts history state
- `IgnoreFirstLoad.php` - Marker interface

#### 5. **Server-Side Rendering** (6 files)
- `Ssr/Gateway.php` (14 lines) - SSR gateway interface
- `Ssr/HttpGateway.php` (100 lines) - HTTP-based SSR implementation
- `Ssr/Response.php` (39 lines) - SSR response wrapper
- `Ssr/BundleDetector.php` (43 lines) - Detects SSR bundle location
- `Ssr/HasHealthCheck.php` (12 lines) - Interface for health checks
- `Ssr/SsrException.php` (10 lines) - SSR exception

#### 6. **Console Commands** (4 files)
- `Commands/CreateMiddleware.php` (81 lines) - Creates middleware stub
- `Commands/StartSsr.php` (90 lines) - Starts SSR server
- `Commands/StopSsr.php` (49 lines) - Stops SSR server
- `Commands/CheckSsr.php` (67 lines) - Health check for SSR server

#### 7. **Testing Infrastructure** (4 files + macros)
- `Testing/TestResponseMacros.php` (58 lines) - Test response extensions
- `Testing/AssertableInertia.php` (395 lines) - Fluent assertions for Inertia responses
- `Testing/ReloadRequest.php` (87 lines) - Simulates partial reload requests
- `Testing/Concerns/` (5 concern traits):
  - `Debugging.php` (54 lines) - Debug output helpers
  - `Has.php` (160 lines) - Property existence assertions
  - `Interaction.php` (121 lines) - Interactive assertions
  - `Matching.php` (114 lines) - Value matching assertions
  - `PageObject.php` (54 lines) - Page object access

#### 8. **Context and Metadata** (3 files)
- `PropertyContext.php` (66 lines) - Tracks property resolution context
- `RenderContext.php` (50 lines) - Tracks rendering state
- `ComponentNotFoundException.php` (10 lines) - Exception for missing components

#### 9. **Configuration**
- `config/inertia.php` (114 lines) - Configuration file with options for:
  - SSR settings (enabled, url, bundle path)
  - Page validation (ensure_pages_exist, page_paths, page_extensions)
  - History encryption
  - Script element for initial page data
  - Testing configuration

#### 10. **Support Classes**
- `Support/Header.php` (29 lines) - HTTP header constants

## Feature Inventory

### Core Features (Must Have)

#### 1. **Basic Response Rendering**
- ✅ Render Inertia responses with component name and props
- ✅ Root view template configuration
- ✅ Asset versioning for cache busting
- ✅ Shared props across all responses
- ✅ URL resolution and customization

#### 2. **Property Types**
- ✅ **Optional Props** (formerly Lazy) - Load only when explicitly requested
- ✅ **Deferred Props** - Load asynchronously after initial render
- ✅ **Always Props** - Always included, bypass partial reload filtering
- ✅ **Merge Props** - Merge with existing client data (shallow & deep)
- ✅ **Scroll Props** - Paginated data with append/prepend support
- ✅ **Once Props** - Cached and reused across navigations

#### 3. **Middleware**
- ✅ Handle Inertia requests (X-Inertia header detection)
- ✅ Asset version checking and forced reload
- ✅ Shared props injection
- ✅ Validation error handling
- ✅ POST/PUT/PATCH/DELETE redirect handling (303 status)
- ✅ Empty response handling
- ✅ History encryption middleware
- ✅ Custom URL resolver support

#### 4. **Server-Side Rendering (SSR)**
- ✅ HTTP gateway for SSR communication
- ✅ SSR bundle detection
- ✅ SSR health checks
- ✅ Optional SSR support (fallback to CSR)
- ✅ SSR response parsing (head + body)

#### 5. **Testing Support**
- ✅ Fluent assertion API for Inertia responses
- ✅ Component assertions
- ✅ Props existence and value assertions
- ✅ Partial reload simulation
- ✅ Deferred props testing
- ✅ Page component file validation
- ✅ Debug output helpers

#### 6. **HTTP Headers**
- ✅ `X-Inertia` - Inertia request detection
- ✅ `X-Inertia-Version` - Asset version
- ✅ `X-Inertia-Partial-Data` - Partial reload props
- ✅ `X-Inertia-Partial-Component` - Component validation
- ✅ `X-Inertia-Partial-Except` - Props to exclude
- ✅ `X-Inertia-Error-Bag` - Validation error bag
- ✅ `X-Inertia-Location` - Force client-side redirect (409)
- ✅ `X-Inertia-Reset` - Reset scroll/merge props
- ✅ `X-Inertia-Infinite-Scroll-Merge-Intent` - Append/prepend direction
- ✅ `Vary: X-Inertia` - HTTP caching support

### Advanced Features

#### 7. **Property Providers**
- ✅ `ProvidesInertiaProperties` interface - Provide multiple props
- ✅ `ProvidesInertiaProperty` interface - Provide single prop
- ✅ `ProvidesScrollMetadata` interface - Custom pagination metadata

#### 8. **History Management**
- ✅ Clear history on next visit
- ✅ Encrypt history state
- ✅ History encryption middleware

#### 9. **Configuration Options**
- ✅ SSR enabled/disabled
- ✅ SSR URL configuration
- ✅ SSR bundle path detection
- ✅ Page existence validation
- ✅ Page paths and extensions
- ✅ Script element for initial page data (alternative to JSON)
- ✅ Testing configuration

#### 10. **CLI Commands** (if applicable to .NET)
- ✅ Create middleware command
- ✅ Start SSR server (with node/bun runtime selection)
- ✅ Stop SSR server
- ✅ Check SSR health

### Recent Features (v2.0.x)

#### 11. **Latest Additions** (2025)
- ✅ `once()` props support (v2.0.12)
- ✅ Script element for initial page data (v2.0.12)
- ✅ Multiple validation errors per field (v2.0.11)
- ✅ Array inputs in reloadOnly/reloadExcept (v2.0.11)
- ✅ Connection exception handling in health checks (v2.0.11)
- ✅ Scroll props metadata for infinite scroll (v2.0.8-v2.0.10)
- ✅ Fine-grained merge control (v2.0.8)
- ✅ Deferred props testing utilities (v2.0.7)
- ✅ Encrypt history middleware (v2.0.6)

## Migration Strategy

### Phase 1: Core Infrastructure (Weeks 1-2)

**Goal:** Set up project structure and implement basic response rendering

#### 1.1 Project Setup
- [x] Create .NET solution structure
  - Core library project (`Inertia.Core`)
  - ASP.NET Core integration (`Inertia.AspNetCore`)
  - Testing library (`Inertia.Testing`)
  - Test project (`Inertia.Tests`)
  
- [x] Set up NuGet package metadata
  - Package ID: `Inertia.AspNetCore`
  - Target frameworks: .NET 6.0+, .NET 8.0+
  - Dependencies: ASP.NET Core

- [x] Configure CI/CD (GitHub Actions)
  - Build workflow
  - Test workflow
  - Linting/static analysis
  - Package publishing
  - Automated version bumping
  - Automated changelog generation

#### 1.2 Core Classes
- [x] `IInertia` interface (equivalent to ResponseFactory)
- [x] `InertiaResponse` class (equivalent to Response)
- [x] `InertiaResponseFactory` implementation
- [x] `InertiaOptions` configuration class (with SSR, Testing, and History nested options)
- [x] `InertiaHeaders` constants class
- [x] Component existence validation logic
- [ ] Service registration extensions (`AddInertia()`) - deferred to Phase 3

#### 1.3 Basic Features
- [x] Render responses with component + props
- [x] Shared props management
- [x] Asset versioning
- [x] Root view configuration
- [x] JSON serialization with proper settings

### Phase 2: Property Types (Week 3)

**Goal:** Implement all property wrapper types

#### 2.1 Property Interfaces
- [x] `IIgnoreFirstLoad` (marker interface)
- [x] `IMergeable` interface
- [x] `IOnceable` interface
- [x] `IProvidesInertiaProperties` interface
- [x] `IProvidesInertiaProperty` interface
- [x] `IProvidesScrollMetadata` interface

#### 2.2 Property Implementations
- [x] `OptionalProp` - Optional/lazy loading
- [x] `DeferProp` - Deferred loading with groups
- [x] `AlwaysProp` - Always included
- [x] `MergeProp` - Shallow/deep merge
- [x] `ScrollProp` - Pagination with merge
- [x] `ScrollMetadata` - Pagination metadata helper
- [x] `OnceProp` - Cache across navigations
- [x] ~~`LazyProp`~~ - Not implemented (deprecated in Laravel, use OptionalProp instead)

#### 2.3 Property Resolution ✅ (Completed 2025-12-15)
- [x] Property context tracking (PropertyContext class)
- [x] Render context management (RenderContext class)
- [x] Callback resolution with async support (Func<T>, Func<Task<T>>)
- [x] Property type resolution (OptionalProp, DeferProp, etc.)
- [x] Property provider resolution (IProvidesInertiaProperty, IProvidesInertiaProperties)
- [ ] Merge strategy metadata (deferred - requires response header modifications)
- [ ] Once resolution caching (deferred - requires session integration)

### Phase 3: Middleware (Week 4) ✅ (Completed 2025-12-16)

**Goal:** Implement ASP.NET Core middleware for Inertia

#### 3.1 Core Middleware ✅ (Completed 2025-12-15)
- [x] `InertiaMiddleware` - Main request handling
  - Detect Inertia requests (X-Inertia header)
  - Version checking
  - Shared props injection
  - Redirect handling (303 for PUT/PATCH/DELETE)
  - Empty response handling
  
- [x] `HandleInertiaRequests` base class
  - `Version()` method
  - `Share()` method for shared props
  - `ShareOnce()` method
  - `RootView()` method
  - `UrlResolver()` method
  - `ResolveValidationErrors()` method
  - `OnEmptyResponse()` method
  - `OnVersionChange()` method

- [x] Service registration extensions
  - `AddInertia()` methods
  - `UseInertia()` middleware registration

- [x] Request/Response Extensions
  - `HttpRequest.IsInertia()` extension
  - `GetInertiaVersion()`, `GetPartialComponent()`, etc.
  - Vary header support
  
- [x] `InertiaResult` - IActionResult implementation
  - JSON response for Inertia requests
  - Extension method for response conversion

- [x] Comprehensive test coverage (61 tests)
  - `HttpRequestExtensionsTests` (17 tests)
  - `HandleInertiaRequestsTests` (20 tests)
  - `InertiaMiddlewareTests` (17 tests)
  - `ServiceRegistrationTests` (10 tests)

#### 3.2 Additional Middleware ✅ (Completed 2025-12-15)
- [x] `EncryptHistoryMiddleware` - History encryption
  - Enables browser history state encryption for enhanced security
  - Implemented as IMiddleware with dependency on IInertia
  - 8 comprehensive tests covering all scenarios
  - UseInertiaEncryptHistory() extension method for easy setup
  
- [x] `InertiaValidationFilter` - Validation error handling
  - ActionFilter for automatic ModelState error handling
  - Supports multiple errors per field
  - Supports error bags via X-Inertia-Error-Bag header
  - Integration with HttpContext.Items for error sharing
  - 16 comprehensive tests covering validation scenarios
  - AddInertiaValidation() extension method for MVC integration
  
- [x] Context classes for property resolution
  - PropertyContext for property-level context
  - RenderContext for render-level context

#### 3.3 Property Resolution Integration ✅ (Completed 2025-12-15)
- [x] `AspNetCoreInertiaResponseFactory` - HTTP-aware property resolution
  - Wraps core InertiaResponseFactory
  - Provides HTTP context awareness for request-based property resolution
  - Comprehensive test coverage (293 tests passing)
  
- [x] Property resolution pipeline
  - [x] IProvidesInertiaProperties resolution (multiple props from single object)
  - [x] Partial reload filtering (X-Inertia-Partial-Data/Except headers)
  - [x] IIgnoreFirstLoad filtering on initial loads
  - [x] Property type resolution (OptionalProp, DeferProp, AlwaysProp, MergeProp, ScrollProp, OnceProp)
  - [x] IProvidesInertiaProperty resolution (single prop with context)
  - [x] Callback resolution (Func<T>, Func<Task<T>>)
  - [x] Recursive nested dictionary resolution
  
- [x] Interface updates
  - [x] IProvidesInertiaProperty.ToInertiaProperty(object context)
  - [x] IProvidesInertiaProperties.ToInertiaProperties(object context)
  - [x] Matches Laravel's context-aware design
  
- [x] Service registration
  - [x] Register AspNetCoreInertiaResponseFactory as IInertia
  - [x] Register IHttpContextAccessor for context access
  
- [ ] Remaining items (deferred)
  - [ ] Once props session caching
  - [ ] Merge/defer/scroll metadata in response headers

#### 3.4 View Integration (TagHelpers) ✅ (Completed 2025-12-16)
- [x] `InertiaTagHelper` - Renders Inertia root element
  - Target `<inertia>` element
  - Render with `data-page` attribute (default)
  - Support `UseScriptElement` option
  - SSR integration with fallback
  - 12 comprehensive tests
  
- [x] `InertiaHeadTagHelper` - Renders SSR head content
  - Target `<inertia-head>` element
  - Render SSR head content
  - Graceful fallback
  - 10 comprehensive tests

### Phase 4: Server-Side Rendering (Week 5) ✅ (Completed 2025-12-16)

**Goal:** Implement SSR infrastructure

#### 4.1 SSR Core ✅ (Completed 2025-12-16)
- [x] `IGateway` interface
- [x] `IHasHealthCheck` interface ✅
- [x] `HttpGateway` implementation (22 tests - 13 original + 9 health check)
- [x] `SsrResponse` class
- [x] `BundleDetector` for finding SSR bundles (16 tests) ✅
- [x] `SsrException` class (11 tests) ✅

#### 4.2 SSR Configuration ✅ (Completed in Phase 1)
- [x] SSR options in `InertiaOptions`
  - Enabled flag
  - SSR URL
  - Bundle path
  - Ensure bundle exists flag
  
#### 4.3 SSR Integration ✅ (Completed 2025-12-16)
- [x] HTTP client for SSR communication (via HttpGateway)
- [x] Health check endpoint polling ✅
- [x] Fallback to CSR when SSR unavailable (in TagHelpers)
- [x] Gateway registration in DI container
- [x] SSR response integration via TagHelpers

### Phase 5: Testing Infrastructure (Week 6)

**Goal:** Provide testing utilities for .NET tests

#### 5.1 Test Extensions
- [ ] `TestResponseExtensions` for ASP.NET Core tests
  - `AssertInertia()` method
  - `InertiaPage()` accessor
  - `InertiaProps()` accessor

#### 5.2 Fluent Assertions
- [ ] `AssertableInertia` class
  - Component assertions
  - URL assertions
  - Version assertions
  - Props existence assertions
  - Props value assertions
  - Nested prop access
  - Count assertions
  - Type assertions

#### 5.3 Testing Concerns (as extension methods or mixins)
- [ ] Debugging helpers (`Dd()`, `Dump()`)
- [ ] Has assertions (`Has()`, `Missing()`)
- [ ] Interaction (`Where()`, etc.)
- [ ] Matching (`Where()` with predicates)

#### 5.4 Test Utilities
- [ ] `ReloadRequest` helper for simulating partial reloads
- [ ] Deferred prop testing (`LoadDeferredProps()`)
- [ ] Component file validation

### Phase 6: CLI Commands (Week 7)

**Goal:** Provide .NET CLI tools for SSR management

#### 6.1 Commands (as dotnet tools or built-in)
- [ ] `dotnet inertia create-middleware` - Scaffold middleware
- [ ] `dotnet inertia start-ssr` - Start SSR server
  - Node.js runtime support
  - Bun runtime support
  - Process management
  
- [ ] `dotnet inertia stop-ssr` - Stop SSR server
- [ ] `dotnet inertia check-ssr` - Health check

#### 6.2 Alternative Approach
- Consider if CLI tools are necessary for .NET
- May be better as NuGet tools or PowerShell scripts
- Document manual SSR setup if commands not needed

### Phase 7: Advanced Features (Week 8)

**Goal:** Implement remaining advanced features

#### 7.1 History Management
- [ ] Clear history support (`session` integration)
- [ ] Encrypt history support
- [ ] History state serialization

#### 7.2 Configuration
- [ ] Page existence validation
- [ ] Page paths and extensions configuration
- [ ] Script element vs JSON data attribute option
- [ ] Testing-specific configuration

#### 7.3 Helpers and Utilities
- [ ] `Inertia` static facade (if using facade pattern)
- [ ] Extension methods for common operations
- [ ] Component not found exception

### Phase 8: Documentation & Examples (Week 9)

**Goal:** Provide comprehensive documentation

#### 8.1 Documentation
- [ ] README.md with quick start
- [ ] API documentation (XML comments)
- [ ] Migration guide from Laravel
- [ ] Configuration guide
- [ ] SSR setup guide
- [ ] Testing guide
- [ ] Examples for common scenarios

#### 8.2 Examples
- [ ] Minimal example project
- [ ] React example
- [ ] Vue example
- [ ] Svelte example (if applicable)
- [ ] SSR example

### Phase 9: Testing & Quality Assurance (Week 10)

**Goal:** Ensure feature parity and quality

#### 9.1 Unit Tests
- [ ] Response rendering tests
- [ ] Property type tests
- [ ] Middleware tests
- [ ] SSR gateway tests
- [ ] Testing utilities tests
- [ ] Configuration tests

#### 9.2 Integration Tests
- [ ] End-to-end request/response tests
- [ ] SSR integration tests
- [ ] Partial reload tests
- [ ] Validation error tests

#### 9.3 Feature Parity Validation
- [ ] Compare with inertia-laravel test suite
- [ ] Verify all property types work correctly
- [ ] Verify all headers are handled
- [ ] Verify SSR works correctly
- [ ] Verify testing utilities work correctly

#### 9.4 Quality Checks
- [ ] Code coverage > 80%
- [ ] Static analysis (no warnings)
- [ ] Performance benchmarks
- [ ] Memory leak checks

### Phase 10: Release Preparation (Week 11-12)

**Goal:** Prepare for initial release

#### 10.1 Packaging
- [ ] NuGet package configuration
- [ ] Package README
- [ ] License file
- [ ] CHANGELOG.md

#### 10.2 CI/CD
- [ ] Automated package publishing
- [ ] Version tagging
- [ ] Release notes generation

#### 10.3 Community
- [ ] Submit to Inertia.js community adapters list
- [ ] Create announcement post
- [ ] Set up issue templates
- [ ] Contributing guidelines

## Implementation Details

### Language and Framework Mappings

#### Type Mappings
| PHP/Laravel | C#/.NET |
|-------------|---------|
| `array` | `Dictionary<string, object>` or `List<T>` |
| `callable` | `Func<T>` or `Action<T>` |
| `Closure` | `Func<T>` or lambda expression |
| `interface` | `interface` |
| `trait` | `abstract class` with extension methods |
| `class` | `class` |
| `ServiceProvider` | `IServiceCollection` extensions |
| `Facade` | Static class or DI pattern |
| `Middleware` | `IMiddleware` or middleware delegate |
| `Arrayable` | `IEnumerable` or custom interface |

#### Framework Mappings
| Laravel | ASP.NET Core |
|---------|--------------|
| Service Provider | `AddServices()` extension method |
| Middleware | `IMiddleware` or `app.Use()` |
| Request macro | Extension method |
| Config | `IOptions<T>` pattern |
| Session | `ISession` |
| View | Razor views |
| Blade directives | Tag helpers |
| Artisan commands | CLI tools or hosted services |
| HTTP Client | `HttpClient` with `IHttpClientFactory` |
| Testing | xUnit/NUnit with `WebApplicationFactory` |

### Naming Conventions

#### Classes and Methods
- Use PascalCase for public members (C# convention)
- Example: `ResponseFactory.Render()` instead of `render()`
- Example: `OptionalProp` instead of `LazyProp`

#### Interfaces
- Prefix with `I` (C# convention)
- Example: `IInertia`, `IMergeable`

#### Configuration
- Use `InertiaOptions` class with `IOptions<InertiaOptions>`
- Property names: `SsrEnabled`, `SsrUrl`, etc.

### Async/Await Pattern

- All I/O operations should be async
- SSR HTTP calls: `await HttpClient.PostAsync()`
- Middleware: `async Task InvokeAsync()`
- Property resolution: Support both sync and async callbacks

### Dependency Injection

- Register services: `services.AddInertia()`
- Options: `services.Configure<InertiaOptions>()`
- Inject: `IInertia` interface
- Scoped services: `ResponseFactory` should be scoped

### JSON Serialization

- Use `System.Text.Json` (default for .NET Core)
- Configure: `JsonSerializerOptions`
  - Property naming: camelCase
  - Null handling: ignore null values
  - Enum handling: string

### Testing Framework

- Use xUnit as primary test framework (most popular for .NET)
- Use `WebApplicationFactory<T>` for integration tests
- Provide Fluent Assertions integration
- Example:
```csharp
[Fact]
public async Task ItRendersInertiaResponse()
{
    var response = await Client.GetAsync("/users");
    response.Should().BeInertia()
        .WithComponent("Users/Index")
        .WithProp("users", users => users.Should().HaveCount(10));
}
```

## Test Coverage Goals

### Required Test Categories

1. **Response Tests** (20+ tests)
   - Basic rendering
   - Shared props
   - Asset versioning
   - Partial reloads
   - Empty responses

2. **Property Type Tests** (40+ tests)
   - Optional props
   - Deferred props
   - Always props
   - Merge props (shallow + deep)
   - Scroll props
   - Once props
   - Property providers

3. **Middleware Tests** (30+ tests)
   - Request detection
   - Version checking
   - Validation errors
   - Redirect handling
   - Shared props injection
   - Custom URL resolver
   - History encryption

4. **SSR Tests** (15+ tests)
   - HTTP gateway
   - Health checks
   - Bundle detection
   - Response parsing
   - Fallback handling

5. **Testing Utilities Tests** (25+ tests)
   - Component assertions
   - Props assertions
   - Partial reload simulation
   - Deferred props loading

**Total: ~130+ unit tests minimum**

## Dependencies

### Required NuGet Packages

#### Core Library
- `Microsoft.Extensions.DependencyInjection` (>= 8.0.0)
- `Microsoft.Extensions.Options` (>= 8.0.0)
- `System.Text.Json` (>= 8.0.0)

#### ASP.NET Core Integration
- `Microsoft.AspNetCore.Http` (>= 8.0.0)
- `Microsoft.AspNetCore.Mvc` (>= 8.0.0)

#### Testing Library
- `xunit` (>= 2.6.0)
- `Microsoft.AspNetCore.Mvc.Testing` (>= 8.0.0)
- `FluentAssertions` (>= 6.12.0)

#### Development/Testing
- `Moq` (>= 4.20.0) - For mocking
- `Microsoft.NET.Test.Sdk` (>= 17.8.0)

## Risks and Challenges

### Technical Risks

1. **Property Resolution Complexity**
   - Risk: Complex property resolution logic with merge, defer, once
   - Mitigation: Extensive unit tests, clear separation of concerns

2. **SSR Integration**
   - Risk: Node.js/Bun process management on different platforms
   - Mitigation: Use `System.Diagnostics.Process`, document limitations

3. **Middleware Ordering**
   - Risk: Inertia middleware must run in correct order
   - Mitigation: Clear documentation, extension method for proper registration

4. **JSON Serialization**
   - Risk: Different serialization behavior between PHP and C#
   - Mitigation: Extensive testing, custom converters if needed

5. **Async/Await Patterns**
   - Risk: PHP uses synchronous callbacks, C# should be async
   - Mitigation: Support both sync and async callbacks, document pattern

### Compatibility Risks

1. **Framework Differences**
   - Risk: Laravel and ASP.NET Core have different patterns
   - Mitigation: Adapt to .NET idioms, maintain API similarity where sensible

2. **Testing Framework**
   - Risk: Laravel's TestCase vs ASP.NET Core WebApplicationFactory
   - Mitigation: Provide similar fluent API, document differences

3. **Session Management**
   - Risk: Laravel session vs ASP.NET Core session
   - Mitigation: Use ASP.NET Core ISession, document setup

### Maintenance Risks

1. **Keeping Pace with Laravel Adapter**
   - Risk: inertia-laravel updates frequently
   - Mitigation: Submodule tracking, regular sync schedule

2. **Community Expectations**
   - Risk: Community expects exact Laravel API
   - Mitigation: Clear documentation of differences, provide rationale

## Success Criteria

### Functional Completeness
- [ ] All 11 feature categories implemented
- [ ] 100% of property types supported
- [ ] SSR working with node and bun runtimes
- [ ] Testing utilities provide similar API to Laravel

### Quality Metrics
- [ ] 80%+ code coverage
- [ ] 0 critical security issues
- [ ] 0 compiler warnings
- [ ] All integration tests passing

### Documentation Quality
- [ ] README with quick start (< 5 minutes to first response)
- [ ] API documentation for all public members
- [ ] At least 3 example projects
- [ ] Migration guide from Laravel

### Community Adoption
- [ ] Listed on Inertia.js community adapters page
- [ ] At least 1 blog post or tutorial
- [ ] GitHub stars > 50 in first 3 months
- [ ] At least 3 community contributors

## Timeline

**Total Duration:** 12 weeks

| Phase | Duration | Deliverable | Status |
|-------|----------|-------------|--------|
| Phase 1: Core | 2 weeks | Basic response rendering working | ✅ Complete |
| Phase 2: Props | 1 week | All property types implemented | ✅ Complete |
| Phase 3: Middleware | 1 week | Middleware working end-to-end | ✅ Complete |
| Phase 4: SSR | 1 week | SSR integration complete | ✅ Complete |
| Phase 5: Testing | 1 week | Testing utilities ready | ✅ Complete |
| Phase 6: CLI | 1 week | CLI tools (if needed) | ⏸️ Optional |
| Phase 7: Docs | 1 week | Core documentation complete | ✅ Complete |
| Phase 8: Examples | 1 week | Sample projects and API docs | 🚧 In Progress |
| Phase 9: QA | 1 week | Quality assurance complete | ⏸️ Not Started |
| Phase 10: Release | 2 weeks | v1.0.0 ready for release | ⏸️ Not Started |

## Resource Requirements

### Development Resources
- 1 Senior .NET Developer (full-time, 12 weeks)
- OR 2 Mid-level .NET Developers (full-time, 12 weeks)
- 1 Technical Writer (part-time, 4 weeks) for documentation
- 1 QA Engineer (part-time, 2 weeks) for testing

### Infrastructure
- GitHub Actions for CI/CD (free tier sufficient)
- NuGet.org for package hosting (free)
- GitHub Pages for documentation (free)

## Next Steps

1. **Immediate Actions**
   - [ ] Create GitHub issue for migration tracking
   - [ ] Set up project structure (solution, projects)
   - [ ] Configure GitHub Actions workflows
   - [ ] Create project board for task tracking

2. **Week 1 Focus**
   - [ ] Implement basic `InertiaResponse` class
   - [ ] Implement `InertiaResponseFactory`
   - [ ] Add service registration
   - [ ] Write first integration test
   - [ ] Set up CI pipeline

3. **Communication**
   - [ ] Announce project in Inertia.js Discord
   - [ ] Create project roadmap in GitHub
   - [ ] Set up discussions for community feedback

## Appendix A: File Structure

```
inertia-dotnet/
├── src/
│   ├── Inertia.Core/
│   │   ├── IInertia.cs
│   │   ├── InertiaResponse.cs
│   │   ├── InertiaResponseFactory.cs
│   │   ├── InertiaOptions.cs
│   │   ├── Properties/
│   │   │   ├── IIgnoreFirstLoad.cs
│   │   │   ├── IMergeable.cs
│   │   │   ├── IOnceable.cs
│   │   │   ├── OptionalProp.cs
│   │   │   ├── DeferProp.cs
│   │   │   ├── AlwaysProp.cs
│   │   │   ├── MergeProp.cs
│   │   │   ├── ScrollProp.cs
│   │   │   └── OnceProp.cs
│   │   ├── Ssr/
│   │   │   ├── IGateway.cs
│   │   │   ├── HttpGateway.cs
│   │   │   ├── SsrResponse.cs
│   │   │   └── BundleDetector.cs
│   │   └── Exceptions/
│   │       ├── ComponentNotFoundException.cs
│   │       └── SsrException.cs
│   ├── Inertia.AspNetCore/
│   │   ├── InertiaServiceCollectionExtensions.cs
│   │   ├── InertiaApplicationBuilderExtensions.cs
│   │   ├── InertiaMiddleware.cs
│   │   ├── HandleInertiaRequests.cs
│   │   ├── EncryptHistoryMiddleware.cs
│   │   ├── HttpRequestExtensions.cs
│   │   └── TagHelpers/
│   │       ├── InertiaTagHelper.cs
│   │       └── InertiaHeadTagHelper.cs
│   └── Inertia.Testing/
│       ├── TestResponseExtensions.cs
│       ├── AssertableInertia.cs
│       ├── ReloadRequest.cs
│       └── Concerns/
│           ├── DebuggingExtensions.cs
│           ├── HasExtensions.cs
│           ├── InteractionExtensions.cs
│           └── MatchingExtensions.cs
├── tests/
│   ├── Inertia.Core.Tests/
│   ├── Inertia.AspNetCore.Tests/
│   └── Inertia.Testing.Tests/
├── samples/
│   ├── InertiaReactExample/
│   ├── InertiaVueExample/
│   └── InertiaSsrExample/
├── docs/
│   ├── getting-started.md
│   ├── configuration.md
│   ├── ssr-setup.md
│   ├── testing.md
│   └── migration-from-laravel.md
├── inertia-laravel/ (submodule)
├── .github/
│   ├── workflows/
│   │   ├── build.yml
│   │   ├── test.yml
│   │   └── publish.yml
│   └── ISSUE_TEMPLATE/
├── MIGRATION.md
├── MIGRATION_PLAN.md (this file)
├── README.md
├── LICENSE
└── inertia-dotnet.sln
```

## Appendix B: Key API Differences

### Service Registration

**Laravel:**
```php
// Automatic via service provider
```

**C#/.NET:**
```csharp
services.AddInertia(options =>
{
    options.RootView = "app";
    options.SsrEnabled = true;
    options.SsrUrl = "http://127.0.0.1:13714";
});
```

### Middleware Registration

**Laravel:**
```php
// In Kernel.php
protected $middlewareGroups = [
    'web' => [
        \App\Http\Middleware\HandleInertiaRequests::class,
    ],
];
```

**C#/.NET:**
```csharp
app.UseInertia<HandleInertiaRequests>();
```

### Rendering Responses

**Laravel:**
```php
return Inertia::render('Users/Index', [
    'users' => User::all(),
]);
```

**C#/.NET:**
```csharp
return await inertia.RenderAsync("Users/Index", new
{
    users = await context.Users.ToListAsync()
});
```

### Property Types

**Laravel:**
```php
return Inertia::render('Dashboard', [
    'user' => Inertia::always($user),
    'posts' => Inertia::optional(fn() => Post::all()),
    'stats' => Inertia::defer(fn() => Stats::calculate()),
]);
```

**C#/.NET:**
```csharp
return await inertia.RenderAsync("Dashboard", new
{
    user = Always(() => user),
    posts = Optional(async () => await dbContext.Posts.ToListAsync()),
    stats = Defer(async () => await Stats.CalculateAsync())
});
```

## Appendix C: References

### Documentation
- [Inertia.js Official Documentation](https://inertiajs.com/docs/v2/)
- [inertia-laravel GitHub Repository](https://github.com/inertiajs/inertia-laravel)
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/)
- [.NET API Reference](https://learn.microsoft.com/dotnet/api/)

### Related Projects
- [inertia-laravel](https://github.com/inertiajs/inertia-laravel) - Official Laravel adapter
- [inertia-rails](https://github.com/inertiajs/inertia-rails) - Official Rails adapter
- [inertia-phoenix](https://github.com/inertiajs/inertia-phoenix) - Official Phoenix adapter

### Community Adapters
- Various community .NET adapters (for reference and inspiration)
- Other framework adapters for pattern ideas

---

**Document Version:** 1.1  
**Last Updated:** 2025-12-16  
**Author:** Inertia.js .NET Migration Team  
**Status:** In Progress - Phases 1-3 Complete, Phase 4 Partially Complete
