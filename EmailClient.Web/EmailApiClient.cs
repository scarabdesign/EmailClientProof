using EmailClient.ApiService;
using Microsoft.AspNetCore.SignalR.Client;

namespace EmailClient.Web;

public class EmailApiClient(HttpClient httpClient)
{
    public async Task<List<EmailAttempt>> GetAllAttempts(CancellationToken cancellationToken = default)
    {
        List<EmailAttempt>? attempts = null;

        await foreach (var attempt in httpClient.GetFromJsonAsAsyncEnumerable<EmailAttempt>("/getAllAttempts", cancellationToken))
        {
            if (attempt is not null)
            {
                attempts ??= [];
                attempts.Add(attempt);
            };
        }

        return attempts ?? [];
    }
}

public static class HubConnectionExtensions
{
    public static IHubConnectionBuilder WithUrl(this IHubConnectionBuilder builder, string url, IHttpMessageHandlerFactory clientFactory)
    {
        return builder.WithUrl(url, options =>
        {
            options.HttpMessageHandlerFactory = _ => clientFactory.CreateHandler();
        });
    }
}