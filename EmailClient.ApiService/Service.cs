using Microsoft.EntityFrameworkCore;

namespace EmailClient.ApiService
{
    public class Service(EmailClientDbContext dbContext, ILogger<EmailClientDbContext> logger)
    {
        public async Task<List<EmailAttempt>?> GetAllEmailAttempts()
        {
            try
            {
                return await dbContext.EmailAttempts.ToListAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, null);
            }

            return null;
        }
    }
}
