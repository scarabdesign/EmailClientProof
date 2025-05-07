using MailKit.Net.Smtp;
using MimeKit;

namespace MailKit.Client;

public class ProofClient() : SmtpClient
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
        CallOnSenderAccepted?.Invoke(message, mailbox, response);
        base.OnSenderAccepted(message, mailbox, response);
    }

    protected override void OnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        CallOnSenderNotAccepted?.Invoke(message, mailbox, response);
        base.OnSenderNotAccepted(message, mailbox, response);
    }


    protected override void OnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        CallOnRecipientAccepted?.Invoke(message, mailbox, response);
        base.OnRecipientAccepted(message, mailbox, response);
    }

    protected override void OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
    {
        CallOnRecipientNotAccepted?.Invoke(message, mailbox, response);
        base.OnRecipientNotAccepted(message, mailbox, response);
    }

    protected override void OnNoRecipientsAccepted(MimeMessage message)
    {
        CallOnNoRecipientsAccepted?.Invoke(message);
        base.OnNoRecipientsAccepted(message);
    }


}