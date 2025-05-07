using Microsoft.VisualBasic;

namespace EmailClient.ApiService
{
    public static class Strings
    {
        public static class CampaignValidation
        {
            public const string CampaignNameError = "Campaign title is required.";
            public const string CampaignSubjectError = "Campaign Email subject is required.";
            public const string CampaignSenderError = "Campaign Sender Email is required.";
            public const string CampaignInvalidEmailError = "Invalid Sender email format.";
            public const string CampaignBodyError = "Campaign Email body is required.";
        }

        public static class SocketSubscriptions
        {
            public const string CampaignUpdated = "CampaignUpdated";
            public const string CampaignsUpdated = "CampaignsUpdated";
        }

        public static class QueueConfig
        {
            public const string MaxAttempts = "MaxAttempts";
            public const string SecondsBetweenLoops = "SecondsBetweenLoops";
            public const string SecondsBetweenErrors = "SecondsBetweenErrors";
        }

        public static class QueueLogInfo
        {
            public const string QueueRunning = "Queue is already running.";
            public const string QueueStarted = "Queue started.";
            public const string QueueNotRunning = "Queue is not running.";
            public const string QueueStopped = "Queue stopped.";
            public const string QueueProcessingTime = "Queue processed at {Time}";
            public const string RecipientNotAccepted = "Recipient Not Accepted";
            public const string SenderNotAccepted = "Sender Not Accepted";
            public const string EmailClientFallBack = "Email client user not present in config. Falling back to locally hosted solution";
            public const string NoMoreEmails = "No email attempts to process. Stopping queue.";
            public const string CampaignNotFound = "Campaign not found";
            public const string ConnectionProblem = "Problem creating connection: {Message}";
            public const string ClientIsNull = "SMTP client is null. Skipping email attempt: {Email}";
            public const string SendingFailed = "Failed to send email to {Email}: {ErrorMessage}";
            public const string QueueFailureError = "An error occurred while running the queue: {ErrorMessage}";
        }

        public static class RouteNames
        {
            public const string StartQueue = "startQueue";
            public const string StopQueue = "stopQueue";
            public const string GetAllAttempts = "getAllAttempts";
            public const string AddAttempt = "addAttempt";
            public const string AddAttempts = "addAttempts";
            public const string RemoveAttempt = "removeAttempt";
            public const string GetAllCampaigns = "getAllCampaigns";
            public const string GetCampaign = "getCampaign";
            public const string AddCampaign = "addCampaign";
            public const string RemoveCampaign = "removeCampaign";
            public const string UpdateCampaign = "updateCampaign";
            public const string ToggleCampaignPause = "toggleCampaignPause";
        }

        public static class RouteResponses
        {
            public const string Ok = "ok";
            public const string AddingAttemptFailed = "Adding attempt failed.";
            public const string AddingAttemptsFailed = "Adding attempts failed.";
            public const string RemovingAttemptFailed = "Removing attempt failed.";
            public const string AddingCampaignFailed = "Adding campaign failed.";
            public const string UpdatingCampaignFailed = "Updating campaign failed.";
        }

        public static class ServiceLogs
        {
            public const string GetAllEmailAttemptsFailed = "An error occurred while retrieving attempts: {ErrorMessage}";
            public const string CampaignDoesNotExist = "Campaign with id: {CampaignId} does not exist.";
            public const string AddEmailAttemptFailed = "An error occurred while adding attempt: {ErrorMessage}";
            public const string RemoveEmailAttemptFailed = "An error occurred while removing attempt: {ErrorMessage}";
            public const string GetAllCampaignsFailed = "An error occurred while retrieving all campaigns: {ErrorMessage}";
            public const string GetCampaignFailed = "An error occurred while retrieving campaign: {ErrorMessage}";
            public const string AddCampaignFailed = "An error occurred while adding campaign: {ErrorMessage}";
            public const string RemoveCampaignFailed = "An error occurred while removing campaign: {ErrorMessage}";
            public const string UpdateCampaignFailed = "An error occurred while updating campaign: {ErrorMessage}";
        }
    }
}
