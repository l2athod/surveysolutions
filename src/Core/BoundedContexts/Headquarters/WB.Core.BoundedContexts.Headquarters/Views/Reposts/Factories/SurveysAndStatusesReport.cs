﻿using System;
using System.Linq;
using NHibernate.Persister.Entity;
using WB.Core.BoundedContexts.Headquarters.Resources;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.InputModels;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Infrastructure.Native.Storage.Postgre;
using WB.Infrastructure.Native.Utils;

namespace WB.Core.BoundedContexts.Headquarters.Views.Reposts.Factories
{
    internal class SurveysAndStatusesReport : ISurveysAndStatusesReport
    {
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaryReader;
        private readonly IUnitOfWork uow;

        public SurveysAndStatusesReport(IQueryableReadSideRepositoryReader<InterviewSummary> interviewSummaryReader,
            IUnitOfWork uow)
        {
            this.interviewSummaryReader = interviewSummaryReader;
            this.uow = uow;
        }

        public SurveysAndStatusesReportView Load(SurveysAndStatusesReportInputModel input)
        {
            var responsible = input.ResponsibleName?.ToLower();
            var teamLead = input.TeamLeadName?.ToLower();

            var queryForItems = this.interviewSummaryReader.Query(_ =>
            {
                var filteredInterviews = FilterByResponsibleOrTeamLead(_, responsible, teamLead, input.QuestionnaireId).Select(x => new
                {
                    x.QuestionnaireId,
                    x.QuestionnaireVersion,
                    x.QuestionnaireTitle,
                    x.ResponsibleName,
                    TeamLeadName = x.SupervisorName,
                    SupervisorAssigned = x.Status == InterviewStatus.SupervisorAssigned ? 1 : 0,
                    InterviewerAssigned = x.Status == InterviewStatus.InterviewerAssigned ? 1 : 0,
                    Completed = x.Status == InterviewStatus.Completed ? 1 : 0,
                    RejectedBySupervisor = x.Status == InterviewStatus.RejectedBySupervisor ? 1 : 0,
                    ApprovedBySupervisor = x.Status == InterviewStatus.ApprovedBySupervisor ? 1 : 0,
                    RejectedByHeadquarters = x.Status == InterviewStatus.RejectedByHeadquarters ? 1 : 0,
                    ApprovedByHeadquarters = x.Status == InterviewStatus.ApprovedByHeadquarters ? 1 : 0
                });

                if (input.QuestionnaireId.HasValue)
                {
                    return filteredInterviews
                        .GroupBy(x => new {x.QuestionnaireId, x.QuestionnaireVersion, x.QuestionnaireTitle})
                        .Select(x => new
                        {
                            QuestionnaireId = x.Key.QuestionnaireId,
                            QuestionnaireVersion = (long?)x.Key.QuestionnaireVersion,
                            QuestionnaireTitle = x.Key.QuestionnaireTitle,
                            SupervisorAssignedCount = (int?) x.Sum(y => y.SupervisorAssigned) ?? 0,
                            InterviewerAssignedCount = (int?) x.Sum(y => y.InterviewerAssigned) ?? 0,
                            CompletedCount = (int?) x.Sum(y => y.Completed) ?? 0,
                            RejectedBySupervisorCount = (int?) x.Sum(y => y.RejectedBySupervisor) ?? 0,
                            ApprovedBySupervisorCount = (int?) x.Sum(y => y.ApprovedBySupervisor) ?? 0,
                            RejectedByHeadquartersCount = (int?) x.Sum(y => y.RejectedByHeadquarters) ?? 0,
                            ApprovedByHeadquartersCount = (int?) x.Sum(y => y.ApprovedByHeadquarters) ?? 0,
                            TotalCount = x.Count()
                        });
                }
                else
                {
                    return filteredInterviews
                        .GroupBy(x => new {x.QuestionnaireId, x.QuestionnaireTitle})
                        .Select(x => new
                        {
                            QuestionnaireId = x.Key.QuestionnaireId,
                            QuestionnaireVersion = (long?)null,
                            QuestionnaireTitle = x.Key.QuestionnaireTitle,
                            SupervisorAssignedCount = (int?) x.Sum(y => y.SupervisorAssigned) ?? 0,
                            InterviewerAssignedCount = (int?) x.Sum(y => y.InterviewerAssigned) ?? 0,
                            CompletedCount = (int?) x.Sum(y => y.Completed) ?? 0,
                            RejectedBySupervisorCount = (int?) x.Sum(y => y.RejectedBySupervisor) ?? 0,
                            ApprovedBySupervisorCount = (int?) x.Sum(y => y.ApprovedBySupervisor) ?? 0,
                            RejectedByHeadquartersCount = (int?) x.Sum(y => y.RejectedByHeadquarters) ?? 0,
                            ApprovedByHeadquartersCount = (int?) x.Sum(y => y.ApprovedByHeadquarters) ?? 0,
                            TotalCount = x.Count()
                        });
                }
            });

            var reportLines = queryForItems.OrderUsingSortExpression(input.Order).Skip((input.Page - 1) * input.PageSize)
                .Take(input.PageSize).ToList().Select(x => new HeadquarterSurveysAndStatusesReportLine
                {
                    QuestionnaireId = x.QuestionnaireId,
                    QuestionnaireVersion = x.QuestionnaireVersion,
                    QuestionnaireTitle = x.QuestionnaireTitle,
                    SupervisorAssignedCount = x.SupervisorAssignedCount,
                    InterviewerAssignedCount = x.InterviewerAssignedCount,
                    CompletedCount = x.CompletedCount,
                    RejectedBySupervisorCount = x.RejectedBySupervisorCount,
                    ApprovedBySupervisorCount = x.ApprovedBySupervisorCount,
                    RejectedByHeadquartersCount = x.RejectedByHeadquartersCount,
                    ApprovedByHeadquartersCount = x.ApprovedByHeadquartersCount,
                    TotalCount = x.TotalCount
                }).ToList();

            HeadquarterSurveysAndStatusesReportLine totalRow = null;

            if (reportLines.Count != 1)
            {
                var queryForTotalRow = this.interviewSummaryReader.Query(_ =>
                    FilterByResponsibleOrTeamLead(_, responsible, teamLead, input.QuestionnaireId).Select(x => new
                        {
                            x.QuestionnaireId,
                            x.QuestionnaireVersion,
                            x.QuestionnaireTitle,
                            x.ResponsibleName,
                            TeamLeadName = x.SupervisorName,
                            SupervisorAssigned = x.Status == InterviewStatus.SupervisorAssigned ? 1 : 0,
                            InterviewerAssigned = x.Status == InterviewStatus.InterviewerAssigned ? 1 : 0,
                            Completed = x.Status == InterviewStatus.Completed ? 1 : 0,
                            RejectedBySupervisor = x.Status == InterviewStatus.RejectedBySupervisor ? 1 : 0,
                            ApprovedBySupervisor = x.Status == InterviewStatus.ApprovedBySupervisor ? 1 : 0,
                            RejectedByHeadquarters = x.Status == InterviewStatus.RejectedByHeadquarters ? 1 : 0,
                            ApprovedByHeadquarters = x.Status == InterviewStatus.ApprovedByHeadquarters ? 1 : 0
                        })
                        .GroupBy(x => 1)
                        .Select(x => new HeadquarterSurveysAndStatusesReportLine
                        {
                            SupervisorAssignedCount = (int?) x.Sum(y => y.SupervisorAssigned) ?? 0,
                            InterviewerAssignedCount = (int?) x.Sum(y => y.InterviewerAssigned) ?? 0,
                            CompletedCount = (int?) x.Sum(y => y.Completed) ?? 0,
                            RejectedBySupervisorCount = (int?) x.Sum(y => y.RejectedBySupervisor) ?? 0,
                            ApprovedBySupervisorCount = (int?) x.Sum(y => y.ApprovedBySupervisor) ?? 0,
                            RejectedByHeadquartersCount = (int?) x.Sum(y => y.RejectedByHeadquarters) ?? 0,
                            ApprovedByHeadquartersCount = (int?) x.Sum(y => y.ApprovedByHeadquarters) ?? 0,
                            TotalCount = x.Count()
                        }));


                totalRow = queryForTotalRow.FirstOrDefault();
            }


            int totalCount = GetTotalRowsCount(input);

            return new SurveysAndStatusesReportView
            {
                TotalCount = totalCount,
                Items = reportLines,
                TotalRow = totalRow 
            };
        }

