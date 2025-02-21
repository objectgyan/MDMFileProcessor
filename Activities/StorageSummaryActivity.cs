using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Azure.Data.Tables;
using ChunkProcessing.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChunkProcessing.Activities
{
    public static class StorageSummaryActivity
    {
        [FunctionName("StoreSummary")]
        public static async Task StoreSummary([ActivityTrigger] ProcessingSummary summary)
        {
            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "ProcessingSummaries");

            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity(summary.FileName, summary.ProcessingStartTime)
            {
                { "TotalRecords", summary.TotalRecords },
                { "SuccessfulRecords", summary.SuccessfulRecords },
                { "FailedRecords", summary.FailedRecords },
                { "ProcessingEndTime", summary.ProcessingEndTime },
                { "ProcessedBy", summary.ProcessedBy }
            };

            await tableClient.AddEntityAsync(entity);
        }

        [FunctionName("GetProcessingSummary")]
        public static async Task<ProcessingSummary> GetProcessingSummary(
            [ActivityTrigger] string fileName)
        {
            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "ProcessingSummaries");

            var queryResults = tableClient.QueryAsync<TableEntity>(
                filter: $"PartitionKey eq '{fileName}'");

            var summaries = new List<ProcessingSummary>();
            await foreach (var entity in queryResults)
            {
                summaries.Add(new ProcessingSummary
                {
                    FileName = entity.PartitionKey,
                    ProcessingStartTime = entity.RowKey,
                    TotalRecords = entity.GetInt32("TotalRecords") ?? 0,
                    SuccessfulRecords = entity.GetInt32("SuccessfulRecords") ?? 0,
                    FailedRecords = entity.GetInt32("FailedRecords") ?? 0,
                    ProcessingEndTime = entity.GetString("ProcessingEndTime"),
                    ProcessedBy = entity.GetString("ProcessedBy")
                });
            }

            return summaries.OrderByDescending(s => s.ProcessingStartTime).FirstOrDefault();
        }
    }
}
