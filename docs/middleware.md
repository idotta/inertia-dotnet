# Middleware

Learn how to use Inertia middleware to handle requests, share data, and manage your application's Inertia behavior.

## Overview

Inertia middleware sits between the browser and your controllers, handling:
- Inertia request detection
- Asset version checking
- Shared data injection
- Validation error handling
- Redirect normalization

## HandleInertiaRequests

The `HandleInertiaRequests` class is the primary way to customize Inertia's behavior.

### Basic Setup

Create a class that extends `HandleInertiaRequests`:

```csharp
using Inertia.AspNetCore;

namespace MyApp.Middleware;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected override string? Version(HttpRequest request)
    {
        // Return your asset version
        return "1.0.0";
    }

    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["appName"] = "My Application"
        };
    }
}
```

### Register Middleware

In `Program.cs`:

```csharp
app.UseInertia<MyApp.Middleware.HandleInertiaRequests>();
```

## Shared Data

Shared data is automatically included with every Inertia response.

### Basic Sharing

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["appName"] = "My App",
        ["appUrl"] = "https://myapp.com",
        ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    };
}
```

### Authenticated User

Share the current user with all pages:

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    var user = request.HttpContext.User;
    
    return new Dictionary<string, object?>
    {
        ["user"] = user.Identity?.IsAuthenticated == true
            ? new
            {
                id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                name = user.Identity.Name,
                email = user.FindFirst(ClaimTypes.Email)?.Value
            }
            : null
    };
}
```

### With Dependency Injection

Access services in your middleware:

```csharp
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public HandleInertiaRequests(
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }
    
    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["appName"] = _configuration["AppName"],
            ["user"] = GetCurrentUser()
        };
    }
    
    private object? GetCurrentUser()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return new
            {
                name = user.Identity.Name,
                // ... other user properties
            };
        }
        return null;
    }
}
```

### Flash Messages

Share flash messages from TempData:

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["flash"] = new
        {
            success = request.HttpContext.TempData["success"]?.ToString(),
            error = request.HttpContext.TempData["error"]?.ToString(),
            warning = request.HttpContext.TempData["warning"]?.ToString(),
            info = request.HttpContext.TempData["info"]?.ToString()
        }
    };
}
```

In your controller:

```csharp
public IActionResult Create(UserModel model)
{
    // Create user...
    
    TempData["success"] = "User created successfully!";
    return RedirectToAction(nameof(Index));
}
```

In your component:

```jsx
import { usePage } from '@inertiajs/react'

export default function Layout({ children }) {
  const { flash } = usePage().props
  
  return (
    <div>
      {flash.success && (
        <div className="alert alert-success">{flash.success}</div>
      )}
      {flash.error && (
        <div className="alert alert-error">{flash.error}</div>
      )}
      {children}
    </div>
  )
}
```

### Errors (Validation)

Share validation errors automatically:

```csharp
protected override Dictionary<string, object?> ResolveValidationErrors(HttpContext context)
{
    // Validation errors are automatically added by InertiaValidationFilter
    if (context.Items.TryGetValue("InertiaValidationErrors", out var errors))
    {
        return (Dictionary<string, object?>)errors;
    }
    
    return new Dictionary<string, object?>();
}
```

Enable validation filter in `Program.cs`:

```csharp
builder.Services.AddControllersWithViews()
    .AddInertiaValidation(); // Automatically handles ModelState errors
```

## Share Once

Share data only on the initial page load, not on subsequent partial reloads:

```csharp
protected override Dictionary<string, object?> ShareOnce(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["countries"] = GetCountries(), // Expensive query
        ["timezones"] = GetTimezones(),
        ["appSettings"] = GetAppSettings()
    };
}
```

This data is sent once and cached on the client, reducing bandwidth on subsequent requests.

## Asset Versioning

Implement asset versioning to ensure users always have the latest code.

### Static Version

```csharp
protected override string? Version(HttpRequest request)
{
    return "1.0.0";
}
```

### Git Commit Hash

```csharp
protected override string? Version(HttpRequest request)
{
    return Environment.GetEnvironmentVariable("GIT_COMMIT_SHA") 
        ?? "development";
}
```

### Manifest File

If you're using Vite or Webpack with a manifest:

```csharp
private static string? _version;

protected override string? Version(HttpRequest request)
{
    if (_version == null)
    {
        var manifestPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "build",
            "manifest.json"
        );
        
        if (File.Exists(manifestPath))
        {
            var manifest = File.ReadAllText(manifestPath);
            var hash = ComputeHash(manifest);
            _version = hash;
        }
    }
    
    return _version;
}

