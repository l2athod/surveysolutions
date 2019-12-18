﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ncqrs.Eventing.Storage;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.DataCollection.Events;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.Synchronization.MetaInfo;

namespace WB.UI.Headquarters.Controllers.Api.DataCollection.Interviewer.v3
{
    [Authorize(Roles = "Interviewer")]
    [Route("api/interviewer/v3/interviews")]
    public class InterviewsApiV3Controller : InterviewerInterviewsControllerBase
    {
        public InterviewsApiV3Controller(
            IImageFileStorage imageFileStorage,
            IAudioFileStorage audioFileStorage,
            IAuthorizedUser authorizedUser,
            IInterviewInformationFactory interviewsFactory,
            IInterviewPackagesService packagesService,
            ICommandService commandService,
            IMetaInfoBuilder metaBuilder,
            IJsonAllTypesSerializer synchronizationSerializer,
            IHeadquartersEventStore eventStore,
            IAudioAuditFileStorage audioAuditFileStorage) :
            base(imageFileStorage,
                audioFileStorage, authorizedUser, interviewsFactory, packagesService, commandService, metaBuilder,
                synchronizationSerializer, eventStore, audioAuditFileStorage)
        {
        }

        [HttpGet]
        [Route("")]
        public override ActionResult<List<InterviewApiView>> Get() => base.Get();

        [HttpPost]
        [Route("{id:guid}/logstate")]
        public override IActionResult LogInterviewAsSuccessfullyHandled(Guid id) => base.LogInterviewAsSuccessfullyHandled(id);

        [HttpGet]
        [Route("{id:guid}")]
        public IActionResult Details(Guid id) => base.DetailsV3(id);

        [HttpPost]
        [Route("{id:guid}")]
        public IActionResult Post(InterviewPackageApiView package) => base.PostV3(package);

        [HttpPost]
        [Route("CheckObsoleteInterviews")]
        public ActionResult<List<Guid>> CheckObsoleteInterviews(List<ObsoletePackageCheck> knownPackages)
        {
            List<Guid> obsoleteInterviews = new List<Guid>();
            foreach (var obsoletePackageCheck in knownPackages)
            {
                if (this.eventStore.HasEventsAfterSpecifiedSequenceWithAnyOfSpecifiedTypes(
                    obsoletePackageCheck.SequenceOfLastReceivedEvent,
                    obsoletePackageCheck.InterviewId, EventsThatChangeAnswersStateProvider.GetTypeNames()))
                {
                    obsoleteInterviews.Add(obsoletePackageCheck.InterviewId);
                }
            }

            return Ok(obsoleteInterviews);
        }

        [HttpPost]
        [Route("{id:guid}/image")]
        public override IActionResult PostImage(PostFileRequest request) => base.PostImage(request);
        [HttpPost]
        [Route("{id:guid}/audio")]
        public override IActionResult PostAudio(PostFileRequest request) => base.PostAudio(request);
        [HttpPost]
        [Route("{id:guid}/audioaudit")]
        public override IActionResult PostAudioAudit(PostFileRequest request) => base.PostAudioAudit(request);

        [HttpPost]
        [Route("{id:guid}/getInterviewUploadState")]
        public InterviewUploadState GetInterviewUploadState(Guid id, [FromBody] EventStreamSignatureTag eventStreamSignatureTag)
            => base.GetInterviewUploadStateImpl(id, eventStreamSignatureTag);
    }
}
