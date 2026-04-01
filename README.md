# inertia-dotnet

[![NuGet](https://img.shields.io/nuget/v/Inertia.AspNetCore.svg)](https://www.nuget.org/packages/Inertia.AspNetCore/)
[![Build Status](https://github.com/idotta/inertia-dotnet/workflows/Build/badge.svg)](https://github.com/idotta/inertia-dotnet/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A .NET adapter for [Inertia.js](https://inertiajs.com/) — build modern single-page applications using classic server-side routing and controllers. Feature-complete port of [inertia-laravel](https://github.com/inertiajs/inertia-laravel) v3.0.1 to ASP.NET Core 10 / C# 14.

## Features

- Full [Inertia.js v3 protocol](https://inertiajs.com/docs/v3/core-concepts/the-protocol) support
- All property types: `Always`, `Optional`, `Defer`, `Merge`, `DeepMerge`, `Once`, `Scroll`
- Partial reloads with dot-notation filtering
- Server-side rendering (SSR) with Vite hot reload support
- History encryption, flash data, shared props
- Dual MVC (`IActionResult`) and minimal API (`IResult`) support
- Exception handling with custom error pages
- Testing package with `AssertableInertia` assertions
- 919 tests, verified side-by-side against inertia-laravel

## Quick Start

### 1. Install

```bash
dotnet add package Inertia.AspNetCore
```

### 2. Register services

```csharp
builder.Services.AddInertia(options =>
{
    options.RootView = "~/Views/App.cshtml";
});
```

### 3. Add middleware

```csharp
app.UseInertia();
```

### 4. Create your root view (`Views/App.cshtml`)

```html
<!DOCTYPE html>
<html>
<head>
    <inertia-head></inertia-head>
</head>
<body>
    <inertia-app></inertia-app>
    <script src="/js/app.js"></script>
</body>
</html>
```

### 5. Render pages

```csharp
// MVC Controller
public IActionResult Index()
{
    return Inertia.Render("Users/Index", new { Users = users });
}

// Minimal API
app.MapGet("/users", (IInertia inertia) =>
    inertia.Render("Users/Index", new { Users = users }));

// Route-level shorthand
app.MapInertia("/about", "About");
```

## Property Types

```csharp
inertia.Render("Dashboard", new Dictionary<string, object?>
{
    ["users"]    = Prop.Defer(() => db.GetUsersAsync()),           // Lazy-loaded after initial render
    ["stats"]    = Prop.Always(() => GetStats()),                  // Always included, even in partials
    ["settings"] = Prop.Optional(() => GetSettings()),             // Only on explicit partial request
    ["items"]    = Prop.Merge(() => GetPage(page)),                // Merged with client-side data
    ["feed"]     = Prop.Scroll(() => GetFeed(page), "data"),      // Infinite scroll pagination
    ["config"]   = Prop.Once(() => LoadConfig()),                  // Cached on client across navigations
});
```

## Shared Data & Flash

```csharp
// In middleware configuration
options.SharedPropsProvider = (ctx, sp) => new Dictionary<string, object?>
{
    ["auth"] = new { User = ctx.User.Identity?.Name },
};

// Per-request
inertia.Share("locale", "en");
inertia.Flash("message", "Settings saved!");
```

## Configuration

Configure via `AddInertia()` or `appsettings.json` under the `"Inertia"` section:

| Option | Default | Description |
|--------|---------|-------------|
| `RootView` | `~/Views/App.cshtml` | Razor view for initial page loads |
| `EncryptHistory` | `false` | Encrypt browser history state |
| `SsrEnabled` | `true` | Enable server-side rendering |
| `SsrUrl` | `http://127.0.0.1:13714` | SSR server URL |
| `VersionProvider` | `null` | `Func<HttpContext, string>` for asset versioning |
| `ValidationErrorProvider` | `null` | `Func<HttpContext, string?, IDictionary>` for validation errors |
| `SharedPropsProvider` | `null` | `Func<HttpContext, IServiceProvider, IDictionary>` for shared props |

See `InertiaOptions` for the full list including SSR, page validation, and exception handling options.

## Testing

```bash
dotnet add package Inertia.Testing
```

```csharp
var response = await client.GetAsync("/users");

await response.AssertInertia(page =>
{
    page.Component("Users/Index");
    page.Has("users", 10);
    page.Where("users.0.name", "Alice");
    page.MissingFlash("error");
});
```

## Project Structure

| Project | Purpose |
|---------|---------|
| `src/Inertia.AspNetCore` | Main library (NuGet package) |
| `src/Inertia.Testing` | Test assertions for consumers (NuGet package) |
| `tests/Inertia.Tests` | Library tests |
| `tests/Inertia.Testing.Tests` | Testing package tests |

## inertia-laravel Submodule

The official Laravel adapter is tracked as a git submodule at `inertia-laravel/` for reference during development. **Current tracked version:** v3.0.1.

```bash
git submodule update --remote inertia-laravel
```

## License

MIT License — same as inertia-laravel.
