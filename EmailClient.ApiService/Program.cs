using EmailClient.ApiService;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

//builder.AddNpgsqlDataSource(connectionName: "emaildb");

builder.AddNpgsqlDbContext<EmailClientDbContext>("emaildb", c => c.DisableTracing = true);

builder.Logging.AddConsole();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<Service>();


var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/getAllAttempts", async () =>
{
    await EnsureCreated();
    return await app.Services.CreateScope().ServiceProvider.GetRequiredService<Service>().GetAllEmailAttempts();
})
.WithName("getAllAttempts");

app.MapDefaultEndpoints().Run();


async Task EnsureCreated()
{
    try
    {
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<EmailClientDbContext>();
                await context.Database.EnsureCreatedAsync();
            }
        }
    }
    catch (NpgsqlException e)
    {
        app.Logger.LogError(e.Message);
    }
}