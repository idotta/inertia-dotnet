# Migration from Laravel

Guide for developers familiar with `inertia-laravel` who want to use Inertia.js with ASP.NET Core.

## Overview

This adapter aims to maintain feature parity with `inertia-laravel` while following .NET conventions. Most concepts translate directly, but there are some framework-specific differences.

## Key Differences

### 1. Service Registration

**Laravel (automatic):**
```php
// Registered via ServiceProvider in config/app.php
'providers' => [
    Inertia\ServiceProvider::class,
],
```

**.NET (explicit):**
```csharp
// Program.cs
builder.Services.AddInertia(options =>
{
    options.RootView = "app";
});
```

### 2. Dependency Injection

**Laravel (Facade):**
```php
use Inertia\Inertia;

return Inertia::render('Users/Index', [
    'users' => $users
]);
```

**.NET (Constructor Injection):**
```csharp
using Inertia;

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
            users = users
        });
    }
}
```

### 3. Middleware

**Laravel:**
```php
// app/Http/Middleware/HandleInertiaRequests.php
class HandleInertiaRequests extends Middleware
{
    public function share(Request $request): array
    {
        return [
            'user' => $request->user(),
        ];
    }
}
```

**.NET:**
```csharp
// Middleware/HandleInertiaRequests.cs
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            ["user"] = GetCurrentUser(request)
        };
    }
}
```

### 4. Views

**Laravel (Blade):**
```blade
<!DOCTYPE html>
<html>
<head>
    @inertiaHead
</head>
<body>
    @inertia
</body>
</html>
```

**.NET (Razor):**
```html
<!DOCTYPE html>
<html>
<head>
    <inertia-head />
</head>
<body>
    <inertia />
</body>
</html>
```

### 5. Async/Await

**Laravel (Synchronous):**
```php
return Inertia::render('Users/Index', [
    'users' => User::all()
]);
```

**.NET (Asynchronous):**
```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = await _dbContext.Users.ToListAsync()
});
```

.NET encourages async I/O operations for better scalability.

## Code Comparisons

### Basic Response

**Laravel:**
```php
public function index()
{
    return Inertia::render('Users/Index', [
        'users' => User::all(),
    ]);
}
```

**.NET:**
```csharp
public async Task<IActionResult> Index()
{
    var users = await _dbContext.Users.ToListAsync();
    return await _inertia.RenderAsync("Users/Index", new { users });
}
```

### Shared Data

**Laravel:**
```php
public function share(Request $request): array
{
    return [
        'auth' => [
            'user' => $request->user(),
        ],
        'flash' => [
            'success' => session('success'),
        ],
    ];
}
```

**.NET:**
```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["auth"] = new
        {
            user = GetCurrentUser(request)
        },
        ["flash"] = new
        {
            success = request.HttpContext.TempData["success"]?.ToString()
        }
    };
}
```

### Property Types

**Laravel:**
```php
return Inertia::render('Dashboard', [
    'stats' => Inertia::optional(fn () => Stats::calculate()),
    'posts' => Inertia::defer(fn () => Post::all()),
    'user' => Inertia::always(fn () => $request->user()),
]);
```

**.NET:**
```csharp
using static Inertia.LazyProps;

return await _inertia.RenderAsync("Dashboard", new
{
    stats = Optional(() => Stats.Calculate()),
    posts = Defer(() => GetPostsAsync()),
    user = Always(() => GetCurrentUser())
});
```

### Validation

**Laravel:**
```php
public function store(Request $request)
{
    $validated = $request->validate([
        'name' => 'required|max:255',
        'email' => 'required|email',
    ]);
    
    User::create($validated);
    
    return redirect()->route('users.index')
        ->with('success', 'User created.');
}
```

