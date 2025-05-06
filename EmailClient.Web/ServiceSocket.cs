using Microsoft.AspNetCore.SignalR.Client;

namespace EmailClient.Web
{
    public class ServiceSocket(IHttpMessageHandlerFactory HttpMessageHandlerFactory) : IDisposable
    {
        private HubConnection? hubConnection;
        private List<string> Subs = new();

        public delegate void MessageReceived(string type, string message);
        public event MessageReceived? OnMessageReceived;

        public async Task WsConnect()
        {
            hubConnection = new HubConnectionBuilder()
                .WithUrl("http://apiservice/clientHub", HttpMessageHandlerFactory)
                .Build();

            await hubConnection.StartAsync();
        }

        public void Subscribe(string subscription)
        {
            if (!Subs.Contains(subscription))
            {
                hubConnection?.On<string>(subscription, message =>
                    OnMessageReceived?.Invoke(subscription, message));
                Subs.Add(subscription);
            }
        }

        public async void Dispose()
        {
            if (hubConnection is not null)
            {
                await hubConnection.DisposeAsync();
            }
        }
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
