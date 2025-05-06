using MailKit.Client;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public class Queue(QueueContext queueContext, MailKitResponseContext mailKitResponseContext, MessageService messageService, MailKitClientFactory mailKitFactory, ILogger<Queue> logger, IConfiguration configuration) : IDisposable
    {
        private readonly int MaxAttempts = 3;
        private readonly int LoopInSeconds = 5;
        private PeriodicTimer? timer;

        public bool QueueRunning { get; private set; } = false;

        public void StartQueue()
        {
            if (QueueRunning)
            {
                logger.LogInformation("Queue is already running.");
                return;
            }
            _ = RunQueue();
            QueueRunning = true;
            logger.LogInformation("Queue started.");
        }

        public void StopQueue()
        {
            if (!QueueRunning)
            {
                logger.LogInformation("Queue is not running.");
                return;
            }
            timer?.Dispose();
            QueueRunning = false;
            logger.LogInformation("Queue stopped.");
        }

        private async Task RunQueue()
        {
            await Task.Yield();
            timer?.Dispose();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(LoopInSeconds));
            while (await timer.WaitForNextTickAsync())
            {
                if (!QueueRunning)
                {
                    return;
                }
                await ProcessQueue();
                logger.LogInformation("Queue processed at {Time}", DateTime.Now);
            }
        }

        private async Task<List<EmailAttempt>?> GetUnsentEmailAttempts() =>
            await queueContext.EmailAttempts.AsNoTracking()
                .Include(e => e.Campaign)
                .Where(e => (e.Status == EmailStatus.Unsent) || (e.Status == EmailStatus.Failed && e.Attempts < MaxAttempts))
                .OrderBy(e => e.CampaignId)
                .ToListAsync();

        private async Task<Campaign?> GetCampaign(int campaignId, bool isQueue)
        {
            if (isQueue)
            {
                return await queueContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId);
            }
            else
            {
                return await mailKitResponseContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId);
            }
        }
            

        private async Task<List<Campaign>> GetAllCampaigns() =>
            await queueContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().ToListAsync();

        private async Task<EmailAttempt?> GetEmailAttemptByMessageId(string? messageId) =>
            await mailKitResponseContext.EmailAttempts.AsNoTracking()
                .Include(e => e.Campaign)
                .FirstOrDefaultAsync(e => e.MessageId == messageId);

        private async Task UpdateEmailAttempt(int id, EmailStatus? status, int? attempts = null, string? messageId = null, string? result = null, int? errorCode = null)
        {
            var targetAttempt = queueContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return;
            targetAttempt.Status = status ?? targetAttempt.Status;
            targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
            targetAttempt.Result = result ?? targetAttempt.Result;
            targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
            targetAttempt.MessageId = messageId ?? targetAttempt.MessageId;
            targetAttempt.LastAttempt = DateTime.UtcNow;
            queueContext.EmailAttempts.Update(targetAttempt);
            await queueContext.SaveChangesAsync();
        }

        private async Task UpdateEmailAttemptFromResponse(int id, EmailStatus? status, int? attempts = null, string? messageId = null, string? result = null, int? errorCode = null)
        {
            var targetAttempt = mailKitResponseContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return;
            targetAttempt.Status = status ?? targetAttempt.Status;
            targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
            targetAttempt.Result = result ?? targetAttempt.Result;
            targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
            targetAttempt.MessageId = messageId ?? targetAttempt.MessageId;
            targetAttempt.LastAttempt = DateTime.UtcNow;
            mailKitResponseContext.EmailAttempts.Update(targetAttempt);
            await mailKitResponseContext.SaveChangesAsync();
        }

        private async Task SendNotify(int campaignId, bool isQueue)
        {
            var camp = CampaignDto.ToDto(await GetCampaign(campaignId, isQueue));
            if (camp != null)
                await messageService.CampaignUpdated(camp);

            await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await GetAllCampaigns()));
        }

        private void AddMailKitListeners()
        {
            RemoveMailKitListeners();
            mailKitFactory.OnSenderAccepted += MailKitFactory_OnSenderAccepted;
            mailKitFactory.OnSenderNotAccepted += MailKitFactory_OnSenderNotAccepted;
            mailKitFactory.OnRecipientAccepted += MailKitFactory_OnRecipientAccepted;
            mailKitFactory.OnRecipientNotAccepted += MailKitFactory_OnRecipientNotAccepted;
            mailKitFactory.OnNoRecipientsAccepted += MailKitFactory_OnNoRecipientsAccepted;
        }

        private void RemoveMailKitListeners()
        {
            mailKitFactory.OnSenderAccepted -= MailKitFactory_OnSenderAccepted;
            mailKitFactory.OnSenderNotAccepted -= MailKitFactory_OnSenderNotAccepted;
            mailKitFactory.OnRecipientAccepted -= MailKitFactory_OnRecipientAccepted;
            mailKitFactory.OnRecipientNotAccepted -= MailKitFactory_OnRecipientNotAccepted;
            mailKitFactory.OnNoRecipientsAccepted -= MailKitFactory_OnNoRecipientsAccepted;
        }

        private async void SmtpClient_MessageSent(object? sender, MailKit.MessageSentEventArgs e)
        {
            logger?.LogInformation("MessageSent called. sender: {sender}, message: {message}, response: {response}", sender?.GetType(), e.Message, e.Response);
            var attempt = await GetEmailAttemptByMessageId(e.Message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttemptFromResponse(attempt.Id, EmailStatus.Sent);
                await SendNotify(attempt.CampaignId, false);
            }
        }

        private async void MailKitFactory_OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            logger?.LogError("OnRecipientNotAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttemptFromResponse(attempt.Id, EmailStatus.Failed, attempt.Attempts, message.MessageId, "Recipient Not Accepted", 550);
                await SendNotify(attempt.CampaignId, false);
            }
        }

        private async void MailKitFactory_OnNoRecipientsAccepted(MimeMessage message)
        {
            logger?.LogError("OnNoRecipientsAccepted called. Message: {Message}", message.ToString());
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttemptFromResponse(attempt.Id, EmailStatus.Failed, attempt.Attempts, message.MessageId, "Recipient Not Accepted", 550);
                await SendNotify(attempt.CampaignId, false);
            }
        }

        private async void MailKitFactory_OnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            logger?.LogError("OnSenderNotAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttemptFromResponse(attempt.Id, EmailStatus.Failed, attempt.Attempts, message.MessageId, "Sender Not Accepted", 550);
                await SendNotify(attempt.CampaignId, false);
            }
        }

        private void MailKitFactory_OnRecipientAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //logger?.LogInformation("OnRecipientAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
        }

        private void MailKitFactory_OnSenderAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            //logger?.LogInformation("OnSenderAccepted called. Message: {Message}, MailboxAddress: {Nailbox}, SmtpResponse: {Response}", message.ToString(), mailbox.Address, response.StatusCode);
        }

        private async Task<ISmtpClient> GetEmailClient(string? username = null)
        {
            AddMailKitListeners();
            ISmtpClient? client;
            if (username == null)
            {
                logger.LogError("Email client username is null. Returning locally hosted solution");
                client = await mailKitFactory.GetSmtpClientAsync(logger);
                client.MessageSent += SmtpClient_MessageSent;
                return client;
            }

            var host = configuration[$"ExternalEmailHosts:{username}:host"];
            var port = int.Parse(configuration[$"ExternalEmailHosts:{username}:port"] ?? "587");
            var password = configuration[$"ExternalEmailHosts:{username}:pass"];
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(password))
            {
                logger.LogError("Email client configuration is missing. Returning locally hosted solution");
                client = await mailKitFactory.GetSmtpClientAsync(logger);
            }
            else
            {
                client = await mailKitFactory.GetCustomClientAsync(
                    host, port, true, username, password, logger
                );
            }

            client.MessageSent += SmtpClient_MessageSent;
            return client;
        }

        private async Task CloseSmtpConnection(ISmtpClient? smtpClient = null)
        {
            if (smtpClient != null)
            {
                await smtpClient.DisconnectAsync(true);
                RemoveMailKitListeners();
                smtpClient.Dispose();
            }
        }

        private async Task ProcessQueue()
        {
            try
            {
                var emailAttempts = await GetUnsentEmailAttempts();
                if (emailAttempts == null || emailAttempts.Count == 0)
                {
                    logger.LogInformation("No email attempts to process.");
                    StopQueue();
                    return;
                }

                ISmtpClient? smtpClient = null;
                int currentCampaignId = -1;
                foreach (var attempt in emailAttempts)
                {
                    if (!QueueRunning)
                    {
                        await CloseSmtpConnection(smtpClient);
                        return;
                    }

                    if (attempt.Campaign == null)
                    {
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attempts: ++attempt.Attempts,
                            messageId: attempt.MessageId,
                            result: "Campaign not found",
                            errorCode: 404
                        );

                        await SendNotify(attempt.CampaignId, true);
                        continue;
                    }

                    if (attempt.CampaignId != currentCampaignId)
                    {
                        currentCampaignId = attempt.CampaignId;
                        await CloseSmtpConnection(smtpClient);
                        try
                        {
                            smtpClient = await GetEmailClient(attempt.Campaign.Sender);
                        }
                        catch(Exception e)
                        {
                            logger.LogError(e, "Problem creating connection: {Message}", e.Message);
                            await UpdateEmailAttempt(
                                attempt.Id,
                                status: EmailStatus.Failed,
                                attempts: ++attempt.Attempts,
                                messageId: attempt.MessageId,
                                result: e.Message,
                                errorCode: 500
                            );

                            await SendNotify(attempt.CampaignId, true);
                            continue;
                        }
                    }

                    if (smtpClient == null)
                    {
                        logger.LogError("SMTP client is null. Skipping email attempt: {Email}", attempt.Email);
                        continue;
                    }

                    try
                    {
                        var message = new MimeMessage
                        {
                            Subject = attempt.Campaign.Subject,
                            From = { new MailboxAddress("", attempt.Campaign.Sender) },
                            To = { new MailboxAddress("", attempt.Email) },
                            Body = new BodyBuilder
                            {
                                HtmlBody = attempt.Campaign.Body,
                                TextBody = attempt.Campaign.Text,
                            }.ToMessageBody(),
                            MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId()
                        };

                        await UpdateEmailAttempt(attempt.Id, EmailStatus.InProgress, ++attempt.Attempts, message.MessageId);
                        await SendNotify(attempt.CampaignId, true);
                        await smtpClient.SendAsync(message);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to send email to {Email}: {ErrorMessage}", attempt.Email, e.Message);
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attempts: ++attempt.Attempts,
                            result: e.Message,
                            errorCode: 500
                        );
                        await SendNotify(attempt.CampaignId, true);
                    }
                }

                await CloseSmtpConnection(smtpClient);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the queue: {ErrorMessage}", ex.Message);
            }
        }

        public void Dispose()
        {
            RemoveMailKitListeners();
        }
    }
}
