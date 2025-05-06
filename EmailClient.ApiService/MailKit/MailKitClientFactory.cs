using MailKit.Net.Smtp;
using MimeKit;

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

    public delegate void SenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
    public event SenderAccepted? OnSenderAccepted;

    public delegate void SenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
    public event SenderNotAccepted? OnSenderNotAccepted;

    public delegate void RecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
    public event RecipientAccepted? OnRecipientAccepted;

    public delegate void RecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
    public event RecipientNotAccepted? OnRecipientNotAccepted;

    public delegate void NoRecipientsAccepted(MimeMessage message);
    public event NoRecipientsAccepted? OnNoRecipientsAccepted;

    public void Dispose()
    {
        
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
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        var client = new ProofClient(logger);
        client.CallOnSenderAccepted += Client_CallOnSenderAccepted;
        client.CallOnSenderNotAccepted += Client_CallOnSenderNotAccepted;
        client.CallOnRecipientAccepted += Client_CallOnRecipientAccepted;
        client.CallOnRecipientNotAccepted += Client_CallOnRecipientNotAccepted;
        client.CallOnNoRecipientsAccepted += Client_CallOnNoRecipientsAccepted;
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
            client.CallOnSenderAccepted -= Client_CallOnSenderAccepted;
            client.CallOnSenderNotAccepted -= Client_CallOnSenderNotAccepted;
            client.CallOnRecipientAccepted -= Client_CallOnRecipientAccepted;
            client.CallOnRecipientNotAccepted -= Client_CallOnRecipientNotAccepted;
            client.CallOnNoRecipientsAccepted -= Client_CallOnNoRecipientsAccepted;
            await client.DisconnectAsync(true, cancellationToken);
            client.Dispose();
            throw;
        }
    }

    private void Client_CallOnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        OnSenderNotAccepted?.Invoke(message, mailbox, response);
    }

    private void Client_CallOnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        OnSenderAccepted?.Invoke(message, mailbox, response);
    }

    private void Client_CallOnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        OnRecipientNotAccepted?.Invoke(message, mailbox, response);
    }

    private void Client_CallOnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        OnRecipientAccepted?.Invoke(message, mailbox, response);
    }

    private void Client_CallOnNoRecipientsAccepted(MimeMessage message)
    {
        OnNoRecipientsAccepted?.Invoke(message);
    }

    public async Task<ISmtpClient> GetCustomClientAsync(
        string endpoint,
        int port = 587,
        bool useSsl = true,
        string? username = null,
        string? password = null,
        ILogger? logger = null,
        CancellationToken cancellationToken = default
    )
    {
        var client = new ProofClient(logger);
        client.CallOnSenderAccepted += Client_CallOnSenderAccepted;
        client.CallOnSenderNotAccepted += Client_CallOnSenderNotAccepted;
        client.CallOnRecipientAccepted += Client_CallOnRecipientAccepted;
        client.CallOnRecipientNotAccepted += Client_CallOnRecipientNotAccepted;
        client.CallOnNoRecipientsAccepted += Client_CallOnNoRecipientsAccepted;

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
            client.CallOnSenderAccepted -= Client_CallOnSenderAccepted;
            client.CallOnSenderNotAccepted -= Client_CallOnSenderNotAccepted;
            client.CallOnRecipientAccepted -= Client_CallOnRecipientAccepted;
            client.CallOnRecipientNotAccepted -= Client_CallOnRecipientNotAccepted;
            client.CallOnNoRecipientsAccepted -= Client_CallOnNoRecipientsAccepted;
            await client.DisconnectAsync(true, cancellationToken);
            client.Dispose();
            throw;
        }
    }

    public class ProofClient(ILogger? Logger) : SmtpClient
    {
        public delegate void SenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
        public event SenderAccepted? CallOnSenderAccepted;

        public delegate void SenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
        public event SenderNotAccepted? CallOnSenderNotAccepted;

        public delegate void RecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
        public event RecipientAccepted? CallOnRecipientAccepted;

        public delegate void RecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response);
        public event RecipientNotAccepted? CallOnRecipientNotAccepted;

        public delegate void NoRecipientsAccepted(MimeMessage message);
        public event NoRecipientsAccepted? CallOnNoRecipientsAccepted;

        protected override void OnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //Logger?.LogError("OnSenderAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            CallOnSenderAccepted?.Invoke(message, mailbox, response);
            base.OnSenderAccepted(message, mailbox, response);
        }

        protected override void OnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //Logger?.LogError("OnSenderNotAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            CallOnSenderNotAccepted?.Invoke(message, mailbox, response);
            base.OnSenderNotAccepted(message, mailbox, response);
        }


        protected override void OnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //Logger?.LogError("OnRecipientAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            CallOnRecipientAccepted?.Invoke(message, mailbox, response);
            base.OnRecipientAccepted(message, mailbox, response);
        }

        protected override void OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //Logger?.LogError("OnRecipientNotAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            CallOnRecipientNotAccepted?.Invoke(message, mailbox, response);
            base.OnRecipientNotAccepted(message, mailbox, response);
        }

        protected override void OnNoRecipientsAccepted(MimeMessage message)
        {
            //Logger?.LogError("OnNoRecipientsAccepted called: {Message}", message.ToString());
            CallOnNoRecipientsAccepted?.Invoke(message);
            base.OnNoRecipientsAccepted(message);
        }
    }
}