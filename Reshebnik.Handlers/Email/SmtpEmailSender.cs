using MailKit.Net.Smtp;
using MailKit.Security;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using MimeKit;

using Polly;

using Reshebnik.Domain.Entities;

namespace Reshebnik.Handlers.Email;

public interface IEmailSender
{
    Task SendEmailAsync(EmailMessageEntity message, CancellationToken cancellationToken = default);
}

public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendEmailAsync(EmailMessageEntity message, CancellationToken cancellationToken = default)
    {
        var botLogin = configuration.GetValue<string>("email:login")!;
        var botPass = configuration.GetValue<string>("email:password")!;

        var mail = new MimeMessage();
        mail.From.Add(MailboxAddress.Parse(botLogin));
        mail.To.Add(MailboxAddress.Parse(message.To));
        mail.Subject = message.Subject;

        foreach (var cc in message.Cc)
            mail.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in message.Bcc)
            mail.Bcc.Add(MailboxAddress.Parse(bcc));

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = message.IsHtml ? message.Body : null,
            TextBody = message.IsHtml ? null : message.Body
        };

        foreach (var att in message.Attachments)
        {
            bodyBuilder.Attachments.Add(att.FileName, att.Content, ContentType.Parse(att.ContentType));
        }

        mail.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        client.Timeout = 1000 * 60 * 5; // 5min

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    // exponential backoff
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var exceptionMessage = outcome.Message;
                    logger.LogWarning($"[Retry {retryAttempt}] after {timespan.TotalSeconds}s due to: {exceptionMessage}");
                }
            );

        try
        {
            int attemp = 0;
            await retryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        attemp++;
                        logger.LogInformation($"Attempt #{attemp} to send Email on {mail.To[0].Name}");
                        await client.ConnectAsync("smtp.timeweb.ru", 465, SecureSocketOptions.Auto, cancellationToken);
                        await client.AuthenticateAsync(botLogin, botPass, cancellationToken);

                        await client.SendAsync(mail, cancellationToken);
                        logger.LogInformation($"Attempt #{attemp} to send Email on {mail.To[0].Name} finished with SUCCESS");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Email send exception: {ex.Message}, Stack: {WriteTrimmedLine(ex.StackTrace)}");
                    }
                }
            );
        }
        catch (Exception ex)
        {
            message.Error = $"{ex.Message} | {ex.StackTrace}";
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    private static string WriteTrimmedLine(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        const int maxLength = 3000;
        if (text.Length > maxLength)
        {
            return text[..maxLength] + "...";
        }
        else
        {
            return text;
        }
    }
}