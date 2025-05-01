using Microsoft.EntityFrameworkCore;

namespace EmailClient.ApiService
{
    public class EmailClientDbContext(DbContextOptions<EmailClientDbContext> options) : DbContext(options)
    {
        public DbSet<EmailAttempt> EmailAttempts { get; set; }
    }

    public class EmailAttempt
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public EmailStatus Status { get; set; }
        public int Attempts { get; set; }
        public string? Result { get; set; }
        public int ErrorCode { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastAttempt { get; set; }
    }

    public enum EmailStatus
    {
        Unsent,
        InProgress,
        Sent,
        Failed,
    }
}
