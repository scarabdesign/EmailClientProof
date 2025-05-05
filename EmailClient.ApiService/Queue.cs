using MailKit.Client;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Net.Mail;
using System.Text.RegularExpressions;
using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public class Queue(QueueContext queueContext, MessageService messageService, MailKitClientFactory mailKitFactory, ILogger<Queue> logger, IConfiguration configuration)
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

        public async Task<List<EmailAttempt>?> GetUnsentEmailAttempts() =>
            await queueContext.EmailAttempts.AsNoTracking()
            .Include(e => e.Campaign)
            .Where(e => (e.Status == EmailStatus.Unsent) || (e.Status == EmailStatus.Failed && e.Attempts < MaxAttempts))
            .OrderBy(e => e.CampaignId)
            .ToListAsync();

        public async Task<Campaign?> GetCampaign(int campaignId) =>
            await queueContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId);

        public async Task<List<Campaign>> GetAllCampaigns() =>
            await queueContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().ToListAsync();

        public async Task UpdateEmailAttempt(int id, EmailStatus? status,int? attempts = null, string? result = null, int? errorCode = null)
        {
            var targetAttempt = queueContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return;
            targetAttempt.Status = status ?? targetAttempt.Status;
            targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
            targetAttempt.Result = result ?? targetAttempt.Result;
            targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
            targetAttempt.LastAttempt = DateTime.UtcNow;
            queueContext.EmailAttempts.Update(targetAttempt);
            await queueContext.SaveChangesAsync();
        }

        private async Task SendNotify(int campaignId)
        {
            var camp = CampaignDto.ToDto(await GetCampaign(campaignId));
            if (camp != null)
                await messageService.CampaignUpdated(camp);

            await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await GetAllCampaigns()));
        }

        private async Task<ISmtpClient> GetEmailClient(string? username = null)
        {
            if (username == null)
            {
                logger.LogError("Email client username is null. Returning locally hosted solution");
                return await mailKitFactory.GetSmtpClientAsync();
            }

            var host = configuration[$"ExternalEmailHosts:{username}:host"];
            var port = int.Parse(configuration[$"ExternalEmailHosts:{username}:port"] ?? "587");
            var password = configuration[$"ExternalEmailHosts:{username}:pass"];
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(password))
            {
                logger.LogError("Email client configuration is missing. Returning locally hosted solution");
                return await mailKitFactory.GetSmtpClientAsync();
            }

            return await mailKitFactory.GetCustomClientAsync(
                host, port, true, username, password
            );
        }

        private static async Task CloseSmtpConnection(ISmtpClient? smtpClient = null)
        {
            if (smtpClient != null)
            {
                await smtpClient.DisconnectAsync(true);
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
                            attempt.Attempts,
                            result: "Campaign not found",
                            errorCode: 404
                        );

                        await SendNotify(attempt.CampaignId);
                        continue;
                    }

                    if (attempt.CampaignId != currentCampaignId)
                    {
                        currentCampaignId = attempt.CampaignId;
                        await CloseSmtpConnection(smtpClient);
                        smtpClient = await GetEmailClient(attempt.Campaign.Sender);
                    }

                    if (smtpClient == null)
                    {
                        logger.LogError("SMTP client is null. Skipping email attempt: {Email}", attempt.Email);
                        continue;
                    }

                    await UpdateEmailAttempt(attempt.Id, EmailStatus.InProgress, ++attempt.Attempts);
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
                            }.ToMessageBody()
                        };
                        await smtpClient.SendAsync(message);
                        await UpdateEmailAttempt(attempt.Id, EmailStatus.Sent);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to send email to {Email}: {ErrorMessage}", attempt.Email, e.Message);
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attempts: attempt.Attempts,
                            result: e.Message,
                            errorCode: 500
                        );
                    }

                    await SendNotify(attempt.CampaignId);
                }

                await CloseSmtpConnection(smtpClient);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the queue: {ErrorMessage}", ex.Message);
            }
        }
    }
}
