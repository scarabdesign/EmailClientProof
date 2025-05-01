var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithPgWeb()
    //.WithDataVolume(isReadOnly: false)
    ;

var postgresdb = postgres.AddDatabase("emaildb");

var apiService = builder.AddProject<Projects.EmailClient_ApiService>("apiservice")
    .WithReference(postgresdb);

builder.AddProject<Projects.EmailClient_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
