using Microsoft.EntityFrameworkCore;
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

    public class QueueContext(DbContextOptions<QueueContext> options) : DbContext(options) 
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
        public required string Body { get; set; }
        public required string Sender { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Updated { get; set; } = DateTime.UtcNow;
        public List<EmailAttempt> EmailAttempts { get; set; } = new List<EmailAttempt>();
    }

    public enum EmailStatus
    {
        Unsent,
        InProgress,
        Sent,
        Failed,
    }
}
