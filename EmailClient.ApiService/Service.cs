
using static EmailClient.ApiService.Dto;

namespace EmailClient.ApiService
{
    public class Service(EmailClientData emailClientData, Queue queue, MessageService messageService, ILogger<Service> logger)
    {
        public void StartQueue()
        {
            queue.StartQueue();
        }

        public void StopQueue()
        {
            queue.StopQueue();
        }

        public async Task<List<EmailAttemptDto>?> GetAllEmailAttempts(int campaignId)
        {
            try
            {
                return EmailAttemptDto.ToDtoList(await emailClientData.GetAllEmailAttempts(campaignId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving emailAttempt attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<(string?, int?)> AddEmailAttempt(EmailAttemptDto emailAttempt)
        {
            try
            {
                if (await emailClientData.CampaignExists(emailAttempt.CampaignId))
                {
                    var emailAttemptModel = EmailAttemptDto.ToEntity(emailAttempt);
                    if (emailAttemptModel != null)
                    {
                        await emailClientData.AddEmailAttempt(emailAttemptModel);
                        StartQueue();
                        return (emailAttempt.Email, emailAttempt.CampaignId);
                    }
                }
                else
                {
                    logger.LogInformation("Campain with id: {CampaignId} does not exist", emailAttempt.CampaignId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving emailAttempt attempts: {ErrorMessage}", ex.Message);
            }

            return (null, null);
        }

        public async Task<List<EmailAttemptDto>?> AddEmailAttempts(List<EmailAttemptDto> emails)
        {
            try
            {
                var campaignId = 0;
                foreach (var emailAttempt in emails)
                {
                    if (!await emailClientData.CampaignExists(emailAttempt.CampaignId))
                    {
                        logger.LogError("Campain with id: {campaignId} does not exist", emailAttempt.CampaignId);
                        continue;
                    }

                    campaignId = emailAttempt.CampaignId;
                    if (await emailClientData.CampaignExists(emailAttempt.CampaignId))
                    {
                        var emailAttemptModel = EmailAttemptDto.ToEntity(emailAttempt);
                        if (emailAttemptModel != null)
                        {
                            await emailClientData.AddEmailAttempt(emailAttemptModel);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Campain with id: {CampaignId} does not exist", emailAttempt.CampaignId);
                    }
                }

                StartQueue();
                return await GetAllEmailAttempts(campaignId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving emailAttempt attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            try
            {
                return await emailClientData.RemoveEmailAttempt(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving emailAttempt attempts: {ErrorMessage}", ex.Message);
            }

            return null;
        }

        public async Task<List<CampaignDto>?> GetAllCampaigns()
        {
            try
            {
                return CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving campaigns: {ErrorMessage}", ex.Message);
            }
            return null;
        }

        public async Task<CampaignDto?> GetCampaign(int id)
        {
            try
            {
                return CampaignDto.ToDto(await emailClientData.GetCampaign(id));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while retrieving campaigns: {ErrorMessage}", ex.Message);
            }
            return null;
        }

        public async Task<int?> AddCampaign(CampaignDto campaign)
        {
            try
            {
                var newId = 0;
                var campaignModel = CampaignDto.ToEntity(campaign);
                if (campaignModel != null)
                {
                    newId = await emailClientData.AddCampaign(campaignModel);
                }

                await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns()));


                var newCampaign = await emailClientData.GetCampaign(newId);
                var newDto = CampaignDto.ToDto(newCampaign);
                await messageService.AttemptsUpdated(newDto);
                if (newId > 0)
                {
                    return newId;
                }
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

        public async Task<int?> UpdateCampaign(int id, string? name, string? subject, string? body, string? sender)
        {
            try
            {
                await emailClientData.UpdateCampaign(id, name, subject, body, sender);
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
