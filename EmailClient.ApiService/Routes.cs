using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public static class Routes
    {
        public static void MapEndPoints(WebApplication app)
        {
            var root = "/";
            var service = app.Services.CreateScope().ServiceProvider.GetRequiredService<Service>();
            app.MapGet(root + Strings.RouteNames.StartQueue, service.StartQueue).WithName(Strings.RouteNames.StartQueue);
            app.MapGet(root + Strings.RouteNames.StopQueue, service.StopQueue).WithName(Strings.RouteNames.StopQueue);
            app.MapGet(root + Strings.RouteNames.GetAllAttempts, async (int id) => await service.GetAllEmailAttempts(id)).WithName(Strings.RouteNames.GetAllAttempts);
            app.MapPost(root + Strings.RouteNames.AddAttempt, async (EmailAttemptDto emailAttempt) =>
            {
                var (email, campaignId) = await service.AddEmailAttempt(emailAttempt);
                return email != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, email, campaignId }) : Results.Problem(Strings.RouteResponses.AddingAttemptFailed);
            }).WithName(Strings.RouteNames.AddAttempt);
            app.MapPost(root + Strings.RouteNames.AddAttempts, async (List<EmailAttemptDto> emails) =>
            {
                var processed = await service.AddEmailAttempts(emails);
                return processed != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, emails = processed}) : Results.Problem(Strings.RouteResponses.AddingAttemptsFailed);
            }).WithName(Strings.RouteNames.AddAttempts);
            app.MapDelete(root + Strings.RouteNames.RemoveAttempt, async (int id) =>
            {
                var email = await service.RemoveEmailAttempt(id);
                return email != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, email }) : Results.Problem(Strings.RouteResponses.RemovingAttemptFailed);
            }).WithName(Strings.RouteNames.RemoveAttempt);
            app.MapGet(root + Strings.RouteNames.GetAllCampaigns, async () => await service.GetAllCampaigns()).WithName(Strings.RouteNames.GetAllCampaigns);
            app.MapGet(root + Strings.RouteNames.GetCampaign, async (int id) => await service.GetCampaign(id)).WithName(Strings.RouteNames.GetCampaign);
            app.MapPost(root + Strings.RouteNames.AddCampaign, async (CampaignDto campaign) =>
            {
                var id = await service.AddCampaign(campaign);
                return id != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, id }) : Results.Problem(Strings.RouteResponses.AddingCampaignFailed);
            }).WithName(Strings.RouteNames.AddCampaign);
            app.MapDelete(root + Strings.RouteNames.RemoveCampaign, async (int id) =>
            {
                await service.RemoveCampaign(id);
                return Results.Ok(new { result = Strings.RouteResponses.Ok });
            }).WithName(Strings.RouteNames.RemoveCampaign);
            app.MapPost(root + Strings.RouteNames.UpdateCampaign, async (CampaignDto campaign) =>
            {
                var returnId = await service.UpdateCampaign(campaign);
                return returnId != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, returnId }) : Results.Problem(Strings.RouteResponses.UpdatingCampaignFailed);
            }).WithName(Strings.RouteNames.UpdateCampaign);
            app.MapGet(root + Strings.RouteNames.ToggleCampaignPause, async (int id) =>
            {
                var returnId = await service.ToggleCampaignPause(id);
                return returnId != null ? Results.Ok(new { result = Strings.RouteResponses.Ok, returnId }) : Results.Problem(Strings.RouteResponses.UpdatingCampaignFailed);
            }).WithName(Strings.RouteNames.ToggleCampaignPause);
        }
    }
}
