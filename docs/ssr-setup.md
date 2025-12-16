# Server-Side Rendering (SSR)

Enable server-side rendering with Inertia.js to improve initial page load performance and SEO.

## Overview

Server-side rendering allows your Inertia pages to be rendered on the server, improving:
- **Initial page load speed** - Faster time to first paint
- **SEO** - Search engines can crawl fully rendered pages
- **Social media sharing** - Proper meta tags and Open Graph data
- **Perceived performance** - Users see content immediately

## How It Works

1. **Client makes request** - Browser requests a page
2. **Server renders** - ASP.NET Core sends page data to Node.js SSR server
3. **SSR server responds** - Node.js renders the component to HTML
4. **Server returns HTML** - Fully rendered page sent to browser
5. **Hydration** - Client-side JavaScript takes over for interactivity

## Prerequisites

Before setting up SSR, ensure you have:
- **Node.js** 18+ or **Bun** 1.0+ installed
- An Inertia.js application already working with client-side rendering
- A bundler configured (Vite, Webpack, etc.)

## Quick Start

### 1. Install SSR Dependencies

```bash
npm install @inertiajs/react
# or
npm install @inertiajs/vue3
# or
npm install @inertiajs/svelte
```

### 2. Create SSR Entry Point

Create `resources/js/ssr.jsx` (or `.tsx`, `.vue`, `.svelte`):

**React:**
```jsx
import { createInertiaApp } from '@inertiajs/react'
import createServer from '@inertiajs/react/server'
import ReactDOMServer from 'react-dom/server'

createServer(page =>
  createInertiaApp({
    page,
    render: ReactDOMServer.renderToString,
    resolve: name => {
      const pages = import.meta.glob('./Pages/**/*.jsx', { eager: true })
      return pages[`./Pages/${name}.jsx`]
    },
    setup: ({ App, props }) => <App {...props} />
  })
)
```

**Vue:**
```javascript
import { createInertiaApp } from '@inertiajs/vue3'
import createServer from '@inertiajs/vue3/server'
import { renderToString } from '@vue/server-renderer'
import { createSSRApp, h } from 'vue'

createServer(page =>
  createInertiaApp({
    page,
    render: renderToString,
    resolve: name => {
      const pages = import.meta.glob('./Pages/**/*.vue', { eager: true })
      return pages[`./Pages/${name}.vue`]
    },
    setup({ App, props, plugin }) {
      return createSSRApp({
        render: () => h(App, props)
      }).use(plugin)
    }
  })
)
```

### 3. Configure Build Tool

**Vite Configuration:**

Update your `vite.config.js`:

```javascript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: 'wwwroot/build',
    manifest: true,
    rollupOptions: {
      input: {
        app: 'resources/js/app.jsx',
        ssr: 'resources/js/ssr.jsx'
      }
    }
  },
  ssr: {
    noExternal: ['@inertiajs/react', '@inertiajs/vue3', '@inertiajs/svelte']
  }
})
```

**Add NPM Scripts:**

```json
{
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "build:ssr": "vite build --ssr resources/js/ssr.jsx --outDir wwwroot/ssr"
  }
}
```

### 4. Build SSR Bundle

```bash
npm run build:ssr
```

This creates `wwwroot/ssr/ssr.mjs` (or `ssr.js`).

### 5. Configure ASP.NET Core

Update `Program.cs`:

```csharp
builder.Services.AddInertia(options =>
{
    options.RootView = "app";
    
    // Enable SSR
    options.Ssr.Enabled = true;
    options.Ssr.Url = "http://127.0.0.1:13714";
    
    // Optional: Specify bundle path (auto-detected if not set)
    // options.Ssr.Bundle = "wwwroot/ssr/ssr.mjs";
});
```

### 6. Start SSR Server

Start the SSR server in a separate terminal:

**Node.js:**
```bash
node wwwroot/ssr/ssr.mjs
```

**Bun:**
```bash
bun wwwroot/ssr/ssr.mjs
```

The server will listen on port 13714 by default.

### 7. Run Your Application

```bash
dotnet run
```

Your pages are now server-rendered!

