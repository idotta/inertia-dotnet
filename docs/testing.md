# Testing Inertia Applications

Learn how to test your Inertia.js applications with comprehensive testing utilities.

## Overview

The `Inertia.Testing` package provides fluent assertions and utilities for testing Inertia responses in your ASP.NET Core applications.

## Installation

The testing utilities are included in the main package:

```bash
dotnet add package Inertia.AspNetCore
```

## Basic Testing

### Setup Test Project

Create a test project if you haven't already:

```bash
dotnet new xunit -n MyApp.Tests
cd MyApp.Tests
dotnet add reference ../MyApp/MyApp.csproj
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Inertia.AspNetCore
```

### Create Test Class

```csharp
using Inertia.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task IndexReturnsInertiaResponse()
    {
        // Act
        var response = await _client.GetAsync("/users");
        
        // Assert
        response.AssertInertia(inertia => inertia
            .WithComponent("Users/Index")
            .Has("users")
        );
    }
}
```

## Asserting Responses

### Component Assertions

Assert that the correct component is rendered:

```csharp
[Fact]
public async Task ShowRendersUserComponent()
{
    var response = await _client.GetAsync("/users/1");
    
    response.AssertInertia(inertia => inertia
        .WithComponent("Users/Show")
    );
}
```

### URL Assertions

Assert the response URL:

```csharp
[Fact]
public async Task CreateRedirectsToShow()
{
    var response = await _client.PostAsync("/users", content);
    
    response.AssertInertia(inertia => inertia
        .WithUrl("/users/123")
    );
}
```

### Version Assertions

Assert asset version:

```csharp
[Fact]
public async Task ResponseHasCorrectVersion()
{
    var response = await _client.GetAsync("/");
    
    response.AssertInertia(inertia => inertia
        .WithVersion("1.0.0")
    );
}
```

## Property Assertions

### Has / Missing

Check if properties exist:

```csharp
[Fact]
public async Task IndexHasUsersProp()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .Has("users")
        .Has("pagination")
        .Missing("secretData")
    );
}
```

### Nested Properties

Use dot notation for nested properties:

```csharp
[Fact]
public async Task ShowHasNestedUserData()
{
    var response = await _client.GetAsync("/users/1");
    
    response.AssertInertia(inertia => inertia
        .Has("user")
        .Has("user.name")
        .Has("user.profile")
        .Has("user.profile.bio")
    );
}
```

### Array Properties

Access array elements by index:

```csharp
[Fact]
public async Task IndexHasUsersArray()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .Has("users.0")
        .Has("users.0.name")
        .Has("users.1.email")
    );
}
```

## Value Assertions

### Exact Values

Assert prop values exactly:

```csharp
[Fact]
public async Task ShowHasCorrectUserName()
{
    var response = await _client.GetAsync("/users/1");
    
    response.AssertInertia(inertia => inertia
        .Where("user.name", "John Doe")
        .Where("user.id", 1)
        .Where("user.active", true)
    );
}
```

### Predicates

Use predicates for complex assertions:

```csharp
[Fact]
public async Task IndexHasValidUsers()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .Where("users", users => 
            users is IEnumerable<object> list && list.Count() > 0)
        .Where("users.0.email", email => 
            email.ToString().Contains("@"))
    );
}
```

### Type Assertions

Assert property types:

```csharp
[Fact]
public async Task ShowHasCorrectTypes()
{
    var response = await _client.GetAsync("/users/1");
    
    response.AssertInertia(inertia => inertia
        .WhereType("user.id", typeof(int))
        .WhereType("user.name", typeof(string))
        .WhereType("user.posts", typeof(IEnumerable<object>))
    );
}
```

### Count Assertions

Assert collection counts:

```csharp
[Fact]
public async Task IndexHasCorrectUserCount()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .WithCount("users", 10)
        .WithCount("users.0.posts", 5)
    );
}
```

## Partial Reload Testing

Test partial reloads with the `ReloadRequest` helper:

```csharp
using Inertia.Testing;

[Fact]
public async Task PartialReloadOnlyLoadsRequestedProps()
{
    // Simulate partial reload
    var reloadRequest = new ReloadRequest("/users", "Users/Index", "1.0.0")
        .ReloadOnly("users", "pagination");
    
    var request = new HttpRequestMessage(HttpMethod.Get, "/users");
    
    // Apply Inertia headers
    foreach (var (key, value) in reloadRequest.GetHeaders())
    {
        request.Headers.Add(key, value);
    }
    
    var response = await _client.SendAsync(request);
    
    response.AssertInertia(inertia => inertia
        .Has("users")
        .Has("pagination")
        .Missing("filters") // Not requested, so not included
    );
}
```

