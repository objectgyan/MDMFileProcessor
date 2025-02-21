using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using ChunkProcessing.Models;
using ChunkProcessing.Services;
using System;
using System.Threading.Tasks;

namespace ChunkProcessing.Activities
{
    public class SendNotificationsActivity
    {
        private readonly NotificationService _notificationService;
        private readonly ILogger<SendNotificationsActivity> _logger;

        public SendNotificationsActivity(NotificationService notificationService, ILogger<SendNotificationsActivity> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [FunctionName("SendNotifications")]
        public async Task Run(
            [ActivityTrigger] ProcessingSummary summary,
            ILogger log)
        {
            try
            {
                _logger.LogInformation($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting notifications for file {summary.FileName} by {summary.ProcessedBy}");

                await _notificationService.SendProcessingNotification(summary);

                _logger.LogInformation($"[2025-02-20 18:49:37] Notifications sent successfully for file {summary.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notifications: {ex.Message}");
                throw;
            }
        }
    }
}