## Configuration

### SSR Options

All SSR options are under `InertiaOptions.Ssr`:

```csharp
builder.Services.AddInertia(options =>
{
    // Enable/disable SSR
    options.Ssr.Enabled = true;
    
    // SSR server URL
    options.Ssr.Url = "http://127.0.0.1:13714";
    
    // Path to SSR bundle (auto-detected if not specified)
    options.Ssr.Bundle = "wwwroot/ssr/ssr.mjs";
    
    // Ensure bundle exists before enabling SSR
    options.Ssr.EnsureBundleExists = true;
});
```

### Auto-Detection

If you don't specify a bundle path, Inertia automatically searches these locations:

1. `wwwroot/ssr/ssr.mjs`
2. `wwwroot/ssr/ssr.js`
3. `bootstrap/ssr/ssr.mjs`
4. `bootstrap/ssr/ssr.js`
5. `public/ssr/ssr.mjs`
6. `public/ssr/ssr.js`

### Environment-Based Configuration

Use different SSR settings for different environments:

```csharp
builder.Services.AddInertia(options =>
{
    var isDevelopment = builder.Environment.IsDevelopment();
    
    options.Ssr.Enabled = !isDevelopment; // Disable SSR in development
    options.Ssr.Url = isDevelopment
        ? "http://127.0.0.1:13714"
        : "http://ssr-server:13714"; // Internal network in production
});
```

## SSR Gateway

The SSR gateway handles communication between ASP.NET Core and the SSR server.

### Health Checks

The gateway automatically checks if the SSR server is healthy:

```csharp
// Health check happens automatically
// If SSR fails, falls back to client-side rendering
```

To manually check health:

```csharp
public class StatusController : Controller
{
    private readonly IGateway _gateway;
    
    public StatusController(IGateway gateway)
    {
        _gateway = gateway;
    }
    
    public async Task<IActionResult> SsrHealth()
    {
        if (_gateway is IHasHealthCheck healthCheck)
        {
            var isHealthy = await healthCheck.IsHealthyAsync();
            return Ok(new { ssr = isHealthy ? "healthy" : "unavailable" });
        }
        
        return Ok(new { ssr = "unknown" });
    }
}
```

### Custom Gateway

Implement a custom gateway for advanced scenarios:

```csharp
using Inertia.Ssr;

public class CustomSsrGateway : IGateway, IHasHealthCheck
{
    public async Task<SsrResponse?> DispatchAsync(Dictionary<string, object?> pageData)
    {
        // Custom SSR logic
        // Send to your SSR server and get response
        
        return new SsrResponse
        {
            Head = "<title>Page Title</title>",
            Body = "<div>Rendered content</div>"
        };
    }
    
    public async Task<bool> IsHealthyAsync()
    {
        // Check if your SSR server is running
        return true;
    }
}

// Register in Program.cs
builder.Services.AddScoped<IGateway, CustomSsrGateway>();
```

## Production Deployment

### Process Management

Use a process manager to keep your SSR server running:

**PM2 (Node.js):**

```bash
npm install -g pm2

# Start SSR server
pm2 start wwwroot/ssr/ssr.mjs --name inertia-ssr

# Save configuration
pm2 save

# Auto-start on boot
pm2 startup
```

**systemd (Linux):**

Create `/etc/systemd/system/inertia-ssr.service`:

```ini
[Unit]
Description=Inertia SSR Server
After=network.target

[Service]
Type=simple
User=www-data
WorkingDirectory=/var/www/myapp
ExecStart=/usr/bin/node /var/www/myapp/wwwroot/ssr/ssr.mjs
Restart=always
Environment=NODE_ENV=production

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
sudo systemctl enable inertia-ssr
sudo systemctl start inertia-ssr
```

### Docker

**Dockerfile:**

```dockerfile
FROM node:18 AS ssr-builder
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build:ssr

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=ssr-builder /app/wwwroot ./wwwroot

# Install Node.js for SSR
RUN apt-get update && apt-get install -y nodejs

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

**docker-compose.yml:**

```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "80:80"
    depends_on:
      - ssr
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Inertia__Ssr__Url=http://ssr:13714

  ssr:
    image: node:18
    working_dir: /app
    volumes:
      - ./wwwroot/ssr:/app
    command: node ssr.mjs
    ports:
      - "13714:13714"
    restart: always