private static string ComputeHash(string input)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash)[..8]; // First 8 characters
}
```

### How It Works

1. Client sends its current version in the `X-Inertia-Version` header
2. Server compares with current version
3. If versions differ, server responds with 409 Conflict
4. Client performs a full page reload to get new assets

## Root View

Customize the root view based on the request:

```csharp
protected override string RootView(HttpRequest request)
{
    // Use different layouts for different sections
    if (request.Path.StartsWithSegments("/admin"))
    {
        return "admin-layout";
    }
    
    if (request.Path.StartsWithSegments("/auth"))
    {
        return "auth-layout";
    }
    
    return "app"; // Default layout
}
```

## URL Resolver

Customize how URLs are resolved:

```csharp
protected override Func<HttpRequest, string>? UrlResolver()
{
    return request =>
    {
        // Use a custom domain for URLs
        var scheme = request.Scheme;
        var host = "myapp.com"; // Custom domain
        var path = request.Path;
        var query = request.QueryString;
        
        return $"{scheme}://{host}{path}{query}";
    };
}
```

This is useful for:
- Multi-tenant applications
- Custom domain mapping
- Subdomain handling

## Empty Response Handling

Handle cases where no Inertia response is returned:

```csharp
protected override IActionResult? OnEmptyResponse(HttpContext context)
{
    // Redirect to homepage
    return new RedirectResult("/");
    
    // Or return 204 No Content
    // return new StatusCodeResult(204);
}
```

## Version Change Handling

Customize behavior when asset versions don't match:

```csharp
protected override IActionResult? OnVersionChange(HttpContext context)
{
    // Force full page reload (default behavior)
    return new StatusCodeResult(409);
    
    // Or redirect to a "maintenance" page
    // return new RedirectResult("/maintenance");
}
```

## Validation Errors

The validation filter automatically handles ModelState errors:

```csharp
public class UserController : Controller
{
    [HttpPost]
    public async Task<IActionResult> Create(UserCreateModel model)
    {
        if (!ModelState.IsValid)
        {
            // Errors automatically sent to client via Inertia
            return await _inertia.RenderAsync("Users/Create", new
            {
                // Re-render form with errors
                errors = ModelState
            });
        }
        
        // Create user...
        return RedirectToAction(nameof(Index));
    }
}
```

### Error Bags

For multiple forms on the same page:

```csharp
[HttpPost]
public IActionResult UpdateProfile(ProfileModel model)
{
    if (!ModelState.IsValid)
    {
        // Specify error bag name
        HttpContext.Items["InertiaErrorBag"] = "updateProfile";
        return BadRequest();
    }
    
    // Update profile...
    return Ok();
}
```

Client-side:

```jsx
import { useForm } from '@inertiajs/react'

function ProfileForm() {
  const { data, setData, post, errors } = useForm({
    name: '',
    email: ''
  })
  
  const submit = (e) => {
    e.preventDefault()
    post('/profile', {
      errorBag: 'updateProfile' // Match server-side error bag
    })
  }
  
  return (
    <form onSubmit={submit}>
      <input
        value={data.name}
        onChange={e => setData('name', e.target.value)}
      />
      {errors.name && <div>{errors.name}</div>}
      
      <button type="submit">Update</button>
    </form>
  )
}
```

## History Encryption

Enable history encryption for enhanced security:

```csharp
// In Program.cs
app.UseInertiaEncryptHistory();
```

Or enable via configuration:

```csharp
builder.Services.AddInertia(options =>
{
    options.History.Encrypt = true;
});
```

This encrypts the history state stored in the browser, preventing users from inspecting or modifying it.

## Custom Middleware

You can create custom middleware for specific needs:

```csharp
public class CustomInertiaMiddleware : IMiddleware
{
    private readonly IInertia _inertia;
    
    public CustomInertiaMiddleware(IInertia inertia)
    {
        _inertia = inertia;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // Custom logic before Inertia processes the request
        
        await next(context);
        
        // Custom logic after Inertia processes the request
    }
}

// Register in Program.cs
builder.Services.AddScoped<CustomInertiaMiddleware>();
app.UseMiddleware<CustomInertiaMiddleware>();
```

## Middleware Order

The order of middleware is important:

```csharp
app.UseStaticFiles(); // Before Inertia
app.UseRouting();
app.UseAuthentication(); // Before Inertia
app.UseAuthorization(); // Before Inertia

app.UseInertia<HandleInertiaRequests>(); // Main Inertia middleware
app.UseInertiaEncryptHistory(); // Optional: History encryption

app.MapControllers();
```

## Best Practices

1. **Keep shared data minimal** - Only share what's needed across all pages
2. **Use ShareOnce for static data** - Reduce bandwidth for reference data
3. **Implement proper versioning** - Ensure users get latest assets
4. **Handle errors gracefully** - Provide good UX for validation and app errors
5. **Use flash messages** - Communicate action results to users
6. **Secure sensitive data** - Don't expose secrets in shared data
7. **Profile performance** - Monitor impact of shared data on response times

## Advanced Examples

### Multi-Tenant Shared Data

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    var tenant = GetTenantFromRequest(request);
    
    return new Dictionary<string, object?>
    {
        ["tenant"] = new
        {
            id = tenant.Id,
            name = tenant.Name,
            logo = tenant.LogoUrl,
            theme = tenant.ThemeSettings
        },
        ["user"] = GetCurrentUser(request)
    };
}
```

### Feature Flags

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["features"] = new
        {
            newDashboard = _featureManager.IsEnabled("NewDashboard"),
            betaFeatures = _featureManager.IsEnabled("BetaFeatures"),
            darkMode = _featureManager.IsEnabled("DarkMode")
        }
    };
}
```

### Localization

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    var culture = CultureInfo.CurrentCulture;
    
    return new Dictionary<string, object?>
    {
        ["locale"] = culture.Name,
        ["translations"] = LoadTranslations(culture),
        ["direction"] = culture.TextInfo.IsRightToLeft ? "rtl" : "ltr"
    };
}
```

## Next Steps

- Learn about [Property Types](properties.md) for advanced data loading
- Explore [Server-Side Rendering](ssr-setup.md) for improved performance
- Read [Testing](testing.md) to test middleware behavior
