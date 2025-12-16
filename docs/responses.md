# Inertia Responses

Learn how to create and work with Inertia responses in your ASP.NET Core application.

## Basic Responses

The most common way to use Inertia is to return responses from your controllers using the `IInertia` service.

### Simple Response

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
        return await _inertia.RenderAsync("Users/Index", new
        {
            users = new[]
            {
                new { id = 1, name = "John Doe" },
                new { id = 2, name = "Jane Smith" }
            }
        });
    }
}
```

The first argument is the component name (relative to your Pages directory), and the second is an object containing props.

### Props as Dictionary

You can also pass props as a dictionary:

```csharp
public async Task<IActionResult> Show(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    
    var props = new Dictionary<string, object?>
    {
        ["user"] = user,
        ["canEdit"] = User.IsInRole("Admin")
    };
    
    return await _inertia.RenderAsync("Users/Show", props);
}
```

### With Database Queries

Integrate with Entity Framework Core seamlessly:

```csharp
public async Task<IActionResult> Index()
{
    var users = await _dbContext.Users
        .OrderBy(u => u.Name)
        .Take(50)
        .ToListAsync();
    
    return await _inertia.RenderAsync("Users/Index", new
    {
        users = users
    });
}
```

## Shared Data

Share data across all Inertia requests using the `HandleInertiaRequests` middleware:

```csharp
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["appName"] = "My App",
            ["user"] = GetAuthenticatedUser(request),
            ["flash"] = GetFlashMessages(request)
        };
    }
    
    private object? GetAuthenticatedUser(HttpRequest request)
    {
        if (request.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            return new
            {
                name = request.HttpContext.User.Identity.Name,
                email = request.HttpContext.User.FindFirst("email")?.Value
            };
        }
        return null;
    }
}
```

Access shared data in your components:

```jsx
export default function Layout({ appName, user, children }) {
  return (
    <div>
      <header>
        <h1>{appName}</h1>
        {user && <span>Welcome, {user.name}</span>}
      </header>
      <main>{children}</main>
    </div>
  )
}
```

### Share Once

To share data only once (not on subsequent partial reloads), use `ShareOnce`:

```csharp
protected override Dictionary<string, object?> ShareOnce(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["countries"] = GetCountries(), // Only loaded on initial page load
        ["timezones"] = GetTimezones()
    };
}
```

## Partial Reloads

Inertia automatically handles partial reloads. When you visit a page, Inertia only requests the data that has changed.

### Client-Side

```jsx
import { router } from '@inertiajs/react'

// Request only specific props
router.reload({ only: ['users'] })

// Request all props except specific ones
router.reload({ except: ['posts'] })
```

### Always Include Certain Props

Use the `Always` prop type to ensure a prop is always included, even in partial reloads:

```csharp
using static Inertia.LazyProps;

public async Task<IActionResult> Index()
{
    return await _inertia.RenderAsync("Dashboard", new
    {
        // Always included, even in partial reloads
        user = Always(() => GetCurrentUser()),
        
        // Only included when explicitly requested
        posts = await GetPostsAsync()
    });
}
```

## Location Responses

Sometimes you need to force a full page reload instead of an Inertia visit. Use `Location`:

```csharp
public IActionResult RedirectExternal()
{
    // Forces a full page visit (not an XHR request)
    return _inertia.Location("https://external-site.com");
}
```

This is useful for:
- Redirecting to external websites
- File downloads
- Cases where you need a full browser navigation

## Empty Responses

Handle cases where no content should be returned:

```csharp
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected override IActionResult? OnEmptyResponse(HttpContext context)
    {
        // Return a 204 No Content or redirect to a default page
        return new StatusCodeResult(204);
    }
}
```

## Redirects

Inertia automatically handles redirects from your controllers:

### Simple Redirect

```csharp
public IActionResult Store(UserCreateModel model)
{
    var user = CreateUser(model);
    
    // Redirects to Users/Show
    return RedirectToAction(nameof(Show), new { id = user.Id });
}
```

### With Flash Messages

Combine redirects with TempData for flash messages:

```csharp
public IActionResult Store(UserCreateModel model)
{
    var user = CreateUser(model);
    
    TempData["success"] = "User created successfully!";
    
    return RedirectToAction(nameof(Show), new { id = user.Id });
}
```

Access in your middleware:

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["flash"] = new
        {
            success = request.HttpContext.TempData["success"]?.ToString(),
            error = request.HttpContext.TempData["error"]?.ToString()
        }
    };
}
```

