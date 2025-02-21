using CsvHelper;
using System.Globalization;
using ChunkProcessing.Models;

namespace ChunkProcessing.Services
{
    public static class CsvProcessor
    {
        public static IEnumerable<Device> ReadChunk(Stream stream, int startRow, int rowCount)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            // Skip to the start of the chunk
            for (int i = 0; i < startRow; i++)
            {
                csv.Read();
            }

            var records = new List<Device>();
            int currentRow = 0;

            while (csv.Read() && currentRow < rowCount)
            {
                records.Add(csv.GetRecord<Device>());
                currentRow++;
            }

            return records;
        }
    }
}