using EmailClient.ApiService;
using EmailClient.Mailing;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<EmailClientDbContext>("emaildb", c => c.DisableTracing = true);
builder.AddMailKitClient("maildev");
builder.Logging.AddConsole();

builder.Services.AddCors(c =>
{
    c.AddPolicy("CorsPolicy", opts =>
    {
        opts.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddProblemDetails();

builder.Services.AddSignalR();
builder.Services.AddScoped<EmailClientData>();
builder.Services.AddScoped<IMailer, Mailer>();
builder.Services.AddScoped<Service>();
builder.Services.AddScoped<Queue>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddScoped<ContextQueue>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("CorsPolicy");

Routes.MapEndPoints(app);

app.Use(async (context, next) =>
{
    await EnsureCreated();
    await next();
});

app.MapHub<MessageHub>("/clientHub");

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