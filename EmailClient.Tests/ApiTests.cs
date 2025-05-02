namespace EmailClient.Tests;

using Aspire.Hosting;
using EmailClient.ApiService;
using System.IO;
using System.Text.Json;
using Xunit.Abstractions;

public class ApiTests
{
    private readonly ITestOutputHelper output;
    private DistributedApplication? app;
    private ResourceNotificationService? resourceNotificationService;
    public ApiTests(ITestOutputHelper output)
    {
        this.output = output;


    }

    private async Task CreateAppHost()
    {
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.EmailClient_ApiService>();
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
        app = await appHost.BuildAsync();
        resourceNotificationService = app.Services.GetRequiredService<ResourceNotificationService>();
        await app.StartAsync();
    }

    [Fact]
    public async Task AddCampainsWithGoodDataReturnsOkAndId()
    {
        // Arrange
        await CreateAppHost();
        if (app == null || resourceNotificationService == null)
        {
            throw new InvalidOperationException("App or ResourceNotificationService is not initialized.");
        }

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await resourceNotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        var response = await CreateCampain(httpClient);

        // Assert
        Assert.Equal("ok", response?.Result);
        Assert.True(response?.Id > 0);

        // Act 2
        var response2 = await CreateCampain(httpClient, 2);

        // Assert 2
        Assert.Equal("ok", response2?.Result);
        Assert.True(response2?.Id > response?.Id);
    }
    
    [Fact]
    public async Task GetAllCampainsReturnsList()
    {
        // Arrange
        await CreateAppHost();
        if (app == null || resourceNotificationService == null)
        {
            throw new InvalidOperationException("App or ResourceNotificationService is not initialized.");
        }

        // Act
        var httpClient = app.CreateHttpClient("apiservice");
        await resourceNotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));

        var listresponse = await GetCampaigns(httpClient);

        // Assert
        Assert.NotNull(listresponse);
        Assert.True(listresponse?.Count == 0);

        // Act
        var response = await CreateCampain(httpClient);

        // Assert
        Assert.Equal("ok", response?.Result);
        Assert.True(response?.Id > 0);

        // Act
        var listresponse2 = await GetCampaigns(httpClient);

        // Assert
        Assert.NotNull(listresponse2);
        Assert.True(listresponse2?.Count > 0);
    }

    private async Task<List<Campaign>?> GetCampaigns(HttpClient client)
    {
        var response = await client.GetAsync("/getAllCampaigns");
        output.WriteLine(response.ToString());
        var strRes = await response.Content.ReadAsStringAsync();
        output.WriteLine(strRes);
        List<Campaign>? result = null;
        try
        {
            result = JsonSerializer.Deserialize<List<Campaign>>(strRes, jOpts);
        }
        catch (Exception ex)
        {
            output.WriteLine(ex.Message);
        }
        return result;
    }

    private async Task<ResponseResult?> CreateCampain(HttpClient client, int indent = 1)
    {
        var response = await client.PostAsync("/addCampaign", 
            new StringContent(
                JsonSerializer.Serialize(
                    new Campaign
                    {
                        Body = $"Test Body {indent}",
                        Name = $"Test Campaign {indent}",
                        Sender = "scarabdesign@gmail.com",
                        Subject = $"Test Subject {indent}",
                    }
                ), 
                System.Text.Encoding.UTF8, 
                "application/json"
            )
        );
        ResponseResult? result = null;
        try
        {
            var strRes = await response.Content.ReadAsStringAsync();
            output.WriteLine(strRes);
            result = JsonSerializer.Deserialize<ResponseResult>(strRes, jOpts);
        }
        catch (Exception ex)
        {
            output.WriteLine(ex.Message);
        }

        return result;
    }

    private JsonSerializerOptions jOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private class ResponseResult
    {
        public required string Result { get; set; } = "failed";
        public required int Id { get; set; }
    }
}
