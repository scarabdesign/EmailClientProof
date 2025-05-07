using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EmailClient.ApiService
{
    public class EmailClientData(ContextQueue contextQueue)
    {
        public async Task<List<EmailAttempt>?> GetAllEmailAttempts(int campaignId) => 
            await contextQueue.Query(async db => await db.EmailAttempts.Where(e => e.CampaignId == campaignId).AsTracking(QueryTrackingBehavior.NoTracking).ToListAsync());

        public async Task AddEmailAttempt(EmailAttempt emailAttempt) {
            await contextQueue.Query(async db =>
            {
                db.EmailAttempts.Add(emailAttempt);
                await db.SaveChangesAsync();
                return null;
            });
        }

        public async Task<string?> RemoveEmailAttempt(int id)
        {
            return await contextQueue.Query(async db =>
            {
                var targetAttempt = db.EmailAttempts.FirstOrDefault(a => a.Id == id);
                if (targetAttempt == null) return null;
                db.EmailAttempts.Remove(targetAttempt);
                await db.SaveChangesAsync();
                return targetAttempt.Email;
            });
        }

        public async Task<List<Campaign>?> GetAllCampaigns() =>
            await contextQueue.Query(async db => await db.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().ToListAsync());

        public async Task<Campaign?> GetCampaign(int campaignId) =>
            await contextQueue.Query(async db => await db.Campaigns.Include(c => c.EmailAttempts).AsNoTracking().FirstOrDefaultAsync(c => c.Id == campaignId));

        public async Task<bool> CampaignExists(int id) =>
            await contextQueue.Query(async db => await db.Campaigns.AsNoTracking().AnyAsync(c => c.Id == id));

        public async Task<int> AddCampaign(Campaign campaign)
        {
            return await contextQueue.Query(async db =>
            {
                db.Campaigns.Add(campaign);
                await db.SaveChangesAsync();
                return campaign.Id;
            });
        }

        public async Task RemoveCampaign(int id)
        {
            await contextQueue.Query(async db =>
            {
                var targetCampaign = db.Campaigns.FirstOrDefault(c => c.Id == id);
                if (targetCampaign == null) return null;
                db.Campaigns.Remove(targetCampaign);
                await db.SaveChangesAsync();
                return null;
            });
        }

        public async Task UnpauseAttampts(int id)
        {
            await contextQueue.Query(async db =>
            {
                var targetCampaign = db.Campaigns.Include(c => c.EmailAttempts).FirstOrDefault(c => c.Id == id);
                if (targetCampaign == null) return null;
                targetCampaign.EmailAttempts.Where(e => e.Status == EmailStatus.Paused).ToList().ForEach(e =>
                {
                    e.Attempts = 0;
                    e.Result = default;
                    e.ErrorCode = default;
                    e.Status = EmailStatus.Unsent;
                    e.MessageId = default;
                });
                await db.SaveChangesAsync();
                return null;
            });
        }

        public async Task UpdateCampaign(int id, string? name, string? subject, string? body, string? sender, CampaignState? state)
        {
            await contextQueue.Query(async db =>
            {
                var targetCampaign = db.Campaigns.FirstOrDefault(c => c.Id == id);
                if (targetCampaign == null) return null;
                targetCampaign.Name = name ?? targetCampaign.Name;
                targetCampaign.Subject = subject ?? targetCampaign.Subject;
                targetCampaign.Sender = sender ?? targetCampaign.Sender;
                targetCampaign.Body = body ?? targetCampaign.Body;
                targetCampaign.Text = Regex.Replace(targetCampaign.Body, "<[^>]*?>", " ").Replace("  ", " ");
                targetCampaign.State = state ?? targetCampaign.State;
                targetCampaign.Updated = DateTime.UtcNow;
                db.Campaigns.Update(targetCampaign);
                await db.SaveChangesAsync();
                return null;
            });
        }
    }
}
