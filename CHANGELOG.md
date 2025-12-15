# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Phase 1: Core Infrastructure (In Progress)

#### Added
- Project structure with Inertia.Core, Inertia.AspNetCore, and Inertia.Testing
- `IInertia` interface for creating Inertia responses
- `InertiaResponse` class with JSON serialization
- `InertiaResponseFactory` implementation
- `InertiaOptions` configuration with SSR, Testing, and History nested options
- `InertiaHeaders` constants for HTTP headers
- Comprehensive test suite (55+ tests)
- CI/CD workflows for build, test, lint, and publish
- Automated version bumping and changelog generation
- NuGet package configuration
- Samples directory structure
- Comprehensive documentation (MIGRATION_PLAN.md, IMPLEMENTATION_CHECKLIST.md, API_MAPPING.md)

#### Planned for Phase 2: Property Types
- Optional props (lazy loading)
- Deferred props (async loading)
- Always props (bypass partial reload filtering)
- Merge props (shallow and deep merge)
- Scroll props (infinite scroll pagination)
- Once props (cache across navigations)

#### Planned for Phase 3: ASP.NET Core Integration
- Service registration extensions
- Inertia middleware
- HandleInertiaRequests base class
- Request/response extensions
- TagHelpers for Razor views
- Validation integration

#### Planned for Phase 4: Server-Side Rendering
- SSR gateway implementation
- HTTP gateway for Node.js/Bun
- Bundle detection
- Health check support

#### Planned for Phase 5: Testing Infrastructure
- Fluent assertion API
- Test response extensions
- Partial reload simulation
- Deferred props testing

### Project Tracking
- 📍 **Current Phase:** Phase 1 (Core Infrastructure)
- ✅ **Completed:** ~75% of Phase 1
- 🎯 **Next Up:** Complete Phase 1, then start Phase 2 (Property Types)
- 🚀 **Target Release:** v1.0.0 (Q1 2026)

---

## Version History

This project is currently in pre-release development. The first stable release will be v1.0.0.

### Pre-release Versions
- v0.1.0 - Initial project setup and planning (December 2025)
