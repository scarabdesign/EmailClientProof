using MailKit.Client;
using MailKit.Net.Smtp;
using MimeKit;

namespace EmailClient.Mailing
{

    public class Mailer(MailKitClientFactory mailKitClientFactory) : IMailer
    {
        public event IMailer.SenderAccepted? OnSenderAccepted;
        public event IMailer.SenderNotAccepted? OnSenderNotAccepted;
        public event IMailer.RecipientAccepted? OnRecipientAccepted;
        public event IMailer.RecipientNotAccepted? OnRecipientNotAccepted;
        public event IMailer.NoRecipientsAccepted? OnNoRecipientsAccepted;
        public event IMailer.MessageSent? OnMessageSent;
        private ProofClient? client;

        public async Task Configure(MailerSettings settings)
        {
            client = await GetEmailClient(
                settings.Username,
                settings.Password,
                settings.Host,
                settings.Port
            );
            client.MessageSent += Client_MessageSent;
            AddListeners(client);
        }

        public void AddListeners(ProofClient client)
        {
            RemoveListeners(client);
            client.CallOnSenderAccepted += CallOnSenderAccepted;
            client.CallOnSenderNotAccepted += CallOnSenderNotAccepted;
            client.CallOnRecipientAccepted += CallOnRecipientAccepted;
            client.CallOnRecipientNotAccepted += CallOnRecipientNotAccepted;
            client.CallOnNoRecipientsAccepted += CallOnNoRecipientsAccepted;
        }

        public void RemoveListeners(ProofClient client)
        {
            client.CallOnSenderAccepted -= CallOnSenderAccepted;
            client.CallOnSenderNotAccepted -= CallOnSenderNotAccepted;
            client.CallOnRecipientAccepted -= CallOnRecipientAccepted;
            client.CallOnRecipientNotAccepted -= CallOnRecipientNotAccepted;
            client.CallOnNoRecipientsAccepted -= CallOnNoRecipientsAccepted;
        }

        private async Task<ProofClient> GetEmailClient(
            string? username,
            string? password = null,
            string? host = null,
            int port = 587
        )
        {
            ProofClient? client;
            if (username == null)
            {
                client = await mailKitClientFactory.GetSmtpClientAsync();
                return client;
            }

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(password))
            {
                client = await mailKitClientFactory.GetSmtpClientAsync();
            }
            else
            {
                client = await mailKitClientFactory.GetCustomClientAsync(
                    host, port, true, username, password
                );
            }

            return client;
        }


        public async Task SendEmail(MimeMessage message)
        {
            if (client == null)
            {
                throw new InvalidOperationException("SMTP client is not configured.");
            }
            await client.SendAsync(message);
        }

        public async Task CloseSmtpConnection()
        {
            if (client != null)
            {
                await client.DisconnectAsync(true);
                client.Dispose();
                client = null;
            }
        }

        public bool IsConnected()
        {
            return client?.IsConnected ?? false;
        }

        private void Client_MessageSent(object? sender, MailKit.MessageSentEventArgs e)
        {
            OnMessageSent?.Invoke(sender, e);
        }

        public void CallOnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            OnSenderNotAccepted?.Invoke(message, mailbox, response);
        }

        public void CallOnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            OnSenderAccepted?.Invoke(message, mailbox, response);
        }

        public void CallOnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            OnRecipientNotAccepted?.Invoke(message, mailbox, response);
        }

        public void CallOnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            OnRecipientAccepted?.Invoke(message, mailbox, response);
        }

        public void CallOnNoRecipientsAccepted(MimeMessage message)
        {
            OnNoRecipientsAccepted?.Invoke(message);
        }
    }
}
