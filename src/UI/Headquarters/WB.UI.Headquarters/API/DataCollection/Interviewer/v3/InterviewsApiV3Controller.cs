﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Results;
using Main.Core.Entities.SubEntities;
using Ncqrs.Eventing;
using Ncqrs.Eventing.Storage;
using WB.Core.BoundedContexts.Headquarters.Implementation.Synchronization;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Views;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.SynchronizationLog;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.Synchronization.MetaInfo;
using WB.UI.Headquarters.Code;

namespace WB.UI.Headquarters.API.DataCollection.Interviewer.v3
{
    [ApiBasicAuth(new[] { UserRoles.Interviewer })]
    public class InterviewsApiV3Controller : InterviewerInterviewsControllerBase
    {
        public InterviewsApiV3Controller(IImageFileStorage imageFileStorage, IAudioFileStorage audioFileStorage, IAuthorizedUser authorizedUser, IInterviewInformationFactory interviewsFactory, IInterviewPackagesService interviewPackagesService, ICommandService commandService, IMetaInfoBuilder metaBuilder, IJsonAllTypesSerializer synchronizationSerializer, IHeadquartersEventStore eventStore, IInterviewPackagesService packagesService) 
            : base(imageFileStorage, audioFileStorage, authorizedUser, interviewsFactory, interviewPackagesService, commandService, metaBuilder, synchronizationSerializer, eventStore, packagesService)
        {
        }

        [HttpGet]
        [WriteToSyncLog(SynchronizationLogType.GetInterviewV3)]
        public override JsonResult<List<CommittedEvent>> DetailsV3(Guid id) => base.DetailsV3(id);

        [WriteToSyncLog(SynchronizationLogType.PostInterviewV3)]
        [HttpPost]
        public override HttpResponseMessage PostV3(InterviewPackageApiView package) => base.PostV3(package);

        [HttpPost]
        [WriteToSyncLog(SynchronizationLogType.CheckObsoleteInterviews)]
        public HttpResponseMessage CheckObsoleteInterviews(List<ObsoletePackageCheck> knownPackages)
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

            return Request.CreateResponse(HttpStatusCode.OK, obsoleteInterviews);
        } 
    }
}