```

### Load Balancing

If running multiple app instances, use a shared SSR server:

```csharp
builder.Services.AddInertia(options =>
{
    // Point all instances to same SSR server
    options.Ssr.Url = "http://ssr-server.internal:13714";
});
```

## Graceful Fallback

Inertia automatically falls back to client-side rendering if SSR fails:

```csharp
// Fallback happens automatically when:
// - SSR server is unreachable
// - SSR server returns an error
// - SSR bundle doesn't exist (if EnsureBundleExists = false)
```

To customize fallback behavior:

```csharp
// In your root view (app.cshtml)
<inertia>
    <div id="app" data-page='@Json.Serialize(ViewData["InertiaPage"])'>
        @if (ViewData.ContainsKey("SsrFailed"))
        {
            <div class="ssr-fallback">
                Loading... (SSR unavailable)
            </div>
        }
    </div>
</inertia>
```

## Debugging

### Enable SSR Logging

```csharp
builder.Logging.AddFilter("Inertia.Ssr", LogLevel.Debug);
```

### Test SSR Endpoint

```bash
curl -X POST http://127.0.0.1:13714/render \
  -H "Content-Type: application/json" \
  -d '{"component":"Home/Index","props":{"message":"Hello"}}'
```

Expected response:

```json
{
  "head": "<title>Home</title>",
  "body": "<div>Rendered HTML</div>"
}
```

### Common Issues

**SSR server won't start:**
- Check if port 13714 is available
- Verify Node.js is installed and in PATH
- Check SSR bundle exists

**SSR renders but styles are missing:**
- Ensure CSS is included in the head
- Check that asset paths are absolute
- Verify Vite/Webpack config includes CSS in SSR build

**SSR works locally but not in production:**
- Check firewall rules
- Verify SSR server is running
- Check URL configuration matches deployment

## Advanced Features

### Custom Head Management

In your component:

```jsx
import { Head } from '@inertiajs/react'

export default function Page({ post }) {
  return (
    <>
      <Head>
        <title>{post.title}</title>
        <meta name="description" content={post.excerpt} />
        <meta property="og:title" content={post.title} />
        <meta property="og:image" content={post.image} />
      </Head>
      
      <article>
        <h1>{post.title}</h1>
        <div>{post.content}</div>
      </article>
    </>
  )
}
```

### SSR with Authentication

Pass authentication context to SSR:

```csharp
protected override Dictionary<string, object?> Share(HttpRequest request)
{
    return new Dictionary<string, object?>
    {
        ["auth"] = new
        {
            user = GetCurrentUser(request),
            // Pass only non-sensitive data
        }
    };
}
```

### Performance Monitoring

Monitor SSR performance:

```csharp
public class SsrPerformanceMiddleware : IMiddleware
{
    private readonly ILogger<SsrPerformanceMiddleware> _logger;
    
    public SsrPerformanceMiddleware(ILogger<SsrPerformanceMiddleware> logger)
    {
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        
        await next(context);
        
        stopwatch.Stop();
        _logger.LogInformation(
            "SSR render took {ElapsedMs}ms for {Path}",
            stopwatch.ElapsedMilliseconds,
            context.Request.Path
        );
    }
}
```

## Best Practices

1. **Use a process manager** - Keep SSR server running in production
2. **Monitor health** - Check SSR server status regularly
3. **Enable fallback** - Always have client-side rendering as backup
4. **Cache SSR responses** - Consider caching for public pages
5. **Optimize bundle size** - Keep SSR bundle small for faster startups
6. **Use environment variables** - Configure SSR per environment
7. **Test both modes** - Ensure app works with and without SSR

## Next Steps

- Learn about [Testing](testing.md) SSR-enabled applications
- Explore [Middleware](middleware.md) for advanced SSR customization
- Read [Property Types](properties.md) to optimize data loading
