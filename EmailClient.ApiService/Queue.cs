using EmailClient.Mailing;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public class Queue(
        ContextQueue contextQueue, 
        MessageService messageService, 
        IMailer mailer, 
        ILogger<Queue> logger, 
        IConfiguration configuration) : IDisposable
    {
        private readonly int MaxAttempts = int.Parse(configuration[Strings.QueueConfig.MaxAttempts] ?? "3");
        private readonly int SecondsBetweenLoops = int.Parse(configuration[Strings.QueueConfig.SecondsBetweenLoops] ?? "5");
        private readonly int SecondsBetweenErrors = int.Parse(configuration[Strings.QueueConfig.SecondsBetweenErrors] ?? "3");
        private PeriodicTimer? timer;

        public bool QueueRunning { get; private set; } = false;

        public void StartQueue()
        {
            if (QueueRunning)
            {
                logger.LogInformation(Strings.QueueLogInfo.QueueRunning);
                return;
            }
            _ = RunQueue();
            QueueRunning = true;
            logger.LogInformation(Strings.QueueLogInfo.QueueStarted);
        }

        public void StopQueue()
        {
            if (!QueueRunning)
            {
                logger.LogInformation(Strings.QueueLogInfo.QueueNotRunning);
                return;
            }
            timer?.Dispose();
            QueueRunning = false;
            logger.LogInformation(Strings.QueueLogInfo.QueueStopped);
        }

        private async Task RunQueue()
        {
            await Task.Yield();
            timer?.Dispose();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(SecondsBetweenLoops));
            while (await timer.WaitForNextTickAsync())
            {
                if (!QueueRunning)
                {
                    return;
                }
                await ProcessQueue();
                logger.LogInformation(Strings.QueueLogInfo.QueueProcessingTime, DateTime.Now);
            }
        }

        private async Task<List<EmailAttempt>?> GetUnsentEmailAttempts() =>
            await contextQueue.Query(async db => await db.EmailAttempts.AsNoTracking()
                .Include(e => e.Campaign)
                .Where(e => (e.Status == EmailStatus.Unsent) || (e.Status == EmailStatus.Failed && e.Attempts < MaxAttempts))
                .OrderBy(e => e.CampaignId)
                .ToListAsync());

        private async Task<Campaign?> GetCampaign(int campaignId)
        {
            return await contextQueue.Query(async db => await db.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId));
        }

        private async Task<List<Campaign>?> GetAllCampaigns()
        {
            return await contextQueue.Query(async db => await db.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().ToListAsync());
        }
            
        private async Task<EmailAttempt?> GetEmailAttemptByMessageId(string? messageId)
        {
            return await contextQueue.Query(async db => await db.EmailAttempts.AsNoTracking()
                .Include(e => e.Campaign)
                .FirstOrDefaultAsync(e =>
                e.MessageId == messageId));
        }

        private async Task UpdateEmailAttempt(int id, EmailStatus? status, int? attempts = null, string? messageId = null, string? result = null, int? errorCode = null)
        {
            await contextQueue.Query(async db =>
            {
                var targetAttempt = db.EmailAttempts.FirstOrDefault(a => a.Id == id);
                if (targetAttempt == null) return null;
                targetAttempt.Status = status ?? targetAttempt.Status;
                targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
                targetAttempt.Result = result ?? targetAttempt.Result;
                targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
                targetAttempt.MessageId = messageId ?? targetAttempt.MessageId;
                targetAttempt.LastAttempt = DateTime.UtcNow;
                db.EmailAttempts.Update(targetAttempt);
                await db.SaveChangesAsync();
                return null;
            });
        }

        private async Task SendNotify(int campaignId)
        {
            var camp = CampaignDto.ToDto(await GetCampaign(campaignId));
            if (camp != null)
                await messageService.CampaignUpdated(camp);

            await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await GetAllCampaigns() ?? []));
        }

        private void AddMailerListeners()
        {
            RemoveMailerListeners();
            mailer.OnSenderNotAccepted += Mailer_OnSenderNotAccepted;
            mailer.OnRecipientNotAccepted += Mailer_OnRecipientNotAccepted;
            mailer.OnNoRecipientsAccepted += Mailer_OnNoRecipientsAccepted;
            mailer.OnMessageSent += Mailer_MessageSent;
        }

        private void RemoveMailerListeners()
        {
            mailer.OnSenderNotAccepted -= Mailer_OnSenderNotAccepted;
            mailer.OnRecipientNotAccepted -= Mailer_OnRecipientNotAccepted;
            mailer.OnNoRecipientsAccepted -= Mailer_OnNoRecipientsAccepted;
            mailer.OnMessageSent -= Mailer_MessageSent;
        }

        private async void Mailer_MessageSent(object? sender, MailKit.MessageSentEventArgs e)
        {
            var attempt = await GetEmailAttemptByMessageId(e.Message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttempt(attempt.Id, EmailStatus.Sent, attempt.Attempts, attempt.MessageId, e.Response);
                await SendNotify(attempt.CampaignId);
            }
        }

        private async void Mailer_OnRecipientNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttempt(attempt.Id, EmailStatus.Failed, attempt.Attempts, attempt.MessageId, Strings.QueueLogInfo.RecipientNotAccepted, 550);
                await SendNotify(attempt.CampaignId);
            }
        }

        private async void Mailer_OnNoRecipientsAccepted(MimeMessage message)
        {
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttempt(attempt.Id, EmailStatus.Failed, attempt.Attempts, message.MessageId, Strings.QueueLogInfo.RecipientNotAccepted, 550);
                await SendNotify(attempt.CampaignId);
            }
        }

        private async void Mailer_OnSenderNotAccepted(MimeMessage message, MailboxAddress mailbox, SmtpResponse response)
        {
            var attempt = await GetEmailAttemptByMessageId(message.MessageId);
            if (attempt != null)
            {
                await UpdateEmailAttempt(attempt.Id, EmailStatus.Failed, attempt.Attempts, message.MessageId, Strings.QueueLogInfo.SenderNotAccepted, 550);
                await SendNotify(attempt.CampaignId);
            }
        }

        private async Task ConfigureMailer(string username)
        {
            AddMailerListeners();
            await mailer.Configure(new MailerSettings 
            {
                Username = username,
                Host = configuration[$"ExternalEmailHosts:{username}:host"],
                Port = int.Parse(configuration[$"ExternalEmailHosts:{username}:port"] ?? "587"),
                Password = configuration[$"ExternalEmailHosts:{username}:pass"]
            });
        }

        private async Task CloseSmtpConnection()
        {
            RemoveMailerListeners();
            await mailer.CloseSmtpConnection();
        }

        private async Task ProcessQueue()
        {
            try
            {
                var emailAttempts = await GetUnsentEmailAttempts();
                if (emailAttempts == null || emailAttempts.Count == 0)
                {
                    logger.LogInformation(Strings.QueueLogInfo.NoMoreEmails);
                    StopQueue();
                    return;
                }

                
                int currentCampaignId = -1;
                foreach (var attempt in emailAttempts)
                {
                    if (!QueueRunning)
                    {
                        await CloseSmtpConnection();
                        return;
                    }

                    if (attempt.Campaign == null)
                    {
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attempts: ++attempt.Attempts,
                            messageId: attempt.MessageId,
                            result: Strings.QueueLogInfo.CampaignNotFound,
                            errorCode: 404
                        );

                        await SendNotify(attempt.CampaignId);
                        continue;
                    }

                    if (attempt.Campaign.State == CampaignState.Paused)
                    {
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Paused
                        );

                        await SendNotify(attempt.CampaignId);
                        continue;
                    }

                    if (attempt.CampaignId != currentCampaignId || !mailer.IsConnected())
                    {
                        currentCampaignId = attempt.CampaignId;
                        await mailer.CloseSmtpConnection();
                        try
                        {
                            await ConfigureMailer(attempt.Campaign.Sender);
                        }
                        catch(Exception e)
                        {
                            logger.LogError(e, Strings.QueueLogInfo.ConnectionProblem, e.Message);
                            await UpdateEmailAttempt(
                                attempt.Id,
                                status: EmailStatus.Failed,
                                attempts: ++attempt.Attempts,
                                messageId: attempt.MessageId,
                                result: e.Message,
                                errorCode: 500
                            );

                            await SendNotify(attempt.CampaignId);
                            await Task.Delay(SecondsBetweenErrors * 1000);
                            continue;
                        }
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
                        await SendNotify(attempt.CampaignId);
                        await mailer.SendEmail(message);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, Strings.QueueLogInfo.SendingFailed, attempt.Email, e.Message);
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attempts: ++attempt.Attempts,
                            result: e.Message,
                            errorCode: 500
                        );
                        await SendNotify(attempt.CampaignId);
                    }
                }

                await CloseSmtpConnection();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.QueueLogInfo.QueueFailureError, ex.Message);
            }
        }

        void IDisposable.Dispose()
        {
            RemoveMailerListeners();
            timer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
