﻿using System;
using System.Threading.Tasks;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Messages;
using WB.Core.SharedKernels.Enumerator.OfflineSync.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.BoundedContexts.Supervisor.Services.Implementation.OfflineSyncHandlers
{
    public class SupervisorBinaryHandler : IHandleCommunicationMessage
    {
        private readonly IPlainStorage<CompanyLogo> logoStorage;
        private readonly IAudioFileStorage audioFileStorage;
        private readonly IImageFileStorage imageFileStorage;
        private readonly IAudioAuditFileStorage audioAuditFileStorage;

        public SupervisorBinaryHandler(IPlainStorage<CompanyLogo> logoStorage,
            IAudioFileStorage audioFileStorage,
            IImageFileStorage imageFileStorage, 
            IAudioAuditFileStorage audioAuditFileStorage)
        {
            this.logoStorage = logoStorage;
            this.audioFileStorage = audioFileStorage;
            this.imageFileStorage = imageFileStorage;
            this.audioAuditFileStorage = audioAuditFileStorage;
        }

        public void Register(IRequestHandler requestHandler)
        {
            requestHandler.RegisterHandler<GetCompanyLogoRequest, GetCompanyLogoResponse>(GetCompanyLogo);

            requestHandler.RegisterHandler<UploadInterviewImageRequest, OkResponse>(UploadImage);
            requestHandler.RegisterHandler<UploadInterviewAudioRequest, OkResponse>(UploadAudio);
            requestHandler.RegisterHandler<UploadInterviewAudioAuditRequest, OkResponse>(UploadAudioAudit);
        }

        public Task<GetCompanyLogoResponse> GetCompanyLogo(GetCompanyLogoRequest request)
        {
            var existingLogo = logoStorage.GetById(CompanyLogo.StorageKey);
            if (existingLogo == null)
            {
                return Task.FromResult(new GetCompanyLogoResponse
                {
                    LogoInfo = new CompanyLogoInfo
                    {
                        HasCustomLogo = false,
                        LogoNeedsToBeUpdated = !string.IsNullOrEmpty(request.Etag)
                    }
                });
            }

            var needUpdate = existingLogo.ETag != request.Etag;

            return Task.FromResult(new GetCompanyLogoResponse
            {
                LogoInfo = new CompanyLogoInfo
                {
                    Etag = existingLogo.ETag,
                    Logo = needUpdate ? existingLogo.File : null,
                    LogoNeedsToBeUpdated = needUpdate,
                    HasCustomLogo = true
                }
            });
        }
        
        private Task<OkResponse> UploadAudioAudit(UploadInterviewAudioAuditRequest request)
        {
            this.audioAuditFileStorage.StoreInterviewBinaryData(request.InterviewAudio.InterviewId, 
                request.InterviewAudio.FileName,
                Convert.FromBase64String(request.InterviewAudio.Data), 
                request.InterviewAudio.ContentType);

            return Task.FromResult(new OkResponse());
        }

        private Task<OkResponse> UploadAudio(UploadInterviewAudioRequest request)
        {
            this.audioFileStorage.StoreInterviewBinaryData(request.InterviewAudio.InterviewId, 
                request.InterviewAudio.FileName,
                Convert.FromBase64String(request.InterviewAudio.Data), 
                request.InterviewAudio.ContentType);

            return Task.FromResult(new OkResponse());
        }

        private Task<OkResponse> UploadImage(UploadInterviewImageRequest request)
        {
            this.imageFileStorage.StoreInterviewBinaryData(request.InterviewImage.InterviewId, 
                request.InterviewImage.FileName,
                Convert.FromBase64String(request.InterviewImage.Data), 
                null);
            return Task.FromResult(new OkResponse());
        }
    }
}
