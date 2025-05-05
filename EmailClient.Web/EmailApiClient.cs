using System.Text.Json;
using static EmailClient.ApiService.Dto;

namespace EmailClient.Web;

public class EmailApiClient(HttpClient httpClient)
{
    private JsonSerializerOptions jOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
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

    public async Task<CampaignDto?> AddEmailAttempt(EmailAttemptDto emailAttempt, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/addAttempt", emailAttempt, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CampaignDto>(cancellationToken: cancellationToken);
        }
        return null;
    }

    public async Task<List<EmailAttemptDto>> AddEmailAttempts(List<EmailAttemptDto> emailAttempts, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/addAttempts", emailAttempts, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadFromJsonAsync<AddAttemptsResponse>(jOpts, cancellationToken: cancellationToken);
            if (results != null)
            {
                return results.Emails;
            }
        }
        return [];
    }

    public async Task<string?> RemoveEmailAttempt(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/removeAttempt?id={id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        return null;
    }

    public async Task<string?> UpdateCampaign(CampaignDto campaign, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/updateCampaign", campaign, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        return null;
    }

    public async Task<string?> AddCampaign(CampaignDto campaign, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/addCampaign", campaign, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        return null;
    }

    public async Task<string?> RemoveCampaign(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/removeCampaign?id={id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        return null;
    }

    private class AddAttemptsResponse
    {
        public string? Result { get; set; }
        public List<EmailAttemptDto> Emails { get; set; } = [];
    }
}


