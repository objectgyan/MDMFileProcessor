using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using ChunkProcessing.Models;
using System.Security.Claims;
using Azure.Data.Tables;

namespace ChunkProcessing.Triggers
{
    public static class MDMBlobTriggerFunction
    {
        [FunctionName("MDMBlobTriggerFunction")]
        public static async Task RunAsync(
            [BlobTrigger("mdm-files/{name}")] Stream myBlob,
            string name,
            string blobTrigger,
            [DurableClient] IDurableOrchestrationClient starter,
            ClaimsPrincipal claimsPrincipal,
            ILogger log)
        {
            try
            {
                // Get the blob client to read file properties
                var blobClient = new BlobClient(
                    Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                    "mdm-files",
                    name);

                var properties = await blobClient.GetPropertiesAsync();

                // Calculate number of chunks based on file size or metadata
                int totalRecords = await GetTotalRecords(myBlob);
                int chunkSize = int.Parse(Environment.GetEnvironmentVariable("ChunkSize") ?? "2000");
                int numberOfChunks = (int)Math.Ceiling((double)totalRecords / chunkSize);

                // Create orchestrator input
                var orchestratorInput = new OrchestratorInput
                {
                    BlobName = name,
                    ProcessedBy = claimsPrincipal?.Identity?.Name ?? "system", // Get user from claims
                    ChunkSize = chunkSize,
                    NumberOfChunks = numberOfChunks,
                    ContainerName = "mdm-files",
                    CorrelationId = Guid.NewGuid().ToString(),
                    ProcessingStartTime = DateTime.UtcNow
                };

                // Store processing metadata in Table Storage
                await StoreProcessingMetadata(orchestratorInput);

                // Start the orchestrator with the input
                string instanceId = await starter.StartNewAsync(
                    "ProcessFileOrchestrator",
                    orchestratorInput.CorrelationId,
                    orchestratorInput);

                log.LogInformation($"Started orchestration with ID = '{instanceId}' for file '{name}'.");

                // Store the orchestration instance ID for tracking
                await UpdateProcessingMetadata(orchestratorInput.CorrelationId, instanceId);
            }
            catch (Exception ex)
            {
                log.LogError($"Error starting orchestration for file {name}: {ex.Message}");
                throw;
            }
        }

        private static async Task<int> GetTotalRecords(Stream blob)
        {
            blob.Position = 0;
            using var reader = new StreamReader(blob);
            int count = 0;
            while (await reader.ReadLineAsync() != null)
            {
                count++;
            }
            blob.Position = 0;
            return count - 1; // Subtract 1 for header row
        }

        private static async Task StoreProcessingMetadata(OrchestratorInput input)
        {
            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "ProcessingMetadata");

            await tableClient.CreateIfNotExistsAsync();

            var entity = new TableEntity(input.BlobName, input.CorrelationId)
            {
                { "ProcessedBy", input.ProcessedBy },
                { "StartTime", input.ProcessingStartTime },
                { "ChunkSize", input.ChunkSize },
                { "NumberOfChunks", input.NumberOfChunks },
                { "Status", "Initiated" }
            };

            await tableClient.AddEntityAsync(entity);
        }

        private static async Task UpdateProcessingMetadata(string correlationId, string instanceId)
        {
            var tableClient = new TableClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"),
                "ProcessingMetadata");

            // Get existing entity
            var result = await tableClient.QueryAsync<TableEntity>(
                filter: $"RowKey eq '{correlationId}'").FirstOrDefaultAsync();

            if (result != null)
            {
                result["InstanceId"] = instanceId;
                result["Status"] = "Processing";
                await tableClient.UpdateEntityAsync(result, result.ETag);
            }
        }
    }
}