using EmailClient.ApiService;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<EmailClientDbContext>("emaildb", c => c.DisableTracing = true);
builder.AddNpgsqlDbContext<QueueContext>("emaildb", c => c.DisableTracing = true);

builder.Logging.AddConsole();


builder.Services.AddCors(c =>
{
    c.AddPolicy("AllowOrigin", opts => opts.AllowAnyOrigin());
});
builder.Services.AddProblemDetails();

builder.Services.AddScoped<EmailClientData>();
builder.Services.AddScoped<Service>();
builder.Services.AddScoped<Queue>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(opts => opts.AllowAnyOrigin());

Routes.MapEndPoints(app);

app.Use(async (context, next) =>
{
    await EnsureCreated();
    await next();
});

app.MapDefaultEndpoints().Run();

async Task EnsureCreated()
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<EmailClientDbContext>();
            await context.Database.EnsureCreatedAsync();
        }
    }
    catch (NpgsqlException e)
    {
        app.Logger.LogError(e.Message);
    }
}