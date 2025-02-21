using System;

namespace ChunkProcessing.Models
{
    public class NotificationConfig
    {
        public EmailNotificationSettings Email { get; set; }
        public TeamsNotificationSettings Teams { get; set; }
        public ServiceBusNotificationSettings ServiceBus { get; set; }
        public NotificationRules Rules { get; set; }
    }

    public class EmailNotificationSettings
    {
        public string FromAddress { get; set; }
        public string[] DefaultRecipients { get; set; }
        public string[] ErrorNotificationRecipients { get; set; }
        public bool SendDetailedErrors { get; set; }
    }

    public class TeamsNotificationSettings
    {
        public string WebhookUrl { get; set; }
        public bool NotifyOnSuccess { get; set; }
        public bool NotifyOnError { get; set; }
        public string[] ChannelIds { get; set; }
    }

    public class ServiceBusNotificationSettings
    {
        public string QueueName { get; set; }
        public bool EnableMessageTracking { get; set; }
        public int MessageTtlHours { get; set; }
    }

    public class NotificationRules
    {
        public int ErrorThresholdPercent { get; set; }
        public bool NotifyOnZeroRecords { get; set; }
        public bool NotifyOnComplete { get; set; }
        public TimeSpan ProcessingTimeThreshold { get; set; }
    }
}