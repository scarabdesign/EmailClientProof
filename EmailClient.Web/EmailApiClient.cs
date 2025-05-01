namespace EmailClient.Web;

public class EmailApiClient(HttpClient httpClient)
{
    public async Task<List<EmailAttempt>> GetAllAttempts(CancellationToken cancellationToken = default)
    {
        List<EmailAttempt>? attempts = null;

        await foreach (var attempt in httpClient.GetFromJsonAsAsyncEnumerable<EmailAttempt>("/getAllAttempts", cancellationToken))
        {
            if (attempt is not null)
            {
                attempts ??= [];
                attempts.Add(attempt);
            }
            ;
        }

        return attempts ?? [];
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
        Failed
    }
}