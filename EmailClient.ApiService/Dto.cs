
using System.ComponentModel.DataAnnotations;

namespace EmailClient.ApiService
{
    public static class Dto
    {
        public class EmailAttemptDto
        {
            public int Id { get; set; }
            public string? Email { get; set; }
            public EmailStatus? Status { get; set; }
            public int Attempts { get; set; }
            public string? Result { get; set; }
            public int ErrorCode { get; set; } = -1;
            public DateTime Created { get; set; } = DateTime.UtcNow;
            public DateTime? LastAttempt { get; set; }
            public required int CampaignId { get; set; }

            public static EmailAttemptDto ToDto(EmailAttempt emailAttempt)
            {
                return new EmailAttemptDto
                {
                    Id = emailAttempt.Id,
                    Email = emailAttempt.Email,
                    Status = emailAttempt.Status,
                    Attempts = emailAttempt.Attempts,
                    Result = emailAttempt.Result,
                    ErrorCode = emailAttempt.ErrorCode,
                    Created = DateTime.SpecifyKind(emailAttempt.Created, DateTimeKind.Local),
                    LastAttempt = emailAttempt.LastAttempt != null ? DateTime.SpecifyKind(emailAttempt.LastAttempt ?? DateTime.UtcNow, DateTimeKind.Local) : null,
                    CampaignId = emailAttempt.CampaignId
                };
            }

            public static List<EmailAttemptDto>? ToDtoList(List<EmailAttempt>? emailAttempts)
            {
                if (emailAttempts == null) return null;
                return [.. emailAttempts.Select(e => ToDto(e))];
            }

            public static EmailAttempt? ToEntity(EmailAttemptDto? emailAttemptDto)
            {
                if (emailAttemptDto == null) return null;
                if (emailAttemptDto.Email == null || emailAttemptDto.CampaignId == 0)
                    return null;
                return new EmailAttempt
                {
                    Id = emailAttemptDto.Id,
                    Email = emailAttemptDto.Email,
                    Status = emailAttemptDto.Status ?? EmailStatus.Unsent,
                    Attempts = emailAttemptDto.Attempts,
                    Result = emailAttemptDto.Result,
                    ErrorCode = emailAttemptDto.ErrorCode,
                    Created = DateTime.SpecifyKind(emailAttemptDto.Created, DateTimeKind.Utc),
                    LastAttempt = emailAttemptDto.LastAttempt != null ? DateTime.SpecifyKind(emailAttemptDto.LastAttempt ?? DateTime.UtcNow, DateTimeKind.Utc) : null,
                    CampaignId = emailAttemptDto.CampaignId
                };
            }
        }

        public class CampaignDto
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Campaign title is required.")]
            public required string Name { get; set; }

            [Required(ErrorMessage = "Campaign Email subject is required.")]
            public required string Subject { get; set; }

            [Required(ErrorMessage = "Campaign Email body is required.")]
            public required string Body { get; set; }

            [Required(ErrorMessage = "Sender Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid Sender email format.")]
            public required string Sender { get; set; }
            public DateTime Created { get; set; } = DateTime.Now;
            public DateTime Updated { get; set; } = DateTime.Now;
            public int EmailCount { get; set; } = 0;
            public List<EmailAttemptDto> EmailAttempts { get; set; } = [];
            public static CampaignDto? ToDto(Campaign? campaign, bool? includeAttempts = true)
            {
                if (campaign == null) return null;
                return new CampaignDto
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    Subject = campaign.Subject,
                    Body = campaign.Body,
                    Sender = campaign.Sender,
                    Created = DateTime.SpecifyKind(campaign.Created, DateTimeKind.Local),
                    Updated = DateTime.SpecifyKind(campaign.Updated, DateTimeKind.Local),
                    EmailCount = campaign.EmailAttempts.Count,
                    EmailAttempts = includeAttempts == true ? [.. campaign.EmailAttempts.Select(EmailAttemptDto.ToDto)] : [],
                };
            }

            public static List<CampaignDto> ToDtoList(List<Campaign> campaigns)
            {
                return [.. campaigns.Select(c => ToDto(c, false))];
            }

            public static Campaign? ToEntity(CampaignDto? campaignDto)
            {
                if (campaignDto == null) return null;
                if (campaignDto.Name == null || campaignDto.Subject == null || campaignDto.Body == null || campaignDto.Sender == null)
                    return null;
                return new Campaign
                {
                    Name = campaignDto.Name,
                    Subject = campaignDto.Subject,
                    Body = campaignDto.Body,
                    Sender = campaignDto.Sender,
                    Created = DateTime.SpecifyKind(campaignDto.Created, DateTimeKind.Utc),
                    Updated = DateTime.SpecifyKind(campaignDto.Updated, DateTimeKind.Utc),
                };
            }
        }

        public class StatusDto 
        { 
            public DateTime Updated { get; set; } = DateTime.Now;
            public CampaignDto? CurrentlyViewing { get; set; }
            public List<CampaignDto> Campaigns { get; set; } = [];
        }
    }
}
