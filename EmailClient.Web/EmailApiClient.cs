using System.Text.Json;
using static EmailClient.ApiService.Dto;

namespace EmailClient.Web;

public class EmailApiClient(HttpClient httpClient)
{
    private JsonSerializerOptions jOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<List<CampaignDto>> GetAllCampaigns(CancellationToken cancellationToken = default)
    {
        List<CampaignDto>? campaigns = null;

        await foreach (var campaign in httpClient.GetFromJsonAsAsyncEnumerable<CampaignDto>($"/getAllCampaigns", cancellationToken))
        {
            if (campaign is not null)
            {
                campaigns ??= [];
                campaigns.Add(campaign);
            };
        }

        return campaigns ?? [];
    }

    public async Task<CampaignDto?> GetCampaign(int campaignId, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<CampaignDto>($"/getCampaign?id={campaignId}", cancellationToken);
    }

    public async Task<bool> AddEmailAttempts(List<EmailAttemptDto> emailAttempts, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/addAttempts", emailAttempts, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var results = await response.Content.ReadFromJsonAsync<AddAttemptsResponse>(jOpts, cancellationToken: cancellationToken);
            if (results != null)
            {
                return true;
            }
        }
        return false;
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

    public async Task<bool> ToggleCampaignPause(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/toggleCampaignPause?id={id}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        return false;
    }

    private class AddAttemptsResponse
    {
        public string? Result { get; set; }
        public List<EmailAttemptDto> Emails { get; set; } = [];
    }
}


