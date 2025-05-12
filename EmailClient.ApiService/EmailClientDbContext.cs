using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailClient.ApiService
{

    public class EmailClientDbContext(DbContextOptions<EmailClientDbContext> options) : DbContext(options)
    {
        public DbSet<EmailAttempt> EmailAttempts { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EmailAttempt>()
                .HasOne(e => e.Campaign)
                .WithMany(c => c.EmailAttempts)
                .HasForeignKey(e => e.CampaignId);
        }
    }

    public class ContextQueue(EmailClientDbContext mailKitResponseContext, ILogger<ContextQueue> logger) : IDisposable
    {
        private ConcurrentQueue<Tuple<Func<EmailClientDbContext, Task<dynamic?>>, TaskCompletionSource<dynamic?>>> _queue = [];
        private readonly SemaphoreSlim seph = new(1, 1);
        private bool QueueRunning;

        public async Task<dynamic?> Query(Func<EmailClientDbContext, Task<dynamic?>> action)
        {
            var source = new TaskCompletionSource<dynamic?>();
            var t = new Tuple<Func<EmailClientDbContext, Task<dynamic?>>, TaskCompletionSource<dynamic?>>(action, source);
            _queue.Enqueue(t);
            if (!QueueRunning)
            {
                RunContextQueue();
            }
            return await source.Task;
        }

        private async void RunContextQueue()
        {
            QueueRunning = true;
            while (!_queue.IsEmpty)
            {
                await seph.WaitAsync();
                try
                {
                    if (_queue.TryDequeue(out var action))
                    {
                        action.Item2.SetResult(await action.Item1(mailKitResponseContext));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while dequeuing");
                    throw;
                }
                finally
                {
                    seph.Release();
                }
            }
            QueueRunning = false;
        }
        public void DisposeQueue() => _queue = new ConcurrentQueue<Tuple<Func<EmailClientDbContext, Task<dynamic?>>, TaskCompletionSource<dynamic?>>>();
        public void DisposeSemaphore() => seph.Dispose();
        public void Dispose()
        {
            DisposeQueue();
            DisposeSemaphore();
            GC.SuppressFinalize(this);
        }
    }

    [PrimaryKey(nameof(Id))]
    public class EmailAttempt
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Email { get; set; }
        public EmailStatus Status { get; set; } = EmailStatus.Unsent;
        public int Attempts { get; set; }
        public string? Result { get; set; }
        public int ErrorCode { get; set; } = -1;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? LastAttempt { get; set; }
        public string? MessageId { get; set; }
        public required int CampaignId { get; set; }
        public Campaign? Campaign { get; set; }
    }

    [PrimaryKey(nameof(Id))]
    public class Campaign
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Subject { get; set; }
        public required string Sender { get; set; }
        public required string Body { get; set; }
        public required string? Text { get; set; }
        public CampaignState State { get; set; } = CampaignState.Running;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Updated { get; set; } = DateTime.UtcNow;
        public List<EmailAttempt> EmailAttempts { get; set; } = [];
    }

    public enum CampaignState
    {
        Unknown,
        Running,
        Paused,
    }

    public enum EmailStatus
    {
        Unsent,
        Paused,
        InProgress,
        Sent,
        Failed,
    }
}
