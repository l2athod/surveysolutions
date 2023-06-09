﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WB.Services.Export.Infrastructure;

namespace WB.Services.Export.Migrations
{
    [DbContext(typeof(TenantDbContext))]
    [Migration("20190208105751_AddingMetadataTable")]
    partial class AddingMetadataTable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.2.1-servicing-10028")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("WB.Services.Export.InterviewDataStorage.InterviewQuestionnaireReferenceNode", b =>
                {
                    b.Property<Guid>("InterviewId")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("interview_id");

                    b.Property<string>("QuestionnaireId")
                        .HasColumnName("questionnaire_id");

                    b.HasKey("InterviewId");

                    b.ToTable("interview__references");
                });

            modelBuilder.Entity("WB.Services.Export.InterviewDataStorage.Metadata", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<long>("GlobalSequence")
                        .HasColumnName("global_sequence");

                    b.HasKey("Id")
                        .HasName("pk_metadata");

                    b.ToTable("metadata");
                });
#pragma warning restore 612, 618
        }
    }
}
