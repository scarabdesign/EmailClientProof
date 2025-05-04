using MailKit.Net.Smtp;

namespace MailKit.Client;

/// <summary>
/// A factory for creating <see cref="ISmtpClient"/> instances
/// given a <paramref name="smtpUri"/> (and optional <paramref name="credentials"/>).
/// </summary>
/// <param name="settings">
/// The <see cref="MailKitClientSettings"/> settings for the SMTP server
/// </param>
public sealed class MailKitClientFactory(MailKitClientSettings settings) : IDisposable
{
    public void Dispose()
    {
        //throw new NotImplementedException();
    }

    /// <summary>
    /// Gets an <see cref="ISmtpClient"/> instance in the connected state
    /// (and that's been authenticated if configured).
    /// </summary>
    /// <param name="cancellationToken">Used to abort client creation and connection.</param>
    /// <returns>A connected (and authenticated) <see cref="ISmtpClient"/> instance.</returns>
    /// <remarks>
    /// Since both the connection and authentication are considered expensive operations,
    /// the <see cref="ISmtpClient"/> returned is intended to be used for the duration of a request
    /// (registered as 'Scoped') and is automatically disposed of.
    /// </remarks>
    public async Task<ISmtpClient> GetSmtpClientAsync(
        CancellationToken cancellationToken = default)
    {
        var client = new SmtpClient();
        try
        {
            if (settings.Endpoint is not null)
            {
                await client.ConnectAsync(settings.Endpoint, cancellationToken)
                             .ConfigureAwait(false);
            }
            return client;
        }
        catch
        {
            await client.DisconnectAsync(true, cancellationToken);
            client.Dispose();
            throw;
        }
    }

    public async Task<ISmtpClient> GetCustomClientAsync(
        string endpoint,
        int port = 587,
        bool useSsl = true,
        string? username = null,
        string? password = null,
        CancellationToken cancellationToken = default
    )
    {
        var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(
                endpoint, port,
                useSsl ? Security.SecureSocketOptions.StartTls : Security.SecureSocketOptions.Auto,
                cancellationToken
            );

            if (useSsl && username != null && password != null)
            {
                await client.AuthenticateAsync(username, password, cancellationToken);
            }

            return client;
        }
        catch
        {
            await client.DisconnectAsync(true, cancellationToken);
            client.Dispose();
            throw;
        }
    }
}