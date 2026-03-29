using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Inertia.Tests.Extensions;

public class InertiaApplicationBuilderExtensionsTests
{
    public class UseInertiaTests
    {
        [Fact]
        public void UseInertia_ReturnsApplicationBuilder()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddInertia();
            var app = builder.Build();

            var result = app.UseInertia();

            result.Should().BeSameAs(app);
        }

        [Fact]
        public void UseInertiaEncryptHistory_ReturnsApplicationBuilder()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddInertia();
            var app = builder.Build();

            var result = app.UseInertiaEncryptHistory();

            result.Should().BeSameAs(app);
        }
    }
}
