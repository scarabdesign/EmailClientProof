using static EmailClient.ApiService.Dto;

namespace EmailClient.Web;

public class EmailApiClient(HttpClient httpClient)
{

    public async Task<List<EmailAttemptDto>> GetAllAttempts(int campaignId, CancellationToken cancellationToken = default)
    {
        List<EmailAttemptDto>? attempts = null;

        await foreach (var attempt in httpClient.GetFromJsonAsAsyncEnumerable<EmailAttemptDto>($"/getAllAttempts?id={campaignId}", cancellationToken))
        {
            if (attempt is not null)
            {
                attempts ??= [];
                attempts.Add(attempt);
            };
        }

        return attempts ?? [];
    }

    public async Task<List<CampaignDto>> GetAllCampaigns(CancellationToken cancellationToken = default)
    {
        List<CampaignDto>? campaigns = null;

        await foreach (var attempt in httpClient.GetFromJsonAsAsyncEnumerable<CampaignDto>($"/getAllCampaigns", cancellationToken))
        {
            if (attempt is not null)
            {
                campaigns ??= [];
                campaigns.Add(attempt);
            };
        }

        return campaigns ?? [];
    }

    public async Task<CampaignDto?> GetCampaign(int campaignId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<CampaignDto>($"/getCampaign?id={campaignId}", cancellationToken);
    }
}


