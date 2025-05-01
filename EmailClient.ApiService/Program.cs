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

MapEndPoints();

app.Use(async (context, next) =>
{
    await EnsureCreated();
    await next();
});

app.MapDefaultEndpoints().Run();

void MapEndPoints()
{
    var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<Service>();
    app.MapGet("/getAllAttempts", async () =>
    {
        return await service.GetAllEmailAttempts();
    })
    .WithName("getAllAttempts");

    app.MapPost("/addAttempt", async (EmailAttempt emailAttempt) =>
    {
        return await service.AddEmailAttempt(emailAttempt) ? Results.Ok() : Results.Problem("Failed to add email attempt.");
    })
    .WithName("addAttempt");
}


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