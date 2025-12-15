# Deferred Items from Phase 3.3: Property Resolution Integration

## Overview

During the implementation of Phase 3.3 (Property Resolution Integration), two features were intentionally deferred to future work as they require additional infrastructure that is not yet available in the project.

## Deferred Features

### 1. Once Props Session Caching

**Description:**  
Once props should be resolved once and cached in the session across navigations, similar to Laravel's implementation.

**Current Status:**  
- The `OnceProp` class exists and the `IOnceable` interface is implemented
- Property resolution pipeline supports once props
- Session caching is NOT implemented

**Laravel Reference:**
```php
// Cached in session across navigations
$props = [
    'translations' => Inertia::once(fn() => Lang::get('messages'))
];
```

**What's Needed:**
- Session integration in ASP.NET Core
- Session key management for once props
- Cache invalidation strategy
- Support for the `X-Inertia-Reset` header to force re-resolution

**Implementation Location:**
- `src/Inertia.AspNetCore/AspNetCoreInertiaResponseFactory.cs` - Add session caching logic
- Update `ResolvePropertyInstancesAsync` to check session cache for once props
- Store resolved once prop values in session with appropriate keys

**Estimated Effort:** Medium (requires session integration design)

**Priority:** Medium - Nice to have for optimization but not critical for core functionality

---

### 2. Merge/Defer/Scroll Metadata in Response Headers

**Description:**  
The response should include metadata about merge, defer, and scroll props in response headers/body to inform the client how to handle them properly.

**Current Status:**  
- Property types (MergeProp, DeferProp, ScrollProp) exist and work
- Properties are resolved correctly
- Metadata is NOT added to the response

**Laravel Reference:**
```php
// Response includes metadata like:
[
    'component' => 'Users/Index',
    'props' => [...],
    'mergeProps' => ['stats'], // Props that should be merged
    'deferredProps' => ['analytics'], // Props that are deferred
    // ... other metadata
]
```

**What's Needed:**
- Extend `InertiaResponse` class to include metadata fields
- Add logic to extract merge/defer/scroll prop information during resolution
- Update response serialization to include this metadata
- Ensure metadata is properly sent to the client

**Implementation Location:**
- `src/Inertia.Core/InertiaResponse.cs` - Add metadata properties
- `src/Inertia.AspNetCore/AspNetCoreInertiaResponseFactory.cs` - Collect metadata during resolution
- `src/Inertia.AspNetCore/InertiaResult.cs` - Include metadata in JSON response

**Estimated Effort:** Medium (requires response structure modifications)

**Priority:** Low-Medium - Useful for advanced features but client can work without explicit metadata

---

## Additional Context

### Why These Were Deferred

1. **Session Integration** - The project doesn't yet have a comprehensive session management strategy in place
2. **Response Structure Changes** - Adding metadata requires careful consideration of the response format and backward compatibility
3. **Client Compatibility** - Need to ensure any metadata changes are compatible with the Inertia.js client library
4. **Scope Management** - Phase 3.3 was already substantial; deferring these keeps the PR focused and reviewable

### Feature Parity Impact

With these items deferred, we have **8 out of 10 features** implemented (80% feature parity with Laravel adapter).

The deferred features represent:
- **Once props session caching** - Optimization feature
- **Metadata headers** - Enhancement for client-side behavior

Core functionality is complete and production-ready without these features.

### When to Implement

**Recommended Timeline:**
1. **Once Props Session Caching** - Phase 4 or 5 (when session management is addressed)
2. **Merge/Defer/Scroll Metadata** - Phase 6 or as needed based on client requirements

### References

- Laravel adapter implementation: `inertia-laravel/src/Response.php`
- Current .NET implementation: `src/Inertia.AspNetCore/AspNetCoreInertiaResponseFactory.cs`
- Phase 3.3 completion summary: `PHASE_3.3_COMPLETION_SUMMARY.md`

---

## Issue Tracking

**Labels:**
- `enhancement`
- `phase-4` or `phase-5`
- `session-management`
- `deferred`

**Related PRs:**
- Phase 3.3: Property Resolution Integration (current PR)

**Acceptance Criteria:**

For Once Props Session Caching:
- [ ] Once props are cached in session after first resolution
- [ ] Cached values are reused across navigations
- [ ] X-Inertia-Reset header forces re-resolution
- [ ] Session keys are properly scoped and managed
- [ ] Tests verify caching behavior

For Metadata Headers:
- [ ] Response includes `mergeProps` array
- [ ] Response includes `deferredProps` array  
- [ ] Response includes scroll metadata when applicable
- [ ] Client can consume metadata correctly
- [ ] Tests verify metadata is included
