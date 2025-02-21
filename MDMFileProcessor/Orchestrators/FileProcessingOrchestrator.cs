using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using ChunkProcessing.Models;
using System.Linq;

namespace ChunkProcessing.Orchestrators
{
    public static class FileProcessingOrchestrator
    {
        [FunctionName("ProcessFileOrchestrator")]
        public static async Task<ProcessingSummary> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var orchestratorInput = context.GetInput<OrchestratorInput>();
            var outputs = new List<ProcessingResult>();

            try
            {
                // Fan out - process chunks in parallel
                var tasks = new Task<ProcessingResult>[orchestratorInput.NumberOfChunks];

                for (int i = 0; i < orchestratorInput.NumberOfChunks; i++)
                {
                    var chunkInfo = new ChunkInfo
                    {
                        BlobName = orchestratorInput.BlobName,
                        ContainerName = orchestratorInput.ContainerName,
                        ChunkNumber = i,
                        StartRow = i * orchestratorInput.ChunkSize,
                        RowCount = orchestratorInput.ChunkSize,
                        ProcessedBy = orchestratorInput.ProcessedBy,
                        CorrelationId = orchestratorInput.CorrelationId
                    };

                    tasks[i] = context.CallActivityAsync<ProcessingResult>("ProcessChunk", chunkInfo);
                }

                // Wait for all chunks to complete
                await Task.WhenAll(tasks);
                outputs.AddRange(tasks.Select(t => t.Result));

                // Create processing summary
                var summary = new ProcessingSummary
                {
                    FileName = orchestratorInput.BlobName,
                    TotalRecords = outputs.Sum(o => o.SuccessCount + o.FailedCount),
                    SuccessfulRecords = outputs.Sum(o => o.SuccessCount),
                    FailedRecords = outputs.Sum(o => o.FailedCount),
                    ProcessingStartTime = orchestratorInput.ProcessingStartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProcessingEndTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    ProcessedBy = orchestratorInput.ProcessedBy,
                    CorrelationId = orchestratorInput.CorrelationId
                };

                // Store final summary
                await context.CallActivityAsync("StoreSummary", summary);

                // Send notifications
                await context.CallActivityAsync("SendNotifications", summary);

                return summary;
            }
            catch (Exception ex)
            {
                log.LogError($"Error in orchestrator: {ex.Message}");
                throw;
            }
        }
    }
}
