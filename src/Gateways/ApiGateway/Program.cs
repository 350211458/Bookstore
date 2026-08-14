var builder = WebApplication.CreateBuilder(args);

// Add shared OpenTelemetry, resilience, health checks and service discovery.
builder.AddServiceDefaults();

// Configure YARP reverse proxy from the "ReverseProxy" section of appsettings.json.
// Service discovery resolves cluster destination names (e.g. "http://identity-api")
// to the actual endpoints registered in the AppHost orchestrator.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapReverseProxy();

app.Run();
