using EmailClient.ApiService;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<EmailClientDbContext>("emaildb", c => c.DisableTracing = true);

builder.Logging.AddConsole();


builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", opts => opts.AllowAnyOrigin());
});
builder.Services.AddProblemDetails();

builder.Services.AddScoped<EmailClientData>();
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
        var (email, campaignId) = await service.AddEmailAttempt(emailAttempt);
        return email != null ? Results.Ok(new { result = "ok", email, campaignId }) : Results.Problem("Adding attempt failed");
    })
    .WithName("addAttempt");

    app.MapDelete("/removeAttempt", async (int id) =>
    {
        var email = await service.RemoveEmailAttempt(id);
        return email != null ? Results.Ok(new { result = "ok", email }) : Results.Problem("Removing attempt failed");
    })
    .WithName("removeAttempt");

    app.MapGet("/getAllCampaigns", async () =>
    {
        return await service.GetAllCampaigns();
    })
    .WithName("getAllCampaigns");

    app.MapPost("/addCampaign", async (Campaign campaign) =>
    {
        var id = await service.AddCampaign(campaign);
        return id != null ? Results.Ok(new { result = "ok", id }) : Results.Problem("Adding campaign failed");
    })
    .WithName("addCampaign");

    app.MapDelete("/removeCampaign", async (int id) =>
    {
        await service.RemoveCampaign(id);
        return Results.Ok(new { result = "ok" });
    })
    .WithName("removeCampaign");

    app.MapPost("/updateCampaign", async (int id, CampaignStatus? status, string? name, string? subject, string? body, string? sender) =>
    {
        var returnId = await service.UpdateCampaign(id, status, name, subject, body, sender);
        return returnId != null ? Results.Ok(new { result = "ok", returnId }) : Results.Problem("Updating campaign failed");
    })
    .WithName("updateCampaign");
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