### ReloadOnly

Request specific props:

```csharp
[Fact]
public async Task ReloadOnlyStats()
{
    var reload = new ReloadRequest("/dashboard", "Dashboard", "1.0.0")
        .ReloadOnly("stats");
    
    var request = new HttpRequestMessage(HttpMethod.Get, "/dashboard");
    reload.ApplyToRequest(request);
    
    var response = await _client.SendAsync(request);
    
    response.AssertInertia(inertia => inertia
        .Has("stats")
        .Missing("user") // Shared prop filtered out
    );
}
```

### ReloadExcept

Request all props except specific ones:

```csharp
[Fact]
public async Task ReloadExceptLargeData()
{
    var reload = new ReloadRequest("/users", "Users/Index", "1.0.0")
        .ReloadExcept("largeDataset");
    
    var request = new HttpRequestMessage(HttpMethod.Get, "/users");
    reload.ApplyToRequest(request);
    
    var response = await _client.SendAsync(request);
    
    response.AssertInertia(inertia => inertia
        .Has("users")
        .Missing("largeDataset")
    );
}
```

## Deferred Props Testing

Test deferred props with `LoadDeferredProps`:

```csharp
[Fact]
public async Task DeferredPropsLoadAfterInitial()
{
    // Initial request
    var response = await _client.GetAsync("/dashboard");
    
    // Assert deferred props are not present initially
    response.AssertInertia(inertia => inertia
        .Has("user")
        .Missing("stats") // Deferred prop
    );
    
    // Load deferred props
    response.AssertInertia(inertia => inertia
        .LoadDeferredProps(async loadedInertia =>
        {
            // Now stats should be present
            loadedInertia.Has("stats");
        })
    );
}
```

### With Groups

Test specific deferred groups:

```csharp
[Fact]
public async Task LoadDeferredPropsByGroup()
{
    var response = await _client.GetAsync("/dashboard");
    
    response.AssertInertia(inertia => inertia
        .LoadDeferredProps(new[] { "primary" }, async loadedInertia =>
        {
            loadedInertia.Has("stats"); // In 'primary' group
            loadedInertia.Missing("logs"); // In 'secondary' group
        })
    );
}
```

## Debugging

### Dump

Output all props to console for debugging:

```csharp
[Fact]
public async Task DumpPropsForDebugging()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .Dump() // Outputs all props to test output
        .Has("users")
    );
}
```

### Dd (Dump and Die)

Dump props and stop test execution:

```csharp
[Fact]
public async Task DdPropsForDebugging()
{
    var response = await _client.GetAsync("/users");
    
    response.AssertInertia(inertia => inertia
        .Dd() // Dumps props and throws exception
    );
    
    // Test stops here
}
```

## Advanced Testing

### Custom Assertions

Create custom assertion helpers:

```csharp
public static class InertiaAssertionExtensions
{
    public static AssertableInertia HasValidUser(this AssertableInertia inertia)
    {
        return inertia
            .Has("user")
            .Has("user.id")
            .Has("user.name")
            .Has("user.email")
            .Where("user.email", email => email.ToString().Contains("@"));
    }
}

// Usage
[Fact]
public async Task ShowHasValidUser()
{
    var response = await _client.GetAsync("/users/1");
    
    response.AssertInertia(inertia => inertia
        .HasValidUser()
    );
}
```

### Testing Validation Errors

Test validation error handling:

```csharp
[Fact]
public async Task CreateReturnsValidationErrors()
{
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["name"] = "", // Invalid: required
        ["email"] = "not-an-email" // Invalid: format
    });
    
    var response = await _client.PostAsync("/users", content);
    
    response.AssertInertia(inertia => inertia
        .Has("errors")
        .Has("errors.name")
        .Has("errors.email")
        .Where("errors.name", errors => 
            errors.ToString().Contains("required"))
    );
}
```

### Testing with Authentication

Test authenticated requests:

```csharp
[Fact]
public async Task DashboardRequiresAuthentication()
{
    // Create client with authentication
    var client = _factory
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "Test", options => { });
            });
        })
        .CreateClient();
    
    var response = await client.GetAsync("/dashboard");
    
    response.AssertInertia(inertia => inertia
        .WithComponent("Dashboard")
        .Has("user")
    );
}
```

### Testing Shared Data

Test middleware shared data:

```csharp
[Fact]
public async Task AllPagesHaveSharedData()
{
    var response = await _client.GetAsync("/");
    
    response.AssertInertia(inertia => inertia
        .Has("appName")
        .Has("flash")
        .Where("appName", "My Application")
    );
}
```

### Testing Flash Messages

