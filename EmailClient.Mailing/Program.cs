using EmailClient.Mailing;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

var builder = WebApplication.CreateBuilder(args);

builder.AddMailKitClient("maildev");

builder.Logging.AddConsole();

builder.Services.AddProblemDetails();
builder.Services.AddScoped<IMailer, Mailer>();

var app = builder.Build();

ProcessCommandLine().Wait();

async Task ProcessCommandLine()
{
    if (args is { Length: 7 })
    {
        Console.WriteLine("Attempting to send email via command-line");
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IMailer>();
            try
            {
                Console.WriteLine($"Configuring: User:{args[0]}, Password:***, Host: {args[2]}, Port: {args[3]}");
                await context.Configure(new MailerSettings
                {
                    Username = args[0],
                    Password = args[1],
                    Host = args[2],
                    Port = int.Parse(args[3]),
                });

                Console.WriteLine($"Sending: From:{args[0]}, To: {args[4]}, Subject: {args[5]}, Body: {args[6].Substring(0, Math.Min(args[6].Length, 10))}...");
                await context.SendEmail(new MimeMessage
                {
                    From = { new MailboxAddress("", args[0]) },
                    To = { new MailboxAddress("", args[4]) },
                    Subject = args[5],
                    Body = new BodyBuilder
                    {
                        HtmlBody = args[6],
                    }.ToMessageBody(),
                    MessageId = MimeKit.Utils.MimeUtils.GenerateMessageId()
                });
            }
            catch(Exception e)
            {
                Console.WriteLine("There was a problem sending the email.");
                Console.WriteLine(e);
            }
        }
    }
    else
    {
        Console.WriteLine("Correct synctax for sending via command-line:");
        Console.WriteLine("> ./EmailClient.Mailing.exe [from/username] [password] [host] [port] [to] [subject] [body]");
        Console.WriteLine("     All parameters are required");
        Console.WriteLine("     [from/username] String; Will be used to authenticate and in the From line");
        Console.WriteLine("     [password] String; For authentication. For Gmail, must be app password");
        Console.WriteLine("     [host] String; e.g., smtp.gmail.com");
        Console.WriteLine("     [port] Integer; e.g., 587");
        Console.WriteLine("     [to] String; Sending email to");
        Console.WriteLine("     [subject] String; Subject of the email");
        Console.WriteLine("     [body] String; Body of the email");
        Console.WriteLine("Example:");
        Console.WriteLine("> ./EmailClient.Mailing.exe \"myAddress@gmail.com\" \"XXXX XXXX XXXX\" \"smtp.gmail.com\" 587 \"toAddress@gmail.com\" \"This is the subject\" \"This is the body\"");
    }
        
}
