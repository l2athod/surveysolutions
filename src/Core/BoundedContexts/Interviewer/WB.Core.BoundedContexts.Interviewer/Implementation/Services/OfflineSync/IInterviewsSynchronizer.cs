﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace WB.Core.BoundedContexts.Interviewer.Implementation.Services.OfflineSync
{
    public interface IInterviewsSynchronizer
    {
        Task UploadPendingInterviews(IProgress<UploadProgress> progress, CancellationToken cancellationToken);
    }
}
