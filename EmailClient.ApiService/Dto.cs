using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

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
            public DateTime Created { get; set; } = DateTime.Now;
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
                    Created = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(emailAttempt.Created, DateTimeKind.Utc), TimeZoneInfo.Local),
                    LastAttempt = emailAttempt.LastAttempt != null ? 
                        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(emailAttempt.LastAttempt ?? DateTime.UtcNow, DateTimeKind.Utc), TimeZoneInfo.Local) : 
                        null,
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
                    Created = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(emailAttemptDto.Created, DateTimeKind.Local), TimeZoneInfo.Local),
                    LastAttempt = emailAttemptDto.LastAttempt != null ? 
                        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(emailAttemptDto.LastAttempt ?? DateTime.Now, DateTimeKind.Local), TimeZoneInfo.Local) :
                        null,
                    CampaignId = emailAttemptDto.CampaignId
                };
            }
        }

        public class CampaignDto
        {
            public int Id { get; set; }
            [Required(AllowEmptyStrings = false, ErrorMessage = "Campaign title is required.")]
            public required string Name { get; set; }
            [Required(AllowEmptyStrings = false, ErrorMessage = "Campaign Email subject is required.")]
            public required string Subject { get; set; }
            [Required(AllowEmptyStrings = false, ErrorMessage = "Campaign Sender Enmail is required.")]
            [EmailAddress(ErrorMessage = "Invalid Sender email format.")]
            public required string Sender { get; set; }
            [Required(AllowEmptyStrings = false, ErrorMessage = "Campaign Email body is required.")]
            public required string Body { get; set; }
            public string? Text { get; set; }
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
                    Sender = campaign.Sender,
                    Body = campaign.Body,
                    Text = campaign.Text,
                    Created = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(campaign.Created, DateTimeKind.Utc), TimeZoneInfo.Local),
                    Updated = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(campaign.Updated, DateTimeKind.Utc), TimeZoneInfo.Local),
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
                    Sender = campaignDto.Sender,
                    Body = campaignDto.Body,
                    Text = campaignDto.Text ?? Regex.Replace(campaignDto.Body, "<[^>]*?>", " ").Replace("  ", " "),
                    Created = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(campaignDto.Created, DateTimeKind.Local), TimeZoneInfo.Local),
                    Updated = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(campaignDto.Updated, DateTimeKind.Local), TimeZoneInfo.Local),
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
