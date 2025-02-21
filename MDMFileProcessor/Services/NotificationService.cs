using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Azure.ServiceBus;
using System.Text.Json;
using ChunkProcessing.Models;

namespace ChunkProcessing.Services
{
    public class NotificationService
    {
        private readonly string _sendGridKey;
        private readonly string _serviceBusConnection;
        private readonly ILogger _logger;

        public NotificationService(string sendGridKey, string serviceBusConnection, ILogger logger)
        {
            _sendGridKey = sendGridKey;
            _serviceBusConnection = serviceBusConnection;
            _logger = logger;
        }

        public async Task SendProcessingNotification(ProcessingSummary summary)
        {
            await Task.WhenAll(
                SendEmailNotification(summary),
                SendTeamsNotification(summary),
                SendServiceBusNotification(summary)
            );
        }

        private async Task SendEmailNotification(ProcessingSummary summary)
        {
            var client = new SendGridClient(_sendGridKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress("noreply@yourcompany.com", "File Processing System"),
                Subject = $"File Processing Complete: {summary.FileName}"
            };

            var emailContent = $@"
                <h2>File Processing Summary</h2>
                <p>File: {summary.FileName}</p>
                <p>Processing Time: {summary.ProcessingStartTime} to {summary.ProcessingEndTime}</p>
                <p>Processed By: {summary.ProcessedBy}</p>
                <hr/>
                <h3>Statistics</h3>
                <ul>
                    <li>Total Records: {summary.TotalRecords}</li>
                    <li>Successful Records: {summary.SuccessfulRecords}</li>
                    <li>Failed Records: {summary.FailedRecords}</li>
                </ul>
                <hr/>
                <p>View detailed results at: https://your-app/results/{summary.FileName}</p>
            ";

            msg.AddContent(MimeType.Html, emailContent);

            // Add recipients based on notification settings
            msg.AddTo(new EmailAddress("admin@yourcompany.com"));
            
            if (summary.FailedRecords > 0)
            {
                msg.AddTo(new EmailAddress("data-quality@yourcompany.com"));
            }

            await client.SendEmailAsync(msg);
        }

        private async Task SendTeamsNotification(ProcessingSummary summary)
        {
            var teamsWebhookUrl = Environment.GetEnvironmentVariable("TeamsWebhookUrl");
            
            var card = new
            {
                type = "MessageCard",
                context = "http://schema.org/extensions",
                themeColor = summary.FailedRecords > 0 ? "FF0000" : "00FF00",
                summary = $"File Processing Complete: {summary.FileName}",
                sections = new[]
                {
                    new
                    {
                        activityTitle = $"File Processing Summary - {summary.FileName}",
                        facts = new[]
                        {
                            new { name = "Total Records", value = summary.TotalRecords.ToString() },
                            new { name = "Successful", value = summary.SuccessfulRecords.ToString() },
                            new { name = "Failed", value = summary.FailedRecords.ToString() },
                            new { name = "Processed By", value = summary.ProcessedBy },
                            new { name = "Duration", value = $"{summary.ProcessingStartTime} to {summary.ProcessingEndTime}" }
                        }
                    }
                }
            };

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonSerializer.Serialize(card));
                await client.PostAsync(teamsWebhookUrl, content);
            }
        }

        private async Task SendServiceBusNotification(ProcessingSummary summary)
        {
            var queueClient = new QueueClient(_serviceBusConnection, "processing-notifications");
            
            var message = new ServiceBusMessage
            {
                ProcessingSummary = summary,
                NotificationType = summary.FailedRecords > 0 ? 
                    NotificationType.ProcessingWithErrors : 
                    NotificationType.ProcessingComplete,
                Timestamp = DateTime.UtcNow
            };

            var messageBody = JsonSerializer.Serialize(message);
            var sbMessage = new Message(System.Text.Encoding.UTF8.GetBytes(messageBody));
            
            // Add custom properties for filtering
            sbMessage.UserProperties.Add("FileName", summary.FileName);
            sbMessage.UserProperties.Add("HasErrors", summary.FailedRecords > 0);
            
            await queueClient.SendAsync(sbMessage);
        }
    }

    public class ServiceBusMessage
    {
        public ProcessingSummary ProcessingSummary { get; set; }
        public NotificationType NotificationType { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum NotificationType
    {
        ProcessingStarted,
        ProcessingComplete,
        ProcessingWithErrors,
        ChunkProcessingFailed,
        ValidationError
    }
}