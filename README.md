# Email Client Proof of Concept

This is a project written in ~4 days in .NET 9 and .NET Aspire. Aspire is designed to be readily 
cloud-deployable and uses a microservice type architecture, utilizing containerization. To run this 
project you will need:

- .Net 9
- Docker

### Notes about the project

When built and launched, the project will create new docker containers for PostgreSQL and the local 
Mail Server implementation. The application uses the MailKit library to send emails. It has a local 
email server that can be used to test sending emails without actually sending them. To send using a 
real SMTP server, you will need to supply credentials in the `appsettings.json`. More on that below.

The project sends POST, GET and DELETE requests from the Web service to the API service. It also uses
a SignalR connection to get real-time updates back to the Web.

As it's configured, the PostgreSQL database is not persistent between application restarts to make 
for easier development and skirt the necessity of running database migrations when I made changes. 
However, in the `appsettings.json` of the `EmailClient.AppHost` project is a section called `PgPref` 
with a preference called `DataVolume` which when made `true` will create an external volume that will 
persist across restarts. There are also other preferences there that will spin up separate containers 
for the `PgWeb` and `PgAdmin` web interfaces.

## Getting Started

Make sure the Docker daemon is running. Clone this repository into a new directory and open a command 
line prompt in that directory. Run the following command to build the project:
```bash
dotnet build
```
Then `cd` into the `EmailClient.AppHost` directory and run the following command to start the project:
```bash
dotnet run
```
When it starts, open a browser window and go to the following URL:
```bash
http://localhost:5268
```
I've tried to make the application as intuitive as possible, but below would be the basic work-flow.
1. Click to create a new Campaign.
    - This will open the Campaign editor modal.
2. Fill in all the required fields and click the "Save Campaign" button.
	- This will save the Campaign and allow you to put in recipients.
3. Type in one or more recipient email addresses separated by comma or space. Click the "Add" button.
	- This will add the recipients to the list of email attempts at the bottom
    - The list will automatically update as it gets status updates from the API.

Campaigns are running by default. New email addresses put into the campaign will immediately start processing
unless the `Pause` button is toggled. 

To get a glimpse of what's going on under the hood, look at the running command prompt and open the link 
that says:
```bash
> Log into the dashboard at http://localhost:XXXX?...
```
When it loads you will see a list of all the running services. To see emails sent locally, 
click on the URL for the `maildev` service. This will open a new tab with the local mail server interface.

## Sending to other SMTP servers
To set up the application to send emails to a real SMTP server, you will need to edit the 
`appsettings.json` file in the `EmailClient.ApiService` directory. Under `ExternalEmailHosts` there's 
an example entry for `sampleemail@gmail.com`. When using email addresses listed here as the `Sender` of a 
campaign, the application will use the information listed here to attempt to make a connection to that SMTP 
server.

To send an email through Gmail's SMTP server, you will need to set up 2fa authentication and create an app 
password:

1. Visit the Google Account settings.
2. Select "Security" from the left-hand side.
3. Under the "Signing in to Google" section, click on "2-Step Verification" and follow the prompts to enable it.
4. After enabling two-step verification, search for "App Passwords". You may be asked to re-enter your password.
5. In the App passwords section, supply a name for the app and click on "Create".
6. Google will generate a new 16-character password for you.
7. Add a new section in the `appsettings.json` as the email you wish to use.
8. Paste the App password in for `pass`.
9. Type in `smtp.gmail.com` for `host`.
10. Make the port `587`.
11. Restart and test the application. Note: Email will most certainly go to the Spam folder

## Testing Failures

The API will attempt to send the emails up to the default of 3 times (settable in `EmailClient.ApiService.appsettings.json` as `MaxAttempts`) and
the failed status can be found in the Campaign editor window in the email list. 

Failures are detected when:
- The user name can't authenticate against the given server
- The sender email is not accepted
- The recipient email is not accepted

As of this publishing, it's not possible to send with a different email address than the one used in the authentication attempt.
I don't know how to trigger a recipient rejection at this time either, so the only testable failure would be authentication.
To set this test up, use fake credentials in the `appsettings.json` (or just use the existing sample email `sampleemail@gmail.com`). 

