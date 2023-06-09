using System.IO;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.SurveySolutions.Documents;

namespace WB.Core.SharedKernels.DataCollection.Views
{
    public class FileDescription : IView
    {
        public Stream Content { get; set; }

        public string Description { get; set; }

        public string FileName { get; set; }

        public string Title { get; set; }
    }
}