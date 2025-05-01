using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmailClient.ApiService
{
    public class EmailClientDbContext(DbContextOptions<EmailClientDbContext> options) : DbContext(options)
    {
        public DbSet<EmailAttempt> EmailAttempts { get; set; }
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
    }

    public enum EmailStatus
    {
        Unsent,
        InProgress,
        Sent,
        Failed,
    }
}
