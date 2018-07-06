﻿using System;
using System.Threading.Tasks;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Messages;

namespace WB.Core.SharedKernels.Enumerator.OfflineSync.Services.Implementation
{
    public class OfflineSyncClient : IOfflineSyncClient
    {
        private readonly INearbyCommunicator communicator;
        private readonly INearbyConnection nearbyConnection;

        public OfflineSyncClient(INearbyCommunicator communicator, INearbyConnection nearbyConnection)
        {
            this.communicator = communicator;
            this.nearbyConnection = nearbyConnection;
        }

        public Task<GetQuestionnaireListResponse> GetQuestionnaireList(string endpoint,
            IProgress<CommunicationProgress> progress = null)
        {
            return this.communicator.SendAsync<GetQuestionnaireListRequest, GetQuestionnaireListResponse>(this.nearbyConnection,
                endpoint, new GetQuestionnaireListRequest(), progress);
        }

        public Task<OkResponse> PostInterviewAsync(string endpoint, PostInterviewRequest package,
            IProgress<CommunicationProgress> progress = null)
        {
            return this.communicator.SendAsync<PostInterviewRequest, OkResponse>(this.nearbyConnection,
                endpoint, package, progress);
        }
    }
}