        private int GetTotalRowsCount(SurveysAndStatusesReportInputModel input)
        {
            // https://github.com/nhibernate/nhibernate-core/issues/1123 cannot be done with linq. Projection queryover also fails
            #region Projection example
            //int totalCount = this.interviewSummaryReader.QueryOver(qo =>
            //{
            //    var query = qo;
            //    if (!string.IsNullOrWhiteSpace(responsible))
            //        query = query.WhereRestrictionOn(x => x.ResponsibleName).IsInsensitiveLike(responsible);

            //    if (!string.IsNullOrWhiteSpace(teamLead))
            //        query = query.WhereRestrictionOn(x => x.TeamLeadName).IsInsensitiveLike(teamLead);
            //    if (input.QuestionnaireId.HasValue)
            //    {
            //        query = query.Where(x => x.QuestionnaireId == input.QuestionnaireId);
            //    }

            //    ProjectionList selectedColumns;
            //    if (input.QuestionnaireId.HasValue)
            //    {
            //        selectedColumns = Projections.ProjectionList()
            //            .Add(Projections.Property(nameof(InterviewSummary.QuestionnaireId)))
            //            .Add(Projections.Property(nameof(InterviewSummary.QuestionnaireVersion)))
            //            .Add(Projections.Property(nameof(InterviewSummary.QuestionnaireTitle)));
            //    }
            //    else
            //    {
            //        selectedColumns = Projections.ProjectionList()
            //            .Add(Projections.GroupProperty(nameof(InterviewSummary.QuestionnaireId)), null)
            //            .Add(Projections.GroupProperty(nameof(InterviewSummary.QuestionnaireTitle)), null);
            //    }
            //    query = query.Select(
            //        Projections.Count(
            //            Projections.Distinct(
            //                selectedColumns
            //            )
            //        )
            //    );

            //    return query.List<int>()[0];
            //});
            #endregion

            string query = "SELECT COUNT(DISTINCT ";

            if (input.QuestionnaireId.HasValue)
            {
                query += "(questionnaireid, questionnairetitle, questionnaireversion))";
            }
            else
            {
                query += "(questionnaireid, questionnairetitle))";
            }


            var fullTableName = this.uow.Session.GetFullTableName<InterviewSummary>();
            query += Environment.NewLine + $"FROM {fullTableName}" + Environment.NewLine;

            query += "WHERE 1 = 1 ";

            if (!string.IsNullOrWhiteSpace(input.ResponsibleName))
                query += "AND responsiblename ILIKE :responsible" + Environment.NewLine;

            if (!string.IsNullOrWhiteSpace(input.TeamLeadName))
                query += "AND teamleadname ILIKE :teamlead" + Environment.NewLine;

            if (input.QuestionnaireId.HasValue)
            {
                query += "AND questionnaireid = :questionnaireId" + Environment.NewLine;;
            }

            var sqlQuery = this.uow.Session.CreateSQLQuery(query);

            if (!string.IsNullOrWhiteSpace(input.ResponsibleName))
                sqlQuery.SetParameter("responsible", input.ResponsibleName);
            if (!string.IsNullOrWhiteSpace(input.TeamLeadName))
                sqlQuery.SetParameter("teamlead", input.TeamLeadName);
            if (input.QuestionnaireId.HasValue)
                sqlQuery.SetParameter("questionnaireId", input.QuestionnaireId);

            return (int) sqlQuery.UniqueResult<long>();
        }

