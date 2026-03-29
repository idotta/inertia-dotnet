using System.Net;
using FluentAssertions;
using Inertia.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inertia.Tests.Extensions;

public class InertiaEndpointExtensionsTests
{
    public class MapInertiaTests
    {
        private async Task<(IHost Host, HttpClient Client)> SetupTestServer(
            Action<IEndpointRouteBuilder> configureEndpoints)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMvc();
                        services.AddInertia(o => o.SsrEnabled = false);
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseInertia();
                        app.UseEndpoints(configureEndpoints);
                    });
                })
                .Build();

            await host.StartAsync();
            var client = host.GetTestServer().CreateClient();
            return (host, client);
        }

        [Fact]
        public async Task MapInertia_GetRequest_Returns200()
        {
            var (host, client) = await SetupTestServer(endpoints =>
                endpoints.MapInertia("/about", "About"));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/about");
                request.Headers.Add("X-Inertia", "true");
                request.Headers.Add("X-Inertia-Version", "");

                var response = await client.SendAsync(request);

                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            finally
            {
                client.Dispose();
                await host.StopAsync();
                host.Dispose();
            }
        }

        [Fact]
        public async Task MapInertia_InertiaRequest_ReturnsJsonWithHeader()
        {
            var (host, client) = await SetupTestServer(endpoints =>
                endpoints.MapInertia("/about", "About"));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/about");
                request.Headers.Add("X-Inertia", "true");
                request.Headers.Add("X-Inertia-Version", "");

                var response = await client.SendAsync(request);

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
                response.Headers.Should().Contain(h => h.Key == "X-Inertia");

                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("\"component\":\"About\"");
            }
            finally
            {
                client.Dispose();
                await host.StopAsync();
                host.Dispose();
            }
        }

        [Fact]
        public async Task MapInertia_WithProps_IncludesPropsInResponse()
        {
            var (host, client) = await SetupTestServer(endpoints =>
                endpoints.MapInertia("/about", "About", new { Title = "About Us" }));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "/about");
                request.Headers.Add("X-Inertia", "true");
                request.Headers.Add("X-Inertia-Version", "");

                var response = await client.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                content.Should().Contain("\"component\":\"About\"");
                content.Should().Contain("About Us");
            }
            finally
            {
                client.Dispose();
                await host.StopAsync();
                host.Dispose();
            }
        }

        [Fact]
        public async Task MapInertia_HeadRequest_Returns200()
        {
            var (host, client) = await SetupTestServer(endpoints =>
                endpoints.MapInertia("/about", "About"));

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, "/about");
                request.Headers.Add("X-Inertia", "true");
                request.Headers.Add("X-Inertia-Version", "");

                var response = await client.SendAsync(request);

                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
            finally
            {
                client.Dispose();
                await host.StopAsync();
                host.Dispose();
            }
        }
    }
}
