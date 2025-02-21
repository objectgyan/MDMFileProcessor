using System.Collections.Generic;

namespace ChunkProcessing.Models
{
    public class ProcessingResult
    {
        public int ChunkNumber { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public List<FailedRecord> FailedRecords { get; set; } = new();
    }

    public class FailedRecord
    {
        public int LineNumber { get; set; }
        public string RawData { get; set; }
        public List<string> ValidationErrors { get; set; }
        public string ProcessedAt { get; set; }
        public string ProcessedBy { get; set; }
    }

    public class ProcessingSummary
    {
        public string FileName { get; set; }
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public string ProcessingStartTime { get; set; }
        public string ProcessingEndTime { get; set; }
        public string ProcessedBy { get; set; }
        public string CorrelationId { get; internal set; }
    }
}