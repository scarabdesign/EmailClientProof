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
            app.MapGet(root + Strings.RouteNames.GetAllCampaigns, async () => await service.GetAllCampaigns()).WithName(Strings.RouteNames.GetAllCampaigns);
            app.MapGet(root + Strings.RouteNames.GetCampaign, async (int id) => await service.GetCampaign(id)).WithName(Strings.RouteNames.GetCampaign);
            app.MapGet(root + Strings.RouteNames.ToggleCampaignPause, async (int id) =>
                await service.ToggleCampaignPause(id) is int _id ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok, id }) :
                    Results.Problem(Strings.RouteResponses.UpdatingCampaignFailed))
                .WithName(Strings.RouteNames.ToggleCampaignPause);
            app.MapPost(root + Strings.RouteNames.AddAttempt, async (EmailAttemptDto emailAttempt) =>
                await service.AddEmailAttempt(emailAttempt) is Tuple<string?, int?> result ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok, email = result.Item1, id = result.Item2 }) :
                    Results.Problem(Strings.RouteResponses.AddingAttemptFailed))
                .WithName(Strings.RouteNames.AddAttempt);
            app.MapPost(root + Strings.RouteNames.AddAttempts, async (List<EmailAttemptDto> inEmails) =>
                await service.AddEmailAttempts(inEmails) is List<EmailAttemptDto> emails ? 
                    Results.Ok(new { result = Strings.RouteResponses.Ok, emails }) : 
                    Results.Problem(Strings.RouteResponses.AddingAttemptsFailed))
                .WithName(Strings.RouteNames.AddAttempts);
            app.MapPost(root + Strings.RouteNames.AddCampaign, async (CampaignDto campaign) =>
                await service.AddCampaign(campaign) is int id ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok, id }) :
                    Results.Problem(Strings.RouteResponses.AddingCampaignFailed))
                .WithName(Strings.RouteNames.AddCampaign);
            app.MapPost(root + Strings.RouteNames.UpdateCampaign, async (CampaignDto campaign) => 
                await service.UpdateCampaign(campaign) is int id ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok, id }) :
                    Results.Problem(Strings.RouteResponses.UpdatingCampaignFailed))
                .WithName(Strings.RouteNames.UpdateCampaign);
            app.MapDelete(root + Strings.RouteNames.RemoveAttempt, async (int id) =>
                await service.RemoveEmailAttempt(id) is string email ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok, email }) :
                    Results.Problem(Strings.RouteResponses.RemovingAttemptFailed))
                .WithName(Strings.RouteNames.RemoveAttempt);
            app.MapDelete(root + Strings.RouteNames.RemoveCampaign, async(int id) =>
                await service.RemoveCampaign(id) is int _id ?
                    Results.Ok(new { result = Strings.RouteResponses.Ok }) :
                    Results.Problem(Strings.RouteResponses.RemovingCampaignFailed))
                .WithName(Strings.RouteNames.RemoveCampaign);
    }
    }
}
