using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EmailClient.ApiService;
public class MessageHub : Hub { }

public class MessageService(IHubContext<MessageHub> hubContext)
{

    private JsonSerializerOptions jOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task AttemptsUpdated(List<EmailAttempt> emailAttempts)
    {
        var payload = JsonSerializer.Serialize(emailAttempts, jOpts);
        await hubContext.Clients.All.SendAsync("ReceiveUpdate", payload);
    }
}
