using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using ChunkProcessing.Models;
using ChunkProcessing.Services;
using ChunkProcessing.Validation;
using System.Linq;
using System.Text.Json;
using System.Data.SqlClient;

namespace ChunkProcessing.Activities
{
    public static class ChunkProcessingActivity
    {
        private static readonly DeviceValidator _validator = new DeviceValidator();
        private static readonly FailedRecordService _failedRecordService = 
            new FailedRecordService(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

        [FunctionName("ProcessChunk")]
        public static async Task<ProcessingResult> ProcessChunk(
                    [ActivityTrigger] ChunkInfo chunkInfo,
                    ILogger log)
        {
            var result = new ProcessingResult
            {
                ChunkNumber = chunkInfo.ChunkNumber
            };

            var blobClient = new BlobClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "mdm-files",
                chunkInfo.BlobName);

            using var stream = await blobClient.OpenReadAsync();
            var devices = CsvProcessor.ReadChunk(stream, chunkInfo.StartRow, chunkInfo.RowCount).ToList();

            foreach (var device in devices)
            {
                var validationResult = _validator.Validate(device);

                if (validationResult.IsValid)
                {
                    await ProcessValidRecord(device);
                    result.SuccessCount++;
                }
                else
                {
                    var failedRecord = new FailedRecord
                    {
                        LineNumber = chunkInfo.StartRow + result.SuccessCount + result.FailedCount + 1,
                        RawData = JsonSerializer.Serialize(device),
                        ValidationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                        ProcessedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        ProcessedBy = chunkInfo.ProcessedBy
                    };

                    await _failedRecordService.SaveFailedRecordAsync(chunkInfo.BlobName, failedRecord);
                    result.FailedRecords.Add(failedRecord);
                    result.FailedCount++;
                }
            }

            return result;
        }

        private static async Task ProcessValidRecord(Device device)
        {
            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = @"
        MERGE INTO Devices AS target
        USING (VALUES (@DeviceId, @Name, @Description, @Type, 
                      @Location, @InstallationDate, @Status, @ProcessedAt)) 
        AS source (DeviceId, Name, Description, Type, 
                  Location, InstallationDate, Status, ProcessedAt)
        ON target.DeviceId = source.DeviceId
        WHEN MATCHED THEN
            UPDATE SET 
                Name = source.Name,
                Description = source.Description,
                Type = source.Type,
                Location = source.Location,
                InstallationDate = source.InstallationDate,
                Status = source.Status,
                ProcessedAt = source.ProcessedAt
        WHEN NOT MATCHED THEN
            INSERT (DeviceId, Name, Description, Type,
                   Location, InstallationDate, Status, ProcessedAt)
            VALUES (source.DeviceId, source.Name, source.Description,
                   source.Type, source.Location, source.InstallationDate,
                   source.Status, source.ProcessedAt);";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@DeviceId", device.DeviceId);
            command.Parameters.AddWithValue("@Name", device.DeviceName);
            command.Parameters.AddWithValue("@Description", device.Description);
            command.Parameters.AddWithValue("@Type", device.Type);
            command.Parameters.AddWithValue("@Location", device.Location);
            command.Parameters.AddWithValue("@InstallationDate", device.InstallationDate);
            command.Parameters.AddWithValue("@Status", device.Status);
            command.Parameters.AddWithValue("@ProcessedAt", DateTime.UtcNow);

            await command.ExecuteNonQueryAsync();
        }
    }
}
