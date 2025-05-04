var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    //.WithPgAdmin()
    //.WithPgWeb()
    //.WithDataVolume(isReadOnly: false)
    ;

var postgresdb = postgres.AddDatabase("emaildb");

var maildev = builder.AddMailDev("maildev");

var apiService = builder.AddProject<Projects.EmailClient_ApiService>("apiservice")
    .WithReference(postgresdb)
    .WithReference(maildev);

builder.AddProject<Projects.EmailClient_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
