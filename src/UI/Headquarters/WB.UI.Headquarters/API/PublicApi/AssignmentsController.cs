﻿using AutoMapper;
using Main.Core.Entities.SubEntities;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.Views.PreloadedData;
using WB.Core.BoundedContexts.Headquarters.Views.User;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.UI.Headquarters.API.PublicApi.Models;
using WB.UI.Headquarters.Code;

namespace WB.UI.Headquarters.API.PublicApi
{
    [ApiBasicAuth(UserRoles.ApiUser, UserRoles.Administrator, TreatPasswordAsPlain = true)]
    [RoutePrefix("api/v1/assignments")]
    public class AssignmentsController : BaseApiServiceController
    {
        private readonly IPlainStorageAccessor<Assignment> assignmentsStorage;
        private readonly IAssignmentViewFactory assignmentViewFactory;
        private readonly IMapper mapper;
        private readonly HqUserManager userManager;
        private readonly IPreloadedDataVerifier preloadedDataVerifier;
        private readonly IQuestionnaireStorage questionnaireStorage;

        public AssignmentsController(
            IAssignmentViewFactory assignmentViewFactory,
            IPlainStorageAccessor<Assignment> assignmentsStorage,
            IPreloadedDataVerifier preloadedDataVerifier,
            IMapper mapper,
            HqUserManager userManager,
            ILogger logger, IQuestionnaireStorage questionnaireStorage) : base(logger)
        {
            this.assignmentViewFactory = assignmentViewFactory;
            this.assignmentsStorage = assignmentsStorage;
            this.mapper = mapper;
            this.userManager = userManager;
            this.questionnaireStorage = questionnaireStorage;
            this.preloadedDataVerifier = preloadedDataVerifier;
        }

        /// <summary>
        /// Single assignment details
        /// </summary>
        /// <response code="200">Assignment details</response>
        /// <response code="404">Assignment cannot be found</response>
        [HttpGet]
        [Route("{id:int}")]
        public AssignmentDetails Details(int id)
        {
            var assignment = assignmentsStorage.GetById(id);

            if (assignment == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return this.mapper.Map<AssignmentDetails>(assignment);
        }

        /// <summary>
        /// List all assignments with filtering
        /// </summary>
        /// <param name="filter">List filter options</param>
        /// <returns>List of assignments</returns>
        [HttpGet]
        [Route("")]
        public AssignmentsListView List([FromUri(SuppressPrefixCheck = true, Name = "")] AssignmentsListFilter filter)
        {
            filter = filter ?? new AssignmentsListFilter
            {
                Page = 1,
                PageSize = 20
            };

            filter.PageSize = filter.PageSize == 0 ? 20 : Math.Min(filter.PageSize, 100);

            QuestionnaireIdentity questionnaireId;
            if (!QuestionnaireIdentity.TryParse(filter.QuestionnaireId, out questionnaireId))
            {
                questionnaireId = null;
            }

            var responsible = GetResponsibleIdPersonFromRequestValue(filter.Responsible);

            AssignmentsWithoutIdentifingData result = this.assignmentViewFactory.Load(new AssignmentsInputModel
            {
                QuestionnaireId = questionnaireId?.QuestionnaireId,
                QuestionnaireVersion = questionnaireId?.Version,
                ResponsibleId = responsible?.Id,
                Order = filter.Order,
                Page = Math.Max(filter.Page, 1),
                PageSize = filter.PageSize,
                SearchBy = filter.SearchBy,
                ShowArchive = filter.ShowArchive,
                SupervisorId = filter.SupervisorId
            });

            var listView = new AssignmentsListView(result.Page, result.PageSize, result.TotalCount, filter.Order);

            listView.Assignments = this.mapper.Map<List<AssignmentViewItem>>(result.Items);
            return listView;
        }

        /// <summary>
        /// Create new assignment
        /// </summary>
        /// <param name="createItem">New assignments options</param>
        /// <response code="200">Created assignment with details</response>
        /// <response code="400">Bad parameters provided or identifiying data incorrect. See response details for more info</response>
        /// <response code="404">Questionnaire or responsible user not found</response>
        /// <response code="406">Responsible user provided in request cannot be assigned to assignment</response>
        [HttpPost]
        [Route]
        public CreateAssignmentResult Create(CreateAssignmentApiRequest createItem)
        {
            var responsible = this.GetResponsibleIdPersonFromRequestValue(createItem?.Responsible);

            this.VerifyAssigneeInRoles(responsible, createItem?.Responsible, UserRoles.Interviewer, UserRoles.Supervisor);

            QuestionnaireIdentity questionnaireId;
            if (!QuestionnaireIdentity.TryParse(createItem.QuestionnaireId, out questionnaireId))
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, $@"Questionnaire not found: {createItem?.QuestionnaireId}"));
            }

