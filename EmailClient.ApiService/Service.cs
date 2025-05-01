using Microsoft.EntityFrameworkCore;

namespace EmailClient.ApiService
{
    public class Service(EmailClientDbContext dbContext, ILogger<Service> logger)
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

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            try
            {
                var targetAttempt = dbContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
                if (targetAttempt != null)
                {
                    dbContext.EmailAttempts.Remove(targetAttempt);
                    await dbContext.SaveChangesAsync();
                    return targetAttempt.Email;
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while removing email attempt: {ErrorMessage}", ex.Message);
            }

            return null;
        }
    }
}
