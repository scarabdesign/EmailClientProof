namespace EmailClient.ApiService
{
    public static class Routes
    {
        public static void MapEndPoints(WebApplication app)
        {
            var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<Service>();
            
            app.MapGet("/startQueue", service.StartQueue).WithName("startQueue");
            app.MapGet("/stopQueue", service.StopQueue).WithName("stopQueue");

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

            app.MapPost("/updateCampaign", async (int id, string? name, string? subject, string? body, string? sender) =>
            {
                var returnId = await service.UpdateCampaign(id, name, subject, body, sender);
                return returnId != null ? Results.Ok(new { result = "ok", returnId }) : Results.Problem("Updating campaign failed");
            })
            .WithName("updateCampaign");
        }
    }
}
