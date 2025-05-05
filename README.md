# Email Client Proof of Concept

This is a project written in ~4 days with .NET Aspire with .NET 9. Aspire is designed to be readily cloud-deployable and uses a 
micorservice type architecture, utilizing Docker and k8 if needed. To run this project you will need:

- .Net 9
- Docker

When built and launched, the project will create new docker containers for PostgreSQL and the local Mail Server implimentation. The application
uses the MailKit library to send emails. It has a local email server that can be used to test sending emails without actually sending them. To send
using a real SMTP server, you will need supply credentials int the `appsettings.json`. More on that below.

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

## TODO

- [ ] Move MaxAttempts to config file
- [ ] Feedback on running queue
- [ ] Type up instructions