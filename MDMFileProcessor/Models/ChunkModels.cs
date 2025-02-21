using System;

namespace ChunkProcessing.Models
{
    public class OrchestratorInput
    {
        public string BlobName { get; set; }
        public string ProcessedBy { get; set; }
        public int ChunkSize { get; set; }
        public int NumberOfChunks { get; set; }
        public string ContainerName { get; set; }
        public string CorrelationId { get; set; }
        public DateTime ProcessingStartTime { get; set; }
    }

    public class ChunkInfo
    {
        public string BlobName { get; set; }
        public int ChunkNumber { get; set; }
        public int StartRow { get; set; }
        public int RowCount { get; set; }
        public string ProcessedBy { get; set; }
        public string ContainerName { get; internal set; }
        public string CorrelationId { get; internal set; }
    }
}
