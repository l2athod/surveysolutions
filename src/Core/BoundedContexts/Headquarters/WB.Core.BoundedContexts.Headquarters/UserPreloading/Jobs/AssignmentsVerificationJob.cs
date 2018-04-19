﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Quartz;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.Views.SampleImport;
using WB.Core.GenericSubdomains.Portable.ServiceLocation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Infrastructure.Native.Threading;

namespace WB.Core.BoundedContexts.Headquarters.UserPreloading.Jobs
{
    [DisallowConcurrentExecution]
    internal class AssignmentsVerificationJob : IJob
    {
        private ILogger logger => ServiceLocator.Current.GetInstance<ILoggerProvider>()
            .GetFor<AssignmentsVerificationJob>();
        
        private IAssignmentsImportService importAssignmentsService => ServiceLocator.Current
            .GetInstance<IAssignmentsImportService>();

        private IPreloadedDataVerifier importAssignmentsVerifier => ServiceLocator.Current
            .GetInstance<IPreloadedDataVerifier>();

        private IQuestionnaireStorage questionnaireStorage => ServiceLocator.Current
            .GetInstance<IQuestionnaireStorage>();

        private IPlainTransactionManager plainTransactionManager => ServiceLocator.Current
            .GetInstance<IPlainTransactionManager>();
        
        private SampleImportSettings sampleImportSettings => ServiceLocator.Current
            .GetInstance<SampleImportSettings>();

        private T ExecuteInPlain<T>(Func<T> func) => this.plainTransactionManager.ExecuteInPlainTransaction(func);
        private void ExecuteInPlain(Action func) => this.plainTransactionManager.ExecuteInPlainTransaction(func);


        public void Execute(IJobExecutionContext context)
        {
            try
            {
                var importProcess = this.ExecuteInPlain(() => this.importAssignmentsService.GetImportStatus());
                if (importProcess == null) return;

                var allAssignmentIds = this.ExecuteInPlain(() => this.importAssignmentsService.GetAllAssignmentIdsToVerify());
                if (allAssignmentIds.Length == 0) return;

                this.logger.Debug("Assignments verification job: Started");

                var sw = new Stopwatch();
                sw.Start();

                Parallel.ForEach(allAssignmentIds,
                    new ParallelOptions { MaxDegreeOfParallelism = this.sampleImportSettings.InterviewsImportParallelTasksLimit },
                    assignmentId =>
                    {
                        try
                        {
                            ThreadMarkerManager.MarkCurrentThreadAsIsolated();

                            var assignmentToVerify = this.ExecuteInPlain(() => this.importAssignmentsService.GetAssignmentById(assignmentId));
                            if (assignmentToVerify == null) return;

                            var questionnaire = this.ExecuteInPlain(() => this.questionnaireStorage.GetQuestionnaire(importProcess.QuestionnaireIdentity, null));
                            if (questionnaire == null)
                            {
                                this.ExecuteInPlain(() => this.importAssignmentsService.RemoveAssignmentToImport(assignmentToVerify.Id));
                                return;
                            }

                            var error = this.ExecuteInPlain(() =>
                                this.importAssignmentsVerifier.VerifyWithInterviewTree(
                                    assignmentToVerify.Answers,
                                    assignmentToVerify.Interviewer ?? assignmentToVerify.Supervisor,
                                    questionnaire));

                            this.ExecuteInPlain(() => this.importAssignmentsService.SetVerifiedToAssignment(assignmentToVerify.Id, error?.ErrorMessage));
                        }
                        finally
                        {
                            ThreadMarkerManager.ReleaseCurrentThreadFromIsolation();
                        }
                    });

                sw.Stop();
                this.logger.Debug($"Assignments verfication job: Finished. Elapsed time: {sw.Elapsed}");
            }
            catch (Exception ex)
            {
                this.logger.Error($"Assignments verification job: FAILED. Reason: {ex.Message} ", ex);
            }
        }
    }
}
