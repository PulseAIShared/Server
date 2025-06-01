using Application.Abstractions.Data;
using Domain.Imports;
using Domain;
using SharedKernel.Enums;
using SharedKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Notification;
using Application.Services;

namespace Application.Imports.Events
{
    internal sealed class ImportJobCompletedDomainEventHandler(
        IApplicationDbContext context,
        INotificationService notificationService)
        : IDomainEventHandler<ImportJobCompletedDomainEvent>
    {
        public async Task Handle(ImportJobCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            // Create in-app notification for the notification inbox
            await CreateInAppNotification(domainEvent, cancellationToken);

            // Send real-time notification via SignalR for immediate feedback
            await SendRealTimeNotification(domainEvent, cancellationToken);
        }

        private async Task CreateInAppNotification(ImportJobCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var notification = new Notification
            {
                UserId = domainEvent.UserId,
                Type = GetNotificationType(domainEvent.Status),
                Category = NotificationCategory.Import,
                Title = GetNotificationTitle(domainEvent.Status),
                Message = GetNotificationMessage(domainEvent),
                ActionUrl = domainEvent.Status == ImportJobStatus.Failed && domainEvent.FailedRecords > 0
                    ? $"/imports/{domainEvent.ImportJobId}/errors"
                    : null,
                ActionText = domainEvent.Status == ImportJobStatus.Failed && domainEvent.FailedRecords > 0
                    ? "Download Error Report"
                    : null,
                Metadata = new Dictionary<string, object>
                {
                    ["importJobId"] = domainEvent.ImportJobId,
                    ["totalRecords"] = domainEvent.TotalRecords,
                    ["successfulRecords"] = domainEvent.SuccessfulRecords,
                    ["failedRecords"] = domainEvent.FailedRecords,
                    ["skippedRecords"] = domainEvent.SkippedRecords,
                    ["summary"] = domainEvent.Summary != null ? JsonSerializer.Serialize(domainEvent.Summary) : null
                },
                ExpiresAt = DateTime.UtcNow.AddDays(30) // Notifications expire after 30 days
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken);
        }
    

     private async Task SendRealTimeNotification(ImportJobCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var realTimeNotification = new
            {
                Id = Guid.NewGuid(),
                Type = GetNotificationType(domainEvent.Status).ToString().ToLower(),
                Title = GetNotificationTitle(domainEvent.Status),
                Message = GetNotificationMessage(domainEvent),
                ImportJobId = domainEvent.ImportJobId,
                Results = new
                {
                    TotalRecords = domainEvent.TotalRecords,
                    SuccessfulRecords = domainEvent.SuccessfulRecords,
                    FailedRecords = domainEvent.FailedRecords,
                    SkippedRecords = domainEvent.SkippedRecords,
                    Summary = domainEvent.Summary
                },
                Timestamp = DateTime.UtcNow,
                ActionUrl = domainEvent.Status == ImportJobStatus.Failed && domainEvent.FailedRecords > 0
                    ? $"/imports/{domainEvent.ImportJobId}/errors"
                    : null,
                ActionText = domainEvent.Status == ImportJobStatus.Failed && domainEvent.FailedRecords > 0
                    ? "Download Error Report"
                    : null
            };

            await notificationService.SendNotificationToUserAsync(
                domainEvent.UserId,
                "import_completed",
                realTimeNotification,
                cancellationToken
            );
        }

        private static NotificationType GetNotificationType(ImportJobStatus status)
        {
            return status switch
            {
                ImportJobStatus.Completed => NotificationType.Success,
                ImportJobStatus.Failed => NotificationType.Error,
                ImportJobStatus.Cancelled => NotificationType.Warning,
                _ => NotificationType.Info
            };
        }

        private static string GetNotificationTitle(ImportJobStatus status)
        {
            return status switch
            {
                ImportJobStatus.Completed => "Import Completed Successfully",
                ImportJobStatus.Failed => "Import Failed",
                ImportJobStatus.Cancelled => "Import Cancelled",
                _ => "Import Status Update"
            };
        }

        private static string GetNotificationMessage(ImportJobCompletedDomainEvent domainEvent)
        {
            return domainEvent.Status switch
            {
                ImportJobStatus.Completed => BuildSuccessMessage(domainEvent),
                ImportJobStatus.Failed => BuildFailureMessage(domainEvent),
                ImportJobStatus.Cancelled => "Your customer import was cancelled.",
                _ => "Import status has been updated."
            };
        }

        private static string BuildSuccessMessage(ImportJobCompletedDomainEvent domainEvent)
        {
            var parts = new List<string>
        {
            $"Successfully imported {domainEvent.SuccessfulRecords:N0} customers"
        };

            if (domainEvent.SkippedRecords > 0)
            {
                parts.Add($"{domainEvent.SkippedRecords:N0} duplicates skipped");
            }

            if (domainEvent.Summary != null)
            {
                parts.Add($"Average revenue: ${domainEvent.Summary.AverageRevenue:N0}");
                if (domainEvent.Summary.HighRiskCustomers > 0)
                {
                    parts.Add($"{domainEvent.Summary.HighRiskCustomers} high-risk customers identified");
                }
            }

            return string.Join(" • ", parts);
        }

        private static string BuildFailureMessage(ImportJobCompletedDomainEvent domainEvent)
        {
            var message = $"Import failed after processing {domainEvent.TotalRecords:N0} records";

            if (!string.IsNullOrEmpty(domainEvent.ErrorMessage))
            {
                message += $": {domainEvent.ErrorMessage}";
            }

            if (domainEvent.FailedRecords > 0)
            {
                message += $" • {domainEvent.FailedRecords:N0} records had errors";
            }

            return message;
        }
    }
}