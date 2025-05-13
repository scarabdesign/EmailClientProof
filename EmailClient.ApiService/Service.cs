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
                logger.LogError(ex, Strings.ServiceLogs.GetAllEmailAttemptsFailed, ex.Message);
            }

            return null;
        }

        public async Task<Tuple<string?, int?>?> AddEmailAttempt(EmailAttemptDto emailAttempt)
        {
            try
            {
                if (await emailClientData.CampaignExists(emailAttempt.CampaignId))
                {
                    var emailAttemptModel = EmailAttemptDto.ToEntity(emailAttempt);
                    if (emailAttemptModel != null)
                    {
                        await emailClientData.AddEmailAttempt(emailAttemptModel);
                        await messageService.CampaignUpdated(CampaignDto.ToDto(await emailClientData.GetCampaign(emailAttempt.CampaignId)));
                        StartQueue();
                        return new Tuple<string?, int?>(emailAttempt.Email, emailAttempt.CampaignId);
                    }
                }
                else
                {
                    logger.LogInformation(Strings.ServiceLogs.CampaignDoesNotExist, emailAttempt.CampaignId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.AddEmailAttemptFailed, ex.Message);
            }

            return default;
        }

        public async Task AddEmailAttempts(List<EmailAttemptDto> emails)
        {
            await Task.Yield();
            try
            {
                var campaignId = 0;
                foreach (var emailAttempt in emails)
                {
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
                        logger.LogInformation(Strings.ServiceLogs.CampaignDoesNotExist, emailAttempt.CampaignId);
                    }
                }
                await messageService.CampaignUpdated(CampaignDto.ToDto(await emailClientData.GetCampaign(campaignId)));
                StartQueue();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.AddEmailAttemptFailed, ex.Message);
            }
        }

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            try
            {
                return await emailClientData.RemoveEmailAttempt(id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.RemoveEmailAttemptFailed, ex.Message);
            }

            return null;
        }

        public async Task<List<CampaignDto>?> GetAllCampaigns()
        {
            try
            {
                return CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns() ?? []);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.GetAllCampaignsFailed, ex.Message);
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
                logger.LogError(ex, Strings.ServiceLogs.GetCampaignFailed, ex.Message);
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

                await messageService.CampaignUpdated(CampaignDto.ToDto(await emailClientData.GetCampaign(newId)));
                await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns() ?? []));
                if (newId > 0)
                {
                    return newId;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.AddCampaignFailed, ex.Message);
            }
            return null;
        }

        public async Task<int?> RemoveCampaign(int id)
        {
            try
            {
                await emailClientData.RemoveCampaign(id);
                await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns() ?? []));
                return id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.RemoveCampaignFailed, ex.Message);
            }
            return null;
        }

        public async Task<int?> UpdateCampaign(CampaignDto campaignDto)
        {
            try
            {
                await emailClientData.UpdateCampaign(campaignDto.Id, campaignDto.Name, campaignDto.Subject, campaignDto.Body, campaignDto.Text, campaignDto.Sender, campaignDto.State);
                await messageService.CampaignUpdated(CampaignDto.ToDto(await emailClientData.GetCampaign(campaignDto.Id)));
                await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns() ?? []));
                return campaignDto.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.UpdateCampaignFailed, ex.Message);
            }
            return null;
        }

        public async Task<int?> ToggleCampaignPause(int id)
        {
            try
            {
                var campaign = await emailClientData.GetCampaign(id);
                if (campaign != null)
                {
                    var state = campaign.State == CampaignState.Paused ? CampaignState.Running : CampaignState.Paused;
                    await emailClientData.TogglePauseState(campaign.Id, state);
                    if (state == CampaignState.Running)
                    {
                        await emailClientData.UnpauseAttampts(campaign.Id);
                        StartQueue();
                    }
                    await messageService.CampaignUpdated(CampaignDto.ToDto(campaign));
                    await messageService.CampaignsUpdated(CampaignDto.ToDtoList(await emailClientData.GetAllCampaigns() ?? []));
                    return id;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, Strings.ServiceLogs.UpdateCampaignFailed, ex.Message);
            }
            return null;
        }
    }
}
