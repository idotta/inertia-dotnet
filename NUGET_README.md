# Inertia.js .NET Adapter

A .NET adapter for [Inertia.js](https://inertiajs.com), enabling you to build modern single-page applications using classic server-side routing with ASP.NET Core.

## Features

- 🚀 **Server-side routing** - Use ASP.NET Core's routing instead of client-side routing
- 🔄 **Automatic code splitting** - Each page is a separate component
- 📦 **Property types** - Optional, deferred, always, merge, scroll, and once props
- ⚡ **Server-side rendering** (SSR) - Optional SSR support with Node.js or Bun
- 🧪 **Testing utilities** - Fluent assertions for testing Inertia responses
- 🎯 **Feature parity** - Matches inertia-laravel functionality

## Installation

```bash
dotnet add package Inertia.AspNetCore
```

## Quick Start

### 1. Configure Services

In your `Program.cs`:

```csharp
using Inertia.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add Inertia
builder.Services.AddInertia(options =>
{
    options.RootView = "app";
});

var app = builder.Build();

// Use Inertia middleware
app.UseInertia<HandleInertiaRequests>();

app.Run();
```

### 2. Create Middleware

Create `HandleInertiaRequests.cs`:

```csharp
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    public override async Task<Dictionary<string, object>> Share(HttpRequest request)
    {
        var baseShared = await base.Share(request);
        
        return new Dictionary<string, object>(baseShared)
        {
            ["appName"] = "My App",
            ["user"] = await GetCurrentUserAsync(request)
        };
    }
}
```

### 3. Render Inertia Responses

In your controller:

```csharp
public class UsersController : Controller
{
    private readonly IInertia _inertia;
    private readonly ApplicationDbContext _context;
    
    public UsersController(IInertia inertia, ApplicationDbContext context)
    {
        _inertia = inertia;
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        return await _inertia.RenderAsync("Users/Index", new
        {
            users = await _context.Users.ToListAsync()
        });
    }
}
```

### 4. Create Root View

Create `Views/Shared/app.cshtml`:

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>My App</title>
    @await Html.RenderComponentAsync<HeadOutlet>()
</head>
<body>
    <inertia />
    <script src="/js/app.js"></script>
</body>
</html>
```

## Property Types

### Optional Props (Lazy Loading)

Load props only when requested:

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    users = await _context.Users.ToListAsync(),
    stats = Optional(async () => await CalculateStatsAsync())
});
```

### Deferred Props

Load props asynchronously after initial page load:

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    recentActivity = Defer(async () => await GetRecentActivityAsync())
});
```

### Always Props

Include props even during partial reloads:

```csharp
return await _inertia.RenderAsync("Settings", new
{
    errors = Always(() => TempData["errors"])
});
```

### Merge Props

Merge with existing client-side data:

```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = Merge(await _context.Users.ToPagedListAsync(page, pageSize))
});
```

### Scroll Props (Infinite Scroll)

For paginated data with append/prepend support:

```csharp
return await _inertia.RenderAsync("Posts/Index", new
{
    posts = Scroll(
        await _context.Posts.ToPagedListAsync(page, 15),
        metadata: p => ScrollMetadata.FromPagedList(p)
    )
});
```

### Once Props

Cache and reuse across navigations:

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    translations = Once(() => _localizer.GetAllStrings())
});
```

## Server-Side Rendering

Enable SSR in `appsettings.json`:

```json
{
  "Inertia": {
    "Ssr": {
      "Enabled": true,
      "Url": "http://127.0.0.1:13714",
      "Bundle": "wwwroot/ssr/ssr.mjs"
    }
  }
}
```

## Testing

```csharp
[Fact]
public async Task UsersPage_ReturnsUsers()
{
    var response = await _client.GetAsync("/users");
    
    response.Should().BeInertia()
        .WithComponent("Users/Index")
        .Has("users", count: 10)
        .Has("users.0", user => user
            .Where("id", 1)
            .Where("name", "John Doe")
        );
}
```

## Documentation

- 📖 [Official Documentation](https://github.com/idotta/inertia-dotnet)
- 🗺️ [API Mapping (Laravel to .NET)](https://github.com/idotta/inertia-dotnet/blob/main/API_MAPPING.md)
- 📋 [Migration Guide](https://github.com/idotta/inertia-dotnet/blob/main/MIGRATION_PLAN.md)
- 💡 [Examples](https://github.com/idotta/inertia-dotnet/tree/main/samples)

## Compatibility

- .NET 6.0+
- .NET 8.0+ (recommended)
- ASP.NET Core

## License

MIT License - Same as [inertia-laravel](https://github.com/inertiajs/inertia-laravel)

## Contributing

Contributions are welcome! See our [GitHub repository](https://github.com/idotta/inertia-dotnet) for:
- Implementation checklist
- Open issues
- Discussion forums

## Related Packages

- `Inertia.Core` - Core library (included)
- `Inertia.Testing` - Testing utilities (separate package)
