var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var churchDb = postgres.AddDatabase("churchwebsite");

var api = builder.AddProject<Projects.ChurchWebsite_Api>("churchwebsite-api")
    .WithReference(churchDb)
    .WaitFor(churchDb);

var frontend = builder.AddViteApp("frontend", "../../app")
    .WithReference(api)
    .WithEnvironment("VITE_API_PROXY_TARGET", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
