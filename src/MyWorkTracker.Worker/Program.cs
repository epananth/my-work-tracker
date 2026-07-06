using MyWorkTracker.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<GitHubSyncWorker>();

var host = builder.Build();
host.Run();