            if (this.questionnaireStorage.GetQuestionnaireDocument(questionnaireId) == null)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.NotFound, $@"Questionnaire not found: {createItem?.QuestionnaireId}"));
            }

            var assignment = new Assignment(questionnaireId, responsible.Id, createItem.Capacity);

            try
            {
                assignment = this.mapper.Map(createItem, assignment);
            }
            catch (AutoMapperMappingException ame)
            {
                throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, ame.InnerException?.Message));
            }

            var preloadData = this.mapper.Map<PreloadedDataByFile>(assignment);

            var verifyResult = this.preloadedDataVerifier.VerifyAssignmentsSample(questionnaireId.QuestionnaireId, questionnaireId.Version, preloadData);
            verifyResult.WasResponsibleProvided = true;
            
            if (!verifyResult.Errors.Any())
            {
                this.assignmentsStorage.Store(assignment, null);
                assignment = this.assignmentsStorage.GetById(assignment.Id);
               
                return new CreateAssignmentResult
                {
                    Assignment = mapper.Map<AssignmentDetails>(assignment),
                    VerificationStatus = verifyResult
                };
            }

            throw new HttpResponseException(Request.CreateResponse(HttpStatusCode.BadRequest, new CreateAssignmentResult
            {
                Assignment = mapper.Map<AssignmentDetails>(assignment),
                VerificationStatus = verifyResult
            }));
        }

        /// <summary>
        /// Assign new responsible person for assignment
        /// </summary>
        /// <param name="id">Assignment id</param>
        /// <param name="assigneeRequest">Responsible user id or name</param>
        /// <response code="200">Assingment details with updated assignee</response>
        /// <response code="404">Assignment or assignee not found</response>
        /// <response code="406">Assignee cannot be assigned to assignment</response>
        [HttpPatch]
        [Route("{id:int}/assign")]
        public AssignmentDetails Assign(int id, [FromBody] AssignmentAssignRequest assigneeRequest)
        {
            var assignment = assignmentsStorage.GetById(id);

            if (assignment == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            var responsibleUser = this.GetResponsibleIdPersonFromRequestValue(assigneeRequest?.Responsible);

            this.VerifyAssigneeInRoles(responsibleUser, assigneeRequest?.Responsible, UserRoles.Interviewer, UserRoles.Supervisor);

            assignment.Reassign(responsibleUser.Id);

            assignmentsStorage.Store(assignment, id);

            return this.mapper.Map<AssignmentDetails>(assignmentsStorage.GetById(id));
        }

        private void VerifyAssigneeInRoles(HqUser responsibleUser, string providedValue, params UserRoles[] roles)
        {
            if (responsibleUser == null)
            {
                throw new HttpResponseException(this.Request.CreateResponse(HttpStatusCode.NotFound,
                    $@"User not found: {providedValue}"));
            }
            
            if (!roles.Any(responsibleUser.IsInRole))
            {
                throw new HttpResponseException(HttpStatusCode.NotAcceptable);
            }
        }

        private HqUser GetResponsibleIdPersonFromRequestValue(string responsible)
        {
            if (string.IsNullOrWhiteSpace(responsible))
            {
                return null;
            }

            Guid responsibleUserId;

            if (!Guid.TryParse(responsible, out responsibleUserId))
            {
                return this.userManager.FindByName(responsible);
            }

            return this.userManager.FindById(responsibleUserId);
        }

        /// <summary>
        /// Change assignments limit on created interviews
        /// </summary>
        /// <param name="id">Assignment id</param>
        /// <param name="capacity">New limit on created interviews</param>
        /// <response code="200">Assingment details with updated capacity</response>
        /// <response code="404">Assignment not found</response>
        [HttpPatch]
        [Route("{id:int}/changeCapacity")]
        public AssignmentDetails ChangeCapacity(int id, [FromBody] int? capacity)
        {
            var assignment = assignmentsStorage.GetById(id);

            if (assignment == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            assignment.UpdateCapacity(capacity);

            assignmentsStorage.Store(assignment, id);

            return this.mapper.Map<AssignmentDetails>(assignmentsStorage.GetById(id));
        }
    }
}