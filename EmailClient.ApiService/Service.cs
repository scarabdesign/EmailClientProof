
namespace EmailClient.ApiService
{
    public class Service(EmailClientData emailClientData, ILogger<Service> logger)
    {
        public async Task<List<EmailAttempt>?> GetAllEmailAttempts()
        {
            try
            {
                return await emailClientData.GetAllEmailAttempts();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving email attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<(string?, int?)> AddEmailAttempt(EmailAttempt emailAttempt)
        {
            try
            {
                if (await emailClientData.CampaignExists(emailAttempt.CampaignId))
                {
                    await emailClientData.AddEmailAttempt(emailAttempt);
                    return (emailAttempt.Email, emailAttempt.CampaignId);
                }
                else
                {
                    logger.LogInformation($"Campain with id: {emailAttempt.CampaignId} does not exist");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving email attempts: {ErrorMessage}", ex.Message);
            }

            return (null, null);
        }

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            try
            {
                return await emailClientData.RemoveEmailAttempt(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving email attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<List<Campaign>?> GetAllCampaigns()
        {
            try
            {
                return await emailClientData.GetAllCampaigns();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving campaigns: {ErrorMessage}", ex.Message);
            }
            return null;
        }

        public async Task<int?> AddCampaign(Campaign campaign)
        {
            try
            {
                return await emailClientData.AddCampaign(campaign);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving campaigns: {ErrorMessage}", ex.Message);
            }
            return null;
        }

        public async Task<int?> RemoveCampaign(int id)
        {
            try
            {
                await emailClientData.RemoveCampaign(id);
                return id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while removing campaign: {ErrorMessage}", ex.Message);
            }
            return null;
        }

        public async Task<int?> UpdateCampaign(int id, CampaignStatus? status, string? name, string? subject, string? body, string? sender)
        {
            try
            {
                await emailClientData.UpdateCampaign(id, status, name, subject, body, sender);
                return id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while updating campaign: {ErrorMessage}", ex.Message);
            }
            return null;
        }
    }
}
