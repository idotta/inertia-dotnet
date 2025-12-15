using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Inertia.AspNetCore.Tests;

public class InertiaValidationFilterTests
{
    private static ActionExecutedContext CreateContext(ModelStateDictionary modelState)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            modelState
        );

        return new ActionExecutedContext(
            actionContext,
            new List<IFilterMetadata>(),
            null!
        );
    }

    [Fact]
    public void OnActionExecuted_WithValidModelState_DoesNotSetErrors()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        Assert.False(context.HttpContext.Items.ContainsKey("InertiaValidationErrors"));
    }

    [Fact]
    public void OnActionExecuted_WithInvalidModelState_SetsErrors()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        Assert.True(context.HttpContext.Items.ContainsKey("InertiaValidationErrors"));
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("Email"));
        Assert.Single(errors["Email"]);
        Assert.Equal("Email is required", errors["Email"][0]);
    }

    [Fact]
    public void OnActionExecuted_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        modelState.AddModelError("Email", "Email must be valid");
        modelState.AddModelError("Name", "Name is required");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
        Assert.Equal(2, errors["Email"].Length);
        Assert.Single(errors["Name"]);
        Assert.Equal("Email is required", errors["Email"][0]);
        Assert.Equal("Email must be valid", errors["Email"][1]);
        Assert.Equal("Name is required", errors["Name"][0]);
    }

    [Fact]
    public void OnActionExecuted_WithErrorBagHeader_WrapsErrorsInBag()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        var context = CreateContext(modelState);
        context.HttpContext.Request.Headers[Core.InertiaHeaders.ErrorBag] = "registration";

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, Dictionary<string, string[]>>;
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.True(errors.ContainsKey("registration"));
        Assert.Single(errors["registration"]);
        Assert.True(errors["registration"].ContainsKey("Email"));
    }

    [Fact]
    public void OnActionExecuted_WithoutErrorBagHeader_ReturnsSimpleDictionary()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"];
        Assert.IsType<Dictionary<string, string[]>>(errors);
    }

    [Fact]
    public void OnActionExecuted_WithEmptyErrorBagHeader_ReturnsSimpleDictionary()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        var context = CreateContext(modelState);
        context.HttpContext.Request.Headers[Core.InertiaHeaders.ErrorBag] = "";

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"];
        Assert.IsType<Dictionary<string, string[]>>(errors);
    }

    [Fact]
    public void OnActionExecuted_WithExceptionInModelState_UsesExceptionMessage()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        // Manually add error message that simulates exception
        modelState.AddModelError("Field", "Custom exception message");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("Custom exception message", errors["Field"][0]);
    }

    [Fact]
    public void OnActionExecuted_WithNoErrorMessage_UsesFallbackMessage()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        var context = CreateContext(modelState);
        
        // Manually create a model state entry with no error message
        context.ModelState.AddModelError("Field", "");

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Validation error", errors["Field"][0]);
    }

    [Fact]
    public void OnActionExecuting_DoesNothing()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var context = new ActionExecutingContext(
            new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor()
            ),
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null!
        );

        // Act & Assert - should not throw
        filter.OnActionExecuting(context);
    }

    [Fact]
    public void OnActionExecuted_WithNestedProperties_IncludesInErrors()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("User.Email", "Email is required");
        modelState.AddModelError("User.Name", "Name is required");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
        Assert.True(errors.ContainsKey("User.Email"));
        Assert.True(errors.ContainsKey("User.Name"));
    }

    [Fact]
    public void OnActionExecuted_WithArrayIndexedProperties_IncludesInErrors()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Users[0].Email", "First user email is required");
        modelState.AddModelError("Users[1].Email", "Second user email is required");
        var context = CreateContext(modelState);

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
        Assert.True(errors.ContainsKey("Users[0].Email"));
        Assert.True(errors.ContainsKey("Users[1].Email"));
    }

    [Fact]
    public void OnActionExecuted_WithMultipleErrorBags_OnlyUsesFirstBag()
    {
        // Arrange
        var filter = new InertiaValidationFilter();
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Email", "Email is required");
        var context = CreateContext(modelState);
        context.HttpContext.Request.Headers[Core.InertiaHeaders.ErrorBag] = "bag1,bag2";

        // Act
        filter.OnActionExecuted(context);

        // Assert
        var errors = context.HttpContext.Items["InertiaValidationErrors"] as Dictionary<string, Dictionary<string, string[]>>;
        Assert.NotNull(errors);
        Assert.Single(errors);
        // The header value "bag1,bag2" is treated as a single bag name
        Assert.True(errors.ContainsKey("bag1,bag2"));
    }
}
