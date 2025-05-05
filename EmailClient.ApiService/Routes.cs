using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public static class Routes
    {
        public static void MapEndPoints(WebApplication app)
        {
            var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<Service>();
            
            app.MapGet("/startQueue", service.StartQueue).WithName("startQueue");
            app.MapGet("/stopQueue", service.StopQueue).WithName("stopQueue");

            app.MapGet("/getAllAttempts", async (int id) =>
            {
                return await service.GetAllEmailAttempts(id);
            })
            .WithName("getAllAttempts");

            app.MapPost("/addAttempt", async (EmailAttemptDto emailAttempt) =>
            {
                var (email, campaignId) = await service.AddEmailAttempt(emailAttempt);
                return email != null ? Results.Ok(new { result = "ok", email, campaignId }) : Results.Problem("Adding attempt failed");
            })
            .WithName("addAttempt");

            app.MapPost("/addAttempts", async (List<EmailAttemptDto> emails) =>
            {
                var processed = await service.AddEmailAttempts(emails);
                return processed != null ? Results.Ok(new { result = "ok", emails = processed}) : Results.Problem("Adding attempts failed");
            })
            .WithName("addAttempts");

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

            app.MapGet("/getCampaign", async (int id) =>
            {
                return await service.GetCampaign(id);
            })
            .WithName("getCampaign");

            app.MapPost("/addCampaign", async (CampaignDto campaign) =>
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

            app.MapPost("/updateCampaign", async (CampaignDto campaign) =>
            {
                var returnId = await service.UpdateCampaign(campaign);
                return returnId != null ? Results.Ok(new { result = "ok", returnId }) : Results.Problem("Updating campaign failed");
            })
            .WithName("updateCampaign");
        }
    }
}
