# Getting Started with Inertia.js for .NET

This guide will walk you through setting up Inertia.js with ASP.NET Core.

## What is Inertia.js?

Inertia.js lets you build modern single-page applications using classic server-side routing and controllers. It works by replacing traditional server-rendered views with client-side components (React, Vue, Svelte), while keeping your application logic server-side.

### Benefits

- 📦 **No API needed** - Your controllers return props directly to your components
- 🎯 **Server-side routing** - Use familiar MVC patterns, no client-side router needed
- ⚡ **Fast navigation** - Subsequent page visits are made via XHR with automatic SPA behavior
- 🔒 **Security** - Keep business logic server-side where it belongs
- 🎨 **Component-based UI** - Use modern frontend frameworks you already know

## Prerequisites

Before you begin, ensure you have:

- **.NET 6.0 or later** installed
- **Node.js** (for frontend tooling and SSR)
- Basic knowledge of **ASP.NET Core MVC**
- Familiarity with a frontend framework (**React**, **Vue**, or **Svelte**)

## Installation

### 1. Create a New ASP.NET Core Project

```bash
dotnet new web -n MyInertiaApp
cd MyInertiaApp
```

### 2. Install Inertia.AspNetCore

```bash
dotnet add package Inertia.AspNetCore
```

### 3. Install Inertia Client-Side Adapter

Choose your frontend framework:

**React:**
```bash
npm install @inertiajs/react react react-dom
npm install -D @vitejs/plugin-react
```

**Vue:**
```bash
npm install @inertiajs/vue3 vue
npm install -D @vitejs/plugin-vue
```

**Svelte:**
```bash
npm install @inertiajs/svelte svelte
npm install -D @sveltejs/vite-plugin-svelte
```

## Configuration

### 1. Configure Program.cs

Update your `Program.cs` to register Inertia services:

```csharp
using Inertia.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// Configure Inertia
builder.Services.AddInertia(options =>
{
    options.RootView = "app"; // Name of your root Razor view
});

var app = builder.Build();

// Configure middleware pipeline
app.UseStaticFiles();
app.UseRouting();

// Add Inertia middleware
app.UseInertia<MyInertiaApp.Middleware.HandleInertiaRequests>();

app.MapControllers();
app.Run();
```

### 2. Create Inertia Middleware

Create a middleware handler at `Middleware/HandleInertiaRequests.cs`:

```csharp
using Inertia.AspNetCore;

namespace MyInertiaApp.Middleware;

public class HandleInertiaRequests : Inertia.AspNetCore.HandleInertiaRequests
{
    /// <summary>
    /// Returns the asset version for cache busting.
    /// </summary>
    protected override string? Version(HttpRequest request)
    {
        // Return a hash of your assets (from manifest, git commit, etc.)
        return "1.0.0";
    }

    /// <summary>
    /// Returns data that should be shared with all Inertia requests.
    /// </summary>
    protected override Dictionary<string, object?> Share(HttpRequest request)
    {
        return new Dictionary<string, object?>
        {
            // Share app-wide data
            ["appName"] = "My Inertia App",
            
            // Share authenticated user (if using authentication)
            // ["user"] = GetAuthenticatedUser(request),
            
            // Share flash messages
            // ["flash"] = GetFlashMessages(request),
        };
    }
}
```

### 3. Create Root View

Create your root Razor view at `Views/Shared/app.cshtml`:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title inertia>My Inertia App</title>
    
    @* Inertia head content (for SSR) *@
    <inertia-head />
    
    @* Your CSS *@
    <link rel="stylesheet" href="~/css/app.css" asp-append-version="true" />
</head>
<body>
    @* Inertia root element *@
    <inertia />
    
    @* Your JavaScript bundle *@
    <script src="~/js/app.js" asp-append-version="true"></script>
</body>
</html>
```

### 4. Set Up Your Frontend

Create your Inertia app entry point. Here's an example with React:

**app.jsx:**
```jsx
import { createRoot } from 'react-dom/client'
import { createInertiaApp } from '@inertiajs/react'

createInertiaApp({
  resolve: name => {
    const pages = import.meta.glob('./Pages/**/*.jsx', { eager: true })
    return pages[`./Pages/${name}.jsx`]
  },
  setup({ el, App, props }) {
    createRoot(el).render(<App {...props} />)
  },
})
```

**Configure Vite (vite.config.js):**
```javascript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot',
    manifest: true,
    rollupOptions: {
      input: 'resources/js/app.jsx'
    }
  }
})
```

## Creating Your First Page

### 1. Create a Controller

Create `Controllers/HomeController.cs`:

```csharp
using Inertia;
using Microsoft.AspNetCore.Mvc;

namespace MyInertiaApp.Controllers;

public class HomeController : Controller
{
    private readonly IInertia _inertia;

    public HomeController(IInertia inertia)
    {
        _inertia = inertia;
    }

    public async Task<IActionResult> Index()
    {
        return await _inertia.RenderAsync("Home/Index", new
        {
            message = "Welcome to Inertia.js with .NET!"
        });
    }
}
```

### 2. Create a Frontend Component

Create `resources/js/Pages/Home/Index.jsx`:

```jsx
import { Head } from '@inertiajs/react'

export default function Index({ message }) {
  return (
    <>
      <Head title="Home" />
      
      <div className="container">
        <h1>{message}</h1>
        <p>You're viewing an Inertia.js page powered by ASP.NET Core!</p>
      </div>
    </>
  )
}
```

### 3. Configure Routing

Add a route in `Program.cs`:

```csharp
app.MapGet("/", () => Results.RedirectToAction("Index", "Home"));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

## Running Your Application

### 1. Build Frontend Assets

```bash
npm run dev
# or for production:
npm run build
```

### 2. Run the Application

```bash
dotnet run
```

Navigate to `http://localhost:5000` - you should see your Inertia page!

## Next Steps

Now that you have a basic Inertia.js application running, explore more features:

- **[Responses](responses.md)** - Learn about different response types
- **[Property Types](properties.md)** - Optimize data loading with lazy, deferred, and merge props
- **[Middleware](middleware.md)** - Share data across all requests
- **[Server-Side Rendering](ssr-setup.md)** - Enable SSR for better performance and SEO
- **[Testing](testing.md)** - Test your Inertia responses

## Common Issues

### The page loads but navigation doesn't work

Ensure you've properly configured your frontend adapter and that the Inertia middleware is registered.

### Props aren't being passed to components

Check that your controller is using `await _inertia.RenderAsync()` and returning the result.

### Static files aren't loading

Make sure `app.UseStaticFiles()` is called before `app.UseInertia()` in your middleware pipeline.

### Asset version mismatches

Implement the `Version()` method in your `HandleInertiaRequests` class to return a hash based on your asset manifest.

## Additional Resources

- [Inertia.js Official Documentation](https://inertiajs.com/)
- [API Reference](../API_MAPPING.md)
- [Sample Projects](../samples/)
- [GitHub Repository](https://github.com/idotta/inertia-dotnet)