### External Redirects

For external redirects, use standard ASP.NET Core redirects:

```csharp
public IActionResult External()
{
    return Redirect("https://example.com");
}
```

## Response Types

### JSON Response (Inertia Request)

When a request has the `X-Inertia` header, Inertia returns JSON:

```json
{
  "component": "Users/Index",
  "props": {
    "users": [...]
  },
  "url": "/users",
  "version": "1.0.0"
}
```

### HTML Response (Initial Visit)

On the initial visit (no `X-Inertia` header), Inertia renders your root view with the page data embedded:

```html
<!DOCTYPE html>
<html>
<head>...</head>
<body>
    <div id="app" data-page='{"component":"Users/Index","props":{...}}'></div>
    <script src="/js/app.js"></script>
</body>
</html>
```

## Asset Versioning

Inertia uses asset versioning to ensure users always have the latest JavaScript and CSS:

```csharp
protected override string? Version(HttpRequest request)
{
    // Read from manifest file generated by Vite/Webpack
    var manifest = ReadManifest();
    return manifest.Version;
    
    // Or use a git commit hash
    // return Environment.GetEnvironmentVariable("GIT_COMMIT_SHA");
}
```

When the version changes, Inertia automatically performs a full page reload to fetch new assets.

### Automatic Version Checking

The middleware automatically compares the client's version with the server's version. If they differ, it forces a full page reload.

## Root View Customization

Customize the root view on a per-request basis:

```csharp
protected override string RootView(HttpRequest request)
{
    // Use different layouts for different sections
    if (request.Path.StartsWithSegments("/admin"))
    {
        return "admin";
    }
    
    return "app"; // Default root view
}
```

## Advanced Patterns

### Conditional Props

Return different props based on conditions:

```csharp
public async Task<IActionResult> Show(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    var isOwner = User.FindFirstValue(ClaimTypes.NameIdentifier) == user.Id.ToString();
    
    var props = new Dictionary<string, object?>
    {
        ["user"] = user
    };
    
    // Only include sensitive data for the owner
    if (isOwner)
    {
        props["privateData"] = await GetPrivateData(user);
    }
    
    return await _inertia.RenderAsync("Users/Show", props);
}
```

### Nested Data

Props can be nested objects:

```csharp
return await _inertia.RenderAsync("Users/Show", new
{
    user = new
    {
        id = user.Id,
        name = user.Name,
        profile = new
        {
            bio = user.Bio,
            avatar = user.AvatarUrl
        },
        settings = new
        {
            notifications = user.NotificationsEnabled,
            privacy = user.PrivacyLevel
        }
    }
});
```

### Computed Properties

Use callbacks to compute props only when needed:

```csharp
using static Inertia.LazyProps;

return await _inertia.RenderAsync("Dashboard", new
{
    user = GetCurrentUser(),
    
    // Computed on access
    stats = Optional(() => CalculateExpensiveStats()),
    
    // Async computation
    reports = Optional(async () => await GenerateReportsAsync())
});
```

## Best Practices

1. **Keep props minimal** - Only send the data your component needs
2. **Use shared data** - Share common data in middleware, not in every controller
3. **Leverage partial reloads** - Design components to work with partial data updates
4. **Version your assets** - Implement proper asset versioning for cache busting
5. **Handle validation errors** - Use the Inertia validation filter for automatic error handling

## Next Steps

- Learn about [Property Types](properties.md) for advanced data loading patterns
- Explore [Middleware](middleware.md) for shared data and request handling
- Read about [Testing](testing.md) to test your Inertia responses
