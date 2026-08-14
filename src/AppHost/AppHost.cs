var builder = DistributedApplication.CreateBuilder(args);

// Core microservices
var identityApi = builder.AddProject<Projects.Identity_Api>("identity-api");
var catalogApi = builder.AddProject<Projects.Catalog_Api>("catalog-api");
var orderApi = builder.AddProject<Projects.Order_Api>("order-api");

// API Gateway (YARP reverse proxy) - public entry point routing to all services
var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithReference(identityApi)
    .WithReference(catalogApi)
    .WithReference(orderApi);

builder.Build().Run();
