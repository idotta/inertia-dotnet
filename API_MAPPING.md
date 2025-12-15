# API Mapping: Laravel to .NET

Quick reference guide for translating inertia-laravel code to inertia-dotnet.

## Table of Contents
1. [Service Registration](#service-registration)
2. [Rendering Responses](#rendering-responses)
3. [Shared Data](#shared-data)
4. [Property Types](#property-types)
5. [Middleware](#middleware)
6. [SSR Configuration](#ssr-configuration)
7. [Testing](#testing)
8. [Headers](#headers)

---

## Service Registration

### Laravel
```php
// Automatic via ServiceProvider in config/app.php
'providers' => [
    Inertia\ServiceProvider::class,
],
```

### .NET
```csharp
// Startup.cs or Program.cs
services.AddInertia(options =>
{
    options.RootView = "app";
    options.SsrEnabled = true;
    options.SsrUrl = "http://127.0.0.1:13714";
});

// Middleware registration
app.UseInertia<HandleInertiaRequests>();
```

---

## Rendering Responses

### Basic Render

**Laravel:**
```php
use Inertia\Inertia;

return Inertia::render('Users/Index', [
    'users' => User::all(),
]);
```

**C#/.NET:**
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

### With Multiple Props

**Laravel:**
```php
return Inertia::render('Dashboard', [
    'stats' => Stats::calculate(),
    'notifications' => auth()->user()->notifications,
    'user' => auth()->user(),
]);
```

**C#/.NET:**
```csharp
public async Task<IActionResult> Dashboard()
{
    return await _inertia.RenderAsync("Dashboard", new
    {
        stats = await Stats.CalculateAsync(),
        notifications = await _userManager.GetNotificationsAsync(User),
        user = await _userManager.GetUserAsync(User)
    });
}
```

---

## Shared Data

### Share in Middleware

**Laravel:**
```php
// app/Http/Middleware/HandleInertiaRequests.php
class HandleInertiaRequests extends Middleware
{
    public function share(Request $request)
    {
        return array_merge(parent::share($request), [
            'auth' => [
                'user' => $request->user() ? [
                    'id' => $request->user()->id,
                    'name' => $request->user()->name,
                    'email' => $request->user()->email,
                ] : null,
            ],
            'flash' => [
                'message' => fn () => $request->session()->get('message'),
            ],
        ]);
    }
}
```

**C#/.NET:**
```csharp
// HandleInertiaRequests.cs
public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    private readonly UserManager<ApplicationUser> _userManager;
    
    public HandleInertiaRequests(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }
    
    public override async Task<Dictionary<string, object>> Share(HttpRequest request)
    {
        var baseShared = await base.Share(request);
        
        var user = await _userManager.GetUserAsync(request.HttpContext.User);
        
        return new Dictionary<string, object>(baseShared)
        {
            ["auth"] = new
            {
                user = user != null ? new
                {
                    id = user.Id,
                    name = user.UserName,
                    email = user.Email
                } : null
            },
            ["flash"] = new
            {
                message = Optional(() => request.HttpContext.TempData["message"])
            }
        };
    }
}
```

### Share Globally

**Laravel:**
```php
// In a service provider or controller
Inertia::share('appName', config('app.name'));
Inertia::share('ziggy', function () {
    return (new Ziggy)->toArray();
});
```

**C#/.NET:**
```csharp
// In middleware or controller
_inertia.Share("appName", _configuration["AppName"]);
_inertia.Share("routes", () => _routeHelper.GetAllRoutes());
```

---

## Property Types

### Optional Props (Lazy)

**Laravel:**
```php
return Inertia::render('Users/Index', [
    'users' => User::paginate(),
    'filters' => Inertia::optional(fn() => $request->all()),
]);
```

**C#/.NET:**
```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = await _context.Users.ToPagedListAsync(page, pageSize),
    filters = Optional(() => Request.Query.ToDictionary(k => k.Key, v => v.Value))
});
```

### Deferred Props

**Laravel:**
```php
return Inertia::render('Dashboard', [
    'recentActivity' => Inertia::defer(fn() => Activity::recent()),
    'stats' => Inertia::defer(fn() => Stats::calculate(), 'analytics'),
]);
```

**C#/.NET:**
```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    recentActivity = Defer(async () => await _context.Activities.RecentAsync()),
    stats = Defer(async () => await Stats.CalculateAsync(), "analytics")
});
```

### Always Props

**Laravel:**
```php
return Inertia::render('Settings', [
    'errors' => Inertia::always($request->session()->get('errors')),
]);
```

**C#/.NET:**
```csharp
return await _inertia.RenderAsync("Settings", new
{
    errors = Always(() => TempData["errors"])
});
```

### Merge Props

**Laravel:**
```php
return Inertia::render('Users/Index', [
    'users' => Inertia::merge(User::paginate()),
    'filters' => Inertia::deepMerge($filters),
]);
```

**C#/.NET:**
```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = Merge(await _context.Users.ToPagedListAsync(page, pageSize)),
    filters = DeepMerge(filters)
});
```

### Scroll Props (Pagination)

**Laravel:**
```php
return Inertia::render('Posts/Index', [
    'posts' => Inertia::scroll(
        Post::paginate(15),
        metadata: fn($paginator) => ScrollMetadata::fromPaginator($paginator)
    ),
]);
```

**C#/.NET:**
```csharp
return await _inertia.RenderAsync("Posts/Index", new
{
    posts = Scroll(
        await _context.Posts.ToPagedListAsync(page, 15),
        wrapper: "data",
        metadata: paginator => ScrollMetadata.FromPagedList(paginator)
    )
});
```

### Once Props

**Laravel:**
```php
// In middleware
public function shareOnce(Request $request): array
{
    return [
        'translations' => fn() => Lang::get('messages'),
    ];
}

// Or inline
return Inertia::render('Dashboard', [
    'config' => Inertia::once(fn() => Config::get('app')),
]);
```

**C#/.NET:**
```csharp
// In middleware
public override Dictionary<string, object> ShareOnce(HttpRequest request)
{
    return new Dictionary<string, object>
    {
        ["translations"] = () => _localizer.GetAllStrings()
    };
}

// Or inline
return await _inertia.RenderAsync("Dashboard", new
{
    config = Once(() => _configuration.GetSection("App").Get<Dictionary<string, string>>())
});
```

---

## Middleware

### Custom Middleware

**Laravel:**
```php
namespace App\Http\Middleware;

use Inertia\Middleware;

class HandleInertiaRequests extends Middleware
{
    protected $rootView = 'app';
    
    public function version(Request $request)
    {
        return parent::version($request);
    }
    
    public function share(Request $request)
    {
        return array_merge(parent::share($request), [
            'auth' => ['user' => $request->user()],
        ]);
    }
}
```

**C#/.NET:**
```csharp
namespace MyApp.Middleware;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    protected string RootView => "app";
    
    public override string Version(HttpRequest request)
    {
        // Return version based on manifest file
        var manifestPath = Path.Combine(_env.WebRootPath, "build", "manifest.json");
        if (File.Exists(manifestPath))
        {
            return ComputeHash(manifestPath);
        }
        return base.Version(request);
    }
    
    public override async Task<Dictionary<string, object>> Share(HttpRequest request)
    {
        var baseShared = await base.Share(request);
        var user = await _userManager.GetUserAsync(request.HttpContext.User);
        
        return new Dictionary<string, object>(baseShared)
        {
            ["auth"] = new { user }
        };
    }
}
```

### Version Checking

**Laravel:**
```php
public function version(Request $request)
{
    if (file_exists($manifest = public_path('build/manifest.json'))) {
        return hash_file('xxh128', $manifest);
    }
    return parent::version($request);
}
```

**C#/.NET:**
```csharp
public override string Version(HttpRequest request)
{
    var manifestPath = Path.Combine(_env.WebRootPath, "build", "manifest.json");
    if (File.Exists(manifestPath))
    {
        using var stream = File.OpenRead(manifestPath);
        using var hash = System.Security.Cryptography.MD5.Create();
        var hashBytes = hash.ComputeHash(stream);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
    return base.Version(request);
}
```

---

## SSR Configuration

### Laravel
```php
// config/inertia.php
return [
    'ssr' => [
        'enabled' => env('INERTIA_SSR_ENABLED', true),
        'url' => env('INERTIA_SSR_URL', 'http://127.0.0.1:13714'),
        'bundle' => base_path('bootstrap/ssr/ssr.mjs'),
    ],
];
```

### .NET
```csharp
// appsettings.json
{
  "Inertia": {
    "Ssr": {
      "Enabled": true,
      "Url": "http://127.0.0.1:13714",
      "Bundle": "wwwroot/ssr/ssr.mjs"
    }
  }
}

// Or in Startup.cs
services.AddInertia(options =>
{
    options.Ssr.Enabled = true;
    options.Ssr.Url = "http://127.0.0.1:13714";
    options.Ssr.Bundle = Path.Combine("wwwroot", "ssr", "ssr.mjs");
});
```

---

## Testing

### Basic Assertions

**Laravel (PHPUnit):**
```php
public function test_users_page_returns_users()
{
    $response = $this->get('/users');
    
    $response->assertInertia(fn ($page) => $page
        ->component('Users/Index')
        ->has('users', 10)
        ->has('users.0', fn ($user) => $user
            ->where('id', 1)
            ->where('name', 'John Doe')
        )
    );
}
```

**C#/.NET (xUnit):**
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

### Partial Reload Testing

**Laravel:**
```php
public function test_partial_reload()
{
    $response = $this
        ->followingRedirects()
        ->withHeaders([
            'X-Inertia' => 'true',
            'X-Inertia-Partial-Data' => 'users',
            'X-Inertia-Partial-Component' => 'Users/Index',
        ])
        ->get('/users');
        
    $response->assertInertia(fn ($page) => $page
        ->has('users')
        ->missing('filters')
    );
}
```

**C#/.NET:**
```csharp
[Fact]
public async Task PartialReload_ReturnsOnlyRequestedProps()
{
    var request = new HttpRequestMessage(HttpMethod.Get, "/users");
    request.Headers.Add("X-Inertia", "true");
    request.Headers.Add("X-Inertia-Partial-Data", "users");
    request.Headers.Add("X-Inertia-Partial-Component", "Users/Index");
    
    var response = await _client.SendAsync(request);
    
    response.Should().BeInertia()
        .Has("users")
        .Missing("filters");
}
```

### Deferred Props Testing

**Laravel:**
```php
public function test_deferred_props()
{
    $response = $this->get('/dashboard');
    
    $response->assertInertia(fn ($page) => $page
        ->component('Dashboard')
        ->missing('stats') // Not loaded initially
    );
    
    $response = $this->loadDeferredProps($response);
    
    $response->assertInertia(fn ($page) => $page
        ->has('stats') // Now loaded
    );
}
```

**C#/.NET:**
```csharp
[Fact]
public async Task DeferredProps_LoadedOnSecondRequest()
{
    var response = await _client.GetAsync("/dashboard");
    
    response.Should().BeInertia()
        .WithComponent("Dashboard")
        .Missing("stats"); // Not loaded initially
    
    var deferredResponse = await LoadDeferredProps(response);
    
    deferredResponse.Should().BeInertia()
        .Has("stats"); // Now loaded
}
```

---

## Headers

### Setting Headers in Controllers

**Laravel:**
```php
return Inertia::location($url); // Returns 409 with X-Inertia-Location header
```

**C#/.NET:**
```csharp
return _inertia.Location(url); // Returns 409 with X-Inertia-Location header
```

### Reading Headers

**Laravel:**
```php
$request->header('X-Inertia'); // Check if Inertia request
$request->inertia(); // Using macro
```

**C#/.NET:**
```csharp
Request.Headers["X-Inertia"]; // Raw header access
Request.IsInertia(); // Using extension method
```

### All Inertia Headers

| Header | Laravel | C#/.NET |
|--------|---------|---------|
| Request detection | `X-Inertia` | `InertiaHeaders.Inertia` |
| Version | `X-Inertia-Version` | `InertiaHeaders.Version` |
| Partial data | `X-Inertia-Partial-Data` | `InertiaHeaders.PartialData` |
| Partial component | `X-Inertia-Partial-Component` | `InertiaHeaders.PartialComponent` |
| Partial except | `X-Inertia-Partial-Except` | `InertiaHeaders.PartialExcept` |
| Error bag | `X-Inertia-Error-Bag` | `InertiaHeaders.ErrorBag` |
| Location (409) | `X-Inertia-Location` | `InertiaHeaders.Location` |
| Reset | `X-Inertia-Reset` | `InertiaHeaders.Reset` |
| Scroll intent | `X-Inertia-Infinite-Scroll-Merge-Intent` | `InertiaHeaders.InfiniteScrollMergeIntent` |

---

## Form Handling

### Laravel
```php
public function store(Request $request)
{
    $validated = $request->validate([
        'name' => 'required|string|max:255',
        'email' => 'required|email',
    ]);
    
    User::create($validated);
    
    return redirect()->route('users.index')
        ->with('message', 'User created successfully.');
}
```

### C#/.NET
```csharp
[HttpPost]
public async Task<IActionResult> Store(CreateUserRequest request)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }
    
    var user = new User
    {
        Name = request.Name,
        Email = request.Email
    };
    
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
    
    TempData["message"] = "User created successfully.";
    return RedirectToAction("Index");
}

public class CreateUserRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
```

---

## Validation Errors

### Laravel
```php
// Automatic via HandleInertiaRequests
public function resolveValidationErrors(Request $request)
{
    if (!$request->hasSession() || !$request->session()->has('errors')) {
        return (object)[];
    }
    
    return (object)collect($request->session()->get('errors')->getBags())
        ->map->messages()
        ->toArray();
}
```

### C#/.NET
```csharp
// In HandleInertiaRequests
public override object ResolveValidationErrors(HttpContext context)
{
    if (!context.Request.HasFormContentType)
    {
        return new { };
    }
    
    var errors = new Dictionary<string, string[]>();
    
    foreach (var key in ModelState.Keys)
    {
        var state = ModelState[key];
        if (state.Errors.Count > 0)
        {
            errors[key] = state.Errors
                .Select(e => e.ErrorMessage)
                .ToArray();
        }
    }
    
    return errors;
}
```

---

## Route Helper

### Laravel (with Ziggy)
```php
// Shared data
'ziggy' => fn () => [
    'url' => url('/'),
    'routes' => app('router')->getRoutes(),
],
```

### C#/.NET
```csharp
// Create a route helper
public class RouteHelper
{
    private readonly LinkGenerator _linkGenerator;
    
    public RouteHelper(LinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }
    
    public Dictionary<string, string> GetAllRoutes()
    {
        // Generate route dictionary
        return new Dictionary<string, string>
        {
            ["users.index"] = _linkGenerator.GetPathByAction("Index", "Users"),
            ["users.show"] = _linkGenerator.GetPathByAction("Show", "Users"),
            // ... more routes
        };
    }
}

// In HandleInertiaRequests
public override async Task<Dictionary<string, object>> Share(HttpRequest request)
{
    return new Dictionary<string, object>
    {
        ["routes"] = Once(() => _routeHelper.GetAllRoutes())
    };
}
```

---

## Common Patterns

### Returning Inertia or JSON

**Laravel:**
```php
public function index(Request $request)
{
    $users = User::paginate();
    
    if ($request->wantsJson()) {
        return response()->json($users);
    }
    
    return Inertia::render('Users/Index', [
        'users' => $users,
    ]);
}
```

**C#/.NET:**
```csharp
public async Task<IActionResult> Index()
{
    var users = await _context.Users.ToPagedListAsync(page, pageSize);
    
    if (Request.Headers["Accept"].ToString().Contains("application/json"))
    {
        return Json(users);
    }
    
    return await _inertia.RenderAsync("Users/Index", new
    {
        users
    });
}
```

### File Downloads

**Laravel:**
```php
public function download()
{
    return response()->download($path);
}
```

**C#/.NET:**
```csharp
public IActionResult Download()
{
    var fileBytes = System.IO.File.ReadAllBytes(path);
    return File(fileBytes, "application/octet-stream", "filename.pdf");
}
```

---

## Type Conversions

### PHP → C# Types

| PHP | C# |
|-----|-----|
| `array` | `Dictionary<string, object>` or `List<T>` |
| `callable` | `Func<T>` or `Action<T>` |
| `Closure` | Lambda expression `() => { }` |
| `string` | `string` |
| `int` | `int` |
| `bool` | `bool` |
| `float` | `double` or `decimal` |
| `null` | `null` |
| `object` | `object` or specific class |
| `mixed` | `object` |
| `Collection` | `IEnumerable<T>` or `List<T>` |
| `Paginator` | Custom or `IPagedList<T>` |

### Naming Conventions

| PHP (snake_case) | C# (PascalCase) |
|------------------|------------------|
| `render()` | `Render()` / `RenderAsync()` |
| `shared_props` | `SharedProps` |
| `get_version()` | `GetVersion()` |
| `set_root_view()` | `SetRootView()` |
| `resolve_validation_errors()` | `ResolveValidationErrors()` |

---

## Quick Reference Table

| Feature | Laravel | C#/.NET |
|---------|---------|---------|
| **Service Setup** | ServiceProvider | `services.AddInertia()` |
| **Render** | `Inertia::render()` | `inertia.RenderAsync()` |
| **Share** | `Inertia::share()` | `inertia.Share()` |
| **Optional** | `Inertia::optional()` | `Optional()` |
| **Defer** | `Inertia::defer()` | `Defer()` |
| **Always** | `Inertia::always()` | `Always()` |
| **Merge** | `Inertia::merge()` | `Merge()` |
| **Scroll** | `Inertia::scroll()` | `Scroll()` |
| **Once** | `Inertia::once()` | `Once()` |
| **Location** | `Inertia::location()` | `inertia.Location()` |
| **Version** | `version()` in middleware | `Version()` override |
| **Validation** | `resolveValidationErrors()` | `ResolveValidationErrors()` |
| **Testing** | `->assertInertia()` | `.BeInertia()` |

---

**Document Version:** 1.0  
**Last Updated:** 2025-12-15  
**Target:** inertia-dotnet v1.0.0
