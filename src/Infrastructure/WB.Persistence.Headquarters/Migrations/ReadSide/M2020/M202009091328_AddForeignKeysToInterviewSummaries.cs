using System.Data;
using FluentMigrator;

namespace WB.Persistence.Headquarters.Migrations.ReadSide
{
    [Migration(202009091328)]
    public class M202009091328_AddForeignKeysToInterviewSummaries : Migration
    {
        public override void Up()
        {
            Delete.Index("interviewsummaries_interviewid_idx")
                .OnTable("interviewsummaries").InSchema("readside");
                
            Create.Index("interviewsummaries_interviewid_unique_idx")
                .OnTable("interviewsummaries").InSchema("readside")
                .OnColumn("interviewid").Unique();
            
            Create.ForeignKey("fk_interview_geo_answers_to_interviewsummaries")
                .FromTable("interview_geo_answers").InSchema("readside").ForeignColumn("interview_id")
                .ToTable("interviewsummaries").InSchema("readside").PrimaryColumn("id")
                .OnDelete(Rule.Cascade);
            
            Create.ForeignKey("fk_report_statistics_to_interviewsummaries")
                .FromTable("report_statistics").InSchema("readside").ForeignColumn("interview_id")
                .ToTable("interviewsummaries").InSchema("readside").PrimaryColumn("id")
                .OnDelete(Rule.Cascade);

            Create.ForeignKey("fk_cumulativereportstatuschanges_to_interviewsummaries")
                .FromTable("cumulativereportstatuschanges").InSchema("readside").ForeignColumn("interviewid")
                .ToTable("interviewsummaries").InSchema("readside").PrimaryColumn("interviewid")
                .OnDelete(Rule.Cascade);

            Create.ForeignKey("fk_commentaries_to_interviewsummaries")
                .FromTable("commentaries").InSchema("readside").ForeignColumn("summary_id")
                .ToTable("interviewsummaries").InSchema("readside").PrimaryColumn("id")
                .OnDelete(Rule.Cascade);

            Delete.ForeignKey("FK_answerstofeaturedquestions_interview_id_interviewsummaries_i")
                .OnTable("answerstofeaturedquestions").InSchema("readside");
            
            Create.ForeignKey("FK_answerstofeaturedquestions_interview_id_interviewsummaries_i")
                .FromTable("answerstofeaturedquestions").InSchema("readside").ForeignColumn("interview_id")
                .ToTable("interviewsummaries").InSchema("readside").PrimaryColumn("id")
                .OnDelete(Rule.Cascade);
        }

        public override void Down()
        {
            
        }
    }
}
