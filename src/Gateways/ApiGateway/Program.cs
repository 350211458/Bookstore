var builder = WebApplication.CreateBuilder(args);

// Add shared OpenTelemetry, resilience, health checks and service discovery.
builder.AddServiceDefaults();

// CORS for the web frontend (spec 07 Req 1). The SPA origin (http://localhost:3000)
// differs from the gateway origin (http://localhost:8080), and YARP does not enable
// CORS by default, so without this policy every cross-origin browser call would fail.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Configure YARP reverse proxy from the "ReverseProxy" section of appsettings.json.
// Service discovery resolves cluster destination names (e.g. "http://identity-api")
// to the actual endpoints registered in the AppHost orchestrator.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

// CORS middleware must run before the proxy so preflight OPTIONS is answered (204)
// here instead of being forwarded to an upstream.
app.UseCors();

app.MapReverseProxy();

app.Run();

public partial class Program;
