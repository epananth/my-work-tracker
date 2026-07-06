var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("trackerdb");

var redis = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.MyWorkTracker_ApiService>("apiservice")
    .WithReference(postgres)
    .WithReference(redis);

var worker = builder.AddProject<Projects.MyWorkTracker_Worker>("worker")
    .WithReference(postgres)
    .WithReference(redis);

builder.AddProject<Projects.MyWorkTracker_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
