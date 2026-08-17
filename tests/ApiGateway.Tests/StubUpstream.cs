using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Tests;

/// <summary>
/// Minimal in-test HTTP upstream (spec 05). The gateway's cluster destinations are pointed
/// at its <see cref="BaseUrl"/> so routing, prefix stripping and passthrough can be asserted
/// without booting the real Identity / Catalog / Order services.
/// </summary>
public sealed class StubUpstream : IDisposable
{
    private readonly object _gate = new();
    private readonly WebApplication _app;

    private StubUpstream(WebApplication app, string baseUrl, string name,
        List<RecordedRequest> requests, object gate)
    {
        _app = app;
        BaseUrl = baseUrl;
        Name = name;
        Requests = requests;
        _gate = gate;
    }

    /// <summary>The cluster this stub stands in for (identity / catalog / order).</summary>
    public string Name { get; }

    /// <summary>Base URL the gateway forwards to, e.g. http://127.0.0.1:54321.</summary>
    public string BaseUrl { get; }

    /// <summary>Every request received, in arrival order.</summary>
    public List<RecordedRequest> Requests { get; }

    /// <summary>Starts a stub that echoes back its cluster name and the received path/query.</summary>
    public static StubUpstream Start(string name)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        var app = builder.Build();
        app.Urls.Add("http://127.0.0.1:0");

        var gate = new object();
        var requests = new List<RecordedRequest>();

        app.MapFallback(async context =>
        {
            var record = new RecordedRequest(
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Request.QueryString.Value ?? "");
            lock (gate)
            {
                requests.Add(record);
            }

            await context.Response.WriteAsJsonAsync(new
            {
                cluster = name,
                method = context.Request.Method,
                path = context.Request.Path.Value,
                query = context.Request.QueryString.Value,
            });
        });

        app.StartAsync().GetAwaiter().GetResult();

        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses;
        var baseUrl = addresses.First(a => a.StartsWith("http://"));

        return new StubUpstream(app, baseUrl, name, requests, gate);
    }

    /// <summary>Forgets all recorded requests (keeps tests independent within a fixture).</summary>
    public void Clear()
    {
        lock (_gate)
        {
            Requests.Clear();
        }
    }

    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

/// <summary>A request as observed by the stub upstream.</summary>
public sealed record RecordedRequest(string Method, string Path, string Query);
