﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WB.Services.Export.Infrastructure;

namespace WB.Services.Export.Migrations
{
    [DbContext(typeof(TenantDbContext))]
    partial class TenantDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.2-servicing-10034")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("WB.Services.Export.InterviewDataStorage.GeneratedQuestionnaireReference", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<DateTime?>("DeletedAt")
                        .HasColumnName("deleted_at");

                    b.HasKey("Id")
                        .HasName("pk_generated_questionnaires");

                    b.ToTable("__generated_questionnaire_reference");
                });

            modelBuilder.Entity("WB.Services.Export.InterviewDataStorage.InterviewReference", b =>
                {
                    b.Property<Guid>("InterviewId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("interview_id");

                    b.Property<DateTime?>("DeletedAtUtc")
                        .HasColumnName("deleted_at_utc");

                    b.Property<string>("Key")
                        .HasColumnName("key");

                    b.Property<string>("QuestionnaireId")
                        .HasColumnName("questionnaire_id");

                    b.Property<int>("Status")
                        .HasColumnName("status");

                    b.Property<DateTime?>("UpdateDateUtc")
                        .HasColumnName("update_date_utc");

                    b.HasKey("InterviewId");

                    b.ToTable("interview__references");
                });

            modelBuilder.Entity("WB.Services.Export.InterviewDataStorage.Metadata", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("Value")
                        .HasColumnName("value");

                    b.HasKey("Id")
                        .HasName("pk_metadata");

                    b.ToTable("metadata");
                });
#pragma warning restore 612, 618
        }
    }
}
