using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService;
public class MessageHub : Hub { }

public class MessageService(IHubContext<MessageHub> hubContext)
{

    private JsonSerializerOptions jOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task AttemptsUpdated(CampaignDto? campaign)
    {
        var payloadObj = new StatusDto
        {
            Updated = DateTime.Now
        };
        if (campaign != null)
        {
            payloadObj.CurrentlyViewing = campaign;
        }
        var payload = JsonSerializer.Serialize(payloadObj, jOpts);
        await hubContext.Clients.All.SendAsync("AttemptsUpdated", payload);
    }

    public async Task CampaignsUpdated(List<CampaignDto> campaigns)
    {
        var payload = JsonSerializer.Serialize(new StatusDto
        {
            Campaigns = campaigns,
            Updated = DateTime.Now
        }, jOpts);
        await hubContext.Clients.All.SendAsync("CampaignsUpdated", payload);
    }
}
