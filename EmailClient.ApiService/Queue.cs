
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EmailClient.ApiService
{
    public class Queue(QueueContext queueContext, MessageService messageService, ILogger<Queue> logger)
    {
        private readonly int MaxAttempts = 3;
        private int LoopInSeconds = 1;
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
            .Where(e => (e.Status == EmailStatus.Unsent) || (e.Status == EmailStatus.Failed && e.Attempts < MaxAttempts)).ToListAsync();

        public async Task<List<EmailAttempt>?> GetAllEmailAttempts() =>
            await queueContext.EmailAttempts.AsTracking(QueryTrackingBehavior.NoTracking).ToListAsync();

        public async Task UpdateEmailAttempt(int id, EmailStatus? status, DateTime? attempTime = null, int? attempts = null, string? result = null, int? errorCode = null)
        {
            var targetAttempt = queueContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return;
            targetAttempt.Status = status ?? targetAttempt.Status;
            targetAttempt.Attempts = attempts ?? targetAttempt.Attempts;
            targetAttempt.Result = result ?? targetAttempt.Result;
            targetAttempt.ErrorCode = errorCode ?? targetAttempt.ErrorCode;
            targetAttempt.LastAttempt = attempTime ?? targetAttempt.LastAttempt;
            queueContext.EmailAttempts.Update(targetAttempt);
            await queueContext.SaveChangesAsync();
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
                foreach (var attempt in emailAttempts)
                {
                    if (!QueueRunning)
                    {
                        return;
                    }

                    await UpdateEmailAttempt(attempt.Id, EmailStatus.InProgress, DateTime.UtcNow, ++attempt.Attempts);

                    // Simulate sending email
                    await Task.Delay(1000);


                    await UpdateEmailAttempt(attempt.Id, EmailStatus.Sent, DateTime.UtcNow);
                }
                //var allAttempts = await GetAllEmailAttempts() ?? [];
                //await messageService.AttemptsUpdated(allAttempts);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while running the queue: {ErrorMessage}", ex.Message);
            }
        }
    }
}
