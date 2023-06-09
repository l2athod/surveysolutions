using System.Collections.Generic;
using System.IO;
using WB.Core.BoundedContexts.Headquarters.Services;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Factories
{
    public interface ICsvWriter
    {
        ICsvWriterService OpenCsvWriter(Stream stream, string delimiter = ",");
        void WriteData(string filePath, IEnumerable<string[]> records, string delimiter);
    }
}