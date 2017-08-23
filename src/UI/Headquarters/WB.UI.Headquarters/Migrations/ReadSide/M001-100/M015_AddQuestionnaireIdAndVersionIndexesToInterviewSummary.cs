﻿using FluentMigrator;

namespace WB.UI.Headquarters.Migrations.ReadSide
{
    [Migration(15)]
    public class M015_AddQuestionnaireIdAndVersionIndexesToInterviewSummary : Migration
    {
        public override void Up()
        {
            Create.Index("interviewsummaries_qiestionnaire_id_indx").OnTable("interviewsummaries").OnColumn("questionnaireid");
            Create.Index("interviewsummaries_qiestionnaire_version_indx").OnTable("interviewsummaries").OnColumn("questionnaireversion");
        }

        public override void Down()
        {
            Delete.Index("interviewsummaries_qiestionnaire_id_indx");
            Delete.Index("interviewsummaries_qiestionnaire_version_indx");
        }
    }
}