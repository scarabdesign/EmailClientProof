# Email Client Proof of Concept

This is a project written in ~4 days with .NET Aspire with .NET 9. Aspire is designed to be readily cloud-deployable and uses a 
micorservice type architecture, utilizing Docker and k8 if needed. To run this project you will need:

- .Net 9
- Docker

When built and launched, the project will create new docker containers for PostgreSQL and the local Mail Server implimentation. The application
uses the MailKit library to send emails. It has a local email server that can be used to test sending emails without actually sending them. To send
using a real SMTP server, you will need supply credentials int the `appsettings.json`. More on that below.

The project send POST, GET and DELETE requests from the Web service to the API service. It also uses a Signalr connection to get
real-time updates back the the Web.

## Getting Started

Make sure the Docker daemon is running. Clone this repo into a new directory and open a command line prompt in that directory. Run the following command to build the project:
```bash
dotnet build
```
Then cd into the EmailClient.AppHost directory and run the following command to start the project:
```bash
dotnet run
```
When it starts, open a browser window and go to the following URL:
```bash
http://localhost:5268
```
I've tried to make the application as intuitive as possible, but below would be a basic workflow.
1. Click to create a new Campaign.
    - This will create a new Email Campaign and open the Campaign editor.
2. Fill in all the required fields and click the "Save Campaign" button.
	- This will save the Campaign and allow you to put in recipients.
3. Type in one or more recipient email addresses seperated by comma or space. Click the "Add" button.
	- This will add the recipients to the list of email attemps at the bottom
    - The list will update itself to show the status of the recipientattemps at the bottom
    - The list will update itself to show the status of the recipient the emails are processed.

To get a glimps of what's going on under the hood, look at the running command prompt and open the link that says:
```bash
> Log into the dashboard at http://localhost:XXXX?...
```
When it loads you will see a list of all the running services. To see emails sent locally, 
click on the URL for the `maildev` service. This will open a new tab with the local mail server.

## Sending to other email servers
To set up the application to send emails to a real SMTP server, you will need to edit the `appsettings.json` 
file in the EmailClient.ApiService directory. Under `ExternalEmailHosts` there's listed an example entry for `sampleemail@gmail.com`.
When using email addresses listed here as the `Sender` of a campain, the application will use the the information listed here to 
attempt to make a connection to that SMTP server.

To send an email through Gmail's SMTP server, you will need to set up 2fa authentication and create an app password.

1. Visit the Google Account settings.
2. Select “Security” from the left-hand side.
3. Under the “Signing in to Google” section, click on “2-Step Verification” and follow the prompts to enable it.
4. After enabling two-step verification, search for “App Passwords”. You may be asked to re-enter your password.
5. In the App passwords section, supply a name for the app and clicl on “Create”.
6. Google will generate a new 16-character password for you. Copy and paste that into the `appsettings.json` file.

## Using Postman

Included in the root of the project folder is a file called `EmailProof.postman_collection.json`. In Postman, click the `Import` 
button and drop that file into it.

- Once they are imported, find the entry called `addCampaign`. Click the `Body` tab to examine the payload and make changes 
if desired. Click `Send`. What's returned should be an `ok` response and an `id` of the newly created campaign:
```JSON
{
    "result": "ok",
    "id": 1
}
```

- Next, go to `getAllCampains` and click `Send`. Admire the list.

- Now, go to `addAttempt` and examine the body there, making changes as you see fit. Be sure the use the `campaignId` of a valid 
campain in the lists on the previous step. Click the `Send` button:
```JSON
{
    "email": "sampleRecipient@emailme.com",
    "campaignId": 1
}
```

- Open the `getAllAttempts` entry. The URL will contain an `id` property which should be a valid `campaignId`. Click `Send` to get a list of all the emails added to the campaign and the status of it's processing. For reference:
```C#
public enum EmailStatus
{
    Unsent = 0,
    InProgress = 1,
    Sent = 2,
    Failed = 3,
}
```

## Using Command Line

When the application is running, all requests can be sent with cURL requests. 

### Adding a campaign
GitCLI:
```bash
curl.exe --data-binary '{"name":"My Email Campaign from command line","subject":"This is a CURL sent email","body":"This email was typed up in a CLI to test the <b>AWESOME</b> email client","sender":"noreplay@emailproof.com"}' -H 'content-type: application/json;' http://localhost:5403/addCampaign

{"result":"ok","id":2}
```
Powershell:
```Powershell
$body = ConvertTo-Json @{
    name = "My Email Campaign from command line"
    subject = "This is a CURL sent email"
    body = "This email was typed up in a CLI to test the <b>AWESOME</b> email client"
    sender = "noreplay@emailproof.com"
}; Invoke-RestMethod -Method Post -Uri http://localhost:5403/addCampaign -Headers @{"Content-Type" = "application/json"} -Body $body

result id
------ --
ok      2
```

### Adding an email
GitCLI:
```bash
curl.exe --data-binary '{"email": "sampleRecipient@emailme.com", "campaignId": 2}' -H 'content-type: application/json;' http://localhost:5403/addAttempt

{"result":"ok","email":"sampleRecipient@emailme.com","campaignId":2}
```
Powershell:
```Powershell
$body = ConvertTo-Json @{
    email = "sampleRecipient@emailme.com"
    campaignId = 2
}; Invoke-RestMethod -Method Post -Uri http://localhost:5403/addAttempt -Headers @{"Content-Type" = "application/json"} -Body $body

result email                       campaignId
------ -----                       ----------
ok     sampleRecipient@emailme.com          2
```

### Viewing campaign status
GitCLI:
```bash
curl.exe http://localhost:5403/getAllAttempts?id=2

[{"id":1,"email":"sampleRecipient@emailme.com","status":2,"attempts":1,"result":null,"errorCode":-1,"created":"2025-05-05T14:48:47.105556-07:00","lastAttempt":"2025-05-05T14:48:52.562336-07:00","campaignId":2},{"id":2,"email":"sampleRecipient@emailme.com","status":2,"attempts":1,"result":null,"errorCode":-1,"created":"2025-05-05T14:49:46.421406-07:00","lastAttempt":"2025-05-05T14:49:51.721792-07:00","campaignId":2}]
```
Powershell:
```Powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5403/getAllAttempts?id=2

id          : 1
email       : sampleRecipient@emailme.com
status      : 2
attempts    : 1
result      :
errorCode   : -1
created     : 2025-05-05T14:48:47.105556-07:00
lastAttempt : 2025-05-05T14:48:52.562336-07:00
campaignId  : 2

id          : 2
email       : sampleRecipient@emailme.com
status      : 2
attempts    : 1
result      :
errorCode   : -1
created     : 2025-05-05T14:49:46.421406-07:00
lastAttempt : 2025-05-05T14:49:51.721792-07:00
campaignId  : 2
```

## TODO

- [ ] Move MaxAttempts to config file
- [ ] Move TinyMCE APi key to config
- [ ] Feedback on running queue
- [ ] Feedback on failed