## Using Postman

Included in the root of the project folder is a file called `EmailProof.postman_collection.json`. In 
Postman, click the `Import` button and drop that file into it.

- Once they are imported, find the entry called `addCampaign`. Click the `Body` tab to examine the payload 
and make changes if desired. Click `Send`. What's returned should be an `ok` response and an `id` of the 
newly created campaign:
```JSON
{
    "result": "ok",
    "id": 1
}
```
If the web front end page is open, you should see the new campaign automatically appear in the list.

- Next, go to `getAllCampains` and click `Send`. Admire the list.

- Now, go to `addAttempt` and examine the body there, making changes as you see fit. Be sure the use the 
`campaignId` of a valid campaign in the lists on the previous step. Click the `Send` button. Your response
should resemble:
```JSON
{
    "email": "sampleRecipient@emailme.com",
    "campaignId": 1
}
```
If the web front end page is open and the campaign editor for your target campaign is showing, you 
should see the new email attempt automatically appear in the list.

- Open the `getAllAttempts` entry. The URL will contain an `id` property which should be a valid 
`campaignId`. Click `Send` to get a list of all the emails added to the campaign and the status of 
it's processing. For reference:
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

When the application is running, all requests can be sent with cURL requests. Any request in the 
Postman list or in the API routes can be used. Below are some examples.

### Adding a campaign
Linux Terminal:
```bash
curl --data-binary '{"name":"My Email Campaign from command line","subject":"This is a CURL sent email","body":"This email was typed up in a CLI to test the <b>AWESOME</b> email client","sender":"noreplay@emailproof.com"}' -H 'content-type: application/json;' http://localhost:5403/addCampaign

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
Linux Terminal:
```bash
curl --data-binary '{"email": "sampleRecipient@emailme.com", "campaignId": 2}' -H 'content-type: application/json;' http://localhost:5403/addAttempt

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
Linux Terminal:
```bash
curl http://localhost:5403/getAllAttempts?id=2

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

### Using Command Line without running the app

If you just want to use the mail client to send an email without the app running, you can run the mailing executable with all the required parameters. 
This will require you to authenticate against the SMTP server you are using 
(Gmail, for example), as the local mail host won't be running.

CD to the directory `EmailClient.Emailing` and run the following command:

```bash
./EmailClient.Mailing.exe [from/username] [password] [host] [port] [to] [subject] [body]
```
For example, to send an email using Gmail's SMTP server:
```bash
./EmailClient.Mailing.exe "myAddress@gmail.com" "XXXX XXXX XXXX" "smtp.gmail.com" 587 "toAddress@gmail.com" "This is the subject" "This is the body"
```

Running this command will send the email and exit. To see the command line output, you can run the command without any arguments.


## Possible Future Improvements
### Best practices
- [ ] Add interfaces for everything!
- [ ] Build up the test suite
- [ ] Add Swagger
- [ ] Enable DB migrations
- [ ] Separate models from dbContext
- [ ] Use `record` for DTOs instead of classes
### Refactoring
- [ ] Combine socket responses to send fewer updates
- [ ] Don't reference classes in web service directly from API service.
- [ ] Use a different rich/HTML text editor or update TinyMCE (paid) and enable HTML editing
### Scalability
- [ ] Add AMQP
- [ ] Add to K8s cluster
- [ ] Deploy to cloud
### Interface improvements
- [ ] Bring more details of errors to the front-end
- [ ] Put email status list in separate interface
- [ ] Make navigable with breadcrumbs; remove large modal
- [ ] Make tables searchable and filterable
- [ ] Allow mass changing of list items (delete, pause, reset)
- [ ] Use more icons
### Marketability
- [ ] Give it a catchy name
- [ ] Spice up the layout
- [ ] Create a logo/favicon
### Nice to haves
- [ ] Supply names with email addresses, maybe other contact details
- [ ] Use more contact details for more personalization
- [ ] Create email bodies as templates that can be saved 
- [ ] Savable email lists that can applied to campaigns
- [ ] Allow campaigns to be archived
- [ ] Exportable templates/lists
- [ ] User feedback on running status of the queue
- [ ] More metadata for campaign status (full count, processed, unprocessed, failed)