**.NET:**
```csharp
[HttpPost]
public async Task<IActionResult> Store(UserCreateModel model)
{
    if (!ModelState.IsValid)
    {
        // Errors automatically handled by InertiaValidationFilter
        return await _inertia.RenderAsync("Users/Create", new
        {
            errors = ModelState
        });
    }
    
    await _dbContext.Users.AddAsync(new User
    {
        Name = model.Name,
        Email = model.Email
    });
    await _dbContext.SaveChangesAsync();
    
    TempData["success"] = "User created.";
    return RedirectToAction(nameof(Index));
}
```

### Redirects

**Laravel:**
```php
return redirect()->route('users.show', $user);
```

**.NET:**
```csharp
return RedirectToAction(nameof(Show), new { id = user.Id });
```

### Location Response

**Laravel:**
```php
return Inertia::location('https://example.com');
```

**.NET:**
```csharp
return _inertia.Location("https://example.com");
```

## Framework Mappings

### Routing

**Laravel:**
```php
Route::get('/users', [UserController::class, 'index'])
    ->name('users.index');
```

**.NET:**
```csharp
app.MapControllerRoute(
    name: "users",
    pattern: "users",
    defaults: new { controller = "Users", action = "Index" }
);

// Or with attribute routing
[Route("users")]
public IActionResult Index() { ... }
```

### Database Queries

**Laravel:**
```php
$users = User::with('profile')
    ->where('active', true)
    ->paginate(15);
```

**.NET:**
```csharp
var users = await _dbContext.Users
    .Include(u => u.Profile)
    .Where(u => u.Active)
    .Skip((page - 1) * 15)
    .Take(15)
    .ToListAsync();
```

### Authentication

**Laravel:**
```php
if ($request->user()) {
    // User is authenticated
}

if ($request->user()->can('edit', $post)) {
    // User is authorized
}
```

**.NET:**
```csharp
if (User.Identity?.IsAuthenticated == true)
{
    // User is authenticated
}

if ((await _authorizationService.AuthorizeAsync(User, post, "Edit")).Succeeded)
{
    // User is authorized
}
```

### Session / Flash Data

**Laravel:**
```php
session()->flash('success', 'User created');
$message = session('success');
```

**.NET:**
```csharp
TempData["success"] = "User created";
var message = TempData["success"]?.ToString();
```

### Configuration

**Laravel:**
```php
// config/inertia.php
return [
    'ssr' => [
        'enabled' => true,
        'url' => 'http://127.0.0.1:13714',
    ],
];

// Usage
$enabled = config('inertia.ssr.enabled');
```

**.NET:**
```csharp
// appsettings.json
{
  "Inertia": {
    "Ssr": {
      "Enabled": true,
      "Url": "http://127.0.0.1:13714"
    }
  }
}

// Usage (injected IOptions)
var enabled = _options.Value.Ssr.Enabled;
```

## Common Pitfalls

### 1. Forgetting Async/Await

❌ **Wrong:**
```csharp
public IActionResult Index()
{
    var users = _dbContext.Users.ToList(); // Synchronous
    return _inertia.RenderAsync("Users/Index", new { users });
}
```

✅ **Correct:**
```csharp
public async Task<IActionResult> Index()
{
    var users = await _dbContext.Users.ToListAsync();
    return await _inertia.RenderAsync("Users/Index", new { users });
}
```

### 2. Not Using TempData for Flash Messages

❌ **Wrong:**
```csharp
ViewBag.Message = "Success"; // Lost on redirect
```

✅ **Correct:**
```csharp
TempData["success"] = "Success"; // Persists across redirect
```

### 3. Case Sensitivity

Laravel route names are case-insensitive, .NET routes are case-sensitive:

**Laravel:**
```php
route('Users.Index') // Works
route('users.index') // Also works
```

**.NET:**
```csharp
RedirectToAction("Index") // Case matters
RedirectToAction("index") // May not work
```

### 4. Anonymous Object Naming

JavaScript expects camelCase, but C# properties are PascalCase:

