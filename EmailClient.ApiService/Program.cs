using EmailClient.ApiService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.Cors;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

//builder.AddNpgsqlDataSource(connectionName: "emaildb");

builder.AddNpgsqlDbContext<EmailClientDbContext>("emaildb", c => c.DisableTracing = true);

builder.Logging.AddConsole();


builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", opts => opts.AllowAnyOrigin());
});
builder.Services.AddProblemDetails();
builder.Services.AddScoped<Service>();


var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(opts => opts.AllowAnyOrigin());

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
        return await service.AddEmailAttempt(emailAttempt) ? Results.Ok(new { result = "ok", email = emailAttempt.Email}) : Results.Problem("Adding attempt failed");
    })
    .WithName("addAttempt");

    app.MapDelete("/removeAttempt", async (int id) =>
    {
        var email = await service.RemoveEmailAttempt(id);
        return email != null ? Results.Ok(new { result = "ok", email }) : Results.Problem("Removing attempt failed");
    })
    .WithName("removeAttempt");
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