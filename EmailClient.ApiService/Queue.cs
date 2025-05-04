
using MailKit.Client;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Net.Mail;

namespace EmailClient.ApiService
{
    public class Queue(QueueContext queueContext, MessageService messageService, MailKitClientFactory mailKitFactory, ILogger<Queue> logger)
    {
        private readonly int MaxAttempts = 3;
        private int LoopInSeconds = 5;
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
            .Where(e => (e.Status == EmailStatus.Unsent) || (e.Status == EmailStatus.Failed && e.Attempts < MaxAttempts)).ToListAsync();

        //public async Task<List<EmailAttempt>?> GetAllEmailAttempts() =>
        //    await queueContext.EmailAttempts.AsTracking(QueryTrackingBehavior.NoTracking).ToListAsync();
        public async Task<Campaign?> GetCampaign(int campaignId) =>
            await queueContext.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId);

        public async Task UpdateEmailAttempt(int id, EmailStatus? status, DateTime? attemptTime = null, int? attempts = null, string? result = null, int? errorCode = null)
        {
            var targetAttempt = queueContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return;
            targetAttempt.Status = status ?? targetAttempt.Status;
            targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
            targetAttempt.Result = result ?? targetAttempt.Result;
            targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
            targetAttempt.LastAttempt = attemptTime ?? targetAttempt.LastAttempt;
            queueContext.EmailAttempts.Update(targetAttempt);
            await queueContext.SaveChangesAsync();
        }

        private async Task SendNotify(int campaignId)
        {
            var camp = Dto.CampaignDto.ToDto(await GetCampaign(campaignId));
            if (camp != null)
                await messageService.AttemptsUpdated(camp);
        }

        private async Task ProcessQueue()
        {
            try
            {
                var emailAttempts = await GetUnsentEmailAttempts();
                if (emailAttempts == null || !emailAttempts.Any())
                {
                    logger.LogInformation("No email attempts to process.");
                    StopQueue();
                    return;
                }

                var smtpClient = await mailKitFactory.GetSmtpClientAsync();

                foreach (var attempt in emailAttempts)
                {
                    if (!QueueRunning)
                    {
                        return;
                    }

                    if (attempt.Campaign == null)
                    {
                        await UpdateEmailAttempt(
                            attempt.Id, 
                            status: EmailStatus.Failed, 
                            attemptTime: DateTime.UtcNow, 
                            attempt.Attempts,
                            result: "Campaign not found",
                            errorCode: 404
                        );

                        await SendNotify(attempt.CampaignId);
                        continue;
                    }

                    await UpdateEmailAttempt(attempt.Id, EmailStatus.InProgress, DateTime.UtcNow, ++attempt.Attempts);
                    try
                    {
                        using var message = new MailMessage(attempt.Campaign.Sender, attempt.Email)
                        {
                            Subject = attempt.Campaign.Subject,
                            Body = attempt.Campaign.Body
                        };
                        await smtpClient.SendAsync(MimeMessage.CreateFromMailMessage(message));
                        await UpdateEmailAttempt(attempt.Id, EmailStatus.Sent, DateTime.UtcNow);
                    }
                    catch(Exception e)
                    {
                        logger.LogError(e, "Failed to send email to {Email}: {ErrorMessage}", attempt.Email, e.Message);
                        await UpdateEmailAttempt(
                            attempt.Id,
                            status: EmailStatus.Failed,
                            attemptTime: DateTime.UtcNow,
                            attempts: attempt.Attempts,
                            result: e.Message,
                            errorCode: 500
                        );
                    }
                    
                    await SendNotify(attempt.CampaignId);
                }
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the queue: {ErrorMessage}", ex.Message);
            }
        }
    }
}