        private static IQueryable<InterviewSummary> FilterByResponsibleOrTeamLead(IQueryable<InterviewSummary> _,
            string responsible, string teamLead, Guid? questionnaireId)
        {
            if (!string.IsNullOrWhiteSpace(responsible))
                _ = _.Where(x => x.ResponsibleName.ToLower() == responsible);

            if (!string.IsNullOrWhiteSpace(teamLead))
                _ = _.Where(x => x.SupervisorName.ToLower() == teamLead);
            if (questionnaireId.HasValue)
            {
                _ = _.Where(x => x.QuestionnaireId == questionnaireId);
            }

            return _;
        }

        public ReportView GetReport(SurveysAndStatusesReportInputModel model)
        {
            var view = this.Load(model);

            var reportItems = view.Items.Select(x => new object[]
            {
                x.QuestionnaireTitle, x.QuestionnaireVersion, x.SupervisorAssignedCount, x.InterviewerAssignedCount,
                x.CompletedCount, x.RejectedBySupervisorCount, x.ApprovedBySupervisorCount,
                x.RejectedByHeadquartersCount, x.ApprovedByHeadquartersCount,
                x.TotalCount
            });

            if (view.TotalRow != null)
            {
                reportItems = new[]
                {
                    new object[]
                    {
                        Strings.AllQuestionnaires, "", view.TotalRow.SupervisorAssignedCount,
                        view.TotalRow.InterviewerAssignedCount,
                        view.TotalRow.CompletedCount, view.TotalRow.RejectedBySupervisorCount,
                        view.TotalRow.ApprovedBySupervisorCount,
                        view.TotalRow.RejectedByHeadquartersCount, view.TotalRow.ApprovedByHeadquartersCount,
                        view.TotalRow.TotalCount
                    }
                }.Concat(reportItems);
            }

            return new ReportView
            {
                Headers = new[]
                {
                    Report.COLUMN_QUESTIONNAIRE_TEMPLATE, Report.COLUMN_TEMPLATE_VERSION,
                    Report.COLUMN_SUPERVISOR_ASSIGNED, Report.COLUMN_INTERVIEWER_ASSIGNED,
                    Report.COLUMN_COMPLETED, Report.COLUMN_REJECTED_BY_SUPERVISOR, Report.COLUMN_APPROVED_BY_SUPERVISOR,
                    Report.COLUMN_REJECTED_BY_HQ, Report.COLUMN_APPROVED_BY_HQ,
                    Report.COLUMN_TOTAL
                },
                Data = reportItems.ToArray()
            };
        }
    }
}
