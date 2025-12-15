# Migration Guidelines

This document describes the process for migrating features from [inertia-laravel](https://github.com/inertiajs/inertia-laravel) to inertia-dotnet.

## Overview

The goal is to keep inertia-dotnet feature-complete and on par with inertia-laravel. The inertia-laravel repository is included as a submodule for reference and tracking purposes.

## Migration Process

### 1. Update the Submodule

Periodically check for updates to the inertia-laravel submodule:

```bash
cd inertia-laravel
git fetch origin
git log HEAD..origin/main --oneline
cd ..
```

To update to the latest version:

```bash
git submodule update --remote inertia-laravel
git add inertia-laravel
git commit -m "Update inertia-laravel submodule to [version/commit]"
```

### 2. Review Changes

After updating the submodule, review what changed:

```bash
cd inertia-laravel
git log --oneline --graph --decorate -20
git show [commit-hash]
cd ..
```

Key areas to review:
- **Source code** (`src/` directory) - Core functionality
- **Configuration** (`config/` directory) - Configuration options
- **Tests** (`tests/` directory) - Test coverage and expected behavior
- **Documentation** (`README.md`, `CHANGELOG.md`) - API changes and new features
- **GitHub Actions** (`.github/` directory) - CI/CD workflows

### 3. Migrate Features to C#

When migrating features from PHP/Laravel to C#/.NET:

#### Language Translation
- **PHP classes** → **C# classes**
- **Laravel service providers** → **.NET service registration** (typically in `ServiceCollection` extensions)
- **Laravel middleware** → **ASP.NET Core middleware**
- **Laravel facades** → **Dependency injection** patterns
- **PHP arrays** → **C# collections** (List, Dictionary, etc.)
- **Blade templates** → **Razor views** (when applicable)

#### Framework Differences
- **Laravel routing** → **ASP.NET Core routing** (attribute-based or conventional)
- **Laravel validation** → **ASP.NET Core ModelState/FluentValidation**
- **Laravel collections** → **LINQ** and standard C# collections
- **Laravel helpers** → **Extension methods** or utility classes
- **Composer packages** → **NuGet packages**

#### Core Principles
1. **Same features** - Provide equivalent functionality
2. **Different implementation** - Use idiomatic C# and .NET patterns
3. **Maintain API compatibility** - Keep similar method names and signatures where sensible
4. **Follow .NET conventions** - Use PascalCase for public members, async/await patterns, etc.

### 4. Migrate Tests

Tests are crucial for ensuring feature parity:

1. Review PHP tests in `inertia-laravel/tests/`
2. Translate test logic to C# using xUnit/NUnit/MSTest
3. Ensure equivalent test coverage
4. Adapt tests to .NET testing patterns (e.g., using Moq for mocking)

### 5. Update Documentation

Documentation should be updated to reflect .NET-specific implementation:

1. Translate Laravel-specific examples to C#/.NET
2. Update code snippets with C# syntax
3. Reference .NET packages instead of Composer packages
4. Document any .NET-specific configuration or setup requirements

### 6. Migrate GitHub Actions

GitHub Actions workflows should be adapted for .NET:

1. Review workflows in `inertia-laravel/.github/workflows/`
2. Replace PHP/Composer actions with .NET/NuGet actions
3. Use `actions/setup-dotnet` instead of `shivammathur/setup-php`
4. Adapt build, test, and publish steps for .NET CLI (`dotnet build`, `dotnet test`, `dotnet pack`)

## Example Migration

### PHP/Laravel Code
```php
class InertiaServiceProvider extends ServiceProvider
{
    public function register()
    {
        $this->app->singleton('inertia', function ($app) {
            return new Inertia();
        });
    }
}
```

### C#/.NET Equivalent
```csharp
public static class InertiaServiceCollectionExtensions
{
    public static IServiceCollection AddInertia(this IServiceCollection services)
    {
        services.AddSingleton<IInertia, Inertia>();
        return services;
    }
}
```

## Tracking Migration Status

When features are migrated, document them in the project:

1. Update CHANGELOG.md with migrated features
2. Create GitHub issues for pending migrations
3. Reference the inertia-laravel commit/version that was migrated
4. Note any differences or limitations in the .NET implementation

## Continuous Synchronization

To maintain feature parity:

1. Set up automated checks for submodule updates (see GitHub Actions workflow)
2. Review and migrate updates regularly (recommended: monthly or quarterly)
3. Monitor inertia-laravel releases and changelog
4. Participate in the Inertia.js community to stay informed of upcoming changes

## Resources

- [inertia-laravel GitHub Repository](https://github.com/inertiajs/inertia-laravel)
- [Inertia.js Documentation](https://inertiajs.com/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
