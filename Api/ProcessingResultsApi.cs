using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ChunkProcessing.Services;
using System;

namespace ChunkProcessing.Api
{
    public static class ProcessingResultsApi
    {
        [FunctionName("GetProcessingResults")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "results/{fileName}")] HttpRequest req,
            string fileName)
        {
            var failedRecordService = new FailedRecordService(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

            var failedRecords = await failedRecordService.GetFailedRecordsAsync(fileName);

            return new OkObjectResult(new
            {
                FileName = fileName,
                FailedRecords = failedRecords
            });
        }
    }
}
