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
                logger.LogError(ex, "An error occurred while retrieving email attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<bool> AddEmailAttempt(EmailAttempt emailAttempt)
        {
            try
            {
                dbContext.EmailAttempts.Add(emailAttempt);
                await dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while adding email attempt: {ErrorMessage}", ex.Message);
            }

            return false;
        }
    }
}
