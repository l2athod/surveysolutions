﻿using System.Collections.Generic;
using Main.Core.Entities.SubEntities;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.SurveyStatistics;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.SurveyStatistics.Data;
using WB.Core.BoundedContexts.Headquarters.Views.Reposts.Views;
using WB.Core.SharedKernels.DataCollection;
using WB.Tests.Abc;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.ReportsTests
{
    [TestFixture]
    public class when_build_report_with_CategoricalReportViewBuilder
    {
        private List<CategoricalOption> answers;
        private List<GetCategoricalReportItem> rows;
        private readonly string firstTeamLead = Id.g1.ToString();
        private readonly string secondTeamLead = Id.g2.ToString();

        readonly int answerNewYork = 1;
        readonly int answerWashington = 2;
        readonly int answerRural = 3;

        private const string interviewerA = "interviewerA";
        private const string interviewerB = "interviewerB";
        private const string interviewerC = "interviewerC";

        private ReportView report;

        [SetUp]
        public void Context()
        {
            this.answers = new List<CategoricalOption>
            {
                Create.Entity.CategoricalOption("New York", answerNewYork),
                Create.Entity.CategoricalOption("Washington", answerWashington),
                Create.Entity.CategoricalOption("Rural", answerRural)
            };

            GetCategoricalReportItem CreateRowItem(string teamLead, string responsible, int answer, long count)
            {
                return new GetCategoricalReportItem
                {
                    TeamLeadName = teamLead,
                    ResponsibleName = responsible,
                    Answer = answer,
                    Count = count
                };
            }

            this.rows = new List<GetCategoricalReportItem>
            {
                CreateRowItem(null, null,      answerNewYork, 100),
                CreateRowItem(null, null,      answerWashington, 200),
                CreateRowItem(null, null,      answerRural, 300),

                CreateRowItem(firstTeamLead, interviewerA,    answerNewYork, 10),
                CreateRowItem(firstTeamLead, interviewerA, answerWashington, 20),
                CreateRowItem(firstTeamLead, interviewerA,      answerRural, 30),


                CreateRowItem(secondTeamLead, interviewerB, answerWashington, 25),
                CreateRowItem(secondTeamLead, interviewerB,      answerRural, 35),
                CreateRowItem(secondTeamLead, interviewerB,    answerNewYork, 15), // changed order of answers

                CreateRowItem(firstTeamLead, interviewerC,    answerNewYork, 100),
                CreateRowItem(firstTeamLead, interviewerC, answerWashington, 200), // missing answer on rural
            };

            var subject = new CategoricalReportViewBuilder(answers, rows);
            this.report = subject.AsReportView();
        }

        [Test]
        public void should_return_columns_in_order_of_answers_then_total()
        {
            Assert.That(this.report.Columns, Is.EqualTo(new []
            {
                "TeamLead", "Responsible", "col_1", "col_2", "col_3", "total"
            }));
        }

        [Test]
        public void should_calculate_total_based_on_rows_with_null_tl_tm()
        {
            Assert.That(this.report.Totals, Is.EqualTo(new object[]
            {
                "All teams", "All interviewers", 100, 200, 300, 600
            }));
        }

        [Test]
        public void should_transpose_counts_into_columns()
        {
            report.Data.AssertEqualInAnyOrder(
                new object[]{ firstTeamLead, interviewerA, 10L, 20L, 30L, 60L },
                new object[]{ firstTeamLead, interviewerC, 100L, 200L, 0L, 300L },
                new object[]{ secondTeamLead, interviewerB, 15L, 25L, 35L, 75L }
            );
        }
    }
}
