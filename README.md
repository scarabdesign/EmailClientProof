# EmailClient


## Notes

- Generate manifest:
	```bash
	dotnet run --publisher manifest --output-path manifest.json
	```

- Set up Gmail 2fa authentication:
    ~https://medium.com/@abhinandkr56/how-to-send-emails-using-net-core-mailkit-and-googles-smtp-server-6521827c4198~
	1. Visit the Google Account settings.
	2. Select “Security” from the left-hand side.
	3. Under the “Signing in to Google” section, click on “2-Step Verification” and follow the prompts to enable it.
	4. After enabling two-step verification, click on “App Passwords”. You may be asked to re-enter your password.
	5. In the App passwords section, select “Mail” in the “Select app” dropdown, and select the device you’re using in the “Select device” dropdown, then click “Generate”.
	6. Google will generate a new 16-character password for you. You will use this in your code, instead of your regular Google password.

## TODO

- [ ] Move MaxAttempts to config file
- [ ] Feedback on running queue
- [ ] Type up instructions