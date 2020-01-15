﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WB.Core.BoundedContexts.Headquarters.EmailProviders;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Utils;

namespace WB.UI.Headquarters.Controllers
{
    public partial class WebInterviewController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> EmailLink(string interviewId, string email)
        {
            var assignmentId = interviewSummary.GetById(interviewId)?.AssignmentId ?? 0;
            var assignment = assignments.GetAssignment(assignmentId);

            int invitationId = invitationService.CreateInvitationForPublicLink(assignment, interviewId);
            
            try
            {
                await invitationMailingService.SendResumeAsync(invitationId, assignment, email);
                if (Request.Cookies[AskForEmail] != null)
                {
                    Response.Cookies.Delete(AskForEmail);
                }

                return this.Json("ok");
            }
            catch (EmailServiceException e)
            {
                invitationService.InvitationWasNotSent(invitationId, assignmentId, email, e.Message);
                return this.Json("fail");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Audio(Guid interviewId, string questionId, IFormFile file)
        {
            IStatefulInterview interview = this.statefulInterviewRepository.Get(interviewId.FormatGuid());

            var questionIdentity = Identity.Parse(questionId);
            InterviewTreeQuestion question = interview.GetQuestion(questionIdentity);

            if (!interview.AcceptsInterviewerAnswers() && question.IsAudio)
            {
                return this.Json("fail");
            }
            try
            {
                using var ms = new MemoryStream();

                await file.CopyToAsync(ms);

                byte[] bytes = ms.ToArray();

                var audioInfo = await this.audioProcessingService.CompressAudioFileAsync(bytes);

                var fileName = $@"{question.VariableName}__{questionIdentity.RosterVector}.m4a";

                audioFileStorage.StoreInterviewBinaryData(interviewId, fileName, audioInfo.Binary, audioInfo.MimeType);

                var command = new AnswerAudioQuestionCommand(interview.Id,
                    interview.CurrentResponsibleId, questionIdentity.Id, questionIdentity.RosterVector,
                    fileName, audioInfo.Duration);

                this.commandService.Execute(command);
            }
            catch (Exception e)
            {
                webInterviewNotificationService.MarkAnswerAsNotSaved(interviewId, questionIdentity, e);
                throw;
            }
            return this.Json("ok");
        }

        [HttpPost]
        public async Task<ActionResult> Image(Guid interviewId, string questionId, IFormFile file)
        {
            IStatefulInterview interview = this.statefulInterviewRepository.Get(interviewId.FormatGuid());

            var questionIdentity = Identity.Parse(questionId);
            var question = interview.GetQuestion(questionIdentity);

            if (!interview.AcceptsInterviewerAnswers() && question.IsMultimedia)
            {
                return this.Json("fail");
            }

            string filename = null;

            try
            {
                using var ms = new MemoryStream();

                await file.CopyToAsync(ms);

                this.imageProcessingService.Validate(ms.ToArray());

                filename = AnswerUtils.GetPictureFileName(question.VariableName, questionIdentity.RosterVector);
                var responsibleId = interview.CurrentResponsibleId;

                this.commandService.Execute(new AnswerPictureQuestionCommand(interview.Id,
                    responsibleId, questionIdentity.Id, questionIdentity.RosterVector, filename));

                this.imageFileStorage.StoreInterviewBinaryData(interview.Id, filename, ms.ToArray(), file.ContentType);
            }
            catch (Exception e) 
            {
                if(filename != null)
                    await this.imageFileStorage.RemoveInterviewBinaryData(interview.Id, filename);

                webInterviewNotificationService.MarkAnswerAsNotSaved(interviewId, questionIdentity, e);
                throw;
            }
            return this.Json("ok");
        }
    }
}