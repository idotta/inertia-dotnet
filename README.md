# inertia-dotnet

[![NuGet](https://img.shields.io/nuget/v/Inertia.AspNetCore.svg)](https://www.nuget.org/packages/Inertia.AspNetCore/)
[![Build Status](https://github.com/idotta/inertia-dotnet/workflows/Build/badge.svg)](https://github.com/idotta/inertia-dotnet/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A .NET adapter for [Inertia.js](https://inertiajs.com/). Build modern single-page applications using classic server-side routing and controllers.

> **Note:** This project is currently in active development (Phase 5 complete - Testing Infrastructure). See [Migration Status](#migration-status) for details.

## Features

✅ **Fully Implemented:**
- 🎯 **Core Response Rendering** - Component and props management
- 🔄 **Property Types** - Optional, Deferred, Always, Merge, Scroll, and Once props
- 🛡️ **Middleware** - Request handling, version checking, validation
- 🎨 **TagHelpers** - Razor view integration
- ⚡ **Server-Side Rendering (SSR)** - Node.js integration with health checks
- 🧪 **Testing Utilities** - Fluent assertions for Inertia responses
- 📦 **Asset Versioning** - Automatic cache busting
- 🔐 **History Encryption** - Enhanced security for browser state

## Installation

```bash
dotnet add package Inertia.AspNetCore
```

## Quick Start

### 1. Configure Services

Add Inertia to your ASP.NET Core application in `Program.cs`:

```csharp
using Inertia.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Inertia services
builder.Services.AddInertia(options =>
{
    options.RootView = "app";  // Your root Razor view
    options.Ssr.Enabled = true;
    options.Ssr.Url = "http://127.0.0.1:13714";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Add Inertia middleware
app.UseInertia<HandleInertiaRequests>();

app.MapControllers();
app.Run();
```

### 2. Create Middleware Handler

Create a class that extends `HandleInertiaRequests`:

```csharp
using Inertia.AspNetCore;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["appName"] = "My Inertia App",
            ["user"] = new { Name = "John Doe", Email = "john@example.com" }
        };
    }

    protected override string? Version(HttpRequest request)
    {
        // Return a hash of your assets for cache busting
        return "1.0.0";
    }
}
```

### 3. Create Your Root View

Create a Razor view (`Views/Shared/app.cshtml`):

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My Inertia App</title>
    @* Your CSS imports *@
</head>
<body>
    <inertia />
    @* Your JavaScript bundle *@
    <script src="~/js/app.js" asp-append-version="true"></script>
</body>
</html>
```

### 4. Return Inertia Responses

Use the `IInertia` service in your controllers:

```csharp
using Inertia;
using Microsoft.AspNetCore.Mvc;

public class UsersController : Controller
{
    private readonly IInertia _inertia;

    public UsersController(IInertia inertia)
    {
        _inertia = inertia;
    }

    public async Task<IActionResult> Index()
    {
        var users = await GetUsersAsync();
        
        return await _inertia.RenderAsync("Users/Index", new
        {
            users = users
        });
    }
}
```

### 5. Create Your Frontend Component

Create a React/Vue/Svelte component (example with React):

```jsx
// resources/js/Pages/Users/Index.jsx
import { Head } from '@inertiajs/react'

export default function Index({ users }) {
  return (
    <>
      <Head title="Users" />
      <h1>Users</h1>
      <ul>
        {users.map(user => (
          <li key={user.id}>{user.name}</li>
        ))}
      </ul>
    </>
  )
}
```

That's it! You now have a working Inertia.js application with ASP.NET Core.

## Documentation

📚 **Comprehensive guides available:**

- **[Getting Started](docs/getting-started.md)** - Detailed setup guide
- **[Responses](docs/responses.md)** - Working with Inertia responses
- **[Property Types](docs/properties.md)** - Optional, Deferred, Merge, and more
- **[Middleware](docs/middleware.md)** - Request handling and shared data
- **[Server-Side Rendering](docs/ssr-setup.md)** - SSR configuration and setup
- **[Testing](docs/testing.md)** - Testing your Inertia applications
- **[Migration from Laravel](docs/migration-from-laravel.md)** - Laravel to .NET guide

## Project Goal

This project aims to stay feature-complete and on par with [inertia-laravel](https://github.com/inertiajs/inertia-laravel). We track the Laravel adapter as a submodule and periodically sync new features and improvements.

## Migration Status

🎯 **Phase 5 Complete** - Testing Infrastructure Implemented!

| Phase | Status | Features |
|-------|--------|----------|
| Phase 1: Core Infrastructure | ✅ Complete | Response rendering, configuration |
| Phase 2: Property Types | ✅ Complete | Optional, Deferred, Always, Merge, Scroll, Once |
| Phase 3: Middleware | ✅ Complete | Request handling, validation, encryption |
| Phase 4: SSR | ✅ Complete | Server-side rendering with Node.js |
| Phase 5: Testing | ✅ Complete | Fluent assertions, test utilities |
| Phase 6: CLI Tools | ⏳ Pending | Optional developer tools |
| Phase 7: Documentation | 🚧 In Progress | Comprehensive guides |
| Phase 8: Examples | ⏳ Pending | Sample projects |

### 📚 Planning Documents (For Contributors)

- **[MIGRATION_SUMMARY.md](MIGRATION_SUMMARY.md)** - Executive summary and overview
- **[MIGRATION_PLAN.md](MIGRATION_PLAN.md)** - Complete implementation plan
- **[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)** - Feature comparison (Laravel vs .NET)
- **[IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)** - 400+ actionable tasks
- **[API_MAPPING.md](API_MAPPING.md)** - Code examples: Laravel → C#
- **[MIGRATION.md](MIGRATION.md)** - Migration process guidelines

## inertia-laravel Submodule

This repository includes the official Laravel adapter as a git submodule to:
- Track the reference implementation
- Monitor for new features and updates
- Ensure feature parity with the Laravel ecosystem

**Current Tracked Version:** v2.0.14 (commit: 7240b646)

### Updating the Submodule

To update the inertia-laravel submodule to the latest version:

```bash
git submodule update --remote inertia-laravel
```

After updating, review changes and migrate new features to C#. See [MIGRATION.md](MIGRATION.md) for guidelines.

## Examples

Check out our [sample projects](samples/) to see Inertia.js in action:

- **InertiaMinimal** - Minimal setup example
- **InertiaReact** - Full React application
- **InertiaVue** - Full Vue 3 application  
- **InertiaSsr** - Server-side rendering example

## Advanced Features

### Property Types

Control how data is loaded and merged:

```csharp
using static Inertia.LazyProps;

return await _inertia.RenderAsync("Dashboard", new
{
    // Always included, bypasses partial reload filtering
    user = Always(() => GetCurrentUser()),
    
    // Loaded only when explicitly requested
    posts = Optional(() => GetPosts()),
    
    // Loaded asynchronously after initial render
    stats = Defer(() => CalculateStatsAsync()),
    
    // Merged with client-side data
    notifications = Merge(() => GetNotifications()),
    
    // Cached and reused across navigations
    settings = Once(() => GetSettings())
});
```

### Server-Side Rendering

Enable SSR for improved performance and SEO:

```csharp
builder.Services.AddInertia(options =>
{
    options.Ssr.Enabled = true;
    options.Ssr.Url = "http://127.0.0.1:13714";
    options.Ssr.Bundle = "wwwroot/ssr/ssr.mjs"; // Auto-detected if not specified
});
```

### Testing

Test your Inertia responses with fluent assertions:

```csharp
using Inertia.Testing;

[Fact]
public async Task ItRendersUsersPage()
{
    var response = await Client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .WithComponent("Users/Index")
        .Has("users")
        .WithCount("users", 10)
        .Where("users.0.name", "John Doe")
    );
}
```

## Development & Contributing

We welcome contributions! Here's how to get started:

### For Contributors

1. **Read the planning docs** (start with [MIGRATION_SUMMARY.md](MIGRATION_SUMMARY.md))
2. **Pick a task** from [IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)
3. **Review code examples** in [API_MAPPING.md](API_MAPPING.md)
4. **Submit a PR** following our guidelines

### Building from Source

```bash
# Clone the repository
git clone https://github.com/idotta/inertia-dotnet.git
cd inertia-dotnet

# Initialize submodules
git submodule update --init --recursive

# Build the solution
dotnet build

# Run tests
dotnet test
```

See [MIGRATION.md](MIGRATION.md) for details on how features from inertia-laravel are migrated to this .NET implementation.

## Contributing

We welcome contributions! Check out:
- [IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md) for available tasks
- [GitHub Issues](https://github.com/idotta/inertia-dotnet/issues) for open items
- [GitHub Discussions](https://github.com/idotta/inertia-dotnet/discussions) for questions

## License

MIT License - Same as inertia-laravel
