using Microsoft.EntityFrameworkCore;

namespace EmailClient.ApiService
{
    public class EmailClientData(EmailClientDbContext dbContext)
    {
        public async Task<List<EmailAttempt>?> GetAllEmailAttempts() => 
            await dbContext.EmailAttempts.AsTracking(QueryTrackingBehavior.NoTracking).ToListAsync();

        public async Task AddEmailAttempt(EmailAttempt emailAttempt) {
            dbContext.EmailAttempts.Add(emailAttempt);
            await dbContext.SaveChangesAsync();
        }

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            var targetAttempt = dbContext.EmailAttempts.FirstOrDefault(a => a.Id == id);
            if (targetAttempt == null) return null;
            dbContext.EmailAttempts.Remove(targetAttempt);
            await dbContext.SaveChangesAsync();
            return targetAttempt.Email;
        }

        public async Task<List<Campaign>> GetAllCampaigns() =>
            await dbContext.Campaigns.AsNoTracking().ToListAsync();

        public async Task<bool> CampaignExists(int id) =>
            await dbContext.Campaigns.AsNoTracking().AnyAsync(c => c.Id == id);

        public async Task<int> AddCampaign(Campaign campaign)
        {
            dbContext.Campaigns.Add(campaign);
            await dbContext.SaveChangesAsync();
            return campaign.Id;
        }

        public async Task RemoveCampaign(int id)
        {
            var targetCampaign = dbContext.Campaigns.FirstOrDefault(c => c.Id == id);
            if (targetCampaign == null) return;
            dbContext.Campaigns.Remove(targetCampaign);
            await dbContext.SaveChangesAsync();
        }

        public async Task UpdateCampaign(int id, string? name, string? subject, string? body, string? sender)
        {
            var targetCampaign = dbContext.Campaigns.FirstOrDefault(c => c.Id == id);
            if (targetCampaign == null) return;
            targetCampaign.Name = name ?? targetCampaign.Name;
            targetCampaign.Subject = subject ?? targetCampaign.Subject;
            targetCampaign.Body = body ?? targetCampaign.Body;
            targetCampaign.Sender = sender ?? targetCampaign.Sender;
            targetCampaign.Updated = DateTime.UtcNow;
            dbContext.Campaigns.Update(targetCampaign);
            await dbContext.SaveChangesAsync();
        }
    }
}
