﻿using System;
using System.Threading;
using System.Threading.Tasks;
using WB.Services.Export.ExportProcessHandlers.Implementation;
using WB.Services.Export.Models;

namespace WB.Services.Export.ExportProcessHandlers
{
    internal interface IBinaryDataSource
    {
        Task ForEachMultimediaAnswerAsync(ExportSettings settings, 
            Func<BinaryData, Task> action, 
            IProgress<int> progress, 
            CancellationToken cancellationToken);
    }
}