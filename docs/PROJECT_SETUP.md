# Project Setup Documentation

This document describes the solution structure and project setup for inertia-dotnet.

## Solution Structure

```
inertia-dotnet/
├── src/                          # Source code projects
│   ├── Inertia.Core/            # Core library (interfaces, types)
│   ├── Inertia.AspNetCore/      # ASP.NET Core integration
│   └── Inertia.Testing/         # Testing utilities
├── tests/                        # Test projects
│   ├── Inertia.Tests/           # Tests for Inertia.Core
│   ├── Inertia.AspNetCore.Tests/# Tests for ASP.NET Core integration
│   └── Inertia.Testing.Tests/   # Tests for testing utilities
├── samples/                      # Sample applications (planned)
├── docs/                         # Documentation
└── inertia-dotnet.sln           # Solution file
```

## Projects

### Inertia.Core

**Purpose:** Core library providing interfaces, types, and base functionality for Inertia.js

**Target Framework:** .NET 8.0

**Key Dependencies:**
- `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.0
- `Microsoft.Extensions.Options` 8.0.0
- `System.Text.Json` 8.0.5

**Features:**
- Core interfaces (IInertia)
- Response types (InertiaResponse)
- Property types (OptionalProp, DeferProp, etc.)
- Configuration options (InertiaOptions)
- SSR gateway interfaces

### Inertia.AspNetCore

**Purpose:** ASP.NET Core integration for Inertia.js

**Target Framework:** .NET 8.0

**Key Dependencies:**
- `Inertia.Core` (project reference)
- `Microsoft.AspNetCore.App` (framework reference)

**Features:**
- Middleware (InertiaMiddleware, HandleInertiaRequests)
- Service registration extensions (AddInertia)
- Tag helpers (InertiaTagHelper)
- HTTP extensions
- Validation integration

### Inertia.Testing

**Purpose:** Testing utilities for Inertia.js applications

**Target Framework:** .NET 8.0

**Key Dependencies:**
- `Inertia.Core` (project reference)
- `Microsoft.AspNetCore.App` (framework reference)
- `xunit.abstractions` 2.0.3
- `FluentAssertions` 6.12.0

**Features:**
- Fluent assertion API (AssertableInertia)
- Test response extensions
- Partial reload helpers (ReloadRequest)
- Deferred props testing utilities

## Test Projects

All test projects use:
- **xUnit** as the test framework
- **FluentAssertions** for fluent assertion syntax
- **coverlet.collector** for code coverage

### Inertia.Tests

Tests for Inertia.Core functionality.

### Inertia.AspNetCore.Tests

Tests for ASP.NET Core integration. Includes:
- `Microsoft.AspNetCore.Mvc.Testing` 8.0.0 for integration testing

### Inertia.Testing.Tests

Tests for testing utilities.

## Building the Solution

```bash
# Restore dependencies
dotnet restore

# Build all projects
dotnet build

# Run tests
dotnet test

# Pack NuGet packages
dotnet pack --configuration Release --output ./artifacts
```

## CI/CD

### GitHub Actions Workflows

#### build.yml
- Runs on push/PR to main, develop, and copilot branches
- Builds solution in Release mode
- Runs tests
- Creates NuGet packages
- Uploads artifacts

#### test.yml
- Cross-platform testing on Ubuntu, Windows, and macOS
- Runs tests on all platforms
- Uploads test results

## Configuration

All projects are configured with:
- **Target Framework:** .NET 8.0
- **Language Version:** C# 12
- **Nullable Reference Types:** Enabled
- **Implicit Usings:** Enabled
- **XML Documentation:** Generated for all public APIs

## NuGet Package Metadata

All packages include:
- **License:** MIT
- **Project URL:** https://github.com/idotta/inertia-dotnet
- **Repository URL:** https://github.com/idotta/inertia-dotnet
- **Tags:** inertia, inertiajs, spa, ssr, dotnet, aspnetcore
- **README:** Included in package

## Development Guidelines

1. **Code Style:**
   - Use file-scoped namespaces
   - Enable nullable reference types
   - Follow .NET naming conventions (PascalCase for public members)

2. **Testing:**
   - Write tests for all new features
   - Use xUnit and FluentAssertions
   - Aim for >80% code coverage

3. **Documentation:**
   - Add XML documentation comments to all public APIs
   - Update documentation when adding features
   - Include examples in documentation

4. **Security:**
   - Keep dependencies up to date
   - Run security scans on packages
   - Fix vulnerabilities promptly

## Next Steps

See [IMPLEMENTATION_CHECKLIST.md](../IMPLEMENTATION_CHECKLIST.md) for the implementation roadmap.

Key upcoming tasks:
1. Implement IInertia interface
2. Create InertiaResponse class
3. Implement property types
4. Create middleware
5. Add SSR support
6. Build testing utilities
