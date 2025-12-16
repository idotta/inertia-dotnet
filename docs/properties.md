# Property Types

Inertia provides several property types to optimize how data is loaded and sent to your components.

## Overview

By default, all props are evaluated and sent with every Inertia response. Property types give you fine-grained control over when and how props are resolved:

- **Optional Props** - Only loaded when explicitly requested
- **Deferred Props** - Loaded asynchronously after the initial page render
- **Always Props** - Always included, even in partial reloads
- **Merge Props** - Merged with existing client-side data
- **Scroll Props** - Specialized merge for paginated/infinite scroll data
- **Once Props** - Cached and reused across navigations

## Import

```csharp
using static Inertia.LazyProps;
```

## Optional Props

Optional props (formerly called "Lazy" props) are only loaded when explicitly requested by the client.

### Usage

```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = await GetUsersAsync(), // Always loaded
    
    // Only loaded when requested
    stats = Optional(() => CalculateStats()),
    
    // Async optional prop
    reports = Optional(async () => await GenerateReportsAsync())
});
```

### Client-Side Request

```jsx
import { router } from '@inertiajs/react'

// Request optional props
router.reload({ only: ['stats'] })
```

### When to Use

- **Expensive computations** - Defer heavy calculations until needed
- **Large datasets** - Load big data only when the user requests it
- **Conditional features** - Load feature data only when the feature is activated

### Example: Tabs

```csharp
public async Task<IActionResult> Show(int id)
{
    var user = await _dbContext.Users.FindAsync(id);
    
    return await _inertia.RenderAsync("Users/Show", new
    {
        user = user,
        
        // Each tab's data loaded only when the tab is active
        posts = Optional(() => GetUserPosts(id)),
        followers = Optional(() => GetUserFollowers(id)),
        activity = Optional(() => GetUserActivity(id))
    });
}
```

```jsx
function UserProfile({ user, posts, followers, activity }) {
  const [activeTab, setActiveTab] = useState('posts')
  
  useEffect(() => {
    router.reload({ only: [activeTab] })
  }, [activeTab])
  
  return (
    <div>
      <h1>{user.name}</h1>
      <Tabs activeTab={activeTab} onChange={setActiveTab}>
        <Tab name="posts">{posts && <Posts data={posts} />}</Tab>
        <Tab name="followers">{followers && <Followers data={followers} />}</Tab>
        <Tab name="activity">{activity && <Activity data={activity} />}</Tab>
      </Tabs>
    </div>
  )
}
```

## Deferred Props

Deferred props are loaded asynchronously after the initial page render, allowing the page to display quickly.

### Usage

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    user = await GetUserAsync(), // Loaded immediately
    
    // Loaded after page renders
    stats = Defer(() => CalculateStatsAsync()),
    notifications = Defer(() => GetNotificationsAsync())
});
```

### With Groups

Group deferred props to load them together:

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    user = await GetUserAsync(),
    
    // Primary group - loaded first
    stats = Defer(() => GetStatsAsync(), "primary"),
    
    // Secondary group - loaded after primary
    logs = Defer(() => GetLogsAsync(), "secondary"),
    reports = Defer(() => GetReportsAsync(), "secondary")
});
```

### Client-Side

Deferred props are automatically loaded after the page renders. Display loading states:

```jsx
function Dashboard({ user, stats, notifications }) {
  return (
    <div>
      <h1>Welcome, {user.name}</h1>
      
      {stats ? (
        <Stats data={stats} />
      ) : (
        <div>Loading stats...</div>
      )}
      
      {notifications ? (
        <Notifications data={notifications} />
      ) : (
        <div>Loading notifications...</div>
      )}
    </div>
  )
}
```

### When to Use

- **Above-the-fold priority** - Load critical content first, defer secondary content
- **Perceived performance** - Show page structure immediately, fill in details
- **Heavy queries** - Defer expensive database queries to load after initial render

## Always Props

Always props are included in every response, even during partial reloads.

### Usage

```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    // Always included, even in partial reloads
    user = Always(() => GetCurrentUser()),
    permissions = Always(() => GetPermissions()),
    
    // Only included in full loads and when requested
    users = await GetUsersAsync()
});
```

### When to Use

- **Authentication data** - Always include current user information
- **Permissions** - Security context should always be fresh
- **Critical state** - Data that must be up-to-date on every request

## Merge Props

Merge props combine server data with existing client data instead of replacing it.

### Shallow Merge

```csharp
return await _inertia.RenderAsync("Posts/Index", new
{
    posts = await GetPostsAsync(),
    
    // Merged with existing filters on client
    filters = Merge(() => new
    {
        search = Request.Query["search"].ToString(),
        status = Request.Query["status"].ToString()
    })
});
```

### Deep Merge

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    settings = Merge(() => GetSettings()).DeepMerge()
});
```

### Merge Path

Merge into a specific nested path:

```csharp
return await _inertia.RenderAsync("Users/Index", new
{
    users = Merge(() => GetUsers()).WithPath("data")
});
```

### Only on Partial

Merge only during partial reloads:

```csharp
return await _inertia.RenderAsync("Posts/Index", new
{
    posts = Merge(() => GetPosts()).OnlyOnPartial()
});
```

## Scroll Props

Scroll props are specialized merge props for pagination and infinite scrolling.

### Append (Infinite Scroll)

```csharp
public async Task<IActionResult> Index(int page = 1)
{
    var posts = await GetPostsAsync(page);
    
    return await _inertia.RenderAsync("Posts/Index", new
    {
        posts = Scroll(
            data: posts,
            metadata: ScrollMetadata.FromPaginator(
                currentPage: page,
                hasMorePages: posts.HasNextPage
            )
        ).Append()
    });
}
```

### Client-Side

```jsx
import { router } from '@inertiajs/react'

