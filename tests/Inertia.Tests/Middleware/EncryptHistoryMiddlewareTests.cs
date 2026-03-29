using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Inertia.Tests;

public class EncryptHistoryMiddlewareTests
{
    private static (EncryptHistoryMiddleware Middleware, InertiaFactory Factory, DefaultHttpContext HttpContext) Create()
    {
        var options = new InertiaOptions();
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var tempData = Substitute.For<ITempDataDictionary>();
        var tempDataFactory = Substitute.For<ITempDataDictionaryFactory>();
        tempDataFactory.GetTempData(httpContext).Returns(tempData);

        var factory = new InertiaFactory(Options.Create(options), accessor, tempDataFactory);

        var services = new ServiceCollection();
        services.AddSingleton<IInertia>(factory);
        httpContext.RequestServices = services.BuildServiceProvider();

        var middleware = new EncryptHistoryMiddleware();
        return (middleware, factory, httpContext);
    }

    [Fact]
    public async Task InvokeAsync_SetsEncryptHistory()
    {
        var (middleware, factory, ctx) = Create();

        await middleware.InvokeAsync(ctx, _ => Task.CompletedTask);

        factory.GetEncryptHistory().Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var (middleware, _, ctx) = Create();
        var nextCalled = false;

        await middleware.InvokeAsync(ctx, _ => { nextCalled = true; return Task.CompletedTask; });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_SetsEncryptHistoryBeforeNext()
    {
        var (middleware, factory, ctx) = Create();
        var wasEncryptedDuringNext = false;

        await middleware.InvokeAsync(ctx, _ =>
        {
            wasEncryptedDuringNext = factory.GetEncryptHistory();
            return Task.CompletedTask;
        });

        wasEncryptedDuringNext.Should().BeTrue();
    }
}
