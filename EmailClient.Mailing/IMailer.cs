using MailKit.Net.Smtp;
using MimeKit;

namespace EmailClient.Mailing
{
    public interface IMailer
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

        public delegate void MessageSent(object? sender, MailKit.MessageSentEventArgs e);
        public event MessageSent? OnMessageSent;

        public Task Configure(MailerSettings settings);
        public Task SendEmail(MimeMessage message);
        public Task CloseSmtpConnection();
        public bool IsConnected();

    }

    public class MailerSettings
    {
        public string? Host { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; }
    }
}
