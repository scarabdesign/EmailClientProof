var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");

var pgPrefs = builder.Configuration.GetSection("PgPrefs");
if (pgPrefs != null)
{
    if (bool.Parse(pgPrefs["PgAdmin"] ?? "false"))
    {
        postgres.WithPgAdmin();
    }
    if (bool.Parse(pgPrefs["PgWeb"] ?? "false"))
    {
        postgres.WithPgWeb();
    }
    if (bool.Parse(pgPrefs["DataVolume"] ?? "false"))
    {
        postgres.WithDataVolume(isReadOnly: false);
    }
}

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