❌ **Without configuration:**
```csharp
new { UserName = "John" } // Becomes "UserName" in JSON
```

✅ **With proper JSON options:**
```csharp
// JSON serializer uses camelCase by default in Inertia
new { UserName = "John" } // Becomes "userName" in JSON
```

### 5. Middleware Registration Order

Middleware order matters in .NET:

❌ **Wrong:**
```csharp
app.UseInertia<HandleInertiaRequests>();
app.UseAuthentication(); // Too late!
```

✅ **Correct:**
```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseInertia<HandleInertiaRequests>();
```

## Feature Availability

| Feature | Laravel | .NET | Notes |
|---------|---------|------|-------|
| Basic Responses | ✅ | ✅ | Identical |
| Shared Data | ✅ | ✅ | Similar API |
| Asset Versioning | ✅ | ✅ | Identical |
| Partial Reloads | ✅ | ✅ | Identical |
| Optional Props | ✅ | ✅ | Called `Optional()` (not `Lazy()`) |
| Deferred Props | ✅ | ✅ | Identical |
| Always Props | ✅ | ✅ | Identical |
| Merge Props | ✅ | ✅ | Identical |
| Scroll Props | ✅ | ✅ | Identical |
| Once Props | ✅ | ✅ | Identical |
| SSR Support | ✅ | ✅ | Identical |
| History Encryption | ✅ | ✅ | Identical |
| Testing Utilities | ✅ | ✅ | Similar API |
| Artisan Commands | ✅ | ⏳ | Coming soon |

## Tips for Laravel Developers

### 1. Embrace Dependency Injection

Instead of facades, use constructor injection:

```csharp
public class UsersController : Controller
{
    private readonly IInertia _inertia;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UsersController> _logger;
    
    public UsersController(
        IInertia inertia,
        ApplicationDbContext db,
        ILogger<UsersController> logger)
    {
        _inertia = inertia;
        _db = db;
        _logger = logger;
    }
}
```

### 2. Use Options Pattern for Configuration

Instead of `config()` helper:

```csharp
public class MyService
{
    private readonly IOptions<MyOptions> _options;
    
    public MyService(IOptions<MyOptions> options)
    {
        _options = options;
    }
    
    public void DoSomething()
    {
        var value = _options.Value.SomeSetting;
    }
}
```

### 3. Learn LINQ

LINQ is similar to Laravel's query builder:

```csharp
// Laravel: User::where('active', true)->orderBy('name')->get()
// .NET:
var users = await _db.Users
    .Where(u => u.Active)
    .OrderBy(u => u.Name)
    .ToListAsync();
```

### 4. Use Async/Await Everywhere

Make all I/O operations async:

```csharp
// Database
await _db.SaveChangesAsync();

// HTTP
await _httpClient.GetAsync(url);

// File I/O
await File.WriteAllTextAsync(path, content);
```

### 5. Understand Middleware Pipeline

The middleware pipeline is explicit in .NET:

```csharp
app.UseStaticFiles();      // 1. Serve static files
app.UseRouting();          // 2. Match routes
app.UseAuthentication();   // 3. Authenticate
app.UseAuthorization();    // 4. Authorize
app.UseInertia<Handler>(); // 5. Handle Inertia
app.MapControllers();      // 6. Execute controllers
```

## Additional Resources

- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [C# Programming Guide](https://learn.microsoft.com/dotnet/csharp/)

## Getting Help

If you're stuck with migration:

1. Check the [API Mapping](../API_MAPPING.md) guide
2. Review [sample projects](../samples/)
3. Compare with [feature comparison](../FEATURE_COMPARISON.md)
4. Ask in [GitHub Discussions](https://github.com/idotta/inertia-dotnet/discussions)

## Next Steps

- Follow the [Getting Started](getting-started.md) guide
- Learn about [Property Types](properties.md)
- Explore [Middleware](middleware.md) customization
- Read about [Testing](testing.md) in .NET
