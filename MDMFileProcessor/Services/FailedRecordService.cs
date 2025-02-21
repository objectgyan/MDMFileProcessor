using Azure.Data.Tables;
using ChunkProcessing.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChunkProcessing.Services
{
    public class FailedRecordService
    {
        private readonly TableClient _tableClient;

        public FailedRecordService(string connectionString)
        {
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient("FailedRecords");
            _tableClient.CreateIfNotExists();
        }

        public async Task SaveFailedRecordAsync(string fileName, FailedRecord failedRecord)
        {
            var entity = new TableEntity(fileName, $"{failedRecord.LineNumber}")
            {
                { "RawData", failedRecord.RawData },
                { "ValidationErrors", JsonSerializer.Serialize(failedRecord.ValidationErrors) },
                { "ProcessedAt", failedRecord.ProcessedAt },
                { "ProcessedBy", failedRecord.ProcessedBy }
            };

            await _tableClient.AddEntityAsync(entity);
        }

        public async Task<List<FailedRecord>> GetFailedRecordsAsync(string fileName)
        {
            var failedRecords = new List<FailedRecord>();
            var queryResults = _tableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '{fileName}'");

            await foreach (var entity in queryResults)
            {
                failedRecords.Add(new FailedRecord
                {
                    LineNumber = int.Parse(entity.RowKey),
                    RawData = entity.GetString("RawData"),
                    ValidationErrors = JsonSerializer.Deserialize<List<string>>(entity.GetString("ValidationErrors")),
                    ProcessedAt = entity.GetString("ProcessedAt"),
                    ProcessedBy = entity.GetString("ProcessedBy")
                });
            }

            return failedRecords;
        }
    }
}