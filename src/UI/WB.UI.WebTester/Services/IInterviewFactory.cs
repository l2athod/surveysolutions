﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.UI.WebTester.Resources;

namespace WB.UI.WebTester.Services
{
    public enum CreationResult
    {
        DataRestored,
        EmptyCreated
    }

    public interface IInterviewFactory
    {
        void CreateInterview(QuestionnaireIdentity questionnaireIdentity, Guid id);

        CreationResult CreateInterview(QuestionnaireIdentity questionnaireIdentity, Guid id, Guid originalInterviewId);
    }

    public class InterviewFactory : IInterviewFactory
    {
        private readonly ICacheStorage<List<ICommand>, Guid> executedCommandsStorage;
        private readonly ICommandService commandService;
        private readonly IAppdomainsPerInterviewManager appdomainsPerInterviewManager;
        private readonly IAggregateRootCacheCleaner mainDomainShadowCleaner;

        public InterviewFactory(ICacheStorage<List<ICommand>, Guid> executedCommandsStorage,
            ICommandService commandService, 
            IAppdomainsPerInterviewManager appdomainsPerInterviewManager, 
            IAggregateRootCacheCleaner mainDomainShadowCleaner)
        {
            this.executedCommandsStorage = executedCommandsStorage ?? throw new ArgumentNullException(nameof(executedCommandsStorage));
            this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            this.appdomainsPerInterviewManager = appdomainsPerInterviewManager;
            this.mainDomainShadowCleaner = mainDomainShadowCleaner;
        }

        public void CreateInterview(QuestionnaireIdentity questionnaireIdentity, Guid id)
        {
            var createInterview = new CreateInterview(
                interviewId: id,
                userId: Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                questionnaireId: questionnaireIdentity,
                answers: new List<InterviewAnswer>(),
                answersTime: DateTime.UtcNow,
                supervisorId: Guid.NewGuid(),
                interviewerId: Guid.NewGuid(),
                interviewKey: new InterviewKey(new Random().Next(99999999)),
                assignmentId: null);

            this.commandService.Execute(createInterview);
        }

        public CreationResult CreateInterview(QuestionnaireIdentity questionnaireIdentity, Guid id, Guid originalInterviewId)
        {
            try
            {
                var existingInterviewCommands = this.executedCommandsStorage.Get(originalInterviewId, originalInterviewId);
                foreach (var existingInterviewCommand in existingInterviewCommands.Cast<InterviewCommand>())
                {
                    if (existingInterviewCommand is CreateInterview createCommand)
                    {
                        createCommand.QuestionnaireId = questionnaireIdentity;
                    }
                    existingInterviewCommand.InterviewId = id;
                    this.commandService.Execute(existingInterviewCommand);
                }

                return CreationResult.DataRestored;
            }
            catch (Exception)
            {
                appdomainsPerInterviewManager.Flush(id);
                mainDomainShadowCleaner.Evict(id);
                this.CreateInterview(questionnaireIdentity, id);
                return CreationResult.EmptyCreated;
            }
        }
    }
}