Test flash message functionality:

```csharp
[Fact]
public async Task CreateShowsSuccessMessage()
{
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["name"] = "John Doe",
        ["email"] = "john@example.com"
    });
    
    var response = await _client.PostAsync("/users", content);
    
    // Follow redirect
    var redirectUrl = response.Headers.Location?.ToString() ?? "/users";
    var followUp = await _client.GetAsync(redirectUrl);
    
    followUp.AssertInertia(inertia => inertia
        .Has("flash")
        .Has("flash.success")
        .Where("flash.success", "User created successfully!")
    );
}
```

### Testing SSR

Test server-side rendered pages:

```csharp
[Fact]
public async Task PageRendersSsrCorrectly()
{
    // Test without X-Inertia header (SSR should render)
    var response = await _client.GetAsync("/users");
    
    var html = await response.Content.ReadAsStringAsync();
    
    // Assert SSR rendered content
    Assert.Contains("<div id=\"app\"", html);
    Assert.Contains("data-page", html);
}
```

## Integration Tests

### Full User Flow

Test complete user interactions:

```csharp
[Fact]
public async Task UserCanCreateAndViewPost()
{
    // 1. Visit create form
    var createPage = await _client.GetAsync("/posts/create");
    createPage.AssertInertia(i => i.WithComponent("Posts/Create"));
    
    // 2. Submit form
    var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["title"] = "Test Post",
        ["content"] = "Test content"
    });
    
    var submitResponse = await _client.PostAsync("/posts", content);
    
    // 3. Assert redirect to show page
    var showUrl = submitResponse.Headers.Location?.ToString();
    Assert.NotNull(showUrl);
    
    // 4. Visit show page
    var showPage = await _client.GetAsync(showUrl);
    showPage.AssertInertia(i => i
        .WithComponent("Posts/Show")
        .Where("post.title", "Test Post")
        .Where("post.content", "Test content")
    );
}
```

## Best Practices

1. **Test the contract** - Focus on the data shape, not implementation
2. **Use partial reloads** - Test that partial reloads work correctly
3. **Test validation** - Ensure validation errors are handled properly
4. **Test authentication** - Verify protected pages require auth
5. **Test shared data** - Ensure middleware shares correct data
6. **Use meaningful names** - Test names should describe behavior
7. **Isolate tests** - Each test should be independent
8. **Mock external services** - Don't hit real APIs in tests

## Test Organization

Organize tests by feature:

```
MyApp.Tests/
  Controllers/
    UsersControllerTests.cs
    PostsControllerTests.cs
  Middleware/
    HandleInertiaRequestsTests.cs
  Integration/
    UserFlowTests.cs
    PostFlowTests.cs
  Helpers/
    TestAuthHandler.cs
    InertiaAssertionExtensions.cs
```

## Example Test Suite

Complete example:

```csharp
public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public UsersControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Index_ReturnsUsersList()
    {
        var response = await _client.GetAsync("/users");
        
        response.AssertInertia(inertia => inertia
            .WithComponent("Users/Index")
            .Has("users")
            .WithCount("users", 10)
            .Has("pagination")
        );
    }
    
    [Fact]
    public async Task Show_ReturnsUser()
    {
        var response = await _client.GetAsync("/users/1");
        
        response.AssertInertia(inertia => inertia
            .WithComponent("Users/Show")
            .Has("user")
            .Where("user.id", 1)
            .Has("user.name")
        );
    }
    
    [Fact]
    public async Task Create_WithValidData_CreatesUser()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "John Doe",
            ["email"] = "john@example.com"
        });
        
        var response = await _client.PostAsync("/users", content);
        
        Assert.True(response.Headers.Location != null);
    }
    
    [Fact]
    public async Task Create_WithInvalidData_ReturnsErrors()
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "", // Required
            ["email"] = "invalid" // Invalid format
        });
        
        var response = await _client.PostAsync("/users", content);
        
        response.AssertInertia(inertia => inertia
            .Has("errors")
            .Has("errors.name")
            .Has("errors.email")
        );
    }
    
    [Fact]
    public async Task PartialReload_OnlyLoadsRequestedProps()
    {
        var reload = new ReloadRequest("/users", "Users/Index", "1.0.0")
            .ReloadOnly("users");
        
        var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        reload.ApplyToRequest(request);
        
        var response = await _client.SendAsync(request);
        
        response.AssertInertia(inertia => inertia
            .Has("users")
            .Missing("pagination")
        );
    }
}
```

## Next Steps

- Explore [Middleware](middleware.md) to understand request handling
- Learn about [Property Types](properties.md) for advanced testing scenarios
- Read [Responses](responses.md) to understand response structure
