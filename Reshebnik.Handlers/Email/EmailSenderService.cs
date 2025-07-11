﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Reshebnik.Domain.Entities;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Email;

public class EmailSenderService(
    ILogger<EmailSenderService> logger,
    IServiceProvider serviceProvider,
    IEmailSender emailSender)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Email sender service is running");
        while (!stoppingToken.IsCancellationRequested)
        {
            EmailMessageEntity? email = null;
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var queue = scope.ServiceProvider.GetRequiredService<IEmailQueue>();
                email = await queue.DequeueAsync(stoppingToken);
                if(email == null) continue;
                await emailSender.SendEmailAsync(email, stoppingToken);
                
                var db = scope.ServiceProvider.GetRequiredService<ReshebnikContext>();
                email = await db.EmailQueue.FirstAsync(f => f.Id == email.Id, cancellationToken: CancellationToken.None);
                email.SentAt = DateTime.UtcNow;
                email.IsSent = true;
                
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                if (email != null)
                {
                    await using var scope = serviceProvider.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<ReshebnikContext>();
                    var emailEntity = await db.EmailQueue.FirstAsync(f => f.Id == email.Id, cancellationToken: stoppingToken);
                    emailEntity.Error = email.Error;
                    await db.SaveChangesAsync(stoppingToken);
                }

                logger.LogError(ex, "Error occurred while sending email");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("Email sender service is stopping");
    }
}