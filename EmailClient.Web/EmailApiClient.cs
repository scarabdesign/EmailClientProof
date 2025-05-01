using EmailClient.ApiService;

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
            }
            ;
        }

        return attempts ?? [];
    }
}