function PostsList({ posts }) {
  const [page, setPage] = useState(1)
  
  const loadMore = () => {
    const nextPage = page + 1
    setPage(nextPage)
    router.reload({
      only: ['posts'],
      data: { page: nextPage }
    })
  }
  
  return (
    <div>
      {posts.data.map(post => (
        <Post key={post.id} {...post} />
      ))}
      
      {posts.hasMore && (
        <button onClick={loadMore}>Load More</button>
      )}
    </div>
  )
}
```

### Prepend (Reverse Chronological)

For feeds where new items appear at the top:

```csharp
return await _inertia.RenderAsync("Feed/Index", new
{
    posts = Scroll(
        data: posts,
        metadata: ScrollMetadata.FromPaginator(page, hasMore)
    ).Prepend()
});
```

### With Eloquent Pagination

```csharp
var posts = await _dbContext.Posts
    .OrderByDescending(p => p.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

var hasMore = await _dbContext.Posts.CountAsync() > page * pageSize;

return await _inertia.RenderAsync("Posts/Index", new
{
    posts = Scroll(
        data: posts,
        metadata: ScrollMetadata.FromPaginator(page, hasMore)
    ).Append("data")
});
```

### Custom Metadata

Implement `IProvidesScrollMetadata` for custom pagination metadata:

```csharp
public class CustomPaginator : IProvidesScrollMetadata
{
    private readonly int _currentPage;
    private readonly int _totalPages;
    
    public CustomPaginator(int currentPage, int totalPages)
    {
        _currentPage = currentPage;
        _totalPages = totalPages;
    }
    
    public string GetPageName() => "page";
    public object? GetPreviousPage() => _currentPage > 1 ? _currentPage - 1 : null;
    public object? GetNextPage() => _currentPage < _totalPages ? _currentPage + 1 : null;
    public object? GetCurrentPage() => _currentPage;
}
```

## Once Props

Once props are cached on the client and reused across navigations within the same session.

### Usage

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    // Loaded once per session
    countries = Once(() => GetCountries()),
    timezones = Once(() => GetTimezones()),
    
    // Loaded on every request
    user = await GetUserAsync()
});
```

### When to Use

- **Static reference data** - Countries, timezones, currencies
- **App configuration** - Settings that don't change often
- **User preferences** - Theme, language, layout preferences

### How It Works

1. First visit: Props are loaded and sent to the client
2. Subsequent visits: Client sends `X-Inertia-Except-Once-Props: true` header
3. Server skips once props in response
4. Client reuses cached values

## Property Providers

Create reusable property classes that implement `IProvidesInertiaProperty`:

```csharp
using Inertia;

public class CurrentUserProperty : IProvidesInertiaProperty
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<User> _userManager;
    
    public CurrentUserProperty(
        IHttpContextAccessor httpContextAccessor,
        UserManager<User> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }
    
    public async Task<object?> ToInertiaProperty(object context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var appUser = await _userManager.GetUserAsync(user);
            return new
            {
                id = appUser.Id,
                name = appUser.UserName,
                email = appUser.Email
            };
        }
        return null;
    }
}
```

Use in controllers:

```csharp
public async Task<IActionResult> Index([FromServices] CurrentUserProperty userProp)
{
    return await _inertia.RenderAsync("Dashboard", new
    {
        user = userProp, // Automatically resolved
        stats = await GetStatsAsync()
    });
}
```

### Multiple Properties

Implement `IProvidesInertiaProperties` to provide multiple props:

```csharp
public class SharedDataProvider : IProvidesInertiaProperties
{
    private readonly IConfiguration _config;
    
    public SharedDataProvider(IConfiguration config)
    {
        _config = config;
    }
    
    public Dictionary<string, object?> ToInertiaProperties(object context)
    {
        return new Dictionary<string, object?>
        {
            ["appName"] = _config["AppName"],
            ["appVersion"] = _config["Version"],
            ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        };
    }
}
```

## Combining Property Types

Property types can be combined for advanced scenarios:

```csharp
return await _inertia.RenderAsync("Dashboard", new
{
    // Always load current user
    user = Always(() => GetCurrentUser()),
    
    // Load on request, but merge with existing data
    notifications = Optional(() => Merge(() => GetNotifications())),
    
    // Defer loading and merge results
    stats = Defer(() => Merge(() => GetStats())),
    
    // Load once per session, with fallback
    settings = Once(() => GetSettings() ?? GetDefaultSettings())
});
```

## Best Practices

1. **Use Optional for expensive data** - Defer loading until the user needs it
2. **Use Deferred for secondary content** - Improve perceived performance
3. **Use Always for security context** - Ensure auth data is always fresh
4. **Use Merge for filters and state** - Preserve client-side state
5. **Use Scroll for pagination** - Implement infinite scroll efficiently
6. **Use Once for static data** - Reduce bandwidth for reference data
7. **Test partial reloads** - Ensure your app works correctly with partial data

## Performance Tips

- **Minimize optional props** - Each optional prop requires an additional request
- **Group deferred props** - Load related data together
- **Cache once props** - Use for data that rarely changes
- **Profile queries** - Monitor database performance for prop callbacks
- **Use async methods** - Leverage async/await for I/O operations

## Next Steps

- Learn about [Server-Side Rendering](ssr-setup.md) to improve initial page load
- Explore [Testing](testing.md) to test property resolution
- Read [Middleware](middleware.md) to understand shared data flow
